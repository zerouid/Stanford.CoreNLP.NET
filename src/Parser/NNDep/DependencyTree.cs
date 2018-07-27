using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>
	/// Represents a partial or complete dependency parse of a sentence, and
	/// provides convenience methods for analyzing the parse.
	/// </summary>
	/// <author>Danqi Chen</author>
	internal class DependencyTree
	{
		internal int n;

		internal readonly IList<int> head;

		internal readonly IList<string> label;

		private int counter;

		public DependencyTree()
		{
			n = 0;
			head = new List<int>();
			head.Add(Config.Nonexist);
			label = new List<string>();
			label.Add(Config.Unknown);
		}

		public DependencyTree(Edu.Stanford.Nlp.Parser.Nndep.DependencyTree tree)
		{
			n = tree.n;
			head = new List<int>(tree.head);
			label = new List<string>(tree.label);
		}

		/// <summary>Add the next token to the parse.</summary>
		/// <param name="h">Head of the next token</param>
		/// <param name="l">Dependency relation label between this node and its head</param>
		public virtual void Add(int h, string l)
		{
			++n;
			head.Add(h);
			label.Add(l);
		}

		/// <summary>
		/// Establish a labeled dependency relation between the two given
		/// nodes.
		/// </summary>
		/// <param name="k">Index of the dependent node</param>
		/// <param name="h">Index of the head node</param>
		/// <param name="l">Label of the dependency relation</param>
		public virtual void Set(int k, int h, string l)
		{
			head.Set(k, h);
			label.Set(k, l);
		}

		public virtual int GetHead(int k)
		{
			if (k <= 0 || k > n)
			{
				return Config.Nonexist;
			}
			else
			{
				return head[k];
			}
		}

		public virtual string GetLabel(int k)
		{
			if (k <= 0 || k > n)
			{
				return Config.Null;
			}
			else
			{
				return label[k];
			}
		}

		/// <summary>
		/// Get the index of the node which is the root of the parse (i.e.,
		/// that node which has the ROOT node as its head).
		/// </summary>
		public virtual int GetRoot()
		{
			for (int k = 1; k <= n; ++k)
			{
				if (GetHead(k) == 0)
				{
					return k;
				}
			}
			return 0;
		}

		/// <summary>Check if this parse has only one root.</summary>
		public virtual bool IsSingleRoot()
		{
			int roots = 0;
			for (int k = 1; k <= n; ++k)
			{
				if (GetHead(k) == 0)
				{
					roots = roots + 1;
				}
			}
			return (roots == 1);
		}

		// check if the tree is legal, O(n)
		public virtual bool IsTree()
		{
			IList<int> h = new List<int>();
			h.Add(-1);
			for (int i = 1; i <= n; ++i)
			{
				if (GetHead(i) < 0 || GetHead(i) > n)
				{
					return false;
				}
				h.Add(-1);
			}
			for (int i_1 = 1; i_1 <= n; ++i_1)
			{
				int k = i_1;
				while (k > 0)
				{
					if (h[k] >= 0 && h[k] < i_1)
					{
						break;
					}
					if (h[k] == i_1)
					{
						return false;
					}
					h.Set(k, i_1);
					k = GetHead(k);
				}
			}
			return true;
		}

		// check if the tree is projective, O(n^2)
		public virtual bool IsProjective()
		{
			if (!IsTree())
			{
				return false;
			}
			counter = -1;
			return VisitTree(0);
		}

		// Inner recursive function for checking projectivity of tree
		private bool VisitTree(int w)
		{
			for (int i = 1; i < w; ++i)
			{
				if (GetHead(i) == w && VisitTree(i) == false)
				{
					return false;
				}
			}
			counter = counter + 1;
			if (w != counter)
			{
				return false;
			}
			for (int i_1 = w + 1; i_1 <= n; ++i_1)
			{
				if (GetHead(i_1) == w && VisitTree(i_1) == false)
				{
					return false;
				}
			}
			return true;
		}

		// TODO properly override equals, hashCode?
		public virtual bool Equal(Edu.Stanford.Nlp.Parser.Nndep.DependencyTree t)
		{
			if (t.n != n)
			{
				return false;
			}
			for (int i = 1; i <= n; ++i)
			{
				if (GetHead(i) != t.GetHead(i))
				{
					return false;
				}
				if (!GetLabel(i).Equals(t.GetLabel(i)))
				{
					return false;
				}
			}
			return true;
		}

		public virtual void Print()
		{
			for (int i = 1; i <= n; ++i)
			{
				System.Console.Out.WriteLine(i + " " + GetHead(i) + " " + GetLabel(i));
			}
			System.Console.Out.WriteLine();
		}
	}
}
