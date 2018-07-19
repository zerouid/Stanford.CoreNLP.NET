using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public class EntityExtractorResultsPrinter : ResultsPrinter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.EntityExtractorResultsPrinter));

		/// <summary>Contains a set of labels that should be excluded from scoring</summary>
		private ICollection<string> excludedClasses;

		/// <summary>Use subtypes for scoring or just types?</summary>
		private bool useSubTypes;

		private bool verbose;

		private bool verboseInstances;

		private static readonly DecimalFormat Formatter = new DecimalFormat();

		static EntityExtractorResultsPrinter()
		{
			Formatter.SetMaximumFractionDigits(1);
			Formatter.SetMinimumFractionDigits(1);
		}

		public EntityExtractorResultsPrinter()
			: this(null, false)
		{
		}

		protected internal EntityExtractorResultsPrinter(ICollection<string> excludedClasses, bool useSubTypes)
		{
			this.excludedClasses = excludedClasses;
			this.useSubTypes = useSubTypes;
			this.verbose = true;
			this.verboseInstances = true;
		}

		public override void PrintResults(PrintWriter pw, IList<ICoreMap> goldStandard, IList<ICoreMap> extractorOutput)
		{
			ResultsPrinter.Align(goldStandard, extractorOutput);
			ICounter<string> correct = new ClassicCounter<string>();
			ICounter<string> predicted = new ClassicCounter<string>();
			ICounter<string> gold = new ClassicCounter<string>();
			for (int i = 0; i < goldStandard.Count; i++)
			{
				ICoreMap goldSent = goldStandard[i];
				ICoreMap sysSent = extractorOutput[i];
				string sysText = sysSent.Get(typeof(CoreAnnotations.TextAnnotation));
				string goldText = goldSent.Get(typeof(CoreAnnotations.TextAnnotation));
				if (verbose)
				{
					log.Info("SCORING THE FOLLOWING SENTENCE:");
					log.Info(sysSent.Get(typeof(CoreAnnotations.TokensAnnotation)));
				}
				HashSet<string> matchedGolds = new HashSet<string>();
				IList<EntityMention> goldEntities = goldSent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				if (goldEntities == null)
				{
					goldEntities = new List<EntityMention>();
				}
				foreach (EntityMention m in goldEntities)
				{
					string label = MakeLabel(m);
					if (excludedClasses != null && excludedClasses.Contains(label))
					{
						continue;
					}
					gold.IncrementCount(label);
				}
				IList<EntityMention> sysEntities = sysSent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				if (sysEntities == null)
				{
					sysEntities = new List<EntityMention>();
				}
				foreach (EntityMention m_1 in sysEntities)
				{
					string label = MakeLabel(m_1);
					if (excludedClasses != null && excludedClasses.Contains(label))
					{
						continue;
					}
					predicted.IncrementCount(label);
					if (verbose)
					{
						log.Info("COMPARING PREDICTED MENTION: " + m_1);
					}
					bool found = false;
					foreach (EntityMention gm in goldEntities)
					{
						if (matchedGolds.Contains(gm.GetObjectId()))
						{
							continue;
						}
						if (verbose)
						{
							log.Info("\tagainst: " + gm);
						}
						if (gm.Equals(m_1, useSubTypes))
						{
							if (verbose)
							{
								log.Info("\t\t\tMATCH!");
							}
							found = true;
							matchedGolds.Add(gm.GetObjectId());
							if (verboseInstances)
							{
								log.Info("TRUE POSITIVE: " + m_1 + " matched " + gm);
								log.Info("In sentence: " + sysText);
							}
							break;
						}
					}
					if (found)
					{
						correct.IncrementCount(label);
					}
					else
					{
						if (verboseInstances)
						{
							log.Info("FALSE POSITIVE: " + m_1.ToString());
							log.Info("In sentence: " + sysText);
						}
					}
				}
				if (verboseInstances)
				{
					foreach (EntityMention m_2 in goldEntities)
					{
						string label = MakeLabel(m_2);
						if (!matchedGolds.Contains(m_2.GetObjectId()) && (excludedClasses == null || !excludedClasses.Contains(label)))
						{
							log.Info("FALSE NEGATIVE: " + m_2.ToString());
							log.Info("In sentence: " + goldText);
						}
					}
				}
			}
			double totalCount = 0;
			double totalCorrect = 0;
			double totalPredicted = 0;
			pw.Println("Label\tCorrect\tPredict\tActual\tPrecn\tRecall\tF");
			IList<string> labels = new List<string>(gold.KeySet());
			labels.Sort();
			foreach (string label_1 in labels)
			{
				if (excludedClasses != null && excludedClasses.Contains(label_1))
				{
					continue;
				}
				double numCorrect = correct.GetCount(label_1);
				double numPredicted = predicted.GetCount(label_1);
				double trueCount = gold.GetCount(label_1);
				double precision = (numPredicted > 0) ? (numCorrect / numPredicted) : 0;
				double recall = numCorrect / trueCount;
				double f = (precision + recall > 0) ? 2 * precision * recall / (precision + recall) : 0.0;
				pw.Println(StringUtils.PadOrTrim(label_1, 21) + "\t" + numCorrect + "\t" + numPredicted + "\t" + trueCount + "\t" + Formatter.Format(precision * 100) + "\t" + Formatter.Format(100 * recall) + "\t" + Formatter.Format(100 * f));
				totalCount += trueCount;
				totalCorrect += numCorrect;
				totalPredicted += numPredicted;
			}
			double precision_1 = (totalPredicted > 0) ? (totalCorrect / totalPredicted) : 0;
			double recall_1 = totalCorrect / totalCount;
			double f_1 = (totalPredicted > 0 && totalCorrect > 0) ? 2 * precision_1 * recall_1 / (precision_1 + recall_1) : 0.0;
			pw.Println("Total\t" + totalCorrect + "\t" + totalPredicted + "\t" + totalCount + "\t" + Formatter.Format(100 * precision_1) + "\t" + Formatter.Format(100 * recall_1) + "\t" + Formatter.Format(100 * f_1));
		}

		private string MakeLabel(EntityMention m)
		{
			string label = m.GetType();
			if (useSubTypes && m.GetSubType() != null)
			{
				label += "-" + m.GetSubType();
			}
			return label;
		}

		public override void PrintResultsUsingLabels(PrintWriter pw, IList<string> goldStandard, IList<string> extractorOutput)
		{
		}
	}
}
