using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Javax.Xml.Parsers;
using Org.Xml.Sax;
using Org.Xml.Sax.Helpers;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Reads XML from an input file or stream and writes XML to an output
	/// file or stream, while transforming text appearing inside specified
	/// XML tags by applying a specified
	/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
	/// .  See TransformXMLApplications for examples.
	/// <i>Implementation note:</i> This is done using SAX2.
	/// </summary>
	/// <?/>
	/// <author>Bill MacCartney</author>
	/// <author>Anna Rafferty (refactoring, making SAXInterface easy to extend elsewhere)</author>
	public class TransformXML<T>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.TransformXML));

		private readonly SAXParser saxParser;

		public virtual TransformXML.SAXInterface<T> BuildSaxInterface()
		{
			return new TransformXML.SAXInterface<T>();
		}

		public class SAXInterface<T> : DefaultHandler
		{
			protected internal IList<string> elementsToBeTransformed;

			protected internal StringBuilder textToBeTransformed;

			protected internal PrintWriter outWriter = new PrintWriter(System.Console.Out, true);

			protected internal IFunction<string, T> function;

			/// <summary>How far down we are in the nested tags.</summary>
			/// <remarks>
			/// How far down we are in the nested tags.  For example, if we've
			/// seen &lt;foo&gt; &lt;bar&gt; and "foo" and "bar" are both tags
			/// we care about, then depth = 2.
			/// </remarks>
			protected internal int depth = 0;

			public SAXInterface()
			{
				elementsToBeTransformed = new List<string>();
				depth = 0;
				openingTag = null;
				textToBeTransformed = new StringBuilder();
			}

			/// <summary>
			/// The first tag from
			/// <see><code>elementsToBeTransformed</code></see>
			/// that we saw the last time
			/// <see><code>depth</code></see>
			/// was
			/// <code>0</code>.
			/// <br />
			/// You would expect incoming XML to be well-formatted, but just in
			/// case it isn't, we keep track of this so we can output the
			/// correct closing tag.
			/// </summary>
			internal string openingTag;

			private void OutputTextAndTag(string qName, IAttributes attributes, bool close)
			{
				// If we're not already in an element to be transformed, first
				// echo the previous text...
				outWriter.Print(XMLUtils.EscapeXML(textToBeTransformed.ToString()));
				textToBeTransformed = new StringBuilder();
				// ... then echo the new tag to outStream 
				outWriter.Print('<');
				if (close)
				{
					outWriter.Print('/');
				}
				outWriter.Print(qName);
				if (attributes != null)
				{
					for (int i = 0; i < attributes.GetLength(); i++)
					{
						outWriter.Print(' ');
						outWriter.Print(attributes.GetQName(i));
						outWriter.Print("=\"");
						outWriter.Print(XMLUtils.EscapeXML(attributes.GetValue(i)));
						outWriter.Print('"');
					}
				}
				outWriter.Print(">\n");
			}

			public override void EndDocument()
			{
				// Theoretically, there shouldn't be anything in the buffer after
				// the last closing tag, but if there is, it's probably better to
				// echo it than ignore it
				outWriter.Print(XMLUtils.EscapeXML(textToBeTransformed.ToString()));
				// we need to flush because there are no other ways we
				// explicitely flush
				outWriter.Flush();
			}

			// Called at the beginning of each element.  If the tag is on the
			// designated list, set flag to remember that we're in an element
			// to be transformed.  In either case, echo tag.
			/// <exception cref="Org.Xml.Sax.SAXException"/>
			public override void StartElement(string uri, string localName, string qName, IAttributes attributes)
			{
				//log.info("start element " + qName);
				if (depth == 0)
				{
					OutputTextAndTag(qName, attributes, false);
				}
				if (elementsToBeTransformed.Contains(qName))
				{
					if (depth == 0)
					{
						openingTag = qName;
					}
					++depth;
				}
			}

			// Called at the end of each element.  If the tag is on the
			// designated list, apply the designated {@link Function
			// <code>Function</code>} to the accumulated text and echo the the
			// result.  In either case, echo the closing tag.
			/// <exception cref="Org.Xml.Sax.SAXException"/>
			public override void EndElement(string uri, string localName, string qName)
			{
				//log.info("end element " + qName + "; function is " + function.getClass());
				//log.info("elementsToBeTransformed is " + elementsToBeTransformed);
				//log.info("textToBeTransformed is " + textToBeTransformed);
				if (depth == 0)
				{
					OutputTextAndTag(qName, null, true);
				}
				else
				{
					if (elementsToBeTransformed.Contains(qName))
					{
						--depth;
						if (depth == 0)
						{
							string text = textToBeTransformed.ToString().Trim();
							// factored out so subclasses can handle the text differently
							ProcessText(text);
							textToBeTransformed = new StringBuilder();
							outWriter.Print("</" + openingTag + ">\n");
						}
					}
				}
			}

			// when we're inside a block to be transformed, we ignore
			// elements that don't end the block.
			public virtual void ProcessText(string text)
			{
				if (text.Length > 0)
				{
					text = function.Apply(text).ToString();
					outWriter.Print(XMLUtils.EscapeXML(text));
					outWriter.Print('\n');
				}
			}

			// Accumulate characters in buffer of text to be transformed
			// (SAX may call this after each line break)
			/// <exception cref="Org.Xml.Sax.SAXException"/>
			public override void Characters(char[] buf, int offset, int len)
			{
				// log.info("characters |" + new String(buf, offset, len) + "|");
				textToBeTransformed.Append(buf, offset, len);
			}
		}

		/// <summary>
		/// This version of the SAXInterface doesn't escape the text produced
		/// by the function.
		/// </summary>
		/// <remarks>
		/// This version of the SAXInterface doesn't escape the text produced
		/// by the function.  This is useful in the case where the function
		/// already produces well-formed XML.  One example of this is the
		/// Tagger, which already escapes the inner text and produces xml
		/// tags around the words.
		/// </remarks>
		public class NoEscapingSAXInterface<T> : TransformXML.SAXInterface<T>
		{
			// end static class SAXInterface
			public override void ProcessText(string text)
			{
				if (text.Length > 0)
				{
					text = function.Apply(text).ToString();
					outWriter.Print(text);
					outWriter.Print('\n');
				}
			}
		}

		public TransformXML()
		{
			try
			{
				saxParser = SAXParserFactory.NewInstance().NewSAXParser();
			}
			catch (Exception e)
			{
				log.Info("Error configuring XML parser: " + e);
				throw new Exception(e);
			}
		}

		/// <summary>
		/// Read XML from the specified file and write XML to stdout,
		/// while transforming text appearing inside the specified XML
		/// tags by applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .  Note that the <code>Function</code>
		/// you supply must be prepared to accept <code>String</code>s as
		/// input; if your <code>Function</code> doesn't handle
		/// <code>String</code>s, you need to write a wrapper for it that
		/// does.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML tag
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>File</code> to read from</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, File @in)
		{
			InputStream ins = null;
			try
			{
				ins = new BufferedInputStream(new FileInputStream(@in));
				TransformXML(tags, fn, ins, System.Console.Out);
			}
			catch (Exception e)
			{
				log.Info("Error reading file " + @in + ": " + e);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(ins);
			}
		}

		/// <summary>
		/// Read XML from the specified file and write XML to specified file,
		/// while transforming text appearing inside the specified XML tags
		/// by applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .
		/// Note that the <code>Function</code> you supply must be
		/// prepared to accept <code>String</code>s as input; if your
		/// <code>Function</code> doesn't handle <code>String</code>s, you
		/// need to write a wrapper for it that does.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML tag
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>File</code> to read from</param>
		/// <param name="out">the <code>File</code> to write to</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, File @in, File @out)
		{
			InputStream ins = null;
			OutputStream outs = null;
			try
			{
				ins = new BufferedInputStream(new FileInputStream(@in));
				outs = new BufferedOutputStream(new FileOutputStream(@out));
				TransformXML(tags, fn, ins, outs);
			}
			catch (Exception e)
			{
				log.Info("Error reading file " + @in + " or writing file " + @out + ": " + e);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(ins);
				IOUtils.CloseIgnoringExceptions(outs);
			}
		}

		/// <summary>
		/// Read XML from input stream and write XML to stdout, while
		/// transforming text appearing inside the specified XML tags by
		/// applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .
		/// Note that the <code>Function</code> you supply must be
		/// prepared to accept <code>String</code>s as input; if your
		/// <code>Function</code> doesn't handle <code>String</code>s, you
		/// need to write a wrapper for it that does.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML tag
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>InputStream</code> to read from</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, InputStream @in)
		{
			TransformXML(tags, fn, @in, System.Console.Out);
		}

		/// <summary>
		/// Read XML from input stream and write XML to output stream,
		/// while transforming text appearing inside the specified XML tags
		/// by applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .
		/// Note that the <code>Function</code> you supply must be
		/// prepared to accept <code>String</code>s as input; if your
		/// <code>Function</code> doesn't handle <code>String</code>s, you
		/// need to write a wrapper for it that does.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML tag
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>InputStream</code> to read from</param>
		/// <param name="out">the <code>OutputStream</code> to write to</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, InputStream @in, OutputStream @out)
		{
			TransformXML(tags, fn, @in, new OutputStreamWriter(@out), BuildSaxInterface());
		}

		/// <summary>
		/// Read XML from input stream and write XML to output stream,
		/// while transforming text appearing inside the specified XML tags
		/// by applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .
		/// Note that the <code>Function</code> you supply must be
		/// prepared to accept <code>String</code>s as input; if your
		/// <code>Function</code> doesn't handle <code>String</code>s, you
		/// need to write a wrapper for it that does.
		/// <p><i>Implementation notes:</i> The InputStream is assumed to already
		/// be buffered if useful, and we need a stream, so that the XML decoder
		/// can determine the correct character encoding of the XML file. The output
		/// is to a Writer, and the provided Writer should again be buffered if
		/// desirable.  Internally, this Writer is wrapped as a PrintWriter.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML entity
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>InputStream</code> to read from</param>
		/// <param name="w">the <code>Writer</code> to write to</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, InputStream @in, TextWriter w)
		{
			TransformXML(tags, fn, @in, w, BuildSaxInterface());
		}

		/// <summary>
		/// Calls the fully specified transformXML with an InputSource
		/// constructed from <code>in</code>.
		/// </summary>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, InputStream @in, TextWriter w, TransformXML.SAXInterface<T> handler)
		{
			TransformXML(tags, fn, new InputSource(@in), w, handler);
		}

		/// <summary>
		/// Calls the fully specified transformXML with an InputSource
		/// constructed from <code>in</code>.
		/// </summary>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, Reader @in, TextWriter w, TransformXML.SAXInterface<T> handler)
		{
			TransformXML(tags, fn, new InputSource(@in), w, handler);
		}

		/// <summary>
		/// Read XML from input source and write XML to output writer,
		/// while transforming text appearing inside the specified XML tags
		/// by applying the specified
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// .
		/// Note that the <code>Function</code> you supply must be
		/// prepared to accept <code>String</code>s as input; if your
		/// <code>Function</code> doesn't handle <code>String</code>s, you
		/// need to write a wrapper for it that does.
		/// <br />
		/// <p><i>Implementation notes:</i> The InputSource is assumed to already
		/// be buffered if useful, and we need a stream, so that the XML decoder
		/// can determine the correct character encoding of the XML file.
		/// TODO: does that mean there's a bug if you send it a Reader
		/// instead of an InputStream?  It seems to work with a Reader...
		/// <br />
		/// The output is to a Writer, and the provided Writer should again
		/// be buffered if desirable.  Internally, this Writer is wrapped as
		/// a PrintWriter.
		/// </summary>
		/// <param name="tags">
		/// an array of <code>String</code>s, each an XML entity
		/// within which the transformation should be applied
		/// </param>
		/// <param name="fn">
		/// the
		/// <see cref="Java.Util.Function.IFunction{T, R}"><code>Function</code></see>
		/// to apply
		/// </param>
		/// <param name="in">the <code>InputStream</code> to read from</param>
		/// <param name="w">the <code>Writer</code> to write to</param>
		/// <param name="saxInterface">the sax handler you would like to use (default is SaxInterface, defined in this class, but you may define your own handler)</param>
		public virtual void TransformXML(string[] tags, IFunction<string, T> fn, InputSource @in, TextWriter w, TransformXML.SAXInterface<T> saxInterface)
		{
			saxInterface.outWriter = new PrintWriter(w, true);
			saxInterface.function = fn;
			saxInterface.elementsToBeTransformed = new List<string>();
			Sharpen.Collections.AddAll(saxInterface.elementsToBeTransformed, Arrays.AsList(tags));
			try
			{
				saxParser.Parse(@in, saxInterface);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}
	}
}
