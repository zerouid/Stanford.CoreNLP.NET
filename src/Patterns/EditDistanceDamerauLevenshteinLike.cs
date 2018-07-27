using System;




namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>
	/// COPIED FROM https://gist.github.com/steveash (public domain license)
	/// Implementation of the OSA (optimal string alignment) which is similar
	/// to the Damerau-Levenshtein in that it allows for transpositions to
	/// count as a single edit distance, but is not a true metric and can
	/// over-estimate the cost because it disallows substrings to edited more than
	/// once.
	/// </summary>
	/// <remarks>
	/// COPIED FROM https://gist.github.com/steveash (public domain license)
	/// Implementation of the OSA (optimal string alignment) which is similar
	/// to the Damerau-Levenshtein in that it allows for transpositions to
	/// count as a single edit distance, but is not a true metric and can
	/// over-estimate the cost because it disallows substrings to edited more than
	/// once. See wikipedia for more discussion on OSA vs DL
	/// <p/>
	/// See Algorithms on Strings, Trees and Sequences by Dan Gusfield for more
	/// information.
	/// <p/>
	/// This also has a set of local buffer implementations to avoid allocating new
	/// buffers each time, which might be a premature optimization
	/// <p/>
	/// </remarks>
	/// <author>Steve Ash, copied by Sonal Gupta (changed to remove dependence on Google code)</author>
	public class EditDistanceDamerauLevenshteinLike
	{
		private const int threadLocalBufferSize = 64;

		private sealed class _ThreadLocal_30 : ThreadLocal<short[]>
		{
			public _ThreadLocal_30()
			{
			}

			protected override short[] InitialValue()
			{
				return new short[EditDistanceDamerauLevenshteinLike.threadLocalBufferSize];
			}
		}

		private static readonly ThreadLocal<short[]> costLocal = new _ThreadLocal_30();

		private sealed class _ThreadLocal_37 : ThreadLocal<short[]>
		{
			public _ThreadLocal_37()
			{
			}

			protected override short[] InitialValue()
			{
				return new short[EditDistanceDamerauLevenshteinLike.threadLocalBufferSize];
			}
		}

		private static readonly ThreadLocal<short[]> back1Local = new _ThreadLocal_37();

		private sealed class _ThreadLocal_44 : ThreadLocal<short[]>
		{
			public _ThreadLocal_44()
			{
			}

			protected override short[] InitialValue()
			{
				return new short[EditDistanceDamerauLevenshteinLike.threadLocalBufferSize];
			}
		}

		private static readonly ThreadLocal<short[]> back2Local = new _ThreadLocal_44();

		//return -1 if the edit distance is more than the threshold
		public static int EditDistance(ICharSequence s, ICharSequence t, int threshold)
		{
			System.Diagnostics.Debug.Assert((s != null));
			System.Diagnostics.Debug.Assert((t != null));
			System.Diagnostics.Debug.Assert((threshold >= 0));
			//"Cannot take edit distance of strings longer than 32k chars"
			System.Diagnostics.Debug.Assert((s.Length < short.MaxValue));
			System.Diagnostics.Debug.Assert((t.Length < short.MaxValue));
			if (s.Length + 1 > threadLocalBufferSize || t.Length + 1 > threadLocalBufferSize)
			{
				return EditDistanceWithNewBuffers(s, t, (short)threshold);
			}
			short[] cost = costLocal.Get();
			short[] back1 = back1Local.Get();
			short[] back2 = back2Local.Get();
			return EditDistanceWithBuffers(s, t, (short)threshold, back2, back1, cost);
		}

		internal static int EditDistanceWithNewBuffers(ICharSequence s, ICharSequence t, short threshold)
		{
			int slen = s.Length;
			short[] back1 = new short[slen + 1];
			// "up 1" row in table
			short[] back2 = new short[slen + 1];
			// "up 2" row in table
			short[] cost = new short[slen + 1];
			// "current cost"
			return EditDistanceWithBuffers(s, t, threshold, back2, back1, cost);
		}

		private static int EditDistanceWithBuffers(ICharSequence s, ICharSequence t, short threshold, short[] back2, short[] back1, short[] cost)
		{
			short slen = (short)s.Length;
			short tlen = (short)t.Length;
			// if one string is empty, the edit distance is necessarily the length of
			// the other
			if (slen == 0)
			{
				return tlen <= threshold ? tlen : -1;
			}
			else
			{
				if (tlen == 0)
				{
					return slen <= threshold ? slen : -1;
				}
			}
			// if lengths are different > k, then can't be within edit distance
			if (Math.Abs(slen - tlen) > threshold)
			{
				return -1;
			}
			if (slen > tlen)
			{
				// swap the two strings to consume less memory
				ICharSequence tmp = s;
				s = t;
				t = tmp;
				slen = tlen;
				tlen = (short)t.Length;
			}
			InitMemoiseTables(threshold, back2, back1, cost, slen);
			for (short j = 1; j <= tlen; j++)
			{
				cost[0] = j;
				// j is the cost of inserting this many characters
				// stripe bounds
				int min = Math.Max(1, j - threshold);
				int max = Min(slen, (short)(j + threshold));
				// at this iteration the left most entry is "too much" so reset it
				if (min > 1)
				{
					cost[min - 1] = short.MaxValue;
				}
				IterateOverStripe(s, t, j, cost, back1, back2, min, max);
				// swap our cost arrays to move on to the next "row"
				short[] tempCost = back2;
				back2 = back1;
				back1 = cost;
				cost = tempCost;
			}
			// after exit, the current cost is in back1
			// if back1[slen] > k then we exceeded, so return -1
			if (back1[slen] > threshold)
			{
				return -1;
			}
			return back1[slen];
		}

		private static void IterateOverStripe(ICharSequence s, ICharSequence t, short j, short[] cost, short[] back1, short[] back2, int min, int max)
		{
			// iterates over the stripe
			for (int i = min; i <= max; i++)
			{
				if (s[i - 1] == t[j - 1])
				{
					cost[i] = back1[i - 1];
				}
				else
				{
					cost[i] = (short)(1 + Min(cost[i - 1], back1[i], back1[i - 1]));
				}
				if (i >= 2 && j >= 2)
				{
					// possible transposition to check for
					if ((s[i - 2] == t[j - 1]) && s[i - 1] == t[j - 2])
					{
						cost[i] = Min(cost[i], (short)(back2[i - 2] + 1));
					}
				}
			}
		}

		private static void InitMemoiseTables(short threshold, short[] back2, short[] back1, short[] cost, short slen)
		{
			// initial "starting" values for inserting all the letters
			short boundary = (short)(Min(slen, threshold) + 1);
			for (short i = 0; i < boundary; i++)
			{
				back1[i] = i;
				back2[i] = i;
			}
			// need to make sure that we don't read a default value when looking "up"
			Arrays.Fill(back1, boundary, slen + 1, short.MaxValue);
			Arrays.Fill(back2, boundary, slen + 1, short.MaxValue);
			Arrays.Fill(cost, 0, slen + 1, short.MaxValue);
		}

		private static short Min(short a, short b)
		{
			return (a <= b ? a : b);
		}

		private static short Min(short a, short b, short c)
		{
			return Min(a, Min(b, c));
		}
	}
}
