using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// Version of ColumnDocumentReaderAndWriter that doesn't read in entire file and
	/// stores it in memory before parsing it.
	/// </summary>
	/// <remarks>
	/// Version of ColumnDocumentReaderAndWriter that doesn't read in entire file and
	/// stores it in memory before parsing it.
	/// Reads in one line at a time. Assumes that sequences are broken up by empty
	/// lines.
	/// Also differs from ColumnDocumentReaderAndWriter in following ways:
	/// <ul>
	/// <li>Splits on tabs (delimiterPattern)</li>
	/// <li>Replaces within field whitespaces with "_" (replaceWhitespace)</li>
	/// <li>Assumes that a line with just one column and starts
	/// with "* xxxxx" indicates the document id (hasDocId)</li>
	/// </ul>
	/// Accepts the following properties
	/// <table>
	/// <tr><th>Field</th><th>Type</th><th>Default</th><th>Description</th></tr>
	/// <tr><td>
	/// <c>columns</c>
	/// </td><td>String</td><td>
	/// <c/>
	/// </td><td>Comma separated list of mapping between annotation (see
	/// <see cref="Edu.Stanford.Nlp.Ling.AnnotationLookup"/>
	/// ) and column index (starting from 0).  Example:
	/// <c>word=0,tag=1</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>delimiter</c>
	/// </td><td>String</td><td>
	/// <c>\t</c>
	/// </td><td>Regular expression for delimiter</td></tr>
	/// <tr><td>
	/// <c>replaceWhitespace</c>
	/// </td><td>Boolean</td><td>
	/// <see langword="true"/>
	/// </td><td>Replace whitespaces with "_"</td></tr>
	/// <tr><td>
	/// <c>tokens</c>
	/// </td><td>Class</td>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokensAnnotation">edu.stanford.nlp.ling.CoreAnnotations$TokensAnnotation</see>
	/// </td>
	/// <td>Annotation field for tokens</td></tr>
	/// <tr><td>
	/// <c>tokenFactory</c>
	/// </td><td>Class</td>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Process.CoreLabelTokenFactory">edu.stanford.nlp.process.CoreLabelTokenFactory</see>
	/// </td>
	/// <td>Factory for creating tokens</td></tr>
	/// </table>
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <author>Sonal Gupta (made the class generic)</author>
	[System.Serializable]
	public class ColumnTabDocumentReaderWriter<In> : IDocumentReaderAndWriter<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ColumnTabDocumentReaderWriter));

		private const long serialVersionUID = 1;

		private string[] map;

		private Pattern delimiterPattern = Pattern.Compile("\t");

		private Pattern whitespacePattern = Pattern.Compile("\\s");

		private bool replaceWhitespace = true;

		private string tokensAnnotationClassName;

		private ICoreTokenFactory<In> tokenFactory;

		// = null;
		/// <summary>
		/// reads the tokenFactory and tokensAnnotationClassName from
		/// <see cref="SeqClassifierFlags"/>
		/// </summary>
		public virtual void Init(SeqClassifierFlags flags)
		{
			if (flags.tokensAnnotationClassName != null)
			{
				this.tokensAnnotationClassName = flags.tokensAnnotationClassName;
			}
			else
			{
				this.tokensAnnotationClassName = "edu.stanford.nlp.ling.CoreAnnotations$TokensAnnotation";
			}
			if (flags.tokenFactory != null)
			{
				try
				{
					this.tokenFactory = (ICoreTokenFactory<In>)System.Activator.CreateInstance(Sharpen.Runtime.GetType(flags.tokenFactory));
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
			else
			{
				this.tokenFactory = (ICoreTokenFactory<In>)new CoreLabelTokenFactory();
			}
			Init(flags, this.tokenFactory, this.tokensAnnotationClassName);
		}

		public virtual void Init(Properties props)
		{
			Init(string.Empty, props);
		}

		public virtual void Init(string name, Properties props)
		{
			string prefix = (name == null) ? string.Empty : name + ".";
			string delimiterRegex = props.GetProperty(prefix + "delimiter");
			if (delimiterRegex != null)
			{
				delimiterPattern = Pattern.Compile(delimiterRegex);
			}
			replaceWhitespace = PropertiesUtils.GetBool(props, prefix + "replaceWhitespace", replaceWhitespace);
			string mapString = props.GetProperty(prefix + "columns");
			tokensAnnotationClassName = props.GetProperty(prefix + "tokens", "edu.stanford.nlp.ling.CoreAnnotations$TokensAnnotation");
			string tokenFactoryClassName = props.GetProperty(prefix + "tokenFactory");
			if (tokenFactoryClassName != null)
			{
				try
				{
					this.tokenFactory = (ICoreTokenFactory<In>)System.Activator.CreateInstance(Sharpen.Runtime.GetType(tokenFactoryClassName));
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
			else
			{
				this.tokenFactory = (ICoreTokenFactory<In>)new CoreLabelTokenFactory();
			}
			Init(mapString, this.tokenFactory, this.tokensAnnotationClassName);
		}

		public virtual void Init(string map)
		{
			Init(map, (ICoreTokenFactory<In>)new CoreLabelTokenFactory(), "edu.stanford.nlp.ling.CoreAnnotations$TokensAnnotation");
		}

		public virtual void Init(SeqClassifierFlags flags, ICoreTokenFactory<In> tokenFactory, string tokensAnnotationClassName)
		{
			this.map = StringUtils.MapStringToArray(flags.map);
			this.tokenFactory = tokenFactory;
			this.tokensAnnotationClassName = tokensAnnotationClassName;
		}

		public virtual void Init(string map, ICoreTokenFactory<In> tokenFactory, string tokensAnnotationClassName)
		{
			this.map = StringUtils.MapStringToArray(map);
			this.tokenFactory = tokenFactory;
			this.tokensAnnotationClassName = tokensAnnotationClassName;
		}

		public virtual IEnumerator<IList<In>> GetIterator(Reader r)
		{
			BufferedReader br;
			if (r is BufferedReader)
			{
				br = (BufferedReader)r;
			}
			else
			{
				br = new BufferedReader(r);
			}
			return new ColumnTabDocumentReaderWriter.BufferedReaderIterator(new ColumnTabDocumentReaderWriter.ColumnDocBufferedGetNextTokens(this, br));
		}

		public virtual IEnumerator<Annotation> GetDocIterator(Reader r)
		{
			BufferedReader br;
			if (r is BufferedReader)
			{
				br = (BufferedReader)r;
			}
			else
			{
				br = new BufferedReader(r);
			}
			return new ColumnTabDocumentReaderWriter.BufferedReaderIterator<Annotation>(new ColumnTabDocumentReaderWriter.ColumnDocBufferedGetNext(this, br, false));
		}

		public virtual IEnumerator<Annotation> GetDocIterator(Reader r, bool includeText)
		{
			BufferedReader br;
			if (r is BufferedReader)
			{
				br = (BufferedReader)r;
			}
			else
			{
				br = new BufferedReader(r);
			}
			return new ColumnTabDocumentReaderWriter.BufferedReaderIterator<Annotation>(new ColumnTabDocumentReaderWriter.ColumnDocBufferedGetNext(this, br, false, includeText));
		}

		private interface IGetNextFunction<E>
		{
			E GetNext();
		}

		private class BufferedReaderIterator<E> : AbstractIterator<E>
		{
			internal E nextItem;

			internal ColumnTabDocumentReaderWriter.IGetNextFunction<E> getNextFunc;

			public BufferedReaderIterator(ColumnTabDocumentReaderWriter.IGetNextFunction<E> getNextFunc)
			{
				this.getNextFunc = getNextFunc;
				this.nextItem = getNextFunc.GetNext();
			}

			public override bool MoveNext()
			{
				return nextItem != null;
			}

			public override E Current
			{
				get
				{
					if (nextItem == null)
					{
						throw new NoSuchElementException();
					}
					E item = nextItem;
					nextItem = getNextFunc.GetNext();
					return item;
				}
			}
		}

		private class ColumnDocBufferedGetNextTokens<In> : ColumnTabDocumentReaderWriter.IGetNextFunction<IList<In>>
			where In : ICoreMap
		{
			internal ColumnTabDocumentReaderWriter.ColumnDocBufferedGetNext docGetNext;

			public ColumnDocBufferedGetNextTokens(ColumnTabDocumentReaderWriter<In> _enclosing, BufferedReader br)
			{
				this._enclosing = _enclosing;
				this.docGetNext = new ColumnTabDocumentReaderWriter.ColumnDocBufferedGetNext(this, br, true);
			}

			public virtual IList<In> GetNext()
			{
				try
				{
					ICoreMap m = this.docGetNext.GetNext();
					Type tokensAnnotationClass = Sharpen.Runtime.GetType(this._enclosing.tokensAnnotationClassName);
					return (IList<In>)((m != null) ? m.Get(tokensAnnotationClass) : null);
				}
				catch (TypeLoadException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
				return null;
			}

			private readonly ColumnTabDocumentReaderWriter<In> _enclosing;
		}

		private static string Join<In>(IEnumerable<In> l, Type textKey, string glue)
			where In : ICoreMap
		{
			StringBuilder sb = new StringBuilder();
			foreach (IN o in l)
			{
				if (sb.Length > 0)
				{
					sb.Append(glue);
				}
				sb.Append(o.Get(textKey));
			}
			return sb.ToString();
		}

		private class ColumnDocBufferedGetNext : ColumnTabDocumentReaderWriter.IGetNextFunction<Annotation>
		{
			private BufferedReader br;

			internal bool includeText = false;

			internal bool keepBoundaries = false;

			internal bool returnTokensOnEmptyLine = true;

			internal bool hasDocId = true;

			internal bool hasDocStart = false;

			internal string docId;

			internal string newDocId;

			internal int itemCnt = 0;

			internal int lineCnt = 0;

			public ColumnDocBufferedGetNext(ColumnTabDocumentReaderWriter<In> _enclosing, BufferedReader br)
				: this(br, true, false)
			{
				this._enclosing = _enclosing;
			}

			public ColumnDocBufferedGetNext(ColumnTabDocumentReaderWriter<In> _enclosing, BufferedReader br, bool returnSegmentsAsDocs)
				: this(br, returnSegmentsAsDocs, false)
			{
				this._enclosing = _enclosing;
			}

			public ColumnDocBufferedGetNext(ColumnTabDocumentReaderWriter<In> _enclosing, BufferedReader br, bool returnSegmentsAsDocs, bool includeText)
			{
				this._enclosing = _enclosing;
				this.br = br;
				this.includeText = includeText;
				if (returnSegmentsAsDocs)
				{
					this.keepBoundaries = false;
					this.returnTokensOnEmptyLine = true;
					this.hasDocStart = false;
				}
				else
				{
					this.keepBoundaries = true;
					this.returnTokensOnEmptyLine = false;
					this.hasDocStart = true;
				}
			}

			private Annotation CreateDoc(string docId, IList<In> tokens, IList<IntPair> sentenceBoundaries, bool includeText)
			{
				try
				{
					string docText = includeText ? ColumnTabDocumentReaderWriter.Join(tokens, typeof(CoreAnnotations.TextAnnotation), " ") : null;
					Annotation doc = new Annotation(docText);
					doc.Set(typeof(CoreAnnotations.DocIDAnnotation), docId);
					Type tokensClass = Sharpen.Runtime.GetType(this._enclosing.tokensAnnotationClassName);
					doc.Set(tokensClass, tokens);
					bool setTokenCharOffsets = includeText;
					if (setTokenCharOffsets)
					{
						int i = 0;
						foreach (IN token in tokens)
						{
							string tokenText = token.Get(typeof(CoreAnnotations.TextAnnotation));
							token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), i);
							i += tokenText.Length;
							token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), i);
							/*
							* if (i > docText.length()) { log.info("index " + i +
							* " larger than docText length " + docText.length());
							* log.info("Token: " + tokenText);
							* log.info("DocText: " + docText); }
							*/
							System.Diagnostics.Debug.Assert((i <= docText.Length));
							i++;
						}
					}
					// Skip space
					if (sentenceBoundaries != null)
					{
						IList<ICoreMap> sentences = new List<ICoreMap>(sentenceBoundaries.Count);
						foreach (IntPair p in sentenceBoundaries)
						{
							// get the sentence text from the first and last character offsets
							IList<In> sentenceTokens = new List<In>(tokens.SubList(p.GetSource(), p.GetTarget() + 1));
							int begin = sentenceTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
							int last = sentenceTokens.Count - 1;
							int end = sentenceTokens[last].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
							string sentenceText = includeText ? ColumnTabDocumentReaderWriter.Join(sentenceTokens, typeof(CoreAnnotations.TextAnnotation), " ") : null;
							// create a sentence annotation with text and token offsets
							Annotation sentence = new Annotation(sentenceText);
							sentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
							sentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
							sentence.Set(tokensClass, sentenceTokens);
							sentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), p.GetSource());
							sentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), p.GetTarget() + 1);
							int sentenceIndex = sentences.Count;
							sentence.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
							// add the sentence to the list
							sentences.Add(sentence);
						}
						// add the sentences annotations to the document
						doc.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
					}
					return doc;
				}
				catch (TypeLoadException e)
				{
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				return null;
			}

			private void MarkBoundary(IList<In> words, IList<IntPair> boundaries)
			{
				if (words != null && !words.IsEmpty())
				{
					int curWordIndex = words.Count - 1;
					if (boundaries.IsEmpty())
					{
						boundaries.Add(new IntPair(0, curWordIndex));
					}
					else
					{
						int lastWordIndex = boundaries[boundaries.Count - 1].GetTarget();
						if (lastWordIndex < curWordIndex)
						{
							boundaries.Add(new IntPair(lastWordIndex + 1, curWordIndex));
						}
					}
				}
			}

			public virtual Annotation GetNext()
			{
				if (this.itemCnt > 0 && this.itemCnt % 1000 == 0)
				{
					ColumnTabDocumentReaderWriter.log.Info("[" + this.itemCnt + "," + this.lineCnt + "]");
					if (this.itemCnt % 10000 == 9000)
					{
						ColumnTabDocumentReaderWriter.log.Info();
					}
				}
				try
				{
					string line;
					IList<In> words = null;
					IList<IntPair> boundaries = null;
					if (this.keepBoundaries)
					{
						boundaries = new List<IntPair>();
					}
					while ((line = this.br.ReadLine()) != null)
					{
						this.lineCnt++;
						line = line.Trim();
						if (line.Length != 0)
						{
							string[] info = this._enclosing.delimiterPattern.Split(line);
							if (this._enclosing.replaceWhitespace)
							{
								for (int i = 0; i < info.Length; i++)
								{
									info[i] = this._enclosing.whitespacePattern.Matcher(info[i]).ReplaceAll("_");
								}
							}
							if (this.hasDocId && line.StartsWith("* ") && info.Length == 1)
							{
								this.newDocId = Sharpen.Runtime.Substring(line, 2);
								if (words != null)
								{
									return this.CreateDoc(this.docId, words, boundaries, this.includeText);
								}
							}
							else
							{
								if (this.hasDocStart && "-DOCSTART-".Equals(info[0]))
								{
									this.newDocId = "doc" + this.itemCnt;
									if (words != null)
									{
										if (this.keepBoundaries)
										{
											this.MarkBoundary(words, boundaries);
										}
										return this.CreateDoc(this.docId, words, boundaries, this.includeText);
									}
								}
								else
								{
									if (words == null)
									{
										words = new List<In>();
										this.docId = this.newDocId;
										this.itemCnt++;
									}
									IN wi;
									if (info.Length == this._enclosing.map.Length)
									{
										wi = this._enclosing.tokenFactory.MakeToken(this._enclosing.map, info);
									}
									else
									{
										wi = this._enclosing.tokenFactory.MakeToken(this._enclosing.map, Sharpen.Collections.ToArray(Arrays.AsList(info).SubList(0, this._enclosing.map.Length), new string[this._enclosing.map.Length]));
									}
									words.Add(wi);
								}
							}
						}
						else
						{
							if (this.returnTokensOnEmptyLine && words != null)
							{
								if (this.keepBoundaries)
								{
									this.MarkBoundary(words, boundaries);
								}
								return this.CreateDoc(this.docId, words, boundaries, this.includeText);
							}
							else
							{
								if (this.keepBoundaries)
								{
									this.MarkBoundary(words, boundaries);
								}
							}
						}
					}
					if (words == null)
					{
						ColumnTabDocumentReaderWriter.log.Info("[" + this.itemCnt + "," + this.lineCnt + "]");
					}
					if (this.keepBoundaries)
					{
						this.MarkBoundary(words, boundaries);
					}
					return (words == null) ? null : this.CreateDoc(this.docId, words, boundaries, this.includeText);
				}
				catch (IOException ex)
				{
					ColumnTabDocumentReaderWriter.log.Info("IOException: " + ex);
					throw new Exception(ex);
				}
			}

			private readonly ColumnTabDocumentReaderWriter<In> _enclosing;
		}

		// end class ColumnDocParser
		public virtual void PrintAnswers(IList<In> doc, PrintWriter @out)
		{
			foreach (IN wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string goldAnswer = wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				string tokenStr = StringUtils.GetNotNullString(wi.Get(typeof(CoreAnnotations.TextAnnotation)));
				@out.Println(tokenStr + "\t" + goldAnswer + "\t" + answer);
			}
			@out.Println();
		}
	}
}
