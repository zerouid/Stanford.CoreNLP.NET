using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A central place for utility functions used when training robust logistic models.</summary>
	/// <author>jtibs</author>
	public class LogisticUtils
	{
		public static int[][] IdentityMatrix(int n)
		{
			int[][] result = new int[n][];
			for (int i = 0; i < n; i++)
			{
				result[i][0] = i;
			}
			return result;
		}

		public static double[] Flatten(double[][] input)
		{
			int length = 0;
			foreach (double[] array in input)
			{
				length += array.Length;
			}
			double[] result = new double[length];
			int count = 0;
			foreach (double[] array_1 in input)
			{
				foreach (double value in array_1)
				{
					result[count++] = value;
				}
			}
			return result;
		}

		public static void Unflatten(double[] input, double[][] output)
		{
			int count = 0;
			for (int i = 0; i < output.Length; i++)
			{
				for (int j = 0; j < output[i].Length; j++)
				{
					output[i][j] = input[count++];
				}
			}
		}

		public static double DotProduct(double[] array, int[] indices, double[] values)
		{
			double result = 0;
			for (int i = 0; i < indices.Length; i++)
			{
				if (indices[i] == -1)
				{
					continue;
				}
				result += array[indices[i]] * values[i];
			}
			return result;
		}

		public static double[][] InitializeDataValues(int[][] data)
		{
			double[][] result = new double[data.Length][];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = new double[data[i].Length];
				Arrays.Fill(result[i], 1.0);
			}
			return result;
		}

		public static int[] IndicesOf<T>(ICollection<T> input, IIndex<T> index)
		{
			int[] result = new int[input.Count];
			int count = 0;
			foreach (T element in input)
			{
				result[count++] = index.IndexOf(element);
			}
			return result;
		}

		public static double[] ConvertToArray(ICollection<double> input)
		{
			double[] result = new double[input.Count];
			int count = 0;
			foreach (double d in input)
			{
				result[count++] = d;
			}
			return result;
		}

		public static double[] CalculateSums(double[][] weights, int[] featureIndices, double[] featureValues)
		{
			int numClasses = weights.Length + 1;
			double[] result = new double[numClasses];
			result[0] = 0.0;
			for (int c = 1; c < numClasses; c++)
			{
				result[c] = -DotProduct(weights[c - 1], featureIndices, featureValues);
			}
			double total = ArrayMath.LogSum(result);
			for (int c_1 = 0; c_1 < numClasses; c_1++)
			{
				result[c_1] -= total;
			}
			return result;
		}

		public static double[] CalculateSums(double[][] weights, int[] featureIndices, double[] featureValues, double[] intercepts)
		{
			int numClasses = weights.Length + 1;
			double[] result = new double[numClasses];
			result[0] = 0.0;
			for (int c = 1; c < numClasses; c++)
			{
				result[c] = -DotProduct(weights[c - 1], featureIndices, featureValues) - intercepts[c - 1];
			}
			double total = ArrayMath.LogSum(result);
			for (int c_1 = 0; c_1 < numClasses; c_1++)
			{
				result[c_1] -= total;
			}
			return result;
		}

		public static double[] CalculateSigmoids(double[][] weights, int[] featureIndices, double[] featureValues)
		{
			return ArrayMath.Exp(CalculateSums(weights, featureIndices, featureValues));
		}

		public static double GetValue(double[][] weights, LogPrior prior)
		{
			double[] flatWeights = Flatten(weights);
			return prior.Compute(flatWeights, new double[flatWeights.Length]);
		}

		public static int Sample(double[] sigmoids)
		{
			double probability = System.Math.Random();
			System.Console.Out.WriteLine("sigmoids: " + Arrays.ToString(sigmoids));
			System.Console.Out.WriteLine("probability: " + probability);
			double offset = 0.0;
			for (int c = 0; c < sigmoids.Length; c++)
			{
				if (probability - offset <= sigmoids[c])
				{
					return c;
				}
				offset += sigmoids[c];
			}
			return sigmoids.Length - 1;
		}

		// should never be reached
		public static void PrettyPrint(double[][] gammas, double[][] thetas, double[][] zprobs)
		{
			PrettyPrint("GAMMAS", gammas);
			PrettyPrint("THETAS", thetas);
			PrettyPrint("ZPROBS", zprobs);
		}

		public static void PrettyPrint(string name, double[][] matrix)
		{
			PrettyPrint(name, matrix, matrix.Length);
		}

		public static void PrettyPrint(string name, double[][] matrix, int maxCount)
		{
			System.Console.Out.WriteLine(name + ": ");
			foreach (double[] array in matrix)
			{
				System.Console.Out.WriteLine(Arrays.ToString(array));
				if (maxCount-- < 0)
				{
					break;
				}
			}
			System.Console.Out.WriteLine();
		}
	}
}
