using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// Feature factory for the IOB clitic segmentation model described by
	/// Green and DeNero (2012).
	/// </summary>
	/// <author>Spence Green</author>
	/// <?/>
	[System.Serializable]
	public class StartAndEndArabicSegmenterFeatureFactory<In> : ArabicSegmenterFeatureFactory<In>
		where In : CoreLabel
	{
		private const long serialVersionUID = 6864940988019110930L;

		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
		}

		protected internal override ICollection<string> FeaturesCpC(PaddedList<In> cInfo, int loc)
		{
			ICollection<string> features = base.FeaturesCpC(cInfo, loc);
			CoreLabel c = cInfo[loc];
			// "Wrapper" feature: identity of first and last two chars of the current word.
			// This helps detect ma+_+sh in dialect, as well as avoiding segmenting possessive
			// pronouns if the word starts with al-.
			if (c.Word().Length > 3)
			{
				string start = Sharpen.Runtime.Substring(c.Word(), 0, 2);
				string end = Sharpen.Runtime.Substring(c.Word(), c.Word().Length - 2);
				if (c.Index() == 2)
				{
					features.Add(start + "_" + end + "-begin-wrap");
				}
				if (c.Index() == c.Word().Length - 1)
				{
					features.Add(start + "_" + end + "-end-wrap");
				}
			}
			return features;
		}
	}
}
