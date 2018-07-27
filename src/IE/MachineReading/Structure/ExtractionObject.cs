using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Represents any object that can be extracted - entity, relation, event</summary>
	/// <author>Andrey Gusev</author>
	/// <author>Mihai</author>
	[System.Serializable]
	public class ExtractionObject
	{
		private const long serialVersionUID = 1L;

		/// <summary>Unique identifier of the object in its document</summary>
		protected internal readonly string objectId;

		/// <summary>
		/// Sentence that contains this object
		/// This assumes that each extraction object is intra-sentential (true in ACE, Roth, BioNLP, MR)
		/// </summary>
		protected internal ICoreMap sentence;

		/// <summary>Type of this mention, e.g., GPE</summary>
		protected internal string type;

		/// <summary>Subtype, if available, e.g., GPE.CITY</summary>
		protected internal readonly string subType;

		/// <summary>
		/// Maximal token span relevant for this object, e.g., the largest NP for an entity mention
		/// The offsets are relative to the sentence that contains this object
		/// </summary>
		protected internal Span extentTokenSpan;

		/// <summary>This stores any optional attributes of ExtractionObjects</summary>
		protected internal ICoreMap attributeMap;

		/// <summary>
		/// Probabilities associated with this object
		/// We report probability values for each possible type for this object
		/// </summary>
		protected internal ICounter<string> typeProbabilities;

		public ExtractionObject(string objectId, ICoreMap sentence, Span span, string type, string subtype)
		{
			this.objectId = objectId;
			this.sentence = sentence;
			this.extentTokenSpan = span;
			this.type = string.Intern(type);
			this.subType = (subtype != null ? string.Intern(subtype) : null);
			this.attributeMap = null;
		}

		public virtual string GetObjectId()
		{
			return objectId;
		}

		public virtual string GetDocumentId()
		{
			return sentence.Get(typeof(CoreAnnotations.DocIDAnnotation));
		}

		public virtual ICoreMap GetSentence()
		{
			return sentence;
		}

		public virtual void SetSentence(ICoreMap sent)
		{
			this.sentence = sent;
		}

		public virtual int GetExtentTokenStart()
		{
			return extentTokenSpan.Start();
		}

		public virtual int GetExtentTokenEnd()
		{
			return extentTokenSpan.End();
		}

		public virtual Span GetExtent()
		{
			return extentTokenSpan;
		}

		public virtual void SetExtent(Span s)
		{
			extentTokenSpan = s;
		}

		public virtual string GetExtentString()
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			StringBuilder sb = new StringBuilder();
			for (int i = extentTokenSpan.Start(); i < extentTokenSpan.End(); i++)
			{
				CoreLabel token = tokens[i];
				if (i > extentTokenSpan.Start())
				{
					sb.Append(" ");
				}
				sb.Append(token.Word());
			}
			return sb.ToString();
		}

		public virtual string GetType()
		{
			return type;
		}

		public virtual string GetSubType()
		{
			return subType;
		}

		public override bool Equals(object other)
		{
			if (!(other is Edu.Stanford.Nlp.IE.Machinereading.Structure.ExtractionObject))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Machinereading.Structure.ExtractionObject o = (Edu.Stanford.Nlp.IE.Machinereading.Structure.ExtractionObject)other;
			return o.objectId.Equals(objectId) && o.sentence.Get(typeof(CoreAnnotations.TextAnnotation)).Equals(sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
		}

		internal class CompByExtent : IComparator<ExtractionObject>
		{
			public virtual int Compare(ExtractionObject o1, ExtractionObject o2)
			{
				if (o1.GetExtentTokenStart() < o2.GetExtentTokenStart())
				{
					return -1;
				}
				else
				{
					if (o1.GetExtentTokenStart() > o2.GetExtentTokenStart())
					{
						return 1;
					}
					else
					{
						if (o1.GetExtentTokenEnd() < o2.GetExtentTokenEnd())
						{
							return -1;
						}
						else
						{
							if (o1.GetExtentTokenEnd() > o2.GetExtentTokenEnd())
							{
								return 1;
							}
							else
							{
								return 0;
							}
						}
					}
				}
			}
		}

		public static void SortByExtent(IList<ExtractionObject> objects)
		{
			objects.Sort(new ExtractionObject.CompByExtent());
		}

		/// <summary>Returns the smallest span that covers the extent of all these objects</summary>
		/// <param name="objs"/>
		public static Span GetSpan(params ExtractionObject[] objs)
		{
			int left = int.MaxValue;
			int right = int.MinValue;
			foreach (ExtractionObject obj in objs)
			{
				if (obj.GetExtentTokenStart() < left)
				{
					left = obj.GetExtentTokenStart();
				}
				if (obj.GetExtentTokenEnd() > right)
				{
					right = obj.GetExtentTokenEnd();
				}
			}
			System.Diagnostics.Debug.Assert((left < int.MaxValue));
			System.Diagnostics.Debug.Assert((right > int.MinValue));
			return new Span(left, right);
		}

		/// <summary>Returns the text corresponding to the extent of this object</summary>
		public virtual string GetValue()
		{
			return GetFullValue();
		}

		/// <summary>
		/// Always returns the text corresponding to the extent of this object, even when
		/// getValue is overridden by subclass.
		/// </summary>
		public string GetFullValue()
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			StringBuilder sb = new StringBuilder();
			if (tokens != null && extentTokenSpan != null)
			{
				for (int i = extentTokenSpan.Start(); i < extentTokenSpan.End(); i++)
				{
					if (i > extentTokenSpan.Start())
					{
						sb.Append(" ");
					}
					sb.Append(tokens[i].Word());
				}
			}
			return sb.ToString();
		}

		public virtual void SetType(string t)
		{
			this.type = t;
		}

		private const string TypeSep = "/";

		/// <summary>Concatenates two types</summary>
		/// <param name="t1"/>
		/// <param name="t2"/>
		public static string ConcatenateTypes(string t1, string t2)
		{
			string[] t1Toks = t1.Split(TypeSep);
			string[] t2Toks = t2.Split(TypeSep);
			ICollection<string> uniqueTypes = Generics.NewHashSet();
			foreach (string t in t1Toks)
			{
				uniqueTypes.Add(t);
			}
			foreach (string t_1 in t2Toks)
			{
				uniqueTypes.Add(t_1);
			}
			string[] types = new string[uniqueTypes.Count];
			Sharpen.Collections.ToArray(uniqueTypes, types);
			Arrays.Sort(types);
			StringBuilder os = new StringBuilder();
			for (int i = 0; i < types.Length; i++)
			{
				if (i > 0)
				{
					os.Append(TypeSep);
				}
				os.Append(types[i]);
			}
			return os.ToString();
		}

		public virtual ICoreMap AttributeMap()
		{
			if (attributeMap == null)
			{
				attributeMap = new ArrayCoreMap();
			}
			return attributeMap;
		}

		public virtual void SetTypeProbabilities(ICounter<string> probs)
		{
			typeProbabilities = probs;
		}

		public virtual ICounter<string> GetTypeProbabilities()
		{
			return typeProbabilities;
		}

		internal virtual string ProbsToString()
		{
			IList<Pair<string, double>> sorted = Counters.ToDescendingMagnitudeSortedListWithCounts(typeProbabilities);
			StringBuilder os = new StringBuilder();
			os.Append("{");
			bool first = true;
			foreach (Pair<string, double> lv in sorted)
			{
				if (!first)
				{
					os.Append("; ");
				}
				os.Append(lv.first + ", " + lv.second);
				first = false;
			}
			os.Append("}");
			return os.ToString();
		}

		/// <summary>
		/// Returns true if it's worth saving/printing this object
		/// This happens in two cases:
		/// 1.
		/// </summary>
		/// <remarks>
		/// Returns true if it's worth saving/printing this object
		/// This happens in two cases:
		/// 1. The type of the object is not nilLabel
		/// 2. The type of the object is nilLabel but the second ranked label is within the given beam (0 -- 100) of the first choice
		/// </remarks>
		/// <param name="beam"/>
		/// <param name="nilLabel"/>
		public virtual bool PrintableObject(double beam, string nilLabel)
		{
			if (typeProbabilities == null)
			{
				return false;
			}
			IList<Pair<string, double>> sorted = Counters.ToDescendingMagnitudeSortedListWithCounts(typeProbabilities);
			// first choice not nil
			if (sorted.Count > 0 && !sorted[0].first.Equals(nilLabel))
			{
				return true;
			}
			// first choice is nil, but second is within beam
			if (sorted.Count > 1 && sorted[0].first.Equals(nilLabel) && beam > 0 && 100.0 * (sorted[0].second - sorted[1].second) < beam)
			{
				return true;
			}
			return false;
		}
	}
}
