using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Javax.Xml.Parsers;
using Org.W3c.Dom;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>A reader for XML format French Treebank files.</summary>
	/// <remarks>
	/// A reader for XML format French Treebank files. Note that the raw
	/// XML files are in ISO-8859-1 format, so they must be converted to UTF-8.
	/// <p>
	/// Handles multiword expressions (MWEs).
	/// <p>
	/// One difference worth documenting between this and the
	/// PennTreeReader is that this does not unescape \* and \/ the way the
	/// PennTreeReader does.  The French Treebank we are using does not
	/// use those escapings.
	/// </remarks>
	/// <author>Spence Green</author>
	public class FrenchXMLTreeReader : ITreeReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.French.FrenchXMLTreeReader));

		private InputStream stream;

		private readonly TreeNormalizer treeNormalizer;

		private readonly ITreeFactory treeFactory;

		private const string NodeSent = "SENT";

		private const string NodeWord = "w";

		private const string AttrNumber = "nb";

		private const string AttrPos = "cat";

		private const string AttrPosMwe = "catint";

		private const string AttrLemma = "lemma";

		private const string AttrMorph = "mph";

		private const string AttrEe = "ee";

		private const string AttrSubcat = "subcat";

		private const string MwePhrasal = "MW";

		public const string EmptyLeaf = "-NONE-";

		public const string MissingPhrasal = "DUMMYP";

		public const string MissingPos = "DUMMY";

		private INodeList sentences;

		private int sentIdx;

		/// <summary>Read parse trees from a Reader.</summary>
		/// <param name="in">The <code>Reader</code></param>
		public FrenchXMLTreeReader(Reader @in, bool ccTagset)
			: this(@in, new LabeledScoredTreeFactory(), new FrenchTreeNormalizer(ccTagset))
		{
		}

		/// <summary>Read parse trees from a Reader.</summary>
		/// <param name="in">Reader</param>
		/// <param name="tf">TreeFactory -- factory to create some kind of Tree</param>
		/// <param name="tn">the method of normalizing trees</param>
		public FrenchXMLTreeReader(Reader @in, ITreeFactory tf, TreeNormalizer tn)
		{
			// Prefix for MWE nodes
			ITreebankLanguagePack tlp = new FrenchTreebankLanguagePack();
			stream = new ReaderInputStream(@in, tlp.GetEncoding());
			treeFactory = tf;
			treeNormalizer = tn;
			DocumentBuilder parser = XMLUtils.GetXmlParser();
			try
			{
				IDocument xml = parser.Parse(stream);
				IElement root = xml.GetDocumentElement();
				sentences = root.GetElementsByTagName(NodeSent);
				sentIdx = 0;
			}
			catch (Exception e)
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
				INode sentRoot = sentences.Item(sentIdx++);
				t = GetTreeFromXML(sentRoot);
				if (t != null)
				{
					t = treeNormalizer.NormalizeWholeTree(t, treeFactory);
					if (t.Label() is CoreLabel)
					{
						string ftbId = ((IElement)sentRoot).GetAttribute(AttrNumber);
						((CoreLabel)t.Label()).Set(typeof(CoreAnnotations.SentenceIDAnnotation), ftbId);
					}
				}
			}
			return t;
		}

		//wsg2010: Sometimes the cat attribute is not present, in which case the POS
		//is in the attribute catint, which indicates a part of a compound / MWE
		private string GetPOS(IElement node)
		{
			string attrPOS = node.HasAttribute(AttrPos) ? node.GetAttribute(AttrPos).Trim() : string.Empty;
			string attrPOSMWE = node.HasAttribute(AttrPosMwe) ? node.GetAttribute(AttrPosMwe).Trim() : string.Empty;
			if (attrPOS != string.Empty)
			{
				return attrPOS;
			}
			else
			{
				if (attrPOSMWE != string.Empty)
				{
					return attrPOSMWE;
				}
			}
			return MissingPos;
		}

		/// <summary>Extract the lemma attribute.</summary>
		/// <param name="node"/>
		private IList<string> GetLemma(IElement node)
		{
			string lemma = node.GetAttribute(AttrLemma);
			if (lemma == null || lemma.Equals(string.Empty))
			{
				return null;
			}
			return GetWordString(lemma);
		}

		/// <summary>Extract the morphological analysis from a leaf.</summary>
		/// <remarks>
		/// Extract the morphological analysis from a leaf. Note that the "ee" field
		/// contains the relativizer flag.
		/// </remarks>
		/// <param name="node"/>
		private string GetMorph(IElement node)
		{
			string ee = node.GetAttribute(AttrEe);
			return ee == null ? string.Empty : ee;
		}

		/// <summary>Get the POS subcategory.</summary>
		/// <param name="node"/>
		/// <returns/>
		private string GetSubcat(IElement node)
		{
			string subcat = node.GetAttribute(AttrSubcat);
			return subcat == null ? string.Empty : subcat;
		}

		/// <summary>Terminals may consist of one or more whitespace-delimited tokens.</summary>
		/// <remarks>
		/// Terminals may consist of one or more whitespace-delimited tokens.
		/// <p>
		/// wsg2010: Marie recommends replacing empty terminals with -NONE- instead of using the lemma
		/// (these are usually the determiner)
		/// </remarks>
		/// <param name="text"/>
		private IList<string> GetWordString(string text)
		{
			IList<string> toks = new List<string>();
			if (text == null || text.Equals(string.Empty))
			{
				toks.Add(EmptyLeaf);
			}
			else
			{
				//Strip spurious parens
				if (text.Length > 1)
				{
					text = text.ReplaceAll("[\\(\\)]", string.Empty);
				}
				//Check for numbers and punctuation
				string noWhitespaceStr = text.ReplaceAll("\\s+", string.Empty);
				if (noWhitespaceStr.Matches("\\d+") || noWhitespaceStr.Matches("\\p{Punct}+"))
				{
					toks.Add(noWhitespaceStr);
				}
				else
				{
					toks = Arrays.AsList(text.Split("\\s+"));
				}
			}
			if (toks.Count == 0)
			{
				throw new Exception(this.GetType().FullName + ": Zero length token list for: " + text);
			}
			return toks;
		}

		private Tree GetTreeFromXML(INode root)
		{
			IElement eRoot = (IElement)root;
			if (eRoot.GetNodeName().Equals(NodeWord) && eRoot.GetElementsByTagName(NodeWord).GetLength() == 0)
			{
				string posStr = GetPOS(eRoot);
				posStr = treeNormalizer.NormalizeNonterminal(posStr);
				IList<string> lemmas = GetLemma(eRoot);
				string morph = GetMorph(eRoot);
				IList<string> leafToks = GetWordString(eRoot.GetTextContent().Trim());
				string subcat = GetSubcat(eRoot);
				if (lemmas != null && lemmas.Count != leafToks.Count)
				{
					// If this happens (and it does for a few poorly editted trees)
					// we assume something has gone wrong and ignore the lemmas.
					log.Info("Lemmas don't match tokens, ignoring lemmas: " + "lemmas " + lemmas + ", tokens " + leafToks);
					lemmas = null;
				}
				//Terminals can have multiple tokens (MWEs). Make these into a
				//flat structure for now.
				Tree t = null;
				IList<Tree> kids = new List<Tree>();
				if (leafToks.Count > 1)
				{
					for (int i = 0; i < leafToks.Count; ++i)
					{
						string tok = leafToks[i];
						string s = treeNormalizer.NormalizeTerminal(tok);
						IList<Tree> leafList = new List<Tree>();
						Tree leafNode = treeFactory.NewLeaf(s);
						if (leafNode.Label() is IHasWord)
						{
							((IHasWord)leafNode.Label()).SetWord(s);
						}
						if (leafNode.Label() is CoreLabel && lemmas != null)
						{
							((CoreLabel)leafNode.Label()).SetLemma(lemmas[i]);
						}
						if (leafNode.Label() is IHasContext)
						{
							((IHasContext)leafNode.Label()).SetOriginalText(morph);
						}
						if (leafNode.Label() is IHasCategory)
						{
							((IHasCategory)leafNode.Label()).SetCategory(subcat);
						}
						leafList.Add(leafNode);
						Tree posNode = treeFactory.NewTreeNode(MissingPos, leafList);
						if (posNode.Label() is IHasTag)
						{
							((IHasTag)posNode.Label()).SetTag(MissingPos);
						}
						kids.Add(posNode);
					}
					t = treeFactory.NewTreeNode(MissingPhrasal, kids);
				}
				else
				{
					string leafStr = treeNormalizer.NormalizeTerminal(leafToks[0]);
					Tree leafNode = treeFactory.NewLeaf(leafStr);
					if (leafNode.Label() is IHasWord)
					{
						((IHasWord)leafNode.Label()).SetWord(leafStr);
					}
					if (leafNode.Label() is CoreLabel && lemmas != null)
					{
						((CoreLabel)leafNode.Label()).SetLemma(lemmas[0]);
					}
					if (leafNode.Label() is IHasContext)
					{
						((IHasContext)leafNode.Label()).SetOriginalText(morph);
					}
					if (leafNode.Label() is IHasCategory)
					{
						((IHasCategory)leafNode.Label()).SetCategory(subcat);
					}
					kids.Add(leafNode);
					t = treeFactory.NewTreeNode(posStr, kids);
					if (t.Label() is IHasTag)
					{
						((IHasTag)t.Label()).SetTag(posStr);
					}
				}
				return t;
			}
			IList<Tree> kids_1 = new List<Tree>();
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
					kids_1.Add(t);
				}
			}
			// MWEs have a label with a
			string rootLabel = eRoot.GetNodeName().Trim();
			bool isMWE = rootLabel.Equals("w") && eRoot.HasAttribute(AttrPos);
			if (isMWE)
			{
				rootLabel = eRoot.GetAttribute(AttrPos).Trim();
			}
			Tree t_1 = (kids_1.Count == 0) ? null : treeFactory.NewTreeNode(treeNormalizer.NormalizeNonterminal(rootLabel), kids_1);
			if (t_1 != null && isMWE)
			{
				t_1 = PostProcessMWE(t_1);
			}
			return t_1;
		}

		private Tree PostProcessMWE(Tree t)
		{
			string tYield = SentenceUtils.ListToString(t.Yield()).ReplaceAll("\\s+", string.Empty);
			if (tYield.Matches("[\\d\\p{Punct}]*"))
			{
				IList<Tree> kids = new List<Tree>();
				kids.Add(treeFactory.NewLeaf(tYield));
				t = treeFactory.NewTreeNode(t.Value(), kids);
			}
			else
			{
				t.SetValue(MwePhrasal + t.Value());
			}
			return t;
		}

		/// <summary>For debugging.</summary>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				System.Console.Error.Printf("Usage: java %s tree_file(s)%n%n", typeof(Edu.Stanford.Nlp.Trees.International.French.FrenchXMLTreeReader).FullName);
				System.Environment.Exit(-1);
			}
			IList<File> fileList = new List<File>();
			foreach (string arg in args)
			{
				fileList.Add(new File(arg));
			}
			ITreeReaderFactory trf = new FrenchXMLTreeReaderFactory(false);
			int totalTrees = 0;
			ICollection<string> morphAnalyses = Generics.NewHashSet();
			try
			{
				foreach (File file in fileList)
				{
					ITreeReader tr = trf.NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(file), "UTF-8")));
					Tree t;
					int numTrees;
					string canonicalFileName = Sharpen.Runtime.Substring(file.GetName(), 0, file.GetName().LastIndexOf('.'));
					for (numTrees = 0; (t = tr.ReadTree()) != null; numTrees++)
					{
						string ftbID = ((CoreLabel)t.Label()).Get(typeof(CoreAnnotations.SentenceIDAnnotation));
						System.Console.Out.Printf("%s-%s\t%s%n", canonicalFileName, ftbID, t.ToString());
						IList<ILabel> leaves = t.Yield();
						foreach (ILabel label in leaves)
						{
							if (label is CoreLabel)
							{
								morphAnalyses.Add(((CoreLabel)label).OriginalText());
							}
						}
					}
					tr.Close();
					System.Console.Error.Printf("%s: %d trees%n", file.GetName(), numTrees);
					totalTrees += numTrees;
				}
				//wsg2011: Print out the observed morphological analyses
				//      for(String analysis : morphAnalyses)
				//        log.info(analysis);
				System.Console.Error.Printf("%nRead %d trees%n", totalTrees);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
