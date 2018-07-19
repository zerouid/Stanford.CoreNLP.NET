using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>A class that takes care of the stuff necessary for variable strings.</summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class VariableStrings
	{
		private readonly IDictionary<string, string> varsToStrings;

		private readonly IntCounter<string> numVarsSet;

		public VariableStrings()
		{
			varsToStrings = ArrayMap.NewArrayMap();
			numVarsSet = new IntCounter<string>(MapFactory.ArrayMapFactory<string, MutableInteger>());
		}

		public virtual void Reset()
		{
			numVarsSet.Clear();
			varsToStrings.Clear();
		}

		public virtual bool IsSet(string o)
		{
			return numVarsSet.GetCount(o) >= 1;
		}

		public virtual void SetVar(string var, string @string)
		{
			string oldString = varsToStrings[var] = @string;
			if (oldString != null && !oldString.Equals(@string))
			{
				throw new Exception("Error -- can't setVar to a different string -- old: " + oldString + " new: " + @string);
			}
			numVarsSet.IncrementCount(var);
		}

		public virtual void UnsetVar(string var)
		{
			if (numVarsSet.GetCount(var) > 0)
			{
				numVarsSet.DecrementCount(var);
			}
			if (numVarsSet.GetCount(var) == 0)
			{
				varsToStrings[var] = null;
			}
		}

		public virtual string GetString(string var)
		{
			return varsToStrings[var];
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append("{");
			bool appended = false;
			foreach (string key in varsToStrings.Keys)
			{
				if (appended)
				{
					s.Append(",");
				}
				else
				{
					appended = true;
				}
				s.Append(key);
				s.Append("=(");
				s.Append(varsToStrings[key]);
				s.Append(":");
				s.Append(numVarsSet.GetCount(key));
				s.Append(")");
			}
			s.Append("}");
			return s.ToString();
		}
	}
}
