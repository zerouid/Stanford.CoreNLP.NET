using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;






using Org.Apache.Commons.Lang3;


namespace Edu.Stanford.Nlp.IE.Ner.Webapp
{
	/// <summary>This is a servlet interface to the CRFClassifier.</summary>
	/// <author>Dat Hoang 2011</author>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class NERServlet : HttpServlet
	{
		private const long serialVersionUID = 1584102147050497227L;

		private string format;

		private bool spacing;

		private string defaultClassifier;

		private IList<string> classifiers = new List<string>();

		private IDictionary<string, CRFClassifier<ICoreMap>> ners;

		private const int MaximumQueryLength = 3000;

		/// <exception cref="Javax.Servlet.ServletException"/>
		public override void Init()
		{
			format = GetServletConfig().GetInitParameter("outputFormat");
			if (format == null || format.Trim().IsEmpty())
			{
				throw new ServletException("Invalid outputFormat setting.");
			}
			string spacingStr = GetServletConfig().GetInitParameter("preserveSpacing");
			if (spacingStr == null || spacingStr.Trim().IsEmpty())
			{
				throw new ServletException("Invalid preserveSpacing setting.");
			}
			//spacing = Boolean.valueOf(spacingStr).booleanValue();
			spacingStr = spacingStr.Trim().ToLower();
			spacing = "true".Equals(spacingStr);
			string path = GetServletContext().GetRealPath("/WEB-INF/data/models");
			foreach (string classifier in new File(path).List())
			{
				classifiers.Add(classifier);
			}
			// TODO: get this from somewhere more interesting?
			defaultClassifier = classifiers[0];
			foreach (string classifier_1 in classifiers)
			{
				Log(classifier_1);
			}
			ners = Generics.NewHashMap();
			foreach (string classifier_2 in classifiers)
			{
				CRFClassifier model = null;
				string filename = "/WEB-INF/data/models/" + classifier_2;
				InputStream @is = GetServletConfig().GetServletContext().GetResourceAsStream(filename);
				if (@is == null)
				{
					throw new ServletException("File not found. Filename = " + filename);
				}
				try
				{
					if (filename.EndsWith(".gz"))
					{
						@is = new BufferedInputStream(new GZIPInputStream(@is));
					}
					else
					{
						@is = new BufferedInputStream(@is);
					}
					model = CRFClassifier.GetClassifier(@is);
				}
				catch (IOException)
				{
					throw new ServletException("IO problem reading classifier.");
				}
				catch (InvalidCastException)
				{
					throw new ServletException("Classifier class casting problem.");
				}
				catch (TypeLoadException)
				{
					throw new ServletException("Classifier class not found problem.");
				}
				finally
				{
					IOUtils.CloseIgnoringExceptions(@is);
				}
				ners[classifier_2] = model;
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
			request.SetAttribute("classifiers", classifiers);
			this.GetServletContext().GetRequestDispatcher("/ner.jsp").Include(request, response);
			AddResults(request, response);
			this.GetServletContext().GetRequestDispatcher("/footer.jsp").Include(request, response);
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoPost(IHttpServletRequest request, IHttpServletResponse response)
		{
			DoGet(request, response);
		}

		/// <exception cref="System.IO.IOException"/>
		private void AddResults(IHttpServletRequest request, IHttpServletResponse response)
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
				@out.Print("This query is too long.  If you want to run very long queries, please download and use our <a href=\"http://nlp.stanford.edu/software/CRF-NER.html\">publicly released distribution</a>.");
				return;
			}
			string outputFormat = request.GetParameter("outputFormat");
			if (outputFormat == null || outputFormat.Trim().IsEmpty())
			{
				outputFormat = this.format;
			}
			bool preserveSpacing;
			string preserveSpacingStr = request.GetParameter("preserveSpacing");
			if (preserveSpacingStr == null || preserveSpacingStr.Trim().IsEmpty())
			{
				preserveSpacing = this.spacing;
			}
			else
			{
				preserveSpacingStr = preserveSpacingStr.Trim();
				preserveSpacing = bool.ValueOf(preserveSpacingStr);
			}
			string classifier = request.GetParameter("classifier");
			if (classifier == null || classifier.Trim().IsEmpty())
			{
				classifier = this.defaultClassifier;
			}
			response.AddHeader("classifier", classifier);
			response.AddHeader("outputFormat", outputFormat);
			response.AddHeader("preserveSpacing", preserveSpacing.ToString());
			if (outputFormat.Equals("highlighted"))
			{
				OutputHighlighting(@out, ners[classifier], input);
			}
			else
			{
				@out.Print(StringEscapeUtils.EscapeHtml4(ners[classifier].ClassifyToString(input, outputFormat, preserveSpacing)));
			}
		}

		private static void OutputHighlighting(PrintWriter @out, CRFClassifier<ICoreMap> classifier, string input)
		{
			ICollection<string> labels = classifier.Labels();
			string background = classifier.BackgroundSymbol();
			IList<IList<ICoreMap>> sentences = classifier.Classify(input);
			IDictionary<string, Color> tagToColorMap = NERGUI.MakeTagToColorMap(labels, background);
			StringBuilder result = new StringBuilder();
			int lastEndOffset = 0;
			foreach (IList<ICoreMap> sentence in sentences)
			{
				foreach (ICoreMap word in sentence)
				{
					int beginOffset = word.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int endOffset = word.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					string answer = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
					if (beginOffset > lastEndOffset)
					{
						result.Append(StringEscapeUtils.EscapeHtml4(Sharpen.Runtime.Substring(input, lastEndOffset, beginOffset)));
					}
					// Add a color bar for any tagged words
					if (!background.Equals(answer))
					{
						Color color = tagToColorMap[answer];
						result.Append("<span style=\"color:#ffffff;background:" + NERGUI.ColorToHTML(color) + "\">");
					}
					result.Append(StringEscapeUtils.EscapeHtml4(Sharpen.Runtime.Substring(input, beginOffset, endOffset)));
					// Turn off the color bar
					if (!background.Equals(answer))
					{
						result.Append("</span>");
					}
					lastEndOffset = endOffset;
				}
			}
			if (lastEndOffset < input.Length)
			{
				result.Append(StringEscapeUtils.EscapeHtml4(Sharpen.Runtime.Substring(input, lastEndOffset)));
			}
			result.Append("<br><br>");
			result.Append("Potential tags:");
			foreach (KeyValuePair<string, Color> stringColorEntry in tagToColorMap)
			{
				result.Append("<br>&nbsp;&nbsp;");
				Color color = stringColorEntry.Value;
				result.Append("<span style=\"color:#ffffff;background:" + NERGUI.ColorToHTML(color) + "\">");
				result.Append(StringEscapeUtils.EscapeHtml4(stringColorEntry.Key));
				result.Append("</span>");
			}
			@out.Print(result);
		}
	}
}
