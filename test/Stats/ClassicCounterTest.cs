using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Tests for the ClassicCounter.</summary>
	/// <author>dramage</author>
	public class ClassicCounterTest : CounterTestBase
	{
		public ClassicCounterTest()
			: base(new ClassicCounter<string>())
		{
		}
	}
}
