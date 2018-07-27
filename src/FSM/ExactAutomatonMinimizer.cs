using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>Minimization of a FA in n log n a la Hopcroft.</summary>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public class ExactAutomatonMinimizer : IAutomatonMinimizer
	{
		private TransducerGraph unminimizedFA = null;

		private IDictionary<TransducerGraph.Arc, ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>> memberToBlock = null;

		private LinkedList<Pair<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, TransducerGraph.Arc>> activePairs = null;

		private bool sparseMode = false;

		private static readonly TransducerGraph.Arc SinkNode = new TransducerGraph.Arc(null);

		protected internal virtual TransducerGraph GetUnminimizedFA()
		{
			return unminimizedFA;
		}

		protected internal virtual ICollection<object> GetSymbols()
		{
			return GetUnminimizedFA().GetInputs();
		}

		public virtual TransducerGraph MinimizeFA(TransducerGraph unminimizedFA)
		{
			this.unminimizedFA = unminimizedFA;
			this.activePairs = Generics.NewLinkedList();
			this.memberToBlock = Generics.NewHashMap();
			Minimize();
			return BuildMinimizedFA();
		}

		protected internal virtual TransducerGraph BuildMinimizedFA()
		{
			TransducerGraph minimizedFA = new TransducerGraph();
			TransducerGraph unminimizedFA = GetUnminimizedFA();
			foreach (TransducerGraph.Arc arc in unminimizedFA.GetArcs())
			{
				ICollection<TransducerGraph.Arc> source = ProjectNode(arc.GetSourceNode());
				ICollection<TransducerGraph.Arc> target = ProjectNode(arc.GetTargetNode());
				try
				{
					if (minimizedFA.CanAddArc(source, target, arc.GetInput(), arc.GetOutput()))
					{
						minimizedFA.AddArc(source, target, arc.GetInput(), arc.GetOutput());
					}
				}
				catch (Exception)
				{
				}
			}
			//throw new IllegalArgumentException();
			minimizedFA.SetStartNode(ProjectNode(unminimizedFA.GetStartNode()));
			foreach (object o in unminimizedFA.GetEndNodes())
			{
				minimizedFA.SetEndNode(ProjectNode(o));
			}
			return minimizedFA;
		}

		protected internal virtual ICollection<TransducerGraph.Arc> ProjectNode(object node)
		{
			return GetBlock(node).GetMembers();
		}

		protected internal virtual bool HasActivePair()
		{
			return activePairs.Count > 0;
		}

		protected internal virtual Pair<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, object> GetActivePair()
		{
			return activePairs.RemoveFirst();
		}

		protected internal virtual void AddActivePair(Pair<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, TransducerGraph.Arc> pair)
		{
			activePairs.AddLast(pair);
		}

		//  protected Collection inverseImages(Collection block, Object symbol) {
		//    List inverseImages = new ArrayList();
		//    for (Iterator nodeI = block.iterator(); nodeI.hasNext();) {
		//      Object node = nodeI.next();
		//      inverseImages.addAll(getUnminimizedFA().getInboundArcs(node, symbol));
		//    }
		//    return inverseImages;
		//  }
		protected internal virtual IDictionary<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, ICollection<Y>> SortIntoBlocks<Y>(ICollection<Y> nodes)
		{
			IDictionary<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, ICollection<Y>> blockToMembers = Generics.NewHashMap();
			// IdentityHashMap();
			foreach (Y o in nodes)
			{
				ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> block = GetBlock(o);
				if (block == null)
				{
					throw new Exception("got null block");
				}
				Maps.PutIntoValueHashSet(blockToMembers, block, o);
			}
			return blockToMembers;
		}

		protected internal virtual void MakeBlock(ICollection<TransducerGraph.Arc> members)
		{
			ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> block = new ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>(Generics.NewHashSet(members));
			foreach (TransducerGraph.Arc member in block.GetMembers())
			{
				if (member != SinkNode)
				{
					memberToBlock[member] = block;
				}
			}
			foreach (object o in GetSymbols())
			{
				TransducerGraph.Arc symbol = (TransducerGraph.Arc)o;
				AddActivePair(new Pair<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, TransducerGraph.Arc>(block, symbol));
			}
		}

		protected internal static void RemoveAll<_T0>(ICollection<_T0> block, ICollection members)
			where _T0 : TransducerGraph.Arc
		{
			// this is because AbstractCollection/Set.removeAll() isn't always linear in members.size()
			foreach (object member in members)
			{
				block.Remove(member);
			}
		}

		protected internal static ICollection<TransducerGraph.Arc> Difference(ICollection<TransducerGraph.Arc> block, ICollection<TransducerGraph.Arc> members)
		{
			ICollection<TransducerGraph.Arc> difference = Generics.NewHashSet();
			foreach (TransducerGraph.Arc member in block)
			{
				if (!members.Contains(member))
				{
					difference.Add(member);
				}
			}
			return difference;
		}

		protected internal virtual ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> GetBlock(object o)
		{
			ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> result = memberToBlock[o];
			if (result == null)
			{
				throw new Exception("memberToBlock had null block");
			}
			return result;
		}

		protected internal virtual ICollection<object> GetInverseImages(ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> block, object symbol)
		{
			IList<object> inverseImages = new List<object>();
			foreach (TransducerGraph.Arc member in block.GetMembers())
			{
				ICollection<TransducerGraph.Arc> arcs = null;
				if (member != SinkNode)
				{
					arcs = GetUnminimizedFA().GetArcsByTargetAndInput(member, symbol);
				}
				else
				{
					arcs = GetUnminimizedFA().GetArcsByInput(symbol);
					if (!sparseMode)
					{
						arcs = Difference(GetUnminimizedFA().GetArcs(), arcs);
					}
				}
				if (arcs == null)
				{
					continue;
				}
				foreach (TransducerGraph.Arc arc in arcs)
				{
					object source = arc.GetSourceNode();
					inverseImages.Add(source);
				}
			}
			return inverseImages;
		}

		protected internal virtual void MakeInitialBlocks()
		{
			// sink block (for if the automaton isn't complete
			MakeBlock(Java.Util.Collections.Singleton(SinkNode));
			// accepting block
			ICollection<TransducerGraph.Arc> endNodes = GetUnminimizedFA().GetEndNodes();
			MakeBlock(endNodes);
			// main block
			ICollection<TransducerGraph.Arc> nonFinalNodes = Generics.NewHashSet(GetUnminimizedFA().GetNodes());
			nonFinalNodes.RemoveAll(endNodes);
			MakeBlock(nonFinalNodes);
		}

		protected internal virtual void Minimize()
		{
			MakeInitialBlocks();
			while (HasActivePair())
			{
				Pair<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, object> activePair = GetActivePair();
				ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> activeBlock = activePair.First();
				object symbol = activePair.Second();
				ICollection<object> inverseImages = GetInverseImages(activeBlock, symbol);
				IDictionary<ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc>, ICollection<object>> inverseImagesByBlock = SortIntoBlocks(inverseImages);
				foreach (ExactAutomatonMinimizer.ExactBlock<TransducerGraph.Arc> block in inverseImagesByBlock.Keys)
				{
					if (block == null)
					{
						throw new Exception("block was null");
					}
					ICollection members = inverseImagesByBlock[block];
					if (members.Count == 0 || members.Count == block.GetMembers().Count)
					{
						continue;
					}
					if (members.Count > block.GetMembers().Count - members.Count)
					{
						members = Difference(block.GetMembers(), members);
					}
					RemoveAll(block.GetMembers(), members);
					MakeBlock(members);
				}
			}
		}

		public ExactAutomatonMinimizer(bool sparseMode)
		{
			this.sparseMode = sparseMode;
		}

		public ExactAutomatonMinimizer()
			: this(false)
		{
		}

		private class ExactBlock<E> : IBlock<E>
		{
			private readonly ICollection<E> members;

			public virtual ICollection<E> GetMembers()
			{
				return members;
			}

			public ExactBlock(ICollection<E> members)
			{
				if (members == null)
				{
					throw new ArgumentException("tried to create block with null members.");
				}
				this.members = members;
			}

			public override string ToString()
			{
				return "Block: " + members.ToString();
			}
		}

		// end static class ExactBlock
		public static void Main(string[] args)
		{
			TransducerGraph fa = new TransducerGraph();
			fa.AddArc(fa.GetStartNode(), "1", "a", string.Empty);
			fa.AddArc(fa.GetStartNode(), "2", "b", string.Empty);
			fa.AddArc(fa.GetStartNode(), "3", "c", string.Empty);
			fa.AddArc("1", "4", "a", string.Empty);
			fa.AddArc("2", "4", "a", string.Empty);
			fa.AddArc("3", "5", "c", string.Empty);
			fa.AddArc("4", "6", "c", string.Empty);
			fa.AddArc("5", "6", "c", string.Empty);
			fa.SetEndNode("6");
			System.Console.Out.WriteLine(fa);
			ExactAutomatonMinimizer minimizer = new ExactAutomatonMinimizer();
			System.Console.Out.WriteLine(minimizer.MinimizeFA(fa));
			System.Console.Out.WriteLine("Starting...");
			Timing.StartTime();
			TransducerGraph randomFA = TransducerGraph.CreateRandomGraph(100, 10, 1.0, 10, new ArrayList());
			TransducerGraph minimizedRandomFA = minimizer.MinimizeFA(randomFA);
			System.Console.Out.WriteLine(randomFA);
			System.Console.Out.WriteLine(minimizedRandomFA);
			Timing.Tick("done. ( " + randomFA.GetArcs().Count + " arcs to " + minimizedRandomFA.GetArcs().Count + " arcs)");
		}
	}
}
