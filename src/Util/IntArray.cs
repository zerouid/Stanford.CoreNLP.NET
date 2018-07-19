using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Simple wrapper around an array of int, which overrides hashCode() and equals()
	/// of Object.
	/// </summary>
	/// <remarks>
	/// Simple wrapper around an array of int, which overrides hashCode() and equals()
	/// of Object. This class is useful if used as a key in a HashMap, Counter, etc.
	/// </remarks>
	/// <author>Michel Galley</author>
	public class IntArray
	{
		private readonly int[] array;

		public IntArray(int[] array)
		{
			this.array = array;
		}

		public virtual int[] Get()
		{
			return array;
		}

		public override int GetHashCode()
		{
			return Arrays.HashCode(array);
		}

		public override bool Equals(object o)
		{
			return Arrays.Equals(array, ((Edu.Stanford.Nlp.Util.IntArray)o).array);
		}
	}
}
