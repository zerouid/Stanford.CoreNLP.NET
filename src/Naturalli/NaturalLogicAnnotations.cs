using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// A collection of
	/// <see cref="Edu.Stanford.Nlp.Ling.ICoreAnnotation{V}"/>
	/// s for various Natural Logic data.
	/// </summary>
	/// <author>Gabor Angeli</author>
	public class NaturalLogicAnnotations
	{
		/// <summary>
		/// An annotation which attaches to a CoreLabel to denote that this is an operator in natural logic,
		/// to describe which operator it is, and to give the scope of its argument(s).
		/// </summary>
		/// <remarks>
		/// An annotation which attaches to a CoreLabel to denote that this is an operator in natural logic,
		/// to describe which operator it is, and to give the scope of its argument(s).
		/// This only attaches to tokens which are operators (i.e., the head words of operators).
		/// </remarks>
		public sealed class OperatorAnnotation : ICoreAnnotation<OperatorSpec>
		{
			public Type GetType()
			{
				return typeof(OperatorSpec);
			}
		}

		/// <summary>
		/// An annotation which attaches to a CoreLabel to denote that this is an operator in natural logic,
		/// to describe which operator it is, and to give the scope of its argument(s).
		/// </summary>
		public sealed class PolarityAnnotation : ICoreAnnotation<Polarity>
		{
			public Type GetType()
			{
				return typeof(Polarity);
			}
		}

		/// <summary>
		/// An annotation, similar to
		/// <see cref="PolarityAnnotation"/>
		/// , which just measures whether
		/// the polarity of a token is upwards, downwards, or flat.
		/// This annotation always has values either "up", "down", or "flat".
		/// </summary>
		public sealed class PolarityDirectionAnnotation : ICoreAnnotation<string>
		{
			public Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The set of sentences which are entailed by the original sentence, according to Natural Logic semantics.</summary>
		public sealed class EntailedSentencesAnnotation : ICoreAnnotation<ICollection<SentenceFragment>>
		{
			public Type GetType()
			{
				return (Type)((object)typeof(ICollection));
			}
		}

		/// <summary>A set of clauses contained in and entailed by this sentence.</summary>
		public sealed class EntailedClausesAnnotation : ICoreAnnotation<ICollection<SentenceFragment>>
		{
			public Type GetType()
			{
				return (Type)((object)typeof(ICollection));
			}
		}

		/// <summary>The set of relation triples extracted from this sentence.</summary>
		public sealed class RelationTriplesAnnotation : ICoreAnnotation<ICollection<RelationTriple>>
		{
			public Type GetType()
			{
				return (Type)((object)typeof(ICollection));
			}
		}
	}
}
