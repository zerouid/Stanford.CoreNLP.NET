using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Produces a new Document of Words in which special characters of the PTB
	/// have been properly escaped.
	/// </summary>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public class PTBEscapingProcessor<In, L, F> : AbstractListProcessor<IN, IHasWord, L, F>, IFunction<IList<IN>, IList<IHasWord>>
		where In : IHasWord
	{
		private static readonly char[] EmptyCharArray = new char[0];

		private static readonly char[] SubstChars = new char[] { '(', ')', '[', ']', '{', '}' };

		private static readonly string[] ReplaceSubsts = new string[] { "-LRB-", "-RRB-", "-LSB-", "-RSB-", "-LCB-", "-RCB-" };

		private readonly char[] substChars;

		private readonly string[] replaceSubsts;

		private readonly char[] escapeChars;

		private readonly string[] replaceEscapes;

		private readonly bool fixQuotes;

		public PTBEscapingProcessor()
			: this(true)
		{
		}

		public PTBEscapingProcessor(bool fixQuotes)
			: this(EmptyCharArray, StringUtils.EmptyStringArray, SubstChars, ReplaceSubsts, fixQuotes)
		{
		}

		public PTBEscapingProcessor(char[] escapeChars, string[] replaceEscapes, char[] substChars, string[] replaceSubsts, bool fixQuotes)
		{
			// starting about 2013, we no longer escape  * and /. We de-escape them when reading Treebank3
			// was  {'/', '*'};
			// was = {"\\/", "\\*"};
			this.escapeChars = escapeChars;
			this.replaceEscapes = replaceEscapes;
			this.substChars = substChars;
			this.replaceSubsts = replaceSubsts;
			this.fixQuotes = fixQuotes;
		}

		/*
		public Document processDocument(Document input) {
		Document result = input.blankDocument();
		result.addAll(process((List)input));
		return result;
		}
		*/
		/// <summary>Escape a List of HasWords.</summary>
		/// <remarks>
		/// Escape a List of HasWords.  Implements the
		/// Function&lt;List&lt;HasWord&gt;, List&lt;HasWord&gt;&gt; interface.
		/// </remarks>
		public virtual IList<IHasWord> Apply(IList<IN> hasWordsList)
		{
			return Process(hasWordsList);
		}

		public static string Unprocess(string s)
		{
			for (int i = 0; i < ReplaceSubsts.Length; i++)
			{
				s = s.ReplaceAll(ReplaceSubsts[i], SubstChars[i].ToString());
			}
			// at present doesn't deal with * / stuff ... never did
			return s;
		}

		/// <param name="input">must be a List of objects of type HasWord</param>
		public override IList<IHasWord> Process<_T0>(IList<_T0> input)
		{
			IList<IHasWord> output = new List<IHasWord>();
			foreach (IN h in input)
			{
				string s = h.Word();
				h.SetWord(EscapeString(s));
				output.Add(h);
			}
			if (fixQuotes)
			{
				return FixQuotes(output);
			}
			return output;
		}

		private static IList<IHasWord> FixQuotes(IList<IHasWord> input)
		{
			int inputSize = input.Count;
			LinkedList<IHasWord> result = new LinkedList<IHasWord>();
			if (inputSize == 0)
			{
				return result;
			}
			bool begin;
			// see if there is a quote at the end
			if (input[inputSize - 1].Word().Equals("\""))
			{
				// alternate from the end
				begin = false;
				for (int i = inputSize - 1; i >= 0; i--)
				{
					IHasWord hw = input[i];
					string tok = hw.Word();
					if (tok.Equals("\""))
					{
						if (begin)
						{
							hw.SetWord("``");
							begin = false;
						}
						else
						{
							hw.SetWord("\'\'");
							begin = true;
						}
					}
					// otherwise leave it alone
					result.AddFirst(hw);
				}
			}
			else
			{
				// end loop
				// alternate from the beginning
				begin = true;
				foreach (IHasWord hw in input)
				{
					string tok = hw.Word();
					if (tok.Equals("\""))
					{
						if (begin)
						{
							hw.SetWord("``");
							begin = false;
						}
						else
						{
							hw.SetWord("\'\'");
							begin = true;
						}
					}
					// otherwise leave it alone
					result.AddLast(hw);
				}
			}
			// end loop
			return result;
		}

		public virtual string EscapeString(string s)
		{
			StringBuilder buff = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char curChar = s[i];
				// run through all the chars we need to replace
				bool found = false;
				for (int k = 0; k < substChars.Length; k++)
				{
					if (curChar == substChars[k])
					{
						buff.Append(replaceSubsts[k]);
						found = true;
						break;
					}
				}
				if (found)
				{
					continue;
				}
				// don't do it if escape is already there usually
				if (curChar == '\\')
				{
					// add this and the next one unless bracket
					buff.Append(curChar);
					if (MaybeAppendOneMore(i + 1, s, buff))
					{
						i++;
					}
					found = true;
				}
				if (found)
				{
					continue;
				}
				// run through all the chars we need to escape
				for (int k_1 = 0; k_1 < escapeChars.Length; k_1++)
				{
					if (curChar == escapeChars[k_1])
					{
						buff.Append(replaceEscapes[k_1]);
						found = true;
						break;
					}
				}
				if (found)
				{
					continue;
				}
				// append the old char no matter what
				buff.Append(curChar);
			}
			return buff.ToString();
		}

		private bool MaybeAppendOneMore(int pos, string s, StringBuilder buff)
		{
			if (pos >= s.Length)
			{
				return false;
			}
			char candidate = s[pos];
			bool found = false;
			foreach (char ch in substChars)
			{
				if (candidate == ch)
				{
					found = true;
					break;
				}
			}
			if (found)
			{
				return false;
			}
			buff.Append(candidate);
			return true;
		}

		/// <summary>This will do the escaping on an input file.</summary>
		/// <remarks>
		/// This will do the escaping on an input file. Input file should already be tokenized,
		/// with tokens separated by whitespace. <br />
		/// Usage: java edu.stanford.nlp.process.PTBEscapingProcessor fileOrUrl
		/// </remarks>
		/// <param name="args">Command line argument: a file or URL</param>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Out.WriteLine("usage: java edu.stanford.nlp.process.PTBEscapingProcessor fileOrUrl");
				return;
			}
			string filename = args[0];
			try
			{
				IDocument<string, Word, Word> d;
				// initialized below
				if (filename.StartsWith("http://"))
				{
					IDocument<string, Word, Word> dpre = new BasicDocument<string>(WhitespaceTokenizer.Factory()).Init(new URL(filename));
					IDocumentProcessor<Word, Word, string, Word> notags = new StripTagsProcessor<string, Word>();
					d = notags.ProcessDocument(dpre);
				}
				else
				{
					d = new BasicDocument<string>(WhitespaceTokenizer.Factory()).Init(new File(filename));
				}
				IDocumentProcessor<Word, IHasWord, string, Word> proc = new Edu.Stanford.Nlp.Process.PTBEscapingProcessor<Word, string, Word>();
				IDocument<string, Word, IHasWord> newD = proc.ProcessDocument(d);
				foreach (IHasWord word in newD)
				{
					System.Console.Out.WriteLine(word);
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
