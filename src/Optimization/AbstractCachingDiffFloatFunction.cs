using System;



namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Dan Klein</author>
	public abstract class AbstractCachingDiffFloatFunction : IDiffFloatFunction, IHasFloatInitial
	{
		private float[] lastX = null;

		protected internal float[] derivative = null;

		protected internal float value = 0.0f;

		public abstract int DomainDimension();

		/// <summary>Calculate the value at x and the derivative and save them in the respective fields</summary>
		protected internal abstract void Calculate(float[] x);

		public virtual float[] Initial()
		{
			float[] initial = new float[DomainDimension()];
			// Arrays.fill(initial, 0.0f);  // not needed; Java arrays zero initialized
			return initial;
		}

		protected internal static void Copy(float[] y, float[] x)
		{
			System.Array.Copy(x, 0, y, 0, x.Length);
		}

		internal virtual void Ensure(float[] x)
		{
			if (Arrays.Equals(x, lastX))
			{
				return;
			}
			if (lastX == null)
			{
				lastX = new float[DomainDimension()];
			}
			if (derivative == null)
			{
				derivative = new float[DomainDimension()];
			}
			Copy(lastX, x);
			Calculate(x);
		}

		public virtual float ValueAt(float[] x)
		{
			Ensure(x);
			return value;
		}

		internal static float Norm2(float[] x)
		{
			float sum = 0.0f;
			foreach (float aX in x)
			{
				sum += aX * aX;
			}
			return (float)Math.Sqrt(sum);
		}

		public virtual float[] DerivativeAt(float[] x)
		{
			Ensure(x);
			return derivative;
		}
	}
}
