using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Util
{
	/// <summary>A (subject, relation, object) triple; e.g., as used in the KBP challenges or in OpenIE systems.</summary>
	/// <author>Gabor Angeli</author>
	public class RelationTriple : IComparable<Edu.Stanford.Nlp.IE.Util.RelationTriple>, IEnumerable<CoreLabel>
	{
		/// <summary>The subject (first argument) of this triple</summary>
		public readonly IList<CoreLabel> subject;

		/// <summary>The subject (first argument) of this triple, in its canonical mention (i.e., coref resolved)</summary>
		public readonly IList<CoreLabel> canonicalSubject;

		/// <summary>The relation (second argument) of this triple.</summary>
		/// <remarks>
		/// The relation (second argument) of this triple.
		/// Note that this is only the part of the relation that can be grounded in the sentence itself.
		/// Often, for a standalone readable relation string, you want to attach additional modifiers
		/// otherwise stored in the dependnecy arc.
		/// Therefore, for getting a String form of the relation, we recommend using
		/// <see cref="RelationGloss()"/>
		/// or
		/// <see cref="RelationLemmaGloss()"/>
		/// .
		/// </remarks>
		public readonly IList<CoreLabel> relation;

		/// <summary>The object (third argument) of this triple</summary>
		public readonly IList<CoreLabel> @object;

		/// <summary>The object (third argument) of this triple, in its canonical mention (i.e., coref resolved).</summary>
		public readonly IList<CoreLabel> canonicalObject;

		/// <summary>A marker for the relation expressing a tmod not grounded in a word in the sentence.</summary>
		private bool istmod = false;

		/// <summary>A marker for the relation expressing a prefix "be" not grounded in a word in the sentence.</summary>
		private bool prefixBe = false;

		/// <summary>A marker for the relation expressing a suffix "be" not grounded in a word in the sentence.</summary>
		private bool suffixBe = false;

		/// <summary>A marker for the relation expressing a suffix "of" not grounded in a word in the sentence.</summary>
		private bool suffixOf = false;

		/// <summary>An optional score (confidence) for this triple</summary>
		public readonly double confidence;

		/// <summary>Create a new triple with known values for the subject, relation, and object.</summary>
		/// <remarks>
		/// Create a new triple with known values for the subject, relation, and object.
		/// For example, "(cats, play with, yarn)"
		/// </remarks>
		/// <param name="subject">The subject of this triple; e.g., "cats".</param>
		/// <param name="relation">The relation of this triple; e.g., "play with".</param>
		/// <param name="object">The object of this triple; e.g., "yarn".</param>
		public RelationTriple(IList<CoreLabel> subject, IList<CoreLabel> relation, IList<CoreLabel> @object, double confidence)
		{
			this.subject = subject;
			this.canonicalSubject = subject;
			this.relation = relation;
			this.@object = @object;
			this.canonicalObject = @object;
			this.confidence = confidence;
		}

		/// <seealso cref="RelationTriple(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E}, double)"/>
		public RelationTriple(IList<CoreLabel> subject, IList<CoreLabel> relation, IList<CoreLabel> @object)
			: this(subject, relation, @object, 1.0)
		{
		}

		/// <summary>Create a new triple with known values for the subject, relation, and object.</summary>
		/// <remarks>
		/// Create a new triple with known values for the subject, relation, and object.
		/// For example, "(cats, play with, yarn)"
		/// </remarks>
		/// <param name="subject">The subject of this triple; e.g., "cats".</param>
		/// <param name="relation">The relation of this triple; e.g., "play with".</param>
		/// <param name="object">The object of this triple; e.g., "yarn".</param>
		public RelationTriple(IList<CoreLabel> subject, IList<CoreLabel> canonicalSubject, IList<CoreLabel> relation, IList<CoreLabel> @object, IList<CoreLabel> canonicalObject, double confidence)
		{
			this.subject = subject;
			this.canonicalSubject = canonicalSubject;
			this.relation = relation;
			this.@object = @object;
			this.canonicalObject = canonicalObject;
			this.confidence = confidence;
		}

		/// <seealso cref="RelationTriple(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E}, double)"/>
		public RelationTriple(IList<CoreLabel> subject, IList<CoreLabel> canonicalSubject, IList<CoreLabel> relation, IList<CoreLabel> canonicalObject, IList<CoreLabel> @object)
			: this(subject, canonicalSubject, relation, @object, canonicalObject, 1.0)
		{
		}

		/// <summary>Returns all the tokens in the extraction, in the order subject then relation then object.</summary>
		public virtual IList<CoreLabel> AllTokens()
		{
			IList<CoreLabel> allTokens = new List<CoreLabel>();
			Sharpen.Collections.AddAll(allTokens, canonicalSubject);
			Sharpen.Collections.AddAll(allTokens, relation);
			Sharpen.Collections.AddAll(allTokens, canonicalObject);
			return allTokens;
		}

		/// <summary>The subject of this relation triple, as a String</summary>
		public virtual string SubjectGloss()
		{
			return StringUtils.Join(canonicalSubject.Stream().Map(null), " ");
		}

		/// <summary>The head of the subject of this relation triple.</summary>
		public virtual CoreLabel SubjectHead()
		{
			return subject[subject.Count - 1];
		}

		/// <summary>The entity link of the subject</summary>
		public virtual string SubjectLink()
		{
			return SubjectLemmaGloss();
		}

		/// <summary>The subject of this relation triple, as a String of the subject's lemmas.</summary>
		/// <remarks>
		/// The subject of this relation triple, as a String of the subject's lemmas.
		/// This method will additionally strip out punctuation as well.
		/// </remarks>
		public virtual string SubjectLemmaGloss()
		{
			return StringUtils.Join(canonicalSubject.Stream().Filter(null).Map(null), " ");
		}

		/// <summary>The object of this relation triple, as a String</summary>
		public virtual string ObjectGloss()
		{
			return StringUtils.Join(canonicalObject.Stream().Map(null), " ");
		}

		/// <summary>The head of the object of this relation triple.</summary>
		public virtual CoreLabel ObjectHead()
		{
			return @object[@object.Count - 1];
		}

		/// <summary>The entity link of the subject</summary>
		public virtual string ObjectLink()
		{
			return ObjectLemmaGloss();
		}

		/// <summary>The object of this relation triple, as a String of the object's lemmas.</summary>
		/// <remarks>
		/// The object of this relation triple, as a String of the object's lemmas.
		/// This method will additionally strip out punctuation as well.
		/// </remarks>
		public virtual string ObjectLemmaGloss()
		{
			return StringUtils.Join(canonicalObject.Stream().Filter(null).Map(null), " ");
		}

		/// <summary>The relation of this relation triple, as a String</summary>
		public virtual string RelationGloss()
		{
			string relationGloss = ((prefixBe ? "is " : string.Empty) + StringUtils.Join(relation.Stream().Map(null), " ") + (suffixBe ? " is" : string.Empty) + (suffixOf ? " of" : string.Empty) + (istmod ? " at_time" : string.Empty)).Trim();
			// Some cosmetic tweaks
			if ("'s".Equals(relationGloss))
			{
				return "has";
			}
			else
			{
				return relationGloss;
			}
		}

		/// <summary>The relation of this relation triple, as a String of the relation's lemmas.</summary>
		/// <remarks>
		/// The relation of this relation triple, as a String of the relation's lemmas.
		/// This method will additionally strip out punctuation as well, and lower-cases the relation.
		/// </remarks>
		public virtual string RelationLemmaGloss()
		{
			// Construct a human readable relation string
			string relationGloss = ((prefixBe ? "be " : string.Empty) + StringUtils.Join(relation.Stream().Filter(null).Map(null), " ").ToLower() + (suffixBe ? " be" : string.Empty) + (suffixOf ? " of" : string.Empty) + (istmod ? " at_time" : string.Empty
				)).Trim();
			// Some cosmetic tweaks
			if ("'s".Equals(relationGloss))
			{
				return "have";
			}
			else
			{
				return relationGloss;
			}
		}

		/// <summary>The head of the relation of this relation triple.</summary>
		/// <remarks>The head of the relation of this relation triple. This is usually the main verb.</remarks>
		public virtual CoreLabel RelationHead()
		{
			return relation.Stream().Filter(null).Reduce(null).OrElse(relation[relation.Count - 1]);
		}

		/// <summary>A textual representation of the confidence.</summary>
		public virtual string ConfidenceGloss()
		{
			return new DecimalFormat("0.000").Format(confidence);
		}

		private static Pair<int, int> GetSpan(IList<CoreLabel> tokens, IToIntFunction<CoreLabel> toMin, IToIntFunction<CoreLabel> toMax)
		{
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (CoreLabel token in tokens)
			{
				min = Math.Min(min, toMin.ApplyAsInt(token));
				max = Math.Max(max, toMax.ApplyAsInt(token) + 1);
			}
			return Pair.MakePair(min, max);
		}

		/// <summary>Gets the span of the NON-CANONICAL subject.</summary>
		public virtual Pair<int, int> SubjectTokenSpan()
		{
			return GetSpan(subject, null, null);
		}

		/// <summary>Get a representative span for the relation expressed by this triple.</summary>
		/// <remarks>
		/// Get a representative span for the relation expressed by this triple.
		/// This is a bit more complicated than the subject and object spans, as the relation
		/// span is occasionally discontinuous.
		/// If this is the case, this method returns the largest contiguous chunk.
		/// If the relation span is empty, return the object span.
		/// </remarks>
		public virtual Pair<int, int> RelationTokenSpan()
		{
			if (relation.IsEmpty())
			{
				return ObjectTokenSpan();
			}
			else
			{
				if (relation.Count == 1)
				{
					return Pair.MakePair(relation[0].Index() - 1, relation[0].Index());
				}
				else
				{
					// Variables to keep track of the longest chunk
					int longestChunk = 0;
					int longestChunkStart = 0;
					int thisChunk = 1;
					int thisChunkStart = 0;
					// Find the longest chunk
					for (int i = 1; i < relation.Count; ++i)
					{
						CoreLabel token = relation[i];
						CoreLabel lastToken = relation[i - 1];
						if (lastToken.Index() + 1 == token.Index())
						{
							thisChunk += 1;
						}
						else
						{
							if (lastToken.Index() + 2 == token.Index())
							{
								thisChunk += 2;
							}
							else
							{
								// a skip of one character is _usually_ punctuation
								if (thisChunk > longestChunk)
								{
									longestChunk = thisChunk;
									longestChunkStart = thisChunkStart;
								}
								thisChunkStart = i;
								thisChunk = 1;
							}
						}
					}
					// (subcase: the last chunk is the longest)
					if (thisChunk > longestChunk)
					{
						longestChunk = thisChunk;
						longestChunkStart = thisChunkStart;
					}
					// Return the longest chunk
					return Pair.MakePair(relation[longestChunkStart].Index() - 1, relation[longestChunkStart].Index() - 1 + longestChunk);
				}
			}
		}

		/// <summary>Gets the span of the NON-CANONICAL object.</summary>
		public virtual Pair<int, int> ObjectTokenSpan()
		{
			return GetSpan(@object, null, null);
		}

		/// <summary>If true, this relation expresses a "to be" relation.</summary>
		/// <remarks>
		/// If true, this relation expresses a "to be" relation.
		/// For example, "President Obama" expresses the relation
		/// (Obama; be; President).
		/// </remarks>
		public virtual bool IsPrefixBe()
		{
			return this.prefixBe;
		}

		/// <summary>Set the value of this relation triple expressing a "to be" relation.</summary>
		/// <param name="newValue">The new value of this relation being a "to be" relation.</param>
		/// <returns>The old value of whether this relation expressed a "to be" relation.</returns>
		public virtual bool IsPrefixBe(bool newValue)
		{
			bool oldValue = this.prefixBe;
			this.prefixBe = newValue;
			return oldValue;
		}

		/// <summary>If true, this relation expresses a "to be" relation (with the be at the end of the sentence).</summary>
		/// <remarks>
		/// If true, this relation expresses a "to be" relation (with the be at the end of the sentence).
		/// For example, "Tim's father Tom" expresses the relation
		/// (Tim; 's father is; Tom).
		/// </remarks>
		public virtual bool IsSuffixBe()
		{
			return this.suffixBe;
		}

		/// <summary>Set the value of this relation triple expressing a "to be" relation (suffix).</summary>
		/// <param name="newValue">The new value of this relation being a "to be" relation.</param>
		/// <returns>The old value of whether this relation expressed a "to be" relation.</returns>
		public virtual bool IsSuffixBe(bool newValue)
		{
			bool oldValue = this.suffixBe;
			this.suffixBe = newValue;
			return oldValue;
		}

		/// <summary>If true, this relation has an ungrounded "of" at the end of the relation.</summary>
		/// <remarks>
		/// If true, this relation has an ungrounded "of" at the end of the relation.
		/// For example, "United States president Barack Obama" expresses the relation
		/// (Obama; is president of; United States).
		/// </remarks>
		public virtual bool IsSuffixOf()
		{
			return this.suffixOf;
		}

		/// <summary>Set the value of this triple missing an ungrounded "of" in the relation string.</summary>
		/// <param name="newValue">The new value of this relation missing an "of".</param>
		/// <returns>The old value of whether this relation missing an "of".</returns>
		public virtual bool IsSuffixOf(bool newValue)
		{
			bool oldValue = this.suffixOf;
			this.suffixOf = newValue;
			return oldValue;
		}

		/// <summary>
		/// If true, this relation expresses a tmod (temporal modifier) relation that is not grounded in
		/// the sentence.
		/// </summary>
		/// <remarks>
		/// If true, this relation expresses a tmod (temporal modifier) relation that is not grounded in
		/// the sentence.
		/// For example, "I went to the store Friday" would otherwise yield a strange triple
		/// (I; go to store; Friday).
		/// </remarks>
		public virtual bool Istmod()
		{
			return this.istmod;
		}

		/// <summary>Set the value of this relation triple expressing a tmod (temporal modifier) relation.</summary>
		/// <param name="newValue">The new value of this relation being a tmod relation.</param>
		/// <returns>The old value of whether this relation expressed a tmod relation.</returns>
		public virtual bool Istmod(bool newValue)
		{
			bool oldValue = this.istmod;
			this.istmod = newValue;
			return oldValue;
		}

		/// <summary>An optional method, returning the dependency tree this triple was extracted from</summary>
		public virtual Optional<SemanticGraph> AsDependencyTree()
		{
			return Optional.Empty();
		}

		/// <summary>Return the given relation triple as a flat sentence</summary>
		public virtual IList<CoreLabel> AsSentence()
		{
			IPriorityQueue<CoreLabel> orderedSentence = new FixedPrioritiesPriorityQueue<CoreLabel>();
			double defaultIndex = 0.0;
			foreach (CoreLabel token in subject)
			{
				orderedSentence.Add(token, token.Index() >= 0 ? (double)-token.Index() : -defaultIndex);
				defaultIndex += 1.0;
			}
			foreach (CoreLabel token_1 in relation)
			{
				orderedSentence.Add(token_1, token_1.Index() >= 0 ? (double)-token_1.Index() : -defaultIndex);
				defaultIndex += 1.0;
			}
			foreach (CoreLabel token_2 in @object)
			{
				orderedSentence.Add(token_2, token_2.Index() >= 0 ? (double)-token_2.Index() : -defaultIndex);
				defaultIndex += 1.0;
			}
			return orderedSentence.ToSortedList();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.IE.Util.RelationTriple))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Util.RelationTriple that = (Edu.Stanford.Nlp.IE.Util.RelationTriple)o;
			return @object.Equals(that.@object) && relation.Equals(that.relation) && subject.Equals(that.subject);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		// Faster than checking CoreLabels
		//    int result = subject.hashCode();
		//    result = 31 * result + relation.hashCode();
		//    result = 31 * result + object.hashCode();
		//    return result;
		/// <summary>Print a human-readable description of this relation triple, as a tab-separated line.</summary>
		public override string ToString()
		{
			return this.confidence.ToString() + '\t' + SubjectGloss() + '\t' + RelationGloss() + '\t' + ObjectGloss();
		}

		/// <summary>
		/// Print in the format expected by Gabriel Stanovsky and Ido Dagan, Creating a Large Benchmark for Open
		/// Information Extraction, EMNLP 2016.
		/// </summary>
		/// <remarks>
		/// Print in the format expected by Gabriel Stanovsky and Ido Dagan, Creating a Large Benchmark for Open
		/// Information Extraction, EMNLP 2016. https://gabrielstanovsky.github.io/assets/papers/emnlp16a/paper.pdf ,
		/// with equivalence classes.
		/// </remarks>
		public virtual string ToQaSrlString(ICoreMap sentence)
		{
			string equivalenceClass = SubjectHead().Index() + "." + RelationHead().Index() + '.' + ObjectHead().Index();
			return equivalenceClass + '\t' + SubjectGloss().Replace('\t', ' ') + '\t' + RelationGloss().Replace('\t', ' ') + '\t' + ObjectGloss().Replace('\t', ' ') + '\t' + confidence + '\t' + StringUtils.Join(sentence.Get(typeof(CoreAnnotations.TokensAnnotation
				)).Stream().Map(null), " ");
		}

		/// <summary>Print a description of this triple, formatted like the ReVerb outputs.</summary>
		public virtual string ToReverbString(string docid, ICoreMap sentence)
		{
			int sentIndex = -1;
			int subjIndex = -1;
			int relationIndex = -1;
			int objIndex = -1;
			int subjIndexEnd = -1;
			int relationIndexEnd = -1;
			int objIndexEnd = -1;
			if (!relation.IsEmpty())
			{
				sentIndex = relation[0].SentIndex();
				relationIndex = relation[0].Index() - 1;
				relationIndexEnd = relation[relation.Count - 1].Index();
			}
			if (!subject.IsEmpty())
			{
				if (sentIndex < 0)
				{
					sentIndex = subject[0].SentIndex();
				}
				subjIndex = subject[0].Index() - 1;
				subjIndexEnd = subject[subject.Count - 1].Index();
			}
			if (!@object.IsEmpty())
			{
				if (sentIndex < 0)
				{
					sentIndex = subject[0].SentIndex();
				}
				objIndex = @object[0].Index() - 1;
				objIndexEnd = @object[@object.Count - 1].Index();
			}
			return (docid == null ? "no_doc_id" : docid) + '\t' + sentIndex + '\t' + SubjectGloss().Replace('\t', ' ') + '\t' + RelationGloss().Replace('\t', ' ') + '\t' + ObjectGloss().Replace('\t', ' ') + '\t' + subjIndex + '\t' + subjIndexEnd + '\t' 
				+ relationIndex + '\t' + relationIndexEnd + '\t' + objIndex + '\t' + objIndexEnd + '\t' + ConfidenceGloss() + '\t' + StringUtils.Join(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Stream().Map(null), " ") + '\t' + StringUtils.Join(
				sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Stream().Map(null), " ") + '\t' + SubjectLemmaGloss().Replace('\t', ' ') + '\t' + RelationLemmaGloss().Replace('\t', ' ') + '\t' + ObjectLemmaGloss().Replace('\t', ' ');
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.IE.Util.RelationTriple o)
		{
			return double.Compare(this.confidence, o.confidence);
		}

		public virtual IEnumerator<CoreLabel> GetEnumerator()
		{
			return CollectionUtils.ConcatIterators(subject.GetEnumerator(), relation.GetEnumerator(), @object.GetEnumerator());
		}

		/// <summary>
		/// A
		/// <see cref="RelationTriple"/>
		/// , but with the tree saved as well.
		/// </summary>
		public class WithTree : RelationTriple
		{
			public readonly SemanticGraph sourceTree;

			/// <summary>Create a new triple with known values for the subject, relation, and object.</summary>
			/// <remarks>
			/// Create a new triple with known values for the subject, relation, and object.
			/// For example, "(cats, play with, yarn)"
			/// </remarks>
			/// <param name="subject">The subject of this triple; e.g., "cats".</param>
			/// <param name="relation">The relation of this triple; e.g., "play with".</param>
			/// <param name="object">The object of this triple; e.g., "yarn".</param>
			/// <param name="tree">The tree this extraction was created from; we create a deep copy of the tree.</param>
			public WithTree(IList<CoreLabel> subject, IList<CoreLabel> relation, IList<CoreLabel> @object, SemanticGraph tree, double confidence)
				: base(subject, relation, @object, confidence)
			{
				this.sourceTree = new SemanticGraph(tree);
			}

			/// <summary>
			/// Create a new triple with known values for the subject, relation, and object,
			/// along with their canonical spans (i.e., resolving coreference)
			/// For example, "(cats, play with, yarn)"
			/// </summary>
			public WithTree(IList<CoreLabel> subject, IList<CoreLabel> canonicalSubject, IList<CoreLabel> relation, IList<CoreLabel> @object, IList<CoreLabel> canonicalObject, double confidence, SemanticGraph tree)
				: base(subject, canonicalSubject, relation, @object, canonicalObject, confidence)
			{
				this.sourceTree = tree;
			}

			/// <summary>The head of the subject of this relation triple.</summary>
			public override CoreLabel SubjectHead()
			{
				if (subject.Count == 1)
				{
					return subject[0];
				}
				Span subjectSpan = Span.FromValues(subject[0].Index(), subject[subject.Count - 1].Index());
				for (int i = subject.Count - 1; i >= 0; --i)
				{
					foreach (SemanticGraphEdge edge in sourceTree.IncomingEdgeIterable(new IndexedWord(subject[i])))
					{
						if (edge.GetGovernor().Index() < subjectSpan.Start() || edge.GetGovernor().Index() >= subjectSpan.End())
						{
							return subject[i];
						}
					}
				}
				return subject[subject.Count - 1];
			}

			/// <summary>The head of the object of this relation triple.</summary>
			public override CoreLabel ObjectHead()
			{
				if (@object.Count == 1)
				{
					return @object[0];
				}
				Span objectSpan = Span.FromValues(@object[0].Index(), @object[@object.Count - 1].Index());
				for (int i = @object.Count - 1; i >= 0; --i)
				{
					foreach (SemanticGraphEdge edge in sourceTree.IncomingEdgeIterable(new IndexedWord(@object[i])))
					{
						if (edge.GetGovernor().Index() < objectSpan.Start() || edge.GetGovernor().Index() >= objectSpan.End())
						{
							return @object[i];
						}
					}
				}
				return @object[@object.Count - 1];
			}

			/// <summary>The head of the relation of this relation triple.</summary>
			public override CoreLabel RelationHead()
			{
				if (relation.Count == 1)
				{
					return relation[0];
				}
				CoreLabel guess = null;
				CoreLabel newGuess = base.RelationHead();
				int iters = 0;
				// make sure we don't infinite loop...
				while (guess != newGuess && iters < 100)
				{
					guess = newGuess;
					iters += 1;
					foreach (SemanticGraphEdge edge in sourceTree.IncomingEdgeIterable(new IndexedWord(guess)))
					{
						// find a node in the relation list which is a governor of the candidate root
						Optional<CoreLabel> governor = relation.Stream().Filter(null).FindFirst();
						// if we found one, this is the new root. The for loop continues
						if (governor.IsPresent())
						{
							newGuess = governor.Get();
						}
					}
				}
				// Return
				if (iters >= 100)
				{
					Redwood.Util.Err("Likely cycle in relation tree");
				}
				return guess;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override Optional<SemanticGraph> AsDependencyTree()
			{
				return Optional.Of(sourceTree);
			}
		}

		/// <summary>
		/// A
		/// <see cref="RelationTriple"/>
		/// , but with both the tree and the entity
		/// links saved as well.
		/// </summary>
		public class WithLink : RelationTriple.WithTree
		{
			/// <summary>The canonical entity link of the subject</summary>
			public readonly Optional<string> subjectLink;

			/// <summary>The canonical entity link of the object</summary>
			public readonly Optional<string> objectLink;

			/// <summary>Create a new relation triple</summary>
			public WithLink(IList<CoreLabel> subject, IList<CoreLabel> canonicalSubject, IList<CoreLabel> relation, IList<CoreLabel> @object, IList<CoreLabel> canonicalObject, double confidence, SemanticGraph tree, string subjectLink, string objectLink)
				: base(subject, canonicalSubject, relation, @object, canonicalObject, confidence, tree)
			{
				this.subjectLink = Optional.OfNullable(subjectLink);
				this.objectLink = Optional.OfNullable(objectLink);
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override string SubjectLink()
			{
				return subjectLink.OrElseGet(null);
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override string ObjectLink()
			{
				return objectLink.OrElseGet(null);
			}
		}
	}
}
