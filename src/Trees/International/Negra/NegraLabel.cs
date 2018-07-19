using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	[System.Serializable]
	public class NegraLabel : StringLabel
	{
		private const long serialVersionUID = 2847331882765391095L;

		public const string FeatureSep = "#";

		private string edge;

		/* An object for edge labels as implemented in the Negra treebank.
		* Negra labels need a CATEGORY, an EDGE LABEL, and a MORPHOLOGICAL
		* INFORMATION value.  There is a different object for lexicalized
		* Negra labels.
		*
		* @author Roger Levy
		*/
		public override ILabelFactory LabelFactory()
		{
			return new NegraLabel.NegraLabelFactory();
		}

		private class NegraLabelFactory : ILabelFactory
		{
			public virtual ILabel NewLabel(string labelStr)
			{
				return new NegraLabel(labelStr);
			}

			/// <summary>Options don't do anything.</summary>
			public virtual ILabel NewLabel(string labelStr, int options)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Nothing fancy now: just makes the argument the value of the label</summary>
			public virtual ILabel NewLabelFromString(string encodedLabelStr)
			{
				return NewLabel(encodedLabelStr);
			}

			/// <summary>Iff oldLabel is a NegraLabel, copy it.</summary>
			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				NegraLabel result;
				if (oldLabel is NegraLabel)
				{
					NegraLabel l = (NegraLabel)oldLabel;
					result = new NegraLabel(l.Value(), l.GetEdge(), Generics.NewHashMap<string, string>());
					foreach (KeyValuePair<string, string> e in l.features)
					{
						result.features[e.Key] = e.Value;
					}
				}
				else
				{
					result = new NegraLabel(oldLabel.Value());
				}
				return result;
			}
		}

		private IDictionary<string, string> features;

		public virtual void SetEdge(string edge)
		{
			this.edge = edge;
		}

		public virtual string GetEdge()
		{
			return edge;
		}

		private NegraLabel()
		{
		}

		public NegraLabel(string str)
			: this(str, Generics.NewHashMap<string, string>())
		{
		}

		public NegraLabel(string str, IDictionary<string, string> features)
			: this(str, null, features)
		{
		}

		public NegraLabel(string str, string edge, IDictionary<string, string> features)
			: base(str)
		{
			this.edge = edge;
			this.features = features;
		}

		public virtual void SetFeatureValue(string feature, string value)
		{
			features[feature] = value;
		}

		public virtual string FeatureValue(string feature)
		{
			return features[feature];
		}

		public override string ToString()
		{
			string str = Value();
			if (edge != null)
			{
				str += "->" + GetEdge();
			}
			if (!features.IsEmpty())
			{
				str += "." + features.ToString();
			}
			return str;
		}
	}
}
