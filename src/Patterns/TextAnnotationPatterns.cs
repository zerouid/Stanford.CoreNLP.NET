using System;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Javax.Json;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 3/10/15.</summary>
	public class TextAnnotationPatterns
	{
		private IDictionary<string, Type> humanLabelClasses = new Dictionary<string, Type>();

		private IDictionary<string, Type> machineAnswerClasses = new Dictionary<string, Type>();

		internal Properties props;

		private string outputFile;

		internal ICounter<string> matchedSeedWords;

		private IDictionary<string, ICollection<CandidatePhrase>> seedWords = new Dictionary<string, ICollection<CandidatePhrase>>();

		private string backgroundSymbol = "O";

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.TextAnnotationPatterns));

		/// <exception cref="System.IO.IOException"/>
		public TextAnnotationPatterns()
		{
		}

		//Properties testProps = new Properties();
		//    if(testPropertiesFile!= null && new File(testPropertiesFile).exists()){
		//      logger.info("Loading test properties from " + testPropertiesFile);
		//      testProps.load(new FileReader(testPropertiesFile));
		//    }
		public virtual string GetAllAnnotations()
		{
			IJsonObjectBuilder obj = Javax.Json.Json.CreateObjectBuilder();
			foreach (KeyValuePair<string, DataInstance> sent in Data.sents)
			{
				bool sentHasLabel = false;
				IJsonObjectBuilder objsent = Javax.Json.Json.CreateObjectBuilder();
				int tokenid = 0;
				foreach (CoreLabel l in sent.Value.GetTokens())
				{
					bool haslabel = false;
					IJsonArrayBuilder labelArr = Javax.Json.Json.CreateArrayBuilder();
					foreach (KeyValuePair<string, Type> en in this.humanLabelClasses)
					{
						if (!l.Get(en.Value).Equals(backgroundSymbol))
						{
							haslabel = true;
							sentHasLabel = true;
							labelArr.Add(en.Key);
						}
					}
					if (haslabel)
					{
						objsent.Add(tokenid.ToString(), labelArr);
					}
					tokenid++;
				}
				if (sentHasLabel)
				{
					obj.Add(sent.Key, objsent);
				}
			}
			return obj.Build().ToString();
		}

		public virtual string GetAllAnnotations(string input)
		{
			IJsonObjectBuilder objsent = Javax.Json.Json.CreateObjectBuilder();
			int tokenid = 0;
			foreach (CoreLabel l in Data.sents[input].GetTokens())
			{
				bool haslabel = false;
				IJsonArrayBuilder labelArr = Javax.Json.Json.CreateArrayBuilder();
				foreach (KeyValuePair<string, Type> en in this.humanLabelClasses)
				{
					if (!l.Get(en.Value).Equals(backgroundSymbol))
					{
						haslabel = true;
						labelArr.Add(en.Key);
					}
				}
				if (haslabel)
				{
					objsent.Add(tokenid.ToString(), labelArr);
				}
				tokenid++;
			}
			return objsent.Build().ToString();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public virtual string SuggestPhrases()
		{
			ResetPatternLabelsInSents(Data.sents);
			GetPatternsFromDataMultiClass<SurfacePattern> model = new GetPatternsFromDataMultiClass<SurfacePattern>(props, Data.sents, seedWords, false, humanLabelClasses);
			//model.constVars.numIterationsForPatterns = 2;
			model.IterateExtractApply();
			return model.constVars.GetLearnedWordsAsJson();
		}

		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="Java.Sql.SQLException"/>
		public virtual string SuggestPhrasesTest(Properties testProps, string modelPropertiesFile, string stopWordsFile)
		{
			logger.Info("Suggesting phrases in test");
			logger.Info("test properties are " + testProps);
			Properties runProps = StringUtils.ArgsToPropertiesWithResolve(new string[] { "-props", modelPropertiesFile });
			string[] removeProperties = new string[] { "allPatternsDir", "storePatsForEachToken", "invertedIndexClass", "savePatternsWordsDir", "batchProcessSents", "outDir", "saveInvertedIndex", "removeOverLappingLabels", "numThreads" };
			foreach (string s in removeProperties)
			{
				if (runProps.Contains(s))
				{
					runProps.Remove(s);
				}
			}
			runProps.SetProperty("stopWordsPatternFiles", stopWordsFile);
			runProps.SetProperty("englishWordsFiles", stopWordsFile);
			runProps.SetProperty("commonWordsPatternFiles", stopWordsFile);
			runProps.PutAll(props);
			runProps.PutAll(testProps);
			props.PutAll(runProps);
			ProcessText(false);
			GetPatternsFromDataMultiClass<SurfacePattern> model = new GetPatternsFromDataMultiClass<SurfacePattern>(runProps, Data.sents, seedWords, true, humanLabelClasses);
			ArgumentParser.FillOptions(model, runProps);
			GetPatternsFromDataMultiClass.LoadFromSavedPatternsWordsDir(model, runProps);
			IDictionary<string, int> alreadyLearnedIters = new Dictionary<string, int>();
			foreach (string label in model.constVars.GetLabels())
			{
				alreadyLearnedIters[label] = model.constVars.GetLearnedWordsEachIter()[label].LastEntry().Key;
			}
			if (model.constVars.learn)
			{
				//      Map<String, E> p0 = new HashMap<String, SurfacePattern>();
				//      Map<String, Counter<CandidatePhrase>> p0Set = new HashMap<String, Counter<CandidatePhrase>>();
				//      Map<String, Set<E>> ignorePatterns = new HashMap<String, Set<E>>();
				model.IterateExtractApply(null, null, null);
			}
			IDictionary<string, ICounter<CandidatePhrase>> allExtractions = new Dictionary<string, ICounter<CandidatePhrase>>();
			//Only for one label right now!
			string label_1 = model.constVars.GetLabels().GetEnumerator().Current;
			allExtractions[label_1] = new ClassicCounter<CandidatePhrase>();
			foreach (KeyValuePair<string, DataInstance> sent in Data.sents)
			{
				StringBuilder str = new StringBuilder();
				foreach (CoreLabel l in sent.Value.GetTokens())
				{
					if (l.Get(typeof(PatternsAnnotations.MatchedPatterns)) != null && !l.Get(typeof(PatternsAnnotations.MatchedPatterns)).IsEmpty())
					{
						str.Append(" " + l.Word());
					}
					else
					{
						allExtractions[label_1].IncrementCount(CandidatePhrase.CreateOrGet(str.ToString().Trim()));
						str.Length = 0;
					}
				}
			}
			allExtractions.PutAll(model.matchedSeedWords);
			return model.constVars.GetSetWordsAsJson(allExtractions);
		}

		//label the sents with the labels provided by humans
		private void ResetPatternLabelsInSents(IDictionary<string, DataInstance> sents)
		{
			foreach (KeyValuePair<string, DataInstance> sent in sents)
			{
				foreach (CoreLabel l in sent.Value.GetTokens())
				{
					foreach (KeyValuePair<string, Type> cl in humanLabelClasses)
					{
						l.Set(machineAnswerClasses[cl.Key], l.Get(cl.Value));
					}
				}
			}
		}

		public virtual string GetMatchedTokensByAllPhrases()
		{
			return GetPatternsFromDataMultiClass.MatchedTokensByPhraseJsonString();
		}

		public virtual string GetMatchedTokensByPhrase(string input)
		{
			return GetPatternsFromDataMultiClass.MatchedTokensByPhraseJsonString(input);
		}

		private void SetProperties(Properties props)
		{
			if (!props.Contains("fileFormat"))
			{
				props.SetProperty("fileFormat", "txt");
			}
			if (!props.Contains("learn"))
			{
				props.SetProperty("learn", "false");
			}
			if (!props.Contains("patternType"))
			{
				props.SetProperty("patternType", "SURFACE");
			}
			props.SetProperty("preserveSentenceSequence", "true");
			if (!props.Contains("debug"))
			{
				props.SetProperty("debug", "3");
			}
			if (!props.Contains("thresholdWordExtract"))
			{
				props.SetProperty("thresholdWordExtract", "0.00000000000000001");
			}
			if (!props.Contains("thresholdNumPatternsApplied"))
			{
				props.SetProperty("thresholdNumPatternsApplied", "1");
			}
			if (!props.Contains("writeMatchedTokensIdsForEachPhrase"))
			{
				props.SetProperty("writeMatchedTokensIdsForEachPhrase", "true");
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		internal virtual void SetUpProperties(string line, bool readFile, bool writeOutputToFile, string additionalSeedWordsFiles)
		{
			IJsonReader jsonReader = Javax.Json.Json.CreateReader(new StringReader(line));
			IJsonObject objarr = jsonReader.ReadObject();
			jsonReader.Close();
			Properties props = new Properties();
			foreach (string o in objarr.Keys)
			{
				if (o.Equals("seedWords"))
				{
					IJsonObject obj = objarr.GetJsonObject(o);
					foreach (string st in obj.Keys)
					{
						seedWords[st] = new HashSet<CandidatePhrase>();
						IJsonArray arr = obj.GetJsonArray(st);
						for (int i = 0; i < arr.Count; i++)
						{
							string val = arr.GetString(i);
							seedWords[st].Add(CandidatePhrase.CreateOrGet(val));
							System.Console.Out.WriteLine("adding " + val + " for label " + st);
						}
					}
				}
				else
				{
					props.SetProperty(o, objarr.GetString(o));
				}
			}
			System.Console.Out.WriteLine("seedwords are " + seedWords);
			if (additionalSeedWordsFiles != null && !additionalSeedWordsFiles.IsEmpty())
			{
				IDictionary<string, ICollection<CandidatePhrase>> additionalSeedWords = GetPatternsFromDataMultiClass.ReadSeedWords(additionalSeedWordsFiles);
				logger.Info("additional seed words are " + additionalSeedWords);
				foreach (string label in seedWords.Keys)
				{
					if (additionalSeedWords.Contains(label))
					{
						Sharpen.Collections.AddAll(seedWords[label], additionalSeedWords[label]);
					}
				}
			}
			outputFile = null;
			if (readFile)
			{
				System.Console.Out.WriteLine("input value is " + objarr.GetString("input"));
				outputFile = props.GetProperty("input") + "_processed";
				props.SetProperty("file", objarr.GetString("input"));
				if (writeOutputToFile && !props.Contains("columnOutputFile"))
				{
					props.SetProperty("columnOutputFile", outputFile);
				}
			}
			else
			{
				string systemdir = Runtime.GetProperty("java.io.tmpdir");
				File tempFile = File.CreateTempFile("sents", ".tmp", new File(systemdir));
				tempFile.DeleteOnExit();
				IOUtils.WriteStringToFile(props.GetProperty("input"), tempFile.GetPath(), "utf8");
				props.SetProperty("file", tempFile.GetAbsolutePath());
			}
			SetProperties(props);
			this.props = props;
			int i_1 = 1;
			foreach (string label_1 in seedWords.Keys)
			{
				string ansclstr = "edu.stanford.nlp.patterns.PatternsAnnotations$PatternLabel" + i_1;
				Type mcCl = (Type)Sharpen.Runtime.GetType(ansclstr);
				machineAnswerClasses[label_1] = mcCl;
				string humanansclstr = "edu.stanford.nlp.patterns.PatternsAnnotations$PatternHumanLabel" + i_1;
				humanLabelClasses[label_1] = (Type)Sharpen.Runtime.GetType(humanansclstr);
				i_1++;
			}
		}

		//the format of the line input is json string of maps. required keys are "input" and "seedWords". "input" can be a string or file (in which case readFile should be true.)
		// For example: {"input":"presidents.txt","seedWords":{"name":["Obama"],"place":["Chicago"]}}
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="Java.Sql.SQLException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MissingMethodException"/>
		public virtual string ProcessText(bool writeOutputToFile)
		{
			logger.Info("Starting to process text");
			logger.Info("all seed words are " + seedWords);
			Pair<IDictionary<string, DataInstance>, IDictionary<string, DataInstance>> sentsPair = GetPatternsFromDataMultiClass.ProcessSents(props, seedWords.Keys);
			Data.sents = sentsPair.First();
			ConstantsAndVariables constVars = new ConstantsAndVariables(props, seedWords.Keys, machineAnswerClasses);
			foreach (string label in seedWords.Keys)
			{
				GetPatternsFromDataMultiClass.RunLabelSeedWords(Data.sents, humanLabelClasses[label], label, seedWords[label], constVars, true);
			}
			if (writeOutputToFile)
			{
				GetPatternsFromDataMultiClass.WriteColumnOutput(outputFile, false, humanLabelClasses);
				System.Console.Out.WriteLine("written the output to " + outputFile);
			}
			logger.Info("Finished processing text");
			return "SUCCESS";
		}

		public virtual string DoRemovePhrases(string line)
		{
			return ("not yet implemented");
		}

		public virtual string DoRemoveAnnotations(string line)
		{
			int tokensNum = ChangeAnnotation(line, true);
			return "SUCCESS . Labeled " + tokensNum + " tokens ";
		}

		//input is a json string, example:{“name”:[“sent1”:”1,2,4,6”,”sent2”:”11,13,15”], “birthplace”:[“sent1”:”3,5”]}
		public virtual string DoNewAnnotations(string line)
		{
			int tokensNum = ChangeAnnotation(line, false);
			return "SUCCESS . Labeled " + tokensNum + " tokens ";
		}

		private int ChangeAnnotation(string line, bool remove)
		{
			int tokensNum = 0;
			IJsonReader jsonReader = Javax.Json.Json.CreateReader(new StringReader(line));
			IJsonObject objarr = jsonReader.ReadObject();
			foreach (string label in objarr.Keys)
			{
				IJsonObject obj4label = objarr.GetJsonObject(label);
				foreach (string sentid in obj4label.Keys)
				{
					IJsonArray tokenArry = obj4label.GetJsonArray(sentid);
					foreach (IJsonValue tokenid in tokenArry)
					{
						tokensNum++;
						Data.sents[sentid].GetTokens()[System.Convert.ToInt32(tokenid.ToString())].Set(humanLabelClasses[label], remove ? backgroundSymbol : label);
					}
				}
			}
			return tokensNum;
		}

		public virtual string CurrentSummary()
		{
			return "Phrases hand labeled : " + seedWords.ToString();
		}

		//line is a jsonstring of map of label to array of strings; ex: {"name":["Bush","Carter","Obama"]}
		/// <exception cref="System.Exception"/>
		public virtual string DoNewPhrases(string line)
		{
			System.Console.Out.WriteLine("adding new phrases");
			ConstantsAndVariables constVars = new ConstantsAndVariables(props, humanLabelClasses.Keys, humanLabelClasses);
			IJsonReader jsonReader = Javax.Json.Json.CreateReader(new StringReader(line));
			IJsonObject objarr = jsonReader.ReadObject();
			foreach (KeyValuePair<string, IJsonValue> o in objarr)
			{
				string label = o.Key;
				ICollection<CandidatePhrase> seed = new HashSet<CandidatePhrase>();
				IJsonArray arr = objarr.GetJsonArray(o.Key);
				for (int i = 0; i < arr.Count; i++)
				{
					string seedw = arr.GetString(i);
					System.Console.Out.WriteLine("adding " + seedw + " to seed ");
					seed.Add(CandidatePhrase.CreateOrGet(seedw));
				}
				Sharpen.Collections.AddAll(seedWords[label], seed);
				constVars.AddSeedWords(label, seed);
				GetPatternsFromDataMultiClass.RunLabelSeedWords(Data.sents, humanLabelClasses[label], label, seed, constVars, false);
			}
			//model.labelWords(label, labelclass, Data.sents, seed);
			return "SUCCESS added new phrases";
		}
	}
}
