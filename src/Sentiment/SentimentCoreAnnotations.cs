using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>Annotations specific to the Sentiment project.</summary>
	/// <remarks>
	/// Annotations specific to the Sentiment project.  In case there are
	/// other projects that use the same RNN machinery, including the RNN
	/// core annotations, this lets a sentence have a tree attached where
	/// that tree specifically has the sentiment annotations.
	/// </remarks>
	/// <author>John Bauer</author>
	public class SentimentCoreAnnotations
	{
		/// <summary>
		/// A tree which contains the annotations used for the Sentiment
		/// task.
		/// </summary>
		/// <remarks>
		/// A tree which contains the annotations used for the Sentiment
		/// task.  After forwardPropagate has been called, the Tree will have
		/// prediction, etc. attached to it.
		/// </remarks>
		public class SentimentAnnotatedTree : ICoreAnnotation<Tree>
		{
			public virtual Type GetType()
			{
				return typeof(Tree);
			}
		}

		/// <summary>The final label given for a sentence.</summary>
		/// <remarks>
		/// The final label given for a sentence.  Set by the
		/// SentimentAnnotator and used by various forms of text output.
		/// </remarks>
		public class SentimentClass : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}
	}
}
