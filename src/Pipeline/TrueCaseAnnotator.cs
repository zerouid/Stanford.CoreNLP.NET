using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	public class TrueCaseAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.TrueCaseAnnotator));

		private readonly CRFBiasedClassifier<CoreLabel> trueCaser;

		private readonly IDictionary<string, string> mixedCaseMap;

		private readonly bool overwriteText;

		private readonly bool verbose;

		public const string DefaultModelBias = "INIT_UPPER:-0.7,UPPER:-0.7,O:0";

		private const string DefaultOverwriteText = "false";

		private const string DefaultVerbose = "false";

		public TrueCaseAnnotator()
			: this(true)
		{
		}

		public TrueCaseAnnotator(bool verbose)
			: this(Runtime.GetProperty("truecase.model", DefaultPaths.DefaultTruecaseModel), Runtime.GetProperty("truecase.bias", DefaultModelBias), Runtime.GetProperty("truecase.mixedcasefile", DefaultPaths.DefaultTruecaseDisambiguationList), bool.Parse
				(Runtime.GetProperty("truecase.overwriteText", Edu.Stanford.Nlp.Pipeline.TrueCaseAnnotator.DefaultOverwriteText)), verbose)
		{
		}

		public TrueCaseAnnotator(Properties properties)
			: this(properties.GetProperty("truecase.model", DefaultPaths.DefaultTruecaseModel), properties.GetProperty("truecase.bias", Edu.Stanford.Nlp.Pipeline.TrueCaseAnnotator.DefaultModelBias), properties.GetProperty("truecase.mixedcasefile", DefaultPaths
				.DefaultTruecaseDisambiguationList), bool.Parse(properties.GetProperty("truecase.overwriteText", Edu.Stanford.Nlp.Pipeline.TrueCaseAnnotator.DefaultOverwriteText)), bool.Parse(properties.GetProperty("truecase.verbose", Edu.Stanford.Nlp.Pipeline.TrueCaseAnnotator
				.DefaultVerbose)))
		{
		}

		public TrueCaseAnnotator(string modelLoc, string classBias, string mixedCaseFileName, bool overwriteText, bool verbose)
		{
			this.overwriteText = overwriteText;
			this.verbose = verbose;
			Properties props = PropertiesUtils.AsProperties("loadClassifier", modelLoc, "mixedCaseMapFile", mixedCaseFileName, "classBias", classBias);
			trueCaser = new CRFBiasedClassifier<CoreLabel>(props);
			if (modelLoc != null)
			{
				trueCaser.LoadClassifierNoExceptions(modelLoc, props);
			}
			else
			{
				throw new Exception("Model location not specified for true-case classifier!");
			}
			if (classBias != null)
			{
				StringTokenizer biases = new StringTokenizer(classBias, ",");
				while (biases.HasMoreTokens())
				{
					StringTokenizer bias = new StringTokenizer(biases.NextToken(), ":");
					string cname = bias.NextToken();
					double w = double.Parse(bias.NextToken());
					trueCaser.SetBiasWeight(cname, w);
					if (this.verbose)
					{
						log.Info("Setting bias for class " + cname + " to " + w);
					}
				}
			}
			// Load map containing mixed-case words:
			mixedCaseMap = LoadMixedCaseMap(mixedCaseFileName);
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (verbose)
			{
				log.Info("Adding true-case annotation...");
			}
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				// classify tokens for each sentence
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					IList<CoreLabel> output = this.trueCaser.ClassifySentence(tokens);
					for (int i = 0; i < size; i++)
					{
						// add the truecaser tag to each token
						string neTag = output[i].Get(typeof(CoreAnnotations.AnswerAnnotation));
						tokens[i].Set(typeof(CoreAnnotations.TrueCaseAnnotation), neTag);
						SetTrueCaseText(tokens[i]);
					}
				}
			}
			else
			{
				throw new Exception("unable to find sentences in: " + annotation);
			}
		}

		private void SetTrueCaseText(CoreLabel l)
		{
			string trueCase = l.GetString<CoreAnnotations.TrueCaseAnnotation>();
			string text = l.Word();
			string trueCaseText = text;
			switch (trueCase)
			{
				case "UPPER":
				{
					trueCaseText = text.ToUpper();
					break;
				}

				case "LOWER":
				{
					trueCaseText = text.ToLower();
					break;
				}

				case "INIT_UPPER":
				{
					trueCaseText = char.ToTitleCase(text[0]) + Sharpen.Runtime.Substring(text, 1).ToLower();
					break;
				}

				case "O":
				{
					// The model predicted mixed case, so lookup the map:
					string lower = text.ToLower();
					if (mixedCaseMap.Contains(lower))
					{
						trueCaseText = mixedCaseMap[lower];
					}
					// else leave it as it was?
					break;
				}
			}
			// System.err.println(text + " was classified as " + trueCase + " and so became " + trueCaseText);
			l.Set(typeof(CoreAnnotations.TrueCaseTextAnnotation), trueCaseText);
			if (overwriteText)
			{
				l.Set(typeof(CoreAnnotations.TextAnnotation), trueCaseText);
				l.Set(typeof(CoreAnnotations.ValueAnnotation), trueCaseText);
			}
		}

		private static IDictionary<string, string> LoadMixedCaseMap(string mapFile)
		{
			IDictionary<string, string> map = Generics.NewHashMap();
			try
			{
				using (BufferedReader br = IOUtils.ReaderFromString(mapFile))
				{
					foreach (string line in ObjectBank.GetLineIterator(br))
					{
						line = line.Trim();
						string[] els = line.Split("\\s+");
						if (els.Length != 2)
						{
							throw new Exception("Wrong format: " + mapFile);
						}
						map[els[0]] = els[1];
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return map;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.PositionAnnotation), typeof(CoreAnnotations.SentencesAnnotation
				))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TrueCaseTextAnnotation), typeof(CoreAnnotations.TrueCaseAnnotation), typeof(CoreAnnotations.AnswerAnnotation), typeof(CoreAnnotations.ShapeAnnotation
				))));
		}
	}
}
