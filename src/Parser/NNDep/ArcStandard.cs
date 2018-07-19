using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>
	/// Defines an arc-standard transition-based dependency parsing system
	/// (Nivre, 2004).
	/// </summary>
	/// <author>Danqi Chen</author>
	public class ArcStandard : ParsingSystem
	{
		private bool singleRoot = true;

		public ArcStandard(ITreebankLanguagePack tlp, IList<string> labels, bool verbose)
			: base(tlp, labels, MakeTransitions(labels), verbose)
		{
		}

		internal override bool IsTerminal(Configuration c)
		{
			return (c.GetStackSize() == 1 && c.GetBufferSize() == 0);
		}

		/// <summary>
		/// Generate all possible transitions which this parsing system can
		/// take for any given configuration.
		/// </summary>
		/// <returns>A List of the transitions</returns>
		private static IList<string> MakeTransitions(IList<string> labels)
		{
			IList<string> moves = new List<string>();
			// TODO store these as objects!
			foreach (string label in labels)
			{
				moves.Add("L(" + label + ')');
			}
			foreach (string label_1 in labels)
			{
				moves.Add("R(" + label_1 + ')');
			}
			moves.Add("S");
			return moves;
		}

		public override Configuration InitialConfiguration(ICoreMap s)
		{
			Configuration c = new Configuration(s);
			int length = s.Get(typeof(CoreAnnotations.TokensAnnotation)).Count;
			// For each token, add dummy elements to the configuration's tree
			// and add the words onto the buffer
			for (int i = 1; i <= length; ++i)
			{
				c.tree.Add(Config.Nonexist, Config.Unknown);
				c.buffer.Add(i);
			}
			// Put the ROOT node on the stack
			c.stack.Add(0);
			return c;
		}

		public override bool CanApply(Configuration c, string t)
		{
			if (t.StartsWith("L") || t.StartsWith("R"))
			{
				string label = Sharpen.Runtime.Substring(t, 2, t.Length - 1);
				int h = t.StartsWith("L") ? c.GetStack(0) : c.GetStack(1);
				if (h < 0)
				{
					return false;
				}
				if (h == 0 && !label.Equals(rootLabel))
				{
					return false;
				}
			}
			//if (h > 0 && label.equals(rootLabel)) return false;
			int nStack = c.GetStackSize();
			int nBuffer = c.GetBufferSize();
			if (t.StartsWith("L"))
			{
				return nStack > 2;
			}
			else
			{
				if (t.StartsWith("R"))
				{
					if (singleRoot)
					{
						return (nStack > 2) || (nStack == 2 && nBuffer == 0);
					}
					else
					{
						return nStack >= 2;
					}
				}
				else
				{
					return nBuffer > 0;
				}
			}
		}

		public override void Apply(Configuration c, string t)
		{
			int w1 = c.GetStack(1);
			int w2 = c.GetStack(0);
			if (t.StartsWith("L"))
			{
				c.AddArc(w2, w1, Sharpen.Runtime.Substring(t, 2, t.Length - 1));
				c.RemoveSecondTopStack();
			}
			else
			{
				if (t.StartsWith("R"))
				{
					c.AddArc(w1, w2, Sharpen.Runtime.Substring(t, 2, t.Length - 1));
					c.RemoveTopStack();
				}
				else
				{
					c.Shift();
				}
			}
		}

		// O(n) implementation
		public override string GetOracle(Configuration c, DependencyTree dTree)
		{
			int w1 = c.GetStack(1);
			int w2 = c.GetStack(0);
			if (w1 > 0 && dTree.GetHead(w1) == w2)
			{
				return "L(" + dTree.GetLabel(w1) + ')';
			}
			else
			{
				if (w1 >= 0 && dTree.GetHead(w2) == w1 && !c.HasOtherChild(w2, dTree))
				{
					return "R(" + dTree.GetLabel(w2) + ')';
				}
				else
				{
					return "S";
				}
			}
		}

		// NOTE: need to check the correctness again.
		private static bool CanReach(Configuration c, DependencyTree dTree)
		{
			int n = c.GetSentenceSize();
			for (int i = 1; i <= n; ++i)
			{
				if (c.GetHead(i) != Config.Nonexist && c.GetHead(i) != dTree.GetHead(i))
				{
					return false;
				}
			}
			bool[] inBuffer = new bool[n + 1];
			bool[] depInList = new bool[n + 1];
			int[] leftL = new int[n + 2];
			int[] rightL = new int[n + 2];
			for (int i_1 = 0; i_1 < c.GetBufferSize(); ++i_1)
			{
				inBuffer[c.buffer[i_1]] = true;
			}
			int nLeft = c.GetStackSize();
			for (int i_2 = 0; i_2 < nLeft; ++i_2)
			{
				int x = c.stack[i_2];
				leftL[nLeft - i_2] = x;
				if (x > 0)
				{
					depInList[dTree.GetHead(x)] = true;
				}
			}
			int nRight = 1;
			rightL[nRight] = leftL[1];
			for (int i_3 = 0; i_3 < c.GetBufferSize(); ++i_3)
			{
				// boolean inList = false;
				int x = c.buffer[i_3];
				if (!inBuffer[dTree.GetHead(x)] || depInList[x])
				{
					rightL[++nRight] = x;
					depInList[dTree.GetHead(x)] = true;
				}
			}
			int[][] g = new int[][] {  };
			for (int i_4 = 1; i_4 <= nLeft; ++i_4)
			{
				for (int j = 1; j <= nRight; ++j)
				{
					g[i_4][j] = -1;
				}
			}
			g[1][1] = leftL[1];
			for (int i_5 = 1; i_5 <= nLeft; ++i_5)
			{
				for (int j_1 = 1; j_1 <= nRight; ++j_1)
				{
					if (g[i_5][j_1] != -1)
					{
						int x = g[i_5][j_1];
						if (j_1 < nRight && dTree.GetHead(rightL[j_1 + 1]) == x)
						{
							g[i_5][j_1 + 1] = x;
						}
						if (j_1 < nRight && dTree.GetHead(x) == rightL[j_1 + 1])
						{
							g[i_5][j_1 + 1] = rightL[j_1 + 1];
						}
						if (i_5 < nLeft && dTree.GetHead(leftL[i_5 + 1]) == x)
						{
							g[i_5 + 1][j_1] = x;
						}
						if (i_5 < nLeft && dTree.GetHead(x) == leftL[i_5 + 1])
						{
							g[i_5 + 1][j_1] = leftL[i_5 + 1];
						}
					}
				}
			}
			return g[nLeft][nRight] != -1;
		}

		internal override bool IsOracle(Configuration c, string t, DependencyTree dTree)
		{
			if (!CanApply(c, t))
			{
				return false;
			}
			if (t.StartsWith("L") && !dTree.GetLabel(c.GetStack(1)).Equals(Sharpen.Runtime.Substring(t, 2, t.Length - 1)))
			{
				return false;
			}
			if (t.StartsWith("R") && !dTree.GetLabel(c.GetStack(0)).Equals(Sharpen.Runtime.Substring(t, 2, t.Length - 1)))
			{
				return false;
			}
			Configuration ct = new Configuration(c);
			Apply(ct, t);
			return CanReach(ct, dTree);
		}
	}
}
