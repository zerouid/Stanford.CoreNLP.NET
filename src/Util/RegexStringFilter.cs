



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Filters Strings based on whether they match a given regex.</summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class RegexStringFilter : IPredicate<string>
	{
		internal readonly Pattern pattern;

		public RegexStringFilter(string pattern)
		{
			this.pattern = Pattern.Compile(pattern);
		}

		public virtual bool Test(string text)
		{
			return pattern.Matcher(text).Matches();
		}

		public override int GetHashCode()
		{
			return pattern.GetHashCode();
		}

		public override bool Equals(object other)
		{
			if (other == this)
			{
				return true;
			}
			if (!(other is Edu.Stanford.Nlp.Util.RegexStringFilter))
			{
				return false;
			}
			return ((Edu.Stanford.Nlp.Util.RegexStringFilter)other).pattern.Equals(pattern);
		}
	}
}
