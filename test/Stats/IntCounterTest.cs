

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Tests for the IntCounter.</summary>
	/// <author>Christopher Manning</author>
	public class IntCounterTest : CounterTestBase
	{
		public IntCounterTest()
			: base(new IntCounter<string>(), true)
		{
		}
	}
}
