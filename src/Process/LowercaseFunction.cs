


namespace Edu.Stanford.Nlp.Process
{
	[System.Serializable]
	public class LowercaseFunction : IFunction<string, string>
	{
		public virtual string Apply(string input)
		{
			if (input == null)
			{
				return null;
			}
			return input.ToLower();
		}

		private const long serialVersionUID = 1L;
	}
}
