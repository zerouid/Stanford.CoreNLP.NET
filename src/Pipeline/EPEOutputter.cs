using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Output dependency annotations in the format for the EPE task
	/// at Depling 2017.
	/// </summary>
	/// <author>Sebastian Schuster</author>
	public class EPEOutputter : JSONOutputter
	{
		private static string OutputRepresentation = Runtime.GetProperty("outputRepresentation", "basic");

		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options)
		{
			PrintWriter writer = new PrintWriter(IOUtils.EncodedOutputStreamWriter(target, options.encoding));
			JSONOutputter.JSONWriter l0 = new JSONOutputter.JSONWriter(writer, options);
			if (doc.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null)
			{
				doc.Get(typeof(CoreAnnotations.SentencesAnnotation)).Stream().ForEach(null);
			}
		}

		private static object GetNodes(SemanticGraph graph)
		{
			if (graph != null)
			{
				IList<IndexedWord> vertexList = graph.VertexListSorted();
				int maxIndex = vertexList[vertexList.Count - 1].Index();
				return vertexList.Stream().Map(null);
			}
			else
			{
				return null;
			}
		}

		private static int GetNodeIndex(IndexedWord token, int maxIndex)
		{
			if (token.CopyCount() == 0)
			{
				return token.Index();
			}
			else
			{
				return token.Index() + maxIndex * token.CopyCount();
			}
		}
	}
}
