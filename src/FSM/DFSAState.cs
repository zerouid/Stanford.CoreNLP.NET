using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>
	/// DFSAState represents the state of a deterministic finite state
	/// automaton without epsilon transitions.
	/// </summary>
	/// <author>Dan Klein</author>
	/// <version>12/14/2000</version>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	/// <?/>
	/// <?/>
	public sealed class DFSAState<T, S> : IScored
	{
		private S stateID;

		private IDictionary<T, DFSATransition<T, S>> inputToTransition;

		public bool accepting;

		private DFSA<T, S> dfsa;

		public double score;

		public double Score()
		{
			return score;
		}

		public void SetScore(double score)
		{
			this.score = score;
		}

		public DFSA<T, S> Dfsa()
		{
			return dfsa;
		}

		public void SetStateID(S stateID)
		{
			this.stateID = stateID;
		}

		public S StateID()
		{
			return stateID;
		}

		public void AddTransition(DFSATransition<T, S> transition)
		{
			inputToTransition[transition.Input()] = transition;
		}

		public DFSATransition<T, S> Transition(T input)
		{
			return inputToTransition[input];
		}

		public ICollection<DFSATransition<T, S>> Transitions()
		{
			return inputToTransition.Values;
		}

		public ICollection<T> ContinuingInputs()
		{
			return inputToTransition.Keys;
		}

		public ICollection<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> SuccessorStates()
		{
			ICollection<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> successors = Generics.NewHashSet();
			ICollection<DFSATransition<T, S>> transitions = inputToTransition.Values;
			foreach (DFSATransition<T, S> transition in transitions)
			{
				successors.Add(transition.GetTarget());
			}
			return successors;
		}

		public void SetAccepting(bool accepting)
		{
			this.accepting = accepting;
		}

		public bool IsAccepting()
		{
			return accepting;
		}

		public bool IsContinuable()
		{
			return !inputToTransition.IsEmpty();
		}

		public override string ToString()
		{
			return stateID.ToString();
		}

		private int hashCodeCache;

		// = 0;
		public override int GetHashCode()
		{
			if (hashCodeCache == 0)
			{
				hashCodeCache = stateID.GetHashCode() ^ dfsa.GetHashCode();
			}
			return hashCodeCache;
		}

		// equals
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Fsm.DFSAState))
			{
				return false;
			}
			Edu.Stanford.Nlp.Fsm.DFSAState s = (Edu.Stanford.Nlp.Fsm.DFSAState)o;
			// historically also checked: accepting == s.accepting &&
			//inputToTransition.equals(s.inputToTransition))
			return dfsa.Equals(s.dfsa) && stateID.Equals(s.stateID);
		}

		public ICollection<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> StatesReachable()
		{
			ICollection<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> visited = Generics.NewHashSet();
			IList<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> toVisit = new List<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>>();
			toVisit.Add(this);
			ExploreStates(toVisit, visited);
			return visited;
		}

		private void ExploreStates(IList<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> toVisit, ICollection<Edu.Stanford.Nlp.Fsm.DFSAState<T, S>> visited)
		{
			while (!toVisit.IsEmpty())
			{
				Edu.Stanford.Nlp.Fsm.DFSAState<T, S> state = toVisit[toVisit.Count - 1];
				toVisit.Remove(toVisit.Count - 1);
				if (!visited.Contains(state))
				{
					Sharpen.Collections.AddAll(toVisit, state.SuccessorStates());
					visited.Add(state);
				}
			}
		}

		public DFSAState(S id, DFSA<T, S> dfsa)
		{
			this.dfsa = dfsa;
			this.stateID = id;
			this.accepting = false;
			this.inputToTransition = Generics.NewHashMap();
			this.score = double.NegativeInfinity;
		}

		public DFSAState(S id, DFSA<T, S> dfsa, double score)
			: this(id, dfsa)
		{
			SetScore(score);
		}
	}
}
