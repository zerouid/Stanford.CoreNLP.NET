using System;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A silly little class to denote a quantifier scope.</summary>
	/// <author>Gabor Angeli</author>
	public class OperatorSpec
	{
		public readonly Operator instance;

		public readonly int quantifierBegin;

		public readonly int quantifierEnd;

		public readonly int quantifierHead;

		public readonly int subjectBegin;

		public readonly int subjectEnd;

		public readonly int objectBegin;

		public readonly int objectEnd;

		public OperatorSpec(Operator instance, int quantifierBegin, int quantifierEnd, int subjectBegin, int subjectEnd, int objectBegin, int objectEnd)
		{
			this.instance = instance;
			this.quantifierBegin = quantifierBegin;
			this.quantifierEnd = quantifierEnd;
			this.quantifierHead = quantifierEnd - 1;
			this.subjectBegin = subjectBegin;
			this.subjectEnd = subjectEnd;
			this.objectBegin = objectBegin;
			this.objectEnd = objectEnd;
		}

		protected internal OperatorSpec(Operator instance, int quantifierBegin, int quantifierEnd, int subjectBegin, int subjectEnd, int objectBegin, int objectEnd, int sentenceLength)
			: this(instance, Math.Max(0, Math.Min(sentenceLength - 1, quantifierBegin)), Math.Max(0, Math.Min(sentenceLength, quantifierEnd)), Math.Max(0, Math.Min(sentenceLength - 1, subjectBegin)), Math.Max(0, Math.Min(sentenceLength, subjectEnd)), Math
				.Max(0, objectBegin == sentenceLength ? sentenceLength : Math.Min(sentenceLength - 1, objectBegin)), Math.Max(0, Math.Min(sentenceLength, objectEnd)))
		{
		}

		/// <summary>
		/// If true, this is an explicit quantifier, such as "all" or "some."
		/// The other option is for this to be an implicit quantification, for instance with proper names:
		/// <code>
		/// "Felix is a cat" -&gt; \forall x, Felix(x) \rightarrow cat(x).
		/// </summary>
		/// <remarks>
		/// If true, this is an explicit quantifier, such as "all" or "some."
		/// The other option is for this to be an implicit quantification, for instance with proper names:
		/// <code>
		/// "Felix is a cat" -&gt; \forall x, Felix(x) \rightarrow cat(x).
		/// </code>
		/// </remarks>
		public virtual bool IsExplicit()
		{
			return instance != Operator.ImplicitNamedEntity;
		}

		public virtual bool IsBinary()
		{
			return objectEnd > objectBegin;
		}

		public virtual int QuantifierLength()
		{
			return quantifierEnd - quantifierBegin;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override string ToString()
		{
			return "QuantifierScope{" + "subjectBegin=" + subjectBegin + ", subjectEnd=" + subjectEnd + ", objectBegin=" + objectBegin + ", objectEnd=" + objectEnd + '}';
		}

		public static Edu.Stanford.Nlp.Naturalli.OperatorSpec Merge(Edu.Stanford.Nlp.Naturalli.OperatorSpec x, Edu.Stanford.Nlp.Naturalli.OperatorSpec y)
		{
			System.Diagnostics.Debug.Assert((x.quantifierBegin == y.quantifierBegin));
			System.Diagnostics.Debug.Assert((x.quantifierEnd == y.quantifierEnd));
			System.Diagnostics.Debug.Assert((x.instance == y.instance));
			return new Edu.Stanford.Nlp.Naturalli.OperatorSpec(x.instance, Math.Min(x.quantifierBegin, y.quantifierBegin), Math.Min(x.quantifierEnd, y.quantifierEnd), Math.Min(x.subjectBegin, y.subjectBegin), Math.Max(x.subjectEnd, y.subjectEnd), Math.Min
				(x.objectBegin, y.objectBegin), Math.Max(x.objectEnd, y.objectEnd));
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Naturalli.OperatorSpec))
			{
				return false;
			}
			Edu.Stanford.Nlp.Naturalli.OperatorSpec that = (Edu.Stanford.Nlp.Naturalli.OperatorSpec)o;
			if (objectBegin != that.objectBegin)
			{
				return false;
			}
			if (objectEnd != that.objectEnd)
			{
				return false;
			}
			if (quantifierBegin != that.quantifierBegin)
			{
				return false;
			}
			if (quantifierEnd != that.quantifierEnd)
			{
				return false;
			}
			if (quantifierHead != that.quantifierHead)
			{
				return false;
			}
			if (subjectBegin != that.subjectBegin)
			{
				return false;
			}
			if (subjectEnd != that.subjectEnd)
			{
				return false;
			}
			if (instance != that.instance)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = instance != null ? instance.GetHashCode() : 0;
			result = 31 * result + quantifierBegin;
			result = 31 * result + quantifierEnd;
			result = 31 * result + quantifierHead;
			result = 31 * result + subjectBegin;
			result = 31 * result + subjectEnd;
			result = 31 * result + objectBegin;
			result = 31 * result + objectEnd;
			return result;
		}
	}
}
