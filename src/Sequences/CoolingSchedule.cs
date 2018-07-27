using System;


namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>
	/// grenager
	/// Date: Dec 14, 2004
	/// </author>
	public abstract class CoolingSchedule
	{
		public abstract int NumIterations();

		public abstract double GetTemperature(int iteration);

		public static CoolingSchedule GetExponentialSchedule(double start, double rate, int numIterations)
		{
			return new _CoolingSchedule_13(numIterations, start, rate);
		}

		private sealed class _CoolingSchedule_13 : CoolingSchedule
		{
			public _CoolingSchedule_13(int numIterations, double start, double rate)
			{
				this.numIterations = numIterations;
				this.start = start;
				this.rate = rate;
			}

			public override int NumIterations()
			{
				return numIterations;
			}

			public override double GetTemperature(int iteration)
			{
				return start * Math.Pow(rate, iteration);
			}

			private readonly int numIterations;

			private readonly double start;

			private readonly double rate;
		}

		public static CoolingSchedule GetLinearSchedule(double start, int numIterations)
		{
			return new _CoolingSchedule_26(start, numIterations);
		}

		private sealed class _CoolingSchedule_26 : CoolingSchedule
		{
			public _CoolingSchedule_26(double start, int numIterations)
			{
				this.start = start;
				this.numIterations = numIterations;
				this.rate = start / numIterations;
			}

			internal readonly double rate;

			public override int NumIterations()
			{
				return numIterations + 1;
			}

			// will hit zero on the last one
			public override double GetTemperature(int iteration)
			{
				return start - this.rate * iteration;
			}

			private readonly double start;

			private readonly int numIterations;
		}
	}
}
