using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Factory for vending Collections.</summary>
	/// <remarks>Factory for vending Collections.  It's a class instead of an interface because I guessed that it'd primarily be used for its inner classes.</remarks>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	[System.Serializable]
	public abstract class CollectionFactory<T>
	{
		private const long serialVersionUID = 3711321773145894069L;

		public static readonly CollectionFactory ArrayListFactory = new CollectionFactory.ArrayListFactory();

		public static readonly CollectionFactory LinkedListFactory = new CollectionFactory.LinkedListFactory();

		public static readonly CollectionFactory HashSetFactory = new CollectionFactory.HashSetFactory();

		public static readonly CollectionFactory TreeSetFactory = new CollectionFactory.TreeSetFactory();

		public abstract ICollection<T> NewCollection();

		public abstract ICollection<T> NewEmptyCollection();

		/// <summary>Return a factory for making ArrayList Collections.</summary>
		/// <remarks>
		/// Return a factory for making ArrayList Collections.
		/// This method allows type safety in calling code.
		/// </remarks>
		/// <returns>A factory for ArrayList collections.</returns>
		public static CollectionFactory<E> ArrayListFactory<E>()
		{
			return ErasureUtils.UncheckedCast(ArrayListFactory);
		}

		public static CollectionFactory<E> ArrayListFactory<E>(int size)
		{
			return ErasureUtils.UncheckedCast(new CollectionFactory.SizedArrayListFactory(size));
		}

		public static CollectionFactory<E> LinkedListFactory<E>()
		{
			return ErasureUtils.UncheckedCast(LinkedListFactory);
		}

		public static CollectionFactory<E> HashSetFactory<E>()
		{
			return ErasureUtils.UncheckedCast(HashSetFactory);
		}

		public static CollectionFactory<E> TreeSetFactory<E>()
		{
			return ErasureUtils.UncheckedCast(TreeSetFactory);
		}

		[System.Serializable]
		public class ArrayListFactory<T> : CollectionFactory<T>
		{
			private const long serialVersionUID = 1L;

			public override ICollection<T> NewCollection()
			{
				return new List<T>();
			}

			public override ICollection<T> NewEmptyCollection()
			{
				return Java.Util.Collections.EmptyList();
			}
		}

		[System.Serializable]
		public class SizedArrayListFactory<T> : CollectionFactory<T>
		{
			private const long serialVersionUID = 1L;

			private int defaultSize = 1;

			public SizedArrayListFactory(int size)
			{
				this.defaultSize = size;
			}

			public override ICollection<T> NewCollection()
			{
				return new List<T>(defaultSize);
			}

			public override ICollection<T> NewEmptyCollection()
			{
				return Java.Util.Collections.EmptyList();
			}
		}

		[System.Serializable]
		public class LinkedListFactory<T> : CollectionFactory<T>
		{
			private const long serialVersionUID = -4236184979948498000L;

			public override ICollection<T> NewCollection()
			{
				return new LinkedList<T>();
			}

			public override ICollection<T> NewEmptyCollection()
			{
				return Java.Util.Collections.EmptyList();
			}
		}

		[System.Serializable]
		public class HashSetFactory<T> : CollectionFactory<T>
		{
			private const long serialVersionUID = -6268401669449458602L;

			public override ICollection<T> NewCollection()
			{
				return Generics.NewHashSet();
			}

			public override ICollection<T> NewEmptyCollection()
			{
				return Java.Util.Collections.EmptySet();
			}
		}

		[System.Serializable]
		public class TreeSetFactory<T> : CollectionFactory<T>
		{
			private const long serialVersionUID = -3451920268219478134L;

			public override ICollection<T> NewCollection()
			{
				return new TreeSet<T>();
			}

			public override ICollection<T> NewEmptyCollection()
			{
				return Java.Util.Collections.EmptySet();
			}
		}
	}
}
