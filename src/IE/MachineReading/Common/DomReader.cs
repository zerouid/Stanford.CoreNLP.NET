using System;
using System.Collections.Generic;
using System.IO;
using Java.IO;
using Javax.Xml.Parsers;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Common
{
	/// <summary>Generic DOM reader for an XML file</summary>
	public class DomReader
	{
		/// <summary>Searches (recursively) for the first child that has the given name</summary>
		protected internal static INode GetChildByName(INode node, string name)
		{
			INodeList children = node.GetChildNodes();
			// this node matches
			if (node.GetNodeName().Equals(name))
			{
				return node;
			}
			// search children
			for (int i = 0; i < children.GetLength(); i++)
			{
				INode found = GetChildByName(children.Item(i), name);
				if (found != null)
				{
					return found;
				}
			}
			// failed
			return null;
		}

		/// <summary>Searches for all immediate children with the given name</summary>
		protected internal static IList<INode> GetChildrenByName(INode node, string name)
		{
			IList<INode> matches = new List<INode>();
			INodeList children = node.GetChildNodes();
			// search children
			for (int i = 0; i < children.GetLength(); i++)
			{
				INode child = children.Item(i);
				if (child.GetNodeName().Equals(name))
				{
					matches.Add(child);
				}
			}
			return matches;
		}

		/// <summary>Searches for children that have the given attribute</summary>
		protected internal static INode GetChildByAttribute(INode node, string attributeName, string attributeValue)
		{
			INodeList children = node.GetChildNodes();
			INamedNodeMap attribs = node.GetAttributes();
			INode attribute = null;
			// this node matches
			if (attribs != null && (attribute = attribs.GetNamedItem(attributeName)) != null && attribute.GetNodeValue().Equals(attributeValue))
			{
				return node;
			}
			// search children
			for (int i = 0; i < children.GetLength(); i++)
			{
				INode found = GetChildByAttribute(children.Item(i), attributeName, attributeValue);
				if (found != null)
				{
					return found;
				}
			}
			// failed
			return null;
		}

		/// <summary>Searches for children that have the given name and attribute</summary>
		protected internal static INode GetChildByNameAndAttribute(INode node, string name, string attributeName, string attributeValue)
		{
			INodeList children = node.GetChildNodes();
			INamedNodeMap attribs = node.GetAttributes();
			INode attribute = null;
			// this node matches
			if (node.GetNodeName().Equals(name) && attribs != null && (attribute = attribs.GetNamedItem(attributeName)) != null && attribute.GetNodeValue().Equals(attributeValue))
			{
				return node;
			}
			// search children
			for (int i = 0; i < children.GetLength(); i++)
			{
				INode found = GetChildByAttribute(children.Item(i), attributeName, attributeValue);
				if (found != null)
				{
					return found;
				}
			}
			// failed
			return null;
		}

		/// <summary>Fetches the value of a given attribute</summary>
		public static string GetAttributeValue(INode node, string attributeName)
		{
			try
			{
				return node.GetAttributes().GetNamedItem(attributeName).GetNodeValue();
			}
			catch (Exception)
			{
			}
			return null;
		}

		/// <summary>Constructs one Document from an XML file</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static IDocument ReadDocument(File f)
		{
			IDocument document = null;
			DocumentBuilderFactory factory = DocumentBuilderFactory.NewInstance();
			// factory.setValidating(true);
			// factory.setNamespaceAware(true);
			try
			{
				DocumentBuilder builder = factory.NewDocumentBuilder();
				document = builder.Parse(f);
			}
			catch (SAXException sxe)
			{
				// displayDocument(document);
				// Error generated during parsing)
				Exception x = sxe;
				if (sxe.GetException() != null)
				{
					x = sxe.GetException();
				}
				Sharpen.Runtime.PrintStackTrace(x);
				throw;
			}
			catch (ParserConfigurationException pce)
			{
				// Parser with specified options can't be built
				Sharpen.Runtime.PrintStackTrace(pce);
				throw;
			}
			catch (IOException ioe)
			{
				// I/O error
				Sharpen.Runtime.PrintStackTrace(ioe);
				throw;
			}
			return document;
		}
		// readDocument
	}
}
