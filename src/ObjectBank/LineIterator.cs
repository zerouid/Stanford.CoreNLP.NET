using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>An Iterator that returns a line of a file at a time.</summary>
	/// <remarks>
	/// An Iterator that returns a line of a file at a time.
	/// Lines are broken as determined by Java's readLine() method.
	/// The returned lines do not include the newline character.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class LineIterator<X> : AbstractIterator<X>
	{
		private readonly IFunction<string, X> op;

		private readonly BufferedReader @in;

		private X nextToken;

		public LineIterator(Reader r)
			: this(r, new IdentityFunction())
		{
		}

		public LineIterator(Reader r, IFunction<string, X> op)
		{
			// = null;
			// it seems like this can't be generified: seems a weird brokenness of Java to me! [cdm]
			this.op = op;
			@in = new BufferedReader(r);
			SetNext();
		}

		private void SetNext()
		{
			string line = null;
			try
			{
				line = @in.ReadLine();
			}
			catch (IOException ioe)
			{
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
			if (line != null)
			{
				nextToken = op.Apply(line);
			}
			else
			{
				nextToken = null;
			}
		}

		public override bool MoveNext()
		{
			return nextToken != null;
		}

		public override X Current
		{
			get
			{
				if (nextToken == null)
				{
					throw new NoSuchElementException("LineIterator reader exhausted");
				}
				X token = nextToken;
				SetNext();
				return token;
			}
		}

		public virtual object Peek()
		{
			return nextToken;
		}

		/// <summary>
		/// Returns a factory that vends LineIterators that read the contents of the
		/// given Reader, splitting on newlines.
		/// </summary>
		/// <returns>An iterator over the lines of a file</returns>
		public static IIteratorFromReaderFactory<X> GetFactory<X>()
		{
			return new LineIterator.LineIteratorFactory<X>();
		}

		/// <summary>
		/// Returns a factory that vends LineIterators that read the contents of the
		/// given Reader, splitting on newlines.
		/// </summary>
		/// <param name="op">A function to be applied to each line before it is returned</param>
		/// <returns>An iterator over the lines of a file</returns>
		public static IIteratorFromReaderFactory<X> GetFactory<X>(IFunction<string, X> op)
		{
			return new LineIterator.LineIteratorFactory<X>(op);
		}

		[System.Serializable]
		public class LineIteratorFactory<X> : IIteratorFromReaderFactory<X>
		{
			private const long serialVersionUID = 1L;

			private readonly IFunction<string, X> function;

			public LineIteratorFactory()
				: this(new IdentityFunction())
			{
			}

			public LineIteratorFactory(IFunction<string, X> op)
			{
				// it seems like this can't be generified: seems a weird brokenness of Java to me! [cdm]
				this.function = op;
			}

			public virtual IEnumerator<X> GetIterator(Reader r)
			{
				return new LineIterator<X>(r, function);
			}
		}
	}
}
