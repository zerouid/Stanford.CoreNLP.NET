using System;
using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A utility class for representing a token in the Simple API.</summary>
	/// <remarks>
	/// A utility class for representing a token in the Simple API.
	/// This nominally tries to conform to a
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
	/// -like interface,
	/// though many of the methods are not supported (most notably, the setters).
	/// </remarks>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class Token : IAbstractToken
	{
		/// <summary>The underlying sentence supplying the fields for this token.</summary>
		public readonly Sentence sentence;

		/// <summary>The index of this token in the underlying sentence.</summary>
		/// <remarks>The index of this token in the underlying sentence. This can be out of bounds; the sentence is assumed to be infinitely padded.</remarks>
		public readonly int index;

		/// <summary>Create a wrapper for a token, given a sentence and an index in the sentence</summary>
		public Token(Sentence sentence, int index)
		{
			this.sentence = sentence;
			this.index = index;
		}

		/// <summary>The previous token in the sentence.</summary>
		public virtual Edu.Stanford.Nlp.Simple.Token Previous()
		{
			return new Edu.Stanford.Nlp.Simple.Token(sentence, index - 1);
		}

		/// <summary>The next token in the sentence.</summary>
		public virtual Edu.Stanford.Nlp.Simple.Token Next()
		{
			return new Edu.Stanford.Nlp.Simple.Token(sentence, index + 1);
		}

		/// <seealso cref="Sentence.Word(int)"></seealso>
		public virtual string Word()
		{
			return sentence.Word(index);
		}

		public virtual void SetWord(string word)
		{
		}

		/// <summary>Return the value at the supplier, but make sure that the index is in bounds first.</summary>
		/// <remarks>
		/// Return the value at the supplier, but make sure that the index is in bounds first.
		/// If the index is out of bounds, return either '^' or '$' depending on whether it's the beginning
		/// or end of the sentence.
		/// </remarks>
		private string Pad(ISupplier<string> value)
		{
			if (index < 0)
			{
				return "^";
			}
			else
			{
				if (index >= sentence.Length())
				{
					return "$";
				}
				else
				{
					return value.Get();
				}
			}
		}

		/// <summary>Return the value at the supplier, but make sure that the index is in bounds first.</summary>
		/// <remarks>
		/// Return the value at the supplier, but make sure that the index is in bounds first.
		/// If the index is out of bounds, return
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// .
		/// </remarks>
		private Optional<E> PadOpt<E>(ISupplier<Optional<E>> value)
		{
			if (index < 0)
			{
				return Optional.Empty();
			}
			else
			{
				if (index >= sentence.Length())
				{
					return Optional.Empty();
				}
				else
				{
					return value.Get();
				}
			}
		}

		/// <seealso cref="Sentence.OriginalText(int)"></seealso>
		public virtual string OriginalText()
		{
			return Pad(null);
		}

		public virtual void SetOriginalText(string originalText)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.Lemma(int)"></seealso>
		public virtual string Lemma()
		{
			return Pad(null);
		}

		public virtual void SetLemma(string lemma)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.NerTag(int)"></seealso>
		public virtual string Ner()
		{
			return Pad(null);
		}

		public virtual void SetNER(string ner)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.NerTag(int)"></seealso>
		public virtual string NerTag()
		{
			return Ner();
		}

		/// <seealso cref="Sentence.PosTag(int)"></seealso>
		public virtual string Tag()
		{
			return Pad(null);
		}

		public virtual void SetTag(string tag)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.PosTag(int)"></seealso>
		public virtual string PosTag()
		{
			return Tag();
		}

		/// <seealso cref="Sentence.Governor(int)"></seealso>
		public virtual Optional<int> Governor()
		{
			return PadOpt(null);
		}

		/// <seealso cref="Sentence.CharacterOffsetBegin(int)"></seealso>
		public virtual int CharacterOffsetBegin()
		{
			if (index < 0)
			{
				return -1;
			}
			else
			{
				if (index >= sentence.Length())
				{
					return -1;
				}
				else
				{
					return sentence.CharacterOffsetBegin(index);
				}
			}
		}

		/// <seealso cref="Sentence.CharacterOffsetEnd(int)"></seealso>
		public virtual int CharacterOffsetEnd()
		{
			if (index < 0)
			{
				return -1;
			}
			else
			{
				if (index >= sentence.Length())
				{
					return -1;
				}
				else
				{
					return sentence.CharacterOffsetEnd(index);
				}
			}
		}

		/// <seealso cref="Sentence.Before(int)"></seealso>
		public virtual string Before()
		{
			if (index < 0)
			{
				return string.Empty;
			}
			else
			{
				if (index >= sentence.Length())
				{
					return string.Empty;
				}
				else
				{
					return sentence.Before(index);
				}
			}
		}

		public virtual void SetBefore(string before)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.After(int)"></seealso>
		public virtual string After()
		{
			if (index < 0)
			{
				return string.Empty;
			}
			else
			{
				if (index >= sentence.Length())
				{
					return string.Empty;
				}
				else
				{
					return sentence.After(index);
				}
			}
		}

		public virtual void SetAfter(string after)
		{
			throw new NotSupportedException();
		}

		/// <seealso cref="Sentence.IncomingDependencyLabel(int)"></seealso>
		public virtual Optional<string> IncomingDependencyLabel()
		{
			return PadOpt(null);
		}

		public virtual string DocID()
		{
			return sentence.document.Docid().OrElse(string.Empty);
		}

		public virtual void SetDocID(string docID)
		{
			throw new NotSupportedException();
		}

		public virtual int SentIndex()
		{
			return sentence.SentenceIndex();
		}

		public virtual void SetSentIndex(int sentIndex)
		{
			throw new NotSupportedException();
		}

		public virtual int Index()
		{
			return index;
		}

		public virtual void SetIndex(int index)
		{
			throw new NotSupportedException();
		}

		public virtual int BeginPosition()
		{
			return CharacterOffsetBegin();
		}

		public virtual void SetBeginPosition(int beginPos)
		{
			throw new NotSupportedException();
		}

		public virtual int EndPosition()
		{
			return CharacterOffsetEnd();
		}

		public virtual void SetEndPosition(int endPos)
		{
			throw new NotSupportedException();
		}
	}
}
