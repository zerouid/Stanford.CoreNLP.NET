using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Evaluate current memory usage</summary>
	/// <author>Angel Chang</author>
	public class MemoryEvaluator : IEvaluator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.MemoryEvaluator));

		private MemoryMonitor memMonitor;

		public MemoryEvaluator()
		{
			memMonitor = new MemoryMonitor();
		}

		public override string ToString()
		{
			return "Memory Usage";
		}

		public virtual double Evaluate(double[] x)
		{
			StringBuilder sb = new StringBuilder("Memory Usage: ");
			sb.Append(" used(KB):").Append(memMonitor.GetUsedMemory(false));
			sb.Append(" maxAvailable(KB):").Append(memMonitor.GetMaxAvailableMemory(false));
			sb.Append(" max(KB):").Append(memMonitor.GetMaxMemory());
			string memString = sb.ToString();
			log.Info(memString);
			return 0;
		}
	}
}
