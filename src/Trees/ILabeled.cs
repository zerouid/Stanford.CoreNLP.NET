using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Interface for Objects which have a <code>Label</code>.</summary>
	/// <remarks>
	/// Interface for Objects which have a <code>Label</code>.
	/// For instance, they may be hand-classified with one or more tags.
	/// Note that it is for things that possess
	/// a label via composition, rather than for things that implement
	/// the <code>Label</code> interface.
	/// An implementor might choose to be read-only and throw an
	/// UnsupportedOperationException on the setLabel(s)() commands, but should
	/// minimally implement both commands to return Label(s).
	/// </remarks>
	/// <author>Sep Kamvar</author>
	/// <author>Christopher Manning</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - filled in types</author>
	public interface ILabeled
	{
		/// <summary>Returns the Object's label.</summary>
		/// <returns>
		/// One of the labels of the object (if there are multiple labels,
		/// preferably the primary label, if it exists).
		/// Returns null if there is no label.
		/// </returns>
		ILabel Label();

		/// <summary>Sets the label associated with this object.</summary>
		/// <param name="label">The Label value</param>
		void SetLabel(ILabel label);

		/// <summary>Gives back all labels for this thing.</summary>
		/// <returns>
		/// A Collection of the Object's labels.  Returns an empty
		/// Collection if there are no labels.
		/// </returns>
		ICollection<ILabel> Labels();

		/// <summary>Sets the labels associated with this object.</summary>
		/// <param name="labels">The set of Label values</param>
		void SetLabels(ICollection<ILabel> labels);
	}
}
