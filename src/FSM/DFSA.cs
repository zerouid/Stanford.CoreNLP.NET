using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>
	/// DFSA: A class for representing a deterministic finite state automaton
	/// without epsilon transitions.
	/// </summary>
	/// <author>Dan Klein</author>
	/// <author>Michel Galley (AT&amp;T FSM library format printing)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	public sealed class DFSA<T, S> : IScored
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(DFSA));

		internal object dfsaID;

		internal DFSAState<T, S> initialState;

		public DFSA(DFSAState<T, S> initialState, double score)
		{
			this.initialState = initialState;
			this.score = score;
		}

		public DFSA(DFSAState<T, S> initialState)
		{
			this.initialState = initialState;
			this.score = double.NaN;
		}

		private double score;

		public double Score()
		{
			return score;
		}

		public void SetScore(double score)
		{
			this.score = score;
		}

		public DFSAState<T, S> InitialState()
		{
			return initialState;
		}

		public void SetInitialState(DFSAState<T, S> initialState)
		{
			this.initialState = initialState;
		}

		public ICollection<DFSAState<T, S>> States()
		{
			ICollection<DFSAState<T, S>> visited = Generics.NewHashSet();
			IList<DFSAState<T, S>> toVisit = new List<DFSAState<T, S>>();
			toVisit.Add(InitialState());
			ExploreStates(toVisit, visited);
			return visited;
		}

		private static void ExploreStates<T, S>(IList<DFSAState<T, S>> toVisit, ICollection<DFSAState<T, S>> visited)
		{
			while (!toVisit.IsEmpty())
			{
				DFSAState<T, S> state = toVisit[toVisit.Count - 1];
				toVisit.Remove(toVisit.Count - 1);
				if (!visited.Contains(state))
				{
					Sharpen.Collections.AddAll(toVisit, state.SuccessorStates());
					visited.Add(state);
				}
			}
		}

		public DFSA(object dfsaID)
		{
			this.dfsaID = dfsaID;
			this.score = 0;
		}

		private static void PrintTrieDFSAHelper<T, S>(DFSAState<T, S> state, int level)
		{
			if (state.IsAccepting())
			{
				return;
			}
			ICollection<T> inputs = state.ContinuingInputs();
			foreach (T input in inputs)
			{
				DFSATransition<T, S> transition = state.Transition(input);
				System.Console.Out.Write(level);
				System.Console.Out.Write(input);
				for (int i = 0; i < level; i++)
				{
					System.Console.Out.Write("   ");
				}
				System.Console.Out.Write(transition.Score());
				System.Console.Out.Write(" ");
				System.Console.Out.WriteLine(input);
				PrintTrieDFSAHelper(transition.Target(), level + 1);
			}
		}

		public static void PrintTrieDFSA<T, S>(DFSA<T, S> dfsa)
		{
			log.Info("DFSA: " + dfsa.dfsaID);
			PrintTrieDFSAHelper(dfsa.InitialState(), 2);
		}

		/// <exception cref="System.IO.IOException"/>
		public void PrintAttFsmFormat(TextWriter w)
		{
			IQueue<DFSAState<T, S>> q = new LinkedList<DFSAState<T, S>>();
			ICollection<DFSAState<T, S>> visited = Generics.NewHashSet();
			q.Offer(initialState);
			while (q.Peek() != null)
			{
				DFSAState<T, S> state = q.Poll();
				if (state == null || visited.Contains(state))
				{
					continue;
				}
				visited.Add(state);
				if (state.IsAccepting())
				{
					w.Write(state.ToString() + "\t" + state.Score() + "\n");
					continue;
				}
				TreeSet<T> inputs = new TreeSet<T>(state.ContinuingInputs());
				foreach (T input in inputs)
				{
					DFSATransition<T, S> transition = state.Transition(input);
					DFSAState<T, S> target = transition.Target();
					if (!visited.Contains(target))
					{
						q.Add(target);
					}
					w.Write(state.ToString() + "\t" + target.ToString() + "\t" + transition.GetInput() + "\t" + transition.Score() + "\n");
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static void PrintTrieAsRulesHelper<T, S>(DFSAState<T, S> state, string prefix, TextWriter w)
		{
			if (state.IsAccepting())
			{
				return;
			}
			ICollection<T> inputs = state.ContinuingInputs();
			foreach (T input in inputs)
			{
				DFSATransition<T, S> transition = state.Transition(input);
				DFSAState<T, S> target = transition.Target();
				ICollection<T> inputs2 = target.ContinuingInputs();
				bool allTerminate = true;
				foreach (T input2 in inputs2)
				{
					DFSATransition<T, S> transition2 = target.Transition(input2);
					DFSAState<T, S> target2 = transition2.Target();
					if (target2.IsAccepting())
					{
						// it's a binary end rule.  Print it.
						w.Write(prefix + " --> " + input + " " + input2 + "\n");
					}
					else
					{
						allTerminate = false;
					}
				}
				if (!allTerminate)
				{
					// there are some longer continuations.  Print continuation rule
					string newPrefix = prefix + "_" + input;
					w.Write(prefix + " --> " + input + " " + newPrefix + "\n");
					PrintTrieAsRulesHelper(transition.Target(), newPrefix, w);
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void PrintTrieAsRules<T, S>(DFSA<T, S> dfsa, TextWriter w)
		{
			PrintTrieAsRulesHelper(dfsa.InitialState(), dfsa.dfsaID.ToString(), w);
		}
	}
}
