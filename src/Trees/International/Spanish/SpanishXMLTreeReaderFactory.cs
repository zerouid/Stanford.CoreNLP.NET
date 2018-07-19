using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>
	/// A class for reading Spanish Treebank trees that have been converted
	/// from XML to PTB format.
	/// </summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class SpanishXMLTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 2019486878175311263L;

		private readonly bool simplifiedTagset;

		private readonly bool aggressiveNormalization;

		private readonly bool retainNER;

		private readonly bool detailedAnnotations;

		public SpanishXMLTreeReaderFactory()
			: this(true, true, false, false)
		{
		}

		public SpanishXMLTreeReaderFactory(bool simplifiedTagset, bool aggressiveNormalization, bool retainNER, bool detailedAnnotations)
		{
			// Initialize with default options
			this.simplifiedTagset = simplifiedTagset;
			this.aggressiveNormalization = aggressiveNormalization;
			this.retainNER = retainNER;
			this.detailedAnnotations = detailedAnnotations;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new SpanishXMLTreeReader(null, @in, simplifiedTagset, aggressiveNormalization, retainNER, detailedAnnotations);
		}

		public virtual ITreeReader NewTreeReader(string path, Reader @in)
		{
			return new SpanishXMLTreeReader(path, @in, simplifiedTagset, aggressiveNormalization, retainNER, detailedAnnotations);
		}
	}
}
