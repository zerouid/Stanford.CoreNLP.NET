using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading;
using Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace
{
	/// <summary>Simple wrapper of Mihai's ACE code to ie.machinereading.structure objects.</summary>
	/// <author>David McClosky</author>
	public class AceReader : GenericDataSetReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.AceReader));

		private readonly ICounter<string> entityCounts;

		private readonly ICounter<string> adjacentEntityMentions;

		private readonly ICounter<string> relationCounts;

		private readonly ICounter<string> nameRelationCounts;

		private readonly ICounter<string> eventCounts;

		private readonly ICounter<string> mentionTypeCounts;

		private readonly string aceVersion;

		private const bool Verbose = false;

		/// <summary>Make an AceReader.</summary>
		public AceReader()
			: this(null, true)
		{
		}

		public AceReader(StanfordCoreNLP processor, bool preprocess)
			: this(processor, preprocess, "ACE2005")
		{
		}

		public AceReader(StanfordCoreNLP processor, bool preprocess, string version)
			: base(processor, preprocess, false, true)
		{
			entityCounts = new ClassicCounter<string>();
			adjacentEntityMentions = new ClassicCounter<string>();
			nameRelationCounts = new ClassicCounter<string>();
			relationCounts = new ClassicCounter<string>();
			eventCounts = new ClassicCounter<string>();
			mentionTypeCounts = new ClassicCounter<string>();
			logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.AceReader).FullName);
			// run quietly by default
			logger.SetLevel(Level.Severe);
			aceVersion = version;
		}

		/// <summary>Reads in ACE*.apf.xml files and converts them to RelationSentence objects.</summary>
		/// <remarks>
		/// Reads in ACE*.apf.xml files and converts them to RelationSentence objects.
		/// Note that you probably should call parse() instead.
		/// Currently, this ignores document boundaries (the list returned will include
		/// sentences from all documents).
		/// </remarks>
		/// <param name="path">
		/// directory containing ACE files to read (e.g.
		/// "/home/mcclosky/scr/data/ACE2005/english_test"). This can also be
		/// the path to a single file.
		/// </param>
		/// <returns>list of RelationSentence objects</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public override Annotation Read(string path)
		{
			IList<ICoreMap> allSentences = new List<ICoreMap>();
			File basePath = new File(path);
			System.Diagnostics.Debug.Assert(basePath.Exists());
			Annotation corpus = new Annotation(string.Empty);
			if (basePath.IsDirectory())
			{
				foreach (File aceFile in IOUtils.IterFilesRecursive(basePath, ".apf.xml"))
				{
					if (aceFile.GetName().EndsWith(".UPC1.apf.xml"))
					{
						continue;
					}
					Sharpen.Collections.AddAll(allSentences, ReadDocument(aceFile, corpus));
				}
			}
			else
			{
				// in case it's a file
				Sharpen.Collections.AddAll(allSentences, ReadDocument(basePath, corpus));
			}
			AnnotationUtils.AddSentences(corpus, allSentences);
			// quick stats
			foreach (ICoreMap sent in allSentences)
			{
				// check for entity mentions of the same type that are adjacent
				CountAdjacentMentions(sent);
				// count relations between two proper nouns
				CountNameRelations(sent);
				// count types of mentions
				CountMentionTypes(sent);
			}
			return corpus;
		}

		private void CountMentionTypes(ICoreMap sent)
		{
			IList<EntityMention> mentions = sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			if (mentions != null)
			{
				foreach (EntityMention m in mentions)
				{
					mentionTypeCounts.IncrementCount(m.GetMentionType());
				}
			}
		}

		private void CountNameRelations(ICoreMap sent)
		{
			IList<RelationMention> mentions = sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			if (mentions != null)
			{
				foreach (RelationMention m in mentions)
				{
					IList<EntityMention> args = m.GetEntityMentionArgs();
					if (args.Count == 2 && args[0].GetMentionType().Equals("NAM") && args[1].GetMentionType().Equals("NAM"))
					{
						nameRelationCounts.IncrementCount(m.GetType() + "." + m.GetSubType());
					}
				}
			}
		}

		private void CountAdjacentMentions(ICoreMap sent)
		{
			IList<EntityMention> mentions = sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			if (mentions != null)
			{
				foreach (EntityMention m1 in mentions)
				{
					foreach (EntityMention m2 in mentions)
					{
						if (m1 == m2)
						{
							continue;
						}
						if (m1.GetHeadTokenEnd() == m2.GetHeadTokenStart() && m1.GetType().Equals(m2.GetType()))
						{
							adjacentEntityMentions.IncrementCount(m1.GetType());
						}
					}
				}
			}
		}

		// todo: Change to use a counters print method (get sorting for free!)
		private void PrintCounter(ICounter<string> c, string h)
		{
			StringBuilder b = new StringBuilder();
			b.Append(h).Append(" counts:\n");
			ICollection<string> keys = c.KeySet();
			foreach (string k in keys)
			{
				b.Append("\t").Append(k).Append(": ").Append(c.GetCount(k)).Append("\n");
			}
			logger.Info(b.ToString());
		}

		/// <summary>
		/// Reads in a single ACE*.apf.xml file and convert it to RelationSentence
		/// objects.
		/// </summary>
		/// <remarks>
		/// Reads in a single ACE*.apf.xml file and convert it to RelationSentence
		/// objects. However, you probably should call parse() instead.
		/// </remarks>
		/// <param name="file">A file object of an ACE file</param>
		/// <returns>list of RelationSentence objects</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		private IList<ICoreMap> ReadDocument(File file, Annotation corpus)
		{
			// remove the extension to make it into a prefix
			string aceFilename = file.GetAbsolutePath().Replace(".apf.xml", string.Empty);
			IList<ICoreMap> sentencesFromFile = ReadDocument(aceFilename, corpus);
			return sentencesFromFile;
		}

		/// <summary>
		/// Reads in a single ACE*.apf.xml file and convert it to RelationSentence
		/// objects.
		/// </summary>
		/// <remarks>
		/// Reads in a single ACE*.apf.xml file and convert it to RelationSentence
		/// objects. However, you probably should call parse() instead.
		/// </remarks>
		/// <param name="prefix">
		/// prefix of ACE filename to read (e.g.
		/// "/u/mcclosky/scr/data/ACE2005/english_test/bc/CNN_CF_20030827.1630.01"
		/// ) (no ".apf.xml" extension)
		/// </param>
		/// <returns>list of RelationSentence objects</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		private IList<ICoreMap> ReadDocument(string prefix, Annotation corpus)
		{
			logger.Info("Reading document: " + prefix);
			IList<ICoreMap> results = new List<ICoreMap>();
			AceDocument aceDocument;
			if (aceVersion.Equals("ACE2004"))
			{
				aceDocument = AceDocument.ParseDocument(prefix, false, aceVersion);
			}
			else
			{
				aceDocument = AceDocument.ParseDocument(prefix, false);
			}
			string docId = aceDocument.GetId();
			// map entity mention ID strings to their EntityMention counterparts
			IDictionary<string, EntityMention> entityMentionMap = Generics.NewHashMap();
			/*
			for (int sentenceIndex = 0; sentenceIndex < aceDocument.getSentenceCount(); sentenceIndex++) {
			List<AceToken> tokens = aceDocument.getSentence(sentenceIndex);
			StringBuffer b = new StringBuffer();
			for(AceToken t: tokens) b.append(t.getLiteral() + " " );
			logger.info("SENTENCE: " + b.toString());
			}
			*/
			int tokenOffset = 0;
			for (int sentenceIndex = 0; sentenceIndex < aceDocument.GetSentenceCount(); sentenceIndex++)
			{
				IList<AceToken> tokens = aceDocument.GetSentence(sentenceIndex);
				IList<CoreLabel> words = new List<CoreLabel>();
				StringBuilder textContent = new StringBuilder();
				for (int i = 0; i < tokens.Count; i++)
				{
					CoreLabel l = new CoreLabel();
					l.SetWord(tokens[i].GetLiteral());
					l.Set(typeof(CoreAnnotations.ValueAnnotation), l.Word());
					l.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), tokens[i].GetByteStart());
					l.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), tokens[i].GetByteEnd());
					words.Add(l);
					if (i > 0)
					{
						textContent.Append(" ");
					}
					textContent.Append(tokens[i].GetLiteral());
				}
				// skip "sentences" that are really just SGML tags (which come from using the RobustTokenizer)
				if (words.Count == 1)
				{
					string word = words[0].Word();
					if (word.StartsWith("<") && word.EndsWith(">"))
					{
						tokenOffset += tokens.Count;
						continue;
					}
				}
				ICoreMap sentence = new Annotation(textContent.ToString());
				sentence.Set(typeof(CoreAnnotations.DocIDAnnotation), docId);
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), words);
				logger.Info("Reading sentence: \"" + textContent + "\"");
				IList<AceEntityMention> entityMentions = aceDocument.GetEntityMentions(sentenceIndex);
				IList<AceRelationMention> relationMentions = aceDocument.GetRelationMentions(sentenceIndex);
				IList<AceEventMention> eventMentions = aceDocument.GetEventMentions(sentenceIndex);
				// convert entity mentions
				foreach (AceEntityMention aceEntityMention in entityMentions)
				{
					string corefID = string.Empty;
					foreach (string entityID in aceDocument.GetKeySetEntities())
					{
						AceEntity e = aceDocument.GetEntity(entityID);
						if (e.GetMentions().Contains(aceEntityMention))
						{
							corefID = entityID;
							break;
						}
					}
					EntityMention convertedMention = ConvertAceEntityMention(aceEntityMention, docId, sentence, tokenOffset, corefID);
					//        EntityMention convertedMention = convertAceEntityMention(aceEntityMention, docId, sentence, tokenOffset);
					entityCounts.IncrementCount(convertedMention.GetType());
					logger.Info("CONVERTED MENTION HEAD SPAN: " + convertedMention.GetHead());
					logger.Info("CONVERTED ENTITY MENTION: " + convertedMention);
					AnnotationUtils.AddEntityMention(sentence, convertedMention);
					entityMentionMap[aceEntityMention.GetId()] = convertedMention;
				}
				// TODO: make Entity objects as needed
				// convert relation mentions
				foreach (AceRelationMention aceRelationMention in relationMentions)
				{
					RelationMention convertedMention = ConvertAceRelationMention(aceRelationMention, docId, sentence, entityMentionMap);
					if (convertedMention != null)
					{
						relationCounts.IncrementCount(convertedMention.GetType());
						logger.Info("CONVERTED RELATION MENTION: " + convertedMention);
						AnnotationUtils.AddRelationMention(sentence, convertedMention);
					}
				}
				// TODO: make Relation objects
				// convert EventMentions
				foreach (AceEventMention aceEventMention in eventMentions)
				{
					EventMention convertedMention = ConvertAceEventMention(aceEventMention, docId, sentence, entityMentionMap, tokenOffset);
					if (convertedMention != null)
					{
						eventCounts.IncrementCount(convertedMention.GetType());
						logger.Info("CONVERTED EVENT MENTION: " + convertedMention);
						AnnotationUtils.AddEventMention(sentence, convertedMention);
					}
				}
				// TODO: make Event objects
				results.Add(sentence);
				tokenOffset += tokens.Count;
			}
			return results;
		}

		private EventMention ConvertAceEventMention(AceEventMention aceEventMention, string docId, ICoreMap sentence, IDictionary<string, EntityMention> entityMap, int tokenOffset)
		{
			ICollection<string> roleSet = aceEventMention.GetRoles();
			IList<string> roles = new List<string>();
			foreach (string role in roleSet)
			{
				roles.Add(role);
			}
			IList<ExtractionObject> convertedArgs = new List<ExtractionObject>();
			int left = int.MaxValue;
			int right = int.MinValue;
			foreach (string role_1 in roles)
			{
				AceEntityMention arg = aceEventMention.GetArg(role_1);
				ExtractionObject o = entityMap[arg.GetId()];
				if (o == null)
				{
					logger.Severe("READER ERROR: Failed to find event argument with id " + arg.GetId());
					logger.Severe("This happens because a few event mentions illegally span multiple sentences. Will ignore this mention.");
					return null;
				}
				convertedArgs.Add(o);
				if (o.GetExtentTokenStart() < left)
				{
					left = o.GetExtentTokenStart();
				}
				if (o.GetExtentTokenEnd() > right)
				{
					right = o.GetExtentTokenEnd();
				}
			}
			AceCharSeq anchor = aceEventMention.GetAnchor();
			ExtractionObject anchorObject = new ExtractionObject(aceEventMention.GetId() + "-anchor", sentence, new Span(anchor.GetTokenStart() - tokenOffset, anchor.GetTokenEnd() + 1 - tokenOffset), "ANCHOR", null);
			EventMention em = new EventMention(aceEventMention.GetId(), sentence, new Span(left, right), aceEventMention.GetParent().GetType(), aceEventMention.GetParent().GetSubtype(), anchorObject, convertedArgs, roles);
			return em;
		}

		private RelationMention ConvertAceRelationMention(AceRelationMention aceRelationMention, string docId, ICoreMap sentence, IDictionary<string, EntityMention> entityMap)
		{
			IList<AceRelationMentionArgument> args = Arrays.AsList(aceRelationMention.GetArgs());
			IList<ExtractionObject> convertedArgs = new List<ExtractionObject>();
			IList<string> argNames = new List<string>();
			// the arguments are already stored in semantic order. Make sure we preserve the same ordering!
			int left = int.MaxValue;
			int right = int.MinValue;
			foreach (AceRelationMentionArgument arg in args)
			{
				ExtractionObject o = entityMap[arg.GetContent().GetId()];
				if (o == null)
				{
					logger.Severe("READER ERROR: Failed to find relation argument with id " + arg.GetContent().GetId());
					logger.Severe("This happens because a few relation mentions illegally span multiple sentences. Will ignore this mention.");
					return null;
				}
				convertedArgs.Add(o);
				argNames.Add(arg.GetRole());
				if (o.GetExtentTokenStart() < left)
				{
					left = o.GetExtentTokenStart();
				}
				if (o.GetExtentTokenEnd() > right)
				{
					right = o.GetExtentTokenEnd();
				}
			}
			if (argNames.Count != 2 || !Sharpen.Runtime.EqualsIgnoreCase(argNames[0], "arg-1") || !Sharpen.Runtime.EqualsIgnoreCase(argNames[1], "arg-2"))
			{
				logger.Severe("READER ERROR: Invalid succession of arguments in relation mention: " + argNames);
				logger.Severe("ACE relations must have two arguments. Will ignore this mention.");
				return null;
			}
			RelationMention relation = new RelationMention(aceRelationMention.GetId(), sentence, new Span(left, right), aceRelationMention.GetParent().GetType(), aceRelationMention.GetParent().GetSubtype(), convertedArgs, null);
			return relation;
		}

		/// <summary>
		/// Convert an
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceEntityMention"/>
		/// to an
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention"/>
		/// .
		/// </summary>
		/// <param name="entityMention">
		/// 
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceEntityMention"/>
		/// to convert
		/// </param>
		/// <param name="docId">ID of the document containing this entity mention</param>
		/// <param name="sentence"/>
		/// <param name="tokenOffset">
		/// An offset in the calculations of position of the extent to sentence boundary
		/// (the ace.reader stores absolute token offset from the beginning of the document, but
		/// we need token offsets from the beginning of the sentence =&gt; adjust by tokenOffset)
		/// </param>
		/// <returns>
		/// entity as an
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention"/>
		/// </returns>
		private EntityMention ConvertAceEntityMention(AceEntityMention entityMention, string docId, ICoreMap sentence, int tokenOffset)
		{
			//log.info("TYPE is " + entityMention.getParent().getType());
			//log.info("SUBTYPE is " + entityMention.getParent().getSubtype());
			//log.info("LDCTYPE is " + entityMention.getLdctype());
			AceCharSeq ext = entityMention.GetExtent();
			AceCharSeq head = entityMention.GetHead();
			int extStart = ext.GetTokenStart() - tokenOffset;
			int extEnd = ext.GetTokenEnd() - tokenOffset + 1;
			if (extStart < 0)
			{
				logger.Severe("READER ERROR: Invalid extent start " + extStart + " for entity mention " + entityMention.GetId() + " in document " + docId + " in sentence " + sentence);
				logger.Severe("This may happen due to incorrect EOS detection. Adjusting entity extent.");
				extStart = 0;
			}
			if (extEnd > sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Count)
			{
				logger.Severe("READER ERROR: Invalid extent end " + extEnd + " for entity mention " + entityMention.GetId() + " in document " + docId + " in sentence " + sentence);
				logger.Severe("This may happen due to incorrect EOS detection. Adjusting entity extent.");
				extEnd = sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Count;
			}
			int headStart = head.GetTokenStart() - tokenOffset;
			int headEnd = head.GetTokenEnd() - tokenOffset + 1;
			if (headStart < 0)
			{
				logger.Severe("READER ERROR: Invalid head start " + headStart + " for entity mention " + entityMention.GetId() + " in document " + docId + " in sentence " + sentence);
				logger.Severe("This may happen due to incorrect EOS detection. Adjusting entity head span.");
				headStart = 0;
			}
			if (headEnd > sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Count)
			{
				logger.Severe("READER ERROR: Invalid head end " + headEnd + " for entity mention " + entityMention.GetId() + " in document " + docId + " in sentence " + sentence);
				logger.Severe("This may happen due to incorrect EOS detection. Adjusting entity head span.");
				headEnd = sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Count;
			}
			// must adjust due to possible incorrect EOS detection
			if (headStart < extStart)
			{
				headStart = extStart;
			}
			if (headEnd > extEnd)
			{
				headEnd = extEnd;
			}
			System.Diagnostics.Debug.Assert((headStart < headEnd));
			// note: the ace.reader stores absolute token offset from the beginning of the document, but
			//       we need token offsets from the beginning of the sentence => adjust by tokenOffset
			// note: in ace.reader the end token position is inclusive, but
			//       in our setup the end token position is exclusive => add 1 to end
			EntityMention converted = new EntityMention(entityMention.GetId(), sentence, new Span(extStart, extEnd), new Span(headStart, headEnd), entityMention.GetParent().GetType(), entityMention.GetParent().GetSubtype(), entityMention.GetLdctype());
			return converted;
		}

		private EntityMention ConvertAceEntityMention(AceEntityMention entityMention, string docId, ICoreMap sentence, int tokenOffset, string corefID)
		{
			EntityMention converted = ConvertAceEntityMention(entityMention, docId, sentence, tokenOffset);
			converted.SetCorefID(corefID);
			return converted;
		}

		// simple testing code
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.AceReader r = new Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.AceReader(new StanfordCoreNLP(props, false), false);
			r.SetLoggerLevel(Level.Info);
			r.Parse("/u/scr/nlp/data/ACE2005/");
			// Annotation a = r.parse("/user/mengqiu/scr/twitter/nlp/corpus_prep/standalone/ar/data");
			// BasicEntityExtractor.saveCoNLLFiles("/tmp/conll", a, false, false);
			log.Info("done");
		}
	}
}
