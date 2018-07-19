using Sharpen;

namespace Edu.Stanford.Nlp.Graph
{
	public class DirectedEdge<V, E>
	{
		internal E data;

		internal V head;

		internal V tail;

		public DirectedEdge(E data, V head, V tail)
		{
			this.data = data;
			this.head = head;
			this.tail = tail;
		}

		internal virtual E GetData()
		{
			return data;
		}

		internal virtual V GetHead()
		{
			return head;
		}

		internal virtual V GetTail()
		{
			return tail;
		}
	}
}
