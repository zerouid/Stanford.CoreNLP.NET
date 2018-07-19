using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This class is used to wrap the ObjectBank used by the sequence
	/// models and is where any sort of general processing, like the IOB mapping
	/// stuff and wordshape stuff, should go.
	/// </summary>
	/// <remarks>
	/// This class is used to wrap the ObjectBank used by the sequence
	/// models and is where any sort of general processing, like the IOB mapping
	/// stuff and wordshape stuff, should go.
	/// It checks the SeqClassifierFlags to decide what to do.
	/// TODO: We should rearchitect this so that the FeatureFactory-specific
	/// stuff is done by a callback to the relevant FeatureFactory.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class ObjectBankWrapper<In> : ObjectBank<IList<IN>>
		where In : ICoreMap
	{
		private const long serialVersionUID = -3838331732026362075L;

		private readonly SeqClassifierFlags flags;

		private readonly ObjectBank<IList<IN>> wrapped;

		private readonly ICollection<string> knownLCWords;

		public ObjectBankWrapper(SeqClassifierFlags flags, ObjectBank<IList<IN>> wrapped, ICollection<string> knownLCWords)
			: base(null, null)
		{
			this.flags = flags;
			this.wrapped = wrapped;
			this.knownLCWords = knownLCWords;
		}

		public override IEnumerator<IList<IN>> GetEnumerator()
		{
			return new ObjectBankWrapper.WrappedIterator(this, wrapped.GetEnumerator());
		}

		private class WrappedIterator : AbstractIterator<IList<IN>>
		{
			private readonly IEnumerator<IList<IN>> wrappedIter;

			private IEnumerator<IList<IN>> spilloverIter;

			public WrappedIterator(ObjectBankWrapper<In> _enclosing, IEnumerator<IList<IN>> wrappedIter)
			{
				this._enclosing = _enclosing;
				this.wrappedIter = wrappedIter;
			}

			private void PrimeNextHelper()
			{
				while ((this.spilloverIter == null || !this.spilloverIter.MoveNext()) && this.wrappedIter.MoveNext())
				{
					IList<IN> doc = this.wrappedIter.Current;
					IList<IList<IN>> docs = new List<IList<IN>>();
					docs.Add(doc);
					this._enclosing.FixDocLengths(docs);
					this.spilloverIter = docs.GetEnumerator();
				}
			}

			public override bool MoveNext()
			{
				this.PrimeNextHelper();
				return this.wrappedIter.MoveNext() || (this.spilloverIter != null && this.spilloverIter.MoveNext());
			}

			public override IList<IN> Current
			{
				get
				{
					this.PrimeNextHelper();
					return this._enclosing.ProcessDocument(this.spilloverIter.Current);
				}
			}

			private readonly ObjectBankWrapper<In> _enclosing;
		}

		// end class WrappedIterator
		public virtual IList<IN> ProcessDocument(IList<IN> doc)
		{
			if (flags.mergeTags)
			{
				MergeTags(doc);
			}
			if (flags.iobTags)
			{
				IobTags(doc);
			}
			DoBasicStuff(doc);
			return doc;
		}

		private string Intern(string s)
		{
			if (flags.intern)
			{
				return string.Intern(s);
			}
			else
			{
				return s;
			}
		}

		private static readonly Pattern monthDayPattern = Pattern.Compile("Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|January|February|March|April|May|June|July|August|September|October|November|December", Pattern.CaseInsensitive);

		private string Fix(string word)
		{
			if (flags.normalizeTerms || flags.normalizeTimex)
			{
				// Same case for days/months: map to lowercase
				if (monthDayPattern.Matcher(word).Matches())
				{
					return word.ToLower();
				}
			}
			if (flags.normalizeTerms)
			{
				return Americanize.Americanize(word, false);
			}
			return word;
		}

		private void DoBasicStuff(IList<IN> doc)
		{
			int position = 0;
			foreach (IN fl in doc)
			{
				// position in document
				fl.Set(typeof(CoreAnnotations.PositionAnnotation), int.ToString((position++)));
				// word shape
				if ((flags.wordShape > WordShapeClassifier.Nowordshape) && !flags.useShapeStrings)
				{
					// TODO: if we pass in a FeatureFactory, as suggested by an earlier comment,
					// we should use that FeatureFactory's getWord function
					string word = fl.Get(typeof(CoreAnnotations.TextAnnotation));
					if (flags.wordFunction != null)
					{
						word = flags.wordFunction.Apply(word);
					}
					if (!word.IsEmpty() && char.IsLowerCase(word.CodePointAt(0)))
					{
						knownLCWords.Add(word);
					}
					string s = Intern(WordShapeClassifier.WordShape(word, flags.wordShape, knownLCWords));
					fl.Set(typeof(CoreAnnotations.ShapeAnnotation), s);
				}
				// normalizing and interning was the following; should presumably now be:
				// if ("CTBSegDocumentReader".equalsIgnoreCase(flags.documentReader)) {
				if (Sharpen.Runtime.EqualsIgnoreCase("edu.stanford.nlp.wordseg.Sighan2005DocumentReaderAndWriter", flags.readerAndWriter))
				{
					// for Chinese segmentation, "word" is no use and ignore goldAnswer for memory efficiency.
					fl.Set(typeof(CoreAnnotations.CharAnnotation), Intern(Fix(fl.Get(typeof(CoreAnnotations.CharAnnotation)))));
				}
				else
				{
					fl.Set(typeof(CoreAnnotations.TextAnnotation), Intern(Fix(fl.Get(typeof(CoreAnnotations.TextAnnotation)))));
					// only override GoldAnswer if not set - so that a DocumentReaderAndWriter can set it right in the first place.
					if (fl.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) == null)
					{
						fl.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), fl.Get(typeof(CoreAnnotations.AnswerAnnotation)));
					}
				}
			}
		}

		/// <summary>
		/// Take a
		/// <see cref="System.Collections.IList{E}"/>
		/// of documents (which are themselves
		/// <see cref="System.Collections.IList{E}"/>
		/// s
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// , CoreLabel by default),
		/// and if any are longer than the length
		/// specified by flags.maxDocSize split them up.  If maxDocSize is negative,
		/// nothing is changed.  In practice, documents need to be not too long or
		/// else the CRF inference will fail due to numerical problems.
		/// This method tries to be smart
		/// and split on sentence boundaries, but this is hard-coded to English.
		/// </summary>
		/// <param name="docs">The list of documents whose length might be adjusted.</param>
		private void FixDocLengths(IList<IList<IN>> docs)
		{
			int maxDocSize = flags.maxDocSize;
			WordToSentenceProcessor<IN> wts = null;
			// allocated lazily
			IList<IList<IN>> newDocuments = new List<IList<IN>>();
			foreach (IList<IN> document in docs)
			{
				if (maxDocSize <= 0 || document.Count <= maxDocSize)
				{
					if (flags.keepEmptySentences || !document.IsEmpty())
					{
						newDocuments.Add(document);
					}
					continue;
				}
				if (wts == null)
				{
					wts = new WordToSentenceProcessor<IN>();
				}
				IList<IList<IN>> sentences = wts.Process(document);
				IList<IN> newDocument = new List<IN>();
				foreach (IList<IN> sentence in sentences)
				{
					if (newDocument.Count + sentence.Count > maxDocSize)
					{
						if (!newDocument.IsEmpty())
						{
							newDocuments.Add(newDocument);
						}
						newDocument = new List<IN>();
					}
					Sharpen.Collections.AddAll(newDocument, sentence);
				}
				if (flags.keepEmptySentences || !newDocument.IsEmpty())
				{
					newDocuments.Add(newDocument);
				}
			}
			docs.Clear();
			Sharpen.Collections.AddAll(docs, newDocuments);
		}

		private void IobTags(IList<IN> doc)
		{
			string lastTag = string.Empty;
			foreach (IN wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (!flags.backgroundSymbol.Equals(answer) && answer != null)
				{
					int index = answer.IndexOf('-');
					string prefix;
					string label;
					if (index < 0)
					{
						prefix = string.Empty;
						label = answer;
					}
					else
					{
						prefix = Sharpen.Runtime.Substring(answer, 0, index);
						label = Sharpen.Runtime.Substring(answer, index + 1);
					}
					if (!prefix.Equals("B"))
					{
						if (!label.Equals(lastTag))
						{
							wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "B-" + label);
						}
						else
						{
							wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "I-" + label);
						}
					}
					lastTag = label;
				}
				else
				{
					lastTag = answer;
				}
			}
		}

		/// <summary>
		/// Change some form of IOB/IOE encoding via forms like "I-PERS" to
		/// IO encoding as just "PERS".
		/// </summary>
		/// <param name="doc">The document for which the AnswerAnnotation will be changed (in place)</param>
		private void MergeTags(IList<IN> doc)
		{
			foreach (IN wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (answer == null)
				{
					continue;
				}
				if (!answer.Equals(flags.backgroundSymbol))
				{
					int index = answer.IndexOf('-');
					if (index >= 0)
					{
						answer = Sharpen.Runtime.Substring(answer, index + 1);
					}
				}
				wi.Set(typeof(CoreAnnotations.AnswerAnnotation), answer);
			}
		}

		// This class inherits ObjectBank's implementation of the two toArray() methods.
		// These are implemented in terms of iterator(), and hence they will correctly use the WrappedIterator.
		// Forwarding these methods to the wrapped ObjectBank would be wrong, as then wrapper processing doesn't happen.
		// all the other crap from ObjectBank
		public override bool Add(IList<IN> o)
		{
			return wrapped.Add(o);
		}

		public override bool AddAll<_T0>(ICollection<_T0> c)
		{
			return Sharpen.Collections.AddAll(wrapped, c);
		}

		public override void Clear()
		{
			wrapped.Clear();
		}

		public override void ClearMemory()
		{
			wrapped.ClearMemory();
		}

		public override bool Contains(object o)
		{
			return wrapped.Contains(o);
		}

		public override bool ContainsAll<_T0>(ICollection<_T0> c)
		{
			return wrapped.ContainsAll(c);
		}

		public override bool IsEmpty()
		{
			return wrapped.IsEmpty();
		}

		public override void KeepInMemory(bool keep)
		{
			wrapped.KeepInMemory(keep);
		}

		public override bool Remove(object o)
		{
			return wrapped.Remove(o);
		}

		public override bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			return wrapped.RemoveAll(c);
		}

		public override bool RetainAll<_T0>(ICollection<_T0> c)
		{
			return wrapped.RetainAll(c);
		}

		public override int Count
		{
			get
			{
				return wrapped.Count;
			}
		}
	}
}
