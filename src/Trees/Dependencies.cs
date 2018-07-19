using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Utilities for Dependency objects.</summary>
	/// <author>Christopher Manning</author>
	public class Dependencies
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Dependencies));

		private Dependencies()
		{
		}

		[System.Serializable]
		public class DependentPuncTagRejectFilter<G, D, N> : IPredicate<IDependency<G, D, N>>
			where G : ILabel
			where D : ILabel
		{
			private IPredicate<string> tagRejectFilter;

			public DependentPuncTagRejectFilter(IPredicate<string> trf)
			{
				// only static methods
				tagRejectFilter = trf;
			}

			public virtual bool Test(IDependency<G, D, N> d)
			{
				/*
				log.info("DRF: Checking " + d + ": hasTag?: " +
				(d.dependent() instanceof HasTag) + "; value: " +
				((d.dependent() instanceof HasTag)? ((HasTag) d.dependent()).tag(): null));
				*/
				if (d == null)
				{
					return false;
				}
				if (!(d.Dependent() is IHasTag))
				{
					return false;
				}
				string tag = ((IHasTag)d.Dependent()).Tag();
				return tagRejectFilter.Test(tag);
			}

			private const long serialVersionUID = -7732189363171164852L;
		}

		[System.Serializable]
		public class DependentPuncWordRejectFilter<G, D, N> : IPredicate<IDependency<G, D, N>>
			where G : ILabel
			where D : ILabel
		{
			private const long serialVersionUID = 1166489968248785287L;

			private readonly IPredicate<string> wordRejectFilter;

			/// <param name="wrf">A filter that rejects punctuation words.</param>
			public DependentPuncWordRejectFilter(IPredicate<string> wrf)
			{
				// end class DependentPuncTagRejectFilter
				// log.info("wrf is " + wrf);
				wordRejectFilter = wrf;
			}

			public virtual bool Test(IDependency<G, D, N> d)
			{
				/*
				log.info("DRF: Checking " + d + ": hasWord?: " +
				(d.dependent() instanceof HasWord) + "; value: " +
				((d.dependent() instanceof HasWord)? ((HasWord) d.dependent()).word(): d.dependent().value()));
				*/
				if (d == null)
				{
					return false;
				}
				string word = null;
				if (d.Dependent() is IHasWord)
				{
					word = ((IHasWord)d.Dependent()).Word();
				}
				if (word == null)
				{
					word = d.Dependent().Value();
				}
				// log.info("Dep: kid is " + ((MapLabel) d.dependent()).toString("value{map}"));
				return wordRejectFilter.Test(word);
			}
		}

		private class ComparatorHolder
		{
			private ComparatorHolder()
			{
			}

			private class DependencyIdxComparator : IComparator<IDependency>
			{
				// end class DependentPuncWordRejectFilter
				// extra class guarantees correct lazy loading (Bloch p.194)
				public virtual int Compare(IDependency dep1, IDependency dep2)
				{
					IHasIndex dep1lab = (IHasIndex)dep1.Dependent();
					IHasIndex dep2lab = (IHasIndex)dep2.Dependent();
					int dep1idx = dep1lab.Index();
					int dep2idx = dep2lab.Index();
					return dep1idx - dep2idx;
				}
			}

			private static readonly IComparator<IDependency> dc = new Dependencies.ComparatorHolder.DependencyIdxComparator();
		}

		public static IDictionary<IndexedWord, IList<TypedDependency>> GovToDepMap(IList<TypedDependency> deps)
		{
			IDictionary<IndexedWord, IList<TypedDependency>> govToDepMap = Generics.NewHashMap();
			foreach (TypedDependency dep in deps)
			{
				IndexedWord gov = dep.Gov();
				IList<TypedDependency> depList = govToDepMap[gov];
				if (depList == null)
				{
					depList = new List<TypedDependency>();
					govToDepMap[gov] = depList;
				}
				depList.Add(dep);
			}
			return govToDepMap;
		}

		private static ICollection<IList<TypedDependency>> GetGovMaxChains(IDictionary<IndexedWord, IList<TypedDependency>> govToDepMap, IndexedWord gov, int depth)
		{
			ICollection<IList<TypedDependency>> depLists = Generics.NewHashSet();
			IList<TypedDependency> children = govToDepMap[gov];
			if (depth > 0 && children != null)
			{
				foreach (TypedDependency child in children)
				{
					IndexedWord childNode = child.Dep();
					if (childNode == null)
					{
						continue;
					}
					ICollection<IList<TypedDependency>> childDepLists = GetGovMaxChains(govToDepMap, childNode, depth - 1);
					if (childDepLists.Count != 0)
					{
						foreach (IList<TypedDependency> childDepList in childDepLists)
						{
							IList<TypedDependency> depList = new List<TypedDependency>(childDepList.Count + 1);
							depList.Add(child);
							Sharpen.Collections.AddAll(depList, childDepList);
							depLists.Add(depList);
						}
					}
					else
					{
						depLists.Add(Arrays.AsList(child));
					}
				}
			}
			return depLists;
		}

		public static ICounter<IList<TypedDependency>> GetTypedDependencyChains(IList<TypedDependency> deps, int maxLength)
		{
			IDictionary<IndexedWord, IList<TypedDependency>> govToDepMap = GovToDepMap(deps);
			ICounter<IList<TypedDependency>> tdc = new ClassicCounter<IList<TypedDependency>>();
			foreach (IndexedWord gov in govToDepMap.Keys)
			{
				ICollection<IList<TypedDependency>> maxChains = GetGovMaxChains(govToDepMap, gov, maxLength);
				foreach (IList<TypedDependency> maxChain in maxChains)
				{
					for (int i = 1; i <= maxChain.Count; i++)
					{
						IList<TypedDependency> chain = maxChain.SubList(0, i);
						tdc.IncrementCount(chain);
					}
				}
			}
			return tdc;
		}

		/// <summary>A Comparator for Dependencies based on their dependent annotation.</summary>
		/// <remarks>
		/// A Comparator for Dependencies based on their dependent annotation.
		/// It will only work if the Labels at the ends of Dependencies have
		/// an index().
		/// </remarks>
		/// <returns>A Comparator for Dependencies</returns>
		public static IComparator<IDependency> DependencyIndexComparator()
		{
			return Dependencies.ComparatorHolder.dc;
		}
	}
}
