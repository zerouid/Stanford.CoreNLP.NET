using System;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// This class collects CoreAnnotations that are used in working with a
	/// SemanticGraph.
	/// </summary>
	/// <remarks>
	/// This class collects CoreAnnotations that are used in working with a
	/// SemanticGraph.  (These were originally separated out at a time when
	/// a SemanticGraph was backed by the JGraphT library so as not to
	/// introduce a library dependency for some tools. This is no longer
	/// the case, but they remain gathered here.)
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class SemanticGraphCoreAnnotations
	{
		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// These are collapsed dependencies!
		/// This key is typically set on sentence annotations.
		/// </remarks>
		[System.ObsoleteAttribute(@"In the future, we will only provide basic, enhanced, and enhanced++ dependencies.")]
		public class CollapsedDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// These are basic dependencies without any post-processing!
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class BasicDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// These are dependencies that are both collapsed and have CC processing!
		/// This key is typically set on sentence annotations.
		/// </remarks>
		[System.ObsoleteAttribute(@"In the future, we will only provide basic, enhanced, and enhanced++ dependencies.")]
		public class CollapsedCCProcessedDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// These are the enhanced dependencies.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class EnhancedDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// These are the enhanced++ dependencies.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class EnhancedPlusPlusDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>The CoreMap key for storing a semantic graph that was converted using a non-default converter.</summary>
		/// <remarks>
		/// The CoreMap key for storing a semantic graph that was converted using a non-default converter.
		/// Currently only used by the DeterministicCorefAnnotator to store the original Stanford dependencies.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class AlternativeDependenciesAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}
	}
}
