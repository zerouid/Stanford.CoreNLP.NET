

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Class for parse edges.</summary>
	/// <author>Dan Klein</author>
	public class Edge : Item
	{
		public Hook backHook;

		public Edge(bool exhaustiveTest)
			: base(exhaustiveTest)
		{
		}

		public Edge(Edu.Stanford.Nlp.Parser.Lexparser.Edge e)
			: base(e)
		{
			backHook = e.backHook;
		}

		public override bool IsEdge()
		{
			return true;
		}

		public override string ToString()
		{
			// TODO: used to contain more useful information
			//return "Edge(" + Numberer.getGlobalNumberer("states").object(state) + ":" + start + "-" + end + "," + head + "/" + Numberer.getGlobalNumberer("tags").object(tag) + ")";
			return "Edge(" + state + ":" + start + "-" + end + "," + head + "/" + tag + ")";
		}

		public override int GetHashCode()
		{
			return (state << 1) ^ (head << 8) ^ (tag << 16) ^ (start << 4) ^ (end << 24);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (o is Edu.Stanford.Nlp.Parser.Lexparser.Edge)
				{
					Edu.Stanford.Nlp.Parser.Lexparser.Edge e = (Edu.Stanford.Nlp.Parser.Lexparser.Edge)o;
					if (state == e.state && head == e.head && tag == e.tag && start == e.start && end == e.end)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
