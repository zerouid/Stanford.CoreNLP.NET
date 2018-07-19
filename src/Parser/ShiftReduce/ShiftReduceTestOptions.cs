using Edu.Stanford.Nlp.Parser.Lexparser;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public class ShiftReduceTestOptions : TestOptions
	{
		public string recordBinarized = null;

		public string recordDebinarized = null;

		public int beamSize = 0;
	}
}
