using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>
	/// Similar to
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// ,
	/// but this class contains
	/// annotations made specifically for storing Coref data.  This is kept
	/// separate from CoreAnnotations so that systems which only need
	/// CoreAnnotations do not depend on Coref classes.
	/// </summary>
	public class CorefCoreAnnotations
	{
		/// <summary>the standard key for the coref label.</summary>
		/// <remarks>
		/// the standard key for the coref label.
		/// not used by the new dcoref system.
		/// </remarks>
		public class CorefAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Destination of the coreference link for this word (if any): it
		/// contains the index of the sentence and the index of the word that
		/// are the end of this coref link Both indices start at 1 The
		/// sentence index is IntTuple.get(0); the token index in the
		/// sentence is IntTuple.get(1)
		/// </summary>
		public class CorefDestAnnotation : ICoreAnnotation<IntTuple>
		{
			public virtual Type GetType()
			{
				return typeof(IntTuple);
			}
		}

		/// <summary>
		/// This stores the entire set of coreference links for one
		/// document.
		/// </summary>
		/// <remarks>
		/// This stores the entire set of coreference links for one
		/// document. Each link is stored as a pair of pointers (source and
		/// destination), where each pointer stores a sentence offset and a
		/// token offset. All offsets start at 0.
		/// </remarks>
		public class CorefGraphAnnotation : ICoreAnnotation<IList<Pair<IntTuple, IntTuple>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>
		/// An integer representing a document-level unique cluster of
		/// coreferent entities.
		/// </summary>
		/// <remarks>
		/// An integer representing a document-level unique cluster of
		/// coreferent entities. In other words, if two entities have the
		/// same CorefClusterIdAnnotation, they are coreferent. This
		/// annotation is typically attached to tokens (CoreLabel).
		/// </remarks>
		public class CorefClusterIdAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// Set of all the CoreLabel objects which are coreferent with a
		/// CoreLabel.
		/// </summary>
		/// <remarks>
		/// Set of all the CoreLabel objects which are coreferent with a
		/// CoreLabel.  Note that the list includes the CoreLabel that was
		/// annotated which creates a cycle.
		/// </remarks>
		public class CorefClusterAnnotation : ICoreAnnotation<ICollection<CoreLabel>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(ISet));
			}
		}

		/// <summary>CorefChainID - CorefChain map</summary>
		public class CorefChainAnnotation : ICoreAnnotation<IDictionary<int, CorefChain>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IDictionary));
			}
		}

		/// <summary>this annotation marks in every sentence the mentions used for coref</summary>
		public class CorefMentionsAnnotation : ICoreAnnotation<IList<Mention>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>index into the document wide list of coref mentions</summary>
		public class CorefMentionIndexesAnnotation : ICoreAnnotation<ICollection<int>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(ISet));
			}
		}
	}
}
