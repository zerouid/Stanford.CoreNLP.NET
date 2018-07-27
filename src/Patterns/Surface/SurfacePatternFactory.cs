using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by sonalg on 10/27/14.</summary>
	public class SurfacePatternFactory : PatternFactory
	{
		/// <summary>
		/// Use POS tag restriction in the target term: One of this and
		/// <code>addPatWithoutPOS</code> has to be true.
		/// </summary>
		public static bool usePOS4Pattern = true;

		/// <summary>Use first two letters of the POS tag</summary>
		public static bool useCoarsePOS = true;

		/// <summary>
		/// Add patterns without POS restriction as well: One of this and
		/// <code>usePOS4Pattern</code> has to be true.
		/// </summary>
		public static bool addPatWithoutPOS = true;

		/// <summary>Consider contexts longer or equal to these many tokens.</summary>
		public static int minWindow4Pattern = 2;

		/// <summary>
		/// Consider contexts less than or equal to these many tokens -- total of left
		/// and right contexts be can double of this.
		/// </summary>
		public static int maxWindow4Pattern = 4;

		/// <summary>Consider contexts on the left of a token.</summary>
		public static bool usePreviousContext = true;

		/// <summary>Consider contexts on the right of a token.</summary>
		public static bool useNextContext = false;

		/// <summary>
		/// If the whole (either left or right) context is just stop words, add the
		/// pattern only if number of tokens is equal or more than this.
		/// </summary>
		/// <remarks>
		/// If the whole (either left or right) context is just stop words, add the
		/// pattern only if number of tokens is equal or more than this. This is get
		/// patterns like "I am on X" but ignore "on X".
		/// </remarks>
		public static int numMinStopWordsToAdd = 3;

		/// <summary>Adds the parent's tag from the parse tree to the target phrase in the patterns</summary>
		public static bool useTargetParserParentRestriction = false;

		/// <summary>
		/// If the NER tag of the context tokens is not the background symbol,
		/// generalize the token with the NER tag
		/// </summary>
		public static bool useContextNERRestriction = false;

		/// <summary>Ignore words like "a", "an", "the" when matching a pattern.</summary>
		public static bool useFillerWordsInPat = true;

		public enum Genre
		{
			Prev,
			Next,
			Prevnext
		}

		internal static Token fw;

		internal static Token sw;

		public static void SetUp(Properties props)
		{
			ArgumentParser.FillOptions(typeof(PatternFactory), props);
			ArgumentParser.FillOptions(typeof(SurfacePatternFactory), props);
			ArgumentParser.FillOptions(typeof(SurfacePattern), props);
			if (!addPatWithoutPOS && !usePOS4Pattern)
			{
				throw new Exception("addPatWithoutPOS and usePOS4Pattern both cannot be false ");
			}
			fw = new Token(PatternFactory.PatternType.Surface);
			if (useFillerWordsInPat)
			{
				fw.SetEnvBindRestriction("$FILLER");
				fw.SetNumOcc(0, 2);
			}
			sw = new Token(PatternFactory.PatternType.Surface);
			if (useStopWordsBeforeTerm)
			{
				sw.SetEnvBindRestriction("$STOPWORD");
				sw.SetNumOcc(0, 2);
			}
		}

		public static ICollection<SurfacePattern> GetContext(IList<CoreLabel> sent, int i, ICollection<CandidatePhrase> stopWords)
		{
			ICollection<SurfacePattern> prevpatterns = new HashSet<SurfacePattern>();
			ICollection<SurfacePattern> nextpatterns = new HashSet<SurfacePattern>();
			ICollection<SurfacePattern> prevnextpatterns = new HashSet<SurfacePattern>();
			CoreLabel token = sent[i];
			string tag = null;
			if (usePOS4Pattern)
			{
				string fulltag = token.Tag();
				if (useCoarsePOS)
				{
					tag = Sharpen.Runtime.Substring(fulltag, 0, Math.Min(fulltag.Length, 2));
				}
				else
				{
					tag = fulltag;
				}
			}
			string nerTag = token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			for (int maxWin = 1; maxWin <= maxWindow4Pattern; maxWin++)
			{
				IList<Token> previousTokens = new List<Token>();
				IList<string> originalPrev = new List<string>();
				IList<string> originalNext = new List<string>();
				IList<Token> nextTokens = new List<Token>();
				int numStopWordsprev = 0;
				int numStopWordsnext = 0;
				// int numPrevTokensSpecial = 0, numNextTokensSpecial = 0;
				int numNonStopWordsNext = 0;
				int numNonStopWordsPrev = 0;
				bool useprev = false;
				bool usenext = false;
				PatternToken twithoutPOS = null;
				//TODO: right now using numWordsCompoundMax.
				if (addPatWithoutPOS)
				{
					twithoutPOS = new PatternToken(tag, false, numWordsCompoundMax > 1, numWordsCompoundMax, nerTag, useTargetNERRestriction, useTargetParserParentRestriction, token.Get(typeof(CoreAnnotations.GrandparentAnnotation)));
				}
				PatternToken twithPOS = null;
				if (usePOS4Pattern)
				{
					twithPOS = new PatternToken(tag, true, numWordsCompoundMax > 1, numWordsCompoundMax, nerTag, useTargetNERRestriction, useTargetParserParentRestriction, token.Get(typeof(CoreAnnotations.GrandparentAnnotation)));
				}
				if (usePreviousContext)
				{
					// int j = Math.max(0, i - 1);
					int j = i - 1;
					int numTokens = 0;
					while (numTokens < maxWin && j >= 0)
					{
						// for (int j = Math.max(i - maxWin, 0); j < i; j++) {
						CoreLabel tokenj = sent[j];
						string tokenjStr;
						if (useLemmaContextTokens)
						{
							tokenjStr = tokenj.Lemma();
						}
						else
						{
							tokenjStr = tokenj.Word();
						}
						// do not use this word in context consideration
						if (useFillerWordsInPat && fillerWords.Contains(tokenj.Word().ToLower()))
						{
							j--;
							continue;
						}
						//          if (!tokenj.containsKey(answerClass.get(label))) {
						//            throw new RuntimeException("how come the class "
						//                + answerClass.get(label) + " for token "
						//                + tokenj.word() + " in " + sent + " is not set");
						//          }
						Triple<bool, Token, string> tr = GetContextTokenStr(tokenj);
						bool isLabeledO = tr.first;
						Token strgeneric = tr.second;
						string strOriginal = tr.third;
						if (!isLabeledO)
						{
							// numPrevTokensSpecial++;
							previousTokens.Add(0, strgeneric);
							// previousTokens.add(0,
							// "[{answer:"
							// + tokenj.get(answerClass.get(label)).toString()
							// + "}]");
							originalPrev.Add(0, strOriginal);
							numNonStopWordsPrev++;
						}
						else
						{
							if (tokenj.Word().StartsWith("http"))
							{
								useprev = false;
								previousTokens.Clear();
								originalPrev.Clear();
								break;
							}
							else
							{
								Token str = SurfacePattern.GetContextToken(tokenj);
								previousTokens.Add(0, str);
								originalPrev.Add(0, tokenjStr);
								if (DoNotUse(tokenjStr, stopWords))
								{
									numStopWordsprev++;
								}
								else
								{
									numNonStopWordsPrev++;
								}
							}
						}
						numTokens++;
						j--;
					}
				}
				if (useNextContext)
				{
					int numTokens = 0;
					int j = i + 1;
					while (numTokens < maxWin && j < sent.Count)
					{
						// for (int j = i + 1; j < sent.size() && j <= i + maxWin; j++) {
						CoreLabel tokenj = sent[j];
						string tokenjStr;
						if (useLemmaContextTokens)
						{
							tokenjStr = tokenj.Lemma();
						}
						else
						{
							tokenjStr = tokenj.Word();
						}
						// do not use this word in context consideration
						if (useFillerWordsInPat && fillerWords.Contains(tokenj.Word().ToLower()))
						{
							j++;
							continue;
						}
						//          if (!tokenj.containsKey(answerClass.get(label))) {
						//            throw new RuntimeException(
						//                "how come the dict annotation for token " + tokenj.word()
						//                    + " in " + sent + " is not set");
						//          }
						Triple<bool, Token, string> tr = GetContextTokenStr(tokenj);
						bool isLabeledO = tr.first;
						Token strgeneric = tr.second;
						string strOriginal = tr.third;
						// boolean isLabeledO = tokenj.get(answerClass.get(label))
						// .equals(SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL);
						if (!isLabeledO)
						{
							// numNextTokensSpecial++;
							numNonStopWordsNext++;
							nextTokens.Add(strgeneric);
							// nextTokens.add("[{" + label + ":"
							// + tokenj.get(answerClass.get(label)).toString()
							// + "}]");
							originalNext.Add(strOriginal);
						}
						else
						{
							// originalNextStr += " "
							// + tokenj.get(answerClass.get(label)).toString();
							if (tokenj.Word().StartsWith("http"))
							{
								usenext = false;
								nextTokens.Clear();
								originalNext.Clear();
								break;
							}
							else
							{
								// if (!tokenj.word().matches("[.,?()]")) {
								Token str = SurfacePattern.GetContextToken(tokenj);
								nextTokens.Add(str);
								originalNext.Add(tokenjStr);
								if (DoNotUse(tokenjStr, stopWords))
								{
									numStopWordsnext++;
								}
								else
								{
									numNonStopWordsNext++;
								}
							}
						}
						j++;
						numTokens++;
					}
				}
				// String prevContext = null, nextContext = null;
				// int numNonSpecialPrevTokens = previousTokens.size()
				// - numPrevTokensSpecial;
				// int numNonSpecialNextTokens = nextTokens.size() - numNextTokensSpecial;
				Token[] prevContext = null;
				//String[] prevContext = null;
				//String[] prevOriginalArr = null;
				// if (previousTokens.size() >= minWindow4Pattern
				// && (numStopWordsprev < numNonSpecialPrevTokens ||
				// numNonSpecialPrevTokens > numMinStopWordsToAdd)) {
				if (previousTokens.Count >= minWindow4Pattern && (numNonStopWordsPrev > 0 || numStopWordsprev > numMinStopWordsToAdd))
				{
					// prevContext = StringUtils.join(previousTokens, fw);
					IList<Token> prevContextList = new List<Token>();
					IList<string> prevOriginal = new List<string>();
					foreach (Token p in previousTokens)
					{
						prevContextList.Add(p);
						if (!fw.IsEmpty())
						{
							prevContextList.Add(fw);
						}
					}
					// add fw and sw to the the originalprev
					foreach (string p_1 in originalPrev)
					{
						prevOriginal.Add(p_1);
						if (!fw.IsEmpty())
						{
							prevOriginal.Add(" FW ");
						}
					}
					if (!sw.IsEmpty())
					{
						prevContextList.Add(sw);
						prevOriginal.Add(" SW ");
					}
					// String str = prevContext + fw + sw;
					if (IsASCII(StringUtils.Join(prevOriginal)))
					{
						prevContext = Sharpen.Collections.ToArray(prevContextList, new Token[0]);
						//prevOriginalArr = prevOriginal.toArray(new String[0]);
						if (previousTokens.Count >= minWindow4Pattern)
						{
							if (twithoutPOS != null)
							{
								SurfacePattern pat = new SurfacePattern(prevContext, twithoutPOS, null, SurfacePatternFactory.Genre.Prev);
								prevpatterns.Add(pat);
							}
							if (twithPOS != null)
							{
								SurfacePattern patPOS = new SurfacePattern(prevContext, twithPOS, null, SurfacePatternFactory.Genre.Prev);
								prevpatterns.Add(patPOS);
							}
						}
						useprev = true;
					}
				}
				Token[] nextContext = null;
				//String [] nextOriginalArr = null;
				// if (nextTokens.size() > 0
				// && (numStopWordsnext < numNonSpecialNextTokens ||
				// numNonSpecialNextTokens > numMinStopWordsToAdd)) {
				if (nextTokens.Count > 0 && (numNonStopWordsNext > 0 || numStopWordsnext > numMinStopWordsToAdd))
				{
					// nextContext = StringUtils.join(nextTokens, fw);
					IList<Token> nextContextList = new List<Token>();
					IList<string> nextOriginal = new List<string>();
					if (!sw.IsEmpty())
					{
						nextContextList.Add(sw);
						nextOriginal.Add(" SW ");
					}
					foreach (Token n in nextTokens)
					{
						if (!fw.IsEmpty())
						{
							nextContextList.Add(fw);
						}
						nextContextList.Add(n);
					}
					foreach (string n_1 in originalNext)
					{
						if (!fw.IsEmpty())
						{
							nextOriginal.Add(" FW ");
						}
						nextOriginal.Add(n_1);
					}
					if (nextTokens.Count >= minWindow4Pattern)
					{
						nextContext = Sharpen.Collections.ToArray(nextContextList, new Token[0]);
						//nextOriginalArr =  nextOriginal.toArray(new String[0]);
						if (twithoutPOS != null)
						{
							SurfacePattern pat = new SurfacePattern(null, twithoutPOS, nextContext, SurfacePatternFactory.Genre.Next);
							nextpatterns.Add(pat);
						}
						if (twithPOS != null)
						{
							SurfacePattern patPOS = new SurfacePattern(null, twithPOS, nextContext, SurfacePatternFactory.Genre.Next);
							nextpatterns.Add(patPOS);
						}
					}
					usenext = true;
				}
				if (useprev && usenext)
				{
					// String strprev = prevContext + fw + sw;
					// String strnext = sw + fw + nextContext;
					if (previousTokens.Count + nextTokens.Count >= minWindow4Pattern)
					{
						if (twithoutPOS != null)
						{
							SurfacePattern pat = new SurfacePattern(prevContext, twithoutPOS, nextContext, SurfacePatternFactory.Genre.Prevnext);
							prevnextpatterns.Add(pat);
						}
						if (twithPOS != null)
						{
							SurfacePattern patPOS = new SurfacePattern(prevContext, twithPOS, nextContext, SurfacePatternFactory.Genre.Prevnext);
							prevnextpatterns.Add(patPOS);
						}
					}
				}
			}
			//    Triple<Set<Integer>, Set<Integer>, Set<Integer>> patterns = new Triple<Set<Integer>, Set<Integer>, Set<Integer>>(
			//        prevpatterns, nextpatterns, prevnextpatterns);
			// System.out.println("For word " + sent.get(i) + " in sentence " + sent +
			// " prev patterns are " + prevpatterns);
			// System.out.println("For word " + sent.get(i) + " in sentence " + sent +
			// " next patterns are " + nextpatterns);
			// System.out.println("For word " + sent.get(i) + " in sentence " + sent +
			// " prevnext patterns are " + prevnextpatterns);
			//getPatternIndex().finishCommit();
			return CollectionUtils.UnionAsSet(prevpatterns, nextpatterns, prevnextpatterns);
		}

		internal static Triple<bool, Token, string> GetContextTokenStr(CoreLabel tokenj)
		{
			Token strgeneric = new Token(PatternFactory.PatternType.Surface);
			string strOriginal = string.Empty;
			bool isLabeledO = true;
			//    for (Entry<String, Class<? extends TypesafeMap.Key<String>>> e : getAnswerClass().entrySet()) {
			//      if (!tokenj.get(e.getValue()).equals(backgroundSymbol)) {
			//        isLabeledO = false;
			//        if (strOriginal.isEmpty()) {
			//          strOriginal = e.getKey();
			//        } else {
			//          strOriginal += "|" + e.getKey();
			//        }
			//        strgeneric.addRestriction(e.getKey(), e.getKey());
			//      }
			//    }
			foreach (KeyValuePair<string, Type> e in ConstantsAndVariables.GetGeneralizeClasses())
			{
				if (!tokenj.ContainsKey(e.Value) || tokenj.Get(e.Value) == null)
				{
					throw new Exception(" Why does the token not have the class " + e.Value + " set? Existing classes " + tokenj.ToString(CoreLabel.OutputFormat.All));
				}
				if (!tokenj.Get(e.Value).Equals(ConstantsAndVariables.backgroundSymbol))
				{
					isLabeledO = false;
					if (strOriginal.IsEmpty())
					{
						strOriginal = e.Key;
					}
					else
					{
						strOriginal += "|" + e.Key;
					}
					strgeneric.AddORRestriction(e.Value, e.Key);
				}
			}
			if (useContextNERRestriction)
			{
				string nerTag = tokenj.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				if (nerTag != null && !nerTag.Equals(SeqClassifierFlags.DefaultBackgroundSymbol))
				{
					isLabeledO = false;
					if (strOriginal.IsEmpty())
					{
						strOriginal = nerTag;
					}
					else
					{
						strOriginal += "|" + nerTag;
					}
					strgeneric.AddORRestriction(typeof(CoreAnnotations.NamedEntityTagAnnotation), nerTag);
				}
			}
			return new Triple<bool, Token, string>(isLabeledO, strgeneric, strOriginal);
		}

		public static bool IsASCII(string text)
		{
			Java.Nio.Charset.Charset charset = Java.Nio.Charset.Charset.ForName("US-ASCII");
			string @checked = new string(Sharpen.Runtime.GetBytesForString(text, charset), charset);
			return @checked.Equals(text);
		}

		// && !text.contains("+") &&
		// !text.contains("*");// && !
		// text.contains("$") && !text.contains("\"");
		public static IDictionary<int, ISet> GetPatternsAroundTokens(DataInstance sent, ICollection<CandidatePhrase> stopWords)
		{
			IDictionary<int, ISet> p = new Dictionary<int, ISet>();
			IList<CoreLabel> tokens = sent.GetTokens();
			for (int i = 0; i < tokens.Count; i++)
			{
				//          p.put(
				//              i,
				//              new Triple<Set<Integer>, Set<Integer>, Set<Integer>>(
				//                  new HashSet<Integer>(), new HashSet<Integer>(),
				//                  new HashSet<Integer>()));
				p[i] = new HashSet<SurfacePattern>();
				CoreLabel token = tokens[i];
				// do not create patterns around stop words!
				if (PatternFactory.DoNotUse(token.Word(), stopWords))
				{
					continue;
				}
				ICollection<SurfacePattern> pat = GetContext(sent.GetTokens(), i, stopWords);
				p[i] = pat;
			}
			return p;
		}
	}
}
