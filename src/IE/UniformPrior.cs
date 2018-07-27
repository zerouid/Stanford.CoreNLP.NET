using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Uniform prior to be used for generic Gibbs inference in the ie.crf.CRFClassifier.</summary>
	/// <remarks>
	/// Uniform prior to be used for generic Gibbs inference in the ie.crf.CRFClassifier.
	/// If used, CRF will do generic Gibbs inference without any priors.
	/// </remarks>
	/// <author>Mihai</author>
	public class UniformPrior<In> : IListeningSequenceModel
		where In : ICoreMap
	{
		protected internal int[] sequence;

		protected internal readonly int backgroundSymbol;

		protected internal readonly int numClasses;

		protected internal readonly int[] possibleValues;

		protected internal readonly IIndex<string> classIndex;

		protected internal readonly IList<In> doc;

		public UniformPrior(string backgroundSymbol, IIndex<string> classIndex, IList<In> doc)
		{
			this.classIndex = classIndex;
			this.backgroundSymbol = classIndex.IndexOf(backgroundSymbol);
			this.numClasses = classIndex.Size();
			this.possibleValues = new int[numClasses];
			for (int i = 0; i < numClasses; i++)
			{
				possibleValues[i] = i;
			}
			this.doc = doc;
		}

		public virtual double ScoreOf(int[] sequence)
		{
			return 0;
		}

		public virtual double[] ScoresOf(int[] sequence, int position)
		{
			double[] probs = new double[numClasses];
			for (int i = 0; i < probs.Length; i++)
			{
				probs[i] = 0.0;
			}
			return probs;
		}

		public virtual int[] GetPossibleValues(int position)
		{
			return possibleValues;
		}

		public virtual int LeftWindow()
		{
			return int.MaxValue;
		}

		// not Markovian!
		public virtual int Length()
		{
			return doc.Count;
		}

		public virtual int RightWindow()
		{
			return int.MaxValue;
		}

		// not Markovian!
		public virtual double ScoreOf(int[] sequence, int position)
		{
			return 0.0;
		}

		public virtual void SetInitialSequence(int[] sequence)
		{
		}

		public virtual void UpdateSequenceElement(int[] sequence, int pos, int oldVal)
		{
		}
	}
}
