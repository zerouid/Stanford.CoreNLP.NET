using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <author>Mengqiu Wang</author>
	public class EmpiricalNERPriorBIO<In> : EntityCachingAbstractSequencePriorBIO<IN>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.EmpiricalNERPriorBIO));

		private double[][] entityMatrix;

		private double[][] subEntityMatrix;

		private SeqClassifierFlags flags;

		protected internal double p1 = Math.Log(0.01);

		protected internal double p2 = Math.Log(2.0);

		protected internal int ORGIndex;

		protected internal int LOCIndex;

		public static bool Debug = false;

		public EmpiricalNERPriorBIO(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<IN> doc, Pair<double[][], double[][]> matrices, SeqClassifierFlags flags)
			: base(backgroundSymbol, classIndex, tagIndex, doc)
		{
			entityMatrix = matrices.First();
			subEntityMatrix = matrices.Second();
			this.flags = flags;
			ORGIndex = tagIndex.IndexOf("ORG");
			LOCIndex = tagIndex.IndexOf("LOC");
		}

		public override double ScoreOf(int[] sequence)
		{
			double p = 0.0;
			for (int i = 0; i < entities.Length; i++)
			{
				EntityBIO entity = entities[i];
				if ((i == 0 || entities[i - 1] != entity) && entity != null)
				{
					int length = entity.words.Count;
					int tag1 = entity.type;
					// String tag1 = classIndex.get(entity.type);
					int[] other = entities[i].otherOccurrences;
					foreach (int otherOccurrence in other)
					{
						EntityBIO otherEntity = null;
						for (int k = otherOccurrence; k < otherOccurrence + length && k < entities.Length; k++)
						{
							otherEntity = entities[k];
							if (otherEntity != null)
							{
								break;
							}
						}
						// singleton + other instance null?
						if (otherEntity == null)
						{
							continue;
						}
						int oLength = otherEntity.words.Count;
						// String tag2 = classIndex.get(otherEntity.type);
						int tag2 = otherEntity.type;
						// exact match??
						bool exact = false;
						int[] oOther = otherEntity.otherOccurrences;
						foreach (int index in oOther)
						{
							if (index >= i && index <= i + length - 1)
							{
								exact = true;
								break;
							}
						}
						double factor;
						// initialized in 2 cases below
						if (exact)
						{
							if (Debug)
							{
								log.Info("Exact match of tag1=" + tagIndex.Get(tag1) + ", tag2=" + tagIndex.Get(tag2));
							}
							// entity not complete
							if (length != oLength)
							{
								// if (DEBUG)
								//   log.info("Entity Not Complete");
								if (tag1 == tag2)
								{
									p += Math.Abs(oLength - length) * p1;
								}
								else
								{
									if (!(tag1 == ORGIndex && tag2 == LOCIndex) && !(tag1 == LOCIndex && tag2 == ORGIndex))
									{
										// shorter
										p += (oLength + length) * p1;
									}
								}
							}
							factor = entityMatrix[tag1][tag2];
						}
						else
						{
							if (Debug)
							{
								log.Info("Sub  match of tag1=" + tagIndex.Get(tag1) + ", tag2=" + tagIndex.Get(tag2));
							}
							factor = subEntityMatrix[tag1][tag2];
						}
						if (tag1 == tag2)
						{
							if (flags.matchNERIncentive)
							{
								factor = p2;
							}
							else
							{
								// factor *= -1;
								factor = 0;
							}
						}
						if (Debug)
						{
							log.Info(" of factor=" + factor + ", p += " + (length * factor));
						}
						p += length * factor;
					}
				}
			}
			return p;
		}
	}
}
