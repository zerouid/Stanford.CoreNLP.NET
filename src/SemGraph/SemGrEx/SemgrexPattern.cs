using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.UD;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>A SemgrexPattern is a pattern for matching node and edge configurations a dependency graph.</summary>
	/// <remarks>
	/// A SemgrexPattern is a pattern for matching node and edge configurations a dependency graph.
	/// Patterns are written in a similar style to
	/// <c>tgrep</c>
	/// or
	/// <c>Tregex</c>
	/// and operate over
	/// <c>SemanticGraph</c>
	/// objects, which contain
	/// <c>IndexedWord nodes</c>
	/// .  Unlike
	/// <c>tgrep</c>
	/// but like Unix
	/// <c>grep</c>
	/// , there is no pre-indexing
	/// of the data to be searched.  Rather there is a linear scan through the graph
	/// where matches are sought.
	/// <h3>Nodes</h3>
	/// A node is represented by a set of attributes and their values contained by
	/// curly braces: {attr1:value1;attr2:value2;...}.  Therefore, {} represents any
	/// node in the graph.  Attributes must be plain strings; values can be strings
	/// or regular expressions blocked off by "/".  Regular expressions must
	/// match the whole attribute value, so that /NN/ matches "NN" only, while /NN.* /
	/// matches "NN", "NNS", "NNP", etc.
	/// <p>
	/// For example,
	/// <c/>
	/// {lemma:slice;tag:/VB.* /}} represents any verb nodes
	/// with "slice" as their lemma.  Attributes are extracted using
	/// <c>edu.stanford.nlp.ling.AnnotationLookup</c>
	/// .
	/// <p>
	/// The root of the graph can be marked by the $ sign, that is
	/// <c/>
	/// {$}}
	/// represents the root node.
	/// <p>
	/// A node description can be negated with '!'. !{lemma:boy} matches any token that isn't "boy"
	/// <h3>Relations</h3>
	/// Relations are defined by a symbol representing the type of relationship and a
	/// string or regular expression representing the value of the relationship. A
	/// relationship string of
	/// <c>%</c>
	/// means any relationship.  It is
	/// also OK simply to omit the relationship symbol altogether.
	/// <p>
	/// Currently supported node relations and their symbols:
	/// <table border = "1">
	/// <tr><th>Symbol<th>Meaning
	/// <tr><td>A &lt;reln B <td> A is the dependent of a relation reln with B
	/// <tr><td>A &gt;reln B <td>A is the governor of a relation reln with B
	/// <tr><td>A &lt;&lt;reln B <td>A is the dependent of a relation reln in a chain to B following
	/// <c>dep-&gt;gov</c>
	/// paths
	/// <tr><td>A &gt;&gt;reln B <td>A is the governor of a relation reln in a chain to B following
	/// <c>gov-&gt;dep</c>
	/// paths
	/// <tr><td>
	/// <c>A x,y&lt;&lt;reln B</c>
	/// <td>A is the dependent of a relation reln in a chain to B following
	/// <c>dep-&gt;gov</c>
	/// paths between distances of x and y
	/// <tr><td>
	/// <c>A x,y&gt;&gt;reln B</c>
	/// <td>A is the governor of a relation reln in a chain to B following
	/// <c>gov-&gt;dep</c>
	/// paths between distances of x and y
	/// <tr><td>A == B <td>A and B are the same nodes in the same graph
	/// <tr><td>A . B <td>A immediately precedes B, i.e. A.index() == B.index() - 1
	/// <tr><td>A $+ B <td>B is a right immediate sibling of A, i.e. A and B have the same parent and A.index() == B.index() - 1
	/// <tr><td>A $- B <td>B is a left immediate sibling of A, i.e. A and B have the same parent and A.index() == B.index() + 1
	/// <tr><td>A $++ B <td>B is a right sibling of A, i.e. A and B have the same parent and
	/// <c>A.index() &lt; B.index()</c>
	/// <tr><td>A $-- B <td>B is a left sibling of A, i.e. A and B have the same parent and
	/// <c>A.index() &gt; B.index()</c>
	/// <tr><td>A @ B <td>A is aligned to B (this is only used when you have two dependency graphs which are aligned)
	/// <caption>Currently supported node relations</caption>
	/// </table>
	/// <p>
	/// In a chain of relations, all relations are relative to the first
	/// node in the chain. For example, "
	/// <c/>
	/// {} &gt;nsubj {} &gt;dobj {}}"
	/// means "any node that is the governor of both a nsubj and
	/// a dobj relation".  If instead what you want is a node that is the
	/// governor of a nsubj relation with a node that is itself the
	/// governor of dobj relation, you should use parentheses and write: "
	/// <c/>
	/// {} &gt;nsubj ({} &gt;dobj {})}".
	/// <p>
	/// If a relation type is specified for the
	/// <c>&lt;&lt;</c>
	/// relation, the
	/// relation type is only used for the first relation in the sequence.
	/// Therefore, if B depends on A with the relation type foo, the
	/// pattern
	/// <c/>
	/// {} &lt;&lt;foo {}} will then match B and
	/// everything that depends on B.
	/// <p>
	/// Similarly, if a relation type is specified for the
	/// <c>&gt;&gt;</c>
	/// relation, the relation type is only used for the last relation in
	/// the sequence.  Therefore, if A governs B with the relation type
	/// foo, the pattern
	/// <c/>
	/// {} &gt;&gt;foo {}} will then match A
	/// and all of the nodes which have a sequence leading to A.
	/// <h3>Boolean relational operators</h3>
	/// Relations can be combined using the '&amp;' and '|' operators, negated with
	/// the '!' operator, and made optional with the '?' operator.
	/// <p>
	/// Relations can be grouped using brackets '[' and ']'.  So the
	/// expression
	/// <blockquote>
	/// <c/>
	/// {} [&lt;subj {} | &lt;agent {}] &amp; @ {} }
	/// </blockquote>
	/// matches a node that is either the dep of a subj or agent relationship and
	/// has an alignment to some other node.
	/// <p>
	/// Relations can be negated with the '!' operator, in which case the
	/// expression will match only if there is no node satisfying the relation.
	/// <p>
	/// Relations can be made optional with the '?' operator.  This way the
	/// expression will match even if the optional relation is not satisfied.
	/// <p>
	/// The operator ":" partitions a pattern into separate patterns,
	/// each of which must be matched.  For example, the following is a
	/// pattern where the matched node must have both "foo" and "bar" as
	/// descendants:
	/// <blockquote>
	/// <c/>
	/// {}=a &gt;&gt; {word:foo} : {}=a &gt;&gt; {word:bar} }
	/// </blockquote>
	/// This pattern could have been written
	/// <blockquote>
	/// <c/>
	/// {}=a &gt;&gt; {word:foo} &gt;&gt; {word:bar} }
	/// </blockquote>
	/// However, for more complex examples, partitioning a pattern may make
	/// it more readable.
	/// <h3>Naming nodes</h3>
	/// Nodes can be given names (a.k.a. handles) using '='.  A named node will
	/// be stored in a map that maps names to nodes so that if a match is found, the
	/// node corresponding to the named node can be extracted from the map.  For
	/// example
	/// <c/>
	/// ({tag:NN}=noun) } will match a singular noun node and
	/// after a match is found, the map can be queried with the name to retrieved the
	/// matched node using
	/// <see cref="SemgrexMatcher.GetNode(string)"/>
	/// with (String)
	/// argument "noun" (<i>not</i> "=noun").  Note that you are not allowed to
	/// name a node that is under the scope of a negation operator (the semantics
	/// would be unclear, since you can't store a node that never gets matched to).
	/// Trying to do so will cause a
	/// <see cref="ParseException"/>
	/// to be thrown. Named nodes
	/// <i>can be put within the scope of an optionality operator</i>.
	/// <p>
	/// Named nodes that refer back to previously named nodes need not have a node
	/// description -- this is known as "backreferencing".  In this case, the
	/// expression will match only when all instances of the same name get matched to
	/// the same node.  For example: the pattern
	/// <c/>
	/// {} &gt;dobj ({} &gt; {}=foo) &gt;mod ({} &gt; {}=foo) }
	/// will match a graph in which there are two nodes,
	/// <c>X</c>
	/// and
	/// <c>Y</c>
	/// , for which
	/// <c>X</c>
	/// is the grandparent of
	/// <c>Y</c>
	/// and there are two paths to
	/// <c>Y</c>
	/// , one of
	/// which goes through a
	/// <c>dobj</c>
	/// and one of which goes
	/// through a
	/// <c>mod</c>
	/// .
	/// <h3>Naming relations</h3>
	/// It is also possible to name relations.  For example, you can write the pattern
	/// <c/>
	/// {idx:1} &gt;=reln {idx:2}}  The name of the relation will then
	/// be stored in the matcher and can be extracted with
	/// <c>getRelnName("reln")</c>
	/// At present, though, there is no backreferencing capability such as with the
	/// named nodes; this is only useful when using the API to extract the name of the
	/// relation used when making the match.
	/// <p>
	/// In the case of ancestor and descendant relations, the <b>last</b>
	/// relation in the sequence of relations is the name used.
	/// <p>
	/// TODO
	/// At present a Semgrex pattern will match only once at a root node, even if there is more than one way of satisfying
	/// it under the root node. Probably its semantics should be changed, or at least the option should be given, to return
	/// all matches, as is the case for Tregex.
	/// </remarks>
	/// <author>Chloe Kiddon</author>
	[System.Serializable]
	public abstract class SemgrexPattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern));

		private const long serialVersionUID = 1722052832350596732L;

		private bool neg;

		private bool opt;

		private string patternString;

		protected internal Env env;

		internal SemgrexPattern()
		{
		}

		// = false;
		// = false;
		// conceptually final, but can't do because of parsing
		//always set with setEnv to make sure that it is also available to child patterns
		// package private constructor
		// NodePattern will return its one child, CoordinationPattern will
		// return the list of children it conjuncts or disjuncts
		internal abstract IList<Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern> GetChildren();

		internal abstract string LocalString();

		internal abstract void SetChild(Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern child);

		internal virtual void Negate()
		{
			if (opt)
			{
				throw new Exception("Node cannot be both negated and optional.");
			}
			neg = true;
		}

		internal virtual void MakeOptional()
		{
			if (neg)
			{
				throw new Exception("Node cannot be both negated and optional.");
			}
			opt = true;
		}

		internal virtual bool IsNegated()
		{
			return neg;
		}

		internal virtual bool IsOptional()
		{
			return opt;
		}

		// matcher methods
		// ------------------------------------------------------------
		// These get implemented in semgrex.CoordinationMatcher and NodeMatcher
		internal abstract SemgrexMatcher Matcher(SemanticGraph sg, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings, bool ignoreCase);

		internal abstract SemgrexMatcher Matcher(SemanticGraph sg, Alignment alignment, SemanticGraph sg_align, bool hypToText, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings
			 variableStrings, bool ignoreCase);

		/// <summary>
		/// Get a
		/// <see cref="SemgrexMatcher"/>
		/// for this pattern in this graph.
		/// </summary>
		/// <param name="sg">the SemanticGraph to match on</param>
		/// <returns>a SemgrexMatcher</returns>
		public virtual SemgrexMatcher Matcher(SemanticGraph sg)
		{
			return Matcher(sg, sg.GetFirstRoot(), Generics.NewHashMap(), Generics.NewHashMap(), new VariableStrings(), false);
		}

		/// <summary>
		/// Get a
		/// <see cref="SemgrexMatcher"/>
		/// for this pattern in this graph, with some
		/// initial conditions on the variable assignments
		/// </summary>
		public virtual SemgrexMatcher Matcher(SemanticGraph sg, IDictionary<string, IndexedWord> variables)
		{
			return Matcher(sg, sg.GetFirstRoot(), variables, Generics.NewHashMap(), new VariableStrings(), false);
		}

		/// <summary>
		/// Get a
		/// <see cref="SemgrexMatcher"/>
		/// for this pattern in this graph.
		/// </summary>
		/// <param name="sg">the SemanticGraph to match on</param>
		/// <param name="ignoreCase">
		/// will ignore case for matching a pattern with a node; not
		/// implemented by Coordination Pattern
		/// </param>
		/// <returns>a SemgrexMatcher</returns>
		public virtual SemgrexMatcher Matcher(SemanticGraph sg, bool ignoreCase)
		{
			return Matcher(sg, sg.GetFirstRoot(), Generics.NewHashMap(), Generics.NewHashMap(), new VariableStrings(), ignoreCase);
		}

		public virtual SemgrexMatcher Matcher(SemanticGraph hypGraph, Alignment alignment, SemanticGraph txtGraph)
		{
			return Matcher(hypGraph, alignment, txtGraph, true, hypGraph.GetFirstRoot(), Generics.NewHashMap(), Generics.NewHashMap(), new VariableStrings(), false);
		}

		public virtual SemgrexMatcher Matcher(SemanticGraph hypGraph, Alignment alignment, SemanticGraph txtGraph, bool ignoreCase)
		{
			return Matcher(hypGraph, alignment, txtGraph, true, hypGraph.GetFirstRoot(), Generics.NewHashMap(), Generics.NewHashMap(), new VariableStrings(), ignoreCase);
		}

		// compile method
		// -------------------------------------------------------------
		/// <summary>Creates a pattern from the given string.</summary>
		/// <param name="semgrex">The pattern string</param>
		/// <returns>A SemgrexPattern for the string.</returns>
		public static Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern Compile(string semgrex, Env env)
		{
			try
			{
				SemgrexParser parser = new SemgrexParser(new StringReader(semgrex + '\n'));
				Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern newPattern = parser.Root();
				newPattern.SetEnv(env);
				newPattern.patternString = semgrex;
				return newPattern;
			}
			catch (Exception ex)
			{
				throw new SemgrexParseException("Error parsing semgrex pattern " + semgrex, ex);
			}
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern Compile(string semgrex)
		{
			return Compile(semgrex, new Env());
		}

		public virtual string Pattern()
		{
			return patternString;
		}

		/// <summary>Recursively sets the env variable to this pattern in this and in all its children</summary>
		/// <param name="env">An Env</param>
		public virtual void SetEnv(Env env)
		{
			this.env = env;
			this.GetChildren().ForEach(null);
		}

		// printing methods
		// -----------------------------------------------------------
		/// <returns>A single-line string representation of the pattern</returns>
		public abstract override string ToString();

		/// <param name="hasPrecedence">
		/// indicates that this pattern has precedence in terms
		/// of "order of operations", so there is no need to parenthesize the
		/// expression
		/// </param>
		public abstract string ToString(bool hasPrecedence);

		private void PrettyPrint(PrintWriter pw, int indent)
		{
			for (int i = 0; i < indent; i++)
			{
				pw.Print("   ");
			}
			pw.Println(LocalString());
			foreach (Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern child in GetChildren())
			{
				child.PrettyPrint(pw, indent + 1);
			}
		}

		/// <summary>Print a multi-line representation of the pattern illustrating its syntax.</summary>
		public virtual void PrettyPrint(PrintWriter pw)
		{
			PrettyPrint(pw, 0);
		}

		/// <summary>Print a multi-line representation of the pattern illustrating its syntax.</summary>
		public virtual void PrettyPrint(TextWriter ps)
		{
			PrettyPrint(new PrintWriter(new OutputStreamWriter(ps), true));
		}

		/// <summary>
		/// Print a multi-line representation of the pattern illustrating its syntax
		/// to
		/// <c>System.out</c>
		/// .
		/// </summary>
		public virtual void PrettyPrint()
		{
			PrettyPrint(System.Console.Out);
		}

		public override bool Equals(object o)
		{
			//noinspection SimplifiableIfStatement
			if (!(o is Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern))
			{
				return false;
			}
			return o.ToString().Equals(this.ToString());
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public enum OutputFormat
		{
			List,
			Offset
		}

		private const string Pattern = "-pattern";

		private const string TreeFile = "-treeFile";

		private const string Mode = "-mode";

		private const string DefaultMode = "BASIC";

		private const string Extras = "-extras";

		private const string ConlluFile = "-conlluFile";

		private const string OutputFormatOption = "-outputFormat";

		private const string DefaultOutputFormat = "LIST";

		public static void Help()
		{
			log.Info("Possible arguments for SemgrexPattern:");
			log.Info(Pattern + ": what pattern to use for matching");
			log.Info(TreeFile + ": a file of trees to process");
			log.Info(ConlluFile + ": a CoNLL-U file of dependency trees to process");
			log.Info(Mode + ": what mode for dependencies.  basic, collapsed, or ccprocessed.  To get 'noncollapsed', use basic with extras");
			log.Info(Extras + ": whether or not to use extras");
			log.Info(OutputFormatOption + ": output format of matches. list or offset. 'list' prints the graph as a list of dependencies, " + "'offset' prints the filename and the line offset in the ConLL-U file.");
			log.Info();
			log.Info(Pattern + " is required");
		}

		/// <summary>Prints out all matches of a semgrex pattern on a file of dependencies.</summary>
		/// <remarks>
		/// Prints out all matches of a semgrex pattern on a file of dependencies.
		/// <p>
		/// Usage:<br />
		/// java edu.stanford.nlp.semgraph.semgrex.SemgrexPattern [args]
		/// <br />
		/// See the help() function for a list of possible arguments to provide.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			IDictionary<string, int> flagMap = Generics.NewHashMap();
			flagMap[Pattern] = 1;
			flagMap[TreeFile] = 1;
			flagMap[Mode] = 1;
			flagMap[Extras] = 1;
			flagMap[ConlluFile] = 1;
			flagMap[OutputFormatOption] = 1;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, flagMap);
			// args = argsMap.get(null);
			// TODO: allow patterns to be extracted from a file
			if (!(argsMap.Contains(Pattern)) || argsMap[Pattern].Length == 0)
			{
				Help();
				System.Environment.Exit(2);
			}
			Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern semgrex = Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexPattern.Compile(argsMap[Pattern][0]);
			string modeString = DefaultMode;
			if (argsMap.Contains(Mode) && argsMap[Mode].Length > 0)
			{
				modeString = argsMap[Mode][0].ToUpper();
			}
			SemanticGraphFactory.Mode mode = SemanticGraphFactory.Mode.ValueOf(modeString);
			string outputFormatString = DefaultOutputFormat;
			if (argsMap.Contains(OutputFormatOption) && argsMap[OutputFormatOption].Length > 0)
			{
				outputFormatString = argsMap[OutputFormatOption][0].ToUpper();
			}
			SemgrexPattern.OutputFormat outputFormat = SemgrexPattern.OutputFormat.ValueOf(outputFormatString);
			bool useExtras = true;
			if (argsMap.Contains(Extras) && argsMap[Extras].Length > 0)
			{
				useExtras = bool.ValueOf(argsMap[Extras][0]);
			}
			IList<SemanticGraph> graphs = Generics.NewArrayList();
			// TODO: allow other sources of graphs, such as dependency files
			if (argsMap.Contains(TreeFile) && argsMap[TreeFile].Length > 0)
			{
				foreach (string treeFile in argsMap[TreeFile])
				{
					log.Info("Loading file " + treeFile);
					MemoryTreebank treebank = new MemoryTreebank(new TreeNormalizer());
					treebank.LoadPath(treeFile);
					foreach (Tree tree in treebank)
					{
						// TODO: allow other languages... this defaults to English
						SemanticGraph graph = SemanticGraphFactory.MakeFromTree(tree, mode, useExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None);
						graphs.Add(graph);
					}
				}
			}
			if (argsMap.Contains(ConlluFile) && argsMap[ConlluFile].Length > 0)
			{
				CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
				foreach (string conlluFile in argsMap[ConlluFile])
				{
					log.Info("Loading file " + conlluFile);
					IEnumerator<SemanticGraph> it = reader.GetIterator(IOUtils.ReaderFromString(conlluFile));
					while (it.MoveNext())
					{
						SemanticGraph graph = it.Current;
						graphs.Add(graph);
					}
				}
			}
			foreach (SemanticGraph graph_1 in graphs)
			{
				SemgrexMatcher matcher = semgrex.Matcher(graph_1);
				if (!matcher.Find())
				{
					continue;
				}
				if (outputFormat == SemgrexPattern.OutputFormat.List)
				{
					log.Info("Matched graph:" + Runtime.LineSeparator() + graph_1.ToString(SemanticGraph.OutputFormat.List));
					int i = 1;
					bool found = true;
					while (found)
					{
						log.Info("Match " + i + " at: " + matcher.GetMatch().ToString(CoreLabel.OutputFormat.ValueIndex));
						IList<string> nodeNames = Generics.NewArrayList();
						Sharpen.Collections.AddAll(nodeNames, matcher.GetNodeNames());
						nodeNames.Sort();
						foreach (string name in nodeNames)
						{
							log.Info("  " + name + ": " + matcher.GetNode(name).ToString(CoreLabel.OutputFormat.ValueIndex));
						}
						log.Info(" ");
						found = matcher.Find();
					}
				}
				else
				{
					if (outputFormat == SemgrexPattern.OutputFormat.Offset)
					{
						if (graph_1.VertexListSorted().IsEmpty())
						{
							continue;
						}
						System.Console.Out.Printf("+%d %s%n", graph_1.VertexListSorted()[0].Get(typeof(CoreAnnotations.LineNumberAnnotation)), argsMap[ConlluFile][0]);
					}
				}
			}
		}
	}
}
