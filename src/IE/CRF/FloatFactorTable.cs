using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Stores a factor table as a one dimensional array of floats.</summary>
	/// <author>Jenny Finkel</author>
	public class FloatFactorTable
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.FloatFactorTable));

		private readonly int numClasses;

		private readonly int windowSize;

		private readonly float[] table;

		public FloatFactorTable(int numClasses, int windowSize)
		{
			this.numClasses = numClasses;
			this.windowSize = windowSize;
			table = new float[SloppyMath.IntPow(numClasses, windowSize)];
			Arrays.Fill(table, float.NegativeInfinity);
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
				sb.Append("\n");
			}
			sb.Append("}");
			return sb.ToString();
		}

		public virtual string ToString<_T0>(IIndex<_T0> classIndex)
		{
			StringBuilder sb = new StringBuilder("{\n");
			for (int i = 0; i < table.Length; i++)
			{
				sb.Append(ToString(ToArray(i), classIndex));
				sb.Append(": ");
				sb.Append(GetValue(i));
				sb.Append("\n");
			}
			sb.Append("}");
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
				sb.Append("\n");
			}
			sb.Append("}");
			return sb.ToString();
		}

		private string ToString<_T0>(int[] array, IIndex<_T0> classIndex)
		{
			IList<object> l = new List<object>();
			foreach (int anArray in array)
			{
				l.Add(classIndex.Get(anArray));
			}
			return l.ToString();
		}

		private int[] ToArray(int index)
		{
			int[] indices = new int[windowSize];
			for (int i = indices.Length - 1; i >= 0; i--)
			{
				indices[i] = index % numClasses;
				index /= numClasses;
			}
			return indices;
		}

		private int IndexOf(int[] entry)
		{
			int index = 0;
			foreach (int anEntry in entry)
			{
				index *= numClasses;
				index += anEntry;
			}
			return index;
		}

		private int IndexOf(int[] front, int end)
		{
			int index = 0;
			foreach (int aFront in front)
			{
				index *= numClasses;
				index += aFront;
			}
			index *= numClasses;
			index += end;
			return index;
		}

		private int[] IndicesEnd(int[] entries)
		{
			int[] indices = new int[SloppyMath.IntPow(numClasses, windowSize - entries.Length)];
			int offset = SloppyMath.IntPow(numClasses, entries.Length);
			int index = 0;
			foreach (int entry in entries)
			{
				index *= numClasses;
				index += entry;
			}
			for (int i = 0; i < indices.Length; i++)
			{
				indices[i] = index;
				index += offset;
			}
			return indices;
		}

		private int[] IndicesFront(int[] entries)
		{
			int[] indices = new int[SloppyMath.IntPow(numClasses, windowSize - entries.Length)];
			int offset = SloppyMath.IntPow(numClasses, windowSize - entries.Length);
			int start = 0;
			foreach (int entry in entries)
			{
				start *= numClasses;
				start += entry;
			}
			start *= offset;
			int end = 0;
			for (int i = 0; i < entries.Length; i++)
			{
				end *= numClasses;
				end += entries[i];
				if (i == entries.Length - 1)
				{
					end += 1;
				}
			}
			end *= offset;
			for (int i_1 = start; i_1 < end; i_1++)
			{
				indices[i_1 - start] = i_1;
			}
			return indices;
		}

		public virtual int WindowSize()
		{
			return windowSize;
		}

		public virtual int NumClasses()
		{
			return numClasses;
		}

		private int Size()
		{
			return table.Length;
		}

		public virtual float TotalMass()
		{
			return ArrayMath.LogSum(table);
		}

		public virtual float UnnormalizedLogProb(int[] label)
		{
			return GetValue(label);
		}

		public virtual float LogProb(int[] label)
		{
			return UnnormalizedLogProb(label) - TotalMass();
		}

		public virtual float Prob(int[] label)
		{
			return (float)System.Math.Exp(UnnormalizedLogProb(label) - TotalMass());
		}

		// given is at the begining, of is at the end
		public virtual float ConditionalLogProb(int[] given, int of)
		{
			if (given.Length != windowSize - 1)
			{
				log.Info("error computing conditional log prob");
				System.Environment.Exit(0);
			}
			int[] label = IndicesFront(given);
			float[] masses = new float[label.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[label[i]];
			}
			float z = ArrayMath.LogSum(masses);
			return table[IndexOf(given, of)] - z;
		}

		public virtual float UnnormalizedLogProbFront(int[] label)
		{
			label = IndicesFront(label);
			float[] masses = new float[label.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[label[i]];
			}
			return ArrayMath.LogSum(masses);
		}

		public virtual float LogProbFront(int[] label)
		{
			return UnnormalizedLogProbFront(label) - TotalMass();
		}

		public virtual float UnnormalizedLogProbEnd(int[] label)
		{
			label = IndicesEnd(label);
			float[] masses = new float[label.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[label[i]];
			}
			return ArrayMath.LogSum(masses);
		}

		public virtual float LogProbEnd(int[] label)
		{
			return UnnormalizedLogProbEnd(label) - TotalMass();
		}

		public virtual float UnnormalizedLogProbEnd(int label)
		{
			int[] l = new int[] { label };
			l = IndicesEnd(l);
			float[] masses = new float[l.Length];
			for (int i = 0; i < masses.Length; i++)
			{
				masses[i] = table[l[i]];
			}
			return ArrayMath.LogSum(masses);
		}

		public virtual float LogProbEnd(int label)
		{
			return UnnormalizedLogProbEnd(label) - TotalMass();
		}

		private float GetValue(int index)
		{
			return table[index];
		}

		public virtual float GetValue(int[] label)
		{
			return table[IndexOf(label)];
		}

		private void SetValue(int index, float value)
		{
			table[index] = value;
		}

		public virtual void SetValue(int[] label, float value)
		{
			table[IndexOf(label)] = value;
		}

		public virtual void IncrementValue(int[] label, float value)
		{
			table[IndexOf(label)] += value;
		}

		private void LogIncrementValue(int index, float value)
		{
			table[index] = SloppyMath.LogAdd(table[index], value);
		}

		public virtual void LogIncrementValue(int[] label, float value)
		{
			int index = IndexOf(label);
			table[index] = SloppyMath.LogAdd(table[index], value);
		}

		public virtual void MultiplyInFront(Edu.Stanford.Nlp.IE.Crf.FloatFactorTable other)
		{
			int divisor = SloppyMath.IntPow(numClasses, windowSize - other.WindowSize());
			for (int i = 0; i < table.Length; i++)
			{
				table[i] += other.GetValue(i / divisor);
			}
		}

		public virtual void MultiplyInEnd(Edu.Stanford.Nlp.IE.Crf.FloatFactorTable other)
		{
			int divisor = SloppyMath.IntPow(numClasses, other.WindowSize());
			for (int i = 0; i < table.Length; i++)
			{
				table[i] += other.GetValue(i % divisor);
			}
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.FloatFactorTable SumOutEnd()
		{
			Edu.Stanford.Nlp.IE.Crf.FloatFactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FloatFactorTable(numClasses, windowSize - 1);
			for (int i = 0; i < table.Length; i++)
			{
				ft.LogIncrementValue(i / numClasses, table[i]);
			}
			return ft;
		}

		public virtual Edu.Stanford.Nlp.IE.Crf.FloatFactorTable SumOutFront()
		{
			Edu.Stanford.Nlp.IE.Crf.FloatFactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FloatFactorTable(numClasses, windowSize - 1);
			int mod = SloppyMath.IntPow(numClasses, windowSize - 1);
			for (int i = 0; i < table.Length; i++)
			{
				ft.LogIncrementValue(i % mod, table[i]);
			}
			return ft;
		}

		public virtual void DivideBy(Edu.Stanford.Nlp.IE.Crf.FloatFactorTable other)
		{
			for (int i = 0; i < table.Length; i++)
			{
				if (table[i] != float.NegativeInfinity || other.table[i] != float.NegativeInfinity)
				{
					table[i] -= other.table[i];
				}
			}
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.IE.Crf.FloatFactorTable ft = new Edu.Stanford.Nlp.IE.Crf.FloatFactorTable(6, 3);
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					for (int k = 0; k < 6; k++)
					{
						int[] b = new int[] { i, j, k };
						ft.SetValue(b, (i * 4) + (j * 2) + k);
					}
				}
			}
			//System.out.println(ft);
			//System.out.println(ft.sumOutFront());
			Edu.Stanford.Nlp.IE.Crf.FloatFactorTable ft2 = new Edu.Stanford.Nlp.IE.Crf.FloatFactorTable(6, 2);
			for (int i_1 = 0; i_1 < 6; i_1++)
			{
				for (int j = 0; j < 6; j++)
				{
					int[] b = new int[] { i_1, j };
					ft2.SetValue(b, i_1 * 6 + j);
				}
			}
			System.Console.Out.WriteLine(ft);
			//FloatFactorTable ft3 = ft2.sumOutFront();
			//System.out.println(ft3);
			for (int i_2 = 0; i_2 < 6; i_2++)
			{
				for (int j = 0; j < 6; j++)
				{
					int[] b = new int[] { i_2, j };
					float t = 0;
					for (int k = 0; k < 6; k++)
					{
						t += System.Math.Exp(ft.ConditionalLogProb(b, k));
						log.Info(k + "|" + i_2 + "," + j + " : " + System.Math.Exp(ft.ConditionalLogProb(b, k)));
					}
					System.Console.Out.WriteLine(t);
				}
			}
		}
	}
}
