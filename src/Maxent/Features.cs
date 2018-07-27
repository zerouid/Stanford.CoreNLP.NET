/*
* Title:        StanfordMaxEnt<p>
* Description:  A Maximum Entropy Toolkit<p>
* Copyright:    Copyright (c) Kristina Toutanova<p>
* Company:      Stanford University<p>
*/
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Maxent
{
	/// <summary>An ArrayList of Feature.</summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class Features
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Maxent.Features));

		private List<Feature> f = new List<Feature>();

		private const int maxValue = 11000000;

		public Features()
		{
		}

		// todo [cdm 2018]: Probably this class can just be removed! Use ArrayList
		public virtual void Add(Feature m)
		{
			f.Add(m);
		}

		public virtual void RemoveLast()
		{
			f.Remove(f.Count - 1);
		}

		public virtual Feature Get(int index)
		{
			return f[index];
		}

		public virtual int Size()
		{
			return f.Count;
		}

		public virtual Experiments Domain()
		{
			Get(0);
			return Feature.domain;
		}

		public virtual void Clean()
		{
		}

		public virtual void Print()
		{
			for (int i = 0; i < Size(); i++)
			{
				Get(i).Print();
			}
		}

		/// <summary>
		/// reads in the features from a file, having already read the
		/// experiments
		/// </summary>
		public Features(string filename, Experiments domain)
		{
			Exception e1 = new Exception("Incorrect data file format!");
			IIndex<IntPair> instanceIndex = domain.CreateIndex();
			try
			{
				using (BufferedReader @in = new BufferedReader(new FileReader(filename)))
				{
					string s;
					while (true)
					{
						s = @in.ReadLine();
						if (s.Equals("<features>"))
						{
							break;
						}
					}
					if (s == null)
					{
						throw e1;
					}
					s = @in.ReadLine();
					if (!s.StartsWith("<fSize>"))
					{
						throw e1;
					}
					if (!s.EndsWith("</fSize>"))
					{
						throw e1;
					}
					int index1 = s.IndexOf(">");
					int index2 = s.LastIndexOf("<");
					string fSt = Sharpen.Runtime.Substring(s, index1 + 1, index2);
					System.Console.Out.WriteLine(fSt);
					int number = System.Convert.ToInt32(fSt);
					System.Console.Out.WriteLine("fSize is " + number);
					int[] arrIndexes = new int[maxValue];
					double[] arrValues = new double[maxValue];
					for (int f = 0; f < number; f++)
					{
						string line = @in.ReadLine();
						int indSp = -1;
						int current = 0;
						while ((indSp = line.IndexOf(" ")) > -1)
						{
							int x = System.Convert.ToInt32(Sharpen.Runtime.Substring(line, 0, indSp));
							line = Sharpen.Runtime.Substring(line, indSp + 1);
							indSp = line.IndexOf(" ");
							if (indSp == -1)
							{
								indSp = line.Length;
							}
							int y = System.Convert.ToInt32(Sharpen.Runtime.Substring(line, 0, indSp));
							line = Sharpen.Runtime.Substring(line, indSp + 1);
							indSp = line.IndexOf(" ");
							if (indSp == -1)
							{
								indSp = line.Length;
							}
							double val = double.ParseDouble(Sharpen.Runtime.Substring(line, 0, indSp));
							if (indSp < line.Length)
							{
								line = Sharpen.Runtime.Substring(line, indSp + 1);
							}
							arrIndexes[current] = instanceIndex.IndexOf(new IntPair(x, y));
							arrValues[current] = val;
							current++;
						}
						int[] indValues = new int[current];
						double[] values = new double[current];
						for (int j = 0; j < current; j++)
						{
							indValues[j] = arrIndexes[j];
							values[j] = arrValues[j];
						}
						Feature bf = new Feature(domain, indValues, values, instanceIndex);
						this.Add(bf);
					}
				}
			}
			catch (Exception e)
			{
				// for f
				log.Warn(e);
			}
		}
	}
}
