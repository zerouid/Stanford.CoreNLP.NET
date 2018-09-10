using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees.Tregex
{
	[System.Serializable]
	public class DescriptionPattern : TregexPattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.DescriptionPattern));

		internal enum DescriptionMode
		{
			Pattern,
			Strings,
			Exact,
			Anything
		}

		private readonly Relation rel;

		private readonly bool negDesc;

		private readonly DescriptionPattern.DescriptionMode descriptionMode;

		private readonly string exactMatch;

		private readonly Pattern descPattern;

		private readonly IPredicate<string> stringFilter;

		private const int MaxStringMatcherSize = 8;

		private readonly string stringDesc;

		/// <summary>The name to give the matched node</summary>
		private readonly string name;

		/// <summary>If this pattern is a link, this is the node linked to</summary>
		private readonly string linkedName;

		private readonly bool isLink;

		private TregexPattern child;

		private readonly IList<Pair<int, string>> variableGroups;

		private readonly Func<string, string> basicCatFunction;

		/// <summary>Used to detect regex expressions which can be simplified to exact matches</summary>
		private static readonly Pattern SingleWordPattern = Pattern.Compile("/\\^(.)\\$/" + "|" + "/\\^\\[(.)\\]\\$/" + "|" + "/\\^([-a-zA-Z']+)\\$/");

		private static readonly Pattern MultiWordPattern = Pattern.Compile("/\\^\\(\\?\\:((?:[-a-zA-Z|]|\\\\\\$)+)\\)\\$\\/");

		private static readonly Pattern CaseInsensitivePattern = Pattern.Compile("/\\^\\(\\?i\\:((?:[-a-zA-Z|]|\\\\\\$)+)\\)\\$\\/");

		/// <summary>Used to detect regex expressions which can be simplified to exact matches</summary>
		private static readonly Pattern PrefixPattern = Pattern.Compile("/\\^([-a-zA-Z|]+)\\/" + "|" + "/\\^\\(\\?\\:([-a-zA-Z|]+)\\)\\/");

		public DescriptionPattern(Relation rel, bool negDesc, string desc, string name, bool useBasicCat, Func<string, string> basicCatFunction, IList<Pair<int, string>> variableGroups, bool isLink, string linkedName)
		{
			// what size string matchers to use before switching to regex for
			// disjunction matches
			// todo: conceptually final, but we'd need to rewrite TregexParser
			// to make it so.
			// also conceptually final, but it depends on the child
			// specifies the groups in a regex that are captured as matcher-global string variables
			// for example, /^:$/
			// for example, /^[$]$/
			// for example, /^-NONE-$/
			// for example, /^JJ/
			this.rel = rel;
			this.negDesc = negDesc;
			this.isLink = isLink;
			this.linkedName = linkedName;
			if (desc != null)
			{
				stringDesc = desc;
				// TODO: factor out some of these blocks of code
				if (desc.Equals("__") || desc.Equals("/.*/") || desc.Equals("/^.*$/"))
				{
					descriptionMode = DescriptionPattern.DescriptionMode.Anything;
					descPattern = null;
					exactMatch = null;
					stringFilter = null;
				}
				else
				{
					if (SingleWordPattern.Matcher(desc).Matches())
					{
						// Expressions are written like this to put special characters
						// in the tregex matcher, but a regular expression is less
						// efficient than a simple string match
						descriptionMode = DescriptionPattern.DescriptionMode.Exact;
						descPattern = null;
						Java.Util.Regex.Matcher matcher = SingleWordPattern.Matcher(desc);
						matcher.Matches();
						string matchedGroup = null;
						for (int i = 1; i <= matcher.GroupCount(); ++i)
						{
							if (matcher.Group(i) != null)
							{
								matchedGroup = matcher.Group(i);
								break;
							}
						}
						exactMatch = matchedGroup;
						stringFilter = null;
					}
					else
					{
						//log.info("DescriptionPattern: converting " + desc + " to " + exactMatch);
						if (MultiWordPattern.Matcher(desc).Matches())
						{
							Java.Util.Regex.Matcher matcher = MultiWordPattern.Matcher(desc);
							matcher.Matches();
							string matchedGroup = null;
							for (int i = 1; i <= matcher.GroupCount(); ++i)
							{
								if (matcher.Group(i) != null)
								{
									matchedGroup = matcher.Group(i);
									break;
								}
							}
							matchedGroup = matchedGroup.ReplaceAll("\\\\", string.Empty);
							if (matchedGroup.Split("[|]").Length > MaxStringMatcherSize)
							{
								descriptionMode = DescriptionPattern.DescriptionMode.Pattern;
								descPattern = Pattern.Compile(Sharpen.Runtime.Substring(desc, 1, desc.Length - 1));
								exactMatch = null;
								stringFilter = null;
							}
							else
							{
								//log.info("DescriptionPattern: not converting " + desc);
								descriptionMode = DescriptionPattern.DescriptionMode.Strings;
								descPattern = null;
								exactMatch = null;
								stringFilter = new ArrayStringFilter(ArrayStringFilter.Mode.Exact, matchedGroup.Split("[|]"));
							}
						}
						else
						{
							//log.info("DescriptionPattern: converting " + desc + " to " + stringFilter);
							if (CaseInsensitivePattern.Matcher(desc).Matches())
							{
								Java.Util.Regex.Matcher matcher = CaseInsensitivePattern.Matcher(desc);
								matcher.Matches();
								string matchedGroup = null;
								for (int i = 1; i <= matcher.GroupCount(); ++i)
								{
									if (matcher.Group(i) != null)
									{
										matchedGroup = matcher.Group(i);
										break;
									}
								}
								matchedGroup = matchedGroup.ReplaceAll("\\\\", string.Empty);
								if (matchedGroup.Split("[|]").Length > MaxStringMatcherSize)
								{
									descriptionMode = DescriptionPattern.DescriptionMode.Pattern;
									descPattern = Pattern.Compile(Sharpen.Runtime.Substring(desc, 1, desc.Length - 1));
									exactMatch = null;
									stringFilter = null;
								}
								else
								{
									//log.info("DescriptionPattern: not converting " + desc);
									descriptionMode = DescriptionPattern.DescriptionMode.Strings;
									descPattern = null;
									exactMatch = null;
									stringFilter = new ArrayStringFilter(ArrayStringFilter.Mode.CaseInsensitive, matchedGroup.Split("[|]"));
								}
							}
							else
							{
								//log.info("DescriptionPattern: converting " + desc + " to " + stringFilter);
								if (PrefixPattern.Matcher(desc).Matches())
								{
									Java.Util.Regex.Matcher matcher = PrefixPattern.Matcher(desc);
									matcher.Matches();
									string matchedGroup = null;
									for (int i = 1; i <= matcher.GroupCount(); ++i)
									{
										if (matcher.Group(i) != null)
										{
											matchedGroup = matcher.Group(i);
											break;
										}
									}
									if (matchedGroup.Split("\\|").Length > MaxStringMatcherSize)
									{
										descriptionMode = DescriptionPattern.DescriptionMode.Pattern;
										descPattern = Pattern.Compile(Sharpen.Runtime.Substring(desc, 1, desc.Length - 1));
										exactMatch = null;
										stringFilter = null;
									}
									else
									{
										//log.info("DescriptionPattern: not converting " + desc);
										descriptionMode = DescriptionPattern.DescriptionMode.Strings;
										descPattern = null;
										exactMatch = null;
										stringFilter = new ArrayStringFilter(ArrayStringFilter.Mode.Prefix, matchedGroup.Split("[|]"));
									}
								}
								else
								{
									//log.info("DescriptionPattern: converting " + desc + " to " + stringFilter);
									if (desc.Matches("/.*/"))
									{
										descriptionMode = DescriptionPattern.DescriptionMode.Pattern;
										descPattern = Pattern.Compile(Sharpen.Runtime.Substring(desc, 1, desc.Length - 1));
										exactMatch = null;
										stringFilter = null;
									}
									else
									{
										if (desc.IndexOf('|') >= 0)
										{
											// patterns which contain ORs are a special case; we either
											// promote those to regex match or make a string matcher out
											// of them.  for short enough disjunctions, a simple string
											// matcher can be more efficient than a regex.
											string[] words = desc.Split("[|]");
											if (words.Length <= MaxStringMatcherSize)
											{
												descriptionMode = DescriptionPattern.DescriptionMode.Strings;
												descPattern = null;
												exactMatch = null;
												stringFilter = new ArrayStringFilter(ArrayStringFilter.Mode.Exact, words);
											}
											else
											{
												descriptionMode = DescriptionPattern.DescriptionMode.Pattern;
												descPattern = Pattern.Compile("^(?:" + desc + ")$");
												exactMatch = null;
												stringFilter = null;
											}
										}
										else
										{
											// raw description
											descriptionMode = DescriptionPattern.DescriptionMode.Exact;
											descPattern = null;
											exactMatch = desc;
											stringFilter = null;
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (name == null && linkedName == null)
				{
					throw new AssertionError("Illegal description pattern.  Does not describe a node or link/name a variable");
				}
				stringDesc = " ";
				descriptionMode = null;
				descPattern = null;
				exactMatch = null;
				stringFilter = null;
			}
			this.name = name;
			SetChild(null);
			this.basicCatFunction = (useBasicCat ? basicCatFunction : null);
			//    System.out.println("Made " + (negDesc ? "negated " : "") + "DescNode with " + desc);
			this.variableGroups = variableGroups;
		}

		public DescriptionPattern(Relation newRelation, Edu.Stanford.Nlp.Trees.Tregex.DescriptionPattern oldPattern)
		{
			this.rel = newRelation;
			this.negDesc = oldPattern.negDesc;
			this.isLink = oldPattern.isLink;
			this.linkedName = oldPattern.linkedName;
			this.stringDesc = oldPattern.stringDesc;
			this.descriptionMode = oldPattern.descriptionMode;
			this.descPattern = oldPattern.descPattern;
			this.exactMatch = oldPattern.exactMatch;
			this.stringFilter = oldPattern.stringFilter;
			this.name = oldPattern.name;
			this.SetChild(oldPattern.child);
			this.basicCatFunction = oldPattern.basicCatFunction;
			this.variableGroups = oldPattern.variableGroups;
		}

		internal override string LocalString()
		{
			return rel.ToString() + ' ' + (negDesc ? "!" : string.Empty) + (basicCatFunction != null ? "@" : string.Empty) + stringDesc + (name == null ? string.Empty : '=' + name);
		}

		public override string ToString()
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
			sb.Append(rel.ToString());
			sb.Append(' ');
			if (child != null)
			{
				sb.Append('(');
			}
			if (negDesc)
			{
				sb.Append('!');
			}
			if (basicCatFunction != null)
			{
				sb.Append('@');
			}
			sb.Append(stringDesc);
			if (isLink)
			{
				sb.Append('~');
				sb.Append(linkedName);
			}
			if (name != null)
			{
				sb.Append('=');
				sb.Append(name);
			}
			sb.Append(' ');
			if (child != null)
			{
				sb.Append(child.ToString());
				sb.Append(')');
			}
			return sb.ToString();
		}

		public virtual void SetChild(TregexPattern n)
		{
			child = n;
		}

		internal override IList<TregexPattern> GetChildren()
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

		internal override TregexMatcher Matcher(Tree root, Tree tree, IdentityHashMap<Tree, Tree> nodesToParents, IDictionary<string, Tree> namesToNodes, VariableStrings variableStrings, IHeadFinder headFinder)
		{
			return new DescriptionPattern.DescriptionMatcher(this, root, tree, nodesToParents, namesToNodes, variableStrings, headFinder);
		}

		private class DescriptionMatcher : TregexMatcher
		{
			private IEnumerator<Tree> treeNodeMatchCandidateIterator;

			private readonly DescriptionPattern myNode;

			private TregexMatcher childMatcher;

			private Tree nextTreeNodeMatchCandidate;

			private bool finished = false;

			private bool matchedOnce = false;

			private bool committedVariables = false;

			public DescriptionMatcher(DescriptionPattern n, Tree root, Tree tree, IdentityHashMap<Tree, Tree> nodesToParents, IDictionary<string, Tree> namesToNodes, VariableStrings variableStrings, IHeadFinder headFinder)
				: base(root, tree, nodesToParents, namesToNodes, variableStrings, headFinder)
			{
				// TODO: Why is this a static class with a pointer to the containing
				// class?  There seems to be no reason for such a thing.
				// cdm: agree: It seems like it should just be a non-static inner class.  Try this and check it works....
				// a DescriptionMatcher only has a single child; if it is the left
				// side of multiple relations, a CoordinationMatcher is used.
				// childMatcher is null until the first time a matcher needs to check the child 
				// myNode.child == null OR resetChild has never been called
				// the Tree node that this DescriptionMatcher node is trying to match on.
				// when finished = true, it means I have exhausted my potential tree node match candidates.
				myNode = n;
			}

			// no need to reset anything - everything starts out as null or false.  
			// lazy initialization of children to save time.
			// resetChildIter();
			internal override void ResetChildIter()
			{
				DecommitVariableGroups();
				RemoveNamedNodes();
				// lazy initialization saves quite a bit of time in use cases
				// where we call something other than matches()
				treeNodeMatchCandidateIterator = null;
				finished = false;
				nextTreeNodeMatchCandidate = null;
				if (childMatcher != null)
				{
					// need to tell the children to clean up any preexisting data
					childMatcher.ResetChildIter();
				}
			}

			private void ResetChild()
			{
				if (childMatcher == null)
				{
					if (myNode.child == null)
					{
						matchedOnce = false;
					}
				}
				else
				{
					childMatcher.ResetChildIter(nextTreeNodeMatchCandidate);
				}
			}

			/* goes to the next node in the tree that is a successful match to my description pattern.
			* This is the hotspot method in running tregex, but not clear how to make it faster. */
			// when finished = false; break; is called, it means I successfully matched.
			private void GoToNextTreeNodeMatch()
			{
				DecommitVariableGroups();
				// make sure variable groups are free.
				RemoveNamedNodes();
				// if we named a node, it should now be unnamed
				finished = true;
				Matcher m = null;
				string value = null;
				if (treeNodeMatchCandidateIterator == null)
				{
					treeNodeMatchCandidateIterator = myNode.rel.SearchNodeIterator(tree, this);
				}
				while (treeNodeMatchCandidateIterator.MoveNext())
				{
					nextTreeNodeMatchCandidate = treeNodeMatchCandidateIterator.Current;
					if (myNode.descriptionMode == null)
					{
						// this is a backreference or link
						if (myNode.isLink)
						{
							Tree otherTree = namesToNodes[myNode.linkedName];
							if (otherTree != null)
							{
								string otherValue = myNode.basicCatFunction == null ? otherTree.Value() : myNode.basicCatFunction.Apply(otherTree.Value());
								string myValue = myNode.basicCatFunction == null ? nextTreeNodeMatchCandidate.Value() : myNode.basicCatFunction.Apply(nextTreeNodeMatchCandidate.Value());
								if (otherValue.Equals(myValue))
								{
									finished = false;
									break;
								}
							}
						}
						else
						{
							if (namesToNodes[myNode.name] == nextTreeNodeMatchCandidate)
							{
								finished = false;
								break;
							}
						}
					}
					else
					{
						// try to match the description pattern.
						// cdm: Nov 2006: Check for null label, just make found false
						// String value = (myNode.basicCatFunction == null ? nextTreeNodeMatchCandidate.value() : myNode.basicCatFunction.apply(nextTreeNodeMatchCandidate.value()));
						// m = myNode.descPattern.matcher(value);
						// boolean found = m.find();
						bool found;
						value = nextTreeNodeMatchCandidate.Value();
						if (value == null)
						{
							found = false;
						}
						else
						{
							if (myNode.basicCatFunction != null)
							{
								value = myNode.basicCatFunction.Apply(value);
							}
							switch (myNode.descriptionMode)
							{
								case DescriptionPattern.DescriptionMode.Exact:
								{
									found = value.Equals(myNode.exactMatch);
									break;
								}

								case DescriptionPattern.DescriptionMode.Pattern:
								{
									m = myNode.descPattern.Matcher(value);
									found = m.Find();
									break;
								}

								case DescriptionPattern.DescriptionMode.Anything:
								{
									found = true;
									break;
								}

								case DescriptionPattern.DescriptionMode.Strings:
								{
									found = myNode.stringFilter.Test(value);
									break;
								}

								default:
								{
									throw new ArgumentException("Unexpected match mode");
								}
							}
						}
						if (found)
						{
							foreach (Pair<int, string> varGroup in myNode.variableGroups)
							{
								// if variables have been captured from a regex, they must match any previous matchings
								string thisVariable = varGroup.Second();
								string thisVarString = variableStrings.GetString(thisVariable);
								if (m != null)
								{
									if (thisVarString != null && !thisVarString.Equals(m.Group(varGroup.First())))
									{
										// failed to match a variable
										found = false;
										break;
									}
								}
								else
								{
									if (thisVarString != null && !thisVarString.Equals(value))
									{
										// here we treat any variable group # as a match
										found = false;
										break;
									}
								}
							}
						}
						if (found != myNode.negDesc)
						{
							finished = false;
							break;
						}
					}
				}
				if (!finished)
				{
					// I successfully matched.
					ResetChild();
					// reset my unique TregexMatcher child based on the Tree node I successfully matched at.
					// cdm bugfix jul 2009: on next line need to check for descPattern not null, or else this is a backreference or a link to an already named node, and the map should _not_ be updated
					if ((myNode.descriptionMode != null || myNode.isLink) && myNode.name != null)
					{
						// note: have to fill in the map as we go for backreferencing
						namesToNodes[myNode.name] = nextTreeNodeMatchCandidate;
					}
					if (m != null)
					{
						// commit variable groups using a matcher, meaning
						// it extracts the expressions from that matcher
						CommitVariableGroups(m);
					}
					else
					{
						if (value != null)
						{
							// commit using a set string (all groups are treated as the string)
							CommitVariableGroups(value);
						}
					}
				}
			}

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

			private void CommitVariableGroups(string value)
			{
				committedVariables = true;
				foreach (Pair<int, string> varGroup in myNode.variableGroups)
				{
					variableStrings.SetVar(varGroup.Second(), value);
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

			private void RemoveNamedNodes()
			{
				if ((myNode.descriptionMode != null || myNode.isLink) && myNode.name != null)
				{
					Sharpen.Collections.Remove(namesToNodes, myNode.name);
				}
			}

			/* tries to match the unique child of the DescriptionPattern node to a Tree node.  Returns "true" if succeeds.*/
			private bool MatchChild()
			{
				// entering here (given that it's called only once in matches())
				// we know finished is false, and either nextChild == null
				// (meaning goToNextChild has not been called) or nextChild exists
				// and has a label or backreference that matches
				if (nextTreeNodeMatchCandidate == null)
				{
					// I haven't been initialized yet, so my child certainly can't be matched yet.
					return false;
				}
				// lazy initialization of the child matcher
				if (childMatcher == null && myNode.child != null)
				{
					childMatcher = myNode.child.Matcher(root, nextTreeNodeMatchCandidate, nodesToParents, namesToNodes, variableStrings, headFinder);
				}
				//childMatcher.resetChildIter();
				if (childMatcher == null)
				{
					if (!matchedOnce)
					{
						matchedOnce = true;
						return true;
					}
					return false;
				}
				return childMatcher.Matches();
			}

			// find the next local match
			public override bool Matches()
			{
				// this is necessary so that a negated/optional node matches only once
				if (finished)
				{
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
							return true;
						}
					}
					else
					{
						GoToNextTreeNodeMatch();
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
					DecommitVariableGroups();
					RemoveNamedNodes();
					nextTreeNodeMatchCandidate = null;
					// didn't match, but return true anyway if optional
					return myNode.IsOptional();
				}
			}

			public override Tree GetMatch()
			{
				return nextTreeNodeMatchCandidate;
			}
		}

		private const long serialVersionUID = 1179819056757295757L;
		// end class DescriptionMatcher
	}
}
