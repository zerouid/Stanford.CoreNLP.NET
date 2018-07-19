using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Text;
using Java.Util;
using Java.Util.Regex;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A script to convert a TSV dump from our KBP sentences table into a Turk-task ready clause splitting dataset.</summary>
	/// <author>Gabor Angeli</author>
	public class CreateClauseDataset : ITSVSentenceProcessor
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Naturalli.CreateClauseDataset));

		private static InputStream @in = Runtime.@in;

		private CreateClauseDataset()
		{
		}

		// static methods class
		private static Span ToSpan<_T0>(IList<_T0> chunk)
			where _T0 : IHasIndex
		{
			int min = int.MaxValue;
			int max = -1;
			foreach (IHasIndex word in chunk)
			{
				min = Math.Min(word.Index() - 1, min);
				max = Math.Max(word.Index(), max);
			}
			System.Diagnostics.Debug.Assert(min >= 0);
			System.Diagnostics.Debug.Assert(max < int.MaxValue && max > 0);
			return new Span(min, max);
		}

		public virtual void Process(long id, Annotation doc)
		{
			ICoreMap sentence = doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
			SemanticGraph depparse = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			log.Info("| " + sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
			// Get all valid subject spans
			BitSet consumedAsSubjects = new BitSet();
			IList<Span> subjectSpans = new List<Span>();
			foreach (IndexedWord head in depparse.TopologicalSort())
			{
				// Check if the node is a noun/pronoun
				if (head.Tag().StartsWith("N") || head.Tag().Equals("PRP"))
				{
					// Try to get the NP chunk
					Optional<IList<IndexedWord>> subjectChunk = segmenter.GetValidChunk(depparse, head, segmenter.ValidSubjectArcs, Optional.Empty(), true);
					if (subjectChunk.IsPresent())
					{
						// Make sure it's not already a member of a larger NP
						foreach (IndexedWord tok in subjectChunk.Get())
						{
							if (consumedAsSubjects.Get(tok.Index()))
							{
								goto NEXTNODE_continue;
							}
						}
						// Already considered. Continue to the next node.
						// Register it as an NP
						foreach (IndexedWord tok_1 in subjectChunk.Get())
						{
							consumedAsSubjects.Set(tok_1.Index());
						}
						// Add it as a subject
						subjectSpans.Add(ToSpan(subjectChunk.Get()));
					}
				}
			}
NEXTNODE_break: ;
		}

		/// <summary>The pattern for traces which are potential subjects</summary>
		private static readonly Pattern TraceTargetPattern = Pattern.Compile("(NP-.*)-([0-9]+)");

		/// <summary>The pattern for trace markers.</summary>
		private static readonly Pattern TraceSourcePattern = Pattern.Compile(".*\\*-([0-9]+)");

		/// <summary>The converter from constituency to dependency trees.</summary>
		private static readonly UniversalEnglishGrammaticalStructureFactory parser = new UniversalEnglishGrammaticalStructureFactory();

		/// <summary>The OpenIE segmenter to use.</summary>
		private static readonly RelationTripleSegmenter segmenter = new RelationTripleSegmenter();

		/// <summary>The natural logic annotator for marking polarity.</summary>
		private static readonly NaturalLogicAnnotator natlog = new NaturalLogicAnnotator();

		/// <summary>Parse a given constituency tree into a dependency graph.</summary>
		/// <param name="tree">The constituency tree, in Penn Treebank style.</param>
		/// <returns>The dependency graph for the tree.</returns>
		private static SemanticGraph Parse(Tree tree)
		{
			return new SemanticGraph(parser.NewGrammaticalStructure(tree).TypedDependenciesCollapsed());
		}

		/// <summary>
		/// Create a dataset of subject/object pairs, such that a sequence of splits that segments this
		/// subject and object is a correct sequence.
		/// </summary>
		/// <param name="depparse">The dependency parse of the sentence.</param>
		/// <param name="traceTargets">The set of spans corresponding to targets of traces.</param>
		/// <param name="traceSources">The set of indices in a sentence corresponding to the sources of traces.</param>
		/// <returns>A dataset of subject/object spans.</returns>
		private static ICollection<Pair<Span, Span>> SubjectObjectPairs(SemanticGraph depparse, IList<CoreLabel> tokens, IDictionary<int, Span> traceTargets, IDictionary<int, int> traceSources)
		{
			//    log(StringUtils.join(tokens.stream().map(CoreLabel::word), " "));
			IList<Pair<Span, Span>> data = new List<Pair<Span, Span>>();
			foreach (SemgrexPattern vpPattern in segmenter.VpPatterns)
			{
				SemgrexMatcher matcher = vpPattern.Matcher(depparse);
				while (matcher.Find())
				{
					// Get the verb and object
					IndexedWord verb = matcher.GetNode("verb");
					IndexedWord @object = matcher.GetNode("object");
					if (verb != null && @object != null)
					{
						// See if there is already a subject attached
						bool hasSubject = false;
						foreach (SemanticGraphEdge edge in depparse.OutgoingEdgeIterable(verb))
						{
							if (edge.GetRelation().ToString().Contains("subj"))
							{
								hasSubject = true;
							}
						}
						foreach (SemanticGraphEdge edge_1 in depparse.OutgoingEdgeIterable(@object))
						{
							if (edge_1.GetRelation().ToString().Contains("subj"))
							{
								hasSubject = true;
							}
						}
						if (!hasSubject)
						{
							// Get the spans for the verb and object
							Optional<IList<IndexedWord>> verbChunk = segmenter.GetValidChunk(depparse, verb, segmenter.ValidAdverbArcs, Optional.Empty(), true);
							Optional<IList<IndexedWord>> objectChunk = segmenter.GetValidChunk(depparse, @object, segmenter.ValidObjectArcs, Optional.Empty(), true);
							if (verbChunk.IsPresent() && objectChunk.IsPresent())
							{
								verbChunk.Get().Sort(IComparer.ComparingInt(null));
								objectChunk.Get().Sort(IComparer.ComparingInt(null));
								// Find a trace
								int traceId = -1;
								Span verbSpan = ToSpan(verbChunk.Get());
								Span traceSpan = Span.FromValues(verbSpan.Start() - 1, verbSpan.End() + 1);
								foreach (KeyValuePair<int, int> entry in traceSources)
								{
									if (traceSpan.Contains(entry.Value))
									{
										traceId = entry.Key;
									}
								}
								//noinspection StatementWithEmptyBody
								if (traceId < 0)
								{
								}
								else
								{
									// Register the VP as an unknown VP
									//                List<CoreLabel> vpChunk = new ArrayList<>();
									//                vpChunk.addAll(verbChunk.get());
									//                vpChunk.addAll(objectChunk.get());
									//                Collections.sort(vpChunk, (a, b) -> a.index() - b.index());
									//                debug("could not find trace for " + vpChunk);
									// Add the obj chunk
									Span subjectSpan = traceTargets[traceId];
									Span objectSpan = ToSpan(objectChunk.Get());
									if (subjectSpan != null)
									{
										//                  debug("(" +
										//                      StringUtils.join(tokens.subList(subjectSpan.start(), subjectSpan.end()).stream().map(CoreLabel::word), " ") + "; " +
										//                      verb.word() + "; " +
										//                      StringUtils.join(tokens.subList(objectSpan.start(), objectSpan.end()).stream().map(CoreLabel::word), " ") +
										//                      ")");
										data.Add(Pair.MakePair(subjectSpan, objectSpan));
									}
								}
							}
						}
					}
				}
			}
			// Run vanilla pattern splits
			foreach (SemgrexPattern vpPattern_1 in segmenter.VerbPatterns)
			{
				SemgrexMatcher matcher = vpPattern_1.Matcher(depparse);
				while (matcher.Find())
				{
					// Get the verb and object
					IndexedWord subject = matcher.GetNode("subject");
					IndexedWord @object = matcher.GetNode("object");
					if (subject != null && @object != null)
					{
						Optional<IList<IndexedWord>> subjectChunk = segmenter.GetValidChunk(depparse, subject, segmenter.ValidSubjectArcs, Optional.Empty(), true);
						Optional<IList<IndexedWord>> objectChunk = segmenter.GetValidChunk(depparse, @object, segmenter.ValidObjectArcs, Optional.Empty(), true);
						if (subjectChunk.IsPresent() && objectChunk.IsPresent())
						{
							Span subjectSpan = ToSpan(subjectChunk.Get());
							Span objectSpan = ToSpan(objectChunk.Get());
							data.Add(Pair.MakePair(subjectSpan, objectSpan));
						}
					}
				}
			}
			return data;
		}

		/// <summary>Collect all the possible targets for traces.</summary>
		/// <remarks>Collect all the possible targets for traces. This is limited to NP-style traces.</remarks>
		/// <param name="root">The tree to search in. This is a recursive function.</param>
		/// <returns>The set of trace targets. The key is the id of the trace, the value is the span of the target of the trace.</returns>
		private static IDictionary<int, Span> FindTraceTargets(Tree root)
		{
			IDictionary<int, Span> spansInTree = new Dictionary<int, Span>(4);
			Matcher m = TraceTargetPattern.Matcher(root.Label().Value() == null ? "NULL" : root.Label().Value());
			if (m.Matches())
			{
				int index = System.Convert.ToInt32(m.Group(2));
				spansInTree[index] = Span.FromPair(root.GetSpan()).ToExclusive();
			}
			foreach (Tree child in root.Children())
			{
				spansInTree.PutAll(FindTraceTargets(child));
			}
			return spansInTree;
		}

		/// <summary>Collect all the trace markers in the sentence.</summary>
		/// <param name="root">The tree to search in. This is a recursive function.</param>
		/// <returns>A map of trace sources. The key is hte id of the trace, the value is the index of the trace's source in the sentence.</returns>
		private static IDictionary<int, int> FindTraceSources(Tree root)
		{
			IDictionary<int, int> spansInTree = new Dictionary<int, int>(4);
			Matcher m = TraceSourcePattern.Matcher(root.Label().Value() == null ? "NULL" : root.Label().Value());
			if (m.Matches())
			{
				int index = System.Convert.ToInt32(m.Group(1));
				spansInTree[index] = ((CoreLabel)root.Label()).Index() - 1;
			}
			foreach (Tree child in root.Children())
			{
				spansInTree.PutAll(FindTraceSources(child));
			}
			return spansInTree;
		}

		/// <summary>Count the number of extractions in the given dataset.</summary>
		/// <remarks>
		/// Count the number of extractions in the given dataset. That is, the sum count of the pair spans
		/// for each sentence.
		/// </remarks>
		/// <param name="data">The dataset.</param>
		/// <returns>The number of extractions in the datasets..</returns>
		private static int CountDatums(IList<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> data)
		{
			int count = 0;
			foreach (Pair<ICoreMap, ICollection<Pair<Span, Span>>> datum in data)
			{
				count += datum.second.Count;
			}
			return count;
		}

		/// <summary>Process all the trees in the given directory.</summary>
		/// <remarks>Process all the trees in the given directory. For example, the WSJ section of the Penn Treebank.</remarks>
		/// <param name="name">The name of the directory we are processing.</param>
		/// <param name="directory">The directory we are processing.</param>
		/// <returns>
		/// A dataset of subject/object pairs in the trees in the directory.
		/// This is a list of sentences, such that each sentence has a collection of pairs of spans.
		/// Each pair of spans is a subject/object span pair that constitutes a valid extraction.
		/// </returns>
		/// <exception cref="System.IO.IOException"/>
		private static IList<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> ProcessDirectory(string name, File directory)
		{
			Redwood.Util.ForceTrack("Processing " + name);
			// Prepare the files to iterate over
			IEnumerable<File> files = IOUtils.IterFilesRecursive(directory, "mrg");
			int numTreesProcessed = 0;
			IList<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> trainingData = new List<Pair<ICoreMap, ICollection<Pair<Span, Span>>>>(1024);
			// Iterate over the files
			foreach (File file in files)
			{
				//      log(file);
				ITreeReader reader = new PennTreeReader(IOUtils.ReaderFromFile(file));
				Tree tree;
				while ((tree = reader.ReadTree()) != null)
				{
					try
					{
						// Prepare the tree
						tree.IndexSpans();
						tree.SetSpans();
						// Get relevant information from sentence
						IList<CoreLabel> tokens = tree.GetLeaves().Stream().Map(null).Collect(Collectors.ToList());
						//            .filter(leaf -> !TRACE_SOURCE_PATTERN.matcher(leaf.word()).matches() && !leaf.tag().equals("-NONE-"))
						SemanticGraph graph = Parse(tree);
						IDictionary<int, Span> targets = FindTraceTargets(tree);
						IDictionary<int, int> sources = FindTraceSources(tree);
						// Create a sentence object
						ICoreMap sentence = new _ArrayCoreMap_325(tokens, graph, 4);
						natlog.DoOneSentence(null, sentence);
						// Generate training data
						ICollection<Pair<Span, Span>> trainingDataFromSentence = SubjectObjectPairs(graph, tokens, targets, sources);
						trainingData.Add(Pair.MakePair(sentence, trainingDataFromSentence));
						// Debug print
						numTreesProcessed += 1;
						if (numTreesProcessed % 100 == 0)
						{
							Redwood.Util.Log("[" + new DecimalFormat("00000").Format(numTreesProcessed) + "] " + CountDatums(trainingData) + " known extractions");
						}
					}
					catch (Exception t)
					{
						Sharpen.Runtime.PrintStackTrace(t);
					}
				}
			}
			// End
			Redwood.Util.Log(string.Empty + numTreesProcessed + " trees processed yielding " + CountDatums(trainingData) + " known extractions");
			Redwood.Util.EndTrack("Processing " + name);
			return trainingData;
		}

		private sealed class _ArrayCoreMap_325 : ArrayCoreMap
		{
			public _ArrayCoreMap_325(IList<CoreLabel> tokens, SemanticGraph graph, int baseArg1)
				: base(baseArg1)
			{
				this.tokens = tokens;
				this.graph = graph;
				{
					this.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
					this.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), graph);
					this.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), graph);
					this.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), graph);
				}
			}

			private readonly IList<CoreLabel> tokens;

			private readonly SemanticGraph graph;
		}

		/// <summary>The main entry point of the code.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Redwood.Util.ForceTrack("Processing treebanks");
			IList<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> trainingData = new List<Pair<ICoreMap, ICollection<Pair<Span, Span>>>>();
			Sharpen.Collections.AddAll(trainingData, ProcessDirectory("WSJ", new File("/home/gabor/lib/data/penn_treebank/wsj")));
			Sharpen.Collections.AddAll(trainingData, ProcessDirectory("Brown", new File("/home/gabor/lib/data/penn_treebank/brown")));
			Redwood.Util.EndTrack("Processing treebanks");
			Redwood.Util.ForceTrack("Training");
			Redwood.Util.Log("dataset size: " + trainingData.Count);
			IClauseSplitter.Train(trainingData.Stream(), new File("/home/gabor/tmp/clauseSearcher.ser.gz"), new File("/home/gabor/tmp/clauseSearcherData.tab.gz"));
			Redwood.Util.EndTrack("Training");
		}
		//    Execution.fillOptions(CreateClauseDataset.class, args);
		//
		//    new CreateClauseDataset().runAndExit(in, System.err, code -> code);
	}
}
