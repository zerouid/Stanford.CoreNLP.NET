using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>Adds a new dependent node, based off of a prototype IndexedWord, with the given relation.</summary>
	/// <remarks>
	/// Adds a new dependent node, based off of a prototype IndexedWord, with the given relation.
	/// The new node's sentence index is inherited from the governing node.  Currently a cheap heuristic
	/// is made, placing the new node as the leftmost child of the governing node.
	/// TODO: add position (a la Tregex)
	/// TODO: determine consistent and intuitive arguments
	/// TODO: because word position is important for certain features (such as bigram lexical overlap), need
	/// ability to specify in which position the new node is inserted.
	/// </remarks>
	/// <author>Eric Yeh</author>
	public class AddDep : SsurgeonEdit
	{
		public const string Label = "addDep";

		internal IndexedWord newNodePrototype;

		internal GrammaticalRelation relation;

		internal string govNodeName;

		internal double weight;

		/// <summary>Creates an EnglishGrammaticalRelation AddDep edit.</summary>
		/// <param name="newNode">String representation of new dependent IndexedFeatureNode map.</param>
		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddDep CreateEngAddDep(string govNodeName, string engRelation, string newNode)
		{
			GrammaticalRelation relation = EnglishGrammaticalRelations.ValueOf(engRelation);
			//  IndexedWord newNodeObj = new IndexedWord(CoreLabel.fromAbstractMapLabel(IndexedFeatureLabel.valueOf(newNode, MapFactory.HASH_MAP_FACTORY)));
			IndexedWord newNodeObj = FromCheapString(newNode);
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddDep(govNodeName, relation, newNodeObj);
		}

		public AddDep(string govNodeName, GrammaticalRelation relation, IndexedWord newNodePrototype)
		{
			this.newNodePrototype = newNodePrototype;
			this.relation = relation;
			this.govNodeName = govNodeName;
			this.weight = 0;
		}

		public AddDep(string govNodeName, GrammaticalRelation relation, IndexedWord newNodePrototype, double weight)
			: this(govNodeName, relation, newNodePrototype)
		{
			this.weight = weight;
		}

		/// <summary>Emits a parseable instruction string.</summary>
		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.GovNodenameArg);
			buf.Write(" ");
			buf.Write(govNodeName);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.RelnArg);
			buf.Write(" ");
			buf.Write(relation.ToString());
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NodeProtoArg);
			buf.Write(" ");
			buf.Write("\"");
			//  buf.write(newNodePrototype.toString("map")); buf.write("\"\t")
			buf.Write(CheapWordToString(newNodePrototype));
			buf.Write("\"\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.WeightArg);
			buf.Write(" ");
			buf.Write(weight.ToString());
			return buf.ToString();
		}

		/// <summary>TODO: figure out how to specify where in the sentence this node goes.</summary>
		/// <remarks>
		/// TODO: figure out how to specify where in the sentence this node goes.
		/// TODO: determine if we should be copying an IndexedWord, or working just with a FeatureLabel.
		/// TODO: bombproof if this gov, dep, and reln already exist.
		/// </remarks>
		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord govNode = sm.GetNode(govNodeName);
			IndexedWord newNode = new IndexedWord(newNodePrototype);
			int newIndex = SemanticGraphUtils.LeftMostChildVertice(govNode, sg).Index();
			// cheap En-specific hack for placing copula (beginning of governing phrase)
			newNode.SetDocID(govNode.DocID());
			newNode.SetIndex(newIndex);
			newNode.SetSentIndex(govNode.SentIndex());
			sg.AddVertex(newNode);
			sg.AddEdge(govNode, newNode, relation, weight, false);
		}

		public const string WordKey = "word";

		public const string LemmaKey = "lemma";

		public const string ValueKey = "value";

		public const string CurrentKey = "current";

		public const string PosKey = "POS";

		public const string TupleDelimiter = "=";

		public const string AtomDelimiter = " ";

		// Simple mapping of all the stuff we care about (until IndexedFeatureLabel --> CoreLabel map pain is fixed)
		/// <summary>This converts the node into a simple string based representation.</summary>
		/// <remarks>
		/// This converts the node into a simple string based representation.
		/// NOTE: this is extremely brittle, and presumes values do not contain delimiters
		/// </remarks>
		public static string CheapWordToString(IndexedWord node)
		{
			StringWriter buf = new StringWriter();
			buf.Write("{");
			buf.Write(WordKey);
			buf.Write(TupleDelimiter);
			buf.Write(NullShield(node.Word()));
			buf.Write(AtomDelimiter);
			buf.Write(LemmaKey);
			buf.Write(TupleDelimiter);
			buf.Write(NullShield(node.Lemma()));
			buf.Write(AtomDelimiter);
			buf.Write(PosKey);
			buf.Write(TupleDelimiter);
			buf.Write(NullShield(node.Tag()));
			buf.Write(AtomDelimiter);
			buf.Write(ValueKey);
			buf.Write(TupleDelimiter);
			buf.Write(NullShield(node.Value()));
			buf.Write(AtomDelimiter);
			buf.Write(CurrentKey);
			buf.Write(TupleDelimiter);
			buf.Write(NullShield(node.OriginalText()));
			buf.Write("}");
			return buf.ToString();
		}

		/// <summary>Given the node arg string, converts it into an IndexedWord.</summary>
		public static IndexedWord FromCheapString(string rawArg)
		{
			string arg = Sharpen.Runtime.Substring(rawArg, 1, rawArg.Length - 1);
			string[] tuples = arg.Split(AtomDelimiter);
			IDictionary<string, string> args = Generics.NewHashMap();
			foreach (string tuple in tuples)
			{
				string[] vals = tuple.Split(TupleDelimiter);
				string key = vals[0];
				string value = string.Empty;
				if (vals.Length == 2)
				{
					value = vals[1];
				}
				args[key] = value;
			}
			IndexedWord newWord = new IndexedWord();
			newWord.SetWord(args[WordKey]);
			newWord.SetLemma(args[LemmaKey]);
			newWord.SetTag(args[PosKey]);
			newWord.SetValue(args[ValueKey]);
			newWord.SetOriginalText(args[CurrentKey]);
			return newWord;
		}

		public static string NullShield(string str)
		{
			return str == null ? string.Empty : str;
		}
	}
}
