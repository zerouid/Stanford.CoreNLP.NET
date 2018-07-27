using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Utilities for sets.</summary>
	/// <author>Roger Levy, Bill MacCartney</author>
	public class Sets
	{
		private Sets()
		{
		}

		// private to prevent instantiation
		/// <summary>Returns the set cross product of s1 and s2, as <code>Pair</code>s</summary>
		public static ICollection<Pair<E, F>> Cross<E, F>(ICollection<E> s1, ICollection<F> s2)
		{
			ICollection<Pair<E, F>> s = Generics.NewHashSet();
			foreach (E o1 in s1)
			{
				foreach (F o2 in s2)
				{
					s.Add(new Pair<E, F>(o1, o2));
				}
			}
			return s;
		}

		/// <summary>Returns the difference of sets s1 and s2.</summary>
		public static ICollection<E> Diff<E>(ICollection<E> s1, ICollection<E> s2)
		{
			ICollection<E> s = Generics.NewHashSet();
			foreach (E o in s1)
			{
				if (!s2.Contains(o))
				{
					s.Add(o);
				}
			}
			return s;
		}

		/// <summary>Returns the symmetric difference of sets s1 and s2 (i.e.</summary>
		/// <remarks>Returns the symmetric difference of sets s1 and s2 (i.e. all elements that are in only one of the two sets)</remarks>
		public static ICollection<E> SymmetricDiff<E>(ICollection<E> s1, ICollection<E> s2)
		{
			ICollection<E> s = Generics.NewHashSet();
			foreach (E o in s1)
			{
				if (!s2.Contains(o))
				{
					s.Add(o);
				}
			}
			foreach (E o_1 in s2)
			{
				if (!s1.Contains(o_1))
				{
					s.Add(o_1);
				}
			}
			return s;
		}

		/// <summary>Returns the union of sets s1 and s2.</summary>
		public static ICollection<E> Union<E>(ICollection<E> s1, ICollection<E> s2)
		{
			ICollection<E> s = Generics.NewHashSet();
			Sharpen.Collections.AddAll(s, s1);
			Sharpen.Collections.AddAll(s, s2);
			return s;
		}

		/// <summary>Returns the intersection of sets s1 and s2.</summary>
		public static ICollection<E> Intersection<E>(ICollection<E> s1, ICollection<E> s2)
		{
			ICollection<E> s = Generics.NewHashSet();
			Sharpen.Collections.AddAll(s, s1);
			s.RetainAll(s2);
			return s;
		}

		/// <summary>Returns true if there is at least element that is in both s1 and s2.</summary>
		/// <remarks>
		/// Returns true if there is at least element that is in both s1 and s2. Faster
		/// than calling intersection(Set,Set) if you don't need the contents of the
		/// intersection.
		/// </remarks>
		public static bool Intersects<E>(ICollection<E> s1, ICollection<E> s2)
		{
			// *ahem* It would seem that Java already had this method. Hopefully this
			// stub will help people find it better than I did.
			return !Java.Util.Collections.Disjoint(s1, s2);
		}

		/// <summary>Returns the powerset (the set of all subsets) of set s.</summary>
		public static ICollection<ICollection<E>> PowerSet<E>(ICollection<E> s)
		{
			if (s.IsEmpty())
			{
				ICollection<ICollection<E>> h = Generics.NewHashSet();
				ICollection<E> h0 = Generics.NewHashSet(0);
				h.Add(h0);
				return h;
			}
			else
			{
				IEnumerator<E> i = s.GetEnumerator();
				E elt = i.Current;
				s.Remove(elt);
				ICollection<ICollection<E>> pow = PowerSet(s);
				ICollection<ICollection<E>> pow1 = PowerSet(s);
				// for (Iterator j = pow1.iterator(); j.hasNext();) {
				foreach (ICollection<E> t in pow1)
				{
					// Set<E> t = Generics.newHashSet((Set<E>) j.next());
					t.Add(elt);
					pow.Add(t);
				}
				s.Add(elt);
				return pow;
			}
		}

		public static void Main(string[] args)
		{
			ICollection<string> h = Generics.NewHashSet();
			h.Add("a");
			h.Add("b");
			h.Add("c");
			ICollection<ICollection<string>> pow = PowerSet(h);
			System.Console.Out.WriteLine(pow);
		}
	}
}
