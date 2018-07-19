using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Static utility methods for operating on arrays.</summary>
	/// <remarks>
	/// Static utility methods for operating on arrays.
	/// Note: You can also find some methods for printing arrays that are tables in
	/// StringUtils.  (Search for makeTextTable, etc.)
	/// </remarks>
	/// <author>Huy Nguyen (htnguyen@cs.stanford.edu)</author>
	/// <author>Michel Galley (mgalley@stanford.edu)</author>
	public class ArrayUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.ArrayUtils));

		/// <summary>Should not be instantiated</summary>
		private ArrayUtils()
		{
		}

		public static byte[] GapEncode(int[] orig)
		{
			IList<byte> encodedList = GapEncodeList(orig);
			byte[] arr = new byte[encodedList.Count];
			int i = 0;
			foreach (byte b in encodedList)
			{
				arr[i++] = b;
			}
			return arr;
		}

		public static IList<byte> GapEncodeList(int[] orig)
		{
			for (int i = 1; i < orig.Length; i++)
			{
				if (orig[i] < orig[i - 1])
				{
					throw new ArgumentException("Array must be sorted!");
				}
			}
			IList<byte> bytes = new List<byte>();
			int index = 0;
			int prevNum = 0;
			byte currByte = 0 << 8;
			foreach (int f in orig)
			{
				string n = (f == prevNum ? string.Empty : int.ToString(f - prevNum, 2));
				for (int ii = 0; ii < n.Length; ii++)
				{
					if (index == 8)
					{
						bytes.Add(currByte);
						currByte = 0 << 8;
						index = 0;
					}
					currByte <<= 1;
					currByte++;
					index++;
				}
				if (index == 8)
				{
					bytes.Add(currByte);
					currByte = 0 << 8;
					index = 0;
				}
				currByte <<= 1;
				index++;
				for (int i_1 = 1; i_1 < n.Length; i_1++)
				{
					if (index == 8)
					{
						bytes.Add(currByte);
						currByte = 0 << 8;
						index = 0;
					}
					currByte <<= 1;
					if (n[i_1] == '1')
					{
						currByte++;
					}
					index++;
				}
				prevNum = f;
			}
			while (index > 0 && index < 9)
			{
				if (index == 8)
				{
					bytes.Add(currByte);
					break;
				}
				currByte <<= 1;
				currByte++;
				index++;
			}
			return bytes;
		}

		public static int[] GapDecode(byte[] gapEncoded)
		{
			return GapDecode(gapEncoded, 0, gapEncoded.Length);
		}

		public static int[] GapDecode(byte[] gapEncoded, int startIndex, int endIndex)
		{
			IList<int> ints = GapDecodeList(gapEncoded, startIndex, endIndex);
			int[] arr = new int[ints.Count];
			int index = 0;
			foreach (int i in ints)
			{
				arr[index++] = i;
			}
			return arr;
		}

		public static IList<int> GapDecodeList(byte[] gapEncoded)
		{
			return GapDecodeList(gapEncoded, 0, gapEncoded.Length);
		}

		public static IList<int> GapDecodeList(byte[] gapEncoded, int startIndex, int endIndex)
		{
			bool gettingSize = true;
			int size = 0;
			IList<int> ints = new List<int>();
			int gap = 0;
			int prevNum = 0;
			for (int i = startIndex; i < endIndex; i++)
			{
				byte b = gapEncoded[i];
				for (int index = 7; index >= 0; index--)
				{
					bool value = ((b >> index) & 1) == 1;
					if (gettingSize)
					{
						if (value)
						{
							size++;
						}
						else
						{
							if (size == 0)
							{
								ints.Add(prevNum);
							}
							else
							{
								if (size == 1)
								{
									prevNum++;
									ints.Add(prevNum);
									size = 0;
								}
								else
								{
									gettingSize = false;
									gap = 1;
									size--;
								}
							}
						}
					}
					else
					{
						gap <<= 1;
						if (value)
						{
							gap++;
						}
						size--;
						if (size == 0)
						{
							prevNum += gap;
							ints.Add(prevNum);
							gettingSize = true;
						}
					}
				}
			}
			return ints;
		}

		public static byte[] DeltaEncode(int[] orig)
		{
			IList<byte> encodedList = DeltaEncodeList(orig);
			byte[] arr = new byte[encodedList.Count];
			int i = 0;
			foreach (byte b in encodedList)
			{
				arr[i++] = b;
			}
			return arr;
		}

		public static IList<byte> DeltaEncodeList(int[] orig)
		{
			for (int i = 1; i < orig.Length; i++)
			{
				if (orig[i] < orig[i - 1])
				{
					throw new ArgumentException("Array must be sorted!");
				}
			}
			IList<byte> bytes = new List<byte>();
			int index = 0;
			int prevNum = 0;
			byte currByte = 0 << 8;
			foreach (int f in orig)
			{
				string n = (f == prevNum ? string.Empty : int.ToString(f - prevNum, 2));
				string n1 = (n.IsEmpty() ? string.Empty : int.ToString(n.Length, 2));
				for (int ii = 0; ii < n1.Length; ii++)
				{
					if (index == 8)
					{
						bytes.Add(currByte);
						currByte = 0 << 8;
						index = 0;
					}
					currByte <<= 1;
					currByte++;
					index++;
				}
				if (index == 8)
				{
					bytes.Add(currByte);
					currByte = 0 << 8;
					index = 0;
				}
				currByte <<= 1;
				index++;
				for (int i_1 = 1; i_1 < n1.Length; i_1++)
				{
					if (index == 8)
					{
						bytes.Add(currByte);
						currByte = 0 << 8;
						index = 0;
					}
					currByte <<= 1;
					if (n1[i_1] == '1')
					{
						currByte++;
					}
					index++;
				}
				for (int i_2 = 1; i_2 < n.Length; i_2++)
				{
					if (index == 8)
					{
						bytes.Add(currByte);
						currByte = 0 << 8;
						index = 0;
					}
					currByte <<= 1;
					if (n[i_2] == '1')
					{
						currByte++;
					}
					index++;
				}
				prevNum = f;
			}
			while (index > 0 && index < 9)
			{
				if (index == 8)
				{
					bytes.Add(currByte);
					break;
				}
				currByte <<= 1;
				currByte++;
				index++;
			}
			return bytes;
		}

		public static int[] DeltaDecode(byte[] deltaEncoded)
		{
			return DeltaDecode(deltaEncoded, 0, deltaEncoded.Length);
		}

		public static int[] DeltaDecode(byte[] deltaEncoded, int startIndex, int endIndex)
		{
			IList<int> ints = DeltaDecodeList(deltaEncoded);
			int[] arr = new int[ints.Count];
			int index = 0;
			foreach (int i in ints)
			{
				arr[index++] = i;
			}
			return arr;
		}

		public static IList<int> DeltaDecodeList(byte[] deltaEncoded)
		{
			return DeltaDecodeList(deltaEncoded, 0, deltaEncoded.Length);
		}

		public static IList<int> DeltaDecodeList(byte[] deltaEncoded, int startIndex, int endIndex)
		{
			bool gettingSize1 = true;
			bool gettingSize2 = false;
			int size1 = 0;
			IList<int> ints = new List<int>();
			int gap = 0;
			int size2 = 0;
			int prevNum = 0;
			for (int i = startIndex; i < endIndex; i++)
			{
				byte b = deltaEncoded[i];
				for (int index = 7; index >= 0; index--)
				{
					bool value = ((b >> index) & 1) == 1;
					if (gettingSize1)
					{
						if (value)
						{
							size1++;
						}
						else
						{
							if (size1 == 0)
							{
								ints.Add(prevNum);
							}
							else
							{
								if (size1 == 1)
								{
									prevNum++;
									ints.Add(prevNum);
									size1 = 0;
								}
								else
								{
									gettingSize1 = false;
									gettingSize2 = true;
									size2 = 1;
									size1--;
								}
							}
						}
					}
					else
					{
						if (gettingSize2)
						{
							size2 <<= 1;
							if (value)
							{
								size2++;
							}
							size1--;
							if (size1 == 0)
							{
								gettingSize2 = false;
								gap = 1;
								size2--;
							}
						}
						else
						{
							gap <<= 1;
							if (value)
							{
								gap++;
							}
							size2--;
							if (size2 == 0)
							{
								prevNum += gap;
								ints.Add(prevNum);
								gettingSize1 = true;
							}
						}
					}
				}
			}
			return ints;
		}

		/// <summary>helper for gap encoding.</summary>
		private static byte[] BitSetToByteArray(BitSet bitSet)
		{
			while (bitSet.Length() % 8 != 0)
			{
				bitSet.Set(bitSet.Length(), true);
			}
			byte[] array = new byte[bitSet.Length() / 8];
			for (int i = 0; i < array.Length; i++)
			{
				int offset = i * 8;
				int index = 0;
				for (int j = 0; j < 8; j++)
				{
					index <<= 1;
					if (bitSet.Get(offset + j))
					{
						index++;
					}
				}
				array[i] = unchecked((byte)(index - 128));
			}
			return array;
		}

		/// <summary>helper for gap encoding.</summary>
		private static BitSet ByteArrayToBitSet(byte[] array)
		{
			BitSet bitSet = new BitSet();
			int index = 0;
			foreach (byte b in array)
			{
				int b1 = ((int)b) + 128;
				bitSet.Set(index++, ((b1 >> 7) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 6) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 5) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 4) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 3) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 2) & 1) == 1);
				bitSet.Set(index++, ((b1 >> 1) & 1) == 1);
				bitSet.Set(index++, (b1 & 1) == 1);
			}
			return bitSet;
		}

		//     for (int i = 1; i < orig.length; i++) {
		//       if (orig[i] < orig[i-1]) { throw new RuntimeException("Array must be sorted!"); }
		//       StringBuilder bits = new StringBuilder();
		//       int prevNum = 0;
		//       for (int f : orig) {
		//         StringBuilder bits1 = new StringBuilder();
		//               log.info(f+"\t");
		//               String n = Integer.toString(f-prevNum, 2);
		//               String n1 = Integer.toString(n.length(), 2);
		//               for (int ii = 0; ii < n1.length(); ii++) {
		//                 bits1.append("1");
		//               }
		//               bits1.append("0");
		//               bits1.append(n1.substring(1));
		//               bits1.append(n.substring(1));
		//               log.info(bits1+"\t");
		//               bits.append(bits1);
		//               prevNum = f;
		//             }
		public static double[] Flatten(double[][] array)
		{
			int size = 0;
			foreach (double[] a in array)
			{
				size += a.Length;
			}
			double[] newArray = new double[size];
			int i = 0;
			foreach (double[] a_1 in array)
			{
				foreach (double d in a_1)
				{
					newArray[i++] = d;
				}
			}
			return newArray;
		}

		public static double[][] To2D(double[] array, int dim1Size)
		{
			int dim2Size = array.Length / dim1Size;
			return To2D(array, dim1Size, dim2Size);
		}

		public static double[][] To2D(double[] array, int dim1Size, int dim2Size)
		{
			double[][] newArray = new double[dim1Size][];
			int k = 0;
			for (int i = 0; i < newArray.Length; i++)
			{
				for (int j = 0; j < newArray[i].Length; j++)
				{
					newArray[i][j] = array[k++];
				}
			}
			return newArray;
		}

		/// <summary>
		/// Removes the element at the specified index from the array, and returns
		/// a new array containing the remaining elements.
		/// </summary>
		/// <remarks>
		/// Removes the element at the specified index from the array, and returns
		/// a new array containing the remaining elements.  If <tt>index</tt> is
		/// invalid, returns <tt>array</tt> unchanged.
		/// </remarks>
		public static double[] RemoveAt(double[] array, int index)
		{
			if (array == null)
			{
				return null;
			}
			if (index < 0 || index >= array.Length)
			{
				return array;
			}
			double[] retVal = new double[array.Length - 1];
			for (int i = 0; i < array.Length; i++)
			{
				if (i < index)
				{
					retVal[i] = array[i];
				}
				else
				{
					if (i > index)
					{
						retVal[i - 1] = array[i];
					}
				}
			}
			return retVal;
		}

		/// <summary>
		/// Removes the element at the specified index from the array, and returns
		/// a new array containing the remaining elements.
		/// </summary>
		/// <remarks>
		/// Removes the element at the specified index from the array, and returns
		/// a new array containing the remaining elements.  If <tt>index</tt> is
		/// invalid, returns <tt>array</tt> unchanged.  Uses reflection to determine
		/// the type of the array and returns an array of the appropriate type.
		/// </remarks>
		public static object[] RemoveAt(object[] array, int index)
		{
			if (array == null)
			{
				return null;
			}
			if (index < 0 || index >= array.Length)
			{
				return array;
			}
			object[] retVal = (object[])System.Array.CreateInstance(array[0].GetType(), array.Length - 1);
			for (int i = 0; i < array.Length; i++)
			{
				if (i < index)
				{
					retVal[i] = array[i];
				}
				else
				{
					if (i > index)
					{
						retVal[i - 1] = array[i];
					}
				}
			}
			return retVal;
		}

		public static string ToString(int[][] a)
		{
			StringBuilder result = new StringBuilder("[");
			for (int i = 0; i < a.Length; i++)
			{
				result.Append(Arrays.ToString(a[i]));
				if (i < a.Length - 1)
				{
					result.Append(',');
				}
			}
			result.Append(']');
			return result.ToString();
		}

		/// <summary>Tests two int[][] arrays for having equal contents.</summary>
		/// <returns>true iff for each i, <code>equalContents(xs[i],ys[i])</code> is true</returns>
		public static bool EqualContents(int[][] xs, int[][] ys)
		{
			if (xs == null)
			{
				return ys == null;
			}
			if (ys == null)
			{
				return false;
			}
			if (xs.Length != ys.Length)
			{
				return false;
			}
			for (int i = xs.Length - 1; i >= 0; i--)
			{
				if (!EqualContents(xs[i], ys[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Tests two double[][] arrays for having equal contents.</summary>
		/// <returns>true iff for each i, <code>equals(xs[i],ys[i])</code> is true</returns>
		public static bool Equals(double[][] xs, double[][] ys)
		{
			if (xs == null)
			{
				return ys == null;
			}
			if (ys == null)
			{
				return false;
			}
			if (xs.Length != ys.Length)
			{
				return false;
			}
			for (int i = xs.Length - 1; i >= 0; i--)
			{
				if (!Arrays.Equals(xs[i], ys[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>tests two int[] arrays for having equal contents</summary>
		/// <returns>true iff xs and ys have equal length, and for each i, <code>xs[i]==ys[i]</code></returns>
		public static bool EqualContents(int[] xs, int[] ys)
		{
			if (xs.Length != ys.Length)
			{
				return false;
			}
			for (int i = xs.Length - 1; i >= 0; i--)
			{
				if (xs[i] != ys[i])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Tests two boolean[][] arrays for having equal contents.</summary>
		/// <returns>true iff for each i, <code>Arrays.equals(xs[i],ys[i])</code> is true</returns>
		public static bool Equals(bool[][] xs, bool[][] ys)
		{
			if (xs == null && ys != null)
			{
				return false;
			}
			if (ys == null)
			{
				return false;
			}
			if (xs.Length != ys.Length)
			{
				return false;
			}
			for (int i = xs.Length - 1; i >= 0; i--)
			{
				if (!Arrays.Equals(xs[i], ys[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Returns true iff object o equals (not ==) some element of array a.</summary>
		public static bool Contains<T>(T[] a, T o)
		{
			foreach (T item in a)
			{
				if (item.Equals(o))
				{
					return true;
				}
			}
			return false;
		}

		// from stackoverflow
		//  http://stackoverflow.com/questions/80476/how-to-concatenate-two-arrays-in-java
		/// <summary>Concatenates two arrays and returns the result</summary>
		public static T[] Concatenate<T>(T[] first, T[] second)
		{
			T[] result = Arrays.CopyOf(first, first.Length + second.Length);
			System.Array.Copy(second, 0, result, first.Length, second.Length);
			return result;
		}

		/// <summary>
		/// Returns an array with only the elements accepted by <code>filter</code>
		/// <br />
		/// Implementation notes: creates two arrays, calls <code>filter</code>
		/// once for each element, does not alter <code>original</code>
		/// </summary>
		public static T[] Filter<T, _T1>(T[] original, IPredicate<_T1> filter)
		{
			T[] result = Arrays.CopyOf(original, original.Length);
			// avoids generic array creation compile error
			int size = 0;
			foreach (T value in original)
			{
				if (filter.Test(value))
				{
					result[size] = value;
					size++;
				}
			}
			if (size == original.Length)
			{
				return result;
			}
			return Arrays.CopyOf(result, size);
		}

		/// <summary>Return a Set containing the same elements as the specified array.</summary>
		public static ICollection<T> AsSet<T>(T[] a)
		{
			return Generics.NewHashSet(Arrays.AsList(a));
		}

		/// <summary>
		/// Return an immutable Set containing the same elements as the specified
		/// array.
		/// </summary>
		/// <remarks>
		/// Return an immutable Set containing the same elements as the specified
		/// array. Arrays with 0 or 1 elements are special cased to return the
		/// efficient small sets from the Collections class.
		/// </remarks>
		public static ICollection<T> AsImmutableSet<T>(T[] a)
		{
			if (a.Length == 0)
			{
				return Java.Util.Collections.EmptySet();
			}
			else
			{
				if (a.Length == 1)
				{
					return Java.Util.Collections.Singleton(a[0]);
				}
				else
				{
					return Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(a)));
				}
			}
		}

		public static void Fill(double[][] d, double val)
		{
			foreach (double[] aD in d)
			{
				Arrays.Fill(aD, val);
			}
		}

		public static void Fill(double[][][] d, double val)
		{
			foreach (double[][] aD in d)
			{
				Fill(aD, val);
			}
		}

		public static void Fill(double[][][][] d, double val)
		{
			foreach (double[][][] aD in d)
			{
				Fill(aD, val);
			}
		}

		public static void Fill(bool[][] d, bool val)
		{
			foreach (bool[] aD in d)
			{
				Arrays.Fill(aD, val);
			}
		}

		public static void Fill(bool[][][] d, bool val)
		{
			foreach (bool[][] aD in d)
			{
				Fill(aD, val);
			}
		}

		public static void Fill(bool[][][][] d, bool val)
		{
			foreach (bool[][][] aD in d)
			{
				Fill(aD, val);
			}
		}

		/// <summary>Casts to a double array</summary>
		public static double[] ToDouble(float[] a)
		{
			double[] d = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				d[i] = a[i];
			}
			return d;
		}

		/// <summary>Casts to a double array.</summary>
		public static double[] ToDouble(int[] array)
		{
			double[] rv = new double[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				rv[i] = array[i];
			}
			return rv;
		}

		/// <summary>
		/// needed because Arrays.asList() won't to autoboxing,
		/// so if you give it a primitive array you get a
		/// singleton list back with just that array as an element.
		/// </summary>
		public static IList<int> AsList(int[] array)
		{
			IList<int> l = new List<int>();
			foreach (int i in array)
			{
				l.Add(i);
			}
			return l;
		}

		public static double[] AsPrimitiveDoubleArray(ICollection<double> d)
		{
			double[] newD = new double[d.Count];
			int i = 0;
			foreach (double j in d)
			{
				newD[i++] = j;
			}
			return newD;
		}

		public static int[] AsPrimitiveIntArray(ICollection<int> d)
		{
			int[] newI = new int[d.Count];
			int i = 0;
			foreach (int j in d)
			{
				newI[i++] = j;
			}
			return newI;
		}

		public static long[] Copy(long[] arr)
		{
			if (arr == null)
			{
				return null;
			}
			long[] newArr = new long[arr.Length];
			System.Array.Copy(arr, 0, newArr, 0, arr.Length);
			return newArr;
		}

		public static int[] Copy(int[] i)
		{
			if (i == null)
			{
				return null;
			}
			int[] newI = new int[i.Length];
			System.Array.Copy(i, 0, newI, 0, i.Length);
			return newI;
		}

		public static int[][] Copy(int[][] i)
		{
			if (i == null)
			{
				return null;
			}
			int[][] newI = new int[i.Length][];
			for (int j = 0; j < newI.Length; j++)
			{
				newI[j] = Copy(i[j]);
			}
			return newI;
		}

		public static double[] Copy(double[] d)
		{
			if (d == null)
			{
				return null;
			}
			double[] newD = new double[d.Length];
			System.Array.Copy(d, 0, newD, 0, d.Length);
			return newD;
		}

		public static double[][] Copy(double[][] d)
		{
			if (d == null)
			{
				return null;
			}
			double[][] newD = new double[d.Length][];
			for (int i = 0; i < newD.Length; i++)
			{
				newD[i] = Copy(d[i]);
			}
			return newD;
		}

		public static double[][][] Copy(double[][][] d)
		{
			if (d == null)
			{
				return null;
			}
			double[][][] newD = new double[d.Length][][];
			for (int i = 0; i < newD.Length; i++)
			{
				newD[i] = Copy(d[i]);
			}
			return newD;
		}

		public static float[] Copy(float[] d)
		{
			if (d == null)
			{
				return null;
			}
			float[] newD = new float[d.Length];
			System.Array.Copy(d, 0, newD, 0, d.Length);
			return newD;
		}

		public static float[][] Copy(float[][] d)
		{
			if (d == null)
			{
				return null;
			}
			float[][] newD = new float[d.Length][];
			for (int i = 0; i < newD.Length; i++)
			{
				newD[i] = Copy(d[i]);
			}
			return newD;
		}

		public static float[][][] Copy(float[][][] d)
		{
			if (d == null)
			{
				return null;
			}
			float[][][] newD = new float[d.Length][][];
			for (int i = 0; i < newD.Length; i++)
			{
				newD[i] = Copy(d[i]);
			}
			return newD;
		}

		public static string ToString(double[][] b)
		{
			StringBuilder result = new StringBuilder("[");
			for (int i = 0; i < b.Length; i++)
			{
				result.Append(Arrays.ToString(b[i]));
				if (i < b.Length - 1)
				{
					result.Append(',');
				}
			}
			result.Append(']');
			return result.ToString();
		}

		public static string ToString(bool[][] b)
		{
			StringBuilder result = new StringBuilder("[");
			for (int i = 0; i < b.Length; i++)
			{
				result.Append(Arrays.ToString(b[i]));
				if (i < b.Length - 1)
				{
					result.Append(',');
				}
			}
			result.Append(']');
			return result.ToString();
		}

		public static long[] ToPrimitive(long[] @in)
		{
			return ToPrimitive(@in, 0L);
		}

		public static int[] ToPrimitive(int[] @in)
		{
			return ToPrimitive(@in, 0);
		}

		public static short[] ToPrimitive(short[] @in)
		{
			return ToPrimitive(@in, (short)0);
		}

		public static char[] ToPrimitive(char[] @in)
		{
			return ToPrimitive(@in, (char)0);
		}

		public static double[] ToPrimitive(double[] @in)
		{
			return ToPrimitive(@in, 0.0);
		}

		public static long[] ToPrimitive(long[] @in, long valueForNull)
		{
			if (@in == null)
			{
				return null;
			}
			long[] @out = new long[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				long b = @in[i];
				@out[i] = (b == null ? valueForNull : b);
			}
			return @out;
		}

		public static int[] ToPrimitive(int[] @in, int valueForNull)
		{
			if (@in == null)
			{
				return null;
			}
			int[] @out = new int[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				int b = @in[i];
				@out[i] = (b == null ? valueForNull : b);
			}
			return @out;
		}

		public static short[] ToPrimitive(short[] @in, short valueForNull)
		{
			if (@in == null)
			{
				return null;
			}
			short[] @out = new short[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				short b = @in[i];
				@out[i] = (b == null ? valueForNull : b);
			}
			return @out;
		}

		public static char[] ToPrimitive(char[] @in, char valueForNull)
		{
			if (@in == null)
			{
				return null;
			}
			char[] @out = new char[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				char b = @in[i];
				@out[i] = (b == null ? valueForNull : b);
			}
			return @out;
		}

		public static double[] ToDoubleArray(string[] @in)
		{
			double[] ret = new double[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				ret[i] = double.ParseDouble(@in[i]);
			}
			return ret;
		}

		public static double[] ToPrimitive(double[] @in, double valueForNull)
		{
			if (@in == null)
			{
				return null;
			}
			double[] @out = new double[@in.Length];
			for (int i = 0; i < @in.Length; i++)
			{
				double b = @in[i];
				@out[i] = (b == null ? valueForNull : b);
			}
			return @out;
		}

		/// <summary>Provides a consistent ordering over arrays.</summary>
		/// <remarks>
		/// Provides a consistent ordering over arrays. First compares by the
		/// first element. If that element is equal, the next element is
		/// considered, and so on. This is the array version of
		/// <see cref="CollectionUtils.CompareLists{T}(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E})"/>
		/// and uses the same logic when the arrays are of different lengths.
		/// </remarks>
		public static int CompareArrays<T>(T[] first, T[] second)
			where T : IComparable<T>
		{
			IList<T> firstAsList = Arrays.AsList(first);
			IList<T> secondAsList = Arrays.AsList(second);
			return CollectionUtils.CompareLists(firstAsList, secondAsList);
		}

		/* -- This is an older more direct implementation of the above, but not necessary unless for performance
		public static <C extends Comparable<C>> int compareArrays(C[] a1, C[] a2) {
		int len = Math.min(a1.length, a2.length);
		for (int i = 0; i < len; i++) {
		int comparison = a1[i].compareTo(a2[i]);
		if (comparison != 0) return comparison;
		}
		// one is a prefix of the other, or they're identical
		if (a1.length < a2.length) return -1;
		if (a1.length > a2.length) return 1;
		return 0;
		}
		*/
		public static IList<int> GetSubListIndex(object[] tofind, object[] tokens)
		{
			return GetSubListIndex(tofind, tokens, null);
		}

		/// <summary>
		/// If tofind is a part of tokens, it finds the ****starting index***** of tofind in tokens
		/// If tofind is not a sub-array of tokens, then it returns null
		/// note that tokens sublist should have the exact elements and order as in tofind
		/// </summary>
		/// <param name="tofind">array you want to find in tokens</param>
		/// <param name="tokens"/>
		/// <param name="matchingFunction">function that takes (tofindtoken, token) pair and returns whether they match</param>
		/// <returns>starting index of the sublist</returns>
		public static IList<int> GetSubListIndex(object[] tofind, object[] tokens, IPredicate<Pair> matchingFunction)
		{
			if (tofind.Length > tokens.Length)
			{
				return null;
			}
			IList<int> allIndices = new List<int>();
			bool matched = false;
			int index = -1;
			int lastUnmatchedIndex = 0;
			for (int i = 0; i < tokens.Length; )
			{
				for (int j = 0; j < tofind.Length; )
				{
					if (matchingFunction.Test(new Pair(tofind[j], tokens[i])))
					{
						index = i;
						i++;
						j++;
						if (j == tofind.Length)
						{
							matched = true;
							break;
						}
					}
					else
					{
						j = 0;
						i = lastUnmatchedIndex + 1;
						lastUnmatchedIndex = i;
						index = -1;
						if (lastUnmatchedIndex == tokens.Length)
						{
							break;
						}
					}
					if (i >= tokens.Length)
					{
						index = -1;
						break;
					}
				}
				if (i == tokens.Length || matched)
				{
					if (index >= 0)
					{
						//index = index - l1.length + 1;
						allIndices.Add(index - tofind.Length + 1);
					}
					matched = false;
					lastUnmatchedIndex = index;
				}
			}
			//break;
			//get starting point
			return allIndices;
		}

		/// <summary>
		/// Returns a new array which has the numbers in the input array
		/// L1-normalized.
		/// </summary>
		/// <param name="ar">Input array</param>
		/// <returns>New array that has L1 normalized form of input array</returns>
		public static double[] Normalize(double[] ar)
		{
			double[] ar2 = new double[ar.Length];
			double total = 0.0;
			foreach (double d in ar)
			{
				total += d;
			}
			for (int i = 0; i < ar.Length; i++)
			{
				ar2[i] = ar[i] / total;
			}
			return ar2;
		}

		public static object[] SubArray(object[] arr, int startindexInclusive, int endindexExclusive)
		{
			if (arr == null)
			{
				return arr;
			}
			Type type = arr.GetType().GetElementType();
			if (endindexExclusive < startindexInclusive || startindexInclusive > arr.Length - 1)
			{
				return (object[])System.Array.CreateInstance(type, 0);
			}
			if (endindexExclusive > arr.Length)
			{
				endindexExclusive = arr.Length;
			}
			if (startindexInclusive < 0)
			{
				startindexInclusive = 0;
			}
			object[] b = (object[])System.Array.CreateInstance(type, endindexExclusive - startindexInclusive);
			System.Array.Copy(arr, startindexInclusive, b, 0, endindexExclusive - startindexInclusive);
			return b;
		}

		public static int CompareBooleanArrays(bool[] a1, bool[] a2)
		{
			int len = Math.Min(a1.Length, a2.Length);
			for (int i = 0; i < len; i++)
			{
				if (!a1[i] && a2[i])
				{
					return -1;
				}
				if (a1[i] && !a2[i])
				{
					return 1;
				}
			}
			// one is a prefix of the other, or they're identical
			if (a1.Length < a2.Length)
			{
				return -1;
			}
			if (a1.Length > a2.Length)
			{
				return 1;
			}
			return 0;
		}

		public static string ToString(double[] doubles, string glue)
		{
			string s = string.Empty;
			for (int i = 0; i < doubles.Length; i++)
			{
				if (i == 0)
				{
					s = doubles[i].ToString();
				}
				else
				{
					s += glue + doubles[i].ToString();
				}
			}
			return s;
		}
	}
}
