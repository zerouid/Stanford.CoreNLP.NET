using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// A HookChart is a chart data structure designed for use with the efficient
	/// O(n^4) chart parsing mechanisms targetted at lexicalized parsing, which
	/// were introduced by Eisner and Satta.
	/// </summary>
	/// <author>Dan Klein</author>
	public class HookChart
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(HookChart));

		private IDictionary<HookChart.ChartIndex, IList<Hook>> registeredPreHooks = Generics.NewHashMap();

		private IDictionary<HookChart.ChartIndex, IList<Hook>> registeredPostHooks = Generics.NewHashMap();

		private IDictionary<HookChart.ChartIndex, IList<Edge>> registeredEdgesByLeftIndex = Generics.NewHashMap();

		private IDictionary<HookChart.ChartIndex, IList<Edge>> registeredEdgesByRightIndex = Generics.NewHashMap();

		private IDictionary<HookChart.WeakChartIndex, IList<Edge>> realEdgesByL = Generics.NewHashMap();

		private IDictionary<HookChart.WeakChartIndex, IList<Edge>> realEdgesByR = Generics.NewHashMap();

		private ICollection<HookChart.ChartIndex> builtLIndexes = Generics.NewHashSet();

		private ICollection<HookChart.ChartIndex> builtRIndexes = Generics.NewHashSet();

		private Interner interner = new Interner();

		private class ChartIndex
		{
			public int state;

			public int head;

			public int tag;

			public int loc;

			// either the start or end of an edge
			public override int GetHashCode()
			{
				return state ^ (head << 8) ^ (tag << 16) ^ (loc << 24);
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o is HookChart.ChartIndex)
				{
					HookChart.ChartIndex ci = (HookChart.ChartIndex)o;
					return state == ci.state && head == ci.head && tag == ci.tag && loc == ci.loc;
				}
				return false;
			}
		}

		private class WeakChartIndex
		{
			public int state;

			public int loc;

			// end class ChartIndex
			// either the start or end of an edge
			public override int GetHashCode()
			{
				return state ^ (loc << 16);
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o is HookChart.WeakChartIndex)
				{
					HookChart.WeakChartIndex ci = (HookChart.WeakChartIndex)o;
					return state == ci.state && loc == ci.loc;
				}
				return false;
			}
		}

		private static readonly ICollection<Edge> empty = Java.Util.Collections.EmptyList();

		private static readonly ICollection<Hook> emptyHooks = Java.Util.Collections.EmptyList();

		private HookChart.ChartIndex tempIndex = new HookChart.ChartIndex();

		private HookChart.WeakChartIndex tempWeakIndex = new HookChart.WeakChartIndex();

		// used in many methods to decrease new's
		// used to decrease new's
		public virtual void RegisterEdgeIndexes(Edge edge)
		{
			tempIndex.state = edge.state;
			tempIndex.head = edge.head;
			tempIndex.tag = edge.tag;
			tempIndex.loc = edge.start;
			HookChart.ChartIndex index = (HookChart.ChartIndex)interner.Intern(tempIndex);
			builtLIndexes.Add(index);
			if (index == tempIndex)
			{
				tempIndex = new HookChart.ChartIndex();
				tempIndex.state = edge.state;
				tempIndex.head = edge.head;
				tempIndex.tag = edge.tag;
			}
			//System.out.println("Edge registered: "+edge);
			tempIndex.loc = edge.end;
			index = (HookChart.ChartIndex)interner.Intern(tempIndex);
			if (index == tempIndex)
			{
				tempIndex = new HookChart.ChartIndex();
			}
			builtRIndexes.Add(index);
		}

		public virtual void RegisterRealEdge(Edge edge)
		{
			tempWeakIndex.state = edge.state;
			tempWeakIndex.loc = edge.start;
			HookChart.WeakChartIndex index = (HookChart.WeakChartIndex)interner.Intern(tempWeakIndex);
			Insert(realEdgesByL, index, edge);
			if (index == tempWeakIndex)
			{
				tempWeakIndex = new HookChart.WeakChartIndex();
				tempWeakIndex.state = edge.state;
			}
			tempWeakIndex.loc = edge.end;
			index = (HookChart.WeakChartIndex)interner.Intern(tempWeakIndex);
			Insert(realEdgesByR, index, edge);
			if (index == tempWeakIndex)
			{
				tempWeakIndex = new HookChart.WeakChartIndex();
			}
		}

		public virtual bool IsBuiltL(int state, int start, int head, int tag)
		{
			tempIndex.state = state;
			tempIndex.head = head;
			tempIndex.tag = tag;
			tempIndex.loc = start;
			return builtLIndexes.Contains(tempIndex);
		}

		public virtual bool IsBuiltR(int state, int end, int head, int tag)
		{
			tempIndex.state = state;
			tempIndex.head = head;
			tempIndex.tag = tag;
			tempIndex.loc = end;
			return builtRIndexes.Contains(tempIndex);
		}

		public virtual ICollection<Edge> GetRealEdgesWithL(int state, int start)
		{
			tempWeakIndex.state = state;
			tempWeakIndex.loc = start;
			ICollection<Edge> edges = realEdgesByL[tempWeakIndex];
			if (edges == null)
			{
				return empty;
			}
			return edges;
		}

		public virtual ICollection<Edge> GetRealEdgesWithR(int state, int end)
		{
			tempWeakIndex.state = state;
			tempWeakIndex.loc = end;
			ICollection<Edge> edges = realEdgesByR[tempWeakIndex];
			if (edges == null)
			{
				return empty;
			}
			return edges;
		}

		public virtual ICollection<Hook> GetPreHooks(Edge edge)
		{
			tempIndex.state = edge.state;
			tempIndex.head = edge.head;
			tempIndex.tag = edge.tag;
			tempIndex.loc = edge.end;
			ICollection<Hook> result = registeredPreHooks[tempIndex];
			if (result == null)
			{
				result = emptyHooks;
			}
			//System.out.println("For "+edge+" returning "+result.size()+" pre hooks");
			return result;
		}

		public virtual ICollection<Hook> GetPostHooks(Edge edge)
		{
			tempIndex.state = edge.state;
			tempIndex.head = edge.head;
			tempIndex.tag = edge.tag;
			tempIndex.loc = edge.start;
			ICollection<Hook> result = registeredPostHooks[tempIndex];
			if (result == null)
			{
				result = emptyHooks;
			}
			//System.out.println("For "+edge+" returning "+result.size()+" post hooks");
			return result;
		}

		public virtual ICollection<Edge> GetEdges(Hook hook)
		{
			tempIndex.state = hook.subState;
			tempIndex.head = hook.head;
			tempIndex.tag = hook.tag;
			ICollection<Edge> result;
			if (hook.IsPreHook())
			{
				tempIndex.loc = hook.start;
				result = registeredEdgesByRightIndex[tempIndex];
			}
			else
			{
				tempIndex.loc = hook.end;
				result = registeredEdgesByLeftIndex[tempIndex];
			}
			if (result == null)
			{
				result = empty;
			}
			//System.out.println("For "+hook+" returning "+result.size()+" edges");
			return result;
		}

		// This hacks up a CollectionValuedMap.  Maybe convert to using that class?
		private static void Insert<K, V>(IDictionary<K, IList<V>> map, K index, V item)
		{
			IList<V> list = map[index];
			if (list == null)
			{
				// make default size small: many only ever contain 1 or 2 items
				list = new List<V>(3);
				map[index] = list;
			}
			list.Add(item);
		}

		// log.info("#### HookChart list length is " + list.size());
		public virtual void AddEdge(Edge edge)
		{
			tempIndex.state = edge.state;
			tempIndex.head = edge.head;
			tempIndex.tag = edge.tag;
			// left index
			tempIndex.loc = edge.start;
			HookChart.ChartIndex index = (HookChart.ChartIndex)interner.Intern(tempIndex);
			Insert(registeredEdgesByLeftIndex, index, edge);
			if (index == tempIndex)
			{
				tempIndex = new HookChart.ChartIndex();
				tempIndex.state = edge.state;
				tempIndex.head = edge.head;
				tempIndex.tag = edge.tag;
			}
			tempIndex.loc = edge.end;
			index = (HookChart.ChartIndex)interner.Intern(tempIndex);
			Insert(registeredEdgesByRightIndex, index, edge);
			if (index == tempIndex)
			{
				tempIndex = new HookChart.ChartIndex();
			}
		}

		public virtual void AddHook(Hook hook)
		{
			IDictionary<HookChart.ChartIndex, IList<Hook>> map;
			tempIndex.state = hook.subState;
			tempIndex.head = hook.head;
			tempIndex.tag = hook.tag;
			if (hook.IsPreHook())
			{
				tempIndex.loc = hook.start;
				map = registeredPreHooks;
			}
			else
			{
				tempIndex.loc = hook.end;
				map = registeredPostHooks;
			}
			HookChart.ChartIndex index = (HookChart.ChartIndex)interner.Intern(tempIndex);
			Insert(map, index, hook);
			if (index == tempIndex)
			{
				tempIndex = new HookChart.ChartIndex();
			}
		}
	}
}
