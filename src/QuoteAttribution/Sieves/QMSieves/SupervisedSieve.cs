using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves.Training;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <summary>Created by mjfang on 7/7/16.</summary>
	public class SupervisedSieve : QMSieve
	{
		private ExtractQuotesClassifier quotesClassifier;

		public SupervisedSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacyList)
			: base(doc, characterMap, pronounCorefMap, animacyList, "supervised")
		{
		}

		public virtual void LoadModel(string filename)
		{
			quotesClassifier = new ExtractQuotesClassifier(filename);
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			if (quotesClassifier == null)
			{
				throw new Exception("need to do training first!");
			}
			SupervisedSieveTraining.FeaturesData fd = SupervisedSieveTraining.Featurize(new SupervisedSieveTraining.SieveData(doc, this.characterMap, this.pronounCorefMap, this.animacySet), null, false);
			quotesClassifier.ScoreBestMentionNew(fd, doc);
		}
	}
}
