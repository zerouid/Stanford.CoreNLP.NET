using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// This class provides a
	/// <see cref="CoreLabel"/>
	/// that uses its
	/// DocIDAnnotation, SentenceIndexAnnotation, and IndexAnnotation to implement
	/// Comparable/compareTo, hashCode, and equals.  This means no other annotations,
	/// including the identity of the word, are taken into account when using these
	/// methods. Historically, this class was introduced for and is mainly used in
	/// the RTE package, and it provides a number of methods that are really specific
	/// to that use case. A second use case is now the Stanford Dependencies code,
	/// where this class directly implements the "copy nodes" of section 4.6 of the
	/// Stanford Dependencies Manual, rather than these being placed directly in the
	/// backing CoreLabel. This was so there can stay one CoreLabel per token, despite
	/// there being multiple IndexedWord nodes, additional ones representing copy
	/// nodes.
	/// <p>
	/// The actual implementation is to wrap a
	/// <c>CoreLabel</c>
	/// .
	/// This avoids breaking the
	/// <c>equals()</c>
	/// and
	/// <c>hashCode()</c>
	/// contract and also avoids expensive copying
	/// when used to represent the same data as the original
	/// <c>CoreLabel</c>
	/// .
	/// </summary>
	/// <author>rafferty</author>
	/// <author>John Bauer</author>
	/// <author>Sonal Gupta</author>
	[System.Serializable]
	public class IndexedWord : IAbstractCoreLabel, IComparable<Edu.Stanford.Nlp.Ling.IndexedWord>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Ling.IndexedWord));

		private const long serialVersionUID = 3739633991145239829L;

		/// <summary>The identifier that points to no word.</summary>
		public static readonly Edu.Stanford.Nlp.Ling.IndexedWord NoWord = new Edu.Stanford.Nlp.Ling.IndexedWord(null, -1, -1);

		private readonly CoreLabel label;

		private int copyCount;

		private int numCopies = 0;

		private Edu.Stanford.Nlp.Ling.IndexedWord original = null;

		/// <summary>Useful for specifying a fine-grained position when butchering parse trees.</summary>
		/// <remarks>
		/// Useful for specifying a fine-grained position when butchering parse trees.
		/// The canonical use case for this is resolving coreference in the OpenIE system, where
		/// we want to move nodes between sentences, but do not want to change their index annotation
		/// (plus, we need to have multiple nodes fit into the space of one pronoun).
		/// </remarks>
		private double pseudoPosition = double.NaN;

		/// <summary>
		/// Default constructor; uses
		/// <see cref="CoreLabel"/>
		/// default constructor
		/// </summary>
		public IndexedWord()
		{
			// = 0;
			label = new CoreLabel();
		}

		/// <summary>
		/// Copy Constructor - relies on
		/// <see cref="CoreLabel"/>
		/// copy constructor
		/// It will set the value, and if the word is not set otherwise, set
		/// the word to the value.
		/// </summary>
		/// <param name="w">A Label to initialize this IndexedWord from</param>
		public IndexedWord(ILabel w)
		{
			if (w is CoreLabel)
			{
				this.label = (CoreLabel)w;
			}
			else
			{
				label = new CoreLabel(w);
				if (label.Word() == null)
				{
					label.SetWord(label.Value());
				}
			}
		}

		/// <summary>Construct an IndexedWord from a CoreLabel just as for a CoreMap.</summary>
		/// <remarks>
		/// Construct an IndexedWord from a CoreLabel just as for a CoreMap.
		/// <i>Implementation note:</i> this is a the same as the constructor
		/// that takes a CoreMap, but is needed to ensure unique most specific
		/// type inference for selecting a constructor at compile-time.
		/// </remarks>
		/// <param name="w">A Label to initialize this IndexedWord from</param>
		public IndexedWord(CoreLabel w)
		{
			label = w;
		}

		/// <summary>
		/// Constructor for setting docID, sentenceIndex, and
		/// index without any other annotations.
		/// </summary>
		/// <param name="docID">The document ID (arbitrary string)</param>
		/// <param name="sentenceIndex">The sentence number in the document (normally 0-based)</param>
		/// <param name="index">The index of the word in the sentence (normally 0-based)</param>
		public IndexedWord(string docID, int sentenceIndex, int index)
		{
			label = new CoreLabel();
			label.Set(typeof(CoreAnnotations.DocIDAnnotation), docID);
			label.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
			label.Set(typeof(CoreAnnotations.IndexAnnotation), index);
		}

		public virtual Edu.Stanford.Nlp.Ling.IndexedWord MakeCopy(int count)
		{
			CoreLabel labelCopy = new CoreLabel(label);
			Edu.Stanford.Nlp.Ling.IndexedWord copy = new Edu.Stanford.Nlp.Ling.IndexedWord(labelCopy);
			copy.SetCopyCount(count);
			return copy;
		}

		public virtual Edu.Stanford.Nlp.Ling.IndexedWord MakeCopy()
		{
			return MakeCopy(++numCopies);
		}

		public virtual Edu.Stanford.Nlp.Ling.IndexedWord MakeSoftCopy(int count)
		{
			Edu.Stanford.Nlp.Ling.IndexedWord copy = new Edu.Stanford.Nlp.Ling.IndexedWord(label);
			copy.SetCopyCount(count);
			copy.original = this;
			return copy;
		}

		public virtual Edu.Stanford.Nlp.Ling.IndexedWord MakeSoftCopy()
		{
			if (original != null)
			{
				return original.MakeSoftCopy();
			}
			else
			{
				return MakeSoftCopy(++numCopies);
			}
		}

		public virtual Edu.Stanford.Nlp.Ling.IndexedWord GetOriginal()
		{
			return original;
		}

		/// <summary>TODO: get rid of this.</summary>
		/// <remarks>TODO: get rid of this.  Only used in two places in RTE (in rewriter code)</remarks>
		public virtual CoreLabel BackingLabel()
		{
			return label;
		}

		public virtual VALUE Get<Value>(Type key)
		{
			return label.Get(key);
		}

		public virtual bool ContainsKey<Value>(Type key)
		{
			return label.ContainsKey(key);
		}

		public virtual VALUE Set<Value>(Type key, VALUE value)
		{
			return label.Set(key, value);
		}

		public virtual string GetString<Key>()
			where Key : TypesafeMap.IKey<string>
		{
			System.Type key = typeof(KEY);
			return label.GetString(key);
		}

		public virtual string GetString<Key>(string def)
			where Key : TypesafeMap.IKey<string>
		{
			System.Type key = typeof(KEY);
			return label.GetString(key, def);
		}

		public virtual VALUE Remove<Value>(Type key)
		{
			return label.Remove(key);
		}

		public virtual ICollection<Type> KeySet()
		{
			return label.KeySet();
		}

		public virtual int Size()
		{
			return label.Size();
		}

		public virtual string Value()
		{
			return label.Value();
		}

		public virtual void SetValue(string value)
		{
			label.SetValue(value);
		}

		public virtual string Tag()
		{
			return label.Tag();
		}

		public virtual void SetTag(string tag)
		{
			label.SetTag(tag);
		}

		public virtual string Word()
		{
			return label.Word();
		}

		public virtual void SetWord(string word)
		{
			label.SetWord(word);
		}

		public virtual string Lemma()
		{
			return label.Lemma();
		}

		public virtual void SetLemma(string lemma)
		{
			label.SetLemma(lemma);
		}

		public virtual string Ner()
		{
			return label.Ner();
		}

		public virtual void SetNER(string ner)
		{
			label.SetNER(ner);
		}

		public virtual string DocID()
		{
			return label.DocID();
		}

		public virtual void SetDocID(string docID)
		{
			label.SetDocID(docID);
		}

		public virtual int Index()
		{
			return label.Index();
		}

		public virtual void SetIndex(int index)
		{
			label.SetIndex(index);
		}

		/// <summary>In most cases, this is just the index of the word.</summary>
		/// <remarks>
		/// In most cases, this is just the index of the word.
		/// However, this should be the value used to sort nodes in
		/// a tree.
		/// </remarks>
		/// <seealso cref="pseudoPosition"/>
		public virtual double PseudoPosition()
		{
			if (!double.IsNaN(pseudoPosition))
			{
				return pseudoPosition;
			}
			else
			{
				return (double)Index();
			}
		}

		/// <seealso cref="pseudoPosition"/>
		public virtual void SetPseudoPosition(double position)
		{
			this.pseudoPosition = position;
		}

		public virtual int SentIndex()
		{
			return label.SentIndex();
		}

		public virtual void SetSentIndex(int sentIndex)
		{
			label.SetSentIndex(sentIndex);
		}

		public virtual string Before()
		{
			return label.Before();
		}

		public virtual void SetBefore(string before)
		{
			label.SetBefore(before);
		}

		public virtual string OriginalText()
		{
			return label.OriginalText();
		}

		public virtual void SetOriginalText(string originalText)
		{
			label.SetOriginalText(originalText);
		}

		public virtual string After()
		{
			return label.After();
		}

		public virtual void SetAfter(string after)
		{
			label.SetAfter(after);
		}

		public virtual int BeginPosition()
		{
			return label.BeginPosition();
		}

		public virtual int EndPosition()
		{
			return label.EndPosition();
		}

		public virtual void SetBeginPosition(int beginPos)
		{
			label.SetBeginPosition(beginPos);
		}

		public virtual void SetEndPosition(int endPos)
		{
			label.SetEndPosition(endPos);
		}

		public virtual int CopyCount()
		{
			return copyCount;
		}

		public virtual void SetCopyCount(int count)
		{
			this.copyCount = count;
		}

		public virtual string ToPrimes()
		{
			return StringUtils.Repeat('\'', copyCount);
		}

		public virtual string ToCopyIndex()
		{
			if (copyCount == 0)
			{
				return this.Index().ToString();
			}
			else
			{
				return this.Index() + "." + copyCount;
			}
		}

		public virtual bool IsCopy(Edu.Stanford.Nlp.Ling.IndexedWord otherWord)
		{
			int myInd = Get(typeof(CoreAnnotations.IndexAnnotation));
			int otherInd = otherWord.Get(typeof(CoreAnnotations.IndexAnnotation));
			if (!Objects.Equals(myInd, otherInd))
			{
				return false;
			}
			int mySentInd = Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
			int otherSentInd = otherWord.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
			if (!Objects.Equals(mySentInd, otherSentInd))
			{
				return false;
			}
			string myDocID = GetString<CoreAnnotations.DocIDAnnotation>();
			string otherDocID = otherWord.GetString<CoreAnnotations.DocIDAnnotation>();
			if (!Objects.Equals(myDocID, otherDocID))
			{
				return false;
			}
			if (CopyCount() == 0 || otherWord.CopyCount() != 0)
			{
				return false;
			}
			return true;
		}

		/// <summary>This .equals is dependent only on docID, sentenceIndex, and index.</summary>
		/// <remarks>
		/// This .equals is dependent only on docID, sentenceIndex, and index.
		/// It doesn't consider the actual word value, but assumes that it is
		/// validly represented by token position.
		/// All IndexedWords that lack these fields will be regarded as equal.
		/// </remarks>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Ling.IndexedWord))
			{
				return false;
			}
			//now compare on appropriate keys
			Edu.Stanford.Nlp.Ling.IndexedWord otherWord = (Edu.Stanford.Nlp.Ling.IndexedWord)o;
			int myInd = Get(typeof(CoreAnnotations.IndexAnnotation));
			int otherInd = otherWord.Get(typeof(CoreAnnotations.IndexAnnotation));
			if (!Objects.Equals(myInd, otherInd))
			{
				return false;
			}
			int mySentInd = Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
			int otherSentInd = otherWord.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
			if (!Objects.Equals(mySentInd, otherSentInd))
			{
				return false;
			}
			string myDocID = GetString<CoreAnnotations.DocIDAnnotation>();
			string otherDocID = otherWord.GetString<CoreAnnotations.DocIDAnnotation>();
			if (!Objects.Equals(myDocID, otherDocID))
			{
				return false;
			}
			if (CopyCount() != otherWord.CopyCount())
			{
				return false;
			}
			// Compare pseudo-positions
			if ((!double.IsNaN(this.pseudoPosition) || !double.IsNaN(otherWord.pseudoPosition)) && this.pseudoPosition != otherWord.pseudoPosition)
			{
				return false;
			}
			return true;
		}

		private int cachedHashCode = 0;

		/// <summary>This hashCode uses only the docID, sentenceIndex, and index.</summary>
		/// <remarks>
		/// This hashCode uses only the docID, sentenceIndex, and index.
		/// See compareTo for more info.
		/// </remarks>
		public override int GetHashCode()
		{
			if (cachedHashCode != 0)
			{
				return cachedHashCode;
			}
			bool sensible = false;
			int result = 0;
			if (Get(typeof(CoreAnnotations.DocIDAnnotation)) != null)
			{
				result = Get(typeof(CoreAnnotations.DocIDAnnotation)).GetHashCode();
				sensible = true;
			}
			if (ContainsKey(typeof(CoreAnnotations.SentenceIndexAnnotation)))
			{
				result = 29 * result + Get(typeof(CoreAnnotations.SentenceIndexAnnotation)).GetHashCode();
				sensible = true;
			}
			if (ContainsKey(typeof(CoreAnnotations.IndexAnnotation)))
			{
				result = 29 * result + Get(typeof(CoreAnnotations.IndexAnnotation)).GetHashCode();
				sensible = true;
			}
			if (!sensible)
			{
				log.Info("WARNING!!!  You have hashed an IndexedWord with no docID, sentIndex or wordIndex. You will almost certainly lose");
			}
			cachedHashCode = result;
			return result;
		}

		/// <summary>
		/// NOTE: This compareTo is based on and made to be compatible with the one
		/// from IndexedFeatureLabel.
		/// </summary>
		/// <remarks>
		/// NOTE: This compareTo is based on and made to be compatible with the one
		/// from IndexedFeatureLabel.  You <em>must</em> have a DocIDAnnotation,
		/// SentenceIndexAnnotation, and IndexAnnotation for this to make sense and
		/// be guaranteed to work properly. Currently, it won't error out and will
		/// try to return something sensible if these are not defined, but that really
		/// isn't proper usage!
		/// This compareTo method is based not by value elements like the word(),
		/// but on passage position. It puts NO_WORD elements first, and then orders
		/// by document, sentence, and word index.  If these do not differ, it
		/// returns equal.
		/// </remarks>
		/// <param name="w">The IndexedWord to compare with</param>
		/// <returns>Whether this is less than w or not in the ordering</returns>
		public virtual int CompareTo(Edu.Stanford.Nlp.Ling.IndexedWord w)
		{
			if (this.Equals(Edu.Stanford.Nlp.Ling.IndexedWord.NoWord))
			{
				if (w.Equals(Edu.Stanford.Nlp.Ling.IndexedWord.NoWord))
				{
					return 0;
				}
				else
				{
					return -1;
				}
			}
			if (w.Equals(Edu.Stanford.Nlp.Ling.IndexedWord.NoWord))
			{
				return 1;
			}
			// Override the default comparator if pseudo-positions are set.
			// This is needed for splicing trees together awkwardly in OpenIE.
			if (!double.IsNaN(w.pseudoPosition) || !double.IsNaN(this.pseudoPosition))
			{
				double val = this.PseudoPosition() - w.PseudoPosition();
				if (val < 0)
				{
					return -1;
				}
				if (val > 0)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
			// Otherwise, compare using the normal doc/sentence/token index hierarchy
			string docID = this.GetString<CoreAnnotations.DocIDAnnotation>();
			int docComp = string.CompareOrdinal(docID, w.GetString<CoreAnnotations.DocIDAnnotation>());
			if (docComp != 0)
			{
				return docComp;
			}
			int sentComp = SentIndex() - w.SentIndex();
			if (sentComp != 0)
			{
				return sentComp;
			}
			int indexComp = Index() - w.Index();
			if (indexComp != 0)
			{
				return indexComp;
			}
			return CopyCount() - w.CopyCount();
		}

		/// <summary>Returns the value-tag of this label.</summary>
		public override string ToString()
		{
			return ToString(CoreLabel.OutputFormat.ValueTag);
		}

		public virtual string ToString(CoreLabel.OutputFormat format)
		{
			return label.ToString(format) + ToPrimes();
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetFromString(string labelStr)
		{
			throw new NotSupportedException("Cannot set from string");
		}

		public static ILabelFactory Factory()
		{
			return new _ILabelFactory_537();
		}

		private sealed class _ILabelFactory_537 : ILabelFactory
		{
			public _ILabelFactory_537()
			{
			}

			public ILabel NewLabel(string labelStr)
			{
				CoreLabel coreLabel = new CoreLabel();
				coreLabel.SetValue(labelStr);
				return new Edu.Stanford.Nlp.Ling.IndexedWord(coreLabel);
			}

			public ILabel NewLabel(string labelStr, int options)
			{
				return this.NewLabel(labelStr);
			}

			public ILabel NewLabel(ILabel oldLabel)
			{
				return new Edu.Stanford.Nlp.Ling.IndexedWord(oldLabel);
			}

			public ILabel NewLabelFromString(string encodedLabelStr)
			{
				throw new NotSupportedException("This code branch left blank" + " because we do not understand what this method should do.");
			}
		}

		/// <summary><inheritDoc/></summary>
		public virtual ILabelFactory LabelFactory()
		{
			return Edu.Stanford.Nlp.Ling.IndexedWord.Factory();
		}
	}
}
