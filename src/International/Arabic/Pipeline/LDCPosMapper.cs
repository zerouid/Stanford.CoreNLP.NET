using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Maps pre-terminal ATB morphological analyses to the shortened Bies tag set.</summary>
	/// <author>Spence Green</author>
	public class LDCPosMapper : IMapper
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.LDCPosMapper));

		protected internal Pattern startOfTagMap = Pattern.Compile("\\(tag-map");

		protected internal Pattern endOfTagMap = Pattern.Compile("^\\s*\\)\\s*$");

		protected internal Pattern mapping = Pattern.Compile("\\((\\S+)\\s+(\\S+)\\)\\s*$");

		protected internal int numExpectedTokens = 2;

		private bool addDT = false;

		private readonly Pattern determiner = Pattern.Compile("DET");

		private readonly Pattern nounBaseTag = Pattern.Compile("NN");

		private readonly Pattern adjBaseTag = Pattern.Compile("JJ");

		private readonly Pattern LDCdeterminer = Pattern.Compile("DT\\+");

		protected internal readonly IDictionary<string, string> tagMap;

		protected internal readonly ICollection<string> tagsToEscape;

		public LDCPosMapper()
			: this(false)
		{
		}

		public LDCPosMapper(bool addDeterminer)
		{
			addDT = addDeterminer;
			tagMap = Generics.NewHashMap();
			//Pre-terminal tags that do not appear in LDC tag maps
			tagsToEscape = Generics.NewHashSet();
			tagsToEscape.Add("-NONE-");
			//Traces
			tagsToEscape.Add("PUNC");
		}

		//Punctuation
		/// <param name="posTag">The preterminal tag</param>
		/// <param name="terminal">The optional terminal, which may be used for context</param>
		public virtual string Map(string posTag, string terminal)
		{
			string rawTag = posTag.Trim();
			if (tagMap.Contains(rawTag))
			{
				return tagMap[rawTag];
			}
			else
			{
				if (tagsToEscape.Contains(rawTag))
				{
					return rawTag;
				}
			}
			System.Console.Error.Printf("%s: No mapping for %s%n", this.GetType().FullName, rawTag);
			return rawTag;
		}

		//Modifies the shortened tag based on information contained in the longer tag
		private string ProcessShortTag(string longTag, string shortTag)
		{
			if (shortTag == null)
			{
				return null;
			}
			//Hacks to make p5+ mappings compatible with p1-3
			if (shortTag.StartsWith("DT+"))
			{
				shortTag = LDCdeterminer.Matcher(shortTag).ReplaceAll(string.Empty);
			}
			if (longTag.Equals("NUMERIC_COMMA"))
			{
				shortTag = "PUNC";
			}
			//As recommended by (Kulick et al., 2006)
			if (addDT && (longTag != null))
			{
				Matcher detInLongTag = determiner.Matcher(longTag);
				Matcher someKindOfNoun = nounBaseTag.Matcher(shortTag);
				Matcher someKindOfAdj = adjBaseTag.Matcher(shortTag);
				if (detInLongTag.Find() && (someKindOfNoun.Find() || someKindOfAdj.Find()))
				{
					shortTag = "DT" + shortTag.Trim();
				}
			}
			if (tagMap.Contains(longTag))
			{
				string existingShortTag = tagMap[longTag];
				if (!existingShortTag.Equals(shortTag))
				{
					System.Console.Error.Printf("%s: Union of mapping files will cause overlap for %s (current: %s new: %s)%n", this.GetType().FullName, longTag, existingShortTag, shortTag);
				}
				return existingShortTag;
			}
			return shortTag;
		}

		public virtual void Setup(File path, params string[] options)
		{
			if (path == null || !path.Exists())
			{
				return;
			}
			LineNumberReader reader = null;
			try
			{
				reader = new LineNumberReader(new FileReader(path));
				bool insideTagMap = false;
				for (string line; (line = reader.ReadLine()) != null; )
				{
					line = line.Trim();
					Matcher isStartSymbol = startOfTagMap.Matcher(line);
					insideTagMap = (isStartSymbol.Matches() || insideTagMap);
					if (insideTagMap)
					{
						//Comment line
						if (line.StartsWith(";"))
						{
							continue;
						}
						Matcher mappingLine = mapping.Matcher(line);
						if (mappingLine.Find())
						{
							if (mappingLine.GroupCount() == numExpectedTokens)
							{
								string finalShortTag = ProcessShortTag(mappingLine.Group(1), mappingLine.Group(2));
								tagMap[mappingLine.Group(1)] = finalShortTag;
							}
							else
							{
								System.Console.Error.Printf("%s: Skipping bad mapping in %s (line %d)%n", this.GetType().FullName, path.GetPath(), reader.GetLineNumber());
							}
						}
						Matcher isEndSymbol = endOfTagMap.Matcher(line);
						if (isEndSymbol.Matches())
						{
							break;
						}
					}
				}
				reader.Close();
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open mapping file %s%n", this.GetType().FullName, path.GetPath());
			}
			catch (IOException)
			{
				int lineNum = (reader == null) ? -1 : reader.GetLineNumber();
				System.Console.Error.Printf("%s: Error reading %s (line %d)%n", this.GetType().FullName, path.GetPath(), lineNum);
			}
		}

		public virtual bool CanChangeEncoding(string parent, string element)
		{
			//POS tags aren't encoded, so no need to check
			return true;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (string longTag in tagMap.Keys)
			{
				sb.Append(longTag).Append('\t').Append(tagMap[longTag]).Append('\n');
			}
			return sb.ToString();
		}

		public static void Main(string[] args)
		{
			IMapper mapper = new Edu.Stanford.Nlp.International.Arabic.Pipeline.LDCPosMapper(true);
			File mapFile = new File("/u/nlp/data/Arabic/ldc/atb-latest/p1/docs/atb1-v4.0-taglist-conversion-to-PennPOS-forrelease.lisp");
			mapper.Setup(mapFile);
			string test1 = "DET+NOUN+NSUFF_FEM_SG+CASE_DEF_ACC";
			string test2 = "ADJXXXXX";
			string test3 = "REL_ADV";
			string test4 = "NUMERIC_COMMA";
			System.Console.Out.Printf("%s --> %s\n", test1, mapper.Map(test1, null));
			System.Console.Out.Printf("%s --> %s\n", test2, mapper.Map(test2, null));
			System.Console.Out.Printf("%s --> %s\n", test3, mapper.Map(test3, null));
			System.Console.Out.Printf("%s --> %s\n", test4, mapper.Map(test4, null));
		}
	}
}
