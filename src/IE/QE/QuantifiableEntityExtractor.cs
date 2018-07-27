using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>Extracts quantifiable entities using rules.</summary>
	/// <author>Angel Chang</author>
	public class QuantifiableEntityExtractor
	{
		private Env env;

		private Options options;

		private CoreMapExpressionExtractor<MatchedExpression> extractor;

		public virtual SimpleQuantifiableEntity Get(double amount, string unitName)
		{
			return new SimpleQuantifiableEntity(amount, (Unit)env.Get(unitName));
		}

		public virtual IList<MatchedExpression> Extract(ICoreMap annotation)
		{
			if (!annotation.ContainsKey(typeof(CoreAnnotations.NumerizedTokensAnnotation)))
			{
				IList<ICoreMap> mergedNumbers = NumberNormalizer.FindAndMergeNumbers(annotation);
				annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), mergedNumbers);
			}
			return extractor.ExtractExpressions(annotation);
		}

		// Initializing
		public virtual void Init(string name, Properties props)
		{
			Init(new Options(name, props));
		}

		public virtual void Init(Options options)
		{
			this.options = options;
			InitEnv();
			extractor = CreateExtractor();
		}

		private CoreMapExpressionExtractor<MatchedExpression> CreateExtractor()
		{
			IList<string> filenames = StringUtils.Split(options.grammarFilename, "\\s*[,;]\\s*");
			return CoreMapExpressionExtractor.CreateExtractorFromFiles(env, filenames);
		}

		private void InitEnv()
		{
			env = TokenSequencePattern.GetNewEnv();
			env.SetDefaultTokensAnnotationKey(typeof(CoreAnnotations.NumerizedTokensAnnotation));
			// Do case insensitive matching
			env.SetDefaultStringMatchFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
			env.SetDefaultStringPatternFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
			try
			{
				Units.RegisterUnits(env, options.unitsFilename);
			}
			catch (IOException ex)
			{
				throw new Exception("Error loading units from " + options.unitsFilename, ex);
			}
			try
			{
				UnitPrefix.RegisterPrefixes(env, options.prefixFilename);
			}
			catch (IOException ex)
			{
				throw new Exception("Error loading prefixes from " + options.prefixFilename, ex);
			}
			env.Bind("options", options);
			env.Bind("numcomptype", typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
			env.Bind("numcompvalue", typeof(CoreAnnotations.NumericCompositeValueAnnotation));
		}

		/// <exception cref="System.IO.IOException"/>
		private static void GeneratePrefixDefs(string infile, string outfile)
		{
			IList<UnitPrefix> prefixes = UnitPrefix.LoadPrefixes(infile);
			PrintWriter pw = IOUtils.GetPrintWriter(outfile);
			pw.Println("SI_PREFIX_MAP = {");
			IList<string> items = new List<string>();
			foreach (UnitPrefix prefix in prefixes)
			{
				if ("SI".Equals(prefix.system))
				{
					items.Add("\"" + prefix.name + "\": " + prefix.GetName().ToUpper());
				}
			}
			pw.Println(StringUtils.Join(items, ",\n"));
			pw.Println("}");
			pw.Println("$SiPrefixes = CreateRegex(Keys(SI_PREFIX_MAP))");
			pw.Println();
			pw.Println("SI_SYM_PREFIX_MAP = {");
			items.Clear();
			foreach (UnitPrefix prefix_1 in prefixes)
			{
				if ("SI".Equals(prefix_1.system))
				{
					items.Add("\"" + prefix_1.symbol + "\": " + prefix_1.GetName().ToUpper());
				}
			}
			pw.Println(StringUtils.Join(items, ",\n"));
			pw.Println("}");
			pw.Println("$SiSymPrefixes = CreateRegex(Keys(SI_SYM_PREFIX_MAP))");
			pw.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		private static void GenerateUnitsStage0Rules(string unitsFiles, string infile, string outfile)
		{
			Pattern tabPattern = Pattern.Compile("\t");
			PrintWriter pw = IOUtils.GetPrintWriter(outfile);
			IList<Unit> units = Units.LoadUnits(unitsFiles);
			pw.Println("SI_UNIT_MAP = {");
			IList<string> items = new List<string>();
			foreach (Unit unit in units)
			{
				if ("SI".Equals(unit.prefixSystem))
				{
					items.Add("\"" + unit.name + "\": " + (unit.GetType() + "_" + unit.GetName()).ToUpper());
				}
			}
			pw.Println(StringUtils.Join(items, ",\n"));
			pw.Println("}");
			pw.Println("$SiUnits = CreateRegex(Keys(SI_UNIT_MAP))");
			pw.Println();
			pw.Println("SI_SYM_UNIT_MAP = {");
			items.Clear();
			foreach (Unit unit_1 in units)
			{
				if ("SI".Equals(unit_1.prefixSystem))
				{
					items.Add("\"" + unit_1.symbol + "\": " + (unit_1.GetType() + "_" + unit_1.GetName()).ToUpper());
				}
			}
			pw.Println(StringUtils.Join(items, ",\n"));
			pw.Println("}");
			pw.Println("$SiSymUnits = CreateRegex(Keys(SI_SYM_UNIT_MAP))");
			pw.Println();
			pw.Println("SYM_UNIT_MAP = {");
			items.Clear();
			foreach (Unit unit_2 in units)
			{
				items.Add("\"" + unit_2.symbol + "\": " + (unit_2.GetType() + "_" + unit_2.GetName()).ToUpper());
			}
			pw.Println(StringUtils.Join(items, ",\n"));
			pw.Println("}");
			pw.Println("$SymUnits = CreateRegex(Keys(SYM_UNIT_MAP))");
			pw.Println();
			BufferedReader br = IOUtils.GetBufferedFileReader(infile);
			string line;
			pw.Println("ENV.defaults[\"stage\"] = 0");
			while ((line = br.ReadLine()) != null)
			{
				string[] fields = tabPattern.Split(line);
				pw.Println(string.Format("{ pattern: ( %s ), action: Tag($0, \"Unit\", %s) }", fields[0], fields[1]));
			}
			br.Close();
			pw.Close();
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// Generate rules files
			Properties props = StringUtils.ArgsToProperties(args);
			Options options = new Options("qe", props);
			GeneratePrefixDefs(options.prefixFilename, options.prefixRulesFilename);
			GenerateUnitsStage0Rules(options.unitsFilename, options.text2UnitMapping, options.unitsRulesFilename);
		}
	}
}
