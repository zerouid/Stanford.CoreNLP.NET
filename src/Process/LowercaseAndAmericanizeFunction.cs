using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	[System.Serializable]
	public class LowercaseAndAmericanizeFunction : IFunction<string, string>
	{
		public virtual string Apply(string input)
		{
			if (input == null)
			{
				return null;
			}
			return Americanize.Americanize(input.ToLower());
		}

		private const long serialVersionUID = 1L;
	}
}
