using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>Factor out some useful methods more than lexparser module may want.</summary>
	public class ParserUtils
	{
		private ParserUtils()
		{
		}

		// static methods
		/// <summary>Construct a fall through tree in case we can't parse this sentence.</summary>
		/// <param name="words">Words of the sentence that didn't parse</param>
		/// <returns>
		/// A tree with X for all the internal nodes.
		/// Preterminals have the right tag if the words are tagged.
		/// </returns>
		public static Tree XTree<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			ITreeFactory treeFactory = new LabeledScoredTreeFactory();
			IList<Tree> lst2 = new List<Tree>();
			foreach (IHasWord obj in words)
			{
				string s = obj.Word();
				Tree t = treeFactory.NewLeaf(s);
				string tag = "XX";
				if (obj is IHasTag)
				{
					if (((IHasTag)obj).Tag() != null)
					{
						tag = ((IHasTag)obj).Tag();
					}
				}
				Tree t2 = treeFactory.NewTreeNode(tag, Java.Util.Collections.SingletonList(t));
				lst2.Add(t2);
			}
			return treeFactory.NewTreeNode("X", lst2);
		}

		public static void PrintOutOfMemory(PrintWriter pw)
		{
			pw.Println();
			pw.Println("*******************************************************");
			pw.Println("***  WARNING!! OUT OF MEMORY! THERE WAS NOT ENOUGH  ***");
			pw.Println("***  MEMORY TO RUN ALL PARSERS.  EITHER GIVE THE    ***");
			pw.Println("***  JVM MORE MEMORY, SET THE MAXIMUM SENTENCE      ***");
			pw.Println("***  LENGTH WITH -maxLength, OR PERHAPS YOU ARE     ***");
			pw.Println("***  HAPPY TO HAVE THE PARSER FALL BACK TO USING    ***");
			pw.Println("***  A SIMPLER PARSER FOR VERY LONG SENTENCES.      ***");
			pw.Println("*******************************************************");
			pw.Println();
		}
	}
}
