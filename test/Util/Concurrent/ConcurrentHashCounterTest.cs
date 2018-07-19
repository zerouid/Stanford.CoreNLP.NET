using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <author>Spence Green</author>
	public class ConcurrentHashCounterTest : CounterTestBase
	{
		public ConcurrentHashCounterTest()
			: base(new ClassicCounter<string>())
		{
		}
		// TODO(spenceg): Fix concurrenthashcounter and reactivate
		//    super(new ConcurrentHashCounter<String>());
	}
}
