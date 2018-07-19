using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Maintains efficient indexing of unary grammar rules.</summary>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class UnaryGrammar : IEnumerable<UnaryRule>
	{
		private readonly IIndex<string> index;

		[System.NonSerialized]
		private IList<UnaryRule>[] rulesWithParent;

		[System.NonSerialized]
		private IList<UnaryRule>[] rulesWithChild;

		[System.NonSerialized]
		private IList<UnaryRule>[] closedRulesWithParent;

		[System.NonSerialized]
		private IList<UnaryRule>[] closedRulesWithChild;

		[System.NonSerialized]
		private UnaryRule[][] closedRulesWithP;

		[System.NonSerialized]
		private UnaryRule[][] closedRulesWithC;

		/// <summary>The basic list of UnaryRules.</summary>
		/// <remarks>The basic list of UnaryRules.  Really this is treated as a set</remarks>
		private IDictionary<UnaryRule, UnaryRule> coreRules;

		/// <summary>The closure of the basic list of UnaryRules.</summary>
		/// <remarks>The closure of the basic list of UnaryRules.  Treated as a set</remarks>
		[System.NonSerialized]
		private IDictionary<UnaryRule, UnaryRule> bestRulesUnderMax;

		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// private transient Map<UnaryRule,Integer> backTrace = null;
		public virtual int NumClosedRules()
		{
			return bestRulesUnderMax.Keys.Count;
		}

		public virtual UnaryRule GetRule(UnaryRule ur)
		{
			return coreRules[ur];
		}

		public virtual IEnumerator<UnaryRule> ClosedRuleIterator()
		{
			return bestRulesUnderMax.Keys.GetEnumerator();
		}

		public virtual int NumRules()
		{
			return coreRules.Keys.Count;
		}

		public virtual IEnumerator<UnaryRule> GetEnumerator()
		{
			return RuleIterator();
		}

		public virtual IEnumerator<UnaryRule> RuleIterator()
		{
			return coreRules.Keys.GetEnumerator();
		}

		public virtual IList<UnaryRule> Rules()
		{
			return new List<UnaryRule>(coreRules.Keys);
		}

		/// <summary>Remove A -&gt; A UnaryRules from bestRulesUnderMax.</summary>
		public void PurgeRules()
		{
			IDictionary<UnaryRule, UnaryRule> bR = Generics.NewHashMap();
			foreach (UnaryRule ur in bestRulesUnderMax.Keys)
			{
				if (ur.parent != ur.child)
				{
					bR[ur] = ur;
				}
				else
				{
					closedRulesWithParent[ur.parent].Remove(ur);
					closedRulesWithChild[ur.child].Remove(ur);
				}
			}
			bestRulesUnderMax = bR;
			MakeCRArrays();
		}

		/* -----------------
		// Not needed any more as we reconstruct unaries in extractBestParse
		public List<Integer> getBestPath(int parent, int child) {
		List<Integer> path = new ArrayList<Integer>();
		UnaryRule tempR = new UnaryRule();
		tempR.parent = parent;
		tempR.child = child;
		//System.out.println("Building path...");
		int loc = parent;
		while (loc != child) {
		path.add(new Integer(loc));
		//System.out.println("Path is "+path);
		tempR.parent = loc;
		Integer nextInt = backTrace.get(tempR);
		if (nextInt == null) {
		loc = child;
		} else {
		loc = nextInt.intValue();
		}
		//System.out.println(Numberer.getGlobalNumberer(stateSpace).object(parent)+"->"+Numberer.getGlobalNumberer(stateSpace).object(child)+" went via "+Numberer.getGlobalNumberer(stateSpace).object(loc));
		if (path.size() > 10) {
		throw new RuntimeException("UnaryGrammar path > 10");
		}
		}
		path.add(new Integer(child));
		return path;
		}
		--------------------------- */
		private void CloseRulesUnderMax(UnaryRule ur)
		{
			for (int i = 0; i < isz; i++)
			{
				UnaryRule pr = closedRulesWithChild[ur.parent][i];
				for (int j = 0; j < jsz; j++)
				{
					UnaryRule cr = closedRulesWithParent[ur.child][j];
					UnaryRule resultR = new UnaryRule(pr.parent, cr.child, pr.score + cr.score + ur.score);
					RelaxRule(resultR);
				}
			}
		}

		/* ----- No longer need to maintain unary rule backpointers
		if (relaxRule(resultR)) {
		if (resultR.parent != ur.parent) {
		backTrace.put(resultR, new Integer(ur.parent));
		} else {
		backTrace.put(resultR, new Integer(ur.child));
		}
		}
		-------- */
		/// <summary>
		/// Possibly update the best way to make this UnaryRule in the
		/// bestRulesUnderMax hash and closedRulesWithX lists.
		/// </summary>
		/// <param name="ur">A UnaryRule with a score</param>
		/// <returns>true if ur is the new best scoring case of that unary rule.</returns>
		private bool RelaxRule(UnaryRule ur)
		{
			UnaryRule bestR = bestRulesUnderMax[ur];
			if (bestR == null)
			{
				bestRulesUnderMax[ur] = ur;
				closedRulesWithParent[ur.parent].Add(ur);
				closedRulesWithChild[ur.child].Add(ur);
				return true;
			}
			else
			{
				if (bestR.score < ur.score)
				{
					bestR.score = ur.score;
					return true;
				}
				return false;
			}
		}

		public virtual double ScoreRule(UnaryRule ur)
		{
			UnaryRule bestR = bestRulesUnderMax[ur];
			return (bestR != null ? bestR.score : double.NegativeInfinity);
		}

		public void AddRule(UnaryRule ur)
		{
			// add rules' closure
			CloseRulesUnderMax(ur);
			coreRules[ur] = ur;
			rulesWithParent[ur.parent].Add(ur);
			rulesWithChild[ur.child].Add(ur);
		}

		private static readonly UnaryRule[] EmptyUnaryRuleArray = new UnaryRule[0];

		//public Iterator closedRuleIterator() {
		//  return bestRulesUnderMax.keySet().iterator();
		//}
		internal virtual void MakeCRArrays()
		{
			int numStates = index.Size();
			closedRulesWithP = new UnaryRule[numStates][];
			closedRulesWithC = new UnaryRule[numStates][];
			for (int i = 0; i < numStates; i++)
			{
				// cdm [2012]: Would it be faster to use same EMPTY_UNARY_RULE_ARRAY when of size zero?  It must be!
				closedRulesWithP[i] = Sharpen.Collections.ToArray(closedRulesWithParent[i], new UnaryRule[closedRulesWithParent[i].Count]);
				closedRulesWithC[i] = Sharpen.Collections.ToArray(closedRulesWithChild[i], new UnaryRule[closedRulesWithChild[i].Count]);
			}
		}

		public virtual UnaryRule[] ClosedRulesByParent(int state)
		{
			if (state >= closedRulesWithP.Length)
			{
				// cdm [2012]: This check shouldn't be needed; delete
				return EmptyUnaryRuleArray;
			}
			return closedRulesWithP[state];
		}

		public virtual UnaryRule[] ClosedRulesByChild(int state)
		{
			if (state >= closedRulesWithC.Length)
			{
				// cdm [2012]: This check shouldn't be needed; delete
				return EmptyUnaryRuleArray;
			}
			return closedRulesWithC[state];
		}

		public virtual IEnumerator<UnaryRule> ClosedRuleIteratorByParent(int state)
		{
			if (state >= closedRulesWithParent.Length)
			{
				IList<UnaryRule> lur = Java.Util.Collections.EmptyList();
				return lur.GetEnumerator();
			}
			return closedRulesWithParent[state].GetEnumerator();
		}

		public virtual IEnumerator<UnaryRule> ClosedRuleIteratorByChild(int state)
		{
			if (state >= closedRulesWithChild.Length)
			{
				IList<UnaryRule> lur = Java.Util.Collections.EmptyList();
				return lur.GetEnumerator();
			}
			return closedRulesWithChild[state].GetEnumerator();
		}

		public virtual IEnumerator<UnaryRule> RuleIteratorByParent(int state)
		{
			if (state >= rulesWithParent.Length)
			{
				IList<UnaryRule> lur = Java.Util.Collections.EmptyList();
				return lur.GetEnumerator();
			}
			return rulesWithParent[state].GetEnumerator();
		}

		public virtual IEnumerator<UnaryRule> RuleIteratorByChild(int state)
		{
			if (state >= rulesWithChild.Length)
			{
				IList<UnaryRule> lur = Java.Util.Collections.EmptyList();
				return lur.GetEnumerator();
			}
			return rulesWithChild[state].GetEnumerator();
		}

		public virtual IList<UnaryRule> RulesByParent(int state)
		{
			if (state >= rulesWithParent.Length)
			{
				return Java.Util.Collections.EmptyList();
			}
			return rulesWithParent[state];
		}

		public virtual IList<UnaryRule> RulesByChild(int state)
		{
			if (state >= rulesWithChild.Length)
			{
				return Java.Util.Collections.EmptyList();
			}
			return rulesWithChild[state];
		}

		public virtual IList<UnaryRule>[] RulesWithParent()
		{
			return rulesWithParent;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			ICollection<UnaryRule> allRules = Generics.NewHashSet(coreRules.Keys);
			Init();
			foreach (UnaryRule ur in allRules)
			{
				AddRule(ur);
			}
			PurgeRules();
		}

		/// <summary>
		/// Create all the array variables, and put in A -&gt; A UnaryRules to feed
		/// the closure algorithm.
		/// </summary>
		/// <remarks>
		/// Create all the array variables, and put in A -&gt; A UnaryRules to feed
		/// the closure algorithm. They then get deleted later.
		/// </remarks>
		private void Init()
		{
			int numStates = index.Size();
			coreRules = Generics.NewHashMap();
			rulesWithParent = new IList[numStates];
			rulesWithChild = new IList[numStates];
			closedRulesWithParent = new IList[numStates];
			closedRulesWithChild = new IList[numStates];
			bestRulesUnderMax = Generics.NewHashMap();
			// backTrace = Generics.newHashMap();
			for (int s = 0; s < numStates; s++)
			{
				rulesWithParent[s] = new List<UnaryRule>();
				rulesWithChild[s] = new List<UnaryRule>();
				closedRulesWithParent[s] = new List<UnaryRule>();
				closedRulesWithChild[s] = new List<UnaryRule>();
				UnaryRule selfR = new UnaryRule(s, s, 0.0);
				RelaxRule(selfR);
			}
		}

		public UnaryGrammar(IIndex<string> stateIndex)
		{
			this.index = stateIndex;
			Init();
		}

		/// <summary>Populates data in this UnaryGrammar from a character stream.</summary>
		/// <param name="in">The Reader the grammar is read from.</param>
		/// <exception cref="System.IO.IOException">If there is a reading problem</exception>
		public virtual void ReadData(BufferedReader @in)
		{
			string line;
			int lineNum = 1;
			// all lines have one rule per line
			line = @in.ReadLine();
			while (line != null && line.Length > 0)
			{
				try
				{
					AddRule(new UnaryRule(line, index));
				}
				catch (Exception)
				{
					throw new IOException("Error on line " + lineNum);
				}
				lineNum++;
				line = @in.ReadLine();
			}
			PurgeRules();
		}

		/// <summary>Writes out data from this Object.</summary>
		/// <param name="w">Data is written to this Writer</param>
		public virtual void WriteData(TextWriter w)
		{
			PrintWriter @out = new PrintWriter(w);
			// all lines have one rule per line
			foreach (UnaryRule ur in this)
			{
				@out.Println(ur.ToString(index));
			}
			@out.Flush();
		}

		/// <summary>Writes out a lot of redundant data from this Object to the Writer w.</summary>
		/// <param name="w">Data is written to this Writer</param>
		public virtual void WriteAllData(TextWriter w)
		{
			int numStates = index.Size();
			PrintWriter @out = new PrintWriter(w);
			// all lines have one rule per line
			@out.Println("Unary ruleIterator");
			for (IEnumerator<UnaryRule> rI = RuleIterator(); rI.MoveNext(); )
			{
				@out.Println(rI.Current.ToString(index));
			}
			@out.Println("Unary closedRuleIterator");
			for (IEnumerator<UnaryRule> rI_1 = ClosedRuleIterator(); rI_1.MoveNext(); )
			{
				@out.Println(rI_1.Current.ToString(index));
			}
			@out.Println("Unary rulesWithParentIterator");
			for (int i = 0; i < numStates; i++)
			{
				@out.Println(index.Get(i));
				for (IEnumerator<UnaryRule> rI_2 = RuleIteratorByParent(i); rI_2.MoveNext(); )
				{
					@out.Print("  ");
					@out.Println(rI_2.Current.ToString(index));
				}
			}
			@out.Println("Unary closedRulesWithParentIterator");
			for (int i_1 = 0; i_1 < numStates; i_1++)
			{
				@out.Println(index.Get(i_1));
				for (IEnumerator<UnaryRule> rI_2 = ClosedRuleIteratorByParent(i_1); rI_2.MoveNext(); )
				{
					@out.Print("  ");
					@out.Println(rI_2.Current.ToString(index));
				}
			}
			@out.Flush();
		}

		public override string ToString()
		{
			TextWriter w = new StringWriter();
			WriteData(w);
			return w.ToString();
		}

		private const long serialVersionUID = 1L;
	}
}
