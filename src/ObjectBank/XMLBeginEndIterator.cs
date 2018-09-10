using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>
	/// A class which iterates over Strings occurring between the begin and end of
	/// a selected tag or tags.
	/// </summary>
	/// <remarks>
	/// A class which iterates over Strings occurring between the begin and end of
	/// a selected tag or tags. The element is specified by a regexp, matched
	/// against the name of the element (i.e., excluding the angle bracket
	/// characters) using
	/// <c>matches()</c>
	/// ).
	/// The class ignores all other characters in the input Reader.
	/// There are a few different ways to modify the output of the
	/// XMLBeginEndIterator.  One way is to ask it to keep internal tags;
	/// if
	/// <c>keepInternalTags</c>
	/// is set, then
	/// <literal><text>A<foo>B</text></literal>
	/// will be printed as
	/// <literal>A<foo>B</literal>
	/// .
	/// Another is to tell it to keep delimiting tags; in the above example,
	/// <literal><text></literal>
	/// will be kept as well.
	/// Finally, you can ask it to keep track of the nesting depth; the
	/// ordinary behavior of this iterator is to close all tags with just
	/// one close tag.  This is incorrect XML behavior, but is kept in case
	/// any code relies on it.  If
	/// <c>countDepth</c>
	/// is set, though,
	/// the iterator keeps track of how much it has nested.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public class XMLBeginEndIterator<E> : AbstractIterator<E>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Objectbank.XMLBeginEndIterator));

		private readonly Pattern tagNamePattern;

		private readonly BufferedReader inputReader;

		private readonly Func<string, E> op;

		private readonly bool keepInternalTags;

		private readonly bool keepDelimitingTags;

		private readonly bool countDepth;

		private E nextToken;

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp)
			: this(@in, tagNameRegexp, new IdentityFunction(), false)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, bool keepInternalTags)
			: this(@in, tagNameRegexp, new IdentityFunction(), keepInternalTags)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, Func<string, E> op, bool keepInternalTags)
			: this(@in, tagNameRegexp, op, keepInternalTags, false)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, bool keepInternalTags, bool keepDelimitingTags)
			: this(@in, tagNameRegexp, new IdentityFunction(), keepInternalTags, keepDelimitingTags)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, bool keepInternalTags, bool keepDelimitingTags, bool countDepth)
			: this(@in, tagNameRegexp, new IdentityFunction(), keepInternalTags, keepDelimitingTags, countDepth)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, Func<string, E> op, bool keepInternalTags, bool keepDelimitingTags)
			: this(@in, tagNameRegexp, op, keepInternalTags, keepDelimitingTags, false)
		{
		}

		public XMLBeginEndIterator(Reader @in, string tagNameRegexp, Func<string, E> op, bool keepInternalTags, bool keepDelimitingTags, bool countDepth)
		{
			// stores the read-ahead next token to return
			// Can't seem to do IdentityFunction without warning!
			this.tagNamePattern = Pattern.Compile(tagNameRegexp);
			this.op = op;
			this.keepInternalTags = keepInternalTags;
			this.keepDelimitingTags = keepDelimitingTags;
			this.countDepth = countDepth;
			this.inputReader = new BufferedReader(@in);
			SetNext();
		}

		private void SetNext()
		{
			string s = GetNext();
			nextToken = ParseString(s);
		}

		// returns null if there is no next object
		private string GetNext()
		{
			StringBuilder result = new StringBuilder();
			try
			{
				XMLUtils.XMLTag tag;
				do
				{
					// String text =
					XMLUtils.ReadUntilTag(inputReader);
					// there may or may not be text before the next tag, but we discard it
					//        System.out.println("outside text: " + text );
					tag = XMLUtils.ReadAndParseTag(inputReader);
					//        System.out.println("outside tag: " + tag);
					if (tag == null)
					{
						return null;
					}
				}
				while (!tagNamePattern.Matcher(tag.name).Matches() || tag.isEndTag || tag.isSingleTag);
				// couldn't find any more tags, so no more elements
				if (keepDelimitingTags)
				{
					result.Append(tag.ToString());
				}
				int depth = 1;
				while (true)
				{
					string text = XMLUtils.ReadUntilTag(inputReader);
					if (text != null)
					{
						// if the text isn't null, we append it
						//        System.out.println("inside text: " + text );
						result.Append(text);
					}
					string tagString = XMLUtils.ReadTag(inputReader);
					tag = XMLUtils.ParseTag(tagString);
					if (tag == null)
					{
						return null;
					}
					// unexpected end of this element, so no more elements
					if (tagNamePattern.Matcher(tag.name).Matches() && tag.isEndTag)
					{
						if ((countDepth && depth == 1) || !countDepth)
						{
							if (keepDelimitingTags)
							{
								result.Append(tagString);
							}
							// this is our end tag so we stop
							break;
						}
						else
						{
							--depth;
							if (keepInternalTags)
							{
								result.Append(tagString);
							}
						}
					}
					else
					{
						if (tagNamePattern.Matcher(tag.name).Matches() && !tag.isEndTag && !tag.isSingleTag && countDepth)
						{
							++depth;
							if (keepInternalTags)
							{
								result.Append(tagString);
							}
						}
						else
						{
							// not our end tag, so we optionally append it and keep going
							if (keepInternalTags)
							{
								result.Append(tagString);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return result.ToString();
		}

		protected internal virtual E ParseString(string s)
		{
			return op.Apply(s);
		}

		public override bool MoveNext()
		{
			return nextToken != null;
		}

		public override E Current
		{
			get
			{
				if (nextToken == null)
				{
					throw new NoSuchElementException();
				}
				E token = nextToken;
				SetNext();
				return token;
			}
		}

		/* ---
		
		// Omit methods that made this class a Tokenizer.
		// Just have it an Iterator as the name suggests.
		// That's all that was used, and this simplifies
		// inter-package dependencies.
		
		public E peek() {
		return nextToken;
		}
		
		* Returns pieces of text in element as a List of tokens.
		*
		* @return A list of all tokens remaining in the underlying Reader
		*
		public List<E> tokenize() {
		// System.out.println("tokenize called");
		List<E> result = new ArrayList<E>();
		while (hasNext()) {
		result.add(next());
		}
		return result;
		}
		
		--- */
		/// <summary>
		/// Returns a factory that vends BeginEndIterators that reads the contents of
		/// the given Reader, extracts text between the specified Strings, then
		/// returns the result.
		/// </summary>
		/// <param name="tag">The tag the XMLBeginEndIterator will match on</param>
		/// <returns>The IteratorFromReaderFactory</returns>
		public static IIteratorFromReaderFactory<string> GetFactory(string tag)
		{
			return new XMLBeginEndIterator.XMLBeginEndIteratorFactory<string>(tag, new IdentityFunction<string>(), false, false);
		}

		public static IIteratorFromReaderFactory<string> GetFactory(string tag, bool keepInternalTags, bool keepDelimitingTags)
		{
			return new XMLBeginEndIterator.XMLBeginEndIteratorFactory<string>(tag, new IdentityFunction<string>(), keepInternalTags, keepDelimitingTags);
		}

		public static IIteratorFromReaderFactory<E> GetFactory<E>(string tag, Func<string, E> op)
		{
			return new XMLBeginEndIterator.XMLBeginEndIteratorFactory<E>(tag, op, false, false);
		}

		public static IIteratorFromReaderFactory<E> GetFactory<E>(string tag, Func<string, E> op, bool keepInternalTags, bool keepDelimitingTags)
		{
			return new XMLBeginEndIterator.XMLBeginEndIteratorFactory<E>(tag, op, keepInternalTags, keepDelimitingTags);
		}

		[System.Serializable]
		internal class XMLBeginEndIteratorFactory<E> : IIteratorFromReaderFactory<E>
		{
			private readonly string tag;

			private readonly Func<string, E> op;

			private readonly bool keepInternalTags;

			private readonly bool keepDelimitingTags;

			public XMLBeginEndIteratorFactory(string tag, Func<string, E> op, bool keepInternalTags, bool keepDelimitingTags)
			{
				this.tag = tag;
				this.op = op;
				this.keepInternalTags = keepInternalTags;
				this.keepDelimitingTags = keepDelimitingTags;
			}

			public virtual IEnumerator<E> GetIterator(Reader r)
			{
				return new XMLBeginEndIterator<E>(r, tag, op, keepInternalTags, keepDelimitingTags);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 3)
			{
				log.Info("usage: XMLBeginEndIterator file element keepInternalBoolean");
				return;
			}
			Reader @in = new FileReader(args[0]);
			IEnumerator<string> iter = new XMLBeginEndIterator<string>(@in, args[1], Sharpen.Runtime.EqualsIgnoreCase(args[2], "true"));
			while (iter.MoveNext())
			{
				string s = iter.Current;
				System.Console.Out.WriteLine("*************************************************");
				System.Console.Out.WriteLine(s);
			}
			@in.Close();
		}
	}
}
