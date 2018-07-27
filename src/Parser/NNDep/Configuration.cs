using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>Describe the current configuration of a parser (i.e., parser state).</summary>
	/// <remarks>
	/// Describe the current configuration of a parser (i.e., parser state).
	/// This class uses an indexing scheme where an index of zero refers to
	/// the ROOT node and actual word indices begin at one.
	/// </remarks>
	/// <author>Danqi Chen</author>
	public class Configuration
	{
		internal readonly IList<int> stack;

		internal readonly IList<int> buffer;

		internal readonly DependencyTree tree;

		internal readonly ICoreMap sentence;

		public Configuration(Edu.Stanford.Nlp.Parser.Nndep.Configuration config)
		{
			stack = new List<int>(config.stack);
			buffer = new List<int>(config.buffer);
			tree = new DependencyTree(config.tree);
			sentence = new CoreLabel(config.sentence);
		}

		public Configuration(ICoreMap sentence)
		{
			this.stack = new List<int>();
			this.buffer = new List<int>();
			this.tree = new DependencyTree();
			this.sentence = sentence;
		}

		public virtual bool Shift()
		{
			int k = GetBuffer(0);
			if (k == Config.Nonexist)
			{
				return false;
			}
			buffer.Remove(0);
			stack.Add(k);
			return true;
		}

		public virtual bool RemoveSecondTopStack()
		{
			int nStack = GetStackSize();
			if (nStack < 2)
			{
				return false;
			}
			stack.Remove(nStack - 2);
			return true;
		}

		public virtual bool RemoveTopStack()
		{
			int nStack = GetStackSize();
			if (nStack < 1)
			{
				return false;
			}
			stack.Remove(nStack - 1);
			return true;
		}

		public virtual int GetStackSize()
		{
			return stack.Count;
		}

		public virtual int GetBufferSize()
		{
			return buffer.Count;
		}

		public virtual int GetSentenceSize()
		{
			return GetCoreLabels().Count;
		}

		/// <param name="k">
		/// Word index (zero = root node; actual word indexing
		/// begins at 1)
		/// </param>
		public virtual int GetHead(int k)
		{
			return tree.GetHead(k);
		}

		/// <param name="k">
		/// Word index (zero = root node; actual word indexing
		/// begins at 1)
		/// </param>
		public virtual string GetLabel(int k)
		{
			return tree.GetLabel(k);
		}

		/// <summary>Get the sentence index of the kth word on the stack.</summary>
		/// <returns>
		/// Sentence index or
		/// <see cref="Config.Nonexist"/>
		/// if stack doesn't
		/// have an element at this index
		/// </returns>
		public virtual int GetStack(int k)
		{
			int nStack = GetStackSize();
			return (k >= 0 && k < nStack) ? stack[nStack - 1 - k] : Config.Nonexist;
		}

		/// <summary>Get the sentence index of the kth word on the buffer.</summary>
		/// <returns>
		/// Sentence index or
		/// <see cref="Config.Nonexist"/>
		/// if stack doesn't
		/// have an element at this index
		/// </returns>
		public virtual int GetBuffer(int k)
		{
			return (k >= 0 && k < GetBufferSize()) ? buffer[k] : Config.Nonexist;
		}

		public virtual IList<CoreLabel> GetCoreLabels()
		{
			return sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		/// <param name="k">
		/// Word index (zero = root node; actual word indexing
		/// begins at 1)
		/// </param>
		public virtual string GetWord(int k)
		{
			if (k == 0)
			{
				return Config.Root;
			}
			else
			{
				k--;
			}
			IList<CoreLabel> lbls = GetCoreLabels();
			return k < 0 || k >= lbls.Count ? Config.Null : lbls[k].Word();
		}

		/// <param name="k">
		/// Word index (zero = root node; actual word indexing
		/// begins at 1)
		/// </param>
		public virtual string GetPOS(int k)
		{
			if (k == 0)
			{
				return Config.Root;
			}
			else
			{
				k--;
			}
			IList<CoreLabel> lbls = GetCoreLabels();
			return k < 0 || k >= lbls.Count ? Config.Null : lbls[k].Tag();
		}

		/// <param name="h">
		/// Word index of governor (zero = root node; actual word
		/// indexing begins at 1)
		/// </param>
		/// <param name="t">
		/// Word index of dependent (zero = root node; actual word
		/// indexing begins at 1)
		/// </param>
		/// <param name="l">Arc label</param>
		public virtual void AddArc(int h, int t, string l)
		{
			tree.Set(t, h, l);
		}

		public virtual int GetLeftChild(int k, int cnt)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Nonexist;
			}
			int c = 0;
			for (int i = 1; i < k; ++i)
			{
				if (tree.GetHead(i) == k)
				{
					if ((++c) == cnt)
					{
						return i;
					}
				}
			}
			return Config.Nonexist;
		}

		public virtual int GetLeftChild(int k)
		{
			return GetLeftChild(k, 1);
		}

		public virtual int GetRightChild(int k, int cnt)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Nonexist;
			}
			int c = 0;
			for (int i = tree.n; i > k; --i)
			{
				if (tree.GetHead(i) == k)
				{
					if ((++c) == cnt)
					{
						return i;
					}
				}
			}
			return Config.Nonexist;
		}

		public virtual int GetRightChild(int k)
		{
			return GetRightChild(k, 1);
		}

		public virtual bool HasOtherChild(int k, DependencyTree goldTree)
		{
			for (int i = 1; i <= tree.n; ++i)
			{
				if (goldTree.GetHead(i) == k && tree.GetHead(i) != k)
				{
					return true;
				}
			}
			return false;
		}

		public virtual int GetLeftValency(int k)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Nonexist;
			}
			int cnt = 0;
			for (int i = 1; i < k; ++i)
			{
				if (tree.GetHead(i) == k)
				{
					++cnt;
				}
			}
			return cnt;
		}

		public virtual int GetRightValency(int k)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Nonexist;
			}
			int cnt = 0;
			for (int i = k + 1; i <= tree.n; ++i)
			{
				if (tree.GetHead(i) == k)
				{
					++cnt;
				}
			}
			return cnt;
		}

		public virtual string GetLeftLabelSet(int k)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Null;
			}
			HashSet<string> labelSet = new HashSet<string>();
			for (int i = 1; i < k; ++i)
			{
				if (tree.GetHead(i) == k)
				{
					labelSet.Add(tree.GetLabel(i));
				}
			}
			return MakeLabelSetString(labelSet);
		}

		public virtual string GetRightLabelSet(int k)
		{
			if (k < 0 || k > tree.n)
			{
				return Config.Null;
			}
			HashSet<string> labelSet = new HashSet<string>();
			for (int i = k + 1; i <= tree.n; ++i)
			{
				if (tree.GetHead(i) == k)
				{
					labelSet.Add(tree.GetLabel(i));
				}
			}
			return MakeLabelSetString(labelSet);
		}

		private static string MakeLabelSetString(ICollection<string> labelSet)
		{
			IList<string> ls = new List<string>(labelSet);
			ls.Sort();
			StringBuilder s = new StringBuilder(128);
			s.Append("[S]");
			foreach (string l in ls)
			{
				s.Append('/').Append(l);
			}
			return s.ToString();
		}

		//returns a string that concatenates all elements on the stack and buffer, and head / label.
		public virtual string GetStr()
		{
			StringBuilder s = new StringBuilder(128);
			s.Append("[S]");
			for (int i = 0; i < GetStackSize(); ++i)
			{
				if (i > 0)
				{
					s.Append(',');
				}
				s.Append(stack[i]);
			}
			s.Append("[B]");
			for (int i_1 = 0; i_1 < GetBufferSize(); ++i_1)
			{
				if (i_1 > 0)
				{
					s.Append(',');
				}
				s.Append(buffer[i_1]);
			}
			s.Append("[H]");
			for (int i_2 = 1; i_2 <= tree.n; ++i_2)
			{
				if (i_2 > 1)
				{
					s.Append(',');
				}
				s.Append(GetHead(i_2)).Append('(').Append(GetLabel(i_2)).Append(')');
			}
			return s.ToString();
		}
	}
}
