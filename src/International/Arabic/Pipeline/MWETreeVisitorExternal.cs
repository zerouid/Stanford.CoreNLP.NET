using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Converts all contiguous MWEs listed in an MWE list to flattened trees.</summary>
	/// <author>Spence Green</author>
	public class MWETreeVisitorExternal : ITreeVisitor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.MWETreeVisitorExternal));

		private const string mweFile = "/home/rayder441/sandbox/javanlp/projects/core/data/edu/stanford/nlp/pipeline/attia-mwe-list.txt.out.tok.fixed.proc.uniq";

		private readonly ICollection<string> mweDictionary;

		public MWETreeVisitorExternal()
		{
			mweDictionary = LoadMWEs();
		}

		private ICollection<string> LoadMWEs()
		{
			ICollection<string> mweSet = Generics.NewHashSet();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(mweFile), "UTF-8"));
				for (string line; (line = br.ReadLine()) != null; )
				{
					mweSet.Add(line.Trim());
				}
				br.Close();
			}
			catch (UnsupportedEncodingException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return mweSet;
		}

		/// <summary>Perform (possibly destructive) operations on the tree.</summary>
		/// <remarks>Perform (possibly destructive) operations on the tree. Do a top-down DFS on the tree.</remarks>
		public virtual void VisitTree(Tree tree)
		{
			if (tree == null)
			{
				return;
			}
			string yield = SentenceUtils.ListToString(tree.Yield());
			if (mweDictionary.Contains(yield))
			{
				IList<Tree> children = GetPreterminalSubtrees(tree);
				string newLabel = "MW" + tree.Value();
				tree.SetValue(newLabel);
				tree.SetChildren(children);
				// Bottom out of the recursion
				return;
			}
			else
			{
				foreach (Tree subTree in tree.Children())
				{
					if (subTree.IsPhrasal())
					{
						// Only phrasal trees can have yields > 1!!
						VisitTree(subTree);
					}
				}
			}
		}

		private IList<Tree> GetPreterminalSubtrees(Tree tree)
		{
			IList<Tree> preterminals = new List<Tree>();
			foreach (Tree subTree in tree)
			{
				if (subTree.IsPreTerminal())
				{
					preterminals.Add(subTree);
				}
			}
			return preterminals;
		}

		/// <summary>For debugging.</summary>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s atb_tree_file > atb_tree_file.out%n", typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.MWETreeVisitorExternal).FullName);
				System.Environment.Exit(-1);
			}
			ITreeReaderFactory trf = new ArabicTreeReaderFactory();
			try
			{
				ITreeReader tr = trf.NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(args[0]), "UTF-8")));
				ITreeVisitor visitor = new Edu.Stanford.Nlp.International.Arabic.Pipeline.MWETreeVisitorExternal();
				int treeId = 0;
				for (Tree tree; (tree = tr.ReadTree()) != null; ++treeId)
				{
					if (tree.Value().Equals("ROOT"))
					{
						// Skip over the ROOT tag
						tree = tree.FirstChild();
					}
					visitor.VisitTree(tree);
					System.Console.Out.WriteLine(tree.ToString());
				}
				tr.Close();
				System.Console.Error.Printf("Processed %d trees.%n", treeId);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
