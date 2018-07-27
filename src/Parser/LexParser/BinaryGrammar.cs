using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Maintains efficient indexing of binary grammar rules.</summary>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning (generified and optimized storage)</author>
	[System.Serializable]
	public class BinaryGrammar : IEnumerable<BinaryRule>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.BinaryGrammar));

		private readonly IIndex<string> index;

		private readonly IList<BinaryRule> allRules;

		[System.NonSerialized]
		private IList<BinaryRule>[] rulesWithParent;

		[System.NonSerialized]
		private IList<BinaryRule>[] rulesWithLC;

		[System.NonSerialized]
		private IList<BinaryRule>[] rulesWithRC;

		[System.NonSerialized]
		private ICollection<BinaryRule>[] ruleSetWithLC;

		[System.NonSerialized]
		private ICollection<BinaryRule>[] ruleSetWithRC;

		[System.NonSerialized]
		private BinaryRule[][] splitRulesWithLC;

		[System.NonSerialized]
		private BinaryRule[][] splitRulesWithRC;

		[System.NonSerialized]
		private IDictionary<BinaryRule, BinaryRule> ruleMap;

		[System.NonSerialized]
		private bool[] synthetic;

		// private static final BinaryRule[] EMPTY_BINARY_RULE_ARRAY = new BinaryRule[0];
		//  private transient BinaryRule[][] splitRulesWithParent = null;
		// for super speed! (maybe)
		public virtual int NumRules()
		{
			return allRules.Count;
		}

		public virtual IList<BinaryRule> Rules()
		{
			return allRules;
		}

		public virtual bool IsSynthetic(int state)
		{
			return synthetic[state];
		}

		/// <summary>Populates the "splitRules" accessor lists using the existing rule lists.</summary>
		/// <remarks>
		/// Populates the "splitRules" accessor lists using the existing rule lists.
		/// If the state is synthetic, these lists contain all rules for the state.
		/// If the state is NOT synthetic, these lists contain only the rules in
		/// which both children are not synthetic.
		/// <p>
		/// <i>This method must be called before the grammar is
		/// used, either after training or deserializing grammar.</i>
		/// </remarks>
		public virtual void SplitRules()
		{
			// first initialize the synthetic array
			int numStates = index.Size();
			synthetic = new bool[numStates];
			for (int s = 0; s < numStates; s++)
			{
				try
				{
					//System.out.println(((String)index.get(s))); // debugging
					synthetic[s] = (index.Get(s)[0] == '@');
				}
				catch (ArgumentNullException)
				{
					synthetic[s] = true;
				}
			}
			splitRulesWithLC = new BinaryRule[numStates][];
			splitRulesWithRC = new BinaryRule[numStates][];
			//    splitRulesWithParent = new BinaryRule[numStates][];
			// rules accessed by their "synthetic" child or left child if none
			for (int state = 0; state < numStates; state++)
			{
				//      System.out.println("Splitting rules for state: " + index.get(state));
				// check synthetic
				if (IsSynthetic(state))
				{
					splitRulesWithLC[state] = Sharpen.Collections.ToArray(rulesWithLC[state], new BinaryRule[rulesWithLC[state].Count]);
					// cdm 2012: I thought sorting the rules might help with speed (memory locality) but didn't seem to
					// Arrays.sort(splitRulesWithLC[state]);
					splitRulesWithRC[state] = Sharpen.Collections.ToArray(rulesWithRC[state], new BinaryRule[rulesWithRC[state].Count]);
				}
				else
				{
					// Arrays.sort(splitRulesWithRC[state]);
					// if state is not synthetic, we add rule to splitRules only if both children are not synthetic
					// do left
					IList<BinaryRule> ruleList = new List<BinaryRule>();
					foreach (BinaryRule br in rulesWithLC[state])
					{
						if (!IsSynthetic(br.rightChild))
						{
							ruleList.Add(br);
						}
					}
					splitRulesWithLC[state] = Sharpen.Collections.ToArray(ruleList, new BinaryRule[ruleList.Count]);
					// Arrays.sort(splitRulesWithLC[state]);
					// do right
					ruleList.Clear();
					foreach (BinaryRule br_1 in rulesWithRC[state])
					{
						if (!IsSynthetic(br_1.leftChild))
						{
							ruleList.Add(br_1);
						}
					}
					splitRulesWithRC[state] = Sharpen.Collections.ToArray(ruleList, new BinaryRule[ruleList.Count]);
				}
			}
		}

		// Arrays.sort(splitRulesWithRC[state]);
		// parent accessor
		//      splitRulesWithParent[state] = toBRArray(rulesWithParent[state]);
		public virtual BinaryRule[] SplitRulesWithLC(int state)
		{
			// if (state >= splitRulesWithLC.length) {
			//   return EMPTY_BINARY_RULE_ARRAY;
			// }
			return splitRulesWithLC[state];
		}

		public virtual BinaryRule[] SplitRulesWithRC(int state)
		{
			// if (state >= splitRulesWithRC.length) {
			//   return EMPTY_BINARY_RULE_ARRAY;
			// }
			return splitRulesWithRC[state];
		}

		//  public BinaryRule[] splitRulesWithParent(int state) {
		//    return splitRulesWithParent[state];
		//  }
		// the sensible version
		public virtual double ScoreRule(BinaryRule br)
		{
			BinaryRule rule = ruleMap[br];
			return (rule != null ? rule.score : double.NegativeInfinity);
		}

		public virtual void AddRule(BinaryRule br)
		{
			//    System.out.println("BG adding rule " + br);
			rulesWithParent[br.parent].Add(br);
			rulesWithLC[br.leftChild].Add(br);
			rulesWithRC[br.rightChild].Add(br);
			ruleSetWithLC[br.leftChild].Add(br);
			ruleSetWithRC[br.rightChild].Add(br);
			allRules.Add(br);
			ruleMap[br] = br;
		}

		public virtual IEnumerator<BinaryRule> GetEnumerator()
		{
			return allRules.GetEnumerator();
		}

		public virtual IEnumerator<BinaryRule> RuleIteratorByParent(int state)
		{
			if (state >= rulesWithParent.Length)
			{
				return Java.Util.Collections.EmptyList<BinaryRule>().GetEnumerator();
			}
			return rulesWithParent[state].GetEnumerator();
		}

		public virtual IEnumerator<BinaryRule> RuleIteratorByRightChild(int state)
		{
			if (state >= rulesWithRC.Length)
			{
				return Java.Util.Collections.EmptyList<BinaryRule>().GetEnumerator();
			}
			return rulesWithRC[state].GetEnumerator();
		}

		public virtual IEnumerator<BinaryRule> RuleIteratorByLeftChild(int state)
		{
			if (state >= rulesWithLC.Length)
			{
				return Java.Util.Collections.EmptyList<BinaryRule>().GetEnumerator();
			}
			return rulesWithLC[state].GetEnumerator();
		}

		public virtual IList<BinaryRule> RuleListByParent(int state)
		{
			if (state >= rulesWithParent.Length)
			{
				return Java.Util.Collections.EmptyList();
			}
			return rulesWithParent[state];
		}

		public virtual IList<BinaryRule> RuleListByRightChild(int state)
		{
			if (state >= rulesWithRC.Length)
			{
				return Java.Util.Collections.EmptyList();
			}
			return rulesWithRC[state];
		}

		public virtual IList<BinaryRule> RuleListByLeftChild(int state)
		{
			if (state >= rulesWithRC.Length)
			{
				return Java.Util.Collections.EmptyList();
			}
			return rulesWithLC[state];
		}

		/* ----
		public Set<BinaryRule> ruleSetByRightChild(int state) {
		if (state >= ruleSetWithRC.length) {
		return Collections.<BinaryRule>emptySet();
		}
		return ruleSetWithRC[state];
		}
		
		public Set<BinaryRule> ruleSetByLeftChild(int state) {
		if (state >= ruleSetWithRC.length) {
		return Collections.<BinaryRule>emptySet();
		}
		return ruleSetWithLC[state];
		}
		--- */
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			Init();
			foreach (BinaryRule br in allRules)
			{
				rulesWithParent[br.parent].Add(br);
				rulesWithLC[br.leftChild].Add(br);
				rulesWithRC[br.rightChild].Add(br);
				ruleMap[br] = br;
			}
			SplitRules();
		}

		private void Init()
		{
			ruleMap = Generics.NewHashMap();
			int numStates = index.Size();
			rulesWithParent = new IList[numStates];
			rulesWithLC = new IList[numStates];
			rulesWithRC = new IList[numStates];
			ruleSetWithLC = new ISet[numStates];
			ruleSetWithRC = new ISet[numStates];
			for (int s = 0; s < numStates; s++)
			{
				rulesWithParent[s] = new List<BinaryRule>();
				rulesWithLC[s] = new List<BinaryRule>();
				rulesWithRC[s] = new List<BinaryRule>();
				ruleSetWithLC[s] = Generics.NewHashSet();
				ruleSetWithRC[s] = Generics.NewHashSet();
			}
		}

		public BinaryGrammar(IIndex<string> stateIndex)
		{
			this.index = stateIndex;
			allRules = new List<BinaryRule>();
			Init();
		}

		/// <summary>
		/// Populates data in this BinaryGrammar from the character stream
		/// given by the Reader r.
		/// </summary>
		/// <param name="in">Where input is read from</param>
		/// <exception cref="System.IO.IOException">If format is bung</exception>
		public virtual void ReadData(BufferedReader @in)
		{
			//if (Test.verbose) log.info(">> readData");
			string line;
			int lineNum = 1;
			line = @in.ReadLine();
			while (line != null && line.Length > 0)
			{
				try
				{
					AddRule(new BinaryRule(line, index));
				}
				catch (Exception)
				{
					throw new IOException("Error on line " + lineNum);
				}
				lineNum++;
				line = @in.ReadLine();
			}
			SplitRules();
		}

		/// <summary>Writes out data from this Object to the Writer w.</summary>
		/// <param name="w">Where output is written</param>
		/// <exception cref="System.IO.IOException">If data can't be written</exception>
		public virtual void WriteData(TextWriter w)
		{
			PrintWriter @out = new PrintWriter(w);
			foreach (BinaryRule br in this)
			{
				@out.Println(br.ToString(index));
			}
			@out.Flush();
		}

		private const long serialVersionUID = 1L;
	}
}
