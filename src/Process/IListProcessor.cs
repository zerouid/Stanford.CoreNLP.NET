using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>An interface for things that operate on a List.</summary>
	/// <remarks>
	/// An interface for things that operate on a List.  This is seen as
	/// a lighter weight and more general interface than the Processor interface
	/// for documents.  IN and OUT are the type of the objects in the List.
	/// The <code>process</code> method acts on a List of IN and produces a List
	/// of OUT.
	/// </remarks>
	/// <author>Teg Grenager</author>
	public interface IListProcessor<In, Out>
	{
		/// <summary>
		/// Take a List (including a Sentence) of input, and return a
		/// List that has been processed in some way.
		/// </summary>
		IList<OUT> Process<_T0>(IList<_T0> list)
			where _T0 : IN;
	}
}
