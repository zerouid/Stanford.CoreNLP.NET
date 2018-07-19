using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	public class AuxiliaryTree
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.AuxiliaryTree));

		private readonly string originalTreeString;

		internal readonly Tree tree;

		internal Tree foot;

		private readonly IdentityHashMap<Tree, string> nodesToNames;

		private readonly IDictionary<string, Tree> namesToNodes;

		public AuxiliaryTree(Tree tree, bool mustHaveFoot)
		{
			// no one else should be able to get this one.
			// this one has a getter.
			originalTreeString = tree.ToString();
			this.tree = tree;
			this.foot = FindFootNode(tree);
			if (foot == null && mustHaveFoot)
			{
				throw new TsurgeonParseException("Error -- no foot node found for " + originalTreeString);
			}
			namesToNodes = Generics.NewHashMap();
			nodesToNames = new IdentityHashMap<Tree, string>();
			InitializeNamesNodesMaps(tree);
		}

		private AuxiliaryTree(Tree tree, Tree foot, IDictionary<string, Tree> namesToNodes, string originalTreeString)
		{
			this.originalTreeString = originalTreeString;
			this.tree = tree;
			this.foot = foot;
			this.namesToNodes = namesToNodes;
			nodesToNames = null;
		}

		public virtual IDictionary<string, Tree> NamesToNodes()
		{
			return namesToNodes;
		}

		public override string ToString()
		{
			return originalTreeString;
		}

		/// <summary>Copies the Auxiliary tree.</summary>
		/// <remarks>
		/// Copies the Auxiliary tree.  Also, puts the new names-&gt;nodes map in the TsurgeonMatcher that called copy.
		/// <br />
		/// The trees and labels to use when making the copy are specified
		/// with treeFactory and labelFactory.  This lets the tsurgeon script
		/// produce trees which are of the same type as the input trees.
		/// Each of the tsurgeon relations which copies a tree should include
		/// pass in the correct factories.
		/// </remarks>
		public virtual Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.AuxiliaryTree Copy(TsurgeonMatcher matcher, ITreeFactory treeFactory, ILabelFactory labelFactory)
		{
			if (labelFactory == null)
			{
				labelFactory = CoreLabel.Factory();
			}
			IDictionary<string, Tree> newNamesToNodes = Generics.NewHashMap();
			Pair<Tree, Tree> result = CopyHelper(tree, newNamesToNodes, treeFactory, labelFactory);
			//if(! result.first().dominates(result.second()))
			//log.info("Error -- aux tree copy doesn't dominate foot copy.");
			matcher.newNodeNames.PutAll(newNamesToNodes);
			return new Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.AuxiliaryTree(result.First(), result.Second(), newNamesToNodes, originalTreeString);
		}

		// returns Pair<node,foot>
		private Pair<Tree, Tree> CopyHelper(Tree node, IDictionary<string, Tree> newNamesToNodes, ITreeFactory treeFactory, ILabelFactory labelFactory)
		{
			Tree clone;
			Tree newFoot = null;
			if (node.IsLeaf())
			{
				if (node == foot)
				{
					// found the foot node; pass it up.
					clone = treeFactory.NewTreeNode(node.Label(), new List<Tree>(0));
					newFoot = clone;
				}
				else
				{
					clone = treeFactory.NewLeaf(labelFactory.NewLabel(node.Label()));
				}
			}
			else
			{
				IList<Tree> newChildren = new List<Tree>(node.Children().Length);
				foreach (Tree child in node.Children())
				{
					Pair<Tree, Tree> newChild = CopyHelper(child, newNamesToNodes, treeFactory, labelFactory);
					newChildren.Add(newChild.First());
					if (newChild.Second() != null)
					{
						if (newFoot != null)
						{
							log.Info("Error -- two feet found when copying auxiliary tree " + tree.ToString() + "; using last foot found.");
						}
						newFoot = newChild.Second();
					}
				}
				clone = treeFactory.NewTreeNode(labelFactory.NewLabel(node.Label()), newChildren);
			}
			if (nodesToNames.Contains(node))
			{
				newNamesToNodes[nodesToNames[node]] = clone;
			}
			return new Pair<Tree, Tree>(clone, newFoot);
		}

		private const string footNodeCharacter = "@";

		private static readonly Pattern footNodeLabelPattern = Pattern.Compile("^(.*)" + footNodeCharacter + '$');

		private static readonly Pattern escapedFootNodeCharacter = Pattern.Compile('\\' + footNodeCharacter);

		/* below here is init stuff for finding the foot node.     */
		/// <summary>
		/// Returns the foot node of the adjunction tree, which is the terminal node
		/// that ends in @.
		/// </summary>
		/// <remarks>
		/// Returns the foot node of the adjunction tree, which is the terminal node
		/// that ends in @.  In the process, turns the foot node into a TreeNode
		/// (rather than a leaf), and destructively un-escapes all the escaped
		/// instances of @ in the tree.  Note that final @ in a non-terminal node is
		/// ignored, and left in.
		/// </remarks>
		private static Tree FindFootNode(Tree t)
		{
			Tree footNode = FindFootNodeHelper(t);
			Tree result = footNode;
			if (footNode != null)
			{
				Tree newFootNode = footNode.TreeFactory().NewTreeNode(footNode.Label(), new List<Tree>());
				Tree parent = footNode.Parent(t);
				if (parent != null)
				{
					int i = parent.ObjectIndexOf(footNode);
					parent.SetChild(i, newFootNode);
				}
				result = newFootNode;
			}
			return result;
		}

		private static Tree FindFootNodeHelper(Tree t)
		{
			Tree foundDtr = null;
			if (t.IsLeaf())
			{
				Matcher m = footNodeLabelPattern.Matcher(t.Label().Value());
				if (m.Matches())
				{
					t.Label().SetValue(m.Group(1));
					return t;
				}
				else
				{
					return null;
				}
			}
			foreach (Tree child in t.Children())
			{
				Tree thisFoundDtr = FindFootNodeHelper(child);
				if (thisFoundDtr != null)
				{
					if (foundDtr != null)
					{
						throw new TsurgeonParseException("Error -- two foot nodes in subtree" + t.ToString());
					}
					else
					{
						foundDtr = thisFoundDtr;
					}
				}
			}
			Matcher m_1 = escapedFootNodeCharacter.Matcher(t.Label().Value());
			t.Label().SetValue(m_1.ReplaceAll(footNodeCharacter));
			return foundDtr;
		}

		internal static readonly Pattern namePattern = Pattern.Compile("^((?:[^\\\\]*)|(?:(?:.*[^\\\\])?)(?:\\\\\\\\)*)=([^=]+)$");

		/* ******************************************************* *
		* below here is init stuff for getting node -> names maps *
		* ******************************************************* */
		// There are two ways in which you can can match the start of a name
		// expression.
		// The first is if you have any number of non-escaping characters
		// preceding an "=" and a name.  This is the ([^\\\\]*) part.
		// The second is if you have any number of any characters, followed
		// by a non-"\" character, as "\" is used to escape the "=".  After
		// that, any number of pairs of "\" are allowed, as we let "\" also
		// escape itself.  After that comes "=" and a name.
		/// <summary>Looks for new names, destructively strips them out.</summary>
		/// <remarks>
		/// Looks for new names, destructively strips them out.
		/// Destructively unescapes escaped chars, including "=", as well.
		/// </remarks>
		private void InitializeNamesNodesMaps(Tree t)
		{
			foreach (Tree node in t.SubTreeList())
			{
				Matcher m = namePattern.Matcher(node.Label().Value());
				if (m.Find())
				{
					namesToNodes[m.Group(2)] = node;
					nodesToNames[node] = m.Group(2);
					node.Label().SetValue(m.Group(1));
				}
				node.Label().SetValue(Unescape(node.Label().Value()));
			}
		}

		internal static string Unescape(string input)
		{
			return input.ReplaceAll("\\\\(.)", "$1");
		}
	}
}
