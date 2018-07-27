using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// A simple interface for classifying and scoring data points, implemented
	/// by most of the classifiers in this package.
	/// </summary>
	/// <remarks>
	/// A simple interface for classifying and scoring data points, implemented
	/// by most of the classifiers in this package.  A basic Classifier
	/// works over a List of categorical features.  For classifiers over
	/// real-valued features, see
	/// <see cref="IRVFClassifier{L, F}"/>
	/// .
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public interface IClassifier<L, F>
	{
		L ClassOf(IDatum<L, F> example);

		ICounter<L> ScoresOf(IDatum<L, F> example);

		ICollection<L> Labels();

		/// <summary>Evaluates the precision and recall of this classifier against a dataset, and the target label.</summary>
		/// <param name="testData">The dataset to evaluate the classifier on.</param>
		/// <param name="targetLabel">The target label (e.g., for relation extraction, this is the relation we're interested in).</param>
		/// <returns>A pair of the precision (first) and recall (second) of the classifier on the target label.</returns>
		Pair<double, double> EvaluatePrecisionAndRecall(GeneralDataset<L, F> testData, L targetLabel);

		// Variables to count
		// Iterate over dataset
		// Get the gold label
		// Get the guess label
		// Compute statistics on datum
		// Aggregate statistics
		/// <summary>Evaluate the accuracy of this classifier on the given dataset.</summary>
		/// <param name="testData">The dataset to evaluate the classifier on.</param>
		/// <returns>The accuracy of the classifier on the given dataset.</returns>
		double EvaluateAccuracy(GeneralDataset<L, F> testData);
		// Get the gold label
		// Get the guess
		// Compute statistics
	}
}
