using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Inference
{
	/// <summary>Created on 8/11/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Holds a factor populated by doubles that knows how to do all the important operations for PGM inference. Internally,
	/// these are just different flavors of two basic data-flow operations:
	/// <p>
	/// - Factor product
	/// - Factor marginalization
	/// <p>
	/// The output here is different ways to grow and shrink factors that turn out to be useful for downstream uses in PGMs.
	/// Basically, we care about message passing, as that will be the primary operation.
	/// <p>
	/// Everything is represented as log-linear, because the primary use for TableFactor is in CliqueTree, and that is
	/// intended for use with log-linear models.
	/// </author>
	public class TableFactor : NDArrayDoubles
	{
		public int[] neighborIndices;

		/// <summary>Construct a TableFactor for inference within a model.</summary>
		/// <remarks>
		/// Construct a TableFactor for inference within a model. This just copies the important bits from the model factor,
		/// and replaces the ConcatVectorTable with an internal datastructure that has done all the dotproducts with the
		/// weights out, and so stores only doubles.
		/// <p>
		/// Each element of the table is given by: t_i = exp(f_i*w)
		/// </remarks>
		/// <param name="weights">the vector to dot product with every element of the factor table</param>
		/// <param name="factor">the feature factor to be multiplied in</param>
		public TableFactor(ConcatVector weights, GraphicalModel.Factor factor)
			: base(factor.featuresTable.GetDimensions())
		{
			this.neighborIndices = factor.neigborIndices;
			// Calculate the factor residents by dot product with the weights
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = factor.featuresTable.FastPassByReferenceIterator();
			int[] assignment = fastPassByReferenceIterator.Current;
			while (true)
			{
				SetAssignmentLogValue(assignment, factor.featuresTable.GetAssignmentValue(assignment).Get().DotProduct(weights));
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

		/// <summary>Fast approximation of the exp() function.</summary>
		/// <remarks>
		/// Fast approximation of the exp() function.
		/// This approximation was suggested in the paper
		/// A Fast, Compact Approximation of the Exponential Function
		/// http://nic.schraudolph.org/pubs/Schraudolph99.pdf
		/// by Nicol N. Schraudolph. However, it does not seem accurate
		/// enough to be a good default for CRFs.
		/// </remarks>
		/// <param name="val">The value to be exponentiated</param>
		/// <returns>The exponentiated value</returns>
		public static double Exp(double val)
		{
			long tmp = (long)(1512775 * val + 1072632447);
			return double.LongBitsToDouble(tmp << 32);
		}

		public const bool UseExpApprox = false;

		/// <summary>Construct a TableFactor for inference within a model.</summary>
		/// <remarks>
		/// Construct a TableFactor for inference within a model. This is the same as the other constructor, except that the
		/// table is observed out before any unnecessary dot products are done out, so hopefully we dramatically reduce the
		/// number of computations required to calculate the resulting table.
		/// <p>
		/// Each element of the table is given by: t_i = exp(f_i*w)
		/// </remarks>
		/// <param name="weights">the vector to dot product with every element of the factor table</param>
		/// <param name="factor">the feature factor to be multiplied in</param>
		public TableFactor(ConcatVector weights, GraphicalModel.Factor factor, int[] observations)
			: base()
		{
			System.Diagnostics.Debug.Assert((observations.Length == factor.neigborIndices.Length));
			int size = 0;
			foreach (int observation in observations)
			{
				if (observation == -1)
				{
					size++;
				}
			}
			neighborIndices = new int[size];
			dimensions = new int[size];
			int[] forwardPointers = new int[size];
			int[] factorAssignment = new int[factor.neigborIndices.Length];
			int cursor = 0;
			for (int i = 0; i < factor.neigborIndices.Length; i++)
			{
				if (observations[i] == -1)
				{
					neighborIndices[cursor] = factor.neigborIndices[i];
					dimensions[cursor] = factor.featuresTable.GetDimensions()[i];
					forwardPointers[cursor] = i;
					cursor++;
				}
				else
				{
					factorAssignment[i] = observations[i];
				}
			}
			System.Diagnostics.Debug.Assert((cursor == size));
			values = new double[CombinatorialNeighborStatesCount()];
			foreach (int[] assn in this)
			{
				for (int i_1 = 0; i_1 < assn.Length; i_1++)
				{
					factorAssignment[forwardPointers[i_1]] = assn[i_1];
				}
				SetAssignmentLogValue(assn, factor.featuresTable.GetAssignmentValue(factorAssignment).Get().DotProduct(weights));
			}
		}

		/// <summary>Remove a variable by observing it at a certain value, return a new factor without that variable.</summary>
		/// <param name="variable">the variable to be observed</param>
		/// <param name="value">the value the variable takes when observed</param>
		/// <returns>a new factor with 'variable' in it</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Inference.TableFactor Observe(int variable, int value)
		{
			return Marginalize(variable, 0, null);
		}

		// This would mean that we're observing something with 0 probability, which will wonk up downstream
		// stuff
		// assert(n != 0);
		/// <summary>Returns the summed marginals for each element in the factor.</summary>
		/// <remarks>
		/// Returns the summed marginals for each element in the factor. These are represented in log space, and are summed
		/// using the numerically stable variant, even though it's slightly slower.
		/// </remarks>
		/// <returns>an array of doubles one-to-one with variable states for each variable</returns>
		public virtual double[][] GetSummedMarginals()
		{
			double[][] results = new double[neighborIndices.Length][];
			for (int i = 0; i < neighborIndices.Length; i++)
			{
				results[i] = new double[GetDimensions()[i]];
			}
			double[][] maxValues = new double[neighborIndices.Length][];
			for (int i_1 = 0; i_1 < neighborIndices.Length; i_1++)
			{
				maxValues[i_1] = new double[GetDimensions()[i_1]];
				for (int j = 0; j < maxValues[i_1].Length; j++)
				{
					maxValues[i_1][j] = double.NegativeInfinity;
				}
			}
			// Get max values
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
			int[] assignment = fastPassByReferenceIterator.Current;
			while (true)
			{
				double v = GetAssignmentLogValue(assignment);
				for (int i_2 = 0; i_2 < neighborIndices.Length; i_2++)
				{
					if (maxValues[i_2][assignment[i_2]] < v)
					{
						maxValues[i_2][assignment[i_2]] = v;
					}
				}
				// This mutates the resultAssignment[] array, rather than creating a new one
				if (fastPassByReferenceIterator.MoveNext())
				{
					fastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
			// Do the summation
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> secondFastPassByReferenceIterator = FastPassByReferenceIterator();
			assignment = secondFastPassByReferenceIterator.Current;
			while (true)
			{
				double v = GetAssignmentLogValue(assignment);
				for (int i_2 = 0; i_2 < neighborIndices.Length; i_2++)
				{
					results[i_2][assignment[i_2]] += Math.Exp(v - maxValues[i_2][assignment[i_2]]);
				}
				// This mutates the resultAssignment[] array, rather than creating a new one
				if (secondFastPassByReferenceIterator.MoveNext())
				{
					secondFastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
			// normalize results, and move to linear space
			for (int i_3 = 0; i_3 < neighborIndices.Length; i_3++)
			{
				double sum = 0.0;
				for (int j = 0; j < results[i_3].Length; j++)
				{
					results[i_3][j] = Math.Exp(maxValues[i_3][j]) * results[i_3][j];
					sum += results[i_3][j];
				}
				if (double.IsInfinite(sum))
				{
					for (int j_1 = 0; j_1 < results[i_3].Length; j_1++)
					{
						results[i_3][j_1] = 1.0 / results[i_3].Length;
					}
				}
				else
				{
					for (int j_1 = 0; j_1 < results[i_3].Length; j_1++)
					{
						results[i_3][j_1] /= sum;
					}
				}
			}
			return results;
		}

		/// <summary>Convenience function to max out all but one variable, and return the marginal array.</summary>
		/// <returns>an array of doubles one-to-one with variable states for each variable</returns>
		public virtual double[][] GetMaxedMarginals()
		{
			double[][] maxValues = new double[neighborIndices.Length][];
			for (int i = 0; i < neighborIndices.Length; i++)
			{
				maxValues[i] = new double[GetDimensions()[i]];
				for (int j = 0; j < maxValues[i].Length; j++)
				{
					maxValues[i][j] = double.NegativeInfinity;
				}
			}
			// Get max values
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
			int[] assignment = fastPassByReferenceIterator.Current;
			while (true)
			{
				double v = GetAssignmentLogValue(assignment);
				for (int i_1 = 0; i_1 < neighborIndices.Length; i_1++)
				{
					if (maxValues[i_1][assignment[i_1]] < v)
					{
						maxValues[i_1][assignment[i_1]] = v;
					}
				}
				// This mutates the resultAssignment[] array, rather than creating a new one
				if (fastPassByReferenceIterator.MoveNext())
				{
					fastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
			for (int i_2 = 0; i_2 < neighborIndices.Length; i_2++)
			{
				NormalizeLogArr(maxValues[i_2]);
			}
			return maxValues;
		}

		/// <summary>Marginalize out a variable by taking the max value.</summary>
		/// <param name="variable">the variable to be maxed out.</param>
		/// <returns>a table factor that will contain the largest value of the variable being marginalized out.</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Inference.TableFactor MaxOut(int variable)
		{
			return Marginalize(variable, double.NegativeInfinity, null);
		}

		/// <summary>Marginalize out a variable by taking a sum.</summary>
		/// <param name="variable">the variable to be summed out</param>
		/// <returns>a factor with variable removed</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Inference.TableFactor SumOut(int variable)
		{
			// OPTIMIZATION: This is by far the most common case, for linear chain inference, and is worth making fast
			// We can use closed loops, and not bother with using the basic iterator to loop through indices.
			// If this special case doesn't trip, we fall back to the standard (but slower) algorithm for the general case
			if (GetDimensions().Length == 2)
			{
				if (neighborIndices[0] == variable)
				{
					Edu.Stanford.Nlp.Loglinear.Inference.TableFactor marginalized = new Edu.Stanford.Nlp.Loglinear.Inference.TableFactor(new int[] { neighborIndices[1] }, new int[] { GetDimensions()[1] });
					for (int i = 0; i < marginalized.values.Length; i++)
					{
						marginalized.values[i] = 0;
					}
					// We use the stable log-sum-exp trick here, so first we calculate the max
					double[] max = new double[GetDimensions()[1]];
					for (int j = 0; j < GetDimensions()[1]; j++)
					{
						max[j] = double.NegativeInfinity;
					}
					for (int i_1 = 0; i_1 < GetDimensions()[0]; i_1++)
					{
						int k = i_1 * GetDimensions()[1];
						for (int j_1 = 0; j_1 < GetDimensions()[1]; j_1++)
						{
							int index = k + j_1;
							if (values[index] > max[j_1])
							{
								max[j_1] = values[index];
							}
						}
					}
					// Then we take the sum, minus the max
					for (int i_2 = 0; i_2 < GetDimensions()[0]; i_2++)
					{
						int k = i_2 * GetDimensions()[1];
						for (int j_1 = 0; j_1 < GetDimensions()[1]; j_1++)
						{
							int index = k + j_1;
							if (double.IsFinite(max[j_1]))
							{
								marginalized.values[j_1] += Math.Exp(values[index] - max[j_1]);
							}
						}
					}
					// And now we exponentiate, and add back in the values
					for (int j_2 = 0; j_2 < GetDimensions()[1]; j_2++)
					{
						if (double.IsFinite(max[j_2]))
						{
							marginalized.values[j_2] = max[j_2] + Math.Log(marginalized.values[j_2]);
						}
						else
						{
							marginalized.values[j_2] = max[j_2];
						}
					}
					return marginalized;
				}
				else
				{
					System.Diagnostics.Debug.Assert((neighborIndices[1] == variable));
					Edu.Stanford.Nlp.Loglinear.Inference.TableFactor marginalized = new Edu.Stanford.Nlp.Loglinear.Inference.TableFactor(new int[] { neighborIndices[0] }, new int[] { GetDimensions()[0] });
					for (int i = 0; i < marginalized.values.Length; i++)
					{
						marginalized.values[i] = 0;
					}
					// We use the stable log-sum-exp trick here, so first we calculate the max
					double[] max = new double[GetDimensions()[0]];
					for (int i_1 = 0; i_1 < GetDimensions()[0]; i_1++)
					{
						max[i_1] = double.NegativeInfinity;
					}
					for (int i_2 = 0; i_2 < GetDimensions()[0]; i_2++)
					{
						int k = i_2 * GetDimensions()[1];
						for (int j = 0; j < GetDimensions()[1]; j++)
						{
							int index = k + j;
							if (values[index] > max[i_2])
							{
								max[i_2] = values[index];
							}
						}
					}
					// Then we take the sum, minus the max
					for (int i_3 = 0; i_3 < GetDimensions()[0]; i_3++)
					{
						int k = i_3 * GetDimensions()[1];
						for (int j = 0; j < GetDimensions()[1]; j++)
						{
							int index = k + j;
							if (double.IsFinite(max[i_3]))
							{
								marginalized.values[i_3] += Math.Exp(values[index] - max[i_3]);
							}
						}
					}
					// And now we exponentiate, and add back in the values
					for (int i_4 = 0; i_4 < GetDimensions()[0]; i_4++)
					{
						if (double.IsFinite(max[i_4]))
						{
							marginalized.values[i_4] = max[i_4] + Math.Log(marginalized.values[i_4]);
						}
						else
						{
							marginalized.values[i_4] = max[i_4];
						}
					}
					return marginalized;
				}
			}
			else
			{
				// This is a little tricky because we need to use the stable log-sum-exp trick on top of our marginalize
				// dataflow operation.
				// First we calculate all the max values to use as pivots to prevent overflow
				Edu.Stanford.Nlp.Loglinear.Inference.TableFactor maxValues = MaxOut(variable);
				// Then we do the sum against an offset from the pivots
				Edu.Stanford.Nlp.Loglinear.Inference.TableFactor marginalized = Marginalize(variable, 0, null);
				// Then we factor the max values back in, and
				foreach (int[] assignment in marginalized)
				{
					marginalized.SetAssignmentLogValue(assignment, maxValues.GetAssignmentLogValue(assignment) + Math.Log(marginalized.GetAssignmentLogValue(assignment)));
				}
				return marginalized;
			}
		}

		/// <summary>Product two factors, taking the multiplication at the intersections.</summary>
		/// <param name="other">the other factor to be multiplied</param>
		/// <returns>a factor containing the union of both variable sets</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Inference.TableFactor Multiply(Edu.Stanford.Nlp.Loglinear.Inference.TableFactor other)
		{
			// Calculate the result domain
			IList<int> domain = new List<int>();
			IList<int> otherDomain = new List<int>();
			IList<int> resultDomain = new List<int>();
			foreach (int n in neighborIndices)
			{
				domain.Add(n);
				resultDomain.Add(n);
			}
			foreach (int n_1 in other.neighborIndices)
			{
				otherDomain.Add(n_1);
				if (!resultDomain.Contains(n_1))
				{
					resultDomain.Add(n_1);
				}
			}
			// Create result TableFactor
			int[] resultNeighborIndices = new int[resultDomain.Count];
			int[] resultDimensions = new int[resultNeighborIndices.Length];
			for (int i = 0; i < resultDomain.Count; i++)
			{
				int var = resultDomain[i];
				resultNeighborIndices[i] = var;
				// assert consistency about variable size, we can't have the same variable with two different sizes
				System.Diagnostics.Debug.Assert(((GetVariableSize(var) == 0 && other.GetVariableSize(var) > 0) || (GetVariableSize(var) > 0 && other.GetVariableSize(var) == 0) || (GetVariableSize(var) == other.GetVariableSize(var))));
				resultDimensions[i] = Math.Max(GetVariableSize(resultDomain[i]), other.GetVariableSize(resultDomain[i]));
			}
			Edu.Stanford.Nlp.Loglinear.Inference.TableFactor result = new Edu.Stanford.Nlp.Loglinear.Inference.TableFactor(resultNeighborIndices, resultDimensions);
			// OPTIMIZATION:
			// If we're a factor of size 2 receiving a message of size 1, then we can optimize that pretty heavily
			// We could just use the general algorithm at the end of this set of special cases, but this is the fastest way
			if (otherDomain.Count == 1 && (resultDomain.Count == domain.Count) && domain.Count == 2)
			{
				int msgVar = otherDomain[0];
				int msgIndex = resultDomain.IndexOf(msgVar);
				if (msgIndex == 0)
				{
					for (int i_1 = 0; i_1 < resultDimensions[0]; i_1++)
					{
						double d = other.values[i_1];
						int k = i_1 * resultDimensions[1];
						for (int j = 0; j < resultDimensions[1]; j++)
						{
							int index = k + j;
							result.values[index] = values[index] + d;
						}
					}
				}
				else
				{
					if (msgIndex == 1)
					{
						for (int i_1 = 0; i_1 < resultDimensions[0]; i_1++)
						{
							int k = i_1 * resultDimensions[1];
							for (int j = 0; j < resultDimensions[1]; j++)
							{
								int index = k + j;
								result.values[index] = values[index] + other.values[j];
							}
						}
					}
				}
			}
			else
			{
				// OPTIMIZATION:
				// The special case where we're a message of size 1, and the other factor is receiving the message, and of size 2
				if (domain.Count == 1 && (resultDomain.Count == otherDomain.Count) && resultDomain.Count == 2)
				{
					return other.Multiply(this);
				}
				else
				{
					// Otherwise we follow the big comprehensive, slow general purpose algorithm
					// Calculate back-pointers from the result domain indices to original indices
					int[] mapping = new int[result.neighborIndices.Length];
					int[] otherMapping = new int[result.neighborIndices.Length];
					for (int i_1 = 0; i_1 < result.neighborIndices.Length; i_1++)
					{
						mapping[i_1] = domain.IndexOf(result.neighborIndices[i_1]);
						otherMapping[i_1] = otherDomain.IndexOf(result.neighborIndices[i_1]);
					}
					// Do the actual joining operation between the two tables, applying 'join' for each result element.
					int[] assignment = new int[neighborIndices.Length];
					int[] otherAssignment = new int[other.neighborIndices.Length];
					// OPTIMIZATION:
					// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
					// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
					IEnumerator<int[]> fastPassByReferenceIterator = result.FastPassByReferenceIterator();
					int[] resultAssignment = fastPassByReferenceIterator.Current;
					while (true)
					{
						// Set the assignment arrays correctly
						for (int i_2 = 0; i_2 < resultAssignment.Length; i_2++)
						{
							if (mapping[i_2] != -1)
							{
								assignment[mapping[i_2]] = resultAssignment[i_2];
							}
							if (otherMapping[i_2] != -1)
							{
								otherAssignment[otherMapping[i_2]] = resultAssignment[i_2];
							}
						}
						result.SetAssignmentLogValue(resultAssignment, GetAssignmentLogValue(assignment) + other.GetAssignmentLogValue(otherAssignment));
						// This mutates the resultAssignment[] array, rather than creating a new one
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
			}
			return result;
		}

		/// <summary>
		/// This is useful for calculating the partition function, and is exposed here because when implemented internally
		/// we can do a much more numerically stable summation.
		/// </summary>
		/// <returns>the sum of all values for all assignments to the TableFactor</returns>
		public virtual double ValueSum()
		{
			// We want the exp(log-sum-exp), for stability
			// This rearranges to exp(a)*(sum-exp)
			double max = 0.0;
			foreach (int[] assignment in this)
			{
				double v = GetAssignmentLogValue(assignment);
				if (v > max)
				{
					max = v;
				}
			}
			double sumExp = 0.0;
			foreach (int[] assignment_1 in this)
			{
				sumExp += Math.Exp(GetAssignmentLogValue(assignment_1) - max);
			}
			return sumExp * Math.Exp(max);
		}

		/// <summary>
		/// Just a pass through to the NDArray version, plus a Math.exp to ensure that to the outside world the TableFactor
		/// doesn't look like it's in log-space
		/// </summary>
		/// <param name="assignment">a list of variable settings, in the same order as the neighbors array of the factor</param>
		/// <returns>the value of the assignment</returns>
		public override double GetAssignmentValue(int[] assignment)
		{
			double d = base.GetAssignmentValue(assignment);
			// if (d == null) d = Double.NEGATIVE_INFINITY;
			return Math.Exp(d);
		}

		/// <summary>
		/// Just a pass through to the NDArray version, plus a Math.log to ensure that to the outside world the TableFactor
		/// doesn't look like it's in log-space
		/// </summary>
		/// <param name="assignment">a list of variable settings, in the same order as the neighbors array of the factor</param>
		/// <param name="value">the value to put into the factor table</param>
		public override void SetAssignmentValue(int[] assignment, double value)
		{
			base.SetAssignmentValue(assignment, Math.Log(value));
		}

		////////////////////////////////////////////////////////////////////////////
		// PRIVATE IMPLEMENTATION
		////////////////////////////////////////////////////////////////////////////
		private double GetAssignmentLogValue(int[] assignment)
		{
			return base.GetAssignmentValue(assignment);
		}

		private void SetAssignmentLogValue(int[] assignment, double value)
		{
			base.SetAssignmentValue(assignment, value);
		}

		/// <summary>
		/// Marginalizes out a variable by applying an associative join operation for each possible assignment to the
		/// marginalized variable.
		/// </summary>
		/// <param name="variable">the variable (by 'name', not offset into neighborIndices)</param>
		/// <param name="startingValue">associativeJoin is basically a foldr over a table, and this is the initialization</param>
		/// <param name="curriedFoldr">
		/// the associative function to use when applying the join operation, taking first the
		/// assignment to the value being marginalized, and then a foldr operation
		/// </param>
		/// <returns>
		/// a new TableFactor that doesn't contain 'variable', where values were gotten through associative
		/// marginalization.
		/// </returns>
		private Edu.Stanford.Nlp.Loglinear.Inference.TableFactor Marginalize(int variable, double startingValue, IBiFunction<int, int[], IBiFunction<double, double, double>> curriedFoldr)
		{
			// Can't marginalize the last variable
			System.Diagnostics.Debug.Assert((GetDimensions().Length > 1));
			// Calculate the result domain
			IList<int> resultDomain = new List<int>();
			foreach (int n in neighborIndices)
			{
				if (n != variable)
				{
					resultDomain.Add(n);
				}
			}
			// Create result TableFactor
			int[] resultNeighborIndices = new int[resultDomain.Count];
			int[] resultDimensions = new int[resultNeighborIndices.Length];
			for (int i = 0; i < resultDomain.Count; i++)
			{
				int var = resultDomain[i];
				resultNeighborIndices[i] = var;
				resultDimensions[i] = GetVariableSize(var);
			}
			Edu.Stanford.Nlp.Loglinear.Inference.TableFactor result = new Edu.Stanford.Nlp.Loglinear.Inference.TableFactor(resultNeighborIndices, resultDimensions);
			// Calculate forward-pointers from the old domain to new domain
			int[] mapping = new int[neighborIndices.Length];
			for (int i_1 = 0; i_1 < neighborIndices.Length; i_1++)
			{
				mapping[i_1] = resultDomain.IndexOf(neighborIndices[i_1]);
			}
			// Initialize
			foreach (int[] assignment in result)
			{
				result.SetAssignmentLogValue(assignment, startingValue);
			}
			// Do the actual fold into the result
			int[] resultAssignment = new int[result.neighborIndices.Length];
			int marginalizedVariableValue = 0;
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
			int[] assignment_1 = fastPassByReferenceIterator.Current;
			while (true)
			{
				// Set the assignment arrays correctly
				for (int i_2 = 0; i_2 < assignment_1.Length; i_2++)
				{
					if (mapping[i_2] != -1)
					{
						resultAssignment[mapping[i_2]] = assignment_1[i_2];
					}
					else
					{
						marginalizedVariableValue = assignment_1[i_2];
					}
				}
				result.SetAssignmentLogValue(resultAssignment, curriedFoldr.Apply(marginalizedVariableValue, resultAssignment).Apply(result.GetAssignmentLogValue(resultAssignment), GetAssignmentLogValue(assignment_1)));
				if (fastPassByReferenceIterator.MoveNext())
				{
					fastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
			return result;
		}

		/// <summary>Address a variable by index to get it's size.</summary>
		/// <remarks>Address a variable by index to get it's size. Basically just a convenience function.</remarks>
		/// <param name="variable">the name, not index into neighbors, of the variable in question</param>
		/// <returns>the size of the factor along this dimension</returns>
		private int GetVariableSize(int variable)
		{
			for (int i = 0; i < neighborIndices.Length; i++)
			{
				if (neighborIndices[i] == variable)
				{
					return GetDimensions()[i];
				}
			}
			return 0;
		}

		/// <summary>Super basic in-place array normalization</summary>
		/// <param name="arr">the array to normalize</param>
		private static void NormalizeLogArr(double[] arr)
		{
			// Find the log-scale normalization value
			double max = double.NegativeInfinity;
			foreach (double d in arr)
			{
				if (d > max)
				{
					max = d;
				}
			}
			double expSum = 0.0;
			foreach (double d_1 in arr)
			{
				expSum += Math.Exp(d_1 - max);
			}
			double logSumExp = max + Math.Log(expSum);
			if (double.IsInfinite(logSumExp))
			{
				// Just put in uniform probabilities if we are normalizing all 0s
				for (int i = 0; i < arr.Length; i++)
				{
					arr[i] = 1.0 / arr.Length;
				}
			}
			else
			{
				// Normalize in log-scale before exponentiation, to help with stability
				for (int i = 0; i < arr.Length; i++)
				{
					arr[i] = Math.Exp(arr[i] - logSumExp);
				}
			}
		}

		/// <summary>FOR PRIVATE USE AND TESTING ONLY</summary>
		internal TableFactor(int[] neighborIndices, int[] dimensions)
			: base(dimensions)
		{
			this.neighborIndices = neighborIndices;
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = double.NegativeInfinity;
			}
		}

		private bool AssertsEnabled()
		{
			bool assertsEnabled = false;
			System.Diagnostics.Debug.Assert((assertsEnabled = true));
			// intentional side effect
			return assertsEnabled;
		}
	}
}
