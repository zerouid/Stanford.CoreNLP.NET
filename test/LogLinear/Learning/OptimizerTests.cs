using System;
using Edu.Stanford.Nlp.Loglinear.Model;

using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/26/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This does its best to Quickcheck our optimizers. The strategy here is to generate convex functions that are solvable
	/// in closed form, and then test that our optimizer is able to achieve a nearly optimal solution at convergence.
	/// </author>
	public class OptimizerTests
	{
		[DataPoint]
		public static AbstractBatchOptimizer backtrackingAdaGrad = new BacktrackingAdaGradOptimizer();

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestOptimizeLogLikelihood(AbstractBatchOptimizer optimizer, GraphicalModel[] dataset, ConcatVector initialWeights, double l2regularization)
		{
			AbstractDifferentiableFunction<GraphicalModel> ll = new LogLikelihoodDifferentiableFunction();
			ConcatVector finalWeights = optimizer.Optimize((GraphicalModel[])dataset, ll, (ConcatVector)initialWeights, (double)l2regularization, 1.0e-9, true);
			System.Console.Error.WriteLine("Finished optimizing");
			double logLikelihood = GetValueSum((GraphicalModel[])dataset, finalWeights, ll, (double)l2regularization);
			// Check in a whole bunch of random directions really nearby that there is no nearby point with a higher log
			// likelihood
			Random r = new Random(42);
			for (int i = 0; i < 1000; i++)
			{
				int size = finalWeights.GetNumberOfComponents();
				ConcatVector randomDirection = new ConcatVector(size);
				for (int j = 0; j < size; j++)
				{
					double[] dense = new double[finalWeights.IsComponentSparse(j) ? finalWeights.GetSparseIndex(j) + 1 : finalWeights.GetDenseComponent(j).Length];
					for (int k = 0; k < dense.Length; k++)
					{
						dense[k] = (r.NextDouble() - 0.5) * 1.0e-3;
					}
					randomDirection.SetDenseComponent(j, dense);
				}
				ConcatVector randomPerturbation = finalWeights.DeepClone();
				randomPerturbation.AddVectorInPlace(randomDirection, 1.0);
				double randomPerturbedLogLikelihood = GetValueSum((GraphicalModel[])dataset, randomPerturbation, ll, (double)l2regularization);
				// Check that we're within a very small margin of error (around 3 decimal places) of the randomly
				// discovered value
				if (logLikelihood < randomPerturbedLogLikelihood - (1.0e-3 * Math.Max(1.0, Math.Abs(logLikelihood))))
				{
					System.Console.Error.WriteLine("Thought optimal point was: " + logLikelihood);
					System.Console.Error.WriteLine("Discovered better point: " + randomPerturbedLogLikelihood);
				}
				NUnit.Framework.Assert.IsTrue(logLikelihood >= randomPerturbedLogLikelihood - (1.0e-3 * Math.Max(1.0, Math.Abs(logLikelihood))));
			}
		}

		/*
		@Theory
		public void testOptimizeLogLikelihoodWithConstraints(AbstractBatchOptimizer optimizer,
		@ForAll(sampleSize = 5) @From(LogLikelihoodFunctionTest.GraphicalModelDatasetGenerator.class) GraphicalModel[] dataset,
		@ForAll(sampleSize = 2) @From(LogLikelihoodFunctionTest.WeightsGenerator.class) ConcatVector initialWeights,
		@ForAll(sampleSize = 2) @InRange(minDouble = 0.0, maxDouble = 5.0) double l2regularization) throws Exception {
		Random r = new Random(42);
		
		int constraintComponent = r.nextInt(initialWeights.getNumberOfComponents());
		double constraintValue = r.nextDouble();
		
		if (r.nextBoolean()) {
		optimizer.addSparseConstraint(constraintComponent, 0, constraintValue);
		} else {
		optimizer.addDenseConstraint(constraintComponent, new double[]{constraintValue});
		}
		
		// Put in some constraints
		
		AbstractDifferentiableFunction<GraphicalModel> ll = new LogLikelihoodDifferentiableFunction();
		ConcatVector finalWeights = optimizer.optimize(dataset, ll, initialWeights, l2regularization, 1.0e-9, false);
		System.err.println("Finished optimizing");
		
		assertEquals(constraintValue, finalWeights.getValueAt(constraintComponent, 0), 1.0e-9);
		
		double logLikelihood = getValueSum(dataset, finalWeights, ll, l2regularization);
		
		// Check in a whole bunch of random directions really nearby that there is no nearby point with a higher log
		// likelihood
		for (int i = 0; i < 1000; i++) {
		int size = finalWeights.getNumberOfComponents();
		ConcatVector randomDirection = new ConcatVector(size);
		for (int j = 0; j < size; j++) {
		if (j == constraintComponent) continue;
		double[] dense = new double[finalWeights.isComponentSparse(j) ? finalWeights.getSparseIndex(j) + 1 : finalWeights.getDenseComponent(j).length];
		for (int k = 0; k < dense.length; k++) {
		dense[k] = (r.nextDouble() - 0.5) * 1.0e-3;
		}
		randomDirection.setDenseComponent(j, dense);
		}
		
		ConcatVector randomPerturbation = finalWeights.deepClone();
		randomPerturbation.addVectorInPlace(randomDirection, 1.0);
		
		double randomPerturbedLogLikelihood = getValueSum(dataset, randomPerturbation, ll, l2regularization);
		
		// Check that we're within a very small margin of error (around 3 decimal places) of the randomly
		// discovered value
		
		if (logLikelihood < randomPerturbedLogLikelihood - (1.0e-3 * Math.max(1.0, Math.abs(logLikelihood)))) {
		System.err.println("Thought optimal point was: " + logLikelihood);
		System.err.println("Discovered better point: " + randomPerturbedLogLikelihood);
		}
		
		assertTrue(logLikelihood >= randomPerturbedLogLikelihood - (1.0e-3 * Math.max(1.0, Math.abs(logLikelihood))));
		}
		}
		*/
		private double GetValueSum<T>(T[] dataset, ConcatVector weights, AbstractDifferentiableFunction<T> fn, double l2regularization)
		{
			double value = 0.0;
			foreach (T t in dataset)
			{
				value += fn.GetSummaryForInstance(t, weights, new ConcatVector(0));
			}
			return (value / dataset.Length) - (weights.DotProduct(weights) * l2regularization);
		}
	}
}
