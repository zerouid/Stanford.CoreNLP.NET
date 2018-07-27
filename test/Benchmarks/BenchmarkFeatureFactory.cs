using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Benchmarks
{
	/// <summary>Created by keenon on 6/19/15.</summary>
	/// <remarks>
	/// Created by keenon on 6/19/15.
	/// Simple feature factory to enable benchmarking of the CRF classifier as it currently is.
	/// </remarks>
	[System.Serializable]
	public class BenchmarkFeatureFactory : FeatureFactory<CoreLabel>
	{
		public override ICollection<string> GetCliqueFeatures(PaddedList<CoreLabel> info, int position, Clique clique)
		{
			ICollection<string> features = new HashSet<string>();
			foreach (CoreLabel l in info)
			{
				for (int i = 0; i < 10; i++)
				{
					features.Add("feat" + i + ":" + l.Word());
				}
			}
			return features;
		}
	}
}
