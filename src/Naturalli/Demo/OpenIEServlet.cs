using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Javax.Servlet.Http;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli.Demo
{
	/// <summary>A simple web frontend to the Open IE System.</summary>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class OpenIEServlet : HttpServlet
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(OpenIEServlet));

		internal StanfordCoreNLP pipeline = null;

		internal StanfordCoreNLP backoff = null;

		/// <summary>Set the properties to the paths they appear at on the servlet.</summary>
		/// <remarks>
		/// Set the properties to the paths they appear at on the servlet.
		/// See build.xml for where these paths get copied.
		/// </remarks>
		/// <exception cref="Javax.Servlet.ServletException">Thrown by the implementation</exception>
		public override void Init()
		{
			Properties commonProps = new _Properties_43();
			try
			{
				string dataDir = GetServletContext().GetRealPath("/WEB-INF/data");
				Runtime.SetProperty("de.jollyday.config", GetServletContext().GetRealPath("/WEB-INF/classes/holidays/jollyday.properties"));
				commonProps.SetProperty("pos.model", dataDir + "/english-left3words-distsim.tagger");
				commonProps.SetProperty("ner.model", dataDir + "/english.all.3class.distsim.crf.ser.gz," + dataDir + "/english.conll.4class.distsim.crf.ser.gz," + dataDir + "/english.muc.7class.distsim.crf.ser.gz");
				commonProps.SetProperty("depparse.model", dataDir + "/english_SD.gz");
				commonProps.SetProperty("parse.model", dataDir + "/englishPCFG.ser.gz");
				commonProps.SetProperty("sutime.rules", dataDir + "/defs.sutime.txt," + dataDir + "/english.sutime.txt," + dataDir + "/english.hollidays.sutime.txt");
				commonProps.SetProperty("openie.splitter.model", dataDir + "/clauseSplitterModel.ser.gz");
				commonProps.SetProperty("openie.affinity_models", dataDir);
			}
			catch (ArgumentNullException)
			{
				log.Info("Could not load servlet context. Are you on the command line?");
			}
			if (this.pipeline == null)
			{
				Properties fullProps = new Properties(commonProps);
				fullProps.SetProperty("annotators", "tokenize,ssplit,pos,lemma,depparse,ner,natlog,openie");
				this.pipeline = new StanfordCoreNLP(fullProps);
			}
			if (this.backoff == null)
			{
				Properties backoffProps = new Properties(commonProps);
				backoffProps.SetProperty("annotators", "parse,natlog,openie");
				backoffProps.SetProperty("enforceRequirements", "false");
				this.backoff = new StanfordCoreNLP(backoffProps);
			}
		}

		private sealed class _Properties_43 : Properties
		{
			public _Properties_43()
			{
				{
					this.SetProperty("depparse.extradependencies", "ref_only_uncollapsed");
					this.SetProperty("parse.extradependencies", "ref_only_uncollapsed");
					this.SetProperty("openie.splitter.threshold", "0.10");
					this.SetProperty("openie.optimze_for", "GENERAL");
					this.SetProperty("openie.ignoreaffinity", "false");
					this.SetProperty("openie.max_entailments_per_clause", "1000");
					this.SetProperty("openie.triple.strict", "true");
				}
			}
		}

		/// <summary>Annotate a document (which is usually just a sentence).</summary>
		public virtual void Annotate(StanfordCoreNLP pipeline, Annotation ann)
		{
			if (ann.Get(typeof(CoreAnnotations.SentencesAnnotation)) == null)
			{
				pipeline.Annotate(ann);
			}
			else
			{
				if (ann.Get(typeof(CoreAnnotations.SentencesAnnotation)).Count == 1)
				{
					ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
					foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						token.Remove(typeof(NaturalLogicAnnotations.OperatorAnnotation));
						token.Remove(typeof(NaturalLogicAnnotations.PolarityAnnotation));
					}
					sentence.Remove(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation));
					sentence.Remove(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation));
					sentence.Remove(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					sentence.Remove(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					sentence.Remove(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation));
					pipeline.Annotate(ann);
				}
			}
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

		private void RunWithPipeline(StanfordCoreNLP pipeline, Annotation ann, ICollection<string> triples, ICollection<string> entailments)
		{
			// Annotate
			Annotate(pipeline, ann);
			// Extract info
			foreach (ICoreMap sentence in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (SentenceFragment fragment in sentence.Get(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation)))
				{
					entailments.Add(Quote(fragment.ToString()));
				}
				foreach (RelationTriple fragment_1 in sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation)))
				{
					triples.Add("[ " + Quote(fragment_1.SubjectGloss()) + ", " + Quote(fragment_1.RelationGloss()) + ", " + Quote(fragment_1.ObjectGloss()) + " ]");
				}
			}
		}

		/// <summary>Actually perform the GET request, given all the relevant information (already sanity checked).</summary>
		/// <remarks>
		/// Actually perform the GET request, given all the relevant information (already sanity checked).
		/// This is the meat of the servlet code.
		/// </remarks>
		/// <param name="out">The writer to write the output to.</param>
		/// <param name="q">The query string.</param>
		private void DoGet(PrintWriter @out, string q)
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
			// Annotate
			Annotation ann = new Annotation(q);
			try
			{
				// Collect results
				ICollection<string> entailments = new HashSet<string>();
				ICollection<string> triples = new LinkedHashSet<string>();
				RunWithPipeline(pipeline, ann, triples, entailments);
				// pipeline must come before backoff
				if (triples.Count == 0)
				{
					RunWithPipeline(backoff, ann, triples, entailments);
				}
				// backoff must come after pipeline
				// Write results
				@out.Println("{ " + "\"ok\":true, " + "\"entailments\": [" + StringUtils.Join(entailments, ",") + "], " + "\"triples\": [" + StringUtils.Join(triples, ",") + "], " + "\"msg\": \"\"" + " }");
			}
			catch (Exception t)
			{
				@out.Println("{ok:false, entailments:[], triples:[], msg:" + Quote(t.Message) + "}");
			}
		}

		/// <summary><inheritDoc/></summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoGet(IHttpServletRequest request, IHttpServletResponse response)
		{
			if (request.GetCharacterEncoding() == null)
			{
				request.SetCharacterEncoding("utf-8");
			}
			response.SetContentType("text/json; charset=UTF-8");
			PrintWriter @out = response.GetWriter();
			string raw = request.GetParameter("q");
			if (raw == null || string.Empty.Equals(raw))
			{
				@out.Println("{ok:false, entailments:[], triples=[], msg=\"\"}");
			}
			else
			{
				DoGet(@out, raw);
			}
			@out.Close();
		}

		/// <summary><inheritDoc/></summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		protected override void DoPost(IHttpServletRequest request, IHttpServletResponse response)
		{
			DoGet(request, response);
		}

		/// <summary>A helper so that we can see how the servlet sees the world, modulo model paths, at least.</summary>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			OpenIEServlet servlet = new OpenIEServlet();
			servlet.Init();
			IOUtils.Console(null);
		}
	}
}
