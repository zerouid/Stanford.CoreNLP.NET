using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Class AbstractListProcessor</summary>
	/// <author>Teg Grenager</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	/// <?/>
	/// <?/>
	public abstract class AbstractListProcessor<In, Out, L, F> : IListProcessor<IN, OUT>, IDocumentProcessor<IN, OUT, L, F>
	{
		public AbstractListProcessor()
		{
		}

		public virtual IDocument<L, F, OUT> ProcessDocument(IDocument<L, F, IN> @in)
		{
			IDocument<L, F, OUT> doc = @in.BlankDocument();
			Sharpen.Collections.AddAll(doc, Process(@in));
			return doc;
		}

		/// <summary>Process a list of lists of tokens.</summary>
		/// <remarks>
		/// Process a list of lists of tokens.  For example this might be a
		/// list of lists of words.
		/// </remarks>
		/// <param name="lists">a List of objects of type List</param>
		/// <returns>a List of objects of type List, each of which has been processed.</returns>
		public virtual IList<IList<OUT>> ProcessLists(IList<IList<In>> lists)
		{
			IList<IList<OUT>> result = new List<IList<OUT>>(lists.Count);
			foreach (IList<In> list in lists)
			{
				IList<OUT> outList = Process(list);
				result.Add(outList);
			}
			return result;
		}

		public abstract IList<OUT> Process(IList<In> arg1);
	}
}
