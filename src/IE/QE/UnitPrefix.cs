using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling.Tokensregex;





namespace Edu.Stanford.Nlp.IE.QE
{
	/// <summary>Potential prefix that goes in front of a quantifiable unit</summary>
	/// <author>Angel Chang</author>
	public class UnitPrefix
	{
		public string name;

		public string symbol;

		public double scale;

		public string system;

		public UnitPrefix(string name, string symbol, double scale, string system)
		{
			// What does this prefix do to the unit?
			this.name = name;
			this.symbol = symbol;
			this.scale = scale;
			this.system = system;
		}

		private Unit Convert(Unit u)
		{
			return new Unit(name + u.GetName(), symbol + u.GetSymbol(), u.GetType(), u, scale);
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

		public virtual double GetScale()
		{
			return scale;
		}

		public virtual void SetScale(double scale)
		{
			this.scale = scale;
		}

		public virtual void SetScale(Number scale)
		{
			this.scale = scale;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void RegisterPrefixes(Env env, string filename)
		{
			IList<Edu.Stanford.Nlp.IE.QE.UnitPrefix> prefixes = LoadPrefixes(filename);
			RegisterPrefixes(env, prefixes);
		}

		public static void RegisterPrefixes(Env env, IList<Edu.Stanford.Nlp.IE.QE.UnitPrefix> prefixes)
		{
			foreach (Edu.Stanford.Nlp.IE.QE.UnitPrefix prefix in prefixes)
			{
				RegisterPrefix(env, prefix);
			}
		}

		public static void RegisterPrefix(Env env, Edu.Stanford.Nlp.IE.QE.UnitPrefix prefix)
		{
			env.Bind(prefix.GetName().ToUpper(), prefix);
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<Edu.Stanford.Nlp.IE.QE.UnitPrefix> LoadPrefixes(string filename)
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
			int iName = headerIndex["name"];
			int iPrefix = headerIndex["prefix"];
			int iBase = headerIndex["base"];
			int iExp = headerIndex["exp"];
			int iSystem = headerIndex["system"];
			string line;
			IList<Edu.Stanford.Nlp.IE.QE.UnitPrefix> list = new List<Edu.Stanford.Nlp.IE.QE.UnitPrefix>();
			while ((line = br.ReadLine()) != null)
			{
				string[] fields = commaPattern.Split(line);
				double @base = double.ParseDouble(fields[iBase]);
				double exp = double.ParseDouble(fields[iExp]);
				double scale = Math.Pow(@base, exp);
				Edu.Stanford.Nlp.IE.QE.UnitPrefix unitPrefix = new Edu.Stanford.Nlp.IE.QE.UnitPrefix(fields[iName], fields[iPrefix], scale, fields[iSystem]);
				list.Add(unitPrefix);
			}
			br.Close();
			return list;
		}
	}
}
