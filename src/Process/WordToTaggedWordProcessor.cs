using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Java.Net;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Transforms a Document of Words into a document all or partly of
	/// TaggedWords by breaking words on a tag divider character.
	/// </summary>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Christopher Manning</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public class WordToTaggedWordProcessor<In, L, F> : AbstractListProcessor<IN, IHasWord, L, F>
		where In : IHasWord
	{
		/// <summary>The char that we will split on.</summary>
		protected internal char splitChar;

		/// <summary>
		/// Returns a new Document where each Word with a tag has been converted
		/// to a TaggedWord.
		/// </summary>
		/// <remarks>
		/// Returns a new Document where each Word with a tag has been converted
		/// to a TaggedWord.  Things in the input which don't implement HasWord
		/// will be deleted in the output.  Things which do will be scanned for
		/// being word + splitChar + tag.  If they are, they are split up and
		/// inserted as TaggedWords, otherwise they are added to the document
		/// with their current type.  More precisely, they will be split on the
		/// last instance of splitChar with index above 0.  This will give the
		/// correct split, providing tags don't include the splitChar, regardless
		/// of escaping, and will not allow an empty or null word - you can think
		/// of the first character as always being escaped.
		/// </remarks>
		/// <param name="words">The input Document (should be of HasWords)</param>
		/// <returns>A new Document, perhaps with some of the things TaggedWords</returns>
		public override IList<IHasWord> Process<_T0>(IList<_T0> words)
		{
			IList<IHasWord> result = new List<IHasWord>();
			foreach (IHasWord w in words)
			{
				result.Add(SplitTag(w));
			}
			return result;
		}

		/// <summary>Splits the Word w on the character splitChar.</summary>
		private IHasWord SplitTag(IHasWord w)
		{
			if (splitChar == 0)
			{
				return w;
			}
			string s = w.Word();
			int split = s.LastIndexOf(splitChar);
			if (split <= 0)
			{
				// == 0 isn't allowed - no empty words!
				return w;
			}
			string word = Sharpen.Runtime.Substring(s, 0, split);
			string tag = Sharpen.Runtime.Substring(s, split + 1, s.Length);
			return new TaggedWord(word, tag);
		}

		/// <summary>
		/// Create a <code>WordToTaggedWordProcessor</code> using the default
		/// forward slash character to split on.
		/// </summary>
		public WordToTaggedWordProcessor()
			: this('/')
		{
		}

		/// <summary>Flexibly set the tag splitting chars.</summary>
		/// <remarks>
		/// Flexibly set the tag splitting chars.  A splitChar of 0 is
		/// interpreted to mean never split off a tag.
		/// </remarks>
		/// <param name="splitChar">The character at which to split</param>
		public WordToTaggedWordProcessor(char splitChar)
		{
			this.splitChar = splitChar;
		}

		/// <summary>This will print out some text, recognizing tags.</summary>
		/// <remarks>
		/// This will print out some text, recognizing tags.  It can be used to
		/// test tag breaking.  <br />  Usage: <code>
		/// java edu.stanford.nlp.process.WordToTaggedWordProcessor fileOrUrl
		/// </code>
		/// </remarks>
		/// <param name="args">Command line argument: a file or URL</param>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Out.WriteLine("usage: java edu.stanford.nlp.process.WordToTaggedWordProcessor fileOrUrl");
				System.Environment.Exit(0);
			}
			string filename = args[0];
			try
			{
				IDocument<IHasWord, Word, Word> d;
				if (filename.StartsWith("http://"))
				{
					IDocument<IHasWord, Word, Word> dpre = new BasicDocument<IHasWord>().Init(new URL(filename));
					IDocumentProcessor<Word, Word, IHasWord, Word> notags = new StripTagsProcessor<IHasWord, Word>();
					d = notags.ProcessDocument(dpre);
				}
				else
				{
					d = new BasicDocument<IHasWord>().Init(new File(filename));
				}
				IDocumentProcessor<Word, IHasWord, IHasWord, Word> proc = new Edu.Stanford.Nlp.Process.WordToTaggedWordProcessor<Word, IHasWord, Word>();
				IDocument<IHasWord, Word, IHasWord> sentd = proc.ProcessDocument(d);
				// System.out.println(sentd);
				int i = 0;
				foreach (IHasWord w in sentd)
				{
					System.Console.Out.WriteLine(i + ": " + w);
					i++;
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
