using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class is the same as a regular Extractor, but keeps a pointer
	/// to the tagger's dictionary as well.
	/// </summary>
	/// <remarks>
	/// This class is the same as a regular Extractor, but keeps a pointer
	/// to the tagger's dictionary as well.
	/// Obviously that means this kind of extractor is not reusable across
	/// multiple taggers (see comments Extractor.java), so no extractor of
	/// this type should be declared static.
	/// </remarks>
	[System.Serializable]
	public class DictionaryExtractor : Extractor
	{
		private const long serialVersionUID = 692763177746328195L;

		/// <summary>A pointer to the creating / owning tagger's dictionary.</summary>
		[System.NonSerialized]
		protected internal Dictionary dict;

		/// <summary>
		/// Any subclass of this extractor that overrides setGlobalHolder
		/// should call this class's setGlobalHolder as well...
		/// </summary>
		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			base.SetGlobalHolder(tagger);
			this.dict = tagger.dict;
		}
	}
}
