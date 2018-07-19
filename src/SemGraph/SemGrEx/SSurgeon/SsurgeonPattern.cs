using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>
	/// This represents a source pattern and a subsequent edit script, or a sequence
	/// of successive in-place edits to perform on a SemanticGraph.
	/// </summary>
	/// <remarks>
	/// This represents a source pattern and a subsequent edit script, or a sequence
	/// of successive in-place edits to perform on a SemanticGraph.
	/// Though the SemgrexMatcher resulting from the Semgrex match over the
	/// SemanticGraph is available to the edit, currently the nodes and edges to be affected
	/// should be named, in order for the edits to identify nodes easily.  See the constructor
	/// for each edit type for appropriate syntax.
	/// NOTE: the edits are currently destructive.  If you wish to preserve your graph, make a copy.
	/// </remarks>
	/// <author>yeh1</author>
	public class SsurgeonPattern
	{
		protected internal string Uid;

		protected internal string notes = string.Empty;

		protected internal IList<SsurgeonEdit> editScript;

		protected internal SemgrexPattern semgrexPattern;

		protected internal SemanticGraph semgrexGraph = null;

		protected internal ISsurgPred predicateTest = null;

		private IDictionary<string, IndexedWord> nodeMap = null;

		public SsurgeonPattern(string Uid, SemgrexPattern pattern, IList<SsurgeonEdit> editScript)
		{
			// Source graph semgrex pattern was derived from (used for pattern learning)
			// Predicate tests to apply, if non-null, must return true to execute.
			// NodeMap is used to maintain a list of named nodes outside of the set in the SemgrexMatcher.
			// Primarily for newly inserted nodes.
			semgrexPattern = pattern;
			this.Uid = Uid;
			this.editScript = editScript;
		}

		public SsurgeonPattern(string Uid, SemgrexPattern pattern)
		{
			this.Uid = Uid;
			this.semgrexPattern = pattern;
			this.editScript = new List<SsurgeonEdit>();
		}

		public SsurgeonPattern(string Uid, SemgrexPattern pattern, SemanticGraph patternGraph)
			: this(Uid, pattern)
		{
			this.semgrexGraph = patternGraph;
		}

		public SsurgeonPattern(SemgrexPattern pattern, IList<SsurgeonEdit> editScript)
			: this(pattern.ToString(), pattern, editScript)
		{
		}

		public SsurgeonPattern(SemgrexPattern pattern)
			: this(pattern.ToString(), pattern)
		{
		}

		public SsurgeonPattern(SemgrexPattern pattern, SemanticGraph patternGraph)
			: this(pattern)
		{
			this.semgrexGraph = patternGraph;
		}

		public virtual void SetPredicate(ISsurgPred predicateTest)
		{
			this.predicateTest = predicateTest;
		}

		public virtual void AddEdit(SsurgeonEdit newEdit)
		{
			newEdit.SetOwningPattern(this);
			editScript.Add(newEdit);
		}

		/// <summary>Adds the node to the set of named nodes registered, using the given name.</summary>
		public virtual void AddNamedNode(IndexedWord node, string name)
		{
			nodeMap[name] = node;
		}

		public virtual IndexedWord GetNamedNode(string name)
		{
			return nodeMap[name];
		}

		public override string ToString()
		{
			StringWriter buf = new StringWriter();
			buf.Append("Semgrex Pattern: UID=");
			buf.Write(GetUID());
			buf.Write("\nNotes: ");
			buf.Write(GetNotes());
			buf.Write("\n");
			buf.Append(semgrexPattern.ToString());
			if (predicateTest != null)
			{
				buf.Write("\nPredicate: ");
				buf.Write(predicateTest.ToString());
			}
			buf.Append("\nEdit script:\n");
			foreach (SsurgeonEdit edit in editScript)
			{
				buf.Append("\t");
				buf.Append(edit.ToString());
				buf.Append("\n");
			}
			return buf.ToString();
		}

		/// <summary>Executes the given sequence of edits against the SemanticGraph.</summary>
		/// <remarks>
		/// Executes the given sequence of edits against the SemanticGraph.
		/// NOTE: because the graph could be destructively modified, the matcher may be invalid, and
		/// thus the pattern will only be executed against the first match.  Repeat this routine on the returned
		/// SemanticGraph to reapply on other matches.
		/// TODO: create variant that returns set of expansions while matcher.find() returns true
		/// </remarks>
		/// <param name="sg">SemanticGraph to operate over (NOT destroyed/modified).</param>
		/// <returns>True if a match was found and executed, otherwise false.</returns>
		/// <exception cref="System.Exception"/>
		public virtual ICollection<SemanticGraph> Execute(SemanticGraph sg)
		{
			ICollection<SemanticGraph> generated = new List<SemanticGraph>();
			SemgrexMatcher matcher = semgrexPattern.Matcher(sg);
			while (matcher.Find())
			{
				// NOTE: Semgrex can match two named nodes to the same node.  In this case, we simply,
				// check the named nodes, and if there are any collisions, we throw out this match.
				ICollection<string> nodeNames = matcher.GetNodeNames();
				ICollection<IndexedWord> seen = Generics.NewHashSet();
				foreach (string name in nodeNames)
				{
					IndexedWord curr = matcher.GetNode(name);
					if (seen.Contains(curr))
					{
						goto nextMatch_break;
					}
					seen.Add(curr);
				}
				//        System.out.println("REDUNDANT NODES FOUDN IN SEMGREX MATCH");
				// if we do have to test, assemble the tests and arguments based off of the current
				// match and test.  If false, continue, else execute as normal.
				if (predicateTest != null)
				{
					if (!predicateTest.Test(matcher))
					{
						continue;
					}
				}
				//      SemanticGraph tgt = new SemanticGraph(sg);
				// Generate a new graph, since we don't want to mutilate the original graph.
				// We use the same nodes, since the matcher operates off of those.
				SemanticGraph tgt = SemanticGraphFactory.DuplicateKeepNodes(sg);
				nodeMap = Generics.NewHashMap();
				foreach (SsurgeonEdit edit in editScript)
				{
					edit.Evaluate(tgt, matcher);
				}
				generated.Add(tgt);
nextMatch_continue: ;
			}
nextMatch_break: ;
			return generated;
		}

		/// <summary>
		/// Executes the Ssurgeon edit, but with the given Semgrex Pattern, instead of the one attached to this
		/// pattern.
		/// </summary>
		/// <remarks>
		/// Executes the Ssurgeon edit, but with the given Semgrex Pattern, instead of the one attached to this
		/// pattern.
		/// NOTE: Predicate tests are still active here, and any named nodes required for evaluation must be
		/// present.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual ICollection<SemanticGraph> Execute(SemanticGraph sg, SemgrexPattern overridePattern)
		{
			SemgrexMatcher matcher = overridePattern.Matcher(sg);
			ICollection<SemanticGraph> generated = new List<SemanticGraph>();
			while (matcher.Find())
			{
				if (predicateTest != null)
				{
					if (!predicateTest.Test(matcher))
					{
						continue;
					}
				}
				// We reset the named node map with each edit set, since these edits
				// should exist in a separate graph for each unique Semgrex match.
				nodeMap = Generics.NewHashMap();
				SemanticGraph tgt = new SemanticGraph(sg);
				foreach (SsurgeonEdit edit in editScript)
				{
					edit.Evaluate(tgt, matcher);
				}
				generated.Add(tgt);
			}
			return generated;
		}

		public virtual SemgrexPattern GetSemgrexPattern()
		{
			return semgrexPattern;
		}

		public const string EltListTag = "ssurgeon-pattern-list";

		public const string UidElemTag = "uid";

		public const string ResourceTag = "resource";

		public const string SsurgeonElemTag = "ssurgeon-pattern";

		public const string SemgrexElemTag = "semgrex";

		public const string SemgrexGraphElemTag = "semgrex-graph";

		public const string PredicateTag = "predicate";

		public const string PredicateAndTag = "and";

		public const string PredicateOrTag = "or";

		public const string PredWordlistTestTag = "wordlist-test";

		public const string PredIdAttr = "id";

		public const string NotesElemTag = "notes";

		public const string EditListElemTag = "edit-list";

		public const string EditElemTag = "edit";

		public const string OrdinalAttr = "ordinal";

		/* ------
		* XML output and input
		* ------ */
		public virtual IList<SsurgeonEdit> GetEditScript()
		{
			return editScript;
		}

		public virtual SemanticGraph GetSemgrexGraph()
		{
			return semgrexGraph;
		}

		public virtual string GetNotes()
		{
			return notes;
		}

		public virtual void SetNotes(string notes)
		{
			this.notes = notes;
		}

		public virtual string GetUID()
		{
			return Uid;
		}

		public virtual void SetUID(string uid)
		{
			Uid = uid;
		}

		/// <summary>Simply reads the given Ssurgeon pattern from file (args[0]), parses it, and prints it out.</summary>
		/// <remarks>
		/// Simply reads the given Ssurgeon pattern from file (args[0]), parses it, and prints it out.
		/// Use this for debugging the class and patterns.
		/// </remarks>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				System.Console.Out.WriteLine("Usage: SsurgeonPattern FILEPATH [\"COMPACT_SEMANTIC_GRAPH\"], FILEPATH=path to ssurgeon pattern to parse and print., SENTENCE=test sentence (in quotes)");
				System.Environment.Exit(-1);
			}
			File tgtFile = new File(args[0]);
			try
			{
				Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().InitLog(new File("./ssurgeon.log"));
				Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().SetLogPrefix("SsurgeonPattern test");
				IList<Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.SsurgeonPattern> patterns = Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().ReadFromFile(tgtFile);
				foreach (Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.SsurgeonPattern pattern in patterns)
				{
					System.Console.Out.WriteLine("- - - - -");
					System.Console.Out.WriteLine(pattern);
				}
				if (args.Length > 1)
				{
					for (int i = 1; i < args.Length; i++)
					{
						string text = args[i];
						SemanticGraph sg = SemanticGraph.ValueOf(text);
						ICollection<SemanticGraph> generated = Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().ExhaustFromPatterns(patterns, sg);
						System.Console.Out.WriteLine("\n= = = = = = = = = =\nSrc text = " + text);
						System.Console.Out.WriteLine(sg.ToCompactString());
						System.Console.Out.WriteLine("# generated  = " + generated.Count);
						foreach (SemanticGraph genSg in generated)
						{
							System.Console.Out.WriteLine(genSg);
							System.Console.Out.WriteLine(". . . . .");
						}
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
