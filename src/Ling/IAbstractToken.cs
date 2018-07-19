using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>An abstract token.</summary>
	/// <remarks>
	/// An abstract token.
	/// This simply joins all the natural token-like interfaces, like
	/// <see cref="IHasWord"/>
	/// ,
	/// <see cref="IHasLemma"/>
	/// , etc.
	/// </remarks>
	/// <author><a href="mailto:gabor@eloquent.ai">Gabor Angeli</a></author>
	public interface IAbstractToken : IHasWord, IHasIndex, IHasTag, IHasLemma, IHasNER, IHasOffset, IHasOriginalText, IHasContext
	{
	}
}
