// TregexMatcher
// Copyright (c) 2004-2007 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: parser-user@lists.stanford.edu
//    Licensing: parser-support@lists.stanford.edu
//    http://www-nlp.stanford.edu/software/tregex.shtml
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>
	/// A TregexMatcher can be used to match a
	/// <see cref="TregexPattern"/>
	/// against a
	/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
	/// .
	/// Usage should be similar to a
	/// <see cref="Java.Util.Regex.Matcher"/>
	/// .
	/// </summary>
	/// <author>Galen Andrew</author>
	public abstract class TregexMatcher
	{
		internal readonly Tree root;

		internal Tree tree;

		internal IdentityHashMap<Tree, Tree> nodesToParents;

		internal readonly IDictionary<string, Tree> namesToNodes;

		internal readonly VariableStrings variableStrings;

		private IEnumerator<Tree> findIterator;

		private Tree findCurrent;

		internal readonly IHeadFinder headFinder;

		internal TregexMatcher(Tree root, Tree tree, IdentityHashMap<Tree, Tree> nodesToParents, IDictionary<string, Tree> namesToNodes, VariableStrings variableStrings, IHeadFinder headFinder)
		{
			// these things are used by "find"
			this.root = root;
			this.tree = tree;
			this.nodesToParents = nodesToParents;
			this.namesToNodes = namesToNodes;
			this.variableStrings = variableStrings;
			this.headFinder = headFinder;
		}

		public virtual IHeadFinder GetHeadFinder()
		{
			return this.headFinder;
		}

		/// <summary>Resets the matcher so that its search starts over.</summary>
		public virtual void Reset()
		{
			findIterator = null;
			findCurrent = null;
			namesToNodes.Clear();
			variableStrings.Reset();
		}

		/// <summary>Resets the matcher to start searching on the given tree for matching subexpressions.</summary>
		/// <param name="tree">The tree to start searching on</param>
		internal virtual void ResetChildIter(Tree tree)
		{
			this.tree = tree;
			ResetChildIter();
		}

		/// <summary>Resets the matcher to restart search for matching subexpressions</summary>
		internal virtual void ResetChildIter()
		{
		}

		/// <summary>
		/// Does the pattern match the tree?  It's actually closer to java.util.regex's
		/// "lookingAt" in that the root of the tree has to match the root of the pattern
		/// but the whole tree does not have to be "accounted for".
		/// </summary>
		/// <remarks>
		/// Does the pattern match the tree?  It's actually closer to java.util.regex's
		/// "lookingAt" in that the root of the tree has to match the root of the pattern
		/// but the whole tree does not have to be "accounted for".  Like with lookingAt
		/// the beginning of the string has to match the pattern, but the whole string
		/// doesn't have to be "accounted for".
		/// </remarks>
		/// <returns>whether the tree matches the pattern</returns>
		public abstract bool Matches();

		/// <summary>
		/// Resets the matcher and tests if it matches on the tree when rooted at
		/// <paramref name="node"/>
		/// .
		/// </summary>
		/// <param name="node">The node where the match is checked</param>
		/// <returns>whether the matcher matches at node</returns>
		public virtual bool MatchesAt(Tree node)
		{
			ResetChildIter(node);
			return Matches();
		}

		/// <summary>Get the last matching tree -- that is, the tree node that matches the root node of the pattern.</summary>
		/// <remarks>
		/// Get the last matching tree -- that is, the tree node that matches the root node of the pattern.
		/// Returns null if there has not been a match.
		/// </remarks>
		/// <returns>last match</returns>
		public abstract Tree GetMatch();

		/// <summary>Find the next match of the pattern on the tree.</summary>
		/// <returns>whether there is a match somewhere in the tree</returns>
		public virtual bool Find()
		{
			if (findIterator == null)
			{
				findIterator = root.GetEnumerator();
			}
			if (findCurrent != null && Matches())
			{
				return true;
			}
			while (findIterator.MoveNext())
			{
				findCurrent = findIterator.Current;
				ResetChildIter(findCurrent);
				if (Matches())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Similar to
		/// <c>find()</c>
		/// , but matches only if
		/// <paramref name="node"/>
		/// is
		/// the root of the match.  All other matches are ignored.  If you
		/// know you are looking for matches with a particular root, this is
		/// much faster than iterating over all matches and taking only the
		/// ones that work and faster than altering the tregex to match only
		/// the correct node.
		/// <br />
		/// If called multiple times with the same node, this will return
		/// subsequent matches in the same manner as find() returns
		/// subsequent matches in the same tree.  If you want to call this using
		/// the same TregexMatcher on more than one node, call reset() first;
		/// otherwise, an AssertionError will be thrown.
		/// </summary>
		public virtual bool FindAt(Tree node)
		{
			if (findCurrent != null && findCurrent != node)
			{
				throw new AssertionError("Error: must call reset() before changing nodes for a call to findAt");
			}
			if (findCurrent != null)
			{
				return Matches();
			}
			findCurrent = node;
			ResetChildIter(findCurrent);
			return Matches();
		}

		/// <summary>
		/// Find the next match of the pattern on the tree such that the
		/// matching node (that is, the tree node matching the root node of
		/// the pattern) differs from the previous matching node.
		/// </summary>
		/// <returns>true iff another matching node is found.</returns>
		public virtual bool FindNextMatchingNode()
		{
			Tree lastMatchingNode = GetMatch();
			while (Find())
			{
				if (GetMatch() != lastMatchingNode)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the node labeled with
		/// <paramref name="name"/>
		/// in the pattern.
		/// </summary>
		/// <param name="name">the name of the node, specified in the pattern.</param>
		/// <returns>node labeled by the name</returns>
		public virtual Tree GetNode(string name)
		{
			return namesToNodes[name];
		}

		public virtual ICollection<string> GetNodeNames()
		{
			return namesToNodes.Keys;
		}

		internal virtual Tree GetParent(Tree node)
		{
			if (node is IHasParent)
			{
				return node.Parent();
			}
			if (nodesToParents == null)
			{
				nodesToParents = new IdentityHashMap<Tree, Tree>();
			}
			if (nodesToParents.IsEmpty())
			{
				FillNodesToParents(root, null);
			}
			return nodesToParents[node];
		}

		private void FillNodesToParents(Tree node, Tree parent)
		{
			nodesToParents[node] = parent;
			foreach (Tree child in node.Children())
			{
				FillNodesToParents(child, node);
			}
		}

		internal virtual Tree GetRoot()
		{
			return root;
		}

		/// <summary>
		/// If there is a current match, and that match involves setting this
		/// particular variable string, this returns that string.
		/// </summary>
		/// <remarks>
		/// If there is a current match, and that match involves setting this
		/// particular variable string, this returns that string.  Otherwise,
		/// it returns null.
		/// </remarks>
		public virtual string GetVariableString(string var)
		{
			return variableStrings.GetString(var);
		}
	}
}
