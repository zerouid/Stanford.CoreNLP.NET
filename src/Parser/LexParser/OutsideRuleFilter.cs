using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>This class is currently unused.</summary>
	/// <author>Dan Klein</author>
	public class OutsideRuleFilter
	{
		private readonly IIndex<string> tagIndex;

		private int numTags;

		private int numFAs;

		private OutsideRuleFilter.FA[] leftFA;

		private OutsideRuleFilter.FA[] rightFA;

		protected internal static IList<A> Reverse<A>(IList<A> list)
		{
			int sz = list.Count;
			IList<A> reverse = new List<A>(sz);
			for (int i = sz - 1; i >= 0; i--)
			{
				reverse.Add(list[i]);
			}
			return reverse;
		}

		private OutsideRuleFilter.FA BuildFA(IList<string> tags)
		{
			OutsideRuleFilter.FA fa = new OutsideRuleFilter.FA(tags.Count + 1, numTags);
			fa.SetLoopState(0, true);
			for (int state = 1; state <= tags.Count; state++)
			{
				string tagO = tags[state - 1];
				if (tagO == null)
				{
					fa.SetLoopState(state, true);
					for (int symbol = 0; symbol < numTags; symbol++)
					{
						fa.SetTransition(state - 1, symbol, state);
					}
				}
				else
				{
					int tag = tagIndex.IndexOf(tagO);
					fa.SetTransition(state - 1, tag, state);
				}
			}
			return fa;
		}

		private void RegisterRule(IList<string> leftTags, IList<string> rightTags, int state)
		{
			leftFA[state] = BuildFA(leftTags);
			rightFA[state] = BuildFA(Reverse(rightTags));
		}

		public virtual void Init()
		{
			for (int rule = 0; rule < numFAs; rule++)
			{
				leftFA[rule].Init();
				rightFA[rule].Init();
			}
		}

		public virtual void AdvanceRight(bool[] tags)
		{
			for (int tag = 0; tag < numTags; tag++)
			{
				if (!tags[tag])
				{
					continue;
				}
				for (int rule = 0; rule < numFAs; rule++)
				{
					leftFA[rule].Input(tag);
				}
			}
			for (int rule_1 = 0; rule_1 < numFAs; rule_1++)
			{
				leftFA[rule_1].Advance();
			}
		}

		public virtual void LeftAccepting(bool[] result)
		{
			for (int rule = 0; rule < numFAs; rule++)
			{
				result[rule] = leftFA[rule].IsAccepting();
			}
		}

		public virtual void AdvanceLeft(bool[] tags)
		{
			for (int tag = 0; tag < numTags; tag++)
			{
				if (!tags[tag])
				{
					continue;
				}
				for (int rule = 0; rule < numFAs; rule++)
				{
					rightFA[rule].Input(tag);
				}
			}
			for (int rule_1 = 0; rule_1 < numFAs; rule_1++)
			{
				rightFA[rule_1].Advance();
			}
		}

		public virtual void RightAccepting(bool[] result)
		{
			for (int rule = 0; rule < numFAs; rule++)
			{
				result[rule] = rightFA[rule].IsAccepting();
			}
		}

		private void Allocate(int numFAs)
		{
			this.numFAs = numFAs;
			leftFA = new OutsideRuleFilter.FA[numFAs];
			rightFA = new OutsideRuleFilter.FA[numFAs];
		}

		public OutsideRuleFilter(BinaryGrammar bg, IIndex<string> stateIndex, IIndex<string> tagIndex)
		{
			this.tagIndex = tagIndex;
			int numStates = stateIndex.Size();
			numTags = tagIndex.Size();
			Allocate(numStates);
			for (int state = 0; state < numStates; state++)
			{
				string stateStr = stateIndex.Get(state);
				IList<string> left = new List<string>();
				IList<string> right = new List<string>();
				if (!bg.IsSynthetic(state))
				{
					RegisterRule(left, right, state);
					continue;
				}
				bool foundSemi = false;
				bool foundDots = false;
				IList<string> array = left;
				StringBuilder sb = new StringBuilder();
				for (int c = 0; c < stateStr.Length; c++)
				{
					if (stateStr[c] == ':')
					{
						foundSemi = true;
						continue;
					}
					if (!foundSemi)
					{
						continue;
					}
					if (stateStr[c] == ' ')
					{
						if (sb.Length > 0)
						{
							string str = sb.ToString();
							if (!tagIndex.Contains(str))
							{
								str = null;
							}
							array.Add(str);
							sb = new StringBuilder();
						}
						continue;
					}
					if (!foundDots && stateStr[c] == '.')
					{
						c += 3;
						foundDots = true;
						array = right;
						continue;
					}
					sb.Append(stateStr[c]);
				}
				RegisterRule(left, right, state);
			}
		}

		/// <summary>This is a simple Finite Automaton implementation.</summary>
		internal class FA
		{
			private bool[] inStatePrev;

			private bool[] inStateNext;

			private readonly bool[] loopState;

			private readonly int acceptingState;

			private const int initialState = 0;

			private readonly int numStates;

			private readonly int numSymbols;

			private readonly int[][] transition;

			// state x tag
			public virtual void Init()
			{
				Arrays.Fill(inStatePrev, false);
				Arrays.Fill(inStateNext, false);
				inStatePrev[initialState] = true;
			}

			public virtual void Input(int symbol)
			{
				for (int prevState = 0; prevState < numStates; prevState++)
				{
					if (inStatePrev[prevState])
					{
						inStateNext[transition[prevState][symbol]] = true;
					}
				}
			}

			public virtual void Advance()
			{
				bool[] temp = inStatePrev;
				inStatePrev = inStateNext;
				inStateNext = temp;
				Arrays.Fill(inStateNext, false);
				for (int state = 0; state < numStates; state++)
				{
					if (inStatePrev[state] && loopState[state])
					{
						inStateNext[state] = true;
					}
				}
			}

			public virtual bool IsAccepting()
			{
				return inStatePrev[acceptingState];
			}

			public virtual void SetTransition(int state, int symbol, int result)
			{
				transition[state][symbol] = result;
			}

			public virtual void SetLoopState(int state, bool loops)
			{
				loopState[state] = loops;
			}

			public FA(int numStates, int numSymbols)
			{
				this.numStates = numStates;
				this.numSymbols = numSymbols;
				acceptingState = numStates - 1;
				inStatePrev = new bool[numStates];
				inStateNext = new bool[numStates];
				loopState = new bool[numStates];
				transition = new int[numStates][];
			}
		}
		// end class FA
	}
}
