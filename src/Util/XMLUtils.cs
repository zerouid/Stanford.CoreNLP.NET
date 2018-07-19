using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Javax.Xml;
using Javax.Xml.Parsers;
using Javax.Xml.Validation;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Provides some utilities for dealing with XML files, both by properly
	/// parsing them and by using the methods of a desperate Perl hacker.
	/// </summary>
	/// <author>Teg Grenager</author>
	/// <author>Grace Muzny</author>
	public class XMLUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.XMLUtils));

		private XMLUtils()
		{
		}

		// only static methods
		/// <summary>Returns the text content of all nodes in the given file with the given tag.</summary>
		/// <returns>List of String text contents of tags.</returns>
		public static IList<string> GetTextContentFromTagsFromFile(File f, string tag)
		{
			IList<string> sents = Generics.NewArrayList();
			try
			{
				sents = GetTextContentFromTagsFromFileSAXException(f, tag);
			}
			catch (SAXException e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>Returns the text content of all nodes in the given file with the given tag.</summary>
		/// <remarks>
		/// Returns the text content of all nodes in the given file with the given tag.
		/// If the text contents contains embedded tags, strips the embedded tags out
		/// of the returned text. E.g.,
		/// <c>
		/// &lt;s&gt;This is a &lt;s&gt;sentence&lt;/s&gt; with embedded tags
		/// &lt;/s&gt;
		/// </c>
		/// would return the list containing ["This is a sentence with embedded
		/// tags", "sentence"].
		/// </remarks>
		/// <exception cref="Org.Xml.Sax.SAXException">if tag doesn't exist in the file.</exception>
		/// <returns>List of String text contents of tags.</returns>
		private static IList<string> GetTextContentFromTagsFromFileSAXException(File f, string tag)
		{
			IList<string> sents = Generics.NewArrayList();
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				DocumentBuilder db = dbf.NewDocumentBuilder();
				IDocument doc = db.Parse(f);
				doc.GetDocumentElement().Normalize();
				INodeList nodeList = doc.GetElementsByTagName(tag);
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					// Get element
					IElement element = (IElement)nodeList.Item(i);
					string raw = element.GetTextContent();
					StringBuilder builtUp = new StringBuilder();
					bool inTag = false;
					for (int j = 0; j < raw.Length; j++)
					{
						if (raw[j] == '<')
						{
							inTag = true;
						}
						if (!inTag)
						{
							builtUp.Append(raw[j]);
						}
						if (raw[j] == '>')
						{
							inTag = false;
						}
					}
					sents.Add(builtUp.ToString());
				}
			}
			catch (Exception e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>Returns the text content of all nodes in the given file with the given tag.</summary>
		/// <returns>List of String text contents of tags.</returns>
		public static IList<IElement> GetTagElementsFromFile(File f, string tag)
		{
			IList<IElement> sents = Generics.NewArrayList();
			try
			{
				sents = GetTagElementsFromFileSAXException(f, tag);
			}
			catch (SAXException e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>Returns the text content of all nodes in the given file with the given tag.</summary>
		/// <remarks>
		/// Returns the text content of all nodes in the given file with the given tag.
		/// If the text contents contains embedded tags, strips the embedded tags out
		/// of the returned text. E.g.,
		/// <c>
		/// &lt;s&gt;This is a &lt;s&gt;sentence&lt;/s&gt; with embedded tags
		/// &lt;/s&gt;
		/// </c>
		/// would return the list containing ["This is a sentence with embedded
		/// tags", "sentence"].
		/// </remarks>
		/// <exception cref="Org.Xml.Sax.SAXException">if tag doesn't exist in the file.</exception>
		/// <returns>List of String text contents of tags.</returns>
		private static IList<IElement> GetTagElementsFromFileSAXException(File f, string tag)
		{
			IList<IElement> sents = Generics.NewArrayList();
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				DocumentBuilder db = dbf.NewDocumentBuilder();
				IDocument doc = db.Parse(f);
				doc.GetDocumentElement().Normalize();
				INodeList nodeList = doc.GetElementsByTagName(tag);
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					// Get element
					IElement element = (IElement)nodeList.Item(i);
					sents.Add(element);
				}
			}
			catch (Exception e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>
		/// Returns the elements in the given file with the given tag associated with
		/// the text content of the two previous siblings and two next siblings.
		/// </summary>
		/// <returns>
		/// List of
		/// <c>Triple&lt;String, Element, String&gt;</c>
		/// Targeted elements surrounded
		/// by the text content of the two previous siblings and two next siblings.
		/// </returns>
		public static IList<Triple<string, IElement, string>> GetTagElementTriplesFromFile(File f, string tag)
		{
			IList<Triple<string, IElement, string>> sents = Generics.NewArrayList();
			try
			{
				sents = GetTagElementTriplesFromFileSAXException(f, tag);
			}
			catch (SAXException e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>
		/// Returns the elements in the given file with the given tag associated with
		/// the text content of the previous and next siblings up to max numIncludedSiblings.
		/// </summary>
		/// <returns>
		/// List of
		/// <c>Triple&lt;String, Element, String&gt;</c>
		/// Targeted elements surrounded
		/// by the text content of the two previous siblings and two next siblings.
		/// </returns>
		public static IList<Triple<string, IElement, string>> GetTagElementTriplesFromFileNumBounded(File f, string tag, int num)
		{
			IList<Triple<string, IElement, string>> sents = Generics.NewArrayList();
			try
			{
				sents = GetTagElementTriplesFromFileNumBoundedSAXException(f, tag, num);
			}
			catch (SAXException e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>
		/// Returns the elements in the given file with the given tag associated with
		/// the text content of the two previous siblings and two next siblings.
		/// </summary>
		/// <exception cref="Org.Xml.Sax.SAXException">if tag doesn't exist in the file.</exception>
		/// <returns>
		/// List of
		/// <c>Triple&lt;String, Element, String&gt;</c>
		/// Targeted elements surrounded
		/// by the text content of the two previous siblings and two next siblings.
		/// </returns>
		public static IList<Triple<string, IElement, string>> GetTagElementTriplesFromFileSAXException(File f, string tag)
		{
			return GetTagElementTriplesFromFileNumBoundedSAXException(f, tag, 2);
		}

		/// <summary>
		/// Returns the elements in the given file with the given tag associated with
		/// the text content of the previous and next siblings up to max numIncludedSiblings.
		/// </summary>
		/// <exception cref="Org.Xml.Sax.SAXException">if tag doesn't exist in the file.</exception>
		/// <returns>
		/// List of
		/// <c>Triple&lt;String, Element, String&gt;</c>
		/// Targeted elements surrounded
		/// by the text content of the two previous siblings and two next siblings.
		/// </returns>
		public static IList<Triple<string, IElement, string>> GetTagElementTriplesFromFileNumBoundedSAXException(File f, string tag, int numIncludedSiblings)
		{
			IList<Triple<string, IElement, string>> sents = Generics.NewArrayList();
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				DocumentBuilder db = dbf.NewDocumentBuilder();
				IDocument doc = db.Parse(f);
				doc.GetDocumentElement().Normalize();
				INodeList nodeList = doc.GetElementsByTagName(tag);
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					// Get element
					INode prevNode = nodeList.Item(i).GetPreviousSibling();
					string prev = string.Empty;
					int count = 0;
					while (prevNode != null && count <= numIncludedSiblings)
					{
						prev = prevNode.GetTextContent() + prev;
						prevNode = prevNode.GetPreviousSibling();
						count++;
					}
					INode nextNode = nodeList.Item(i).GetNextSibling();
					string next = string.Empty;
					count = 0;
					while (nextNode != null && count <= numIncludedSiblings)
					{
						next = next + nextNode.GetTextContent();
						nextNode = nextNode.GetNextSibling();
						count++;
					}
					IElement element = (IElement)nodeList.Item(i);
					Triple<string, IElement, string> t = new Triple<string, IElement, string>(prev, element, next);
					sents.Add(t);
				}
			}
			catch (Exception e)
			{
				log.Warn(e);
			}
			return sents;
		}

		/// <summary>Returns a non-validating XML parser.</summary>
		/// <remarks>Returns a non-validating XML parser. The parser ignores both DTDs and XSDs.</remarks>
		/// <returns>An XML parser in the form of a DocumentBuilder</returns>
		public static DocumentBuilder GetXmlParser()
		{
			DocumentBuilder db = null;
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				dbf.SetValidating(false);
				//Disable DTD loading and validation
				//See http://stackoverflow.com/questions/155101/make-documentbuilder-parse-ignore-dtd-references
				dbf.SetFeature("http://apache.org/xml/features/nonvalidating/load-dtd-grammar", false);
				dbf.SetFeature("http://apache.org/xml/features/nonvalidating/load-external-dtd", false);
				db = dbf.NewDocumentBuilder();
				db.SetErrorHandler(new XMLUtils.SAXErrorHandler());
			}
			catch (ParserConfigurationException e)
			{
				log.Warnf("%s: Unable to create XML parser\n", typeof(Edu.Stanford.Nlp.Util.XMLUtils).FullName);
				log.Warn(e);
			}
			catch (NotSupportedException e)
			{
				log.Warnf("%s: API error while setting up XML parser. Check your JAXP version\n", typeof(Edu.Stanford.Nlp.Util.XMLUtils).FullName);
				log.Warn(e);
			}
			return db;
		}

		/// <summary>Returns a validating XML parser given an XSD (not DTD!).</summary>
		/// <param name="schemaFile">File wit hXML schema</param>
		/// <returns>An XML parser in the form of a DocumentBuilder</returns>
		public static DocumentBuilder GetValidatingXmlParser(File schemaFile)
		{
			DocumentBuilder db = null;
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				SchemaFactory factory = SchemaFactory.NewInstance(XMLConstants.W3cXmlSchemaNsUri);
				Schema schema = factory.NewSchema(schemaFile);
				dbf.SetSchema(schema);
				db = dbf.NewDocumentBuilder();
				db.SetErrorHandler(new XMLUtils.SAXErrorHandler());
			}
			catch (ParserConfigurationException e)
			{
				log.Warnf("%s: Unable to create XML parser\n", typeof(Edu.Stanford.Nlp.Util.XMLUtils).FullName);
				log.Warn(e);
			}
			catch (SAXException e)
			{
				log.Warnf("%s: XML parsing exception while loading schema %s\n", typeof(Edu.Stanford.Nlp.Util.XMLUtils).FullName, schemaFile.GetPath());
				log.Warn(e);
			}
			catch (NotSupportedException e)
			{
				log.Warnf("%s: API error while setting up XML parser. Check your JAXP version\n", typeof(Edu.Stanford.Nlp.Util.XMLUtils).FullName);
				log.Warn(e);
			}
			return db;
		}

		/// <summary>Block-level HTML tags that are rendered with surrounding line breaks.</summary>
		private static readonly ICollection<string> breakingTags = Generics.NewHashSet(Arrays.AsList(new string[] { "blockquote", "br", "div", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "li", "ol", "p", "pre", "ul", "tr", "td" }));

		/// <param name="r">the reader to read the XML/HTML from</param>
		/// <param name="mapBack">
		/// a List of Integers mapping the positions in the result buffer
		/// to positions in the original Reader, will be cleared on receipt
		/// </param>
		/// <returns>the String containing the resulting text</returns>
		public static string StripTags(Reader r, IList<int> mapBack, bool markLineBreaks)
		{
			if (mapBack != null)
			{
				mapBack.Clear();
			}
			// just in case it has something in it!
			StringBuilder result = new StringBuilder();
			try
			{
				int position = 0;
				do
				{
					string text = Edu.Stanford.Nlp.Util.XMLUtils.ReadUntilTag(r);
					if (text.Length > 0)
					{
						// add offsets to the map back
						for (int i = 0; i < text.Length; i++)
						{
							result.Append(text[i]);
							if (mapBack != null)
							{
								mapBack.Add(int.Parse(position + i));
							}
						}
						position += text.Length;
					}
					//        System.err.println(position + " got text: " + text);
					string tag = Edu.Stanford.Nlp.Util.XMLUtils.ReadTag(r);
					if (tag == null)
					{
						break;
					}
					if (markLineBreaks && Edu.Stanford.Nlp.Util.XMLUtils.IsBreaking(ParseTag(tag)))
					{
						result.Append("\n");
						if (mapBack != null)
						{
							mapBack.Add(int.Parse(-position));
						}
					}
					position += tag.Length;
				}
				while (true);
			}
			catch (IOException e)
			{
				//        System.err.println(position + " got tag: " + tag);
				log.Warn("Error reading string");
				log.Warn(e);
			}
			return result.ToString();
		}

		public static bool IsBreaking(string tag)
		{
			return breakingTags.Contains(tag);
		}

		public static bool IsBreaking(XMLUtils.XMLTag tag)
		{
			return breakingTags.Contains(tag.name);
		}

		/// <summary>Reads all text up to next XML tag and returns it as a String.</summary>
		/// <returns>the String of the text read, which may be empty.</returns>
		/// <exception cref="System.IO.IOException"/>
		public static string ReadUntilTag(Reader r)
		{
			if (!r.Ready())
			{
				return string.Empty;
			}
			StringBuilder b = new StringBuilder();
			int c = r.Read();
			while (c >= 0 && c != '<')
			{
				b.Append((char)c);
				c = r.Read();
			}
			return b.ToString();
		}

		/// <returns>the new XMLTag object, or null if couldn't be created</returns>
		/// <exception cref="System.IO.IOException"/>
		public static XMLUtils.XMLTag ReadAndParseTag(Reader r)
		{
			string s = ReadTag(r);
			if (s == null)
			{
				return null;
			}
			XMLUtils.XMLTag ret = null;
			try
			{
				ret = new XMLUtils.XMLTag(s);
			}
			catch (Exception)
			{
				log.Warn("Failed to handle |" + s + "|");
			}
			return ret;
		}

		private static readonly Pattern xmlEscapingPattern = Pattern.Compile("&.+?;");

		// Pattern is reentrant, going by the statement "many matchers can share the same pattern"
		// on the Pattern javadoc.  Therefore, this should be safe as a static final variable.
		public static string UnescapeStringForXML(string s)
		{
			StringBuilder result = new StringBuilder();
			Matcher m = xmlEscapingPattern.Matcher(s);
			int end = 0;
			while (m.Find())
			{
				int start = m.Start();
				result.Append(Sharpen.Runtime.Substring(s, end, start));
				end = m.End();
				result.Append(Translate(Sharpen.Runtime.Substring(s, start, end)));
			}
			result.Append(Sharpen.Runtime.Substring(s, end, s.Length));
			return result.ToString();
		}

		private static char Translate(string s)
		{
			switch (s)
			{
				case "&amp;":
				{
					return '&';
				}

				case "&lt;":
				case "&Lt;":
				{
					return '<';
				}

				case "&gt;":
				case "&Gt;":
				{
					return '>';
				}

				case "&quot;":
				{
					return '\"';
				}

				case "&apos;":
				{
					return '\'';
				}

				case "&ast;":
				case "&sharp;":
				{
					return '-';
				}

				case "&equals;":
				{
					return '=';
				}

				case "&nbsp;":
				{
					return (char)unchecked((int)(0xA0));
				}

				case "&iexcl;":
				{
					return (char)unchecked((int)(0xA1));
				}

				case "&cent;":
				case "&shilling;":
				{
					return (char)unchecked((int)(0xA2));
				}

				case "&pound;":
				{
					return (char)unchecked((int)(0xA3));
				}

				case "&curren;":
				{
					return (char)unchecked((int)(0xA4));
				}

				case "&yen;":
				{
					return (char)unchecked((int)(0xA5));
				}

				case "&brvbar;":
				{
					return (char)unchecked((int)(0xA6));
				}

				case "&sect;":
				{
					return (char)unchecked((int)(0xA7));
				}

				case "&uml;":
				{
					return (char)unchecked((int)(0xA8));
				}

				case "&copy;":
				{
					return (char)unchecked((int)(0xA9));
				}

				case "&ordf;":
				{
					return (char)unchecked((int)(0xAA));
				}

				case "&laquo; ":
				{
					return (char)unchecked((int)(0xAB));
				}

				case "&not;":
				{
					return (char)unchecked((int)(0xAC));
				}

				case "&shy; ":
				{
					return (char)unchecked((int)(0xAD));
				}

				case "&reg;":
				{
					return (char)unchecked((int)(0xAE));
				}

				case "&macr;":
				{
					return (char)unchecked((int)(0xAF));
				}

				case "&deg;":
				{
					return (char)unchecked((int)(0xB0));
				}

				case "&plusmn;":
				{
					return (char)unchecked((int)(0xB1));
				}

				case "&sup2;":
				{
					return (char)unchecked((int)(0xB2));
				}

				case "&sup3;":
				{
					return (char)unchecked((int)(0xB3));
				}

				case "&acute;":
				{
					return (char)unchecked((int)(0xB4));
				}

				case "&micro;":
				{
					return (char)unchecked((int)(0xB5));
				}

				case "&middot;":
				{
					return (char)unchecked((int)(0xB7));
				}

				case "&cedil;":
				{
					return (char)unchecked((int)(0xB8));
				}

				case "&sup1;":
				{
					return (char)unchecked((int)(0xB9));
				}

				case "&ordm;":
				{
					return (char)unchecked((int)(0xBA));
				}

				case "&raquo;":
				{
					return (char)unchecked((int)(0xBB));
				}

				case "&frac14; ":
				{
					return (char)unchecked((int)(0xBC));
				}

				case "&frac12;":
				{
					return (char)unchecked((int)(0xBD));
				}

				case "&frac34; ":
				{
					return (char)unchecked((int)(0xBE));
				}

				case "&iquest;":
				{
					return (char)unchecked((int)(0xBF));
				}

				case "&Agrave;":
				{
					return (char)unchecked((int)(0xC0));
				}

				case "&Aacute;":
				{
					return (char)unchecked((int)(0xC1));
				}

				case "&Acirc;":
				{
					return (char)unchecked((int)(0xC2));
				}

				case "&Atilde;":
				{
					return (char)unchecked((int)(0xC3));
				}

				case "&Auml;":
				{
					return (char)unchecked((int)(0xC4));
				}

				case "&Aring;":
				{
					return (char)unchecked((int)(0xC5));
				}

				case "&AElig;":
				{
					return (char)unchecked((int)(0xC6));
				}

				case "&Ccedil;":
				{
					return (char)unchecked((int)(0xC7));
				}

				case "&Egrave;":
				{
					return (char)unchecked((int)(0xC8));
				}

				case "&Eacute;":
				{
					return (char)unchecked((int)(0xC9));
				}

				case "&Ecirc;":
				{
					return (char)unchecked((int)(0xCA));
				}

				case "&Euml;":
				{
					return (char)unchecked((int)(0xCB));
				}

				case "&Igrave;":
				{
					return (char)unchecked((int)(0xCC));
				}

				case "&Iacute;":
				{
					return (char)unchecked((int)(0xCD));
				}

				case "&Icirc;":
				{
					return (char)unchecked((int)(0xCE));
				}

				case "&Iuml;":
				{
					return (char)unchecked((int)(0xCF));
				}

				case "&ETH;":
				{
					return (char)unchecked((int)(0xD0));
				}

				case "&Ntilde;":
				{
					return (char)unchecked((int)(0xD1));
				}

				case "&Ograve;":
				{
					return (char)unchecked((int)(0xD2));
				}

				case "&Oacute;":
				{
					return (char)unchecked((int)(0xD3));
				}

				case "&Ocirc;":
				{
					return (char)unchecked((int)(0xD4));
				}

				case "&Otilde;":
				{
					return (char)unchecked((int)(0xD5));
				}

				case "&Ouml;":
				{
					return (char)unchecked((int)(0xD6));
				}

				case "&times;":
				{
					return (char)unchecked((int)(0xD7));
				}

				case "&Oslash;":
				{
					return (char)unchecked((int)(0xD8));
				}

				case "&Ugrave;":
				{
					return (char)unchecked((int)(0xD9));
				}

				case "&Uacute;":
				{
					return (char)unchecked((int)(0xDA));
				}

				case "&Ucirc;":
				{
					return (char)unchecked((int)(0xDB));
				}

				case "&Uuml;":
				{
					return (char)unchecked((int)(0xDC));
				}

				case "&Yacute;":
				{
					return (char)unchecked((int)(0xDD));
				}

				case "&THORN;":
				{
					return (char)unchecked((int)(0xDE));
				}

				case "&szlig;":
				{
					return (char)unchecked((int)(0xDF));
				}

				case "&agrave;":
				{
					return (char)unchecked((int)(0xE0));
				}

				case "&aacute;":
				{
					return (char)unchecked((int)(0xE1));
				}

				case "&acirc;":
				{
					return (char)unchecked((int)(0xE2));
				}

				case "&atilde;":
				{
					return (char)unchecked((int)(0xE3));
				}

				case "&auml;":
				{
					return (char)unchecked((int)(0xE4));
				}

				case "&aring;":
				{
					return (char)unchecked((int)(0xE5));
				}

				case "&aelig;":
				{
					return (char)unchecked((int)(0xE6));
				}

				case "&ccedil;":
				{
					return (char)unchecked((int)(0xE7));
				}

				case "&egrave;":
				{
					return (char)unchecked((int)(0xE8));
				}

				case "&eacute;":
				{
					return (char)unchecked((int)(0xE9));
				}

				case "&ecirc;":
				{
					return (char)unchecked((int)(0xEA));
				}

				case "&euml; ":
				{
					return (char)unchecked((int)(0xEB));
				}

				case "&igrave;":
				{
					return (char)unchecked((int)(0xEC));
				}

				case "&iacute;":
				{
					return (char)unchecked((int)(0xED));
				}

				case "&icirc;":
				{
					return (char)unchecked((int)(0xEE));
				}

				case "&iuml;":
				{
					return unchecked((int)(0xEF));
				}

				case "&eth;":
				{
					return (char)unchecked((int)(0xF0));
				}

				case "&ntilde;":
				{
					return (char)unchecked((int)(0xF1));
				}

				case "&ograve;":
				{
					return (char)unchecked((int)(0xF2));
				}

				case "&oacute;":
				{
					return (char)unchecked((int)(0xF3));
				}

				case "&ocirc;":
				{
					return (char)unchecked((int)(0xF4));
				}

				case "&otilde;":
				{
					return (char)unchecked((int)(0xF5));
				}

				case "&ouml;":
				{
					return (char)unchecked((int)(0xF6));
				}

				case "&divide;":
				{
					return (char)unchecked((int)(0xF7));
				}

				case "&oslash;":
				{
					return (char)unchecked((int)(0xF8));
				}

				case "&ugrave;":
				{
					return (char)unchecked((int)(0xF9));
				}

				case "&uacute;":
				{
					return (char)unchecked((int)(0xFA));
				}

				case "&ucirc;":
				{
					return (char)unchecked((int)(0xFB));
				}

				case "&uuml;":
				{
					return (char)unchecked((int)(0xFC));
				}

				case "&yacute;":
				{
					return (char)unchecked((int)(0xFD));
				}

				case "&thorn;":
				{
					return (char)unchecked((int)(0xFE));
				}

				case "&yuml;":
				{
					return (char)unchecked((int)(0xFF));
				}

				case "&OElig;":
				{
					return (char)unchecked((int)(0x152));
				}

				case "&oelig;":
				{
					return (char)unchecked((int)(0x153));
				}

				case "&Scaron;":
				{
					return (char)unchecked((int)(0x160));
				}

				case "&scaron;":
				{
					return (char)unchecked((int)(0x161));
				}

				case "&Yuml;":
				{
					return (char)unchecked((int)(0x178));
				}

				case "&circ;":
				{
					return (char)unchecked((int)(0x2C6));
				}

				case "&tilde;":
				{
					return (char)unchecked((int)(0x2DC));
				}

				case "&lrm;":
				{
					return (char)unchecked((int)(0x200E));
				}

				case "&rlm;":
				{
					return (char)unchecked((int)(0x200F));
				}

				case "&ndash;":
				{
					return (char)unchecked((int)(0x2013));
				}

				case "&mdash;":
				{
					return (char)unchecked((int)(0x2014));
				}

				case "&lsquo;":
				{
					return (char)unchecked((int)(0x2018));
				}

				case "&rsquo;":
				{
					return (char)unchecked((int)(0x2019));
				}

				case "&sbquo;":
				{
					return (char)unchecked((int)(0x201A));
				}

				case "&ldquo;":
				case "&bquo;":
				case "&bq;":
				{
					return (char)unchecked((int)(0x201C));
				}

				case "&rdquo;":
				case "&equo;":
				{
					return (char)0X201D;
				}

				case "&bdquo;":
				{
					return (char)unchecked((int)(0x201E));
				}

				case "&sim;":
				{
					return (char)unchecked((int)(0x223C));
				}

				case "&radic;":
				{
					return (char)unchecked((int)(0x221A));
				}

				case "&le;":
				{
					return (char)unchecked((int)(0x2264));
				}

				case "&ge;":
				{
					return (char)unchecked((int)(0x2265));
				}

				case "&larr;":
				{
					return (char)unchecked((int)(0x2190));
				}

				case "&darr;":
				{
					return (char)unchecked((int)(0x2193));
				}

				case "&rarr;":
				{
					return (char)unchecked((int)(0x2192));
				}

				case "&hellip;":
				{
					return (char)unchecked((int)(0x2026));
				}

				case "&prime;":
				{
					return (char)unchecked((int)(0x2032));
				}

				case "&Prime;":
				case "&ins;":
				{
					return (char)unchecked((int)(0x2033));
				}

				case "&trade;":
				{
					return (char)unchecked((int)(0x2122));
				}

				case "&Alpha;":
				case "&Agr;":
				{
					return (char)unchecked((int)(0x391));
				}

				case "&Beta;":
				case "&Bgr;":
				{
					return (char)unchecked((int)(0x392));
				}

				case "&Gamma;":
				case "&Ggr;":
				{
					return (char)unchecked((int)(0x393));
				}

				case "&Delta;":
				case "&Dgr;":
				{
					return (char)unchecked((int)(0x394));
				}

				case "&Epsilon;":
				case "&Egr;":
				{
					return (char)unchecked((int)(0x395));
				}

				case "&Zeta;":
				case "&Zgr;":
				{
					return (char)unchecked((int)(0x396));
				}

				case "&Eta;":
				{
					return (char)unchecked((int)(0x397));
				}

				case "&Theta;":
				case "&THgr;":
				{
					return (char)unchecked((int)(0x398));
				}

				case "&Iota;":
				case "&Igr;":
				{
					return (char)unchecked((int)(0x399));
				}

				case "&Kappa;":
				case "&Kgr;":
				{
					return (char)unchecked((int)(0x39A));
				}

				case "&Lambda;":
				case "&Lgr;":
				{
					return (char)unchecked((int)(0x39B));
				}

				case "&Mu;":
				case "&Mgr;":
				{
					return (char)unchecked((int)(0x39C));
				}

				case "&Nu;":
				case "&Ngr;":
				{
					return (char)unchecked((int)(0x39D));
				}

				case "&Xi;":
				case "&Xgr;":
				{
					return (char)unchecked((int)(0x39E));
				}

				case "&Omicron;":
				case "&Ogr;":
				{
					return (char)unchecked((int)(0x39F));
				}

				case "&Pi;":
				case "&Pgr;":
				{
					return (char)unchecked((int)(0x3A0));
				}

				case "&Rho;":
				case "&Rgr;":
				{
					return (char)unchecked((int)(0x3A1));
				}

				case "&Sigma;":
				case "&Sgr;":
				{
					return (char)unchecked((int)(0x3A3));
				}

				case "&Tau;":
				case "&Tgr;":
				{
					return (char)unchecked((int)(0x3A4));
				}

				case "&Upsilon;":
				case "&Ugr;":
				{
					return (char)unchecked((int)(0x3A5));
				}

				case "&Phi;":
				case "&PHgr;":
				{
					return (char)unchecked((int)(0x3A6));
				}

				case "&Chi;":
				case "&KHgr;":
				{
					return (char)unchecked((int)(0x3A7));
				}

				case "&Psi;":
				case "&PSgr;":
				{
					return (char)unchecked((int)(0x3A8));
				}

				case "&Omega;":
				case "&OHgr;":
				{
					return (char)unchecked((int)(0x3A9));
				}

				case "&alpha;":
				case "&agr;":
				{
					return (char)unchecked((int)(0x3B1));
				}

				case "&beta;":
				case "&bgr;":
				{
					return (char)unchecked((int)(0x3B2));
				}

				case "&gamma;":
				case "&ggr;":
				{
					return (char)unchecked((int)(0x3B3));
				}

				case "&delta;":
				case "&dgr;":
				{
					return (char)unchecked((int)(0x3B4));
				}

				case "&epsilon;":
				case "&egr;":
				{
					return (char)unchecked((int)(0x3B5));
				}

				case "&zeta;":
				case "&zgr;":
				{
					return (char)unchecked((int)(0x3B6));
				}

				case "&eta;":
				case "&eegr;":
				{
					return (char)unchecked((int)(0x3B7));
				}

				case "&theta;":
				case "&thgr;":
				{
					return (char)unchecked((int)(0x3B8));
				}

				case "&iota;":
				case "&igr;":
				{
					return (char)unchecked((int)(0x3B9));
				}

				case "&kappa;":
				case "&kgr;":
				{
					return (char)unchecked((int)(0x3BA));
				}

				case "&lambda;":
				case "&lgr;":
				{
					return (char)unchecked((int)(0x3BB));
				}

				case "&mu;":
				case "&mgr;":
				{
					return (char)unchecked((int)(0x3BC));
				}

				case "&nu;":
				case "&ngr;":
				{
					return (char)unchecked((int)(0x3BD));
				}

				case "&xi;":
				case "&xgr;":
				{
					return (char)unchecked((int)(0x3BE));
				}

				case "&omicron;":
				case "&ogr;":
				{
					return (char)unchecked((int)(0x3BF));
				}

				case "&pi;":
				case "&pgr;":
				{
					return (char)unchecked((int)(0x3C0));
				}

				case "&rho;":
				case "&rgr;":
				{
					return (char)unchecked((int)(0x3C1));
				}

				case "&sigma;":
				case "&sgr;":
				{
					return (char)unchecked((int)(0x3C3));
				}

				case "&tau;":
				case "&tgr;":
				{
					return (char)unchecked((int)(0x3C4));
				}

				case "&upsilon;":
				case "&ugr;":
				{
					return (char)unchecked((int)(0x3C5));
				}

				case "&phi;":
				case "&phgr;":
				{
					return (char)unchecked((int)(0x3C6));
				}

				case "&chi;":
				case "&khgr;":
				{
					return (char)unchecked((int)(0x3C7));
				}

				case "&psi;":
				case "&psgr;":
				{
					return (char)unchecked((int)(0x3C8));
				}

				case "&omega;":
				case "&ohgr;":
				{
					return (char)unchecked((int)(0x3C9));
				}

				case "&bull;":
				{
					return (char)unchecked((int)(0x2022));
				}

				case "&percnt;":
				{
					return '%';
				}

				case "&plus;":
				{
					return '+';
				}

				case "&dash;":
				{
					return '-';
				}

				case "&abreve;":
				case "&amacr;":
				case "&ape;":
				case "&aogon;":
				{
					return 'a';
				}

				case "&Amacr;":
				{
					return 'A';
				}

				case "&cacute;":
				case "&ccaron;":
				case "&ccirc;":
				{
					return 'c';
				}

				case "&Ccaron;":
				{
					return 'C';
				}

				case "&dcaron;":
				{
					return 'd';
				}

				case "&ecaron;":
				case "&emacr;":
				case "&eogon;":
				{
					return 'e';
				}

				case "&Emacr;":
				case "&Ecaron;":
				{
					return 'E';
				}

				case "&lacute;":
				{
					return 'l';
				}

				case "&Lacute;":
				{
					return 'L';
				}

				case "&nacute;":
				case "&ncaron;":
				case "&ncedil;":
				{
					return 'n';
				}

				case "&rcaron;":
				case "&racute;":
				{
					return 'r';
				}

				case "&Rcaron;":
				{
					return 'R';
				}

				case "&omacr;":
				{
					return 'o';
				}

				case "&imacr;":
				{
					return 'i';
				}

				case "&sacute;":
				case "&scedil;":
				case "&scirc;":
				{
					return 's';
				}

				case "&Sacute":
				case "&Scedil;":
				{
					return 'S';
				}

				case "&tcaron;":
				case "&tcedil;":
				{
					return 't';
				}

				case "&umacr;":
				case "&uring;":
				{
					return 'u';
				}

				case "&wcirc;":
				{
					return 'w';
				}

				case "&Ycirc;":
				{
					return 'Y';
				}

				case "&ycirc;":
				{
					return 'y';
				}

				case "&zcaron;":
				case "&zacute;":
				{
					return 'z';
				}

				case "&Zcaron;":
				{
					return 'Z';
				}

				case "&hearts;":
				{
					return (char)unchecked((int)(0x2665));
				}

				case "&infin;":
				{
					return (char)unchecked((int)(0x221E));
				}

				case "&dollar;":
				{
					return '$';
				}

				case "&sub;":
				case "&lcub;":
				{
					return (char)unchecked((int)(0x2282));
				}

				case "&sup;":
				case "&rcub;":
				{
					return (char)unchecked((int)(0x2283));
				}

				case "&lsqb;":
				{
					return '[';
				}

				case "&rsqb;":
				{
					return ']';
				}

				default:
				{
					return ' ';
				}
			}
		}

		/// <summary>
		/// Returns a String in which all the XML special characters have been
		/// escaped.
		/// </summary>
		/// <remarks>
		/// Returns a String in which all the XML special characters have been
		/// escaped. The resulting String is valid to print in an XML file as an
		/// attribute or element value in all circumstances.  (Note that it may
		/// escape characters that didn't need to be escaped.)
		/// </remarks>
		/// <param name="in">The String to escape</param>
		/// <returns>The escaped String</returns>
		public static string EscapeXML(string @in)
		{
			int leng = @in.Length;
			StringBuilder sb = new StringBuilder(leng);
			for (int i = 0; i < leng; i++)
			{
				char c = @in[i];
				if (c == '&')
				{
					sb.Append("&amp;");
				}
				else
				{
					if (c == '<')
					{
						sb.Append("&lt;");
					}
					else
					{
						if (c == '>')
						{
							sb.Append("&gt;");
						}
						else
						{
							if (c == '"')
							{
								sb.Append("&quot;");
							}
							else
							{
								if (c == '\'')
								{
									sb.Append("&apos;");
								}
								else
								{
									sb.Append(c);
								}
							}
						}
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns a String in which some the XML special characters have been
		/// escaped: just the ones that need escaping in an element content.
		/// </summary>
		/// <param name="in">The String to escape</param>
		/// <returns>The escaped String</returns>
		public static string EscapeElementXML(string @in)
		{
			int leng = @in.Length;
			StringBuilder sb = new StringBuilder(leng);
			for (int i = 0; i < leng; i++)
			{
				char c = @in[i];
				if (c == '&')
				{
					sb.Append("&amp;");
				}
				else
				{
					if (c == '<')
					{
						sb.Append("&lt;");
					}
					else
					{
						if (c == '>')
						{
							sb.Append("&gt;");
						}
						else
						{
							sb.Append(c);
						}
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns a String in which some XML special characters have been
		/// escaped.
		/// </summary>
		/// <remarks>
		/// Returns a String in which some XML special characters have been
		/// escaped. This just escapes attribute value ones, assuming that
		/// you're going to quote with double quotes.
		/// That is, only " and & are escaped.
		/// </remarks>
		/// <param name="in">The String to escape</param>
		/// <returns>The escaped String</returns>
		public static string EscapeAttributeXML(string @in)
		{
			int leng = @in.Length;
			StringBuilder sb = new StringBuilder(leng);
			for (int i = 0; i < leng; i++)
			{
				char c = @in[i];
				if (c == '&')
				{
					sb.Append("&amp;");
				}
				else
				{
					if (c == '"')
					{
						sb.Append("&quot;");
					}
					else
					{
						sb.Append(c);
					}
				}
			}
			return sb.ToString();
		}

		public static string EscapeTextAroundXMLTags(string s)
		{
			StringBuilder result = new StringBuilder();
			Reader r = new StringReader(s);
			try
			{
				do
				{
					string text = ReadUntilTag(r);
					//      System.err.println("got text: " + text);
					result.Append(EscapeXML(text));
					XMLUtils.XMLTag tag = ReadAndParseTag(r);
					//      System.err.println("got tag: " + tag);
					if (tag == null)
					{
						break;
					}
					result.Append(tag);
				}
				while (true);
			}
			catch (IOException e)
			{
				log.Warn("Error reading string");
				log.Warn(e);
			}
			return result.ToString();
		}

		/// <summary>return either the first space or the first nbsp</summary>
		public static int FindSpace(string haystack, int begin)
		{
			int space = haystack.IndexOf(' ', begin);
			int nbsp = haystack.IndexOf('\u00A0', begin);
			if (space == -1 && nbsp == -1)
			{
				return -1;
			}
			else
			{
				if (space >= 0 && nbsp >= 0)
				{
					return Math.Min(space, nbsp);
				}
				else
				{
					// eg one is -1, and the other is >= 0
					return Math.Max(space, nbsp);
				}
			}
		}

		public class XMLTag
		{
			/// <summary>Stores the complete string passed in as the tag on construction.</summary>
			public string text;

			/// <summary>Stores the elememnt name, such as "doc".</summary>
			public string name;

			/// <summary>Stores attributes as a Map from keys to values.</summary>
			public IDictionary<string, string> attributes;

			/// <summary>Whether this is an ending tag or not.</summary>
			public bool isEndTag;

			/// <summary>
			/// Whether this is an empty element expressed as a single empty element tag like
			/// <c>&lt;p/&gt;</c>
			/// .
			/// </summary>
			public bool isSingleTag;

			/// <summary>Assumes that String contains an XML tag.</summary>
			/// <param name="tag">String to turn into an XMLTag object</param>
			public XMLTag(string tag)
			{
				if (tag == null || tag.IsEmpty())
				{
					throw new ArgumentNullException("Attempted to parse empty/null tag");
				}
				if (tag[0] != '<')
				{
					throw new ArgumentException("Tag did not start with <");
				}
				if (tag[tag.Length - 1] != '>')
				{
					throw new ArgumentException("Tag did not end with >");
				}
				text = tag;
				int begin = 1;
				if (tag[1] == '/')
				{
					begin = 2;
					isEndTag = true;
				}
				else
				{
					isEndTag = false;
				}
				int end = tag.Length - 1;
				if (tag[tag.Length - 2] == '/')
				{
					end = tag.Length - 2;
					isSingleTag = true;
				}
				else
				{
					isSingleTag = false;
				}
				tag = Sharpen.Runtime.Substring(tag, begin, end);
				attributes = Generics.NewHashMap();
				begin = 0;
				end = FindSpace(tag, 0);
				if (end < 0)
				{
					name = tag;
				}
				else
				{
					name = Sharpen.Runtime.Substring(tag, begin, end);
					do
					{
						begin = end + 1;
						while (begin < tag.Length && tag[begin] < unchecked((int)(0x21)))
						{
							begin++;
						}
						// get rid of leading whitespace
						if (begin == tag.Length)
						{
							break;
						}
						end = tag.IndexOf('=', begin);
						if (end < 0)
						{
							string att = Sharpen.Runtime.Substring(tag, begin);
							attributes[att] = string.Empty;
							break;
						}
						string att_1 = Sharpen.Runtime.Substring(tag, begin, end).Trim();
						begin = end + 1;
						string value = null;
						if (tag.Length > begin)
						{
							while (begin < tag.Length && tag[begin] < unchecked((int)(0x21)))
							{
								begin++;
							}
							if (begin < tag.Length && tag[begin] == '\"')
							{
								// get quoted expression
								begin++;
								end = tag.IndexOf('\"', begin);
								if (end < 0)
								{
									break;
								}
								// this is a problem
								value = Sharpen.Runtime.Substring(tag, begin, end);
								end++;
							}
							else
							{
								// get unquoted expression
								end = FindSpace(tag, begin);
								if (end < 0)
								{
									end = tag.Length;
								}
								//              System.err.println(begin + " " + end);
								value = Sharpen.Runtime.Substring(tag, begin, end);
							}
						}
						attributes[att_1] = value;
					}
					while (end < tag.Length - 3);
				}
			}

			public override string ToString()
			{
				return text;
			}

			/// <summary>Given a list of attributes, return the first one that is non-null</summary>
			public virtual string GetFirstNonNullAttributeFromList(IList<string> attributesList)
			{
				foreach (string attribute in attributesList)
				{
					if (attributes[attribute] != null)
					{
						return attributes[attribute];
					}
				}
				return null;
			}
		}

		// end static class XMLTag
		/// <summary>Reads all text of the XML tag and returns it as a String.</summary>
		/// <remarks>
		/// Reads all text of the XML tag and returns it as a String.
		/// Assumes that a '&lt;' character has already been read.
		/// </remarks>
		/// <param name="r">The reader to read from</param>
		/// <returns>
		/// The String representing the tag, or null if one couldn't be read
		/// (i.e., EOF).  The returned item is a complete tag including angle
		/// brackets, such as
		/// <c>&lt;TXT&gt;</c>
		/// </returns>
		/// <exception cref="System.IO.IOException"/>
		public static string ReadTag(Reader r)
		{
			if (!r.Ready())
			{
				return null;
			}
			StringBuilder b = new StringBuilder("<");
			int c = r.Read();
			while (c >= 0)
			{
				b.Append((char)c);
				if (c == '>')
				{
					break;
				}
				c = r.Read();
			}
			if (b.Length == 1)
			{
				return null;
			}
			return b.ToString();
		}

		public static XMLUtils.XMLTag ParseTag(string tagString)
		{
			if (tagString == null || tagString.IsEmpty())
			{
				return null;
			}
			if (tagString[0] != '<' || tagString[tagString.Length - 1] != '>')
			{
				return null;
			}
			return new XMLUtils.XMLTag(tagString);
		}

		/// <exception cref="System.Exception"/>
		public static IDocument ReadDocumentFromFile(string filename)
		{
			InputSource @in = new InputSource(new FileReader(filename));
			DocumentBuilderFactory factory = DocumentBuilderFactory.NewInstance();
			factory.SetNamespaceAware(false);
			DocumentBuilder db = factory.NewDocumentBuilder();
			db.SetErrorHandler(new XMLUtils.SAXErrorHandler());
			return db.Parse(@in);
		}

		private class SAXErrorHandler : IErrorHandler
		{
			public static string MakeBetterErrorString(string msg, SAXParseException ex)
			{
				StringBuilder sb = new StringBuilder(msg);
				sb.Append(": ");
				string str = ex.Message;
				if (str.LastIndexOf('.') == str.Length - 1)
				{
					str = Sharpen.Runtime.Substring(str, 0, str.Length - 1);
				}
				sb.Append(str);
				sb.Append(" at document line ").Append(ex.GetLineNumber());
				sb.Append(", column ").Append(ex.GetColumnNumber());
				if (ex.GetSystemId() != null)
				{
					sb.Append(" in entity from systemID ").Append(ex.GetSystemId());
				}
				else
				{
					if (ex.GetPublicId() != null)
					{
						sb.Append(" in entity from publicID ").Append(ex.GetPublicId());
					}
				}
				sb.Append('.');
				return sb.ToString();
			}

			public virtual void Warning(SAXParseException exception)
			{
				log.Warn(MakeBetterErrorString("Warning", exception));
			}

			public virtual void Error(SAXParseException exception)
			{
				log.Error(MakeBetterErrorString("Error", exception));
			}

			/// <exception cref="Org.Xml.Sax.SAXParseException"/>
			public virtual void FatalError(SAXParseException ex)
			{
				throw new SAXParseException(MakeBetterErrorString("Fatal Error", ex), ex.GetPublicId(), ex.GetSystemId(), ex.GetLineNumber(), ex.GetColumnNumber());
			}
			// throw new RuntimeException(makeBetterErrorString("Fatal Error", ex));
		}

		// end class SAXErrorHandler
		/// <exception cref="System.Exception"/>
		public static IDocument ReadDocumentFromString(string s)
		{
			InputSource @in = new InputSource(new StringReader(s));
			DocumentBuilderFactory factory = DocumentBuilderFactory.NewInstance();
			factory.SetNamespaceAware(false);
			return factory.NewDocumentBuilder().Parse(@in);
		}

		/// <summary>Tests a few methods.</summary>
		/// <remarks>
		/// Tests a few methods.
		/// If the first arg is -readDoc then this method tests
		/// readDocumentFromFile.
		/// Otherwise, it tests readTag/readUntilTag and slurpFile.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			if (args[0].Equals("-readDoc"))
			{
				IDocument doc = ReadDocumentFromFile(args[1]);
				System.Console.Out.WriteLine(doc);
			}
			else
			{
				string s = IOUtils.SlurpFile(args[0]);
				Reader r = new StringReader(s);
				string tag = ReadTag(r);
				while (tag != null && !tag.IsEmpty())
				{
					ReadUntilTag(r);
					tag = ReadTag(r);
					if (tag == null || tag.IsEmpty())
					{
						break;
					}
					System.Console.Out.WriteLine("got tag=" + new XMLUtils.XMLTag(tag));
				}
			}
		}
	}
}
