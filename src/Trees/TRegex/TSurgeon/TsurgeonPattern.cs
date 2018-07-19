// TsurgeonPattern
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
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// An abstract class for patterns to manipulate
	/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
	/// s when
	/// successfully matched on with a
	/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexMatcher"/>
	/// .
	/// </summary>
	/// <author>Roger Levy</author>
	public abstract class TsurgeonPattern
	{
		internal static readonly Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern[] EmptyTsurgeonPatternArray = new Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern[0];

		internal readonly string label;

		internal readonly Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern[] children;

		internal Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern root;

		// TODO: can remove? Nothing seems to look at it.
		protected internal virtual void SetRoot(TsurgeonPatternRoot root)
		{
			this.root = root;
			foreach (Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern child in children)
			{
				child.SetRoot(root);
			}
		}

		/// <summary>In some cases, the order of the children has special meaning.</summary>
		/// <remarks>
		/// In some cases, the order of the children has special meaning.
		/// For example, in the case of ReplaceNode, the first child will
		/// evaluate to the node to be replaced, and the other(s) will
		/// evaluate to the replacement.
		/// </remarks>
		internal TsurgeonPattern(string label, Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonPattern[] children)
		{
			this.label = label;
			this.children = children;
		}

		public override string ToString()
		{
			StringBuilder resultSB = new StringBuilder();
			resultSB.Append(label);
			if (children.Length > 0)
			{
				resultSB.Append('(');
				for (int i = 0; i < children.Length; i++)
				{
					resultSB.Append(children[i]);
					if (i < children.Length - 1)
					{
						resultSB.Append(", ");
					}
				}
				resultSB.Append(')');
			}
			return resultSB.ToString();
		}

		public virtual TsurgeonMatcher Matcher()
		{
			throw new NotSupportedException("Only the root node can produce the top level matcher");
		}

		public abstract TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer);
	}
}
