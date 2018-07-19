using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// This was the empirical NER prior used for long distance consistency
	/// in the Finkel et al.
	/// </summary>
	/// <remarks>
	/// This was the empirical NER prior used for long distance consistency
	/// in the Finkel et al. ACL 2005 paper.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class EmpiricalNERPrior<In> : EntityCachingAbstractSequencePrior<IN>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.EmpiricalNERPrior));

		protected internal const string Org = "ORGANIZATION";

		protected internal const string Per = "PERSON";

		protected internal const string Loc = "LOCATION";

		protected internal const string Misc = "MISC";

		public EmpiricalNERPrior(string backgroundSymbol, IIndex<string> classIndex, IList<IN> doc)
			: base(backgroundSymbol, classIndex, doc)
		{
		}

		protected internal double p1 = -Math.Log(0.01);

		protected internal double dem1 = 6631.0;

		protected internal double p2 = -Math.Log(6436.0 / dem1) / 2.0;

		protected internal double p3 = -Math.Log(188 / dem1) / 2.0;

		protected internal double p4 = -Math.Log(4 / dem1) / 2.0;

		protected internal double p5 = -Math.Log(3 / dem1) / 2.0;

		protected internal double dem2 = 3169.0;

		protected internal double p6 = -Math.Log(188.0 / dem2) / 2.0;

		protected internal double p7 = -Math.Log(2975 / dem2) / 2.0;

		protected internal double p8 = -Math.Log(5 / dem2) / 2.0;

		protected internal double p9 = -Math.Log(1 / dem2) / 2.0;

		protected internal double dem3 = 3151.0;

		protected internal double p10 = -Math.Log(4.0 / dem3) / 2.0;

		protected internal double p11 = -Math.Log(5 / dem3) / 2.0;

		protected internal double p12 = -Math.Log(3141 / dem3) / 2.0;

		protected internal double p13 = -Math.Log(1 / dem3) / 2.0;

		protected internal double dem4 = 2035.0;

		protected internal double p14 = -Math.Log(3.0 / dem4) / 2.0;

		protected internal double p15 = -Math.Log(1 / dem4) / 2.0;

		protected internal double p16 = -Math.Log(1 / dem4) / 2.0;

		protected internal double p17 = -Math.Log(2030 / dem4) / 2.0;

		protected internal double dem5 = 724.0;

		protected internal double p18 = -Math.Log(167.0 / dem5);

		protected internal double p19 = -Math.Log(328.0 / dem5);

		protected internal double p20 = -Math.Log(5.0 / dem5);

		protected internal double p21 = -Math.Log(224.0 / dem5);

		protected internal double dem6 = 834.0;

		protected internal double p22 = -Math.Log(6.0 / dem6);

		protected internal double p23 = -Math.Log(819.0 / dem6);

		protected internal double p24 = -Math.Log(2.0 / dem6);

		protected internal double p25 = -Math.Log(7.0 / dem6);

		protected internal double dem7 = 1978.0;

		protected internal double p26 = -Math.Log(1.0 / dem7);

		protected internal double p27 = -Math.Log(22.0 / dem7);

		protected internal double p28 = -Math.Log(1941.0 / dem7);

		protected internal double p29 = -Math.Log(14.0 / dem7);

		protected internal double dem8 = 622.0;

		protected internal double p30 = -Math.Log(63.0 / dem8);

		protected internal double p31 = -Math.Log(191.0 / dem8);

		protected internal double p32 = -Math.Log(3.0 / dem8);

		protected internal double p33 = -Math.Log(365.0 / dem8);

		public override double ScoreOf(int[] sequence)
		{
			double p = 0.0;
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				//log.info(entity);
				if ((i == 0 || entities[i - 1] != entity) && entity != null)
				{
					//log.info(1);
					int length = entity.words.Count;
					string tag1 = classIndex.Get(entity.type);
					// Use canonical String values, so we can henceforth just use ==
					if (tag1.Equals(Loc))
					{
						tag1 = Loc;
					}
					else
					{
						if (tag1.Equals(Org))
						{
							tag1 = Org;
						}
						else
						{
							if (tag1.Equals(Per))
							{
								tag1 = Per;
							}
							else
							{
								if (tag1.Equals(Misc))
								{
									tag1 = Misc;
								}
							}
						}
					}
					int[] other = entities[i].otherOccurrences;
					foreach (int otherOccurrence in other)
					{
						Entity otherEntity = null;
						for (int k = otherOccurrence; k < otherOccurrence + length && k < entities.Length; k++)
						{
							otherEntity = entities[k];
							if (otherEntity != null)
							{
								//               if (k > other[j]) {
								//                 log.info(entity.words+" "+otherEntity.words);
								//               }
								break;
							}
						}
						// singleton + other instance null?
						if (otherEntity == null)
						{
							//p -= length * Math.log(0.1);
							//if (entity.words.size() == 1) {
							//p -= length * p1;
							//}
							continue;
						}
						int oLength = otherEntity.words.Count;
						string tag2 = classIndex.Get(otherEntity.type);
						// Use canonical String values, so we can henceforth just use ==
						if (tag2.Equals(Loc))
						{
							tag2 = Loc;
						}
						else
						{
							if (tag2.Equals(Org))
							{
								tag2 = Org;
							}
							else
							{
								if (tag2.Equals(Per))
								{
									tag2 = Per;
								}
								else
								{
									if (tag2.Equals(Misc))
									{
										tag2 = Misc;
									}
								}
							}
						}
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
						if (exact)
						{
							// entity not complete
							if (length != oLength)
							{
								if (tag1 == (tag2))
								{
									// || ((tag1 == LOC && tag2 == ORG) || (tag1 == ORG && tag2 == LOC))) { // ||
									//p -= Math.abs(oLength - length) * Math.log(0.1);
									p -= Math.Abs(oLength - length) * p1;
								}
								else
								{
									if (!(tag1.Equals(Org) && tag2.Equals(Loc)) && !(tag2.Equals(Loc) && tag1.Equals(Org)))
									{
										// shorter
										p -= (oLength + length) * p1;
									}
								}
							}
							if (tag1 == (Loc))
							{
								if (tag2 == (Loc))
								{
								}
								else
								{
									//p -= length * Math.log(6436.0 / dem);
									//p -= length * p2;
									if (tag2 == (Org))
									{
										//p -= length * Math.log(188 / dem);
										p -= length * p3;
									}
									else
									{
										if (tag2 == (Per))
										{
											//p -= length * Math.log(4 / dem);
											p -= length * p4;
										}
										else
										{
											if (tag2 == (Misc))
											{
												//p -= length * Math.log(3 / dem);
												p -= length * p5;
											}
										}
									}
								}
							}
							else
							{
								if (tag1 == (Org))
								{
									//double dem = 3169.0;
									if (tag2 == (Loc))
									{
										//p -= length * Math.log(188.0 / dem);
										p -= length * p6;
									}
									else
									{
										if (tag2 == (Org))
										{
										}
										else
										{
											//p -= length * Math.log(2975 / dem);
											//p -= length * p7;
											if (tag2 == (Per))
											{
												//p -= length * Math.log(5 / dem);
												p -= length * p8;
											}
											else
											{
												if (tag2 == (Misc))
												{
													//p -= length * Math.log(1 / dem);
													p -= length * p9;
												}
											}
										}
									}
								}
								else
								{
									if (tag1 == (Per))
									{
										//double dem = 3151.0;
										if (tag2 == (Loc))
										{
											//p -= length * Math.log(4.0 / dem);
											p -= length * p10;
										}
										else
										{
											if (tag2 == (Org))
											{
												//p -= length * Math.log(5 / dem);
												p -= length * p11;
											}
											else
											{
												if (tag2 == (Per))
												{
												}
												else
												{
													//p -= length * Math.log(3141 / dem);
													//p -= length * p12;
													if (tag2 == (Misc))
													{
														//p -= length * Math.log(1 / dem);
														p -= length * p13;
													}
												}
											}
										}
									}
									else
									{
										if (tag1 == (Misc))
										{
											//double dem = 2035.0;
											if (tag2 == (Loc))
											{
												//p -= length * Math.log(3.0 / dem);
												p -= length * p14;
											}
											else
											{
												if (tag2 == (Org))
												{
													//p -= length * Math.log(1 / dem);
													p -= length * p15;
												}
												else
												{
													if (tag2 == (Per))
													{
														//p -= length * Math.log(1 / dem);
														p -= length * p16;
													}
													else
													{
														if (tag2 == (Misc))
														{
														}
													}
												}
											}
										}
									}
								}
							}
						}
						else
						{
							//p -= length * Math.log(2030 / dem);
							//p -= length * p17;
							if (tag1 == (Loc))
							{
								//double dem = 724.0;
								if (tag2 == (Loc))
								{
								}
								else
								{
									//p -= length * Math.log(167.0 / dem);
									//p -= length * p18;
									if (tag2 == (Org))
									{
									}
									else
									{
										//p -= length * Math.log(328.0 / dem);
										//p -= length * p19;
										if (tag2 == (Per))
										{
											//p -= length * Math.log(5.0 / dem);
											p -= length * p20;
										}
										else
										{
											if (tag2 == (Misc))
											{
												//p -= length * Math.log(224.0 / dem);
												p -= length * p21;
											}
										}
									}
								}
							}
							else
							{
								if (tag1 == (Org))
								{
									//double dem = 834.0;
									if (tag2 == (Loc))
									{
										//p -= length * Math.log(6.0 / dem);
										p -= length * p22;
									}
									else
									{
										if (tag2 == (Org))
										{
										}
										else
										{
											//p -= length * Math.log(819.0 / dem);
											//p -= length * p23;
											if (tag2 == (Per))
											{
												//p -= length * Math.log(2.0 / dem);
												p -= length * p24;
											}
											else
											{
												if (tag2 == (Misc))
												{
													//p -= length * Math.log(7.0 / dem);
													p -= length * p25;
												}
											}
										}
									}
								}
								else
								{
									if (tag1 == (Per))
									{
										//double dem = 1978.0;
										if (tag2 == (Loc))
										{
											//p -= length * Math.log(1.0 / dem);
											p -= length * p26;
										}
										else
										{
											if (tag2 == (Org))
											{
												//p -= length * Math.log(22.0 / dem);
												p -= length * p27;
											}
											else
											{
												if (tag2 == (Per))
												{
												}
												else
												{
													//p -= length * Math.log(1941.0 / dem);
													//p -= length * p28;
													if (tag2 == (Misc))
													{
														//p -= length * Math.log(14.0 / dem);
														p -= length * p29;
													}
												}
											}
										}
									}
									else
									{
										if (tag1 == (Misc))
										{
											//double dem = 622.0;
											if (tag2 == (Loc))
											{
												//p -= length * Math.log(63.0 / dem);
												p -= length * p30;
											}
											else
											{
												if (tag2 == (Org))
												{
													//p -= length * Math.log(191.0 / dem);
													p -= length * p31;
												}
												else
												{
													if (tag2 == (Per))
													{
														//p -= length * Math.log(3.0 / dem);
														p -= length * p32;
													}
													else
													{
														if (tag2 == (Misc))
														{
															//p -= length * Math.log(365.0 / dem);
															p -= length * p33;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			//           if (tag1 == PER) {
			//             int personIndex = classIndex.indexOf(PER);
			//             String lastName = entity.words.get(entity.words.size()-1);
			//             for (int k = 0; k < doc.size(); k++) {
			//               String w = doc.get(k).word();
			//               if (w.equalsIgnoreCase(lastName)) {
			//                 if (sequence[k] != personIndex) {
			//                   p -= p1;
			//                 }
			//               }
			//             }
			//           }
			return p;
		}
	}
}
