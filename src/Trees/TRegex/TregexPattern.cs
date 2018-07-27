// TregexPattern -- a Tgrep2-style utility for recognizing patterns in trees.
// Tregex/Tsurgeon Distribution
// Copyright (c) 2003-2008, 2017 The Board of Trustees of
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
// along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
//
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: parser-user@lists.stanford.edu
//    Licensing: parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/tregex.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>
	/// A TregexPattern is a regular expression-like pattern that is designed to match node configurations within
	/// a Tree where the nodes are labeled with symbols, rather than a character string.
	/// </summary>
	/// <remarks>
	/// A TregexPattern is a regular expression-like pattern that is designed to match node configurations within
	/// a Tree where the nodes are labeled with symbols, rather than a character string.
	/// The Tregex language follows but slightly expands
	/// the tree pattern languages pioneered by
	/// <c>tgrep</c>
	/// and
	/// <c>tgrep2</c>
	/// . However, unlike these
	/// tree pattern matching systems, but like Unix
	/// <c>grep</c>
	/// , there is no pre-indexing of the data to be searched.
	/// Rather there is a linear scan through the trees where matches are sought.
	/// As a result, matching is slower, but a TregexPattern can be applied
	/// to an arbitrary set of trees at runtime in a processing pipeline without pre-indexing.
	/// TregexPattern instances can be matched against instances of the
	/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
	/// class.
	/// The
	/// <see cref="Main(string[])"/>
	/// method can be used to find matching nodes of a treebank from the command line.
	/// <h3>Getting Started</h3>
	/// Suppose we want to find all examples of subtrees where the label of
	/// the root of the subtree starts with MW and it has a child node with the label IN.
	/// That is, we want any subtree whose root is labeled MWV, MWN, etc. that has an IN child.
	/// The first thing to do is figure out what pattern to use.  Since we
	/// want to match anything starting with MW, we use a regular expression pattern
	/// for the top node and then also check for the child. The pattern is:
	/// <c>/^MW/ &lt; IN</c>
	/// .
	/// We then create a pattern, find matches in a given tree, and process
	/// those matches as follows:
	/// <blockquote>
	/// <code>
	/// // Create a reusable pattern object <br />
	/// TregexPattern patternMW = TregexPattern.compile("/^MW/ &lt; IN"); <br />
	/// // Run the pattern on one particular tree <br />
	/// TregexMatcher matcher = patternMW.matcher(tree); <br />
	/// // Iterate over all of the subtrees that matched <br />
	/// while (matcher.findNextMatchingNode()) { <br />
	/// &nbsp;&nbsp;Tree match = matcher.getMatch(); <br />
	/// &nbsp;&nbsp;// do what we want to do with the subtree <br />
	/// &nbsp;&nbsp;match.pennPrint();
	/// }
	/// </code>
	/// </blockquote>
	/// <h3>Tregex pattern language</h3>
	/// The currently supported node-node relations and their symbols are:
	/// <table border = "1">
	/// <tr><th>Symbol<th>Meaning
	/// <tr><td>A &lt;&lt; B <td>A dominates B
	/// <tr><td>A &gt;&gt; B <td>A is dominated by B
	/// <tr><td>A &lt; B <td>A immediately dominates B
	/// <tr><td>A &gt; B <td>A is immediately dominated by B
	/// <tr><td>A &#36; B <td>A is a sister of B (and not equal to B)
	/// <tr><td>A .. B <td>A precedes B
	/// <tr><td>A . B <td>A immediately precedes B
	/// <tr><td>A ,, B <td>A follows B
	/// <tr><td>A , B <td>A immediately follows B
	/// <tr><td>A &lt;&lt;, B <td>B is a leftmost descendant of A
	/// <tr><td>A &lt;&lt;- B <td>B is a rightmost descendant of A
	/// <tr><td>A &gt;&gt;, B <td>A is a leftmost descendant of B
	/// <tr><td>A &gt;&gt;- B <td>A is a rightmost descendant of B
	/// <tr><td>A &lt;, B <td>B is the first child of A
	/// <tr><td>A &gt;, B <td>A is the first child of B
	/// <tr><td>A &lt;- B <td>B is the last child of A
	/// <tr><td>A &gt;- B <td>A is the last child of B
	/// <tr><td>A &lt;` B <td>B is the last child of A
	/// <tr><td>A &gt;` B <td>A is the last child of B
	/// <tr><td>A &lt;i B <td>B is the ith child of A (i &gt; 0)
	/// <tr><td>A &gt;i B <td>A is the ith child of B (i &gt; 0)
	/// <tr><td>A &lt;-i B <td>B is the ith-to-last child of A (i &gt; 0)
	/// <tr><td>A &gt;-i B <td>A is the ith-to-last child of B (i &gt; 0)
	/// <tr><td>A &lt;: B <td>B is the only child of A
	/// <tr><td>A &gt;: B <td>A is the only child of B
	/// <tr><td>A &lt;&lt;: B <td>A dominates B via an unbroken chain (length &gt; 0) of unary local trees.
	/// <tr><td>A &gt;&gt;: B <td>A is dominated by B via an unbroken chain (length &gt; 0) of unary local trees.
	/// <tr><td>A &#36;++ B <td>A is a left sister of B (same as &#36;.. for context-free trees)
	/// <tr><td>A &#36;-- B <td>A is a right sister of B (same as &#36;,, for context-free trees)
	/// <tr><td>A &#36;+ B <td>A is the immediate left sister of B (same as &#36;. for context-free trees)
	/// <tr><td>A &#36;- B <td>A is the immediate right sister of B (same as &#36;, for context-free trees)
	/// <tr><td>A &#36;.. B <td>A is a sister of B and precedes B
	/// <tr><td>A &#36;,, B <td>A is a sister of B and follows B
	/// <tr><td>A &#36;. B <td>A is a sister of B and immediately precedes B
	/// <tr><td>A &#36;, B <td>A is a sister of B and immediately follows B
	/// <tr><td>A &lt;+(C) B <td>A dominates B via an unbroken chain of (zero or more) nodes matching description C
	/// <tr><td>A &gt;+(C) B <td>A is dominated by B via an unbroken chain of (zero or more) nodes matching description C
	/// <tr><td>A .+(C) B <td>A precedes B via an unbroken chain of (zero or more) nodes matching description C
	/// <tr><td>A ,+(C) B <td>A follows B via an unbroken chain of (zero or more) nodes matching description C
	/// <tr><td>A &lt;&lt;&#35; B <td>B is a head of phrase A
	/// <tr><td>A &gt;&gt;&#35; B <td>A is a head of phrase B
	/// <tr><td>A &lt;&#35; B <td>B is the immediate head of phrase A
	/// <tr><td>A &gt;&#35; B <td>A is the immediate head of phrase B
	/// <tr><td>A == B <td>A and B are the same node
	/// <tr><td>A &lt;= B <td>A and B are the same node or A is the parent of B
	/// <tr><td>A : B<td>[this is a pattern-segmenting operator that places no constraints on the relationship between A and B]
	/// <tr><td>A &lt;... { B ; C ; ... }<td>A has exactly B, C, etc as its subtree, with no other children.
	/// </table>
	/// Label descriptions can be literal strings, which much match labels
	/// exactly, or regular expressions in regular expression bars: /regex/.
	/// Literal string matching proceeds as String equality.
	/// In order to prevent ambiguity with other Tregex symbols, ASCII symbols (ASCII range characters that
	/// are not letters or digits) are
	/// not allowed in literal strings, and literal strings cannot begin with ASCII digits.
	/// (That is literals can be standard "identifiers" matching
	/// [a-zA-Z]([a-zA-Z0-9_-])* but also may include letters from other alphabets.)
	/// If you want to use other symbols, you can do so by using a regular
	/// expression instead of a literal string.
	/// A disjunctive list of literal strings can be given separated by '|'.
	/// The special string '__' (two underscores) can be used to match any
	/// node.  (WARNING!!  Use of the '__' node description may seriously
	/// slow down search.)  If a label description is preceded by '@', the
	/// label will match any node whose <em>basicCategory</em> matches the
	/// description.  <emph>NB: A single '@' thus scopes over a disjunction
	/// specified by '|': @NP|VP means things with basic category NP or VP.
	/// </emph> The basicCategory is defined according to a Function
	/// mapping Strings to Strings, as provided by
	/// <see cref="Edu.Stanford.Nlp.Trees.AbstractTreebankLanguagePack.GetBasicCategoryFunction()"/>
	/// .
	/// Note that Label description regular expressions are matched as
	/// <c>find()</c>
	/// ,
	/// as in Perl/tgrep, not as
	/// <c>matches()</c>
	/// ;
	/// you need to use
	/// <c>^</c>
	/// or
	/// <c>$</c>
	/// to constrain matches to
	/// the ends of strings.
	/// <b>Chains of relations have a special non-associative semantics:</b>
	/// In a chain of relations A op B op C ...,
	/// all relations are relative to the first node in
	/// the chain. For example,
	/// <c>(S &lt; VP &lt; NP)</c>
	/// means
	/// "an S over a VP and also over an NP".
	/// Nodes can be grouped using parentheses '(' and ')'
	/// as in
	/// <c>S &lt; (NP $++ VP)</c>
	/// to match an S
	/// over an NP, where the NP has a VP as a right sister.
	/// So, if instead what you want is an S above a VP above an NP, you must write
	/// "
	/// <c>S &lt; (VP &lt; NP)</c>
	/// ".
	/// <h3>Notes on relations</h3>
	/// Node
	/// <c>B</c>
	/// "follows" node
	/// <c>A</c>
	/// if
	/// <c>B</c>
	/// or one of its ancestors is a right sibling of
	/// <c>A</c>
	/// or one
	/// of its ancestors.  Node
	/// <c>B</c>
	/// "immediately follows" node
	/// <c>A</c>
	/// if
	/// <c>B</c>
	/// follows
	/// <c>A</c>
	/// and there
	/// is no node
	/// <c>C</c>
	/// such that
	/// <c>B</c>
	/// follows
	/// <c>C</c>
	/// and
	/// <c>C</c>
	/// follows
	/// <c>A</c>
	/// .
	/// Node
	/// <c>A</c>
	/// dominates
	/// <c>B</c>
	/// through an unbroken
	/// chain of unary local trees only if
	/// <c>A</c>
	/// is also
	/// unary.
	/// <c>(A (B))</c>
	/// is a valid example that matches
	/// <c>A &lt;&lt;: B</c>
	/// When specifying that nodes are dominated via an unbroken chain of
	/// nodes matching a description
	/// <c>C</c>
	/// , the description
	/// <c>C</c>
	/// cannot be a full Tregex expression, but only an
	/// expression specifying the name of the node.  Negation of this
	/// description is allowed.
	/// == has the same precedence as the other relations, so the expression
	/// <c>A &lt;&lt; B == A &lt;&lt; C</c>
	/// associates as
	/// <c>(((A &lt;&lt; B) == A) &lt;&lt; C)</c>
	/// , not as
	/// <c>((A &lt;&lt; B) == (A &lt;&lt; C))</c>
	/// .  (Both expressions are
	/// equivalent, of course, but this is just an example.)
	/// <h3>Boolean relational operators</h3>
	/// Relations can be combined using the '&' and '|' operators,
	/// negated with the '!' operator, and made optional with the '?' operator.
	/// Thus
	/// <c>(NP &lt; NN | &lt; NNS)</c>
	/// will match an NP node dominating either
	/// an NN or an NNS.
	/// <c>(NP &gt; S & $++ VP)</c>
	/// matches an NP that
	/// is both under an S and has a VP as a right sister.
	/// Expressions stop evaluating as soon as the result is known.  For
	/// example, if the pattern is
	/// <c>NP=a | NNP=b</c>
	/// and the NP
	/// matches, then variable
	/// <c>b</c>
	/// will not be assigned even if
	/// there is an NNP in the tree.
	/// Relations can be grouped using brackets '[' and ']'.  So the expression
	/// <blockquote>
	/// <c>NP [&lt; NN | &lt; NNS] & &gt; S</c>
	/// </blockquote>
	/// matches an NP that (1) dominates either an NN or an NNS, and (2) is under an S.  Without
	/// brackets, &amp; takes precedence over |, and equivalent operators are
	/// left-associative.  Also note that &amp; is the default combining operator if the
	/// operator is omitted in a chain of relations, so that the two patterns are equivalent:
	/// <blockquote>
	/// <c>(S &lt; VP &lt; NP)</c>
	/// <br />
	/// <c>(S &lt; VP & &lt; NP)</c>
	/// </blockquote>
	/// As another example,
	/// <c>(VP &lt; VV | &lt; NP % NP)</c>
	/// can be written explicitly as
	/// <c>(VP [&lt; VV | [&lt; NP & % NP] ] )</c>
	/// Relations can be negated with the '!' operator, in which case the
	/// expression will match only if there is no node satisfying the relation.
	/// For example
	/// <c>(NP !&lt; NNP)</c>
	/// matches only NPs not dominating
	/// an NNP.  Label descriptions can also be negated with '!': (NP &lt; !NNP|NNS) matches
	/// NPs dominating some node that is not an NNP or an NNS.
	/// Relations can be made optional with the '?' operator.  This way the
	/// expression will match even if the optional relation is not satisfied.  This is useful when used together
	/// with node naming (see below).
	/// <h3>Basic Categories</h3>
	/// In order to consider only the "basic category" of a tree label,
	/// i.e. to ignore functional tags or other annotations on the label,
	/// prefix that node's description with the
	/// <c>@</c>
	/// symbol.  For example
	/// <c>(@NP &lt; @/NN.?/)</c>
	/// This can only be used for individual nodes;
	/// if you want all nodes to use the basic category, it would be more efficient
	/// to use a
	/// <see cref="Edu.Stanford.Nlp.Trees.TreeNormalizer"/>
	/// to remove functional
	/// tags before passing the tree to the TregexPattern.
	/// <h3>Segmenting patterns</h3>
	/// The ":" operator allows you to segment a pattern into two pieces.  This can simplify your pattern writing.  For example,
	/// the pattern
	/// <blockquote>
	/// S : NP
	/// </blockquote>
	/// matches only those S nodes in trees that also have an NP node.
	/// <h3>Naming nodes</h3>
	/// Nodes can be given names (a.k.a. handles) using '='.  A named node will be stored in a
	/// map that maps names to nodes so that if a match is found, the node
	/// corresponding to the named node can be extracted from the map.  For
	/// example
	/// <c>(NP &lt; NNP=name)</c>
	/// will match an NP dominating an NNP
	/// and after a match is found, the map can be queried with the
	/// name to retreived the matched node using
	/// <see cref="TregexMatcher.GetNode(string)"/>
	/// with (String) argument "name" (<it>not</it> "=name").
	/// Note that you are not allowed to name a node that is under the scope of a negation operator (the semantics would
	/// be unclear, since you can't store a node that never gets matched to).
	/// Trying to do so will cause a
	/// <see cref="TregexParseException"/>
	/// to be thrown. Named nodes
	/// <it>can be put within the scope of an optionality operator</it>.
	/// Named nodes that refer back to previous named nodes need not have a node
	/// description -- this is known as "backreferencing".  In this case, the expression
	/// will match only when all instances of the same name get matched to the same tree node.
	/// For example: the pattern
	/// <blockquote>
	/// <c>(@NP &lt;, (@NP $+ (/,/ $+ (@NP $+ /,/=comma))) &lt;- =comma)</c>
	/// </blockquote>
	/// matches only an NP dominating exactly the four node sequence
	/// <c>NP , NP ,</c>
	/// -- the mother NP cannot have any other
	/// daughters. Multiple backreferences are allowed.  If the node w/ no
	/// node description does not refer to a previously named node, there
	/// will be no error, the expression simply will not match anything.
	/// Another way to refer to previously named nodes is with the "link" symbol: '~'.
	/// A link is like a backreference, except that instead of having to be <i>equal to</i> the
	/// referred node, the current node only has to match the label of the referred to node.
	/// A link cannot have a node description, i.e. the '~' symbol must immediately follow a
	/// relation symbol.
	/// <h3>Customizing headship and basic categories</h3>
	/// The HeadFinder used to determine heads for the head relations
	/// <c>&lt;#</c>
	/// ,
	/// <c>&gt;#</c>
	/// ,
	/// <c>&lt;&lt;#</c>
	/// ,
	/// and
	/// <c>&gt;&gt;#</c>
	/// , and also
	/// the Function mapping from labels to Basic Category tags can be
	/// chosen by using a
	/// <see cref="TregexPatternCompiler"/>
	/// .
	/// <h3>Variable Groups</h3>
	/// If you write a node description using a regular expression, you can assign its matching groups to variable names.
	/// If more than one node has a group assigned to the same variable name, then matching will only occur when all such groups
	/// capture the same string.  This is useful for enforcing coindexation constraints.  The syntax is
	/// <blockquote>
	/// <c>/ &lt;regex-stuff&gt; /#&lt;group-number&gt;%&lt;variable-name&gt;</c>
	/// </blockquote>
	/// For example, the pattern (designed for Penn Treebank trees)
	/// <blockquote>
	/// <c>@SBAR &lt; /^WH.*-([0-9]+)$/#1%index &lt;&lt; (__=empty &lt; (/^-NONE-/ &lt; /^\*T\*-([0-9]+)$/#1%index))</c>
	/// </blockquote>
	/// will match only such that the WH- node under the SBAR is coindexed with the trace node that gets the name
	/// <c>empty</c>
	/// .
	/// <h3>Current known bugs/shortcomings:</h3>
	/// <ul>
	/// <li> Tregex does not support disjunctions at the root level.  For
	/// example, the pattern
	/// <c>A | B</c>
	/// will not work.
	/// <li> Using multiple variable strings in one regex may not
	/// necessarily work.  For example, suppose the first two regex
	/// patterns are
	/// <c>/(.*)/#1%foo</c>
	/// and
	/// <c>/(.*)/#1%bar</c>
	/// .  You might then want to write a pattern
	/// that matches the concatenation of these patterns,
	/// <c>/(.*)(.*)/#1%foo#2%bar</c>
	/// , but that will not work.
	/// </ul>
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Roger Levy (rog@csli.stanford.edu)</author>
	/// <author>Anna Rafferty (filter mode)</author>
	/// <author>John Bauer (extensively tested and bugfixed)</author>
	[System.Serializable]
	public abstract class TregexPattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.TregexPattern));

		private bool neg;

		private bool opt;

		private string patternString;

		// = false;
		// = false;
		internal virtual void Negate()
		{
			neg = true;
			if (opt)
			{
				throw new Exception("Node cannot be both negated and optional.");
			}
		}

		internal virtual void MakeOptional()
		{
			opt = true;
			if (neg)
			{
				throw new Exception("Node cannot be both negated and optional.");
			}
		}

		private void PrettyPrint(PrintWriter pw, int indent)
		{
			for (int i = 0; i < indent; i++)
			{
				pw.Print("   ");
			}
			if (neg)
			{
				pw.Print('!');
			}
			if (opt)
			{
				pw.Print('?');
			}
			pw.Println(LocalString());
			foreach (Edu.Stanford.Nlp.Trees.Tregex.TregexPattern child in GetChildren())
			{
				child.PrettyPrint(pw, indent + 1);
			}
		}

		internal TregexPattern()
		{
		}

		// package private constructor
		internal abstract IList<Edu.Stanford.Nlp.Trees.Tregex.TregexPattern> GetChildren();

		internal abstract string LocalString();

		internal virtual bool IsNegated()
		{
			return neg;
		}

		internal virtual bool IsOptional()
		{
			return opt;
		}

		internal abstract TregexMatcher Matcher(Tree root, Tree tree, IdentityHashMap<Tree, Tree> nodesToParents, IDictionary<string, Tree> namesToNodes, VariableStrings variableStrings, IHeadFinder headFinder);

		/// <summary>
		/// Get a
		/// <see cref="TregexMatcher"/>
		/// for this pattern on this tree.
		/// </summary>
		/// <param name="t">a tree to match on</param>
		/// <returns>a TregexMatcher</returns>
		public virtual TregexMatcher Matcher(Tree t)
		{
			// In the assumption that there will usually be very few names in
			// the pattern, we use an ArrayMap instead of a hash map
			// TODO: it would be even more efficient if we set this to be exactly the right size
			return Matcher(t, t, null, ArrayMap.NewArrayMap(), new VariableStrings(), null);
		}

		/// <summary>
		/// Get a
		/// <see cref="TregexMatcher"/>
		/// for this pattern on this tree.  Any Relations which use heads of trees should use the provided HeadFinder.
		/// </summary>
		/// <param name="t">a tree to match on</param>
		/// <param name="headFinder">a HeadFinder to use when matching</param>
		/// <returns>a TregexMatcher</returns>
		public virtual TregexMatcher Matcher(Tree t, IHeadFinder headFinder)
		{
			return Matcher(t, t, null, ArrayMap.NewArrayMap(), new VariableStrings(), headFinder);
		}

		/// <summary>
		/// Creates a pattern from the given string using the default HeadFinder and
		/// BasicCategoryFunction.
		/// </summary>
		/// <remarks>
		/// Creates a pattern from the given string using the default HeadFinder and
		/// BasicCategoryFunction.  If you want to use a different HeadFinder or
		/// BasicCategoryFunction, use a
		/// <see cref="TregexPatternCompiler"/>
		/// object.
		/// </remarks>
		/// <param name="tregex">the pattern string</param>
		/// <returns>a TregexPattern for the string.</returns>
		/// <exception cref="TregexParseException">if the string does not parse</exception>
		public static Edu.Stanford.Nlp.Trees.Tregex.TregexPattern Compile(string tregex)
		{
			return TregexPatternCompiler.defaultCompiler.Compile(tregex);
		}

		/// <summary>
		/// Creates a pattern from the given string using the default HeadFinder and
		/// BasicCategoryFunction.
		/// </summary>
		/// <remarks>
		/// Creates a pattern from the given string using the default HeadFinder and
		/// BasicCategoryFunction.  If you want to use a different HeadFinder or
		/// BasicCategoryFunction, use a
		/// <see cref="TregexPatternCompiler"/>
		/// object.
		/// Rather than throwing an exception when the string does not parse,
		/// simply returns null.
		/// </remarks>
		/// <param name="tregex">the pattern string</param>
		/// <param name="verbose">whether to log errors when the string doesn't parse</param>
		/// <returns>a TregexPattern for the string, or null if the string does not parse.</returns>
		public static Edu.Stanford.Nlp.Trees.Tregex.TregexPattern SafeCompile(string tregex, bool verbose)
		{
			Edu.Stanford.Nlp.Trees.Tregex.TregexPattern result = null;
			try
			{
				result = TregexPatternCompiler.defaultCompiler.Compile(tregex);
			}
			catch (TregexParseException ex)
			{
				if (verbose)
				{
					log.Info("Could not parse " + tregex + ':');
					log.Info(ex);
				}
			}
			return result;
		}

		public virtual string Pattern()
		{
			return patternString;
		}

		/// <summary>Only used by the TregexPatternCompiler to set the pattern.</summary>
		/// <remarks>Only used by the TregexPatternCompiler to set the pattern. Pseudo-final.</remarks>
		internal virtual void SetPatternString(string patternString)
		{
			this.patternString = patternString;
		}

		/// <returns>A single-line string representation of the pattern</returns>
		public abstract override string ToString();

		/// <summary>
		/// Print a multi-line representation
		/// of the pattern illustrating it's syntax.
		/// </summary>
		public virtual void PrettyPrint(PrintWriter pw)
		{
			PrettyPrint(pw, 0);
		}

		/// <summary>
		/// Print a multi-line representation
		/// of the pattern illustrating it's syntax.
		/// </summary>
		public virtual void PrettyPrint(TextWriter ps)
		{
			PrettyPrint(new PrintWriter(new OutputStreamWriter(ps), true));
		}

		/// <summary>
		/// Print a multi-line representation of the pattern illustrating
		/// it's syntax to System.out.
		/// </summary>
		public virtual void PrettyPrint()
		{
			PrettyPrint(System.Console.Out);
		}

		private static readonly Java.Util.Regex.Pattern codePattern = Java.Util.Regex.Pattern.Compile("([0-9]+):([0-9]+)");

		private static void ExtractSubtrees(IList<string> codeStrings, string treeFile)
		{
			IList<Pair<int, int>> codes = new List<Pair<int, int>>();
			foreach (string s in codeStrings)
			{
				Java.Util.Regex.Matcher m = codePattern.Matcher(s);
				if (m.Matches())
				{
					codes.Add(new Pair<int, int>(System.Convert.ToInt32(m.Group(1)), System.Convert.ToInt32(m.Group(2))));
				}
				else
				{
					throw new Exception("Error: illegal node code " + s);
				}
			}
			ITreeReaderFactory trf = new TregexPattern.TRegexTreeReaderFactory();
			MemoryTreebank treebank = new MemoryTreebank(trf);
			treebank.LoadPath(treeFile, null, true);
			foreach (Pair<int, int> code in codes)
			{
				Tree t = treebank[code.First() - 1];
				t.GetNodeNumber(code.Second()).PennPrint();
			}
		}

		/// <summary>Prints out all matches of a tree pattern on each tree in the path.</summary>
		/// <remarks>
		/// Prints out all matches of a tree pattern on each tree in the path. Usage:
		/// <c>java edu.stanford.nlp.trees.tregex.TregexPattern [[-TCwfosnu] [-filter] [-h &lt;node-name&gt;]]* pattern filepath</c>
		/// Arguments:
		/// <ul>
		/// <li>
		/// <c>pattern</c>
		/// : the tree
		/// pattern which optionally names some set of nodes (i.e., gives it the "handle")
		/// <c>=name</c>
		/// (for some arbitrary
		/// string "name")
		/// <li>
		/// <c>filepath</c>
		/// : the path to files with trees. If this is a directory, there will be recursive descent and the pattern will be run on all files beneath the specified directory.
		/// </ul>
		/// Options:
		/// <ul>
		/// <li>
		/// <c>-C</c>
		/// suppresses printing of matches, so only the
		/// number of matches is printed.
		/// <li>
		/// <c>-w</c>
		/// causes the whole of a tree that matches to be printed.
		/// <li>
		/// <c>-f</c>
		/// causes the filename to be printed.
		/// <li>
		/// <c>-i &lt;filename&gt;</c>
		/// causes the pattern to be matched to be read from
		/// <c>&lt;filename&gt;</c>
		/// rather than the command line.  Don't specify a pattern when this option is used.
		/// <li>
		/// <c>-o</c>
		/// Specifies that each tree node can be reported only once as the root of a match (by default a node will
		/// be printed once for every <em>way</em> the pattern matches).
		/// <li>
		/// <c>-s</c>
		/// causes trees to be printed all on one line (by default they are pretty printed).
		/// <li>
		/// <c>-n</c>
		/// causes the number of the tree in which the match was found to be
		/// printed before every match.
		/// <li>
		/// <c>-u</c>
		/// causes only the label of each matching node to be printed, not complete subtrees.
		/// <li>
		/// <c>-t</c>
		/// causes only the yield (terminal words) of the selected node to be printed (or the yield of the whole tree, if the
		/// <c>-w</c>
		/// option is used).
		/// <li>
		/// <c>-encoding &lt;charset_encoding&gt;</c>
		/// option allows specification of character encoding of trees..
		/// <li>
		/// <c>-h &lt;node-handle&gt;</c>
		/// If a
		/// <c>-h</c>
		/// option is given, the root tree node will not be printed.  Instead,
		/// for each
		/// <c>node-handle</c>
		/// specified, the node matched and given that handle will be printed.  Multiple nodes can be printed by using the
		/// <c>-h</c>
		/// option multiple times on a single command line.
		/// <li>
		/// <c>-hf &lt;headfinder-class-name&gt;</c>
		/// use the specified
		/// <see cref="Edu.Stanford.Nlp.Trees.IHeadFinder"/>
		/// class to determine headship relations.
		/// <li>
		/// <c>-hfArg &lt;string&gt;</c>
		/// pass a string argument in to the
		/// <see cref="Edu.Stanford.Nlp.Trees.IHeadFinder"/>
		/// class's constructor.
		/// <c>-hfArg</c>
		/// can be used multiple times to pass in multiple arguments.
		/// <li>
		/// <c>-trf &lt;TreeReaderFactory-class-name&gt;</c>
		/// use the specified
		/// <see cref="Edu.Stanford.Nlp.Trees.ITreeReaderFactory"/>
		/// class to read trees from files.
		/// <li>
		/// <c>-e &lt;extension&gt;</c>
		/// Only attempt to read files with the given extension. If not provided, will attempt to read all files.</li>
		/// <li>
		/// <c>-v</c>
		/// print every tree that contains no matches of the specified pattern, but print no matches to the pattern.
		/// <li>
		/// <c>-x</c>
		/// Instead of the matched subtree, print the matched subtree's identifying number as defined in <tt>tgrep2</tt>:a
		/// unique identifier for the subtree and is in the form s:n, where s is an integer specifying
		/// the sentence number in the corpus (starting with 1), and n is an integer giving the order
		/// in which the node is encountered in a depth-first search starting with 1 at top node in the
		/// sentence tree.
		/// <li>
		/// <c>-extract &lt;tree-file&gt;</c>
		/// extracts the subtree s:n specified by <tt>code</tt> from the specified <tt>tree-file</tt>.  Overrides all other behavior of tregex.  Can't specify multiple encodings etc. yet.
		/// <li>
		/// <c>-extractFile &lt;code-file&gt; &lt;tree-file&gt;</c>
		/// extracts every subtree specified by the subtree codes in
		/// <c>code-file</c>
		/// , which must appear exactly one per line, from the specified
		/// <c>tree-file</c>
		/// .
		/// Overrides all other behavior of tregex. Can't specify multiple encodings etc. yet.
		/// <li>
		/// <c>-filter</c>
		/// causes this to act as a filter, reading tree input from stdin
		/// <li>
		/// <c>-T</c>
		/// causes all trees to be printed as processed (for debugging purposes).  Otherwise only matching nodes are printed.
		/// <li>
		/// <c>-macros &lt;filename&gt;</c>
		/// filename with macro substitutions to use.  file with tab separated lines original-tab-replacement
		/// </ul>
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Timing.StartTime();
			StringBuilder treePrintFormats = new StringBuilder();
			string printNonMatchingTreesOption = "-v";
			string subtreeCodeOption = "-x";
			string extractSubtreesOption = "-extract";
			string extractSubtreesFileOption = "-extractFile";
			string inputFileOption = "-i";
			string headFinderOption = "-hf";
			string headFinderArgOption = "-hfArg";
			string trfOption = "-trf";
			string extensionOption = "-e";
			string extension = null;
			string headFinderClassName = null;
			string[] headFinderArgs = StringUtils.EmptyStringArray;
			string treeReaderFactoryClassName = null;
			string printHandleOption = "-h";
			string markHandleOption = "-k";
			string encodingOption = "-encoding";
			string encoding = "UTF-8";
			string macroOption = "-macros";
			string macroFilename = string.Empty;
			string yieldOnly = "-t";
			string printAllTrees = "-T";
			string quietMode = "-C";
			string wholeTreeMode = "-w";
			string filenameOption = "-f";
			string oneMatchPerRootNodeMode = "-o";
			string reportTreeNumbers = "-n";
			string rootLabelOnly = "-u";
			string oneLine = "-s";
			IDictionary<string, int> flagMap = Generics.NewHashMap();
			flagMap[extractSubtreesOption] = 2;
			flagMap[extractSubtreesFileOption] = 2;
			flagMap[subtreeCodeOption] = 0;
			flagMap[printNonMatchingTreesOption] = 0;
			flagMap[encodingOption] = 1;
			flagMap[inputFileOption] = 1;
			flagMap[printHandleOption] = 1;
			flagMap[markHandleOption] = 2;
			flagMap[headFinderOption] = 1;
			flagMap[headFinderArgOption] = 1;
			flagMap[trfOption] = 1;
			flagMap[extensionOption] = 1;
			flagMap[macroOption] = 1;
			flagMap[yieldOnly] = 0;
			flagMap[quietMode] = 0;
			flagMap[wholeTreeMode] = 0;
			flagMap[printAllTrees] = 0;
			flagMap[filenameOption] = 0;
			flagMap[oneMatchPerRootNodeMode] = 0;
			flagMap[reportTreeNumbers] = 0;
			flagMap[rootLabelOnly] = 0;
			flagMap[oneLine] = 0;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, flagMap);
			args = argsMap[null];
			if (argsMap.Contains(encodingOption))
			{
				encoding = argsMap[encodingOption][0];
				log.Info("Encoding set to " + encoding);
			}
			PrintWriter errPW = new PrintWriter(new OutputStreamWriter(System.Console.Error, encoding), true);
			if (argsMap.Contains(extractSubtreesOption))
			{
				IList<string> subTreeStrings = Java.Util.Collections.SingletonList(argsMap[extractSubtreesOption][0]);
				ExtractSubtrees(subTreeStrings, argsMap[extractSubtreesOption][1]);
				return;
			}
			if (argsMap.Contains(extractSubtreesFileOption))
			{
				IList<string> subTreeStrings = Arrays.AsList(IOUtils.SlurpFile(argsMap[extractSubtreesFileOption][0]).Split("\n|\r|\n\r"));
				ExtractSubtrees(subTreeStrings, argsMap[extractSubtreesFileOption][0]);
				return;
			}
			if (args.Length < 1)
			{
				errPW.Println("Usage: java edu.stanford.nlp.trees.tregex.TregexPattern [-T] [-C] [-w] [-f] [-o] [-n] [-s] [-filter]  [-hf class] [-trf class] [-h handle]* [-e ext] pattern [filepath]");
				return;
			}
			string matchString = args[0];
			if (argsMap.Contains(macroOption))
			{
				macroFilename = argsMap[macroOption][0];
			}
			if (argsMap.Contains(headFinderOption))
			{
				headFinderClassName = argsMap[headFinderOption][0];
				errPW.Println("Using head finder " + headFinderClassName + "...");
			}
			if (argsMap.Contains(headFinderArgOption))
			{
				headFinderArgs = argsMap[headFinderArgOption];
			}
			if (argsMap.Contains(trfOption))
			{
				treeReaderFactoryClassName = argsMap[trfOption][0];
				errPW.Println("Using tree reader factory " + treeReaderFactoryClassName + "...");
			}
			if (argsMap.Contains(extensionOption))
			{
				extension = argsMap[extensionOption][0];
			}
			if (argsMap.Contains(printAllTrees))
			{
				TregexPattern.TRegexTreeVisitor.printTree = true;
			}
			if (argsMap.Contains(inputFileOption))
			{
				string inputFile = argsMap[inputFileOption][0];
				matchString = IOUtils.SlurpFile(inputFile, encoding);
				string[] newArgs = new string[args.Length + 1];
				System.Array.Copy(args, 0, newArgs, 1, args.Length);
				args = newArgs;
			}
			if (argsMap.Contains(quietMode))
			{
				TregexPattern.TRegexTreeVisitor.printMatches = false;
				TregexPattern.TRegexTreeVisitor.printNumMatchesToStdOut = true;
			}
			if (argsMap.Contains(printNonMatchingTreesOption))
			{
				TregexPattern.TRegexTreeVisitor.printNonMatchingTrees = true;
			}
			if (argsMap.Contains(subtreeCodeOption))
			{
				TregexPattern.TRegexTreeVisitor.printSubtreeCode = true;
				TregexPattern.TRegexTreeVisitor.printMatches = false;
			}
			if (argsMap.Contains(wholeTreeMode))
			{
				TregexPattern.TRegexTreeVisitor.printWholeTree = true;
			}
			if (argsMap.Contains(filenameOption))
			{
				TregexPattern.TRegexTreeVisitor.printFilename = true;
			}
			if (argsMap.Contains(oneMatchPerRootNodeMode))
			{
				TregexPattern.TRegexTreeVisitor.oneMatchPerRootNode = true;
			}
			if (argsMap.Contains(reportTreeNumbers))
			{
				TregexPattern.TRegexTreeVisitor.reportTreeNumbers = true;
			}
			if (argsMap.Contains(rootLabelOnly))
			{
				treePrintFormats.Append(TreePrint.rootLabelOnlyFormat).Append(',');
			}
			else
			{
				if (argsMap.Contains(oneLine))
				{
					// display short form
					treePrintFormats.Append("oneline,");
				}
				else
				{
					if (argsMap.Contains(yieldOnly))
					{
						treePrintFormats.Append("words,");
					}
					else
					{
						treePrintFormats.Append("penn,");
					}
				}
			}
			IHeadFinder hf = new CollinsHeadFinder();
			if (headFinderClassName != null)
			{
				Type[] hfArgClasses = new Type[headFinderArgs.Length];
				for (int i = 0; i < hfArgClasses.Length; i++)
				{
					hfArgClasses[i] = typeof(string);
				}
				try
				{
					hf = (IHeadFinder)Sharpen.Runtime.GetType(headFinderClassName).GetConstructor(hfArgClasses).NewInstance((object[])headFinderArgs);
				}
				catch (Exception e)
				{
					// cast to Object[] necessary to avoid varargs-related warning.
					throw new Exception("Error occurred while constructing HeadFinder: " + e);
				}
			}
			TregexPattern.TRegexTreeVisitor.tp = new TreePrint(treePrintFormats.ToString(), new PennTreebankLanguagePack());
			try
			{
				//TreePattern p = TreePattern.compile("/^S/ > S=dt $++ '' $-- ``");
				TregexPatternCompiler tpc = new TregexPatternCompiler(hf);
				Macros.AddAllMacros(tpc, macroFilename, encoding);
				Edu.Stanford.Nlp.Trees.Tregex.TregexPattern p = tpc.Compile(matchString);
				errPW.Println("Pattern string:\n" + p.Pattern());
				errPW.Println("Parsed representation:");
				p.PrettyPrint(errPW);
				string[] handles = argsMap[printHandleOption];
				if (argsMap.Contains("-filter"))
				{
					ITreeReaderFactory trf = GetTreeReaderFactory(treeReaderFactoryClassName);
					treebank = new MemoryTreebank(trf, encoding);
					//has to be in memory since we're not storing it on disk
					//read from stdin
					Reader reader = new BufferedReader(new InputStreamReader(Runtime.@in, encoding));
					((MemoryTreebank)treebank).Load(reader);
					reader.Close();
				}
				else
				{
					if (args.Length == 1)
					{
						errPW.Println("using default tree");
						ITreeReader r = new PennTreeReader(new StringReader("(VP (VP (VBZ Try) (NP (NP (DT this) (NN wine)) (CC and) (NP (DT these) (NNS snails)))) (PUNCT .))"), new LabeledScoredTreeFactory(new StringLabelFactory()));
						Tree t = r.ReadTree();
						treebank = new MemoryTreebank();
						treebank.Add(t);
					}
					else
					{
						int last = args.Length - 1;
						errPW.Println("Reading trees from file(s) " + args[last]);
						ITreeReaderFactory trf = GetTreeReaderFactory(treeReaderFactoryClassName);
						treebank = new DiskTreebank(trf, encoding);
						treebank.LoadPath(args[last], extension, true);
					}
				}
				TregexPattern.TRegexTreeVisitor vis = new TregexPattern.TRegexTreeVisitor(p, handles, encoding);
				treebank.Apply(vis);
				Timing.EndTime();
				if (TregexPattern.TRegexTreeVisitor.printMatches)
				{
					errPW.Println("There were " + vis.NumMatches() + " matches in total.");
				}
				if (TregexPattern.TRegexTreeVisitor.printNumMatchesToStdOut)
				{
					System.Console.Out.WriteLine(vis.NumMatches());
				}
			}
			catch (IOException e)
			{
				log.Warn(e);
			}
			catch (TregexParseException e)
			{
				errPW.Println("Error parsing expression: " + args[0]);
				errPW.Println("Parse exception: " + e);
			}
		}

		private static ITreeReaderFactory GetTreeReaderFactory(string treeReaderFactoryClassName)
		{
			ITreeReaderFactory trf = new TregexPattern.TRegexTreeReaderFactory();
			if (treeReaderFactoryClassName != null)
			{
				try
				{
					trf = (ITreeReaderFactory)System.Activator.CreateInstance(Sharpen.Runtime.GetType(treeReaderFactoryClassName));
				}
				catch (Exception e)
				{
					throw new Exception("Error occurred while constructing TreeReaderFactory: " + e);
				}
			}
			return trf;
		}

		private static Treebank treebank;

		private const long serialVersionUID = 5060298043763944913L;

		private class TRegexTreeVisitor : ITreeVisitor
		{
			private static bool printNumMatchesToStdOut = false;

			internal static bool printNonMatchingTrees = false;

			internal static bool printSubtreeCode = false;

			internal static bool printTree = false;

			internal static bool printWholeTree = false;

			internal static bool printMatches = true;

			internal static bool printFilename = false;

			internal static bool oneMatchPerRootNode = false;

			internal static bool reportTreeNumbers = false;

			internal static TreePrint tp;

			private PrintWriter pw;

			internal int treeNumber = 0;

			private readonly TregexPattern p;

			internal string[] handles;

			internal int numMatches;

			internal TRegexTreeVisitor(TregexPattern p, string[] handles, string encoding)
			{
				// used by main method, must be accessible
				// not thread-safe, but only used by TregexPattern's main method
				this.p = p;
				this.handles = handles;
				try
				{
					pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
				}
				catch (UnsupportedEncodingException)
				{
					log.Info("Error -- encoding " + encoding + " is unsupported.  Using platform default PrintWriter instead.");
					pw = new PrintWriter(System.Console.Out, true);
				}
			}

			// todo: add an option to only print each tree once, regardless.  Most useful in conjunction with -w
			public virtual void VisitTree(Tree t)
			{
				treeNumber++;
				if (printTree)
				{
					pw.Print(treeNumber + ":");
					pw.Println("Next tree read:");
					tp.PrintTree(t, pw);
				}
				TregexMatcher match = p.Matcher(t);
				if (printNonMatchingTrees)
				{
					if (match.Find())
					{
						numMatches++;
					}
					else
					{
						tp.PrintTree(t, pw);
					}
					return;
				}
				Tree lastMatchingRootNode = null;
				while (match.Find())
				{
					if (oneMatchPerRootNode)
					{
						if (lastMatchingRootNode == match.GetMatch())
						{
							continue;
						}
						else
						{
							lastMatchingRootNode = match.GetMatch();
						}
					}
					numMatches++;
					if (printFilename && treebank is DiskTreebank)
					{
						DiskTreebank dtb = (DiskTreebank)treebank;
						pw.Print("# ");
						pw.Println(dtb.GetCurrentFilename());
					}
					if (printSubtreeCode)
					{
						pw.Print(treeNumber);
						pw.Print(':');
						pw.Println(match.GetMatch().NodeNumber(t));
					}
					if (printMatches)
					{
						if (reportTreeNumbers)
						{
							pw.Print(treeNumber);
							pw.Print(": ");
						}
						if (printTree)
						{
							pw.Println("Found a full match:");
						}
						if (printWholeTree)
						{
							tp.PrintTree(t, pw);
						}
						else
						{
							if (handles != null)
							{
								if (printTree)
								{
									pw.Println("Here's the node you were interested in:");
								}
								foreach (string handle in handles)
								{
									Tree labeledNode = match.GetNode(handle);
									if (labeledNode == null)
									{
										log.Info("Error!!  There is no matched node \"" + handle + "\"!  Did you specify such a label in the pattern?");
									}
									else
									{
										tp.PrintTree(labeledNode, pw);
									}
								}
							}
							else
							{
								tp.PrintTree(match.GetMatch(), pw);
							}
						}
					}
				}
			}

			// pw.println();  // TreePrint already puts a blank line in
			// end if (printMatches)
			// end while match.find()
			// end visitTree
			public virtual int NumMatches()
			{
				return numMatches;
			}
		}

		public class TRegexTreeReaderFactory : ITreeReaderFactory
		{
			private readonly TreeNormalizer tn;

			public TRegexTreeReaderFactory()
				: this(new _TreeNormalizer_922())
			{
			}

			private sealed class _TreeNormalizer_922 : TreeNormalizer
			{
				public _TreeNormalizer_922()
				{
					this.serialVersionUID = -2998972954089638189L;
				}

				// end class TRegexTreeVisitor
				public override string NormalizeNonterminal(string str)
				{
					if (str == null)
					{
						return string.Empty;
					}
					else
					{
						return str;
					}
				}
			}

			public TRegexTreeReaderFactory(TreeNormalizer tn)
			{
				this.tn = tn;
			}

			public virtual ITreeReader NewTreeReader(Reader @in)
			{
				return new PennTreeReader(new BufferedReader(@in), new LabeledScoredTreeFactory(), tn);
			}
		}
		// end class TRegexTreeReaderFactory
	}
}
