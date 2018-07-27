using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;






namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Annotates text using HeidelTime.</summary>
	/// <remarks>
	/// Annotates text using HeidelTime.
	/// The main difference from edu.stanford.nlp.time.HeidelTimeAnnotator is that
	/// we handle XML documents that are common in KBP.
	/// GUTIME/TimeML specifications can be found at:
	/// <a href="http://www.timeml.org/site/tarsqi/modules/gutime/index.html">
	/// http://www.timeml.org/site/tarsqi/modules/gutime/index.html</a>.
	/// </remarks>
	/// <author>Arun Chaganty</author>
	public class HeidelTimeKBPAnnotator : IAnnotator
	{
		private const string BasePath = "$NLP_DATA_HOME/packages/heideltime/";

		private static readonly string DefaultPath = DataFilePaths.Convert(BasePath);

		private readonly File heideltimePath;

		private readonly bool outputResults;

		private readonly string language;

		private readonly HeidelTimeKBPAnnotator.HeidelTimeOutputReader outputReader = new HeidelTimeKBPAnnotator.HeidelTimeOutputReader();

		public const string HeideltimePathProperty = "heideltime.path";

		public const string HeideltimeLanguageProperty = "heideltime.language";

		public const string HeideltimeOutputResults = "heideltime.outputResults";

		public HeidelTimeKBPAnnotator()
			: this(new File(Runtime.GetProperty("heideltime", DefaultPath)))
		{
		}

		public HeidelTimeKBPAnnotator(File heideltimePath)
			: this(heideltimePath, "english", false)
		{
		}

		public HeidelTimeKBPAnnotator(File heideltimePath, string language, bool outputResults)
		{
			// This could probably be fixed in newer HeidelTime versions, which even support using our tagger.
			// if used in a pipeline or constructed with a Properties object,
			// this property tells the annotator where to find the script
			this.heideltimePath = heideltimePath;
			this.outputResults = outputResults;
			this.language = language;
		}

		public HeidelTimeKBPAnnotator(string name, Properties props)
		{
			this.heideltimePath = new File(props.GetProperty(HeideltimePathProperty, Runtime.GetProperty("heideltime", DefaultPath)));
			this.outputResults = bool.ValueOf(props.GetProperty(HeideltimeOutputResults, "false"));
			this.language = props.GetProperty(HeideltimeLanguageProperty, "english");
		}

		//    this.tagList = Arrays.asList(props.getProperty("clean.xmltags", "").toLowerCase().split("\\|"))
		//        .stream().filter(x -> x.length() > 0)
		//        .collect(Collectors.toList());
		public virtual void Annotate(Annotation annotation)
		{
			try
			{
				this.Annotate((ICoreMap)annotation);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private sealed class _Dictionary_80 : Dictionary<string, string>
		{
			public _Dictionary_80()
			{
				{
					this["*NL*"] = "\n";
				}
			}
		}

		private static readonly IDictionary<string, string> Translate = new _Dictionary_80();

		/// <exception cref="System.IO.IOException"/>
		public virtual void Annotate(ICoreMap document)
		{
			try
			{
				//--Create Input File
				//(create file)
				File inputFile = File.CreateTempFile("heideltime", ".input");
				//(write to file)
				PrintWriter inputWriter = new PrintWriter(inputFile);
				PrepareHeidelTimeInput(inputWriter, document);
				inputWriter.Close();
				Optional<string> pubDate = GetPubDate(document);
				//--Build Command
				IList<string> args = new List<string>(Arrays.AsList("java", "-jar", this.heideltimePath.GetPath() + "/heideltime.jar", "-c", this.heideltimePath.GetPath() + "/config.props", "-l", this.language, "-t", "NEWS"));
				if (pubDate.IsPresent())
				{
					args.Add("-dct");
					args.Add(pubDate.Get());
				}
				args.Add(inputFile.GetPath());
				// run HeidelTime on the input file
				ProcessBuilder process = new ProcessBuilder(args);
				StringWriter outputWriter = new StringWriter();
				SystemUtils.Run(process, outputWriter, null);
				string output = outputWriter.GetBuffer().ToString();
				IList<ICoreMap> timexAnns = outputReader.Process(document, output);
				document.Set(typeof(TimeAnnotations.TimexAnnotations), timexAnns);
				if (outputResults)
				{
					System.Console.Out.WriteLine(timexAnns);
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				System.Console.Error.WriteLine("error running HeidelTime on this doc: " + document.Get(typeof(CoreAnnotations.DocIDAnnotation)));
			}
		}

		//      throw e;
		private Optional<string> GetPubDate(ICoreMap document)
		{
			//--Get Date
			//(error checks)
			if (!document.ContainsKey(typeof(CoreAnnotations.CalendarAnnotation)) && !document.ContainsKey(typeof(CoreAnnotations.DocDateAnnotation)))
			{
				throw new ArgumentException("CoreMap must have either a Calendar or DocDate annotation");
			}
			//not strictly necessary, technically...
			//(variables)
			Calendar dateCalendar = document.Get(typeof(CoreAnnotations.CalendarAnnotation));
			if (dateCalendar != null)
			{
				//(case: calendar annotation)
				return Optional.Of(string.Format("%TF", dateCalendar));
			}
			else
			{
				//(case: docdateannotation)
				string s = document.Get(typeof(CoreAnnotations.DocDateAnnotation));
				if (s != null)
				{
					return Optional.Of(s);
				}
			}
			return Optional.Empty();
		}

		private void PrepareHeidelTimeInput(PrintWriter stream, ICoreMap document)
		{
			// We really should use the full text annotation because our cleanxml can be useless.
			foreach (ICoreMap sentence in document.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					string text = token.OriginalText();
					stream.Append(Translate.GetOrDefault(text, text));
					// HACK: will not handle contractions like "del = de + el" properly -- will be deel.
					// stream.append(token.after().length() > 0 ? " " : "");
					// HACK: will not handle things like 12-abr-2011 which are chunked up properly into 12 - abr-2011.
					stream.Append(" ");
				}
				stream.Append("\n");
			}
		}

		internal class HeidelTimeOutputReader
		{
			public class Node
			{
				public readonly string contents;

				public readonly int start;

				public readonly int end;

				public Node(string contents, int start, int end)
				{
					this.contents = contents;
					this.start = start;
					this.end = end;
				}

				public override string ToString()
				{
					return "[" + contents + "]";
				}
			}

			public class TimexNode : HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node
			{
				public readonly Timex timex;

				public TimexNode(string contents, int start, int end, Timex timex)
					: base(contents, start, end)
				{
					this.timex = timex;
				}

				public override string ToString()
				{
					return "[" + contents + "|" + "TIMEX:" + timex + "]";
				}
			}

			internal Pattern timeMLOpen = Pattern.Compile(".*<TimeML>", Pattern.Dotall);

			internal Pattern timeMLClose = Pattern.Compile("</TimeML>.*", Pattern.Dotall);

			internal Pattern timexTagOpen = Pattern.Compile("<TIMEX3\\s*(?:(?:[a-z]+)=\"(?:[^\"]+)\"\\s*)*>");

			internal Pattern attr = Pattern.Compile("(?<key>[a-z]+)=\"(?<value>[^\"]+)\"");

			internal Pattern timexTagClose = Pattern.Compile("</TIMEX3>");

			public virtual IList<ICoreMap> Process(ICoreMap document, string output)
			{
				List<ICoreMap> ret = new List<ICoreMap>();
				IList<ICoreMap> sentences = document.Get(typeof(CoreAnnotations.SentencesAnnotation));
				IList<CoreLabel> tokens = document.Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node> nodes = ToNodeSequence(output);
				int tokenIdx = 0;
				int nodeIdx = 0;
				string partial = string.Empty;
				// Things that are left over from previous partially matched tokens.
				foreach (HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node node in nodes)
				{
					// Get tokens.
					string text = node.contents.Trim();
					while (tokens[tokenIdx].Word().Equals("*NL*") && tokenIdx < tokens.Count)
					{
						tokenIdx += 1;
					}
					// Skip past stupid *NL* tags.
					int tokenEndIdx = tokenIdx;
					foreach (CoreLabel token in tokens.SubList(tokenIdx, tokens.Count))
					{
						if (text.Length == 0)
						{
							break;
						}
						tokenEndIdx++;
						string matchStr = token.OriginalText().Trim();
						// This is necessarily in the middle.
						if (Objects.Equals(matchStr, "*NL*"))
						{
							continue;
						}
						// This is one weird case where JavaNLP has a whitespace token.
						if ((partial + text).StartsWith(matchStr))
						{
							text = Sharpen.Runtime.Substring(text, matchStr.Length - partial.Length).Trim();
							partial = string.Empty;
						}
						else
						{
							// And clear partial.
							if (matchStr.StartsWith(partial + text))
							{
								// uh oh we have a partial match.
								partial = Sharpen.Runtime.Substring(matchStr, 0, partial.Length + text.Length);
								// we need to remember what we matched earlier.
								text = string.Empty;
							}
							else
							{
								// This should never happen.
								System.Diagnostics.Debug.Assert(false);
							}
						}
					}
					// Only process time nodes if they span the same sentence.
					if (node is HeidelTimeKBPAnnotator.HeidelTimeOutputReader.TimexNode && tokens[tokenIdx].SentIndex() == tokens[tokenEndIdx - 1].SentIndex())
					{
						HeidelTimeKBPAnnotator.HeidelTimeOutputReader.TimexNode timexNode = (HeidelTimeKBPAnnotator.HeidelTimeOutputReader.TimexNode)node;
						ICoreMap sentence = sentences[tokens[tokenIdx].SentIndex()];
						ret.Add(MakeTimexMap(timexNode, tokens.SubList(tokenIdx, tokenEndIdx), sentence));
					}
					if (partial.Length > 0)
					{
						tokenIdx = tokenEndIdx - 1;
					}
					else
					{
						// Move back a token because this is actually shared between the two nodes.
						tokenIdx = tokenEndIdx;
					}
					nodeIdx++;
				}
				return ret;
			}

			private ICoreMap MakeTimexMap(HeidelTimeKBPAnnotator.HeidelTimeOutputReader.TimexNode node, IList<CoreLabel> tokens, ICoreMap sentence)
			{
				ICoreMap timexMap = new ArrayCoreMap();
				timexMap.Set(typeof(TimeAnnotations.TimexAnnotation), node.timex);
				timexMap.Set(typeof(CoreAnnotations.TextAnnotation), node.contents);
				timexMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), BeginOffset(tokens[0]));
				timexMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), EndOffset(tokens[tokens.Count - 1]));
				timexMap.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokens[0].Index());
				timexMap.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokens[tokens.Count - 1].Index());
				timexMap.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				if (sentence.Get(typeof(TimeAnnotations.TimexAnnotations)) == null)
				{
					sentence.Set(typeof(TimeAnnotations.TimexAnnotations), new List<ICoreMap>());
				}
				sentence.Get(typeof(TimeAnnotations.TimexAnnotations)).Add(timexMap);
				// update NER for tokens
				foreach (CoreLabel token in tokens)
				{
					token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
					token.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), node.timex.Value());
					token.Set(typeof(TimeAnnotations.TimexAnnotation), node.timex);
				}
				return timexMap;
			}

			private IList<HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node> ToNodeSequence(string output)
			{
				// First of all, get rid of all XML markup that HeidelTime inserts.
				output = timeMLOpen.Matcher(output).ReplaceAll(string.Empty).Trim();
				output = timeMLClose.Matcher(output).ReplaceAll(string.Empty).Trim();
				// Now go through and chunk sequence into <TIMEX3> tag regions.
				Matcher openMatcher = timexTagOpen.Matcher(output);
				Matcher attrMatcher = attr.Matcher(output);
				Matcher closeMatcher = timexTagClose.Matcher(output);
				IList<HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node> ret = new List<HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node>();
				// TODO: save metadata of TIMEX token positions or stuff.
				int charIdx = 0;
				Dictionary<string, string> attrs = new Dictionary<string, string>();
				while (openMatcher.Find(charIdx))
				{
					int tagBegin = openMatcher.Start();
					int tagBeginEnd = openMatcher.End();
					// Add everything before this tagBegin to a node.
					if (charIdx < tagBegin)
					{
						ret.Add(new HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node(Sharpen.Runtime.Substring(output, charIdx, tagBegin), charIdx, tagBegin));
					}
					attrs.Clear();
					// Get the attributes
					while (attrMatcher.Find(tagBegin + 1) && attrMatcher.End() < tagBeginEnd)
					{
						attrs[attrMatcher.Group("key")] = attrMatcher.Group("value");
						tagBegin = attrMatcher.End();
					}
					// Ok, move to the close tag.
					bool matched = closeMatcher.Find(tagBeginEnd);
					System.Diagnostics.Debug.Assert(matched);
					// Assert statements are sometimes ignored.
					int tagEndBegin = closeMatcher.Start();
					int tagEnd = closeMatcher.End();
					string text = Sharpen.Runtime.Substring(output, tagBeginEnd, tagEndBegin);
					Timex timex = ToTimex(text, attrs);
					ret.Add(new HeidelTimeKBPAnnotator.HeidelTimeOutputReader.TimexNode(text, tagBeginEnd, tagEndBegin, timex));
					charIdx = closeMatcher.End();
				}
				// Add everything before this tagBegin to a node. to the
				if (charIdx < output.Length)
				{
					ret.Add(new HeidelTimeKBPAnnotator.HeidelTimeOutputReader.Node(Sharpen.Runtime.Substring(output, charIdx, output.Length), charIdx, output.Length));
				}
				return ret;
			}

			private Timex ToTimex(string text, IDictionary<string, string> attrs)
			{
				// Mandatory attributes
				string tid = attrs["tid"];
				string val = attrs.GetOrDefault("val", attrs["value"]);
				string altVal = attrs["alTVal"];
				string type = attrs["type"];
				// Optional attributes
				int beginPoint = System.Convert.ToInt32(attrs.GetOrDefault("beginpoint", "-1"));
				int endPoint = System.Convert.ToInt32(attrs.GetOrDefault("endpoint", "-1"));
				// NOTE(chaganty): I do not support timex ranges.
				return new Timex(type, val, altVal, tid, text, beginPoint, endPoint);
			}
		}

		private static int BeginOffset(ICoreMap ann)
		{
			return ann.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
		}

		private static int EndOffset(ICoreMap ann)
		{
			return ann.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
		}

		//  static final List<String> skipList = Arrays.asList("*NL*");
		//  private static int updateTokenIdx(List<CoreLabel> tokens, int currentIndex, String text) {
		//    text = text.trim();
		//    while(currentIndex < tokens.size() && text.length() > 0) {
		//      CoreLabel token = tokens.get(currentIndex++);
		//      if (skipList.contains(token.originalText())) continue;
		//
		//      if (text.startsWith(token.originalText())) {
		//        text = text.substring(token.originalText().length()).trim();
		//      } else if (token.originalText().startsWith(text)) { // In case text is smaller than original text
		//          text = text.substring(text.length()).trim();
		//      } else { // skip
		//        logf("WARNING: Could not figure out how to match token %s to string %s; skipping", token.originalText(), text.substring(0, Math.min(40, text.length())));
		//        if (text.indexOf(token.originalText()) > 0) {
		//          text = text.substring(text.indexOf(token.originalText()));
		//        } else {
		//          text = text.substring(text.length()).trim();
		//        }
		//        currentIndex--;
		//      }
		//    }
		////    return advanceTokenIdx(tokens, currentIndex);
		//  }
		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(TimeAnnotations.TimexAnnotations));
		}
	}
}
