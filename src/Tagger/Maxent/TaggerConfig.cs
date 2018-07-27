using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Reads and stores configuration information for a POS tagger.</summary>
	/// <remarks>
	/// Reads and stores configuration information for a POS tagger.
	/// <i>Implementation note:</i> To add a new parameter: (1) define a default
	/// String value, (2) add it to defaultValues map, (3) add line to constructor,
	/// (4) add getter method, (5) add to dump() method, (6) add to printGenProps()
	/// method, (7) add to class javadoc of MaxentTagger.
	/// </remarks>
	/// <author>William Morgan</author>
	/// <author>Anna Rafferty</author>
	/// <author>Michel Galley</author>
	[System.Serializable]
	public class TaggerConfig : Properties
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.TaggerConfig));

		private const long serialVersionUID = -4136407850147157497L;

		public enum Mode
		{
			Train,
			Test,
			Tag,
			Dump
		}

		public const string Search = "qn";

		public const string TagSeparator = "/";

		public const string Tokenize = "true";

		public const string Debug = "false";

		public const string Iterations = "100";

		public const string Arch = string.Empty;

		public const string WordFunction = string.Empty;

		public const string RareWordThresh = "5";

		public const string MinFeatureThresh = "5";

		public const string CurWordMinFeatureThresh = "2";

		public const string RareWordMinFeatureThresh = "10";

		public const string VeryCommonWordThresh = "250";

		public const string OccurringTagsOnly = "false";

		public const string PossibleTagsOnly = "false";

		public static readonly string SigmaSquared = 0.5.ToString();

		public const string Encoding = "UTF-8";

		public const string LearnClosedClass = "false";

		public const string ClosedClassThreshold = "40";

		public const string Verbose = "false";

		public const string VerboseResults = "true";

		public const string Sgml = "false";

		public const string Lang = string.Empty;

		public const string TokenizerFactory = string.Empty;

		public const string XmlInput = string.Empty;

		public const string TagInside = string.Empty;

		public const string Approximate = "-1.0";

		public const string TokenizerOptions = string.Empty;

		public const string DefaultRegL1 = "1.0";

		public const string OutputFile = string.Empty;

		public const string OutputFormat = "slashTags";

		public const string OutputFormatOptions = string.Empty;

		public const string Nthreads = "1";

		public const string EncodingProperty = "encoding";

		public const string TagSeparatorProperty = "tagSeparator";

		private static readonly IDictionary<string, string> defaultValues = Generics.NewHashMap();

		static TaggerConfig()
		{
			/* Inherits implementation of Serializable! */
			/* defaults. sentenceDelimiter might be null; the others all have non-null values. */
			defaultValues["arch"] = Arch;
			defaultValues["wordFunction"] = WordFunction;
			defaultValues["closedClassTags"] = string.Empty;
			defaultValues["closedClassTagThreshold"] = ClosedClassThreshold;
			defaultValues["search"] = Search;
			defaultValues[TagSeparatorProperty] = TagSeparator;
			defaultValues["tokenize"] = Tokenize;
			defaultValues["debug"] = Debug;
			defaultValues["iterations"] = Iterations;
			defaultValues["rareWordThresh"] = RareWordThresh;
			defaultValues["minFeatureThresh"] = MinFeatureThresh;
			defaultValues["curWordMinFeatureThresh"] = CurWordMinFeatureThresh;
			defaultValues["rareWordMinFeatureThresh"] = RareWordMinFeatureThresh;
			defaultValues["veryCommonWordThresh"] = VeryCommonWordThresh;
			defaultValues["occurringTagsOnly"] = OccurringTagsOnly;
			defaultValues["possibleTagsOnly"] = PossibleTagsOnly;
			defaultValues["sigmaSquared"] = SigmaSquared;
			defaultValues[EncodingProperty] = Encoding;
			defaultValues["learnClosedClassTags"] = LearnClosedClass;
			defaultValues["verbose"] = Verbose;
			defaultValues["verboseResults"] = VerboseResults;
			defaultValues["openClassTags"] = string.Empty;
			defaultValues["lang"] = Lang;
			defaultValues["tokenizerFactory"] = TokenizerFactory;
			defaultValues["xmlInput"] = XmlInput;
			defaultValues["tagInside"] = TagInside;
			defaultValues["sgml"] = Sgml;
			defaultValues["approximate"] = Approximate;
			defaultValues["tokenizerOptions"] = TokenizerOptions;
			defaultValues["regL1"] = DefaultRegL1;
			defaultValues["outputFile"] = OutputFile;
			defaultValues["outputFormat"] = OutputFormat;
			defaultValues["outputFormatOptions"] = OutputFormatOptions;
			defaultValues["nthreads"] = Nthreads;
		}

		/// <summary>This constructor is just for creating an instance with default values.</summary>
		/// <remarks>
		/// This constructor is just for creating an instance with default values.
		/// Used internally.
		/// </remarks>
		private TaggerConfig()
			: base()
		{
			this.PutAll(defaultValues);
		}

		/// <summary>
		/// We force you to pass in a TaggerConfig rather than any other
		/// superclass so that we know the arg error checking has already occurred
		/// </summary>
		public TaggerConfig(Edu.Stanford.Nlp.Tagger.Maxent.TaggerConfig old)
			: base(old)
		{
		}

		public TaggerConfig(params string[] args)
			: this(StringUtils.ArgsToProperties(args))
		{
		}

		public TaggerConfig(Properties props)
			: this()
		{
			// load up the default properties
			/* Try and use the default properties from the model */
			// Properties modelProps = new Properties();
			// TaggerConfig oldConfig = new TaggerConfig(); // loads default values in oldConfig
			if (!props.Contains("trainFile"))
			{
				string name = props.GetProperty("model");
				if (name == null)
				{
					name = props.GetProperty("dump");
				}
				if (name != null)
				{
					try
					{
						using (DataInputStream @in = new DataInputStream(IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(name)))
						{
							log.Info("Loading default properties from tagger " + name);
							this.PutAll(Edu.Stanford.Nlp.Tagger.Maxent.TaggerConfig.ReadConfig(@in));
						}
					}
					catch (Exception)
					{
						// overwrites defaults with any serialized values.
						throw new RuntimeIOException("No such trained tagger config file found: " + name);
					}
				}
			}
			SetProperties(props);
		}

		public virtual void SetProperties(Properties props)
		{
			if (props.GetProperty(string.Empty) != null)
			{
				throw new Exception("unknown argument(s): \"" + props.GetProperty(string.Empty) + '\"');
			}
			if (props.GetProperty("genprops") != null)
			{
				PrintGenProps(System.Console.Out);
				System.Environment.Exit(0);
			}
			if (props.Contains("mode") && props.Contains("file"))
			{
				this.SetProperty("mode", props.GetProperty("mode"));
				this.SetProperty("file", props.GetProperty("file"));
			}
			else
			{
				if (props.Contains("trainFile"))
				{
					//Training mode
					this.SetProperty("mode", TaggerConfig.Mode.Train.ToString());
					this.SetProperty("file", props.GetProperty("trainFile", string.Empty).Trim());
				}
				else
				{
					if (props.Contains("testFile"))
					{
						//Testing mode
						this.SetProperty("mode", TaggerConfig.Mode.Test.ToString());
						this.SetProperty("file", props.GetProperty("testFile", string.Empty).Trim());
					}
					else
					{
						if (props.Contains("textFile"))
						{
							//Tagging mode
							this.SetProperty("mode", TaggerConfig.Mode.Tag.ToString());
							this.SetProperty("file", props.GetProperty("textFile", string.Empty).Trim());
						}
						else
						{
							if (props.Contains("dump"))
							{
								this.SetProperty("mode", TaggerConfig.Mode.Dump.ToString());
								// this.setProperty("file", props.getProperty("dump").trim());
								props.SetProperty("model", props.GetProperty("dump").Trim());
							}
							else
							{
								this.SetProperty("mode", TaggerConfig.Mode.Tag.ToString());
								this.SetProperty("file", "stdin");
							}
						}
					}
				}
			}
			//for any mode other than train, we load a classifier, which means we load a config - model always needs to be specified
			//on command line/in props file
			//Get the path to the model (or the path where you'd like to save the model); this is necessary for training, testing, and tagging
			this.SetProperty("model", props.GetProperty("model", this.GetProperty("model", string.Empty)).Trim());
			if (!(this.GetMode() == TaggerConfig.Mode.Dump) && this.GetProperty("model").IsEmpty())
			{
				throw new Exception("'model' parameter must be specified");
			}
			this.SetProperty("search", props.GetProperty("search", this.GetProperty("search")).Trim().ToLower());
			string srch = this.GetProperty("search");
			if (!(srch.Equals("cg") || srch.Equals("iis") || srch.Equals("owlqn") || srch.Equals("qn") || srch.Equals("owlqn2")))
			{
				throw new Exception("'search' must be one of 'iis', 'cg', 'qn' or 'owlqn' or 'owlqn2': " + srch);
			}
			this.SetProperty("sigmaSquared", props.GetProperty("sigmaSquared", this.GetProperty("sigmaSquared")));
			this.SetProperty(TagSeparatorProperty, props.GetProperty(TagSeparatorProperty, this.GetProperty(TagSeparatorProperty)));
			this.SetProperty("iterations", props.GetProperty("iterations", this.GetProperty("iterations")));
			this.SetProperty("rareWordThresh", props.GetProperty("rareWordThresh", this.GetProperty("rareWordThresh")));
			this.SetProperty("minFeatureThresh", props.GetProperty("minFeatureThresh", this.GetProperty("minFeatureThresh")));
			this.SetProperty("curWordMinFeatureThresh", props.GetProperty("curWordMinFeatureThresh", this.GetProperty("curWordMinFeatureThresh")));
			this.SetProperty("rareWordMinFeatureThresh", props.GetProperty("rareWordMinFeatureThresh", this.GetProperty("rareWordMinFeatureThresh")));
			this.SetProperty("veryCommonWordThresh", props.GetProperty("veryCommonWordThresh", this.GetProperty("veryCommonWordThresh")));
			this.SetProperty("occurringTagsOnly", props.GetProperty("occurringTagsOnly", this.GetProperty("occurringTagsOnly", OccurringTagsOnly)));
			this.SetProperty("possibleTagsOnly", props.GetProperty("possibleTagsOnly", this.GetProperty("possibleTagsOnly")));
			this.SetProperty("lang", props.GetProperty("lang", this.GetProperty("lang")));
			this.SetProperty("openClassTags", props.GetProperty("openClassTags", this.GetProperty("openClassTags")).Trim());
			this.SetProperty("closedClassTags", props.GetProperty("closedClassTags", this.GetProperty("closedClassTags")).Trim());
			this.SetProperty("learnClosedClassTags", props.GetProperty("learnClosedClassTags", this.GetProperty("learnClosedClassTags")));
			this.SetProperty("closedClassTagThreshold", props.GetProperty("closedClassTagThreshold", this.GetProperty("closedClassTagThreshold")));
			this.SetProperty("arch", props.GetProperty("arch", this.GetProperty("arch")));
			if (this.GetMode() == TaggerConfig.Mode.Train && this.GetProperty("arch").IsEmpty())
			{
				throw new ArgumentException("No architecture specified; " + "set the -arch flag with " + "the features to be used");
			}
			this.SetProperty("wordFunction", props.GetProperty("wordFunction", this.GetProperty("wordFunction", WordFunction)));
			this.SetProperty("tokenize", props.GetProperty("tokenize", this.GetProperty("tokenize")));
			this.SetProperty("tokenizerFactory", props.GetProperty("tokenizerFactory", this.GetProperty("tokenizerFactory")));
			this.SetProperty("debugPrefix", props.GetProperty("debugPrefix", this.GetProperty("debugPrefix", string.Empty)));
			this.SetProperty("debug", props.GetProperty("debug", Debug));
			this.SetProperty(EncodingProperty, props.GetProperty(EncodingProperty, this.GetProperty(EncodingProperty)));
			this.SetProperty("sgml", props.GetProperty("sgml", this.GetProperty("sgml")));
			this.SetProperty("verbose", props.GetProperty("verbose", this.GetProperty("verbose")));
			this.SetProperty("verboseResults", props.GetProperty("verboseResults", this.GetProperty("verboseResults")));
			this.SetProperty("regL1", props.GetProperty("regL1", this.GetProperty("regL1")));
			//this is a property that is stored (not like the general properties)
			this.SetProperty("xmlInput", props.GetProperty("xmlInput", this.GetProperty("xmlInput")).Trim());
			this.SetProperty("tagInside", props.GetProperty("tagInside", this.GetProperty("tagInside")));
			//this isn't something we save from time to time
			this.SetProperty("approximate", props.GetProperty("approximate", this.GetProperty("approximate")));
			//this isn't something we save from time to time
			this.SetProperty("tokenizerOptions", props.GetProperty("tokenizerOptions", this.GetProperty("tokenizerOptions")));
			//this isn't something we save from time to time
			this.SetProperty("outputFile", props.GetProperty("outputFile", this.GetProperty("outputFile")).Trim());
			//this isn't something we save from time to time
			this.SetProperty("outputFormat", props.GetProperty("outputFormat", this.GetProperty("outputFormat")).Trim());
			//this isn't something we save from time to time
			this.SetProperty("outputFormatOptions", props.GetProperty("outputFormatOptions", this.GetProperty("outputFormatOptions")).Trim());
			//this isn't something we save from time to time
			this.SetProperty("nthreads", props.GetProperty("nthreads", this.GetProperty("nthreads", Nthreads)).Trim());
			string sentenceDelimiter = props.GetProperty("sentenceDelimiter", this.GetProperty("sentenceDelimiter"));
			if (sentenceDelimiter != null)
			{
				// this isn't something we save from time to time.
				// It is only relevant when tagging text files.
				// In fact, we let this one be null, as it really is useful to
				// let the null value represent no sentence delimiter.
				this.SetProperty("sentenceDelimiter", sentenceDelimiter);
			}
		}

		public virtual string GetModel()
		{
			return GetProperty("model");
		}

		public virtual string GetFile()
		{
			return GetProperty("file");
		}

		public virtual string GetOutputFile()
		{
			return GetProperty("outputFile");
		}

		public virtual string GetOutputFormat()
		{
			return GetProperty("outputFormat");
		}

		public virtual string[] GetOutputOptions()
		{
			return GetProperty("outputFormatOptions").Split("\\s*,\\s*");
		}

		public virtual bool GetOutputVerbosity()
		{
			return GetOutputOptionsContains("verbose");
		}

		public virtual bool GetOutputLemmas()
		{
			return GetOutputOptionsContains("lemmatize");
		}

		public virtual bool KeepEmptySentences()
		{
			return GetOutputOptionsContains("keepEmptySentences");
		}

		public virtual bool GetOutputOptionsContains(string sought)
		{
			string[] options = GetOutputOptions();
			foreach (string option in options)
			{
				if (option.Equals(sought))
				{
					return true;
				}
			}
			return false;
		}

		public virtual string GetSearch()
		{
			return GetProperty("search");
		}

		public virtual double GetSigmaSquared()
		{
			return double.ParseDouble(GetProperty("sigmaSquared"));
		}

		public virtual int GetIterations()
		{
			return System.Convert.ToInt32(GetProperty("iterations"));
		}

		public virtual int GetRareWordThresh()
		{
			return System.Convert.ToInt32(GetProperty("rareWordThresh"));
		}

		public virtual int GetMinFeatureThresh()
		{
			return System.Convert.ToInt32(GetProperty("minFeatureThresh"));
		}

		public virtual int GetCurWordMinFeatureThresh()
		{
			return System.Convert.ToInt32(GetProperty("curWordMinFeatureThresh"));
		}

		public virtual int GetRareWordMinFeatureThresh()
		{
			return System.Convert.ToInt32(GetProperty("rareWordMinFeatureThresh"));
		}

		public virtual int GetVeryCommonWordThresh()
		{
			return System.Convert.ToInt32(GetProperty("veryCommonWordThresh"));
		}

		public virtual bool OccurringTagsOnly()
		{
			return bool.ParseBoolean(GetProperty("occurringTagsOnly"));
		}

		public virtual bool PossibleTagsOnly()
		{
			return bool.ParseBoolean(GetProperty("possibleTagsOnly"));
		}

		public virtual string GetLang()
		{
			return GetProperty("lang");
		}

		public virtual string[] GetOpenClassTags()
		{
			return WsvStringToStringArray(GetProperty("openClassTags"));
		}

		public virtual string[] GetClosedClassTags()
		{
			return WsvStringToStringArray(GetProperty("closedClassTags"));
		}

		private static string[] WsvStringToStringArray(string str)
		{
			if (StringUtils.IsNullOrEmpty(str))
			{
				return StringUtils.EmptyStringArray;
			}
			else
			{
				return str.Split("\\s+");
			}
		}

		public virtual bool GetLearnClosedClassTags()
		{
			return bool.ParseBoolean(GetProperty("learnClosedClassTags"));
		}

		public virtual int GetClosedTagThreshold()
		{
			return System.Convert.ToInt32(GetProperty("closedClassTagThreshold"));
		}

		public virtual string GetArch()
		{
			return GetProperty("arch");
		}

		public virtual string GetWordFunction()
		{
			return GetProperty("wordFunction");
		}

		public virtual bool GetDebug()
		{
			return bool.ParseBoolean(GetProperty("debug"));
		}

		public virtual string GetDebugPrefix()
		{
			return GetProperty("debugPrefix");
		}

		public virtual string GetTokenizerFactory()
		{
			return GetProperty("tokenizerFactory");
		}

		public static string GetDefaultTagSeparator()
		{
			return TagSeparator;
		}

		public string GetTagSeparator()
		{
			return GetProperty(TagSeparatorProperty);
		}

		public virtual bool GetTokenize()
		{
			return bool.ParseBoolean(GetProperty("tokenize"));
		}

		public virtual string GetEncoding()
		{
			return GetProperty(EncodingProperty);
		}

		public virtual double GetRegL1()
		{
			return double.ParseDouble(GetProperty("regL1"));
		}

		public virtual string[] GetXMLInput()
		{
			return WsvStringToStringArray(GetProperty("xmlInput"));
		}

		public virtual bool GetVerbose()
		{
			return bool.ParseBoolean(GetProperty("verbose"));
		}

		public virtual bool GetVerboseResults()
		{
			return bool.ParseBoolean(GetProperty("verboseResults"));
		}

		public virtual bool GetSGML()
		{
			return bool.ParseBoolean(GetProperty("sgml"));
		}

		public virtual int GetNThreads()
		{
			return System.Convert.ToInt32(GetProperty("nthreads"));
		}

		/// <summary>Return a regex of XML elements to tag inside of.</summary>
		/// <remarks>
		/// Return a regex of XML elements to tag inside of.  This may return an
		/// empty String, but never null.
		/// </remarks>
		/// <returns>A regex of XML elements to tag inside of</returns>
		public virtual string GetTagInside()
		{
			string str = GetProperty("tagInside");
			if (str == null)
			{
				return string.Empty;
			}
			return str;
		}

		public virtual string GetTokenizerOptions()
		{
			return GetProperty("tokenizerOptions");
		}

		public virtual bool GetTokenizerInvertible()
		{
			string tokenizerOptions = GetTokenizerOptions();
			if (tokenizerOptions != null && tokenizerOptions.Matches("(^|.*,)invertible=true"))
			{
				return true;
			}
			return GetOutputVerbosity() || GetOutputLemmas();
		}

		/// <summary>
		/// Returns a default score to be used for each tag that is incompatible with
		/// the current word (e.g., the tag CC for the word "apple").
		/// </summary>
		/// <remarks>
		/// Returns a default score to be used for each tag that is incompatible with
		/// the current word (e.g., the tag CC for the word "apple"). Using a default
		/// score may slightly decrease performance for some languages (e.g., Chinese and
		/// German), but allows the tagger to run considerably faster (since the computation
		/// of the normalization term Z requires much less feature extraction). This approximation
		/// does not decrease performance in English (on the WSJ). If this function returns
		/// 0.0, the tagger will compute exact scores.
		/// </remarks>
		/// <returns>default score</returns>
		public virtual double GetDefaultScore()
		{
			string approx = GetProperty("approximate");
			if (Sharpen.Runtime.EqualsIgnoreCase("false", approx))
			{
				return -1.0;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("true", approx))
				{
					return 1.0;
				}
				else
				{
					return double.ParseDouble(approx);
				}
			}
		}

		public virtual void Dump()
		{
			Dump(new PrintWriter(System.Console.Error));
		}

		public virtual void Dump(TextWriter stream)
		{
			PrintWriter pw = new PrintWriter(stream);
			Dump(pw);
		}

		private void Dump(PrintWriter pw)
		{
			pw.Println("                   model = " + GetProperty("model"));
			pw.Println("                    arch = " + GetProperty("arch"));
			pw.Println("            wordFunction = " + GetProperty("wordFunction"));
			if (this.GetMode() == TaggerConfig.Mode.Train || this.GetMode() == TaggerConfig.Mode.Dump)
			{
				pw.Println("               trainFile = " + GetProperty("file"));
			}
			else
			{
				if (this.GetMode() == TaggerConfig.Mode.Tag)
				{
					pw.Println("                textFile = " + GetProperty("file"));
				}
				else
				{
					if (this.GetMode() == TaggerConfig.Mode.Test)
					{
						pw.Println("                testFile = " + GetProperty("file"));
					}
				}
			}
			pw.Println("         closedClassTags = " + GetProperty("closedClassTags"));
			pw.Println(" closedClassTagThreshold = " + GetProperty("closedClassTagThreshold"));
			pw.Println(" curWordMinFeatureThresh = " + GetProperty("curWordMinFeatureThresh"));
			pw.Println("                   debug = " + GetProperty("debug"));
			pw.Println("             debugPrefix = " + GetProperty("debugPrefix"));
			pw.Println("            " + TagSeparatorProperty + " = " + GetProperty(TagSeparatorProperty));
			pw.Println("                " + EncodingProperty + " = " + GetProperty(EncodingProperty));
			pw.Println("              iterations = " + GetProperty("iterations"));
			pw.Println("                    lang = " + GetProperty("lang"));
			pw.Println("    learnClosedClassTags = " + GetProperty("learnClosedClassTags"));
			pw.Println("        minFeatureThresh = " + GetProperty("minFeatureThresh"));
			pw.Println("           openClassTags = " + GetProperty("openClassTags"));
			pw.Println("rareWordMinFeatureThresh = " + GetProperty("rareWordMinFeatureThresh"));
			pw.Println("          rareWordThresh = " + GetProperty("rareWordThresh"));
			pw.Println("                  search = " + GetProperty("search"));
			pw.Println("                    sgml = " + GetProperty("sgml"));
			pw.Println("            sigmaSquared = " + GetProperty("sigmaSquared"));
			pw.Println("                   regL1 = " + GetProperty("regL1"));
			pw.Println("               tagInside = " + GetProperty("tagInside"));
			pw.Println("                tokenize = " + GetProperty("tokenize"));
			pw.Println("        tokenizerFactory = " + GetProperty("tokenizerFactory"));
			pw.Println("        tokenizerOptions = " + GetProperty("tokenizerOptions"));
			pw.Println("                 verbose = " + GetProperty("verbose"));
			pw.Println("          verboseResults = " + GetProperty("verboseResults"));
			pw.Println("    veryCommonWordThresh = " + GetProperty("veryCommonWordThresh"));
			pw.Println("                xmlInput = " + GetProperty("xmlInput"));
			pw.Println("              outputFile = " + GetProperty("outputFile"));
			pw.Println("            outputFormat = " + GetProperty("outputFormat"));
			pw.Println("     outputFormatOptions = " + GetProperty("outputFormatOptions"));
			pw.Println("                nthreads = " + GetProperty("nthreads"));
			pw.Flush();
		}

		public override string ToString()
		{
			StringWriter sw = new StringWriter(200);
			PrintWriter pw = new PrintWriter(sw);
			Dump(pw);
			return sw.ToString();
		}

		/// <summary>
		/// This returns the sentence delimiter used when tokenizing text
		/// using the tokenizer requested in this config.
		/// </summary>
		/// <remarks>
		/// This returns the sentence delimiter used when tokenizing text
		/// using the tokenizer requested in this config.  In general, it is
		/// assumed the tokenizer doesn't need a sentence delimiter.... If you
		/// use the whitespace tokenizer, though, a newline breaks sentences.
		/// </remarks>
		/// <returns>A null String unless tokenize is false and then the String</returns>
		public virtual string GetSentenceDelimiter()
		{
			string delimiter = GetProperty("sentenceDelimiter");
			if (delimiter == null && !GetTokenize())
			{
				delimiter = "\n";
			}
			return delimiter;
		}

		/// <summary>
		/// Returns whether or not we should use stdin for reading when
		/// tagging data.
		/// </summary>
		/// <remarks>
		/// Returns whether or not we should use stdin for reading when
		/// tagging data.  For now, this returns true iff the filename given
		/// was "stdin".
		/// (TODO: kind of ugly)
		/// </remarks>
		public virtual bool UseStdin()
		{
			return Sharpen.Runtime.EqualsIgnoreCase(GetFile().Trim(), "stdin");
		}

		/// <summary>
		/// Prints out the automatically generated props file - in its own
		/// method to make code above easier to read
		/// </summary>
		private static void PrintGenProps(TextWriter @out)
		{
			@out.WriteLine("## Sample properties file for maxent tagger. This file is used for three main");
			@out.WriteLine("## operations: training, testing, and tagging. It may also be used to dump");
			@out.WriteLine("## the contents of a model.");
			@out.WriteLine("## To train or test a model, or to tag something, run:");
			@out.WriteLine("##   java edu.stanford.nlp.tagger.maxent.MaxentTagger -prop <properties file>");
			@out.WriteLine("## Arguments can be overridden on the commandline, e.g.:");
			@out.WriteLine("##   java ....MaxentTagger -prop <properties file> -testFile /other/file ");
			@out.WriteLine();
			@out.WriteLine("# Model file name (created at train time; used at tag and test time)");
			@out.WriteLine("# (you can leave this blank and specify it on the commandline with -model)");
			@out.WriteLine("# model = ");
			@out.WriteLine();
			@out.WriteLine("# Path to file to be operated on (trained from, tested against, or tagged)");
			@out.WriteLine("# Specify -textFile <filename> to tag text in the given file, -trainFile <filename> to");
			@out.WriteLine("# to train a model using data in the given file, or -testFile <filename> to test your");
			@out.WriteLine("# model using data in the given file.  Alternatively, you may specify");
			@out.WriteLine("# -dump <filename> to dump the parameters stored in a model or ");
			@out.WriteLine("# -convertToSingleFile <filename> to save an old, multi-file model (specified as -model)");
			@out.WriteLine("# to the new single file format.  The new model will be saved in the file filename.");
			@out.WriteLine("# If you choose to convert an old file, you must specify ");
			@out.WriteLine("# the correct 'arch' parameter used to create the original model.");
			@out.WriteLine("# trainFile = ");
			@out.WriteLine();
			@out.WriteLine("# Path to outputFile to write tagged output to.");
			@out.WriteLine("# If empty, stdout is used.");
			@out.WriteLine("# outputFile = " + OutputFile);
			@out.WriteLine();
			@out.WriteLine("# Output format. One of: slashTags (default), xml, or tsv");
			@out.WriteLine("# outputFormat = " + OutputFormat);
			@out.WriteLine();
			@out.WriteLine("# Output format options. Comma separated list.");
			@out.WriteLine("# currently \"lemmatize\" and \"keepEmptySentences\" are supported.");
			@out.WriteLine("# outputFormatOptions = " + OutputFormatOptions);
			@out.WriteLine();
			@out.WriteLine("# Tag separator character that separates word and pos tags");
			@out.WriteLine("# (for both training and test data) and used for");
			@out.WriteLine("# separating words and tags in slashTags format output.");
			@out.WriteLine("# tagSeparator = " + TagSeparator);
			@out.WriteLine();
			@out.WriteLine("# Encoding format in which files are stored.  If left blank, UTF-8 is assumed.");
			@out.WriteLine("# encoding = " + Encoding);
			@out.WriteLine();
			@out.WriteLine("# A couple flags for controlling the amount of output:");
			@out.WriteLine("# - print extra debugging information:");
			@out.WriteLine("# verbose = " + Verbose);
			@out.WriteLine("# - print intermediate results:");
			@out.WriteLine("# verboseResults = " + VerboseResults);
			@out.WriteLine("######### parameters for tag and test operations #########");
			@out.WriteLine();
			@out.WriteLine("# Class to use for tokenization. Default blank value means Penn Treebank");
			@out.WriteLine("# tokenization.  If you'd like to just assume that tokenization has been done,");
			@out.WriteLine("# and the input is whitespace-tokenized, use");
			@out.WriteLine("# edu.stanford.nlp.process.WhitespaceTokenizer or set tokenize to false.");
			@out.WriteLine("# tokenizerFactory = ");
			@out.WriteLine();
			@out.WriteLine("# Options to the tokenizer.  A comma separated list.");
			@out.WriteLine("# This depends on what the tokenizer supports.");
			@out.WriteLine("# For PTBTokenizer, you might try options like americanize=false");
			@out.WriteLine("# or asciiQuotes (for German!).");
			@out.WriteLine("# tokenizerOptions = ");
			@out.WriteLine();
			@out.WriteLine("# Whether to tokenize text for tag and test operations. Default is true.");
			@out.WriteLine("# If false, your text must already be whitespace tokenized.");
			@out.WriteLine("# tokenize = " + Tokenize);
			@out.WriteLine();
			@out.WriteLine("# Write debugging information (words, top words, unknown words). Useful for");
			@out.WriteLine("# error analysis. Default is false.");
			@out.WriteLine("# debug = " + Debug);
			@out.WriteLine();
			@out.WriteLine("# Prefix for debugging output (if debug == true). Default is to use the");
			@out.WriteLine("# filename from 'file'");
			@out.WriteLine("# debugPrefix = ");
			@out.WriteLine();
			@out.WriteLine("######### parameters for training  #########");
			@out.WriteLine();
			@out.WriteLine("# model architecture: This is one or more comma separated strings, which");
			@out.WriteLine("# specify which extractors to use. Some of them take one or more integer");
			@out.WriteLine("# or string ");
			@out.WriteLine("# (file path) arguments in parentheses, written as m, n, and s below:");
			@out.WriteLine("# 'left3words', 'left5words', 'bidirectional', 'bidirectional5words',");
			@out.WriteLine("# 'generic', 'sighan2005', 'german', 'words(m,n)', 'wordshapes(m,n)',");
			@out.WriteLine("# 'biwords(m,n)', 'lowercasewords(m,n)', 'vbn(n)', distsimconjunction(s,m,n)',");
			@out.WriteLine("# 'naacl2003unknowns', 'naacl2003conjunctions', 'distsim(s,m,n)',");
			@out.WriteLine("# 'suffix(n)', 'prefix(n)', 'prefixsuffix(n)', 'capitalizationsuffix(n)',");
			@out.WriteLine("# 'wordshapes(m,n)', 'unicodeshapes(m,n)', 'unicodeshapeconjunction(m,n)',");
			@out.WriteLine("# 'lctagfeatures', 'order(k)', 'chinesedictionaryfeatures(s)'.");
			@out.WriteLine("# These keywords determines the features extracted.  'generic' is language independent.");
			@out.WriteLine("# distsim: Distributional similarity classes can be an added source of information");
			@out.WriteLine("# about your words. An English distsim file is included, or you can use your own.");
			@out.WriteLine("# arch = ");
			@out.WriteLine();
			@out.WriteLine("# 'wordFunction'.  A function applied to the text before training or tagging.");
			@out.WriteLine("# For example, edu.stanford.nlp.util.LowercaseFunction");
			@out.WriteLine("# This function turns all the words into lowercase");
			@out.WriteLine("# The function must implement java.util.function.Function<String, String>");
			@out.WriteLine("# Blank means no preprocessing function");
			@out.WriteLine("# wordFunction = ");
			@out.WriteLine();
			@out.WriteLine("# 'language'.  This is really the tag set which is used for the");
			@out.WriteLine("# list of open-class tags, and perhaps deterministic  tag");
			@out.WriteLine("# expansion). Currently we have 'english', 'arabic', 'german', 'chinese'");
			@out.WriteLine("# or 'polish' predefined. For your own language, you can specify ");
			@out.WriteLine("# the same information via openClassTags or closedClassTags below");
			@out.WriteLine("# (only ONE of these three options may be specified). ");
			@out.WriteLine("# 'english' means UPenn English treebank tags. 'german' is STTS");
			@out.WriteLine("# 'chinese' is CTB, and Arabic is an expanded Bies mapping from the ATB");
			@out.WriteLine("# 'polish' means some tags that some guy on the internet once used. ");
			@out.WriteLine("# See the TTags class for more information.");
			@out.WriteLine("# lang = ");
			@out.WriteLine();
			@out.WriteLine("# a space-delimited list of open-class parts of speech");
			@out.WriteLine("# alternatively, you can specify language above to use a pre-defined list or specify the closed class tags (below)");
			@out.WriteLine("# openClassTags = ");
			@out.WriteLine();
			@out.WriteLine("# a space-delimited list of closed-class parts of speech");
			@out.WriteLine("# alternatively, you can specify language above to use a pre-defined list or specify the open class tags (above)");
			@out.WriteLine("# closedClassTags = ");
			@out.WriteLine();
			@out.WriteLine("# A boolean indicating whether you would like the trained model to set POS tags as closed");
			@out.WriteLine("# based on their frequency in training; default is false.  The frequency threshold can be set below. ");
			@out.WriteLine("# This option is ignored if any of {openClassTags, closedClassTags, lang} are specified.");
			@out.WriteLine("# learnClosedClassTags = ");
			@out.WriteLine();
			@out.WriteLine("# Used only if learnClosedClassTags=true.  Tags that have fewer tokens than this threshold are");
			@out.WriteLine("# considered closed in the trained model.");
			@out.WriteLine("# closedClassTagThreshold = ");
			@out.WriteLine();
			@out.WriteLine("# search method for optimization. Normally use the default 'qn'. choices: 'qn' (quasi-Newton),");
			@out.WriteLine("# 'cg' (conjugate gradient, 'owlqn' (L1 regularization) or 'iis' (improved iterative scaling)");
			@out.WriteLine("# search = " + Search);
			@out.WriteLine();
			@out.WriteLine("# for conjugate gradient or quasi-Newton search, sigma-squared smoothing/regularization");
			@out.WriteLine("# parameter. if left blank, the default is 0.5, which is usually okay");
			@out.WriteLine("# sigmaSquared = " + SigmaSquared);
			@out.WriteLine();
			@out.WriteLine("# for OWLQN search, regularization");
			@out.WriteLine("# parameter. if left blank, the default is 1.0, which is usually okay");
			@out.WriteLine("# regL1 = " + DefaultRegL1);
			@out.WriteLine();
			@out.WriteLine("# For improved iterative scaling, the number of iterations, otherwise ignored");
			@out.WriteLine("# iterations = " + Iterations);
			@out.WriteLine();
			@out.WriteLine("# rare word threshold. words that occur less than this number of");
			@out.WriteLine("# times are considered rare words.");
			@out.WriteLine("# rareWordThresh = " + RareWordThresh);
			@out.WriteLine();
			@out.WriteLine("# minimum feature threshold. features whose history appears less");
			@out.WriteLine("# than this number of times are ignored.");
			@out.WriteLine("# minFeatureThresh = " + MinFeatureThresh);
			@out.WriteLine();
			@out.WriteLine("# current word feature threshold. words that occur more than this");
			@out.WriteLine("# number of times will generate features with all of their occurring");
			@out.WriteLine("# tags.");
			@out.WriteLine("# curWordMinFeatureThresh = " + CurWordMinFeatureThresh);
			@out.WriteLine();
			@out.WriteLine("# rare word minimum feature threshold. features of rare words whose histories");
			@out.WriteLine("# appear less than this times will be ignored.");
			@out.WriteLine("# rareWordMinFeatureThresh = " + RareWordMinFeatureThresh);
			@out.WriteLine();
			@out.WriteLine("# very common word threshold. words that occur more than this number of");
			@out.WriteLine("# times will form an equivalence class by themselves. ignored unless");
			@out.WriteLine("# you are using equivalence classes.");
			@out.WriteLine("# veryCommonWordThresh = " + VeryCommonWordThresh);
			@out.WriteLine();
			@out.WriteLine("# sgml = ");
			@out.WriteLine("# tagInside = ");
			@out.WriteLine();
			@out.WriteLine("# testFile and textFile can use multiple threads to process text.");
			@out.WriteLine("# nthreads = " + Nthreads);
		}

		public virtual TaggerConfig.Mode GetMode()
		{
			if (!Contains("mode"))
			{
				return null;
			}
			return TaggerConfig.Mode.ValueOf(GetProperty("mode"));
		}

		/// <summary>Serialize the TaggerConfig.</summary>
		/// <param name="os">Where to write this TaggerConfig</param>
		/// <exception cref="System.IO.IOException">If any IO problems</exception>
		public virtual void SaveConfig(OutputStream os)
		{
			ObjectOutputStream @out = new ObjectOutputStream(os);
			@out.WriteObject(this);
		}

		/// <summary>Read in a TaggerConfig.</summary>
		/// <param name="stream">Where to read from</param>
		/// <returns>The TaggerConfig</returns>
		/// <exception cref="System.IO.IOException">Misc IOError</exception>
		/// <exception cref="System.TypeLoadException">Class error</exception>
		public static Edu.Stanford.Nlp.Tagger.Maxent.TaggerConfig ReadConfig(DataInputStream stream)
		{
			ObjectInputStream @in = new ObjectInputStream(stream);
			return (Edu.Stanford.Nlp.Tagger.Maxent.TaggerConfig)@in.ReadObject();
		}
	}
}
