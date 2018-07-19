using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Find the (Levenshtein) edit distance between two Strings or Character
	/// arrays.
	/// </summary>
	/// <remarks>
	/// Find the (Levenshtein) edit distance between two Strings or Character
	/// arrays.
	/// By default it allows transposition.
	/// <br />
	/// This is an object so that you can save on the cost of allocating /
	/// deallocating the large array when possible
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>John Bauer - rewrote using DP instead of memorization</author>
	public class EditDistance
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.EditDistance));

		internal readonly bool allowTranspose;

		protected internal double[][] score = null;

		public EditDistance()
		{
			allowTranspose = true;
		}

		public EditDistance(bool allowTranspose)
		{
			this.allowTranspose = allowTranspose;
		}

		protected internal virtual void Clear(int sourceLength, int targetLength)
		{
			if (score == null || score.Length < sourceLength + 1 || score[0].Length < targetLength + 1)
			{
				score = new double[][] {  };
			}
			foreach (double[] aScore in score)
			{
				Arrays.Fill(aScore, Worst());
			}
		}

		// CONSTRAINT SEMIRING START
		protected internal virtual double Best()
		{
			return 0.0;
		}

		protected internal virtual double Worst()
		{
			return double.PositiveInfinity;
		}

		protected internal virtual double Unit()
		{
			return 1.0;
		}

		protected internal virtual double Better(double x, double y)
		{
			if (x < y)
			{
				return x;
			}
			return y;
		}

		protected internal virtual double Combine(double x, double y)
		{
			return x + y;
		}

		// CONSTRAINT SEMIRING END
		// COST FUNCTION BEGIN
		protected internal virtual double InsertCost(object o)
		{
			return Unit();
		}

		protected internal virtual double DeleteCost(object o)
		{
			return Unit();
		}

		protected internal virtual double SubstituteCost(object source, object target)
		{
			if (source.Equals(target))
			{
				return Best();
			}
			return Unit();
		}

		internal virtual double TransposeCost(object s1, object s2, object t1, object t2)
		{
			if (s1.Equals(t2) && s2.Equals(t1))
			{
				if (allowTranspose)
				{
					return Unit();
				}
				else
				{
					return 2 * Unit();
				}
			}
			return Worst();
		}

		// COST FUNCTION END
		internal virtual double Score(object[] source, int sPos, object[] target, int tPos)
		{
			for (int i = 0; i <= sPos; ++i)
			{
				for (int j = 0; j <= tPos; ++j)
				{
					double bscore = score[i][j];
					if (bscore != Worst())
					{
						continue;
					}
					if (i == 0 && j == 0)
					{
						bscore = Best();
					}
					else
					{
						if (i > 0)
						{
							bscore = Better(bscore, (Combine(score[i - 1][j], DeleteCost(source[i - 1]))));
						}
						if (j > 0)
						{
							bscore = Better(bscore, (Combine(score[i][j - 1], InsertCost(target[j - 1]))));
						}
						if (i > 0 && j > 0)
						{
							bscore = Better(bscore, (Combine(score[i - 1][j - 1], SubstituteCost(source[i - 1], target[j - 1]))));
						}
						if (i > 1 && j > 1)
						{
							bscore = Better(bscore, (Combine(score[i - 2][j - 2], TransposeCost(source[i - 2], source[i - 1], target[j - 2], target[j - 1]))));
						}
					}
					score[i][j] = bscore;
				}
			}
			return score[sPos][tPos];
		}

		public virtual double Score(object[] source, object[] target)
		{
			Clear(source.Length, target.Length);
			return Score(source, source.Length, target, target.Length);
		}

		public virtual double Score(string sourceStr, string targetStr)
		{
			if (sourceStr.Equals(targetStr))
			{
				return 0;
			}
			object[] source = Characters.AsCharacterArray(sourceStr);
			object[] target = Characters.AsCharacterArray(targetStr);
			Clear(source.Length, target.Length);
			return Score(source, source.Length, target, target.Length);
		}

		public static void Main(string[] args)
		{
			if (args.Length >= 2)
			{
				Edu.Stanford.Nlp.Util.EditDistance d = new Edu.Stanford.Nlp.Util.EditDistance();
				System.Console.Out.WriteLine(d.Score(args[0], args[1]));
			}
			else
			{
				log.Info("usage: java EditDistance str1 str2");
			}
		}
	}
}
