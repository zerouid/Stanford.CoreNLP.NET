using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Extracts bilexical dependencies from Penn Treebank-style phrase structure trees
	/// as described in (Collins, 1999) and the later Comp.
	/// </summary>
	/// <remarks>
	/// Extracts bilexical dependencies from Penn Treebank-style phrase structure trees
	/// as described in (Collins, 1999) and the later Comp. Ling. paper (Collins, 2003).
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class CollinsDependency : IDependency<CoreLabel, CoreLabel, string>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.CollinsDependency));

		private const long serialVersionUID = -4236496863919294754L;

		private const string normPOSLabel = "TAG";

		private readonly CoreLabel modifier;

		private readonly CoreLabel head;

		private readonly CollinsRelation relation;

		/// <summary>Modifier must have IndexAnnotation.</summary>
		/// <remarks>
		/// Modifier must have IndexAnnotation. If head has 0 as its index, then it is
		/// the start symbol ("boundary symbol" in the Dan Klein code).
		/// </remarks>
		/// <param name="modifier"/>
		/// <param name="head"/>
		/// <param name="rel"/>
		public CollinsDependency(CoreLabel modifier, CoreLabel head, CollinsRelation rel)
		{
			if (modifier.Index() == 0)
			{
				throw new Exception("No index annotation for " + modifier.ToString());
			}
			this.modifier = modifier;
			this.head = head;
			relation = rel;
		}

		public virtual CollinsRelation GetRelation()
		{
			return relation;
		}

		public virtual IDependencyFactory DependencyFactory()
		{
			return null;
		}

		public virtual CoreLabel Dependent()
		{
			return modifier;
		}

		public virtual CoreLabel Governor()
		{
			return head;
		}

		public virtual bool EqualsIgnoreName(object o)
		{
			return this.Equals(o);
		}

		public virtual string Name()
		{
			return "CollinsBilexicalDependency";
		}

		public virtual string ToString(string format)
		{
			return ToString();
		}

		private static CoreLabel MakeStartLabel(string label)
		{
			CoreLabel root = new CoreLabel();
			root.Set(typeof(CoreAnnotations.ValueAnnotation), label);
			root.Set(typeof(CoreAnnotations.IndexAnnotation), 0);
			return root;
		}

		public static ICollection<Edu.Stanford.Nlp.Trees.CollinsDependency> ExtractFromTree(Tree t, string startSymbol, IHeadFinder hf)
		{
			return ExtractFromTree(t, startSymbol, hf, false);
		}

		public static ICollection<Edu.Stanford.Nlp.Trees.CollinsDependency> ExtractNormalizedFromTree(Tree t, string startSymbol, IHeadFinder hf)
		{
			return ExtractFromTree(t, startSymbol, hf, true);
		}

		/// <summary>This method assumes that a start symbol node has been added to the tree.</summary>
		/// <param name="t">The tree</param>
		/// <param name="hf">A head finding algorithm.</param>
		/// <returns>A set of dependencies</returns>
		private static ICollection<Edu.Stanford.Nlp.Trees.CollinsDependency> ExtractFromTree(Tree t, string startSymbol, IHeadFinder hf, bool normPOS)
		{
			if (t == null || startSymbol.Equals(string.Empty) || hf == null)
			{
				return null;
			}
			ICollection<Edu.Stanford.Nlp.Trees.CollinsDependency> deps = Generics.NewHashSet();
			if (t.Value().Equals(startSymbol))
			{
				t = t.FirstChild();
			}
			bool mustProcessRoot = true;
			foreach (Tree node in t)
			{
				if (node.IsLeaf() || node.NumChildren() < 2)
				{
					continue;
				}
				Tree headDaughter = hf.DetermineHead(node);
				Tree head = node.HeadTerminal(hf);
				if (headDaughter == null || head == null)
				{
					log.Info("WARNING: CollinsDependency.extractFromTree() could not find root for:\n" + node.PennString());
				}
				else
				{
					//Make dependencies
					if (mustProcessRoot)
					{
						mustProcessRoot = false;
						CoreLabel startLabel = MakeStartLabel(startSymbol);
						deps.Add(new Edu.Stanford.Nlp.Trees.CollinsDependency(new CoreLabel(head.Label()), startLabel, new CollinsRelation(startSymbol, startSymbol, node.Value(), CollinsRelation.Direction.Right)));
					}
					CollinsRelation.Direction dir = CollinsRelation.Direction.Left;
					foreach (Tree daughter in node.Children())
					{
						if (daughter.Equals(headDaughter))
						{
							dir = CollinsRelation.Direction.Right;
						}
						else
						{
							Tree headOfDaughter = daughter.HeadTerminal(hf);
							string relParent = (normPOS && node.IsPreTerminal()) ? normPOSLabel : node.Value();
							string relHead = (normPOS && headDaughter.IsPreTerminal()) ? normPOSLabel : headDaughter.Value();
							string relModifier = (normPOS && daughter.IsPreTerminal()) ? normPOSLabel : daughter.Value();
							Edu.Stanford.Nlp.Trees.CollinsDependency newDep = new Edu.Stanford.Nlp.Trees.CollinsDependency(new CoreLabel(headOfDaughter.Label()), new CoreLabel(head.Label()), new CollinsRelation(relParent, relHead, relModifier, dir));
							deps.Add(newDep);
						}
					}
				}
			}
			//TODO Combine the indexing procedure above with yield here so that two searches aren't performed.
			if (t.Yield().Count != deps.Count)
			{
				System.Console.Error.Printf("WARNING: Number of extracted dependencies (%d) does not match yield (%d):\n", deps.Count, t.Yield().Count);
				log.Info(t.PennString());
				log.Info();
				int num = 0;
				foreach (Edu.Stanford.Nlp.Trees.CollinsDependency dep in deps)
				{
					log.Info(num++ + ": " + dep.ToString());
				}
			}
			return deps;
		}

		public override string ToString()
		{
			return string.Format("%s (%d)   %s (%d)  <%s>", modifier.Value(), modifier.Index(), head.Value(), head.Index(), relation.ToString());
		}

		public override bool Equals(object other)
		{
			if (this == other)
			{
				return true;
			}
			if (!(other is Edu.Stanford.Nlp.Trees.CollinsDependency))
			{
				return false;
			}
			Edu.Stanford.Nlp.Trees.CollinsDependency otherDep = (Edu.Stanford.Nlp.Trees.CollinsDependency)other;
			return (modifier.Equals(otherDep.modifier) && head.Equals(otherDep.head) && relation.Equals(otherDep.relation));
		}

		public override int GetHashCode()
		{
			int hash = 1;
			hash *= (31 + modifier.Index());
			hash *= 138 * head.Value().GetHashCode();
			return hash;
		}
	}
}
