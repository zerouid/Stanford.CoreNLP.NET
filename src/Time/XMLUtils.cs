using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;






using Org.W3c.Dom;


namespace Edu.Stanford.Nlp.Time
{
	/// <summary>XML Utility functions for use with dealing with Timex expressions</summary>
	/// <author>Angel Chang</author>
	public class XMLUtils
	{
		private static readonly IDocument document = CreateDocument();

		private static readonly TransformerFactory tFactory = TransformerFactory.NewInstance();

		private XMLUtils()
		{
		}

		// todo: revert: According to the docs, neither TransformerFactory nor DocumentBuilderFactory is guaranteed threadsafe.
		// todo: A good application might make one of these per thread, but maybe easier just to revert to creating one each time, sigh.
		// static class
		public static string DocumentToString(IDocument document)
		{
			StringOutputStream s = new StringOutputStream();
			PrintNode(s, document, true, true);
			return s.ToString();
		}

		public static string NodeToString(INode node, bool prettyPrint)
		{
			StringOutputStream s = new StringOutputStream();
			PrintNode(s, node, prettyPrint, false);
			return s.ToString();
		}

		public static void PrintNode(OutputStream @out, INode node, bool prettyPrint, bool includeXmlDeclaration)
		{
			try
			{
				Transformer serializer = tFactory.NewTransformer();
				if (prettyPrint)
				{
					//Setup indenting to "pretty print"
					serializer.SetOutputProperty(OutputKeys.Indent, "yes");
					serializer.SetOutputProperty("{http://xml.apache.org/xslt}indent-amount", "2");
				}
				if (!includeXmlDeclaration)
				{
					serializer.SetOutputProperty(OutputKeys.OmitXmlDeclaration, "yes");
				}
				DOMSource xmlSource = new DOMSource(node);
				StreamResult outputTarget = new StreamResult(@out);
				serializer.Transform(xmlSource, outputTarget);
			}
			catch (TransformerException e)
			{
				throw new Exception(e);
			}
		}

		public static IDocument CreateDocument()
		{
			try
			{
				DocumentBuilderFactory dbFactory = DocumentBuilderFactory.NewInstance();
				DocumentBuilder docBuilder = dbFactory.NewDocumentBuilder();
				IDocument doc = docBuilder.NewDocument();
				return doc;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static IText CreateTextNode(string text)
		{
			return document.CreateTextNode(text);
		}

		public static IElement CreateElement(string tag)
		{
			return document.CreateElement(tag);
		}

		public static IElement ParseElement(string xml)
		{
			try
			{
				DocumentBuilderFactory dbFactory = DocumentBuilderFactory.NewInstance();
				DocumentBuilder docBuilder = dbFactory.NewDocumentBuilder();
				IDocument doc = docBuilder.Parse(new ByteArrayInputStream(Sharpen.Runtime.GetBytesForString(xml)));
				return doc.GetDocumentElement();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		// Like element.getAttribute except returns null if attribute not present
		public static string GetAttribute(IElement element, string name)
		{
			IAttr attr = element.GetAttributeNode(name);
			return (attr != null) ? attr.GetValue() : null;
		}

		public static void RemoveChildren(INode e)
		{
			INodeList list = e.GetChildNodes();
			for (int i = 0; i < list.GetLength(); i++)
			{
				INode n = list.Item(i);
				e.RemoveChild(n);
			}
		}

		private static void GetMatchingNodes(INode node, Pattern[] nodePath, int cur, IList<INode> res)
		{
			if (cur < 0 || cur >= nodePath.Length)
			{
				return;
			}
			bool last = (cur == nodePath.Length - 1);
			Pattern pattern = nodePath[cur];
			INodeList children = node.GetChildNodes();
			for (int i = 0; i < children.GetLength(); i++)
			{
				INode c = children.Item(i);
				if (pattern.Matcher(c.GetNodeName()).Matches())
				{
					if (last)
					{
						res.Add(c);
					}
					else
					{
						GetMatchingNodes(c, nodePath, cur + 1, res);
					}
				}
			}
		}

		public static IList<INode> GetNodes(INode node, params Pattern[] nodePath)
		{
			IList<INode> res = new List<INode>();
			GetMatchingNodes(node, nodePath, 0, res);
			return res;
		}

		public static string GetNodeText(INode node, params Pattern[] nodePath)
		{
			IList<INode> nodes = GetNodes(node, nodePath);
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0].GetTextContent();
			}
			else
			{
				return null;
			}
		}

		public static INode GetNode(INode node, params Pattern[] nodePath)
		{
			IList<INode> nodes = GetNodes(node, nodePath);
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0];
			}
			else
			{
				return null;
			}
		}

		private static void GetMatchingNodes(INode node, string[] nodePath, int cur, IList<INode> res)
		{
			if (cur < 0 || cur >= nodePath.Length)
			{
				return;
			}
			bool last = (cur == nodePath.Length - 1);
			string name = nodePath[cur];
			if (node.HasChildNodes())
			{
				INodeList children = node.GetChildNodes();
				for (int i = 0; i < children.GetLength(); i++)
				{
					INode c = children.Item(i);
					if (name.Equals(c.GetNodeName()))
					{
						if (last)
						{
							res.Add(c);
						}
						else
						{
							GetMatchingNodes(c, nodePath, cur + 1, res);
						}
					}
				}
			}
		}

		public static IList<INode> GetNodes(INode node, params string[] nodePath)
		{
			IList<INode> res = new List<INode>();
			GetMatchingNodes(node, nodePath, 0, res);
			return res;
		}

		public static IList<string> GetNodeTexts(INode node, params string[] nodePath)
		{
			IList<INode> nodes = GetNodes(node, nodePath);
			if (nodes != null)
			{
				IList<string> strs = new List<string>(nodes.Count);
				foreach (INode n in nodes)
				{
					strs.Add(n.GetTextContent());
				}
				return strs;
			}
			else
			{
				return null;
			}
		}

		public static string GetNodeText(INode node, params string[] nodePath)
		{
			IList<INode> nodes = GetNodes(node, nodePath);
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0].GetTextContent();
			}
			else
			{
				return null;
			}
		}

		public static string GetAttributeValue(INode node, string name)
		{
			INode attr = GetAttribute(node, name);
			return (attr != null) ? attr.GetNodeValue() : null;
		}

		public static INode GetAttribute(INode node, string name)
		{
			return node.GetAttributes().GetNamedItem(name);
		}

		public static INode GetNode(INode node, params string[] nodePath)
		{
			IList<INode> nodes = GetNodes(node, nodePath);
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0];
			}
			else
			{
				return null;
			}
		}
	}
}
