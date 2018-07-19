using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>
	/// Converts a
	/// <c>Counter&lt;K&gt;</c>
	/// to a
	/// <see cref="CompressedFeatureVector"/>
	/// (i.e., parallel lists of integer
	/// keys and double values), which takes up much less memory.
	/// </summary>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class Compressor<K>
	{
		private const long serialVersionUID = 364548642855692442L;

		private readonly IDictionary<K, int> index;

		private readonly IDictionary<int, K> inverse;

		public Compressor()
		{
			index = new Dictionary<K, int>();
			inverse = new Dictionary<int, K>();
		}

		public virtual CompressedFeatureVector Compress(ICounter<K> c)
		{
			IList<int> keys = new List<int>(c.Size());
			IList<double> values = new List<double>(c.Size());
			foreach (KeyValuePair<K, double> e in c.EntrySet())
			{
				K key = e.Key;
				int id = index[key];
				if (id == null)
				{
					id = index.Count;
					inverse[id] = key;
					index[key] = id;
				}
				keys.Add(id);
				values.Add(e.Value);
			}
			return new CompressedFeatureVector(keys, values);
		}

		public virtual ICounter<K> Uncompress(CompressedFeatureVector cvf)
		{
			ICounter<K> c = new ClassicCounter<K>();
			for (int i = 0; i < cvf.keys.Count; i++)
			{
				c.IncrementCount(inverse[cvf.keys[i]], cvf.values[i]);
			}
			return c;
		}
	}
}
