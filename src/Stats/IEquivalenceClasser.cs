using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A strategy-type interface for specifying a function from
	/// <see cref="object"/>
	/// s
	/// to their equivalence classes.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <seealso cref="EquivalenceClassEval{IN, OUT}"/>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Re-templatization)</author>
	/// <?/>
	/// <?/>
	public interface IEquivalenceClasser<In, Out>
	{
		OUT EquivalenceClass(IN o);
	}
}
