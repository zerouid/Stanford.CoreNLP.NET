

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>This token can produce / set original texts</summary>
	/// <author>Gabor Angeli</author>
	public interface IHasOriginalText
	{
		// These next two are a partial implementation of HasContext. Maybe clean this up someday?
		string OriginalText();

		void SetOriginalText(string originalText);
	}
}
