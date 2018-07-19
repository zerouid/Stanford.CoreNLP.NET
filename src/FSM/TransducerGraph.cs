using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>
	/// TransducerGraph represents a deterministic finite state automaton
	/// without epsilon transitions.
	/// </summary>
	/// <author>Teg Grenager</author>
	/// <version>11/02/03</version>
	public class TransducerGraph : ICloneable
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Fsm.TransducerGraph));

		public const string EpsilonInput = "EPSILON";

		private const string DefaultStartNode = "START";

		private static readonly Random r = new Random();

		private readonly ICollection<TransducerGraph.Arc> arcs;

		private readonly IDictionary<object, ICollection<TransducerGraph.Arc>> arcsBySource;

		private readonly IDictionary<object, ICollection<TransducerGraph.Arc>> arcsByTarget;

		private readonly IDictionary<object, ICollection<TransducerGraph.Arc>> arcsByInput;

		private IDictionary<Pair<object, object>, TransducerGraph.Arc> arcsBySourceAndInput;

		private IDictionary<object, ICollection<TransducerGraph.Arc>> arcsByTargetAndInput;

		private object startNode;

		private ISet endNodes;

		private bool checkDeterminism = false;

		// TODO: needs some work to make type-safe.
		// (In several places, it takes an Object and does instanceof to see what
		// it is, or assumes one of the alphabets is a Double, etc....)
		// internal data structures
		public virtual void SetDeterminism(bool checkDeterminism)
		{
			this.checkDeterminism = checkDeterminism;
		}

		public TransducerGraph()
		{
			arcs = Generics.NewHashSet();
			arcsBySource = Generics.NewHashMap();
			arcsByTarget = Generics.NewHashMap();
			arcsByInput = Generics.NewHashMap();
			arcsBySourceAndInput = Generics.NewHashMap();
			arcsByTargetAndInput = Generics.NewHashMap();
			endNodes = Generics.NewHashSet();
			SetStartNode(DefaultStartNode);
		}

		public TransducerGraph(Edu.Stanford.Nlp.Fsm.TransducerGraph other)
			: this(other, (TransducerGraph.IArcProcessor)null)
		{
		}

		public TransducerGraph(Edu.Stanford.Nlp.Fsm.TransducerGraph other, TransducerGraph.IArcProcessor arcProcessor)
			: this(other.GetArcs(), other.GetStartNode(), other.GetEndNodes(), arcProcessor, null)
		{
		}

		public TransducerGraph(Edu.Stanford.Nlp.Fsm.TransducerGraph other, TransducerGraph.INodeProcessor nodeProcessor)
			: this(other.GetArcs(), other.GetStartNode(), other.GetEndNodes(), null, nodeProcessor)
		{
		}

		public TransducerGraph(ICollection<TransducerGraph.Arc> newArcs, object startNode, ISet endNodes, TransducerGraph.IArcProcessor arcProcessor, TransducerGraph.INodeProcessor nodeProcessor)
			: this()
		{
			TransducerGraph.IArcProcessor arcProcessor2 = null;
			if (nodeProcessor != null)
			{
				arcProcessor2 = new TransducerGraph.NodeProcessorWrappingArcProcessor(nodeProcessor);
			}
			foreach (TransducerGraph.Arc a in newArcs)
			{
				a = new TransducerGraph.Arc(a);
				// make a copy
				if (arcProcessor != null)
				{
					a = arcProcessor.ProcessArc(a);
				}
				if (arcProcessor2 != null)
				{
					a = arcProcessor2.ProcessArc(a);
				}
				AddArc(a);
			}
			if (nodeProcessor != null)
			{
				this.startNode = nodeProcessor.ProcessNode(startNode);
			}
			else
			{
				this.startNode = startNode;
			}
			if (nodeProcessor != null)
			{
				if (endNodes != null)
				{
					foreach (object o in endNodes)
					{
						this.endNodes.Add(nodeProcessor.ProcessNode(o));
					}
				}
			}
			else
			{
				if (endNodes != null)
				{
					Sharpen.Collections.AddAll(this.endNodes, endNodes);
				}
			}
		}

		/// <summary>Uses the Arcs newArcs.</summary>
		public TransducerGraph(ICollection<TransducerGraph.Arc> newArcs)
			: this(newArcs, null, null, null, null)
		{
		}

		/// <exception cref="Java.Lang.CloneNotSupportedException"/>
		public virtual Edu.Stanford.Nlp.Fsm.TransducerGraph Clone()
		{
			base.MemberwiseClone();
			Edu.Stanford.Nlp.Fsm.TransducerGraph result = new Edu.Stanford.Nlp.Fsm.TransducerGraph(this, (TransducerGraph.IArcProcessor)null);
			return result;
		}

		public virtual ICollection<TransducerGraph.Arc> GetArcs()
		{
			return arcs;
		}

		/// <summary>Just does union of keysets of maps.</summary>
		public virtual ISet GetNodes()
		{
			ISet result = Generics.NewHashSet();
			Sharpen.Collections.AddAll(result, arcsBySource.Keys);
			Sharpen.Collections.AddAll(result, arcsByTarget.Keys);
			return result;
		}

		public virtual ISet GetInputs()
		{
			return arcsByInput.Keys;
		}

		public virtual void SetStartNode(object o)
		{
			startNode = o;
		}

		public virtual void SetEndNode(object o)
		{
			//System.out.println(this + " setting endNode to " + o);
			endNodes.Add(o);
		}

		public virtual object GetStartNode()
		{
			return startNode;
		}

		public virtual ISet GetEndNodes()
		{
			//System.out.println(this + " getting endNode " + endNode);
			return endNodes;
		}

		/// <summary>Returns a Set of type TransducerGraph.Arc.</summary>
		public virtual ICollection<TransducerGraph.Arc> GetArcsByInput(object node)
		{
			return Ensure(arcsByInput[node]);
		}

		/// <summary>Returns a Set of type TransducerGraph.Arc.</summary>
		public virtual ICollection<TransducerGraph.Arc> GetArcsBySource(object node)
		{
			return Ensure(arcsBySource[node]);
		}

		private static ICollection<TransducerGraph.Arc> Ensure(ICollection<TransducerGraph.Arc> s)
		{
			if (s == null)
			{
				return Java.Util.Collections.EmptySet();
			}
			return s;
		}

		/// <summary>Returns a Set of type TransducerGraph.Arc.</summary>
		public virtual ICollection<TransducerGraph.Arc> GetArcsByTarget(object node)
		{
			return Ensure(arcsByTarget[node]);
		}

		/// <summary>Can only be one because automaton is deterministic.</summary>
		public virtual TransducerGraph.Arc GetArcBySourceAndInput(object node, object input)
		{
			return arcsBySourceAndInput[Generics.NewPair(node, input)];
		}

		/// <summary>Returns a Set of type TransducerGraph.Arc.</summary>
		public virtual ICollection<TransducerGraph.Arc> GetArcsByTargetAndInput(object node, object input)
		{
			return Ensure(arcsByTargetAndInput[Generics.NewPair(node, input)]);
		}

		/// <summary>Slow implementation.</summary>
		public virtual TransducerGraph.Arc GetArc(object source, object target)
		{
			ISet arcsFromSource = arcsBySource[source];
			ISet arcsToTarget = arcsByTarget[target];
			ISet result = Generics.NewHashSet();
			Sharpen.Collections.AddAll(result, arcsFromSource);
			result.RetainAll(arcsToTarget);
			// intersection
			if (result.Count < 1)
			{
				return null;
			}
			if (result.Count > 1)
			{
				throw new Exception("Problem in TransducerGraph data structures.");
			}
			// get the only member
			IEnumerator iterator = result.GetEnumerator();
			return (TransducerGraph.Arc)iterator.Current;
		}

		/// <returns>true if and only if it created a new Arc and added it to the graph.</returns>
		public virtual bool AddArc(object source, object target, object input, object output)
		{
			TransducerGraph.Arc a = new TransducerGraph.Arc(source, target, input, output);
			return AddArc(a);
		}

		/// <returns>
		/// true if and only if it added Arc a to the graph.
		/// determinism.
		/// </returns>
		protected internal virtual bool AddArc(TransducerGraph.Arc a)
		{
			object source = a.GetSourceNode();
			object target = a.GetTargetNode();
			object input = a.GetInput();
			if (source == null || target == null || input == null)
			{
				return false;
			}
			// add to data structures
			if (arcs.Contains(a))
			{
				return false;
			}
			// it's new, so add to the rest of the data structures
			// add to source and input map
			Pair p = Generics.NewPair(source, input);
			if (arcsBySourceAndInput.Contains(p) && checkDeterminism)
			{
				throw new Exception("Creating nondeterminism while inserting arc " + a + " because it already has arc " + arcsBySourceAndInput[p] + checkDeterminism);
			}
			arcsBySourceAndInput[p] = a;
			Maps.PutIntoValueHashSet(arcsBySource, source, a);
			p = Generics.NewPair(target, input);
			Maps.PutIntoValueHashSet(arcsByTargetAndInput, p, a);
			Maps.PutIntoValueHashSet(arcsByTarget, target, a);
			Maps.PutIntoValueHashSet(arcsByInput, input, a);
			// add to arcs
			arcs.Add(a);
			return true;
		}

		public virtual bool RemoveArc(TransducerGraph.Arc a)
		{
			object source = a.GetSourceNode();
			object target = a.GetTargetNode();
			object input = a.GetInput();
			// remove from arcs
			if (!arcs.Remove(a))
			{
				return false;
			}
			// remove from arcsBySourceAndInput
			Pair p = Generics.NewPair(source, input);
			if (!arcsBySourceAndInput.Contains(p))
			{
				return false;
			}
			Sharpen.Collections.Remove(arcsBySourceAndInput, p);
			// remove from arcsBySource
			ICollection<TransducerGraph.Arc> s = arcsBySource[source];
			if (s == null)
			{
				return false;
			}
			if (!s.Remove(a))
			{
				return false;
			}
			// remove from arcsByTargetAndInput
			p = Generics.NewPair(target, input);
			s = arcsByTargetAndInput[p];
			if (s == null)
			{
				return false;
			}
			if (!s.Remove(a))
			{
				return false;
			}
			// remove from arcsByTarget
			s = arcsByTarget[target];
			if (s == null)
			{
				return false;
			}
			s = arcsByInput[input];
			if (s == null)
			{
				return false;
			}
			if (!s.Remove(a))
			{
				return false;
			}
			return true;
		}

		public virtual bool CanAddArc(object source, object target, object input, object output)
		{
			TransducerGraph.Arc a = new TransducerGraph.Arc(source, target, input, output);
			if (arcs.Contains(a))
			{
				// inexpensive check
				return false;
			}
			Pair p = Generics.NewPair(source, input);
			return !arcsBySourceAndInput.Contains(p);
		}

		/// <summary>An arc in a finite state transducer.</summary>
		/// <?/>
		/// <?/>
		/// <?/>
		public class Arc<Node, In, Out>
		{
			private NODE sourceNode;

			private NODE targetNode;

			private IN input;

			private OUT output;

			// expensive check
			public virtual NODE GetSourceNode()
			{
				return sourceNode;
			}

			public virtual NODE GetTargetNode()
			{
				return targetNode;
			}

			public virtual IN GetInput()
			{
				return input;
			}

			public virtual OUT GetOutput()
			{
				return output;
			}

			public virtual void SetSourceNode(NODE o)
			{
				sourceNode = o;
			}

			public virtual void SetTargetNode(NODE o)
			{
				targetNode = o;
			}

			public virtual void SetInput(IN o)
			{
				input = o;
			}

			public virtual void SetOutput(OUT o)
			{
				output = o;
			}

			public override int GetHashCode()
			{
				return sourceNode.GetHashCode() ^ (targetNode.GetHashCode() << 16) ^ (input.GetHashCode() << 16);
			}

			public override bool Equals(object o)
			{
				if (o == this)
				{
					return true;
				}
				if (!(o is TransducerGraph.Arc))
				{
					return false;
				}
				TransducerGraph.Arc a = (TransducerGraph.Arc)o;
				return ((sourceNode == null ? a.sourceNode == null : sourceNode.Equals(a.sourceNode)) && (targetNode == null ? a.targetNode == null : targetNode.Equals(a.targetNode)) && (input == null ? a.input == null : input.Equals(a.input)));
			}

			protected internal Arc(TransducerGraph.Arc<NODE, IN, OUT> a)
				: this(a.GetSourceNode(), a.GetTargetNode(), a.GetInput(), a.GetOutput())
			{
			}

			protected internal Arc(NODE sourceNode, NODE targetNode)
				: this(sourceNode, targetNode, null, null)
			{
			}

			protected internal Arc(NODE sourceNode, NODE targetNode, IN input)
				: this(sourceNode, targetNode, input, null)
			{
			}

			protected internal Arc(NODE sourceNode, NODE targetNode, IN input, OUT output)
			{
				// makes a copy of Arc a
				this.sourceNode = sourceNode;
				this.targetNode = targetNode;
				this.input = input;
				this.output = output;
			}

			public override string ToString()
			{
				return sourceNode + " --> " + targetNode + " (" + input + " : " + output + ")";
			}
		}

		public interface IArcProcessor
		{
			// end static class Arc
			/// <summary>Modifies Arc a.</summary>
			TransducerGraph.Arc ProcessArc(TransducerGraph.Arc a);
		}

		public class OutputCombiningProcessor : TransducerGraph.IArcProcessor
		{
			public virtual TransducerGraph.Arc ProcessArc(TransducerGraph.Arc a)
			{
				a = new TransducerGraph.Arc(a);
				a.SetInput(Generics.NewPair(a.GetInput(), a.GetOutput()));
				a.SetOutput(null);
				return a;
			}
		}

		public class InputSplittingProcessor : TransducerGraph.IArcProcessor
		{
			public virtual TransducerGraph.Arc ProcessArc(TransducerGraph.Arc a)
			{
				a = new TransducerGraph.Arc(a);
				Pair p = (Pair)a.GetInput();
				a.SetInput(p.first);
				a.SetOutput(p.second);
				return a;
			}
		}

		public class NodeProcessorWrappingArcProcessor : TransducerGraph.IArcProcessor
		{
			private readonly TransducerGraph.INodeProcessor nodeProcessor;

			public NodeProcessorWrappingArcProcessor(TransducerGraph.INodeProcessor nodeProcessor)
			{
				this.nodeProcessor = nodeProcessor;
			}

			public virtual TransducerGraph.Arc ProcessArc(TransducerGraph.Arc a)
			{
				a = new TransducerGraph.Arc(a);
				a.SetSourceNode(nodeProcessor.ProcessNode(a.GetSourceNode()));
				a.SetTargetNode(nodeProcessor.ProcessNode(a.GetTargetNode()));
				return a;
			}
		}

		public interface INodeProcessor
		{
			object ProcessNode(object node);
		}

		public class SetToStringNodeProcessor : TransducerGraph.INodeProcessor
		{
			private ITreebankLanguagePack tlp;

			public SetToStringNodeProcessor(ITreebankLanguagePack tlp)
			{
				this.tlp = tlp;
			}

			public virtual object ProcessNode(object node)
			{
				ISet s = null;
				if (node is ISet)
				{
					s = (ISet)node;
				}
				else
				{
					if (node is IBlock)
					{
						IBlock b = (IBlock)node;
						s = b.GetMembers();
					}
					else
					{
						throw new Exception("Unexpected node class");
					}
				}
				object sampleNode = s.GetEnumerator().Current;
				if (s.Count == 1)
				{
					if (sampleNode is IBlock)
					{
						return ProcessNode(sampleNode);
					}
					else
					{
						return sampleNode;
					}
				}
				// nope there's a set of things
				if (sampleNode is string)
				{
					string str = (string)sampleNode;
					if (str[0] != '@')
					{
						// passive category...
						return tlp.BasicCategory(str) + "-" + s.GetHashCode();
					}
				}
				// TODO remove b/c there could be collisions
				//          return tlp.basicCategory(str) + "-" + System.identityHashCode(s);
				return "@NodeSet-" + s.GetHashCode();
			}
			// TODO remove b/c there could be collisions
			//      return sampleNode.toString();
		}

		public class ObjectToSetNodeProcessor : TransducerGraph.INodeProcessor
		{
			public virtual object ProcessNode(object node)
			{
				return Java.Util.Collections.Singleton(node);
			}
		}

		public interface IGraphProcessor
		{
			TransducerGraph ProcessGraph(TransducerGraph g);
		}

		public class NormalizingGraphProcessor : TransducerGraph.IGraphProcessor
		{
			internal bool forward = true;

			public NormalizingGraphProcessor(bool forwardNormalization)
			{
				this.forward = forwardNormalization;
			}

			public virtual TransducerGraph ProcessGraph(TransducerGraph g)
			{
				g = new TransducerGraph(g);
				ISet nodes = g.GetNodes();
				foreach (object node in nodes)
				{
					ICollection<TransducerGraph.Arc> myArcs = null;
					if (forward)
					{
						myArcs = g.GetArcsBySource(node);
					}
					else
					{
						myArcs = g.GetArcsByTarget(node);
					}
					// compute a total
					double total = 0.0;
					foreach (TransducerGraph.Arc a in myArcs)
					{
						total += ((double)a.GetOutput());
					}
					// divide each by total
					foreach (TransducerGraph.Arc a_1 in myArcs)
					{
						a_1.SetOutput(Math.Log(((double)a_1.GetOutput()) / total));
					}
				}
				return g;
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			DepthFirstSearch(true, sb);
			return sb.ToString();
		}

		private bool dotWeightInverted = false;

		private void SetDotWeightingInverted(bool inverted)
		{
			dotWeightInverted = true;
		}

		public virtual string AsDOTString()
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(3);
			nf.SetMinimumFractionDigits(1);
			StringBuilder result = new StringBuilder();
			ISet nodes = GetNodes();
			result.Append("digraph G {\n");
			//    result.append("page = \"8.5,11\";\n");
			//    result.append("margin = \"0.25\";\n");
			// Heuristic number of pages
			int sz = arcs.Count;
			int ht = 105;
			int mag = 250;
			while (sz > mag)
			{
				ht += 105;
				mag *= 2;
			}
			int wd = 8;
			mag = 500;
			while (sz > mag)
			{
				wd += 8;
				mag *= 4;
			}
			double htd = ht / 10.0;
			result.Append("size = \"" + wd + "," + htd + "\";\n");
			result.Append("graph [rankdir = \"LR\"];\n");
			result.Append("graph [ranksep = \"0.2\"];\n");
			foreach (object node in nodes)
			{
				string cleanString = StringUtils.FileNameClean(node.ToString());
				result.Append(cleanString);
				result.Append(" [ ");
				//      if (getEndNodes().contains(node)) {
				//        result.append("label=\"" + node.toString() + "\", style=filled, ");
				//      } else
				result.Append("label=\"" + node.ToString() + "\"");
				result.Append("height=\"0.3\", width=\"0.3\"");
				result.Append(" ];\n");
				foreach (TransducerGraph.Arc arc in GetArcsBySource(node))
				{
					result.Append(StringUtils.FileNameClean(arc.GetSourceNode().ToString()));
					result.Append(" -> ");
					result.Append(StringUtils.FileNameClean(arc.GetTargetNode().ToString()));
					result.Append(" [ ");
					result.Append("label=\"");
					result.Append(arc.GetInput());
					result.Append(" : ");
					// result.append(arc.getOutput());
					object output = arc.GetOutput();
					string wt = string.Empty;
					if (output is Number)
					{
						double dd = ((Number)output);
						if (dd == -0.0d)
						{
							result.Append(nf.Format(0.0d));
						}
						else
						{
							result.Append(nf.Format(output));
						}
						int weight;
						if (dotWeightInverted)
						{
							weight = (int)(20.0 - dd);
						}
						else
						{
							weight = (int)dd;
						}
						if (weight > 0)
						{
							wt = ", weight = \"" + weight + "\"";
						}
						if (dotWeightInverted && dd <= 2.0 || (!dotWeightInverted) && dd >= 20.0)
						{
							wt += ", style=bold";
						}
					}
					else
					{
						result.Append(output);
					}
					result.Append("\"");
					result.Append(wt);
					// result.append("fontsize = 14 ");
					if (arc.GetInput().ToString().Equals("EPSILON"))
					{
						result.Append(", style = \"dashed\" ");
					}
					else
					{
						result.Append(", style = \"solid\" ");
					}
					// result.append(", weight = \"" + arc.getOutput() + "\" ");
					result.Append("];\n");
				}
			}
			result.Append("}\n");
			return result.ToString();
		}

		public virtual double InFlow(object node)
		{
			ICollection<TransducerGraph.Arc> arcs = GetArcsByTarget(node);
			return SumOutputs(arcs);
		}

		public virtual double OutFlow(object node)
		{
			ICollection<TransducerGraph.Arc> arcs = GetArcsBySource(node);
			return SumOutputs(arcs);
		}

		private static double SumOutputs(ICollection<TransducerGraph.Arc> arcs)
		{
			double sum = 0.0;
			foreach (TransducerGraph.Arc arc in arcs)
			{
				sum += ((double)arc.GetOutput());
			}
			return sum;
		}

		private double GetSourceTotal(object node)
		{
			double result = 0.0;
			ICollection<TransducerGraph.Arc> arcs = GetArcsBySource(node);
			if (arcs.IsEmpty())
			{
				log.Info("No outbound arcs from node.");
				return result;
			}
			foreach (TransducerGraph.Arc arc in arcs)
			{
				result += ((double)arc.GetOutput());
			}
			return result;
		}

		/// <summary>For testing only.</summary>
		/// <remarks>For testing only.  Doubles combined by addition.</remarks>
		public virtual double GetOutputOfPathInGraph(IList path)
		{
			double score = 0.0;
			object node = GetStartNode();
			foreach (object input in path)
			{
				TransducerGraph.Arc arc = GetArcBySourceAndInput(node, input);
				// next input in path
				if (arc == null)
				{
					System.Console.Out.WriteLine(" NOT ACCEPTED :" + path);
					return double.NegativeInfinity;
				}
				score += ((double)arc.GetOutput());
				node = arc.GetTargetNode();
			}
			return score;
		}

		/// <summary>for testing only.</summary>
		/// <remarks>for testing only. doubles combined by addition.</remarks>
		public virtual IList SampleUniformPathFromGraph()
		{
			IList list = new ArrayList();
			object node = this.GetStartNode();
			ISet endNodes = this.GetEndNodes();
			while (!endNodes.Contains(node))
			{
				IList<TransducerGraph.Arc> arcs = new List<TransducerGraph.Arc>(this.GetArcsBySource(node));
				TransducerGraph.Arc arc = arcs[r.NextInt(arcs.Count)];
				list.Add(arc.GetInput());
				node = arc.GetTargetNode();
			}
			return list;
		}

		private IDictionary<IList, double> SamplePathsFromGraph(int numPaths)
		{
			IDictionary<IList, double> result = Generics.NewHashMap();
			for (int i = 0; i < numPaths; i++)
			{
				IList l = SampleUniformPathFromGraph();
				result[l] = GetOutputOfPathInGraph(l);
			}
			return result;
		}

		/// <summary>For testing only.</summary>
		private static void PrintPathOutputs(IList<IList> pathList, TransducerGraph graph, bool printPaths)
		{
			int i = 0;
			foreach (IList path in pathList)
			{
				if (printPaths)
				{
					foreach (object aPath in path)
					{
						System.Console.Out.Write(aPath + " ");
					}
				}
				else
				{
					System.Console.Out.Write(i++ + " ");
				}
				System.Console.Out.Write("output: " + graph.GetOutputOfPathInGraph(path));
				System.Console.Out.WriteLine();
			}
		}

		/// <summary>For testing only.</summary>
		public virtual IList<double> GetPathOutputs(IList<IList> pathList)
		{
			IList<double> outputList = new List<double>();
			foreach (IList path in pathList)
			{
				outputList.Add(GetOutputOfPathInGraph(path));
			}
			return outputList;
		}

		public static bool TestGraphPaths(TransducerGraph sourceGraph, TransducerGraph testGraph, int numPaths)
		{
			for (int i = 0; i < numPaths; i++)
			{
				IList path = sourceGraph.SampleUniformPathFromGraph();
				double score = sourceGraph.GetOutputOfPathInGraph(path);
				double newScore = testGraph.GetOutputOfPathInGraph(path);
				if ((score - newScore) / (score + newScore) > 1e-10)
				{
					System.Console.Out.WriteLine("Problem: " + score + " vs. " + newScore + " on " + path);
					return false;
				}
			}
			return true;
		}

		/// <summary>For testing only.</summary>
		/// <remarks>For testing only.  Doubles combined by multiplication.</remarks>
		private bool CanAddPath(IList path)
		{
			object node = this.GetStartNode();
			for (int j = 0; j < path.Count - 1; j++)
			{
				object input = path[j];
				TransducerGraph.Arc arc = this.GetArcBySourceAndInput(node, input);
				// next input in path
				if (arc == null)
				{
					return true;
				}
				node = arc.GetTargetNode();
			}
			object input_1 = path[path.Count - 1];
			// last element
			TransducerGraph.Arc arc_1 = this.GetArcBySourceAndInput(node, input_1);
			// next input in path
			if (arc_1 == null)
			{
				return true;
			}
			else
			{
				return GetEndNodes().Contains(arc_1.GetTargetNode());
			}
		}

		/// <summary>
		/// If markovOrder is zero, we always transition back to the start state
		/// If markovOrder is negative, we assume that it is infinite
		/// </summary>
		public static TransducerGraph CreateGraphFromPaths(IList paths, int markovOrder)
		{
			ClassicCounter pathCounter = new ClassicCounter();
			foreach (object o in paths)
			{
				pathCounter.IncrementCount(o);
			}
			return CreateGraphFromPaths(pathCounter, markovOrder);
		}

		public static TransducerGraph CreateGraphFromPaths<T>(ClassicCounter<IList<T>> pathCounter, int markovOrder)
		{
			TransducerGraph graph = new TransducerGraph();
			// empty
			foreach (IList<T> path in pathCounter.KeySet())
			{
				double count = pathCounter.GetCount(path);
				AddOnePathToGraph(path, count, markovOrder, graph);
			}
			return graph;
		}

		// assumes that the path already has EPSILON as the last element.
		public static void AddOnePathToGraph(IList path, double count, int markovOrder, TransducerGraph graph)
		{
			object source = graph.GetStartNode();
			for (int j = 0; j < path.Count; j++)
			{
				object input = path[j];
				TransducerGraph.Arc a = graph.GetArcBySourceAndInput(source, input);
				if (a != null)
				{
					// increment the arc weight
					a.output = ((double)a.output) + count;
				}
				else
				{
					object target;
					if (input.Equals(TransducerGraph.EpsilonInput))
					{
						target = "END";
					}
					else
					{
						// to ensure they all share the same end node
						if (markovOrder == 0)
						{
							// we all transition back to the same state
							target = source;
						}
						else
						{
							if (markovOrder > 0)
							{
								// the state is described by the partial history
								target = path.SubList((j < markovOrder ? 0 : j - markovOrder + 1), j + 1);
							}
							else
							{
								// the state is described by the full history
								target = path.SubList(0, j + 1);
							}
						}
					}
					double output = count;
					a = new TransducerGraph.Arc(source, target, input, output);
					graph.AddArc(a);
				}
				source = a.GetTargetNode();
			}
			graph.SetEndNode(source);
		}

		/// <summary>For testing only.</summary>
		/// <remarks>
		/// For testing only. All paths will be added to pathList as Lists.
		/// // generate a bunch of paths through the graph with the input alphabet
		/// // and create new nodes for each one.
		/// </remarks>
		public static TransducerGraph CreateRandomGraph(int numPaths, int pathLengthMean, double pathLengthVariance, int numInputs, IList pathList)
		{
			// compute the path length. Draw from a normal distribution
			int pathLength = (int)(r.NextGaussian() * pathLengthVariance + pathLengthMean);
			for (int i = 0; i < numPaths; i++)
			{
				// make a path
				IList path = new ArrayList();
				for (int j = 0; j < pathLength; j++)
				{
					string input = int.ToString(r.NextInt(numInputs));
					path.Add(input);
				}
				// TODO: createRandomPaths had the following difference:
				// we're done, add one more arc to get to the endNode.
				//input = TransducerGraph.EPSILON_INPUT;
				//path.add(input);
				pathList.Add(path);
			}
			return CreateGraphFromPaths(pathList, -1);
		}

		public static IList CreateRandomPaths(int numPaths, int pathLengthMean, double pathLengthVariance, int numInputs)
		{
			IList pathList = new ArrayList();
			// make a bunch of paths, randomly
			// compute the path length. Draw from a normal distribution
			int pathLength = (int)(r.NextGaussian() * pathLengthVariance + pathLengthMean);
			for (int i = 0; i < numPaths; i++)
			{
				// make a path
				IList<string> path = new List<string>();
				string input;
				for (int j = 0; j < pathLength; j++)
				{
					input = int.ToString(r.NextInt(numInputs));
					path.Add(input);
				}
				// we're done, add one more arc to get to the endNode.
				input = TransducerGraph.EpsilonInput;
				path.Add(input);
				pathList.Add(path);
			}
			return pathList;
		}

		public virtual void DepthFirstSearch(bool forward, StringBuilder b)
		{
			if (forward)
			{
				DepthFirstSearchHelper(GetStartNode(), new HashSet(), 0, true, b);
			}
			else
			{
				foreach (object o in GetEndNodes())
				{
					DepthFirstSearchHelper(o, new HashSet(), 0, false, b);
				}
			}
		}

		/// <summary>For testing only.</summary>
		private void DepthFirstSearchHelper(object node, ISet marked, int level, bool forward, StringBuilder b)
		{
			if (marked.Contains(node))
			{
				return;
			}
			marked.Add(node);
			ICollection<TransducerGraph.Arc> arcs;
			if (forward)
			{
				arcs = this.GetArcsBySource(node);
			}
			else
			{
				arcs = this.GetArcsByTarget(node);
			}
			if (arcs == null)
			{
				return;
			}
			foreach (TransducerGraph.Arc newArc in arcs)
			{
				// print it out
				for (int i = 0; i < level; i++)
				{
					b.Append("  ");
				}
				if (GetEndNodes().Contains(newArc.GetTargetNode()))
				{
					b.Append(newArc + " END\n");
				}
				else
				{
					b.Append(newArc + "\n");
				}
				if (forward)
				{
					DepthFirstSearchHelper(newArc.GetTargetNode(), marked, level + 1, forward, b);
				}
				else
				{
					DepthFirstSearchHelper(newArc.GetSourceNode(), marked, level + 1, forward, b);
				}
			}
		}

		/// <summary>For testing only.</summary>
		public static void Main(string[] args)
		{
			IList pathList = new ArrayList();
			TransducerGraph graph = CreateRandomGraph(1000, 10, 0.0, 10, pathList);
			System.Console.Out.WriteLine("Done creating random graph");
			PrintPathOutputs(pathList, graph, true);
			System.Console.Out.WriteLine("Depth first search from start node");
			StringBuilder b = new StringBuilder();
			graph.DepthFirstSearch(true, b);
			System.Console.Out.WriteLine(b.ToString());
			b = new StringBuilder();
			System.Console.Out.WriteLine("Depth first search back from end node");
			graph.DepthFirstSearch(false, b);
			System.Console.Out.WriteLine(b.ToString());
		}
	}
}
