using System;
using System.Collections;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>Minimization in n log n a la Hopcroft.</summary>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public class FastExactAutomatonMinimizer : IAutomatonMinimizer
	{
		internal TransducerGraph unminimizedFA = null;

		internal IDictionary memberToBlock = null;

		internal ArrayList splits = null;

		internal bool sparseMode = true;

		internal static readonly object SinkNode = "SINK_NODE";

		internal class Split
		{
			internal ICollection members;

			internal object symbol;

			internal FastExactAutomatonMinimizer.Block block;

			public virtual ICollection GetMembers()
			{
				return members;
			}

			public virtual object GetSymbol()
			{
				return symbol;
			}

			public virtual FastExactAutomatonMinimizer.Block GetBlock()
			{
				return block;
			}

			public Split(ICollection members, object symbol, FastExactAutomatonMinimizer.Block block)
			{
				this.members = members;
				this.symbol = symbol;
				this.block = block;
			}
		}

		internal class Block
		{
			internal ISet members;

			public virtual ISet GetMembers()
			{
				return members;
			}

			public Block(ISet members)
			{
				this.members = members;
			}
		}

		protected internal virtual TransducerGraph GetUnminimizedFA()
		{
			return unminimizedFA;
		}

		protected internal virtual ICollection GetSymbols()
		{
			return GetUnminimizedFA().GetInputs();
		}

		public virtual TransducerGraph MinimizeFA(TransducerGraph unminimizedFA)
		{
			//    System.out.println(unminimizedFA);
			this.unminimizedFA = unminimizedFA;
			this.splits = new ArrayList();
			this.memberToBlock = new Hashtable();
			//new IdentityHashMap(); // TEG: I had to change this b/c some weren't matching
			Minimize();
			return BuildMinimizedFA();
		}

		protected internal virtual TransducerGraph BuildMinimizedFA()
		{
			TransducerGraph minimizedFA = new TransducerGraph();
			TransducerGraph unminimizedFA = GetUnminimizedFA();
			foreach (TransducerGraph.Arc arc in unminimizedFA.GetArcs())
			{
				object source = ProjectNode(arc.GetSourceNode());
				object target = ProjectNode(arc.GetTargetNode());
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

		protected internal virtual object ProjectNode(object node)
		{
			ISet members = GetBlock(node).GetMembers();
			return members;
		}

		protected internal virtual bool HasSplit()
		{
			return splits.Count > 0;
		}

		protected internal virtual FastExactAutomatonMinimizer.Split GetSplit()
		{
			return (FastExactAutomatonMinimizer.Split)splits.RemoveFirst();
		}

		protected internal virtual void AddSplit(FastExactAutomatonMinimizer.Split split)
		{
			splits.AddLast(split);
		}

		//  protected Collection inverseImages(Collection block, Object symbol) {
		//    List inverseImages = new ArrayList();
		//    for (Iterator nodeI = block.iterator(); nodeI.hasNext();) {
		//      Object node = nodeI.next();
		//      inverseImages.addAll(getUnminimizedFA().getInboundArcs(node, symbol));
		//    }
		//    return inverseImages;
		//  }
		protected internal virtual IDictionary SortIntoBlocks(ICollection nodes)
		{
			IDictionary blockToMembers = new IdentityHashMap();
			foreach (object o in nodes)
			{
				FastExactAutomatonMinimizer.Block block = GetBlock(o);
				Maps.PutIntoValueHashSet(blockToMembers, block, o);
			}
			return blockToMembers;
		}

		protected internal virtual void MakeBlock(ICollection members)
		{
			FastExactAutomatonMinimizer.Block block = new FastExactAutomatonMinimizer.Block(new HashSet(members));
			foreach (object member in block.GetMembers())
			{
				if (member != SinkNode)
				{
					//        System.out.println("putting in memberToBlock: " + member + " " + block);
					memberToBlock[member] = block;
				}
			}
			AddSplits(block);
		}

		protected internal virtual void AddSplits(FastExactAutomatonMinimizer.Block block)
		{
			IDictionary symbolToTarget = new Hashtable();
			foreach (object member in block.GetMembers())
			{
				foreach (object o in GetInverseArcs(member))
				{
					TransducerGraph.Arc arc = (TransducerGraph.Arc)o;
					object symbol = arc.GetInput();
					object target = arc.GetTargetNode();
					Maps.PutIntoValueArrayList(symbolToTarget, symbol, target);
				}
			}
			foreach (object symbol_1 in symbolToTarget.Keys)
			{
				AddSplit(new FastExactAutomatonMinimizer.Split((IList)symbolToTarget[symbol_1], symbol_1, block));
			}
		}

		protected internal virtual void RemoveAll(ICollection block, ICollection members)
		{
			// this is because AbstractCollection/Set.removeAll() isn't always linear in members.size()
			foreach (object member in members)
			{
				block.Remove(member);
			}
		}

		protected internal virtual ICollection Difference(ICollection block, ICollection members)
		{
			ISet difference = new HashSet();
			foreach (object member in block)
			{
				if (!members.Contains(member))
				{
					difference.Add(member);
				}
			}
			return difference;
		}

		protected internal virtual FastExactAutomatonMinimizer.Block GetBlock(object o)
		{
			FastExactAutomatonMinimizer.Block result = (FastExactAutomatonMinimizer.Block)memberToBlock[o];
			if (result == null)
			{
				System.Console.Out.WriteLine("No block found for: " + o);
				// debug
				System.Console.Out.WriteLine("But I do have blocks for: ");
				foreach (object o1 in memberToBlock.Keys)
				{
					System.Console.Out.WriteLine(o1);
				}
				throw new Exception("FastExactAutomatonMinimizer: no block found");
			}
			return result;
		}

		protected internal virtual ICollection GetInverseImages(FastExactAutomatonMinimizer.Split split)
		{
			IList inverseImages = new ArrayList();
			object symbol = split.GetSymbol();
			FastExactAutomatonMinimizer.Block block = split.GetBlock();
			foreach (object member in split.GetMembers())
			{
				if (!block.GetMembers().Contains(member))
				{
					continue;
				}
				ICollection arcs = GetInverseArcs(member, symbol);
				foreach (object arc1 in arcs)
				{
					TransducerGraph.Arc arc = (TransducerGraph.Arc)arc1;
					object source = arc.GetSourceNode();
					inverseImages.Add(source);
				}
			}
			return inverseImages;
		}

		protected internal virtual ICollection GetInverseArcs(object member, object symbol)
		{
			if (member != SinkNode)
			{
				return GetUnminimizedFA().GetArcsByTargetAndInput(member, symbol);
			}
			return GetUnminimizedFA().GetArcsByInput(symbol);
		}

		protected internal virtual ICollection GetInverseArcs(object member)
		{
			if (member != SinkNode)
			{
				return GetUnminimizedFA().GetArcsByTarget(member);
			}
			return GetUnminimizedFA().GetArcs();
		}

		protected internal virtual void MakeInitialBlocks()
		{
			// sink block (for if the automaton isn't complete
			MakeBlock(Java.Util.Collections.Singleton(SinkNode));
			// accepting block
			ISet endNodes = GetUnminimizedFA().GetEndNodes();
			MakeBlock(endNodes);
			// main block
			ICollection nonFinalNodes = new HashSet(GetUnminimizedFA().GetNodes());
			nonFinalNodes.RemoveAll(endNodes);
			MakeBlock(nonFinalNodes);
		}

		protected internal virtual void Minimize()
		{
			MakeInitialBlocks();
			while (HasSplit())
			{
				FastExactAutomatonMinimizer.Split split = GetSplit();
				ICollection inverseImages = GetInverseImages(split);
				IDictionary inverseImagesByBlock = SortIntoBlocks(inverseImages);
				foreach (object o in inverseImagesByBlock.Keys)
				{
					FastExactAutomatonMinimizer.Block block = (FastExactAutomatonMinimizer.Block)o;
					ICollection members = (ICollection)inverseImagesByBlock[block];
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

		public static void Main(string[] args)
		{
			/*
			TransducerGraph fa = new TransducerGraph();
			fa.addArc(fa.getStartNode(),"1","a","");
			fa.addArc(fa.getStartNode(),"2","b","");
			fa.addArc(fa.getStartNode(),"3","c","");
			fa.addArc("1","4","a","");
			fa.addArc("2","4","a","");
			fa.addArc("3","5","c","");
			fa.addArc("4",fa.getEndNode(),"c","");
			fa.addArc("5",fa.getEndNode(),"c","");
			System.out.println(fa);
			ExactAutomatonMinimizer minimizer = new ExactAutomatonMinimizer();
			System.out.println(minimizer.minimizeFA(fa));
			*/
			System.Console.Out.WriteLine("Starting minimizer test...");
			IList pathList = new ArrayList();
			TransducerGraph randomFA = TransducerGraph.CreateRandomGraph(5000, 5, 1.0, 5, pathList);
			IList outputs = randomFA.GetPathOutputs(pathList);
			TransducerGraph.IGraphProcessor quasiDeterminizer = new QuasiDeterminizer();
			IAutomatonMinimizer minimizer = new FastExactAutomatonMinimizer();
			TransducerGraph.INodeProcessor ntsp = new TransducerGraph.SetToStringNodeProcessor(new PennTreebankLanguagePack());
			TransducerGraph.IArcProcessor isp = new TransducerGraph.InputSplittingProcessor();
			TransducerGraph.IArcProcessor ocp = new TransducerGraph.OutputCombiningProcessor();
			TransducerGraph detGraph = quasiDeterminizer.ProcessGraph(randomFA);
			TransducerGraph combGraph = new TransducerGraph(detGraph, ocp);
			// combine outputs into inputs
			TransducerGraph result = minimizer.MinimizeFA(combGraph);
			// minimize the thing
			System.Console.Out.WriteLine("Minimized from " + randomFA.GetNodes().Count + " to " + result.GetNodes().Count);
			result = new TransducerGraph(result, ntsp);
			// pull out strings from sets returned by minimizer
			result = new TransducerGraph(result, isp);
			// split outputs from inputs
			IList minOutputs = result.GetPathOutputs(pathList);
			System.Console.Out.WriteLine("Equal? " + outputs.Equals(minOutputs));
		}
		/*
		randomFA = new TransducerGraph(randomFA, new TransducerGraph.OutputCombiningProcessor());
		System.out.print("Starting fast minimization...");
		FastExactAutomatonMinimizer minimizer2 = new FastExactAutomatonMinimizer();
		Timing.startTime();
		TransducerGraph minimizedRandomFA = minimizer2.minimizeFA(randomFA);
		Timing.tick("done. ( "+randomFA.getArcs().size()+" arcs to "+minimizedRandomFA.getArcs().size()+" arcs)");
		minimizedRandomFA = new TransducerGraph(minimizedRandomFA, new TransducerGraph.InputSplittingProcessor());
		List minOutputs = minimizedRandomFA.getPathOutputs(pathList);
		System.out.println("Equal? "+outputs.equals(minOutputs));
		
		System.out.print("Starting slow minimization...");
		ExactAutomatonMinimizer minimizer = new ExactAutomatonMinimizer();
		Timing.startTime();
		minimizedRandomFA = minimizer.minimizeFA(randomFA);
		Timing.tick("done. ( "+randomFA.getArcs().size()+" arcs to "+minimizedRandomFA.getArcs().size()+" arcs)");
		minimizedRandomFA = new TransducerGraph(minimizedRandomFA, new TransducerGraph.InputSplittingProcessor());
		minOutputs = minimizedRandomFA.getPathOutputs(pathList);
		System.out.println("Equal? "+outputs.equals(minOutputs));
		*/
	}
}
