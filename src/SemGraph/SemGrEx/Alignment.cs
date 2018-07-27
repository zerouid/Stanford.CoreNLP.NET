using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>
	/// Represents an alignment between a text and a hypothesis as a map from
	/// hypothesis words to text words, along with a real-valued score and
	/// (optionally) a justification string.
	/// </summary>
	/// <author>Bill MacCartney</author>
	public class Alignment
	{
		private IDictionary<IndexedWord, IndexedWord> map;

		protected internal double score;

		private string justification;

		public Alignment(IDictionary<IndexedWord, IndexedWord> map, double score, string justification)
		{
			// kill RecursiveAlignment, make this private!
			this.map = map;
			this.score = score;
			this.justification = justification;
		}

		/*
		* Returns the score for this <code>Alignment</code>.
		*/
		public virtual double GetScore()
		{
			return score;
		}

		/*
		* Returns the map from hypothesis words to text words for this
		* <code>Alignment</code>.
		*/
		public virtual IDictionary<IndexedWord, IndexedWord> GetMap()
		{
			return map;
		}

		/*
		* Returns the justification for this <code>Alignment</code>.
		*/
		public virtual string GetJustification()
		{
			return justification;
		}

		public override string ToString()
		{
			return ToString("readable");
		}

		public virtual string ToString(string format)
		{
			StringBuilder sb = new StringBuilder();
			if (format == "readable")
			{
				// sb.append("Alignment map:\n");
				IList<IndexedWord> keys = new List<IndexedWord>(map.Keys);
				keys.Sort();
				foreach (IndexedWord key in keys)
				{
					sb.Append(string.Format("%-20s ==> %s%n", IwToString(key), IwToString(map[key])));
				}
				sb.Append(string.Format("%s %6.3f%n", "Alignment score:", score));
			}
			else
			{
				if (format == "readable-tag-index")
				{
					IList<IndexedWord> keys = new List<IndexedWord>(map.Keys);
					keys.Sort();
					foreach (IndexedWord key in keys)
					{
						sb.Append(string.Format("%-20s ==> %s%n", IwToString(key), IwToString(map[key])));
					}
					sb.Append(string.Format("%s %6.3f%n", "Alignment score:", score));
				}
				else
				{
					if (format == "readable-old")
					{
						// sb.append("Alignment map:\n");
						foreach (KeyValuePair<IndexedWord, IndexedWord> entry in map)
						{
							sb.Append(string.Format("%-20s ==> %s%n", IwToString(entry.Key), IwToString(entry.Value)));
						}
						sb.Append("Alignment score: ");
						sb.Append(string.Format("%6.3f", score));
						sb.Append("\n");
					}
					else
					{
						// default
						sb.Append(map.ToString());
					}
				}
			}
			return sb.ToString();
		}

		private static string IwToString(IndexedWord iw)
		{
			if (iw == null || iw.Equals(IndexedWord.NoWord))
			{
				return "_";
			}
			return iw.ToString(CoreLabel.OutputFormat.Value);
		}

		/// <summary>Defined on map only.</summary>
		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment))
			{
				return false;
			}
			Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment other = (Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment)o;
			return map.Equals(other.map);
		}

		/// <summary>Defined on map only.</summary>
		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		/// <summary>
		/// returns a new alignment with the guarantee that:
		/// (i)   every node in hypGraph has a corresponding alignment
		/// (ii)  no alignment exists that doesn't have a node in hypGraph
		/// (iii) the only alignment that exists that doesn't have a node in
		/// txtGraph is an alignment to NO_WORD
		/// TODO[wcmac]: What is this for?  Looks like nothing is using this?
		/// </summary>
		internal virtual Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment PatchedAlignment(SemanticGraph hypGraph, SemanticGraph txtGraph)
		{
			IDictionary<IndexedWord, IndexedWord> patchedMap = Generics.NewHashMap();
			ICollection<IndexedWord> txtVertexSet = txtGraph.VertexSet();
			foreach (object o in hypGraph.VertexSet())
			{
				IndexedWord vertex = (IndexedWord)o;
				if (map.Contains(vertex) && txtVertexSet.Contains(map[vertex]))
				{
					patchedMap[vertex] = map[vertex];
				}
				else
				{
					patchedMap[vertex] = IndexedWord.NoWord;
				}
			}
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment(patchedMap, score, justification);
		}

		/// <summary>
		/// Constructs and returns a new Alignment from the given hypothesis
		/// <c>SemanticGraph</c>
		/// to the given text (passage) SemanticGraph, using
		/// the given array of indexes.  The i'th node of the array should contain the
		/// index of the node in the text (passage) SemanticGraph to which the i'th
		/// node in the hypothesis SemanticGraph is aligned, or -1 if it is aligned to
		/// NO_WORD.
		/// </summary>
		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment MakeFromIndexArray(SemanticGraph txtGraph, SemanticGraph hypGraph, int[] indexes, double score, string justification)
		{
			if (txtGraph == null || txtGraph.IsEmpty())
			{
				throw new ArgumentException("Invalid txtGraph " + txtGraph);
			}
			if (hypGraph == null || hypGraph.IsEmpty())
			{
				throw new ArgumentException("Invalid hypGraph " + hypGraph);
			}
			if (indexes == null)
			{
				throw new ArgumentException("Null index array");
			}
			if (indexes.Length != hypGraph.Size())
			{
				throw new ArgumentException("Index array length " + indexes.Length + " does not match hypGraph size " + hypGraph.Size());
			}
			IDictionary<IndexedWord, IndexedWord> map = Generics.NewHashMap();
			for (int i = 0; i < indexes.Length; i++)
			{
				IndexedWord hypNode = hypGraph.GetNodeByIndex(i);
				IndexedWord txtNode = IndexedWord.NoWord;
				if (indexes[i] >= 0)
				{
					txtNode = txtGraph.GetNodeByIndex(indexes[i]);
				}
				map[hypNode] = txtNode;
			}
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment(map, score, justification);
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment MakeFromIndexArray(SemanticGraph txtGraph, SemanticGraph hypGraph, int[] indexes)
		{
			return MakeFromIndexArray(txtGraph, hypGraph, indexes, 0.0, null);
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment MakeFromIndexArray(SemanticGraph txtGraph, SemanticGraph hypGraph, int[] indexes, double score)
		{
			return MakeFromIndexArray(txtGraph, hypGraph, indexes, score, null);
		}
	}
}
