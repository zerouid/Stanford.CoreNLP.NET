using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This class provides methods for reading plain text documents and writing out
	/// those documents once classified in several different formats.
	/// </summary>
	/// <remarks>
	/// This class provides methods for reading plain text documents and writing out
	/// those documents once classified in several different formats.
	/// The output formats are named: slashTags, xml, inlineXML, tsv, tabbedEntities.
	/// <i>Implementation note:</i> see
	/// itest/src/edu/stanford/nlp/ie/crf/CRFClassifierITest.java for examples and
	/// test cases for the output options.
	/// This class works over a list of anything that extends
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// .
	/// The usual case is
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
	/// .
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning (new output options organization)</author>
	/// <author>Sonal Gupta (made the class generic)</author>
	[System.Serializable]
	public class PlainTextDocumentReaderAndWriter<In> : IDocumentReaderAndWriter<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter));

		private const long serialVersionUID = -2420535144980273136L;

		[System.Serializable]
		public sealed class OutputStyle
		{
			public static readonly PlainTextDocumentReaderAndWriter.OutputStyle SlashTags = new PlainTextDocumentReaderAndWriter.OutputStyle("slashTags");

			public static readonly PlainTextDocumentReaderAndWriter.OutputStyle Xml = new PlainTextDocumentReaderAndWriter.OutputStyle("xml");

			public static readonly PlainTextDocumentReaderAndWriter.OutputStyle InlineXml = new PlainTextDocumentReaderAndWriter.OutputStyle("inlineXML");

			public static readonly PlainTextDocumentReaderAndWriter.OutputStyle Tsv = new PlainTextDocumentReaderAndWriter.OutputStyle("tsv");

			public static readonly PlainTextDocumentReaderAndWriter.OutputStyle Tabbed = new PlainTextDocumentReaderAndWriter.OutputStyle("tabbedEntities");

			private readonly string shortName;

			internal OutputStyle(string shortName)
			{
				this.shortName = shortName;
			}

			private static readonly IDictionary<string, PlainTextDocumentReaderAndWriter.OutputStyle> shortNames = Generics.NewHashMap();

			static OutputStyle()
			{
				foreach (PlainTextDocumentReaderAndWriter.OutputStyle style in PlainTextDocumentReaderAndWriter.OutputStyle.Values())
				{
					PlainTextDocumentReaderAndWriter.OutputStyle.shortNames[style.shortName] = style;
				}
			}

			/// <summary>
			/// Convert a String expressing an output format to its internal
			/// coding as an OutputStyle.
			/// </summary>
			/// <param name="name">The String name</param>
			/// <returns>OutputStyle The internal constant</returns>
			public static PlainTextDocumentReaderAndWriter.OutputStyle FromShortName(string name)
			{
				PlainTextDocumentReaderAndWriter.OutputStyle result = PlainTextDocumentReaderAndWriter.OutputStyle.shortNames[name];
				if (result == null)
				{
					throw new ArgumentException(name + " is not an OutputStyle");
				}
				return result;
			}

			public static bool DefaultToPreserveSpacing(string str)
			{
				return str.Equals(PlainTextDocumentReaderAndWriter.OutputStyle.Xml.shortName) || str.Equals(PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml.shortName);
			}
		}

		private static readonly Pattern sgml = Pattern.Compile("<[^>]*>");

		private readonly WordToSentenceProcessor<In> wts = new WordToSentenceProcessor<In>(WordToSentenceProcessor.NewlineIsSentenceBreak.Always);

		private SeqClassifierFlags flags;

		private ITokenizerFactory<In> tokenizerFactory;

		/// <summary>Construct a PlainTextDocumentReaderAndWriter.</summary>
		/// <remarks>
		/// Construct a PlainTextDocumentReaderAndWriter. You should call init() after
		/// using the constructor.
		/// </remarks>
		public PlainTextDocumentReaderAndWriter()
		{
		}

		// end enum Output style
		// = null;
		public virtual void Init(SeqClassifierFlags flags)
		{
			string options = "tokenizeNLs=false,invertible=true";
			if (flags.tokenizerOptions != null)
			{
				options = options + ',' + flags.tokenizerOptions;
			}
			ITokenizerFactory<In> factory;
			if (flags.tokenizerFactory != null)
			{
				try
				{
					Type clazz = ErasureUtils.UncheckedCast(Sharpen.Runtime.GetType(flags.tokenizerFactory));
					MethodInfo factoryMethod = clazz.GetMethod("newCoreLabelTokenizerFactory", typeof(string));
					factory = ErasureUtils.UncheckedCast(factoryMethod.Invoke(null, options));
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
			else
			{
				factory = ErasureUtils.UncheckedCast(PTBTokenizer.PTBTokenizerFactory.NewCoreLabelTokenizerFactory(options));
			}
			Init(flags, factory);
		}

		public virtual void Init(SeqClassifierFlags flags, ITokenizerFactory<In> tokenizerFactory)
		{
			this.flags = flags;
			this.tokenizerFactory = tokenizerFactory;
		}

		// todo: give options for document splitting. A line or the whole file or sentence splitting as now
		public virtual IEnumerator<IList<In>> GetIterator(Reader r)
		{
			ITokenizer<In> tokenizer = tokenizerFactory.GetTokenizer(r);
			// PTBTokenizer.newPTBTokenizer(r, false, true);
			IList<In> words = new List<In>();
			IN previous = null;
			StringBuilder prepend = new StringBuilder();
			/*
			* This changes SGML tags into whitespace -- it should maybe be moved elsewhere
			*/
			while (tokenizer.MoveNext())
			{
				IN w = tokenizer.Current;
				string word = w.Get(typeof(CoreAnnotations.TextAnnotation));
				Matcher m = sgml.Matcher(word);
				if (m.Matches())
				{
					string before = StringUtils.GetNotNullString(w.Get(typeof(CoreAnnotations.BeforeAnnotation)));
					string after = StringUtils.GetNotNullString(w.Get(typeof(CoreAnnotations.AfterAnnotation)));
					prepend.Append(before).Append(word);
					if (previous != null)
					{
						string previousTokenAfter = StringUtils.GetNotNullString(previous.Get(typeof(CoreAnnotations.AfterAnnotation)));
						previous.Set(typeof(CoreAnnotations.AfterAnnotation), previousTokenAfter + word + after);
					}
				}
				else
				{
					// previous.appendAfter(w.word() + w.after());
					string before = StringUtils.GetNotNullString(w.Get(typeof(CoreAnnotations.BeforeAnnotation)));
					if (prepend.Length > 0)
					{
						prepend.Append(before);
						w.Set(typeof(CoreAnnotations.BeforeAnnotation), prepend.ToString());
						prepend = new StringBuilder();
					}
					words.Add(w);
					previous = w;
				}
			}
			IList<IList<In>> sentences = wts.Process(words);
			string after_1 = string.Empty;
			IN last = null;
			foreach (IList<In> sentence in sentences)
			{
				int pos = 0;
				foreach (IN w in sentence)
				{
					w.Set(typeof(CoreAnnotations.PositionAnnotation), int.ToString(pos));
					after_1 = StringUtils.GetNotNullString(w.Get(typeof(CoreAnnotations.AfterAnnotation)));
					w.Remove(typeof(CoreAnnotations.AfterAnnotation));
					last = w;
				}
			}
			if (last != null)
			{
				last.Set(typeof(CoreAnnotations.AfterAnnotation), after_1);
			}
			return sentences.GetEnumerator();
		}

		/// <summary>Print the classifications for the document to the given Writer.</summary>
		/// <remarks>
		/// Print the classifications for the document to the given Writer. This method
		/// now checks the
		/// <c>outputFormat</c>
		/// property, and can print in
		/// slashTags, inlineXML, xml (stand-Off XML), tsv, or a 3-column tabbed format
		/// for easy entity retrieval. For both the XML output
		/// formats, it preserves spacing, while for the other formats, it prints
		/// tokenized (since preserveSpacing output is somewhat dysfunctional with these
		/// formats, but you can control this by calling getAnswers()).
		/// </remarks>
		/// <param name="list">List of tokens with classifier answers</param>
		/// <param name="out">Where to print the output to</param>
		public virtual void PrintAnswers(IList<In> list, PrintWriter @out)
		{
			string style = null;
			if (flags != null)
			{
				style = flags.outputFormat;
			}
			if (style == null || style.IsEmpty())
			{
				style = "slashTags";
			}
			PlainTextDocumentReaderAndWriter.OutputStyle outputStyle = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(style);
			PrintAnswers(list, @out, outputStyle, PlainTextDocumentReaderAndWriter.OutputStyle.DefaultToPreserveSpacing(style));
		}

		public virtual string GetAnswers(IList<In> l, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle, bool preserveSpacing)
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			PrintAnswers(l, pw, outputStyle, preserveSpacing);
			pw.Flush();
			return sw.ToString();
		}

		public virtual void PrintAnswers(IList<In> l, PrintWriter @out, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle, bool preserveSpacing)
		{
			switch (outputStyle)
			{
				case PlainTextDocumentReaderAndWriter.OutputStyle.SlashTags:
				{
					if (preserveSpacing)
					{
						PrintAnswersAsIsText(l, @out);
					}
					else
					{
						PrintAnswersTokenizedText(l, @out);
					}
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.Xml:
				{
					if (preserveSpacing)
					{
						PrintAnswersXML(l, @out);
					}
					else
					{
						PrintAnswersTokenizedXML(l, @out);
					}
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml:
				{
					if (preserveSpacing)
					{
						PrintAnswersInlineXML(l, @out);
					}
					else
					{
						PrintAnswersTokenizedInlineXML(l, @out);
					}
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.Tsv:
				{
					if (preserveSpacing)
					{
						PrintAnswersAsIsTextTsv(l, @out);
					}
					else
					{
						PrintAnswersTokenizedTextTsv(l, @out);
					}
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.Tabbed:
				{
					if (preserveSpacing)
					{
						PrintAnswersAsIsTextTabbed(l, @out);
					}
					else
					{
						PrintAnswersTokenizedTextTabbed(l, @out);
					}
					break;
				}

				default:
				{
					throw new ArgumentException(outputStyle + " is an unsupported OutputStyle");
				}
			}
		}

		private static void PrintAnswersTokenizedText<In>(IList<In> l, PrintWriter @out)
			where In : ICoreMap
		{
			foreach (IN wi in l)
			{
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation))));
				@out.Print('/');
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
				@out.Print(' ');
			}
			@out.Println();
		}

		// put a single newline at the end [added 20091024].
		private static void PrintAnswersAsIsText<In>(IList<In> l, PrintWriter @out)
			where In : ICoreMap
		{
			foreach (IN wi in l)
			{
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.BeforeAnnotation))));
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation))));
				@out.Print('/');
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AfterAnnotation))));
			}
		}

		private static void PrintAnswersTokenizedTextTsv<In>(IList<In> l, PrintWriter @out)
			where In : ICoreMap
		{
			foreach (IN wi in l)
			{
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation))));
				@out.Print('\t');
				@out.Println(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
			}
			@out.Println();
		}

		// put a single newline at the end [added 20091024].
		private static void PrintAnswersAsIsTextTsv<In>(IList<In> l, PrintWriter @out)
			where In : ICoreMap
		{
			foreach (IN wi in l)
			{
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.BeforeAnnotation))));
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation))));
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AfterAnnotation))));
				@out.Print('\t');
				@out.Println(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
			}
		}

		private void PrintAnswersAsIsTextTabbed(IList<In> l, PrintWriter @out)
		{
			string background = flags.backgroundSymbol;
			string lastEntityType = null;
			foreach (IN wi in l)
			{
				string entityType = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string token = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation)));
				string before = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				string after = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AfterAnnotation)));
				if (entityType.Equals(lastEntityType))
				{
					// continue the same entity in column 1 or 3
					@out.Print(before);
					@out.Print(token);
					@out.Print(after);
				}
				else
				{
					if (lastEntityType != null && !background.Equals(lastEntityType))
					{
						// different entity type.  If previous not background/start, write in column 2
						@out.Print('\t');
						@out.Print(lastEntityType);
					}
					if (background.Equals(entityType))
					{
						// we'll print it in column 3. Normally, we're in column 2, unless we were at the start of doc
						if (lastEntityType == null)
						{
							@out.Print('\t');
						}
						@out.Print('\t');
					}
					else
					{
						// otherwise we're printing in column 1 again
						@out.Println();
					}
					@out.Print(before);
					@out.Print(token);
					@out.Print(after);
					lastEntityType = entityType;
				}
			}
			// if we're in the middle of printing an entity, then we should print its type
			if (lastEntityType != null && !background.Equals(lastEntityType))
			{
				@out.Print('\t');
				@out.Print(lastEntityType);
			}
			// finish line then add blank line
			@out.Println();
			@out.Println();
		}

		private void PrintAnswersTokenizedTextTabbed(IList<In> l, PrintWriter @out)
		{
			string background = flags.backgroundSymbol;
			string lastEntityType = null;
			foreach (IN wi in l)
			{
				string entityType = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string token = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation)));
				if (entityType.Equals(lastEntityType))
				{
					// continue the same entity in column 1 or 3
					@out.Print(' ');
					@out.Print(token);
				}
				else
				{
					if (lastEntityType != null && !background.Equals(lastEntityType))
					{
						// different entity type.  If previous not background/start, write in column 2
						@out.Print('\t');
						@out.Print(lastEntityType);
					}
					if (background.Equals(entityType))
					{
						// we'll print it in column 3. Normally, we're in column 2, unless we were at the start of doc
						if (lastEntityType == null)
						{
							@out.Print('\t');
						}
						@out.Print('\t');
					}
					else
					{
						// otherwise we're printing in column 1 again
						@out.Println();
					}
					@out.Print(token);
					lastEntityType = entityType;
				}
			}
			// if we're in the middle of printing an entity, then we should print its type
			if (lastEntityType != null && !background.Equals(lastEntityType))
			{
				@out.Print('\t');
				@out.Print(lastEntityType);
			}
			// finish line then add blank line
			@out.Println();
			@out.Println();
		}

		private static void PrintAnswersXML<In>(IList<In> doc, PrintWriter @out)
			where In : ICoreMap
		{
			int num = 0;
			foreach (IN wi in doc)
			{
				string prev = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				@out.Print(prev);
				@out.Print("<wi num=\"");
				// tag.append(wi.get("position"));
				@out.Print(num++);
				@out.Print("\" entity=\"");
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
				@out.Print("\">");
				@out.Print(XMLUtils.EscapeXML(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation)))));
				@out.Print("</wi>");
				string after = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AfterAnnotation)));
				@out.Print(after);
			}
		}

		private static void PrintAnswersTokenizedXML<In>(IList<In> doc, PrintWriter @out)
			where In : ICoreMap
		{
			int num = 0;
			foreach (IN wi in doc)
			{
				@out.Print("<wi num=\"");
				// tag.append(wi.get("position"));
				@out.Print(num++);
				@out.Print("\" entity=\"");
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation))));
				@out.Print("\">");
				@out.Print(XMLUtils.EscapeXML(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation)))));
				@out.Println("</wi>");
			}
		}

		private void PrintAnswersInlineXML(IList<In> doc, PrintWriter @out)
		{
			string background = flags.backgroundSymbol;
			string prevTag = background;
			for (IEnumerator<In> wordIter = doc.GetEnumerator(); wordIter.MoveNext(); )
			{
				IN wi = wordIter.Current;
				string tag = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				string before = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				string current = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
				if (!tag.Equals(prevTag))
				{
					if (!prevTag.Equals(background) && !tag.Equals(background))
					{
						@out.Print("</");
						@out.Print(prevTag);
						@out.Print('>');
						@out.Print(before);
						@out.Print('<');
						@out.Print(tag);
						@out.Print('>');
					}
					else
					{
						if (!prevTag.Equals(background))
						{
							@out.Print("</");
							@out.Print(prevTag);
							@out.Print('>');
							@out.Print(before);
						}
						else
						{
							if (!tag.Equals(background))
							{
								@out.Print(before);
								@out.Print('<');
								@out.Print(tag);
								@out.Print('>');
							}
						}
					}
				}
				else
				{
					@out.Print(before);
				}
				@out.Print(current);
				string afterWS = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AfterAnnotation)));
				if (!tag.Equals(background) && !wordIter.MoveNext())
				{
					@out.Print("</");
					@out.Print(tag);
					@out.Print('>');
					prevTag = background;
				}
				else
				{
					prevTag = tag;
				}
				@out.Print(afterWS);
			}
		}

		private void PrintAnswersTokenizedInlineXML(IList<In> doc, PrintWriter @out)
		{
			string background = flags.backgroundSymbol;
			string prevTag = background;
			bool first = true;
			for (IEnumerator<In> wordIter = doc.GetEnumerator(); wordIter.MoveNext(); )
			{
				IN wi = wordIter.Current;
				string tag = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				if (!tag.Equals(prevTag))
				{
					if (!prevTag.Equals(background) && !tag.Equals(background))
					{
						@out.Print("</");
						@out.Print(prevTag);
						@out.Print("> <");
						@out.Print(tag);
						@out.Print('>');
					}
					else
					{
						if (!prevTag.Equals(background))
						{
							@out.Print("</");
							@out.Print(prevTag);
							@out.Print("> ");
						}
						else
						{
							if (!tag.Equals(background))
							{
								if (!first)
								{
									@out.Print(' ');
								}
								@out.Print('<');
								@out.Print(tag);
								@out.Print('>');
							}
						}
					}
				}
				else
				{
					if (!first)
					{
						@out.Print(' ');
					}
				}
				first = false;
				@out.Print(StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.OriginalTextAnnotation))));
				if (!wordIter.MoveNext())
				{
					if (!tag.Equals(background))
					{
						@out.Print("</");
						@out.Print(tag);
						@out.Print('>');
					}
					@out.Print(' ');
					prevTag = background;
				}
				else
				{
					prevTag = tag;
				}
			}
			@out.Println();
		}
	}
}
