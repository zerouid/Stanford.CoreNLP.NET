using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Fsm
{
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public interface IBlock<E>
	{
		ICollection<E> GetMembers();
	}
}
