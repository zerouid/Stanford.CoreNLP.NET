using System.Collections.Generic;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This interface is used for writing
	/// lattices out of
	/// <see cref="SequenceClassifier"/>
	/// s.
	/// </summary>
	/// <author>Michel Galley</author>
	public interface ILatticeWriter<In, T, S>
		where In : ICoreMap
	{
		/// <summary>
		/// This method prints the output lattice (typically, Viterbi search graph) of
		/// the classifier to a
		/// <see cref="Java.IO.PrintWriter"/>
		/// .
		/// </summary>
		void PrintLattice(DFSA<T, S> tagLattice, IList<In> doc, PrintWriter @out);
	}
}
