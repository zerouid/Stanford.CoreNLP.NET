using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Serializes Annotation objects using our own format.</summary>
	/// <remarks>
	/// Serializes Annotation objects using our own format.
	/// Note[gabor]: This is a lossy serialization! For similar performance, and
	/// lossless (or less lossy) serialization see,
	/// <see cref="ProtobufAnnotationSerializer"/>
	/// .
	/// </remarks>
	/// <author>Mihai</author>
	public class CustomAnnotationSerializer : AnnotationSerializer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.CustomAnnotationSerializer));

		private readonly bool compress;

		/// <summary>
		/// If true, it means we store/load also AntecedentAnnotation
		/// This annotation is used ONLY in our KBP annotation.
		/// </summary>
		/// <remarks>
		/// If true, it means we store/load also AntecedentAnnotation
		/// This annotation is used ONLY in our KBP annotation.
		/// By default, it is not needed because we store the entire coref graph anyway.
		/// </remarks>
		private readonly bool haveExplicitAntecedent;

		public CustomAnnotationSerializer()
			: this(true, false)
		{
		}

		public CustomAnnotationSerializer(bool compress, bool haveAnte)
		{
			this.compress = compress;
			this.haveExplicitAntecedent = haveAnte;
		}

		/// <exception cref="System.IO.IOException"/>
		private static AnnotationSerializer.IntermediateSemanticGraph LoadDependencyGraph(BufferedReader reader)
		{
			AnnotationSerializer.IntermediateSemanticGraph graph = new AnnotationSerializer.IntermediateSemanticGraph();
			// first line: list of nodes
			string line = reader.ReadLine().Trim();
			// System.out.println("PARSING LINE: " + line);
			if (line.Length > 0)
			{
				string[] bits = line.Split("\t");
				if (bits.Length < 3)
				{
					throw new Exception("ERROR: Invalid dependency node line: " + line);
				}
				string docId = bits[0];
				if (docId.Equals("-"))
				{
					docId = string.Empty;
				}
				int sentIndex = System.Convert.ToInt32(bits[1]);
				for (int i = 2; i < bits.Length; i++)
				{
					string bit = bits[i];
					string[] bbits = bit.Split("-");
					int copyAnnotation = -1;
					bool isRoot = false;
					if (bbits.Length > 3)
					{
						throw new Exception("ERROR: Invalid format for dependency graph: " + line);
					}
					else
					{
						if (bbits.Length == 2)
						{
							copyAnnotation = System.Convert.ToInt32(bbits[1]);
						}
						else
						{
							if (bbits.Length == 3)
							{
								copyAnnotation = System.Convert.ToInt32(bbits[1]);
								isRoot = bbits[2].Equals("R");
							}
						}
					}
					int index = System.Convert.ToInt32(bbits[0]);
					graph.nodes.Add(new AnnotationSerializer.IntermediateNode(docId, sentIndex, index, copyAnnotation, isRoot));
				}
			}
			// second line: list of deps
			line = reader.ReadLine().Trim();
			if (line.Length > 0)
			{
				string[] bits = line.Split("\t");
				foreach (string bit in bits)
				{
					string[] bbits = bit.Split(" ");
					if (bbits.Length < 3 || bbits.Length > 6)
					{
						throw new Exception("ERROR: Invalid format for dependency graph: " + line);
					}
					string dep = bbits[0];
					int source = System.Convert.ToInt32(bbits[1]);
					int target = System.Convert.ToInt32(bbits[2]);
					bool isExtra = (bbits.Length == 4) ? bool.ParseBoolean(bbits[3]) : false;
					int sourceCopy = (bbits.Length > 4) ? System.Convert.ToInt32(bbits[4]) : 0;
					int targetCopy = (bbits.Length > 5) ? System.Convert.ToInt32(bbits[5]) : 0;
					graph.edges.Add(new AnnotationSerializer.IntermediateEdge(dep, source, sourceCopy, target, targetCopy, isExtra));
				}
			}
			return graph;
		}

		/// <summary>Saves all arcs in the graph on two lines: first line contains the vertices, second the edges.</summary>
		/// <param name="graph"/>
		/// <param name="pw"/>
		private static void SaveDependencyGraph(SemanticGraph graph, PrintWriter pw)
		{
			if (graph == null)
			{
				pw.Println();
				pw.Println();
				return;
			}
			bool outputHeader = false;
			foreach (IndexedWord node in graph.VertexSet())
			{
				// first line: sentence index for all nodes; we recover the words
				// from the original tokens the first two tokens in this line
				// indicate: docid, sentence index
				if (!outputHeader)
				{
					string docId = node.Get(typeof(CoreAnnotations.DocIDAnnotation));
					if (docId != null && docId.Length > 0)
					{
						pw.Print(docId);
					}
					else
					{
						pw.Print("-");
					}
					pw.Print("\t");
					pw.Print(node.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
					outputHeader = true;
				}
				pw.Print("\t");
				pw.Print(node.Index());
				// CopyAnnotations indicate copied (or virtual nodes) generated due to CCs (see EnglishGrammaticalStructure)
				// These annotations are usually not set, so print them only if necessary
				if (node.CopyCount() > 0)
				{
					pw.Print("-");
					pw.Print(node.CopyCount());
				}
				// System.out.println("FOUND COPY ANNOTATION: " + node.get(CoreAnnotations.CopyAnnotation.class));
				if (graph.GetRoots().Contains(node))
				{
					if (node.CopyCount() > 0)
					{
						pw.Print("-R");
					}
					else
					{
						pw.Print("-0-R");
					}
				}
			}
			pw.Println();
			// second line: all edges
			bool first = true;
			foreach (SemanticGraphEdge edge in graph.EdgeIterable())
			{
				if (!first)
				{
					pw.Print("\t");
				}
				string rel = edge.GetRelation().ToString();
				// no spaces allowed in the relation name
				// note that they might occur due to the tokenization of HTML/XML/RDF tags
				rel = rel.ReplaceAll("\\s+", string.Empty);
				pw.Print(rel);
				pw.Print(" ");
				pw.Print(edge.GetSource().Index());
				pw.Print(" ");
				pw.Print(edge.GetTarget().Index());
				if (edge.IsExtra() || edge.GetSource().CopyCount() > 0 || edge.GetTarget().CopyCount() > 0)
				{
					pw.Print(" ");
					pw.Print(edge.IsExtra());
					pw.Print(" ");
					pw.Print(edge.GetSource().CopyCount());
					pw.Print(" ");
					pw.Print(edge.GetTarget().CopyCount());
				}
				first = false;
			}
			pw.Println();
		}

		/// <summary>Serializes the CorefChain objects</summary>
		/// <param name="chains">all clusters in a doc</param>
		/// <param name="pw">the buffer</param>
		private static void SaveCorefChains(IDictionary<int, CorefChain> chains, PrintWriter pw)
		{
			if (chains == null)
			{
				pw.Println();
				return;
			}
			// how many clusters
			pw.Println(chains.Count);
			// save each cluster
			foreach (KeyValuePair<int, CorefChain> integerCorefChainEntry in chains)
			{
				// cluster id + how many mentions in the cluster
				SaveCorefChain(pw, integerCorefChainEntry.Key, integerCorefChainEntry.Value);
			}
			// an empty line at end
			pw.Println();
		}

		private static int CountMentions(CorefChain cluster)
		{
			int count = 0;
			foreach (IntPair mid in cluster.GetMentionMap().Keys)
			{
				count += cluster.GetMentionMap()[mid].Count;
			}
			return count;
		}

		/// <summary>Serializes one coref cluster (i.e., one entity).</summary>
		/// <param name="pw">the buffer</param>
		/// <param name="cid">id of cluster to save</param>
		/// <param name="cluster">the cluster</param>
		public static void SaveCorefChain(PrintWriter pw, int cid, CorefChain cluster)
		{
			pw.Println(cid + " " + CountMentions(cluster));
			// each mention saved on one line
			IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentionMap = cluster.GetMentionMap();
			foreach (KeyValuePair<IntPair, ICollection<CorefChain.CorefMention>> intPairSetEntry in mentionMap)
			{
				// all mentions with the same head
				IntPair mentionIndices = intPairSetEntry.Key;
				ICollection<CorefChain.CorefMention> mentions = intPairSetEntry.Value;
				foreach (CorefChain.CorefMention mention in mentions)
				{
					// one mention per line
					pw.Print(mentionIndices.GetSource() + " " + mentionIndices.GetTarget());
					if (mention == cluster.GetRepresentativeMention())
					{
						pw.Print(" " + 1);
					}
					else
					{
						pw.Print(" " + 0);
					}
					pw.Print(" " + mention.mentionType);
					pw.Print(" " + mention.number);
					pw.Print(" " + mention.gender);
					pw.Print(" " + mention.animacy);
					pw.Print(" " + mention.startIndex);
					pw.Print(" " + mention.endIndex);
					pw.Print(" " + mention.headIndex);
					pw.Print(" " + mention.corefClusterID);
					pw.Print(" " + mention.mentionID);
					pw.Print(" " + mention.sentNum);
					pw.Print(" " + mention.position.Length());
					for (int i = 0; i < mention.position.Length(); i++)
					{
						pw.Print(" " + mention.position.Get(i));
					}
					pw.Print(" " + EscapeSpace(mention.mentionSpan));
					pw.Println();
				}
			}
		}

		private static string EscapeSpace(string s)
		{
			return s.ReplaceAll("\\s", SpaceHolder);
		}

		private static string UnescapeSpace(string s)
		{
			return s.ReplaceAll(SpaceHolder, " ");
		}

		private static Dictionaries.MentionType ParseMentionType(string s)
		{
			return Dictionaries.MentionType.ValueOf(s);
		}

		private static Dictionaries.Number ParseNumber(string s)
		{
			return Dictionaries.Number.ValueOf(s);
		}

		private static Dictionaries.Gender ParseGender(string s)
		{
			return Dictionaries.Gender.ValueOf(s);
		}

		private static Dictionaries.Animacy ParseAnimacy(string s)
		{
			return Dictionaries.Animacy.ValueOf(s);
		}

		/// <summary>Loads the CorefChain objects from the serialized buffer.</summary>
		/// <param name="reader">the buffer</param>
		/// <returns>A map from cluster id to clusters</returns>
		/// <exception cref="System.IO.IOException"/>
		private static IDictionary<int, CorefChain> LoadCorefChains(BufferedReader reader)
		{
			string line = reader.ReadLine().Trim();
			if (line.IsEmpty())
			{
				return null;
			}
			int clusterCount = System.Convert.ToInt32(line);
			IDictionary<int, CorefChain> chains = Generics.NewHashMap();
			// read each cluster
			for (int c = 0; c < clusterCount; c++)
			{
				line = reader.ReadLine().Trim();
				string[] bits = line.Split("\\s");
				int cid = System.Convert.ToInt32(bits[0]);
				int mentionCount = System.Convert.ToInt32(bits[1]);
				IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentionMap = Generics.NewHashMap();
				CorefChain.CorefMention representative = null;
				// read each mention in this cluster
				for (int m = 0; m < mentionCount; m++)
				{
					line = reader.ReadLine();
					bits = line.Split("\\s");
					IntPair key = new IntPair(System.Convert.ToInt32(bits[0]), System.Convert.ToInt32(bits[1]));
					bool rep = bits[2].Equals("1");
					Dictionaries.MentionType mentionType = ParseMentionType(bits[3]);
					Dictionaries.Number number = ParseNumber(bits[4]);
					Dictionaries.Gender gender = ParseGender(bits[5]);
					Dictionaries.Animacy animacy = ParseAnimacy(bits[6]);
					int startIndex = System.Convert.ToInt32(bits[7]);
					int endIndex = System.Convert.ToInt32(bits[8]);
					int headIndex = System.Convert.ToInt32(bits[9]);
					int clusterID = System.Convert.ToInt32(bits[10]);
					int mentionID = System.Convert.ToInt32(bits[11]);
					int sentNum = System.Convert.ToInt32(bits[12]);
					int posLen = System.Convert.ToInt32(bits[13]);
					int[] posElems = new int[posLen];
					for (int i = 0; i < posLen; i++)
					{
						posElems[i] = System.Convert.ToInt32(bits[14 + i]);
					}
					IntTuple position = new IntTuple(posElems);
					string span = UnescapeSpace(bits[14 + posLen]);
					CorefChain.CorefMention mention = new CorefChain.CorefMention(mentionType, number, gender, animacy, startIndex, endIndex, headIndex, clusterID, mentionID, sentNum, position, span);
					ICollection<CorefChain.CorefMention> mentionsWithThisHead = mentionMap[key];
					if (mentionsWithThisHead == null)
					{
						mentionsWithThisHead = Generics.NewHashSet();
						mentionMap[key] = mentionsWithThisHead;
					}
					mentionsWithThisHead.Add(mention);
					if (rep)
					{
						representative = mention;
					}
				}
				// construct the cluster
				CorefChain chain = new CorefChain(cid, mentionMap, representative);
				chains[cid] = chain;
			}
			reader.ReadLine();
			return chains;
		}

		/// <exception cref="System.IO.IOException"/>
		public override OutputStream Write(Annotation corpus, OutputStream os)
		{
			if (!(os is GZIPOutputStream))
			{
				if (compress)
				{
					os = new GZIPOutputStream(os);
				}
			}
			PrintWriter pw = new PrintWriter(os);
			// save the coref graph in the new format
			IDictionary<int, CorefChain> chains = corpus.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			SaveCorefChains(chains, pw);
			// save the coref graph on one line
			// Note: this is the old format!
			IList<Pair<IntTuple, IntTuple>> corefGraph = corpus.Get(typeof(CorefCoreAnnotations.CorefGraphAnnotation));
			if (corefGraph != null)
			{
				bool first = true;
				foreach (Pair<IntTuple, IntTuple> arc in corefGraph)
				{
					if (!first)
					{
						pw.Print(" ");
					}
					pw.Printf("%d %d %d %d", arc.first.Get(0), arc.first.Get(1), arc.second.Get(0), arc.second.Get(1));
					first = false;
				}
			}
			pw.Println();
			// save sentences separated by an empty line
			IList<ICoreMap> sentences = corpus.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sent in sentences)
			{
				// save the parse tree first, on a single line
				Tree tree = sent.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				if (tree != null)
				{
					string treeString = tree.ToString();
					// no \n allowed in the parse tree string (might happen due to tokenization of HTML/XML/RDF tags)
					treeString = treeString.ReplaceAll("\n", " ");
					pw.Println(treeString);
				}
				else
				{
					pw.Println();
				}
				SemanticGraph collapsedDeps = sent.Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation));
				SaveDependencyGraph(collapsedDeps, pw);
				SemanticGraph uncollapsedDeps = sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
				SaveDependencyGraph(uncollapsedDeps, pw);
				SemanticGraph ccDeps = sent.Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation));
				SaveDependencyGraph(ccDeps, pw);
				// save all sentence tokens
				IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
				if (tokens != null)
				{
					foreach (CoreLabel token in tokens)
					{
						SaveToken(token, haveExplicitAntecedent, pw);
						pw.Println();
					}
				}
				// add an empty line after every sentence
				pw.Println();
			}
			pw.Flush();
			return os;
		}

		/// <exception cref="System.IO.IOException"/>
		public override Pair<Annotation, InputStream> Read(InputStream @is)
		{
			if (compress && !(@is is GZIPInputStream))
			{
				@is = new GZIPInputStream(@is);
			}
			BufferedReader reader = new BufferedReader(new InputStreamReader(@is));
			Annotation doc = new Annotation(string.Empty);
			string line;
			// read the coref graph (new format)
			IDictionary<int, CorefChain> chains = LoadCorefChains(reader);
			if (chains != null)
			{
				doc.Set(typeof(CorefCoreAnnotations.CorefChainAnnotation), chains);
			}
			// read the coref graph (old format)
			line = reader.ReadLine().Trim();
			if (line.Length > 0)
			{
				string[] bits = line.Split(" ");
				if (bits.Length % 4 != 0)
				{
					throw new RuntimeIOException("ERROR: Incorrect format for the serialized coref graph: " + line);
				}
				IList<Pair<IntTuple, IntTuple>> corefGraph = new List<Pair<IntTuple, IntTuple>>();
				for (int i = 0; i < bits.Length; i += 4)
				{
					IntTuple src = new IntTuple(2);
					IntTuple dst = new IntTuple(2);
					src.Set(0, System.Convert.ToInt32(bits[i]));
					src.Set(1, System.Convert.ToInt32(bits[i + 1]));
					dst.Set(0, System.Convert.ToInt32(bits[i + 2]));
					dst.Set(1, System.Convert.ToInt32(bits[i + 3]));
					corefGraph.Add(new Pair<IntTuple, IntTuple>(src, dst));
				}
				doc.Set(typeof(CorefCoreAnnotations.CorefGraphAnnotation), corefGraph);
			}
			// read individual sentences
			IList<ICoreMap> sentences = new List<ICoreMap>();
			while ((line = reader.ReadLine()) != null)
			{
				ICoreMap sentence = new Annotation(string.Empty);
				// first line is the parse tree. construct it with CoreLabels in Tree nodes
				Tree tree = new PennTreeReader(new StringReader(line), new LabeledScoredTreeFactory(CoreLabel.Factory())).ReadTree();
				sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), tree);
				// read the dependency graphs
				AnnotationSerializer.IntermediateSemanticGraph intermCollapsedDeps = LoadDependencyGraph(reader);
				AnnotationSerializer.IntermediateSemanticGraph intermUncollapsedDeps = LoadDependencyGraph(reader);
				AnnotationSerializer.IntermediateSemanticGraph intermCcDeps = LoadDependencyGraph(reader);
				// the remaining lines until empty line are tokens
				IList<CoreLabel> tokens = new List<CoreLabel>();
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Length == 0)
					{
						break;
					}
					CoreLabel token = LoadToken(line, haveExplicitAntecedent);
					tokens.Add(token);
				}
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				// convert the intermediate graph to an actual SemanticGraph
				SemanticGraph collapsedDeps = intermCollapsedDeps.ConvertIntermediateGraph(tokens);
				sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), collapsedDeps);
				SemanticGraph uncollapsedDeps = intermUncollapsedDeps.ConvertIntermediateGraph(tokens);
				sentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), uncollapsedDeps);
				SemanticGraph ccDeps = intermCcDeps.ConvertIntermediateGraph(tokens);
				sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), ccDeps);
				sentences.Add(sentence);
			}
			doc.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
			return Pair.MakePair(doc, @is);
		}

		private const string SpaceHolder = "##";

		private static CoreLabel LoadToken(string line, bool haveExplicitAntecedent)
		{
			CoreLabel token = new CoreLabel();
			string[] bits = line.Split("\t", -1);
			if (bits.Length < 7)
			{
				throw new RuntimeIOException("ERROR: Invalid format token for serialized token (only " + bits.Length + " tokens): " + line);
			}
			// word
			string word = bits[0].ReplaceAll(SpaceHolder, " ");
			token.Set(typeof(CoreAnnotations.TextAnnotation), word);
			token.Set(typeof(CoreAnnotations.ValueAnnotation), word);
			// if(word.length() == 0) log.info("FOUND 0-LENGTH TOKEN!");
			// lemma
			if (bits[1].Length > 0 || bits[0].Length == 0)
			{
				string lemma = bits[1].ReplaceAll(SpaceHolder, " ");
				token.Set(typeof(CoreAnnotations.LemmaAnnotation), lemma);
			}
			// POS tag
			if (bits[2].Length > 0)
			{
				token.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), bits[2]);
			}
			// NE tag
			if (bits[3].Length > 0)
			{
				token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), bits[3]);
			}
			// Normalized NE tag
			if (bits[4].Length > 0)
			{
				token.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), bits[4]);
			}
			// Character offsets
			if (bits[5].Length > 0)
			{
				token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), System.Convert.ToInt32(bits[5]));
			}
			if (bits[6].Length > 0)
			{
				token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), System.Convert.ToInt32(bits[6]));
			}
			if (haveExplicitAntecedent)
			{
				// This block is specific to KBP
				// We may have AntecedentAnnotation
				if (bits.Length > 7)
				{
					string aa = bits[7].ReplaceAll(SpaceHolder, " ");
					if (aa.Length > 0)
					{
						token.Set(typeof(CoreAnnotations.AntecedentAnnotation), aa);
					}
				}
			}
			return token;
		}

		/// <summary>Saves one individual sentence token, in a simple tabular format, in the style of CoNLL</summary>
		/// <param name="token"/>
		/// <param name="pw"/>
		private static void SaveToken(CoreLabel token, bool haveExplicitAntecedent, PrintWriter pw)
		{
			string word = token.Get(typeof(CoreAnnotations.TextAnnotation));
			if (word == null)
			{
				word = token.Get(typeof(CoreAnnotations.ValueAnnotation));
			}
			if (word != null)
			{
				word = word.ReplaceAll("\\s+", SpaceHolder);
				// spaces are used for formatting
				pw.Print(word);
			}
			pw.Print("\t");
			string lemma = token.Get(typeof(CoreAnnotations.LemmaAnnotation));
			if (lemma != null)
			{
				lemma = lemma.ReplaceAll("\\s+", SpaceHolder);
				// spaces are used for formatting
				pw.Print(lemma);
			}
			pw.Print("\t");
			string pos = token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			if (pos != null)
			{
				pw.Print(pos);
			}
			pw.Print("\t");
			string ner = token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			if (ner != null)
			{
				pw.Print(ner);
			}
			pw.Print("\t");
			string normNer = token.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation));
			if (normNer != null)
			{
				pw.Print(normNer);
			}
			pw.Print("\t");
			int charBegin = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			if (charBegin != null)
			{
				pw.Print(charBegin);
			}
			pw.Print("\t");
			int charEnd = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			if (charEnd != null)
			{
				pw.Print(charEnd);
			}
			if (haveExplicitAntecedent)
			{
				// This block is specific to KBP
				// in some cases where we now the entity in focus (i.e., web queries), AntecedentAnnotation is generated
				// let's save it as an optional, always last, token
				string aa = token.Get(typeof(CoreAnnotations.AntecedentAnnotation));
				if (aa != null)
				{
					pw.Print("\t");
					aa = aa.ReplaceAll("\\s+", SpaceHolder);
					// spaces are used for formatting
					pw.Print(aa);
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			string file = props.GetProperty("file");
			string loadFile = props.GetProperty("loadFile");
			if (loadFile != null && !loadFile.IsEmpty())
			{
				Edu.Stanford.Nlp.Pipeline.CustomAnnotationSerializer ser = new Edu.Stanford.Nlp.Pipeline.CustomAnnotationSerializer(false, false);
				InputStream @is = new FileInputStream(loadFile);
				Pair<Annotation, InputStream> pair = ser.Read(@is);
				pair.second.Close();
				Annotation anno = pair.first;
				System.Console.Out.WriteLine(anno.ToShorterString(StringUtils.EmptyStringArray));
				@is.Close();
			}
			else
			{
				if (file != null && !file.Equals(string.Empty))
				{
					string text = IOUtils.SlurpFile(file);
					Annotation doc = new Annotation(text);
					pipeline.Annotate(doc);
					Edu.Stanford.Nlp.Pipeline.CustomAnnotationSerializer ser = new Edu.Stanford.Nlp.Pipeline.CustomAnnotationSerializer(false, false);
					TextWriter os = new TextWriter(new FileOutputStream(file + ".ser"));
					ser.Write(doc, os).Close();
					log.Info("Serialized annotation saved in " + file + ".ser");
				}
				else
				{
					log.Info("usage: CustomAnnotationSerializer [-file file] [-loadFile file]");
				}
			}
		}
	}
}
