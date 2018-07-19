using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Machinereading.Structure
{
	/// <summary>Apparently nothing works unless I test it.</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class SpanTest
	{
		[Test]
		public virtual void TestUnion()
		{
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(1, 2), Span.FromValues(3, 5)));
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(1, 2), Span.FromValues(1, 5)));
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(1, 5), Span.FromValues(2, 3)));
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(3, 5), Span.FromValues(1, 2)));
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(1, 1), Span.FromValues(5, 5)));
			NUnit.Framework.Assert.AreEqual(Span.FromValues(1, 5), Span.Union(Span.FromValues(5, 5), Span.FromValues(1, 1)));
		}
	}
}
