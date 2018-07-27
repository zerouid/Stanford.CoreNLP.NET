using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Constructs
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
	/// s from Strings optionally with
	/// beginning and ending (character after the end) offset positions in
	/// an original text.  The makeToken method will put the token in the
	/// OriginalTextAnnotation AND TextAnnotation keys (2 places!),
	/// and optionally records
	/// begin and position after offsets in BeginPositionAnnotation and
	/// EndPositionAnnotation.  If the tokens are built in PTBTokenizer with
	/// an "invertible" tokenizer, you will also get a BeforeAnnotation and for
	/// the last token an AfterAnnotation. You can also get an empty CoreLabel token.
	/// </summary>
	/// <author>Anna Rafferty</author>
	/// <author>Sonal Gupta (now implements CoreTokenFactory, you can make tokens using many options)</author>
	[System.Serializable]
	public class CoreLabelTokenFactory : ICoreTokenFactory<CoreLabel>, ILexedTokenFactory<CoreLabel>
	{
		private readonly bool addIndices;

		/// <summary>
		/// Constructor for a new token factory which will add in the word, the
		/// "current" annotation, and the begin/end position annotations.
		/// </summary>
		public CoreLabelTokenFactory()
			: this(true)
		{
		}

		/// <summary>
		/// Constructor that allows one to choose if index annotation
		/// indicating begin/end position will be included in the label.
		/// </summary>
		/// <param name="addIndices">if true, begin and end position annotations will be included (this is the default)</param>
		public CoreLabelTokenFactory(bool addIndices)
			: base()
		{
			this.addIndices = addIndices;
		}

		/// <summary>Constructs a CoreLabel as a String with a corresponding BEGIN and END position.</summary>
		/// <remarks>
		/// Constructs a CoreLabel as a String with a corresponding BEGIN and END position.
		/// (Does not take substring).
		/// </remarks>
		public virtual CoreLabel MakeToken(string tokenText, int begin, int length)
		{
			return MakeToken(tokenText, tokenText, begin, length);
		}

		/// <summary>
		/// Constructs a CoreLabel as a String with a corresponding BEGIN and END position,
		/// when the original OriginalTextAnnotation is different from TextAnnotation
		/// (Does not take substring).
		/// </summary>
		public virtual CoreLabel MakeToken(string tokenText, string originalText, int begin, int length)
		{
			CoreLabel cl = addIndices ? new CoreLabel(5) : new CoreLabel();
			cl.SetValue(tokenText);
			cl.SetWord(tokenText);
			cl.SetOriginalText(originalText);
			if (addIndices)
			{
				cl.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
				cl.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), begin + length);
			}
			return cl;
		}

		public virtual CoreLabel MakeToken()
		{
			CoreLabel l = new CoreLabel();
			return l;
		}

		public virtual CoreLabel MakeToken(string[] keys, string[] values)
		{
			CoreLabel l = new CoreLabel(keys, values);
			return l;
		}

		public virtual CoreLabel MakeToken(CoreLabel labelToBeCopied)
		{
			CoreLabel l = new CoreLabel(labelToBeCopied);
			return l;
		}

		private const long serialVersionUID = 4L;
	}
}
