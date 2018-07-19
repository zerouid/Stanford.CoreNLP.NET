using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Stores a factor table as a one dimensional array of doubles.</summary>
	/// <remarks>
	/// Stores a factor table as a one dimensional array of doubles.
	/// This class supports a restricted form of factor table where each
	/// variable has the same set of values, but supports cliques of
	/// arbitrary size.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class FactorTable
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.FactorTable));

		private readonly int numClasses;

		private readonly int windowSize;

		private readonly double[] table;

		public FactorTable(int numClasses, int windowSize)
		{
			this.numClasses = numClasses;
			this.windowSize = windowSize;
			table = new double[SloppyMath.IntPow(numClasses, windowSize)];
			Arrays.Fill(table, double.NegativeInfinity);
		}

		public FactorTable(Edu.Stanford.Nlp.IE.Crf.FactorTable t)
		{
			numClasses = t.NumClasses();
			windowSize = t.WindowSize();
			table = new double[t.Size()];
			System.Array.Copy(t.table, 0, table, 0, t.Size());
		}

		public virtual bool HasNaN()
		{
			return ArrayMath.HasNaN(table);
		}

		public virtual string ToProbString()
		{
			StringBuilder sb = new StringBuilder("{\n");
			for (int i = 0; i < table.Length; i++)
			{
				sb.Append(Arrays.ToString(ToArray(i)));
				sb.Append(": ");
				sb.Append(Prob(ToArray(i)));
				sb.Append('\n');
			}
			sb.Append('}');
			return sb.ToString();
		}

		public virtual string ToNonLogString()
		{
			StringBuilder sb = new StringBuilder("{\n");
			for (int i = 0; i < table.Length; i++)
			{
				sb.Append(Arrays.ToString(ToArray(i)));
				sb.Append(": ");
				sb.Append(System.Math.Exp(GetValue(i)));
				sb.Append('\n');
			}
			sb.Append('}');
			return sb.ToString();
		}

		public virtual string ToString<L>(IIndex<L> classIndex)
		{
			StringBuilder sb = new StringBuilder("{\n");
			for (int i = 0; i < table.Length; i++)
			{
				sb.Append(ToString(ToArray(i), classIndex));
				sb.Append(": ");
				sb.Append(GetValue(i));
				sb.Append('\n');
			}
			sb.Append('}');
			return sb.ToString();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("{\n");
			for (int i = 0; i < table.Length; i++)
			{
				sb.Append(Arrays.ToString(ToArray(i)));
				sb.Append(": ");
				sb.Append(GetValue(i));
				sb.Append('\n');
			}
			sb.Append('}');
			return sb.ToString();
		}

		private static string ToString<L>(int[] array, IIndex<L> classIndex)
		{
			IList<L> l = new List<L>(array.Length);
			foreach (int item in array)
			{
				l.Add(classIndex.Get(item));
			}
			return l.ToString();
		}

		public virtual int[] ToArray(int index)
		{
			int[] indices = new int[windowSize];
			for (int i = indices.Length - 1; i >= 0; i--)
			{
				indices[i] = index % numClasses;
				index /= numClasses;
			}
			return indices;
		}

		/* e.g., numClasses = 4
		[2,3] -> 11
		0 1 2 3
		4 5 6 7
		8 9 10 11
		[0,2] -> 2
		
		summary:
		index % numClasses -> curr timestamp index
		index / numClasses -> prev timestamp index
		*/
		private int IndexOf(int[] entry)
		{
			int index = 0;
			foreach (int item in entry)
			{
				index *= numClasses;
				index += item;
			}
			// if (index < 0) throw new RuntimeException("index=" + index + " entry=" + Arrays.toString(entry)); // only if overflow
			return index;
		}

		private int IndexOf(int[] front, int end)
		{
			int index = 0;
			foreach (int item in front)
			{
				index *= numClasses;
				index += item;
			}
			index *= numClasses;
			index += end;
			return index;
		}

		private int IndexOf(int front, int[] end)
		{
			int index = front;
			foreach (int item in end)
			{
				index *= numClasses;
				index += item;
			}
			return index;
		}

		private int[] IndicesEnd(int[] entries)
		{
			int index = 0;
			foreach (int entry in entries)
			{
				index *= numClasses;
				index += entry;
			}
			int[] indices = new int[SloppyMath.IntPow(numClasses, windowSize - entries.Length)];
			int offset = SloppyMath.IntPow(numClasses, entries.Length);
			for (int i = 0; i < indices.Length; i++)
			{
				indices[i] = index;
				index += offset;
			}
			// log.info("indicesEnd returning: " + Arrays.toString(indices));
			return indices;
		}

		/// <summary>This now returns the first index of the requested entries.</summary>
		/// <remarks>
		/// This now returns the first index of the requested entries.
		/// The run of numClasses ^ (windowSize - entries.length)
		/// successive entries will give all of them.
		/// </remarks>
		/// <param name="entries">The class indices of size windowsSize</param>
		/// <returns>First index of requested entries</returns>
		private int IndicesFront(int[] entries)
		{
			int start = 0;
			foreach (int entry in entries)
			{
				start *= numClasses;
				start += entry;
			}
			int offset = SloppyMath.IntPow(numClasses, windowSize - entries.Length);
			return start * offset;
		}

		public virtual int WindowSize()
		{
			return windowSize;
		}

		public virtual int NumClasses()
		{
			return numClasses;
		}

		public virtual int Size()
		{
			return table.Length;
		}

		public virtual double TotalMass()
		{
			return ArrayMath.LogSum(table);
		}

		/// <summary>Returns a single clique potential.</summary>
		public virtual double UnnormalizedLogProb(int[] label)
		{
			return GetValue(label);
		}

		public virtual double LogProb(int[] label)
		{
			return UnnormalizedLogProb(label) - TotalMass();
		}

		public virtual double Prob(int[] label)
		{
			return System.Math.Exp(UnnormalizedLogProb(label) - TotalMass());
		}

		/// <summary>
		/// Computes the probability of the tag OF being at the end of the table given
		/// that the previous tag sequence in table is GIVEN.
		/// </summary>
		/// <remarks>
		/// Computes the probability of the tag OF being at the end of the table given
		/// that the previous tag sequence in table is GIVEN. given is at the beginning,
		/// of is at the end.
		/// </remarks>
		/// <returns>the probability of the tag OF being at the end of the table</returns>
		public virtual double ConditionalLogProbGivenPrevious(int[] given, int of)
		{
			if (given.Length != windowSize - 1)
			{
				throw new ArgumentException("conditionalLogProbGivenPrevious requires given one less than clique size (" + windowSize + ") but was " + Arrays.ToString(given));
			}
			// Note: other similar methods could be optimized like this one, but this is the one the CRF uses....
			/*
			int startIndex = indicesFront(given);
			int numCellsToSum = SloppyMath.intPow(numClasses, windowSize - given.length);
			double z = ArrayMath.logSum(table, startIndex, startIndex + numCellsToSum);
			int i = indexOf(given, of);
			System.err.printf("startIndex is %d, numCellsToSum is %d, i is %d (of is %d)%n", startIndex, numCellsToSum, i, of);
			*/
			int startIndex = IndicesFront(given);
			double z = ArrayMath.LogSum(table, startIndex, startIndex + numClasses);
			int i = startIndex + of;
			// System.err.printf("startIndex is %d, numCellsToSum is %d, i is %d (of is %d)%n", startIndex, numClasses, i, of);
			return table[i] - z;
		}

		//  public double conditionalLogProbGivenPreviousForPartial(int[] given, int of) {
		//    if (given.length != windowSize - 1) {
		//      log.info("error computing conditional log prob");
		//      System.exit(0);
		//    }
		//    // int[] label = indicesFront(given);
		//    // double[] masses = new double[label.length];
		//    // for (int i = 0; i < masses.length; i++) {
		//    // masses[i] = table[label[i]];
		//    // }
		//    // double z = ArrayMath.logSum(masses);
		//
		//    int i = indexOf(given, of);
		//    // if (SloppyMath.isDangerous(z) || SloppyMath.isDangerous(table[i])) {
		//    // log.info("z="+z);
		//    // log.info("t="+table[i]);
		//    // }
		//
		//    return table[i];
		//  }
		/// <summary>
		/// Computes the probabilities of the tag at the end of the table given that
		/// the previous tag sequence in table is GIVEN.
		/// </summary>
		/// <remarks>
		/// Computes the probabilities of the tag at the end of the table given that
		/// the previous tag sequence in table is GIVEN. given is at the beginning,
		/// position in question is at the end
		/// </remarks>
		/// <returns>the probabilities of the tag at the end of the table</returns>
		public virtual double[] ConditionalLogProbsGivenPrevious(int[] given)
		{
			if (given.Length != windowSize - 1)
			{
				throw new ArgumentException("conditionalLogProbsGivenPrevious requires given one less than clique size (" + windowSize + ") but was " + Arrays.ToString(given));
			}
			double[] result = new double[numClasses];
			for (int i = 0; i < numClasses; i++)
			{
				int index = IndexOf(given, i);
				result[i] = table[index];
			}
			ArrayMath.LogNormalize(result);
			return result;
		}

		/// <summary>
		/// Computes the probability of the sequence OF being at the end of the table
		/// given that the first tag in table is GIVEN.
		/// </summary>
		/// <remarks>
		/// Computes the probability of the sequence OF being at the end of the table
		/// given that the first tag in table is GIVEN. given is at the beginning, of is
		/// at the end
		/// </remarks>
		/// <returns>the probability of the sequence of being at the end of the table</returns>
		public virtual double ConditionalLogProbGivenFirst(int given, int[] of)
		{
			if (of.Length != windowSize - 1)
			{
				throw new ArgumentException("conditionalLogProbGivenFirst requires of one less than clique size (" + windowSize + ") but was " + Arrays.ToString(of));
			}
			// compute P(given, of)
			int[] labels = new int[windowSize];
			labels[0] = given;
			System.Array.Copy(of, 0, labels, 1, windowSize - 1);
			// double probAll = logProb(labels);
			double probAll = UnnormalizedLogProb(labels);
			// compute P(given)
			// double probGiven = logProbFront(given);
			double probGiven = UnnormalizedLogProbFront(given);
			// compute P(given, of) / P(given)
			return probAll - probGiven;
		}

		/// <summary>
		/// Computes the probability of the sequence OF being at the end of the table
		/// given that the first tag in table is GIVEN.
		/// </summary>
		/// <remarks>
		/// Computes the probability of the sequence OF being at the end of the table
		/// given that the first tag in table is GIVEN. given is at the beginning, of is
		/// at the end.
		/// </remarks>
		/// <returns>the probability of the sequence of being at the end of the table</returns>
		public virtual double UnnormalizedConditionalLogProbGivenFirst(int given, int[] of)
		{
			if (of.Length != windowSize - 1)
			{
				throw new ArgumentException("unnormalizedConditionalLogProbGivenFirst requires of one less than clique size (" + windowSize + ") but was " + Arrays.ToString(of));
			}
			// compute P(given, of)
			int[] labels = new int[windowSize];
			labels[0] = given;
			System.Array.Copy(of, 0, labels, 1, windowSize - 1);
			// double probAll = logProb(labels);
			double probAll = UnnormalizedLogProb(labels);
			// compute P(given)
			// double probGiven = logProbFront(given);
			// double probGiven = unnormalizedLogProbFront(given);
			// compute P(given, of) / P(given)
			// return probAll - probGiven;
			return probAll;
		}

		/// <summary>
		/// Computes the probability of the tag OF being at the beginning of the table
		/// given that the tag sequence GIVEN is at the end of the table.
		/// </summary>
		/// <remarks>
		/// Computes the probability of the tag OF being at the beginning of the table
		/// given that the tag sequence GIVEN is at the end of the table. given is at
		/// the end, of is at the beginning
		/// </remarks>
		/// <returns>the probability of the tag of being at the beginning of the table</returns>
		public virtual double ConditionalLogProbGivenNext(int[] given, int of)
		{
			if (given.Length != windowSize - 1)
			{
				throw new ArgumentException("conditionalLogProbGivenNext requires given one less than clique size (" + windowSize + ") but was " + Arrays.ToString(given));
			}
			int[] label = IndicesEnd(given);
			double[] masses = new double[label.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[label[i]];
			}
			double z = ArrayMath.LogSum(masses);
			return table[IndexOf(of, given)] - z;
		}

		public virtual double UnnormalizedLogProbFront(int[] labels)
		{
			int startIndex = IndicesFront(labels);
			int numCellsToSum = SloppyMath.IntPow(numClasses, windowSize - labels.Length);
			// double[] masses = new double[labels.length];
			// for (int i = 0; i < masses.length; i++) {
			//   masses[i] = table[labels[i]];
			// }
			return ArrayMath.LogSum(table, startIndex, startIndex + numCellsToSum);
		}

		public virtual double LogProbFront(int[] label)
		{
			return UnnormalizedLogProbFront(label) - TotalMass();
		}

		public virtual double UnnormalizedLogProbFront(int label)
		{
			int[] labels = new int[] { label };
			return UnnormalizedLogProbFront(labels);
		}

		public virtual double LogProbFront(int label)
		{
			return UnnormalizedLogProbFront(label) - TotalMass();
		}

		public virtual double UnnormalizedLogProbEnd(int[] labels)
		{
			labels = IndicesEnd(labels);
			double[] masses = new double[labels.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[labels[i]];
			}
			return ArrayMath.LogSum(masses);
		}

		public virtual double LogProbEnd(int[] labels)
		{
			return UnnormalizedLogProbEnd(labels) - TotalMass();
		}

		public virtual double UnnormalizedLogProbEnd(int label)
		{
			int[] labels = new int[] { label };
			return UnnormalizedLogProbEnd(labels);
		}

		public virtual double LogProbEnd(int label)
		{
			return UnnormalizedLogProbEnd(label) - TotalMass();
		}

		public virtual double GetValue(int index)
		{
			return table[index];
		}

		public virtual double GetValue(int[] label)
		{
			return table[IndexOf(label)];
		}

		public virtual void SetValue(int index, double value)
		{
			table[index] = value;
		}

		public virtual void SetValue(int[] label, double value)
		{
			// try{
			table[IndexOf(label)] = value;
		}

		// } catch (Exception e) {
		// e.printStackTrace();
		// log.info("Table length: " + table.length + " indexOf(label): "
		// + indexOf(label));
		// throw new ArrayIndexOutOfBoundsException(e.toString());
		// // System.exit(1);
		// }
		public virtual void IncrementValue(int[] label, double value)
		{
			IncrementValue(IndexOf(label), value);
		}

		public virtual void IncrementValue(int index, double value)
		{
			table[index] += value;
		}

		internal virtual void LogIncrementValue(int index, double value)
		{
			table[index] = SloppyMath.LogAdd(table[index], value);
		}

		public virtual void LogIncrementValue(int[] label, double value)
		{
			LogIncrementValue(IndexOf(label), value);
		}

		public virtual void MultiplyInFront(Edu.Stanford.Nlp.IE.Crf.FactorTable other)
		{
			int divisor = SloppyMath.IntPow(numClasses, windowSize - other.WindowSize());
			for (int i = 0; i < table.Length; i++)
			{
				table[i] += other.GetValue(i / divisor);
			}
		}

		public virtual void MultiplyInEnd(Edu.Stanford.Nlp.IE.Crf.FactorTable other)
		{
			int divisor = SloppyMath.IntPow(numClasses, other.WindowSize());
			for (int i = 0; i < table.Length; i++)
			{
				table[i] += other.GetValue(i % divisor);
			}
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.FactorTable SumOutEnd()
		{
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FactorTable(numClasses, windowSize - 1);
			for (int i = 0; i < sz; i++)
			{
				ft.table[i] = ArrayMath.LogSum(table, i * numClasses, (i + 1) * numClasses);
			}
			/*
			for (int i = 0; i < table.length; i++) {
			ft.logIncrementValue(i / numClasses, table[i]);
			}
			*/
			return ft;
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.FactorTable SumOutFront()
		{
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FactorTable(numClasses, windowSize - 1);
			int stride = ft.Size();
			for (int i = 0; i < stride; i++)
			{
				ft.SetValue(i, ArrayMath.LogSum(table, i, table.Length, stride));
			}
			return ft;
		}

		public virtual void DivideBy(Edu.Stanford.Nlp.IE.Crf.FactorTable other)
		{
			for (int i = 0; i < table.Length; i++)
			{
				if (table[i] != double.NegativeInfinity || other.table[i] != double.NegativeInfinity)
				{
					table[i] -= other.table[i];
				}
			}
		}

		public static void Main(string[] args)
		{
			int numClasses = 6;
			int cliqueSize = 3;
			System.Console.Error.Printf("Creating factor table with %d classes and window (clique) size %d%n", numClasses, cliqueSize);
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FactorTable(numClasses, cliqueSize);
			for (int i = 0; i < numClasses; i++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					for (int k = 0; k < numClasses; k++)
					{
						int[] b = new int[] { i, j, k };
						ft.SetValue(b, (i * 4) + (j * 2) + k);
					}
				}
			}
			log.Info(ft);
			double normalization = 0.0;
			for (int i_1 = 0; i_1 < numClasses; i_1++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					for (int k = 0; k < numClasses; k++)
					{
						normalization += ft.UnnormalizedLogProb(new int[] { i_1, j, k });
					}
				}
			}
			log.Info("Normalization Z = " + normalization);
			log.Info(ft.SumOutFront());
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft2 = new Edu.Stanford.Nlp.IE.Crf.FactorTable(numClasses, 2);
			for (int i_2 = 0; i_2 < numClasses; i_2++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					int[] b = new int[] { i_2, j };
					ft2.SetValue(b, i_2 * numClasses + j);
				}
			}
			log.Info(ft2);
			// FactorTable ft3 = ft2.sumOutFront();
			// log.info(ft3);
			for (int i_3 = 0; i_3 < numClasses; i_3++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					int[] b = new int[] { i_3, j };
					double t = 0;
					for (int k = 0; k < numClasses; k++)
					{
						t += System.Math.Exp(ft.ConditionalLogProbGivenPrevious(b, k));
						System.Console.Error.WriteLine(k + "|" + i_3 + ',' + j + " : " + System.Math.Exp(ft.ConditionalLogProbGivenPrevious(b, k)));
					}
					log.Info(t);
				}
			}
			log.Info("conditionalLogProbGivenFirst");
			for (int j_1 = 0; j_1 < numClasses; j_1++)
			{
				for (int k = 0; k < numClasses; k++)
				{
					int[] b = new int[] { j_1, k };
					double t = 0.0;
					for (int i_4 = 0; i_4 < numClasses; i_4++)
					{
						t += ft.UnnormalizedConditionalLogProbGivenFirst(i_4, b);
						System.Console.Error.WriteLine(i_4 + "|" + j_1 + ',' + k + " : " + ft.UnnormalizedConditionalLogProbGivenFirst(i_4, b));
					}
					log.Info(t);
				}
			}
			log.Info("conditionalLogProbGivenFirst");
			for (int i_5 = 0; i_5 < numClasses; i_5++)
			{
				for (int j = 0; j_1 < numClasses; j_1++)
				{
					int[] b = new int[] { i_5, j_1 };
					double t = 0.0;
					for (int k = 0; k < numClasses; k++)
					{
						t += ft.ConditionalLogProbGivenNext(b, k);
						System.Console.Error.WriteLine(i_5 + "," + j_1 + '|' + k + " : " + ft.ConditionalLogProbGivenNext(b, k));
					}
					log.Info(t);
				}
			}
			numClasses = 2;
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft3 = new Edu.Stanford.Nlp.IE.Crf.FactorTable(numClasses, cliqueSize);
			ft3.SetValue(new int[] { 0, 0, 0 }, System.Math.Log(0.25));
			ft3.SetValue(new int[] { 0, 0, 1 }, System.Math.Log(0.35));
			ft3.SetValue(new int[] { 0, 1, 0 }, System.Math.Log(0.05));
			ft3.SetValue(new int[] { 0, 1, 1 }, System.Math.Log(0.07));
			ft3.SetValue(new int[] { 1, 0, 0 }, System.Math.Log(0.08));
			ft3.SetValue(new int[] { 1, 0, 1 }, System.Math.Log(0.16));
			ft3.SetValue(new int[] { 1, 1, 0 }, System.Math.Log(1e-50));
			ft3.SetValue(new int[] { 1, 1, 1 }, System.Math.Log(1e-50));
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft4 = ft3.SumOutFront();
			log.Info(ft4.ToNonLogString());
			Edu.Stanford.Nlp.IE.Crf.FactorTable ft5 = ft3.SumOutEnd();
			log.Info(ft5.ToNonLogString());
		}
		// end main
	}
}
