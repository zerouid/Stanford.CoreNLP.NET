using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Basic results for a Sequence Match</summary>
	/// <author>Angel Chang</author>
	public class BasicSequenceMatchResult<T> : ISequenceMatchResult<T>
	{
		internal SequencePattern<T> pattern;

		internal IList<T> elements;

		internal BasicSequenceMatchResult.MatchedGroup[] matchedGroups;

		internal object[] matchedResults;

		internal IFunction<IList<T>, string> nodesToStringConverter;

		internal SequencePattern.VarGroupBindings varGroupBindings;

		internal double score = 0.0;

		internal double priority = 0.0;

		internal int order;

		// Pattern we matched against
		// Original sequence
		// Groups that we matched
		// Additional information about matches (per element)
		public virtual IList<T> Elements()
		{
			return elements;
		}

		public virtual SequencePattern<T> Pattern()
		{
			return pattern;
		}

		//  public static <T> BasicSequenceMatchResult<T> toBasicSequenceMatchResult(List<? extends T> elements) {
		//    BasicSequenceMatchResult<T> matchResult = new BasicSequenceMatchResult<T>();
		//    matchResult.elements = elements;
		//    matchResult.matchedGroups = new MatchedGroup[0];
		//    return matchResult;
		//  }
		public virtual BasicSequenceMatchResult<T> ToBasicSequenceMatchResult()
		{
			return Copy();
		}

		public virtual BasicSequenceMatchResult<T> Copy()
		{
			BasicSequenceMatchResult<T> res = new BasicSequenceMatchResult<T>();
			res.pattern = pattern;
			res.elements = elements;
			res.matchedGroups = new BasicSequenceMatchResult.MatchedGroup[matchedGroups.Length];
			res.nodesToStringConverter = nodesToStringConverter;
			res.score = score;
			res.priority = priority;
			res.order = order;
			res.varGroupBindings = varGroupBindings;
			for (int i = 0; i < matchedGroups.Length; i++)
			{
				if (matchedGroups[i] != null)
				{
					res.matchedGroups[i] = new BasicSequenceMatchResult.MatchedGroup(matchedGroups[i]);
				}
			}
			if (matchedResults != null)
			{
				res.matchedResults = new object[matchedResults.Length];
				System.Array.Copy(res.matchedResults, 0, matchedResults, 0, matchedResults.Length);
			}
			return res;
		}

		public virtual Interval<int> GetInterval()
		{
			return SequenceMatchResult<T>Constants.ToInterval.Apply(this);
		}

		public virtual int GetOrder()
		{
			return order;
		}

		public virtual void SetOrder(int order)
		{
			this.order = order;
		}

		public virtual double Priority()
		{
			return priority;
		}

		public virtual double Score()
		{
			return score;
		}

		public virtual int Start()
		{
			return Start(0);
		}

		public virtual int Start(int group)
		{
			if (group == SequenceMatchResult<T>Constants.GroupBeforeMatch)
			{
				return 0;
			}
			else
			{
				if (group == SequenceMatchResult<T>Constants.GroupAfterMatch)
				{
					return matchedGroups[0].matchEnd;
				}
			}
			if (matchedGroups[group] != null)
			{
				return matchedGroups[group].matchBegin;
			}
			else
			{
				return -1;
			}
		}

		public virtual int Start(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return Start(g);
			}
			else
			{
				return -1;
			}
		}

		public virtual int End()
		{
			return End(0);
		}

		public virtual int End(int group)
		{
			if (group == SequenceMatchResult<T>Constants.GroupBeforeMatch)
			{
				return matchedGroups[0].matchBegin;
			}
			else
			{
				if (group == SequenceMatchResult<T>Constants.GroupAfterMatch)
				{
					return elements.Count;
				}
			}
			if (matchedGroups[group] != null)
			{
				return matchedGroups[group].matchEnd;
			}
			else
			{
				return -1;
			}
		}

		public virtual int End(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return End(g);
			}
			else
			{
				return -1;
			}
		}

		public virtual string Group()
		{
			return Group(0);
		}

		public virtual string Group(int group)
		{
			IList<T> groupTokens = GroupNodes(group);
			if (nodesToStringConverter == null)
			{
				return (groupTokens != null) ? StringUtils.Join(groupTokens, " ") : null;
			}
			else
			{
				return nodesToStringConverter.Apply(groupTokens);
			}
		}

		public virtual string Group(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return Group(g);
			}
			else
			{
				return null;
			}
		}

		public virtual IList<T> GroupNodes()
		{
			return GroupNodes(0);
		}

		public virtual IList<T> GroupNodes(int group)
		{
			if (group == SequenceMatchResult<T>Constants.GroupBeforeMatch || group == SequenceMatchResult<T>Constants.GroupAfterMatch)
			{
				// return a new list so the resulting object is serializable
				return new List<T>(elements.SubList(Start(group), End(group)));
			}
			if (matchedGroups.Length > group && matchedGroups[group] != null)
			{
				// return a new list so the resulting object is serializable
				return new List<T>(elements.SubList(matchedGroups[group].matchBegin, matchedGroups[group].matchEnd));
			}
			else
			{
				return null;
			}
		}

		public virtual IList<T> GroupNodes(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return GroupNodes(g);
			}
			else
			{
				return null;
			}
		}

		public virtual object GroupValue()
		{
			return GroupValue(0);
		}

		public virtual object GroupValue(int group)
		{
			if (group == SequenceMatchResult<T>Constants.GroupBeforeMatch || group == SequenceMatchResult<T>Constants.GroupAfterMatch)
			{
				// return a new list so the resulting object is serializable
				return new List<_T1807317499>(elements.SubList(Start(group), End(group)));
			}
			if (matchedGroups[group] != null)
			{
				return matchedGroups[group].value;
			}
			else
			{
				return null;
			}
		}

		public virtual object GroupValue(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return GroupValue(g);
			}
			else
			{
				return null;
			}
		}

		public virtual SequenceMatchResult.MatchedGroupInfo<T> GroupInfo()
		{
			return GroupInfo(0);
		}

		public virtual SequenceMatchResult.MatchedGroupInfo<T> GroupInfo(int group)
		{
			IList<T> nodes = GroupNodes(group);
			if (nodes != null)
			{
				object value = GroupValue(group);
				string text = Group(group);
				IList<object> matchedResults = GroupMatchResults(group);
				string varName = group >= this.varGroupBindings.varnames.Length ? null : this.varGroupBindings.varnames[group];
				return new SequenceMatchResult.MatchedGroupInfo<T>(text, nodes, matchedResults, value, varName);
			}
			else
			{
				return null;
			}
		}

		public virtual SequenceMatchResult.MatchedGroupInfo<T> GroupInfo(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return GroupInfo(g);
			}
			else
			{
				return null;
			}
		}

		public virtual int GroupCount()
		{
			return matchedGroups.Length - 1;
		}

		public virtual IList<object> GroupMatchResults()
		{
			return GroupMatchResults(0);
		}

		public virtual IList<object> GroupMatchResults(int group)
		{
			if (matchedResults == null)
			{
				return null;
			}
			if (group == SequenceMatchResult<T>Constants.GroupBeforeMatch || group == SequenceMatchResult<T>Constants.GroupAfterMatch)
			{
				return Arrays.AsList(Arrays.CopyOfRange(matchedResults, Start(group), End(group)));
			}
			if (matchedGroups[group] != null)
			{
				return Arrays.AsList(Arrays.CopyOfRange(matchedResults, matchedGroups[group].matchBegin, matchedGroups[group].matchEnd));
			}
			else
			{
				return null;
			}
		}

		public virtual IList<object> GroupMatchResults(string var)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return GroupMatchResults(g);
			}
			else
			{
				return null;
			}
		}

		public virtual object NodeMatchResult(int index)
		{
			if (matchedResults != null)
			{
				return matchedResults[index];
			}
			else
			{
				return null;
			}
		}

		public virtual object GroupMatchResult(int group, int index)
		{
			if (matchedResults != null)
			{
				int s = Start(group);
				int e = End(group);
				if (s >= 0 && e > s)
				{
					int d = e - s;
					if (index >= 0 && index < d)
					{
						return matchedResults[s + index];
					}
				}
			}
			return null;
		}

		public virtual object GroupMatchResult(string var, int index)
		{
			int g = GetFirstVarGroup(var);
			if (g >= 0)
			{
				return GroupMatchResult(g, index);
			}
			else
			{
				return null;
			}
		}

		private int GetFirstVarGroup(string v)
		{
			// Trim the variable...
			v = v.Trim();
			for (int i = 0; i < varGroupBindings.varnames.Length; i++)
			{
				string s = varGroupBindings.varnames[i];
				if (v.Equals(s))
				{
					if (matchedGroups[i] != null)
					{
						return i;
					}
				}
			}
			return -1;
		}

		protected internal class MatchedGroup
		{
			internal int matchBegin = -1;

			internal int matchEnd = -1;

			internal object value = null;

			protected internal MatchedGroup(BasicSequenceMatchResult.MatchedGroup mg)
			{
				this.matchBegin = mg.matchBegin;
				this.matchEnd = mg.matchEnd;
				this.value = mg.value;
			}

			protected internal MatchedGroup(int matchBegin, int matchEnd, object value)
			{
				this.matchBegin = matchBegin;
				this.matchEnd = matchEnd;
				this.value = value;
			}

			public override string ToString()
			{
				return "(" + matchBegin + ',' + matchEnd + ')';
			}

			public virtual int MatchLength()
			{
				if (matchBegin >= 0 && matchEnd >= 0)
				{
					return matchEnd - matchBegin;
				}
				else
				{
					return -1;
				}
			}
		}
	}
}
