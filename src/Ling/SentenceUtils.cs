using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>SentenceUtils holds a couple utility methods for lists that are sentences.</summary>
	/// <remarks>
	/// SentenceUtils holds a couple utility methods for lists that are sentences.
	/// Those include a method that nicely prints a list of words and methods that
	/// construct lists of words from lists of strings.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning (generified)</author>
	/// <author>John Bauer</author>
	/// <version>2010</version>
	public class SentenceUtils
	{
		private SentenceUtils()
		{
		}

		// static methods
		/// <summary>
		/// Create an ArrayList as a list of
		/// <c>TaggedWord</c>
		/// from two
		/// lists of
		/// <c>String</c>
		/// , one for the words, and the second for
		/// the tags.
		/// </summary>
		/// <param name="lex">
		/// a list whose items are of type
		/// <c>String</c>
		/// and
		/// are the words
		/// </param>
		/// <param name="tags">
		/// a list whose items are of type
		/// <c>String</c>
		/// and
		/// are the tags
		/// </param>
		/// <returns>The Sentence</returns>
		public static List<TaggedWord> ToTaggedList(IList<string> lex, IList<string> tags)
		{
			List<TaggedWord> sent = new List<TaggedWord>();
			int ls = lex.Count;
			int ts = tags.Count;
			if (ls != ts)
			{
				throw new ArgumentException("Sentence.toSentence: lengths differ");
			}
			for (int i = 0; i < ls; i++)
			{
				sent.Add(new TaggedWord(lex[i], tags[i]));
			}
			return sent;
		}

		/// <summary>
		/// Create an ArrayList as a list of
		/// <c>Word</c>
		/// from a
		/// list of
		/// <c>String</c>
		/// .
		/// </summary>
		/// <param name="lex">
		/// a list whose items are of type
		/// <c>String</c>
		/// and
		/// are the words
		/// </param>
		/// <returns>The Sentence</returns>
		public static List<Word> ToUntaggedList(IList<string> lex)
		{
			//TODO wsg2010: This should be deprecated in favor of the method below with new labels
			List<Word> sent = new List<Word>();
			foreach (string str in lex)
			{
				sent.Add(new Word(str));
			}
			return sent;
		}

		/// <summary>
		/// Create a Sentence as a list of
		/// <c>Word</c>
		/// objects from
		/// an array of String objects.
		/// </summary>
		/// <param name="words">The words to make it from</param>
		/// <returns>The Sentence</returns>
		public static List<Word> ToUntaggedList(params string[] words)
		{
			//TODO wsg2010: This should be deprecated in favor of the method below with new labels
			List<Word> sent = new List<Word>();
			foreach (string str in words)
			{
				sent.Add(new Word(str));
			}
			return sent;
		}

		public static IList<IHasWord> ToWordList(params string[] words)
		{
			IList<IHasWord> sent = new List<IHasWord>();
			foreach (string word in words)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetValue(word);
				cl.SetWord(word);
				sent.Add(cl);
			}
			return sent;
		}

		/// <summary>
		/// Create a sentence as a List of
		/// <c>CoreLabel</c>
		/// objects from
		/// an array (or varargs) of String objects.
		/// </summary>
		/// <param name="words">The words to make it from</param>
		/// <returns>The Sentence</returns>
		public static IList<CoreLabel> ToCoreLabelList(params string[] words)
		{
			IList<CoreLabel> sent = new List<CoreLabel>(words.Length);
			foreach (string word in words)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetValue(word);
				cl.SetWord(word);
				sent.Add(cl);
			}
			return sent;
		}

		/// <summary>
		/// Create a sentence as a List of
		/// <c>CoreLabel</c>
		/// objects from
		/// a List of other label objects.
		/// </summary>
		/// <param name="words">The words to make it from</param>
		/// <returns>The Sentence</returns>
		public static IList<CoreLabel> ToCoreLabelList<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			IList<CoreLabel> sent = new List<CoreLabel>(words.Count);
			foreach (IHasWord word in words)
			{
				CoreLabel cl = new CoreLabel();
				if (word is ILabel)
				{
					cl.SetValue(((ILabel)word).Value());
				}
				cl.SetWord(word.Word());
				if (word is IHasTag)
				{
					cl.SetTag(((IHasTag)word).Tag());
				}
				if (word is IHasLemma)
				{
					cl.SetLemma(((IHasLemma)word).Lemma());
				}
				sent.Add(cl);
			}
			return sent;
		}

		/// <summary>Returns the sentence as a string with a space between words.</summary>
		/// <remarks>
		/// Returns the sentence as a string with a space between words.
		/// It prints out the
		/// <c>value()</c>
		/// of each item -
		/// this will give the expected answer for a short form representation
		/// of the "sentence" over a range of cases.  It is equivalent to
		/// calling
		/// <c>toString(true)</c>
		/// .
		/// TODO: Sentence used to be a subclass of ArrayList, with this
		/// method as the toString.  Therefore, there may be instances of
		/// ArrayList being printed that expect this method to be used.
		/// </remarks>
		/// <param name="list">The tokenized sentence to print out</param>
		/// <returns>The tokenized sentence as a String</returns>
		public static string ListToString<T>(IList<T> list)
		{
			return ListToString(list, true);
		}

		/// <summary>Returns the sentence as a string with a space between words.</summary>
		/// <remarks>
		/// Returns the sentence as a string with a space between words.
		/// Designed to work robustly, even if the elements stored in the
		/// 'Sentence' are not of type Label.
		/// This one uses the default separators for any word type that uses
		/// separators, such as TaggedWord.
		/// </remarks>
		/// <param name="list">The tokenized sentence to print out</param>
		/// <param name="justValue">
		/// If
		/// <see langword="true"/>
		/// and the elements are of type
		/// <c>Label</c>
		/// , return just the
		/// <c>value()</c>
		/// of the
		/// <c>Label</c>
		/// of each word;
		/// otherwise,
		/// call the
		/// <c>toString()</c>
		/// method on each item.
		/// </param>
		/// <returns>The sentence in String form</returns>
		public static string ListToString<T>(IList<T> list, bool justValue)
		{
			return ListToString(list, justValue, null);
		}

		/// <summary>
		/// As already described, but if separator is not null, then objects
		/// such as TaggedWord
		/// </summary>
		/// <param name="separator">
		/// The string used to separate Word and Tag
		/// in TaggedWord, etc
		/// </param>
		public static string ListToString<T>(IList<T> list, bool justValue, string separator)
		{
			StringBuilder s = new StringBuilder();
			for (IEnumerator<T> wordIterator = list.GetEnumerator(); wordIterator.MoveNext(); )
			{
				T o = wordIterator.Current;
				s.Append(WordToString(o, justValue, separator));
				if (wordIterator.MoveNext())
				{
					s.Append(' ');
				}
			}
			return s.ToString();
		}

		/// <summary>Pretty print CoreMap classes using the same semantics as the toShorterString method.</summary>
		public static string ListToString<T>(IList<T> list, params string[] keys)
			where T : ICoreMap
		{
			StringBuilder s = new StringBuilder();
			for (IEnumerator<T> wordIterator = list.GetEnumerator(); wordIterator.MoveNext(); )
			{
				T o = wordIterator.Current;
				s.Append(o.ToShorterString(keys));
				if (wordIterator.MoveNext())
				{
					s.Append(' ');
				}
			}
			return s.ToString();
		}

		/// <summary>
		/// Returns the sentence as a string, based on the original text and spacing
		/// prior to tokenization.
		/// </summary>
		/// <remarks>
		/// Returns the sentence as a string, based on the original text and spacing
		/// prior to tokenization.
		/// This method assumes that this extra information has been encoded in CoreLabel
		/// objects for each token of the sentence, which do have the original spacing
		/// preserved (done with "invertible=true" for PTBTokenizer).
		/// However, the method has loose typing for easier inter-operation
		/// with old code that still works with a
		/// <c>List&lt;HasWord&gt;</c>
		/// .
		/// </remarks>
		/// <param name="list">The sentence (List of tokens) to print out</param>
		/// <returns>The original sentence String, which may contain newlines or other artifacts of spacing</returns>
		public static string ListToOriginalTextString<T>(IList<T> list)
			where T : IHasWord
		{
			return ListToOriginalTextString(list, true);
		}

		/// <summary>
		/// Returns the sentence as a string, based on the original text and spacing
		/// prior to tokenization.
		/// </summary>
		/// <remarks>
		/// Returns the sentence as a string, based on the original text and spacing
		/// prior to tokenization.
		/// This method assumes that this extra information has been encoded in CoreLabel
		/// objects for each token of the sentence, which do have the original spacing
		/// preserved (done with "invertible=true" for PTBTokenizer). If that information
		/// is not there, you will see null outputs, and if you do not pass in a List
		/// of CoreLabel objects, then the code will Exception.
		/// The method has loose typing for easier inter-operation
		/// with old code that still works with a
		/// <c>List&lt;HasWord&gt;</c>
		/// .
		/// </remarks>
		/// <param name="list">The sentence (List of tokens) to print out</param>
		/// <param name="printBeforeBeforeStart">
		/// Whether to print the BeforeAnnotation before the first token
		/// of the sentence. (In general, the BeforeAnnotation is the same
		/// as the AfterAnnotation of the preceding token. So, usually this
		/// is correct to do only for the first sentence of a text.)
		/// </param>
		/// <returns>The original sentence String, which may contain newlines or other artifacts of spacing</returns>
		public static string ListToOriginalTextString<T>(IList<T> list, bool printBeforeBeforeStart)
			where T : IHasWord
		{
			if (list == null)
			{
				return null;
			}
			StringBuilder s = new StringBuilder();
			foreach (IHasWord word in list)
			{
				CoreLabel cl = (CoreLabel)word;
				if (printBeforeBeforeStart)
				{
					// Only print Before for first token, since otherwise same as After of previous token
					// BUG: if you print a sequence of sentences, you double up between sentence spacing.
					if (cl.Get(typeof(CoreAnnotations.BeforeAnnotation)) != null)
					{
						s.Append(cl.Get(typeof(CoreAnnotations.BeforeAnnotation)));
					}
					printBeforeBeforeStart = false;
				}
				s.Append(cl.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
				if (cl.Get(typeof(CoreAnnotations.AfterAnnotation)) != null)
				{
					s.Append(cl.Get(typeof(CoreAnnotations.AfterAnnotation)));
				}
				else
				{
					s.Append(' ');
				}
			}
			return s.ToString();
		}

		public static string WordToString<T>(T o, bool justValue)
		{
			return WordToString(o, justValue, null);
		}

		public static string WordToString<T>(T o, bool justValue, string separator)
		{
			if (justValue && o is ILabel)
			{
				if (o is CoreLabel)
				{
					CoreLabel l = (CoreLabel)o;
					string w = l.Value();
					if (w == null)
					{
						w = l.Word();
					}
					return w;
				}
				else
				{
					return (((ILabel)o).Value());
				}
			}
			else
			{
				if (o is CoreLabel)
				{
					CoreLabel l = ((CoreLabel)o);
					string w = l.Value();
					if (w == null)
					{
						w = l.Word();
					}
					if (l.Tag() != null)
					{
						if (separator == null)
						{
							return w + CoreLabel.TagSeparator + l.Tag();
						}
						else
						{
							return w + separator + l.Tag();
						}
					}
					return w;
				}
				else
				{
					// an interface that covered these next four cases would be
					// nice, but we're moving away from these data types anyway
					if (separator != null && o is TaggedWord)
					{
						return ((TaggedWord)o).ToString(separator);
					}
					else
					{
						if (separator != null && o is LabeledWord)
						{
							return ((LabeledWord)o).ToString(separator);
						}
						else
						{
							if (separator != null && o is WordLemmaTag)
							{
								return ((WordLemmaTag)o).ToString(separator);
							}
							else
							{
								if (separator != null && o is WordTag)
								{
									return ((WordTag)o).ToString(separator);
								}
								else
								{
									return (o.ToString());
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns the substring of the sentence from start (inclusive)
		/// to end (exclusive).
		/// </summary>
		/// <param name="start">Leftmost index of the substring</param>
		/// <param name="end">Rightmost index of the ngram</param>
		/// <returns>
		/// The ngram as a String. Currently returns null if one of the indices is out of bounds.
		/// But maybe it should exception instead.
		/// </returns>
		public static string ExtractNgram<T>(IList<T> list, int start, int end)
		{
			if (start < 0 || end > list.Count || start >= end)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			for (int i = start; i < end; i++)
			{
				T o = list[i];
				if (sb.Length != 0)
				{
					sb.Append(' ');
				}
				sb.Append((o is IHasWord) ? ((IHasWord)o).Word() : o.ToString());
			}
			return sb.ToString();
		}
	}
}
