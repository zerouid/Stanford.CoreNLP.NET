using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// Feature factory for the IOB clitic segmentation model described by
	/// Green and DeNero (2012).
	/// </summary>
	/// <author>Spence Green</author>
	/// <?/>
	[System.Serializable]
	public class BigWindowArabicSegmenterFeatureFactory<In> : ArabicSegmenterFeatureFactory<IN>
		where In : CoreLabel
	{
		private const long serialVersionUID = 6864940988019110930L;

		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
		}

		protected internal override ICollection<string> FeaturesC(PaddedList<IN> cInfo, int loc)
		{
			ICollection<string> features = base.FeaturesC(cInfo, loc);
			CoreLabel n3 = cInfo[loc + 3];
			CoreLabel p3 = cInfo[loc - 3];
			string charn3 = n3.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp3 = p3.Get(typeof(CoreAnnotations.CharAnnotation));
			// a 7 character window instead of a 5 character window
			features.Add(charn3 + "-n3");
			features.Add(charp3 + "-p3");
			return features;
		}
	}
}
