using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling
{
	public class CoreUtilities
	{
		private CoreUtilities()
		{
		}

		// class of static methods
		/// <summary>
		/// Pieces a List of CoreMaps back together using
		/// word and setting a white space between each word
		/// TODO: remove this (SentenceUtils.listToString does the same thing - why 2 separate classes)
		/// </summary>
		public static string ToSentence<_T0>(IList<_T0> sentence)
			where _T0 : ICoreMap
		{
			StringBuilder text = new StringBuilder();
			for (int i = 0; i < sz; i++)
			{
				ICoreMap iw = sentence[i];
				text.Append(iw.Get(typeof(CoreAnnotations.TextAnnotation)));
				if (i < sz - 1)
				{
					text.Append(' ');
				}
			}
			return text.ToString();
		}

		public static IList<CoreLabel> DeepCopy(IList<CoreLabel> tokens)
		{
			IList<CoreLabel> copy = new List<CoreLabel>();
			foreach (CoreLabel ml in tokens)
			{
				CoreLabel ml1 = new CoreLabel(ml);
				// copy the labels
				copy.Add(ml1);
			}
			return copy;
		}

		public static IList<CoreLabel> ToCoreLabelList(params string[] words)
		{
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Length);
			foreach (string word in words)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(word);
				tokens.Add(cl);
			}
			return tokens;
		}

		public static IList<CoreLabel> ToCoreLabelList(IList<string> words)
		{
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Count);
			foreach (string word in words)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(word);
				tokens.Add(cl);
			}
			return tokens;
		}

		public static IList<CoreLabel> ToCoreLabelList(string[] words, string[] tags)
		{
			System.Diagnostics.Debug.Assert(tags.Length == words.Length);
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Length);
			for (int i = 0; i < sz; i++)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(words[i]);
				cl.SetTag(tags[i]);
				tokens.Add(cl);
			}
			return tokens;
		}

		public static IList<CoreLabel> ToCoreLabelListWithCharacterOffsets(string[] words, string[] tags)
		{
			System.Diagnostics.Debug.Assert(tags.Length == words.Length);
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Length);
			int offset = 0;
			for (int i = 0; i < sz; i++)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(words[i]);
				cl.SetTag(tags[i]);
				cl.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), offset);
				offset += words[i].Length;
				cl.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), offset);
				offset++;
				// assume one space between words :-)
				tokens.Add(cl);
			}
			return tokens;
		}

		public static IList<CoreLabel> ToCoreLabelList(string[] words, string[] tags, string[] answers)
		{
			System.Diagnostics.Debug.Assert(tags.Length == words.Length);
			System.Diagnostics.Debug.Assert(answers.Length == words.Length);
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Length);
			for (int i = 0; i < sz; i++)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(words[i]);
				cl.SetTag(tags[i]);
				cl.Set(typeof(CoreAnnotations.AnswerAnnotation), answers[i]);
				tokens.Add(cl);
			}
			return tokens;
		}
	}
}
