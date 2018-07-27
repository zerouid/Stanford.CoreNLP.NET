using System.Text;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>
	/// Implements the ACE
	/// <literal><charseq></literal>
	/// construct.
	/// </summary>
	/// <author>David McClosky</author>
	/// <author>Andrey Gusev</author>
	public class AceCharSeq
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceCharSeq));

		/// <summary>The exact text matched by this sequence</summary>
		private string mText;

		/// <summary>Offset in the document stream</summary>
		private Span mByteOffset;

		/// <summary>Span of tokens that match this char sequence</summary>
		private Span mTokenOffset;

		/// <summary>Token that incorporates this whole char sequence, e.g.</summary>
		/// <remarks>
		/// Token that incorporates this whole char sequence, e.g.
		/// "George_Bush/NNP_NNP" for the text "George Bush" XXX: not used anymore
		/// </remarks>
		public AceCharSeq(string text, int start, int end)
		{
			// private AceToken mPhrase;
			mText = text;
			mByteOffset = new Span(start, end);
			mTokenOffset = null;
		}

		// mPhrase = null;
		public virtual string ToXml(string label, int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AceElement.AppendOffset(buffer, offset);
			buffer.Append('<').Append(label).Append(">\n");
			AceElement.AppendOffset(buffer, offset + 2);
			buffer.Append("<charseq START=\"").Append(mByteOffset.Start()).Append("\" END=\"").Append(mByteOffset.End()).Append("\">");
			buffer.Append(mText).Append("</charseq>");
			buffer.Append('\n');
			AceElement.AppendOffset(buffer, offset);
			buffer.Append("</").Append(label).Append('>');
			return buffer.ToString();
		}

		public virtual string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AceElement.AppendOffset(buffer, offset + 2);
			buffer.Append("<charseq START=\"").Append(mByteOffset.Start()).Append("\" END=\"").Append(mByteOffset.End()).Append("\">");
			buffer.Append(mText).Append("</charseq>");
			return buffer.ToString();
		}

		public virtual string GetText()
		{
			return mText;
		}

		public virtual int GetByteStart()
		{
			return mByteOffset.Start();
		}

		public virtual int GetByteEnd()
		{
			return mByteOffset.End();
		}

		public virtual Span GetByteOffset()
		{
			return mByteOffset;
		}

		public virtual int GetTokenStart()
		{
			if (mTokenOffset == null)
			{
				return -1;
			}
			return mTokenOffset.Start();
		}

		public virtual int GetTokenEnd()
		{
			if (mTokenOffset == null)
			{
				return -1;
			}
			return mTokenOffset.End();
		}

		public virtual Span GetTokenOffset()
		{
			return mTokenOffset;
		}

		// public AceToken getPhrase() { return mPhrase; }
		/// <summary>
		/// Matches this char seq against the full token stream As a result of this
		/// method mTokenOffset is initialized
		/// </summary>
		/// <exception cref="Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.MatchException"/>
		public virtual void Match(Vector<AceToken> tokens)
		{
			int start = -1;
			int end = -1;
			for (int i = 0; i < tokens.Count; i++)
			{
				//
				// we found the starting token
				//
				if (tokens[i].GetByteOffset().Start() == mByteOffset.Start())
				{
					start = i;
				}
				else
				{
					//
					// we do not tokenize dashed-words, hence the start may be inside a token
					// e.g. Saddam => pro-Saddam
					// the same situation will happen due to (uncommon) annotation errors
					//
					if (mByteOffset.Start() > tokens[i].GetByteOffset().Start() && mByteOffset.Start() < tokens[i].GetByteOffset().End())
					{
						start = i;
					}
				}
				//
				// we found the ending token
				// Note: ACE is inclusive for the end position, my tokenization is not
				// in ACE: end position == position of last byte in token
				// in .sgm.pre: end position == position of last byte + 1
				//
				if (tokens[i].GetByteOffset().End() == mByteOffset.End() + 1)
				{
					end = i;
					break;
				}
				else
				{
					//
					// we do not tokenize dashed-words, hence the end may be inside a token
					// e.g. Conference => Conference-leading
					// the same situation will happen due to (uncommon) annotation errors
					//
					if (mByteOffset.End() >= tokens[i].GetByteOffset().Start() && mByteOffset.End() < tokens[i].GetByteOffset().End() - 1)
					{
						end = i;
						break;
					}
				}
			}
			if (start >= 0 && end >= 0)
			{
				mTokenOffset = new Span(start, end);
			}
			else
			{
				// mPhrase = makePhrase(tokens, mTokenOffset);
				throw new MatchException("Match failed!");
			}
		}

		public override string ToString()
		{
			return "AceCharSeq [mByteOffset=" + mByteOffset + ", mText=" + mText + ", mTokenOffset=" + mTokenOffset + ']';
		}
		/*
		* private AceToken makePhrase(Vector<AceToken> tokens, Span span) {
		* StringBuffer word = new StringBuffer(); StringBuffer lemma = new
		* StringBuffer(); StringBuffer pos = new StringBuffer(); StringBuffer chunk =
		* new StringBuffer(); StringBuffer nerc = new StringBuffer();
		*
		* for(int i = span.mStart; i <= span.mEnd; i ++){ if(i > span.mStart){
		* word.append("_"); lemma.append("_"); pos.append("_"); chunk.append("_");
		* nerc.append("_"); }
		*
		* AceToken tok = tokens.get(i);
		* word.append(AceToken.WORDS.get(tok.getWord()));
		* lemma.append(AceToken.LEMMAS.get(tok.getLemma()));
		* pos.append(AceToken.OTHERS.get(tok.getPos()));
		* chunk.append(AceToken.OTHERS.get(tok.getChunk()));
		* nerc.append(AceToken.OTHERS.get(tok.getNerc())); }
		*
		* AceToken phrase = new AceToken(word.toString(), lemma.toString(),
		* pos.toString(), chunk.toString(), nerc.toString(), null, null, -1);
		*
		* //log.info("Constructed phrase: " + phrase.display()); return
		* phrase; }
		*/
	}
}
