using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang.Reflect;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>List of units</summary>
	/// <author>Angel Chang</author>
	public class Units
	{
		public static void RegisterDerivedUnit(Env env, Type clazz, string derivedType, string suffix, string symbolSuffix)
		{
			FieldInfo[] fields = Sharpen.Runtime.GetDeclaredFields(clazz);
			foreach (FieldInfo field in fields)
			{
				bool isStatic = Modifier.IsStatic(field.GetModifiers());
				bool isUnit = typeof(Unit).IsAssignableFrom(field.GetType());
				if (isStatic && isUnit)
				{
					try
					{
						Unit unit = ErasureUtils.UncheckedCast(field.GetValue(null));
						RegisterDerivedUnit(env, unit, derivedType, suffix, symbolSuffix);
					}
					catch (MemberAccessException)
					{
					}
				}
			}
		}

		public static void RegisterDerivedUnit(Env env, Unit unit, string derivedType, string suffix, string symbolSuffix)
		{
			Unit derivedUnit = new Unit(unit.GetName() + " " + suffix, unit.GetSymbol() + symbolSuffix, derivedType);
			env.Bind(derivedType + "_" + unit.GetName().ToUpper() + "_" + suffix.ToUpper(), derivedUnit);
		}

		public static void RegisterUnit(Env env, Type clazz)
		{
			FieldInfo[] fields = Sharpen.Runtime.GetDeclaredFields(clazz);
			foreach (FieldInfo field in fields)
			{
				bool isStatic = Modifier.IsStatic(field.GetModifiers());
				bool isUnit = typeof(Unit).IsAssignableFrom(field.GetType());
				if (isStatic && isUnit)
				{
					try
					{
						Unit unit = ErasureUtils.UncheckedCast(field.GetValue(null));
						RegisterUnit(env, unit);
					}
					catch (MemberAccessException)
					{
					}
				}
			}
		}

		public static void RegisterUnit(Env env, Unit unit)
		{
			env.Bind((unit.GetType() + "_" + unit.GetName()).ToUpper(), unit);
		}

		public class MoneyUnit : Unit
		{
			public const string Type = "MONEY";

			public MoneyUnit(string name, string symbol)
				: base(name, symbol, Type)
			{
			}

			public MoneyUnit(string name, string symbol, Unit defaultUnit, double defaultUnitScale)
				: base(name, symbol, Type, defaultUnit, defaultUnitScale)
			{
			}

			public override string Format(double amount)
			{
				// Format to 2 decimal places
				return symbol + string.Format("%.2f", amount);
			}
		}

		public class Currencies
		{
			public static readonly Unit Dollar = new Units.MoneyUnit("dollar", "$");

			public static readonly Unit Cent = new Units.MoneyUnit("cent", "¢", Dollar, 0.01);

			public static readonly Unit Pound = new Units.MoneyUnit("pound", "\u00A3");

			public static readonly Unit Penny = new Units.MoneyUnit("penny", "¢", Dollar, 0.01);

			public static readonly Unit Euro = new Units.MoneyUnit("euro", "\u00AC");

			public static readonly Unit Yen = new Units.MoneyUnit("yen", "\u00A5");

			public static readonly Unit Yuan = new Units.MoneyUnit("yuan", "\u5143");

			public static readonly Unit Won = new Units.MoneyUnit("won", "\u20A9");

			private Currencies()
			{
			}
			// constant holder class
		}

		/// <exception cref="System.IO.IOException"/>
		public static void RegisterUnits(Env env, string filename)
		{
			IList<Unit> units = LoadUnits(filename);
			RegisterUnits(env, units);
			RegisterUnit(env, typeof(Units.Currencies));
		}

		public static void RegisterUnits(Env env, IList<Unit> units)
		{
			foreach (Unit unit in units)
			{
				RegisterUnit(env, unit);
				if ("LENGTH".Equals(unit.GetType()))
				{
					RegisterDerivedUnit(env, unit, "AREA", "2", "2");
					RegisterDerivedUnit(env, unit, "VOLUME", "3", "3");
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<Unit> LoadUnits(string filename)
		{
			Pattern commaPattern = Pattern.Compile("\\s*,\\s*");
			BufferedReader br = IOUtils.GetBufferedFileReader(filename);
			string headerString = br.ReadLine();
			string[] header = commaPattern.Split(headerString);
			IDictionary<string, int> headerIndex = new Dictionary<string, int>();
			for (int i = 0; i < header.Length; i++)
			{
				headerIndex[header[i]] = i;
			}
			int iName = headerIndex["unit"];
			int iPrefix = headerIndex["prefix"];
			int iSymbol = headerIndex["symbol"];
			int iType = headerIndex["type"];
			int iSystem = headerIndex["system"];
			int iDefaultUnit = headerIndex["defaultUnit"];
			int iDefaultUnitScale = headerIndex["defaultUnitScale"];
			string line;
			IList<Unit> list = new List<Unit>();
			IDictionary<string, Unit> unitsByName = new Dictionary<string, Unit>();
			IDictionary<string, Pair<string, double>> unitToDefaultUnits = new Dictionary<string, Pair<string, double>>();
			while ((line = br.ReadLine()) != null)
			{
				string[] fields = commaPattern.Split(line);
				Unit unit = new Unit(fields[iName], fields[iSymbol], fields[iType].ToUpper());
				unit.system = fields[iSystem];
				if (fields.Length > iPrefix)
				{
					unit.prefixSystem = fields[iPrefix];
				}
				if (fields.Length > iDefaultUnit)
				{
					double scale = 1.0;
					if (fields.Length > iDefaultUnitScale)
					{
						scale = double.ParseDouble(fields[iDefaultUnitScale]);
					}
					unitToDefaultUnits[unit.GetName()] = Pair.MakePair(fields[iDefaultUnit], scale);
				}
				unitsByName[unit.GetName()] = unit;
				list.Add(unit);
			}
			foreach (KeyValuePair<string, Pair<string, double>> entry in unitToDefaultUnits)
			{
				Unit unit = unitsByName[entry.Key];
				Unit defaultUnit = unitsByName[entry.Value.first];
				if (defaultUnit != null)
				{
					unit.defaultUnit = defaultUnit;
					unit.defaultUnitScale = entry.Value.second;
				}
				else
				{
					Redwood.Util.Warn("Unknown default unit " + entry.Value.first + " for " + entry.Key);
				}
			}
			br.Close();
			return list;
		}
	}
}
