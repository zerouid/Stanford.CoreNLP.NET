using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;







namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>A simple web frontend to the SPIED System.</summary>
	/// <remarks>
	/// A simple web frontend to the SPIED System.
	/// Shamelessly copied from Gabor's Naturali
	/// </remarks>
	/// <author>Sonal</author>
	[System.Serializable]
	public class SPIEDServlet : HttpServlet
	{
		internal Logger logger = Logger.GetAnonymousLogger();

		internal string testPropertiesFile;

		internal IDictionary<string, string> modelNametoDirName;

		/// <summary>Set the properties to the paths they appear at on the servlet.</summary>
		/// <remarks>
		/// Set the properties to the paths they appear at on the servlet.
		/// See build.xml for where these paths get copied.
		/// </remarks>
		/// <exception cref="Javax.Servlet.ServletException">Thrown by the implementation</exception>
		public override void Init()
		{
			testPropertiesFile = GetServletContext().GetRealPath("/WEB-INF/data/test.properties");
			modelNametoDirName = new Dictionary<string, string>();
			modelNametoDirName["food"] = "food";
		}

		/// <summary>Originally extracted from Jettison; copied from http://stackoverflow.com/questions/3020094/how-should-i-escape-strings-in-json</summary>
		/// <param name="string">The string to quote.</param>
		/// <returns>A quoted version of the string, safe to send over the wire.</returns>
		public static string Quote(string @string)
		{
			if (@string == null || @string.Length == 0)
			{
				return "\"\"";
			}
			char c = 0;
			int i;
			int len = @string.Length;
			StringBuilder sb = new StringBuilder(len + 4);
			string t;
			sb.Append('"');
			for (i = 0; i < len; i += 1)
			{
				c = @string[i];
				switch (c)
				{
					case '\\':
					case '"':
					{
						sb.Append('\\');
						sb.Append(c);
						break;
					}

					case '/':
					{
						//                if (b == '<') {
						sb.Append('\\');
						//                }
						sb.Append(c);
						break;
					}

					case '\b':
					{
						sb.Append("\\b");
						break;
					}

					case '\t':
					{
						sb.Append("\\t");
						break;
					}

					case '\n':
					{
						sb.Append("\\n");
						break;
					}

					case '\f':
					{
						sb.Append("\\f");
						break;
					}

					case '\r':
					{
						sb.Append("\\r");
						break;
					}

					default:
					{
						if (c < ' ')
						{
							t = "000" + int.ToHexString(c);
							sb.Append("\\u" + Sharpen.Runtime.Substring(t, t.Length - 4));
						}
						else
						{
							sb.Append(c);
						}
						break;
					}
				}
			}
			sb.Append('"');
			return sb.ToString();
		}

		/// <summary>Actually perform the GET request, given all the relevant information (already sanity checked).</summary>
		/// <remarks>
		/// Actually perform the GET request, given all the relevant information (already sanity checked).
		/// This is the meat of the servlet code.
		/// </remarks>
		/// <param name="out">The writer to write the output to.</param>
		/// <param name="q">The query string.</param>
		/// <exception cref="System.Exception"/>
		private void Run(PrintWriter @out, string q, string seedWords, string model)
		{
			// Clean the string a bit
			q = q.Trim();
			if (q.Length == 0)
			{
				return;
			}
			char lastChar = q[q.Length - 1];
			if (lastChar != '.' && lastChar != '!' && lastChar != '?')
			{
				q = q + ".";
			}
			TextAnnotationPatterns annotate = new TextAnnotationPatterns();
			string quotedString = Quote(q);
			string jsonObject = "{\"input\":" + quotedString + ",\"seedWords\":{\"NAME\":[\"" + StringUtils.Join(seedWords.Split("[,;]"), "\",\"") + "\"]}}";
			bool testmode = true;
			if (Sharpen.Runtime.EqualsIgnoreCase(model, "new"))
			{
				testmode = false;
			}
			logger.Info("Testmode is " + testmode);
			logger.Info("model is " + model);
			string suggestions;
			// Collect results
			if (testmode)
			{
				Properties testProps = new Properties();
				if (testPropertiesFile != null && new File(testPropertiesFile).Exists())
				{
					try
					{
						string props = IOUtils.StringFromFile(testPropertiesFile);
						testProps.Load(new StringReader(props));
					}
					catch (IOException e)
					{
						WriteError(e, @out, "Cannot read test properties file");
						return;
					}
				}
				else
				{
					WriteError(new Exception("test prop file not found"), @out, "Test properties file not found");
					return;
				}
				string modelDir = GetServletContext().GetRealPath("/WEB-INF/data/" + modelNametoDirName[model]);
				testProps.SetProperty("patternsWordsDir", modelDir);
				logger.Info("Reading saved model from " + modelDir);
				string seedWordsFiles = "NAME," + modelDir + "/NAME/seedwords.txt," + modelDir + "/NAME/phrases.txt";
				string modelPropertiesFile = modelDir + "/model.properties";
				logger.Info("Loading model properties from " + modelPropertiesFile);
				string stopWordsFile = modelDir + "/stopwords.txt";
				bool writeOutputFile = false;
				annotate.SetUpProperties(jsonObject, false, writeOutputFile, seedWordsFiles);
				suggestions = annotate.SuggestPhrasesTest(testProps, modelPropertiesFile, stopWordsFile);
			}
			else
			{
				bool writeOutputFile = false;
				annotate.SetUpProperties(jsonObject, false, writeOutputFile, null);
				annotate.ProcessText(writeOutputFile);
				suggestions = annotate.SuggestPhrases();
			}
			@out.Print(suggestions);
		}

		/// <summary><inheritDoc/></summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoGet(IHttpServletRequest request, IHttpServletResponse response)
		{
			logger.Info("GET SPIED query from " + request.GetRemoteAddr());
			if (request.GetCharacterEncoding() == null)
			{
				request.SetCharacterEncoding("utf-8");
			}
			response.SetContentType("text/json; charset=UTF-8");
			PrintWriter @out = response.GetWriter();
			try
			{
				string raw = request.GetParameter("q");
				string seedwords = request.GetParameter("seedwords");
				string model = request.GetParameter("model");
				if (raw == null || string.Empty.Equals(raw))
				{
					@out.Print("{\"okay\":false,\"reason\":\"No data provided\"}");
				}
				else
				{
					Run(@out, raw, seedwords, model);
				}
			}
			catch (Exception t)
			{
				WriteError(t, @out, request.ToString());
			}
			@out.Close();
		}

		internal virtual void WriteError(Exception t, PrintWriter @out, string input)
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			Sharpen.Runtime.PrintStackTrace(t, pw);
			logger.Info("input is " + input);
			logger.Info(sw.ToString());
			@out.Print("{\"okay\":false, \"reason\":\"Something bad happened. Contact the author.\"}");
		}

		/// <summary><inheritDoc/></summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoPost(IHttpServletRequest request, IHttpServletResponse response)
		{
			logger.Info("Responding to the request for SPIED");
			GetServletContext().Log("Responding through SPIED through servlet context!!");
			DoGet(request, response);
		}

		//    logger.info("POST SPIED query from " + request.getRemoteAddr());
		//
		//    //StringBuffer jb = new StringBuffer();
		//    String line = "";
		//    response.setContentType("text/json; charset=UTF-8");
		//    PrintWriter out = response.getWriter();
		//    try {
		//      String raw = StringUtils.toAscii(request.getParameter("q"));
		//      String seedwords = request.getParameter("seedwords");
		////      BufferedReader reader = request.getReader();
		////      while ((line = reader.readLine()) != null)
		////        jb.append(line);
		////      JsonReader jsonReader = Json.createReader(new StringReader(jb.toString()));
		////      JsonObject obj = jsonReader.readObject();
		////      String raw = obj.get("q").toString();
		////      String seedwords = obj.get("seedwords").toString();
		//      line = request.toString();
		//      if (raw == null || "".equals(raw)) {
		//        out.print("{\"okay\":false,\"reason\":\"No data provided\"}");
		//      } else {
		//        run(out, raw, seedwords);
		//      }
		//    } catch (Throwable t) {
		//      writeError(t, out, line);
		//    }
		//
		//    out.close();
		/// <summary>A helper so that we can see how the servlet sees the world, modulo model paths, at least.</summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			SPIEDServlet servlet = new SPIEDServlet();
			servlet.Init();
		}
		//    IOUtils.console(line -> {
		//      StringWriter str = new StringWriter();
		//      PrintWriter out = new PrintWriter(str);
		//      servlet.doGet(new PrintWriter(out), line,"obama");
		//      out.close();
		//      System.out.println(str.toString());
		//    });
	}
}
