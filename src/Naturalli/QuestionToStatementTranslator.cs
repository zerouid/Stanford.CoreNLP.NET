using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// <p>
	/// Translate a question to a statement.
	/// </summary>
	/// <remarks>
	/// <p>
	/// Translate a question to a statement. For example, "where was Obama born?" to "Obama was born in ?".
	/// </p>
	/// <p>
	/// This class was developed for, and therefore likely performs best on (read: "over-fits gloriously to")
	/// the webquestions dataset (http://www-nlp.stanford.edu/software/sempre/).
	/// The rules were created based off of the webquestions
	/// training set, and tested against the sentences in the QuestionToStatementTranslatorTest.
	/// If something fails, please add it to the test when you fix it!
	/// If you change something here, please validate it wit the test!
	/// </p>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class QuestionToStatementTranslator
	{
		public class UnknownTokenMarker : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		private sealed class _CoreLabel_42 : CoreLabel
		{
			public _CoreLabel_42()
			{
				{
					this.SetWord("thing");
					this.SetValue("thing");
					this.SetLemma("thing");
					this.SetTag("NN");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker, when the object of the sentence is not type constrained.</summary>
		private readonly CoreLabel WordMissing = new _CoreLabel_42();

		private sealed class _CoreLabel_57 : CoreLabel
		{
			public _CoreLabel_57()
			{
				{
					this.SetWord("location");
					this.SetValue("location");
					this.SetLemma("location");
					this.SetTag("NNP");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker typed as a location.</summary>
		private readonly CoreLabel WordMissingLocation = new _CoreLabel_57();

		private sealed class _CoreLabel_72 : CoreLabel
		{
			public _CoreLabel_72()
			{
				{
					this.SetWord("person");
					this.SetValue("person");
					this.SetLemma("person");
					this.SetTag("NNP");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker typed as a person.</summary>
		private readonly CoreLabel WordMissingPerson = new _CoreLabel_72();

		private sealed class _CoreLabel_85 : CoreLabel
		{
			public _CoreLabel_85()
			{
				{
					this.SetWord("time");
					this.SetValue("time");
					this.SetLemma("time");
					this.SetTag("NN");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker typed as a time.</summary>
		private readonly CoreLabel WordMissingTime = new _CoreLabel_85();

		private sealed class _CoreLabel_100 : CoreLabel
		{
			public _CoreLabel_100()
			{
				{
					this.SetWord("adjective");
					this.SetValue("adjective");
					this.SetLemma("adjective");
					this.SetTag("JJ");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker typed as a time.</summary>
		private readonly CoreLabel WordAdjective = new _CoreLabel_100();

		private sealed class _CoreLabel_115 : CoreLabel
		{
			public _CoreLabel_115()
			{
				{
					this.SetWord("way");
					this.SetValue("way");
					this.SetLemma("way");
					this.SetTag("RB");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
					this.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
		}

		/// <summary>The missing word marker typed as a time.</summary>
		private readonly CoreLabel WordWay = new _CoreLabel_115();

		private sealed class _CoreLabel_130 : CoreLabel
		{
			public _CoreLabel_130()
			{
				{
					this.SetWord("from");
					this.SetValue("from");
					this.SetLemma("from");
					this.SetTag("IN");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
				}
			}
		}

		/// <summary>The word "from" as a CoreLabel</summary>
		private readonly CoreLabel WordFrom = new _CoreLabel_130();

		private sealed class _CoreLabel_144 : CoreLabel
		{
			public _CoreLabel_144()
			{
				{
					this.SetWord("at");
					this.SetValue("at");
					this.SetLemma("at");
					this.SetTag("IN");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
				}
			}
		}

		/// <summary>The word "at" as a CoreLabel</summary>
		private readonly CoreLabel WordAt = new _CoreLabel_144();

		private sealed class _CoreLabel_158 : CoreLabel
		{
			public _CoreLabel_158()
			{
				{
					this.SetWord("in");
					this.SetValue("in");
					this.SetLemma("in");
					this.SetTag("IN");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
				}
			}
		}

		/// <summary>The word "in" as a CoreLabel</summary>
		private readonly CoreLabel WordIn = new _CoreLabel_158();

		private sealed class _CoreLabel_172 : CoreLabel
		{
			public _CoreLabel_172()
			{
				{
					this.SetWord("to");
					this.SetValue("to");
					this.SetLemma("to");
					this.SetTag("TO");
					this.SetNER("O");
					this.SetIndex(-1);
					this.SetBeginPosition(-1);
					this.SetEndPosition(-1);
					this.SetBefore(" ");
					this.SetAfter(" ");
				}
			}
		}

		/// <summary>The word "to" as a CoreLabel</summary>
		private readonly CoreLabel WordTo = new _CoreLabel_172();

		private sealed class _HashSet_185 : HashSet<string>
		{
			public _HashSet_185()
			{
				{
					this.Add("funding");
					this.Add("oil");
				}
			}
		}

		private readonly ICollection<string> fromNotAtDict = Java.Util.Collections.UnmodifiableSet(new _HashSet_185());

		/// <summary>The pattern for "what is ..." sentences.</summary>
		/// <seealso cref="ProcessWhatIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhatIs = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [tag:/N.*/]+)? " + "(?$be [{lemma:be}] )" + "(?: /the/ (?$answer_type [word:/name/]) [tag:/[PW].*/])? "
			 + "(?$statement_body []+?) " + "(?$prep_num [!{tag:IN}] [tag:CD] )? " + "(?$suffix [tag:/[RI].*/] )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhatIs"/>
		private IList<CoreLabel> ProcessWhatIs(TokenSequenceMatcher matcher)
		{
			// Grab the body of the sentence
			LinkedList<CoreLabel> body = new LinkedList<CoreLabel>((IList<CoreLabel>)matcher.GroupNodes("$statement_body"));
			// Add the "be" token
			// [Gabor]: This is basically the most principled code I've ever written.
			// [Gabor]: If the "be" gets misplaced, God help us all.
			// [Gabor]: Mostly you. I'm graduated and gone, so you'll need most of the help.
			IList<CoreLabel> be = (IList<CoreLabel>)matcher.GroupNodes("$be");
			IList<CoreLabel> suffix = (IList<CoreLabel>)matcher.GroupNodes("$suffix");
			bool addedBe = false;
			bool addedSuffix = false;
			if (body.Count > 1 && !"PRP".Equals(body[0].Tag()))
			{
				for (int i = 2; i < body.Count; ++i)
				{
					CoreLabel tokI = body[i];
					if (tokI.Tag() != null && ((tokI.Tag().StartsWith("V") && !tokI.Tag().Equals("VBD") && !"be".Equals(body[i - 1].Lemma())) || (tokI.Tag().StartsWith("J") && suffix != null) || (tokI.Tag().StartsWith("D") && suffix != null) || (tokI.Tag().StartsWith
						("R") && suffix != null)))
					{
						body.Add(i, be[0]);
						i += 1;
						if (suffix != null)
						{
							while (i < body.Count && body[i].Tag() != null && (body[i].Tag().StartsWith("J") || body[i].Tag().StartsWith("V") || body[i].Tag().StartsWith("R") || body[i].Tag().StartsWith("N") || body[i].Tag().StartsWith("D")) && !body[i].Tag().Equals("VBG"
								))
							{
								i += 1;
							}
							body.Add(i, suffix[0]);
							addedSuffix = true;
						}
						addedBe = true;
						break;
					}
				}
			}
			// Tweak to handle dropped prepositions
			IList<CoreLabel> prepNum = (IList<CoreLabel>)matcher.GroupNodes("$prep_num");
			if (prepNum != null)
			{
				body.Add(prepNum[0]);
				body.Add(WordIn);
				body.Add(prepNum[1]);
			}
			// Add the "be" and suffix
			if (!addedSuffix && suffix != null)
			{
				Sharpen.Collections.AddAll(body, suffix);
			}
			if (!addedBe)
			{
				if (body.Count > 1 && "PRP".Equals(body[0].Tag()))
				{
					body.Add(1, be[0]);
				}
				else
				{
					Sharpen.Collections.AddAll(body, be);
				}
			}
			// Drop a final 'do'
			if (body.Count > 1 && "do".Equals(body[body.Count - 1].Word()))
			{
				body = new LinkedList<CoreLabel>(body.SubList(0, body.Count - 1));
			}
			// Grab the object
			IList<CoreLabel> objType = (IList<CoreLabel>)matcher.GroupNodes("$answer_type");
			// (try to insert the object earlier)
			int i_1 = body.Count - 1;
			while (i_1 >= 1 && body[i_1].Tag() != null && (body[i_1].Tag().StartsWith("N") || body[i_1].Tag().StartsWith("J")))
			{
				i_1 -= 1;
			}
			// (actually insert the object)
			if (objType == null || objType.IsEmpty() || (objType.Count == 1 && objType[0].Word().Equals("name")))
			{
				// (case: untyped)
				if (i_1 < body.Count - 1 && body[i_1].Tag() != null && body[i_1].Tag().StartsWith("IN"))
				{
					body.Add(i_1, WordMissing);
				}
				else
				{
					if (body.Count >= 2 && body[body.Count - 2].Tag() != null && body[body.Count - 2].Tag().StartsWith("N") && !body[body.Count - 1].Tag().Equals("IN"))
					{
						// This is a bit of a giant hack. But:
						// 1. Add 'thing is' to the beginning of the sentence
						// 2. remove the 'be' we added to the end of the sentence above
						if (!addedBe)
						{
							Java.Util.Collections.Reverse(be);
							be.ForEach(null);
						}
						body.AddFirst(WordMissing);
						IEnumerator<CoreLabel> beIter = be.GetEnumerator();
						if (beIter.MoveNext() && body.GetLast() == beIter.Current)
						{
							body.RemoveLast();
						}
					}
					else
					{
						body.AddLast(WordMissing);
					}
				}
			}
			else
			{
				// (case: typed)
				foreach (CoreLabel obj in objType)
				{
					obj.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
				Sharpen.Collections.AddAll(body, objType);
			}
			// Swap determiner + be -> be determiner
			for (int k = 1; k < body.Count; ++k)
			{
				if ("DT".Equals(body[k - 1].Tag()) && "be".Equals(body[k].Lemma()))
				{
					Java.Util.Collections.Swap(body, k - 1, k);
				}
			}
			// Swap IN + be -> be IN
			if (body.Stream().NoneMatch(null))
			{
				for (int k_1 = 1; k_1 < body.Count; ++k_1)
				{
					if ("IN".Equals(body[k_1 - 1].Tag()) && "be".Equals(body[k_1].Lemma()))
					{
						Java.Util.Collections.Swap(body, k_1 - 1, k_1);
					}
				}
			}
			// Return
			return body;
		}

		/// <summary>The pattern for "what NN will (I|NN) ..." sentences.</summary>
		/// <seealso cref="ProcessWhNNIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhNNWill = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [!{lemma:be} & !{pos:\"PRP$\"} & !{pos:MD}]+) " + "(?$will [{pos:MD}]) " + "(?$subj [{pos:/NN.?.?/} | {pos:PRP}]+) "
			 + "(?$statement_body [!{pos:IN}]+) " + "(?$pp_prefix [{pos:IN}]*) " + "(?$pp [{pos:IN}]) " + "(?$pp_body []*) " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what NN is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhNNIs"/>
		private IList<CoreLabel> ProcessWhNNWill(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$subj");
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$will"));
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$statement_body"));
			ICollection<CoreLabel> answerType = (ICollection<CoreLabel>)matcher.GroupNodes("$answer_type");
			foreach (CoreLabel lbl in answerType)
			{
				lbl.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
			}
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$pp_prefix"));
			Sharpen.Collections.AddAll(sentence, answerType);
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$pp"));
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$pp_body"));
			return sentence;
		}

		/// <summary>The pattern for "what/which NN is ..." sentences.</summary>
		/// <seealso cref="ProcessWhNNIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhNNIs = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [!{lemma:be} & !{pos:\"PRP$\"} | {word:i}]+) " + "(?$be [{lemma:be}] [{tag:/[VRIJ].*/}] ) " + "(?$statement_body []+?) "
			 + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what NN is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhNNIs"/>
		private IList<CoreLabel> ProcessWhNNIs(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$answer_type");
			foreach (CoreLabel lbl in sentence)
			{
				lbl.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
			}
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$be"));
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$statement_body"));
			return sentence;
		}

		/// <summary>The pattern for "what/which NN have ..." sentences.</summary>
		/// <seealso cref="ProcessWhNNHaveIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhNNHave = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [!{tag:/V.*/}]+) " + "(?$have [{lemma:have} | {lemma:do} | {lemma:be}] ) " + "(?$pre_verb [!{tag:/V.*/}]+ ) "
			 + "(?$verb [{tag:/V.*/}] [{tag:IN}]? ) " + "(?$post_verb []+ )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what NN has ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhNNHave"/>
		private IList<CoreLabel> ProcessWhNNHaveIs(TokenSequenceMatcher matcher)
		{
			// Add prefix
			IList<CoreLabel> sentence = new List<CoreLabel>((ICollection<CoreLabel>)matcher.GroupNodes("$pre_verb"));
			// Add have/do
			IList<CoreLabel> have = (IList<CoreLabel>)matcher.GroupNodes("$have");
			if (have != null && have.Count > 0 && have[0].Lemma() != null && (Sharpen.Runtime.EqualsIgnoreCase(have[0].Lemma(), "have") || Sharpen.Runtime.EqualsIgnoreCase(have[0].Lemma(), "be")))
			{
				Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$have"));
			}
			// Compute answer type
			IList<CoreLabel> answer = (IList<CoreLabel>)matcher.GroupNodes("$answer_type");
			if (answer != null)
			{
				foreach (CoreLabel lbl in answer)
				{
					lbl.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
				}
			}
			// Add verb + Answer
			IList<CoreLabel> verb = (IList<CoreLabel>)matcher.GroupNodes("$verb");
			IList<CoreLabel> post = (IList<CoreLabel>)matcher.GroupNodes("$post_verb");
			if (verb.Count < 2 || post == null || post.Count == 0 || post[0].Tag() == null || post[0].Tag().Equals("CD"))
			{
				Sharpen.Collections.AddAll(sentence, verb);
				if (answer == null)
				{
					sentence.Add(WordMissing);
				}
				else
				{
					Sharpen.Collections.AddAll(sentence, answer);
				}
			}
			else
			{
				sentence.Add(verb[0]);
				if (answer == null)
				{
					sentence.Add(WordMissing);
				}
				else
				{
					Sharpen.Collections.AddAll(sentence, answer);
				}
				Sharpen.Collections.AddAll(sentence, verb.SubList(1, verb.Count));
			}
			// Add postfix
			if (post != null)
			{
				if (post.Count == 1 && post[0].Tag() != null && post[0].Tag().Equals("CD"))
				{
					sentence.Add(WordIn);
				}
				Sharpen.Collections.AddAll(sentence, post);
			}
			// Return
			return sentence;
		}

		/// <summary>The pattern for "what/which NN have NN ..." sentences.</summary>
		/// <seealso cref="ProcessWhNNHaveNN(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhNNHaveNN = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [tag:/N.*/]+) " + "(?$have [{lemma:have}] ) " + "(?$statement_body [!{tag:/V.*/}]+?) " + "(?$punct [word:/[?\\.!]/])"
			);

		/// <summary>Process sentences matching the "what NN have NN ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhNNHaveNN"/>
		private IList<CoreLabel> ProcessWhNNHaveNN(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$answer_type");
			foreach (CoreLabel lbl in sentence)
			{
				lbl.Set(typeof(QuestionToStatementTranslator.UnknownTokenMarker), true);
			}
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$have"));
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$statement_body"));
			return sentence;
		}

		/// <summary>The pattern for "what is there ..." sentences.</summary>
		/// <seealso cref="ProcessWhatIsThere(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhatIsThere = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$answer_type [tag:/N.*/]+)? " + "(?$be [{lemma:be}] )" + "(?$there [{lemma:there; tag:RB}] ) " + "(?$adjmod [{tag:/[JN].*/}] )? "
			 + "(?$to_verb [{tag:TO}] [{tag:/V.*/}] )? " + "(?$statement_body [{tag:IN}] []+?) " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhatIsThere"/>
		private IList<CoreLabel> ProcessWhatIsThere(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> optSpan;
			// Grab the prefix of the sentence
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$there");
			Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$be"));
			// Grab the unknown term
			if ((optSpan = (IList<CoreLabel>)matcher.GroupNodes("$adjmod")) != null)
			{
				Sharpen.Collections.AddAll(sentence, optSpan);
			}
			sentence.Add(WordMissing);
			// Add body
			if ((optSpan = (IList<CoreLabel>)matcher.GroupNodes("$to_verb")) != null)
			{
				Sharpen.Collections.AddAll(sentence, optSpan);
			}
			Sharpen.Collections.AddAll(sentence, (ICollection<CoreLabel>)matcher.GroupNodes("$statement_body"));
			// Return
			return sentence;
		}

		/// <summary>The pattern for "where do..."  sentences.</summary>
		/// <seealso cref="ProcessWhereDo(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher, System.Collections.Generic.IList{E})"/>
		private readonly TokenSequencePattern triggerWhereDo = TokenSequencePattern.Compile("[{lemma:where; tag:/W.*/}] " + "(?$do [ {lemma:/do/} ]) " + "(?$statement_body []+?) " + "(?$at [tag:/[IT].*/] )? " + "(?$loc [tag:/N.*/] )*? " + "(?$punct [word:/[?\\.!]/])"
			);

		/// <summary>Process sentences matching the "where do ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <param name="question">The original question we asked</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhereDo"/>
		private IList<CoreLabel> ProcessWhereDo(TokenSequenceMatcher matcher, IList<CoreLabel> question)
		{
			// Get the "at" preposition and the "location" missing marker to use
			IList<CoreLabel> specloc = (IList<CoreLabel>)matcher.GroupNodes("$loc");
			CoreLabel wordAt = WordAt;
			CoreLabel missing = WordMissingLocation;
			if (specloc != null && fromNotAtDict.Contains(specloc[0].Word()))
			{
				wordAt = WordFrom;
				missing = WordMissing;
			}
			string questionLemmas = " " + StringUtils.Join(question.Stream().Map(null), " ") + " ";
			if (questionLemmas.Contains(" go ") && !questionLemmas.Contains(" go to "))
			{
				wordAt = WordTo;
			}
			// Grab the prefix of the sentence
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$statement_body");
			// (check if we should be looking for a location)
			foreach (CoreLabel lbl in sentence)
			{
				if ("name".Equals(lbl.Word()))
				{
					missing = WordMissing;
				}
			}
			// Add the "at" part
			IList<CoreLabel> at = (IList<CoreLabel>)matcher.GroupNodes("$at");
			if (at != null && at.Count > 0)
			{
				Sharpen.Collections.AddAll(sentence, at);
			}
			else
			{
				if (specloc != null)
				{
					Sharpen.Collections.AddAll(sentence, specloc);
				}
				sentence.Add(wordAt);
			}
			// Add the location
			sentence.Add(missing);
			// Add an optional specifier location
			//    if (specloc != null && at != null) {
			//      sentence.add(WORD_COMMA);
			//      sentence.addAll(specloc);
			//    }
			// Return
			return sentence;
		}

		/// <summary>The pattern for "where is..."  sentences.</summary>
		/// <seealso cref="ProcessWhereIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhereIs = TokenSequencePattern.Compile("[{lemma:where; tag:/W.*/}] " + "(?$be [ {lemma:/be/} ]) " + "(?$initial_verb [tag:/[VJ].*/] )? " + "(?$subject [{tag:/NN.?.?/}]+ ((in|at|of) [{tag:/NN.?.?/}]+)* )? "
			 + "(?$statement_body []*?)? " + "(?$ignored [lemma:locate] [tag:IN] [word:a]? [word:map]? )? " + "(?$final_verb [tag:/[VJ].*/] )? " + "(?$at [tag:IN] )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "where is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhereIs"/>
		private IList<CoreLabel> ProcessWhereIs(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			// The subject of the sentence
			IList<CoreLabel> subject = (IList<CoreLabel>)matcher.GroupNodes("$subject");
			if (subject != null)
			{
				Sharpen.Collections.AddAll(sentence, subject);
			}
			// Add the "is" part
			IList<CoreLabel> be = (IList<CoreLabel>)matcher.GroupNodes("$be");
			Sharpen.Collections.AddAll(sentence, be);
			// The extra body of the sentence
			IList<CoreLabel> body = (IList<CoreLabel>)matcher.GroupNodes("$statement_body");
			if (body != null)
			{
				Sharpen.Collections.AddAll(sentence, body);
			}
			// Add the optional final verb
			IList<CoreLabel> verb = (IList<CoreLabel>)matcher.GroupNodes("$final_verb");
			if (verb != null)
			{
				Sharpen.Collections.AddAll(sentence, verb);
			}
			// Add the optional initial verb (from disfluent questions!)
			verb = (IList<CoreLabel>)matcher.GroupNodes("$initial_verb");
			if (verb != null)
			{
				Sharpen.Collections.AddAll(sentence, verb);
			}
			// Add the "at" part
			IList<CoreLabel> at = (IList<CoreLabel>)matcher.GroupNodes("$at");
			if (at != null && at.Count > 0)
			{
				Sharpen.Collections.AddAll(sentence, at);
			}
			else
			{
				sentence.Add(WordAt);
			}
			// Add the location
			sentence.Add(WordMissingLocation);
			// Return
			return sentence;
		}

		/// <summary>The pattern for "who is..."  sentences.</summary>
		/// <seealso cref="ProcessWhoIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhoIs = TokenSequencePattern.Compile("[{lemma:who; tag:/W.*/}] " + "(?$be [ {lemma:/be/} ] ) " + "(?$prep [ {tag:/IN|V.*/} ] )? " + "(?$statement_body []+?) " + "(?$final_verb [tag:/V.*/] [tag:/[IRT].*/] )? "
			 + "(?$final_verb [tag:VBG] )? " + "(?$now [tag:RB] )? " + "(?$prep_num [!{tag:IN}] [tag:CD] )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "who is ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhoIs"/>
		private IList<CoreLabel> ProcessWhoIs(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			IList<CoreLabel> prep = (IList<CoreLabel>)matcher.GroupNodes("$prep");
			bool addedBe = false;
			if (prep != null && !prep.IsEmpty())
			{
				// Add the person
				sentence.Add(WordMissingPerson);
				// Add the "is" part
				IList<CoreLabel> be = (IList<CoreLabel>)matcher.GroupNodes("$be");
				Sharpen.Collections.AddAll(sentence, be);
				addedBe = true;
				// Add the preposition
				Sharpen.Collections.AddAll(sentence, prep);
				// Grab the prefix of the sentence
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$statement_body"));
			}
			else
			{
				// Grab the prefix of the sentence
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$statement_body"));
				// Tweak to handle dropped prepositions
				IList<CoreLabel> prepNum = (IList<CoreLabel>)matcher.GroupNodes("$prep_num");
				if (prepNum != null)
				{
					sentence.Add(prepNum[0]);
					sentence.Add(WordIn);
					sentence.Add(prepNum[1]);
				}
				// Add the "is" part
				IList<CoreLabel> be = (IList<CoreLabel>)matcher.GroupNodes("$be");
				if (sentence.Count > 1 && !sentence[sentence.Count - 1].Word().Equals("be"))
				{
					Sharpen.Collections.AddAll(sentence, be);
					addedBe = true;
				}
				// Add the final verb part
				IList<CoreLabel> verb = (IList<CoreLabel>)matcher.GroupNodes("$final_verb");
				if (verb != null)
				{
					if (verb.Count > 1 && verb[verb.Count - 1].Word().Equals("too"))
					{
						// Fix common typo
						verb[verb.Count - 1].SetWord("to");
						verb[verb.Count - 1].SetValue("to");
						verb[verb.Count - 1].SetLemma("to");
						verb[verb.Count - 1].SetTag("IN");
					}
					Sharpen.Collections.AddAll(sentence, verb);
				}
				// Add the person
				sentence.Add(WordMissingPerson);
			}
			// Add a final modifier (e.g., "now")
			IList<CoreLabel> now = (IList<CoreLabel>)matcher.GroupNodes("$now");
			if (now != null)
			{
				Sharpen.Collections.AddAll(sentence, now);
			}
			// Insert "was" before first verb, if applicable
			if (!addedBe)
			{
				for (int i = 0; i < sentence.Count; ++i)
				{
					if (sentence[i].Tag() != null && sentence[i].Tag().StartsWith("V"))
					{
						sentence.Add(i, (CoreLabel)matcher.GroupNodes("$be")[0]);
						break;
					}
				}
			}
			// Return
			return sentence;
		}

		/// <summary>The pattern for "who did..."  sentences.</summary>
		/// <seealso cref="ProcessWhoDid(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhoDid = TokenSequencePattern.Compile("[{lemma:who; tag:/W.*/}] " + "(?$do [ {lemma:/do/} ] ) " + "(?$statement_body []+?) " + "(?$now [tag:RB] )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "who did ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhoDid"/>
		private IList<CoreLabel> ProcessWhoDid(TokenSequenceMatcher matcher)
		{
			// Get the body
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$statement_body");
			// Check if there is no main verb other than "do"
			// If it doesn't, then the sentence should be "person do ...."
			bool hasVerb = false;
			foreach (CoreLabel w in sentence)
			{
				if (w.Tag() != null && w.Tag().StartsWith("V"))
				{
					hasVerb = true;
				}
			}
			if (!hasVerb)
			{
				sentence.Add(0, WordMissingPerson);
				sentence.Add(1, (CoreLabel)matcher.GroupNodes("$do")[0]);
				return sentence;
			}
			// Add the missing word
			// (in front of the PPs)
			bool addedPerson = false;
			if (sentence.Count > 0 && sentence[sentence.Count - 1].Tag() != null && !sentence[sentence.Count - 1].Tag().StartsWith("I"))
			{
				for (int i = 0; i < sentence.Count - 1; ++i)
				{
					if (sentence[i].Tag() != null && (sentence[i].Tag().Equals("IN") || sentence[i].Word().Equals("last") || sentence[i].Word().Equals("next") || sentence[i].Word().Equals("this")))
					{
						sentence.Add(i, WordMissingPerson);
						addedPerson = true;
						break;
					}
				}
			}
			// (at the end of the sentence)
			if (!addedPerson)
			{
				sentence.Add(WordMissingPerson);
			}
			// Add "now" / "first" / etc.
			IList<CoreLabel> now = (IList<CoreLabel>)matcher.GroupNodes("$now");
			if (now != null)
			{
				Sharpen.Collections.AddAll(sentence, now);
			}
			// Return
			return sentence;
		}

		/// <summary>The pattern for "what do..."  sentences.</summary>
		/// <seealso cref="ProcessWhatDo(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhatDo = TokenSequencePattern.Compile("[{lemma:/what|which/; tag:/W.*/}] " + "(?$do [ {lemma:/do/} ]) " + "(?$pre_do [ !{lemma:do} & !{tag:IN} ]+) " + "(?$mid_do [ {lemma:do} ] )? " + "(?$in [ {tag:IN} ] )? "
			 + "(?$post_do []+ )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "what do ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhatDo"/>
		private IList<CoreLabel> ProcessWhatDo(TokenSequenceMatcher matcher)
		{
			// Grab the prefix of the sentence
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$pre_do");
			// Add the optional middle do
			IList<CoreLabel> midDo = (IList<CoreLabel>)matcher.GroupNodes("$mid_do");
			if (midDo != null)
			{
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$do"));
			}
			// Add the thing (not end of sentence)
			if (matcher.GroupNodes("$post_do") != null)
			{
				sentence.Add(WordMissing);
			}
			// Add IN token
			IList<CoreLabel> midIN = (IList<CoreLabel>)matcher.GroupNodes("$in");
			if (midIN != null)
			{
				Sharpen.Collections.AddAll(sentence, midIN);
			}
			// Add the thing (end of sentence)
			if (matcher.GroupNodes("$post_do") == null)
			{
				if (sentence.Count > 1 && "off".Equals(sentence[sentence.Count - 1].Word()))
				{
					// Fix common typo
					sentence[sentence.Count - 1].SetWord("of");
					sentence[sentence.Count - 1].SetValue("of");
					sentence[sentence.Count - 1].SetLemma("of");
					sentence[sentence.Count - 1].SetTag("IN");
				}
				sentence.Add(WordMissing);
			}
			// Add post do
			IList<CoreLabel> postDo = (IList<CoreLabel>)matcher.GroupNodes("$post_do");
			if (postDo != null)
			{
				Sharpen.Collections.AddAll(sentence, postDo);
			}
			// Tweak to handle dropped prepositions
			if (sentence.Count > 2 && !"IN".Equals(sentence[sentence.Count - 2].Tag()) && "CD".Equals(sentence[sentence.Count - 1].Tag()))
			{
				sentence.Add(sentence.Count - 1, WordIn);
			}
			// Return
			return sentence;
		}

		/// <summary>The pattern for "when do..."  sentences.</summary>
		/// <seealso cref="ProcessWhenDo(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhenDo = TokenSequencePattern.Compile("[{lemma:when; tag:/W.*/}] " + "(?$do [ {lemma:/do/} ]) " + "(?$statement_body []+?) " + "(?$in [tag:/[IT].*/] )? " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "when do ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhenDo"/>
		private IList<CoreLabel> ProcessWhenDo(TokenSequenceMatcher matcher)
		{
			// Grab the prefix of the sentence
			IList<CoreLabel> sentence = (IList<CoreLabel>)matcher.GroupNodes("$statement_body");
			// Add the "at" part
			IList<CoreLabel> @in = (IList<CoreLabel>)matcher.GroupNodes("$in");
			if (@in != null && @in.Count > 0)
			{
				Sharpen.Collections.AddAll(sentence, @in);
			}
			else
			{
				sentence.Add(WordIn);
			}
			// Add the location
			sentence.Add(WordMissingTime);
			// Return
			return sentence;
		}

		/// <summary>The pattern for "what have..."  sentences.</summary>
		/// <seealso cref="ProcessWhereIs(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerWhatHave = TokenSequencePattern.Compile("[{lemma:what; tag:/W.*/}] " + "(?$have [ {lemma:/have/} ]) " + "(?$pre_verb [!{tag:/V.*/}]+ )? " + "(?$verb [tag:/V.*/] [tag:IN]? ) " + "(?$post_verb []+ )? "
			 + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching the "when do ..." pattern.</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerWhenDo"/>
		private IList<CoreLabel> ProcessWhatHave(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			// Grab the prefix of the sentence
			IList<CoreLabel> preVerb = (IList<CoreLabel>)matcher.GroupNodes("$pre_verb");
			if (preVerb != null)
			{
				Sharpen.Collections.AddAll(sentence, preVerb);
			}
			// Add "thing have verb" or "have verb thing"
			if (sentence.Count == 0)
			{
				sentence.Add(WordMissing);
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$have"));
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$verb"));
			}
			else
			{
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$have"));
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$verb"));
				sentence.Add(WordMissing);
			}
			IList<CoreLabel> postVerb = (IList<CoreLabel>)matcher.GroupNodes("$post_verb");
			if (postVerb != null)
			{
				Sharpen.Collections.AddAll(sentence, postVerb);
			}
			return sentence;
		}

		/// <summary>The pattern for "when do..."  sentences.</summary>
		/// <seealso cref="ProcessHow(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerHow = TokenSequencePattern.Compile("([{lemma:/[Hh]ow/; tag:/W.*/}] | /[Ww]hat/ [{lemma:be}] /ways?/ (?$prp0 [{tag:/PRP.?/} | {word:i}]) ) " + "((?$do [ {lemma:/do/} | {lemma:can}]) | (?$jj [ {pos:JJ} ]{0,3}) (?$be [ {lemma:be} ])) "
			 + "(?$prp1 [{tag:/PRP.?/} | {word:i}])? " + "(?$statement_body []+?) " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching 'how...'</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerHow"/>
		private IList<CoreLabel> ProcessHow(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			// Resolve prepositions
			IList<CoreLabel> prp = (IList<CoreLabel>)matcher.GroupNodes("$prp0");
			if (prp == null || prp.IsEmpty())
			{
				prp = (IList<CoreLabel>)matcher.GroupNodes("$prp1");
			}
			if (prp != null && !prp.IsEmpty())
			{
				Sharpen.Collections.AddAll(sentence, prp);
				IList<CoreLabel> doOrCan = (IList<CoreLabel>)matcher.GroupNodes("$do");
				if (doOrCan != null && doOrCan.Count == 1 && Sharpen.Runtime.EqualsIgnoreCase("can", doOrCan[0].Lemma()))
				{
					Sharpen.Collections.AddAll(sentence, doOrCan);
				}
			}
			// Add the meat
			Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$statement_body"));
			// Add an optional 'be'
			IList<CoreLabel> wordBe = (IList<CoreLabel>)matcher.GroupNodes("$be");
			if (wordBe != null)
			{
				Sharpen.Collections.AddAll(sentence, wordBe);
				sentence.Add(WordAdjective);
			}
			else
			{
				sentence.Add(WordWay);
			}
			// Return
			return sentence;
		}

		/// <summary>The pattern for "how much...do..."  sentences.</summary>
		/// <seealso cref="ProcessHowMuchDo(Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequenceMatcher)"/>
		private readonly TokenSequencePattern triggerHowMuchDo = TokenSequencePattern.Compile("[{lemma:/[Hh]ow/; tag:/W.*/}] " + "(much | many) [{pos:NN}]{0,10} " + "((?$do [ {lemma:/do/} | {lemma:can}]) | (?$jj [ {pos:JJ} ]) (?$be [ {lemma:be} ])) "
			 + "(?$prefix [!{lemma:to}]{1,25}) " + "(?$connective [{lemma:to}])? " + "(?$suffix [!{lemma:to}]{1,25}) " + "(?$punct [word:/[?\\.!]/])");

		/// <summary>Process sentences matching 'how much do...'</summary>
		/// <param name="matcher">The matcher that matched the pattern.</param>
		/// <returns>The converted statement.</returns>
		/// <seealso cref="triggerHowMuchDo"/>
		private IList<CoreLabel> ProcessHowMuchDo(TokenSequenceMatcher matcher)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>((IList<CoreLabel>)matcher.GroupNodes("$prefix"));
			IList<CoreLabel> connective = (IList<CoreLabel>)matcher.GroupNodes("$connective");
			if (connective != null && !connective.IsEmpty())
			{
				sentence.Add(WordMissing);
				Sharpen.Collections.AddAll(sentence, connective);
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$suffix"));
			}
			else
			{
				Sharpen.Collections.AddAll(sentence, (IList<CoreLabel>)matcher.GroupNodes("$suffix"));
				sentence.Add(WordWay);
			}
			return sentence;
		}

		/// <summary>
		/// Post-process a statement, e.g., replacing 'I' with 'you', capitalizing the
		/// first letter (and not capitalizing the other letters), etc.
		/// </summary>
		/// <param name="question">The original question that we converted to a statement.</param>
		/// <param name="statement">The statement to post-process.</param>
		/// <returns>The post-processed utterance.</returns>
		private IList<IList<CoreLabel>> PostProcess(IList<CoreLabel> question, IList<CoreLabel> statement)
		{
			// 1. Replace 'i' with 'you', etc.
			foreach (CoreLabel token in statement)
			{
				string originalText = token.OriginalText();
				if (originalText == null || string.Empty.Equals(originalText))
				{
					originalText = token.Word();
				}
				switch (originalText.ToLower())
				{
					case "i":
					{
						token.Set(typeof(CoreAnnotations.StatementTextAnnotation), "you");
						break;
					}

					case "you":
					{
						token.Set(typeof(CoreAnnotations.StatementTextAnnotation), "i");
						break;
					}

					case "my":
					{
						token.Set(typeof(CoreAnnotations.StatementTextAnnotation), "your");
						break;
					}

					case "your":
					{
						token.Set(typeof(CoreAnnotations.StatementTextAnnotation), "my");
						break;
					}

					default:
					{
						token.Set(typeof(CoreAnnotations.StatementTextAnnotation), originalText);
						break;
					}
				}
			}
			// 2. Property upper-case the sentence
			for (int i = 0; i < statement.Count; ++i)
			{
				CoreLabel token_1 = statement[i];
				string originalText = token_1.Get(typeof(CoreAnnotations.StatementTextAnnotation));
				string uppercase = originalText.Length == 0 ? originalText : char.ToUpperCase(originalText[0]) + Sharpen.Runtime.Substring(originalText, 1);
				if (i == 0)
				{
					token_1.Set(typeof(CoreAnnotations.StatementTextAnnotation), uppercase);
				}
				else
				{
					if (Optional.OfNullable(token_1.Tag()).Map(null).OrElse(false))
					{
						token_1.Set(typeof(CoreAnnotations.StatementTextAnnotation), uppercase);
					}
					else
					{
						switch (originalText.ToLower())
						{
							case "i":
							{
								token_1.Set(typeof(CoreAnnotations.StatementTextAnnotation), uppercase);
								break;
							}

							default:
							{
								token_1.Set(typeof(CoreAnnotations.StatementTextAnnotation), originalText.ToLower());
								break;
							}
						}
					}
				}
			}
			// 3. Fix the tense of the question
			// 3.1. Get tense + participality(sp?)
			bool past = false;
			bool participle = false;
			foreach (CoreLabel token_2 in question)
			{
				switch (Optional.OfNullable(token_2.Lemma()).OrElse(token_2.Word()).ToLower())
				{
					case "do":
					{
						switch (token_2.Tag())
						{
							case "VBG":
							{
								participle = true;
								goto case "VB";
							}

							case "VB":
							{
								past = false;
								goto TENSE_LOOP_break;
							}

							case "VBN":
							{
								participle = true;
								goto case "VBD";
							}

							case "VBD":
							{
								past = true;
								goto TENSE_LOOP_break;
							}
						}
						break;
					}
				}
			}
TENSE_LOOP_break: ;
			// 3.2. Get plurality
			bool plural = false;
			foreach (CoreLabel token_3 in statement)
			{
				switch (Optional.OfNullable(token_3.Tag()).OrElse(string.Empty))
				{
					case "NN":
					case "NNP":
					{
						plural = false;
						goto PLURALITY_LOOP_break;
					}

					case "NNS":
					case "NNPS":
					{
						plural = true;
						goto PLURALITY_LOOP_break;
					}
				}
			}
PLURALITY_LOOP_break: ;
			// 3.3. Get person
			int person = 3;
			// 1st, 2nd, or 3rd
			foreach (CoreLabel token_4 in statement)
			{
				if (Optional.OfNullable(token_4.Tag()).Map(null).OrElse(false))
				{
					break;
				}
				switch (token_4.Get(typeof(CoreAnnotations.StatementTextAnnotation)).ToLower())
				{
					case "us":
					{
						plural = true;
						person = 1;
						goto PERSON_LOOP_break;
					}

					case "i":
					case "me":
					case "mine":
					case "my":
					{
						plural = false;
						person = 1;
						goto PERSON_LOOP_break;
					}

					case "you":
					{
						plural = false;
						person = 2;
						goto PERSON_LOOP_break;
					}

					case "they":
					case "them":
					{
						plural = true;
						person = 2;
						goto PERSON_LOOP_break;
					}

					case "he":
					case "she":
					case "him":
					case "her":
					case "it":
					{
						plural = false;
						person = 3;
						goto PERSON_LOOP_break;
					}
				}
			}
PERSON_LOOP_break: ;
			// 3.4. Conjugate the verb
			VerbTense tense = VerbTense.Of(past, plural, participle, person);
			bool foundVerb = false;
			foreach (CoreLabel token_5 in statement)
			{
				if (Optional.OfNullable(token_5.Tag()).Map(null).OrElse(false))
				{
					foundVerb = true;
					token_5.Set(typeof(CoreAnnotations.StatementTextAnnotation), tense.ConjugateEnglish(token_5.Get(typeof(CoreAnnotations.StatementTextAnnotation)), false));
				}
			}
			if (!foundVerb)
			{
				foreach (CoreLabel token_1 in statement)
				{
					if (Optional.OfNullable(token_1.Tag()).Map(null).OrElse(false))
					{
						token_1.Set(typeof(CoreAnnotations.StatementTextAnnotation), tense.ConjugateEnglish(token_1.Get(typeof(CoreAnnotations.StatementTextAnnotation)), false));
					}
				}
			}
			// Return
			return Java.Util.Collections.SingletonList(statement);
		}

		/// <summary>Convert a question to a statement, if possible.</summary>
		/// <remarks>
		/// Convert a question to a statement, if possible.
		/// <ul>
		/// <li>The question must have words, lemmas, and part of speech tags.</li>
		/// <li>The question must have valid punctuation.</li>
		/// </ul>
		/// </remarks>
		/// <param name="question">The question to convert to a statement.</param>
		/// <returns>A list of statement translations of the question. This is usually a singleton list.</returns>
		public virtual IList<IList<CoreLabel>> ToStatement(IList<CoreLabel> question)
		{
			TokenSequenceMatcher matcher;
			if ((matcher = triggerWhatIsThere.Matcher(question)).Matches())
			{
				// must come before triggerWhatIs
				return PostProcess(question, ProcessWhatIsThere(matcher));
			}
			else
			{
				if ((matcher = triggerWhNNWill.Matcher(question)).Matches())
				{
					// must come before triggerWhNNIs
					return PostProcess(question, ProcessWhNNWill(matcher));
				}
				else
				{
					if ((matcher = triggerWhNNIs.Matcher(question)).Matches())
					{
						// must come before triggerWhatIs
						return PostProcess(question, ProcessWhNNIs(matcher));
					}
					else
					{
						if ((matcher = triggerWhNNHave.Matcher(question)).Matches())
						{
							// must come before triggerWhatHave
							return PostProcess(question, ProcessWhNNHaveIs(matcher));
						}
						else
						{
							if ((matcher = triggerWhNNHaveNN.Matcher(question)).Matches())
							{
								// must come before triggerWhatHave
								return PostProcess(question, ProcessWhNNHaveNN(matcher));
							}
							else
							{
								if ((matcher = triggerHow.Matcher(question)).Matches())
								{
									// must come before triggerWhatIs
									return PostProcess(question, ProcessHow(matcher));
								}
								else
								{
									if ((matcher = triggerHowMuchDo.Matcher(question)).Matches())
									{
										return PostProcess(question, ProcessHowMuchDo(matcher));
									}
									else
									{
										if ((matcher = triggerWhatIs.Matcher(question)).Matches())
										{
											return PostProcess(question, ProcessWhatIs(matcher));
										}
										else
										{
											if ((matcher = triggerWhatHave.Matcher(question)).Matches())
											{
												return PostProcess(question, ProcessWhatHave(matcher));
											}
											else
											{
												if ((matcher = triggerWhereDo.Matcher(question)).Matches())
												{
													return PostProcess(question, ProcessWhereDo(matcher, question));
												}
												else
												{
													if ((matcher = triggerWhereIs.Matcher(question)).Matches())
													{
														return PostProcess(question, ProcessWhereIs(matcher));
													}
													else
													{
														if ((matcher = triggerWhoIs.Matcher(question)).Matches())
														{
															return PostProcess(question, ProcessWhoIs(matcher));
														}
														else
														{
															if ((matcher = triggerWhoDid.Matcher(question)).Matches())
															{
																return PostProcess(question, ProcessWhoDid(matcher));
															}
															else
															{
																if ((matcher = triggerWhatDo.Matcher(question)).Matches())
																{
																	return PostProcess(question, ProcessWhatDo(matcher));
																}
																else
																{
																	if ((matcher = triggerWhenDo.Matcher(question)).Matches())
																	{
																		return PostProcess(question, ProcessWhenDo(matcher));
																	}
																	else
																	{
																		return Java.Util.Collections.EmptyList();
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,lemma"));
			QuestionToStatementTranslator translator = new QuestionToStatementTranslator();
			IOUtils.Console("question> ", null);
		}
	}
}
