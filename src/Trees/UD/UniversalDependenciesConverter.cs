using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <summary>
	/// Command-line utility to:
	/// a) convert constituency trees to basic English UD trees;
	/// b) convert basic dependency trees to enhanced and enhanced++ UD graphs.
	/// </summary>
	/// <author>Sebastian Schuster</author>
	public class UniversalDependenciesConverter
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.UD.UniversalDependenciesConverter));

		private static string NerCombinerName = "edu.stanford.nlp.ie.NERClassifierCombiner";

		private static readonly bool UseName = Runtime.GetProperty("UDUseNameRelation") != null;

		private UniversalDependenciesConverter()
		{
		}

		// static main
		private static GrammaticalStructure SemanticGraphToGrammaticalStructure(SemanticGraph sg)
		{
			/* sg.typedDependency() generates an ArrayList */
			IList<TypedDependency> deps = (IList<TypedDependency>)sg.TypedDependencies();
			IndexedWord root = deps[0].Gov();
			TreeGraphNode rootNode = new TreeGraphNode(root);
			GrammaticalStructure gs = new UniversalEnglishGrammaticalStructure(deps, rootNode);
			return gs;
		}

		/// <summary>Converts basic UD tree to enhanced UD graph.</summary>
		private static SemanticGraph ConvertBasicToEnhanced(SemanticGraph sg)
		{
			GrammaticalStructure gs = SemanticGraphToGrammaticalStructure(sg);
			return SemanticGraphFactory.GenerateEnhancedDependencies(gs);
		}

		/// <summary>Converts basic UD tree to enhanced++ UD graph.</summary>
		private static SemanticGraph ConvertBasicToEnhancedPlusPlus(SemanticGraph sg)
		{
			GrammaticalStructure gs = SemanticGraphToGrammaticalStructure(sg);
			return SemanticGraphFactory.GenerateEnhancedPlusPlusDependencies(gs);
		}

		private static SemanticGraph ConvertTreeToBasic(Tree tree)
		{
			AddLemmata(tree);
			AddNERTags(tree);
			SemanticGraph sg = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.None, null, false, true);
			AddLemmata(sg);
			if (UseName)
			{
				AddNERTags(sg);
			}
			return sg;
		}

		private class TreeToSemanticGraphIterator : IEnumerator<SemanticGraph>
		{
			private IEnumerator<Tree> treeIterator;

			private Tree currentTree;

			public TreeToSemanticGraphIterator(IEnumerator<Tree> treeIterator)
			{
				// = null;
				this.treeIterator = treeIterator;
			}

			public virtual bool MoveNext()
			{
				return treeIterator.MoveNext();
			}

			public virtual SemanticGraph Current
			{
				get
				{
					Tree t = treeIterator.Current;
					currentTree = t;
					return ConvertTreeToBasic(t);
				}
			}

			public virtual Tree GetCurrentTree()
			{
				return this.currentTree;
			}
		}

		private static Morphology Morph = new Morphology();

		// end static class TreeToSemanticGraphIterator
		private static void AddLemmata(SemanticGraph sg)
		{
			sg.VertexListSorted().ForEach(null);
		}

		private static void AddLemmata(Tree tree)
		{
			tree.Yield().ForEach(null);
		}

		/// <summary>variables for accessing NERClassifierCombiner via reflection</summary>
		private static object NerTagger;

		private static MethodInfo NerClassifyMethod;

		// = null;
		// = null;
		/// <summary>Try to set up the NER tagger.</summary>
		private static void SetupNERTagger()
		{
			Type NerTaggerClass;
			try
			{
				NerTaggerClass = Sharpen.Runtime.GetType(NerCombinerName);
			}
			catch (Exception)
			{
				log.Warn(NerCombinerName + " not found - not applying NER tags!");
				return;
			}
			try
			{
				MethodInfo createMethod = Sharpen.Runtime.GetDeclaredMethod(NerTaggerClass, "createNERClassifierCombiner", typeof(string), typeof(Properties));
				NerTagger = createMethod.Invoke(null, null, new Properties());
				NerClassifyMethod = Sharpen.Runtime.GetDeclaredMethod(NerTaggerClass, "classify", typeof(IList));
			}
			catch (Exception)
			{
				log.Warn("Error setting up " + NerCombinerName + "! Not applying NER tags!");
			}
		}

		/// <summary>Add NER tags to a semantic graph.</summary>
		private static void AddNERTags(SemanticGraph sg)
		{
			// set up tagger if necessary
			if (NerTagger == null || NerClassifyMethod == null)
			{
				SetupNERTagger();
			}
			if (NerTagger != null && NerClassifyMethod != null)
			{
				// we have everything successfully setup and so can act.
				try
				{
					// classify
					IList<CoreLabel> labels = sg.VertexListSorted().Stream().Map(null).Collect(Collectors.ToList());
					NerClassifyMethod.Invoke(NerTagger, labels);
				}
				catch (Exception)
				{
					log.Warn("Error running " + NerCombinerName + " on SemanticGraph!  Not applying NER tags!");
				}
			}
		}

		/// <summary>Add NER tags to a tree.</summary>
		private static void AddNERTags(Tree tree)
		{
			// set up tagger if necessary
			if (NerTagger == null || NerClassifyMethod == null)
			{
				SetupNERTagger();
			}
			if (NerTagger != null && NerClassifyMethod != null)
			{
				// we have everything successfully setup and so can act.
				try
				{
					// classify
					IList<CoreLabel> labels = tree.Yield().Stream().Map(null).Collect(Collectors.ToList());
					NerClassifyMethod.Invoke(NerTagger, labels);
				}
				catch (Exception)
				{
					log.Warn("Error running " + NerCombinerName + " on Tree!  Not applying NER tags!");
				}
			}
		}

		/// <summary>
		/// Converts a constituency tree to the English basic, enhanced, or
		/// enhanced++ Universal dependencies representation, or an English basic
		/// Universal dependencies tree to the enhanced or enhanced++ representation.
		/// </summary>
		/// <remarks>
		/// Converts a constituency tree to the English basic, enhanced, or
		/// enhanced++ Universal dependencies representation, or an English basic
		/// Universal dependencies tree to the enhanced or enhanced++ representation.
		/// <p>
		/// Command-line options:<br />
		/// <c>-treeFile</c>
		/// : File with PTB-formatted constituency trees<br />
		/// <c>-conlluFile</c>
		/// : File with basic dependency trees in CoNLL-U format<br />
		/// <c>-outputRepresentation</c>
		/// : "basic" (default), "enhanced", or "enhanced++"
		/// </remarks>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			string treeFileName = props.GetProperty("treeFile");
			string conlluFileName = props.GetProperty("conlluFile");
			string outputRepresentation = props.GetProperty("outputRepresentation", "basic");
			IEnumerator<SemanticGraph> sgIterator;
			// = null;
			if (treeFileName != null)
			{
				MemoryTreebank tb = new MemoryTreebank(new NPTmpRetainingTreeNormalizer(0, false, 1, false));
				tb.LoadPath(treeFileName);
				IEnumerator<Tree> treeIterator = tb.GetEnumerator();
				sgIterator = new UniversalDependenciesConverter.TreeToSemanticGraphIterator(treeIterator);
			}
			else
			{
				if (conlluFileName != null)
				{
					CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
					try
					{
						sgIterator = reader.GetIterator(IOUtils.ReaderFromString(conlluFileName));
					}
					catch (Exception e)
					{
						throw new Exception(e);
					}
				}
				else
				{
					System.Console.Error.WriteLine("No input file specified!");
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.Printf("Usage: java %s [-treeFile trees.tree | -conlluFile deptrees.conllu]" + " [-outputRepresentation basic|enhanced|enhanced++ (default: basic)]%n", typeof(UniversalDependenciesConverter).GetCanonicalName());
					return;
				}
			}
			CoNLLUDocumentWriter writer = new CoNLLUDocumentWriter();
			while (sgIterator.MoveNext())
			{
				SemanticGraph sg = sgIterator.Current;
				if (treeFileName != null)
				{
					//add UPOS tags
					Tree tree = ((UniversalDependenciesConverter.TreeToSemanticGraphIterator)sgIterator).GetCurrentTree();
					Tree uposTree = UniversalPOSMapper.MapTree(tree);
					IList<ILabel> uposLabels = uposTree.PreTerminalYield();
					foreach (IndexedWord token in sg.VertexListSorted())
					{
						int idx = token.Index() - 1;
						string uposTag = uposLabels[idx].Value();
						token.Set(typeof(CoreAnnotations.CoarseTagAnnotation), uposTag);
					}
				}
				else
				{
					AddLemmata(sg);
					if (UseName)
					{
						AddNERTags(sg);
					}
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(outputRepresentation, "enhanced"))
				{
					sg = ConvertBasicToEnhanced(sg);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(outputRepresentation, "enhanced++"))
					{
						sg = ConvertBasicToEnhancedPlusPlus(sg);
					}
				}
				System.Console.Out.Write(writer.PrintSemanticGraph(sg));
			}
		}
	}
}
