using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A <code>Constituent</code> object defines a generic edge in a graph.</summary>
	/// <remarks>
	/// A <code>Constituent</code> object defines a generic edge in a graph.
	/// The <code>Constituent</code> class is designed to be extended.  It
	/// implements the <code>Comparable</code> interface in order to allow
	/// graphs to be topologically sorted by the ordinary <code>Collection</code>
	/// library in <code>java.util</code>, keying primarily on right-hand
	/// node ID number.  The <code>Constituent</code> class implements most
	/// of the functionality of the the <code>Label</code>
	/// interface by passing all requests down to the <code>Label</code> which
	/// might be contained in the <code>Constituent</code>.  This allows one
	/// to put a <code>Constituent</code> anywhere that a <code>Label</code> is
	/// required.  A <code>Constituent</code> is always <code>Scored</code>.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public abstract class Constituent : ILabeled, IScored, ILabel
	{
		public Constituent()
		{
		}

		/// <summary>access start node.</summary>
		public abstract int Start();

		/// <summary>set start node.</summary>
		public abstract void SetStart(int start);

		/// <summary>access end node.</summary>
		public abstract int End();

		/// <summary>set end node.</summary>
		public abstract void SetEnd(int end);

		/// <summary>access label</summary>
		public virtual ILabel Label()
		{
			return null;
		}

		/// <summary>
		/// Sets the label associated with the current Constituent,
		/// if there is one.
		/// </summary>
		public virtual void SetLabel(ILabel label)
		{
		}

		// a noop
		/// <summary>Access labels -- actually always a singleton here.</summary>
		public virtual ICollection<ILabel> Labels()
		{
			return Java.Util.Collections.SingletonList(Label());
		}

		public virtual void SetLabels(ICollection<ILabel> labels)
		{
			throw new NotSupportedException("Constituent can't be multilabeled");
		}

		/// <summary>access score</summary>
		public virtual double Score()
		{
			return double.NaN;
		}

		/// <summary>Sets the score associated with the current node, if there is one</summary>
		public virtual void SetScore(double score)
		{
		}

		// a no-op
		/// <summary>Return a string representation of a <code>Constituent</code>.</summary>
		/// <returns>The full string representation.</returns>
		public override string ToString()
		{
			StringBuilder sb;
			ILabel lab = Label();
			if (lab != null)
			{
				sb = new StringBuilder(lab.ToString());
			}
			else
			{
				sb = new StringBuilder();
			}
			sb.Append("(").Append(Start()).Append(",").Append(End()).Append(")");
			return sb.ToString();
		}

		/// <summary>Return the length of a <code>Constituent</code></summary>
		public virtual int Size()
		{
			return End() - Start();
		}

		/// <summary>Compare with another Object for equality.</summary>
		/// <remarks>
		/// Compare with another Object for equality.
		/// Two Constituent objects are equal if they have the same start and end,
		/// and, if at least one of them has a non-null label, then their labels are equal.
		/// The score of a Constituent is not considered in the equality test.
		/// This seems to make sense for most of the applications we have in mind
		/// where one wants to assess equality independent of score, and then if
		/// necessary to relax a constituent if one with a better score is found.
		/// (Note, however, that if you do want to compare Constituent scores for
		/// equality, then you have to be careful,
		/// because two <code>double</code> NaN values are considered unequal in
		/// Java.)
		/// The general contract of equals() implies that one can't have a
		/// subclass of a concrete [non-abstract] class redefine equals() to use
		/// extra aspects, so subclasses shouldn't override this in ways that
		/// make use of extra fields.
		/// </remarks>
		/// <param name="obj">The object being compared with</param>
		/// <returns>true if the objects are equal</returns>
		public override bool Equals(object obj)
		{
			// unclear if this will be a speedup in general
			// if (this == o)
			//      return true;
			if (obj is Edu.Stanford.Nlp.Trees.Constituent)
			{
				Edu.Stanford.Nlp.Trees.Constituent c = (Edu.Stanford.Nlp.Trees.Constituent)obj;
				// System.out.println("Comparing " + this + " to " + c + "\n  " +
				//	"start: " + (start() == c.start()) + " end: " +
				//	(end() == c.end()) + " score: " + (score() == c.score()));
				if ((Start() == c.Start()) && (End() == c.End()))
				{
					ILabel lab1 = Label();
					ILabel lab2 = c.Label();
					if (lab1 == null)
					{
						return lab2 == null;
					}
					string lv1 = lab1.Value();
					string lv2 = lab2.Value();
					if (lv1 == null && lv2 == null)
					{
						return true;
					}
					if (lv1 != null && lv2 != null)
					{
						return lab1.Value().Equals(lab2.Value());
					}
				}
			}
			return false;
		}

		/// <summary>A hashCode for Constituents done by shifting and or'ing for speed.</summary>
		/// <remarks>
		/// A hashCode for Constituents done by shifting and or'ing for speed.
		/// Now includes the label if the constituent has one (otherwise things
		/// would work very badly if you were hashing constituents over the
		/// same span....).
		/// </remarks>
		/// <returns>the integer hashCode</returns>
		public override int GetHashCode()
		{
			int hash = (Start() << 16) | End();
			ILabel lab = Label();
			return (lab == null || lab.Value() == null) ? hash : hash ^ lab.Value().GetHashCode();
		}

		/// <summary>
		/// Detects whether this constituent overlaps a constituent without
		/// nesting, that is, whether they "cross".
		/// </summary>
		/// <param name="c">The constituent to check against</param>
		/// <returns>True if the two constituents cross</returns>
		public virtual bool Crosses(Edu.Stanford.Nlp.Trees.Constituent c)
		{
			return (Start() < c.Start() && c.Start() < End() && End() < c.End()) || (c.Start() < Start() && Start() < c.End() && c.End() < End());
		}

		/// <summary>
		/// Detects whether this constituent overlaps any of a Collection of
		/// Constituents without
		/// nesting, that is, whether it "crosses" any of them.
		/// </summary>
		/// <param name="constColl">The set of constituent to check against</param>
		/// <returns>True if some constituent in the collection is crossed</returns>
		/// <exception cref="System.InvalidCastException">
		/// If some member of the Collection isn't
		/// a Constituent
		/// </exception>
		public virtual bool Crosses(ICollection<Edu.Stanford.Nlp.Trees.Constituent> constColl)
		{
			foreach (Edu.Stanford.Nlp.Trees.Constituent c in constColl)
			{
				if (Crosses(c))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Detects whether this constituent contains a constituent, that is
		/// whether they are nested.
		/// </summary>
		/// <remarks>
		/// Detects whether this constituent contains a constituent, that is
		/// whether they are nested.  That is, the other constituent's yield is
		/// a sublist of this constituent's yield.
		/// </remarks>
		/// <param name="c">The constituent to check against</param>
		/// <returns>True if the other Constituent is contained in this one</returns>
		public virtual bool Contains(Edu.Stanford.Nlp.Trees.Constituent c)
		{
			return Start() <= c.Start() && End() >= c.End();
		}

		// -- below here is stuff to implement the Label interface
		/// <summary>Return the value of the label (or null if none).</summary>
		/// <returns>String the value for the label</returns>
		public virtual string Value()
		{
			ILabel lab = Label();
			if (lab == null)
			{
				return null;
			}
			return lab.Value();
		}

		/// <summary>Set the value for the label (if one is stored).</summary>
		/// <param name="value">The value for the label</param>
		public virtual void SetValue(string value)
		{
			ILabel lab = Label();
			if (lab != null)
			{
				lab.SetValue(value);
			}
		}

		/// <summary>
		/// Make a new label with this <code>String</code> as the "name", perhaps
		/// by doing some appropriate decoding of the string.
		/// </summary>
		/// <param name="labelStr">
		/// the String that translates into the content of the
		/// label
		/// </param>
		public virtual void SetFromString(string labelStr)
		{
			ILabel lab = Label();
			if (lab != null)
			{
				lab.SetFromString(labelStr);
			}
		}

		/// <summary>
		/// Print out as a string the subpart of a sentence covered
		/// by this <code>Constituent</code>.
		/// </summary>
		/// <returns>The subpart of the sentence</returns>
		public virtual string ToSentenceString(ArrayList s)
		{
			// TODO: genericize this!
			StringBuilder sb = new StringBuilder();
			for (int wordNum = Start(); wordNum <= end; wordNum++)
			{
				sb.Append(s[wordNum]);
				if (wordNum != end)
				{
					sb.Append(" ");
				}
			}
			return sb.ToString();
		}

		public abstract ILabelFactory LabelFactory();
	}
}
