using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Makes sure that the pool creates new Annotators when the signature properties change</summary>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class AnnotatorPoolTest
	{
		internal class SampleAnnotatorFactory : Lazy<IAnnotator>
		{
			private const long serialVersionUID = 1L;

			public SampleAnnotatorFactory(Properties props)
			{
			}

			protected override IAnnotator Compute()
			{
				return new _IAnnotator_22();
			}

			private sealed class _IAnnotator_22 : IAnnotator
			{
				public _IAnnotator_22()
				{
				}

				public void Annotate(Annotation annotation)
				{
				}

				// empty body; we don't actually use it here
				public ICollection<Type> RequirementsSatisfied()
				{
					// empty body; we don't actually use it here
					return Java.Util.Collections.EmptySet();
				}

				public ICollection<Type> Requires()
				{
					// empty body; we don't actually use it here
					return Java.Util.Collections.EmptySet();
				}
			}

			public override bool IsCache()
			{
				return false;
			}
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestSignature()
		{
			Properties props = new Properties();
			props.SetProperty("sample.prop", "v1");
			AnnotatorPool pool = new AnnotatorPool();
			pool.Register("sample", props, new AnnotatorPoolTest.SampleAnnotatorFactory(props));
			IAnnotator a1 = pool.Get("sample");
			System.Console.Out.WriteLine("First annotator: " + a1);
			pool.Register("sample", props, new AnnotatorPoolTest.SampleAnnotatorFactory(props));
			IAnnotator a2 = pool.Get("sample");
			System.Console.Out.WriteLine("Second annotator: " + a2);
			NUnit.Framework.Assert.IsTrue(a1 == a2);
			props.SetProperty("sample.prop", "v2");
			pool.Register("sample", props, new AnnotatorPoolTest.SampleAnnotatorFactory(props));
			IAnnotator a3 = pool.Get("sample");
			System.Console.Out.WriteLine("Third annotator: " + a3);
			NUnit.Framework.Assert.IsTrue(a1 != a3);
		}
		/*public void testGlobalCache() throws Exception {
		Properties props = new Properties();
		props.setProperty("sample.prop", "v1");
		AnnotatorPool pool1 = new AnnotatorPool();
		pool1.register("sample", props, new SampleAnnotatorFactory(props));
		Annotator a1 = pool1.get("sample");
		
		AnnotatorPool pool2 = new AnnotatorPool();
		pool2.register("sample", props, new SampleAnnotatorFactory(props));
		Annotator a2 = pool2.get("sample");
		
		assertTrue(a1 == a2);
		}*/
	}
}
