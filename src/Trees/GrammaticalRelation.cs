// Stanford Dependencies - Code for producing and using Stanford dependencies.
// Copyright © 2005-2014 The Board of Trustees of
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
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    parser-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/stanford-dependencies.shtml
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// <c>GrammaticalRelation</c>
	/// is used to define a
	/// standardized, hierarchical set of grammatical relations,
	/// together with patterns for identifying them in
	/// parse trees.
	/// Each
	/// <c>GrammaticalRelation</c>
	/// has:
	/// <ul>
	/// <li>A
	/// <c>String</c>
	/// short name, which should be a lowercase
	/// abbreviation of some kind (in the fure mainly Universal Dependency names).</li>
	/// <li>A
	/// <c>String</c>
	/// long name, which should be descriptive.</li>
	/// <li>A parent in the
	/// <c>GrammaticalRelation</c>
	/// hierarchy.</li>
	/// <li>A
	/// <see cref="Java.Util.Regex.Pattern"/>
	/// 
	/// <c>Pattern</c>
	/// } called
	/// <c>sourcePattern</c>
	/// which matches (parent) nodes from which
	/// this
	/// <c>GrammaticalRelation</c>
	/// could hold.  (Note: this is done
	/// with the Java regex Pattern
	/// <c>matches()</c>
	/// predicate. The pattern
	/// must match the
	/// whole node name, and
	/// <c>^</c>
	/// or
	/// <c>$</c>
	/// aren't needed.
	/// Tregex constructions like __ do not work. Use ".*" to be applicable
	/// at all nodes. This prefiltering is used for efficiency.)</li>
	/// <li>A list of zero or more
	/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
	/// {
	/// <c>TregexPattern</c>
	/// s} called
	/// <c>targetPatterns</c>
	/// ,
	/// which describe the local tree structure which must hold between
	/// the source node and a target node for the
	/// <c>GrammaticalRelation</c>
	/// to apply. (Note:
	/// <c>tregex</c>
	/// regular expressions match with the
	/// <c>find()</c>
	/// method, while
	/// literal string label descriptions that are not regular expressions must
	/// be
	/// <c>equals()</c>
	/// .)</li>
	/// </ul>
	/// The
	/// <c>targetPatterns</c>
	/// associated
	/// with a
	/// <c>GrammaticalRelation</c>
	/// are designed as follows.
	/// In order to recognize a grammatical relation X holding between
	/// nodes A and B in a parse tree, we want to associate with
	/// <c>GrammaticalRelation</c>
	/// X a
	/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
	/// {
	/// <c>TregexPattern</c>
	/// } such that:
	/// <ul>
	/// <li>the root of the pattern matches A, and</li>
	/// <li>the pattern includes a node labeled "target", which matches B.</li>
	/// </ul>
	/// For example, for the grammatical relation
	/// <c>PREDICATE</c>
	/// which holds between a clause and its primary verb phrase, we might
	/// want to use the pattern
	/// <c>"S &lt; VP=target"</c>
	/// , in which the
	/// root will match a clause and the node labeled
	/// <c>"target"</c>
	/// will match the verb phrase.<p>
	/// For a given grammatical relation, the method
	/// <see cref="GetRelatedNodes(TreeGraphNode, TreeGraphNode, IHeadFinder)"/>
	/// 
	/// <c>getRelatedNodes()</c>
	/// }
	/// takes a
	/// <c>Tree</c>
	/// node as an argument and attempts to
	/// return other nodes which have this grammatical relation to the
	/// argument node.  By default, this method operates as follows: it
	/// steps through the patterns in the pattern list, trying to match
	/// each pattern against the argument node, until it finds some
	/// matches.  If a pattern matches, all matching nodes (that is, each
	/// node which corresponds to node label "target" in some match) are
	/// returned as a list; otherwise the next pattern is tried.<p>
	/// For some grammatical relations, we need more sophisticated logic to
	/// identify related nodes.  In such cases,
	/// <see cref="GetRelatedNodes(TreeGraphNode, TreeGraphNode, IHeadFinder)"/>
	/// 
	/// <c>getRelatedNodes()</c>
	/// }
	/// can be overridden on a per-relation basis using anonymous subclassing.<p>
	/// </summary>
	/// <seealso cref="GrammaticalStructure"/>
	/// <seealso cref="EnglishGrammaticalStructure"/>
	/// <seealso cref="EnglishGrammaticalRelations"/>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseGrammaticalRelations"/>
	/// <author>Bill MacCartney</author>
	/// <author>Galen Andrew (refactoring English-specific stuff)</author>
	/// <author>Ilya Sherman (refactoring annotation-relation pairing, which is now gone)</author>
	[System.Serializable]
	public class GrammaticalRelation : IComparable<Edu.Stanford.Nlp.Trees.GrammaticalRelation>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.GrammaticalRelation));

		private const long serialVersionUID = 892618003417550128L;

		private static readonly bool Debug = Runtime.GetProperty("GrammaticalRelation", null) != null;

		private static readonly EnumMap<Language, IDictionary<string, Edu.Stanford.Nlp.Trees.GrammaticalRelation>> stringsToRelations = new EnumMap<Language, IDictionary<string, Edu.Stanford.Nlp.Trees.GrammaticalRelation>>(typeof(Language));

		/// <summary>
		/// The "governor" grammatical relation, which is the inverse of "dependent".<p>
		/// <p/>
		/// Example: "the red car" &rarr;
		/// <c>gov</c>
		/// (red, car)
		/// </summary>
		public static readonly Edu.Stanford.Nlp.Trees.GrammaticalRelation Governor = new Edu.Stanford.Nlp.Trees.GrammaticalRelation(Language.Any, "gov", "governor", null);

		/// <summary>
		/// The "dependent" grammatical relation, which is the inverse of "governor".<p>
		/// <p/>
		/// Example: "the red car" &rarr;
		/// <c>dep</c>
		/// (car, red)
		/// </summary>
		public static readonly Edu.Stanford.Nlp.Trees.GrammaticalRelation Dependent = new Edu.Stanford.Nlp.Trees.GrammaticalRelation(Language.Any, "dep", "dependent", null);

		/// <summary>The "root" grammatical relation between a faked "ROOT" node, and the root of the sentence.</summary>
		public static readonly Edu.Stanford.Nlp.Trees.GrammaticalRelation Root = new Edu.Stanford.Nlp.Trees.GrammaticalRelation(Language.Any, "root", "root", null);

		/// <summary>Dummy relation, used while collapsing relations, e.g., in English &amp; Chinese GrammaticalStructure</summary>
		public static readonly Edu.Stanford.Nlp.Trees.GrammaticalRelation Kill = new Edu.Stanford.Nlp.Trees.GrammaticalRelation(Language.Any, "KILL", "dummy relation kill", null);

		/// <summary>
		/// Returns the GrammaticalRelation having the given string
		/// representation (e.g.
		/// </summary>
		/// <remarks>
		/// Returns the GrammaticalRelation having the given string
		/// representation (e.g. "nsubj"), or null if no such is found.
		/// </remarks>
		/// <param name="s">The short name of the GrammaticalRelation</param>
		/// <param name="values">The set of GrammaticalRelations to look for it among.</param>
		/// <returns>The GrammaticalRelation with that name</returns>
		public static Edu.Stanford.Nlp.Trees.GrammaticalRelation ValueOf(string s, ICollection<Edu.Stanford.Nlp.Trees.GrammaticalRelation> values, ILock readValuesLock)
		{
			readValuesLock.Lock();
			try
			{
				foreach (Edu.Stanford.Nlp.Trees.GrammaticalRelation reln in values)
				{
					if (reln.ToString().Equals(s))
					{
						return reln;
					}
				}
			}
			finally
			{
				readValuesLock.Unlock();
			}
			return null;
		}

		/// <summary>
		/// Returns the GrammaticalRelation having the given string
		/// representation (e.g.
		/// </summary>
		/// <remarks>
		/// Returns the GrammaticalRelation having the given string
		/// representation (e.g. "nsubj"), or null if no such is found.
		/// </remarks>
		/// <param name="s">The short name of the GrammaticalRelation</param>
		/// <param name="map">The map from string to GrammaticalRelation</param>
		/// <returns>The GrammaticalRelation with that name</returns>
		public static Edu.Stanford.Nlp.Trees.GrammaticalRelation ValueOf(string s, IDictionary<string, Edu.Stanford.Nlp.Trees.GrammaticalRelation> map)
		{
			if (map.Contains(s))
			{
				return map[s];
			}
			return null;
		}

		/// <summary>
		/// Convert from a String representation of a GrammaticalRelation to a
		/// GrammaticalRelation.
		/// </summary>
		/// <remarks>
		/// Convert from a String representation of a GrammaticalRelation to a
		/// GrammaticalRelation.  Where possible, you should avoid using this
		/// method and simply work with true GrammaticalRelations rather than
		/// String representations.  Correct behavior of this method depends
		/// on the underlying data structure resources used being kept in sync
		/// with the toString() and equals() methods.  However, there is really
		/// no choice but to use this method when storing GrammaticalRelations
		/// to text files and then reading them back in, so this method is not
		/// deprecated.
		/// </remarks>
		/// <param name="s">The String representation of a GrammaticalRelation</param>
		/// <returns>The grammatical relation represented by this String</returns>
		public static Edu.Stanford.Nlp.Trees.GrammaticalRelation ValueOf(Language language, string s)
		{
			Edu.Stanford.Nlp.Trees.GrammaticalRelation reln;
			lock (stringsToRelations)
			{
				reln = (stringsToRelations[language] != null ? ValueOf(s, stringsToRelations[language]) : null);
			}
			if (reln == null)
			{
				// TODO this breaks the hierarchical structure of the classes,
				//      but it makes English relations that much likelier to work.
				reln = UniversalEnglishGrammaticalRelations.ValueOf(s);
			}
			if (reln == null)
			{
				// the block below fails when 'specific' includes underscores.
				// this is possible on weird web text, which generates relations such as prep______
				/*
				String[] names = s.split("_");
				String specific = names.length > 1? names[1] : null;
				reln = new GrammaticalRelation(language, names[0], null, null, null, specific);
				*/
				string name;
				string specific;
				char separator = language == Language.UniversalEnglish ? ':' : '_';
				int underscorePosition = s.IndexOf(separator);
				if (underscorePosition > 0)
				{
					name = Sharpen.Runtime.Substring(s, 0, underscorePosition);
					specific = Sharpen.Runtime.Substring(s, underscorePosition + 1);
				}
				else
				{
					name = s;
					specific = null;
				}
				reln = new Edu.Stanford.Nlp.Trees.GrammaticalRelation(language, name, null, null, specific);
			}
			return reln;
		}

		public static Edu.Stanford.Nlp.Trees.GrammaticalRelation ValueOf(string s)
		{
			return ValueOf(Language.Any, s);
		}

		/// <summary>
		/// This function is used to determine whether the GrammaticalRelation in
		/// question is one that was created to be a thin wrapper around a String
		/// representation by valueOf(String), or whether it is a full-fledged
		/// GrammaticalRelation created by direct invocation of the constructor.
		/// </summary>
		/// <returns>Whether this relation is just a wrapper created by valueOf(String)</returns>
		public virtual bool IsFromString()
		{
			return longName == null;
		}

		private readonly Language language;

		private readonly string shortName;

		private readonly string longName;

		private readonly Edu.Stanford.Nlp.Trees.GrammaticalRelation parent;

		private readonly IList<Edu.Stanford.Nlp.Trees.GrammaticalRelation> children = new List<Edu.Stanford.Nlp.Trees.GrammaticalRelation>();

		private readonly Pattern sourcePattern;

		private readonly IList<TregexPattern> targetPatterns = new List<TregexPattern>();

		private readonly string specific;

		private GrammaticalRelation(Language language, string shortName, string longName, Edu.Stanford.Nlp.Trees.GrammaticalRelation parent, string sourcePattern, TregexPatternCompiler tregexCompiler, string[] targetPatterns, string specificString)
		{
			/* Non-static stuff */
			// a regexp for node values at which this relation can hold
			// to hold the specific prep or conjunction associated with the grammatical relation
			// TODO document constructor
			// TODO change to put specificString after longName, and then use String... for targetPatterns
			this.language = language;
			this.shortName = shortName;
			this.longName = longName;
			this.parent = parent;
			this.specific = specificString;
			// this can be null!
			if (parent != null)
			{
				parent.AddChild(this);
			}
			if (sourcePattern != null)
			{
				try
				{
					this.sourcePattern = Pattern.Compile(sourcePattern);
				}
				catch (PatternSyntaxException)
				{
					throw new Exception("Bad pattern: " + sourcePattern);
				}
			}
			else
			{
				this.sourcePattern = null;
			}
			foreach (string pattern in targetPatterns)
			{
				try
				{
					TregexPattern p = tregexCompiler.Compile(pattern);
					this.targetPatterns.Add(p);
				}
				catch (TregexParseException pe)
				{
					throw new Exception("Bad pattern: " + pattern, pe);
				}
			}
			Edu.Stanford.Nlp.Trees.GrammaticalRelation previous;
			lock (stringsToRelations)
			{
				IDictionary<string, Edu.Stanford.Nlp.Trees.GrammaticalRelation> sToR = stringsToRelations[language];
				if (sToR == null)
				{
					sToR = Generics.NewHashMap();
					stringsToRelations[language] = sToR;
				}
				previous = sToR[ToString()] = this;
			}
			if (previous != null)
			{
				if (!previous.IsFromString() && !IsFromString())
				{
					throw new ArgumentException("There is already a relation named " + ToString() + '!');
				}
			}
		}

		public GrammaticalRelation(Language language, string shortName, string longName, Edu.Stanford.Nlp.Trees.GrammaticalRelation parent, string sourcePattern, TregexPatternCompiler tregexCompiler, params string[] targetPatterns)
			: this(language, shortName, longName, parent, sourcePattern, tregexCompiler, targetPatterns, null)
		{
		}

		public GrammaticalRelation(Language language, string shortName, string longName, Edu.Stanford.Nlp.Trees.GrammaticalRelation parent)
			: this(language, shortName, longName, parent, null, null, StringUtils.EmptyStringArray, null)
		{
		}

		public GrammaticalRelation(Language language, string shortName, string longName, Edu.Stanford.Nlp.Trees.GrammaticalRelation parent, string specificString)
			: this(language, shortName, longName, parent, null, null, StringUtils.EmptyStringArray, specificString)
		{
		}

		/* We get here if we previously just built a fake relation from a string
		* we previously read in from a file.
		*/
		// TODO is it worth copying all of the information from this real
		//      relation into the old fake one?
		// This is the main constructor used
		// Used for non-leaf relations with no patterns
		// used to create collapsed relations with specificString
		private void AddChild(Edu.Stanford.Nlp.Trees.GrammaticalRelation child)
		{
			children.Add(child);
		}

		/// <summary>
		/// Given a
		/// <c>Tree</c>
		/// node
		/// <paramref name="t"/>
		/// , attempts to
		/// return a list of nodes to which node
		/// <paramref name="t"/>
		/// has this
		/// grammatical relation, with
		/// <paramref name="t"/>
		/// as the governor.
		/// </summary>
		/// <param name="t">Target for finding dependents of t related by this GR</param>
		/// <param name="root">The root of the Tree</param>
		/// <returns>A Collection of dependent nodes to which t bears this GR</returns>
		public virtual ICollection<TreeGraphNode> GetRelatedNodes(TreeGraphNode t, TreeGraphNode root, IHeadFinder headFinder)
		{
			ICollection<TreeGraphNode> nodeList = new ArraySet<TreeGraphNode>();
			foreach (TregexPattern p in targetPatterns)
			{
				// cdm: I deleted: && nodeList.isEmpty()
				// Initialize the TregexMatcher with the HeadFinder so that we
				// can use the same HeadFinder through the entire process of
				// building the dependencies
				TregexMatcher m = p.Matcher(root, headFinder);
				while (m.FindAt(t))
				{
					TreeGraphNode target = (TreeGraphNode)m.GetNode("target");
					if (target == null)
					{
						throw new AssertionError("Expression has no target: " + p);
					}
					nodeList.Add(target);
					if (Debug)
					{
						log.Info("found " + this + "(" + t + "-" + t.HeadWordNode() + ", " + m.GetNode("target") + "-" + ((TreeGraphNode)m.GetNode("target")).HeadWordNode() + ") using pattern " + p);
						foreach (string nodeName in m.GetNodeNames())
						{
							if (nodeName.Equals("target"))
							{
								continue;
							}
							log.Info("  node " + nodeName + ": " + m.GetNode(nodeName));
						}
					}
				}
			}
			return nodeList;
		}

		/// <summary>
		/// Returns
		/// <see langword="true"/>
		/// iff the value of
		/// <c>Tree</c>
		/// node
		/// <paramref name="t"/>
		/// matches the
		/// <c>sourcePattern</c>
		/// for
		/// this
		/// <c>GrammaticalRelation</c>
		/// , indicating that this
		/// <c>GrammaticalRelation</c>
		/// is one that could hold between
		/// <c>Tree</c>
		/// node
		/// <paramref name="t"/>
		/// and some other node.
		/// </summary>
		public virtual bool IsApplicable(Tree t)
		{
			// log.info("Testing whether " + sourcePattern + " matches " + ((TreeGraphNode) t).toOneLineString());
			return (sourcePattern != null) && (t.Value() != null) && sourcePattern.Matcher(t.Value()).Matches();
		}

		/// <summary>Returns whether this is equal to or an ancestor of gr in the grammatical relations hierarchy.</summary>
		public virtual bool IsAncestor(Edu.Stanford.Nlp.Trees.GrammaticalRelation gr)
		{
			while (gr != null)
			{
				// Changed this test from this == gr (mrsmith)
				if (this.Equals(gr))
				{
					return true;
				}
				gr = gr.parent;
			}
			return false;
		}

		/// <summary>
		/// Returns short name (abbreviation) for this
		/// <c>GrammaticalRelation</c>
		/// .  toString() for collapsed
		/// relations will include the word that was collapsed.
		/// <br/>
		/// <i>Implementation note:</i> Note that this method must be synced with
		/// the equals() and valueOf(String) methods
		/// </summary>
		public sealed override string ToString()
		{
			if (specific == null)
			{
				return shortName;
			}
			else
			{
				char sep = language == Language.UniversalEnglish ? ':' : '_';
				return shortName + sep + specific;
			}
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this
		/// <c>GrammaticalRelation</c>
		/// and the hierarchy below
		/// it, with one node per line, indented according to level.
		/// </summary>
		/// <returns>
		/// 
		/// <c>String</c>
		/// representation of this
		/// <c>GrammaticalRelation</c>
		/// </returns>
		public virtual string ToPrettyString()
		{
			StringBuilder buf = new StringBuilder("\n");
			ToPrettyString(0, buf);
			return buf.ToString();
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this
		/// <c>GrammaticalRelation</c>
		/// and the hierarchy below
		/// it, with one node per line, indented according to
		/// <paramref name="indentLevel"/>
		/// .
		/// </summary>
		/// <param name="indentLevel">how many levels to indent (0 for root node)</param>
		private void ToPrettyString(int indentLevel, StringBuilder buf)
		{
			for (int i = 0; i < indentLevel; i++)
			{
				buf.Append("  ");
			}
			buf.Append(shortName).Append(" (").Append(longName).Append("): ").Append(targetPatterns);
			foreach (Edu.Stanford.Nlp.Trees.GrammaticalRelation child in children)
			{
				buf.Append('\n');
				child.ToPrettyString(indentLevel + 1, buf);
			}
		}

		/// <summary>
		/// Grammatical relations are equal with other grammatical relations if they
		/// have the same shortName and specific (if present).
		/// </summary>
		/// <remarks>
		/// Grammatical relations are equal with other grammatical relations if they
		/// have the same shortName and specific (if present).
		/// <i>Implementation note:</i> Note that this method must be synced with
		/// the toString() and valueOf(String) methods
		/// </remarks>
		/// <param name="o">Object to be compared</param>
		/// <returns>Whether equal</returns>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o is string)
			{
				// TODO: Remove this. It's broken but was meant to cover legacy code. It would be correct to just return false.
				Sharpen.Runtime.PrintStackTrace(new Exception("Warning: comparing GrammaticalRelation to String"));
				return this.ToString().Equals(o);
			}
			if (!(o is Edu.Stanford.Nlp.Trees.GrammaticalRelation))
			{
				return false;
			}
			Edu.Stanford.Nlp.Trees.GrammaticalRelation gr = (Edu.Stanford.Nlp.Trees.GrammaticalRelation)o;
			// == okay for language as enum!
			// TODO(gabor) perhaps Language.Any shouldn't be equal to any language? This is a bit of a hack around some dependencies caring about language and others not.
			return (this.language.CompatibleWith(gr.language)) && this.shortName.Equals(gr.shortName) && (this.specific == gr.specific || (this.specific != null && this.specific.Equals(gr.specific)));
		}

		public override int GetHashCode()
		{
			int result = 17;
			result = 29 * result + (language != null ? language.ToString().GetHashCode() : 0);
			result = 29 * result + (shortName != null ? shortName.GetHashCode() : 0);
			result = 29 * result + (specific != null ? specific.GetHashCode() : 0);
			return result;
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Trees.GrammaticalRelation o)
		{
			string thisN = this.ToString();
			string oN = o.ToString();
			return string.CompareOrdinal(thisN, oN);
		}

		public virtual string GetLongName()
		{
			return longName;
		}

		public virtual string GetShortName()
		{
			return shortName;
		}

		/// <summary>Get the language of the grammatical relation.</summary>
		public virtual Language GetLanguage()
		{
			return this.language;
		}

		public virtual string GetSpecific()
		{
			return specific;
		}

		/// <summary>
		/// When deserializing a GrammaticalRelation, it needs to be matched
		/// up with the existing singleton relation of the same type.
		/// </summary>
		/// <remarks>
		/// When deserializing a GrammaticalRelation, it needs to be matched
		/// up with the existing singleton relation of the same type.
		/// TODO: there are a bunch of things wrong with this.  For one
		/// thing, it's crazy slow, since it goes through all the existing
		/// relations in an array.  For another, it would be cleaner to have
		/// subclasses for the English and Chinese relations
		/// </remarks>
		/// <exception cref="Java.IO.ObjectStreamException"/>
		protected internal virtual object ReadResolve()
		{
			switch (language)
			{
				case Language.Any:
				{
					if (shortName.Equals(Governor.shortName))
					{
						return Governor;
					}
					else
					{
						if (shortName.Equals(Dependent.shortName))
						{
							return Dependent;
						}
						else
						{
							if (shortName.Equals(Root.shortName))
							{
								return Root;
							}
							else
							{
								if (shortName.Equals(Kill.shortName))
								{
									return Kill;
								}
								else
								{
									throw new Exception("Unknown general relation " + shortName);
								}
							}
						}
					}
					goto case Language.English;
				}

				case Language.English:
				{
					Edu.Stanford.Nlp.Trees.GrammaticalRelation rel = EnglishGrammaticalRelations.ValueOf(ToString());
					if (rel == null)
					{
						switch (shortName)
						{
							case "conj":
							{
								return EnglishGrammaticalRelations.GetConj(specific);
							}

							case "prep":
							{
								return EnglishGrammaticalRelations.GetPrep(specific);
							}

							case "prepc":
							{
								return EnglishGrammaticalRelations.GetPrepC(specific);
							}

							default:
							{
								// TODO: we need to figure out what to do with relations
								// which were serialized and then deprecated.  Perhaps there
								// is a good way to make them singletons
								return this;
							}
						}
					}
					else
					{
						//throw new RuntimeException("Unknown English relation " + this);
						return rel;
					}
					goto case Language.Chinese;
				}

				case Language.Chinese:
				{
					Edu.Stanford.Nlp.Trees.GrammaticalRelation rel = ChineseGrammaticalRelations.ValueOf(ToString());
					if (rel == null)
					{
						// TODO: we need to figure out what to do with relations
						// which were serialized and then deprecated.  Perhaps there
						// is a good way to make them singletons
						return this;
					}
					//throw new RuntimeException("Unknown Chinese relation " + this);
					return rel;
				}

				case Language.UniversalEnglish:
				{
					Edu.Stanford.Nlp.Trees.GrammaticalRelation rel_1 = UniversalEnglishGrammaticalRelations.ValueOf(ToString());
					if (rel_1 == null)
					{
						switch (shortName)
						{
							case "conj":
							{
								return UniversalEnglishGrammaticalRelations.GetConj(specific);
							}

							case "nmod":
							{
								return UniversalEnglishGrammaticalRelations.GetNmod(specific);
							}

							case "acl":
							{
								return UniversalEnglishGrammaticalRelations.GetAcl(specific);
							}

							case "advcl":
							{
								return UniversalEnglishGrammaticalRelations.GetAdvcl(specific);
							}

							default:
							{
								// TODO: we need to figure out what to do with relations
								// which were serialized and then deprecated.  Perhaps there
								// is a good way to make them singletons
								return this;
							}
						}
					}
					else
					{
						//throw new RuntimeException("Unknown English relation " + this);
						return rel_1;
					}
					goto default;
				}

				default:
				{
					throw new Exception("Unknown language " + language);
				}
			}
		}

		/// <summary>
		/// Returns the parent of this
		/// <c>GrammaticalRelation</c>
		/// .
		/// </summary>
		public virtual Edu.Stanford.Nlp.Trees.GrammaticalRelation GetParent()
		{
			return parent;
		}

		public static void Main(string[] args)
		{
			string[] names = new string[] { "dep", "pred", "prep_to", "rcmod" };
			foreach (string name in names)
			{
				Edu.Stanford.Nlp.Trees.GrammaticalRelation reln = ValueOf(Language.English, name);
				System.Console.Out.WriteLine("Data for GrammaticalRelation loaded as valueOf(\"" + name + "\"):");
				System.Console.Out.WriteLine("\tShort name:    " + reln.GetShortName());
				System.Console.Out.WriteLine("\tLong name:     " + reln.GetLongName());
				System.Console.Out.WriteLine("\tSpecific name: " + reln.GetSpecific());
			}
		}
	}
}
