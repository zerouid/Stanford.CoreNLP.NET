using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	public abstract class AnnotationSerializer
	{
		/// <summary>Append a single object to this stream.</summary>
		/// <remarks>
		/// Append a single object to this stream. Subsequent calls to append on the same stream must supply the returned
		/// output stream; furthermore, implementations of this function must be prepared to handle
		/// the same output stream being passed in as it returned on the previous write.
		/// </remarks>
		/// <param name="corpus">The document to serialize to the stream.</param>
		/// <param name="os">The output stream to serialize to.</param>
		/// <returns>
		/// The output stream which should be closed when done writing, and which should be passed into subsequent
		/// calls to write() on this serializer.
		/// </returns>
		/// <exception cref="System.IO.IOException">Thrown if the underlying output stream throws the exception.</exception>
		public abstract OutputStream Write(Annotation corpus, OutputStream os);

		/// <summary>Read a single object from this stream.</summary>
		/// <remarks>
		/// Read a single object from this stream. Subsequent calls to read on the same input stream must supply the
		/// returned input stream; furthermore, implementations of this function must be prepared to handle the same
		/// input stream being passed to it as it returned on the previous read.
		/// </remarks>
		/// <param name="is">The input stream to read a document from.</param>
		/// <returns>
		/// A pair of the read document, and the implementation-specific input stream which it was actually read from.
		/// This stream should be passed to subsequent calls to read on the same stream, and should be closed when reading
		/// completes.
		/// </returns>
		/// <exception cref="System.IO.IOException">Thrown if the underlying stream throws the exception.</exception>
		/// <exception cref="System.TypeLoadException">Thrown if an object was read that does not exist in the classpath.</exception>
		/// <exception cref="System.InvalidCastException">Thrown if the signature of a class changed in way that was incompatible with the serialized document.</exception>
		public abstract Pair<Annotation, InputStream> Read(InputStream @is);

		/// <summary>Append a CoreDocument to this output stream.</summary>
		/// <param name="document">The CoreDocument to serialize (its internal annotation is serialized)</param>
		/// <param name="os">The output stream to serialize to</param>
		/// <returns>The output stream which should be closed</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual OutputStream WriteCoreDocument(CoreDocument document, OutputStream os)
		{
			Annotation wrappedAnnotation = document.Annotation();
			return Write(wrappedAnnotation, os);
		}

		/// <summary>Read in a CoreDocument from this input stream.</summary>
		/// <param name="is">The input stream to read a CoreDocument's annotation from</param>
		/// <returns>A pair with the CoreDocument and the input stream</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public virtual Pair<CoreDocument, InputStream> ReadCoreDocument(InputStream @is)
		{
			Pair<Annotation, InputStream> readPair = Read(@is);
			CoreDocument readCoreDocument = new CoreDocument(readPair.First());
			return new Pair<CoreDocument, InputStream>(readCoreDocument, @is);
		}

		public class IntermediateNode
		{
			internal string docId;

			internal int sentIndex;

			internal int index;

			internal int copyAnnotation;

			internal bool isRoot;

			public IntermediateNode(string docId, int sentIndex, int index, int copy, bool isRoot)
			{
				this.docId = docId;
				this.sentIndex = sentIndex;
				this.index = index;
				this.copyAnnotation = copy;
				this.isRoot = isRoot;
			}
		}

		public class IntermediateEdge
		{
			internal int source;

			internal int sourceCopy;

			internal int target;

			internal int targetCopy;

			internal string dep;

			internal bool isExtra;

			public IntermediateEdge(string dep, int source, int sourceCopy, int target, int targetCopy, bool isExtra)
			{
				this.dep = dep;
				this.source = source;
				this.sourceCopy = sourceCopy;
				this.target = target;
				this.targetCopy = targetCopy;
				this.isExtra = isExtra;
			}
		}

		public class IntermediateSemanticGraph
		{
			public IList<AnnotationSerializer.IntermediateNode> nodes;

			public IList<AnnotationSerializer.IntermediateEdge> edges;

			public IntermediateSemanticGraph()
			{
				nodes = new List<AnnotationSerializer.IntermediateNode>();
				edges = new List<AnnotationSerializer.IntermediateEdge>();
			}

			public IntermediateSemanticGraph(IList<AnnotationSerializer.IntermediateNode> nodes, IList<AnnotationSerializer.IntermediateEdge> edges)
			{
				this.nodes = new List<AnnotationSerializer.IntermediateNode>(nodes);
				this.edges = new List<AnnotationSerializer.IntermediateEdge>(edges);
			}

			private static readonly object Lock = new object();

			public virtual SemanticGraph ConvertIntermediateGraph(IList<CoreLabel> sentence)
			{
				SemanticGraph graph = new SemanticGraph();
				// First construct the actual nodes; keep them indexed by their index and copy count.
				// Sentences such as "I went over the river and through the woods" have
				// two copies for "went" in the collapsed dependencies.
				TwoDimensionalMap<int, int, IndexedWord> nodeMap = TwoDimensionalMap.HashMap();
				foreach (AnnotationSerializer.IntermediateNode @in in nodes)
				{
					CoreLabel token = sentence[@in.index - 1];
					// index starts at 1!
					IndexedWord word;
					if (@in.copyAnnotation > 0)
					{
						// TODO: if we make a copy wrapper CoreLabel, use it here instead
						word = new IndexedWord(new CoreLabel(token));
						word.SetCopyCount(@in.copyAnnotation);
					}
					else
					{
						word = new IndexedWord(token);
					}
					// for backwards compatibility - new annotations should have
					// these fields set, but annotations older than August 2014 might not
					if (word.DocID() == null && @in.docId != null)
					{
						word.SetDocID(@in.docId);
					}
					if (word.SentIndex() < 0 && @in.sentIndex >= 0)
					{
						word.SetSentIndex(@in.sentIndex);
					}
					if (word.Index() < 0 && @in.index >= 0)
					{
						word.SetIndex(@in.index);
					}
					nodeMap.Put(word.Index(), word.CopyCount(), word);
					graph.AddVertex(word);
					if (@in.isRoot)
					{
						graph.AddRoot(word);
					}
				}
				// add all edges to the actual graph
				foreach (AnnotationSerializer.IntermediateEdge ie in edges)
				{
					IndexedWord source = nodeMap.Get(ie.source, ie.sourceCopy);
					if (source == null)
					{
						throw new RuntimeIOException("Failed to find node " + ie.source + "-" + ie.sourceCopy);
					}
					IndexedWord target = nodeMap.Get(ie.target, ie.targetCopy);
					if (target == null)
					{
						throw new RuntimeIOException("Failed to find node " + ie.target + "-" + ie.targetCopy);
					}
					// assert(target != null);
					lock (Lock)
					{
						// this is not thread-safe: there are static fields in GrammaticalRelation
						GrammaticalRelation rel = GrammaticalRelation.ValueOf(ie.dep);
						graph.AddEdge(source, target, rel, 1.0, ie.isExtra);
					}
				}
				// compute root nodes if they weren't stored in the graph
				if (!graph.IsEmpty() && graph.GetRoots().Count == 0)
				{
					graph.ResetRoots();
				}
				return graph;
			}
		}
	}
}
