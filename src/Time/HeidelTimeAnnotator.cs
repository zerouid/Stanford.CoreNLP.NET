using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Org.W3c.Dom;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Annotates text using HeidelTime.</summary>
	/// <remarks>
	/// Annotates text using HeidelTime.
	/// GUTIME/TimeML specifications can be found at:
	/// <a href="http://www.timeml.org/site/tarsqi/modules/gutime/index.html">
	/// http://www.timeml.org/site/tarsqi/modules/gutime/index.html</a>.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class HeidelTimeAnnotator : IAnnotator
	{
		private const string BasePath = "$NLP_DATA_HOME/packages/heideltime/";

		private static readonly string DefaultPath = DataFilePaths.Convert(BasePath);

		private readonly File heideltimePath;

		private readonly bool outputResults;

		private readonly string language;

		public const string HeideltimePathProperty = "heideltime.path";

		public const string HeideltimeLanguageProperty = "heideltime.language";

		public const string HeideltimeOutputResults = "heideltime.outputResults";

		public HeidelTimeAnnotator()
			: this(new File(Runtime.GetProperty("heideltime", DefaultPath)))
		{
		}

		public HeidelTimeAnnotator(File heideltimePath)
			: this(heideltimePath, "english", false)
		{
		}

		public HeidelTimeAnnotator(File heideltimePath, string language, bool outputResults)
		{
			// TODO HeidelTime doesn't actually run on the NLP machines :( (TreeTagger doesn't run.)
			// This could probably be fixed in newer HeidelTime versions, which even support using our tagger.
			// if used in a pipeline or constructed with a Properties object,
			// this property tells the annotator where to find the script
			this.heideltimePath = heideltimePath;
			this.outputResults = outputResults;
			this.language = language;
		}

		public HeidelTimeAnnotator(string name, Properties props)
			: this(new File(props.GetProperty(HeideltimePathProperty, Runtime.GetProperty("heideltime", DefaultPath))), props.GetProperty(HeideltimeLanguageProperty, "english"), bool.ValueOf(props.GetProperty(HeideltimeOutputResults, "false")))
		{
		}

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

		/// <exception cref="System.IO.IOException"/>
		public virtual void Annotate(ICoreMap document)
		{
			//--Create Input File
			//(create file)
			File inputFile = File.CreateTempFile("heideltime", ".input");
			//(write to file)
			PrintWriter inputWriter = new PrintWriter(inputFile);
			inputWriter.Println(document.Get(typeof(CoreAnnotations.TextAnnotation)));
			inputWriter.Close();
			//--Get Date
			//(error checks)
			if (!document.ContainsKey(typeof(CoreAnnotations.CalendarAnnotation)) && !document.ContainsKey(typeof(CoreAnnotations.DocDateAnnotation)))
			{
				throw new ArgumentException("CoreMap must have either a Calendar or DocDate annotation");
			}
			//not strictly necessary, technically...
			//(variables)
			Calendar dateCalendar = document.Get(typeof(CoreAnnotations.CalendarAnnotation));
			string pubDate = null;
			if (dateCalendar != null)
			{
				//(case: calendar annotation)
				pubDate = string.Format("%TF", dateCalendar);
			}
			else
			{
				//(case: docdateannotation)
				string s = document.Get(typeof(CoreAnnotations.DocDateAnnotation));
				if (s != null)
				{
					pubDate = s;
				}
			}
			//--Build Command
			List<string> args = new List<string>();
			args.Add("java");
			args.Add("-jar");
			args.Add(this.heideltimePath.GetPath() + "/heideltime.jar");
			args.Add("-c");
			args.Add(this.heideltimePath.GetPath() + "/config.props");
			args.Add("-l");
			args.Add(this.language);
			args.Add("-t");
			args.Add("NEWS");
			if (pubDate != null)
			{
				args.Add("-dct");
				args.Add(pubDate);
			}
			args.Add(inputFile.GetPath());
			// run HeidelTime on the input file
			ProcessBuilder process = new ProcessBuilder(args);
			StringWriter outputWriter = new StringWriter();
			SystemUtils.Run(process, outputWriter, null);
			string output = outputWriter.GetBuffer().ToString();
			Pattern docClose = Pattern.Compile("</DOC>.*", Pattern.Dotall);
			output = docClose.Matcher(output).ReplaceAll("</DOC>").ReplaceAll("<!DOCTYPE TimeML SYSTEM \"TimeML.dtd\">", string.Empty);
			//TODO TimeML.dtd? FileNotFoundException if we leave it in
			Pattern badNestedTimex = Pattern.Compile(Pattern.Quote("<T</TIMEX3>IMEX3"));
			output = badNestedTimex.Matcher(output).ReplaceAll("</TIMEX3><TIMEX3");
			Pattern badNestedTimex2 = Pattern.Compile(Pattern.Quote("<TI</TIMEX3>MEX3"));
			output = badNestedTimex2.Matcher(output).ReplaceAll("</TIMEX3><TIMEX3");
			//output = output.replaceAll("\\n\\n<TimeML>\\n\\n","<TimeML>");
			output = output.ReplaceAll("<TimeML>", string.Empty);
			// parse the HeidelTime output
			IElement outputXML;
			try
			{
				outputXML = XMLUtils.ParseElement(output);
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("error:\n%s\ninput:\n%s\noutput:\n%s", ex, IOUtils.SlurpFile(inputFile), output), ex);
			}
			inputFile.Delete();
			// get Timex annotations
			IList<ICoreMap> timexAnns = ToTimexCoreMaps(outputXML, document);
			document.Set(typeof(TimeAnnotations.TimexAnnotations), timexAnns);
			if (outputResults)
			{
				System.Console.Out.WriteLine(timexAnns);
			}
			// align Timex annotations to sentences
			int timexIndex = 0;
			foreach (ICoreMap sentence in document.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				int sentBegin = BeginOffset(sentence);
				int sentEnd = EndOffset(sentence);
				// skip times before the sentence
				while (timexIndex < timexAnns.Count && BeginOffset(timexAnns[timexIndex]) < sentBegin)
				{
					++timexIndex;
				}
				// determine times within the sentence
				int sublistBegin = timexIndex;
				int sublistEnd = timexIndex;
				while (timexIndex < timexAnns.Count && sentBegin <= BeginOffset(timexAnns[timexIndex]) && EndOffset(timexAnns[timexIndex]) <= sentEnd)
				{
					++sublistEnd;
					++timexIndex;
				}
				// set the sentence timexes
				sentence.Set(typeof(TimeAnnotations.TimexAnnotations), timexAnns.SubList(sublistBegin, sublistEnd));
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

		private static IList<ICoreMap> ToTimexCoreMaps(IElement docElem, ICoreMap originalDocument)
		{
			//--Collect Token Offsets
			IDictionary<int, int> beginMap = Generics.NewHashMap();
			IDictionary<int, int> endMap = Generics.NewHashMap();
			bool haveTokenOffsets = true;
			foreach (ICoreMap sent in originalDocument.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel token in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					int tokBegin = token.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					int tokEnd = token.Get(typeof(CoreAnnotations.TokenEndAnnotation));
					if (tokBegin == null || tokEnd == null)
					{
						haveTokenOffsets = false;
					}
					int charBegin = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int charEnd = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					beginMap[charBegin] = tokBegin;
					endMap[charEnd] = tokEnd;
				}
			}
			IList<ICoreMap> timexMaps = new List<ICoreMap>();
			int offset = 0;
			INodeList docNodes = docElem.GetChildNodes();
			for (int i = 0; i < docNodes.GetLength(); i++)
			{
				INode content = docNodes.Item(i);
				if (content is IText)
				{
					IText text = (IText)content;
					offset += text.GetWholeText().Length;
				}
				else
				{
					if (content is IElement)
					{
						IElement child = (IElement)content;
						if (child.GetNodeName().Equals("TIMEX3"))
						{
							Timex timex = new Timex(child);
							if (child.GetChildNodes().GetLength() != 1)
							{
								throw new Exception("TIMEX3 should only contain text " + child);
							}
							string timexText = child.GetTextContent();
							ICoreMap timexMap = new ArrayCoreMap();
							timexMap.Set(typeof(TimeAnnotations.TimexAnnotation), timex);
							timexMap.Set(typeof(CoreAnnotations.TextAnnotation), timexText);
							int charBegin = offset;
							timexMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), offset);
							offset += timexText.Length;
							timexMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), offset);
							int charEnd = offset;
							//(tokens)
							if (haveTokenOffsets)
							{
								int tokBegin = beginMap[charBegin];
								int searchStep = 1;
								//if no exact match, search around the character offset
								while (tokBegin == null)
								{
									tokBegin = beginMap[charBegin - searchStep];
									if (tokBegin == null)
									{
										tokBegin = beginMap[charBegin + searchStep];
									}
									searchStep += 1;
								}
								searchStep = 1;
								int tokEnd = endMap[charEnd];
								while (tokEnd == null)
								{
									tokEnd = endMap[charEnd - searchStep];
									if (tokEnd == null)
									{
										tokEnd = endMap[charEnd + searchStep];
									}
									searchStep += 1;
								}
								timexMap.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokBegin);
								timexMap.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokEnd);
							}
							timexMaps.Add(timexMap);
						}
						else
						{
							throw new Exception("unexpected element " + child);
						}
					}
					else
					{
						throw new Exception("unexpected content " + content);
					}
				}
			}
			return timexMaps;
		}

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
