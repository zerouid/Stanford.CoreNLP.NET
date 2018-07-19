using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Read _conll file format from CoNLL2011.</summary>
	/// <remarks>
	/// Read _conll file format from CoNLL2011.  See http://conll.bbn.com/index.php/data.html.
	/// CoNLL2011 files are in /u/scr/nlp/data/conll-2011/v0/data/
	/// dev
	/// train
	/// Contains *_auto_conll files (auto generated) and _gold_conll (hand labelled), default reads _gold_conll
	/// There is also /u/scr/nlp/data/conll-2011/v0/conll.trial which has *.conll files (parse has _ at end)
	/// Column 	Type 	Description
	/// 1   	Document ID 	This is a variation on the document filename
	/// 2   	Part number 	Some files are divided into multiple parts numbered as 000, 001, 002, ... etc.
	/// 3   	Word number
	/// 4   	Word itself
	/// 5   	Part-of-Speech
	/// 6   	Parse bit 	This is the bracketed structure broken before the first open parenthesis in the parse, and the word/part-of-speech leaf replaced with a *. The full parse can be created by substituting the asterix with the "([pos] [word])" string (or leaf) and concatenating the items in the rows of that column.
	/// 7   	Predicate lemma 	The predicate lemma is mentioned for the rows for which we have semantic role information. All other rows are marked with a "-"
	/// 8   	Predicate Frameset ID 	This is the PropBank frameset ID of the predicate in Column 7.
	/// 9   	Word sense 	This is the word sense of the word in Column 3.
	/// 10   	Speaker/Author 	This is the speaker or author name where available. Mostly in Broadcast Conversation and Web Log data.
	/// 11   	Named Entities 	These columns identifies the spans representing various named entities.
	/// 12:N   	Predicate Arguments 	There is one column each of predicate argument structure information for the predicate mentioned in Column 7.
	/// N   	Coreference 	Coreference chain information encoded in a parenthesis structure.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class CoNLL2011DocumentReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Dcoref.CoNLL2011DocumentReader));

		private const int FieldLast = -1;

		private const int FieldDocId = 0;

		private const int FieldPartNo = 1;

		private const int FieldWordNo = 2;

		private const int FieldWord = 3;

		private const int FieldPosTag = 4;

		private const int FieldParseBit = 5;

		private const int FieldSpeakerAuthor = 9;

		private const int FieldNerTag = 10;

		private const int FieldCoref = FieldLast;

		private const int FieldsMin = 12;

		private CoNLL2011DocumentReader.DocumentIterator docIterator;

		protected internal readonly IList<File> fileList;

		private int curFileIndex;

		private readonly CoNLL2011DocumentReader.Options options;

		public static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.Dcoref.CoNLL2011DocumentReader).FullName);

		public CoNLL2011DocumentReader(string filepath)
			: this(filepath, new CoNLL2011DocumentReader.Options())
		{
		}

		public CoNLL2011DocumentReader(string filepath, CoNLL2011DocumentReader.Options options)
		{
			//  private static final int FIELD_PRED_LEMMA = 6;
			//  private static final int FIELD_PRED_FRAMESET_ID = 7;
			//  private static final int FIELD_WORD_SENSE = 8;
			//  private static final int FIELD_PRED_ARGS = 11;  // Predicate args follow...
			// Last field
			// There should be at least 13 fields
			//  private String filepath;
			//    this.filepath = filepath;
			this.fileList = GetFiles(filepath, options.filePattern);
			this.options = options;
			if (options.sortFiles)
			{
				this.fileList.Sort();
			}
			curFileIndex = 0;
			logger.Info("Reading " + fileList.Count + " CoNll2011 files from " + filepath);
		}

		private static IList<File> GetFiles(string filepath, Pattern filter)
		{
			IEnumerable<File> iter = IOUtils.IterFilesRecursive(new File(filepath), filter);
			IList<File> fileList = new List<File>();
			foreach (File f in iter)
			{
				fileList.Add(f);
			}
			fileList.Sort();
			return fileList;
		}

		public virtual void Reset()
		{
			curFileIndex = 0;
			if (docIterator != null)
			{
				docIterator.Close();
				docIterator = null;
			}
		}

		public virtual CoNLL2011DocumentReader.Document GetNextDocument()
		{
			try
			{
				if (curFileIndex >= fileList.Count)
				{
					return null;
				}
				// DONE!
				File curFile = fileList[curFileIndex];
				if (docIterator == null)
				{
					docIterator = new CoNLL2011DocumentReader.DocumentIterator(curFile.GetAbsolutePath(), options);
				}
				while (!docIterator.MoveNext())
				{
					logger.Info("Processed " + docIterator.docCnt + " documents in " + curFile.GetAbsolutePath());
					docIterator.Close();
					curFileIndex++;
					if (curFileIndex >= fileList.Count)
					{
						return null;
					}
					// DONE!
					curFile = fileList[curFileIndex];
					docIterator = new CoNLL2011DocumentReader.DocumentIterator(curFile.GetAbsolutePath(), options);
				}
				CoNLL2011DocumentReader.Document next = docIterator.Current;
				SieveCoreferenceSystem.logger.Fine("Reading document: " + next.GetDocumentID());
				return next;
			}
			catch (IOException ex)
			{
				throw new RuntimeIOException(ex);
			}
		}

		public virtual void Close()
		{
			IOUtils.CloseIgnoringExceptions(docIterator);
		}

		public class NamedEntityAnnotation : ICoreAnnotation<ICoreMap>
		{
			public virtual Type GetType()
			{
				return typeof(ICoreMap);
			}
		}

		public class CorefMentionAnnotation : ICoreAnnotation<ICoreMap>
		{
			public virtual Type GetType()
			{
				return typeof(ICoreMap);
			}
		}

		/// <summary>Flags</summary>
		public class Options
		{
			public bool useCorefBIOESEncoding = false;

			public bool annotateTokenCoref = true;

			public bool annotateTokenSpeaker = true;

			public bool annotateTokenPos = true;

			public bool annotateTokenNer = true;

			public bool annotateTreeCoref = false;

			public bool annotateTreeNer = false;

			public string backgroundNerTag = "O";

			protected internal string fileFilter;

			protected internal Pattern filePattern;

			protected internal bool sortFiles;

			public Options()
				: this(".*_gold_conll$")
			{
			}

			public Options(string filter)
			{
				// Marks Coref mentions with prefix
				// B- begin, I- inside, E- end, S- single
				// Annotate token with CorefAnnotation
				// If token belongs to multiple clusters
				// coref clusterid are separted by '|'
				// Annotate token with SpeakerAnnotation
				// Annotate token with PartOfSpeechAnnotation
				// Annotate token with NamedEntityTagAnnotation
				// Annotate tree with CorefMentionAnnotation
				// Annotate tree with NamedEntityAnnotation
				// Background NER tag
				// _gold_conll or _auto_conll   or .conll
				fileFilter = filter;
				filePattern = Pattern.Compile(fileFilter);
			}

			public virtual void SetFilter(string filter)
			{
				fileFilter = filter;
				filePattern = Pattern.Compile(fileFilter);
			}
		}

		public class Document
		{
			internal string documentIdPart;

			internal string documentID;

			internal string partNo;

			internal IList<IList<string[]>> sentenceWordLists = new List<IList<string[]>>();

			internal Annotation annotation;

			internal CollectionValuedMap<string, ICoreMap> corefChainMap;

			internal IList<ICoreMap> nerChunks;

			public virtual string GetDocumentID()
			{
				return documentID;
			}

			public virtual void SetDocumentID(string documentID)
			{
				this.documentID = documentID;
			}

			public virtual string GetPartNo()
			{
				return partNo;
			}

			public virtual void SetPartNo(string partNo)
			{
				this.partNo = partNo;
			}

			public virtual IList<IList<string[]>> GetSentenceWordLists()
			{
				return sentenceWordLists;
			}

			public virtual void AddSentence(IList<string[]> sentence)
			{
				this.sentenceWordLists.Add(sentence);
			}

			public virtual Annotation GetAnnotation()
			{
				return annotation;
			}

			public virtual void SetAnnotation(Annotation annotation)
			{
				this.annotation = annotation;
			}

			public virtual CollectionValuedMap<string, ICoreMap> GetCorefChainMap()
			{
				return corefChainMap;
			}
		}

		private static string GetField(string[] fields, int pos)
		{
			if (pos == FieldLast)
			{
				return fields[fields.Length - 1];
			}
			else
			{
				return fields[pos];
			}
		}

		private static string ConcatField(IList<string[]> sentWords, int pos)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string[] fields in sentWords)
			{
				if (sb.Length > 0)
				{
					sb.Append(' ');
				}
				sb.Append(GetField(fields, pos));
			}
			return sb.ToString();
		}

		/// <summary>Helper iterator</summary>
		private class DocumentIterator : AbstractIterator<CoNLL2011DocumentReader.Document>, ICloseable
		{
			private static readonly Pattern delimiterPattern = Pattern.Compile("\\s+");

			private static readonly LabeledScoredTreeReaderFactory treeReaderFactory = new LabeledScoredTreeReaderFactory((TreeNormalizer)null);

			private readonly CoNLL2011DocumentReader.Options options;

			internal string filename;

			internal BufferedReader br;

			internal CoNLL2011DocumentReader.Document nextDoc;

			internal int lineCnt = 0;

			internal int docCnt = 0;

			/// <exception cref="System.IO.IOException"/>
			public DocumentIterator(string filename, CoNLL2011DocumentReader.Options options)
			{
				// State
				this.options = options;
				this.filename = filename;
				this.br = IOUtils.GetBufferedFileReader(filename);
				nextDoc = ReadNextDocument();
			}

			public override bool MoveNext()
			{
				return nextDoc != null;
			}

			public override CoNLL2011DocumentReader.Document Current
			{
				get
				{
					if (nextDoc == null)
					{
						throw new NoSuchElementException("DocumentIterator exhausted.");
					}
					CoNLL2011DocumentReader.Document curDoc = nextDoc;
					nextDoc = ReadNextDocument();
					return curDoc;
				}
			}

			private static readonly Pattern starPattern = Pattern.Compile("\\*");

			private static Tree WordsToParse(IList<string[]> sentWords)
			{
				StringBuilder sb = new StringBuilder();
				foreach (string[] fields in sentWords)
				{
					if (sb.Length > 0)
					{
						sb.Append(' ');
					}
					string str = fields[FieldParseBit].Replace("NOPARSE", "X");
					string tagword = "(" + fields[FieldPosTag] + " " + fields[FieldWord] + ")";
					// Replace stars
					int si = str.IndexOf('*');
					sb.Append(Sharpen.Runtime.Substring(str, 0, si));
					sb.Append(tagword);
					sb.Append(Sharpen.Runtime.Substring(str, si + 1));
					si = str.IndexOf('*', si + 1);
					if (si >= 0)
					{
						logger.Warning(" Parse bit with multiple *: " + str);
					}
				}
				string parseStr = sb.ToString();
				return Tree.ValueOf(parseStr, treeReaderFactory);
			}

			private static IList<Triple<int, int, string>> GetCorefSpans(IList<string[]> sentWords)
			{
				return GetLabelledSpans(sentWords, FieldCoref, Hyphen, true);
			}

			private static IList<Triple<int, int, string>> GetNerSpans(IList<string[]> sentWords)
			{
				return GetLabelledSpans(sentWords, FieldNerTag, Asterisk, false);
			}

			private const string Asterisk = "*";

			private const string Hyphen = "-";

			private static IList<Triple<int, int, string>> GetLabelledSpans(IList<string[]> sentWords, int fieldIndex, string defaultMarker, bool checkEndLabel)
			{
				IList<Triple<int, int, string>> spans = new List<Triple<int, int, string>>();
				Stack<Triple<int, int, string>> openSpans = new Stack<Triple<int, int, string>>();
				bool removeStar = (Asterisk.Equals(defaultMarker));
				for (int wordPos = 0; wordPos < sentWords.Count; wordPos++)
				{
					string[] fields = sentWords[wordPos];
					string val = GetField(fields, fieldIndex);
					if (!defaultMarker.Equals(val))
					{
						int openParenIndex = -1;
						int lastDelimiterIndex = -1;
						for (int j = 0; j < val.Length; j++)
						{
							char c = val[j];
							bool isDelimiter = false;
							if (c == '(' || c == ')' || c == '|')
							{
								if (openParenIndex >= 0)
								{
									string s = Sharpen.Runtime.Substring(val, openParenIndex + 1, j);
									if (removeStar)
									{
										s = starPattern.Matcher(s).ReplaceAll(string.Empty);
									}
									openSpans.Push(new Triple<int, int, string>(wordPos, -1, s));
									openParenIndex = -1;
								}
								isDelimiter = true;
							}
							if (c == '(')
							{
								openParenIndex = j;
							}
							else
							{
								if (c == ')')
								{
									Triple<int, int, string> t = openSpans.Pop();
									if (checkEndLabel)
									{
										// NOTE: end parens may cross (usually because mention either start or end on the same token
										// and it is just an artifact of the ordering
										string s = Sharpen.Runtime.Substring(val, lastDelimiterIndex + 1, j);
										if (!s.Equals(t.Third()))
										{
											Stack<Triple<int, int, string>> saved = new Stack<Triple<int, int, string>>();
											while (!s.Equals(t.Third()))
											{
												// find correct match
												saved.Push(t);
												if (openSpans.IsEmpty())
												{
													throw new Exception("Cannot find matching labelled span for " + s);
												}
												t = openSpans.Pop();
											}
											while (!saved.IsEmpty())
											{
												openSpans.Push(saved.Pop());
											}
											System.Diagnostics.Debug.Assert((s.Equals(t.Third())));
										}
									}
									t.SetSecond(wordPos);
									spans.Add(t);
								}
							}
							if (isDelimiter)
							{
								lastDelimiterIndex = j;
							}
						}
						if (openParenIndex >= 0)
						{
							string s = Sharpen.Runtime.Substring(val, openParenIndex + 1, val.Length);
							if (removeStar)
							{
								s = starPattern.Matcher(s).ReplaceAll(string.Empty);
							}
							openSpans.Push(new Triple<int, int, string>(wordPos, -1, s));
						}
					}
				}
				if (openSpans.Count != 0)
				{
					throw new Exception("Error extracting labelled spans for column " + fieldIndex + ": " + ConcatField(sentWords, fieldIndex));
				}
				return spans;
			}

			private ICoreMap WordsToSentence(IList<string[]> sentWords)
			{
				string sentText = ConcatField(sentWords, FieldWord);
				Annotation sentence = new Annotation(sentText);
				Tree tree = WordsToParse(sentWords);
				sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), tree);
				IList<Tree> leaves = tree.GetLeaves();
				// Check leaves == number of words
				System.Diagnostics.Debug.Assert((leaves.Count == sentWords.Count));
				IList<CoreLabel> tokens = new List<CoreLabel>(leaves.Count);
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				for (int i = 0; i < sentWords.Count; i++)
				{
					string[] fields = sentWords[i];
					int wordPos = System.Convert.ToInt32(fields[FieldWordNo]);
					System.Diagnostics.Debug.Assert((wordPos == i));
					Tree leaf = leaves[i];
					CoreLabel token = (CoreLabel)leaf.Label();
					tokens.Add(token);
					if (options.annotateTokenSpeaker)
					{
						string speaker = fields[FieldSpeakerAuthor].Replace("_", " ");
						if (!Hyphen.Equals(speaker))
						{
							token.Set(typeof(CoreAnnotations.SpeakerAnnotation), speaker);
						}
					}
				}
				if (options.annotateTokenPos)
				{
					foreach (Tree leaf in leaves)
					{
						CoreLabel token = (CoreLabel)leaf.Label();
						token.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), leaf.Parent(tree).Value());
					}
				}
				if (options.annotateTokenNer)
				{
					IList<Triple<int, int, string>> nerSpans = GetNerSpans(sentWords);
					foreach (Triple<int, int, string> nerSpan in nerSpans)
					{
						int startToken = nerSpan.First();
						int endToken = nerSpan.Second();
						/* inclusive */
						string label = nerSpan.Third();
						for (int i_1 = startToken; i_1 <= endToken; i_1++)
						{
							Tree leaf = leaves[i_1];
							CoreLabel token = (CoreLabel)leaf.Label();
							string oldLabel = token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
							if (oldLabel != null)
							{
								logger.Warning("Replacing old named entity tag " + oldLabel + " with " + label);
							}
							token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), label);
						}
					}
					foreach (CoreLabel token_1 in tokens)
					{
						if (!token_1.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
						{
							token_1.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), options.backgroundNerTag);
						}
					}
				}
				if (options.annotateTokenCoref)
				{
					IList<Triple<int, int, string>> corefSpans = GetCorefSpans(sentWords);
					foreach (Triple<int, int, string> corefSpan in corefSpans)
					{
						int startToken = corefSpan.First();
						int endToken = corefSpan.Second();
						/* inclusive */
						string label = corefSpan.Third();
						for (int i_1 = startToken; i_1 <= endToken; i_1++)
						{
							Tree leaf = leaves[i_1];
							CoreLabel token = (CoreLabel)leaf.Label();
							string curLabel = label;
							if (options.useCorefBIOESEncoding)
							{
								string prefix;
								if (startToken == endToken)
								{
									prefix = "S-";
								}
								else
								{
									if (i_1 == startToken)
									{
										prefix = "B-";
									}
									else
									{
										if (i_1 == endToken)
										{
											prefix = "E-";
										}
										else
										{
											prefix = "I-";
										}
									}
								}
								curLabel = prefix + label;
							}
							string oldLabel = token.Get(typeof(CorefCoreAnnotations.CorefAnnotation));
							if (oldLabel != null)
							{
								curLabel = oldLabel + "|" + curLabel;
							}
							token.Set(typeof(CorefCoreAnnotations.CorefAnnotation), curLabel);
						}
					}
				}
				return sentence;
			}

			public static Annotation SentencesToDocument(string documentID, IList<ICoreMap> sentences)
			{
				string docText = null;
				Annotation document = new Annotation(docText);
				document.Set(typeof(CoreAnnotations.DocIDAnnotation), documentID);
				document.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
				// Accumulate docTokens and label sentence with overall token begin/end, and sentence index annotations
				IList<CoreLabel> docTokens = new List<CoreLabel>();
				int sentenceIndex = 0;
				int tokenBegin = 0;
				foreach (ICoreMap sentenceAnnotation in sentences)
				{
					IList<CoreLabel> sentenceTokens = sentenceAnnotation.Get(typeof(CoreAnnotations.TokensAnnotation));
					Sharpen.Collections.AddAll(docTokens, sentenceTokens);
					int tokenEnd = tokenBegin + sentenceTokens.Count;
					sentenceAnnotation.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenBegin);
					sentenceAnnotation.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenEnd);
					sentenceAnnotation.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
					sentenceIndex++;
					tokenBegin = tokenEnd;
				}
				document.Set(typeof(CoreAnnotations.TokensAnnotation), docTokens);
				// Put in character offsets
				int i = 0;
				foreach (CoreLabel token in docTokens)
				{
					string tokenText = token.Get(typeof(CoreAnnotations.TextAnnotation));
					token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), i);
					i += tokenText.Length;
					token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), i);
					i++;
				}
				// Skip space
				foreach (ICoreMap sentenceAnnotation_1 in sentences)
				{
					IList<CoreLabel> sentenceTokens = sentenceAnnotation_1.Get(typeof(CoreAnnotations.TokensAnnotation));
					sentenceAnnotation_1.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), sentenceTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
					sentenceAnnotation_1.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), sentenceTokens[sentenceTokens.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
				}
				return document;
			}

			private static Tree GetLowestCommonAncestor(Tree root, int startToken, int endToken)
			{
				Tree leftLeaf = Edu.Stanford.Nlp.Trees.Trees.GetLeaf(root, startToken);
				Tree rightLeaf = Edu.Stanford.Nlp.Trees.Trees.GetLeaf(root, endToken);
				// todo [cdm 2013]: It might be good to climb certain unaries here, like VP or S under NP, but it's not good to climb all unaries (e.g., NP under FRAG)
				return Edu.Stanford.Nlp.Trees.Trees.GetLowestCommonAncestor(leftLeaf, rightLeaf, root);
			}

			private static Tree GetTreeNonTerminal(Tree root, int startToken, int endToken, bool acceptPreTerminals)
			{
				Tree t = GetLowestCommonAncestor(root, startToken, endToken);
				if (t.IsLeaf())
				{
					t = t.Parent(root);
				}
				if (!acceptPreTerminals && t.IsPreTerminal())
				{
					t = t.Parent(root);
				}
				return t;
			}

			public virtual void AnnotateDocument(CoNLL2011DocumentReader.Document document)
			{
				IList<ICoreMap> sentences = new List<ICoreMap>(document.sentenceWordLists.Count);
				foreach (IList<string[]> sentWords in document.sentenceWordLists)
				{
					sentences.Add(WordsToSentence(sentWords));
				}
				Annotation docAnnotation = SentencesToDocument(document.documentIdPart, sentences);
				/*document.documentID + "." + document.partNo */
				document.SetAnnotation(docAnnotation);
				// Do this here so we have updated character offsets and all
				CollectionValuedMap<string, ICoreMap> corefChainMap = new CollectionValuedMap<string, ICoreMap>(CollectionFactory.ArrayListFactory<ICoreMap>());
				IList<ICoreMap> nerChunks = new List<ICoreMap>();
				for (int i = 0; i < sentences.Count; i++)
				{
					ICoreMap sentence = sentences[i];
					Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					tree.SetSpans();
					IList<string[]> sentWords_1 = document.sentenceWordLists[i];
					// Get NER chunks
					IList<Triple<int, int, string>> nerSpans = GetNerSpans(sentWords_1);
					foreach (Triple<int, int, string> nerSpan in nerSpans)
					{
						int startToken = nerSpan.First();
						int endToken = nerSpan.Second();
						/* inclusive */
						string label = nerSpan.Third();
						ICoreMap nerChunk = ChunkAnnotationUtils.GetAnnotatedChunk(sentence, startToken, endToken + 1);
						nerChunk.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), label);
						nerChunk.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
						nerChunks.Add(nerChunk);
						Tree t = GetTreeNonTerminal(tree, startToken, endToken, true);
						if (t.GetSpan().GetSource() == startToken && t.GetSpan().GetTarget() == endToken)
						{
							nerChunk.Set(typeof(TreeCoreAnnotations.TreeAnnotation), t);
							if (options.annotateTreeNer)
							{
								ILabel tlabel = t.Label();
								if (tlabel is CoreLabel)
								{
									((CoreLabel)tlabel).Set(typeof(CoNLL2011DocumentReader.NamedEntityAnnotation), nerChunk);
								}
							}
						}
					}
					IList<Triple<int, int, string>> corefSpans = GetCorefSpans(sentWords_1);
					foreach (Triple<int, int, string> corefSpan in corefSpans)
					{
						int startToken = corefSpan.First();
						int endToken = corefSpan.Second();
						/* inclusive */
						string corefId = corefSpan.Third();
						ICoreMap mention = ChunkAnnotationUtils.GetAnnotatedChunk(sentence, startToken, endToken + 1);
						mention.Set(typeof(CorefCoreAnnotations.CorefAnnotation), corefId);
						mention.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
						corefChainMap.Add(corefId, mention);
						Tree t = GetTreeNonTerminal(tree, startToken, endToken, true);
						mention.Set(typeof(TreeCoreAnnotations.TreeAnnotation), t);
						if (options.annotateTreeCoref)
						{
							ILabel tlabel = t.Label();
							if (tlabel is CoreLabel)
							{
								((CoreLabel)tlabel).Set(typeof(CoNLL2011DocumentReader.CorefMentionAnnotation), mention);
							}
						}
					}
				}
				document.corefChainMap = corefChainMap;
				document.nerChunks = nerChunks;
			}

			private const string docStart = "#begin document ";

			private static readonly int docStartLength = docStart.Length;

			public virtual CoNLL2011DocumentReader.Document ReadNextDocument()
			{
				try
				{
					IList<string[]> curSentWords = new List<string[]>();
					CoNLL2011DocumentReader.Document document = null;
					for (string line; (line = br.ReadLine()) != null; )
					{
						lineCnt++;
						line = line.Trim();
						if (line.Length != 0)
						{
							if (line.StartsWith(docStart))
							{
								// Start of new document
								if (document != null)
								{
									logger.Warning("Unexpected begin document at line (\" + filename + \",\" + lineCnt + \")");
								}
								document = new CoNLL2011DocumentReader.Document();
								document.documentIdPart = Sharpen.Runtime.Substring(line, docStartLength);
							}
							else
							{
								if (line.StartsWith("#end document"))
								{
									AnnotateDocument(document);
									docCnt++;
									return document;
								}
								else
								{
									// End of document
									System.Diagnostics.Debug.Assert(document != null);
									string[] fields = delimiterPattern.Split(line);
									if (fields.Length < FieldsMin)
									{
										throw new Exception("Unexpected number of field " + fields.Length + ", expected >= " + FieldsMin + " for line (" + filename + "," + lineCnt + "): " + line);
									}
									string curDocId = fields[FieldDocId];
									string partNo = fields[FieldPartNo];
									if (document.GetDocumentID() == null)
									{
										document.SetDocumentID(curDocId);
										document.SetPartNo(partNo);
									}
									else
									{
										// Check documentID didn't suddenly change on us
										System.Diagnostics.Debug.Assert((document.GetDocumentID().Equals(curDocId)));
										System.Diagnostics.Debug.Assert((document.GetPartNo().Equals(partNo)));
									}
									curSentWords.Add(fields);
								}
							}
						}
						else
						{
							// Current sentence has ended, new sentence is about to be started
							if (curSentWords.Count > 0)
							{
								System.Diagnostics.Debug.Assert(document != null);
								document.AddSentence(curSentWords);
								curSentWords = new List<string[]>();
							}
						}
					}
				}
				catch (IOException ex)
				{
					throw new RuntimeIOException(ex);
				}
				return null;
			}

			public virtual void Close()
			{
				IOUtils.CloseIgnoringExceptions(br);
			}
		}

		// end static class DocumentIterator
		public static void Usage()
		{
			log.Info("java edu.stanford.nlp.dcoref.CoNLL2011DocumentReader [-ext <extension to match>] -i <inputpath> -o <outputfile>");
		}

		public static Pair<int, int> GetMention(int index, string corefG, IList<CoreLabel> sentenceAnno)
		{
			int i = -1;
			int end = index;
			foreach (CoreLabel newAnno in sentenceAnno)
			{
				i += 1;
				if (i > index)
				{
					string corefS = newAnno.Get(typeof(CorefCoreAnnotations.CorefAnnotation));
					if (corefS != null)
					{
						string[] allC = corefS.Split("\\|");
						if (Arrays.AsList(allC).Contains(corefG))
						{
							end = i;
						}
						else
						{
							break;
						}
					}
					else
					{
						break;
					}
				}
			}
			return Pair.MakePair(index, end);
		}

		public static bool Include(IDictionary<Pair<int, int>, string> sentenceInfo, Pair<int, int> mention, string corefG)
		{
			ICollection<Pair<int, int>> keys = sentenceInfo.Keys;
			foreach (Pair<int, int> key in keys)
			{
				string corefS = sentenceInfo[key];
				if (corefS != null && corefS.Equals(corefG))
				{
					if (key.first < mention.first && key.second.Equals(mention.second))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void WriteTabSep(PrintWriter pw, ICoreMap sentence, CollectionValuedMap<string, ICoreMap> chainmap)
		{
			IHeadFinder headFinder = new ModCollinsHeadFinder();
			IList<CoreLabel> sentenceAnno = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree sentenceTree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			IDictionary<Pair<int, int>, string> sentenceInfo = Generics.NewHashMap();
			ICollection<Tree> sentenceSubTrees = sentenceTree.SubTrees();
			sentenceTree.SetSpans();
			IDictionary<Pair<int, int>, Tree> treeSpanMap = Generics.NewHashMap();
			IDictionary<Pair<int, int>, IList<Tree>> wordSpanMap = Generics.NewHashMap();
			foreach (Tree ctree in sentenceSubTrees)
			{
				IntPair span = ctree.GetSpan();
				if (span != null)
				{
					treeSpanMap[Pair.MakePair(span.GetSource(), span.GetTarget())] = ctree;
					wordSpanMap[Pair.MakePair(span.GetSource(), span.GetTarget())] = ctree.GetLeaves();
				}
			}
			string[][] finalSentence;
			finalSentence = new string[sentenceAnno.Count][];
			IDictionary<Pair<int, int>, string> allHeads = Generics.NewHashMap();
			int index = -1;
			foreach (CoreLabel newAnno in sentenceAnno)
			{
				index += 1;
				string word = newAnno.Word();
				string tag = newAnno.Tag();
				string cat = newAnno.Ner();
				string coref = newAnno.Get(typeof(CorefCoreAnnotations.CorefAnnotation));
				finalSentence[index] = new string[4];
				finalSentence[index][0] = word;
				finalSentence[index][1] = tag;
				finalSentence[index][2] = cat;
				finalSentence[index][3] = coref;
				if (coref == null)
				{
					sentenceInfo[Pair.MakePair(index, index)] = coref;
					finalSentence[index][3] = "O";
				}
				else
				{
					string[] allC = coref.Split("\\|");
					foreach (string corefG in allC)
					{
						Pair<int, int> mention = GetMention(index, corefG, sentenceAnno);
						if (!Include(sentenceInfo, mention, corefG))
						{
							// find largest NP in mention
							sentenceInfo[mention] = corefG;
							Tree mentionTree = treeSpanMap[mention];
							string head = null;
							if (mentionTree != null)
							{
								head = mentionTree.HeadTerminal(headFinder).NodeString();
							}
							else
							{
								if (mention.first.Equals(mention.second))
								{
									head = word;
								}
							}
							allHeads[mention] = head;
						}
					}
					if (allHeads.Values.Contains(word))
					{
						finalSentence[index][3] = "MENTION";
					}
					else
					{
						finalSentence[index][3] = "O";
					}
				}
			}
			for (int i = 0; i < finalSentence.Length; i++)
			{
				string[] wordInfo = finalSentence[i];
				if (i < finalSentence.Length - 1)
				{
					string[] nextWordInfo = finalSentence[i + 1];
					if (nextWordInfo[3].Equals("MENTION") && nextWordInfo[0].Equals("'s"))
					{
						wordInfo[3] = "MENTION";
						finalSentence[i + 1][3] = "O";
					}
				}
				pw.Println(wordInfo[0] + "\t" + wordInfo[1] + "\t" + wordInfo[2] + "\t" + wordInfo[3]);
			}
			pw.Println(string.Empty);
		}

		public class CorpusStats
		{
			internal IntCounter<string> mentionTreeLabelCounter = new IntCounter<string>();

			internal IntCounter<string> mentionTreeNonPretermLabelCounter = new IntCounter<string>();

			internal IntCounter<string> mentionTreePretermNonPretermNoMatchLabelCounter = new IntCounter<string>();

			internal IntCounter<string> mentionTreeMixedLabelCounter = new IntCounter<string>();

			internal IntCounter<int> mentionTokenLengthCounter = new IntCounter<int>();

			internal IntCounter<int> nerMentionTokenLengthCounter = new IntCounter<int>();

			internal int mentionExactTreeSpan = 0;

			internal int nonPretermSpanMatches = 0;

			internal int totalMentions = 0;

			internal int nestedNerMentions = 0;

			internal int nerMentions = 0;

			public virtual void Process(CoNLL2011DocumentReader.Document doc)
			{
				IList<ICoreMap> sentences = doc.GetAnnotation().Get(typeof(CoreAnnotations.SentencesAnnotation));
				foreach (string id in doc.corefChainMap.Keys)
				{
					ICollection<ICoreMap> mentions = doc.corefChainMap[id];
					foreach (ICoreMap m in mentions)
					{
						ICoreMap sent = sentences[m.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))];
						Tree root = sent.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
						Tree t = m.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
						Tree npt = t;
						Tree npt2 = t;
						if (npt.IsPreTerminal())
						{
							npt = npt.Parent(root);
						}
						int sentTokenStart = sent.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
						int tokenStart = m.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - sentTokenStart;
						int tokenEnd = m.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - sentTokenStart;
						int length = tokenEnd - tokenStart;
						mentionTokenLengthCounter.IncrementCount(length);
						// Check if exact span
						IntPair span = t.GetSpan();
						if (span != null)
						{
							if (span.GetSource() == tokenStart && span.GetTarget() == tokenEnd - 1)
							{
								mentionExactTreeSpan++;
							}
							else
							{
								logger.Info("Tree span is " + span + ", tree node is " + t);
								logger.Info("Mention span is " + tokenStart + " " + (tokenEnd - 1) + ", mention is " + m);
							}
						}
						else
						{
							logger.Warning("No span for " + t);
						}
						IntPair nptSpan = npt.GetSpan();
						if (nptSpan.GetSource() == tokenStart && nptSpan.GetTarget() == tokenEnd - 1)
						{
							nonPretermSpanMatches++;
							npt2 = npt;
						}
						else
						{
							mentionTreePretermNonPretermNoMatchLabelCounter.IncrementCount(t.Label().Value());
							logger.Info("NPT: Tree span is " + span + ", tree node is " + npt);
							logger.Info("NPT: Mention span is " + tokenStart + " " + (tokenEnd - 1) + ", mention is " + m);
							ILabel tlabel = t.Label();
							if (tlabel is CoreLabel)
							{
								ICoreMap mention = ((CoreLabel)tlabel).Get(typeof(CoNLL2011DocumentReader.CorefMentionAnnotation));
								string corefClusterId = mention.Get(typeof(CorefCoreAnnotations.CorefAnnotation));
								ICollection<ICoreMap> clusteredMentions = doc.corefChainMap[corefClusterId];
								foreach (ICoreMap m2 in clusteredMentions)
								{
									logger.Info("NPT: Clustered mention " + m2.Get(typeof(CoreAnnotations.TextAnnotation)));
								}
							}
						}
						totalMentions++;
						mentionTreeLabelCounter.IncrementCount(t.Label().Value());
						mentionTreeNonPretermLabelCounter.IncrementCount(npt.Label().Value());
						mentionTreeMixedLabelCounter.IncrementCount(npt2.Label().Value());
						ILabel tlabel_1 = t.Label();
						if (tlabel_1 is CoreLabel)
						{
							if (((CoreLabel)tlabel_1).ContainsKey(typeof(CoNLL2011DocumentReader.NamedEntityAnnotation)))
							{
								// walk up tree
								nerMentions++;
								nerMentionTokenLengthCounter.IncrementCount(length);
								Tree parent = t.Parent(root);
								while (parent != null)
								{
									ILabel plabel = parent.Label();
									if (plabel is CoreLabel)
									{
										if (((CoreLabel)plabel).ContainsKey(typeof(CoNLL2011DocumentReader.NamedEntityAnnotation)))
										{
											logger.Info("NER Mention: " + m);
											ICoreMap parentNerChunk = ((CoreLabel)plabel).Get(typeof(CoNLL2011DocumentReader.NamedEntityAnnotation));
											logger.Info("Nested inside NER Mention: " + parentNerChunk);
											logger.Info("Nested inside NER Mention parent node: " + parent);
											nestedNerMentions++;
											break;
										}
									}
									parent = parent.Parent(root);
								}
							}
						}
					}
				}
			}

			private static void AppendFrac(StringBuilder sb, string label, int num, int den)
			{
				double frac = ((double)num) / den;
				sb.Append(label).Append("\t").Append(frac).Append("\t(").Append(num).Append("/").Append(den).Append(")");
			}

			private static void AppendIntCountStats<E>(StringBuilder sb, string label, IntCounter<E> counts)
			{
				sb.Append(label).Append("\n");
				IList<E> sortedKeys = Counters.ToSortedList(counts);
				int total = counts.TotalIntCount();
				foreach (E key in sortedKeys)
				{
					int count = counts.GetIntCount(key);
					AppendFrac(sb, key.ToString(), count, total);
					sb.Append("\n");
				}
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				AppendIntCountStats(sb, "Mention Tree Labels (no preterminals)", mentionTreeNonPretermLabelCounter);
				sb.Append("\n");
				AppendIntCountStats(sb, "Mention Tree Labels (with preterminals)", mentionTreeLabelCounter);
				sb.Append("\n");
				AppendIntCountStats(sb, "Mention Tree Labels (preterminals with parent span not match)", mentionTreePretermNonPretermNoMatchLabelCounter);
				sb.Append("\n");
				AppendIntCountStats(sb, "Mention Tree Labels (mixed)", mentionTreeMixedLabelCounter);
				sb.Append("\n");
				AppendIntCountStats(sb, "Mention Lengths", mentionTokenLengthCounter);
				sb.Append("\n");
				AppendFrac(sb, "Mention Exact Non Preterm Tree Span", nonPretermSpanMatches, totalMentions);
				sb.Append("\n");
				AppendFrac(sb, "Mention Exact Tree Span", mentionExactTreeSpan, totalMentions);
				sb.Append("\n");
				AppendFrac(sb, "NER", nerMentions, totalMentions);
				sb.Append("\n");
				AppendFrac(sb, "Nested NER", nestedNerMentions, totalMentions);
				sb.Append("\n");
				AppendIntCountStats(sb, "NER Mention Lengths", nerMentionTokenLengthCounter);
				return sb.ToString();
			}
		}

		/// <summary>Reads and dumps output, mainly for debugging.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			bool debug = bool.ParseBoolean(props.GetProperty("debug", "false"));
			string filepath = props.GetProperty("i");
			string outfile = props.GetProperty("o");
			if (filepath == null || outfile == null)
			{
				Usage();
				System.Environment.Exit(-1);
			}
			PrintWriter fout = new PrintWriter(outfile);
			logger.Info("Writing to " + outfile);
			string ext = props.GetProperty("ext");
			CoNLL2011DocumentReader.Options options;
			if (ext != null)
			{
				options = new CoNLL2011DocumentReader.Options(".*" + ext + "$");
			}
			else
			{
				options = new CoNLL2011DocumentReader.Options();
			}
			options.annotateTreeCoref = true;
			options.annotateTreeNer = true;
			CoNLL2011DocumentReader.CorpusStats corpusStats = new CoNLL2011DocumentReader.CorpusStats();
			CoNLL2011DocumentReader reader = new CoNLL2011DocumentReader(filepath, options);
			int docCnt = 0;
			int sentCnt = 0;
			int tokenCnt = 0;
			for (CoNLL2011DocumentReader.Document doc; (doc = reader.GetNextDocument()) != null; )
			{
				corpusStats.Process(doc);
				docCnt++;
				Annotation anno = doc.GetAnnotation();
				if (debug)
				{
					System.Console.Out.WriteLine("Document " + docCnt + ": " + anno.Get(typeof(CoreAnnotations.DocIDAnnotation)));
				}
				foreach (ICoreMap sentence in anno.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					if (debug)
					{
						System.Console.Out.WriteLine("Parse: " + sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
					}
					if (debug)
					{
						System.Console.Out.WriteLine("Sentence Tokens: " + StringUtils.Join(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)), ","));
					}
					WriteTabSep(fout, sentence, doc.corefChainMap);
					sentCnt++;
					tokenCnt += sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Count;
				}
				if (debug)
				{
					foreach (ICoreMap ner in doc.nerChunks)
					{
						System.Console.Out.WriteLine("NER Chunk: " + ner);
					}
					foreach (string id in doc.corefChainMap.Keys)
					{
						System.Console.Out.WriteLine("Coref: " + id + " = " + StringUtils.Join(doc.corefChainMap[id], ";"));
					}
				}
			}
			fout.Close();
			System.Console.Out.WriteLine("Total document count: " + docCnt);
			System.Console.Out.WriteLine("Total sentence count: " + sentCnt);
			System.Console.Out.WriteLine("Total token count: " + tokenCnt);
			System.Console.Out.WriteLine(corpusStats);
		}
	}
}
