using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// A static class with functions to convert lists of tokens between
	/// different IOB-style representations.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class IOBUtils
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sequences.IOBUtils));

		private IOBUtils()
		{
		}

		// static methods
		/// <summary>
		/// This can be used to map from any IOB-style (i.e., "I-PERS" style labels)
		/// or just categories representation to any other.
		/// </summary>
		/// <remarks>
		/// This can be used to map from any IOB-style (i.e., "I-PERS" style labels)
		/// or just categories representation to any other.
		/// It can read and change any representation to other representations:
		/// a 4 way representation of all entities, like S-PERS, B-PERS,
		/// I-PERS, E-PERS for single word, beginning, internal, and end of entity
		/// (IOBES or SBIEO); always marking the first word of an entity (IOB2 or BIO);
		/// only marking specially the beginning of non-first
		/// items of an entity sequences with B-PERS (IOB1);
		/// the reverse IOE1 and IOE2; IO where everything is I-tagged; and
		/// NOPREFIX, where no prefixes are written on category labels.
		/// The last two representations are deficient in not allowing adjacent
		/// entities of the same class to be represented, but nevertheless
		/// convenient.  Note that the background label is never given a prefix.
		/// This code is very specific to the particular CoNLL way of labeling
		/// classes for IOB-style encoding, but this notation is quite widespread.
		/// It will work on any of these styles of input.
		/// This will also recognize BILOU format (B=B, I=I, L=E, O=O, U=S).
		/// It also works with lowercased names like i-org.
		/// If the labels are not of the form "C-Y+", where C is a single character,
		/// then they will be regarded as NOPREFIX labels.
		/// This method updates the List tokens in place.
		/// </remarks>
		/// <param name="tokens">List of tokens (each a CoreLabel) in some style</param>
		/// <param name="key">The key in the CoreLabel to change, commonly CoreAnnotations.AnswerAnnotation.class</param>
		/// <param name="backgroundLabel">The background label, which gets special treatment</param>
		/// <param name="style">Output style; one of iob[12], ioe[12], io, sbieo/iobes, noprefix</param>
		/// <param name="intern">Whether to String-intern the new labels (may as well, small number!)</param>
		public static void EntitySubclassify<Tok>(IList<TOK> tokens, Type key, string backgroundLabel, string style, bool intern)
			where Tok : ICoreMap
		{
			int how;
			string lowerStyle = style.ToLower(Locale.English);
			switch (lowerStyle)
			{
				case "iob1":
				{
					how = 0;
					break;
				}

				case "iob2":
				case "bio":
				{
					how = 1;
					break;
				}

				case "ioe1":
				{
					how = 2;
					break;
				}

				case "ioe2":
				{
					how = 3;
					break;
				}

				case "io":
				{
					how = 4;
					break;
				}

				case "sbieo":
				case "iobes":
				{
					how = 5;
					break;
				}

				case "noprefix":
				{
					how = 6;
					break;
				}

				case "bilou":
				{
					how = 7;
					break;
				}

				default:
				{
					throw new ArgumentException("entitySubclassify: unknown style: " + style);
				}
			}
			IList<TOK> paddedTokens = new PaddedList<TOK>(tokens, (TOK)new CoreLabel());
			int size = paddedTokens.Count;
			string[] newAnswers = new string[size];
			for (int i = 0; i < size; i++)
			{
				TOK c = paddedTokens[i];
				TOK p = paddedTokens[i - 1];
				TOK n = paddedTokens[i + 1];
				string cAns = c.Get(key);
				string pAns = p.Get(key);
				if (pAns == null)
				{
					pAns = backgroundLabel;
				}
				string nAns = n.Get(key);
				if (nAns == null)
				{
					nAns = backgroundLabel;
				}
				string @base;
				char prefix;
				if (cAns.Length > 2 && cAns[1] == '-')
				{
					@base = Sharpen.Runtime.Substring(cAns, 2, cAns.Length);
					prefix = char.ToUpperCase(cAns[0]);
				}
				else
				{
					@base = cAns;
					prefix = ' ';
				}
				string pBase;
				char pPrefix;
				if (pAns.Length > 2 && pAns[1] == '-')
				{
					pBase = Sharpen.Runtime.Substring(pAns, 2, pAns.Length);
					pPrefix = char.ToUpperCase(pAns[0]);
				}
				else
				{
					pBase = pAns;
					pPrefix = ' ';
				}
				string nBase;
				char nPrefix;
				if (nAns.Length > 2 && nAns[1] == '-')
				{
					nBase = Sharpen.Runtime.Substring(nAns, 2, nAns.Length);
					nPrefix = char.ToUpperCase(nAns[0]);
				}
				else
				{
					nBase = nAns;
					nPrefix = ' ';
				}
				bool isStartAdjacentSame = IsSameEntityBoundary(pBase, pPrefix, @base, prefix);
				bool isEndAdjacentSame = IsSameEntityBoundary(@base, prefix, nBase, nPrefix);
				bool isFirst = IsDifferentEntityBoundary(pBase, @base) || isStartAdjacentSame;
				bool isLast = IsDifferentEntityBoundary(@base, nBase) || isEndAdjacentSame;
				string newAnswer = @base;
				if (!@base.Equals(backgroundLabel))
				{
					switch (how)
					{
						case 0:
						{
							// iob1, only B if adjacent
							if (isStartAdjacentSame)
							{
								newAnswer = "B-" + @base;
							}
							else
							{
								newAnswer = "I-" + @base;
							}
							break;
						}

						case 1:
						{
							// iob2 always B at start
							if (isFirst)
							{
								newAnswer = "B-" + @base;
							}
							else
							{
								newAnswer = "I-" + @base;
							}
							break;
						}

						case 2:
						{
							// ioe1
							if (isEndAdjacentSame)
							{
								newAnswer = "E-" + @base;
							}
							else
							{
								newAnswer = "I-" + @base;
							}
							break;
						}

						case 3:
						{
							// ioe2
							if (isLast)
							{
								newAnswer = "E-" + @base;
							}
							else
							{
								newAnswer = "I-" + @base;
							}
							break;
						}

						case 4:
						{
							newAnswer = "I-" + @base;
							break;
						}

						case 5:
						{
							if (isFirst && isLast)
							{
								newAnswer = "S-" + @base;
							}
							else
							{
								if ((!isFirst) && isLast)
								{
									newAnswer = "E-" + @base;
								}
								else
								{
									if (isFirst && (!isLast))
									{
										newAnswer = "B-" + @base;
									}
									else
									{
										newAnswer = "I-" + @base;
									}
								}
							}
							break;
						}

						case 7:
						{
							// nothing to do on case 6 as it's just base
							if (isFirst && isLast)
							{
								newAnswer = "U-" + @base;
							}
							else
							{
								if ((!isFirst) && isLast)
								{
									newAnswer = "L-" + @base;
								}
								else
								{
									if (isFirst && (!isLast))
									{
										newAnswer = "B-" + @base;
									}
									else
									{
										newAnswer = "I-" + @base;
									}
								}
							}
							break;
						}
					}
				}
				if (intern)
				{
					newAnswer = string.Intern(newAnswer);
				}
				newAnswers[i] = newAnswer;
			}
			for (int i_1 = 0; i_1 < size; i_1++)
			{
				TOK c = tokens[i_1];
				c.Set(typeof(CoreAnnotations.AnswerAnnotation), newAnswers[i_1]);
			}
		}

		public static bool IsEntityBoundary(string beforeEntity, char beforePrefix, string afterEntity, char afterPrefix)
		{
			return !beforeEntity.Equals(afterEntity) || afterPrefix == 'B' || afterPrefix == 'S' || afterPrefix == 'U' || beforePrefix == 'E' || beforePrefix == 'L' || beforePrefix == 'S' || beforePrefix == 'U';
		}

		public static bool IsSameEntityBoundary(string beforeEntity, char beforePrefix, string afterEntity, char afterPrefix)
		{
			return beforeEntity.Equals(afterEntity) && (afterPrefix == 'B' || afterPrefix == 'S' || afterPrefix == 'U' || beforePrefix == 'E' || beforePrefix == 'L' || beforePrefix == 'S' || beforePrefix == 'U');
		}

		public static bool IsDifferentEntityBoundary(string beforeEntity, string afterEntity)
		{
			return !beforeEntity.Equals(afterEntity);
		}

		/// <summary>
		/// For a sequence labeling task with multi-token entities, like NER,
		/// this works out TP, FN, FP counts that can be used for entity-level
		/// F1 results.
		/// </summary>
		/// <remarks>
		/// For a sequence labeling task with multi-token entities, like NER,
		/// this works out TP, FN, FP counts that can be used for entity-level
		/// F1 results. This works with any kind of prefixed IOB labeling, or
		/// just with simply entity names (also treated as IO labeling).
		/// </remarks>
		/// <param name="doc">The document (with Answer and GoldAnswer annotations) to score</param>
		/// <param name="entityTP">Counter from entity type to count of true positives</param>
		/// <param name="entityFP">Counter from entity type to count of false positives</param>
		/// <param name="entityFN">Counter from entity type to count of false negatives</param>
		/// <param name="background">
		/// The background symbol. Normally it isn't counted in entity-level
		/// F1 scores. If you want it counted, pass in null for this.
		/// </param>
		/// <returns>
		/// Whether scoring was successful (it'll only be unsuccessful if information
		/// is missing or ill-formed in the doc).
		/// </returns>
		public static bool CountEntityResults<_T0>(IList<_T0> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN, string background)
			where _T0 : ICoreMap
		{
			bool entityCorrect = true;
			// the annotations
			string previousGold = background;
			string previousGuess = background;
			// the part after the I- or B- in the annotation
			string previousGoldEntity = string.Empty;
			string previousGuessEntity = string.Empty;
			char previousGoldPrefix = ' ';
			char previousGuessPrefix = ' ';
			foreach (ICoreMap word in doc)
			{
				string gold = word.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				string guess = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string goldEntity;
				string guessEntity;
				char goldPrefix;
				char guessPrefix;
				if (gold == null || gold.IsEmpty())
				{
					log.Info("Missing gold entity");
					return false;
				}
				else
				{
					if (gold.Length > 2 && gold[1] == '-')
					{
						goldEntity = Sharpen.Runtime.Substring(gold, 2, gold.Length);
						goldPrefix = char.ToUpperCase(gold[0]);
					}
					else
					{
						goldEntity = gold;
						goldPrefix = ' ';
					}
				}
				if (guess == null || guess.IsEmpty())
				{
					log.Info("Missing guess entity");
					return false;
				}
				else
				{
					if (guess.Length > 2 && guess[1] == '-')
					{
						guessEntity = Sharpen.Runtime.Substring(guess, 2, guess.Length);
						guessPrefix = char.ToUpperCase(guess[0]);
					}
					else
					{
						guessEntity = guess;
						guessPrefix = ' ';
					}
				}
				//System.out.println("Gold: " + gold + " (" + goldPrefix + ' ' + goldEntity + "); " +
				//        "Guess: " + guess + " (" + guessPrefix + ' ' + guessEntity + ')');
				bool newGold = !gold.Equals(background) && IsEntityBoundary(previousGoldEntity, previousGoldPrefix, goldEntity, goldPrefix);
				bool newGuess = !guess.Equals(background) && IsEntityBoundary(previousGuessEntity, previousGuessPrefix, guessEntity, guessPrefix);
				bool goldEnded = !previousGold.Equals(background) && IsEntityBoundary(previousGoldEntity, previousGoldPrefix, goldEntity, goldPrefix);
				bool guessEnded = !previousGuess.Equals(background) && IsEntityBoundary(previousGuessEntity, previousGuessPrefix, guessEntity, guessPrefix);
				// System.out.println("  newGold " + newGold + "; newGuess " + newGuess +
				//        "; goldEnded:" + goldEnded + "; guessEnded: " + guessEnded);
				if (goldEnded)
				{
					if (guessEnded)
					{
						if (entityCorrect)
						{
							entityTP.IncrementCount(previousGoldEntity);
						}
						else
						{
							// same span but wrong label
							entityFN.IncrementCount(previousGoldEntity);
							entityFP.IncrementCount(previousGuessEntity);
						}
						entityCorrect = goldEntity.Equals(guessEntity);
					}
					else
					{
						entityFN.IncrementCount(previousGoldEntity);
						entityCorrect = gold.Equals(background) && guess.Equals(background);
					}
				}
				else
				{
					if (guessEnded)
					{
						entityCorrect = false;
						entityFP.IncrementCount(previousGuessEntity);
					}
				}
				// nothing to do if neither gold nor guess have ended (a category change signals an end)
				if (newGold)
				{
					if (newGuess)
					{
						entityCorrect = guessEntity.Equals(goldEntity);
					}
					else
					{
						entityCorrect = false;
					}
				}
				else
				{
					if (newGuess)
					{
						entityCorrect = false;
					}
				}
				previousGold = gold;
				previousGuess = guess;
				previousGoldEntity = goldEntity;
				previousGuessEntity = guessEntity;
				previousGoldPrefix = goldPrefix;
				previousGuessPrefix = guessPrefix;
			}
			// At the end, we need to check the last entity
			if (!previousGold.Equals(background))
			{
				if (entityCorrect)
				{
					entityTP.IncrementCount(previousGoldEntity);
				}
				else
				{
					entityFN.IncrementCount(previousGoldEntity);
				}
			}
			if (!previousGuess.Equals(background))
			{
				if (!entityCorrect)
				{
					entityFP.IncrementCount(previousGuessEntity);
				}
			}
			return true;
		}

		/// <summary>Converts entity representation of a file.</summary>
		public static void Main(string[] args)
		{
			// todo!
			if (args.Length == 0)
			{
			}
			else
			{
				foreach (string arg in args)
				{
				}
			}
		}
	}
}
