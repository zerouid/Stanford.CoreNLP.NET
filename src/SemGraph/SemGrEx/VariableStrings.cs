using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>a class that takes care of the stuff necessary for variable strings.</summary>
	/// <remarks>
	/// a class that takes care of the stuff necessary for variable strings.
	/// // todo: if this is just a copy of the tregex one, use same for both?
	/// </remarks>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class VariableStrings
	{
		private IDictionary<object, string> varsToStrings;

		private IntCounter<object> numVarsSet;

		public VariableStrings()
		{
			varsToStrings = Generics.NewHashMap();
			numVarsSet = new IntCounter<object>();
		}

		public virtual bool IsSet(object o)
		{
			return numVarsSet.GetCount(o) == 1;
		}

		public virtual void SetVar(object var, string @string)
		{
			string oldString = varsToStrings[var] = @string;
			if (oldString != null && !oldString.Equals(@string))
			{
				throw new Exception("Error -- can't setVar to a different string -- old: " + oldString + " new: " + @string);
			}
			numVarsSet.IncrementCount(var);
		}

		public virtual void UnsetVar(object var)
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

		public virtual string GetString(object var)
		{
			return varsToStrings[var];
		}
	}
}
