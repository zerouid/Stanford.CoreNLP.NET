using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class AmericanizeTest
	{
		private string[] exBrEWords = new string[] { "colour", "encyclopaedia", "devour", "glamour", "armour", "haematophilia", "programme", "behaviours", "vapours", "travelling", "realise", "rumours", "detour", "Defence" };

		private string[] exAmEWords = new string[] { "color", "encyclopedia", "devour", "glamour", "armor", "hematophilia", "program", "behaviors", "vapors", "traveling", "realize", "rumors", "detour", "Defense" };

		[NUnit.Framework.Test]
		public virtual void TestAmericanize()
		{
			Americanize am = new Americanize();
			System.Diagnostics.Debug.Assert((exBrEWords.Length == exAmEWords.Length));
			for (int i = 0; i < exBrEWords.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("Americanization failed to agree", Americanize.Americanize(exBrEWords[i]), exAmEWords[i]);
			}
		}
	}
}
