using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Javax.Xml.Parsers;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class LatticeXMLReader : IEnumerable<Lattice>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.LatticeXMLReader));

		public const string Sentence = "sentence";

		public const string Node = "node";

		public const string NodeId = "id";

		public const string Edge = "edge";

		public const string FromNode = "from";

		public const string ToNode = "to";

		public const string Segment = "label";

		public const string Weight = "wt";

		public const string EAttrNode = "attribute";

		public const string EAttr = "attr";

		public const string EAttrVal = "value";

		private const int NodeOffset = 100;

		private IList<Lattice> lattices;

		public LatticeXMLReader()
		{
			//	private static final String ROOT = "sentences";
			// This *must* be the same as the offset in lattice-gen.py
			lattices = new List<Lattice>();
		}

		public virtual IEnumerator<Lattice> GetEnumerator()
		{
			return lattices.GetEnumerator();
		}

		public virtual int GetNumLattices()
		{
			return lattices.Count;
		}

		private bool Load(ObjectInputStream os)
		{
			try
			{
				lattices = (IList<Lattice>)os.ReadObject();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return false;
			}
			catch (TypeLoadException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return false;
			}
			return true;
		}

		public virtual bool Load(InputStream stream, bool isObject)
		{
			if (isObject)
			{
				ObjectInputStream os = (ObjectInputStream)stream;
				return Load(os);
			}
			else
			{
				return Load(stream);
			}
		}

		private bool Load(InputStream stream)
		{
			DocumentBuilder parser = XMLUtils.GetXmlParser();
			if (parser == null)
			{
				return false;
			}
			try
			{
				IDocument xmlDocument = parser.Parse(stream);
				IElement root = xmlDocument.GetDocumentElement();
				INodeList sentences = root.GetElementsByTagName(Sentence);
				for (int i = 0; i < sentences.GetLength(); i++)
				{
					IElement sentence = (IElement)sentences.Item(i);
					Lattice lattice = new Lattice();
					//Create the node map
					ISortedSet<int> nodes = new TreeSet<int>();
					INodeList xmlNodes = sentence.GetElementsByTagName(Node);
					for (int nodeIdx = 0; nodeIdx < xmlNodes.GetLength(); nodeIdx++)
					{
						IElement xmlNode = (IElement)xmlNodes.Item(nodeIdx);
						int nodeName = System.Convert.ToInt32(xmlNode.GetAttribute(NodeId));
						nodes.Add(nodeName);
					}
					IDictionary<int, int> nodeMap = Generics.NewHashMap();
					int realNodeIdx = 0;
					int lastBoundaryNode = -1;
					foreach (int nodeName_1 in nodes)
					{
						if (lastBoundaryNode == -1)
						{
							System.Diagnostics.Debug.Assert(nodeName_1 % NodeOffset == 0);
							lastBoundaryNode = realNodeIdx;
						}
						else
						{
							if (nodeName_1 % NodeOffset == 0)
							{
								ParserConstraint c = new ParserConstraint(lastBoundaryNode, realNodeIdx, ".*");
								lattice.AddConstraint(c);
							}
						}
						nodeMap[nodeName_1] = realNodeIdx;
						realNodeIdx++;
					}
					//Read the edges
					INodeList xmlEdges = sentence.GetElementsByTagName(Edge);
					for (int edgeIdx = 0; edgeIdx < xmlEdges.GetLength(); edgeIdx++)
					{
						IElement xmlEdge = (IElement)xmlEdges.Item(edgeIdx);
						string segment = xmlEdge.GetAttribute(Segment);
						double weight = double.ParseDouble(xmlEdge.GetAttribute(Weight));
						//Input weights should be log scale
						int from = System.Convert.ToInt32(xmlEdge.GetAttribute(FromNode));
						int normFrom = nodeMap[from];
						int to = System.Convert.ToInt32(xmlEdge.GetAttribute(ToNode));
						int normTo = nodeMap[to];
						LatticeEdge e = new LatticeEdge(segment, weight, normFrom, normTo);
						// Set attributes below here
						INodeList xmlAttrs = xmlEdge.GetElementsByTagName(EAttrNode);
						for (int attrIdx = 0; attrIdx < xmlAttrs.GetLength(); attrIdx++)
						{
							IElement xmlAttr = (IElement)xmlAttrs.Item(attrIdx);
							string key = xmlAttr.GetAttribute(EAttr);
							string value = xmlAttr.GetAttribute(EAttrVal);
							e.SetAttr(key, value);
						}
						lattice.AddEdge(e);
					}
					//Configure for parsing in ExhaustivePCFG parser
					lattice.AddBoundary();
					lattices.Add(lattice);
				}
			}
			catch (IOException e)
			{
				System.Console.Error.Printf("%s: Error reading XML from input stream.%n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
				return false;
			}
			catch (SAXException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return false;
			}
			return true;
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.LatticeXMLReader reader = new Edu.Stanford.Nlp.Parser.Lexparser.LatticeXMLReader();
			try
			{
				Runtime.SetIn(new FileInputStream(args[0]));
			}
			catch (FileNotFoundException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			reader.Load(Runtime.@in);
			int numLattices = 0;
			foreach (Lattice lattice in reader)
			{
				System.Console.Out.WriteLine(lattice.ToString());
				numLattices++;
			}
			System.Console.Out.Printf("\nLoaded %d lattices\n", numLattices);
		}
	}
}
