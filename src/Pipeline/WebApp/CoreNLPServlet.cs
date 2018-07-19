using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Pipeline;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Javax.Servlet;
using Javax.Servlet.Http;
using NU.Xom;
using NU.Xom.Xslt;
using Org.Apache.Commons.Lang3;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline.Webapp
{
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class CoreNLPServlet : HttpServlet
	{
		private const long serialVersionUID = 1L;

		private StanfordCoreNLP pipeline;

		private XSLTransform corenlpTransformer;

		private string defaultFormat = "pretty";

		private const int MaximumQueryLength = 4096;

		/// <exception cref="Javax.Servlet.ServletException"/>
		public override void Init()
		{
			pipeline = new StanfordCoreNLP();
			string xslPath = GetServletContext().GetRealPath("/WEB-INF/data/CoreNLP-to-HTML.xsl");
			try
			{
				Builder builder = new Builder();
				Document stylesheet = builder.Build(new File(xslPath));
				corenlpTransformer = new XSLTransform(stylesheet);
			}
			catch (Exception e)
			{
				throw new ServletException(e);
			}
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoGet(IHttpServletRequest request, IHttpServletResponse response)
		{
			if (request.GetCharacterEncoding() == null)
			{
				request.SetCharacterEncoding("utf-8");
			}
			response.SetContentType("text/html; charset=UTF-8");
			this.GetServletContext().GetRequestDispatcher("/header.jsp").Include(request, response);
			AddResults(request, response);
			this.GetServletContext().GetRequestDispatcher("/footer.jsp").Include(request, response);
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoPost(IHttpServletRequest request, IHttpServletResponse response)
		{
			DoGet(request, response);
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual void AddResults(IHttpServletRequest request, IHttpServletResponse response)
		{
			string input = request.GetParameter("input");
			if (input == null)
			{
				return;
			}
			input = input.Trim();
			if (input.IsEmpty())
			{
				return;
			}
			PrintWriter @out = response.GetWriter();
			if (input.Length > MaximumQueryLength)
			{
				@out.Print("<div>This query is too long.  If you want to run very long queries, please download and use our <a href=\"http://nlp.stanford.edu/software/corenlp.html\">publicly released distribution</a>.</div>");
				return;
			}
			Annotation annotation = new Annotation(input);
			pipeline.Annotate(annotation);
			string outputFormat = request.GetParameter("outputFormat");
			if (outputFormat == null || outputFormat.Trim().IsEmpty())
			{
				outputFormat = this.defaultFormat;
			}
			switch (outputFormat)
			{
				case "xml":
				{
					OutputXml(@out, annotation);
					break;
				}

				case "json":
				{
					OutputJson(@out, annotation);
					break;
				}

				case "conll":
				{
					OutputCoNLL(@out, annotation);
					break;
				}

				case "pretty":
				{
					OutputPretty(@out, annotation);
					break;
				}

				default:
				{
					OutputVisualise(@out, annotation);
					break;
				}
			}
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual void OutputVisualise(PrintWriter @out, Annotation annotation)
		{
			// Note: A lot of the HTML generation in this method could/should be
			// done at a templating level, but as-of-yet I am not entirely sure how
			// this should be done in jsp. Also, a lot of the HTML is unnecessary
			// for the other outputs such as pretty print and XML.
			// Div for potential error messages when fetching the configuration.
			@out.Println("<div id=\"config_error\">");
			@out.Println("</div>");
			// Insert divs that will be used for each visualisation type.
			int visualiserDivPxWidth = 700;
			IDictionary<string, string> nameByAbbrv = new LinkedHashMap<string, string>();
			nameByAbbrv["pos"] = "Part-of-Speech";
			nameByAbbrv["ner"] = "Named Entity Recognition";
			nameByAbbrv["coref"] = "Coreference";
			nameByAbbrv["basic_dep"] = "Basic Dependencies";
			//nameByAbbrv.put("collapsed_dep", "Collapsed dependencies");
			nameByAbbrv["collapsed_ccproc_dep"] = "Enhanced Dependencies";
			foreach (KeyValuePair<string, string> entry in nameByAbbrv)
			{
				@out.Println("<h2>" + entry.Value + ":</h2>");
				@out.Println("<div id=\"" + entry.Key + "\" style=\"width:" + visualiserDivPxWidth + "px\">");
				@out.Println("    <div id=\"" + entry.Key + "_loading\">");
				@out.Println("        <p>Loading...</p>");
				@out.Println("    </div>");
				@out.Println("</div>");
				@out.Println(string.Empty);
			}
			// Time to get the XML data into HTML.
			StringWriter xmlOutput = new StringWriter();
			pipeline.XmlPrint(annotation, xmlOutput);
			xmlOutput.Flush();
			// Escape the XML to be embeddable into a Javascript string.
			string escapedXml = xmlOutput.ToString().ReplaceAll("\\r\\n|\\r|\\n", string.Empty).Replace("\"", "\\\"");
			// Inject the XML results into the HTML to be retrieved by the Javascript.
			@out.Println("<script type=\"text/javascript\">");
			@out.Println("// <![CDATA[");
			@out.Println("    stanfordXML = \"" + escapedXml + "\";");
			@out.Println("// ]]>");
			@out.Println("</script>");
			// Relative brat installation location to CoreNLP.
			string bratLocation = "../brat";
			// Inject the location variable, we need it in Javascript mode.
			@out.Println("<script type=\"text/javascript\">");
			@out.Println("// <![CDATA[");
			@out.Println("    bratLocation = \"" + bratLocation + "\";");
			@out.Println("    webFontURLs = [\n" + "        '" + bratLocation + "/static/fonts/Astloch-Bold.ttf',\n" + "        '" + bratLocation + "/static/fonts/PT_Sans-Caption-Web-Regular.ttf',\n" + "        '" + bratLocation + "/static/fonts/Liberation_Sans-Regular.ttf'];"
				);
			@out.Println("// ]]>");
			@out.Println("</script>");
			// Inject the brat stylesheet (removing this line breaks visualisation).
			@out.Println("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + bratLocation + "/style-vis.css\"/>");
			// Include the Javascript libraries necessary to run brat.
			@out.Println("<script type=\"text/javascript\" src=\"" + bratLocation + "/client/lib/head.load.min.js\"></script>");
			// Main Javascript that hooks into all that we have introduced so far.
			@out.Println("<script type=\"text/javascript\" src=\"brat.js\"></script>");
			// Link to brat, I hope this is okay to have here...
			@out.Println("<h>Visualisation provided using the " + "<a href=\"http://brat.nlplab.org/\">brat " + "visualisation/annotation software</a>.</h>");
			@out.Println("<br/>");
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		public virtual void OutputPretty(PrintWriter @out, Annotation annotation)
		{
			try
			{
				Document input = XMLOutputter.AnnotationToDoc(annotation, pipeline);
				Nodes output = corenlpTransformer.Transform(input);
				for (int i = 0; i < output.Size(); i++)
				{
					@out.Print(output.Get(i).ToXML());
				}
			}
			catch (Exception e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new ServletException(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static void OutputByWriter(IConsumer<StringWriter> printer, PrintWriter @out)
		{
			StringWriter output = new StringWriter();
			printer.Accept(output);
			output.Flush();
			string escapedXml = StringEscapeUtils.EscapeHtml4(output.ToString());
			string[] lines = escapedXml.Split("\n");
			@out.Print("<div><pre>");
			foreach (string line in lines)
			{
				int numSpaces = 0;
				while (numSpaces < line.Length && line[numSpaces] == ' ')
				{
					@out.Print("&nbsp;");
					++numSpaces;
				}
				@out.Print(Sharpen.Runtime.Substring(line, numSpaces));
				@out.Print("\n");
			}
			@out.Print("</pre></div>");
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void OutputXml(PrintWriter @out, Annotation annotation)
		{
			OutputByWriter(null, @out);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void OutputJson(PrintWriter @out, Annotation annotation)
		{
			OutputByWriter(null, @out);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void OutputCoNLL(PrintWriter @out, Annotation annotation)
		{
			OutputByWriter(null, @out);
		}
	}
}
