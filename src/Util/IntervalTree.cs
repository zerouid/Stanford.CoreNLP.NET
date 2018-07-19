using System;
using System.Collections.Generic;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// An interval tree maintains a tree so that all intervals to the left start
	/// before current interval and all intervals to the right start after.
	/// </summary>
	/// <author>Angel Chang</author>
	public class IntervalTree<E, T> : AbstractCollection<T>
		where E : IComparable<E>
		where T : IHasInterval<E>
	{
		private const double defaultAlpha = 0.65;

		private const bool debug = false;

		private IntervalTree.TreeNode<E, T> root = new IntervalTree.TreeNode<E, T>();

		public class TreeNode<E, T>
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			internal T value;

			internal E maxEnd;

			internal int size;

			internal IntervalTree.TreeNode<E, T> left;

			internal IntervalTree.TreeNode<E, T> right;

			internal IntervalTree.TreeNode<E, T> parent;

			// How balanced we want this tree (between 0.5 and 1.0)
			// Tree node
			// Maximum end in this subtree
			// Parent for convenience
			public virtual bool IsEmpty()
			{
				return value == null;
			}

			public virtual void Clear()
			{
				value = null;
				maxEnd = null;
				size = 0;
				left = null;
				right = null;
			}
			//      parent = null;
		}

		public override bool IsEmpty()
		{
			return root.IsEmpty();
		}

		public override void Clear()
		{
			root.Clear();
		}

		public override string ToString()
		{
			return "Size: " + root.size;
		}

		public override bool Add(T target)
		{
			return Add(root, target, defaultAlpha);
		}

		public virtual bool Add(IntervalTree.TreeNode<E, T> node, T target)
		{
			return Add(node, target, defaultAlpha);
		}

		// Add node to tree - attempting to maintain alpha balance
		public virtual bool Add(IntervalTree.TreeNode<E, T> node, T target, double alpha)
		{
			if (target == null)
			{
				return false;
			}
			IntervalTree.TreeNode<E, T> n = node;
			int depth = 0;
			int thresholdDepth = (node.size > 10) ? ((int)(-Math.Log(node.size) / Math.Log(alpha) + 1)) : 10;
			while (n != null)
			{
				if (n.value == null)
				{
					n.value = target;
					n.maxEnd = target.GetInterval().GetEnd();
					n.size = 1;
					if (depth > thresholdDepth)
					{
						// Do rebalancing
						IntervalTree.TreeNode<E, T> p = n.parent;
						while (p != null)
						{
							if (p.size > 10 && !IsAlphaBalanced(p, alpha))
							{
								IntervalTree.TreeNode<E, T> newParent = Balance(p);
								if (p == root)
								{
									root = newParent;
								}
								break;
							}
							p = p.parent;
						}
					}
					return true;
				}
				else
				{
					depth++;
					n.maxEnd = Interval.Max(n.maxEnd, target.GetInterval().GetEnd());
					n.size++;
					if (target.GetInterval().CompareTo(n.value.GetInterval()) <= 0)
					{
						// Should go on left
						if (n.left == null)
						{
							n.left = new IntervalTree.TreeNode<E, T>();
							n.left.parent = n;
						}
						n = n.left;
					}
					else
					{
						// Should go on right
						if (n.right == null)
						{
							n.right = new IntervalTree.TreeNode<E, T>();
							n.right.parent = n;
						}
						n = n.right;
					}
				}
			}
			return false;
		}

		public override int Count
		{
			get
			{
				return root.size;
			}
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return new IntervalTree.TreeNodeIterator<E, T>(root);
		}

		private class TreeNodeIterator<E, T> : AbstractIterator<T>
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			internal IntervalTree.TreeNode<E, T> node;

			internal IEnumerator<T> curIter;

			internal int stage = -1;

			internal T next;

			public TreeNodeIterator(IntervalTree.TreeNode<E, T> node)
			{
				this.node = node;
				if (node.IsEmpty())
				{
					stage = 3;
				}
			}

			public override bool MoveNext()
			{
				if (next == null)
				{
					next = GetNext();
				}
				return next != null;
			}

			public override T Current
			{
				get
				{
					if (MoveNext())
					{
						T x = next;
						next = GetNext();
						return x;
					}
					else
					{
						throw new NoSuchElementException();
					}
				}
			}

			private T GetNext()
			{
				// TODO: Do more efficient traversal down the tree
				if (stage > 2)
				{
					return null;
				}
				while (curIter == null || !curIter.MoveNext())
				{
					stage++;
					switch (stage)
					{
						case 0:
						{
							curIter = (node.left != null) ? new IntervalTree.TreeNodeIterator<E, T>(node.left) : null;
							break;
						}

						case 1:
						{
							curIter = null;
							return node.value;
						}

						case 2:
						{
							curIter = (node.right != null) ? new IntervalTree.TreeNodeIterator<E, T>(node.right) : null;
							break;
						}

						default:
						{
							return null;
						}
					}
				}
				if (curIter != null && curIter.MoveNext())
				{
					return curIter.Current;
				}
				else
				{
					return null;
				}
			}
		}

		public override bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			bool modified = false;
			foreach (object t in c)
			{
				if (Remove(t))
				{
					modified = true;
				}
			}
			return modified;
		}

		public override bool RetainAll<_T0>(ICollection<_T0> c)
		{
			throw new NotSupportedException("retainAll not implemented");
		}

		public override bool Contains(object o)
		{
			try
			{
				return Contains((T)o);
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		public override bool Remove(object o)
		{
			try
			{
				return Remove((T)o);
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		public virtual bool Remove(T target)
		{
			return Remove(root, target);
		}

		public virtual bool Remove(IntervalTree.TreeNode<E, T> node, T target)
		{
			if (target == null)
			{
				return false;
			}
			if (node.value == null)
			{
				return false;
			}
			if (target.Equals(node.value))
			{
				int leftSize = (node.left != null) ? node.left.size : 0;
				int rightSize = (node.right != null) ? node.right.size : 0;
				if (leftSize == 0)
				{
					if (rightSize == 0)
					{
						node.Clear();
					}
					else
					{
						node.value = node.right.value;
						node.size = node.right.size;
						node.maxEnd = node.right.maxEnd;
						node.left = node.right.left;
						node.right = node.right.right;
						if (node.left != null)
						{
							node.left.parent = node;
						}
						if (node.right != null)
						{
							node.right.parent = node;
						}
					}
				}
				else
				{
					if (rightSize == 0)
					{
						node.value = node.left.value;
						node.size = node.left.size;
						node.maxEnd = node.left.maxEnd;
						node.left = node.left.left;
						node.right = node.left.right;
						if (node.left != null)
						{
							node.left.parent = node;
						}
						if (node.right != null)
						{
							node.right.parent = node;
						}
					}
					else
					{
						// Rotate left up
						node.value = node.left.value;
						node.size--;
						node.maxEnd = Interval.Max(node.left.maxEnd, node.right.maxEnd);
						IntervalTree.TreeNode<E, T> origRight = node.right;
						node.right = node.left.right;
						node.left = node.left.left;
						if (node.left != null)
						{
							node.left.parent = node;
						}
						if (node.right != null)
						{
							node.right.parent = node;
						}
						// Attach origRight somewhere...
						IntervalTree.TreeNode<E, T> rightmost = GetRightmostNode(node);
						rightmost.right = origRight;
						if (rightmost.right != null)
						{
							rightmost.right.parent = rightmost;
							// adjust maxEnd and sizes on the right
							AdjustUpwards(rightmost.right, node);
						}
					}
				}
				return true;
			}
			else
			{
				if (target.GetInterval().CompareTo(node.value.GetInterval()) <= 0)
				{
					// Should go on left
					if (node.left == null)
					{
						return false;
					}
					bool res = Remove(node.left, target);
					if (res)
					{
						node.maxEnd = Interval.Max(node.maxEnd, node.left.maxEnd);
						node.size--;
					}
					return res;
				}
				else
				{
					// Should go on right
					if (node.right == null)
					{
						return false;
					}
					bool res = Remove(node.right, target);
					if (res)
					{
						node.maxEnd = Interval.Max(node.maxEnd, node.right.maxEnd);
						node.size--;
					}
					return res;
				}
			}
		}

		private void AdjustUpwards(IntervalTree.TreeNode<E, T> node)
		{
			AdjustUpwards(node, null);
		}

		// Adjust upwards starting at this node until stopAt
		private void AdjustUpwards(IntervalTree.TreeNode<E, T> node, IntervalTree.TreeNode<E, T> stopAt)
		{
			IntervalTree.TreeNode<E, T> n = node;
			while (n != null && n != stopAt)
			{
				int leftSize = (n.left != null) ? n.left.size : 0;
				int rightSize = (n.right != null) ? n.right.size : 0;
				n.maxEnd = n.value.GetInterval().GetEnd();
				if (n.left != null)
				{
					n.maxEnd = Interval.Max(n.maxEnd, n.left.maxEnd);
				}
				if (n.right != null)
				{
					n.maxEnd = Interval.Max(n.maxEnd, n.right.maxEnd);
				}
				n.size = leftSize + 1 + rightSize;
				if (n == n.parent)
				{
					throw new InvalidOperationException("node is same as parent!!!");
				}
				n = n.parent;
			}
		}

		private void Adjust(IntervalTree.TreeNode<E, T> node)
		{
			AdjustUpwards(node, node.parent);
		}

		public virtual void Check()
		{
			Check(root);
		}

		public virtual void Check(IntervalTree.TreeNode<E, T> treeNode)
		{
			Stack<IntervalTree.TreeNode<E, T>> todo = new Stack<IntervalTree.TreeNode<E, T>>();
			todo.Add(treeNode);
			while (!todo.IsEmpty())
			{
				IntervalTree.TreeNode<E, T> node = todo.Pop();
				if (node == node.parent)
				{
					throw new InvalidOperationException("node is same as parent!!!");
				}
				if (node.IsEmpty())
				{
					if (node.left != null)
					{
						throw new InvalidOperationException("Empty node shouldn't have left branch");
					}
					if (node.right != null)
					{
						throw new InvalidOperationException("Empty node shouldn't have right branch");
					}
					continue;
				}
				int leftSize = (node.left != null) ? node.left.size : 0;
				int rightSize = (node.right != null) ? node.right.size : 0;
				E leftMax = (node.left != null) ? node.left.maxEnd : null;
				E rightMax = (node.right != null) ? node.right.maxEnd : null;
				E maxEnd = node.value.GetInterval().GetEnd();
				if (leftMax != null && leftMax.CompareTo(maxEnd) > 0)
				{
					maxEnd = leftMax;
				}
				if (rightMax != null && rightMax.CompareTo(maxEnd) > 0)
				{
					maxEnd = rightMax;
				}
				if (!maxEnd.Equals(node.maxEnd))
				{
					throw new InvalidOperationException("max end is not as expected!!!");
				}
				if (node.size != leftSize + rightSize + 1)
				{
					throw new InvalidOperationException("node size is not one plus the sum of left and right!!!");
				}
				if (node.left != null)
				{
					if (node.left.parent != node)
					{
						throw new InvalidOperationException("node left parent is not same as node!!!");
					}
				}
				if (node.right != null)
				{
					if (node.right.parent != node)
					{
						throw new InvalidOperationException("node right parent is not same as node!!!");
					}
				}
				if (node.parent != null)
				{
					// Go up parent and make sure we are on correct side
					IntervalTree.TreeNode<E, T> n = node;
					while (n != null && n.parent != null)
					{
						// Check we are either right or left
						if (n == n.parent.left)
						{
							// Check that node is less than the parent
							if (node.value != null)
							{
								if (node.value.GetInterval().CompareTo(n.parent.value.GetInterval()) > 0)
								{
									throw new InvalidOperationException("node is not on the correct side!!!");
								}
							}
						}
						else
						{
							if (n == n.parent.right)
							{
								// Check that node is greater than the parent
								if (node.value.GetInterval().CompareTo(n.parent.value.GetInterval()) <= 0)
								{
									throw new InvalidOperationException("node is not on the correct side!!!");
								}
							}
							else
							{
								throw new InvalidOperationException("node is not parent's left or right child!!!");
							}
						}
						n = n.parent;
					}
				}
				if (node.left != null)
				{
					todo.Add(node.left);
				}
				if (node.right != null)
				{
					todo.Add(node.right);
				}
			}
		}

		public virtual bool IsAlphaBalanced(IntervalTree.TreeNode<E, T> node, double alpha)
		{
			int leftSize = (node.left != null) ? node.left.size : 0;
			int rightSize = (node.right != null) ? node.right.size : 0;
			int threshold = (int)(alpha * node.size) + 1;
			return (leftSize <= threshold) && (rightSize <= threshold);
		}

		public virtual void Balance()
		{
			root = Balance(root);
		}

		// Balances this tree
		public virtual IntervalTree.TreeNode<E, T> Balance(IntervalTree.TreeNode<E, T> node)
		{
			Stack<IntervalTree.TreeNode<E, T>> todo = new Stack<IntervalTree.TreeNode<E, T>>();
			todo.Add(node);
			IntervalTree.TreeNode<E, T> newRoot = null;
			while (!todo.IsEmpty())
			{
				IntervalTree.TreeNode<E, T> n = todo.Pop();
				// Balance tree between this node
				// Select median nodes and try to balance the tree
				int medianAt = n.size / 2;
				IntervalTree.TreeNode<E, T> median = GetNode(n, medianAt);
				// Okay, this is going to be our root
				if (median != null && median != n)
				{
					// Yes, there is indeed something to be done
					RotateUp(median, n);
				}
				if (newRoot == null)
				{
					newRoot = median;
				}
				if (median.left != null)
				{
					todo.Push(median.left);
				}
				if (median.right != null)
				{
					todo.Push(median.right);
				}
			}
			if (newRoot == null)
			{
				return node;
			}
			else
			{
				return newRoot;
			}
		}

		// Moves this node up the tree until it replaces the target node
		public virtual void RotateUp(IntervalTree.TreeNode<E, T> node, IntervalTree.TreeNode<E, T> target)
		{
			IntervalTree.TreeNode<E, T> n = node;
			bool done = false;
			while (n != null && n.parent != null && !done)
			{
				// Check if we are the left or right child
				done = (n.parent == target);
				if (n == n.parent.left)
				{
					n = RightRotate(n.parent);
				}
				else
				{
					if (n == n.parent.right)
					{
						n = LeftRotate(n.parent);
					}
					else
					{
						throw new InvalidOperationException("Not on parent's left or right branches.");
					}
				}
			}
		}

		// Moves this node to the right and the left child up and returns the new root
		public virtual IntervalTree.TreeNode<E, T> RightRotate(IntervalTree.TreeNode<E, T> oldRoot)
		{
			if (oldRoot == null || oldRoot.IsEmpty() || oldRoot.left == null)
			{
				return oldRoot;
			}
			IntervalTree.TreeNode<E, T> oldLeftRight = oldRoot.left.right;
			IntervalTree.TreeNode<E, T> newRoot = oldRoot.left;
			newRoot.right = oldRoot;
			oldRoot.left = oldLeftRight;
			// Adjust parents and such
			newRoot.parent = oldRoot.parent;
			newRoot.maxEnd = oldRoot.maxEnd;
			newRoot.size = oldRoot.size;
			if (newRoot.parent != null)
			{
				if (newRoot.parent.left == oldRoot)
				{
					newRoot.parent.left = newRoot;
				}
				else
				{
					if (newRoot.parent.right == oldRoot)
					{
						newRoot.parent.right = newRoot;
					}
					else
					{
						throw new InvalidOperationException("Old root not a child of it's parent");
					}
				}
			}
			oldRoot.parent = newRoot;
			if (oldLeftRight != null)
			{
				oldLeftRight.parent = oldRoot;
			}
			Adjust(oldRoot);
			return newRoot;
		}

		// Moves this node to the left and the right child up and returns the new root
		public virtual IntervalTree.TreeNode<E, T> LeftRotate(IntervalTree.TreeNode<E, T> oldRoot)
		{
			if (oldRoot == null || oldRoot.IsEmpty() || oldRoot.right == null)
			{
				return oldRoot;
			}
			IntervalTree.TreeNode<E, T> oldRightLeft = oldRoot.right.left;
			IntervalTree.TreeNode<E, T> newRoot = oldRoot.right;
			newRoot.left = oldRoot;
			oldRoot.right = oldRightLeft;
			// Adjust parents and such
			newRoot.parent = oldRoot.parent;
			newRoot.maxEnd = oldRoot.maxEnd;
			newRoot.size = oldRoot.size;
			if (newRoot.parent != null)
			{
				if (newRoot.parent.left == oldRoot)
				{
					newRoot.parent.left = newRoot;
				}
				else
				{
					if (newRoot.parent.right == oldRoot)
					{
						newRoot.parent.right = newRoot;
					}
					else
					{
						throw new InvalidOperationException("Old root not a child of it's parent");
					}
				}
			}
			oldRoot.parent = newRoot;
			if (oldRightLeft != null)
			{
				oldRightLeft.parent = oldRoot;
			}
			Adjust(oldRoot);
			return newRoot;
		}

		public virtual int Height()
		{
			return Height(root);
		}

		public virtual int Height(IntervalTree.TreeNode<E, T> node)
		{
			if (node.value == null)
			{
				return 0;
			}
			int lh = (node.left != null) ? Height(node.left) : 0;
			int rh = (node.right != null) ? Height(node.right) : 0;
			return Math.Max(lh, rh) + 1;
		}

		public virtual IntervalTree.TreeNode<E, T> GetLeftmostNode(IntervalTree.TreeNode<E, T> node)
		{
			IntervalTree.TreeNode<E, T> n = node;
			while (n.left != null)
			{
				n = n.left;
			}
			return n;
		}

		public virtual IntervalTree.TreeNode<E, T> GetRightmostNode(IntervalTree.TreeNode<E, T> node)
		{
			IntervalTree.TreeNode<E, T> n = node;
			while (n.right != null)
			{
				n = n.right;
			}
			return n;
		}

		// Returns ith node
		public virtual IntervalTree.TreeNode<E, T> GetNode(IntervalTree.TreeNode<E, T> node, int nodeIndex)
		{
			int i = nodeIndex;
			IntervalTree.TreeNode<E, T> n = node;
			while (n != null)
			{
				if (i < 0 || i >= n.size)
				{
					return null;
				}
				int leftSize = (n.left != null) ? n.left.size : 0;
				if (i == leftSize)
				{
					return n;
				}
				else
				{
					if (i > leftSize)
					{
						// Look for in right side of tree
						n = n.right;
						i = i - leftSize - 1;
					}
					else
					{
						n = n.left;
					}
				}
			}
			return null;
		}

		public virtual bool AddNonOverlapping(T target)
		{
			if (Overlaps(target))
			{
				return false;
			}
			Add(target);
			return true;
		}

		public virtual bool AddNonNested(T target)
		{
			if (ContainsInterval(target, false))
			{
				return false;
			}
			Add(target);
			return true;
		}

		public virtual bool Overlaps(T target)
		{
			return Overlaps(root, target.GetInterval());
		}

		public virtual IList<T> GetOverlapping(T target)
		{
			return GetOverlapping(root, target.GetInterval());
		}

		public static IList<T> GetOverlapping<E, T>(IntervalTree.TreeNode<E, T> n, E p)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			IList<T> overlapping = new List<T>();
			GetOverlapping(n, p, overlapping);
			return overlapping;
		}

		public static IList<T> GetOverlapping<E, T>(IntervalTree.TreeNode<E, T> n, Interval<E> target)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			IList<T> overlapping = new List<T>();
			GetOverlapping(n, target, overlapping);
			return overlapping;
		}

		// Search for all intervals which contain p, starting with the
		// node "n" and adding matching intervals to the list "result"
		public static void GetOverlapping<E, T>(IntervalTree.TreeNode<E, T> n, E p, IList<T> result)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			GetOverlapping(n, Interval.ToInterval(p, p), result);
		}

		public static void GetOverlapping<E, T>(IntervalTree.TreeNode<E, T> node, Interval<E> target, IList<T> result)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			IQueue<IntervalTree.TreeNode<E, T>> todo = new LinkedList<IntervalTree.TreeNode<E, T>>();
			todo.Add(node);
			while (!todo.IsEmpty())
			{
				IntervalTree.TreeNode<E, T> n = todo.Poll();
				// Don't search nodes that don't exist
				if (n == null || n.IsEmpty())
				{
					continue;
				}
				// If target is to the right of the rightmost point of any interval
				// in this node and all children, there won't be any matches.
				if (target.first.CompareTo(n.maxEnd) > 0)
				{
					continue;
				}
				// Search left children
				if (n.left != null)
				{
					todo.Add(n.left);
				}
				// Check this node
				if (n.value.GetInterval().Overlaps(target))
				{
					result.Add(n.value);
				}
				// If target is to the left of the start of this interval,
				// then it can't be in any child to the right.
				if (target.second.CompareTo(n.value.GetInterval().First()) < 0)
				{
					continue;
				}
				// Otherwise, search right children
				if (n.right != null)
				{
					todo.Add(n.right);
				}
			}
		}

		public static bool Overlaps<E, T>(IntervalTree.TreeNode<E, T> n, E p)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			return Overlaps(n, Interval.ToInterval(p, p));
		}

		public static bool Overlaps<E, T>(IntervalTree.TreeNode<E, T> node, Interval<E> target)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			Stack<IntervalTree.TreeNode<E, T>> todo = new Stack<IntervalTree.TreeNode<E, T>>();
			todo.Push(node);
			while (!todo.IsEmpty())
			{
				IntervalTree.TreeNode<E, T> n = todo.Pop();
				// Don't search nodes that don't exist
				if (n == null || n.IsEmpty())
				{
					continue;
				}
				// If target is to the right of the rightmost point of any interval
				// in this node and all children, there won't be any matches.
				if (target.first.CompareTo(n.maxEnd) > 0)
				{
					continue;
				}
				// Check this node
				if (n.value.GetInterval().Overlaps(target))
				{
					return true;
				}
				// Search left children
				if (n.left != null)
				{
					todo.Add(n.left);
				}
				// If target is to the left of the start of this interval,
				// then it can't be in any child to the right.
				if (target.second.CompareTo(n.value.GetInterval().First()) < 0)
				{
					continue;
				}
				if (n.right != null)
				{
					todo.Add(n.right);
				}
			}
			return false;
		}

		public virtual bool Contains(T target)
		{
			return ContainsValue(this, target);
		}

		public virtual bool ContainsInterval(T target, bool exact)
		{
			return ContainsInterval(this, target.GetInterval(), exact);
		}

		public static bool ContainsInterval<E, T>(IntervalTree<E, T> n, E p, bool exact)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			return ContainsInterval(n, Interval.ToInterval(p, p), exact);
		}

		public static bool ContainsInterval<E, T>(IntervalTree<E, T> node, Interval<E> target, bool exact)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			IPredicate<T> containsTargetFunction = new IntervalTree.ContainsIntervalFunction(target, exact);
			return Contains(node, target.GetInterval(), containsTargetFunction);
		}

		public static bool ContainsValue<E, T>(IntervalTree<E, T> node, T target)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			IPredicate<T> containsTargetFunction = new IntervalTree.ContainsValueFunction(target);
			return Contains(node, target.GetInterval(), containsTargetFunction);
		}

		private class ContainsValueFunction<E, T> : IPredicate<T>
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			private T target;

			public ContainsValueFunction(T target)
			{
				this.target = target;
			}

			public virtual bool Test(T @in)
			{
				return @in.Equals(target);
			}
		}

		private class ContainsIntervalFunction<E, T> : IPredicate<T>
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			private Interval<E> target;

			private bool exact;

			public ContainsIntervalFunction(Interval<E> target, bool exact)
			{
				this.target = target;
				this.exact = exact;
			}

			public virtual bool Test(T @in)
			{
				if (exact)
				{
					return @in.GetInterval().Equals(target);
				}
				else
				{
					return @in.GetInterval().Contains(target);
				}
			}
		}

		private static bool Contains<E, T>(IntervalTree<E, T> tree, Interval<E> target, IPredicate<T> containsTargetFunction)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			return Contains(tree.root, target, containsTargetFunction);
		}

		private static bool Contains<E, T>(IntervalTree.TreeNode<E, T> node, Interval<E> target, IPredicate<T> containsTargetFunction)
			where E : IComparable<E>
			where T : IHasInterval<E>
		{
			Stack<IntervalTree.TreeNode<E, T>> todo = new Stack<IntervalTree.TreeNode<E, T>>();
			todo.Push(node);
			// Don't search nodes that don't exist
			while (!todo.IsEmpty())
			{
				IntervalTree.TreeNode<E, T> n = todo.Pop();
				// Don't search nodes that don't exist
				if (n == null || n.IsEmpty())
				{
					continue;
				}
				// If target is to the right of the rightmost point of any interval
				// in this node and all children, there won't be any matches.
				if (target.first.CompareTo(n.maxEnd) > 0)
				{
					continue;
				}
				// Check this node
				if (containsTargetFunction.Test(n.value))
				{
					return true;
				}
				if (n.left != null)
				{
					todo.Push(n.left);
				}
				// If target is to the left of the start of this interval, then no need to search right
				if (target.second.CompareTo(n.value.GetInterval().First()) <= 0)
				{
					continue;
				}
				// Need to check right children
				if (n.right != null)
				{
					todo.Push(n.right);
				}
			}
			return false;
		}

		public static IList<T> GetNonOverlapping<T, E, _T2, _T3>(IList<_T2> items, IFunction<_T3> toIntervalFunc)
			where E : IComparable<E>
			where _T2 : T
		{
			IList<T> nonOverlapping = new List<T>();
			IntervalTree<E, Interval<E>> intervals = new IntervalTree<E, Interval<E>>();
			foreach (T item in items)
			{
				Interval<E> i = toIntervalFunc.Apply(item);
				bool addOk = intervals.AddNonOverlapping(i);
				if (addOk)
				{
					nonOverlapping.Add(item);
				}
			}
			return nonOverlapping;
		}

		public static IList<T> GetNonOverlapping<T, E, _T2, _T3, _T4>(IList<_T2> items, IFunction<_T3> toIntervalFunc, IComparator<_T4> compareFunc)
			where E : IComparable<E>
			where _T2 : T
		{
			IList<T> sorted = new List<T>(items);
			sorted.Sort(compareFunc);
			return GetNonOverlapping(sorted, toIntervalFunc);
		}

		public static IList<T> GetNonOverlapping<T, E, _T2, _T3>(IList<_T2> items, IComparator<_T3> compareFunc)
			where T : IHasInterval<E>
			where E : IComparable<E>
			where _T2 : T
		{
			IFunction<T, Interval<E>> toIntervalFunc = null;
			return GetNonOverlapping(items, toIntervalFunc, compareFunc);
		}

		public static IList<T> GetNonOverlapping<T, E, _T2>(IList<_T2> items)
			where T : IHasInterval<E>
			where E : IComparable<E>
			where _T2 : T
		{
			IFunction<T, Interval<E>> toIntervalFunc = null;
			return GetNonOverlapping(items, toIntervalFunc);
		}

		private class PartialScoredList<T, E>
		{
			internal T @object;

			internal E lastMatchKey;

			internal int size;

			internal double score;
		}

		public static IList<T> GetNonOverlappingMaxScore<T, E, _T2, _T3, _T4>(IList<_T2> items, IFunction<_T3> toIntervalFunc, IToDoubleFunction<_T4> scoreFunc)
			where E : IComparable<E>
			where _T2 : T
		{
			if (items.Count > 1)
			{
				IDictionary<E, IntervalTree.PartialScoredList<T, E>> bestNonOverlapping = new SortedDictionary<E, IntervalTree.PartialScoredList<T, E>>();
				foreach (T item in items)
				{
					Interval<E> itemInterval = toIntervalFunc.Apply(item);
					E mBegin = itemInterval.GetBegin();
					E mEnd = itemInterval.GetEnd();
					IntervalTree.PartialScoredList<T, E> bestk = bestNonOverlapping[mEnd];
					double itemScore = scoreFunc.ApplyAsDouble(item);
					if (bestk == null)
					{
						bestk = new IntervalTree.PartialScoredList<T, E>();
						bestk.size = 1;
						bestk.score = itemScore;
						bestk.@object = item;
						bestNonOverlapping[mEnd] = bestk;
					}
					// Assumes map is ordered
					foreach (E j in bestNonOverlapping.Keys)
					{
						if (j.CompareTo(mBegin) > 0)
						{
							break;
						}
						// Consider adding this match into the bestNonOverlapping strand at j
						IntervalTree.PartialScoredList<T, E> bestj = bestNonOverlapping[j];
						double withMatchScore = bestj.score + itemScore;
						bool better = false;
						if (withMatchScore > bestk.score)
						{
							better = true;
						}
						else
						{
							if (withMatchScore == bestk.score)
							{
								if (bestj.size + 1 < bestk.size)
								{
									better = true;
								}
							}
						}
						if (better)
						{
							bestk.size = bestj.size + 1;
							bestk.score = withMatchScore;
							bestk.@object = item;
							bestk.lastMatchKey = j;
						}
					}
				}
				IntervalTree.PartialScoredList<T, E> best = null;
				foreach (IntervalTree.PartialScoredList<T, E> v in bestNonOverlapping.Values)
				{
					if (best == null || v.score > best.score)
					{
						best = v;
					}
				}
				IList<T> nonOverlapping = new List<T>(best.size);
				IntervalTree.PartialScoredList<T, E> prev = best;
				while (prev != null)
				{
					if (prev.@object != null)
					{
						nonOverlapping.Add(prev.@object);
					}
					if (prev.lastMatchKey != null)
					{
						prev = bestNonOverlapping[prev.lastMatchKey];
					}
					else
					{
						prev = null;
					}
				}
				Java.Util.Collections.Reverse(nonOverlapping);
				return nonOverlapping;
			}
			else
			{
				IList<T> nonOverlapping = new List<T>(items);
				return nonOverlapping;
			}
		}

		public static IList<T> GetNonOverlappingMaxScore<T, E, _T2, _T3>(IList<_T2> items, IToDoubleFunction<_T3> scoreFunc)
			where T : IHasInterval<E>
			where E : IComparable<E>
			where _T2 : T
		{
			IFunction<T, Interval<E>> toIntervalFunc = null;
			return GetNonOverlappingMaxScore(items, toIntervalFunc, scoreFunc);
		}

		public static IList<T> GetNonNested<T, E, _T2, _T3, _T4>(IList<_T2> items, IFunction<_T3> toIntervalFunc, IComparator<_T4> compareFunc)
			where E : IComparable<E>
			where _T2 : T
		{
			IList<T> sorted = new List<T>(items);
			sorted.Sort(compareFunc);
			IList<T> res = new List<T>();
			IntervalTree<E, Interval<E>> intervals = new IntervalTree<E, Interval<E>>();
			foreach (T item in sorted)
			{
				Interval<E> i = toIntervalFunc.Apply(item);
				bool addOk = intervals.AddNonNested(i);
				if (addOk)
				{
					res.Add(item);
				}
			}
			//        log.info("Discarding " + item);
			return res;
		}
	}
}
