using NUnit.Framework;


namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>Test some simple properties of operators</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class OperatorTest
	{
		[Test]
		public virtual void TestValuesOrderedDesc()
		{
			int currLength = int.MaxValue;
			foreach (Operator op in Operator.valuesByLengthDesc)
			{
				NUnit.Framework.Assert.IsTrue(op.surfaceForm.Split(" ").Length <= currLength);
				currLength = op.surfaceForm.Split(" ").Length;
			}
		}
	}
}
