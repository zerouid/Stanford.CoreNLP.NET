using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>DocumentReader for the original CoNLL 03 format.</summary>
	/// <remarks>
	/// DocumentReader for the original CoNLL 03 format.  In this format, there is
	/// one word per line, with extra attributes of a word (POS tag, chunk, etc.) in
	/// other space or tab separated columns, where leading and trailing whitespace
	/// on the line are ignored.  Sentences are supposedly
	/// separated by a blank line (one with no non-whitespace characters), but
	/// where blank lines occur is in practice often fairly random. In particular,
	/// sometimes entities span blank lines.  Nevertheless, in this class, like in
	/// our original CoNLL system, these blank lines are preserved as a special
	/// BOUNDARY token and detected and exploited by some features. The text is
	/// divided into documents at each '-DOCSTART-' token, which is seen as a
	/// special token, which is also preserved.  The reader can read data in any
	/// of the IOB/IOE/etc. formats and output tokens in any other, based on the
	/// entitySubclassification flag.
	/// <p>
	/// This reader is specifically for replicating CoNLL systems. For normal use,
	/// you should use the saner ColumnDocumentReaderAndWriter.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Huy Nguyen</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class CoNLLDocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		private const long serialVersionUID = 6281374154299530460L;

		public const string Boundary = "*BOUNDARY*";

		/// <summary>Historically, this reader used to treat the whole input as one document, but now it doesn't</summary>
		private const bool TreatFileAsOneDocument = false;

		private static readonly Pattern docPattern = Pattern.Compile("^\\s*-DOCSTART-\\s");

		private static readonly Pattern white = Pattern.Compile("^\\s*$");

		private SeqClassifierFlags flags;

		// = null;
		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
		}

		public override string ToString()
		{
			return "CoNLLDocumentReaderAndWriter[entitySubclassification: " + flags.entitySubclassification + ", intern: " + flags.intern + ']';
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return new CoNLLDocumentReaderAndWriter.CoNLLIterator(this, r);
		}

		private class CoNLLIterator : AbstractIterator<IList<CoreLabel>>
		{
			public CoNLLIterator(CoNLLDocumentReaderAndWriter _enclosing, Reader r)
			{
				this._enclosing = _enclosing;
				this.stringIter = CoNLLDocumentReaderAndWriter.SplitIntoDocs(r);
			}

			public override bool MoveNext()
			{
				return this.stringIter.MoveNext();
			}

			public override IList<CoreLabel> Current
			{
				get
				{
					return this._enclosing.ProcessDocument(this.stringIter.Current);
				}
			}

			private IEnumerator<string> stringIter;

			private readonly CoNLLDocumentReaderAndWriter _enclosing;
			// = null;
		}

		// end class CoNLLIterator
		private static IEnumerator<string> SplitIntoDocs(Reader r)
		{
			ICollection<string> docs = new List<string>();
			ObjectBank<string> ob = ObjectBank.GetLineIterator(r);
			StringBuilder current = new StringBuilder();
			Matcher matcher = docPattern.Matcher(string.Empty);
			foreach (string line in ob)
			{
				if (matcher.Reset(line).LookingAt())
				{
					// Start new doc, store old one if non-empty
					if (current.Length > 0)
					{
						docs.Add(current.ToString());
						current.Length = 0;
					}
				}
				current.Append(line).Append('\n');
			}
			if (current.Length > 0)
			{
				docs.Add(current.ToString());
			}
			return docs.GetEnumerator();
		}

		private IList<CoreLabel> ProcessDocument(string doc)
		{
			IList<CoreLabel> list = new List<CoreLabel>();
			string[] lines = doc.Split("\n");
			foreach (string line in lines)
			{
				if (!flags.deleteBlankLines || !white.Matcher(line).Matches())
				{
					list.Add(MakeCoreLabel(line));
				}
			}
			IOBUtils.EntitySubclassify(list, typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol, flags.entitySubclassification, flags.intern);
			return list;
		}

		/// <summary>
		/// This deals with the CoNLL files for different languages which have
		/// between 2 and 5 columns on non-blank lines.
		/// </summary>
		/// <param name="line">A line of CoNLL input</param>
		/// <returns>The constructed token</returns>
		private CoreLabel MakeCoreLabel(string line)
		{
			CoreLabel wi = new CoreLabel();
			// wi.line = line;
			string[] bits = line.Split("\\s+");
			switch (bits.Length)
			{
				case 0:
				case 1:
				{
					wi.SetWord(Boundary);
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
					break;
				}

				case 2:
				{
					wi.SetWord(bits[0]);
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), bits[1]);
					break;
				}

				case 3:
				{
					wi.SetWord(bits[0]);
					wi.SetTag(bits[1]);
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), bits[2]);
					break;
				}

				case 4:
				{
					wi.SetWord(bits[0]);
					wi.SetTag(bits[1]);
					wi.Set(typeof(CoreAnnotations.ChunkAnnotation), bits[2]);
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), bits[3]);
					break;
				}

				case 5:
				{
					if (flags.useLemmaAsWord)
					{
						wi.SetWord(bits[1]);
					}
					else
					{
						wi.SetWord(bits[0]);
					}
					wi.Set(typeof(CoreAnnotations.LemmaAnnotation), bits[1]);
					wi.SetTag(bits[2]);
					wi.Set(typeof(CoreAnnotations.ChunkAnnotation), bits[3]);
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), bits[4]);
					break;
				}

				default:
				{
					throw new RuntimeIOException("Unexpected input (many fields): " + line);
				}
			}
			//Value annotation is used in a lot of place in corenlp so setting here as the word itself
			wi.Set(typeof(CoreAnnotations.ValueAnnotation), wi.Word());
			// The copy to GoldAnswerAnnotation is done before the recoding is done, and so it preserves the original coding.
			// This is important if the original coding is true, but the recoding is defective (like IOB2 to IO), since
			// it will allow correct evaluation later.
			wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
			return wi;
		}

		/// <summary>
		/// Return the coding scheme to IOB1 coding, regardless of what was used
		/// internally (unless retainEntitySubclassification is set).
		/// </summary>
		/// <remarks>
		/// Return the coding scheme to IOB1 coding, regardless of what was used
		/// internally (unless retainEntitySubclassification is set).
		/// This is useful for scoring against CoNLL test output.
		/// </remarks>
		/// <param name="tokens">List of tokens in some NER encoding</param>
		private void DeEndify(IList<CoreLabel> tokens)
		{
			if (flags.retainEntitySubclassification)
			{
				return;
			}
			IOBUtils.EntitySubclassify(tokens, typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol, "iob1", flags.intern);
		}

		/// <summary>Write a standard CoNLL format output file.</summary>
		/// <param name="doc">The document: A List of CoreLabel</param>
		/// <param name="out">Where to send the answers to</param>
		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter @out)
		{
			// boolean tagsMerged = flags.mergeTags;
			// boolean useHead = flags.splitOnHead;
			if (!Sharpen.Runtime.EqualsIgnoreCase("iob1", flags.entitySubclassification))
			{
				DeEndify(doc);
			}
			foreach (CoreLabel fl in doc)
			{
				string word = fl.Word();
				if (word == Boundary)
				{
					// Using == is okay, because it is set to constant
					@out.Println();
				}
				else
				{
					string gold = fl.GetString<CoreAnnotations.GoldAnswerAnnotation>();
					string guess = fl.Get(typeof(CoreAnnotations.AnswerAnnotation));
					// log.info(word + "\t" + gold + "\t" + guess));
					string pos = fl.GetString<CoreAnnotations.PartOfSpeechAnnotation>();
					string chunk = fl.GetString<CoreAnnotations.ChunkAnnotation>();
					@out.Println(fl.Word() + '\t' + pos + '\t' + chunk + '\t' + gold + '\t' + guess);
				}
			}
		}

		private static StringBuilder MaybeIncrementCounter(StringBuilder inProgressMisc, ICounter<string> miscCounter)
		{
			if (inProgressMisc.Length > 0)
			{
				miscCounter.IncrementCount(inProgressMisc.ToString());
				inProgressMisc = new StringBuilder();
			}
			return inProgressMisc;
		}

		/// <summary>Count some stats on what occurs in a file.</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			CoNLLDocumentReaderAndWriter rw = new CoNLLDocumentReaderAndWriter();
			rw.Init(new SeqClassifierFlags());
			int numDocs = 0;
			int numTokens = 0;
			int numEntities = 0;
			string lastAnsBase = string.Empty;
			ICounter<string> miscCounter = new ClassicCounter<string>();
			StringBuilder inProgressMisc = new StringBuilder();
			for (IEnumerator<IList<CoreLabel>> it = rw.GetIterator(IOUtils.ReaderFromString(args[0])); it.MoveNext(); )
			{
				IList<CoreLabel> doc = it.Current;
				numDocs++;
				foreach (CoreLabel fl in doc)
				{
					string word = fl.Word();
					// System.out.println("FL " + (++i) + " was " + fl);
					if (word.Equals(Boundary))
					{
						continue;
					}
					string ans = fl.Get(typeof(CoreAnnotations.AnswerAnnotation));
					string ansBase;
					string ansPrefix;
					string[] bits = ans.Split("-");
					if (bits.Length == 1)
					{
						ansBase = bits[0];
						ansPrefix = string.Empty;
					}
					else
					{
						ansBase = bits[1];
						ansPrefix = bits[0];
					}
					numTokens++;
					if (!ansBase.Equals("O"))
					{
						if (ansBase.Equals(lastAnsBase))
						{
							if (ansPrefix.Equals("B"))
							{
								numEntities++;
								inProgressMisc = MaybeIncrementCounter(inProgressMisc, miscCounter);
							}
						}
						else
						{
							numEntities++;
							inProgressMisc = MaybeIncrementCounter(inProgressMisc, miscCounter);
						}
						if (ansBase.Equals("MISC"))
						{
							if (inProgressMisc.Length > 0)
							{
								// already something there
								inProgressMisc.Append(' ');
							}
							inProgressMisc.Append(word);
						}
					}
					else
					{
						inProgressMisc = MaybeIncrementCounter(inProgressMisc, miscCounter);
					}
					lastAnsBase = ansBase;
				}
			}
			// for tokens
			// for documents
			System.Console.Out.WriteLine("File " + args[0] + " has " + numDocs + " documents, " + numTokens + " (non-blank line) tokens and " + numEntities + " entities.");
			System.Console.Out.Printf("Here are the %.0f MISC items with counts:%n", miscCounter.TotalCount());
			System.Console.Out.WriteLine(Counters.ToVerticalString(miscCounter, "%.0f\t%s"));
		}
		// end main
	}
}
