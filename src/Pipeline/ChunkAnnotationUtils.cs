using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Utility functions for annotating chunks</summary>
	/// <author>Angel Chang</author>
	public class ChunkAnnotationUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.ChunkAnnotationUtils));

		private static readonly CoreLabelTokenFactory tokenFactory = new CoreLabelTokenFactory(true);

		private ChunkAnnotationUtils()
		{
		}

		// static methods
		/// <summary>Checks if offsets of doc and sentence matches.</summary>
		/// <param name="docAnnotation">The document Annotation to analyze</param>
		/// <returns>true if the offsets match, false otherwise</returns>
		public static bool CheckOffsets(ICoreMap docAnnotation)
		{
			bool okay = true;
			string docText = docAnnotation.Get(typeof(CoreAnnotations.TextAnnotation));
			string docId = docAnnotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			IList<CoreLabel> docTokens = docAnnotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> sentences = docAnnotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				string sentText = sentence.Get(typeof(CoreAnnotations.TextAnnotation));
				IList<CoreLabel> sentTokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				int sentBeginChar = sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int sentEndChar = sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				int sentBeginToken = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				int sentEndToken = sentence.Get(typeof(CoreAnnotations.TokenEndAnnotation));
				string docTextSpan = Sharpen.Runtime.Substring(docText, sentBeginChar, sentEndChar);
				IList<CoreLabel> docTokenSpan = new List<CoreLabel>(docTokens.SubList(sentBeginToken, sentEndToken));
				logger.Debug("Checking Document " + docId + " span (" + sentBeginChar + "," + sentEndChar + ") ");
				if (!docTextSpan.Equals(sentText))
				{
					okay = false;
					logger.Debug("WARNING: Document " + docId + " span does not match sentence");
					logger.Debug("DocSpanText: " + docTextSpan);
					logger.Debug("SentenceText: " + sentText);
				}
				string sentTokenStr = GetTokenText(sentTokens, typeof(CoreAnnotations.TextAnnotation));
				string docTokenStr = GetTokenText(docTokenSpan, typeof(CoreAnnotations.TextAnnotation));
				if (!docTokenStr.Equals(sentTokenStr))
				{
					okay = false;
					logger.Debug("WARNING: Document " + docId + " tokens does not match sentence");
					logger.Debug("DocSpanTokens: " + docTokenStr);
					logger.Debug("SentenceTokens: " + sentTokenStr);
				}
			}
			return okay;
		}

		/// <summary>
		/// Fix token offsets of sentences to match those in the document (assumes tokens are shared)
		/// sentence token indices may not match document token list if certain html elements are ignored.
		/// </summary>
		/// <param name="docAnnotation">The document Annotation to analyze</param>
		/// <returns>true if fix was okay, false otherwise</returns>
		public static bool FixTokenOffsets(ICoreMap docAnnotation)
		{
			IList<CoreLabel> docTokens = docAnnotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> sentences = docAnnotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int i = 0;
			CoreLabel curDocToken = docTokens[0];
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> sentTokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				CoreLabel sentTokenFirst = sentTokens[0];
				while (curDocToken != sentTokenFirst)
				{
					i++;
					if (i >= docTokens.Count)
					{
						return false;
					}
					curDocToken = docTokens[i];
				}
				int sentTokenBegin = i;
				CoreLabel sentTokenLast = sentTokens[sentTokens.Count - 1];
				while (curDocToken != sentTokenLast)
				{
					i++;
					if (i >= docTokens.Count)
					{
						return false;
					}
					curDocToken = docTokens[i];
				}
				int sentTokenEnd = i + 1;
				sentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), sentTokenBegin);
				sentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), sentTokenEnd);
			}
			return true;
		}

		/// <summary>Copies annotation over to this CoreMap if not already set.</summary>
		public static void CopyUnsetAnnotations(ICoreMap src, ICoreMap dest)
		{
			foreach (Type key in src.KeySet())
			{
				if (!dest.ContainsKey(key))
				{
					dest.Set(key, src.Get(key));
				}
			}
		}

		/// <summary>
		/// Give an list of character offsets for chunk, fix tokenization so tokenization occurs at
		/// boundary of chunks.
		/// </summary>
		/// <param name="docAnnotation"/>
		/// <param name="chunkCharOffsets"/>
		public static bool FixChunkTokenBoundaries(ICoreMap docAnnotation, IList<IntPair> chunkCharOffsets)
		{
			// First identify any tokens that need to be fixed
			string text = docAnnotation.Get(typeof(CoreAnnotations.TextAnnotation));
			IList<CoreLabel> tokens = docAnnotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<CoreLabel> output = new List<CoreLabel>(tokens.Count);
			int i = 0;
			CoreLabel token = tokens[i];
			foreach (IntPair offsets in chunkCharOffsets)
			{
				System.Diagnostics.Debug.Assert((token.BeginPosition() >= 0));
				System.Diagnostics.Debug.Assert((token.EndPosition() >= 0));
				int offsetBegin = offsets.GetSource();
				int offsetEnd = offsets.GetTarget();
				// Find tokens where token begins after chunk starts
				// and token ends after chunk starts
				while (offsetBegin < token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) || offsetBegin >= token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
				{
					output.Add(token);
					i++;
					if (i >= tokens.Count)
					{
						return false;
					}
					token = tokens[i];
				}
				// offsetBegin is now >= token begin and < token end
				// go until we find a token that starts after our chunk has ended
				while (offsetEnd > token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
				{
					// Check if chunk includes token
					if (offsetBegin > token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
					{
						// Chunk starts in the middle of the token
						if (offsetEnd < token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
						{
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, token.BeginPosition(), offsetBegin), token.BeginPosition(), offsetBegin - token.BeginPosition()));
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, offsetBegin, offsetEnd), offsetBegin, offsetEnd - offsetBegin));
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, offsetEnd, token.EndPosition()), offsetEnd, token.EndPosition() - offsetEnd));
						}
						else
						{
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, token.BeginPosition(), offsetBegin), token.BeginPosition(), offsetBegin - token.BeginPosition()));
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, offsetBegin, token.EndPosition()), offsetBegin, token.EndPosition() - offsetBegin));
						}
					}
					else
					{
						if (offsetEnd < token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
						{
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, token.BeginPosition(), offsetEnd), token.BeginPosition(), offsetEnd - token.BeginPosition()));
							output.Add(tokenFactory.MakeToken(Sharpen.Runtime.Substring(text, offsetEnd, token.EndPosition()), offsetEnd, token.EndPosition() - offsetEnd));
						}
						else
						{
							// success!  chunk contains token
							output.Add(token);
						}
					}
					i++;
					if (i >= tokens.Count)
					{
						return false;
					}
					token = tokens[i];
				}
			}
			// Add rest of the tokens
			for (; i < tokens.Count; i++)
			{
				token = tokens[i];
				output.Add(token);
			}
			docAnnotation.Set(typeof(CoreAnnotations.TokensAnnotation), output);
			return true;
		}

		/// <summary>Create chunk that is merged from chunkIndexStart to chunkIndexEnd (exclusive).</summary>
		/// <param name="chunkList">- List of chunks</param>
		/// <param name="origText">- Text from which to extract chunk text</param>
		/// <param name="chunkIndexStart">- Index of first chunk to merge</param>
		/// <param name="chunkIndexEnd">- Index of last chunk to merge (exclusive)</param>
		/// <param name="tokenFactory">- factory for creating tokens (if we want to get a merged corelabel instead of something random)</param>
		/// <returns>new merged chunk</returns>
		public static ICoreMap GetMergedChunk<_T0>(IList<_T0> chunkList, string origText, int chunkIndexStart, int chunkIndexEnd, CoreLabelTokenFactory tokenFactory)
			where _T0 : ICoreMap
		{
			ICoreMap firstChunk = chunkList[chunkIndexStart];
			ICoreMap lastChunk = chunkList[chunkIndexEnd - 1];
			int firstCharOffset = firstChunk.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int lastCharOffset = lastChunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			int firstTokenIndex = firstChunk.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			int lastTokenIndex = lastChunk.Get(typeof(CoreAnnotations.TokenEndAnnotation));
			string chunkText = Sharpen.Runtime.Substring(origText, firstCharOffset, lastCharOffset);
			ICoreMap newChunk;
			if (tokenFactory != null)
			{
				newChunk = tokenFactory.MakeToken(chunkText, firstCharOffset, lastCharOffset);
			}
			else
			{
				newChunk = new Annotation(chunkText);
			}
			newChunk.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), firstCharOffset);
			newChunk.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), lastCharOffset);
			newChunk.Set(typeof(CoreAnnotations.TokenBeginAnnotation), firstTokenIndex);
			newChunk.Set(typeof(CoreAnnotations.TokenEndAnnotation), lastTokenIndex);
			IList<CoreLabel> tokens = new List<CoreLabel>(lastTokenIndex - firstTokenIndex);
			for (int i = chunkIndexStart; i < chunkIndexEnd; i++)
			{
				ICoreMap chunk = chunkList[i];
				Sharpen.Collections.AddAll(tokens, chunk.Get(typeof(CoreAnnotations.TokensAnnotation)));
			}
			newChunk.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			// TODO: merge other keys into this new chunk ??
			return newChunk;
		}

		/// <summary>Create chunk that is merged from chunkIndexStart to chunkIndexEnd (exclusive)</summary>
		/// <param name="chunkList">- List of chunks</param>
		/// <param name="chunkIndexStart">- Index of first chunk to merge</param>
		/// <param name="chunkIndexEnd">- Index of last chunk to merge (exclusive)</param>
		/// <param name="aggregators">- Aggregators</param>
		/// <param name="tokenFactory">- factory for creating tokens (if we want to get a merged corelabel instead of something random)</param>
		/// <returns>new merged chunk</returns>
		public static ICoreMap GetMergedChunk<_T0>(IList<_T0> chunkList, int chunkIndexStart, int chunkIndexEnd, IDictionary<Type, CoreMapAttributeAggregator> aggregators, CoreLabelTokenFactory tokenFactory)
			where _T0 : ICoreMap
		{
			ICoreMap newChunk;
			if (tokenFactory != null)
			{
				newChunk = tokenFactory.MakeToken();
			}
			else
			{
				newChunk = new Annotation(string.Empty);
			}
			foreach (KeyValuePair<Type, CoreMapAttributeAggregator> entry in aggregators)
			{
				if (chunkIndexEnd > chunkList.Count)
				{
					System.Diagnostics.Debug.Assert((false));
				}
				object value = entry.Value.Aggregate(entry.Key, chunkList.SubList(chunkIndexStart, chunkIndexEnd));
				newChunk.Set(entry.Key, value);
			}
			if (newChunk is CoreLabel)
			{
				CoreLabel cl = (CoreLabel)newChunk;
				cl.SetValue(cl.Word());
				cl.SetOriginalText(cl.Word());
			}
			return newChunk;
		}

		/// <summary>Return chunk offsets</summary>
		/// <param name="chunkList">- List of chunks</param>
		/// <param name="charStart">- character begin offset</param>
		/// <param name="charEnd">- character end offset</param>
		/// <returns>chunk offsets</returns>
		public static Interval<int> GetChunkOffsetsUsingCharOffsets<_T0>(IList<_T0> chunkList, int charStart, int charEnd)
			where _T0 : ICoreMap
		{
			int chunkStart = 0;
			int chunkEnd = chunkList.Count;
			// Find first chunk with start > charStart
			for (int i = 0; i < chunkList.Count; i++)
			{
				int start = chunkList[i].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				if (start > charStart)
				{
					break;
				}
				chunkStart = i;
			}
			// Find first chunk with start >= charEnd
			for (int i_1 = chunkStart; i_1 < chunkList.Count; i_1++)
			{
				int start = chunkList[i_1].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				if (start >= charEnd)
				{
					chunkEnd = i_1;
					break;
				}
			}
			return Interval.ToInterval(chunkStart, chunkEnd, Interval.IntervalOpenEnd);
		}

		/// <summary>Merge chunks from chunkIndexStart to chunkIndexEnd (exclusive) and replace them in the list.</summary>
		/// <param name="chunkList">- List of chunks</param>
		/// <param name="origText">- Text from which to extract chunk text</param>
		/// <param name="chunkIndexStart">- Index of first chunk to merge</param>
		/// <param name="chunkIndexEnd">- Index of last chunk to merge (exclusive)</param>
		public static void MergeChunks(IList<ICoreMap> chunkList, string origText, int chunkIndexStart, int chunkIndexEnd)
		{
			ICoreMap newChunk = GetMergedChunk(chunkList, origText, chunkIndexStart, chunkIndexEnd, null);
			int nChunksToRemove = chunkIndexEnd - chunkIndexStart - 1;
			for (int i = 0; i < nChunksToRemove; i++)
			{
				chunkList.Remove(chunkIndexStart);
			}
			chunkList.Set(chunkIndexStart, newChunk);
		}

		private static char GetFirstNonWsChar(ICoreMap sent)
		{
			string sentText = sent.Get(typeof(CoreAnnotations.TextAnnotation));
			for (int j = 0; j < sentText.Length; j++)
			{
				char c = sentText[j];
				if (!char.IsWhiteSpace(c))
				{
					return c;
				}
			}
			return null;
		}

		private static int GetFirstNonWsCharOffset(ICoreMap sent, bool relative)
		{
			string sentText = sent.Get(typeof(CoreAnnotations.TextAnnotation));
			for (int j = 0; j < sentText.Length; j++)
			{
				char c = sentText[j];
				if (!char.IsWhiteSpace(c))
				{
					if (relative)
					{
						return j;
					}
					else
					{
						return j + sent.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					}
				}
			}
			return null;
		}

		private static string GetTrimmedText(ICoreMap sent)
		{
			string sentText = sent.Get(typeof(CoreAnnotations.TextAnnotation));
			return sentText.Trim();
		}

		/// <summary>
		/// Give an list of character offsets for chunk, fix sentence splitting
		/// so sentences doesn't break the chunks.
		/// </summary>
		/// <param name="docAnnotation">Document with sentences</param>
		/// <param name="chunkCharOffsets">ordered pairs of different chunks that should appear in sentences</param>
		/// <returns>true if fix was okay (chunks are in all sentences), false otherwise</returns>
		public static bool FixChunkSentenceBoundaries(ICoreMap docAnnotation, IList<IntPair> chunkCharOffsets)
		{
			return FixChunkSentenceBoundaries(docAnnotation, chunkCharOffsets, false, false, false);
		}

		/// <summary>
		/// Give an list of character offsets for chunk, fix sentence splitting
		/// so sentences doesn't break the chunks.
		/// </summary>
		/// <param name="docAnnotation">Document with sentences</param>
		/// <param name="chunkCharOffsets">ordered pairs of different chunks that should appear in sentences</param>
		/// <param name="offsetsAreNotSorted">Treat each pair of offsets as independent (look through all sentences again)</param>
		/// <param name="extendedFixSentence">Do extended sentence fixing based on some heuristics</param>
		/// <param name="moreExtendedFixSentence">Do even more extended sentence fixing based on some heuristics</param>
		/// <returns>true if fix was okay (chunks are in all sentences), false otherwise</returns>
		public static bool FixChunkSentenceBoundaries(ICoreMap docAnnotation, IList<IntPair> chunkCharOffsets, bool offsetsAreNotSorted, bool extendedFixSentence, bool moreExtendedFixSentence)
		{
			string text = docAnnotation.Get(typeof(CoreAnnotations.TextAnnotation));
			IList<ICoreMap> sentences = docAnnotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences == null || sentences.Count == 0)
			{
				return true;
			}
			if (chunkCharOffsets != null)
			{
				int i = 0;
				ICoreMap sentence = sentences[i];
				foreach (IntPair offsets in chunkCharOffsets)
				{
					int offsetBegin = offsets.GetSource();
					int offsetEnd = offsets.GetTarget();
					// Find sentence where sentence begins after chunk starts
					// and sentence ends after chunk starts
					while (offsetBegin < sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) || offsetBegin >= sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
					{
						i++;
						if (i >= sentences.Count)
						{
							return false;
						}
						sentence = sentences[i];
					}
					// offsetBegin is now >= sentence begin and < sentence end
					// Check if sentence end includes chunk
					if (sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) >= offsetEnd)
					{
					}
					else
					{
						// success!  sentence contains chunk
						// hmm, sentence contains beginning of chunk, but not end
						// Lets find sentence that contains end of chunk and merge sentences
						int startSentIndex = i;
						while (offsetEnd > sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
						{
							i++;
							if (i >= sentences.Count)
							{
								return false;
							}
							sentence = sentences[i];
						}
						int firstNonWsCharOffset = GetFirstNonWsCharOffset(sentence, false);
						if (firstNonWsCharOffset != null && firstNonWsCharOffset >= offsetEnd)
						{
							// Ends before first real character of this sentence, don't include this sentence
							i--;
							sentence = sentences[i];
						}
						// Okay, now let's merge sentences from startSendIndex to i (includes i)
						MergeChunks(sentences, text, startSentIndex, i + 1);
						// Reset our iterating index i to startSentIndex
						i = startSentIndex;
						sentence = sentences[i];
					}
					if (extendedFixSentence)
					{
						//log.info("Doing extended fixing of sentence:" + text.substring(offsetBegin,offsetEnd));
						if (i + 1 < sentences.Count)
						{
							// Extended sentence fixing:
							// Check if entity is at the end of this sentence and if next sentence starts with uppercase
							// If not uppercase, merge with next sentence
							bool entityAtSentEnd = true;
							int sentCharBegin = sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
							string sentText = sentence.Get(typeof(CoreAnnotations.TextAnnotation));
							int offsetEndInSentText = offsetEnd - sentCharBegin;
							for (int j = offsetEndInSentText; j < sentText.Length; j++)
							{
								char c = sentText[j];
								if (!char.IsWhiteSpace(c))
								{
									entityAtSentEnd = false;
									break;
								}
							}
							bool doMerge = false;
							if (entityAtSentEnd)
							{
								ICoreMap nextSentence = sentences[i + 1];
								char c = GetFirstNonWsChar(nextSentence);
								if (c != null)
								{
									doMerge = !char.IsUpperCase((char)c);
									if (!doMerge)
									{
										logger.Debug("No merge: c is '" + c + "'");
									}
								}
								else
								{
									logger.Debug("No merge: no char");
								}
							}
							else
							{
								logger.Debug("No merge: entity not at end");
							}
							if (doMerge)
							{
								logger.Debug("Merge chunks");
								MergeChunks(sentences, text, i, i + 2);
							}
						}
					}
					if (offsetsAreNotSorted)
					{
						i = 0;
					}
					sentence = sentences[i];
				}
			}
			// Do a bit more sentence fixing
			if (moreExtendedFixSentence)
			{
				int i = 0;
				while (i + 1 < sentences.Count)
				{
					bool doMerge = false;
					ICoreMap sentence = sentences[i];
					ICoreMap nextSentence = sentences[i + 1];
					string sentTrimmedText = GetTrimmedText(sentence);
					string nextSentTrimmedText = GetTrimmedText(nextSentence);
					if (sentTrimmedText.Length <= 1 || nextSentTrimmedText.Length <= 1)
					{
						// Merge
						doMerge = true;
					}
					else
					{
						//         List<CoreLabel> sentTokens = sentence.get(CoreAnnotations.TokensAnnotation.class);
						//         CoreLabel lastSentToken = sentTokens.get(sentTokens.size()-1);
						char c = GetFirstNonWsChar(nextSentence);
						//         List<CoreLabel> nextSentTokens = nextSentence.get(CoreAnnotations.TokensAnnotation.class);
						if (c != null && !char.IsUpperCase((char)c))
						{
							if (c == ',' || (char.IsLowerCase((char)c)))
							{
								doMerge = true;
							}
						}
					}
					if (doMerge)
					{
						MergeChunks(sentences, text, i, i + 2);
					}
					else
					{
						i++;
					}
				}
			}
			// Set sentence indices
			for (int i_1 = 0; i_1 < sentences.Count; i_1++)
			{
				ICoreMap sentence = sentences[i_1];
				sentence.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), i_1);
			}
			return true;
		}

		/// <summary>Annotates a CoreMap representing a chunk with basic chunk information.</summary>
		/// <remarks>
		/// Annotates a CoreMap representing a chunk with basic chunk information.
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + totalTokenOffset
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + totalTokenOffset
		/// </remarks>
		/// <param name="chunk">- CoreMap to be annotated</param>
		/// <param name="tokens">- List of tokens to look for chunks</param>
		/// <param name="tokenStartIndex">- Index (relative to current list of tokens) at which this chunk starts</param>
		/// <param name="tokenEndIndex">- Index (relative to current list of tokens) at which this chunk ends (not inclusive)</param>
		/// <param name="totalTokenOffset">- Index of tokens to offset by</param>
		public static void AnnotateChunk(ICoreMap chunk, IList<CoreLabel> tokens, int tokenStartIndex, int tokenEndIndex, int totalTokenOffset)
		{
			IList<CoreLabel> chunkTokens = new List<CoreLabel>(tokens.SubList(tokenStartIndex, tokenEndIndex));
			chunk.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), chunkTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			chunk.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), chunkTokens[chunkTokens.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			chunk.Set(typeof(CoreAnnotations.TokensAnnotation), chunkTokens);
			chunk.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenStartIndex + totalTokenOffset);
			chunk.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenEndIndex + totalTokenOffset);
		}

		public static string GetTokenText<_T0>(IList<_T0> tokens, Type tokenTextKey)
			where _T0 : ICoreMap
		{
			return GetTokenText(tokens, tokenTextKey, " ");
		}

		public static string GetTokenText<_T0>(IList<_T0> tokens, Type tokenTextKey, string delimiter)
			where _T0 : ICoreMap
		{
			StringBuilder sb = new StringBuilder();
			int prevEndIndex = -1;
			foreach (ICoreMap cm in tokens)
			{
				object obj = cm.Get(tokenTextKey);
				bool includeDelimiter = sb.Length > 0;
				if (cm.ContainsKey(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) && cm.ContainsKey(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
				{
					int beginIndex = cm.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int endIndex = cm.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					if (prevEndIndex == beginIndex)
					{
						// No spaces
						includeDelimiter = false;
					}
					prevEndIndex = endIndex;
				}
				if (obj != null)
				{
					if (includeDelimiter)
					{
						sb.Append(delimiter);
					}
					sb.Append(obj);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Annotates a CoreMap representing a chunk with text information
		/// TextAnnotation - String representing tokens in this chunks (token text separated by space)
		/// </summary>
		/// <param name="chunk">- CoreMap to be annotated</param>
		/// <param name="tokenTextKey">- Key to use to find the token text</param>
		public static void AnnotateChunkText(ICoreMap chunk, Type tokenTextKey)
		{
			IList<CoreLabel> chunkTokens = chunk.Get(typeof(CoreAnnotations.TokensAnnotation));
			string text = GetTokenText(chunkTokens, tokenTextKey);
			chunk.Set(typeof(CoreAnnotations.TextAnnotation), text);
		}

		public static bool HasCharacterOffsets(ICoreMap chunk)
		{
			return chunk.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) != null && chunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) != null;
		}

		/// <summary>
		/// Annotates a CoreMap representing a chunk with text information
		/// TextAnnotation - String extracted from the origAnnotation using character offset information for this chunk
		/// </summary>
		/// <param name="chunk">- CoreMap to be annotated</param>
		/// <param name="origAnnotation">- Annotation from which to extract the text for this chunk</param>
		public static bool AnnotateChunkText(ICoreMap chunk, ICoreMap origAnnotation)
		{
			string annoText = origAnnotation.Get(typeof(CoreAnnotations.TextAnnotation));
			if (annoText == null)
			{
				return false;
			}
			if (!HasCharacterOffsets(chunk))
			{
				return false;
			}
			int annoBeginCharOffset = origAnnotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			if (annoBeginCharOffset == null)
			{
				annoBeginCharOffset = 0;
			}
			int chunkBeginCharOffset = chunk.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) - annoBeginCharOffset;
			int chunkEndCharOffset = chunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) - annoBeginCharOffset;
			if (chunkBeginCharOffset < 0)
			{
				logger.Debug("Adjusting begin char offset from " + chunkBeginCharOffset + " to 0");
				logger.Debug("Chunk begin offset: " + chunk.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) + ", Source text begin offset " + annoBeginCharOffset);
				chunkBeginCharOffset = 0;
			}
			if (chunkBeginCharOffset > annoText.Length)
			{
				logger.Debug("Adjusting begin char offset from " + chunkBeginCharOffset + " to " + annoText.Length);
				logger.Debug("Chunk begin offset: " + chunk.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) + ", Source text begin offset " + annoBeginCharOffset);
				chunkBeginCharOffset = annoText.Length;
			}
			if (chunkEndCharOffset < 0)
			{
				logger.Debug("Adjusting end char offset from " + chunkEndCharOffset + " to 0");
				logger.Debug("Chunk end offset: " + chunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) + ", Source text begin offset " + annoBeginCharOffset);
				chunkEndCharOffset = 0;
			}
			if (chunkEndCharOffset > annoText.Length)
			{
				logger.Debug("Adjusting end char offset from " + chunkEndCharOffset + " to " + annoText.Length);
				logger.Debug("Chunk end offset: " + chunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) + ", Source text begin offset " + annoBeginCharOffset);
				chunkEndCharOffset = annoText.Length;
			}
			if (chunkEndCharOffset < chunkBeginCharOffset)
			{
				logger.Debug("Adjusting end char offset from " + chunkEndCharOffset + " to " + chunkBeginCharOffset);
				logger.Debug("Chunk end offset: " + chunk.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) + ", Source text begin offset " + annoBeginCharOffset);
				chunkEndCharOffset = chunkBeginCharOffset;
			}
			string chunkText = Sharpen.Runtime.Substring(annoText, chunkBeginCharOffset, chunkEndCharOffset);
			chunk.Set(typeof(CoreAnnotations.TextAnnotation), chunkText);
			return true;
		}

		/// <summary>Annotates tokens in chunk.</summary>
		/// <param name="chunk">- CoreMap representing chunk (should have TextAnnotation and TokensAnnotation)</param>
		/// <param name="tokenChunkKey">- If not null, each token is annotated with the chunk using this key</param>
		/// <param name="tokenLabelKey">- If not null, each token is annotated with the text associated with the chunk using this key</param>
		public static void AnnotateChunkTokens(ICoreMap chunk, Type tokenChunkKey, Type tokenLabelKey)
		{
			IList<CoreLabel> chunkTokens = chunk.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tokenLabelKey != null)
			{
				string text = chunk.Get(typeof(CoreAnnotations.TextAnnotation));
				foreach (CoreLabel t in chunkTokens)
				{
					t.Set(tokenLabelKey, text);
				}
			}
			if (tokenChunkKey != null)
			{
				foreach (CoreLabel t in chunkTokens)
				{
					t.Set(tokenChunkKey, chunk);
				}
			}
		}

		/// <summary>Create a new chunk Annotation with basic chunk information.</summary>
		/// <remarks>
		/// Create a new chunk Annotation with basic chunk information.
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + totalTokenOffset
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + totalTokenOffset
		/// </remarks>
		/// <param name="tokens">- List of tokens to look for chunks</param>
		/// <param name="tokenStartIndex">- Index (relative to current list of tokens) at which this chunk starts</param>
		/// <param name="tokenEndIndex">- Index (relative to current list of tokens) at which this chunk ends (not inclusive)</param>
		/// <param name="totalTokenOffset">- Index of tokens to offset by</param>
		/// <returns>Annotation representing new chunk</returns>
		public static Annotation GetAnnotatedChunk(IList<CoreLabel> tokens, int tokenStartIndex, int tokenEndIndex, int totalTokenOffset)
		{
			Annotation chunk = new Annotation(string.Empty);
			AnnotateChunk(chunk, tokens, tokenStartIndex, tokenEndIndex, totalTokenOffset);
			return chunk;
		}

		/// <summary>Create a new chunk Annotation with basic chunk information.</summary>
		/// <remarks>
		/// Create a new chunk Annotation with basic chunk information.
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + totalTokenOffset
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + totalTokenOffset
		/// TextAnnotation - String extracted from the origAnnotation using character offset information for this chunk
		/// </remarks>
		/// <param name="tokens">- List of tokens to look for chunks</param>
		/// <param name="tokenStartIndex">- Index (relative to current list of tokens) at which this chunk starts</param>
		/// <param name="tokenEndIndex">- Index (relative to current list of tokens) at which this chunk ends (not inclusive)</param>
		/// <param name="totalTokenOffset">- Index of tokens to offset by</param>
		/// <param name="tokenChunkKey">- If not null, each token is annotated with the chunk using this key</param>
		/// <param name="tokenTextKey">- Key to use to find the token text</param>
		/// <param name="tokenLabelKey">- If not null, each token is annotated with the text associated with the chunk using this key</param>
		/// <returns>Annotation representing new chunk</returns>
		public static Annotation GetAnnotatedChunk(IList<CoreLabel> tokens, int tokenStartIndex, int tokenEndIndex, int totalTokenOffset, Type tokenChunkKey, Type tokenTextKey, Type tokenLabelKey)
		{
			Annotation chunk = GetAnnotatedChunk(tokens, tokenStartIndex, tokenEndIndex, totalTokenOffset);
			AnnotateChunkText(chunk, tokenTextKey);
			AnnotateChunkTokens(chunk, tokenChunkKey, tokenLabelKey);
			return chunk;
		}

		/// <summary>
		/// Create a new chunk Annotation with basic chunk information
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + annotation's TokenBeginAnnotation
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + annotation's TokenBeginAnnotation
		/// TextAnnotation - String extracted from the origAnnotation using character offset information for this chunk
		/// </summary>
		/// <param name="annotation">- Annotation from which to extract the text for this chunk</param>
		/// <param name="tokenStartIndex">- Index (relative to current list of tokens) at which this chunk starts</param>
		/// <param name="tokenEndIndex">- Index (relative to current list of tokens) at which this chunk ends (not inclusive)</param>
		/// <returns>Annotation representing new chunk</returns>
		public static Annotation GetAnnotatedChunk(ICoreMap annotation, int tokenStartIndex, int tokenEndIndex)
		{
			int annoTokenBegin = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (annoTokenBegin == null)
			{
				annoTokenBegin = 0;
			}
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			Annotation chunk = GetAnnotatedChunk(tokens, tokenStartIndex, tokenEndIndex, annoTokenBegin);
			bool annotatedTextFromCharOffsets = AnnotateChunkText(chunk, annotation);
			if (!annotatedTextFromCharOffsets)
			{
				// Use tokens to get text annotation
				AnnotateChunkText(chunk, typeof(CoreAnnotations.TextAnnotation));
			}
			return chunk;
		}

		/// <summary>
		/// Create a new chunk Annotation with basic chunk information
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + annotation's TokenBeginAnnotation
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + annotation's TokenBeginAnnotation
		/// TextAnnotation - String extracted from the origAnnotation using character offset information for this chunk
		/// </summary>
		/// <param name="annotation">- Annotation from which to extract the text for this chunk</param>
		/// <param name="tokenStartIndex">- Index (relative to current list of tokens) at which this chunk starts</param>
		/// <param name="tokenEndIndex">- Index (relative to current list of tokens) at which this chunk ends (not inclusive)</param>
		/// <param name="tokenChunkKey">- If not null, each token is annotated with the chunk using this key</param>
		/// <param name="tokenLabelKey">- If not null, each token is annotated with the text associated with the chunk using this key</param>
		/// <returns>Annotation representing new chunk</returns>
		public static Annotation GetAnnotatedChunk(ICoreMap annotation, int tokenStartIndex, int tokenEndIndex, Type tokenChunkKey, Type tokenLabelKey)
		{
			Annotation chunk = GetAnnotatedChunk(annotation, tokenStartIndex, tokenEndIndex);
			AnnotateChunkTokens(chunk, tokenChunkKey, tokenLabelKey);
			return chunk;
		}

		/// <summary>Returns a chunk annotation based on char offsets.</summary>
		/// <param name="annotation">Annotation from which to extract the text for this chunk</param>
		/// <param name="charOffsetStart">Start character offset</param>
		/// <param name="charOffsetEnd">End (not inclusive) character offset</param>
		/// <returns>
		/// An Annotation representing the new chunk. Or
		/// <see langword="null"/>
		/// if no chunk matches offsets.
		/// </returns>
		public static ICoreMap GetAnnotatedChunkUsingCharOffsets(ICoreMap annotation, int charOffsetStart, int charOffsetEnd)
		{
			// TODO: make more efficient search
			IList<ICoreMap> cm = GetAnnotatedChunksUsingSortedCharOffsets(annotation, CollectionUtils.MakeList(new IntPair(charOffsetStart, charOffsetEnd)));
			if (!cm.IsEmpty())
			{
				return cm[0];
			}
			else
			{
				return null;
			}
		}

		public static IList<ICoreMap> GetAnnotatedChunksUsingSortedCharOffsets(ICoreMap annotation, IList<IntPair> charOffsets)
		{
			return GetAnnotatedChunksUsingSortedCharOffsets(annotation, charOffsets, true, null, null, true);
		}

		/// <summary>Create a list of new chunk Annotation with basic chunk information.</summary>
		/// <remarks>
		/// Create a list of new chunk Annotation with basic chunk information.
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// tokenStartIndex + annotation's TokenBeginAnnotation
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// tokenEndIndex + annotation's TokenBeginAnnotation
		/// TextAnnotation - String extracted from the origAnnotation using character offset information for this chunk
		/// </remarks>
		/// <param name="annotation">Annotation from which to extract the text for this chunk</param>
		/// <param name="charOffsets">
		/// - List of start and end (not inclusive) character offsets
		/// Note: assume char offsets are sorted and non-overlapping!!!
		/// </param>
		/// <param name="charOffsetIsRelative">- Whether the character offsets are relative to the current annotation or absolute offsets</param>
		/// <param name="tokenChunkKey">- If not null, each token is annotated with the chunk using this key</param>
		/// <param name="tokenLabelKey">- If not null, each token is annotated with the text associated with the chunk using this key</param>
		/// <param name="allowPartialTokens">- Whether to allow partial tokens or not</param>
		/// <returns>List of Annotation representing new chunks; may be empty never null</returns>
		public static IList<ICoreMap> GetAnnotatedChunksUsingSortedCharOffsets(ICoreMap annotation, IList<IntPair> charOffsets, bool charOffsetIsRelative, Type tokenChunkKey, Type tokenLabelKey, bool allowPartialTokens)
		{
			string annoText = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			IList<ICoreMap> chunks = new List<ICoreMap>(charOffsets.Count);
			IList<CoreLabel> annoTokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			int annoCharBegin = annotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			if (annoCharBegin == null)
			{
				annoCharBegin = 0;
			}
			int annoTokenBegin = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (annoTokenBegin == null)
			{
				annoTokenBegin = 0;
			}
			int i = 0;
			foreach (IntPair p in charOffsets)
			{
				int beginRelCharOffset = charOffsetIsRelative ? p.GetSource() : p.GetSource() - annoCharBegin;
				int endRelCharOffset = charOffsetIsRelative ? p.GetTarget() : p.GetTarget() - annoCharBegin;
				int beginCharOffset = beginRelCharOffset + annoCharBegin;
				int endCharOffset = endRelCharOffset + annoCharBegin;
				if (beginRelCharOffset >= annoText.Length)
				{
					break;
				}
				if (endRelCharOffset > annoText.Length)
				{
					endRelCharOffset = annoText.Length;
				}
				if (allowPartialTokens)
				{
					while (i < annoTokens.Count && annoTokens[i].EndPosition() <= beginCharOffset)
					{
						i++;
					}
				}
				else
				{
					while (i < annoTokens.Count && annoTokens[i].BeginPosition() < beginCharOffset)
					{
						i++;
					}
				}
				if (i >= annoTokens.Count)
				{
					break;
				}
				int tokenBegin = i;
				int j = i;
				if (allowPartialTokens)
				{
					while (j < annoTokens.Count && annoTokens[j].BeginPosition() < endCharOffset)
					{
						j++;
					}
				}
				else
				{
					while (j < annoTokens.Count && annoTokens[j].EndPosition() <= endCharOffset)
					{
						System.Diagnostics.Debug.Assert((annoTokens[j].BeginPosition() >= beginCharOffset));
						j++;
					}
				}
				int tokenEnd = j;
				IList<CoreLabel> chunkTokens = new List<CoreLabel>(annoTokens.SubList(tokenBegin, tokenEnd));
				string chunkText = Sharpen.Runtime.Substring(annoText, beginRelCharOffset, endRelCharOffset);
				Annotation chunk = new Annotation(chunkText);
				chunk.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), beginCharOffset);
				chunk.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), endCharOffset);
				chunk.Set(typeof(CoreAnnotations.TokensAnnotation), chunkTokens);
				chunk.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenBegin + annoTokenBegin);
				chunk.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenEnd + annoTokenBegin);
				AnnotateChunkTokens(chunk, tokenChunkKey, tokenLabelKey);
				chunks.Add(chunk);
				if (j >= annoTokens.Count)
				{
					break;
				}
			}
			if (chunks.Count != charOffsets.Count)
			{
				logger.Warning("WARNING: Only " + chunks.Count + "/" + charOffsets.Count + " chunks found.  Check if offsets are sorted/nonoverlapping");
			}
			return chunks;
		}

		public static void AnnotateChunk(ICoreMap annotation, Type newAnnotationKey, Type aggrKey, CoreMapAttributeAggregator aggregator)
		{
			object v = aggregator.Aggregate(aggrKey, annotation.Get(typeof(CoreAnnotations.TokensAnnotation)));
			annotation.Set(newAnnotationKey, v);
		}

		public static void AnnotateChunk(ICoreMap chunk, IDictionary<string, string> attributes)
		{
			foreach (KeyValuePair<string, string> entry in attributes)
			{
				string key = entry.Key;
				string value = entry.Value;
				Type coreKeyClass = AnnotationLookup.ToCoreKey(key);
				if (key != null)
				{
					if (value != null)
					{
						try
						{
							Type valueClass = AnnotationLookup.GetValueType(coreKeyClass);
							if (valueClass == typeof(string))
							{
								chunk.Set(coreKeyClass, value);
							}
							else
							{
								MethodInfo valueOfMethod = valueClass.GetMethod("valueOf", typeof(string));
								if (valueOfMethod != null)
								{
									chunk.Set(coreKeyClass, valueOfMethod.Invoke(valueClass, value));
								}
							}
						}
						catch (Exception ex)
						{
							throw new Exception("Unable to annotate attribute " + key, ex);
						}
					}
					else
					{
						chunk.Set(coreKeyClass, null);
					}
				}
				else
				{
					throw new NotSupportedException("Unknown null attribute.");
				}
			}
		}

		public static void AnnotateChunks<_T0>(IList<_T0> chunks, int start, int end, IDictionary<string, string> attributes)
			where _T0 : ICoreMap
		{
			for (int i = start; i < end; i++)
			{
				AnnotateChunk(chunks[i], attributes);
			}
		}

		public static void AnnotateChunks<_T0>(IList<_T0> chunks, IDictionary<string, string> attributes)
			where _T0 : ICoreMap
		{
			foreach (ICoreMap chunk in chunks)
			{
				AnnotateChunk(chunk, attributes);
			}
		}

		public static T CreateCoreMap<T>(ICoreMap cm, string text, int start, int end, ICoreTokenFactory<T> factory)
			where T : ICoreMap
		{
			if (end > start)
			{
				T token = factory.MakeToken();
				int cmCharStart = cm.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				if (cmCharStart == null)
				{
					cmCharStart = 0;
				}
				string tokenText = Sharpen.Runtime.Substring(text, start, end);
				token.Set(typeof(CoreAnnotations.TextAnnotation), tokenText);
				if (token is CoreLabel)
				{
					token.Set(typeof(CoreAnnotations.ValueAnnotation), tokenText);
				}
				token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), cmCharStart + start);
				token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), cmCharStart + end);
				return token;
			}
			else
			{
				return null;
			}
		}

		public static void AppendCoreMap<T>(IList<T> res, ICoreMap cm, string text, int start, int end, ICoreTokenFactory<T> factory)
			where T : ICoreMap
		{
			T scm = CreateCoreMap(cm, text, start, end, factory);
			if (scm != null)
			{
				res.Add(scm);
			}
		}

		public static IList<T> SplitCoreMap<T>(Pattern p, bool includeMatched, ICoreMap cm, ICoreTokenFactory<T> factory)
			where T : ICoreMap
		{
			IList<T> res = new List<T>();
			string text = cm.Get(typeof(CoreAnnotations.TextAnnotation));
			Matcher m = p.Matcher(text);
			int index = 0;
			while (m.Find())
			{
				int start = m.Start();
				int end = m.End();
				// Include characters from index to m.start()
				AppendCoreMap(res, cm, text, index, start, factory);
				// Include matched pattern
				if (includeMatched)
				{
					AppendCoreMap(res, cm, text, start, end, factory);
				}
				index = end;
			}
			AppendCoreMap(res, cm, text, index, text.Length, factory);
			return res;
		}
	}
}
