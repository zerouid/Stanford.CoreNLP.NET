using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;




namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>QuasiDeterminizer performing quasi-determinization on TransducerGraphs.</summary>
	/// <author>Teg Grenager</author>
	/// <version>11/02/03</version>
	public class QuasiDeterminizer : TransducerGraph.IGraphProcessor
	{
		public virtual TransducerGraph ProcessGraph(TransducerGraph graph)
		{
			// compute lambda function
			ClassicCounter lambda = ComputeLambda(graph);
			// not destructive
			// do the pushing
			TransducerGraph result = PushLambdas(graph, lambda);
			// creates a new one
			return result;
		}

		/// <summary>Takes time linear in number of arcs.</summary>
		public static ClassicCounter ComputeLambda(TransducerGraph graph)
		{
			ArrayList queue = new ArrayList();
			ClassicCounter lambda = new ClassicCounter();
			ClassicCounter length = new ClassicCounter();
			IDictionary first = new Hashtable();
			ISet nodes = graph.GetNodes();
			foreach (object node in nodes)
			{
				lambda.SetCount(node, 0);
				length.SetCount(node, double.PositiveInfinity);
			}
			ISet endNodes = graph.GetEndNodes();
			foreach (object o in endNodes)
			{
				lambda.SetCount(o, 0);
				length.SetCount(o, 0);
				queue.AddLast(o);
			}
			// Breadth first search
			// get the first node from the queue
			object node_1 = null;
			try
			{
				node_1 = queue.RemoveFirst();
			}
			catch (NoSuchElementException)
			{
			}
			while (node_1 != null)
			{
				double oldLen = length.GetCount(node_1);
				ISet arcs = graph.GetArcsByTarget(node_1);
				if (arcs != null)
				{
					foreach (object arc1 in arcs)
					{
						TransducerGraph.Arc arc = (TransducerGraph.Arc)arc1;
						object newNode = arc.GetSourceNode();
						IComparable a = (IComparable)arc.GetInput();
						double k = ((double)arc.GetOutput());
						double newLen = length.GetCount(newNode);
						if (newLen == double.PositiveInfinity)
						{
							// we are discovering this
							queue.AddLast(newNode);
						}
						IComparable f = (IComparable)first[newNode];
						if (newLen == double.PositiveInfinity || (newLen == oldLen + 1 && a.CompareTo(f) < 0))
						{
							// f can't be null, since we have a newLen
							// we do this to this to newNode when we have new info, possibly many times
							first[newNode] = a;
							// ejecting old one if necessary
							length.SetCount(newNode, oldLen + 1);
							// this may already be the case
							lambda.SetCount(newNode, k + lambda.GetCount(node_1));
						}
					}
				}
				// get a new node from the queue
				node_1 = null;
				try
				{
					node_1 = queue.RemoveFirst();
				}
				catch (NoSuchElementException)
				{
				}
			}
			return lambda;
		}

		/// <summary>Takes time linear in number of arcs.</summary>
		public virtual TransducerGraph PushLambdas(TransducerGraph graph, ClassicCounter lambda)
		{
			TransducerGraph result = null;
			result = graph.Clone();
			// arcs have been copied too so we don't mess up graph
			ICollection<TransducerGraph.Arc> arcs = result.GetArcs();
			foreach (TransducerGraph.Arc arc in arcs)
			{
				double sourceLambda = lambda.GetCount(arc.GetSourceNode());
				double targetLambda = lambda.GetCount(arc.GetTargetNode());
				double oldOutput = ((double)arc.GetOutput());
				double newOutput = oldOutput + targetLambda - sourceLambda;
				arc.SetOutput(newOutput);
			}
			// do initialOutput
			double startLambda = lambda.GetCount(result.GetStartNode());
			if (startLambda != 0.0)
			{
				// add it back to the outbound arcs from start (instead of adding it to the initialOutput)
				ICollection<TransducerGraph.Arc> startArcs = result.GetArcsBySource(result.GetStartNode());
				foreach (TransducerGraph.Arc arc_1 in startArcs)
				{
					double oldOutput = ((double)arc_1.GetOutput());
					double newOutput = oldOutput + startLambda;
					arc_1.SetOutput(newOutput);
				}
			}
			// do finalOutput
			foreach (object o in result.GetEndNodes())
			{
				double endLambda = lambda.GetCount(o);
				if (endLambda != 0.0)
				{
					// subtract it from the inbound arcs to end (instead of subtracting it from the finalOutput)
					ICollection<TransducerGraph.Arc> endArcs = result.GetArcsByTarget(o);
					foreach (TransducerGraph.Arc arc_1 in endArcs)
					{
						double oldOutput = ((double)arc_1.GetOutput());
						double newOutput = oldOutput - endLambda;
						arc_1.SetOutput(newOutput);
					}
				}
			}
			return result;
		}

		public static void Main(string[] args)
		{
			TransducerGraph.IGraphProcessor qd = new QuasiDeterminizer();
			IList pathList = new ArrayList();
			TransducerGraph graph = TransducerGraph.CreateRandomGraph(1000, 10, 1.0, 10, pathList);
			StringBuilder b = new StringBuilder();
			graph.DepthFirstSearch(true, b);
			System.Console.Out.WriteLine(b.ToString());
			System.Console.Out.WriteLine("Done creating random graph");
			//    TransducerGraph.printPathOutputs(pathList, graph, false);
			//System.out.println("Depth first search from start node");
			//TransducerGraph.depthFirstSearch(graph, TransducerGraph.END_NODE, new HashSet(), 0, false);
			TransducerGraph newGraph = qd.ProcessGraph(graph);
			System.Console.Out.WriteLine("Done quasi-determinizing");
			//TransducerGraph.printPathOutputs(pathList, newGraph, false);
			//System.out.println("Depth first search from start node");
			//TransducerGraph.depthFirstSearch(newGraph, TransducerGraph.END_NODE, new HashSet(), 0, false);
			TransducerGraph.TestGraphPaths(graph, newGraph, 1000);
		}
	}
}
