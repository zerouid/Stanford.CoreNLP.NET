using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>Defines a class for pretty-printing SemanticGraphs.</summary>
	/// <author>Bill MacCartney</author>
	public class SemanticGraphFormatter
	{
		private const string Lparen = "[";

		private const string Rparen = "]";

		private const string Space = " ";

		private const string Colon = ">";

		private const int DefaultWidth = 80;

		private const int DefaultIndent = 4;

		private const bool DefaultSmartIndent = true;

		private const bool DefaultShowRelns = true;

		private const bool DefaultShowTags = true;

		private const bool DefaultShowAnnos = false;

		private const bool DefaultShowIndices = false;

		private int width = DefaultWidth;

		private int indent = DefaultIndent;

		private bool smartIndent = DefaultSmartIndent;

		private bool showRelns = DefaultShowRelns;

		private bool showTags = DefaultShowTags;

		private bool showAnnos = DefaultShowAnnos;

		private bool showIndices = DefaultShowIndices;

		private StringBuilder @out;

		private ICollection<IndexedWord> used;

		public SemanticGraphFormatter()
			: this(DefaultWidth, DefaultIndent, DefaultSmartIndent, DefaultShowRelns, DefaultShowTags, DefaultShowAnnos, DefaultShowIndices)
		{
		}

		public SemanticGraphFormatter(int width, int indent, bool smartIndent, bool showRelns, bool showTags, bool showAnnos, bool showIndices)
		{
			// named constants ------------------------------------------------------------
			// member variables -----------------------------------------------------------
			// working variables -- not thread-safe!!!
			// constructors ---------------------------------------------------------------
			this.width = width;
			this.indent = indent;
			this.smartIndent = smartIndent;
			this.showRelns = showRelns;
			this.showTags = showTags;
			this.showAnnos = showAnnos;
			this.showIndices = showIndices;
		}

		// public method --------------------------------------------------------------
		/// <summary>
		/// Returns a pretty-printed string representation of the given semantic graph,
		/// on one or more lines.
		/// </summary>
		public virtual string FormatSemanticGraph(SemanticGraph sg)
		{
			if (sg.VertexSet().IsEmpty())
			{
				return "[]";
			}
			@out = new StringBuilder();
			// not thread-safe!!!
			used = Generics.NewHashSet();
			if (sg.GetRoots().Count == 1)
			{
				FormatSGNode(sg, sg.GetFirstRoot(), 1);
			}
			else
			{
				int index = 0;
				foreach (IndexedWord root in sg.GetRoots())
				{
					index += 1;
					@out.Append("root_").Append(index).Append("> ");
					FormatSGNode(sg, root, 9);
					@out.Append("\n");
				}
			}
			string result = @out.ToString();
			if (!result.StartsWith("["))
			{
				result = "[" + result + "]";
			}
			return result;
		}

		// private methods ------------------------------------------------------------
		/// <summary>
		/// Appends to this.out a one-line or multi-line string representation of the given
		/// semantic graph, using the given number of spaces for indentation.
		/// </summary>
		private void FormatSGNode(SemanticGraph sg, IndexedWord node, int spaces)
		{
			used.Add(node);
			string oneline = FormatSGNodeOneline(sg, node);
			bool toolong = (spaces + oneline.Length > width);
			bool breakable = sg.HasChildren(node);
			if (toolong && breakable)
			{
				FormatSGNodeMultiline(sg, node, spaces);
			}
			else
			{
				@out.Append(oneline);
			}
		}

		private string FormatSGNodeOneline(SemanticGraph sg, IndexedWord node)
		{
			StringBuilder sb = new StringBuilder();
			ICollection<IndexedWord> usedOneline = Generics.NewHashSet();
			FormatSGNodeOnelineHelper(sg, node, sb, usedOneline);
			return sb.ToString();
		}

		private void FormatSGNodeOnelineHelper(SemanticGraph sg, IndexedWord node, StringBuilder sb, ICollection<IndexedWord> usedOneline)
		{
			usedOneline.Add(node);
			bool isntLeaf = (sg.OutDegree(node) > 0);
			if (isntLeaf)
			{
				sb.Append(Lparen);
			}
			sb.Append(FormatLabel(node));
			foreach (SemanticGraphEdge depcy in sg.GetOutEdgesSorted(node))
			{
				IndexedWord dep = depcy.GetDependent();
				sb.Append(Space);
				if (showRelns)
				{
					sb.Append(depcy.GetRelation());
					sb.Append(Colon);
				}
				if (!usedOneline.Contains(dep) && !used.Contains(dep))
				{
					// avoid infinite loop
					FormatSGNodeOnelineHelper(sg, dep, sb, usedOneline);
				}
				else
				{
					sb.Append(FormatLabel(dep));
				}
			}
			if (isntLeaf)
			{
				sb.Append(Rparen);
			}
		}

		/// <summary>
		/// Appends to this.out a multi-line string representation of the given
		/// semantic graph, using the given number of spaces for indentation.
		/// </summary>
		/// <remarks>
		/// Appends to this.out a multi-line string representation of the given
		/// semantic graph, using the given number of spaces for indentation.
		/// The semantic graph's label and each of its children appear on separate
		/// lines.  A child may appear with a one-line or multi-line representation,
		/// depending upon available space.
		/// </remarks>
		private void FormatSGNodeMultiline(SemanticGraph sg, IndexedWord node, int spaces)
		{
			@out.Append(Lparen);
			@out.Append(FormatLabel(node));
			if (smartIndent)
			{
				spaces += 1;
			}
			else
			{
				spaces += indent;
			}
			foreach (SemanticGraphEdge depcy in sg.GetOutEdgesSorted(node))
			{
				IndexedWord dep = depcy.GetDependent();
				@out.Append("\n");
				@out.Append(StringUtils.Repeat(Space, spaces));
				int sp = spaces;
				if (showRelns)
				{
					string reln = depcy.GetRelation().ToString();
					@out.Append(reln);
					@out.Append(Colon);
					if (smartIndent)
					{
						sp += (reln.Length + 1);
					}
				}
				if (!used.Contains(dep))
				{
					// avoid infinite loop
					FormatSGNode(sg, dep, sp);
				}
			}
			@out.Append(Rparen);
		}

		private string FormatLabel(IndexedWord node)
		{
			string s = node.Word();
			if (showIndices)
			{
				s = node.SentIndex() + ":" + node.Index() + "-" + s;
			}
			if (showTags)
			{
				string tag = node.Tag();
				if (tag != null && tag.Length > 0)
				{
					s += "/" + tag;
				}
			}
			if (showAnnos)
			{
				s += node.ToString(CoreLabel.OutputFormat.Map);
			}
			return s;
		}

		// testing -----------------------------------------------------------------------
		private void Test(string s)
		{
			SemanticGraph sg = SemanticGraph.ValueOf(s);
			System.Console.Out.WriteLine(sg.ToCompactString());
			System.Console.Out.WriteLine(FormatSemanticGraph(sg));
			System.Console.Out.WriteLine();
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Semgraph.SemanticGraphFormatter fmt = new Edu.Stanford.Nlp.Semgraph.SemanticGraphFormatter();
			System.Console.Out.WriteLine("0        1         2         3         4         5         6         7         8");
			System.Console.Out.WriteLine("12345678901234567890123456789012345678901234567890123456789012345678901234567890");
			System.Console.Out.WriteLine();
			fmt.Test("[like subj>Bill dobj>[muffins compound>blueberrry]]");
			fmt.Test("[eligible nsubj>Zambia cop>became xcomp>[receive mark>to dobj>[assistance amod>UNCDF] nmod:in>1991]]");
			fmt.Test("[say advcl>[are mark>If nsubj>[polls det>the] xcomp>[believed aux>to auxpass>be]] nsubj>[voters amod>American] aux>will advmod>[much dep>[same det>the]] nmod:to>[Republicans nmod:poss>[Bush case>'s compound>George] case>to] dep>[vote advmod>when nsubj>they nmod:in>[elections amod>congressional det>the case>in] nmod:on>[[November num>7th case>on]]]]"
				);
		}
	}
}
