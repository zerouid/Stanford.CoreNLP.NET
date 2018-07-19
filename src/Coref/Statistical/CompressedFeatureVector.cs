using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>
	/// A low-memory representation of a
	/// <see cref="Edu.Stanford.Nlp.Stats.ICounter{E}"/>
	/// created by a
	/// <see cref="Compressor{K}"/>
	/// .
	/// </summary>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class CompressedFeatureVector
	{
		private const long serialVersionUID = -8889507443653366753L;

		public readonly IList<int> keys;

		public readonly IList<double> values;

		public CompressedFeatureVector(IList<int> keys, IList<double> values)
		{
			this.keys = keys;
			this.values = values;
		}
	}
}
