using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public interface IBlock<E>
	{
		ICollection<E> GetMembers();
	}
}
