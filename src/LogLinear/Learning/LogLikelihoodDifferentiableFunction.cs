using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Inference;
using Edu.Stanford.Nlp.Loglinear.Model;


namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/23/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Generates (potentially noisy, no promises about exactness) gradients from a batch of examples that were provided to
	/// the system.
	/// </author>
	public class LogLikelihoodDifferentiableFunction : AbstractDifferentiableFunction<GraphicalModel>
	{
		public const string VariableTrainingValue = "learning.LogLikelihoodDifferentiableFunction.VARIABLE_TRAINING_VALUE";

		// This sets a gold observation for a model to use as training gold data
		/// <summary>
		/// Gets a summary of the log-likelihood of a singe model at a point
		/// <p>
		/// It assumes that the models have observations for training set as metadata in
		/// LogLikelihoodDifferentiableFunction.OBSERVATION_FOR_TRAINING.
		/// </summary>
		/// <remarks>
		/// Gets a summary of the log-likelihood of a singe model at a point
		/// <p>
		/// It assumes that the models have observations for training set as metadata in
		/// LogLikelihoodDifferentiableFunction.OBSERVATION_FOR_TRAINING. The models can also have observations fixed in
		/// CliqueTree.VARIABLE_OBSERVED_VALUE, but these will be considered fixed and will not be learned against.
		/// </remarks>
		/// <param name="model">the model to find the log-likelihood of</param>
		/// <param name="weights">the weights to use</param>
		/// <returns>the gradient and value of the function at that point</returns>
		public override double GetSummaryForInstance(GraphicalModel model, ConcatVector weights, ConcatVector gradient)
		{
			double logLikelihood = 0.0;
			CliqueTree.MarginalResult result = new CliqueTree(model, weights).CalculateMarginals();
			// Cache everything in preparation for multiple redundant requests for feature vectors
			foreach (GraphicalModel.Factor factor in model.factors)
			{
				factor.featuresTable.CacheVectors();
			}
			// Subtract log partition function
			logLikelihood -= Math.Log(result.partitionFunction);
			// Quit if we have an infinite partition function
			if (double.IsInfinite(logLikelihood))
			{
				return 0.0;
			}
			// Add the determined assignment by training values
			foreach (GraphicalModel.Factor factor_1 in model.factors)
			{
				// Find the assignment, taking both fixed and training observed variables into account
				int[] assignment = new int[factor_1.neigborIndices.Length];
				for (int i = 0; i < assignment.Length; i++)
				{
					int deterministicValue = GetDeterministicAssignment(result.marginals[factor_1.neigborIndices[i]]);
					if (deterministicValue != -1)
					{
						assignment[i] = deterministicValue;
					}
					else
					{
						int trainingObservation = System.Convert.ToInt32(model.GetVariableMetaDataByReference(factor_1.neigborIndices[i])[LogLikelihoodDifferentiableFunction.VariableTrainingValue]);
						assignment[i] = trainingObservation;
					}
				}
				ConcatVector features = factor_1.featuresTable.GetAssignmentValue(assignment).Get();
				// Add the log-likelihood from this observation to the log-likelihood
				logLikelihood += features.DotProduct(weights);
				// Add the vector from this observation to the gradient
				gradient.AddVectorInPlace(features, 1.0);
			}
			// Take expectations over features given marginals
			// NOTE: This is extremely expensive. Not sure what to do about that
			foreach (GraphicalModel.Factor factor_2 in model.factors)
			{
				// OPTIMIZATION:
				// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
				// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
				IEnumerator<int[]> fastPassByReferenceIterator = factor_2.featuresTable.FastPassByReferenceIterator();
				int[] assignment = fastPassByReferenceIterator.Current;
				while (true)
				{
					// calculate assignment prob
					double assignmentProb = result.jointMarginals[factor_2].GetAssignmentValue(assignment);
					// subtract this feature set, weighted by the probability of the assignment
					if (assignmentProb > 0)
					{
						gradient.AddVectorInPlace(factor_2.featuresTable.GetAssignmentValue(assignment).Get(), -assignmentProb);
					}
					// This mutates the assignment[] array, rather than creating a new one
					if (fastPassByReferenceIterator.MoveNext())
					{
						fastPassByReferenceIterator.Current;
					}
					else
					{
						break;
					}
				}
			}
			// Uncache everything, now that the computations have completed
			foreach (GraphicalModel.Factor factor_3 in model.factors)
			{
				factor_3.featuresTable.ReleaseCache();
			}
			return logLikelihood;
		}

		/// <summary>Finds the deterministic assignment forced by a distribution, or if none exists returns -1</summary>
		/// <param name="distribution">the potentially deterministic distribution</param>
		/// <returns>the assignment given by the distribution with probability 1, if one exists, else -1</returns>
		private static int GetDeterministicAssignment(double[] distribution)
		{
			int assignment = -1;
			for (int i = 0; i < distribution.Length; i++)
			{
				if (distribution[i] == 1.0)
				{
					if (assignment == -1)
					{
						assignment = i;
					}
					else
					{
						return -1;
					}
				}
				else
				{
					if (distribution[i] != 0.0)
					{
						return -1;
					}
				}
			}
			return assignment;
		}
	}
}
