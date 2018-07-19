using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>Quantifiable entity</summary>
	/// <author>Angel Chang</author>
	public class SimpleQuantifiableEntity
	{
		private double amount;

		private Unit unit;

		public SimpleQuantifiableEntity(double amount, Unit unit)
		{
			this.unit = unit;
			this.amount = amount;
		}

		public SimpleQuantifiableEntity(Number amount, Unit unit)
		{
			this.unit = unit;
			this.amount = amount;
		}

		public virtual double GetAmount()
		{
			return amount;
		}

		public virtual void SetAmount(double amount)
		{
			this.amount = amount;
		}

		public virtual Unit GetUnit()
		{
			return unit;
		}

		public virtual void SetUnit(Unit unit)
		{
			this.unit = unit;
		}

		public override string ToString()
		{
			return unit.FormatInDefaultUnit(amount);
		}
	}
}
