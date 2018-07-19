using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Graph
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DirectedMultiGraphTest
	{
		internal DirectedMultiGraph<int, string> graph = new DirectedMultiGraph<int, string>();

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			graph.Clear();
			graph.Add(1, 2, "1->2");
			graph.Add(2, 3, "2->3");
			graph.Add(1, 4, "1->4");
			// cyclic
			graph.Add(4, 1, "4->1");
			graph.AddVertex(5);
			graph.Add(5, 6, "5->6");
			graph.Add(7, 6, "7->6");
			graph.AddVertex(7);
			graph.AddVertex(8);
			graph.Add(9, 10, "9->10");
		}

		/// <summary>Check that the graph's incoming and outgoing edges are consistent.</summary>
		public virtual void CheckGraphConsistency<V, E>(DirectedMultiGraph<V, E> graph)
		{
			IDictionary<V, IDictionary<V, IList<E>>> incoming = graph.incomingEdges;
			IDictionary<V, IDictionary<V, IList<E>>> outgoing = graph.outgoingEdges;
			foreach (V source in incoming.Keys)
			{
				foreach (V target in incoming[source].Keys)
				{
					NUnit.Framework.Assert.IsTrue(outgoing.Contains(target));
					NUnit.Framework.Assert.IsTrue(outgoing[target].Contains(source));
					NUnit.Framework.Assert.AreEqual(incoming[source][target], outgoing[target][source]);
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestForm()
		{
			System.Console.Out.WriteLine("Graph is \n" + graph.ToString());
			NUnit.Framework.Assert.AreEqual(graph.GetNumVertices(), 10);
			NUnit.Framework.Assert.AreEqual(graph.GetNumEdges(), 7);
		}

		[NUnit.Framework.Test]
		public virtual void TestRemove()
		{
			graph.RemoveVertex(2);
			System.Console.Out.WriteLine("after deleting 2\n" + graph.ToString());
			NUnit.Framework.Assert.AreEqual(graph.GetNumVertices(), 9);
			NUnit.Framework.Assert.AreEqual(graph.GetNumEdges(), 5);
			// vertex 11 doesn't exist in the graph and thus this function should return
			// false
			NUnit.Framework.Assert.IsFalse(graph.RemoveVertex(11));
			SetUp();
			NUnit.Framework.Assert.IsTrue(graph.RemoveEdges(2, 3));
			System.Console.Out.WriteLine("after deleting 2->3 edge\n" + graph.ToString());
			NUnit.Framework.Assert.AreEqual(graph.GetNumVertices(), 10);
			NUnit.Framework.Assert.AreEqual(graph.GetNumEdges(), 6);
			NUnit.Framework.Assert.IsFalse(graph.RemoveEdges(2, 3));
		}

		[NUnit.Framework.Test]
		public virtual void TestDelZeroDegreeNodes()
		{
			graph.RemoveVertex(2);
			graph.RemoveZeroDegreeNodes();
			System.Console.Out.WriteLine("after deleting 2, and then zero deg nodes \n" + graph.ToString());
			NUnit.Framework.Assert.AreEqual(graph.GetNumVertices(), 7);
			NUnit.Framework.Assert.AreEqual(graph.GetNumEdges(), 5);
		}

		[NUnit.Framework.Test]
		public virtual void TestShortestPathDirectionSensitiveNodes()
		{
			IList<int> nodes = graph.GetShortestPath(1, 3, true);
			System.Console.Out.WriteLine("directed path nodes btw 1 and 3 is " + nodes);
			NUnit.Framework.Assert.AreEqual(3, nodes.Count);
			NUnit.Framework.Assert.AreEqual(1, nodes[0]);
			NUnit.Framework.Assert.AreEqual(2, nodes[1]);
			NUnit.Framework.Assert.AreEqual(3, nodes[2]);
			nodes = graph.GetShortestPath(2, 4, true);
			System.Console.Out.WriteLine("directed path nodes btw 2 and 4 is " + nodes);
			NUnit.Framework.Assert.AreEqual(null, nodes);
			nodes = graph.GetShortestPath(1, 5, true);
			System.Console.Out.WriteLine("directed path nodes btw 1 and 5 is " + nodes);
			NUnit.Framework.Assert.AreEqual(null, nodes);
		}

		[NUnit.Framework.Test]
		public virtual void TestShortedPathDirectionSensitiveEdges()
		{
			IList<string> edges = graph.GetShortestPathEdges(1, 3, true);
			System.Console.Out.WriteLine("directed path edges btw 1 and 3 is " + edges);
			NUnit.Framework.Assert.AreEqual(2, edges.Count);
			NUnit.Framework.Assert.AreEqual("1->2", edges[0]);
			NUnit.Framework.Assert.AreEqual("2->3", edges[1]);
			edges = graph.GetShortestPathEdges(2, 4, true);
			System.Console.Out.WriteLine("directed path edges btw 2 and 4 is " + edges);
			NUnit.Framework.Assert.AreEqual(null, edges);
			edges = graph.GetShortestPathEdges(1, 5, true);
			System.Console.Out.WriteLine("directed path edges btw 1 and 5 is " + edges);
			NUnit.Framework.Assert.AreEqual(null, edges);
		}

		[NUnit.Framework.Test]
		public virtual void TestShortestPathDirectionInsensitiveNodes()
		{
			IList<int> nodes = graph.GetShortestPath(1, 3);
			System.Console.Out.WriteLine("undirected nodes btw 1 and 3 is " + nodes);
			NUnit.Framework.Assert.AreEqual(3, nodes.Count);
			NUnit.Framework.Assert.AreEqual(1, nodes[0]);
			NUnit.Framework.Assert.AreEqual(2, nodes[1]);
			NUnit.Framework.Assert.AreEqual(3, nodes[2]);
			nodes = graph.GetShortestPath(2, 4);
			System.Console.Out.WriteLine("undirected nodes btw 2 and 4 is " + nodes);
			NUnit.Framework.Assert.AreEqual(3, nodes.Count);
			NUnit.Framework.Assert.AreEqual(2, nodes[0]);
			NUnit.Framework.Assert.AreEqual(1, nodes[1]);
			NUnit.Framework.Assert.AreEqual(4, nodes[2]);
			nodes = graph.GetShortestPath(1, 5);
			System.Console.Out.WriteLine("undirected nodes btw 1 and 5 is " + nodes);
			NUnit.Framework.Assert.AreEqual(null, nodes);
		}

		[NUnit.Framework.Test]
		public virtual void TestShortestPathDirectionInsensitiveEdges()
		{
			IList<string> edges = graph.GetShortestPathEdges(1, 3, false);
			System.Console.Out.WriteLine("undirected edges btw 1 and 3 is " + edges);
			NUnit.Framework.Assert.AreEqual(2, edges.Count);
			NUnit.Framework.Assert.AreEqual("1->2", edges[0]);
			NUnit.Framework.Assert.AreEqual("2->3", edges[1]);
			edges = graph.GetShortestPathEdges(2, 4, false);
			System.Console.Out.WriteLine("undirected edges btw 2 and 4 is " + edges);
			NUnit.Framework.Assert.AreEqual(2, edges.Count);
			NUnit.Framework.Assert.AreEqual("1->2", edges[0]);
			NUnit.Framework.Assert.AreEqual("1->4", edges[1]);
			edges = graph.GetShortestPathEdges(1, 5, false);
			System.Console.Out.WriteLine("undirected edges btw 2 and 4 is " + edges);
			NUnit.Framework.Assert.AreEqual(null, edges);
		}

		[NUnit.Framework.Test]
		public virtual void TestConnectedComponents()
		{
			System.Console.Out.WriteLine("graph is " + graph.ToString());
			IList<ICollection<int>> ccs = graph.GetConnectedComponents();
			foreach (ICollection<int> cc in ccs)
			{
				System.Console.Out.WriteLine("Connected component: " + cc);
			}
			NUnit.Framework.Assert.AreEqual(ccs.Count, 4);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(ccs[0]), Arrays.AsList(1, 2, 3, 4));
		}

		[NUnit.Framework.Test]
		public virtual void TestEdgesNodes()
		{
			NUnit.Framework.Assert.IsTrue(graph.IsEdge(1, 2));
			NUnit.Framework.Assert.IsFalse(graph.IsEdge(2, 1));
			NUnit.Framework.Assert.IsTrue(graph.IsNeighbor(2, 1));
			IList<string> incomingEdges = graph.GetEdges(4, 1);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(incomingEdges), Arrays.AsList("4->1"));
			ICollection<int> neighbors = graph.GetNeighbors(2);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(neighbors), CollectionUtils.Sorted(Arrays.AsList(1, 3)));
			ICollection<int> parents = graph.GetParents(4);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(parents), CollectionUtils.Sorted(Arrays.AsList(1)));
			parents = graph.GetParents(1);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(parents), CollectionUtils.Sorted(Arrays.AsList(4)));
			parents = graph.GetParents(6);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.Sorted(parents), CollectionUtils.Sorted(Arrays.AsList(5, 7)));
		}

		[NUnit.Framework.Test]
		public virtual void TestAdd()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			NUnit.Framework.Assert.AreEqual(0, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.AddVertex(1);
			NUnit.Framework.Assert.AreEqual(1, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(1, g.GetNumEdges());
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			// repeated adds should not clobber vertices or edges
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			g.AddVertex(3);
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			// regardless of what our overwriting edges policy is, this really
			// ought to be allowed
			g.Add(1, 3, "bar");
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(3, g.GetNumEdges());
			g.Add(2, 3, "foo");
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(4, g.GetNumEdges());
			g.Add(2, 3, "baz");
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(5, g.GetNumEdges());
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(6, g.GetNumEdges());
		}

		[NUnit.Framework.Test]
		public virtual void TestSmallAddRemove()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(1, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.IsEdge(1, 2));
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.IsFalse(g.IsEdge(1, 2));
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.IsTrue(g.IsEdge(1, 2));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.IsEdge(1, 2));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(1, g.GetNumEdges());
			g.RemoveEdge(1, 2, "bar");
			NUnit.Framework.Assert.IsFalse(g.IsEdge(1, 2));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
		}

		[NUnit.Framework.Test]
		public virtual void TestSmallRemoveVertex()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			g.RemoveVertex(2);
			NUnit.Framework.Assert.AreEqual(1, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.IsEdge(1, 2));
			NUnit.Framework.Assert.IsFalse(g.IsEdge(2, 1));
		}

		/// <summary>specifically test the method "containsVertex".</summary>
		/// <remarks>
		/// specifically test the method "containsVertex".  if previous tests
		/// passed, then containsVertex is the only new thing tested here
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestSmallContains()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.ContainsVertex(1));
			NUnit.Framework.Assert.IsTrue(g.ContainsVertex(2));
			NUnit.Framework.Assert.IsFalse(g.ContainsVertex(3));
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.ContainsVertex(1));
			NUnit.Framework.Assert.IsTrue(g.ContainsVertex(2));
			NUnit.Framework.Assert.IsFalse(g.ContainsVertex(3));
			g.RemoveVertex(2);
			NUnit.Framework.Assert.AreEqual(1, g.GetNumVertices());
			NUnit.Framework.Assert.IsTrue(g.ContainsVertex(1));
			NUnit.Framework.Assert.IsFalse(g.ContainsVertex(2));
			NUnit.Framework.Assert.IsFalse(g.ContainsVertex(3));
		}

		[NUnit.Framework.Test]
		public virtual void TestAddRemove()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			g.AddVertex(3);
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(6, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.IsEdge(2, 3));
			g.RemoveEdges(2, 3);
			NUnit.Framework.Assert.IsFalse(g.IsEdge(2, 3));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(4, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.IsEdge(1, 2));
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.IsEdge(1, 2));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(3, g.GetNumEdges());
			g.RemoveEdge(1, 2, "bar");
			NUnit.Framework.Assert.IsFalse(g.IsEdge(1, 2));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.RemoveEdge(3, 1, "bar"));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.RemoveEdge(1, 3, "bar"));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(1, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.RemoveEdge(2, 4, "arg"));
			NUnit.Framework.Assert.IsTrue(g.RemoveEdge(2, 4, "baz"));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.RemoveVertex(5));
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.RemoveVertex(4));
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.RemoveVertex(4));
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			IList<int> vertices = Arrays.AsList(3, 4);
			NUnit.Framework.Assert.IsTrue(g.RemoveVertices(vertices));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.RemoveVertices(vertices));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.Clear();
			NUnit.Framework.Assert.AreEqual(0, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			// reuse the graph, run some more tests
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			g.AddVertex(3);
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(6, g.GetNumEdges());
			NUnit.Framework.Assert.IsTrue(g.RemoveVertices(vertices));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(2, g.GetNumEdges());
		}

		[NUnit.Framework.Test]
		public virtual void TestAddRemove2()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.Clear();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			g.AddVertex(3);
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(6, g.GetNumEdges());
			IList<int> vertices = Arrays.AsList(2, 4);
			NUnit.Framework.Assert.IsTrue(g.RemoveVertices(vertices));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(1, g.GetNumEdges());
		}

		[NUnit.Framework.Test]
		public virtual void TestAddRemove3()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.Clear();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			g.AddVertex(3);
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			g.Add(2, 4, "baz");
			g.RemoveEdges(2, 3);
			g.RemoveEdge(1, 2, "foo");
			g.RemoveEdge(1, 2, "bar");
			g.RemoveEdge(1, 3, "bar");
			g.RemoveEdge(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(4, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			g.RemoveVertex(4);
			NUnit.Framework.Assert.AreEqual(3, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			IList<int> vertices = Arrays.AsList(2, 4);
			NUnit.Framework.Assert.IsTrue(g.RemoveVertices(vertices));
			NUnit.Framework.Assert.AreEqual(2, g.GetNumVertices());
			NUnit.Framework.Assert.AreEqual(0, g.GetNumEdges());
			NUnit.Framework.Assert.IsFalse(g.IsEmpty());
			g.RemoveVertex(1);
			NUnit.Framework.Assert.IsFalse(g.IsEmpty());
			g.RemoveVertex(3);
			NUnit.Framework.Assert.IsTrue(g.IsEmpty());
		}

		[NUnit.Framework.Test]
		public virtual void TestGetAllVertices()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetAllVertices());
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2)), g.GetAllVertices());
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			g.AddVertex(3);
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2, 3)), g.GetAllVertices());
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2, 3, 4)), g.GetAllVertices());
			g.RemoveEdges(2, 3);
			g.RemoveEdge(1, 2, "foo");
			g.RemoveEdge(1, 2, "bar");
			g.RemoveEdge(1, 3, "bar");
			g.RemoveEdge(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2, 3, 4)), g.GetAllVertices());
			g.RemoveVertex(4);
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2, 3)), g.GetAllVertices());
			g.Add(1, 4, "blah");
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2, 3, 4)), g.GetAllVertices());
			g.RemoveZeroDegreeNodes();
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 4)), g.GetAllVertices());
		}

		/// <summary>
		/// Test the methods that return the sets of neighbors, parents &amp;
		/// children for a variety of add and remove cases
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestNeighbors()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(null, g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(null, g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(null, g.GetNeighbors(2));
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetNeighbors(2));
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(2));
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(2));
			g.AddVertex(3);
			g.Add(1, 3, "bar");
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 3)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2)), g.GetNeighbors(3));
			g.Add(2, 4, "baz");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3, 4)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 3, 4)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 2)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(4));
			g.RemoveEdges(2, 3);
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(4)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 4)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(4));
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(4)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 4)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(4));
			g.RemoveEdge(1, 2, "bar");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(4)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(4)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(4));
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2, 3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(4)), g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1, 4)), g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(2)), g.GetNeighbors(4));
			g.RemoveVertex(2);
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3)), g.GetChildren(1));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(3)), g.GetNeighbors(1));
			NUnit.Framework.Assert.AreEqual(null, g.GetParents(2));
			NUnit.Framework.Assert.AreEqual(null, g.GetChildren(2));
			NUnit.Framework.Assert.AreEqual(null, g.GetNeighbors(2));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetParents(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(3));
			NUnit.Framework.Assert.AreEqual(new HashSet<int>(Arrays.AsList(1)), g.GetNeighbors(3));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetParents(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetChildren(4));
			NUnit.Framework.Assert.AreEqual(Java.Util.Collections.EmptySet(), g.GetNeighbors(4));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsNeighbor()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 1));
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			g.AddVertex(3);
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(3, 2));
			g.Add(1, 3, "bar");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(3, 2));
			g.Add(2, 3, "foo");
			g.Add(2, 3, "baz");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 2));
			g.RemoveEdge(1, 2, "foo");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 2));
			g.RemoveEdge(1, 2, "bar");
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 2));
			g.Add(1, 2, "foo");
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 2));
			g.RemoveEdges(1, 2);
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 2));
			g.RemoveVertex(2);
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(1, 2));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 1));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(1, 3));
			NUnit.Framework.Assert.IsTrue(g.IsNeighbor(3, 1));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(2, 3));
			NUnit.Framework.Assert.IsFalse(g.IsNeighbor(3, 2));
		}

		/// <summary>
		/// Test the getInDegree() and getOutDegree() methods using a couple
		/// different graph shapes
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestDegree()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			g.Add(1, 2, "foo");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(2));
			g.Add(1, 2, "bar");
			NUnit.Framework.Assert.AreEqual(2, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(2));
			g.Add(1, 3, "foo");
			NUnit.Framework.Assert.AreEqual(3, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.Add(2, 3, "foo");
			NUnit.Framework.Assert.AreEqual(3, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(3));
			g.RemoveVertex(2);
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.Add(2, 1, "foo");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.Add(2, 1, "bar");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(2, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.Add(2, 1, "baz");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(3, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(3, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.RemoveEdge(2, 1, "blah");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(3, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(3, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.RemoveEdge(2, 1, "bar");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(2, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.RemoveEdges(2, 1);
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(1, g.GetInDegree(3));
			g.Add(2, 3, "bar");
			g.Add(3, 4, "bar");
			g.Add(3, 5, "bar");
			g.Add(3, 6, "bar");
			g.Add(3, 7, "bar");
			g.Add(3, 8, "bar");
			g.Add(3, 9, "bar");
			g.Add(3, 10, "bar");
			g.Add(3, 10, "foo");
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(1));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(1));
			NUnit.Framework.Assert.AreEqual(1, g.GetOutDegree(2));
			NUnit.Framework.Assert.AreEqual(0, g.GetInDegree(2));
			NUnit.Framework.Assert.AreEqual(8, g.GetOutDegree(3));
			NUnit.Framework.Assert.AreEqual(2, g.GetInDegree(3));
		}

		public virtual void CheckIterator<E>(IEnumerable<E> edges, params E[] expected)
		{
			ICollection<E> expectedSet = new HashSet<E>(Arrays.AsList(expected));
			ICollection<E> foundSet = new HashSet<E>();
			foreach (E edge in edges)
			{
				if (foundSet.Contains(edge))
				{
					throw new AssertionError("Received two copies of " + edge + " when running an edge iterator");
				}
				foundSet.Add(edge);
			}
			NUnit.Framework.Assert.AreEqual(expectedSet, foundSet);
		}

		[NUnit.Framework.Test]
		public virtual void TestIterables()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1));
			CheckIterator(g.IncomingEdgeIterable(2));
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable());
			g.Add(1, 2, "1-2");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable(), "1-2");
			g.Add(1, 2, "1-2b");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b");
			g.Add(1, 3, "1-3");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b", "1-3");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b", "1-3");
			CheckIterator(g.GetEdges(1, 2), "1-2", "1-2b");
			CheckIterator(g.GetEdges(1, 3), "1-3");
			g.Add(1, 3, "1-3b");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b", "1-3", "1-3b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3", "1-3b");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b", "1-3", "1-3b");
			g.RemoveEdge(1, 3, "1-3b");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b", "1-3");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b", "1-3");
			g.RemoveEdge(1, 3, "1-3b");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b", "1-3");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b", "1-3");
			g.RemoveEdge(1, 3, "1-3");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3));
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b");
			g.Add(1, 3, "1-3");
			g.Add(1, 3, "1-3b");
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2", "1-2b", "1-3", "1-3b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3", "1-3b");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-2", "1-2b", "1-3", "1-3b");
			g.Add(1, 1, "1-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2", "1-2b", "1-3", "1-3b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.IncomingEdgeIterable(3), "1-3", "1-3b");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "1-2b", "1-3", "1-3b");
			g.Add(2, 1, "2-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2", "1-2b", "1-3", "1-3b");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2", "1-2b");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.IncomingEdgeIterable(3), "1-3", "1-3b");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "1-2b", "1-3", "1-3b", "2-1");
			CheckIterator(g.GetEdges(1, 1), "1-1");
			CheckIterator(g.GetEdges(1, 2), "1-2", "1-2b");
			CheckIterator(g.GetEdges(1, 3), "1-3", "1-3b");
			CheckIterator(g.GetEdges(3, 1));
			g.RemoveVertex(2);
			CheckIterator(g.IncomingEdgeIterable(1), "1-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-3", "1-3b");
			CheckIterator(g.IncomingEdgeIterable(3), "1-3", "1-3b");
			CheckIterator(g.OutgoingEdgeIterable(3));
			CheckIterator(g.EdgeIterable(), "1-1", "1-3", "1-3b");
		}

		/// <summary>Test the behavior of the copy constructor; namely, make sure it's doing a deep copy</summary>
		[NUnit.Framework.Test]
		public virtual void TestCopyConstructor()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.AddVertex(3);
			g.Add(1, 2, "1-2a");
			g.Add(1, 2, "1-2b");
			g.Add(1, 2, "1-2c");
			g.Add(1, 3, "1-3a");
			g.Add(1, 3, "1-3b");
			g.Add(2, 3, "2-3a");
			g.Add(2, 3, "2-3b");
			g.Add(3, 1, "3-1a");
			g.Add(3, 1, "3-1b");
			DirectedMultiGraph<int, string> copy = new DirectedMultiGraph<int, string>(g);
			NUnit.Framework.Assert.AreEqual(g.GetNumEdges(), copy.GetNumEdges());
			int originalSize = g.GetNumEdges();
			NUnit.Framework.Assert.AreEqual(originalSize, g.GetNumEdges());
			copy.RemoveEdge(1, 2, "1-2b");
			NUnit.Framework.Assert.AreEqual(originalSize - 1, copy.GetNumEdges());
			NUnit.Framework.Assert.AreEqual(originalSize, g.GetNumEdges());
			copy.RemoveVertex(3);
			NUnit.Framework.Assert.AreEqual(originalSize - 7, copy.GetNumEdges());
			NUnit.Framework.Assert.AreEqual(originalSize, g.GetNumEdges());
		}

		/// <summary>
		/// Check to make sure
		/// <see cref="DirectedMultiGraph{V, E}.EdgeIterator()"/>
		/// .remove() works as expected
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestIteratorRemove()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.AddVertex(3);
			g.Add(1, 2, "1-2a");
			g.Add(1, 2, "1-2b");
			g.Add(1, 2, "1-2c");
			g.Add(1, 3, "1-3a");
			g.Add(1, 3, "1-3b");
			g.Add(2, 3, "2-3a");
			g.Add(2, 3, "2-3b");
			g.Add(3, 1, "3-1a");
			g.Add(3, 1, "3-1b");
			CheckGraphConsistency(g);
			foreach (string edge in g.GetAllEdges())
			{
				// Create copy and remove edge from copy manually
				int originalSize = g.GetNumEdges();
				DirectedMultiGraph<int, string> gold = new DirectedMultiGraph<int, string>(g);
				DirectedMultiGraph<int, string> guess = new DirectedMultiGraph<int, string>(g);
				gold.RemoveEdge(System.Convert.ToInt32(Sharpen.Runtime.Substring(edge, 0, 1)), System.Convert.ToInt32(Sharpen.Runtime.Substring(edge, 2, 3)), edge);
				NUnit.Framework.Assert.AreEqual(originalSize, g.GetNumEdges());
				NUnit.Framework.Assert.AreEqual(originalSize - 1, gold.GetAllEdges().Count);
				// Use iter.remove()
				IEnumerator<string> iter = guess.EdgeIterator();
				int iterations = 0;
				while (iter.MoveNext())
				{
					++iterations;
					if (iter.Current.Equals(edge))
					{
						iter.Remove();
						CheckGraphConsistency(guess);
					}
				}
				NUnit.Framework.Assert.AreEqual(9, iterations);
				CheckGraphConsistency(guess);
				// Assert that they're the same
				NUnit.Framework.Assert.AreEqual(gold, guess);
			}
		}

		/// <summary>A few loops get tested in testIterables; this exercises them more</summary>
		[NUnit.Framework.Test]
		public virtual void TestLoops()
		{
			DirectedMultiGraph<int, string> g = new DirectedMultiGraph<int, string>();
			g.AddVertex(1);
			g.AddVertex(2);
			g.Add(1, 1, "1-1");
			g.Add(1, 2, "1-2");
			g.Add(2, 1, "2-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "2-1");
			g.RemoveVertex(1);
			CheckIterator(g.IncomingEdgeIterable(2));
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable());
			g.AddVertex(1);
			CheckIterator(g.IncomingEdgeIterable(1));
			CheckIterator(g.OutgoingEdgeIterable(1));
			CheckIterator(g.IncomingEdgeIterable(2));
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable());
			g.Add(1, 1, "1-1");
			g.Add(1, 2, "1-2");
			g.Add(2, 1, "2-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "2-1");
			g.RemoveEdge(1, 1, "1-1");
			CheckIterator(g.IncomingEdgeIterable(1), "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-2", "2-1");
			g.Add(1, 1, "1-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "2-1");
			g.RemoveEdges(1, 1);
			CheckIterator(g.IncomingEdgeIterable(1), "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-2", "2-1");
			g.Add(1, 1, "1-1");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "2-1");
			g.RemoveEdges(1, 2);
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1");
			CheckIterator(g.IncomingEdgeIterable(2));
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "2-1");
			g.Add(1, 2, "1-2");
			CheckIterator(g.IncomingEdgeIterable(1), "1-1", "2-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2), "2-1");
			CheckIterator(g.EdgeIterable(), "1-1", "1-2", "2-1");
			g.RemoveEdges(2, 1);
			CheckIterator(g.IncomingEdgeIterable(1), "1-1");
			CheckIterator(g.OutgoingEdgeIterable(1), "1-1", "1-2");
			CheckIterator(g.IncomingEdgeIterable(2), "1-2");
			CheckIterator(g.OutgoingEdgeIterable(2));
			CheckIterator(g.EdgeIterable(), "1-1", "1-2");
		}
	}
}
