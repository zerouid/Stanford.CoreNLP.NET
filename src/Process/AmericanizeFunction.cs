


namespace Edu.Stanford.Nlp.Process
{
	[System.Serializable]
	public class AmericanizeFunction : Func<string, string>
	{
		public virtual string Apply(string input)
		{
			if (input == null)
			{
				return null;
			}
			return Americanize.Americanize(input);
		}

		private const long serialVersionUID = 1L;
	}
}
