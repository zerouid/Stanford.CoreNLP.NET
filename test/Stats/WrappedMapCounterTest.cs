using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Tests a counter that wraps an underlying map.</summary>
	/// <author>dramage</author>
	public class WrappedMapCounterTest : CounterTestBase
	{
		public WrappedMapCounterTest()
			: base(Counters.FromMap<double>(new Dictionary<string, double>()))
		{
		}
	}
}
