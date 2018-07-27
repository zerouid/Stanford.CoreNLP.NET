using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>Various static convenience methods for processing Arabic parse trees.</summary>
	/// <author>Spence Green</author>
	public class ATBTreeUtils
	{
		private static readonly IPredicate<Tree> emptyFilter = new ArabicTreeNormalizer.ArabicEmptyFilter();

		private static readonly ITreeFactory tf = new LabeledScoredTreeFactory();

		public static string segMarker = "-";

		public const string morphBoundary = "+";

		public const string puncTag = "PUNC";

		private const string reservedWordList = "-PLUS- -LRB- -RRB-";

		public static readonly ICollection<string> reservedWords = Generics.NewHashSet();

		static ATBTreeUtils()
		{
			//The default segmentation marker. Can be changed for processing e.g. IBM Arabic.
			//The default morpheme boundary marker. Present only in the vocalized sections.
			//Global tag for all punctuation
			//Reserved tokens class
			Sharpen.Collections.AddAll(reservedWords, Arrays.AsList(reservedWordList.Split("\\s+")));
		}

		private ATBTreeUtils()
		{
		}

		// static class
		/// <summary>Escapes tokens from flat strings that are reserved for usage in the ATB.</summary>
		/// <param name="s">- An Arabic string</param>
		/// <returns>A string with all reserved words replaced by the appropriate tokens</returns>
		public static string Escape(string s)
		{
			if (s == null)
			{
				return null;
			}
			//LDC escape sequences (as of ATB3p3)
			s = s.ReplaceAll("\\(", "-LRB-");
			s = s.ReplaceAll("\\)", "-RRB-");
			s = s.ReplaceAll("\\+", "-PLUS-");
			return s;
		}

		/// <summary>Reverts escaping from a flat string.</summary>
		/// <param name="s">- An Arabic string</param>
		/// <returns>A string with all reserved words inserted into the appropriate locations</returns>
		public static string UnEscape(string s)
		{
			if (s == null)
			{
				return null;
			}
			//LDC escape sequences (as of ATB3p3)
			s = s.ReplaceAll("-LRB-", "(");
			s = s.ReplaceAll("-RRB-", ")");
			s = s.ReplaceAll("-PLUS-", "+");
			return s;
		}

		/// <summary>Returns the string associated with the input parse tree.</summary>
		/// <remarks>
		/// Returns the string associated with the input parse tree. Traces and
		/// ATB-specific escape sequences (e.g., "-RRB-" for ")") are removed.
		/// </remarks>
		/// <param name="t">- A parse tree</param>
		/// <returns>The yield of the input parse tree</returns>
		public static string FlattenTree(Tree t)
		{
			t = t.Prune(emptyFilter, tf);
			string flatString = SentenceUtils.ListToString(t.Yield());
			return flatString;
		}

		/// <summary>Converts a parse tree into a string of tokens.</summary>
		/// <remarks>
		/// Converts a parse tree into a string of tokens. Each token is a word and
		/// its POS tag separated by the delimiter specified by <code>separator</code>
		/// </remarks>
		/// <param name="t">- A parse tree</param>
		/// <param name="removeEscaping">- If true, remove LDC escape characters. Otherwise, leave them.</param>
		/// <param name="separator">Word/tag separator</param>
		/// <returns>A string of tagged words</returns>
		public static string TaggedStringFromTree(Tree t, bool removeEscaping, string separator)
		{
			t = t.Prune(emptyFilter, tf);
			IList<CoreLabel> taggedSentence = t.TaggedLabeledYield();
			foreach (CoreLabel token in taggedSentence)
			{
				string word = (removeEscaping) ? UnEscape(token.Word()) : token.Word();
				token.SetWord(word);
				token.SetValue(word);
			}
			return SentenceUtils.ListToString(taggedSentence, false, separator);
		}

		public static void Main(string[] args)
		{
			string debug = "( the big lion ) + (the small rabbit)";
			string escaped = Edu.Stanford.Nlp.Trees.International.Arabic.ATBTreeUtils.Escape(debug);
			System.Console.Out.WriteLine(escaped);
		}
	}
}
