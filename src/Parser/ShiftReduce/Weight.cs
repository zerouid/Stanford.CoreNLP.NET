using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Stores one row of the sparse matrix which makes up the multiclass perceptron.</summary>
	/// <remarks>
	/// Stores one row of the sparse matrix which makes up the multiclass perceptron.
	/// Uses a lot of bit fiddling to get the desired results.  What we
	/// want is a row of scores representing transitions where each score
	/// is the score for that transition (for the feature using this Weight
	/// object).  Since the average model seems to have about 3 non-zero
	/// scores per feature, we condense that by keeping pairs of index and
	/// score.  However, we can then further condense that by bit packing
	/// the index and score into one long.  This cuts down on object
	/// creation and makes it faster to read/write the models.
	/// Thankfully, all of the unpleasant bit fiddling can be hidden away
	/// in this one class.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class Weight
	{
		public Weight()
		{
			packed = null;
		}

		public Weight(Edu.Stanford.Nlp.Parser.Shiftreduce.Weight other)
		{
			if (other.Size() == 0)
			{
				packed = null;
				return;
			}
			packed = ArrayUtils.Copy(other.packed);
			Condense();
		}

		public virtual int Size()
		{
			if (packed == null)
			{
				return 0;
			}
			return packed.Length;
		}

		private int UnpackIndex(int i)
		{
			long pack = packed[i];
			return (int)((long)(((ulong)pack) >> 32));
		}

		private float UnpackScore(int i)
		{
			long pack = packed[i];
			return Sharpen.Runtime.IntBitsToFloat((int)(pack & unchecked((int)(0xFFFFFFFF))));
		}

		private static long Pack(int index, float score)
		{
			long pack = ((long)(Sharpen.Runtime.FloatToIntBits(score))) & unchecked((long)(0x00000000FFFFFFFFL));
			pack = pack | (((long)index) << 32);
			return pack;
		}

		public virtual void Score(float[] scores)
		{
			for (int i = 0; i < Size(); ++i)
			{
				// Since this is the critical method, we optimize it even further.
				// We could do this:
				// int index = unpackIndex; float score = unpackScore;
				// That results in an extra array lookup
				long pack = packed[i];
				int index = (int)((long)(((ulong)pack) >> 32));
				float score = Sharpen.Runtime.IntBitsToFloat((int)(pack & unchecked((int)(0xFFFFFFFF))));
				scores[index] += score;
			}
		}

		public virtual void AddScaled(Edu.Stanford.Nlp.Parser.Shiftreduce.Weight other, float scale)
		{
			for (int i = 0; i < other.Size(); ++i)
			{
				int index = other.UnpackIndex(i);
				float score = other.UnpackScore(i);
				UpdateWeight(index, score * scale);
			}
		}

		public virtual void Condense()
		{
			if (packed == null)
			{
				return;
			}
			int nonzero = 0;
			for (int i = 0; i < packed.Length; ++i)
			{
				if (UnpackScore(i) != 0.0f)
				{
					++nonzero;
				}
			}
			if (nonzero == 0)
			{
				packed = null;
				return;
			}
			if (nonzero == packed.Length)
			{
				return;
			}
			long[] newPacked = new long[nonzero];
			int j = 0;
			for (int i_1 = 0; i_1 < packed.Length; ++i_1)
			{
				if (UnpackScore(i_1) == 0.0f)
				{
					continue;
				}
				int index = UnpackIndex(i_1);
				float score = UnpackScore(i_1);
				newPacked[j] = Pack(index, score);
				++j;
			}
			packed = newPacked;
		}

		public virtual void UpdateWeight(int index, float increment)
		{
			if (index < 0)
			{
				return;
			}
			if (packed == null)
			{
				packed = new long[1];
				packed[0] = Pack(index, increment);
				return;
			}
			for (int i = 0; i < packed.Length; ++i)
			{
				if (UnpackIndex(i) == index)
				{
					float score = UnpackScore(i);
					packed[i] = Pack(index, score + increment);
					return;
				}
			}
			long[] newPacked = new long[packed.Length + 1];
			for (int i_1 = 0; i_1 < packed.Length; ++i_1)
			{
				newPacked[i_1] = packed[i_1];
			}
			newPacked[packed.Length] = Pack(index, increment);
			packed = newPacked;
		}

		private long[] packed;

		private const long serialVersionUID = 1;
	}
}
