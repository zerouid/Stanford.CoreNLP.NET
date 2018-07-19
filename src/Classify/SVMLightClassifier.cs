using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>This class represents a trained SVM Classifier.</summary>
	/// <remarks>
	/// This class represents a trained SVM Classifier.  It is actually just a
	/// LinearClassifier, but it can have a Platt (sigmoid) model overlaying
	/// it for the purpose of producing meaningful probabilities.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (templatization)</author>
	[System.Serializable]
	public class SVMLightClassifier<L, F> : LinearClassifier<L, F>
	{
		private const long serialVersionUID = 1L;

		public LinearClassifier<L, L> platt = null;

		public SVMLightClassifier(ClassicCounter<Pair<F, L>> weightCounter, ClassicCounter<L> thresholds)
			: base(weightCounter, thresholds)
		{
		}

		public SVMLightClassifier(ClassicCounter<Pair<F, L>> weightCounter, ClassicCounter<L> thresholds, LinearClassifier<L, L> platt)
			: base(weightCounter, thresholds)
		{
			this.platt = platt;
		}

		public virtual void SetPlatt(LinearClassifier<L, L> platt)
		{
			this.platt = platt;
		}

		/// <summary>
		/// Returns a counter for the log probability of each of the classes
		/// looking at the the sum of e^v for each count v, should be 1
		/// Note: Uses SloppyMath.logSum which isn't exact but isn't as
		/// offensively slow as doing a series of exponentials
		/// </summary>
		public override ICounter<L> LogProbabilityOf(IDatum<L, F> example)
		{
			if (platt == null)
			{
				throw new NotSupportedException("If you want to ask for the probability, you must train a Platt model!");
			}
			ICounter<L> scores = ScoresOf(example);
			scores.IncrementCount(null);
			ICounter<L> probs = platt.LogProbabilityOf(new RVFDatum<L, L>(scores));
			//System.out.println(scores+" "+probs);
			return probs;
		}

		/// <summary>
		/// Returns a counter for the log probability of each of the classes
		/// looking at the the sum of e^v for each count v, should be 1
		/// Note: Uses SloppyMath.logSum which isn't exact but isn't as
		/// offensively slow as doing a series of exponentials
		/// </summary>
		public override ICounter<L> LogProbabilityOf(RVFDatum<L, F> example)
		{
			if (platt == null)
			{
				throw new NotSupportedException("If you want to ask for the probability, you must train a Platt model!");
			}
			ICounter<L> scores = ScoresOf(example);
			scores.IncrementCount(null);
			ICounter<L> probs = platt.LogProbabilityOf(new RVFDatum<L, L>(scores));
			//System.out.println(scores+" "+probs);
			return probs;
		}
	}
}
