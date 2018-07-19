using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Lang.Ref;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// Shared methods for training a
	/// <see cref="LinearClassifier{L, F}"/>
	/// .
	/// Inheriting classes need to implement the
	/// <code>trainWeights</code> method.
	/// </summary>
	/// <author>Dan Klein</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public abstract class AbstractLinearClassifierFactory<L, F> : IClassifierFactory<L, F, IClassifier<L, F>>
	{
		private const long serialVersionUID = 1L;

		internal IIndex<L> labelIndex = new HashIndex<L>();

		internal IIndex<F> featureIndex = new HashIndex<F>();

		public AbstractLinearClassifierFactory()
		{
		}

		internal virtual int NumFeatures()
		{
			return featureIndex.Size();
		}

		internal virtual int NumClasses()
		{
			return labelIndex.Size();
		}

		protected internal abstract double[][] TrainWeights(GeneralDataset<L, F> dataset);

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
		/// objects and gives you back a
		/// <see cref="IClassifier{L, F}"/>
		/// trained on it.
		/// </summary>
		/// <param name="examples">
		/// 
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
		/// objects to train the
		/// classifier on
		/// </param>
		/// <returns>
		/// A
		/// <see cref="IClassifier{L, F}"/>
		/// trained on it.
		/// </returns>
		public virtual LinearClassifier<L, F> TrainClassifier(ICollection<IDatum<L, F>> examples)
		{
			Dataset<L, F> dataset = new Dataset<L, F>();
			dataset.AddAll(examples);
			return TrainClassifier(dataset);
		}

		/// <summary>
		/// Takes a
		/// <see cref="Java.Lang.Ref.Reference{T}"/>
		/// to a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
		/// objects and gives you back a
		/// <see cref="IClassifier{L, F}"/>
		/// trained on them
		/// </summary>
		/// <param name="ref">
		/// 
		/// <see cref="Java.Lang.Ref.Reference{T}"/>
		/// to a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
		/// objects to train the classifier on
		/// </param>
		/// <returns>A Classifier trained on a collection of Datum</returns>
		public virtual LinearClassifier<L, F> TrainClassifier<_T0>(Reference<_T0> @ref)
			where _T0 : ICollection<IDatum<L, F>>
		{
			ICollection<IDatum<L, F>> examples = @ref.Get();
			return TrainClassifier(examples);
		}

		/// <summary>
		/// Trains a
		/// <see cref="IClassifier{L, F}"/>
		/// on a
		/// <see cref="Dataset{L, F}"/>
		/// .
		/// </summary>
		/// <returns>
		/// A
		/// <see cref="IClassifier{L, F}"/>
		/// trained on the data.
		/// </returns>
		public virtual LinearClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data)
		{
			labelIndex = data.LabelIndex();
			featureIndex = data.FeatureIndex();
			double[][] weights = TrainWeights(data);
			return new LinearClassifier<L, F>(weights, featureIndex, labelIndex);
		}
	}
}
