using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class LatticeEdge
	{
		public readonly string word;

		public string label = null;

		public double weight;

		public readonly int start;

		public readonly int end;

		public readonly IDictionary<string, string> attrs;

		public LatticeEdge(string word, double weight, int start, int end)
		{
			this.word = word;
			this.weight = weight;
			this.start = start;
			this.end = end;
			attrs = Generics.NewHashMap();
		}

		public virtual void SetAttr(string key, string value)
		{
			attrs[key] = value;
		}

		public virtual string GetAttr(string key)
		{
			return attrs[key];
		}

		public virtual void SetLabel(string l)
		{
			label = l;
		}

		public virtual void SetWeight(double w)
		{
			weight = w;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[ " + word);
			sb.Append(string.Format(" start(%d) end(%d) wt(%f) ]", start, end, weight));
			if (label != null)
			{
				sb.Append(" / " + label);
			}
			return sb.ToString();
		}

		private const long serialVersionUID = 4416189959485854286L;
	}
}
