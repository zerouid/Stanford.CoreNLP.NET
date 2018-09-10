using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Ling.Tokensregex.Matcher;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// TokensRegexNERAnnotator labels tokens with types based on a simple manual mapping from
	/// regular expressions to the types of the entities they are meant to describe.
	/// </summary>
	/// <remarks>
	/// TokensRegexNERAnnotator labels tokens with types based on a simple manual mapping from
	/// regular expressions to the types of the entities they are meant to describe.
	/// The user provides a file formatted as follows:
	/// <pre>
	/// regex1    TYPE    overwritableType1,Type2...    priority
	/// regex2    TYPE    overwritableType1,Type2...    priority
	/// ...
	/// </pre>
	/// where each argument is tab-separated, and the last two arguments are optional. Several regexes can be
	/// associated with a single type. In the case where multiple regexes match a phrase, the priority ranking
	/// (higher priority is favored) is used to choose between the possible types.
	/// When the priority is the same, then longer matches are favored.
	/// <p>
	/// This annotator is designed to be used as part of a full
	/// NER system to label entities that don't fall into the usual NER categories. It only records the label
	/// if the token has not already been NER-annotated, or it has been annotated but the NER-type has been
	/// designated overwritable (the third argument).
	/// It is also possible to use this annotator to annotate fields other than the
	/// <c>NamedEntityTagAnnotation</c>
	/// field by
	/// and providing the header
	/// <p>
	/// The first column regex may follow one of two formats:
	/// <ol>
	/// <li> A TokensRegex expression (marked by starting with "( " and ending with " )".
	/// See
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern"/>
	/// for TokensRegex syntax.
	/// <br/><em>Example</em>:
	/// <c/>
	/// ( /University/ /of/ [ {ner:LOCATION} ] )    SCHOOL}
	/// </li>
	/// <li> a sequence of regex, each separated by whitespace (matching "\s+").
	/// <br/><em>Example</em>:
	/// <c>Stanford    SCHOOL</c>
	/// <br/>
	/// The regex will match if the successive regex match a sequence of tokens in the input.
	/// Spaces can only be used to separate regular expression tokens; within tokens \s or similar non-space
	/// representations need to be used instead.
	/// <br/>
	/// Notes: Following Java regex conventions, some characters in the file need to be escaped. Only a single
	/// backslash should be used though, as these are not String literals. The input to RegexNER will have
	/// already been tokenized.  So, for example, with our usual English tokenization, things like genitives
	/// and commas at the end of words will be separated in the input and matched as a separate token.</li>
	/// </ol>
	/// <p>
	/// This annotator is similar to
	/// <see cref="RegexNERAnnotator"/>
	/// but uses TokensRegex as the underlying library for matching
	/// regular expressions.  This allows for more flexibility in the types of expressions matched as well as utilizing
	/// any optimization that is included in the TokensRegex library.
	/// <p>
	/// Main differences from
	/// <see cref="RegexNERAnnotator"/>
	/// :
	/// <ul>
	/// <li>Supports annotation of fields other than the
	/// <c>NamedEntityTagAnnotation</c>
	/// field</li>
	/// <li>Supports both TokensRegex patterns and patterns over the text of the tokens</li>
	/// <li>When NER annotation can be overwritten based on the original NER labels.  The rules for when the new NER labels are used
	/// are given below:
	/// <br/>If the found expression overlaps with a previous NER phrase, then the NER labels are not replaced.
	/// <br/>  <em>Example</em>: Old NER phrase:
	/// <c>The ABC Company</c>
	/// , Found Phrase:
	/// <c>ABC =&gt;</c>
	/// Old NER labels are not replaced.
	/// <br/>If the found expression has inconsistent NER tags among the tokens, then the NER labels are replaced.
	/// <br/>  <em>Example</em>: Old NER phrase:
	/// <c>The/O ABC/MISC Company/ORG =&gt; The/ORG ABC/ORG Company/ORG</c>
	/// </li>
	/// <li>How
	/// <c>validpospattern</c>
	/// is handled for POS tags is specified by
	/// <c>PosMatchType</c>
	/// </li>
	/// <li>By default, there is no
	/// <c>validPosPattern</c>
	/// </li>
	/// <li>By default, both O and MISC is always replaced</li>
	/// </ul>
	/// <p>
	/// Configuration:
	/// <table>
	/// <tr><th>Field</th><th>Description</th><th>Default</th></tr>
	/// <tr><td>
	/// <c>mapping</c>
	/// </td><td>Comma separated list of mapping files to use </td>
	/// <td>
	/// <c>edu/stanford/nlp/models/kbp/regexner_caseless.tab</c>
	/// </td>
	/// </tr>
	/// <tr><td>
	/// <c>mapping.header</c>
	/// </td>
	/// <td>Comma separated list of header fields (or
	/// <see langword="true"/>
	/// if header is specified in the file)</td>
	/// <td>pattern,ner,overwrite,priority,group</td></tr>
	/// <tr><td>
	/// <c>mapping.field.&lt;fieldname&gt;</c>
	/// </td>
	/// <td>Class mapping for annotation fields other than ner</td></tr>
	/// <tr><td>
	/// <c>commonWords</c>
	/// </td>
	/// <td>Comma separated list of files for common words to not annotate (in case your mapping isn't very clean)</td></tr>
	/// <tr><td>
	/// <c>backgroundSymbol</c>
	/// </td><td>Comma separated list of NER labels to always replace</td>
	/// <td>
	/// <c>O,MISC</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>posmatchtype</c>
	/// </td>
	/// <td>How should
	/// <c>validpospattern</c>
	/// be used to match the POS of the tokens.
	/// <c>MATCH_ALL_TOKENS</c>
	/// - All tokens has to match.<br/>
	/// <c>MATCH_AT_LEAST_ONE_TOKEN</c>
	/// - At least one token has to match.<br/>
	/// <c>MATCH_ONE_TOKEN_PHRASE_ONLY</c>
	/// - Only has to match for one token phrases.<br/>
	/// </td>
	/// <td>
	/// <c>MATCH_AT_LEAST_ONE_TOKEN</c>
	/// </td>
	/// </tr>
	/// <tr><td>
	/// <c>validpospattern</c>
	/// </td><td>Regular expression pattern for matching POS tags.</td>
	/// <td>
	/// <c/>
	/// </td>
	/// </tr>
	/// <tr><td>
	/// <c>noDefaultOverwriteLabels</c>
	/// </td>
	/// <td>Comma separated list of output types for which default NER labels are not overwritten.
	/// For these types, only if the matched expression has NER type matching the
	/// specified overwriteableType for the regex will the NER type be overwritten.</td>
	/// <td>
	/// <c/>
	/// </td></tr>
	/// <tr><td>
	/// <c>ignoreCase</c>
	/// </td><td>If true, case is ignored</td></td>
	/// <td>
	/// <see langword="false"/>
	/// </td></tr>
	/// <tr><td>
	/// <c>verbose</c>
	/// </td><td>If true, turns on extra debugging messages.</td>
	/// <td>
	/// <see langword="false"/>
	/// </td></tr>
	/// </table>
	/// <p>
	/// You can specify a different header for each mapping file.
	/// Here is an example of mapping files with header declaration:
	/// <pre>
	/// properties.setProperty("ner.fine.regexner.mapping", "ignorecase=true, header=pattern overwrite priority, file1.tab;" + "ignorecase=true, file2.tab");
	/// </pre>
	/// The header items are whitespace separated and can also be specified for one of the files.
	/// Files MUST be separated with a semi-colon.
	/// In the same way, it is possible to fetch the header from the first line of a specific file.
	/// <pre>
	/// properties.setProperty("ner.fine.regexner.mapping", "ignorecase=true, header=true, file1.tab;" + "ignorecase=true, header=pattern overwrite priority group, file2.tab");
	/// </pre>
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <author>Alberto Soragna (@alsora) (added per-file headers</author>
	public class TokensRegexNERAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		protected internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.TokensRegexNERAnnotator));

		protected internal const string PatternField = "pattern";

		protected internal const string OverwriteField = "overwrite";

		protected internal const string PriorityField = "priority";

		protected internal const string WeightField = "weight";

		protected internal const string GroupField = "group";

		protected internal static readonly ICollection<string> predefinedHeaderFields = CollectionUtils.AsSet(PatternField, OverwriteField, PriorityField, WeightField, GroupField);

		protected internal const string defaultHeader = "pattern,ner,overwrite,priority,group";

		private readonly bool ignoreCase;

		private readonly IList<bool> ignoreCaseList;

		private readonly ICollection<string> commonWords;

		private readonly IList<TokensRegexNERAnnotator.Entry> entries;

		private readonly IDictionary<SequencePattern<ICoreMap>, TokensRegexNERAnnotator.Entry> patternToEntry;

		private readonly MultiPatternMatcher<ICoreMap> multiPatternMatcher;

		private readonly IList<Type> annotationFields;

		private readonly ICollection<string> myLabels;

		private readonly Pattern validPosPattern;

		private readonly IList<Pattern> validPosPatternList;

		private readonly IList<string[]> headerList;

		private readonly bool verbose;

		private readonly IDictionary<TokensRegexNERAnnotator.Entry, int> entryToMappingFileNumber;

		private readonly ICollection<string> noDefaultOverwriteLabels;

		internal enum PosMatchType
		{
			MatchAllTokens,
			MatchAtLeastOneToken,
			MatchOneTokenPhraseOnly
		}

		private readonly TokensRegexNERAnnotator.PosMatchType posMatchType;

		public static readonly TokensRegexNERAnnotator.PosMatchType DefaultPosMatchType = TokensRegexNERAnnotator.PosMatchType.MatchAtLeastOneToken;

		public const string DefaultBackgroundSymbol = SeqClassifierFlags.DefaultBackgroundSymbol + ",MISC";

		public static PropertiesUtils.Property[] SupportedProperties = new PropertiesUtils.Property[] { new PropertiesUtils.Property("mapping", DefaultPaths.DefaultRegexnerRules, "List of mapping files to use, separated by commas or semi-colons."), 
			new PropertiesUtils.Property("mapping.header", defaultHeader, "Comma separated list specifying order of fields in the mapping file"), new PropertiesUtils.Property("mapping.field.<fieldname>", string.Empty, "Class mapping for annotation fields other than ner"
			), new PropertiesUtils.Property("commonWords", string.Empty, "Comma separated list of files for common words to not annotate (in case your mapping isn't very clean)"), new PropertiesUtils.Property("ignorecase", "false", "Whether to ignore case or not when matching patterns."
			), new PropertiesUtils.Property("validpospattern", string.Empty, "Regular expression pattern for matching POS tags."), new PropertiesUtils.Property("posmatchtype", DefaultPosMatchType.ToString(), "How should 'validpospattern' be used to match the POS of the tokens."
			), new PropertiesUtils.Property("noDefaultOverwriteLabels", string.Empty, "Comma separated list of output types for which default NER labels are not overwritten.\n" + " For these types, only if the matched expression has NER type matching the\n"
			 + " specified overwriteableType for the regex will the NER type be overwritten."), new PropertiesUtils.Property("backgroundSymbol", DefaultBackgroundSymbol, "Comma separated list of NER labels to always replace."), new PropertiesUtils.Property
			("verbose", "false", string.Empty) };

		/// <summary>Construct a new TokensRegexAnnotator.</summary>
		/// <param name="mapping">A comma-separated list of files, URLs, or classpath resources to load mappings from</param>
		public TokensRegexNERAnnotator(string mapping)
			: this(mapping, false)
		{
		}

		public TokensRegexNERAnnotator(string mapping, bool ignoreCase)
			: this(mapping, ignoreCase, null)
		{
		}

		public TokensRegexNERAnnotator(string mapping, bool ignoreCase, string validPosRegex)
			: this("tokenregexner", GetProperties("tokenregexner", mapping, ignoreCase, validPosRegex))
		{
		}

		// list of fields to annotate (default to just NamedEntityTag)
		// set of labels to always overwrite
		// Labels for which we don't use the default overwrite types (mylabels)
		// all tokens must match the pos pattern
		// only one token must match the pos pattern
		// only single token phrases have to match the pos pattern
		private static Properties GetProperties(string name, string mapping, bool ignoreCase, string validPosRegex)
		{
			string prefix = !StringUtils.IsNullOrEmpty(name) ? name + '.' : string.Empty;
			Properties props = new Properties();
			props.SetProperty(prefix + "mapping", mapping);
			props.SetProperty(prefix + "ignorecase", ignoreCase.ToString());
			if (validPosRegex != null)
			{
				props.SetProperty(prefix + "validpospattern", validPosRegex);
			}
			return props;
		}

		private static readonly Pattern CommaDelimitersPattern = Pattern.Compile("\\s*,\\s*");

		private static readonly Pattern SemicolonDelimitersPattern = Pattern.Compile("\\s*;\\s*");

		private static readonly Pattern EqualsDelimitersPattern = Pattern.Compile("\\s*=\\s*");

		private static readonly Pattern NumberPattern = Pattern.Compile("-?[0-9]+(?:\\.[0-9]+)?");

		public TokensRegexNERAnnotator(string name, Properties properties)
		{
			string prefix = !StringUtils.IsNullOrEmpty(name) ? name + '.' : string.Empty;
			string backgroundSymbol = properties.GetProperty(prefix + "backgroundSymbol", DefaultBackgroundSymbol);
			string[] backgroundSymbols = CommaDelimitersPattern.Split(backgroundSymbol);
			string mappingFiles = properties.GetProperty(prefix + "mapping", DefaultPaths.DefaultKbpTokensregexNerSettings);
			string[] mappings = ProcessListMappingFiles(mappingFiles);
			string validPosRegex = properties.GetProperty(prefix + "validpospattern");
			this.posMatchType = TokensRegexNERAnnotator.PosMatchType.ValueOf(properties.GetProperty(prefix + "posmatchtype", DefaultPosMatchType.ToString()));
			string commonWordsFile = properties.GetProperty(prefix + "commonWords");
			commonWords = new HashSet<string>();
			if (commonWordsFile != null)
			{
				try
				{
					using (BufferedReader reader = IOUtils.ReaderFromString(commonWordsFile))
					{
						for (string line; (line = reader.ReadLine()) != null; )
						{
							commonWords.Add(line);
						}
					}
				}
				catch (IOException ex)
				{
					throw new RuntimeIOException("TokensRegexNERAnnotator " + name + ": Error opening the common words file: " + commonWordsFile, ex);
				}
			}
			string headerProp = properties.GetProperty(prefix + "mapping.header", defaultHeader);
			bool readHeaderFromFile = Sharpen.Runtime.EqualsIgnoreCase(headerProp, "true");
			string[] annotationFieldnames = null;
			string[] headerFields = null;
			if (readHeaderFromFile)
			{
				annotationFieldnames = StringUtils.EmptyStringArray;
				annotationFields = new List<Type>();
				// Set the read header property of each file to true
				for (int i = 0; i < mappings.Length; i++)
				{
					string mappingLine = mappings[i];
					if (!mappingLine.Contains("header"))
					{
						mappingLine = "header=true, " + mappingLine;
						mappings[i] = mappingLine;
					}
					else
					{
						if (!Pattern.Compile("header\\s*=\\s*true").Matcher(mappingLine.ToLower()).Find())
						{
							throw new InvalidOperationException("The annotator header property is set to true, but a different option has been provided for mapping file: " + mappingLine);
						}
					}
				}
			}
			else
			{
				headerFields = CommaDelimitersPattern.Split(headerProp);
				// Take header fields and remove known headers to get annotation field names
				IList<string> fieldNames = new List<string>();
				IList<Type> fieldClasses = new List<Type>();
				foreach (string field in headerFields)
				{
					if (!predefinedHeaderFields.Contains(field))
					{
						Type fieldClass = EnvLookup.LookupAnnotationKeyWithClassname(null, field);
						if (fieldClass == null)
						{
							// check our properties
							string classname = properties.GetProperty(prefix + "mapping.field." + field);
							fieldClass = EnvLookup.LookupAnnotationKeyWithClassname(null, classname);
						}
						if (fieldClass != null)
						{
							fieldNames.Add(field);
							fieldClasses.Add(fieldClass);
						}
						else
						{
							logger.Warn(name + ": Unknown field: " + field + " cannot find suitable annotation class");
						}
					}
				}
				annotationFieldnames = new string[fieldNames.Count];
				Sharpen.Collections.ToArray(fieldNames, annotationFieldnames);
				annotationFields = fieldClasses;
			}
			string noDefaultOverwriteLabelsProp = properties.GetProperty(prefix + "noDefaultOverwriteLabels", "CITY");
			this.noDefaultOverwriteLabels = Java.Util.Collections.UnmodifiableSet(CollectionUtils.AsSet(CommaDelimitersPattern.Split(noDefaultOverwriteLabelsProp)));
			this.ignoreCase = PropertiesUtils.GetBool(properties, prefix + "ignorecase", false);
			this.verbose = PropertiesUtils.GetBool(properties, prefix + "verbose", false);
			if (!StringUtils.IsNullOrEmpty(validPosRegex))
			{
				validPosPattern = Pattern.Compile(validPosRegex);
			}
			else
			{
				validPosPattern = null;
			}
			validPosPatternList = new List<Pattern>();
			ignoreCaseList = new List<bool>();
			headerList = new List<string[]>();
			entryToMappingFileNumber = new Dictionary<TokensRegexNERAnnotator.Entry, int>();
			annotationFieldnames = ProcessPerFileOptions(name, mappings, ignoreCaseList, validPosPatternList, headerList, ignoreCase, validPosPattern, headerFields, annotationFieldnames, annotationFields);
			entries = Java.Util.Collections.UnmodifiableList(ReadEntries(name, noDefaultOverwriteLabels, ignoreCaseList, headerList, entryToMappingFileNumber, verbose, annotationFieldnames, mappings));
			IdentityHashMap<SequencePattern<ICoreMap>, TokensRegexNERAnnotator.Entry> patternToEntry = new IdentityHashMap<SequencePattern<ICoreMap>, TokensRegexNERAnnotator.Entry>();
			multiPatternMatcher = CreatePatternMatcher(patternToEntry);
			this.patternToEntry = Java.Util.Collections.UnmodifiableMap(patternToEntry);
			ICollection<string> myLabels = Generics.NewHashSet();
			// Can always override background or none.
			Java.Util.Collections.AddAll(myLabels, backgroundSymbols);
			myLabels.Add(null);
			// Always overwrite labels
			foreach (TokensRegexNERAnnotator.Entry entry in entries)
			{
				Java.Util.Collections.AddAll(myLabels, entry.types);
			}
			this.myLabels = Java.Util.Collections.UnmodifiableSet(myLabels);
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (verbose)
			{
				logger.Info("Adding TokensRegexNER annotations ... ");
			}
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null)
			{
				foreach (ICoreMap sentence in sentences)
				{
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					AnnotateMatched(tokens);
				}
			}
			else
			{
				IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				if (tokens != null)
				{
					AnnotateMatched(tokens);
				}
				else
				{
					throw new Exception("Unable to find sentences or tokens in " + annotation);
				}
			}
			if (verbose)
			{
				logger.Info("done.");
			}
		}

		private MultiPatternMatcher<ICoreMap> CreatePatternMatcher(IDictionary<SequencePattern<ICoreMap>, TokensRegexNERAnnotator.Entry> patternToEntry)
		{
			// Convert to tokensregex pattern
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>(entries.Count);
			foreach (TokensRegexNERAnnotator.Entry entry in entries)
			{
				TokenSequencePattern pattern;
				bool ignoreCaseEntry = ignoreCaseList[entryToMappingFileNumber[entry]];
				int patternFlags = ignoreCaseEntry ? Pattern.CaseInsensitive | Pattern.UnicodeCase : 0;
				int stringMatchFlags = ignoreCaseEntry ? (NodePattern.CaseInsensitive | NodePattern.UnicodeCase) : 0;
				Env env = TokenSequencePattern.GetNewEnv();
				env.SetDefaultStringPatternFlags(patternFlags);
				env.SetDefaultStringMatchFlags(stringMatchFlags);
				NodePattern<string> posTagPattern = (validPosPatternList[entryToMappingFileNumber[entry]] != null && TokensRegexNERAnnotator.PosMatchType.MatchAllTokens.Equals(posMatchType)) ? new ComplexNodePattern.StringAnnotationRegexPattern(validPosPatternList
					[entryToMappingFileNumber[entry]]) : null;
				if (entry.tokensRegex != null)
				{
					// TODO: posTagPatterns...
					pattern = ((TokenSequencePattern)TokenSequencePattern.Compile(env, entry.tokensRegex));
				}
				else
				{
					IList<SequencePattern.PatternExpr> nodePatterns = new List<SequencePattern.PatternExpr>();
					foreach (string p in entry.regex)
					{
						CoreMapNodePattern c = CoreMapNodePattern.ValueOf(p, patternFlags);
						if (posTagPattern != null)
						{
							c.Add(typeof(CoreAnnotations.PartOfSpeechAnnotation), posTagPattern);
						}
						nodePatterns.Add(new SequencePattern.NodePatternExpr(c));
					}
					pattern = ((TokenSequencePattern)TokenSequencePattern.Compile(new SequencePattern.SequencePatternExpr(nodePatterns)));
				}
				if (entry.annotateGroup < 0 || entry.annotateGroup > pattern.GetTotalGroups())
				{
					throw new Exception("Invalid match group for entry " + entry);
				}
				pattern.SetPriority(entry.priority);
				pattern.SetWeight(entry.weight);
				patterns.Add(pattern);
				patternToEntry[pattern] = entry;
			}
			return TokenSequencePattern.GetMultiPatternMatcher(patterns);
		}

		private void AnnotateMatched(IList<CoreLabel> tokens)
		{
			IList<ISequenceMatchResult<ICoreMap>> matched = multiPatternMatcher.FindNonOverlapping(tokens);
			foreach (ISequenceMatchResult<ICoreMap> m in matched)
			{
				TokensRegexNERAnnotator.Entry entry = patternToEntry[m.Pattern()];
				// Check if we will overwrite the existing annotation with this annotation
				int g = entry.annotateGroup;
				int start = m.Start(g);
				int end = m.End(g);
				string str = m.Group(g);
				if (commonWords.Contains(str))
				{
					if (verbose)
					{
						logger.Info("Not annotating (common word) '" + str + "': " + StringUtils.JoinFields(m.GroupNodes(g), typeof(CoreAnnotations.NamedEntityTagAnnotation)) + " with " + entry.GetTypeDescription() + ", sentence is '" + StringUtils.JoinWords(tokens
							, " ") + "'");
					}
					continue;
				}
				bool overwriteOriginalNer = CheckPosTags(tokens, start, end);
				if (overwriteOriginalNer)
				{
					overwriteOriginalNer = CheckOrigNerTags(entry, tokens, start, end);
				}
				if (overwriteOriginalNer)
				{
					for (int i = start; i < end; i++)
					{
						CoreLabel token = tokens[i];
						for (int j = 0; j < annotationFields.Count; j++)
						{
							token.Set(annotationFields[j], entry.types[j]);
						}
					}
				}
				else
				{
					// tokens.get(i).set(CoreAnnotations.NamedEntityTagAnnotation.class, entry.type);
					if (verbose)
					{
						logger.Info("Not annotating  '" + m.Group(g) + "': " + StringUtils.JoinFields(m.GroupNodes(g), typeof(CoreAnnotations.NamedEntityTagAnnotation)) + " with " + entry.GetTypeDescription() + ", sentence is '" + StringUtils.JoinWords(tokens, " ")
							 + "'");
					}
				}
			}
		}

		// TODO: roll check into tokens regex pattern?
		// That allows for better matching because unmatched sequences will be eliminated at match time
		private bool CheckPosTags(IList<CoreLabel> tokens, int start, int end)
		{
			if (validPosPattern != null || AtLeastOneValidPosPattern(validPosPatternList))
			{
				switch (posMatchType)
				{
					case TokensRegexNERAnnotator.PosMatchType.MatchOneTokenPhraseOnly:
					{
						// Need to check POS tag too...
						if (tokens.Count > 1)
						{
							return true;
						}
						goto case TokensRegexNERAnnotator.PosMatchType.MatchAtLeastOneToken;
					}

					case TokensRegexNERAnnotator.PosMatchType.MatchAtLeastOneToken:
					{
						// fall through
						for (int i = start; i < end; i++)
						{
							CoreLabel token = tokens[i];
							string pos = token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
							if (pos != null && validPosPattern != null && validPosPattern.Matcher(pos).Matches())
							{
								return true;
							}
							else
							{
								if (pos != null)
								{
									foreach (Pattern pattern in validPosPatternList)
									{
										if (pattern != null && pattern.Matcher(pos).Matches())
										{
											return true;
										}
									}
								}
							}
						}
						return false;
					}

					case TokensRegexNERAnnotator.PosMatchType.MatchAllTokens:
					{
						// Checked else where
						return true;
					}

					default:
					{
						// Don't know this match type....
						return true;
					}
				}
			}
			return true;
		}

		private static bool IsLocationOrGpe(CoreLabel token)
		{
			return "LOCATION".Equals(token.Ner()) || "GPE".Equals(token.Ner());
		}

		private bool CheckOrigNerTags(TokensRegexNERAnnotator.Entry entry, IList<CoreLabel> tokens, int start, int end)
		{
			// cdm Aug 2016: Add in a special hack - always allow a sequence of GPE or LOCATION to overwrite
			// this is the current expected behavior, and the itest expects this.
			bool specialCasePass = true;
			for (int i = start; i < end; i++)
			{
				if (!IsLocationOrGpe(tokens[i]))
				{
					specialCasePass = false;
					break;
				}
			}
			if (specialCasePass)
			{
				return true;
			}
			// end special Chinese KBP 2016 code
			int prevNerEndIndex = start - 1;
			int nextNerStartIndex = end;
			// Check if we found a pattern that overlaps with existing ner labels
			// tag1 tag1 x   x  tag2 tag2
			//      tag tag tag tag
			// Don't overwrite the old ner label if we overlap like this
			string startNer = tokens[start].Ner();
			string endNer = tokens[end - 1].Ner();
			if (startNer != null && !myLabels.Contains(startNer))
			{
				while (prevNerEndIndex >= 0)
				{
					// go backwards to find different entity type
					string ner = tokens[prevNerEndIndex].Ner();
					if (ner == null || !ner.Equals(startNer))
					{
						break;
					}
					prevNerEndIndex--;
				}
			}
			if (endNer != null && !myLabels.Contains(endNer))
			{
				while (nextNerStartIndex < tokens.Count)
				{
					// go backwards to find different entity type
					string ner = tokens[nextNerStartIndex].Ner();
					if (ner == null || !ner.Equals(endNer))
					{
						break;
					}
					nextNerStartIndex++;
				}
			}
			bool overwriteOriginalNer = false;
			//noinspection StatementWithEmptyBody
			if (prevNerEndIndex != (start - 1) || nextNerStartIndex != end)
			{
			}
			else
			{
				// Cutting across already recognized NEs don't disturb
				if (startNer == null)
				{
					// No old ner, okay to replace
					overwriteOriginalNer = true;
				}
				else
				{
					// Check if we have one consistent NER tag
					// if not, overwrite
					// if consistent, overwrite only if in our set of ner tags that we overwrite
					for (int i_1 = start + 1; i_1 < end; i_1++)
					{
						if (!startNer.Equals(tokens[i_1].Ner()))
						{
							overwriteOriginalNer = true;
							break;
						}
					}
					if (!overwriteOriginalNer)
					{
						// check if old ner type was one that was specified as explicitly overwritable by this entry
						if (entry.overwritableTypes.Contains(startNer))
						{
							overwriteOriginalNer = true;
						}
						else
						{
							// if this ner type doesn't belong to the labels for which we don't overwrite the default labels (noDefaultOverwriteLabels)
							// we check mylabels to see if we can overwrite this entry
							if (!HasNoOverwritableType(noDefaultOverwriteLabels, entry.types))
							{
								/*entry.overwritableTypes.isEmpty() || */
								overwriteOriginalNer = myLabels.Contains(startNer);
							}
						}
					}
				}
			}
			return overwriteOriginalNer;
		}

		private class Entry
		{
			public readonly string tokensRegex;

			public readonly string[] regex;

			public readonly string[] types;

			public readonly ICollection<string> overwritableTypes;

			public readonly double priority;

			public readonly double weight;

			public readonly int annotateGroup;

			public Entry(string tokensRegex, string[] regex, string[] types, ICollection<string> overwritableTypes, double priority, double weight, int annotateGroup)
			{
				// the regex, tokenized by splitting on white space
				// the associated types
				// what types can be overwritten by this entry
				this.tokensRegex = tokensRegex;
				this.regex = regex;
				this.types = new string[types.Length];
				for (int i = 0; i < types.Length; i++)
				{
					// TODO: for some types, it doesn't make sense to be interning...
					this.types[i] = string.Intern(types[i]);
				}
				this.overwritableTypes = overwritableTypes;
				this.priority = priority;
				this.weight = weight;
				this.annotateGroup = annotateGroup;
			}

			public virtual string GetTypeDescription()
			{
				return Arrays.ToString(types);
			}

			public override string ToString()
			{
				return "Entry{" + ((tokensRegex != null) ? tokensRegex : StringUtils.Join(regex)) + ' ' + StringUtils.Join(types) + ' ' + overwritableTypes + " prio:" + priority + '}';
			}
		}

		// end static class Entry
		/// <summary>Creates a combined list of Entries using the provided mapping files.</summary>
		/// <param name="mappings">List of mapping files</param>
		/// <returns>list of Entries</returns>
		private static IList<TokensRegexNERAnnotator.Entry> ReadEntries(string annotatorName, ICollection<string> noDefaultOverwriteLabels, IList<bool> ignoreCaseList, IList<string[]> headerList, IDictionary<TokensRegexNERAnnotator.Entry, int> entryToMappingFileNumber
			, bool verbose, string[] annotationFieldnames, params string[] mappings)
		{
			// Unlike RegexNERClassifier, we don't bother sorting the entries
			// We leave it to TokensRegex NER to sort out the priorities and matches
			// (typically after all the matches has been made since for some TokenRegex expression,
			// we don't know how many tokens are matched until after the matching is done)
			IList<TokensRegexNERAnnotator.Entry> entries = new List<TokensRegexNERAnnotator.Entry>();
			TrieMap<string, TokensRegexNERAnnotator.Entry> seenRegexes = new TrieMap<string, TokensRegexNERAnnotator.Entry>();
			// Arrays.sort(mappings);
			for (int mappingFileIndex = 0; mappingFileIndex < mappings.Length; mappingFileIndex++)
			{
				string mapping = mappings[mappingFileIndex];
				try
				{
					using (BufferedReader rd = IOUtils.ReaderFromString(mapping))
					{
						ReadEntries(annotatorName, headerList[mappingFileIndex], annotationFieldnames, entries, seenRegexes, mapping, rd, noDefaultOverwriteLabels, ignoreCaseList[mappingFileIndex], mappingFileIndex, entryToMappingFileNumber, verbose);
					}
				}
				catch (IOException e)
				{
					throw new RuntimeIOException("Couldn't read TokensRegexNER from " + mapping, e);
				}
			}
			if (mappings.Length != 1)
			{
				logger.Log(annotatorName + ": Read " + entries.Count + " unique entries from " + mappings.Length + " files");
			}
			return entries;
		}

		private static IDictionary<string, int> GetHeaderIndexMap(string[] headerFields)
		{
			IDictionary<string, int> map = new Dictionary<string, int>();
			for (int i = 0; i < headerFields.Length; i++)
			{
				string field = headerFields[i];
				if (map.Contains(field))
				{
					throw new ArgumentException("Duplicate header field: " + field);
				}
				map[field] = i;
			}
			return map;
		}

		private static int GetIndex(IDictionary<string, int> map, string name)
		{
			int index = map[name];
			if (index == null)
			{
				return -1;
			}
			else
			{
				return index;
			}
		}

		/// <summary>Reads a list of Entries from a mapping file and update the given entries.</summary>
		/// <remarks>
		/// Reads a list of Entries from a mapping file and update the given entries.
		/// Line numbers start from 1.
		/// </remarks>
		/// <returns>the updated list of Entries</returns>
		/// <exception cref="System.IO.IOException"/>
		private static IList<TokensRegexNERAnnotator.Entry> ReadEntries(string annotatorName, string[] headerFields, string[] annotationFieldnames, IList<TokensRegexNERAnnotator.Entry> entries, TrieMap<string, TokensRegexNERAnnotator.Entry> seenRegexes
			, string mappingFilename, BufferedReader mapping, ICollection<string> noDefaultOverwriteLabels, bool ignoreCase, int mappingFileIndex, IDictionary<TokensRegexNERAnnotator.Entry, int> entryToMappingFileNumber, bool verbose)
		{
			int origEntriesSize = entries.Count;
			int isTokensRegex = 0;
			int lineCount = 0;
			IDictionary<string, int> headerIndexMap = GetHeaderIndexMap(headerFields);
			int iPattern = GetIndex(headerIndexMap, PatternField);
			if (iPattern < 0)
			{
				throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Header does not contain 'pattern': " + StringUtils.Join(headerFields));
			}
			int iOverwrite = GetIndex(headerIndexMap, OverwriteField);
			int iPriority = GetIndex(headerIndexMap, PriorityField);
			int iWeight = GetIndex(headerIndexMap, WeightField);
			int iGroup = GetIndex(headerIndexMap, GroupField);
			int[] annotationCols = new int[annotationFieldnames.Length];
			int iLastAnnotationField = -1;
			for (int i = 0; i < annotationFieldnames.Length; i++)
			{
				annotationCols[i] = GetIndex(headerIndexMap, annotationFieldnames[i]);
				if (annotationCols[i] < 0)
				{
					throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Header does not contain annotation field '" + annotationFieldnames[i] + "': " + StringUtils.Join(headerFields));
				}
				if (annotationCols[i] > iLastAnnotationField)
				{
					iLastAnnotationField = annotationCols[i];
				}
			}
			// Take minimum of "pattern" and last annotation field; add one to it to map array index to minimum length
			int minLength = Math.Max(iPattern, iLastAnnotationField) + 1;
			int maxLength = headerFields.Length;
			// Take maximum number of headerFields
			for (string line; (line = mapping.ReadLine()) != null; )
			{
				lineCount++;
				string[] split = line.Split("\t");
				if (lineCount == 1)
				{
					if (split.Length == headerFields.Length)
					{
						bool equals = true;
						for (int i_1 = 0; i_1 < split.Length; i_1++)
						{
							if (!Objects.Equals(split[i_1], headerFields[i_1]))
							{
								equals = false;
								break;
							}
						}
						if (equals)
						{
							//This is the header line -> skip
							continue;
						}
					}
				}
				if (split.Length < minLength || split.Length > maxLength)
				{
					string err = "many";
					string expect = "<= " + maxLength;
					string extra = string.Empty;
					if (split.Length < minLength)
					{
						err = "few";
						expect = ">= " + minLength;
						if (split.Length == 1)
						{
							extra = "Maybe the problem is that you are using spaces not tabs? ";
						}
					}
					throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Line " + lineCount + " of provided mapping file has too " + err + " tab-separated columns (" + split.Length + " expecting " + expect + "). " + extra + "Line: "
						 + line);
				}
				string regex = split[iPattern].Trim();
				string tokensRegex = null;
				string[] regexes = null;
				if (regex.StartsWith("( ") && regex.EndsWith(" )"))
				{
					// Tokens regex (remove start and end parenthesis)
					tokensRegex = Sharpen.Runtime.Substring(regex, 1, regex.Length - 1).Trim();
				}
				else
				{
					regexes = regex.Split("\\s+");
				}
				string[] key = (regexes != null) ? regexes : new string[] { tokensRegex };
				if (ignoreCase)
				{
					string[] norm = new string[key.Length];
					for (int i_1 = 0; i_1 < key.Length; i_1++)
					{
						norm[i_1] = key[i_1].ToLower();
					}
					key = norm;
				}
				string[] types = new string[annotationCols.Length];
				for (int i_2 = 0; i_2 < annotationCols.Length; i_2++)
				{
					types[i_2] = split[annotationCols[i_2]].Trim();
				}
				ICollection<string> overwritableTypes = Generics.NewHashSet();
				double priority = 0.0;
				if (iOverwrite >= 0 && split.Length > iOverwrite)
				{
					if (NumberPattern.Matcher(split[iOverwrite].Trim()).Matches())
					{
						logger.Warn("Number in types column for " + Arrays.ToString(key) + " is probably priority: " + split[iOverwrite]);
					}
					Sharpen.Collections.AddAll(overwritableTypes, Arrays.AsList(CommaDelimitersPattern.Split(split[iOverwrite].Trim())));
				}
				if (iPriority >= 0 && split.Length > iPriority)
				{
					try
					{
						priority = double.Parse(split[iPriority].Trim());
					}
					catch (NumberFormatException e)
					{
						throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Invalid priority in line " + lineCount + " in regexner file " + mappingFilename + ": \"" + line + "\"!", e);
					}
				}
				double weight = 0.0;
				if (iWeight >= 0 && split.Length > iWeight)
				{
					try
					{
						weight = double.Parse(split[iWeight].Trim());
					}
					catch (NumberFormatException e)
					{
						throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Invalid weight in line " + lineCount + " in regexner file " + mappingFilename + ": \"" + line + "\"!", e);
					}
				}
				int annotateGroup = 0;
				// Get annotate group from input....
				if (iGroup >= 0 && split.Length > iGroup)
				{
					// Which group to take (allow for context)
					string context = split[iGroup].Trim();
					try
					{
						annotateGroup = System.Convert.ToInt32(context);
					}
					catch (NumberFormatException e)
					{
						throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Invalid group in line " + lineCount + " in regexner file " + mappingFilename + ": \"" + line + "\"!", e);
					}
				}
				// Print some warnings about the type
				for (int i_3 = 0; i_3 < types.Length; i_3++)
				{
					string type = types[i_3];
					// TODO: Have option to allow commas in types
					int commaPos = type.IndexOf(',');
					if (commaPos > 0)
					{
						// Strip the "," and just take first type
						string newType = Sharpen.Runtime.Substring(type, 0, commaPos).Trim();
						logger.Warn(annotatorName + ": Entry has multiple types for " + annotationFieldnames[i_3] + ": " + line + ".  Taking type to be " + newType);
						types[i_3] = newType;
					}
				}
				TokensRegexNERAnnotator.Entry entry = new TokensRegexNERAnnotator.Entry(tokensRegex, regexes, types, overwritableTypes, priority, weight, annotateGroup);
				if (seenRegexes.Contains(Arrays.AsList(key)))
				{
					TokensRegexNERAnnotator.Entry oldEntry = seenRegexes.Get(key);
					if (priority > oldEntry.priority)
					{
						logger.Warn(annotatorName + ": Replacing duplicate entry (higher priority): old=" + oldEntry + ", new=" + entry);
					}
					else
					{
						string oldTypeDesc = oldEntry.GetTypeDescription();
						string newTypeDesc = entry.GetTypeDescription();
						if (!oldTypeDesc.Equals(newTypeDesc))
						{
							if (verbose)
							{
								logger.Warn(annotatorName + ": Ignoring duplicate entry: " + split[0] + ", old type = " + oldTypeDesc + ", new type = " + newTypeDesc);
							}
						}
						// } else {
						//   if (verbose) {
						//     logger.warn(annotatorName + ": Duplicate entry [ignored]: " +
						//             split[0] + ", old type = " + oldEntry.type + ", new type = " + type);
						//   }
						continue;
					}
				}
				// Print some warning if label belongs to noDefaultOverwriteLabels but there is no overwritable types
				if (entry.overwritableTypes.IsEmpty() && HasNoOverwritableType(noDefaultOverwriteLabels, entry.types))
				{
					logger.Warn(annotatorName + ": Entry doesn't have overwriteable types " + entry + ", but entry type is in noDefaultOverwriteLabels");
				}
				entries.Add(entry);
				entryToMappingFileNumber[entry] = mappingFileIndex;
				seenRegexes.Put(key, entry);
				if (entry.tokensRegex != null)
				{
					isTokensRegex++;
				}
			}
			logger.Log(annotatorName + ": Read " + (entries.Count - origEntriesSize) + " unique entries out of " + lineCount + " from " + mappingFilename + ", " + isTokensRegex + " TokensRegex patterns.");
			return entries;
		}

		private static bool HasNoOverwritableType(ICollection<string> noDefaultOverwriteLabels, string[] types)
		{
			foreach (string type in types)
			{
				if (noDefaultOverwriteLabels.Contains(type))
				{
					return true;
				}
			}
			return false;
		}

		// todo [cdm 2016]: This logic seems wrong. If you have semi-colons only between files, it doesn't work!
		private static string[] ProcessListMappingFiles(string mappingFiles)
		{
			if (mappingFiles.Contains(";") && mappingFiles.Contains(","))
			{
				return SemicolonDelimitersPattern.Split(mappingFiles);
			}
			else
			{
				//Semicolons separate the files and for each file, commas separate the options - options handled later
				if (mappingFiles.Contains(","))
				{
					return CommaDelimitersPattern.Split(mappingFiles);
				}
				else
				{
					//No per-file options, commas separate the files
					//Semicolons separate the files
					return SemicolonDelimitersPattern.Split(mappingFiles);
				}
			}
		}

		private static string[] ProcessPerFileOptions(string annotatorName, string[] mappings, IList<bool> ignoreCaseList, IList<Pattern> validPosPatternList, IList<string[]> headerList, bool ignoreCase, Pattern validPosPattern, string[] headerFields
			, string[] annotationFieldnames, IList<Type> annotationFields)
		{
			int numMappingFiles = mappings.Length;
			for (int index = 0; index < numMappingFiles; index++)
			{
				bool ignoreCaseSet = false;
				bool validPosPatternSet = false;
				bool headerSet = false;
				string[] allOptions = CommaDelimitersPattern.Split(mappings[index].Trim());
				int numOptions = allOptions.Length;
				string filePath = allOptions[allOptions.Length - 1];
				if (numOptions > 1)
				{
					// there are some per file options here
					for (int i = 0; i < numOptions - 1; i++)
					{
						string[] optionAndValue = EqualsDelimitersPattern.Split(allOptions[i].Trim());
						if (optionAndValue.Length != 2)
						{
							throw new ArgumentException("TokensRegexNERAnnotator " + annotatorName + " ERROR: Incorrectly specified options for mapping file " + mappings[index].Trim());
						}
						else
						{
							switch (optionAndValue[0].Trim().ToLower())
							{
								case "ignorecase":
								{
									ignoreCaseList.Add(bool.Parse(optionAndValue[1].Trim()));
									ignoreCaseSet = true;
									break;
								}

								case "validpospattern":
								{
									string validPosRegex = optionAndValue[1].Trim();
									if (!StringUtils.IsNullOrEmpty(validPosRegex))
									{
										validPosPatternList.Add(Pattern.Compile(validPosRegex));
									}
									else
									{
										validPosPatternList.Add(validPosPattern);
									}
									validPosPatternSet = true;
									break;
								}

								case "header":
								{
									string header = optionAndValue[1].Trim();
									string[] headerItems = header.Split("\\s+");
									headerSet = true;
									if (headerItems.Length == 1 && Sharpen.Runtime.EqualsIgnoreCase(headerItems[0], "true"))
									{
										try
										{
											using (BufferedReader br = IOUtils.ReaderFromString(filePath))
											{
												string headerLine = br.ReadLine();
												headerItems = headerLine.Split("\\t");
											}
										}
										catch (IOException e)
										{
											logger.Err(e);
										}
									}
									headerList.Add(headerItems);
									foreach (string field in headerItems)
									{
										if (!predefinedHeaderFields.Contains(field) && !Arrays.AsList(annotationFieldnames).Contains(field))
										{
											Type fieldClass = EnvLookup.LookupAnnotationKeyWithClassname(null, field);
											if (fieldClass == null)
											{
												throw new Exception("Not recognized annotation class field \"" + field + "\" in header for mapping file " + allOptions[numOptions - 1]);
											}
											else
											{
												annotationFields.Add(fieldClass);
												annotationFieldnames = Arrays.CopyOf(annotationFieldnames, annotationFieldnames.Length + 1);
												annotationFieldnames[annotationFieldnames.Length - 1] = field;
											}
										}
									}
									break;
								}

								default:
								{
									break;
								}
							}
						}
					}
					mappings[index] = allOptions[numOptions - 1];
				}
				if (!ignoreCaseSet)
				{
					ignoreCaseList.Add(ignoreCase);
				}
				if (!validPosPatternSet)
				{
					validPosPatternList.Add(validPosPattern);
				}
				if (!headerSet)
				{
					headerList.Add(headerFields);
				}
			}
			return annotationFieldnames;
		}

		private static bool AtLeastOneValidPosPattern(IList<Pattern> validPosPatternList)
		{
			foreach (Pattern pattern in validPosPatternList)
			{
				if (pattern != null)
				{
					return true;
				}
			}
			return false;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			// TODO: we might want to allow for different RegexNER annotators
			// to satisfy different requirements
			return Java.Util.Collections.UnmodifiableSet(new ArraySet(annotationFields));
		}
	}
}
