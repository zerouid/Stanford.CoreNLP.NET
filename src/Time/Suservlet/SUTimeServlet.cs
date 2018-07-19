using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Javax.Servlet.Http;
using Org.Apache.Commons.Lang3;
using Sharpen;

namespace Edu.Stanford.Nlp.Time.Suservlet
{
	[System.Serializable]
	public class SUTimeServlet : HttpServlet
	{
		private SUTimePipeline pipeline;

		// = null;
		/// <exception cref="Javax.Servlet.ServletException"/>
		public override void Init()
		{
			string dataDir = GetServletContext().GetRealPath("/WEB-INF/data");
			string taggerFilename = dataDir + "/english-left3words-distsim.tagger";
			Properties pipelineProps = new Properties();
			pipelineProps.SetProperty("pos.model", taggerFilename);
			pipeline = new SUTimePipeline(pipelineProps);
			Runtime.SetProperty("de.jollyday.config", GetServletContext().GetRealPath("/WEB-INF/classes/holidays/jollyday.properties"));
		}

		public static bool ParseBoolean(string value)
		{
			if (StringUtils.IsNullOrEmpty(value))
			{
				return false;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(value, "on"))
			{
				return true;
			}
			return bool.ParseBoolean(value);
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

		private string GetRuleFilepaths(params string[] files)
		{
			string rulesDir = GetServletContext().GetRealPath("/WEB-INF/data/rules");
			StringBuilder sb = new StringBuilder();
			foreach (string file in files)
			{
				if (sb.Length > 0)
				{
					sb.Append(",");
				}
				sb.Append(rulesDir + "/" + file);
			}
			return sb.ToString();
		}

		private Properties GetTimeAnnotatorProperties(IHttpServletRequest request)
		{
			// Parses request and set up properties for time annotators
			bool markTimeRanges = ParseBoolean(request.GetParameter("markTimeRanges"));
			bool includeNested = ParseBoolean(request.GetParameter("includeNested"));
			bool includeRange = ParseBoolean(request.GetParameter("includeRange"));
			bool readRules = true;
			string heuristicLevel = request.GetParameter("relativeHeuristicLevel");
			Options.RelativeHeuristicLevel relativeHeuristicLevel = Options.RelativeHeuristicLevel.None;
			if (!StringUtils.IsNullOrEmpty(heuristicLevel))
			{
				relativeHeuristicLevel = Options.RelativeHeuristicLevel.ValueOf(heuristicLevel);
			}
			string ruleFile = null;
			if (readRules)
			{
				string rules = request.GetParameter("rules");
				if (Sharpen.Runtime.EqualsIgnoreCase("English", rules))
				{
					ruleFile = GetRuleFilepaths("defs.sutime.txt", "english.sutime.txt", "english.holidays.sutime.txt");
				}
			}
			// Create properties
			Properties props = new Properties();
			if (markTimeRanges)
			{
				props.SetProperty("sutime.markTimeRanges", "true");
			}
			if (includeNested)
			{
				props.SetProperty("sutime.includeNested", "true");
			}
			if (includeRange)
			{
				props.SetProperty("sutime.includeRange", "true");
			}
			if (ruleFile != null)
			{
				props.SetProperty("sutime.rules", ruleFile);
				props.SetProperty("sutime.binders", "1");
				props.SetProperty("sutime.binder.1", "edu.stanford.nlp.time.JollyDayHolidays");
				props.SetProperty("sutime.binder.1.xml", GetServletContext().GetRealPath("/WEB-INF/data/holidays/Holidays_sutime.xml"));
				props.SetProperty("sutime.binder.1.pathtype", "file");
			}
			props.SetProperty("sutime.teRelHeurLevel", relativeHeuristicLevel.ToString());
			//    props.setProperty("sutime.verbose", "true");
			//    props.setProperty("heideltime.path", getServletContext().getRealPath("/packages/heideltime"));
			//    props.setProperty("gutime.path", getServletContext().getRealPath("/packages/gutime"));
			return props;
		}

		private static void DisplayAnnotation(PrintWriter @out, string query, Annotation anno, bool includeOffsets)
		{
			IList<ICoreMap> timexAnns = anno.Get(typeof(TimeAnnotations.TimexAnnotations));
			IList<string> pieces = new List<string>();
			IList<bool> tagged = new List<bool>();
			int previousEnd = 0;
			foreach (ICoreMap timexAnn in timexAnns)
			{
				int begin = timexAnn.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int end = timexAnn.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				if (begin >= previousEnd)
				{
					pieces.Add(Sharpen.Runtime.Substring(query, previousEnd, begin));
					tagged.Add(false);
					pieces.Add(Sharpen.Runtime.Substring(query, begin, end));
					tagged.Add(true);
					previousEnd = end;
				}
			}
			if (previousEnd < query.Length)
			{
				pieces.Add(Sharpen.Runtime.Substring(query, previousEnd));
				tagged.Add(false);
			}
			@out.Println("<table id='Annotated'><tr><td>");
			for (int i = 0; i < pieces.Count; ++i)
			{
				if (tagged[i])
				{
					@out.Print("<span style=\"background-color: #FF8888\">");
					@out.Print(StringEscapeUtils.EscapeHtml4(pieces[i]));
					@out.Print("</span>");
				}
				else
				{
					@out.Print(StringEscapeUtils.EscapeHtml4(pieces[i]));
				}
			}
			@out.Println("</td></tr></table>");
			@out.Println("<h3>Temporal Expressions</h3>");
			if (timexAnns.Count > 0)
			{
				@out.Println("<table>");
				@out.Println("<tr><th>Text</th><th>Value</th>");
				if (includeOffsets)
				{
					@out.Println("<th>Char Begin</th><th>Char End</th><th>Token Begin</th><th>Token End</th>");
				}
				@out.Println("<th>Timex3 Tag</th></tr>");
				foreach (ICoreMap timexAnn_1 in timexAnns)
				{
					@out.Println("<tr>");
					Timex timex = timexAnn_1.Get(typeof(TimeAnnotations.TimexAnnotation));
					int begin = timexAnn_1.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int end = timexAnn_1.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					@out.Print("<td>" + StringEscapeUtils.EscapeHtml4(Sharpen.Runtime.Substring(query, begin, end)) + "</td>");
					@out.Print("<td>" + ((timex.Value() != null) ? StringEscapeUtils.EscapeHtml4(timex.Value()) : string.Empty) + "</td>");
					if (includeOffsets)
					{
						@out.Print("<td>" + begin + "</td>");
						@out.Print("<td>" + end + "</td>");
						@out.Print("<td>" + timexAnn_1.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) + "</td>");
						@out.Print("<td>" + timexAnn_1.Get(typeof(CoreAnnotations.TokenEndAnnotation)) + "</td>");
					}
					@out.Print("<td>" + StringEscapeUtils.EscapeHtml4(timex.ToString()) + "</td>");
					@out.Println("</tr>");
				}
				@out.Println("</table>");
			}
			else
			{
				@out.Println("<em>No temporal expressions.</em>");
			}
			@out.Println("<h3>POS Tags</h3>");
			@out.Println("<table><tr><td>");
			foreach (ICoreMap sentence in anno.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				foreach (CoreLabel token in tokens)
				{
					string tokenOutput = StringEscapeUtils.EscapeHtml4(token.Word() + "/" + token.Tag());
					@out.Print(tokenOutput + " ");
				}
				@out.Println("<br>");
			}
			@out.Println("</td></tr></table>");
		}

		/// <exception cref="System.IO.IOException"/>
		private void AddResults(IHttpServletRequest request, IHttpServletResponse response)
		{
			// if we can't handle UTF-8, need to do something like this...
			//String originalQuery = request.getParameter("q");
			//String query = WebappUtil.convertString(originalQuery);
			string query = request.GetParameter("q");
			string dateString = request.GetParameter("d");
			// TODO: this always returns true...
			bool dateError = !pipeline.IsDateOkay(dateString);
			bool includeOffsets = ParseBoolean(request.GetParameter("includeOffsets"));
			PrintWriter @out = response.GetWriter();
			if (dateError)
			{
				@out.Println("<br><br>Warning: unparseable date " + StringEscapeUtils.EscapeHtml4(dateString));
			}
			if (!StringUtils.IsNullOrEmpty(query))
			{
				Properties props = GetTimeAnnotatorProperties(request);
				string annotatorType = request.GetParameter("annotator");
				if (annotatorType == null)
				{
					annotatorType = "sutime";
				}
				IAnnotator timeAnnotator = pipeline.GetTimeAnnotator(annotatorType, props);
				if (timeAnnotator != null)
				{
					Annotation anno = pipeline.Process(query, dateString, timeAnnotator);
					@out.Println("<h3>Annotated Text</h3> <em>(tagged using " + annotatorType + "</em>)");
					DisplayAnnotation(@out, query, anno, includeOffsets);
				}
				else
				{
					@out.Println("<br><br>Error creating annotator for " + StringEscapeUtils.EscapeHtml4(annotatorType));
				}
			}
		}

		private const long serialVersionUID = 1L;
	}
}
