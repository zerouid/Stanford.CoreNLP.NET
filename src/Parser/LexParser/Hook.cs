using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Class for parse table hooks.</summary>
	/// <remarks>
	/// Class for parse table hooks.  A "hook" is the parse item that Eisner and
	/// Satta introduced to reduce the complexity of lexicalized parsing to
	/// O(n^4).
	/// </remarks>
	/// <author>Dan Klein</author>
	public class Hook : Item
	{
		public int subState;

		public Hook(bool exhaustiveTest)
			: base(exhaustiveTest)
		{
		}

		public Hook(Edu.Stanford.Nlp.Parser.Lexparser.Hook h)
			: base(h)
		{
			subState = h.subState;
		}

		public override bool IsPreHook()
		{
			return head < start;
		}

		public override bool IsPostHook()
		{
			return head >= end;
		}

		public override string ToString()
		{
			// TODO: used to have more useful information
			//return (isPreHook() ? "Pre" : "Post") + "Hook(" + Numberer.getGlobalNumberer("states").object(state) + "|" + Numberer.getGlobalNumberer("states").object(subState) + ":" + start + "-" + end + "," + head + "/" + Numberer.getGlobalNumberer("tags").object(tag) + ")";
			return (IsPreHook() ? "Pre" : "Post") + "Hook(" + state + "|" + subState + ":" + start + "-" + end + "," + head + "/" + tag + ")";
		}

		public override int GetHashCode()
		{
			return 1 + (state << 14) ^ (subState << 16) ^ (head << 22) ^ (tag << 27) ^ (start << 1) ^ (end << 7);
		}

		/// <summary>
		/// Hooks are equal if they have same state, substate, head, tag, start,
		/// and end.
		/// </summary>
		public override bool Equals(object o)
		{
			// System.out.println("\nCHECKING HOOKS: " + this + " vs. " + o);
			if (this == o)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Parser.Lexparser.Hook)
			{
				Edu.Stanford.Nlp.Parser.Lexparser.Hook e = (Edu.Stanford.Nlp.Parser.Lexparser.Hook)o;
				if (state == e.state && subState == e.subState && head == e.head && tag == e.tag && start == e.start && end == e.end)
				{
					return true;
				}
			}
			return false;
		}
	}
}
