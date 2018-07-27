using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	[System.Serializable]
	public class NodePattern : SemgrexPattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.NodePattern));

		private const long serialVersionUID = -5981133879119233896L;

		private readonly GraphRelation reln;

		private readonly bool negDesc;

		/// <summary>
		/// A hash map from a key to a pair (case_sensitive_pattern, case_insensitive_pattern)
		/// If the type of the entry is a String, then string comparison is safe.
		/// </summary>
		/// <remarks>
		/// A hash map from a key to a pair (case_sensitive_pattern, case_insensitive_pattern)
		/// If the type of the entry is a String, then string comparison is safe.
		/// If the type is a Boolean, it will always either match or not match corresponding to the Boolean
		/// value.
		/// Otherwise, the type will be a Pattern, and you must use Pattern.matches().
		/// </remarks>
		private readonly IDictionary<string, Pair<object, object>> attributes;

		private readonly bool isRoot;

		private bool isLink;

		private bool isEmpty;

		private readonly string name;

		private string descString;

		internal SemgrexPattern child;

		private IList<Pair<int, string>> variableGroups;

		public NodePattern(GraphRelation r, bool negDesc, IDictionary<string, string> attrs, bool root, bool empty, string name)
			: this(r, negDesc, attrs, root, empty, name, new List<Pair<int, string>>(0))
		{
		}

		public NodePattern(GraphRelation r, bool negDesc, IDictionary<string, string> attrs, bool root, bool empty, string name, IList<Pair<int, string>> variableGroups)
		{
			// specifies the groups in a regex that are captured as
			// matcher-global string variables
			// TODO: there is no capacity for named variable groups in the parser right now
			this.reln = r;
			this.negDesc = negDesc;
			attributes = Generics.NewHashMap();
			descString = "{";
			foreach (KeyValuePair<string, string> entry in attrs)
			{
				if (!descString.Equals("{"))
				{
					descString += ";";
				}
				string key = entry.Key;
				string value = entry.Value;
				// Add the attributes for this key
				if (value.Equals("__"))
				{
					attributes[key] = Pair.MakePair(true, true);
				}
				else
				{
					if (value.Matches("/.*/"))
					{
						bool isRegexp = false;
						for (int i = 1; i < value.Length - 1; ++i)
						{
							char chr = value[i];
							if (!((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || (chr >= '0' && chr <= '9')))
							{
								isRegexp = true;
								break;
							}
						}
						string patternContent = Sharpen.Runtime.Substring(value, 1, value.Length - 1);
						if (isRegexp)
						{
							attributes[key] = Pair.MakePair(Pattern.Compile(patternContent), Pattern.Compile(patternContent, Pattern.CaseInsensitive | Pattern.UnicodeCase));
						}
						else
						{
							attributes[key] = Pair.MakePair(patternContent, patternContent);
						}
					}
					else
					{
						// raw description
						attributes[key] = Pair.MakePair(value, value);
					}
				}
				//      if (value.equals("__")) {
				//        attributes.put(key, Pair.makePair(Pattern.compile(".*"), Pattern.compile(".*", Pattern.CASE_INSENSITIVE)));
				//      } else if (value.matches("/.*/")) {
				//        attributes.put(key, Pair.makePair(
				//            Pattern.compile(value.substring(1, value.length() - 1)),
				//            Pattern.compile(value.substring(1, value.length() - 1), Pattern.CASE_INSENSITIVE))
				//        );
				//      } else { // raw description
				//        attributes.put(key, Pair.makePair(
				//            Pattern.compile("^(" + value + ")$"),
				//            Pattern.compile("^(" + value + ")$", Pattern.CASE_INSENSITIVE))
				//        );
				//      }
				descString += (key + ':' + value);
			}
			if (root)
			{
				descString += "$";
			}
			else
			{
				if (empty)
				{
					descString += "#";
				}
			}
			descString += '}';
			this.name = name;
			this.child = null;
			this.isRoot = root;
			this.isEmpty = empty;
			this.variableGroups = variableGroups;
		}

		public virtual bool NodeAttrMatch(IndexedWord node, SemanticGraph sg, bool ignoreCase)
		{
			// System.out.println(node.word());
			if (isRoot)
			{
				return (negDesc ? !sg.GetRoots().Contains(node) : sg.GetRoots().Contains(node));
			}
			// System.out.println("not root");
			if (isEmpty)
			{
				return (negDesc ? !node.Equals(IndexedWord.NoWord) : node.Equals(IndexedWord.NoWord));
			}
			// log.info("Attributes are: " + attributes);
			foreach (KeyValuePair<string, Pair<object, object>> attr in attributes)
			{
				string key = attr.Key;
				// System.out.println(key);
				string nodeValue;
				// if (key.equals("idx"))
				// nodeValue = Integer.toString(node.index());
				// else {
				Type c = Env.LookupAnnotationKey(env, key);
				//find class for the key
				object value = node.Get(c);
				if (value == null)
				{
					nodeValue = null;
				}
				else
				{
					nodeValue = value.ToString();
				}
				// }
				// System.out.println(nodeValue);
				if (nodeValue == null)
				{
					return negDesc;
				}
				// Get the node pattern
				object toMatch = ignoreCase ? attr.Value.second : attr.Value.first;
				bool matches;
				if (toMatch is bool)
				{
					matches = ((bool)toMatch);
				}
				else
				{
					if (toMatch is string)
					{
						if (ignoreCase)
						{
							matches = Sharpen.Runtime.EqualsIgnoreCase(nodeValue, toMatch.ToString());
						}
						else
						{
							matches = nodeValue.Equals(toMatch.ToString());
						}
					}
					else
					{
						if (toMatch is Pattern)
						{
							matches = ((Pattern)toMatch).Matcher(nodeValue).Matches();
						}
						else
						{
							throw new InvalidOperationException("Unknown matcher type: " + toMatch + " (of class + " + toMatch.GetType() + ")");
						}
					}
				}
				if (!matches)
				{
					// System.out.println("doesn't match");
					// System.out.println("");
					return negDesc;
				}
			}
			// System.out.println("matches");
			// System.out.println("");
			return !negDesc;
		}

		public virtual void MakeLink()
		{
			isLink = true;
		}

		public virtual bool IsRoot()
		{
			return isRoot;
		}

		public virtual bool IsNull()
		{
			return isEmpty;
		}

		internal override string LocalString()
		{
			return ToString(true, false);
		}

		public override string ToString()
		{
			return ToString(true, true);
		}

		public override string ToString(bool hasPrecedence)
		{
			return ToString(hasPrecedence, true);
		}

		public virtual string ToString(bool hasPrecedence, bool addChild)
		{
			StringBuilder sb = new StringBuilder();
			if (IsNegated())
			{
				sb.Append('!');
			}
			if (IsOptional())
			{
				sb.Append('?');
			}
			sb.Append(' ');
			if (reln != null)
			{
				sb.Append(reln);
				sb.Append(' ');
			}
			if (!hasPrecedence && addChild && child != null)
			{
				sb.Append('(');
			}
			if (negDesc)
			{
				sb.Append('!');
			}
			sb.Append(descString);
			if (name != null)
			{
				sb.Append('=').Append(name);
			}
			if (addChild && child != null)
			{
				sb.Append(' ');
				sb.Append(child.ToString(false));
				if (!hasPrecedence)
				{
					sb.Append(')');
				}
			}
			return sb.ToString();
		}

		internal override void SetChild(SemgrexPattern n)
		{
			child = n;
		}

		internal override IList<SemgrexPattern> GetChildren()
		{
			if (child == null)
			{
				return Java.Util.Collections.EmptyList();
			}
			else
			{
				return Java.Util.Collections.SingletonList(child);
			}
		}

		public virtual string GetName()
		{
			return name;
		}

		internal override SemgrexMatcher Matcher(SemanticGraph sg, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings, bool ignoreCase)
		{
			return new NodePattern.NodeMatcher(this, sg, null, null, true, node, namesToNodes, namesToRelations, variableStrings, ignoreCase);
		}

		internal override SemgrexMatcher Matcher(SemanticGraph sg, Alignment alignment, SemanticGraph sg_align, bool hyp, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings 
			variableStrings, bool ignoreCase)
		{
			// log.info("making matcher: " +
			// ((reln.equals(GraphRelation.ALIGNED_ROOT)) ? false : hyp));
			return new NodePattern.NodeMatcher(this, sg, alignment, sg_align, (reln.Equals(GraphRelation.AlignedRoot)) ? false : hyp, (reln.Equals(GraphRelation.AlignedRoot)) ? sg_align.GetFirstRoot() : node, namesToNodes, namesToRelations, variableStrings
				, ignoreCase);
		}

		private class NodeMatcher : SemgrexMatcher
		{
			/// <summary>
			/// when finished = true, it means I have exhausted my potential
			/// node match candidates.
			/// </summary>
			private bool finished = false;

			private IEnumerator<IndexedWord> nodeMatchCandidateIterator = null;

			private readonly NodePattern myNode;

			/// <summary>
			/// a NodeMatcher only has a single child; if it is the left side
			/// of multiple relations, a CoordinationMatcher is used.
			/// </summary>
			private SemgrexMatcher childMatcher;

			private bool matchedOnce = false;

			private bool committedVariables = false;

			private string nextMatchReln = null;

			private IndexedWord nextMatch = null;

			private bool namedFirst = false;

			private bool relnNamedFirst = false;

			private bool ignoreCase = false;

			public NodeMatcher(NodePattern n, SemanticGraph sg, Alignment alignment, SemanticGraph sg_align, bool hyp, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings
				, bool ignoreCase)
				: base(sg, alignment, sg_align, hyp, node, namesToNodes, namesToRelations, variableStrings)
			{
				// universal: childMatcher is null if and only if
				// myNode.child == null OR resetChild has never been called
				myNode = n;
				this.ignoreCase = ignoreCase;
				ResetChildIter();
			}

			internal override void ResetChildIter()
			{
				nodeMatchCandidateIterator = myNode.reln.SearchNodeIterator(node, hyp ? sg : sg_aligned);
				if (myNode.reln is GraphRelation.ALIGNMENT)
				{
					((GraphRelation.ALIGNMENT)myNode.reln).SetAlignment(alignment, hyp, (GraphRelation.SearchNodeIterator)nodeMatchCandidateIterator);
				}
				finished = false;
				if (nextMatch != null)
				{
					DecommitVariableGroups();
					DecommitNamedNodes();
					DecommitNamedRelations();
				}
				nextMatch = null;
			}

			private void ResetChild()
			{
				if (childMatcher == null)
				{
					if (myNode.child == null)
					{
						matchedOnce = false;
					}
					else
					{
						childMatcher = myNode.child.Matcher(sg, alignment, sg_aligned, (myNode.reln is GraphRelation.ALIGNMENT) ? !hyp : hyp, nextMatch, namesToNodes, namesToRelations, variableStrings, ignoreCase);
					}
				}
				else
				{
					childMatcher.ResetChildIter(nextMatch);
				}
			}

			/*
			* goes to the next node in the tree that is a successful match to my
			* description pattern
			*/
			// when finished = false; break; is called, it means I successfully matched.
			private void GoToNextNodeMatch()
			{
				DecommitVariableGroups();
				// make sure variable groups are free.
				DecommitNamedNodes();
				DecommitNamedRelations();
				finished = true;
				Matcher m = null;
				while (nodeMatchCandidateIterator.MoveNext())
				{
					if (myNode.reln.GetName() != null)
					{
						string foundReln = namesToRelations[myNode.reln.GetName()];
						nextMatchReln = ((GraphRelation.SearchNodeIterator)nodeMatchCandidateIterator).GetReln();
						if ((foundReln != null) && (!nextMatchReln.Equals(foundReln)))
						{
							nextMatch = nodeMatchCandidateIterator.Current;
							continue;
						}
					}
					nextMatch = nodeMatchCandidateIterator.Current;
					// log.info("going to next match: " + nextMatch.word() + " " +
					// myNode.descString + " " + myNode.isLink);
					if (myNode.descString.Equals("{}") && myNode.isLink)
					{
						IndexedWord otherNode = namesToNodes[myNode.name];
						if (otherNode != null)
						{
							if (otherNode.Equals(nextMatch))
							{
								if (!myNode.negDesc)
								{
									finished = false;
									break;
								}
							}
							else
							{
								if (myNode.negDesc)
								{
									finished = false;
									break;
								}
							}
						}
						else
						{
							bool found = myNode.NodeAttrMatch(nextMatch, hyp ? sg : sg_aligned, ignoreCase);
							if (found)
							{
								foreach (Pair<int, string> varGroup in myNode.variableGroups)
								{
									// if variables have been captured from a regex, they
									// must match any previous matchings
									string thisVariable = varGroup.Second();
									string thisVarString = variableStrings.GetString(thisVariable);
									if (thisVarString != null && !thisVarString.Equals(m.Group(varGroup.First())))
									{
										// failed to match a variable
										found = false;
										break;
									}
								}
								// nodeAttrMatch already checks negDesc, so no need to
								// check for that here
								finished = false;
								break;
							}
						}
					}
					else
					{
						// try to match the description pattern.
						bool found = myNode.NodeAttrMatch(nextMatch, hyp ? sg : sg_aligned, ignoreCase);
						if (found)
						{
							foreach (Pair<int, string> varGroup in myNode.variableGroups)
							{
								// if variables have been captured from a regex, they
								// must match any previous matchings
								string thisVariable = varGroup.Second();
								string thisVarString = variableStrings.GetString(thisVariable);
								if (thisVarString != null && !thisVarString.Equals(m.Group(varGroup.First())))
								{
									// failed to match a variable
									found = false;
									break;
								}
							}
							// nodeAttrMatch already checks negDesc, so no need to
							// check for that here
							finished = false;
							break;
						}
					}
				}
				// end while
				if (!finished)
				{
					// I successfully matched.
					ResetChild();
					if (myNode.name != null)
					{
						// note: have to fill in the map as we go for backreferencing
						if (!namesToNodes.Contains(myNode.name))
						{
							// log.info("making namedFirst");
							namedFirst = true;
						}
						// log.info("adding named node: " + myNode.name + "=" +
						// nextMatch.word());
						namesToNodes[myNode.name] = nextMatch;
					}
					if (myNode.reln.GetName() != null)
					{
						if (!namesToRelations.Contains(myNode.reln.GetName()))
						{
							relnNamedFirst = true;
						}
						namesToRelations[myNode.reln.GetName()] = nextMatchReln;
					}
					CommitVariableGroups(m);
				}
			}

			// commit my variable groups.
			// finished is false exiting this if and only if nextChild exists
			// and has a label or backreference that matches
			// (also it will just have been reset)
			private void CommitVariableGroups(Matcher m)
			{
				committedVariables = true;
				// commit all my variable groups.
				foreach (Pair<int, string> varGroup in myNode.variableGroups)
				{
					string thisVarString = m.Group(varGroup.First());
					variableStrings.SetVar(varGroup.Second(), thisVarString);
				}
			}

			private void DecommitVariableGroups()
			{
				if (committedVariables)
				{
					foreach (Pair<int, string> varGroup in myNode.variableGroups)
					{
						variableStrings.UnsetVar(varGroup.Second());
					}
				}
				committedVariables = false;
			}

			private void DecommitNamedNodes()
			{
				if (namesToNodes.Contains(myNode.name) && namedFirst)
				{
					namedFirst = false;
					Sharpen.Collections.Remove(namesToNodes, myNode.name);
				}
			}

			private void DecommitNamedRelations()
			{
				if (namesToRelations.Contains(myNode.reln.name) && relnNamedFirst)
				{
					relnNamedFirst = false;
					Sharpen.Collections.Remove(namesToRelations, myNode.reln.name);
				}
			}

			/*
			* tries to match the unique child of the NodePattern node to a node.
			* Returns "true" if succeeds.
			*/
			private bool MatchChild()
			{
				// entering here (given that it's called only once in matches())
				// we know finished is false, and either nextChild == null
				// (meaning goToNextChild has not been called) or nextChild exists
				// and has a label or backreference that matches
				if (nextMatch == null)
				{
					// I haven't been initialized yet, so my child
					// certainly can't be matched yet.
					return false;
				}
				if (childMatcher == null)
				{
					if (!matchedOnce)
					{
						matchedOnce = true;
						return true;
					}
					return false;
				}
				// childMatcher.namesToNodes.putAll(this.namesToNodes);
				// childMatcher.namesToRelations.putAll(this.namesToRelations);
				bool match = childMatcher.Matches();
				if (match)
				{
				}
				else
				{
					// namesToNodes.putAll(childMatcher.namesToNodes);
					// namesToRelations.putAll(childMatcher.namesToRelations);
					// System.out.println(node.word() + " " +
					// namesToNodes.get("partnerTwo"));
					if (nextMatch != null)
					{
						DecommitVariableGroups();
						DecommitNamedNodes();
						DecommitNamedRelations();
					}
				}
				return match;
			}

			// find the next local match
			public override bool Matches()
			{
				// System.out.println(toString());
				// System.out.println(namesToNodes);
				// log.info("matches: " + myNode.reln);
				// this is necessary so that a negated/optional node matches only once
				if (finished)
				{
					// System.out.println(false);
					return false;
				}
				while (!finished)
				{
					if (MatchChild())
					{
						if (myNode.IsNegated())
						{
							// negated node only has to fail once
							finished = true;
							return false;
						}
						else
						{
							// cannot be optional and negated
							if (myNode.IsOptional())
							{
								finished = true;
							}
							// System.out.println(true);
							return true;
						}
					}
					else
					{
						GoToNextNodeMatch();
					}
				}
				if (myNode.IsNegated())
				{
					// couldn't match my relation/pattern, so succeeded!
					return true;
				}
				else
				{
					// couldn't match my relation/pattern, so failed!
					nextMatch = null;
					DecommitVariableGroups();
					DecommitNamedNodes();
					DecommitNamedRelations();
					// didn't match, but return true anyway if optional
					return myNode.IsOptional();
				}
			}

			public override IndexedWord GetMatch()
			{
				return nextMatch;
			}

			public override string ToString()
			{
				return "node matcher for: " + myNode.LocalString();
			}
		}
		// end static class NodeMatcher
	}
}
