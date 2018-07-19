using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>This is used to convert an array of double into byte array which makes it possible to keep it more efficiently.</summary>
	/// <author>Kristina Toutanova</author>
	public sealed class ConvertByteArray
	{
		private ConvertByteArray()
		{
		}

		private static short Shortflag = unchecked((int)(0x00ff));

		private static int Intflag = unchecked((int)(0x000000ff));

		private static long Longflag = unchecked((int)(0x00000000000000ff));

		// static methods
		public static void WriteIntToByteArr(byte[] b, int off, int i)
		{
			b[off] = unchecked((byte)i);
			b[off + 1] = unchecked((byte)(i >> 8));
			b[off + 2] = unchecked((byte)(i >> 16));
			b[off + 3] = unchecked((byte)(i >> 24));
		}

		public static void WriteLongToByteArr(byte[] b, int off, long l)
		{
			for (int i = 0; i < 8; i++)
			{
				b[off + i] = unchecked((byte)(l >> (8 * i)));
			}
		}

		public static void WriteFloatToByteArr(byte[] b, int off, float f)
		{
			int i = Sharpen.Runtime.FloatToIntBits(f);
			WriteIntToByteArr(b, off, i);
		}

		public static void WriteDoubleToByteArr(byte[] b, int off, double d)
		{
			long l = double.DoubleToLongBits(d);
			WriteLongToByteArr(b, off, l);
		}

		public static void WriteBooleanToByteArr(byte[] b, int off, bool @bool)
		{
			if (@bool)
			{
				b[off] = 0;
			}
			else
			{
				b[off] = 1;
			}
		}

		public static void WriteCharToByteArr(byte[] b, int off, char c)
		{
			b[off + 1] = unchecked((byte)c);
			b[off] = unchecked((byte)(c >> 8));
		}

		public static void WriteShortToByteArr(byte[] b, int off, short s)
		{
			b[off] = unchecked((byte)s);
			b[off + 1] = unchecked((byte)(s >> 8));
		}

		public static void WriteUStringToByteArr(byte[] b, int off, string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				b[2 * i + 1 + off] = unchecked((byte)c);
				b[2 * i + off] = unchecked((byte)(c >> 8));
			}
		}

		public static void WriteUStringToByteArr(byte[] b, int off, string s, int pos, int length)
		{
			for (int i = pos; i < (pos + length); i++)
			{
				char c = s[i];
				b[2 * i + 1 + off] = unchecked((byte)c);
				b[2 * i + off] = unchecked((byte)(c >> 8));
			}
		}

		public static void WriteAStringToByteArr(byte[] b, int off, string s)
		{
			System.Array.Copy(Sharpen.Runtime.GetBytesForString(s), 0, b, off, s.Length);
		}

		public static void WriteAStringToByteArr(byte[] b, int off, string s, int pos, int length)
		{
			string sub = Sharpen.Runtime.Substring(s, pos, pos + length);
			WriteAStringToByteArr(b, off, sub);
		}

		//-------------------------------------------------------------------
		public static int ByteArrToInt(byte[] b, int off)
		{
			int z = 0;
			for (int i = 3; i > 0; i--)
			{
				z = (z | (b[off + i] & Intflag)) << 8;
			}
			z = z | (b[off] & Intflag);
			return z;
		}

		public static short ByteArrToShort(byte[] b, int off)
		{
			short s = (short)(((0 | (b[off + 1] & Shortflag)) << 8) | (b[off] & Shortflag));
			return s;
		}

		public static float ByteArrToFloat(byte[] b, int off)
		{
			int i = ByteArrToInt(b, off);
			return Sharpen.Runtime.IntBitsToFloat(i);
		}

		public static double ByteArrToDouble(byte[] b, int off)
		{
			long l = ByteArrToLong(b, off);
			return double.LongBitsToDouble(l);
		}

		public static long ByteArrToLong(byte[] b, int off)
		{
			long z = 0;
			for (int i = 7; i > 0; i--)
			{
				z = (z | (b[off + i] & Longflag)) << 8;
			}
			z = z | (b[off] & Longflag);
			return z;
		}

		public static bool ByteArrToBoolean(byte[] b, int off)
		{
			return b[off] == 0;
		}

		public static char ByteArrToChar(byte[] b, int off)
		{
			char c = (char)((b[off] << 8) | b[off + 1]);
			return c;
		}

		public static string ByteArrToUString(byte[] b)
		{
			string s;
			if (b.Length == 0)
			{
				s = string.Empty;
			}
			else
			{
				char[] c = new char[b.Length / 2];
				for (int i = 0; i < (b.Length / 2); i++)
				{
					int j = (b[2 * i] << 8) | b[2 * i + 1];
					c[i] = (char)j;
				}
				s = new string(c);
			}
			return s;
		}

		public static string ByteArrToUString(byte[] b, int off, int strLen)
		{
			string s;
			if (strLen == 0)
			{
				s = string.Empty;
			}
			else
			{
				char[] c = new char[strLen];
				for (int i = 0; i < strLen; i++)
				{
					int j = (b[2 * i + off] << 8) | b[2 * i + 1 + off];
					c[i] = (char)j;
				}
				s = new string(c);
			}
			return s;
		}

		public static string ByteArrToAString(byte[] b)
		{
			return Sharpen.Runtime.GetStringForBytes(b);
		}

		public static string ByteArrToAString(byte[] b, int off, int length)
		{
			if (length == 0)
			{
				return string.Empty;
			}
			else
			{
				return Sharpen.Runtime.GetStringForBytes(b, off, length);
			}
		}

		//-----------------------------------------------------------------
		public static byte[] StringUToByteArr(string s)
		{
			char c;
			byte[] b = new byte[2 * s.Length];
			for (int i = 0; i < s.Length; i++)
			{
				c = s[i];
				b[2 * i + 1] = unchecked((byte)c);
				b[2 * i] = unchecked((byte)(c >> 8));
			}
			return b;
		}

		public static byte[] StringAToByteArr(string s)
		{
			return Sharpen.Runtime.GetBytesForString(s);
		}

		//-----------------------------------------------------------------
		public static byte[] IntArrToByteArr(int[] i)
		{
			return IntArrToByteArr(i, 0, i.Length);
		}

		public static byte[] IntArrToByteArr(int[] i, int off, int length)
		{
			byte[] y = new byte[4 * length];
			for (int j = off; j < (off + length); j++)
			{
				y[4 * (j - off)] = unchecked((byte)i[j]);
				y[4 * (j - off) + 1] = unchecked((byte)(i[j] >> 8));
				y[4 * (j - off) + 2] = unchecked((byte)(i[j] >> 16));
				y[4 * (j - off) + 3] = unchecked((byte)(i[j] >> 24));
			}
			return y;
		}

		public static void IntArrToByteArr(byte[] b, int pos, int[] i, int off, int len)
		{
			for (int j = off; j < (off + len); j++)
			{
				b[4 * (j - off) + pos] = unchecked((byte)i[j]);
				b[4 * (j - off) + 1 + pos] = unchecked((byte)(i[j] >> 8));
				b[4 * (j - off) + 2 + pos] = unchecked((byte)(i[j] >> 16));
				b[4 * (j - off) + 3 + pos] = unchecked((byte)(i[j] >> 24));
			}
		}

		public static byte[] LongArrToByteArr(long[] l)
		{
			return LongArrToByteArr(l, 0, l.Length);
		}

		public static byte[] LongArrToByteArr(long[] l, int off, int length)
		{
			byte[] b = new byte[8 * length];
			for (int j = off; j < (off + length); j++)
			{
				for (int i = 0; i < 8; i++)
				{
					b[8 * (j - off) + i] = unchecked((byte)(l[j] >> (8 * i)));
				}
			}
			return b;
		}

		public static void LongArrToByteArr(byte[] b, int pos, long[] l, int off, int length)
		{
			for (int j = off; j < (off + length); j++)
			{
				for (int i = 0; i < 8; i++)
				{
					b[8 * (j - off) + i + pos] = unchecked((byte)(l[j] >> (8 * i)));
				}
			}
		}

		public static byte[] BooleanArrToByteArr(bool[] b)
		{
			return BooleanArrToByteArr(b, 0, b.Length);
		}

		public static byte[] BooleanArrToByteArr(bool[] b, int off, int len)
		{
			byte[] bytes = new byte[len];
			for (int i = 0; i < len; i++)
			{
				if (b[i])
				{
					bytes[i] = 0;
				}
				else
				{
					bytes[i] = 1;
				}
			}
			return bytes;
		}

		public static void BooleanArrToByteArr(byte[] bytes, int pos, bool[] b, int off, int length)
		{
			for (int i = 0; i < length; i++)
			{
				if (b[i])
				{
					bytes[i + pos] = 0;
				}
				else
				{
					bytes[i + pos] = 1;
				}
			}
		}

		public static byte[] CharArrToByteArr(char[] c)
		{
			return CharArrToByteArr(c, 0, c.Length);
		}

		public static byte[] CharArrToByteArr(char[] c, int off, int len)
		{
			byte[] b = new byte[len * 2];
			for (int i = 0; i < len; i++)
			{
				b[2 * i + 1] = unchecked((byte)c[off + i]);
				b[2 * i] = unchecked((byte)(c[off + i] >> 8));
			}
			return b;
		}

		public static void CharArrToByteArr(byte[] b, int pos, char[] c, int off, int len)
		{
			for (int i = 0; i < len; i++)
			{
				b[2 * i + 1 + pos] = unchecked((byte)c[off + i]);
				b[2 * i + pos] = unchecked((byte)(c[off + i] >> 8));
			}
		}

		public static byte[] FloatArrToByteArr(float[] f)
		{
			return FloatArrToByteArr(f, 0, f.Length);
		}

		public static byte[] FloatArrToByteArr(float[] f, int off, int length)
		{
			byte[] y = new byte[4 * length];
			for (int j = off; j < (off + length); j++)
			{
				int i = Sharpen.Runtime.FloatToIntBits(f[j]);
				y[4 * (j - off)] = unchecked((byte)i);
				y[4 * (j - off) + 1] = unchecked((byte)(i >> 8));
				y[4 * (j - off) + 2] = unchecked((byte)(i >> 16));
				y[4 * (j - off) + 3] = unchecked((byte)(i >> 24));
			}
			return y;
		}

		public static void FloatArrToByteArr(byte[] b, int pos, float[] f, int off, int len)
		{
			for (int j = off; j < (off + len); j++)
			{
				int i = Sharpen.Runtime.FloatToIntBits(f[j]);
				b[4 * (j - off) + pos] = unchecked((byte)i);
				b[4 * (j - off) + 1 + pos] = unchecked((byte)(i >> 8));
				b[4 * (j - off) + 2 + pos] = unchecked((byte)(i >> 16));
				b[4 * (j - off) + 3 + pos] = unchecked((byte)(i >> 24));
			}
		}

		public static byte[] DoubleArrToByteArr(double[] d)
		{
			return DoubleArrToByteArr(d, 0, d.Length);
		}

		public static byte[] DoubleArrToByteArr(double[] d, int off, int length)
		{
			byte[] b = new byte[8 * length];
			for (int j = off; j < (off + length); j++)
			{
				long l = double.DoubleToLongBits(d[j]);
				for (int i = 0; i < 8; i++)
				{
					b[8 * (j - off) + i] = unchecked((byte)(l >> (8 * i)));
				}
			}
			return b;
		}

		public static void DoubleArrToByteArr(byte[] b, int pos, double[] d, int off, int length)
		{
			for (int j = off; j < (off + length); j++)
			{
				long l = double.DoubleToLongBits(d[j]);
				for (int i = 0; i < 8; i++)
				{
					b[8 * (j - off) + i + pos] = unchecked((byte)(l >> (8 * i)));
				}
			}
		}

		public static byte[] ShortArrToByteArr(short[] s)
		{
			return ShortArrToByteArr(s, 0, s.Length);
		}

		public static byte[] ShortArrToByteArr(short[] s, int off, int length)
		{
			byte[] y = new byte[2 * length];
			for (int j = off; j < (off + length); j++)
			{
				y[4 * (j - off)] = unchecked((byte)s[j]);
				y[4 * (j - off) + 1] = unchecked((byte)(s[j] >> 8));
			}
			return y;
		}

		public static void ShortArrToByteArr(byte[] b, int pos, short[] s, int off, int len)
		{
			for (int j = off; j < (off + len); j++)
			{
				b[4 * (j - off) + pos] = unchecked((byte)s[j]);
				b[4 * (j - off) + 1 + pos] = unchecked((byte)(s[j] >> 8));
			}
		}

		public static byte[] UStringArrToByteArr(string[] s)
		{
			return UStringArrToByteArr(s, 0, s.Length);
		}

		public static byte[] UStringArrToByteArr(string[] s, int off, int length)
		{
			int byteOff = 0;
			int byteCount = 0;
			for (int i = off; i < (off + length); i++)
			{
				if (s[i] != null)
				{
					byteCount += 2 * s[i].Length;
				}
			}
			byte[] b = new byte[byteCount + 4 * s.Length];
			for (int i_1 = off; i_1 < (off + length); i_1++)
			{
				if (s[i_1] != null)
				{
					WriteIntToByteArr(b, byteOff, s[i_1].Length);
					byteOff += 4;
					WriteUStringToByteArr(b, byteOff, s[i_1]);
					byteOff += 2 * s[i_1].Length;
				}
				else
				{
					WriteIntToByteArr(b, byteOff, 0);
					byteOff += 4;
				}
			}
			return b;
		}

		public static void UStringArrToByteArr(byte[] b, int pos, string[] s, int off, int length)
		{
			for (int i = off; i < (off + length); i++)
			{
				if (s[i] != null)
				{
					WriteIntToByteArr(b, pos, s[i].Length);
					pos += 4;
					WriteUStringToByteArr(b, pos, s[i]);
					pos += 2 * s[i].Length;
				}
				else
				{
					WriteIntToByteArr(b, pos, 0);
					pos += 4;
				}
			}
		}

		public static byte[] AStringArrToByteArr(string[] s)
		{
			return AStringArrToByteArr(s, 0, s.Length);
		}

		public static byte[] AStringArrToByteArr(string[] s, int off, int length)
		{
			int byteOff = 0;
			int byteCount = 0;
			for (int i = off; i < (off + length); i++)
			{
				if (s[i] != null)
				{
					byteCount += s[i].Length;
				}
			}
			byte[] b = new byte[byteCount + 4 * s.Length];
			for (int i_1 = off; i_1 < (off + length); i_1++)
			{
				if (s[i_1] != null)
				{
					WriteIntToByteArr(b, byteOff, s[i_1].Length);
					byteOff += 4;
					WriteAStringToByteArr(b, byteOff, s[i_1]);
					byteOff += s[i_1].Length;
				}
				else
				{
					WriteIntToByteArr(b, byteOff, 0);
					byteOff += 4;
				}
			}
			return b;
		}

		public static void AStringArrToByteArr(byte[] b, int pos, string[] s, int off, int length)
		{
			for (int i = off; i < (off + length); i++)
			{
				if (s[i] != null)
				{
					WriteIntToByteArr(b, pos, s[i].Length);
					pos += 4;
					WriteAStringToByteArr(b, pos, s[i]);
					pos += s[i].Length;
				}
				else
				{
					WriteIntToByteArr(b, pos, 0);
					pos += 4;
				}
			}
		}

		//-----------------------------------------------------------------
		public static int[] ByteArrToIntArr(byte[] b)
		{
			return ByteArrToIntArr(b, 0, b.Length / 4);
		}

		public static int[] ByteArrToIntArr(byte[] b, int off, int length)
		{
			int[] z = new int[length];
			for (int i = 0; i < length; i++)
			{
				z[i] = 0;
				for (int j = 3; j > 0; j--)
				{
					z[i] = (z[i] | (b[off + j + 4 * i] & Intflag)) << 8;
				}
				z[i] = z[i] | (b[off + 4 * i] & Intflag);
			}
			return z;
		}

		public static void ByteArrToIntArr(byte[] b, int off, int[] i, int pos, int length)
		{
			for (int j = 0; j < length; j++)
			{
				i[j + pos] = 0;
				for (int k = 3; k > 0; k--)
				{
					i[j + pos] = (i[j + pos] | (b[off + k + 4 * j] & Intflag)) << 8;
				}
				i[j + pos] = i[j + pos] | (b[off + 4 * j] & Intflag);
			}
		}

		public static long[] ByteArrToLongArr(byte[] b)
		{
			return ByteArrToLongArr(b, 0, b.Length / 8);
		}

		public static long[] ByteArrToLongArr(byte[] b, int off, int length)
		{
			long[] l = new long[length];
			for (int i = 0; i < length; i++)
			{
				l[i] = 0;
				for (int j = 0; j < 8; j++)
				{
					l[i] = l[i] | ((b[8 * i + j + off] & unchecked((int)(0x000000ff))) << (8 * j));
				}
			}
			return l;
		}

		public static void ByteArrToLongArr(byte[] b, int off, long[] l, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				l[i + pos] = 0;
				for (int j = 0; j < 8; j++)
				{
					l[i + pos] = l[i + pos] | ((b[8 * i + j + off] & unchecked((int)(0x000000ff))) << (8 * j));
				}
			}
		}

		public static bool[] ByteArrToBooleanArr(byte[] b)
		{
			return ByteArrToBooleanArr(b, 0, b.Length);
		}

		public static bool[] ByteArrToBooleanArr(byte[] b, int off, int length)
		{
			bool[] @bool = new bool[length];
			for (int i = 0; i < length; i++)
			{
				@bool[i] = b[i + off] == 0;
			}
			return @bool;
		}

		public static void ByteArrToBooleanArr(byte[] b, int off, bool[] @bool, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				@bool[i + pos] = b[i + off] == 0;
			}
		}

		public static char[] ByteArrToCharArr(byte[] b)
		{
			return ByteArrToCharArr(b, 0, b.Length / 2);
		}

		public static char[] ByteArrToCharArr(byte[] b, int off, int length)
		{
			char[] c = new char[length];
			for (int i = 0; i < (length); i++)
			{
				c[i] = (char)((b[2 * i + off] << 8) | b[2 * i + 1 + off]);
			}
			return c;
		}

		public static void ByteArrToCharArr(byte[] b, int off, char[] c, int pos, int length)
		{
			for (int i = 0; i < (length); i++)
			{
				c[i + pos] = (char)((b[2 * i + off] << 8) | b[2 * i + 1 + off]);
			}
		}

		public static short[] ByteArrToShortArr(byte[] b)
		{
			return ByteArrToShortArr(b, 0, b.Length / 2);
		}

		public static short[] ByteArrToShortArr(byte[] b, int off, int length)
		{
			short[] z = new short[length];
			for (int i = 0; i < length; i++)
			{
				z[i] = (short)(((0 | (b[off + 1 + 2 * i] & Shortflag)) << 8) | (b[off + 2 * i] & Shortflag));
			}
			return z;
		}

		public static void ByteArrToShortArr(byte[] b, int off, short[] s, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				s[i + pos] = (short)(((0 | (b[off + 1 + 2 * i] & Shortflag)) << 8) | (b[off + 2 * i] & Shortflag));
			}
		}

		public static float[] ByteArrToFloatArr(byte[] b)
		{
			return ByteArrToFloatArr(b, 0, b.Length / 4);
		}

		public static float[] ByteArrToFloatArr(byte[] b, int off, int length)
		{
			float[] z = new float[length];
			for (int i = 0; i < length; i++)
			{
				int k = 0;
				for (int j = 3; j > 0; j--)
				{
					k = (k | (b[off + j + 4 * i] & Intflag)) << 8;
				}
				k = k | (b[off + 4 * i] & Intflag);
				z[i] = Sharpen.Runtime.IntBitsToFloat(k);
			}
			return z;
		}

		public static void ByteArrToFloatArr(byte[] b, int off, float[] f, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				int k = 0;
				for (int j = 3; j > 0; j--)
				{
					k = (k | (b[off + j + 4 * i] & Intflag)) << 8;
				}
				k = k | (b[off + 4 * i] & Intflag);
				f[pos + i] = Sharpen.Runtime.IntBitsToFloat(k);
			}
		}

		/// <summary>
		/// This method allocates a new double[] to return, based on the size of
		/// the array b (namely b.length / 8 in size)
		/// </summary>
		/// <param name="b">Array to decode to doubles</param>
		/// <returns>Array of doubles.</returns>
		public static double[] ByteArrToDoubleArr(byte[] b)
		{
			return ByteArrToDoubleArr(b, 0, b.Length / 8);
		}

		public static double[] ByteArrToDoubleArr(byte[] b, int off, int length)
		{
			double[] d = new double[length];
			for (int i = 0; i < length; i++)
			{
				long l = 0;
				for (int j = 0; j < 8; j++)
				{
					l = l | ((long)(b[8 * i + j + off] & unchecked((int)(0x00000000000000ff))) << (8 * j));
				}
				d[i] = double.LongBitsToDouble(l);
			}
			return d;
		}

		public static void ByteArrToDoubleArr(byte[] b, int off, double[] d, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				long l = 0;
				for (int j = 0; j < 8; j++)
				{
					l = l | ((long)(b[8 * i + j + off] & unchecked((int)(0x00000000000000ff))) << (8 * j));
				}
				d[pos + i] = double.LongBitsToDouble(l);
			}
		}

		public static string[] ByteArrToUStringArr(byte[] b)
		{
			int off = 0;
			Vector<string> v = new Vector<string>();
			while (off < b.Length)
			{
				int length = ByteArrToInt(b, off);
				if (length != 0)
				{
					v.Add(ByteArrToUString(b, off + 4, length));
				}
				else
				{
					v.Add(string.Empty);
				}
				off = off + 2 * length + 4;
			}
			string[] s = new string[v.Count];
			for (int i = 0; i < s.Length; i++)
			{
				s[i] = v[i];
			}
			return s;
		}

		public static string[] ByteArrToUStringArr(byte[] b, int off, int length)
		{
			string[] s = new string[length];
			for (int i = 0; i < length; i++)
			{
				int stringLen = ByteArrToInt(b, off);
				off += 4;
				if (stringLen != 0)
				{
					s[i] = ByteArrToUString(b, off, stringLen);
					off += 2 * s[i].Length;
				}
				else
				{
					s[i] = string.Empty;
				}
			}
			return s;
		}

		public static void ByteArrToUStringArr(byte[] b, int off, string[] s, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				int stringLen = ByteArrToInt(b, off);
				off += 4;
				if (stringLen != 0)
				{
					s[i + pos] = ByteArrToUString(b, off, stringLen);
					off += 2 * s[i].Length;
				}
				else
				{
					s[i] = string.Empty;
				}
			}
		}

		public static string[] ByteArrToAStringArr(byte[] b)
		{
			int off = 0;
			Vector<string> v = new Vector<string>();
			while (off < b.Length)
			{
				int length = ByteArrToInt(b, off);
				if (length != 0)
				{
					v.Add(ByteArrToAString(b, off + 4, length));
				}
				else
				{
					v.Add(string.Empty);
				}
				off = off + length + 4;
			}
			string[] s = new string[v.Count];
			for (int i = 0; i < s.Length; i++)
			{
				s[i] = v[i];
			}
			return s;
		}

		public static string[] ByteArrToAStringArr(byte[] b, int off, int length)
		{
			string[] s = new string[length];
			for (int i = 0; i < length; i++)
			{
				int stringLen = ByteArrToInt(b, off);
				off += 4;
				if (stringLen != 0)
				{
					s[i] = ByteArrToAString(b, off, stringLen);
					off += s[i].Length;
				}
				else
				{
					s[i] = string.Empty;
				}
			}
			return s;
		}

		public static void ByteArrToAStringArr(byte[] b, int off, string[] s, int pos, int length)
		{
			for (int i = 0; i < length; i++)
			{
				int stringLen = ByteArrToInt(b, off);
				off += 4;
				if (stringLen != 0)
				{
					s[i + pos] = ByteArrToAString(b, off, stringLen);
					off += s[i].Length;
				}
				else
				{
					s[i] = string.Empty;
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void SaveDoubleArr(DataOutputStream rf, double[] arr)
		{
			rf.WriteInt(arr.Length);
			byte[] lArr = DoubleArrToByteArr(arr);
			rf.Write(lArr);
			rf.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void SaveFloatArr(DataOutputStream rf, float[] arr)
		{
			rf.WriteInt(arr.Length);
			byte[] lArr = FloatArrToByteArr(arr);
			rf.Write(lArr);
			rf.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static double[] ReadDoubleArr(DataInputStream rf)
		{
			int size = rf.ReadInt();
			byte[] b = new byte[8 * size];
			rf.Read(b);
			return ByteArrToDoubleArr(b);
		}

		/// <exception cref="System.IO.IOException"/>
		public static float[] ReadFloatArr(DataInputStream rf)
		{
			int size = rf.ReadInt();
			byte[] b = new byte[4 * size];
			rf.Read(b);
			return ByteArrToFloatArr(b);
		}
	}
}
