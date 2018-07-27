


namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>An enum for the Simple CoreNLP API to represent a sentiment value.</summary>
	/// <author><a href="mailto:angeli@stanford.edu">Gabor Angeli</a></author>
	[System.Serializable]
	public sealed class SentimentClass
	{
		public static readonly SentimentClass VeryPositive = new SentimentClass();

		public static readonly SentimentClass Positive = new SentimentClass();

		public static readonly SentimentClass Neutral = new SentimentClass();

		public static readonly SentimentClass Negative = new SentimentClass();

		public static readonly SentimentClass VeryNegative = new SentimentClass();

		public bool IsPositive()
		{
			return this == SentimentClass.VeryPositive || this == SentimentClass.Positive;
		}

		public bool IsNegative()
		{
			return this == SentimentClass.VeryNegative || this == SentimentClass.Negative;
		}

		public bool IsExtreme()
		{
			return this == SentimentClass.VeryNegative || this == SentimentClass.VeryPositive;
		}

		public bool IsMild()
		{
			return !IsExtreme();
		}

		public bool IsNeutral()
		{
			return this == SentimentClass.Neutral;
		}

		/// <summary>
		/// Get the sentiment class from the Stanford Sentiment Treebank
		/// integer encoding.
		/// </summary>
		/// <remarks>
		/// Get the sentiment class from the Stanford Sentiment Treebank
		/// integer encoding. That is, an integer between 0 and 4 (inclusive)
		/// </remarks>
		/// <param name="sentiment">The Integer representation of a sentiment.</param>
		/// <returns>The sentiment class associated with that integer.</returns>
		public static SentimentClass FromInt(int sentiment)
		{
			switch (sentiment)
			{
				case 0:
				{
					return SentimentClass.VeryNegative;
				}

				case 1:
				{
					return SentimentClass.Negative;
				}

				case 2:
				{
					return SentimentClass.Neutral;
				}

				case 3:
				{
					return SentimentClass.Positive;
				}

				case 4:
				{
					return SentimentClass.VeryPositive;
				}

				default:
				{
					throw new NoSuchElementException("No sentiment value for integer: " + sentiment);
				}
			}
		}
	}
}
