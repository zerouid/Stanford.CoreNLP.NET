using System;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IE.Machinereading.Common;
using Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Stores the ACE elements annotated in this document</summary>
	public class AceDocument : AceElement
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument));

		/// <summary>Prefix of the files from where this doc was created</summary>
		private string mPrefix;

		/// <summary>Value of the SOURCE XML field</summary>
		private string mSource;

		/// <summary>All entities</summary>
		private IDictionary<string, AceEntity> mEntities;

		/// <summary>All entity mentions</summary>
		private IDictionary<string, AceEntityMention> mEntityMentions;

		/// <summary>All entity mentions in a given sentence, sorted in textual order</summary>
		private List<List<AceEntityMention>> mSentenceEntityMentions;

		/// <summary>All relations</summary>
		private IDictionary<string, AceRelation> mRelations;

		/// <summary>All relation mentions</summary>
		private IDictionary<string, AceRelationMention> mRelationMentions;

		/// <summary>All relation mentions in a given sentence, sorted in textual order</summary>
		private List<List<AceRelationMention>> mSentenceRelationMentions;

		/// <summary>All events</summary>
		private IDictionary<string, AceEvent> mEvents;

		/// <summary>All event mentions</summary>
		private IDictionary<string, AceEventMention> mEventMentions;

		/// <summary>All event mentions in a given sentence, sorted in textual order</summary>
		private List<List<AceEventMention>> mSentenceEventMentions;

		/// <summary>The list of all tokens in the document, sorted in textual order</summary>
		private Vector<AceToken> mTokens;

		/// <summary>List of all sentences in the document</summary>
		private IList<IList<AceToken>> mSentences;

		/// <summary>The raw byte document, no preprocessing</summary>
		private string mRawBuffer;

		internal static Logger mLog = Logger.GetLogger(typeof(AceReader).FullName);

		public AceDocument(string id)
			: base(id)
		{
			mEntities = Generics.NewHashMap();
			mEntityMentions = Generics.NewHashMap();
			mSentenceEntityMentions = new List<List<AceEntityMention>>();
			mRelations = Generics.NewHashMap();
			mRelationMentions = Generics.NewHashMap();
			mSentenceRelationMentions = new List<List<AceRelationMention>>();
			mEvents = Generics.NewHashMap();
			mEventMentions = Generics.NewHashMap();
			mSentenceEventMentions = new List<List<AceEventMention>>();
			mTokens = new Vector<AceToken>();
		}

		public virtual void SetPrefix(string p)
		{
			mPrefix = p;
			SetSource(mPrefix);
		}

		public virtual string GetPrefix()
		{
			return mPrefix;
		}

		public virtual void SetSource(string p)
		{
			if (p.IndexOf("bc/") >= 0)
			{
				mSource = "broadcast conversation";
			}
			else
			{
				if (p.IndexOf("bn/") >= 0)
				{
					mSource = "broadcast news";
				}
				else
				{
					if (p.IndexOf("cts/") >= 0)
					{
						mSource = "telephone";
					}
					else
					{
						if (p.IndexOf("nw/") >= 0)
						{
							mSource = "newswire";
						}
						else
						{
							if (p.IndexOf("un/") >= 0)
							{
								mSource = "usenet";
							}
							else
							{
								if (p.IndexOf("wl/") >= 0)
								{
									mSource = "weblog";
								}
								else
								{
									log.Info("WARNING: Unknown source for doc: " + p);
									mSource = "none";
								}
							}
						}
					}
				}
			}
		}

		public virtual int GetSentenceCount()
		{
			return mSentenceEntityMentions.Count;
		}

		public virtual List<AceEntityMention> GetEntityMentions(int sent)
		{
			return mSentenceEntityMentions[sent];
		}

		public virtual List<List<AceEntityMention>> GetAllEntityMentions()
		{
			return mSentenceEntityMentions;
		}

		public virtual List<AceRelationMention> GetRelationMentions(int sent)
		{
			return mSentenceRelationMentions[sent];
		}

		public virtual List<List<AceRelationMention>> GetAllRelationMentions()
		{
			return mSentenceRelationMentions;
		}

		public virtual List<AceEventMention> GetEventMentions(int sent)
		{
			return mSentenceEventMentions[sent];
		}

		public virtual List<List<AceEventMention>> GetAllEventMentions()
		{
			return mSentenceEventMentions;
		}

		public virtual AceEntity GetEntity(string id)
		{
			return mEntities[id];
		}

		public virtual ICollection<string> GetKeySetEntities()
		{
			return mEntities.Keys;
		}

		public virtual void AddEntity(AceEntity e)
		{
			mEntities[e.GetId()] = e;
		}

		public virtual IDictionary<string, AceEntityMention> GetEntityMentions()
		{
			return mEntityMentions;
		}

		public virtual AceEntityMention GetEntityMention(string id)
		{
			return mEntityMentions[id];
		}

		public virtual void AddEntityMention(AceEntityMention em)
		{
			mEntityMentions[em.GetId()] = em;
		}

		public virtual AceRelation GetRelation(string id)
		{
			return mRelations[id];
		}

		public virtual void AddRelation(AceRelation r)
		{
			mRelations[r.GetId()] = r;
		}

		public virtual IDictionary<string, AceRelationMention> GetRelationMentions()
		{
			return mRelationMentions;
		}

		public virtual AceRelationMention GetRelationMention(string id)
		{
			return mRelationMentions[id];
		}

		public virtual void AddRelationMention(AceRelationMention e)
		{
			mRelationMentions[e.GetId()] = e;
		}

		public virtual AceEvent GetEvent(string id)
		{
			return mEvents[id];
		}

		public virtual void AddEvent(AceEvent r)
		{
			mEvents[r.GetId()] = r;
		}

		public virtual IDictionary<string, AceEventMention> GetEventMentions()
		{
			return mEventMentions;
		}

		public virtual AceEventMention GetEventMention(string id)
		{
			return mEventMentions[id];
		}

		public virtual void AddEventMention(AceEventMention e)
		{
			mEventMentions[e.GetId()] = e;
		}

		public virtual void AddToken(AceToken t)
		{
			mTokens.Add(t);
		}

		public virtual int GetTokenCount()
		{
			return mTokens.Count;
		}

		public virtual AceToken GetToken(int i)
		{
			return mTokens[i];
		}

		public virtual IList<AceToken> GetSentence(int index)
		{
			return mSentences[index];
		}

		public virtual IList<IList<AceToken>> GetSentences()
		{
			return mSentences;
		}

		public virtual void SetSentences(IList<IList<AceToken>> sentences)
		{
			mSentences = sentences;
		}

		public override string ToString()
		{
			return ToXml(0);
		}

		public virtual string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AppendOffset(buffer, offset);
			buffer.Append("<?xml version=\"1.0\"?>\n");
			AppendOffset(buffer, offset);
			buffer.Append("<!DOCTYPE source_file SYSTEM \"apf.v5.1.2.dtd\">\n");
			AppendOffset(buffer, offset);
			buffer.Append("<source_file URI=\"" + mId + ".sgm\" SOURCE=\"" + mSource + "\" TYPE=\"text\" AUTHOR=\"LDC\" ENCODING=\"UTF-8\">\n");
			AppendOffset(buffer, offset);
			buffer.Append("<document DOCID=\"" + GetId() + "\">\n");
			// display all entities
			ICollection<string> entKeys = mEntities.Keys;
			foreach (string key in entKeys)
			{
				AceEntity e = mEntities[key];
				buffer.Append(e.ToXml(offset));
				buffer.Append("\n");
			}
			// display all relations
			ICollection<string> relKeys = mRelations.Keys;
			foreach (string key_1 in relKeys)
			{
				AceRelation r = mRelations[key_1];
				if (!r.GetType().Equals(AceRelation.NilLabel))
				{
					buffer.Append(r.ToXml(offset));
					buffer.Append("\n");
				}
			}
			// TODO: display all events
			AppendOffset(buffer, offset);
			buffer.Append("</document>\n");
			AppendOffset(buffer, offset);
			buffer.Append("</source_file>\n");
			return buffer.ToString();
		}

		private string TokensWithByteSpan(int start, int end)
		{
			StringBuilder buf = new StringBuilder();
			bool doPrint = false;
			buf.Append("...");
			foreach (AceToken mToken in mTokens)
			{
				// start printing
				if (doPrint == false && mToken.GetByteOffset().Start() > start - 20 && mToken.GetByteOffset().End() < end)
				{
					doPrint = true;
				}
				else
				{
					// end printing
					if (doPrint == true && mToken.GetByteOffset().Start() > end + 20)
					{
						doPrint = false;
					}
				}
				if (doPrint)
				{
					buf.Append(" " + mToken.Display());
				}
			}
			buf.Append("...");
			return buf.ToString();
		}

		/// <summary>Matches all relevant mentions, i.e.</summary>
		/// <remarks>
		/// Matches all relevant mentions, i.e. entities and anchors, to tokens Note:
		/// entity mentions may match with multiple tokens!
		/// </remarks>
		public virtual void MatchCharSeqs(string filePrefix)
		{
			//
			// match the head and extent of entity mentions
			//
			ICollection<string> keys = mEntityMentions.Keys;
			foreach (string key in keys)
			{
				AceEntityMention m = mEntityMentions[key];
				//
				// match the head charseq to 1+ phrase(s)
				//
				try
				{
					m.GetHead().Match(mTokens);
				}
				catch (MatchException)
				{
					mLog.Severe("READER ERROR: Failed to match entity mention head: " + "[" + m.GetHead().GetText() + ", " + m.GetHead().GetByteStart() + ", " + m.GetHead().GetByteEnd() + "]");
					mLog.Severe("Document tokens: " + TokensWithByteSpan(m.GetHead().GetByteStart(), m.GetHead().GetByteEnd()));
					mLog.Severe("Document prefix: " + filePrefix);
					System.Environment.Exit(1);
				}
				//
				// match the extent charseq to 1+ phrase(s)
				//
				try
				{
					m.GetExtent().Match(mTokens);
				}
				catch (MatchException)
				{
					mLog.Severe("READER ERROR: Failed to match entity mention extent: " + "[" + m.GetExtent().GetText() + ", " + m.GetExtent().GetByteStart() + ", " + m.GetExtent().GetByteEnd() + "]");
					mLog.Severe("Document tokens: " + TokensWithByteSpan(m.GetExtent().GetByteStart(), m.GetExtent().GetByteEnd()));
					System.Environment.Exit(1);
				}
				//
				// set the head word of the mention
				//
				m.DetectHeadToken(this);
			}
			// we need to do this for events as well since they may not have any AceEntityMentions associated with them (if they have no arguments)
			ICollection<string> eventKeys = mEventMentions.Keys;
			foreach (string key_1 in eventKeys)
			{
				AceEventMention m = mEventMentions[key_1];
				//
				// match the extent charseq to 1+ phrase(s)
				//
				try
				{
					m.GetExtent().Match(mTokens);
				}
				catch (MatchException)
				{
					mLog.Severe("READER ERROR: Failed to match event mention extent: " + "[" + m.GetExtent().GetText() + ", " + m.GetExtent().GetByteStart() + ", " + m.GetExtent().GetByteEnd() + "]");
					mLog.Severe("Document tokens: " + TokensWithByteSpan(m.GetExtent().GetByteStart(), m.GetExtent().GetByteEnd()));
					System.Environment.Exit(1);
				}
			}
		}

		public const string XmlExt = ".apf.xml";

		public const string OrigExt = ".sgm";

		/// <summary>Parses an ACE document.</summary>
		/// <remarks>
		/// Parses an ACE document. Works in the following steps: (a) reads both the
		/// XML annotations; (b) reads the tokens; (c) matches the tokens against the
		/// annotations (d) constructs mSentenceEntityMentions and
		/// mRelationEntityMentions
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument ParseDocument(string prefix, bool usePredictedBoundaries)
		{
			mLog.Fine("Reading document " + prefix);
			Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument doc = null;
			//
			// read the ACE XML annotations
			//
			if (usePredictedBoundaries == false)
			{
				doc = AceDomReader.ParseDocument(new File(prefix + XmlExt));
			}
			else
			{
				// log.info("Parsed " + doc.getEntityMentions().size() +
				// " entities in document " + prefix);
				//
				// will use the predicted entity boundaries (see below)
				//
				int lastSlash = prefix.LastIndexOf(File.separator);
				System.Diagnostics.Debug.Assert((lastSlash > 0 && lastSlash < prefix.Length - 1));
				string id = Sharpen.Runtime.Substring(prefix, lastSlash + 1);
				// log.info(id + ": " + prefix);
				doc = new Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument(id);
			}
			doc.SetPrefix(prefix);
			//
			// read the raw byte stream
			//
			string trueCasedFileName = prefix + OrigExt + ".truecase";
			if ((new File(trueCasedFileName).Exists()))
			{
				mLog.Severe("Using truecased file: " + trueCasedFileName);
				doc.ReadRawBytes(trueCasedFileName);
			}
			else
			{
				doc.ReadRawBytes(prefix + OrigExt);
			}
			//
			// read the AceTokens
			//
			int offsetToSubtract = 0;
			IList<IList<AceToken>> sentences = AceSentenceSegmenter.TokenizeAndSegmentSentences(prefix);
			doc.SetSentences(sentences);
			foreach (IList<AceToken> sentence in sentences)
			{
				foreach (AceToken token in sentence)
				{
					offsetToSubtract = token.AdjustPhrasePositions(offsetToSubtract, token.GetLiteral());
					doc.AddToken(token);
				}
			}
			//
			// match char sequences to phrases
			//
			doc.MatchCharSeqs(prefix);
			//
			// construct the mEntityMentions matrix
			//
			ICollection<string> entityKeys = doc.mEntityMentions.Keys;
			int sentence_1;
			foreach (string key in entityKeys)
			{
				AceEntityMention em = doc.mEntityMentions[key];
				sentence_1 = doc.mTokens[em.GetHead().GetTokenStart()].GetSentence();
				// adjust the number of rows if necessary
				while (sentence_1 >= doc.mSentenceEntityMentions.Count)
				{
					doc.mSentenceEntityMentions.Add(new List<AceEntityMention>());
					doc.mSentenceRelationMentions.Add(new List<AceRelationMention>());
					doc.mSentenceEventMentions.Add(new List<AceEventMention>());
				}
				// store the entity mentions in increasing order:
				// (a) of the start position of their head
				// (b) if start is the same, in increasing order of the head end
				List<AceEntityMention> sentEnts = doc.mSentenceEntityMentions[sentence_1];
				bool added = false;
				for (int i = 0; i < sentEnts.Count; i++)
				{
					AceEntityMention crt = sentEnts[i];
					if ((crt.GetHead().GetTokenStart() > em.GetHead().GetTokenStart()) || (crt.GetHead().GetTokenStart() == em.GetHead().GetTokenStart() && crt.GetHead().GetTokenEnd() > em.GetHead().GetTokenEnd()))
					{
						sentEnts.Add(i, em);
						added = true;
						break;
					}
				}
				if (!added)
				{
					sentEnts.Add(em);
				}
			}
			// 
			// construct the mRelationMentions matrix
			//
			ICollection<string> relKeys = doc.mRelationMentions.Keys;
			foreach (string key_1 in relKeys)
			{
				AceRelationMention rm = doc.mRelationMentions[key_1];
				sentence_1 = doc.mTokens[rm.GetArg(0).GetHead().GetTokenStart()].GetSentence();
				//
				// no need to adjust the number of rows: was done above
				//
				// store the relation mentions in increasing order
				// (a) of the start position of their head, or
				// (b) if start is the same, in increasing order of ends
				List<AceRelationMention> sentRels = doc.mSentenceRelationMentions[sentence_1];
				bool added = false;
				for (int i = 0; i < sentRels.Count; i++)
				{
					AceRelationMention crt = sentRels[i];
					if ((crt.GetMinTokenStart() > rm.GetMinTokenStart()) || (crt.GetMinTokenStart() == rm.GetMinTokenStart() && crt.GetMaxTokenEnd() > rm.GetMaxTokenEnd()))
					{
						sentRels.Add(i, rm);
						added = true;
						break;
					}
				}
				if (!added)
				{
					sentRels.Add(rm);
				}
			}
			// 
			// construct the mEventMentions matrix
			//
			ICollection<string> eventKeys = doc.mEventMentions.Keys;
			foreach (string key_2 in eventKeys)
			{
				AceEventMention em = doc.mEventMentions[key_2];
				sentence_1 = doc.mTokens[em.GetMinTokenStart()].GetSentence();
				/*
				* adjust the number of rows if necessary -- if you're wondering why we do
				* this here again, (after we've done it for entities) it's because we can
				* have an event with no entities near the end of the document and thus
				* won't have created rows in mSentence*Mentions
				*/
				while (sentence_1 >= doc.mSentenceEntityMentions.Count)
				{
					doc.mSentenceEntityMentions.Add(new List<AceEntityMention>());
					doc.mSentenceRelationMentions.Add(new List<AceRelationMention>());
					doc.mSentenceEventMentions.Add(new List<AceEventMention>());
				}
				// store the event mentions in increasing order
				// (a) first, event mentions with no arguments
				// (b) then by the start position of their head, or
				// (c) if start is the same, in increasing order of ends
				List<AceEventMention> sentEvents = doc.mSentenceEventMentions[sentence_1];
				bool added = false;
				for (int i = 0; i < sentEvents.Count; i++)
				{
					AceEventMention crt = sentEvents[i];
					if ((crt.GetMinTokenStart() > em.GetMinTokenStart()) || (crt.GetMinTokenStart() == em.GetMinTokenStart() && crt.GetMaxTokenEnd() > em.GetMaxTokenEnd()))
					{
						sentEvents.Add(i, em);
						added = true;
						break;
					}
				}
				if (!added)
				{
					sentEvents.Add(em);
				}
			}
			return doc;
		}

		//
		// heeyoung : skip relation, event parsing part - for ACE2004 
		//
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument ParseDocument(string prefix, bool usePredictedBoundaries, string AceVersion)
		{
			mLog.Fine("Reading document " + prefix);
			Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument doc = null;
			//
			// read the ACE XML annotations
			//
			if (usePredictedBoundaries == false)
			{
				doc = AceDomReader.ParseDocument(new File(prefix + XmlExt));
			}
			else
			{
				// log.info("Parsed " + doc.getEntityMentions().size() +
				// " entities in document " + prefix);
				//
				// will use the predicted entity boundaries (see below)
				//
				int lastSlash = prefix.LastIndexOf(File.separator);
				System.Diagnostics.Debug.Assert((lastSlash > 0 && lastSlash < prefix.Length - 1));
				string id = Sharpen.Runtime.Substring(prefix, lastSlash + 1);
				// log.info(id + ": " + prefix);
				doc = new Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceDocument(id);
			}
			doc.SetPrefix(prefix);
			//
			// read the raw byte stream
			//
			string trueCasedFileName = prefix + OrigExt + ".truecase";
			if ((new File(trueCasedFileName).Exists()))
			{
				mLog.Severe("Using truecased file: " + trueCasedFileName);
				doc.ReadRawBytes(trueCasedFileName);
			}
			else
			{
				doc.ReadRawBytes(prefix + OrigExt);
			}
			//
			// read the AceTokens
			//
			int offsetToSubtract = 0;
			IList<IList<AceToken>> sentences = AceSentenceSegmenter.TokenizeAndSegmentSentences(prefix);
			doc.SetSentences(sentences);
			foreach (IList<AceToken> sentence in sentences)
			{
				foreach (AceToken token in sentence)
				{
					offsetToSubtract = token.AdjustPhrasePositions(offsetToSubtract, token.GetLiteral());
					doc.AddToken(token);
				}
			}
			//
			// match char sequences to phrases
			//
			doc.MatchCharSeqs(prefix);
			//
			// construct the mEntityMentions matrix
			//
			ICollection<string> entityKeys = doc.mEntityMentions.Keys;
			int sentence_1;
			foreach (string key in entityKeys)
			{
				AceEntityMention em = doc.mEntityMentions[key];
				sentence_1 = doc.mTokens[em.GetHead().GetTokenStart()].GetSentence();
				// adjust the number of rows if necessary
				while (sentence_1 >= doc.mSentenceEntityMentions.Count)
				{
					doc.mSentenceEntityMentions.Add(new List<AceEntityMention>());
					doc.mSentenceRelationMentions.Add(new List<AceRelationMention>());
					doc.mSentenceEventMentions.Add(new List<AceEventMention>());
				}
				// store the entity mentions in increasing order:
				// (a) of the start position of their head
				// (b) if start is the same, in increasing order of the head end
				List<AceEntityMention> sentEnts = doc.mSentenceEntityMentions[sentence_1];
				bool added = false;
				for (int i = 0; i < sentEnts.Count; i++)
				{
					AceEntityMention crt = sentEnts[i];
					if ((crt.GetHead().GetTokenStart() > em.GetHead().GetTokenStart()) || (crt.GetHead().GetTokenStart() == em.GetHead().GetTokenStart() && crt.GetHead().GetTokenEnd() > em.GetHead().GetTokenEnd()))
					{
						sentEnts.Add(i, em);
						added = true;
						break;
					}
				}
				if (!added)
				{
					sentEnts.Add(em);
				}
			}
			return doc;
		}

		// TODO: never used?
		public virtual void ConstructSentenceRelationMentions()
		{
			// 
			// construct the mRelationEntityMentions matrix
			//
			ICollection<string> relKeys = mRelationMentions.Keys;
			foreach (string key in relKeys)
			{
				AceRelationMention rm = mRelationMentions[key];
				int sentence = mTokens[rm.GetArg(0).GetHead().GetTokenStart()].GetSentence();
				//
				// no need to adjust the number of rows: was done in parseDocument
				//
				// store the relation mentions in increasing order
				// (a) of the start position of their head, or
				// (b) if start is the same, in increasing order of ends
				List<AceRelationMention> sentRels = mSentenceRelationMentions[sentence];
				bool added = false;
				for (int i = 0; i < sentRels.Count; i++)
				{
					AceRelationMention crt = sentRels[i];
					if ((crt.GetMinTokenStart() > rm.GetMinTokenStart()) || (crt.GetMinTokenStart() == rm.GetMinTokenStart() && crt.GetMaxTokenEnd() > rm.GetMaxTokenEnd()))
					{
						sentRels.Add(i, rm);
						added = true;
						break;
					}
				}
				if (!added)
				{
					sentRels.Add(rm);
				}
			}
		}

		/// <summary>Verifies if the two tokens are part of the same chunk</summary>
		public virtual bool SameChunk(int left, int right)
		{
			for (int i = right; i > left; i--)
			{
				string chunk = AceToken.Others.Get(GetToken(i).GetChunk());
				if (!chunk.StartsWith("I-"))
				{
					return false;
				}
				string word = AceToken.Words.Get(GetToken(i).GetWord());
				if (word.Equals(",") || word.Equals("(") || word.Equals("-"))
				{
					return false;
				}
			}
			string leftChunk = AceToken.Others.Get(GetToken(left).GetChunk());
			if (leftChunk.Equals("O"))
			{
				return false;
			}
			return true;
		}

		public virtual bool IsChunkHead(int pos)
		{
			string next = AceToken.Others.Get(GetToken(pos + 1).GetChunk());
			if (next.StartsWith("I-"))
			{
				return false;
			}
			return true;
		}

		public virtual int FindChunkEnd(int pos)
		{
			string crt = AceToken.Others.Get(GetToken(pos).GetChunk());
			if (crt.Equals("O"))
			{
				return pos;
			}
			for (pos = pos + 1; pos < GetTokenCount(); pos++)
			{
				crt = AceToken.Others.Get(GetToken(pos).GetChunk());
				if (!crt.StartsWith("I-"))
				{
					break;
				}
			}
			return pos - 1;
		}

		public virtual int FindChunkStart(int pos)
		{
			string crt = AceToken.Others.Get(GetToken(pos).GetChunk());
			if (crt.Equals("O") || crt.StartsWith("B-"))
			{
				return pos;
			}
			for (pos = pos - 1; pos >= 0; pos--)
			{
				crt = AceToken.Others.Get(GetToken(pos).GetChunk());
				if (crt.StartsWith("B-"))
				{
					break;
				}
			}
			return pos;
		}

		public virtual bool IsApposition(int left, int right)
		{
			int leftEnd = FindChunkEnd(left);
			int rightStart = FindChunkStart(right);
			if (rightStart == leftEnd + 1)
			{
				return true;
			}
			if (rightStart == leftEnd + 2)
			{
				string comma = AceToken.Words.Get(GetToken(leftEnd + 1).GetWord());
				if (comma.Equals(",") || comma.Equals("-") || comma.Equals("_"))
				{
					return true;
				}
			}
			return false;
		}

		public virtual int CountVerbs(int start, int end)
		{
			int count = 0;
			for (int i = start; i < end; i++)
			{
				string crt = AceToken.Others.Get(GetToken(i).GetPos());
				if (crt.StartsWith("VB"))
				{
					count++;
				}
			}
			return count;
		}

		public virtual int CountCommas(int start, int end)
		{
			int count = 0;
			for (int i = start; i < end; i++)
			{
				string crt = AceToken.Words.Get(GetToken(i).GetWord());
				if (crt.Equals(","))
				{
					count++;
				}
			}
			return count;
		}

		/// <exception cref="System.IO.IOException"/>
		private void ReadRawBytes(string fileName)
		{
			BufferedReader @in = new BufferedReader(new FileReader(fileName));
			StringBuilder buf = new StringBuilder();
			int c;
			while ((c = @in.Read()) >= 0)
			{
				buf.Append((char)c);
			}
			mRawBuffer = buf.ToString();
			// System.out.println(mRawBuffer);
			@in.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		private void ReadPredictedEntityBoundaries(BufferedReader @is)
		{
			// System.out.println("Reading boundaries from file: " + mPrefix);
			//
			// read Massi's B-ENT, I-ENT, or O labels
			//
			List<string> labels = new List<string>();
			string line;
			while ((line = @is.ReadLine()) != null)
			{
				List<string> tokens = SimpleTokenize.Tokenize(line);
				if (tokens.IsEmpty() == false)
				{
					labels.Add(tokens[0]);
				}
			}
			System.Diagnostics.Debug.Assert((labels.Count == mTokens.Count));
			int entityId = 1;
			//
			// traverse the label array and create entities as needed
			//
			for (int i = 0; i < labels.Count; i++)
			{
				// System.out.println(labels.get(i));
				if (labels[i].StartsWith("B-") || labels[i].StartsWith("I-"))
				{
					// Massi's
					// ents
					// may
					// start
					// with
					// I-ENT
					int startToken = i;
					int endToken = i + 1;
					while (endToken < labels.Count && labels[endToken].StartsWith("I-"))
					{
						endToken++;
					}
					//
					// Set the type/subtype to whatever Massi predicted
					// This is not directly used in this system. It is needed only
					// to generate the APF files with Massi info, which are needed
					// by Edgar. Otherwise type/subtype could be safely set to "none".
					//
					string label = labels[startToken];
					int dash = label.IndexOf("-", 2);
					if (dash <= 2 || dash >= label.Length)
					{
						throw new Exception(label);
					}
					System.Diagnostics.Debug.Assert((dash > 2 && dash < label.Length - 1));
					string type = Sharpen.Runtime.Substring(label, 2, dash);
					string subtype = Sharpen.Runtime.Substring(label, dash + 1);
					/*
					* String type = "none"; String subtype = "none";
					*/
					// create a new entity between [startToken, endToken)
					MakeEntity(startToken, endToken, entityId, type, subtype);
					// skip over this entity
					i = endToken - 1;
					entityId++;
				}
				else
				{
					System.Diagnostics.Debug.Assert((labels[i].Equals("O")));
				}
			}
		}

		public virtual AceCharSeq MakeCharSeq(int startToken, int endToken)
		{
			/*
			* StringBuffer buf = new StringBuffer(); for(int i = startToken; i <
			* endToken; i ++){ if(i > startToken) buf.append(" ");
			* buf.append(mTokens.get(i).getLiteral()); }
			*/
			startToken = Math.Max(0, startToken);
			while (mTokens[startToken].GetByteStart() < 0)
			{
				// SGML token
				startToken++;
			}
			endToken = Math.Min(endToken, mTokens.Count);
			while (mTokens[endToken - 1].GetByteStart() < 0)
			{
				// SGML token
				endToken--;
			}
			System.Diagnostics.Debug.Assert((endToken > startToken));
			string text = Sharpen.Runtime.Substring(mRawBuffer, mTokens[startToken].GetRawByteStart(), mTokens[endToken - 1].GetRawByteEnd());
			/*
			* if(mTokens.get(startToken).getByteStart() > mTokens.get(endToken -
			* 1).getByteEnd() - 1){ for(int i = startToken; i < endToken; i ++){
			* System.out.println("Token: " + mTokens.get(i).display()); } }
			*/
			return new AceCharSeq(text, mTokens[startToken].GetByteStart(), mTokens[endToken - 1].GetByteEnd() - 1);
		}

		// buf.toString(),
		/// <summary>Makes an ACE entity from the span [startToken, endToken)</summary>
		private void MakeEntity(int startToken, int endToken, int id, string type, string subtype)
		{
			string eid = mId + "-E" + id;
			AceEntity ent = new AceEntity(eid, type, subtype, "SPC");
			AddEntity(ent);
			AceCharSeq cseq = MakeCharSeq(startToken, endToken);
			string emid = mId + "-E" + id + "-1";
			AceEntityMention entm = new AceEntityMention(emid, "NOM", "NOM", cseq, cseq);
			AddEntityMention(entm);
			ent.AddMention(entm);
		}
	}
}
