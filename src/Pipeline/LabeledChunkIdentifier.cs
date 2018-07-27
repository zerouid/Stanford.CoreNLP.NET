using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Identifies chunks based on labels that uses IOB-like encoding
	/// (Erik F.
	/// </summary>
	/// <remarks>
	/// Identifies chunks based on labels that uses IOB-like encoding
	/// (Erik F. Tjong Kim Sang and Jorn Veenstra, Representing Text Chunks, EACL 1999).
	/// Assumes labels have the form
	/// <c>&lt;tag&gt;-&lt;type&gt;</c>
	/// ,
	/// where the tag is a prefix indicating where in the chunk it is.
	/// Supports various encodings: IO, IOB, IOE, BILOU, SBEIO, []
	/// The type is
	/// Example:  Bill   gave  Xerox Bank   of     America shares
	/// IO:       I-PER  O     I-ORG I-ORG  I-ORG  I-ORG   O
	/// IOB1:     I-PER  O     I-ORG B-ORG  I-ORG  I-ORG   O
	/// IOB2:     B-PER  O     B-ORG B-ORG  I-ORG  I-ORG   O
	/// IOE1:     I-PER  O     E-ORG I-ORG  I-ORG  I-ORG   O
	/// IOE2:     E-PER  O     E-ORG I-ORG  I-ORG  E-ORG   O
	/// BILOU:    U-PER  O     U-ORG B-ORG  I-ORG  L-ORG   O
	/// SBEIO:    S-PER  O     S-ORG B-ORG  I-ORG  E-ORG   O
	/// </remarks>
	/// <author>Angel Chang</author>
	public class LabeledChunkIdentifier
	{
		/// <summary>Whether to use or ignore provided tag (the label prefix).</summary>
		private bool ignoreProvidedTag = false;

		/// <summary>Label/Type indicating the token is not a part of a chunk.</summary>
		private string negLabel = "O";

		/// <summary>
		/// What tag to default to if label/type indicate it is part of a chunk
		/// (used if type does not match negLabel and
		/// the tag is not provided or ignoreProvidedTag is set).
		/// </summary>
		private string defaultPosTag = "I";

		/// <summary>
		/// What tag to default to if label/type indicate it is not part of a chunk
		/// (used if type matches negLabel and
		/// the tag is not provided or ignoreProvidedTag is set).
		/// </summary>
		private string defaultNegTag = "O";

		/// <summary>Find and annotate chunks.</summary>
		/// <remarks>Find and annotate chunks.  Returns list of CoreMap (Annotation) objects.</remarks>
		/// <param name="tokens">- List of tokens to look for chunks</param>
		/// <param name="totalTokensOffset">- Index of tokens to offset by</param>
		/// <param name="textKey">- Key to use to find the token text</param>
		/// <param name="labelKey">- Key to use to find the token label (to determine if inside chunk or not)</param>
		/// <returns>List of annotations (each as a CoreMap) representing the chunks of tokens</returns>
		public virtual IList<ICoreMap> GetAnnotatedChunks(IList<CoreLabel> tokens, int totalTokensOffset, Type textKey, Type labelKey)
		{
			return GetAnnotatedChunks(tokens, totalTokensOffset, textKey, labelKey, null, null);
		}

		public virtual IList<ICoreMap> GetAnnotatedChunks(IList<CoreLabel> tokens, int totalTokensOffset, Type textKey, Type labelKey, IPredicate<Pair<CoreLabel, CoreLabel>> checkTokensCompatible)
		{
			return GetAnnotatedChunks(tokens, totalTokensOffset, textKey, labelKey, null, null, checkTokensCompatible);
		}

		public virtual IList<ICoreMap> GetAnnotatedChunks(IList<CoreLabel> tokens, int totalTokensOffset, Type textKey, Type labelKey, Type tokenChunkKey, Type tokenLabelKey)
		{
			return GetAnnotatedChunks(tokens, totalTokensOffset, textKey, labelKey, tokenChunkKey, tokenLabelKey, null);
		}

		/// <summary>Find and annotate chunks.</summary>
		/// <remarks>
		/// Find and annotate chunks.  Returns list of CoreMap (Annotation) objects
		/// each representing a chunk with the following annotations set:
		/// CharacterOffsetBeginAnnotation - set to CharacterOffsetBeginAnnotation of first token in chunk
		/// CharacterOffsetEndAnnotation - set to CharacterOffsetEndAnnotation of last token in chunk
		/// TokensAnnotation - List of tokens in this chunk
		/// TokenBeginAnnotation - Index of first token in chunk (index in original list of tokens)
		/// TokenEndAnnotation - Index of last token in chunk (index in original list of tokens)
		/// TextAnnotation - String representing tokens in this chunks (token text separated by space)
		/// </remarks>
		/// <param name="tokens">- List of tokens to look for chunks</param>
		/// <param name="totalTokensOffset">- Index of tokens to offset by</param>
		/// <param name="labelKey">- Key to use to find the token label (to determine if inside chunk or not)</param>
		/// <param name="textKey">- Key to use to find the token text</param>
		/// <param name="tokenChunkKey">- If not null, each token is annotated with the chunk using this key</param>
		/// <param name="tokenLabelKey">- If not null, each token is annotated with the text associated with the chunk using this key</param>
		/// <param name="checkTokensCompatible">- If not null, additional check to see if this token and the previous are compatible</param>
		/// <returns>List of annotations (each as a CoreMap) representing the chunks of tokens</returns>
		public virtual IList<ICoreMap> GetAnnotatedChunks(IList<CoreLabel> tokens, int totalTokensOffset, Type textKey, Type labelKey, Type tokenChunkKey, Type tokenLabelKey, IPredicate<Pair<CoreLabel, CoreLabel>> checkTokensCompatible)
		{
			IList<ICoreMap> chunks = new ArrayList();
			LabeledChunkIdentifier.LabelTagType prevTagType = null;
			int tokenBegin = -1;
			for (int i = 0; i < tokens.Count; i++)
			{
				CoreLabel token = tokens[i];
				string label = (string)token.Get(labelKey);
				LabeledChunkIdentifier.LabelTagType curTagType = GetTagType(label);
				bool isCompatible = true;
				if (checkTokensCompatible != null)
				{
					CoreLabel prev = null;
					if (i > 0)
					{
						prev = tokens[i - 1];
					}
					Pair<CoreLabel, CoreLabel> p = Pair.MakePair(token, prev);
					isCompatible = checkTokensCompatible.Test(p);
				}
				if (IsEndOfChunk(prevTagType, curTagType) || !isCompatible)
				{
					int tokenEnd = i;
					if (tokenBegin >= 0 && tokenEnd > tokenBegin)
					{
						ICoreMap chunk = ChunkAnnotationUtils.GetAnnotatedChunk(tokens, tokenBegin, tokenEnd, totalTokensOffset, tokenChunkKey, textKey, tokenLabelKey);
						chunk.Set(labelKey, prevTagType.type);
						chunks.Add(chunk);
						tokenBegin = -1;
					}
				}
				if (IsStartOfChunk(prevTagType, curTagType) || (!isCompatible && IsChunk(curTagType)))
				{
					if (tokenBegin >= 0)
					{
						throw new Exception("New chunk started, prev chunk not ended yet!");
					}
					tokenBegin = i;
				}
				prevTagType = curTagType;
			}
			if (tokenBegin >= 0)
			{
				ICoreMap chunk = ChunkAnnotationUtils.GetAnnotatedChunk(tokens, tokenBegin, tokens.Count, totalTokensOffset, tokenChunkKey, textKey, tokenLabelKey);
				chunk.Set(labelKey, prevTagType.type);
				chunks.Add(chunk);
			}
			//    System.out.println("number of chunks " +  chunks.size());
			return chunks;
		}

		/// <summary>Returns whether a chunk ended between the previous and current token.</summary>
		/// <param name="prevTag">- the tag of the previous token</param>
		/// <param name="prevType">- the type of the previous token</param>
		/// <param name="curTag">- the tag of the current token</param>
		/// <param name="curType">- the type of the current token</param>
		/// <returns>true if the previous token was the last token of a chunk</returns>
		private static bool IsEndOfChunk(string prevTag, string prevType, string curTag, string curType)
		{
			bool chunkEnd = false;
			if ("B".Equals(prevTag) && "B".Equals(curTag))
			{
				chunkEnd = true;
			}
			if ("B".Equals(prevTag) && "O".Equals(curTag))
			{
				chunkEnd = true;
			}
			if ("I".Equals(prevTag) && "B".Equals(curTag))
			{
				chunkEnd = true;
			}
			if ("I".Equals(prevTag) && "O".Equals(curTag))
			{
				chunkEnd = true;
			}
			if ("E".Equals(prevTag) || "L".Equals(prevTag) || "S".Equals(prevTag) || "U".Equals(prevTag) || "[".Equals(prevTag) || "]".Equals(prevTag))
			{
				chunkEnd = true;
			}
			if (!"O".Equals(prevTag) && !".".Equals(prevTag) && !prevType.Equals(curType))
			{
				chunkEnd = true;
			}
			return chunkEnd;
		}

		/// <summary>Returns whether a chunk ended between the previous and current token.</summary>
		/// <param name="prev">- the label/tag/type of the previous token</param>
		/// <param name="cur">- the label/tag/type of the current token</param>
		/// <returns>true if the previous token was the last token of a chunk</returns>
		public static bool IsEndOfChunk(LabeledChunkIdentifier.LabelTagType prev, LabeledChunkIdentifier.LabelTagType cur)
		{
			if (prev == null)
			{
				return false;
			}
			return IsEndOfChunk(prev.tag, prev.type, cur.tag, cur.type);
		}

		/// <summary>Returns whether a chunk started between the previous and current token</summary>
		/// <param name="prevTag">- the tag of the previous token</param>
		/// <param name="prevType">- the type of the previous token</param>
		/// <param name="curTag">- the tag of the current token</param>
		/// <param name="curType">- the type of the current token</param>
		/// <returns>true if the current token was the first token of a chunk</returns>
		private static bool IsStartOfChunk(string prevTag, string prevType, string curTag, string curType)
		{
			bool chunkStart = false;
			bool prevTagE = "E".Equals(prevTag) || "L".Equals(prevTag) || "S".Equals(prevTag) || "U".Equals(prevTag);
			bool curTagE = "E".Equals(curTag) || "L".Equals(curTag) || "S".Equals(curTag) || "U".Equals(curTag);
			if (prevTagE && curTagE)
			{
				chunkStart = true;
			}
			if (prevTagE && "I".Equals(curTag))
			{
				chunkStart = true;
			}
			if ("O".Equals(prevTag) && curTagE)
			{
				chunkStart = true;
			}
			if ("O".Equals(prevTag) && "I".Equals(curTag))
			{
				chunkStart = true;
			}
			if ("B".Equals(curTag) || "S".Equals(curTag) || "U".Equals(curTag) || "[".Equals(curTag) || "]".Equals(curTag))
			{
				chunkStart = true;
			}
			if (!"O".Equals(curTag) && !".".Equals(curTag) && !prevType.Equals(curType))
			{
				chunkStart = true;
			}
			return chunkStart;
		}

		/// <summary>Returns whether a chunk started between the previous and current token</summary>
		/// <param name="prev">- the label/tag/type of the previous token</param>
		/// <param name="cur">- the label/tag/type of the current token</param>
		/// <returns>true if the current token was the first token of a chunk</returns>
		public static bool IsStartOfChunk(LabeledChunkIdentifier.LabelTagType prev, LabeledChunkIdentifier.LabelTagType cur)
		{
			if (prev == null)
			{
				return IsStartOfChunk("O", "O", cur.tag, cur.type);
			}
			else
			{
				return IsStartOfChunk(prev.tag, prev.type, cur.tag, cur.type);
			}
		}

		private static bool IsChunk(LabeledChunkIdentifier.LabelTagType cur)
		{
			return (!"O".Equals(cur.tag) && !".".Equals(cur.tag));
		}

		private static readonly Pattern labelPattern = Pattern.Compile("^([^-]*)-(.*)$");

		/// <summary>Class representing a label, tag and type.</summary>
		public class LabelTagType
		{
			public string label;

			public string tag;

			public string type;

			public LabelTagType(string label, string tag, string type)
			{
				this.label = label;
				this.tag = tag;
				this.type = type;
			}

			public virtual bool TypeMatches(LabeledChunkIdentifier.LabelTagType other)
			{
				return this.type.Equals(other.type);
			}

			public override string ToString()
			{
				return '(' + label + ',' + tag + ',' + type + ')';
			}
		}

		// end static class LabelTagType
		public virtual LabeledChunkIdentifier.LabelTagType GetTagType(string label)
		{
			if (label == null)
			{
				return new LabeledChunkIdentifier.LabelTagType(negLabel, defaultNegTag, negLabel);
			}
			string type;
			string tag;
			Matcher matcher = labelPattern.Matcher(label);
			if (matcher.Matches())
			{
				if (ignoreProvidedTag)
				{
					type = matcher.Group(2);
					if (negLabel.Equals(type))
					{
						tag = defaultNegTag;
					}
					else
					{
						tag = defaultPosTag;
					}
				}
				else
				{
					tag = matcher.Group(1);
					type = matcher.Group(2);
				}
			}
			else
			{
				type = label;
				if (negLabel.Equals(label))
				{
					tag = defaultNegTag;
				}
				else
				{
					tag = defaultPosTag;
				}
			}
			return new LabeledChunkIdentifier.LabelTagType(label, tag, type);
		}

		public virtual string GetDefaultPosTag()
		{
			return defaultPosTag;
		}

		public virtual void SetDefaultPosTag(string defaultPosTag)
		{
			this.defaultPosTag = defaultPosTag;
		}

		public virtual string GetDefaultNegTag()
		{
			return defaultNegTag;
		}

		public virtual void SetDefaultNegTag(string defaultNegTag)
		{
			this.defaultNegTag = defaultNegTag;
		}

		public virtual string GetNegLabel()
		{
			return negLabel;
		}

		public virtual void SetNegLabel(string negLabel)
		{
			this.negLabel = negLabel;
		}

		public virtual bool IsIgnoreProvidedTag()
		{
			return ignoreProvidedTag;
		}

		public virtual void SetIgnoreProvidedTag(bool ignoreProvidedTag)
		{
			this.ignoreProvidedTag = ignoreProvidedTag;
		}
	}
}
