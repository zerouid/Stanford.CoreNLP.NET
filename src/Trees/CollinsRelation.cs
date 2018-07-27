



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A relation 4-tuple for the dependency representation of Collins (1999; 2003).</summary>
	/// <remarks>
	/// A relation 4-tuple for the dependency representation of Collins (1999; 2003).
	/// The tuple represents categories common to a head and its modifier:
	/// Parent    - The common parent between the head daughter and the daughter in which the
	/// modifier appears.
	/// Head      - The category label of the head daughter.
	/// Modifier  - The category label of the daughter in which the modifier appears.
	/// Direction - Orientation of the modifier with respect to the head.
	/// </remarks>
	/// <author>Spence Green</author>
	public class CollinsRelation
	{
		public enum Direction
		{
			Left,
			Right
		}

		private readonly string parent;

		private readonly string head;

		private readonly string modifier;

		private readonly CollinsRelation.Direction direction;

		private const int defaultPadding = 8;

		public CollinsRelation(string par, string head, string mod, CollinsRelation.Direction dir)
		{
			parent = par;
			this.head = head;
			modifier = mod;
			direction = dir;
		}

		public override string ToString()
		{
			string dir = (direction == CollinsRelation.Direction.Left) ? "L" : "R";
			return string.Format("%s%s%s%s", Pad(parent), Pad(head), Pad(modifier), dir);
		}

		private static string Pad(string s)
		{
			if (s == null)
			{
				return null;
			}
			int add = defaultPadding - s.Length;
			//Number of whitespace characters to add
			if (add <= 0)
			{
				return s;
			}
			StringBuilder str = new StringBuilder(s);
			char[] ch = new char[add];
			Arrays.Fill(ch, ' ');
			str.Append(ch);
			return str.ToString();
		}

		public override bool Equals(object other)
		{
			if (this == other)
			{
				return true;
			}
			if (!(other is Edu.Stanford.Nlp.Trees.CollinsRelation))
			{
				return false;
			}
			Edu.Stanford.Nlp.Trees.CollinsRelation otherRel = (Edu.Stanford.Nlp.Trees.CollinsRelation)other;
			return (parent.Equals(otherRel.parent) && head.Equals(otherRel.head) && modifier.Equals(otherRel.modifier) && direction == otherRel.direction);
		}

		public override int GetHashCode()
		{
			int hash = 1;
			hash *= 68 * parent.GetHashCode();
			hash *= 983 * modifier.GetHashCode();
			hash *= 672 * head.GetHashCode();
			hash *= (direction == CollinsRelation.Direction.Left) ? -1 : 1;
			return hash;
		}
	}
}
