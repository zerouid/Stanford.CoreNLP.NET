using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Represents a stack where one prefix of the stack can branch in
	/// several directions.
	/// </summary>
	/// <remarks>
	/// Represents a stack where one prefix of the stack can branch in
	/// several directions.  Calling "push" on this object returns a new
	/// object which points to the previous state.  Calling "pop" returns a
	/// pointer to the previous state.  The only way to access the current
	/// node's information is with "peek".
	/// <br />
	/// Note that if you have an earlier node in the tree, you have no way
	/// of recovering later nodes.  It is essential to keep the ends of the
	/// stack you are interested in.
	/// </remarks>
	public class TreeShapedStack<T>
	{
		/// <summary>Creates an empty stack.</summary>
		public TreeShapedStack()
			: this(null, null, 0)
		{
		}

		private TreeShapedStack(Edu.Stanford.Nlp.Util.TreeShapedStack<T> previous, T data, int size)
		{
			this.previous = previous;
			this.data = data;
			this.size = size;
		}

		/// <summary>Returns the previous state.</summary>
		/// <remarks>
		/// Returns the previous state.  If the size of the stack is 0, an
		/// exception is thrown.  If the size is 1, an empty node is
		/// returned.
		/// </remarks>
		public virtual Edu.Stanford.Nlp.Util.TreeShapedStack<T> Pop()
		{
			if (size == 0)
			{
				throw new EmptyStackException();
			}
			return previous;
		}

		/// <summary>Returns a new node with the new data attached.</summary>
		public virtual Edu.Stanford.Nlp.Util.TreeShapedStack<T> Push(T data)
		{
			return new Edu.Stanford.Nlp.Util.TreeShapedStack<T>(this, data, size + 1);
		}

		/// <summary>Returns the data in the top node of the stack.</summary>
		/// <remarks>
		/// Returns the data in the top node of the stack.  If there is no
		/// data, eg the stack size is 0, an exception is thrown.
		/// </remarks>
		public virtual T Peek()
		{
			if (size == 0)
			{
				throw new EmptyStackException();
			}
			return data;
		}

		/// <summary>How many nodes in this branch of the stack</summary>
		public virtual int Size()
		{
			return size;
		}

		/// <summary>Returns the current stack as a list</summary>
		public virtual IList<T> AsList()
		{
			IList<T> result = Generics.NewArrayList(size);
			Edu.Stanford.Nlp.Util.TreeShapedStack<T> current = this;
			for (int index = 0; index < size; ++index)
			{
				result.Add(current.data);
				current = current.Pop();
			}
			Java.Util.Collections.Reverse(result);
			return result;
		}

		public override string ToString()
		{
			return "[" + InternalToString(" ") + "]";
		}

		public virtual string ToString(string delimiter)
		{
			return "[" + InternalToString(delimiter) + "]";
		}

		private string InternalToString(string delimiter)
		{
			if (Size() == 0)
			{
				return " ";
			}
			else
			{
				if (Size() == 1)
				{
					return data.ToString();
				}
				else
				{
					return previous.InternalToString(delimiter) + "," + delimiter + data.ToString();
				}
			}
		}

		public override int GetHashCode()
		{
			int hash = Size();
			if (Size() > 0 && Peek() != null)
			{
				hash ^= Peek().GetHashCode();
			}
			return hash;
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.TreeShapedStack))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.TreeShapedStack<object> other = (Edu.Stanford.Nlp.Util.TreeShapedStack<object>)o;
			Edu.Stanford.Nlp.Util.TreeShapedStack<T> current = this;
			if (other.Size() != this.Size())
			{
				return false;
			}
			for (int i = 0; i < Size(); ++i)
			{
				T currentObject = current.Peek();
				object otherObject = other.Peek();
				if (!(currentObject == otherObject || (currentObject != null && currentObject.Equals(otherObject))))
				{
					return false;
				}
				other = other.Pop();
				current = current.Pop();
			}
			return true;
		}

		internal readonly T data;

		internal readonly int size;

		internal readonly Edu.Stanford.Nlp.Util.TreeShapedStack<T> previous;
	}
}
