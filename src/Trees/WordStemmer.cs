using Edu.Stanford.Nlp.Process;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Stems the Words in a Tree using Morphology.</summary>
	/// <author>Huy Nguyen (htnguyen@cs.stanford.edu)</author>
	public class WordStemmer : ITreeVisitor
	{
		public WordStemmer()
		{
		}

		public virtual void VisitTree(Tree t)
		{
			// A single Morphology is not threadsafe, so to make this class
			// threadsafe, we have to create a new Morphology for each visit
			ProcessTree(t, null, new Morphology());
		}

		private static void ProcessTree(Tree t, string tag, Morphology morpha)
		{
			if (t.IsPreTerminal())
			{
				tag = t.Label().Value();
			}
			if (t.IsLeaf())
			{
				t.Label().SetValue(morpha.Lemma(t.Label().Value(), tag));
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					ProcessTree(kid, tag, morpha);
				}
			}
		}

		/// <summary>Reads, stems, and prints the trees in the file.</summary>
		/// <param name="args">Usage: WordStemmer file</param>
		public static void Main(string[] args)
		{
			Treebank treebank = new DiskTreebank();
			treebank.LoadPath(args[0]);
			Edu.Stanford.Nlp.Trees.WordStemmer ls = new Edu.Stanford.Nlp.Trees.WordStemmer();
			foreach (Tree tree in treebank)
			{
				ls.VisitTree(tree);
				System.Console.Out.WriteLine(tree);
			}
		}
	}
}
