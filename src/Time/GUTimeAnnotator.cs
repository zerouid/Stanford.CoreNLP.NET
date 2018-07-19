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
	/// <summary>Annotates text using GUTime perl script.</summary>
	/// <remarks>
	/// Annotates text using GUTime perl script.
	/// GUTIME/TimeML specifications can be found at:
	/// <a href="http://www.timeml.org/site/tarsqi/modules/gutime/index.html">
	/// http://www.timeml.org/site/tarsqi/modules/gutime/index.html</a>.
	/// </remarks>
	public class GUTimeAnnotator : IAnnotator
	{
		private const string BasePath = "$NLP_DATA_HOME/packages/GUTime";

		private static readonly string DefaultPath = DataFilePaths.Convert(BasePath);

		private readonly File gutimePath;

		private readonly bool outputResults;

		public const string GutimePathProperty = "gutime.path";

		public const string GutimeOutputResults = "gutime.outputResults";

		public GUTimeAnnotator()
			: this(new File(Runtime.GetProperty("gutime", DefaultPath)))
		{
		}

		public GUTimeAnnotator(File gutimePath)
		{
			// if used in a pipeline or constructed with a Properties object,
			// this property tells the annotator where to find the script
			this.gutimePath = gutimePath;
			this.outputResults = false;
		}

		public GUTimeAnnotator(string name, Properties props)
		{
			string path = props.GetProperty(GutimePathProperty, Runtime.GetProperty("gutime", DefaultPath));
			this.gutimePath = new File(path);
			this.outputResults = bool.ValueOf(props.GetProperty(GutimeOutputResults, "false"));
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
			// write input file in GUTime format
			IElement inputXML = ToInputXML(document);
			File inputFile = File.CreateTempFile("gutime", ".input");
			//Document doc = new Document(inputXML);
			PrintWriter inputWriter = new PrintWriter(inputFile);
			inputWriter.Println(XMLUtils.NodeToString(inputXML, false));
			// new XMLOutputter().output(inputXML, inputWriter);
			inputWriter.Close();
			bool useFirstDate = (!document.ContainsKey(typeof(CoreAnnotations.CalendarAnnotation)) && !document.ContainsKey(typeof(CoreAnnotations.DocDateAnnotation)));
			List<string> args = new List<string>();
			args.Add("perl");
			args.Add("-I" + this.gutimePath.GetPath());
			args.Add(new File(this.gutimePath, "TimeTag.pl").GetPath());
			if (useFirstDate)
			{
				args.Add("-FDNW");
			}
			args.Add(inputFile.GetPath());
			// run GUTime on the input file
			ProcessBuilder process = new ProcessBuilder(args);
			StringWriter outputWriter = new StringWriter();
			SystemUtils.Run(process, outputWriter, null);
			string output = outputWriter.GetBuffer().ToString();
			Pattern docClose = Pattern.Compile("</DOC>.*", Pattern.Dotall);
			output = docClose.Matcher(output).ReplaceAll("</DOC>");
			//The TimeTag.pl result file contains next tags which must be removed
			output = output.ReplaceAll("<lex.*?>", string.Empty);
			output = output.Replace("</lex>", string.Empty);
			output = output.Replace("<NG>", string.Empty);
			output = output.Replace("</NG>", string.Empty);
			output = output.Replace("<VG>", string.Empty);
			output = output.Replace("</VG>", string.Empty);
			output = output.Replace("<s>", string.Empty);
			output = output.Replace("</s>", string.Empty);
			// parse the GUTime output
			IElement outputXML;
			try
			{
				outputXML = XMLUtils.ParseElement(output);
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("error:\n%s\ninput:\n%s\noutput:\n%s", ex, IOUtils.SlurpFile(inputFile), output), ex);
			}
			/*
			try {
			outputXML = new SAXBuilder().build(new StringReader(output)).getRootElement();
			} catch (JDOMException e) {
			throw new RuntimeException(String.format("error:\n%s\ninput:\n%s\noutput:\n%s",
			e, IOUtils.slurpFile(inputFile), output));
			} */
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

		private static IElement ToInputXML(ICoreMap document)
		{
			// construct GUTime format XML
			IElement doc = XMLUtils.CreateElement("DOC");
			doc.AppendChild(XMLUtils.CreateTextNode("\n"));
			// populate the date element
			Calendar dateCalendar = document.Get(typeof(CoreAnnotations.CalendarAnnotation));
			if (dateCalendar != null)
			{
				IElement date = XMLUtils.CreateElement("date");
				date.AppendChild(XMLUtils.CreateTextNode(string.Format("%TF", dateCalendar)));
				doc.AppendChild(date);
				doc.AppendChild(XMLUtils.CreateTextNode("\n"));
			}
			else
			{
				string s = document.Get(typeof(CoreAnnotations.DocDateAnnotation));
				if (s != null)
				{
					IElement date = XMLUtils.CreateElement("date");
					date.AppendChild(XMLUtils.CreateTextNode(s));
					doc.AppendChild(date);
					doc.AppendChild(XMLUtils.CreateTextNode("\n"));
				}
			}
			IElement textElem = XMLUtils.CreateElement("text");
			doc.AppendChild(textElem);
			doc.AppendChild(XMLUtils.CreateTextNode("\n"));
			// populate the text element
			string text = document.Get(typeof(CoreAnnotations.TextAnnotation));
			int offset = 0;
			foreach (ICoreMap sentence in document.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				int sentBegin = sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int sentEnd = sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				// add text before the first token
				textElem.AppendChild(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(text, offset, sentBegin)));
				offset = sentBegin;
				// add one "s" element per sentence
				IElement s = XMLUtils.CreateElement("s");
				textElem.AppendChild(s);
				foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					int tokenBegin = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int tokenEnd = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					s.AppendChild(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(text, offset, tokenBegin)));
					offset = tokenBegin;
					// add one "lex" element per token
					IElement lex = XMLUtils.CreateElement("lex");
					s.AppendChild(lex);
					string posTag = token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
					if (posTag != null)
					{
						lex.SetAttribute("pos", posTag);
					}
					System.Diagnostics.Debug.Assert(token.Word().Equals(Sharpen.Runtime.Substring(text, offset, tokenEnd)));
					lex.AppendChild(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(text, offset, tokenEnd)));
					offset = tokenEnd;
				}
				// add text after the last token
				textElem.AppendChild(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(text, offset, sentEnd)));
				offset = sentEnd;
			}
			// add text after the last sentence
			textElem.AppendChild(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(text, offset, text.Length)));
			// return the document
			return doc;
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
			//--Set Timexes
			IList<ICoreMap> timexMaps = new List<ICoreMap>();
			int offset = 0;
			INodeList docNodes = docElem.GetChildNodes();
			IElement textElem = null;
			// Find first "text" elem
			for (int i = 0; i < docNodes.GetLength(); i++)
			{
				INode n = docNodes.Item(i);
				if ("text".Equals(n.GetNodeName()))
				{
					textElem = (IElement)n;
					break;
				}
			}
			INodeList textNodes = textElem.GetChildNodes();
			for (int i_1 = 0; i_1 < textNodes.GetLength(); i_1++)
			{
				INode content = textNodes.Item(i_1);
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
							//(timex)
							timexMap.Set(typeof(TimeAnnotations.TimexAnnotation), timex);
							//(text)
							timexMap.Set(typeof(CoreAnnotations.TextAnnotation), timexText);
							//(characters)
							int charBegin = offset;
							timexMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), charBegin);
							offset += timexText.Length;
							int charEnd = offset;
							timexMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), charEnd);
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
							//(add)
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
