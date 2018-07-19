using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Math
{
	/// <summary>
	/// The class
	/// <c>DoubleAD</c>
	/// was created to extend the
	/// current calculations of gradient to automatically include a calculation of the
	/// Hessian vector product with another vector
	/// <c>v</c>
	/// .  This is used with the
	/// Stochastic Meta Descent Optimization, but could be extended for use in any application
	/// that requires an additional order of differentiation without explicitly creating the code.
	/// </summary>
	/// <author>Alex Kleeman</author>
	/// <version>2006/12/06</version>
	[System.Serializable]
	public class DoubleAD : Number
	{
		private const long serialVersionUID = -5702334375099248894L;

		private double val;

		private double dot;

		public DoubleAD()
		{
			Setval(0);
			Setdot(1);
		}

		public DoubleAD(double initVal, double initDot)
		{
			val = initVal;
			dot = initDot;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!(obj is Edu.Stanford.Nlp.Math.DoubleAD))
			{
				return false;
			}
			Edu.Stanford.Nlp.Math.DoubleAD b = (Edu.Stanford.Nlp.Math.DoubleAD)obj;
			return b.Getval() == val && b.Getdot() == dot;
		}

		public virtual bool Equals(double valToCompare, double dotToCompare)
		{
			return valToCompare == val && dotToCompare == dot;
		}

		public virtual bool Equals(double valToCompare, double dotToCompare, double Tol)
		{
			return System.Math.Abs(valToCompare - val) < Tol && System.Math.Abs(dotToCompare - dot) < Tol;
		}

		public virtual double Getval()
		{
			return val;
		}

		public virtual double Getdot()
		{
			return dot;
		}

		public virtual void Set(double value, double dotValue)
		{
			val = value;
			dot = dotValue;
		}

		public virtual void Setval(double a)
		{
			val = a;
		}

		public virtual void Setdot(double a)
		{
			dot = a;
		}

		public virtual void PlusEqualsConst(double a)
		{
			Setval(val + a);
		}

		public virtual void PlusEquals(Edu.Stanford.Nlp.Math.DoubleAD a)
		{
			Setval(val + a.Getval());
			Setdot(dot + a.Getdot());
		}

		public virtual void MinusEquals(Edu.Stanford.Nlp.Math.DoubleAD a)
		{
			Setval(val - a.Getval());
			Setdot(dot - a.Getdot());
		}

		public virtual void MinusEqualsConst(double a)
		{
			Setval(val - a);
		}

		public override double DoubleValue()
		{
			return Getval();
		}

		public override float FloatValue()
		{
			return (float)this;
		}

		public override int IntValue()
		{
			return (int)this;
		}

		public override long LongValue()
		{
			return (long)this;
		}

		public override string ToString()
		{
			return "Value= " + val + "; Dot= " + dot;
		}

		public override int GetHashCode()
		{
			int result;
			long temp;
			temp = double.DoubleToLongBits(val);
			result = (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			temp = double.DoubleToLongBits(dot);
			result = 31 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			return result;
		}
	}
}
