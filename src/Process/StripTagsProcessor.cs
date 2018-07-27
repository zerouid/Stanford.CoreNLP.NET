using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// A <code>Processor</code> whose <code>process</code> method deletes all
	/// SGML/XML/HTML tags (tokens starting with <code>&lt;</code> and ending
	/// with <code>&gt;<code>.
	/// </summary>
	/// <remarks>
	/// A <code>Processor</code> whose <code>process</code> method deletes all
	/// SGML/XML/HTML tags (tokens starting with <code>&lt;</code> and ending
	/// with <code>&gt;<code>. Optionally, newlines can be inserted after the
	/// end of block-level tags to roughly simulate where continuous text was
	/// broken up (this helps finding sentence boundaries for example).
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public class StripTagsProcessor<L, F> : AbstractListProcessor<Word, Word, L, F>
	{
		private static readonly ICollection<string> Blocktags = Generics.NewHashSet(Arrays.AsList("blockquote", "br", "div", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "li", "ol", "p", "pre", "table", "tr", "ul"));

		/// <summary>Block-level HTML tags that are rendered with surrounding line breaks.</summary>
		public static readonly ICollection<string> blockTags = Blocktags;

		/// <summary>Whether to insert "\n" words after ending block tags.</summary>
		private bool markLineBreaks;

		/// <summary>Constructs a new StripTagsProcessor that doesn't mark line breaks.</summary>
		public StripTagsProcessor()
			: this(false)
		{
		}

		/// <summary>Constructs a new StripTagProcessor that marks line breaks as specified.</summary>
		public StripTagsProcessor(bool markLineBreaks)
		{
			SetMarkLineBreaks(markLineBreaks);
		}

		/// <summary>
		/// Returns whether the output of the processor will contain newline words
		/// ("\n") at the end of block-level tags.
		/// </summary>
		/// <returns>
		/// Whether the output of the processor will contain newline words
		/// ("\n") at the end of block-level tags.
		/// </returns>
		public virtual bool GetMarkLineBreaks()
		{
			return (markLineBreaks);
		}

		/// <summary>
		/// Sets whether the output of the processor will contain newline words
		/// ("\n") at the end of block-level tags.
		/// </summary>
		public virtual void SetMarkLineBreaks(bool markLineBreaks)
		{
			this.markLineBreaks = markLineBreaks;
		}

		/// <summary>
		/// Returns a new Document with the same meta-data as <tt>in</tt>,
		/// and the same words except tags are stripped.
		/// </summary>
		public override IList<Word> Process<_T0>(IList<_T0> @in)
		{
			IList<Word> @out = new List<Word>();
			bool justInsertedNewline = false;
			// to prevent contiguous newlines
			foreach (Word w in @in)
			{
				string ws = w.Word();
				if (ws.StartsWith("<") && ws.EndsWith(">"))
				{
					if (markLineBreaks && !justInsertedNewline)
					{
						// finds start and end of tag name (ignores brackets and /)
						// e.g. <p>, <br/>, or </table>
						//       se   s e        s    e
						int tagStartIndex = 1;
						while (tagStartIndex < ws.Length && !char.IsLetter(ws[tagStartIndex]))
						{
							tagStartIndex++;
						}
						if (tagStartIndex == ws.Length)
						{
							continue;
						}
						// no tag text
						int tagEndIndex = ws.Length - 1;
						while (tagEndIndex > tagStartIndex && !char.IsLetterOrDigit(ws[tagEndIndex]))
						{
							tagEndIndex--;
						}
						// looks up tag name in list of known block-level tags
						string tagName = Sharpen.Runtime.Substring(ws, tagStartIndex, tagEndIndex + 1).ToLower();
						if (blockTags.Contains(tagName))
						{
							@out.Add(new Word("\n"));
							// mark newline for block-level tags
							justInsertedNewline = true;
						}
					}
				}
				else
				{
					@out.Add(w);
					// normal word
					justInsertedNewline = false;
				}
			}
			return @out;
		}

		/// <summary>For internal debugging purposes only.</summary>
		public static void Main(string[] args)
		{
			new BasicDocument<string>();
			IDocument<string, Word, Word> htmlDoc = BasicDocument.Init("top text <h1>HEADING text</h1> this is <p>new paragraph<br>next line<br/>xhtml break etc.");
			System.Console.Out.WriteLine("Before:");
			System.Console.Out.WriteLine(htmlDoc);
			IDocument<string, Word, Word> txtDoc = new Edu.Stanford.Nlp.Process.StripTagsProcessor<string, Word>(true).ProcessDocument(htmlDoc);
			System.Console.Out.WriteLine("After:");
			System.Console.Out.WriteLine(txtDoc);
			IDocument<string, Word, IList<Word>> sentences = new WordToSentenceProcessor<Word>().ProcessDocument(txtDoc);
			System.Console.Out.WriteLine("Sentences:");
			System.Console.Out.WriteLine(sentences);
		}
	}
}
