using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth
{
	/// <author>Mason Smith</author>
	public class RothResultsByRelation : ResultsPrinter
	{
		private RelationFeatureFactory featureFactory;

		private RelationMentionFactory mentionFactory;

		/*
		* FeatureFactory class to use for generating features from relations for relation extraction.
		* Default is "edu.stanford.nlp.ie.machinereading.RelationFeatureFactory"
		*/
		//private Class<RelationFeatureFactory> relationFeatureFactoryClass = edu.stanford.nlp.ie.machinereading.RelationFeatureFactory.class;
		/*
		* comma-separated list of feature types to generate for relation extraction
		*/
		//private String relationFeatures;
		public override void PrintResults(PrintWriter pw, IList<ICoreMap> goldStandard, IList<ICoreMap> extractorOutput)
		{
			featureFactory = MachineReading.MakeRelationFeatureFactory(MachineReadingProperties.relationFeatureFactoryClass, MachineReadingProperties.relationFeatures, false);
			mentionFactory = new RelationMentionFactory();
			// generic mentions work well in this domain
			ResultsPrinter.Align(goldStandard, extractorOutput);
			IList<RelationMention> relations = new List<RelationMention>();
			IDictionary<RelationMention, string> predictions = new Dictionary<RelationMention, string>();
			for (int i = 0; i < goldStandard.Count; i++)
			{
				IList<RelationMention> goldRelations = AnnotationUtils.GetAllRelations(mentionFactory, goldStandard[i], true);
				Sharpen.Collections.AddAll(relations, goldRelations);
				foreach (RelationMention rel in goldRelations)
				{
					predictions[rel] = AnnotationUtils.GetRelation(mentionFactory, extractorOutput[i], rel.GetArg(0), rel.GetArg(1)).GetType();
				}
			}
			ICounter<Pair<Pair<string, string>, string>> pathCounts = new ClassicCounter<Pair<Pair<string, string>, string>>();
			foreach (RelationMention rel_1 in relations)
			{
				pathCounts.IncrementCount(new Pair<Pair<string, string>, string>(new Pair<string, string>(rel_1.GetArg(0).GetType(), rel_1.GetArg(1).GetType()), featureFactory.GetFeature(rel_1, "dependency_path_lowlevel")));
			}
			ICounter<string> singletonCorrect = new ClassicCounter<string>();
			ICounter<string> singletonPredicted = new ClassicCounter<string>();
			ICounter<string> singletonActual = new ClassicCounter<string>();
			foreach (RelationMention rel_2 in relations)
			{
				if (pathCounts.GetCount(new Pair<Pair<string, string>, string>(new Pair<string, string>(rel_2.GetArg(0).GetType(), rel_2.GetArg(1).GetType()), featureFactory.GetFeature(rel_2, "dependency_path_lowlevel"))) == 1.0)
				{
					string prediction = predictions[rel_2];
					if (prediction.Equals(rel_2.GetType()))
					{
						singletonCorrect.IncrementCount(prediction);
					}
					singletonPredicted.IncrementCount(prediction);
					singletonActual.IncrementCount(rel_2.GetType());
				}
			}
			// Group together actual relations of a type with relations that were
			// predicted to be that type
			// String rel1group = RelationsSentence.isUnrelatedLabel(rel1.getType())
			// ? prediction1 : rel1.getType();
			// String rel2group = RelationsSentence.isUnrelatedLabel(rel2.getType())
			// ? prediction2 : rel2.getType();
			// int groupComp = rel1group.compareTo(rel2group);
			// int pathComp =
			// getFeature(rel1,"generalized_dependency_path").compareTo(getFeature(rel2,"generalized_dependency_path"));
			// } else if (pathComp != 0) {
			// return pathComp;
			_T1018869951 relComp = new _T1018869951(this);
			relations.Sort(relComp);
			foreach (RelationMention rel_3 in relations)
			{
				string prediction = predictions[rel_3];
				// if (RelationsSentence.isUnrelatedLabel(prediction) &&
				// RelationsSentence.isUnrelatedLabel(rel.getType())) {
				// continue;
				// }
				string type1 = rel_3.GetArg(0).GetType();
				string type2 = rel_3.GetArg(1).GetType();
				string path = featureFactory.GetFeature(rel_3, "dependency_path_lowlevel");
				if (!((type1.Equals("PEOPLE") && type2.Equals("PEOPLE")) || (type1.Equals("PEOPLE") && type2.Equals("LOCATION")) || (type1.Equals("LOCATION") && type2.Equals("LOCATION")) || (type1.Equals("ORGANIZATION") && type2.Equals("LOCATION")) || (type1
					.Equals("PEOPLE") && type2.Equals("ORGANIZATION"))))
				{
					continue;
				}
				if (path.Equals(string.Empty))
				{
					continue;
				}
				pw.Println("\nLABEL: " + prediction);
				pw.Println(rel_3);
				pw.Println(path);
				pw.Println(featureFactory.GetFeatures(rel_3, "dependency_path_words"));
				pw.Println(featureFactory.GetFeature(rel_3, "surface_path_POS"));
			}
		}

		internal class _T1018869951 : IComparator<RelationMention>
		{
			public virtual int Compare(RelationMention rel1, RelationMention rel2)
			{
				string prediction1 = predictions[rel1];
				string prediction2 = predictions[rel2];
				int entComp = string.CompareOrdinal((rel1.GetArg(0).GetType() + rel1.GetArg(1).GetType()), rel2.GetArg(0).GetType() + rel2.GetArg(1).GetType());
				int typeComp = string.CompareOrdinal(rel1.GetType(), rel2.GetType());
				int predictionComp = string.CompareOrdinal(prediction1, prediction2);
				double pathCount1 = pathCounts.GetCount(new Pair<Pair<string, string>, string>(new Pair<string, string>(rel1.GetArg(0).GetType(), rel1.GetArg(1).GetType()), this._enclosing.featureFactory.GetFeature(rel1, "dependency_path_lowlevel")));
				double pathCount2 = pathCounts.GetCount(new Pair<Pair<string, string>, string>(new Pair<string, string>(rel2.GetArg(0).GetType(), rel2.GetArg(1).GetType()), this._enclosing.featureFactory.GetFeature(rel2, "dependency_path_lowlevel")));
				if (entComp != 0)
				{
					return entComp;
				}
				else
				{
					if (pathCount1 < pathCount2)
					{
						return -1;
					}
					else
					{
						if (pathCount1 > pathCount2)
						{
							return 1;
						}
						else
						{
							if (typeComp != 0)
							{
								return typeComp;
							}
							else
							{
								if (predictionComp != 0)
								{
									return predictionComp;
								}
								else
								{
									return string.CompareOrdinal(rel1.GetSentence().Get(typeof(CoreAnnotations.TextAnnotation)), rel2.GetSentence().Get(typeof(CoreAnnotations.TextAnnotation)));
								}
							}
						}
					}
				}
			}

			internal _T1018869951(RothResultsByRelation _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly RothResultsByRelation _enclosing;
		}

		public override void PrintResultsUsingLabels(PrintWriter pw, IList<string> goldStandard, IList<string> extractorOutput)
		{
		}
	}
}
