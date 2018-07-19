using System;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>ValueLabel</code> object acts as a Label with linguistic
	/// attributes.
	/// </summary>
	/// <remarks>
	/// A <code>ValueLabel</code> object acts as a Label with linguistic
	/// attributes.  This is an abstract class, which doesn't actually store
	/// or return anything.  It returns <code>null</code> to any requests. However,
	/// it does
	/// stipulate that equals() and compareTo() are defined solely with respect to
	/// value(); this should not be changed by subclasses.
	/// Other fields of a ValueLabel subclass should be regarded
	/// as secondary facets (it is almost impossible to override equals in
	/// a useful way while observing the contract for equality defined for Object,
	/// in particular, that equality must by symmetric).
	/// This class is designed to be extended.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public abstract class ValueLabel : ILabel, IComparable<Edu.Stanford.Nlp.Ling.ValueLabel>
	{
		protected internal ValueLabel()
		{
		}

		/// <summary>Return the value of the label (or null if none).</summary>
		/// <remarks>
		/// Return the value of the label (or null if none).
		/// The default value returned by an <code>ValueLabel</code> is
		/// always <code>null</code>
		/// </remarks>
		/// <returns>the value for the label</returns>
		public virtual string Value()
		{
			return null;
		}

		/// <summary>Set the value for the label (if one is stored).</summary>
		/// <param name="value">- the value for the label</param>
		public virtual void SetValue(string value)
		{
		}

		/// <summary>Return a string representation of the label.</summary>
		/// <remarks>
		/// Return a string representation of the label.  This will just
		/// be the <code>value()</code> if it is non-<code>null</code>,
		/// and the empty string otherwise.
		/// </remarks>
		/// <returns>The string representation</returns>
		public override string ToString()
		{
			string val = Value();
			return (val == null) ? string.Empty : val;
		}

		public virtual void SetFromString(string labelStr)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Equality for <code>ValueLabel</code>s is defined in the first instance
		/// as equality of their <code>String</code> <code>value()</code>.
		/// </summary>
		/// <remarks>
		/// Equality for <code>ValueLabel</code>s is defined in the first instance
		/// as equality of their <code>String</code> <code>value()</code>.
		/// Now rewritten to correctly enforce the contract of equals in Object.
		/// Equality for a <code>ValueLabel</code> is determined simply by String
		/// equality of its <code>value()</code>.  Subclasses should not redefine
		/// this to include other aspects of the <code>ValueLabel</code>, or the
		/// contract for <code>equals()</code> is broken.
		/// </remarks>
		/// <param name="obj">the object against which equality is to be checked</param>
		/// <returns>true if <code>this</code> and <code>obj</code> are equal</returns>
		public override bool Equals(object obj)
		{
			string val = Value();
			return (obj is Edu.Stanford.Nlp.Ling.ValueLabel) && (val == null ? ((ILabel)obj).Value() == null : val.Equals(((ILabel)obj).Value()));
		}

		/// <summary>Return the hashCode of the String value providing there is one.</summary>
		/// <remarks>
		/// Return the hashCode of the String value providing there is one.
		/// Otherwise, returns an arbitrary constant for the case of
		/// <code>null</code>.
		/// </remarks>
		public override int GetHashCode()
		{
			string val = Value();
			return val == null ? 3 : val.GetHashCode();
		}

		/// <summary>Orders by <code>value()</code>'s lexicographic ordering.</summary>
		/// <param name="valueLabel">object to compare to</param>
		/// <returns>result (positive if this is greater than obj)</returns>
		public virtual int CompareTo(Edu.Stanford.Nlp.Ling.ValueLabel valueLabel)
		{
			return string.CompareOrdinal(Value(), valueLabel.Value());
		}

		/// <summary>Returns a factory that makes Labels of the appropriate sort.</summary>
		/// <returns>the <code>LabelFactory</code></returns>
		public abstract ILabelFactory LabelFactory();

		private const long serialVersionUID = -1413303679077285530L;
	}
}
