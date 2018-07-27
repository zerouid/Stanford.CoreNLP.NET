using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class CRFLabel
	{
		private const long serialVersionUID = 7403010868396790276L;

		private readonly int[] label;

		private int hashCode = -1;

		private const int maxNumClasses = 10;

		public CRFLabel(int[] label)
		{
			// todo: When rebuilding, change this to a better hash function like 31
			this.label = label;
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.IE.Crf.CRFLabel))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Crf.CRFLabel other = (Edu.Stanford.Nlp.IE.Crf.CRFLabel)o;
			if (other.label.Length != label.Length)
			{
				return false;
			}
			for (int i = 0; i < label.Length; i++)
			{
				if (label[i] != other.label[i])
				{
					return false;
				}
			}
			return true;
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.CRFLabel GetSmallerLabel(int size)
		{
			int[] newLabel = new int[size];
			System.Array.Copy(label, label.Length - size, newLabel, 0, size);
			return new Edu.Stanford.Nlp.IE.Crf.CRFLabel(newLabel);
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.CRFLabel GetOneSmallerLabel()
		{
			return GetSmallerLabel(label.Length - 1);
		}

		public virtual int[] GetLabel()
		{
			return label;
		}

		public virtual string ToString<E>(IIndex<E> classIndex)
		{
			IList<E> l = new List<E>();
			foreach (int aLabel in label)
			{
				l.Add(classIndex.Get(aLabel));
			}
			return l.ToString();
		}

		public override string ToString()
		{
			IList<int> l = new List<int>();
			foreach (int aLabel in label)
			{
				l.Add(int.Parse(aLabel));
			}
			return l.ToString();
		}

		public override int GetHashCode()
		{
			if (hashCode < 0)
			{
				hashCode = 0;
				foreach (int aLabel in label)
				{
					hashCode *= maxNumClasses;
					hashCode += aLabel;
				}
			}
			return hashCode;
		}
	}
}
