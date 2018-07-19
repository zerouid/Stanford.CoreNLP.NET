using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseCharacterBasedLexicon : ILexicon
	{
		private readonly double lengthPenalty;

		private readonly int penaltyType;

		private IDictionary<IList, Distribution<ChineseCharacterBasedLexicon.Symbol>> charDistributions;

		private ICollection<ChineseCharacterBasedLexicon.Symbol> knownChars;

		private Distribution<string> POSDistribution;

		private readonly bool useUnknownCharacterModel;

		private const int ContextLength = 2;

		private readonly IIndex<string> wordIndex;

		private readonly IIndex<string> tagIndex;

		public ChineseCharacterBasedLexicon(ChineseTreebankParserParams @params, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			// penaltyType should be set as follows:
			// 0: no length penalty
			// 1: quadratic length penalty
			// 2: penalty for continuation chars only
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			this.lengthPenalty = @params.lengthPenalty;
			this.penaltyType = @params.penaltyType;
			this.useUnknownCharacterModel = @params.useUnknownCharacterModel;
		}

		[System.NonSerialized]
		private IList<IList<TaggedWord>> trainingSentences;

		// We need to make two passes over the data, whereas the calling
		// routines only pass in the sentences or trees once, so we keep all
		// the sentences and then process them at the end
		public virtual void InitializeTraining(double numTrees)
		{
			trainingSentences = new List<IList<TaggedWord>>();
		}

		/// <summary>Train this lexicon on the given set of trees.</summary>
		public virtual void Train(ICollection<Tree> trees)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, 1.0);
			}
		}

		/// <summary>Train this lexicon on the given set of trees.</summary>
		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		/// <summary>TODO: make this method do something with the weight</summary>
		public virtual void Train(Tree tree, double weight)
		{
			trainingSentences.Add(tree.TaggedYield());
		}

		public virtual void TrainUnannotated(IList<TaggedWord> sentence, double weight)
		{
			// TODO: for now we just punt on these
			throw new NotSupportedException("This version of the parser does not support non-tree training data");
		}

		public virtual void IncrementTreesRead(double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void Train(TaggedWord tw, int loc, double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void Train(IList<TaggedWord> sentence, double weight)
		{
			trainingSentences.Add(sentence);
		}

		public virtual void FinishTraining()
		{
			Timing.Tick("Counting characters...");
			ClassicCounter<ChineseCharacterBasedLexicon.Symbol> charCounter = new ClassicCounter<ChineseCharacterBasedLexicon.Symbol>();
			// first find all chars that occur only once
			foreach (IList<TaggedWord> labels in trainingSentences)
			{
				foreach (TaggedWord label in labels)
				{
					string word = label.Word();
					if (word.Equals(LexiconConstants.Boundary))
					{
						continue;
					}
					for (int j = 0; j < length; j++)
					{
						ChineseCharacterBasedLexicon.Symbol sym = ChineseCharacterBasedLexicon.Symbol.CannonicalSymbol(word[j]);
						charCounter.IncrementCount(sym);
					}
					charCounter.IncrementCount(ChineseCharacterBasedLexicon.Symbol.EndWord);
				}
			}
			ICollection<ChineseCharacterBasedLexicon.Symbol> singletons = Counters.KeysBelow(charCounter, 1.5);
			knownChars = Generics.NewHashSet(charCounter.KeySet());
			Timing.Tick("Counting nGrams...");
			GeneralizedCounter[] POSspecificCharNGrams = new GeneralizedCounter[ContextLength + 1];
			for (int i = 0; i <= ContextLength; i++)
			{
				POSspecificCharNGrams[i] = new GeneralizedCounter(i + 2);
			}
			ClassicCounter<string> POSCounter = new ClassicCounter<string>();
			IList<ISerializable> context = new List<ISerializable>(ContextLength + 1);
			foreach (IList<TaggedWord> words in trainingSentences)
			{
				foreach (TaggedWord taggedWord in words)
				{
					string word = taggedWord.Word();
					string tag = taggedWord.Tag();
					tagIndex.Add(tag);
					if (word.Equals(LexiconConstants.Boundary))
					{
						continue;
					}
					POSCounter.IncrementCount(tag);
					for (int i_1 = 0; i_1 <= size; i_1++)
					{
						ChineseCharacterBasedLexicon.Symbol sym;
						ChineseCharacterBasedLexicon.Symbol unknownCharClass = null;
						context.Clear();
						context.Add(tag);
						if (i_1 < size)
						{
							char thisCh = word[i_1];
							sym = ChineseCharacterBasedLexicon.Symbol.CannonicalSymbol(thisCh);
							if (singletons.Contains(sym))
							{
								unknownCharClass = UnknownCharClass(sym);
								charCounter.IncrementCount(unknownCharClass);
							}
						}
						else
						{
							sym = ChineseCharacterBasedLexicon.Symbol.EndWord;
						}
						POSspecificCharNGrams[0].IncrementCount(context, sym);
						// POS-specific 1-gram
						if (unknownCharClass != null)
						{
							POSspecificCharNGrams[0].IncrementCount(context, unknownCharClass);
						}
						// for unknown ch model
						// context is constructed incrementally:
						// tag prevChar prevPrevChar
						// this could be made faster using .sublist like in score
						for (int j = 1; j <= ContextLength; j++)
						{
							// poly grams
							if (i_1 - j < 0)
							{
								context.Add(ChineseCharacterBasedLexicon.Symbol.BeginWord);
								POSspecificCharNGrams[j].IncrementCount(context, sym);
								if (unknownCharClass != null)
								{
									POSspecificCharNGrams[j].IncrementCount(context, unknownCharClass);
								}
								// for unknown ch model
								break;
							}
							else
							{
								ChineseCharacterBasedLexicon.Symbol prev = ChineseCharacterBasedLexicon.Symbol.CannonicalSymbol(word[i_1 - j]);
								if (singletons.Contains(prev))
								{
									context.Add(UnknownCharClass(prev));
								}
								else
								{
									context.Add(prev);
								}
								POSspecificCharNGrams[j].IncrementCount(context, sym);
								if (unknownCharClass != null)
								{
									POSspecificCharNGrams[j].IncrementCount(context, unknownCharClass);
								}
							}
						}
					}
				}
			}
			// for unknown ch model
			POSDistribution = Distribution.GetDistribution(POSCounter);
			Timing.Tick("Creating character prior distribution...");
			charDistributions = Generics.NewHashMap();
			//    charDistributions = Generics.newHashMap();  // 1.5
			//    charCounter.incrementCount(Symbol.UNKNOWN, singletons.size());
			int numberOfKeys = charCounter.Size() + singletons.Count;
			Distribution<ChineseCharacterBasedLexicon.Symbol> prior = Distribution.GoodTuringSmoothedCounter(charCounter, numberOfKeys);
			charDistributions[Java.Util.Collections.EmptyList] = prior;
			for (int i_2 = 0; i_2 <= ContextLength; i_2++)
			{
				ICollection<KeyValuePair<IList<ISerializable>, ClassicCounter<ChineseCharacterBasedLexicon.Symbol>>> counterEntries = POSspecificCharNGrams[i_2].LowestLevelCounterEntrySet();
				Timing.Tick("Creating " + counterEntries.Count + " character " + (i_2 + 1) + "-gram distributions...");
				foreach (KeyValuePair<IList<ISerializable>, ClassicCounter<ChineseCharacterBasedLexicon.Symbol>> entry in counterEntries)
				{
					context = entry.Key;
					ClassicCounter<ChineseCharacterBasedLexicon.Symbol> c = entry.Value;
					Distribution<ChineseCharacterBasedLexicon.Symbol> thisPrior = charDistributions[context.SubList(0, context.Count - 1)];
					double priorWeight = thisPrior.GetNumberOfKeys() / 200.0;
					Distribution<ChineseCharacterBasedLexicon.Symbol> newDist = Distribution.DynamicCounterWithDirichletPrior(c, thisPrior, priorWeight);
					charDistributions[context] = newDist;
				}
			}
		}

		public virtual Distribution<string> GetPOSDistribution()
		{
			return POSDistribution;
		}

		public static bool IsForeign(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				int num = char.GetNumericValue(s[i]);
				if (num < 10 || num > 35)
				{
					return false;
				}
			}
			return true;
		}

		private ChineseCharacterBasedLexicon.Symbol UnknownCharClass(ChineseCharacterBasedLexicon.Symbol ch)
		{
			if (useUnknownCharacterModel)
			{
				return new ChineseCharacterBasedLexicon.Symbol(char.ToString(RadicalMap.GetRadical(ch.GetCh()))).Intern();
			}
			else
			{
				return ChineseCharacterBasedLexicon.Symbol.Unknown;
			}
		}

		public virtual float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			string tag = tagIndex.Get(iTW.tag);
			System.Diagnostics.Debug.Assert(!word.Equals(LexiconConstants.Boundary));
			char[] chars = word.ToCharArray();
			IList<ISerializable> charList = new List<ISerializable>(chars.Length + ContextLength + 1);
			// this starts of storing Symbol's and then starts storing String's. Clean this up someday!
			// charList is constructed backward
			// END_WORD char[length-1] char[length-2] ... char[0] BEGIN_WORD BEGIN_WORD
			charList.Add(ChineseCharacterBasedLexicon.Symbol.EndWord);
			for (int i = chars.Length - 1; i >= 0; i--)
			{
				ChineseCharacterBasedLexicon.Symbol ch = ChineseCharacterBasedLexicon.Symbol.CannonicalSymbol(chars[i]);
				if (knownChars.Contains(ch))
				{
					charList.Add(ch);
				}
				else
				{
					charList.Add(UnknownCharClass(ch));
				}
			}
			for (int i_1 = 0; i_1 < ContextLength; i_1++)
			{
				charList.Add(ChineseCharacterBasedLexicon.Symbol.BeginWord);
			}
			double score = 0.0;
			for (int i_2 = 0; i_2 < size - ContextLength; i_2++)
			{
				ChineseCharacterBasedLexicon.Symbol nextChar = (ChineseCharacterBasedLexicon.Symbol)charList[i_2];
				charList.Set(i_2, tag);
				double charScore = GetBackedOffDist(charList.SubList(i_2, i_2 + ContextLength + 1)).ProbabilityOf(nextChar);
				score += Math.Log(charScore);
			}
			switch (penaltyType)
			{
				case 0:
				{
					break;
				}

				case 1:
				{
					score -= (chars.Length * (chars.Length + 1)) * (lengthPenalty / 2);
					break;
				}

				case 2:
				{
					score -= (chars.Length - 1) * lengthPenalty;
					break;
				}
			}
			return (float)score;
		}

		// this is where we do backing off for unseen contexts
		// (backing off for rarely seen contexts is done implicitly
		// because the distributions are smoothed)
		private Distribution<ChineseCharacterBasedLexicon.Symbol> GetBackedOffDist(IList<ISerializable> context)
		{
			// context contains [tag prevChar prevPrevChar]
			for (int i = ContextLength + 1; i >= 0; i--)
			{
				IList<ISerializable> l = context.SubList(0, i);
				if (charDistributions.Contains(l))
				{
					return charDistributions[l];
				}
			}
			throw new Exception("OOPS... no prior distribution...?");
		}

		/// <summary>Samples from the distribution over words with this POS according to the lexicon.</summary>
		/// <param name="tag">the POS of the word to sample</param>
		/// <returns>a sampled word</returns>
		public virtual string SampleFrom(string tag)
		{
			StringBuilder buf = new StringBuilder();
			IList<ISerializable> context = new List<ISerializable>(ContextLength + 1);
			// context must contain [tag prevChar prevPrevChar]
			context.Add(tag);
			for (int i = 0; i < ContextLength; i++)
			{
				context.Add(ChineseCharacterBasedLexicon.Symbol.BeginWord);
			}
			Distribution<ChineseCharacterBasedLexicon.Symbol> d = GetBackedOffDist(context);
			ChineseCharacterBasedLexicon.Symbol gen = d.SampleFrom();
			while (gen != ChineseCharacterBasedLexicon.Symbol.EndWord)
			{
				buf.Append(gen.GetCh());
				switch (penaltyType)
				{
					case 1:
					{
						if (Math.Random() > Math.Pow(lengthPenalty, buf.Length))
						{
							goto genLoop_break;
						}
						break;
					}

					case 2:
					{
						if (Math.Random() > lengthPenalty)
						{
							goto genLoop_break;
						}
						break;
					}
				}
				for (int i_1 = 1; i_1 < ContextLength; i_1++)
				{
					context.Set(i_1 + 1, context[i_1]);
				}
				context.Set(1, gen);
				d = GetBackedOffDist(context);
				gen = d.SampleFrom();
genLoop_continue: ;
			}
genLoop_break: ;
			return buf.ToString();
		}

		/// <summary>
		/// Samples over words regardless of POS: first samples POS, then samples
		/// word according to that POS
		/// </summary>
		/// <returns>a sampled word</returns>
		public virtual string SampleFrom()
		{
			string Pos = POSDistribution.SampleFrom();
			return SampleFrom(Pos);
		}

		// don't think this should be used, but just in case...
		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec)
		{
			throw new NotSupportedException("ChineseCharacterBasedLexicon has no rule iterator!");
		}

		// don't think this should be used, but just in case...
		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc, string featureSpec)
		{
			throw new NotSupportedException("ChineseCharacterBasedLexicon has no rule iterator!");
		}

		/// <summary>Returns the number of rules (tag rewrites as word) in the Lexicon.</summary>
		/// <remarks>
		/// Returns the number of rules (tag rewrites as word) in the Lexicon.
		/// This method isn't yet implemented in this class.
		/// It currently just returns 0, which may or may not be helpful.
		/// </remarks>
		public virtual int NumRules()
		{
			return 0;
		}

		private Distribution<int> GetWordLengthDistribution()
		{
			int samples = 0;
			ClassicCounter<int> c = new ClassicCounter<int>();
			while (samples++ < 10000)
			{
				string s = SampleFrom();
				c.IncrementCount(int.Parse(s.Length));
				if (samples % 1000 == 0)
				{
					System.Console.Out.Write(".");
				}
			}
			System.Console.Out.WriteLine();
			Distribution<int> genWordLengthDist = Distribution.GetDistribution(c);
			return genWordLengthDist;
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadData(BufferedReader @in)
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteData(TextWriter w)
		{
			throw new NotSupportedException();
		}

		public virtual bool IsKnown(int word)
		{
			throw new NotSupportedException();
		}

		public virtual bool IsKnown(string word)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<string> TagSet(IFunction<string, string> basicCategoryFunction)
		{
			ICollection<string> tagSet = new HashSet<string>();
			foreach (string tag in tagIndex.ObjectsList())
			{
				tagSet.Add(basicCategoryFunction.Apply(tag));
			}
			return tagSet;
		}

		[System.Serializable]
		internal class Symbol
		{
			private const int UnknownType = 0;

			private const int DigitType = 1;

			private const int LetterType = 2;

			private const int BeginWordType = 3;

			private const int EndWordType = 4;

			private const int CharType = 5;

			private const int UnkClassType = 6;

			private char ch;

			private string unkClass;

			internal int type;

			public static readonly ChineseCharacterBasedLexicon.Symbol Unknown = new ChineseCharacterBasedLexicon.Symbol(UnknownType);

			public static readonly ChineseCharacterBasedLexicon.Symbol Digit = new ChineseCharacterBasedLexicon.Symbol(DigitType);

			public static readonly ChineseCharacterBasedLexicon.Symbol Letter = new ChineseCharacterBasedLexicon.Symbol(LetterType);

			public static readonly ChineseCharacterBasedLexicon.Symbol BeginWord = new ChineseCharacterBasedLexicon.Symbol(BeginWordType);

			public static readonly ChineseCharacterBasedLexicon.Symbol EndWord = new ChineseCharacterBasedLexicon.Symbol(EndWordType);

			public static readonly Interner<ChineseCharacterBasedLexicon.Symbol> interner = new Interner<ChineseCharacterBasedLexicon.Symbol>();

			public Symbol(char ch)
			{
				type = CharType;
				this.ch = ch;
			}

			public Symbol(string unkClass)
			{
				type = UnkClassType;
				this.unkClass = unkClass;
			}

			public Symbol(int type)
			{
				System.Diagnostics.Debug.Assert(type != CharType);
				this.type = type;
			}

			public static ChineseCharacterBasedLexicon.Symbol CannonicalSymbol(char ch)
			{
				if (char.IsDigit(ch))
				{
					return Digit;
				}
				//{ Digits.add(new Character(ch)); return DIGIT; }
				if (char.GetNumericValue(ch) >= 10 && char.GetNumericValue(ch) <= 35)
				{
					return Letter;
				}
				//{ Letters.add(new Character(ch)); return LETTER; }
				return new ChineseCharacterBasedLexicon.Symbol(ch);
			}

			public virtual char GetCh()
			{
				if (type == CharType)
				{
					return ch;
				}
				else
				{
					return '*';
				}
			}

			public virtual ChineseCharacterBasedLexicon.Symbol Intern()
			{
				return interner.Intern(this);
			}

			public override string ToString()
			{
				if (type == CharType)
				{
					return "[u" + (int)ch + "]";
				}
				else
				{
					if (type == UnkClassType)
					{
						return "UNK:" + unkClass;
					}
					else
					{
						return int.ToString(type);
					}
				}
			}

			/// <exception cref="Java.IO.ObjectStreamException"/>
			protected internal virtual object ReadResolve()
			{
				switch (type)
				{
					case CharType:
					{
						return Intern();
					}

					case UnkClassType:
					{
						return Intern();
					}

					case UnknownType:
					{
						return Unknown;
					}

					case DigitType:
					{
						return Digit;
					}

					case LetterType:
					{
						return Letter;
					}

					case BeginWordType:
					{
						return BeginWord;
					}

					case EndWordType:
					{
						return EndWord;
					}

					default:
					{
						// impossible...
						throw new InvalidObjectException("ILLEGAL VALUE IN SERIALIZED SYMBOL");
					}
				}
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is ChineseCharacterBasedLexicon.Symbol))
				{
					return false;
				}
				ChineseCharacterBasedLexicon.Symbol symbol = (ChineseCharacterBasedLexicon.Symbol)o;
				if (ch != symbol.ch)
				{
					return false;
				}
				if (type != symbol.type)
				{
					return false;
				}
				if (unkClass != null ? !unkClass.Equals(symbol.unkClass) : symbol.unkClass != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result;
				result = ch;
				result = 29 * result + (unkClass != null ? unkClass.GetHashCode() : 0);
				result = 29 * result + type;
				return result;
			}

			private const long serialVersionUID = 8925032621317022510L;
		}

		private const long serialVersionUID = -5357655683145854069L;

		// end class Symbol
		public virtual IUnknownWordModel GetUnknownWordModel()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public virtual void SetUnknownWordModel(IUnknownWordModel uwm)
		{
		}

		// TODO Auto-generated method stub
		public virtual void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees)
		{
			Train(trees);
		}
	}
}
