using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Set of common annotations for
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// s
	/// that require classes from the trees package.  See
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// for more information.
	/// This class exists so that
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// need not depend on
	/// trees classes, making distributions easier.
	/// </summary>
	/// <author>Anna Rafferty</author>
	public class TreeCoreAnnotations
	{
		private TreeCoreAnnotations()
		{
		}

		/// <summary>The CoreMap key for getting the syntactic parse tree of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic parse tree of a sentence.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class TreeAnnotation : ICoreAnnotation<Tree>
		{
			// only static members
			public virtual Type GetType()
			{
				return typeof(Tree);
			}
		}

		/// <summary>
		/// The CoreMap key for getting the binarized version of the
		/// syntactic parse tree of a sentence.
		/// </summary>
		/// <remarks>
		/// The CoreMap key for getting the binarized version of the
		/// syntactic parse tree of a sentence.
		/// This key is typically set on sentence annotations.  It is only
		/// set if the parser annotator was specifically set to parse with
		/// this (parse.saveBinarized).  The sentiment annotator requires
		/// this kind of tree, but otherwise it is not typically used.
		/// </remarks>
		public class BinarizedTreeAnnotation : ICoreAnnotation<Tree>
		{
			public virtual Type GetType()
			{
				return typeof(Tree);
			}
		}

		/// <summary>
		/// The standard key for storing a head word in the map as a pointer to
		/// the head label.
		/// </summary>
		public class HeadWordLabelAnnotation : ICoreAnnotation<CoreLabel>
		{
			public virtual Type GetType()
			{
				return typeof(CoreLabel);
			}
		}

		/// <summary>
		/// The standard key for storing a head tag in the map as a pointer to
		/// the head label.
		/// </summary>
		public class HeadTagLabelAnnotation : ICoreAnnotation<CoreLabel>
		{
			public virtual Type GetType()
			{
				return typeof(CoreLabel);
			}
		}

		/// <summary>The standard key for storing a list of k-best parses.</summary>
		public class KBestTreesAnnotation : ICoreAnnotation<IList<Tree>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}
	}
}
