using System;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>An abstract tokenizer.</summary>
	/// <remarks>
	/// An abstract tokenizer. Tokenizers extending AbstractTokenizer need only
	/// implement the
	/// <c>getNext()</c>
	/// method. This implementation does not
	/// allow null tokens, since
	/// null is used in the protected nextToken field to signify that no more
	/// tokens are available.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public abstract class AbstractTokenizer<T> : ITokenizer<T>
	{
		/// <summary>For tokenizing carriage returns.</summary>
		/// <remarks>
		/// For tokenizing carriage returns.
		/// We return this token as a representation of newlines when a tokenizer has the option
		/// <c>tokenizeNLs = true</c>
		/// . It is assumed that no tokenizer allows *NL* as a token.
		/// This is certainly true for PTBTokenizer-derived tokenizers, where the asterisks would
		/// become separate tokens.
		/// </remarks>
		public const string NewlineToken = "*NL*";

		protected internal T nextToken;

		// import edu.stanford.nlp.util.logging.Redwood;
		// /** A logger for this class */
		// private static final Redwood.RedwoodChannels log = Redwood.channels(AbstractTokenizer.class);
		// = null;
		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal abstract T GetNext();

		/// <summary>Returns the next token from this Tokenizer.</summary>
		/// <returns>the next token in the token stream.</returns>
		/// <exception cref="Java.Util.NoSuchElementException">if the token stream has no more tokens.</exception>
		public virtual T Current
		{
			get
			{
				if (nextToken == null)
				{
					nextToken = GetNext();
				}
				T result = nextToken;
				nextToken = null;
				if (result == null)
				{
					throw new NoSuchElementException();
				}
				return result;
			}
		}

		/// <summary>
		/// Returns
		/// <see langword="true"/>
		/// if this Tokenizer has more elements.
		/// </summary>
		public virtual bool MoveNext()
		{
			if (nextToken == null)
			{
				nextToken = GetNext();
			}
			return nextToken != null;
		}

		/// <summary>This is an optional operation, by default not supported.</summary>
		public virtual void Remove()
		{
			throw new NotSupportedException();
		}

		/// <summary>This is an optional operation, by default supported.</summary>
		/// <returns>The next token in the token stream.</returns>
		/// <exception cref="Java.Util.NoSuchElementException">if the token stream has no more tokens.</exception>
		public virtual T Peek()
		{
			if (nextToken == null)
			{
				nextToken = GetNext();
			}
			if (nextToken == null)
			{
				throw new NoSuchElementException();
			}
			return nextToken;
		}

		private const int DefaultTokenizeListSize = 64;

		// Assume that the text we are being asked to tokenize is usually more than 10 tokens; save 5 reallocations
		/// <summary>Returns text as a List of tokens.</summary>
		/// <returns>A list of all tokens remaining in the underlying Reader</returns>
		public virtual IList<T> Tokenize()
		{
			List<T> result = new List<T>(DefaultTokenizeListSize);
			while (MoveNext())
			{
				result.Add(Current);
			}
			// log.info("tokenize() produced " + result);
			// if it was tiny, reallocate small
			if (result.Count <= DefaultTokenizeListSize / 4)
			{
				result.TrimToSize();
			}
			return result;
		}
	}
}
