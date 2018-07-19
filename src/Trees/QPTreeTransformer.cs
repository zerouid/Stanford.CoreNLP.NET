using System;
using System.IO;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Transforms an English structure parse tree in order to get the dependencies right:
	/// Adds an extra structure in QP phrases:
	/// <br />
	/// (QP (RB well) (IN over) (CD 9)) becomes
	/// <br />
	/// (QP (XS (RB well) (IN over)) (CD 9))
	/// <br />
	/// (QP (...) (CC ...) (...)) becomes
	/// <br />
	/// (QP (NP ...) (CC ...) (NP ...))
	/// </summary>
	/// <author>mcdm</author>
	public class QPTreeTransformer : ITreeTransformer
	{
		private bool universalDependencies = false;

		public QPTreeTransformer()
			: this(false)
		{
		}

		public QPTreeTransformer(bool universalDependencies)
		{
			this.universalDependencies = universalDependencies;
		}

		/// <summary>
		/// Right now (Jan 2013) we only deal with the following QP structures:
		/// <ul>
		/// <li> NP (QP ...) (QP (CC and/or) ...)
		/// <li> QP (RB IN CD|DT ...)   well over, more than
		/// <li> QP (JJR IN CD|DT ...)  fewer than
		/// <li> QP (IN JJS CD|DT ...)  at least
		/// <li> QP (...
		/// </summary>
		/// <remarks>
		/// Right now (Jan 2013) we only deal with the following QP structures:
		/// <ul>
		/// <li> NP (QP ...) (QP (CC and/or) ...)
		/// <li> QP (RB IN CD|DT ...)   well over, more than
		/// <li> QP (JJR IN CD|DT ...)  fewer than
		/// <li> QP (IN JJS CD|DT ...)  at least
		/// <li> QP (... CC ...)        between 5 and 10
		/// </ul>
		/// </remarks>
		/// <param name="t">tree to be transformed</param>
		/// <returns>The tree t with an extra layer if there was a QP structure matching the ones mentioned above</returns>
		public virtual Tree TransformTree(Tree t)
		{
			return QPtransform(t);
		}

		private static TregexPattern flattenNPoverQPTregex = TregexPattern.Compile("NP < (QP=left $+ (QP=right < CC))");

		private static TsurgeonPattern flattenNPoverQPTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree QP left right] [excise left left] [excise right right]");

		private static TregexPattern multiwordXSTregex = TregexPattern.Compile("QP <1 /^RB|JJ|IN/=left [ ( <2 /^JJ|IN/=right <3 /^CD|DT/ ) | ( <2 /^JJ|IN/ <3 ( IN=right < /^(?i:as|than)$/ ) <4 /^CD|DT/ ) ] ");

		private static TsurgeonPattern multiwordXSTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree XS left right");

		private static TregexPattern splitCCTregex = TregexPattern.Compile("QP < (CC $- __=r1 $+ __=l2 ?$-- /^[$]|CC$/=lnum ?$++ /^[$]|CC$/=rnum) <1 __=l1 <- __=r2 !< (__ < (__ < __))");

		private static TsurgeonPattern splitCCTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[if exists lnum createSubtree QP l1 r1] [if not exists lnum createSubtree NP l1 r1] " + "[if exists rnum createSubtree QP l2 r2] [if not exists rnum createSubtree NP l2 r2]"
			);

		private static TregexPattern splitMoneyTregex = TregexPattern.Compile("QP < (/^[$]$/ !$++ /^(?!([$]|CD)).*$/ !$++ (__ < (__ < __)) $+ __=left) <- __=right");

		private static TsurgeonPattern splitMoneyTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree QP left right");

		// TODO: should add NN and $ to the numeric expressions captured
		//   NN is for words such as "half" which are probably misparsed
		// TODO: <3 (IN < as|than) is to avoid one weird case in PTB,
		// "more than about".  Perhaps there is some way to generalize this
		// TODO: "all but X"
		// TODO: "all but about X"
		// the old style split any flat QP with a CC in the middle
		// TOD: there should be some allowances for phrases such as "or more", "or so", etc
		/// <summary>
		/// Transforms t if it contains one of the following QP structure:
		/// <ul>
		/// <li> NP (QP ...) (QP (CC and/or) ...)
		/// <li> QP (RB IN CD|DT ...)   well over, more than
		/// <li> QP (JJR IN CD|DT ...)  fewer than
		/// <li> QP (IN JJS CD|DT ...)  at least
		/// <li> QP (...
		/// </summary>
		/// <remarks>
		/// Transforms t if it contains one of the following QP structure:
		/// <ul>
		/// <li> NP (QP ...) (QP (CC and/or) ...)
		/// <li> QP (RB IN CD|DT ...)   well over, more than
		/// <li> QP (JJR IN CD|DT ...)  fewer than
		/// <li> QP (IN JJS CD|DT ...)  at least
		/// <li> QP (... CC ...)        between 5 and 10
		/// </ul>
		/// </remarks>
		/// <param name="t">a tree to be transformed</param>
		/// <returns>t transformed</returns>
		public virtual Tree QPtransform(Tree t)
		{
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(flattenNPoverQPTregex, flattenNPoverQPTsurgeon, t);
			if (!universalDependencies)
			{
				t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(multiwordXSTregex, multiwordXSTsurgeon, t);
			}
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(splitCCTregex, splitCCTsurgeon, t);
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(splitMoneyTregex, splitMoneyTsurgeon, t);
			return t;
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Trees.QPTreeTransformer transformer = new Edu.Stanford.Nlp.Trees.QPTreeTransformer();
			Treebank tb = new MemoryTreebank();
			Properties props = StringUtils.ArgsToProperties(args);
			string treeFileName = props.GetProperty("treeFile");
			if (treeFileName != null)
			{
				try
				{
					ITreeReader tr = new PennTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(treeFileName))), new LabeledScoredTreeFactory());
					Tree t;
					while ((t = tr.ReadTree()) != null)
					{
						tb.Add(t);
					}
				}
				catch (IOException e)
				{
					throw new Exception("File problem: " + e);
				}
			}
			foreach (Tree t_1 in tb)
			{
				System.Console.Out.WriteLine("Original tree");
				t_1.PennPrint();
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("Tree transformed");
				Tree tree = transformer.TransformTree(t_1);
				tree.PennPrint();
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("----------------------------");
			}
		}
	}
}
