

namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>
	/// grenager
	/// Date: Apr 18, 2005
	/// </author>
	public class FactoredSequenceListener : ISequenceListener
	{
		internal ISequenceListener model1;

		internal ISequenceListener model2;

		internal ISequenceListener[] models = null;

		/// <summary>Informs this sequence model that the value of the element at position pos has changed.</summary>
		/// <remarks>
		/// Informs this sequence model that the value of the element at position pos has changed.
		/// This allows this sequence model to update its internal model if desired.
		/// </remarks>
		public virtual void UpdateSequenceElement(int[] sequence, int pos, int oldVal)
		{
			if (models != null)
			{
				foreach (ISequenceListener model in models)
				{
					model.UpdateSequenceElement(sequence, pos, oldVal);
				}
				return;
			}
			model1.UpdateSequenceElement(sequence, pos, oldVal);
			model2.UpdateSequenceElement(sequence, pos, oldVal);
		}

		/// <summary>Informs this sequence model that the value of the whole sequence is initialized to sequence</summary>
		public virtual void SetInitialSequence(int[] sequence)
		{
			if (models != null)
			{
				foreach (ISequenceListener model in models)
				{
					model.SetInitialSequence(sequence);
				}
				return;
			}
			model1.SetInitialSequence(sequence);
			model2.SetInitialSequence(sequence);
		}

		public FactoredSequenceListener(ISequenceListener model1, ISequenceListener model2)
		{
			this.model1 = model1;
			this.model2 = model2;
		}

		public FactoredSequenceListener(ISequenceListener[] models)
		{
			this.models = models;
		}
	}
}
