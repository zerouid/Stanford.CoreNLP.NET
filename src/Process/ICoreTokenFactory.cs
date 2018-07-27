using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>To make tokens like CoreMap or CoreLabel.</summary>
	/// <remarks>
	/// To make tokens like CoreMap or CoreLabel. An alternative to LexedTokenFactory
	/// since this one has option to make tokens differently, which would have been
	/// an overhead for LexedTokenFactory
	/// </remarks>
	/// <author>Sonal Gupta</author>
	/// <?/>
	public interface ICoreTokenFactory<In>
		where In : ICoreMap
	{
		IN MakeToken();

		IN MakeToken(string[] keys, string[] values);

		IN MakeToken(IN tokenToBeCopied);
	}
}
