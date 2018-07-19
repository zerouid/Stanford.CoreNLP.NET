using Sharpen;

namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>Quantifiable entity unit.</summary>
	/// <author>Angel Chang</author>
	public class Unit
	{
		protected internal string name;

		protected internal string symbol;

		protected internal string type;

		protected internal string system;

		protected internal string prefixSystem;

		protected internal Edu.Stanford.Nlp.IE.QE.Unit defaultUnit;

		protected internal double defaultUnitScale = 1.0;

		public Unit(string name, string symbol, string type)
		{
			// What unit should be used to express this unit
			this.name = name;
			this.symbol = symbol;
			this.type = type;
		}

		public Unit(string name, string symbol, string type, Edu.Stanford.Nlp.IE.QE.Unit defaultUnit, double defaultUnitScale)
		{
			this.name = name;
			this.symbol = symbol;
			this.type = type;
			this.defaultUnit = defaultUnit;
			this.defaultUnitScale = defaultUnitScale;
		}

		// TODO: unit specific formatting
		public virtual string Format(double amount)
		{
			return amount.ToString() + symbol;
		}

		public virtual string FormatInDefaultUnit(double amount)
		{
			if (defaultUnit != null && defaultUnit != this)
			{
				return defaultUnit.FormatInDefaultUnit(amount * defaultUnitScale);
			}
			else
			{
				return Format(amount);
			}
		}

		public virtual string GetName()
		{
			return name;
		}

		public virtual void SetName(string name)
		{
			this.name = name;
		}

		public virtual string GetSymbol()
		{
			return symbol;
		}

		public virtual void SetSymbol(string symbol)
		{
			this.symbol = symbol;
		}

		public virtual string GetType()
		{
			return type;
		}

		public virtual void SetType(string type)
		{
			this.type = type;
		}

		public virtual Edu.Stanford.Nlp.IE.QE.Unit GetDefaultUnit()
		{
			return defaultUnit;
		}

		public virtual void SetDefaultUnit(Edu.Stanford.Nlp.IE.QE.Unit defaultUnit)
		{
			this.defaultUnit = defaultUnit;
		}

		public virtual double GetDefaultUnitScale()
		{
			return defaultUnitScale;
		}

		public virtual void SetDefaultUnitScale(double defaultUnitScale)
		{
			this.defaultUnitScale = defaultUnitScale;
		}
	}
}
