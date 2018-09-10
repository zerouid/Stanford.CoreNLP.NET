using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;







namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>
	/// An Iterator that reads the contents of a Reader, delimited by the specified
	/// delimiter, and then subsequently processed by an Function to produce
	/// Objects of type T.
	/// </summary>
	/// <author>Jenny Finkel <a href="mailto:jrfinkel@cs.stanford.edu>jrfinkel@cs.stanford.edu</a></author>
	/// <?/>
	public class DelimitRegExIterator<T> : AbstractIterator<T>
	{
		private IEnumerator<string> tokens;

		private readonly Func<string, T> op;

		private T nextToken;

		// = null;
		//TODO: not sure if this is the best way to name things...
		public static Edu.Stanford.Nlp.Objectbank.DelimitRegExIterator<string> DefaultDelimitRegExIterator(Reader @in, string delimiter)
		{
			return new Edu.Stanford.Nlp.Objectbank.DelimitRegExIterator<string>(@in, delimiter, new IdentityFunction<string>());
		}

		public DelimitRegExIterator(Reader r, string delimiter, Func<string, T> op)
		{
			this.op = op;
			BufferedReader @in = new BufferedReader(r);
			try
			{
				string line;
				StringBuilder input = new StringBuilder(10000);
				while ((line = @in.ReadLine()) != null)
				{
					input.Append(line).Append('\n');
				}
				line = input.ToString();
				Matcher m = Pattern.Compile(delimiter).Matcher(line);
				List<string> toks = new List<string>();
				int prev = 0;
				while (m.Find())
				{
					if (m.Start() == 0)
					{
						// Skip empty first part
						continue;
					}
					toks.Add(Sharpen.Runtime.Substring(line, prev, m.Start()));
					prev = m.End();
				}
				if (prev < line.Length)
				{
					// Except empty last part
					toks.Add(Sharpen.Runtime.Substring(line, prev, line.Length));
				}
				tokens = toks.GetEnumerator();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			SetNext();
		}

		private void SetNext()
		{
			nextToken = tokens.MoveNext() ? ParseString(tokens.Current) : null;
		}

		protected internal virtual T ParseString(string s)
		{
			return op.Apply(s);
		}

		public override bool MoveNext()
		{
			return nextToken != null;
		}

		public override T Current
		{
			get
			{
				if (nextToken == null)
				{
					throw new NoSuchElementException("DelimitRegExIterator exhausted");
				}
				T token = nextToken;
				SetNext();
				return token;
			}
		}

		public virtual object Peek()
		{
			return nextToken;
		}

		/// <summary>
		/// Returns a factory that vends DelimitRegExIterators that read the contents of the
		/// given Reader, splits on the specified delimiter, then returns the result.
		/// </summary>
		public static IIteratorFromReaderFactory<string> GetFactory(string delim)
		{
			return DelimitRegExIterator.DelimitRegExIteratorFactory.DefaultDelimitRegExIteratorFactory(delim);
		}

		/// <summary>
		/// Returns a factory that vends DelimitRegExIterators that reads the contents of the
		/// given Reader, splits on the specified delimiter, applies op, then returns the result.
		/// </summary>
		public static IIteratorFromReaderFactory<T> GetFactory<T>(string delim, Func<string, T> op)
		{
			return new DelimitRegExIterator.DelimitRegExIteratorFactory<T>(delim, op);
		}

		[System.Serializable]
		public class DelimitRegExIteratorFactory<T> : IIteratorFromReaderFactory<T>
		{
			private const long serialVersionUID = 6846060575832573082L;

			private readonly string delim;

			private readonly Func<string, T> op;

			/*, Serializable */
			public static DelimitRegExIterator.DelimitRegExIteratorFactory<string> DefaultDelimitRegExIteratorFactory(string delim)
			{
				return new DelimitRegExIterator.DelimitRegExIteratorFactory<string>(delim, new IdentityFunction<string>());
			}

			public DelimitRegExIteratorFactory(string delim, Func<string, T> op)
			{
				this.delim = delim;
				this.op = op;
			}

			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return new DelimitRegExIterator<T>(r, delim, op);
			}
		}
	}
}
