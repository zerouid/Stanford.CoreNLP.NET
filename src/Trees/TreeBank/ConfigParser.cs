using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <author>Spence Green</author>
	public class ConfigParser : IEnumerable<Properties>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Treebank.ConfigParser));

		private const string Delim = "=";

		public const string paramName = "NAME";

		public const string paramPath = "PATH";

		public const string paramOutputPath = "OUTPUT_PATH";

		public const string paramSplit = "SPLIT";

		public const string paramEncode = "OUTPUT_ENCODING";

		public const string paramMapping = "MAPPING";

		public const string paramDistrib = "DISTRIB";

		public const string paramType = "TYPE";

		public const string paramFlat = "FLAT";

		public const string paramDT = "USEDET";

		public const string paramTagDelim = "TAGDELIM";

		public const string paramFileExt = "FILEEXT";

		public const string paramLexMapper = "LEXMAPPER";

		public const string paramLexMapOptions = "LEXOPTS";

		public const string paramNoDashTags = "NODASHTAGS";

		public const string paramAddRoot = "ADDROOT";

		public const string paramUnEscape = "UNESCAPE";

		public const string paramPosMapper = "POSMAPPER";

		public const string paramPosMapOptions = "POSOPTS";

		public const string paramMaxLen = "MAXLEN";

		public const string paramMorph = "MORPH";

		public const string paramTransform = "TVISITOR";

		public const string paramCCTagset = "CC_TAGSET";

		private static readonly Pattern matchName = Pattern.Compile(paramName + Delim);

		private static readonly Pattern matchSplit = Pattern.Compile(paramSplit + Delim);

		private static readonly Pattern matchDistrib = Pattern.Compile(paramDistrib + Delim);

		private static readonly Pattern matchType = Pattern.Compile(paramType + Delim);

		private static readonly Pattern matchFlat = Pattern.Compile(paramFlat + Delim);

		private static readonly Pattern matchDT = Pattern.Compile(paramDT + Delim);

		private static readonly Pattern matchTagDelim = Pattern.Compile(paramTagDelim + Delim);

		private static readonly Pattern matchFileExt = Pattern.Compile(paramFileExt + Delim);

		private static readonly Pattern matchLexMapper = Pattern.Compile(paramLexMapper + Delim);

		private static readonly Pattern matchNoDashTags = Pattern.Compile(paramNoDashTags + Delim);

		private static readonly Pattern matchAddRoot = Pattern.Compile(paramAddRoot + Delim);

		private static readonly Pattern matchUnEscape = Pattern.Compile(paramUnEscape + Delim);

		private static readonly Pattern matchLexMapOptions = Pattern.Compile(paramLexMapOptions + Delim);

		private static readonly Pattern matchPosMapper = Pattern.Compile(paramPosMapper + Delim);

		private static readonly Pattern matchPosMapOptions = Pattern.Compile(paramPosMapOptions + Delim);

		private static readonly Pattern matchMaxLen = Pattern.Compile(paramMaxLen + Delim);

		private static readonly Pattern matchMorph = Pattern.Compile(paramMorph + Delim);

		private static readonly Pattern matchTransform = Pattern.Compile(paramTransform + Delim);

		private static readonly Pattern matchEncode = Pattern.Compile(paramEncode + Delim);

		private static readonly Pattern matchEncodeArgs = Pattern.Compile("Buckwalter|UTF8");

		private static readonly Pattern matchCCTagset = Pattern.Compile(paramCCTagset + Delim);

		private static readonly Pattern booleanArgs = Pattern.Compile("true|false");

		public static readonly Pattern matchPath = Pattern.Compile(paramPath);

		public static readonly Pattern matchOutputPath = Pattern.Compile(paramOutputPath);

		public static readonly Pattern matchMapping = Pattern.Compile(paramMapping);

		private static readonly Pattern setDelim = Pattern.Compile(";;");

		private static readonly Pattern skipLine = Pattern.Compile("^#|^\\s*$");

		private readonly IList<Properties> datasetList;

		private readonly IDictionary<string, Pair<Pattern, Pattern>> patternsMap;

		private readonly string configFile;

		public ConfigParser(string filename)
		{
			//The parameter names and delimiter
			//Name of the dataset
			//Path to files in the dataset
			// Where to output the results
			//A file listing filenames in a split
			//Preferred output encoding [Buckwalter | UTF8]
			//Path to an LDC-format POS tag mapping file
			//Add to distribution or not [true | false]
			//Specify the Dataset type to use
			//Output terminals only
			//Add a determiner to the Bies tag ("Stanfordization")
			//Delimiter for separating words and tags in tagger datasets
			//File extension for the treebank files
			//Class name for the LexMapper to use
			//Option string for the lexmapper (comma-separated)
			//Remove ATB dash tags
			//Add a node "ROOT" to every tree
			//Remove LDC/ATB special characters from flattened output
			//Class name for POS mapper to use
			//Options string for the posmapper (comma-separated)
			//Max yield of the trees in the data set
			//Add the pre-terminal morphological analysis to the leaf (using the delimiter)
			//Apply a custom TreeVisitor to each tree in the dataset
			// specific to French.  TODO: move it to the French dataset
			//Absolute parameters
			//Pre-fix parameters
			//Patterns for the parser
			//Other members
			configFile = filename;
			datasetList = new List<Properties>();
			//For Pair<Pattern,Pattern>, the first pattern matches the parameter name
			//while the second (optionally) accepts the parameter values
			patternsMap = Generics.NewHashMap();
			patternsMap[paramName] = new Pair<Pattern, Pattern>(matchName, null);
			patternsMap[paramType] = new Pair<Pattern, Pattern>(matchType, null);
			patternsMap[paramPath] = new Pair<Pattern, Pattern>(matchPath, null);
			patternsMap[paramOutputPath] = new Pair<Pattern, Pattern>(matchOutputPath, null);
			patternsMap[paramSplit] = new Pair<Pattern, Pattern>(matchSplit, null);
			patternsMap[paramTagDelim] = new Pair<Pattern, Pattern>(matchTagDelim, null);
			patternsMap[paramFileExt] = new Pair<Pattern, Pattern>(matchFileExt, null);
			patternsMap[paramEncode] = new Pair<Pattern, Pattern>(matchEncode, matchEncodeArgs);
			patternsMap[paramMapping] = new Pair<Pattern, Pattern>(matchMapping, null);
			patternsMap[paramDistrib] = new Pair<Pattern, Pattern>(matchDistrib, booleanArgs);
			patternsMap[paramFlat] = new Pair<Pattern, Pattern>(matchFlat, booleanArgs);
			patternsMap[paramDT] = new Pair<Pattern, Pattern>(matchDT, booleanArgs);
			patternsMap[paramLexMapper] = new Pair<Pattern, Pattern>(matchLexMapper, null);
			patternsMap[paramNoDashTags] = new Pair<Pattern, Pattern>(matchNoDashTags, booleanArgs);
			patternsMap[paramAddRoot] = new Pair<Pattern, Pattern>(matchAddRoot, booleanArgs);
			patternsMap[paramUnEscape] = new Pair<Pattern, Pattern>(matchUnEscape, booleanArgs);
			patternsMap[paramLexMapOptions] = new Pair<Pattern, Pattern>(matchLexMapOptions, null);
			patternsMap[paramPosMapper] = new Pair<Pattern, Pattern>(matchPosMapper, null);
			patternsMap[paramPosMapOptions] = new Pair<Pattern, Pattern>(matchPosMapOptions, null);
			patternsMap[paramMaxLen] = new Pair<Pattern, Pattern>(matchMaxLen, null);
			patternsMap[paramMorph] = new Pair<Pattern, Pattern>(matchMorph, null);
			patternsMap[paramTransform] = new Pair<Pattern, Pattern>(matchTransform, null);
			patternsMap[paramCCTagset] = new Pair<Pattern, Pattern>(matchCCTagset, null);
		}

		public virtual IEnumerator<Properties> GetEnumerator()
		{
			IEnumerator<Properties> itr = Java.Util.Collections.UnmodifiableList(datasetList).GetEnumerator();
			return itr;
		}

		public virtual void Parse()
		{
			int lineNum = 0;
			try
			{
				LineNumberReader reader = new LineNumberReader(new FileReader(configFile));
				Properties paramsForDataset = null;
				while (reader.Ready())
				{
					string line = reader.ReadLine();
					lineNum = reader.GetLineNumber();
					//For exception handling
					Matcher m = skipLine.Matcher(line);
					if (m.LookingAt())
					{
						continue;
					}
					m = setDelim.Matcher(line);
					if (m.Matches() && paramsForDataset != null)
					{
						datasetList.Add(paramsForDataset);
						paramsForDataset = null;
						continue;
					}
					else
					{
						if (paramsForDataset == null)
						{
							paramsForDataset = new Properties();
						}
					}
					bool matched = false;
					foreach (string param in patternsMap.Keys)
					{
						Pair<Pattern, Pattern> paramTemplate = patternsMap[param];
						Matcher paramToken = paramTemplate.first.Matcher(line);
						if (paramToken.LookingAt())
						{
							matched = true;
							string[] tokens = line.Split(Delim);
							if (tokens.Length != 2)
							{
								System.Console.Error.Printf("%s: Skipping malformed parameter in %s (line %d)%n", this.GetType().FullName, configFile, reader.GetLineNumber());
								break;
							}
							string actualParam = tokens[0].Trim();
							string paramValue = tokens[1].Trim();
							if (paramTemplate.second != null)
							{
								paramToken = paramTemplate.second.Matcher(paramValue);
								if (paramToken.Matches())
								{
									paramsForDataset.SetProperty(actualParam, paramValue);
								}
								else
								{
									System.Console.Error.Printf("%s: Skipping illegal parameter value in %s (line %d)%n", this.GetType().FullName, configFile, reader.GetLineNumber());
									break;
								}
							}
							else
							{
								paramsForDataset.SetProperty(actualParam, paramValue);
							}
						}
					}
					if (!matched)
					{
						string error = this.GetType().FullName + ": Unknown token in " + configFile + " (line " + reader.GetLineNumber() + ")%n";
						System.Console.Error.Printf(error);
						throw new ArgumentException(error);
					}
				}
				if (paramsForDataset != null)
				{
					datasetList.Add(paramsForDataset);
				}
				reader.Close();
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Cannot open file %s%n", this.GetType().FullName, configFile);
			}
			catch (IOException)
			{
				System.Console.Error.Printf("%s: Error reading %s (line %d)%n", this.GetType().FullName, configFile, lineNum);
			}
		}

		public override string ToString()
		{
			int numDatasets = datasetList.Count;
			StringBuilder sb = new StringBuilder(string.Format("Loaded %d datasets: %n", numDatasets));
			int dataSetNum = 1;
			foreach (Properties sm in datasetList)
			{
				if (sm.Contains(paramName))
				{
					sb.Append(string.Format(" %d: %s%n", dataSetNum++, sm.GetProperty(paramName)));
				}
				else
				{
					sb.Append(string.Format(" %d: %s%n", dataSetNum++, "UNKNOWN NAME"));
				}
			}
			return sb.ToString();
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Trees.Treebank.ConfigParser cp = new Edu.Stanford.Nlp.Trees.Treebank.ConfigParser("configurations/sample.conf");
			cp.Parse();
			System.Console.Out.WriteLine(cp.ToString());
			foreach (Properties sm in cp)
			{
				System.Console.Out.WriteLine("--------------------");
				foreach (string key in sm.StringPropertyNames())
				{
					System.Console.Out.Printf(" %s: %s%n", key, sm[key]);
				}
			}
		}
	}
}
