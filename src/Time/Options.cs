using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Various options for using time expression extractor</summary>
	/// <author>Angel Chang</author>
	public class Options
	{
		public enum RelativeHeuristicLevel
		{
			None,
			Basic,
			More
		}

		public bool markTimeRanges = false;

		internal bool restrictToTimex3 = false;

		internal Options.RelativeHeuristicLevel teRelHeurLevel = Options.RelativeHeuristicLevel.None;

		internal bool includeNested = false;

		internal bool includeRange = false;

		internal bool searchForDocDate = false;

		public string language = "english";

		public static Dictionary<string, string> languageToRulesFiles = new Dictionary<string, string>();

		internal string grammarFilename = null;

		internal Env.IBinder[] binders = null;

		internal const string DefaultGrammarFiles = "edu/stanford/nlp/models/sutime/defs.sutime.txt,edu/stanford/nlp/models/sutime/english.sutime.txt,edu/stanford/nlp/models/sutime/english.holidays.sutime.txt";

		internal const string DefaultBritishGrammarFiles = "edu/stanford/nlp/models/sutime/defs.sutime.txt,edu/stanford/nlp/models/sutime/british.sutime.txt,edu/stanford/nlp/models/sutime/english.sutime.txt,edu/stanford/nlp/models/sutime/english.holidays.sutime.txt";

		internal const string DefaultSpanishGrammarFiles = "edu/stanford/nlp/models/sutime/defs.sutime.txt,edu/stanford/nlp/models/sutime/spanish.sutime.txt";

		internal static readonly string[] DefaultBinders = new string[] { "edu.stanford.nlp.time.JollyDayHolidays" };

		static Options()
		{
			// Whether to mark time ranges like from 1991 to 1992 as one timex
			// or leave it separate
			// Whether include non timex3 temporal expressions
			// Heuristics for determining relative time
			// level 1 = no heuristics (default)
			// level 2 = basic heuristics taking into past tense
			// level 3 = more heuristics with since/until
			// Include nested time expressions
			// Create range for all temporals and include range attribute in timex annotation
			// Look for document date in the document text (if not provided)
			// language for SUTime
			// TODO: Add default country for holidays and default time format
			// would want a per document default as well
			//static final String[] DEFAULT_BINDERS = { };
			languageToRulesFiles["english"] = DefaultGrammarFiles;
			languageToRulesFiles["en"] = DefaultGrammarFiles;
			languageToRulesFiles["british"] = DefaultBritishGrammarFiles;
			languageToRulesFiles["spanish"] = DefaultSpanishGrammarFiles;
			languageToRulesFiles["es"] = DefaultSpanishGrammarFiles;
		}

		internal bool verbose = false;

		static Options()
		{
		}

		public Options()
		{
		}

		public Options(string name, Properties props)
		{
			includeRange = PropertiesUtils.GetBool(props, name + ".includeRange", includeRange);
			markTimeRanges = PropertiesUtils.GetBool(props, name + ".markTimeRanges", markTimeRanges);
			includeNested = PropertiesUtils.GetBool(props, name + ".includeNested", includeNested);
			restrictToTimex3 = PropertiesUtils.GetBool(props, name + ".restrictToTimex3", restrictToTimex3);
			teRelHeurLevel = Options.RelativeHeuristicLevel.ValueOf(props.GetProperty(name + ".teRelHeurLevel", teRelHeurLevel.ToString()));
			verbose = PropertiesUtils.GetBool(props, name + ".verbose", verbose);
			// set default rules by SUTime language
			language = props.GetProperty(name + ".language", language);
			if (!languageToRulesFiles.Keys.Contains(language))
			{
				language = "english";
			}
			grammarFilename = languageToRulesFiles[language];
			// override if rules are set by properties
			grammarFilename = props.GetProperty(name + ".rules", grammarFilename);
			searchForDocDate = PropertiesUtils.GetBool(props, name + ".searchForDocDate", searchForDocDate);
			string binderProperty = props.GetProperty(name + ".binders");
			int nBinders;
			string[] binderClasses;
			if (binderProperty == null)
			{
				nBinders = DefaultBinders.Length;
				binderClasses = DefaultBinders;
			}
			else
			{
				nBinders = PropertiesUtils.GetInt(props, name + ".binders", 0);
				binderClasses = new string[nBinders];
				for (int i = 0; i < nBinders; ++i)
				{
					string binderPrefix = name + ".binder." + (i + 1);
					binderClasses[i] = props.GetProperty(binderPrefix);
				}
			}
			if (nBinders > 0 && Runtime.GetProperty("STS") == null)
			{
				binders = new Env.IBinder[nBinders];
				for (int i = 0; i < nBinders; i++)
				{
					int bi = i + 1;
					string binderPrefix = name + ".binder." + bi;
					try
					{
						Type binderClass = Sharpen.Runtime.GetType(binderClasses[i]);
						binderPrefix = binderPrefix + ".";
						binders[i] = (Env.IBinder)System.Activator.CreateInstance(binderClass);
						binders[i].Init(binderPrefix, props);
					}
					catch (Exception ex)
					{
						throw new Exception("Error initializing binder " + bi, ex);
					}
				}
			}
		}
	}
}
