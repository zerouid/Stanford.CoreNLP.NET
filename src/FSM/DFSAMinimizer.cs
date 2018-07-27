using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>
	/// DFSAMinimizer minimizes (unweighted) deterministic finite state
	/// automata.
	/// </summary>
	/// <author>Dan Klein</author>
	/// <version>12/14/2000</version>
	public sealed class DFSAMinimizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Fsm.DFSAMinimizer));

		internal static bool debug = false;

		private DFSAMinimizer()
		{
		}

		internal class IntPair
		{
			internal int i;

			internal int j;

			internal IntPair(int i, int j)
			{
				// static methods class
				this.i = i;
				this.j = j;
			}
		}

		public static void UnweightedMinimize<T, S>(DFSA<T, S> dfsa)
		{
			ICollection<DFSAState<T, S>> states = dfsa.States();
			long time = Runtime.CurrentTimeMillis();
			if (debug)
			{
				time = Runtime.CurrentTimeMillis();
				log.Info("\nStarting on " + dfsa.dfsaID);
				log.Info(" -- " + states.Count + " states.");
			}
			int numStates = states.Count;
			// assign ids
			int id = 0;
			DFSAState<T, S>[] state = ErasureUtils.UncheckedCast<DFSAState<T, S>[]>(new DFSAState[numStates]);
			IDictionary<DFSAState<T, S>, int> stateToID = Generics.NewHashMap();
			foreach (DFSAState<T, S> state1 in states)
			{
				state[id] = state1;
				stateToID[state1] = int.Parse(id);
				id++;
			}
			// initialize grid
			bool[][] distinct = new bool[numStates][];
			IList<DFSAMinimizer.IntPair>[][] dependentList = ErasureUtils.UncheckedCast<IList<DFSAMinimizer.IntPair>[][]>(new IList[numStates][]);
			for (int i = 0; i < numStates; i++)
			{
				for (int j = i + 1; j < numStates; j++)
				{
					distinct[i][j] = state[i].IsAccepting() != state[j].IsAccepting();
				}
			}
			if (debug)
			{
				log.Info("Initialized: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// visit all non-distinct
			for (int i_1 = 0; i_1 < numStates; i_1++)
			{
				for (int j = i_1 + 1; j < numStates; j++)
				{
					if (!distinct[i_1][j])
					{
						DFSAState<T, S> state1_1 = state[i_1];
						DFSAState<T, S> state2 = state[j];
						DFSAMinimizer.IntPair ip = new DFSAMinimizer.IntPair(i_1, j);
						// check if some input distinguishes this pair
						ICollection<T> inputs = Generics.NewHashSet();
						Sharpen.Collections.AddAll(inputs, state1_1.ContinuingInputs());
						Sharpen.Collections.AddAll(inputs, state2.ContinuingInputs());
						bool distinguishable = false;
						ICollection<DFSAMinimizer.IntPair> pendingIPairs = Generics.NewHashSet();
						IEnumerator<T> inputI = inputs.GetEnumerator();
						while (inputI.MoveNext() && !distinguishable)
						{
							T input = inputI.Current;
							DFSATransition<T, S> transition1 = state1_1.Transition(input);
							DFSATransition<T, S> transition2 = state2.Transition(input);
							if ((transition1 == null) != (transition2 == null))
							{
								distinguishable = true;
							}
							if (transition1 != null && transition2 != null)
							{
								DFSAState<T, S> target1 = transition1.GetTarget();
								DFSAState<T, S> target2 = transition2.GetTarget();
								int num1 = stateToID[target1];
								int num2 = stateToID[target2];
								DFSAMinimizer.IntPair targetIPair = new DFSAMinimizer.IntPair(num1, num2);
								if (num1 != num2)
								{
									if (distinct[num1][num2])
									{
										distinguishable = true;
									}
									else
									{
										pendingIPairs.Add(targetIPair);
									}
								}
							}
						}
						if (distinguishable)
						{
							// if the pair is distinguishable, record that
							IList<DFSAMinimizer.IntPair> markStack = new List<DFSAMinimizer.IntPair>();
							markStack.Add(ip);
							while (!markStack.IsEmpty())
							{
								DFSAMinimizer.IntPair ipToMark = markStack[markStack.Count - 1];
								markStack.Remove(markStack.Count - 1);
								distinct[ipToMark.i][ipToMark.j] = true;
								IList<DFSAMinimizer.IntPair> addList = dependentList[ipToMark.i][ipToMark.j];
								if (addList != null)
								{
									Sharpen.Collections.AddAll(markStack, addList);
								}
							}
						}
						else
						{
							// otherwise add it to any pending pairs
							foreach (DFSAMinimizer.IntPair pendingIPair in pendingIPairs)
							{
								IList<DFSAMinimizer.IntPair> dependentList1 = dependentList[pendingIPair.i][pendingIPair.j];
								if (dependentList1 == null)
								{
									dependentList1 = new List<DFSAMinimizer.IntPair>();
									dependentList[pendingIPair.i][pendingIPair.j] = dependentList1;
								}
								dependentList1.Add(ip);
							}
						}
					}
				}
			}
			if (debug)
			{
				log.Info("All pairs marked: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// decide what canonical state each state will map to...
			IDisjointSet<DFSAState<T, S>> stateClasses = new FastDisjointSet<DFSAState<T, S>>(states);
			for (int i_2 = 0; i_2 < numStates; i_2++)
			{
				for (int j = i_2 + 1; j < numStates; j++)
				{
					if (!distinct[i_2][j])
					{
						DFSAState<T, S> state1_1 = state[i_2];
						DFSAState<T, S> state2 = state[j];
						stateClasses.Union(state1_1, state2);
					}
				}
			}
			IDictionary<DFSAState<T, S>, DFSAState<T, S>> stateToRep = Generics.NewHashMap();
			foreach (DFSAState<T, S> state1_2 in states)
			{
				DFSAState<T, S> rep = stateClasses.Find(state1_2);
				stateToRep[state1_2] = rep;
			}
			if (debug)
			{
				log.Info("Canonical states chosen: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// reduce the DFSA by replacing transition targets with their reps
			foreach (DFSAState<T, S> state1_3 in states)
			{
				if (!state1_3.Equals(stateToRep[state1_3]))
				{
					continue;
				}
				foreach (DFSATransition<T, S> transition in state1_3.Transitions())
				{
					//if (!transition.target.equals(stateToRep.get(transition.target)))
					//  System.out.println(Utils.pad(transition.target.toString(),30)+stateToRep.get(transition.target));
					transition.target = stateToRep[transition.target];
				}
			}
			dfsa.initialState = stateToRep[dfsa.initialState];
			if (debug)
			{
				log.Info("Done: " + (Runtime.CurrentTimeMillis() - time));
			}
		}

		// done!
		internal static void UnweightedMinimizeOld<T, S>(DFSA<T, S> dfsa)
		{
			ICollection<DFSAState<T, S>> states = dfsa.States();
			IDictionary<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>, IList<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>>> stateUPairToDependentUPairList = Generics.NewHashMap(states.Count * states.Count / 2 + 1);
			IDictionary<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>, bool> stateUPairToDistinguished = Generics.NewHashMap(states.Count * states.Count / 2 + 1);
			int[] c = new int[states.Count * states.Count / 2 + 1];
			int streak = 0;
			int collisions = 0;
			int entries = 0;
			long time = Runtime.CurrentTimeMillis();
			if (debug)
			{
				time = Runtime.CurrentTimeMillis();
				log.Info("Starting on " + dfsa.dfsaID);
				log.Info(" -- " + states.Count + " states.");
			}
			// initialize grid
			int numDone = 0;
			foreach (DFSAState<T, S> state1 in states)
			{
				foreach (DFSAState<T, S> state2 in states)
				{
					UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> up = new UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>(state1, state2);
					if (state1.Equals(state2))
					{
						continue;
					}
					if (stateUPairToDistinguished.Contains(up))
					{
						continue;
					}
					int bucket = (up.GetHashCode() & unchecked((int)(0x7FFFFFFF))) % (states.Count * states.Count / 2 + 1);
					c[bucket]++;
					entries++;
					if (c[bucket] > 1)
					{
						collisions++;
						streak = 0;
					}
					else
					{
						streak++;
					}
					if (state1.IsAccepting() != state2.IsAccepting())
					{
						//log.info(Utils.pad((String)state1.stateID, 20)+" "+state2.stateID);
						stateUPairToDistinguished[up] = true;
					}
					else
					{
						stateUPairToDistinguished[up] = false;
					}
				}
				//stateUPairToDependentUPairList.put(up, new ArrayList());
				numDone++;
				if (numDone % 20 == 0)
				{
					log.Info("\r" + numDone + "  " + ((double)collisions / (double)entries));
				}
			}
			if (debug)
			{
				log.Info("\nInitialized: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// visit each undistinguished pair
			foreach (UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> up_1 in stateUPairToDistinguished.Keys)
			{
				DFSAState<T, S> state1_1 = up_1.first;
				DFSAState<T, S> state2 = up_1.second;
				if (stateUPairToDistinguished[up_1].Equals(true))
				{
					continue;
				}
				// check if some input distinguishes this pair
				ICollection<T> inputs = Generics.NewHashSet(state1_1.ContinuingInputs());
				Sharpen.Collections.AddAll(inputs, state2.ContinuingInputs());
				bool distinguishable = false;
				ICollection<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>> pendingUPairs = Generics.NewHashSet();
				IEnumerator<T> inputI = inputs.GetEnumerator();
				while (inputI.MoveNext() && !distinguishable)
				{
					T input = inputI.Current;
					DFSATransition<T, S> transition1 = state1_1.Transition(input);
					DFSATransition<T, S> transition2 = state2.Transition(input);
					if ((transition1 == null) != (transition2 == null))
					{
						distinguishable = true;
					}
					if (transition1 != null && transition2 != null)
					{
						DFSAState<T, S> target1 = transition1.GetTarget();
						DFSAState<T, S> target2 = transition2.GetTarget();
						UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> targetUPair = new UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>(target1, target2);
						if (!target1.Equals(target2))
						{
							if (stateUPairToDistinguished[targetUPair].Equals(true))
							{
								distinguishable = true;
							}
							else
							{
								pendingUPairs.Add(targetUPair);
							}
						}
					}
				}
				// if the pair is distinguishable, record that
				if (distinguishable)
				{
					IList<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>> markStack = new List<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>>();
					markStack.Add(up_1);
					while (!markStack.IsEmpty())
					{
						UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> upToMark = markStack[markStack.Count - 1];
						markStack.Remove(markStack.Count - 1);
						stateUPairToDistinguished[upToMark] = true;
						IList<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>> addList = stateUPairToDependentUPairList[upToMark];
						if (addList != null)
						{
							Sharpen.Collections.AddAll(markStack, addList);
							stateUPairToDependentUPairList[upToMark].Clear();
						}
					}
				}
				else
				{
					// otherwise add it to any pending pairs
					foreach (UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> pendingUPair in pendingUPairs)
					{
						IList<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>> dependentList = stateUPairToDependentUPairList[pendingUPair];
						if (dependentList == null)
						{
							dependentList = new List<UnorderedPair<DFSAState<T, S>, DFSAState<T, S>>>();
							stateUPairToDependentUPairList[pendingUPair] = dependentList;
						}
						dependentList.Add(up_1);
					}
				}
			}
			if (debug)
			{
				log.Info("All pairs marked: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// decide what canonical state each state will map to...
			IDisjointSet<DFSAState<T, S>> stateClasses = new FastDisjointSet<DFSAState<T, S>>(states);
			foreach (UnorderedPair<DFSAState<T, S>, DFSAState<T, S>> up_2 in stateUPairToDistinguished.Keys)
			{
				if (stateUPairToDistinguished[up_2].Equals(false))
				{
					DFSAState<T, S> state1_1 = up_2.first;
					DFSAState<T, S> state2 = up_2.second;
					stateClasses.Union(state1_1, state2);
				}
			}
			IDictionary<DFSAState<T, S>, DFSAState<T, S>> stateToRep = Generics.NewHashMap();
			foreach (DFSAState<T, S> state in states)
			{
				DFSAState<T, S> rep = stateClasses.Find(state);
				stateToRep[state] = rep;
			}
			if (debug)
			{
				log.Info("Canonical states chosen: " + (Runtime.CurrentTimeMillis() - time));
				time = Runtime.CurrentTimeMillis();
			}
			// reduce the DFSA by replacing transition targets with their reps
			foreach (DFSAState<T, S> state_1 in states)
			{
				if (!state_1.Equals(stateToRep[state_1]))
				{
					continue;
				}
				foreach (DFSATransition<T, S> transition in state_1.Transitions())
				{
					transition.target = stateClasses.Find(transition.target);
				}
			}
			dfsa.initialState = stateClasses.Find(dfsa.initialState);
			if (debug)
			{
				log.Info("Done: " + (Runtime.CurrentTimeMillis() - time));
			}
		}
		// done!
	}
}
