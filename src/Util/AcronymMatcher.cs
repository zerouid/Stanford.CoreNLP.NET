using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;





namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A simple class with a variety of acronym matching utilities.</summary>
	/// <remarks>
	/// A simple class with a variety of acronym matching utilities.
	/// You're probably looking for the method
	/// <see cref="IsAcronym(string, System.Collections.Generic.IList{E})"/>
	/// .
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class AcronymMatcher
	{
		private static readonly Pattern discardPattern = Pattern.Compile("[-._]");

		private sealed class _HashSet_22 : HashSet<string>
		{
			public _HashSet_22()
			{
				{
					this.Add("'d");
					this.Add("'ll");
					this.Add("'re");
					this.Add("'s");
					this.Add("'t");
					this.Add("'ve");
					this.Add("n't");
					this.Add("a");
					this.Add("about");
					this.Add("above");
					this.Add("after");
					this.Add("again");
					this.Add("against");
					this.Add("all");
					this.Add("am");
					this.Add("an");
					this.Add("and");
					this.Add("any");
					this.Add("are");
					this.Add("as");
					this.Add("at");
					this.Add("be");
					this.Add("because");
					this.Add("been");
					this.Add("before");
					this.Add("being");
					this.Add("below");
					this.Add("between");
					this.Add("both");
					this.Add("but");
					this.Add("by");
					this.Add("cannot");
					this.Add("could");
					this.Add("did");
					this.Add("do");
					this.Add("does");
					this.Add("doing");
					this.Add("down");
					this.Add("during");
					this.Add("each");
					this.Add("few");
					this.Add("for");
					this.Add("from");
					this.Add("further");
					this.Add("had");
					this.Add("has");
					this.Add("have");
					this.Add("having");
					this.Add("he");
					this.Add("her");
					this.Add("here");
					this.Add("hers");
					this.Add("herself");
					this.Add("him");
					this.Add("himself");
					this.Add("his");
					this.Add("how");
					this.Add("i");
					this.Add("if");
					this.Add("in");
					this.Add("into");
					this.Add("is");
					this.Add("it");
					this.Add("its");
					this.Add("itself");
					this.Add("me");
					this.Add("more");
					this.Add("most");
					this.Add("my");
					this.Add("myself");
					this.Add("no");
					this.Add("nor");
					this.Add("not");
					this.Add("of");
					this.Add("off");
					this.Add("on");
					this.Add("once");
					this.Add("only");
					this.Add("or");
					this.Add("other");
					this.Add("ought");
					this.Add("our");
					this.Add("ours");
					this.Add("ourselves");
					this.Add("out");
					this.Add("over");
					this.Add("own");
					this.Add("same");
					this.Add("she");
					this.Add("should");
					this.Add("so");
					this.Add("some");
					this.Add("such");
					this.Add("than");
					this.Add("their");
					this.Add("theirs");
					this.Add("them");
					this.Add("themselves");
					this.Add("the");
					this.Add("then");
					this.Add("there");
					this.Add("these");
					this.Add("they");
					this.Add("this");
					this.Add("those");
					this.Add("through");
					this.Add("to");
					this.Add("too");
					this.Add("under");
					this.Add("until");
					this.Add("up");
					this.Add("very");
					this.Add("was");
					this.Add("we");
					this.Add("were");
					this.Add("what");
					this.Add("when");
					this.Add("where");
					this.Add("which");
					this.Add("while");
					this.Add("who");
					this.Add("whom");
					this.Add("why");
					this.Add("with");
					this.Add("would");
					this.Add("you");
					this.Add("your");
					this.Add("yours");
					this.Add("yourself");
					this.Add("yourselves");
					this.Add("de");
					this.Add("del");
					this.Add("di");
					this.Add("y");
					this.Add("corporation");
					this.Add("corp");
					this.Add("corp.");
					this.Add("co");
					this.Add("llc");
					this.Add("inc");
					this.Add("inc.");
					this.Add("ltd");
					this.Add("ltd.");
					this.Add("llp");
					this.Add("llp.");
					this.Add("plc");
					this.Add("plc.");
					this.Add("&");
					this.Add(",");
					this.Add("-");
				}
			}
		}

		/// <summary>A set of words that should be considered stopwords for the acronym matcher</summary>
		private static readonly ICollection<string> Stopwords = Java.Util.Collections.UnmodifiableSet(new _HashSet_22());

		private AcronymMatcher()
		{
		}

		// static methods
		private static IList<string> GetTokenStrs(IList<CoreLabel> tokens)
		{
			IList<string> mainTokenStrs = new List<string>(tokens.Count);
			foreach (CoreLabel token in tokens)
			{
				string text = token.Get(typeof(CoreAnnotations.TextAnnotation));
				mainTokenStrs.Add(text);
			}
			return mainTokenStrs;
		}

		private static IList<string> GetMainTokenStrs(IList<CoreLabel> tokens)
		{
			IList<string> mainTokenStrs = new List<string>(tokens.Count);
			foreach (CoreLabel token in tokens)
			{
				string text = token.Get(typeof(CoreAnnotations.TextAnnotation));
				if (!text.IsEmpty() && (text.Length >= 4 || char.IsUpperCase(text[0])))
				{
					mainTokenStrs.Add(text);
				}
			}
			return mainTokenStrs;
		}

		private static IList<string> GetMainTokenStrs(string[] tokens)
		{
			IList<string> mainTokenStrs = new List<string>(tokens.Length);
			foreach (string text in tokens)
			{
				if (!text.IsEmpty() && (text.Length >= 4 || char.IsUpperCase(text[0])))
				{
					mainTokenStrs.Add(text);
				}
			}
			return mainTokenStrs;
		}

		public static IList<string> GetMainStrs(IList<string> tokens)
		{
			IList<string> mainTokenStrs = new List<string>(tokens.Count);
			Sharpen.Collections.AddAll(mainTokenStrs, tokens.Stream().Filter(null).Collect(Collectors.ToList()));
			return mainTokenStrs;
		}

		public static bool IsAcronym(string str, string[] tokens)
		{
			return IsAcronymImpl(str, Arrays.AsList(tokens));
		}

		// Public static utility methods
		public static bool IsAcronymImpl(string str, IList<string> tokens)
		{
			// Remove some words from the candidate acronym
			str = discardPattern.Matcher(str).ReplaceAll(string.Empty);
			// Remove stopwords if we need to
			if (str.Length != tokens.Count)
			{
				tokens = tokens.Stream().Filter(null).Collect(Collectors.ToList());
			}
			// Run the matcher
			if (str.Length == tokens.Count)
			{
				for (int i = 0; i < str.Length; i++)
				{
					char ch = char.ToUpperCase(str[i]);
					if (!tokens[i].IsEmpty() && char.ToUpperCase(tokens[i][0]) != ch)
					{
						return false;
					}
				}
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool IsAcronym<_T0>(string str, IList<_T0> tokens)
		{
			IList<string> strs = new List<string>(tokens.Count);
			foreach (object tok in tokens)
			{
				if (tok is string)
				{
					strs.Add(tok.ToString());
				}
				else
				{
					if (tok is ICoreMap)
					{
						strs.Add(((ICoreMap)tok).Get(typeof(CoreAnnotations.TextAnnotation)));
					}
					else
					{
						strs.Add(tok.ToString());
					}
				}
			}
			return IsAcronymImpl(str, strs);
		}

		/// <summary>Returns true if either chunk1 or chunk2 is acronym of the other.</summary>
		/// <returns>true if either chunk1 or chunk2 is acronym of the other</returns>
		public static bool IsAcronym(ICoreMap chunk1, ICoreMap chunk2)
		{
			string text1 = chunk1.Get(typeof(CoreAnnotations.TextAnnotation));
			string text2 = chunk2.Get(typeof(CoreAnnotations.TextAnnotation));
			if (text1.Length <= 1 || text2.Length <= 1)
			{
				return false;
			}
			IList<string> tokenStrs1 = GetTokenStrs(chunk1.Get(typeof(CoreAnnotations.TokensAnnotation)));
			IList<string> tokenStrs2 = GetTokenStrs(chunk2.Get(typeof(CoreAnnotations.TokensAnnotation)));
			bool isAcro = IsAcronymImpl(text1, tokenStrs2) || IsAcronymImpl(text2, tokenStrs1);
			if (!isAcro)
			{
				tokenStrs1 = GetMainTokenStrs(chunk1.Get(typeof(CoreAnnotations.TokensAnnotation)));
				tokenStrs2 = GetMainTokenStrs(chunk2.Get(typeof(CoreAnnotations.TokensAnnotation)));
				isAcro = IsAcronymImpl(text1, tokenStrs2) || IsAcronymImpl(text2, tokenStrs1);
			}
			return isAcro;
		}

		/// <seealso cref="IsAcronym(ICoreMap, ICoreMap)"></seealso>
		public static bool IsAcronym(string[] chunk1, string[] chunk2)
		{
			string text1 = StringUtils.Join(chunk1);
			string text2 = StringUtils.Join(chunk2);
			if (text1.Length <= 1 || text2.Length <= 1)
			{
				return false;
			}
			IList<string> tokenStrs1 = Arrays.AsList(chunk1);
			IList<string> tokenStrs2 = Arrays.AsList(chunk2);
			bool isAcro = IsAcronymImpl(text1, tokenStrs2) || IsAcronymImpl(text2, tokenStrs1);
			if (!isAcro)
			{
				tokenStrs1 = GetMainTokenStrs(chunk1);
				tokenStrs2 = GetMainTokenStrs(chunk2);
				isAcro = IsAcronymImpl(text1, tokenStrs2) || IsAcronymImpl(text2, tokenStrs1);
			}
			return isAcro;
		}

		public static bool IsFancyAcronym(string[] chunk1, string[] chunk2)
		{
			string text1 = StringUtils.Join(chunk1);
			string text2 = StringUtils.Join(chunk2);
			if (text1.Length <= 1 || text2.Length <= 1)
			{
				return false;
			}
			IList<string> tokenStrs1 = Arrays.AsList(chunk1);
			IList<string> tokenStrs2 = Arrays.AsList(chunk2);
			return IsFancyAcronymImpl(text1, tokenStrs2) || IsFancyAcronymImpl(text2, tokenStrs1);
		}

		public static bool IsFancyAcronymImpl(string str, IList<string> tokens)
		{
			str = discardPattern.Matcher(str).ReplaceAll(string.Empty);
			string text = StringUtils.Join(tokens);
			int prev_index = 0;
			for (int i = 0; i < str.Length; i++)
			{
				char ch = str[i];
				if (text.IndexOf(ch) != -1)
				{
					prev_index = text.IndexOf(ch, prev_index);
					if (prev_index == -1)
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}
			return true;
		}
	}
}
