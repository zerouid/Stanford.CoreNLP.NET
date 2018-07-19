using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Demo
{
	/// <summary>
	/// A small demo that shows how to convert a tree to a SemanticGraph
	/// and then run a SemgrexPattern on it
	/// </summary>
	/// <author>John Bauer</author>
	public class SemgrexDemo
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.Demo.SemgrexDemo));

		private SemgrexDemo()
		{
		}

		// just static main
		public static void Main(string[] args)
		{
			string treeString = "(ROOT  (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))";
			// Typically the tree is constructed by parsing or reading a
			// treebank.  This is just for example purposes
			Tree tree = Tree.ValueOf(treeString);
			// This creates English uncollapsed dependencies as a
			// SemanticGraph.  If you are creating many SemanticGraphs, you
			// should use a GrammaticalStructureFactory and use it to generate
			// the intermediate GrammaticalStructure instead
			SemanticGraph graph = SemanticGraphFactory.GenerateUncollapsedDependencies(tree);
			// Alternatively, this could have been the Chinese params or any
			// other language supported.  As of 2014, only English and Chinese
			ITreebankLangParserParams @params = new EnglishTreebankParserParams();
			IGrammaticalStructureFactory gsf = @params.TreebankLanguagePack().GrammaticalStructureFactory(@params.TreebankLanguagePack().PunctuationWordRejectFilter(), @params.TypedDependencyHeadFinder());
			GrammaticalStructure gs = gsf.NewGrammaticalStructure(tree);
			log.Info(graph);
			SemgrexPattern semgrex = SemgrexPattern.Compile("{}=A <<nsubj {}=B");
			SemgrexMatcher matcher = semgrex.Matcher(graph);
			// This will produce two results on the given tree: "likes" is an
			// ancestor of both "dog" and "my" via the nsubj relation
			while (matcher.Find())
			{
				log.Info(matcher.GetNode("A") + " <<nsubj " + matcher.GetNode("B"));
			}
		}
	}
}
