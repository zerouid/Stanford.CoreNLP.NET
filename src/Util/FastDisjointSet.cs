using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Disjoint forest with path compression and union-by-rank.</summary>
	/// <remarks>
	/// Disjoint forest with path compression and union-by-rank.  The set
	/// is unmodifiable except by unions.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <version>4/17/01</version>
	public class FastDisjointSet<T> : IDisjointSet<T>
	{
		internal class Element<Tt>
		{
			internal FastDisjointSet.Element<TT> parent;

			internal int rank;

			internal TT @object;

			internal Element(TT o)
			{
				@object = o;
				rank = 0;
				parent = this;
			}
		}

		private IDictionary<T, FastDisjointSet.Element<T>> objectToElement;

		private static void LinkElements<Ttt>(FastDisjointSet.Element<TTT> e, FastDisjointSet.Element<TTT> f)
		{
			if (e.rank > f.rank)
			{
				f.parent = e;
			}
			else
			{
				e.parent = f;
				if (e.rank == f.rank)
				{
					f.rank++;
				}
			}
		}

		private static FastDisjointSet.Element<TTT> FindElement<Ttt>(FastDisjointSet.Element<TTT> e)
		{
			if (e.parent == e)
			{
				return e;
			}
			FastDisjointSet.Element<TTT> rep = FindElement(e.parent);
			e.parent = rep;
			return rep;
		}

		public virtual T Find(T o)
		{
			FastDisjointSet.Element<T> e = objectToElement[o];
			if (e == null)
			{
				return null;
			}
			FastDisjointSet.Element<T> element = FindElement(e);
			return element.@object;
		}

		public virtual void Union(T a, T b)
		{
			FastDisjointSet.Element<T> e = objectToElement[a];
			FastDisjointSet.Element<T> f = objectToElement[b];
			if (e == null || f == null)
			{
				return;
			}
			if (e == f)
			{
				return;
			}
			LinkElements(FindElement(e), FindElement(f));
		}

		public FastDisjointSet(ICollection<T> objectSet)
		{
			objectToElement = Generics.NewHashMap();
			foreach (T o in objectSet)
			{
				// create an element
				FastDisjointSet.Element<T> e = new FastDisjointSet.Element<T>(o);
				objectToElement[o] = e;
			}
		}
	}
}
