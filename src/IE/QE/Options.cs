using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>Quantifiable Entity Extractor Options.</summary>
	/// <author>Angel Chang</author>
	public class Options
	{
		private const string RulesDir = "edu/stanford/nlp/models/ie/qe/rules";

		private static readonly string[] DefaultGrammarFiles = new string[] { RulesDir + "/english.qe.txt" };

		private const string DefaultPrefixFile = RulesDir + "/prefixes.txt";

		private const string DefaultUnitsFile = RulesDir + "/units.txt";

		internal string prefixFilename = DefaultPrefixFile;

		internal string prefixRulesFilename = RulesDir + "/prefixes.rules.txt";

		internal string unitsFilename = DefaultUnitsFile;

		internal string unitsRulesFilename = RulesDir + "/english.units.rules.txt";

		internal string text2UnitMapping = RulesDir + "/english.units.txt";

		internal string grammarFilename = StringUtils.Join(new string[] { RulesDir + "/defs.qe.txt", prefixRulesFilename, unitsRulesFilename }, ",") + ',' + StringUtils.Join(DefaultGrammarFiles);

		public Options(string name, Properties props)
		{
			prefixFilename = props.GetProperty(name + ".prefixes", prefixFilename);
			grammarFilename = props.GetProperty(name + ".rules", grammarFilename);
		}
	}
}
