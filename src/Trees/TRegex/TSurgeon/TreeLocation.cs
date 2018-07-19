using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy</author>
	internal class TreeLocation
	{
		private readonly string relation;

		private readonly TsurgeonPattern child;

		public TreeLocation(string relation, TsurgeonPattern p)
		{
			this.relation = relation;
			this.child = p;
		}

		internal virtual void SetRoot(TsurgeonPatternRoot root)
		{
			child.SetRoot(root);
		}

		private static readonly Pattern daughterPattern = Pattern.Compile(">-?([0-9]+)");

		public virtual TreeLocation.LocationMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new TreeLocation.LocationMatcher(this, newNodeNames, coindexer);
		}

		/// <summary>TODO: it would be nice to refactor this with TsurgeonMatcher somehow</summary>
		internal class LocationMatcher
		{
			internal IDictionary<string, Tree> newNodeNames;

			internal CoindexationGenerator coindexer;

			internal TsurgeonMatcher childMatcher;

			internal LocationMatcher(TreeLocation _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
			{
				this._enclosing = _enclosing;
				this.newNodeNames = newNodeNames;
				this.coindexer = coindexer;
				this.childMatcher = this._enclosing.child.Matcher(newNodeNames, coindexer);
			}

			internal virtual Pair<Tree, int> Evaluate(Tree tree, TregexMatcher tregex)
			{
				int newIndex;
				// initialized below
				Tree parent;
				// initialized below
				Tree relativeNode = this.childMatcher.Evaluate(tree, tregex);
				Matcher m = TreeLocation.daughterPattern.Matcher(this._enclosing.relation);
				if (m.Matches())
				{
					newIndex = System.Convert.ToInt32(m.Group(1)) - 1;
					parent = relativeNode;
					if (this._enclosing.relation[1] == '-')
					{
						// backwards.
						newIndex = parent.Children().Length - newIndex;
					}
				}
				else
				{
					parent = relativeNode.Parent(tree);
					if (parent == null)
					{
						throw new Exception("Error: looking for a non-existent parent in tree " + tree + " for \"" + this.ToString() + '"');
					}
					int index = parent.ObjectIndexOf(relativeNode);
					switch (this._enclosing.relation)
					{
						case "$+":
						{
							newIndex = index;
							break;
						}

						case "$-":
						{
							newIndex = index + 1;
							break;
						}

						default:
						{
							throw new Exception("Error: Haven't dealt with relation " + this._enclosing.relation + " yet.");
						}
					}
				}
				return new Pair<Tree, int>(parent, newIndex);
			}

			private readonly TreeLocation _enclosing;
		}

		public override string ToString()
		{
			return relation + ' ' + child;
		}
	}
}
