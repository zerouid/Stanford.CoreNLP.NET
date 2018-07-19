using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Javax.Xml.Parsers;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>A reader for XML format AnCora treebank files.</summary>
	/// <remarks>
	/// A reader for XML format AnCora treebank files.
	/// This reader makes AnCora-specific fixes; see
	/// <see cref="GetPOS(Org.W3c.Dom.IElement)"/>
	/// .
	/// </remarks>
	/// <author>Jon Gauthier</author>
	/// <author>Spence Green (original French XML reader)</author>
	public class SpanishXMLTreeReader : ITreeReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Spanish.SpanishXMLTreeReader));

		private InputStream stream;

		private readonly TreeNormalizer treeNormalizer;

		private readonly ITreeFactory treeFactory;

		private bool simplifiedTagset;

		private bool detailedAnnotations;

		private const string NodeSent = "sentence";

		private const string AttrWord = "wd";

		private const string AttrLemma = "lem";

		private const string AttrFunc = "func";

		private const string AttrNamedEntity = "ne";

		private const string AttrPos = "pos";

		private const string AttrPostype = "postype";

		private const string AttrElliptic = "elliptic";

		private const string AttrPunct = "punct";

		private const string AttrGender = "gen";

		private const string AttrNumber = "num";

		private const string AttrCoordinating = "coord";

		private const string AttrClauseType = "clausetype";

		private INodeList sentences;

		private int sentIdx;

		/// <summary>Read parse trees from a Reader.</summary>
		/// <param name="filename"/>
		/// <param name="in">
		/// The
		/// <c>Reader</c>
		/// </param>
		/// <param name="simplifiedTagset">
		/// If `true`, convert part-of-speech labels to a
		/// simplified version of the EAGLES tagset, where the tags do not
		/// include extensive morphological analysis
		/// </param>
		/// <param name="aggressiveNormalization">
		/// Perform aggressive "normalization"
		/// on the trees read from the provided corpus documents:
		/// split multi-word tokens into their constituent words (and
		/// infer parts of speech of the constituent words).
		/// </param>
		/// <param name="retainNER">
		/// Retain NER information in preterminals (for later
		/// use in `MultiWordPreprocessor) and add NER-specific
		/// parents to single-word NE tokens
		/// </param>
		/// <param name="detailedAnnotations">
		/// Retain detailed tree node annotations. These
		/// annotations on parse tree constituents may be useful for
		/// e.g. training a parser.
		/// </param>
		public SpanishXMLTreeReader(string filename, Reader @in, bool simplifiedTagset, bool aggressiveNormalization, bool retainNER, bool detailedAnnotations)
		{
			// Constituent annotations
			ITreebankLanguagePack tlp = new SpanishTreebankLanguagePack();
			this.simplifiedTagset = simplifiedTagset;
			this.detailedAnnotations = detailedAnnotations;
			stream = new ReaderInputStream(@in, tlp.GetEncoding());
			treeFactory = new LabeledScoredTreeFactory();
			treeNormalizer = new SpanishTreeNormalizer(simplifiedTagset, aggressiveNormalization, retainNER);
			DocumentBuilder parser = XMLUtils.GetXmlParser();
			try
			{
				IDocument xml = parser.Parse(stream);
				IElement root = xml.GetDocumentElement();
				sentences = root.GetElementsByTagName(NodeSent);
				sentIdx = 0;
			}
			catch (SAXException e)
			{
				log.Info("Parse exception while reading " + filename);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public virtual void Close()
		{
			try
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}
			catch (IOException)
			{
			}
		}

		//Silently ignore
		public virtual Tree ReadTree()
		{
			Tree t = null;
			while (t == null && sentences != null && sentIdx < sentences.GetLength())
			{
				int thisSentenceId = sentIdx++;
				INode sentRoot = sentences.Item(thisSentenceId);
				t = GetTreeFromXML(sentRoot);
				if (t != null)
				{
					t = treeNormalizer.NormalizeWholeTree(t, treeFactory);
					if (t.Label() is CoreLabel)
					{
						((CoreLabel)t.Label()).Set(typeof(CoreAnnotations.SentenceIDAnnotation), int.ToString(thisSentenceId));
					}
				}
			}
			return t;
		}

		private static bool IsWordNode(IElement node)
		{
			return node.HasAttribute(AttrWord) && !node.HasChildNodes();
		}

		private static bool IsEllipticNode(IElement node)
		{
			return node.HasAttribute(AttrElliptic);
		}

		/// <summary>Determine the part of speech of the given leaf node.</summary>
		/// <remarks>
		/// Determine the part of speech of the given leaf node.
		/// Use some heuristics to make up for missing part-of-speech labels.
		/// </remarks>
		private string GetPOS(IElement node)
		{
			string pos = node.GetAttribute(AttrPos);
			string namedAttribute = node.GetAttribute(AttrNamedEntity);
			if (pos.StartsWith("np") && pos.Length == 7 && pos[pos.Length - 1] == '0')
			{
				// Some nouns are missing a named entity annotation in the final
				// character of their POS tags, but still have a proper named
				// entity annotation in the `ne` attribute. Fix this:
				char annotation = '0';
				if (namedAttribute.Equals("location"))
				{
					annotation = 'l';
				}
				else
				{
					if (namedAttribute.Equals("person"))
					{
						annotation = 'p';
					}
					else
					{
						if (namedAttribute.Equals("organization"))
						{
							annotation = 'o';
						}
					}
				}
				pos = Sharpen.Runtime.Substring(pos, 0, 6) + annotation;
			}
			else
			{
				if (pos.Equals(string.Empty))
				{
					// Make up for some missing part-of-speech tags
					string word = GetWord(node);
					if (word.Equals("."))
					{
						return "fp";
					}
					if (namedAttribute.Equals("date"))
					{
						return "w";
					}
					else
					{
						if (namedAttribute.Equals("number"))
						{
							return "z0";
						}
					}
					string tagName = node.GetTagName();
					if (tagName.Equals("i"))
					{
						return "i";
					}
					else
					{
						if (tagName.Equals("r"))
						{
							return "rg";
						}
						else
						{
							if (tagName.Equals("z"))
							{
								return "z0";
							}
						}
					}
					// Handle icky issues related to "que"
					string posType = node.GetAttribute(AttrPostype);
					if (tagName.Equals("c") && posType.Equals("subordinating"))
					{
						return "cs";
					}
					else
					{
						if (tagName.Equals("p") && posType.Equals("relative") && Sharpen.Runtime.EqualsIgnoreCase(word, "que"))
						{
							return "pr0cn000";
						}
					}
					if (tagName.Equals("s") && (Sharpen.Runtime.EqualsIgnoreCase(word, "de") || Sharpen.Runtime.EqualsIgnoreCase(word, "del") || Sharpen.Runtime.EqualsIgnoreCase(word, "en")))
					{
						return "sps00";
					}
					else
					{
						if (word.Equals("REGRESA"))
						{
							return "vmip3s0";
						}
					}
					if (simplifiedTagset)
					{
						// If we are using the simplified tagset, we can make some more
						// broad inferences
						if (word.Equals("verme"))
						{
							return "vmn0000";
						}
						else
						{
							if (tagName.Equals("a"))
							{
								return "aq0000";
							}
							else
							{
								if (posType.Equals("proper"))
								{
									return "np00000";
								}
								else
								{
									if (posType.Equals("common"))
									{
										return "nc0s000";
									}
									else
									{
										if (tagName.Equals("d") && posType.Equals("numeral"))
										{
											return "dn0000";
										}
										else
										{
											if (tagName.Equals("d") && (posType.Equals("article") || Sharpen.Runtime.EqualsIgnoreCase(word, "el") || Sharpen.Runtime.EqualsIgnoreCase(word, "la")))
											{
												return "da0000";
											}
											else
											{
												if (tagName.Equals("p") && posType.Equals("relative"))
												{
													return "pr000000";
												}
												else
												{
													if (tagName.Equals("p") && posType.Equals("personal"))
													{
														return "pp000000";
													}
													else
													{
														if (tagName.Equals("p") && posType.Equals("indefinite"))
														{
															return "pi000000";
														}
														else
														{
															if (tagName.Equals("s") && Sharpen.Runtime.EqualsIgnoreCase(word, "como"))
															{
																return "sp000";
															}
															else
															{
																if (tagName.Equals("n"))
																{
																	string gen = node.GetAttribute(AttrGender);
																	string num = node.GetAttribute(AttrNumber);
																	char genCode = gen == null ? '0' : gen[0];
																	char numCode = num == null ? '0' : num[0];
																	return 'n' + genCode + '0' + numCode + "000";
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					if (node.HasAttribute(AttrPunct))
					{
						if (word.Equals("\""))
						{
							return "fe";
						}
						else
						{
							if (word.Equals("'"))
							{
								return "fz";
							}
							else
							{
								if (word.Equals("-"))
								{
									return "fg";
								}
								else
								{
									if (word.Equals("("))
									{
										return "fpa";
									}
									else
									{
										if (word.Equals(")"))
										{
											return "fpt";
										}
									}
								}
							}
						}
						return "fz";
					}
				}
			}
			return pos;
		}

		private static string GetWord(IElement node)
		{
			string word = node.GetAttribute(AttrWord);
			if (word.IsEmpty())
			{
				return SpanishTreeNormalizer.EmptyLeafValue;
			}
			return word.Trim();
		}

		private Tree GetTreeFromXML(INode root)
		{
			IElement eRoot = (IElement)root;
			if (IsWordNode(eRoot))
			{
				return BuildWordNode(eRoot);
			}
			else
			{
				if (IsEllipticNode(eRoot))
				{
					return BuildEllipticNode(eRoot);
				}
				else
				{
					IList<Tree> kids = new List<Tree>();
					for (INode childNode = eRoot.GetFirstChild(); childNode != null; childNode = childNode.GetNextSibling())
					{
						if (childNode.GetNodeType() != NodeConstants.ElementNode)
						{
							continue;
						}
						Tree t = GetTreeFromXML(childNode);
						if (t == null)
						{
							System.Console.Error.Printf("%s: Discarding empty tree (root: %s)%n", this.GetType().FullName, childNode.GetNodeName());
						}
						else
						{
							kids.Add(t);
						}
					}
					return kids.IsEmpty() ? null : BuildConstituentNode(eRoot, kids);
				}
			}
		}

		/// <summary>Build a parse tree node corresponding to the word in the given XML node.</summary>
		private Tree BuildWordNode(INode root)
		{
			IElement eRoot = (IElement)root;
			string posStr = GetPOS(eRoot);
			posStr = treeNormalizer.NormalizeNonterminal(posStr);
			string lemma = eRoot.GetAttribute(AttrLemma);
			string word = GetWord(eRoot);
			string leafStr = treeNormalizer.NormalizeTerminal(word);
			Tree leafNode = treeFactory.NewLeaf(leafStr);
			if (leafNode.Label() is IHasWord)
			{
				((IHasWord)leafNode.Label()).SetWord(leafStr);
			}
			if (leafNode.Label() is IHasLemma && lemma != null)
			{
				((IHasLemma)leafNode.Label()).SetLemma(lemma);
			}
			IList<Tree> kids = new List<Tree>();
			kids.Add(leafNode);
			Tree t = treeFactory.NewTreeNode(posStr, kids);
			if (t.Label() is IHasTag)
			{
				((IHasTag)t.Label()).SetTag(posStr);
			}
			return t;
		}

		/// <summary>Build a parse tree node corresponding to an elliptic node in the parse XML.</summary>
		private Tree BuildEllipticNode(INode root)
		{
			IElement eRoot = (IElement)root;
			string constituentStr = eRoot.GetNodeName();
			IList<Tree> kids = new List<Tree>();
			Tree leafNode = treeFactory.NewLeaf(SpanishTreeNormalizer.EmptyLeafValue);
			if (leafNode.Label() is IHasWord)
			{
				((IHasWord)leafNode.Label()).SetWord(SpanishTreeNormalizer.EmptyLeafValue);
			}
			kids.Add(leafNode);
			Tree t = treeFactory.NewTreeNode(constituentStr, kids);
			return t;
		}

		/// <summary>Build a parse tree node corresponding to a constituent.</summary>
		/// <param name="root">Node describing the constituent</param>
		/// <param name="children">Collected child nodes, already parsed</param>
		private Tree BuildConstituentNode(INode root, IList<Tree> children)
		{
			IElement eRoot = (IElement)root;
			string label = eRoot.GetNodeName().Trim();
			if (detailedAnnotations)
			{
				if (eRoot.GetAttribute(AttrCoordinating).Equals("yes"))
				{
					label += "-coord";
				}
				else
				{
					if (eRoot.HasAttribute(AttrClauseType))
					{
						label += '-' + eRoot.GetAttribute(AttrClauseType);
					}
				}
			}
			return treeFactory.NewTreeNode(treeNormalizer.NormalizeNonterminal(label), children);
		}

		/// <summary>
		/// Determine if the given tree contains a leaf which matches the
		/// part-of-speech and lexical criteria.
		/// </summary>
		/// <param name="pos">
		/// Regular expression to match part of speech (may be null,
		/// in which case any POS is allowed)
		/// </param>
		/// <param name="pos">
		/// Regular expression to match word (may be null, in which
		/// case any word is allowed)
		/// </param>
		private static bool ShouldPrintTree(Tree tree, Pattern pos, Pattern word)
		{
			foreach (Tree t in tree)
			{
				if (t.IsPreTerminal())
				{
					CoreLabel label = (CoreLabel)t.Label();
					string tpos = label.Value();
					Tree wordNode = t.FirstChild();
					CoreLabel wordLabel = (CoreLabel)wordNode.Label();
					string tword = wordLabel.Value();
					if ((pos == null || pos.Matcher(tpos).Find()) && (word == null || word.Matcher(tword).Find()))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static string ToString(Tree tree, bool plainPrint)
		{
			if (!plainPrint)
			{
				return tree.ToString();
			}
			StringBuilder sb = new StringBuilder();
			IList<Tree> leaves = tree.GetLeaves();
			foreach (Tree leaf in leaves)
			{
				sb.Append(leaf.Label().Value()).Append(' ');
			}
			return sb.ToString();
		}

		/// <summary>
		/// Read trees from the given file and output their processed forms to
		/// standard output.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Process(File file, ITreeReader tr, Pattern posPattern, Pattern wordPattern, bool plainPrint)
		{
			Tree t;
			int numTrees = 0;
			int numTreesRetained = 0;
			string canonicalFileName = Sharpen.Runtime.Substring(file.GetName(), 0, file.GetName().LastIndexOf('.'));
			while ((t = tr.ReadTree()) != null)
			{
				numTrees++;
				if (!ShouldPrintTree(t, posPattern, wordPattern))
				{
					continue;
				}
				numTreesRetained++;
				string ftbID = ((CoreLabel)t.Label()).Get(typeof(CoreAnnotations.SentenceIDAnnotation));
				string output = ToString(t, plainPrint);
				System.Console.Out.Printf("%s-%s\t%s%n", canonicalFileName, ftbID, output);
			}
			System.Console.Error.Printf("%s: %d trees, %d matched and printed%n", file.GetName(), numTrees, numTreesRetained);
		}

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTIONS] file(s)%n%n", typeof(Edu.Stanford.Nlp.Trees.International.Spanish.SpanishXMLTreeReader).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("   -help: Print this message").Append(nl);
			sb.Append("   -ner: Add NER-specific information to trees").Append(nl);
			sb.Append("   -detailedAnnotations: Retain detailed annotations on tree constituents (useful for making treebank for parser, etc.)").Append(nl);
			sb.Append("   -plain: Output corpus in plaintext rather than as trees").Append(nl);
			sb.Append("   -searchPos posRegex: Only print sentences which contain a token whose part of speech matches the given regular expression").Append(nl);
			sb.Append("   -searchWord wordRegex: Only print sentences which contain a token which matches the given regular expression").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> ArgOptionDefs()
		{
			IDictionary<string, int> argOptionDefs = Generics.NewHashMap();
			argOptionDefs["help"] = 0;
			argOptionDefs["ner"] = 0;
			argOptionDefs["detailedAnnotations"] = 0;
			argOptionDefs["plain"] = 0;
			argOptionDefs["searchPos"] = 1;
			argOptionDefs["searchWord"] = 1;
			return argOptionDefs;
		}

		public static void Main(string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args, ArgOptionDefs());
			if (args.Length < 1 || options.Contains("help"))
			{
				log.Info(Usage());
				return;
			}
			Pattern posPattern = options.Contains("searchPos") ? Pattern.Compile(options.GetProperty("searchPos")) : null;
			Pattern wordPattern = options.Contains("searchWord") ? Pattern.Compile(options.GetProperty("searchWord")) : null;
			bool plainPrint = PropertiesUtils.GetBool(options, "plain", false);
			bool ner = PropertiesUtils.GetBool(options, "ner", false);
			bool detailedAnnotations = PropertiesUtils.GetBool(options, "detailedAnnotations", false);
			string[] remainingArgs = options.GetProperty(string.Empty).Split(" ");
			IList<File> fileList = new List<File>();
			foreach (string remainingArg in remainingArgs)
			{
				fileList.Add(new File(remainingArg));
			}
			SpanishXMLTreeReaderFactory trf = new SpanishXMLTreeReaderFactory(true, true, ner, detailedAnnotations);
			IExecutorService pool = Executors.NewFixedThreadPool(Runtime.GetRuntime().AvailableProcessors());
			foreach (File file in fileList)
			{
				pool.Execute(null);
			}
			pool.Shutdown();
			try
			{
				pool.AwaitTermination(long.MaxValue, TimeUnit.Nanoseconds);
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
		}
	}
}
