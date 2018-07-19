using System.Collections.Generic;
using Java.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Interface representing a mapping between pairs of typed objects and double
	/// values.
	/// </summary>
	/// <author>Angel Chang</author>
	public interface ITwoDimensionalCounterInterface<K1, K2>
	{
		void DefaultReturnValue(double rv);

		double DefaultReturnValue();

		/// <returns>total number of entries (key pairs)</returns>
		int Size();

		bool ContainsKey(K1 o1, K2 o2);

		void IncrementCount(K1 o1, K2 o2);

		void IncrementCount(K1 o1, K2 o2, double count);

		void DecrementCount(K1 o1, K2 o2);

		void DecrementCount(K1 o1, K2 o2, double count);

		void SetCount(K1 o1, K2 o2, double count);

		double Remove(K1 o1, K2 o2);

		double GetCount(K1 o1, K2 o2);

		double TotalCount();

		double TotalCount(K1 k1);

		ICollection<K1> FirstKeySet();

		ICollection<K2> SecondKeySet();

		bool IsEmpty();

		void Remove(K1 key);

		string ToMatrixString(int cellSize);

		/// <summary>
		/// Given an ordering of the first (row) and second (column) keys, will produce
		/// a double matrix.
		/// </summary>
		double[][] ToMatrix(IList<K1> firstKeys, IList<K2> secondKeys);

		string ToCSVString(NumberFormat nf);

		/// <returns>the inner Counter associated with key o</returns>
		ICounter<K2> GetCounter(K1 o);
		//public Set<Map.Entry<K1, ClassicCounter<K2>>> entrySet();
		//public Counter<K2> setCounter(K1 o, Counter<K2> c);
		//public Counter<Pair<K1, K2>> flatten();
		//public void addAll(TwoDimensionalCounterInterface<K1, K2> c);
		//public void addAll(K1 key, Counter<K2> c);
		//public void subtractAll(K1 key, Counter<K2> c);
		//public void subtractAll(TwoDimensionalCounterInterface<K1, K2> c, boolean removeKeys);
		//public Counter<K1> sumInnerCounter();
	}
}
