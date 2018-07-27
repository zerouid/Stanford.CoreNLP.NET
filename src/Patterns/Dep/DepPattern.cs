using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Patterns.Dep
{
	/// <summary>Created by sonalg on 10/31/14.</summary>
	[System.Serializable]
	public class DepPattern : Pattern
	{
		internal int hashCode;

		internal IList<Pair<Token, GrammaticalRelation>> relations;

		public DepPattern(IList<Pair<Token, GrammaticalRelation>> relations)
			: base(PatternFactory.PatternType.Dep)
		{
			this.relations = relations;
			hashCode = this.ToString().GetHashCode();
		}

		public DepPattern(Token token, GrammaticalRelation relation)
			: base(PatternFactory.PatternType.Dep)
		{
			this.relations = new List<Pair<Token, GrammaticalRelation>>();
			relations.Add(new Pair<Token, GrammaticalRelation>(token, relation));
			hashCode = this.ToString().GetHashCode();
		}

		public override CollectionValuedMap<string, string> GetRelevantWords()
		{
			CollectionValuedMap<string, string> relwordsThisPat = new CollectionValuedMap<string, string>();
			foreach (Pair<Token, GrammaticalRelation> r in relations)
			{
				GetRelevantWordsBase(r.First(), relwordsThisPat);
			}
			return relwordsThisPat;
		}

		public override int EqualContext(Pattern p)
		{
			return -1;
		}

		public override string ToStringSimple()
		{
			return ToString();
		}

		public override string ToString(IList<string> notAllowedClasses)
		{
			//TODO: implement this
			return ToString();
		}

		public override string ToString()
		{
			if (relations.Count > 1)
			{
				throw new NotSupportedException();
			}
			Pair<Token, GrammaticalRelation> rel = relations[0];
			//String pattern = "({" + wordType + ":/" + parent + "/}=parent >>" + rel + "=reln {}=node)";
			string p = "(" + rel.First().ToString() + "=parent >" + rel.Second().ToString() + "=reln {}=node)";
			return p;
		}

		public override int GetHashCode()
		{
			return hashCode;
		}

		public override bool Equals(object p)
		{
			if (!(p is Edu.Stanford.Nlp.Patterns.Dep.DepPattern))
			{
				return false;
			}
			return this.ToString().Equals(((Edu.Stanford.Nlp.Patterns.Dep.DepPattern)p).ToString());
		}

		//TODO: implement compareTo
		//TODO: implement these
		public static bool SameGenre(Edu.Stanford.Nlp.Patterns.Dep.DepPattern p1, Edu.Stanford.Nlp.Patterns.Dep.DepPattern p2)
		{
			return true;
		}

		public static bool Subsumes(Edu.Stanford.Nlp.Patterns.Dep.DepPattern pat, Edu.Stanford.Nlp.Patterns.Dep.DepPattern p)
		{
			return false;
		}
	}
}
