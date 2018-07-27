using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Filter which removes stop-listed words.</summary>
	/// <author>Sepandar Kamvar (sdkamvar@stanford.edu)</author>
	public class StoplistFilter<L, F> : IDocumentProcessor<Word, Word, L, F>
	{
		private StopList stoplist;

		/// <summary>Create a new StopListFilter with a small default stoplist</summary>
		public StoplistFilter()
			: this(new StopList())
		{
		}

		/// <summary>Create a new StopListFilter with the stoplist given in <code>stoplistfile</code></summary>
		public StoplistFilter(string stoplistfile)
			: this(new StopList(new File(stoplistfile)))
		{
		}

		/// <summary>Create a new StoplistFilter with the given StopList.</summary>
		public StoplistFilter(StopList stoplist)
		{
			this.stoplist = stoplist;
		}

		/// <summary>
		/// Returns a new Document with the same meta-data as <tt>in</tt> and the same words
		/// except those on the stop list this filter was constructed with.
		/// </summary>
		public virtual IDocument<L, F, Word> ProcessDocument(IDocument<L, F, Word> @in)
		{
			IDocument<L, F, Word> @out = @in.BlankDocument();
			foreach (Word w in @in)
			{
				if (!stoplist.Contains(w))
				{
					@out.Add(w);
				}
			}
			return (@out);
		}
	}
}
