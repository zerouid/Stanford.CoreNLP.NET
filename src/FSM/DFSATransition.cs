using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>
	/// DFSATransition represents a transition in a weighted finite state
	/// transducer.
	/// </summary>
	/// <remarks>
	/// DFSATransition represents a transition in a weighted finite state
	/// transducer.  For now, just null out fields that may not apply.
	/// This should really be FSATransition as there's nothing
	/// deterministic-specific.  If FSA is ever made, this should be
	/// abstracted.  The ID is a descriptor, not a unique ID.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <version>12/14/00</version>
	public sealed class DFSATransition<T, S> : IScored
	{
		private object transitionID;

		private DFSAState<T, S> source;

		protected internal DFSAState<T, S> target;

		private double score;

		private T input;

		private object output;

		public DFSATransition(object transitionID, DFSAState<T, S> source, DFSAState<T, S> target, T input, object output, double score)
		{
			// used directly in DFSAMinimizer (only)
			this.transitionID = transitionID;
			this.source = source;
			this.target = target;
			this.input = input;
			this.output = output;
			this.score = score;
		}

		public DFSAState<T, S> GetSource()
		{
			return source;
		}

		public DFSAState<T, S> Source()
		{
			return source;
		}

		public DFSAState<T, S> GetTarget()
		{
			return target;
		}

		public DFSAState<T, S> Target()
		{
			return target;
		}

		public object GetID()
		{
			return transitionID;
		}

		public double Score()
		{
			return score;
		}

		public T GetInput()
		{
			return input;
		}

		public T Input()
		{
			return input;
		}

		public object GetOutput()
		{
			return output;
		}

		public object Output()
		{
			return output;
		}

		public override string ToString()
		{
			return "[" + transitionID + "]" + source + " -" + input + ":" + output + "-> " + target;
		}
	}
}
