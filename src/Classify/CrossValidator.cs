using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// This class is meant to simplify performing cross validation of
	/// classifiers for hyper-parameters.
	/// </summary>
	/// <remarks>
	/// This class is meant to simplify performing cross validation of
	/// classifiers for hyper-parameters.  It has the ability to save
	/// state for each fold (for instance, the weights for a MaxEnt
	/// classifier, and the alphas for an SVM).
	/// </remarks>
	/// <author>Aria Haghighi</author>
	/// <author>Jenny Finkel</author>
	/// <author>Sarah Spikes (Templatization)</author>
	public class CrossValidator<L, F>
	{
		private readonly GeneralDataset<L, F> originalTrainData;

		private readonly int kFold;

		private readonly CrossValidator<L,F>.SavedState[] savedStates;

		public CrossValidator(GeneralDataset<L, F> trainData)
			: this(trainData, 10)
		{
		}

		public CrossValidator(GeneralDataset<L, F> trainData, int kFold)
		{
			originalTrainData = trainData;
			this.kFold = kFold;
			savedStates = new CrossValidator.SavedState[kFold];
			for (int i = 0; i < savedStates.Length; i++)
			{
				savedStates[i] = new CrossValidator.SavedState();
			}
		}

		/// <summary>Returns an Iterator over train/test/saved states.</summary>
		/// <returns>An Iterator over train/test/saved states</returns>
		private IEnumerator<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator<L,F>.SavedState>> Iterator()
		{
			return new CrossValidator.CrossValidationIterator(this);
		}

		/// <summary>This computes the average over all folds of the function we're trying to optimize.</summary>
		/// <remarks>
		/// This computes the average over all folds of the function we're trying to optimize.
		/// The input triple contains, in order, the train set, the test set, and the saved state.
		/// You don't have to use the saved state if you don't want to.
		/// </remarks>
		public virtual double ComputeAverage(IToDoubleFunction<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>> function)
		{
			double sum = 0;
			IEnumerator<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>> foldIt = Iterator();
			while (foldIt.MoveNext())
			{
				sum += function.ApplyAsDouble(foldIt.Current);
			}
			return sum / kFold;
		}

		internal class CrossValidationIterator : IEnumerator<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>>
		{
			private int iter = 0;

			public virtual bool MoveNext()
			{
				return this.iter < this._enclosing.kFold;
			}

			public virtual void Remove()
			{
				throw new NotSupportedException("CrossValidationIterator doesn't support remove()");
			}

			public virtual Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState> Current
			{
				get
				{
					if (this.iter == this._enclosing.kFold)
					{
						throw new NoSuchElementException("CrossValidatorIterator exhausted.");
					}
					int start = this._enclosing.originalTrainData.Size() * this.iter / this._enclosing.kFold;
					int end = this._enclosing.originalTrainData.Size() * (this.iter + 1) / this._enclosing.kFold;
					//Logging.logger(this.getClass()).info("##train data size: " +  originalTrainData.size() + " start " + start + " end " + end);
					Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> split = this._enclosing.originalTrainData.Split(start, end);
					return new Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>(split.First(), split.Second(), this._enclosing.savedStates[this.iter++]);
				}
			}

			internal CrossValidationIterator(CrossValidator<L, F> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly CrossValidator<L, F> _enclosing;
		}

		public class SavedState
		{
			public object state;
			// end class CrossValidationIterator
		}

		public static void Main(string[] args)
		{
			Dataset<string, string> d = Dataset.ReadSVMLightFormat(args[0]);
			IEnumerator<Triple<GeneralDataset<string, string>, GeneralDataset<string, string>, CrossValidator.SavedState>> it = (new CrossValidator<string, string>(d)).Iterator();
			while (it.MoveNext())
			{
				it.Current;
			}
		}
	}
}
