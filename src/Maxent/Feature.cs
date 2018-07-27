using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Maxent
{
	/// <summary>
	/// This class is used as a base class for TaggerFeature for the
	/// tagging problem and for BinaryFeature for the general problem with binary
	/// features.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class Feature
	{
		/// <summary>
		/// This will contain the (x,y) pairs for which the feature is non-zero in
		/// case it is sparse.
		/// </summary>
		/// <remarks>
		/// This will contain the (x,y) pairs for which the feature is non-zero in
		/// case it is sparse.
		/// The pairs (x,y) are coded as x*ySize+y. The values are kept in valuesI.
		/// For example, if a feature has only two non-zero values, e.g f(1,2)=3
		/// and f(6,3)=0.74, then indexedValues will have values
		/// indexedValues={1*ySize+2,6*ySize+2} and valuesI will be {3,.74}
		/// </remarks>
		public int[] indexedValues;

		/// <summary>
		/// These are the non-zero values we want to keep for the points in
		/// indexedValues.
		/// </summary>
		private double[] valuesI;

		internal static Experiments domain;

		private IDictionary<int, double> hashValues;

		protected internal double sum;

		protected internal IIndex<IntPair> instanceIndex;

		public Feature()
		{
		}

		/// <summary>This is if we are given an array of double with a value for each training sample in the order of their occurrence.</summary>
		public Feature(Experiments e, double[] vals, IIndex<IntPair> instanceIndex)
		{
			// todo [cdm 2013]: This needs to be removed! Try to put field in Features class, rather than adding as field to every object.
			// the sum of all values
			this.instanceIndex = instanceIndex;
			IDictionary<int, double> setNonZeros = Generics.NewHashMap();
			for (int i = 0; i < vals.Length; i++)
			{
				if (vals[i] != 0.0)
				{
					int @in = int.Parse(IndexOf(e.Get(i)[0], e.Get(i)[1]));
					// new Integer(e.get(i)[0]*e.ySize+e.get(i)[1]);
					double oldVal = setNonZeros[@in] = double.ValueOf(vals[i]);
					if (oldVal != null && oldVal != vals[i])
					{
						throw new InvalidOperationException("Incorrect function specification: Feature has two values at one point: " + oldVal + " and " + vals[i]);
					}
				}
			}
			//if
			// for
			int[] keys = Sharpen.Collections.ToArray(setNonZeros.Keys, new int[setNonZeros.Keys.Count]);
			indexedValues = new int[keys.Length];
			valuesI = new double[keys.Length];
			for (int j = 0; j < keys.Length; j++)
			{
				indexedValues[j] = keys[j];
				valuesI[j] = setNonZeros[keys[j]];
			}
			// for
			domain = e;
		}

		internal virtual int IndexOf(int x, int y)
		{
			IntPair iP = new IntPair(x, y);
			return instanceIndex.IndexOf(iP);
		}

		internal virtual IntPair GetPair(int index)
		{
			return instanceIndex.Get(index);
		}

		internal virtual int GetXInstance(int index)
		{
			IntPair iP = GetPair(index);
			return iP.Get(0);
		}

		internal virtual int GetYInstance(int index)
		{
			IntPair iP = GetPair(index);
			return iP.Get(1);
		}

		/// <param name="vals">a value for each (x,y) pair</param>
		public Feature(Experiments e, double[][] vals, IIndex<IntPair> instanceIndex)
		{
			this.instanceIndex = instanceIndex;
			domain = e;
			int num = 0;
			for (int x = 0; x < e.xSize; x++)
			{
				for (int y = 0; y < e.ySize; y++)
				{
					if (vals[x][y] != 0)
					{
						num++;
					}
				}
			}
			indexedValues = new int[num];
			valuesI = new double[num];
			int current = 0;
			for (int x_1 = 0; x_1 < e.xSize; x_1++)
			{
				for (int y = 0; y < e.ySize; y++)
				{
					if (vals[x_1][y] != 0)
					{
						indexedValues[current] = IndexOf(x_1, y);
						valuesI[current] = vals[x_1][y];
						current++;
					}
				}
			}
		}

		public Feature(Experiments e, int numElems, IIndex<IntPair> instanceIndex)
		{
			//if
			//for
			this.instanceIndex = instanceIndex;
			domain = e;
			indexedValues = new int[numElems];
			valuesI = new double[numElems];
		}

		/// <param name="indexes">The pairs (x,y) for which the feature is non-zero. They are coded as x*ySize+y</param>
		/// <param name="vals">The values at these points.</param>
		public Feature(Experiments e, int[] indexes, double[] vals, IIndex<IntPair> instanceIndex)
		{
			domain = e;
			indexedValues = indexes;
			valuesI = vals;
			this.instanceIndex = instanceIndex;
		}

		/// <summary>
		/// Prints out the points where the feature is non-zero and the values
		/// at these points.
		/// </summary>
		public virtual void Print()
		{
			Print(System.Console.Out);
		}

		/// <summary>
		/// Used to sequentially set the values of a feature -- index is the pace in the arrays ; key goes into
		/// indexedValues, and value goes into valuesI.
		/// </summary>
		public virtual void SetValue(int index, int key, double value)
		{
			indexedValues[index] = key;
			valuesI[index] = value;
		}

		public virtual void Print(TextWriter pf)
		{
			for (int i = 0; i < indexedValues.Length; i++)
			{
				IntPair iP = GetPair(indexedValues[i]);
				int x = iP.Get(0);
				int y = iP.Get(1);
				// int y=indexedValues[i]-x*domain.ySize;
				pf.WriteLine(x + ", " + y + ' ' + valuesI[i]);
			}
		}

		/// <summary>Get the value at the index-ed non zero value pair (x,y)</summary>
		public virtual double GetVal(int index)
		{
			return valuesI[index];
		}

		public virtual void SetSum()
		{
			foreach (double value in valuesI)
			{
				sum += value;
			}
		}

		public virtual int Len()
		{
			if (indexedValues != null)
			{
				return indexedValues.Length;
			}
			else
			{
				return 0;
			}
		}

		/// <returns>the history x of the index-th (x,y) pair</returns>
		public virtual int GetX(int index)
		{
			return GetXInstance(indexedValues[index]);
		}

		/// <returns>the outcome y of the index-th (x,y) pair</returns>
		public virtual int GetY(int index)
		{
			return GetYInstance(indexedValues[index]);
		}

		// return indexedValues[index]-(indexedValues[index]/domain.ySize)*domain.ySize;
		/// <summary>
		/// This is rarely used because it is slower and requires initHashVals() to be called beforehand
		/// to initialize the hashValues.
		/// </summary>
		public virtual double GetVal(int x, int y)
		{
			double val = hashValues[int.Parse(IndexOf(x, y))];
			if (val == null)
			{
				return 0.0;
			}
			else
			{
				return val;
			}
		}

		/// <summary>
		/// Creates a HashMap with keys indices from pairs (x,y) and values the value of the function at the pair;
		/// required for use of getVal(x,y)
		/// </summary>
		public virtual void InitHashVals()
		{
			hashValues = Generics.NewHashMap();
			for (int i = 0; i < Len(); i++)
			{
				int x = GetX(i);
				int y = GetY(i);
				double value = GetVal(i);
				this.hashValues[int.Parse(IndexOf(x, y))] = value;
			}
		}

		/// <returns>The empirical expectation of the feature.</returns>
		public virtual double Ftilde()
		{
			double s = 0.0;
			for (int i = 0; i < indexedValues.Length; i++)
			{
				int x = GetXInstance(indexedValues[i]);
				int y = GetYInstance(indexedValues[i]);
				// int y=indexedValues[i]-x*domain.ySize;
				s = s + domain.PtildeXY(x, y) * GetVal(i);
			}
			return s;
		}
	}
}
