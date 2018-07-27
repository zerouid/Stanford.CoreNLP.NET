


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Vends
	/// <see cref="PennTreeReader"/>
	/// objects.
	/// </summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	public class PennTreeReaderFactory : ITreeReaderFactory
	{
		private readonly ITreeFactory tf;

		private readonly TreeNormalizer tn;

		/// <summary>
		/// Default constructor; uses a
		/// <see cref="LabeledScoredTreeFactory"/>
		/// ,
		/// with StringLabels, a
		/// <see cref="PennTreebankTokenizer"/>
		/// ,
		/// and a
		/// <see cref="TreeNormalizer"/>
		/// .
		/// </summary>
		public PennTreeReaderFactory()
			: this(new LabeledScoredTreeFactory())
		{
		}

		/// <summary>
		/// Specify your own
		/// <see cref="ITreeFactory"/>
		/// ;
		/// uses a
		/// <see cref="PennTreebankTokenizer"/>
		/// , and a
		/// <see cref="TreeNormalizer"/>
		/// .
		/// </summary>
		/// <param name="tf">The TreeFactory to use in building Tree objects to return.</param>
		public PennTreeReaderFactory(ITreeFactory tf)
			: this(tf, new TreeNormalizer())
		{
		}

		/// <summary>
		/// Specify your own
		/// <see cref="TreeNormalizer"/>
		/// ;
		/// uses a
		/// <see cref="PennTreebankTokenizer"/>
		/// , and a
		/// <see cref="LabeledScoredTreeFactory"/>
		/// .
		/// </summary>
		/// <param name="tn">The TreeNormalizer to use in building Tree objects to return.</param>
		public PennTreeReaderFactory(TreeNormalizer tn)
			: this(new LabeledScoredTreeFactory(), tn)
		{
		}

		/// <summary>
		/// Specify your own
		/// <see cref="ITreeFactory"/>
		/// ;
		/// uses a
		/// <see cref="PennTreebankTokenizer"/>
		/// , and a
		/// <see cref="TreeNormalizer"/>
		/// .
		/// </summary>
		/// <param name="tf">The TreeFactory to use in building Tree objects to return.</param>
		/// <param name="tn">The TreeNormalizer to use</param>
		public PennTreeReaderFactory(ITreeFactory tf, TreeNormalizer tn)
		{
			this.tf = tf;
			this.tn = tn;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, tf, tn, new PennTreebankTokenizer(@in));
		}
	}
}
