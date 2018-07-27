// TaggerExperiments -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2008 Leland Stanford Junior University
//
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    http://www-nlp.stanford.edu/software/tagger.shtml
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Maxent;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>This class represents the training samples.</summary>
	/// <remarks>
	/// This class represents the training samples. It can return statistics of
	/// them, for example the frequency of each x or y in the training data.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class TaggerExperiments : Experiments
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.TaggerExperiments));

		private const bool Debug = true;

		private const string zeroSt = "0";

		private readonly TaggerFeatures feats;

		private readonly ICollection<FeatureKey> sTemplates = Generics.NewHashSet();

		private readonly HistoryTable tHistories = new HistoryTable();

		private readonly int numFeatsGeneral;

		private readonly int numFeatsAll;

		private readonly MaxentTagger maxentTagger;

		private readonly TemplateHash tFeature;

		private byte[][] fnumArr;

		internal TaggerExperiments(MaxentTagger maxentTagger)
		{
			// This constructor is only used by unit tests.
			this.maxentTagger = maxentTagger;
			this.tFeature = new TemplateHash(maxentTagger);
			numFeatsGeneral = maxentTagger.extractors.Size();
			numFeatsAll = numFeatsGeneral + maxentTagger.extractorsRare.Size();
			feats = new TaggerFeatures(this);
		}

		/// <summary>This method gets feature statistics from a training file found in the TaggerConfig.</summary>
		/// <remarks>
		/// This method gets feature statistics from a training file found in the TaggerConfig.
		/// It is the start of the training process.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		protected internal TaggerExperiments(TaggerConfig config, MaxentTagger maxentTagger)
			: this(maxentTagger)
		{
			log.Info("TaggerExperiments: adding word/tags");
			PairsHolder pairs = new PairsHolder();
			ReadDataTagged c = new ReadDataTagged(config, maxentTagger, pairs);
			vArray = new int[][] {  };
			InitTemplatesNew();
			log.Info("Featurizing tagged data tokens...");
			for (int i = 0; i < size; i++)
			{
				DataWordTag d = c.Get(i);
				string yS = d.GetY();
				History h = d.GetHistory();
				int indX = tHistories.Add(h);
				int indY = d.GetYInd();
				AddTemplatesNew(h, yS);
				AddRareTemplatesNew(h, yS);
				vArray[i][0] = indX;
				vArray[i][1] = indY;
			}
			// It's the 2010s now and it doesn't take so long to featurize....
			// if (i > 0 && (i % 10000) == 0) {
			//   System.err.printf("%d ", i);
			//   if (i % 100000 == 0) { System.err.println(); }
			// }
			// log.info();
			log.Info("Featurized " + c.GetSize() + " data tokens [done].");
			c.Release();
			Ptilde();
			maxentTagger.xSize = xSize;
			maxentTagger.ySize = ySize;
			log.Info("xSize [num Phi templates] = " + xSize + "; ySize [num classes] = " + ySize);
			HashHistories();
			// if we'll look at occurring tags only, we need the histories and pairs still
			if (!maxentTagger.occurringTagsOnly && !maxentTagger.possibleTagsOnly)
			{
				tHistories.Release();
				pairs.Clear();
			}
			GetFeaturesNew();
		}

		public virtual TaggerFeatures GetTaggerFeatures()
		{
			return feats;
		}

		/// <summary>Adds a FeatureKey to the set of known FeatureKeys.</summary>
		/// <param name="s">The feature key to be added</param>
		/// <returns>Whether the key was already known (false) or added (true)</returns>
		protected internal virtual bool Add(FeatureKey s)
		{
			if ((sTemplates.Contains(s)))
			{
				return false;
			}
			sTemplates.Add(s);
			return true;
		}

		internal virtual byte[][] GetFnumArr()
		{
			return fnumArr;
		}

		/// <summary>This method uses and deletes a file tempXXXXXX.x in the current directory!</summary>
		private void GetFeaturesNew()
		{
			// todo: Change to rethrow a RuntimeIOException.
			// todo: can fnumArr overflow?
			try
			{
				log.Info("TaggerExperiments.getFeaturesNew: initializing fnumArr.");
				fnumArr = new byte[xSize][];
				// what is the maximum number of active features
				File hFile = File.CreateTempFile("temp", ".x", new File("./"));
				RandomAccessFile hF = new RandomAccessFile(hFile, "rw");
				log.Info("  length of sTemplates keys: " + sTemplates.Count);
				log.Info("getFeaturesNew adding features ...");
				int current = 0;
				int numFeats = 0;
				bool Verbose = false;
				foreach (FeatureKey fK in sTemplates)
				{
					int numF = fK.num;
					int[] xValues;
					Pair<int, string> wT = new Pair<int, string>(numF, fK.val);
					xValues = tFeature.GetXValues(wT);
					if (xValues == null)
					{
						log.Info("  xValues is null: " + fK);
						//  + " " + i
						continue;
					}
					int numEvidence = 0;
					int y = maxentTagger.tags.GetIndex(fK.tag);
					foreach (int xValue in xValues)
					{
						if (maxentTagger.occurringTagsOnly)
						{
							//check whether the current word in x has occurred with y
							string word = ExtractorFrames.cWord.Extract(tHistories.GetHistory(xValue));
							if (maxentTagger.dict.GetCount(word, fK.tag) == 0)
							{
								continue;
							}
						}
						if (maxentTagger.possibleTagsOnly)
						{
							string word = ExtractorFrames.cWord.Extract(tHistories.GetHistory(xValue));
							string[] tags = maxentTagger.dict.GetTags(word);
							ICollection<string> s = Generics.NewHashSet(Arrays.AsList(maxentTagger.tags.DeterministicallyExpandTags(tags)));
							System.Console.Error.Printf("possible tags for %s: %s\n", word, Arrays.ToString(Sharpen.Collections.ToArray(s)));
							if (!s.Contains(fK.tag))
							{
								continue;
							}
						}
						numEvidence += this.px[xValue];
					}
					if (Populated(numF, numEvidence))
					{
						int[] positions = tFeature.GetPositions(fK);
						if (maxentTagger.occurringTagsOnly || maxentTagger.possibleTagsOnly)
						{
							// TODO
							positions = null;
						}
						if (positions == null)
						{
							// write this in the file and create a TaggerFeature for it
							//int numElem
							int numElements = 0;
							foreach (int x in xValues)
							{
								if (maxentTagger.occurringTagsOnly)
								{
									//check whether the current word in x has occurred with y
									string word = ExtractorFrames.cWord.Extract(tHistories.GetHistory(x));
									if (maxentTagger.dict.GetCount(word, fK.tag) == 0)
									{
										continue;
									}
								}
								if (maxentTagger.possibleTagsOnly)
								{
									string word = ExtractorFrames.cWord.Extract(tHistories.GetHistory(x));
									string[] tags = maxentTagger.dict.GetTags(word);
									ICollection<string> s = Generics.NewHashSet(Arrays.AsList(maxentTagger.tags.DeterministicallyExpandTags(tags)));
									if (!s.Contains(fK.tag))
									{
										continue;
									}
								}
								numElements++;
								hF.WriteInt(x);
								fnumArr[x][y]++;
							}
							TaggerFeature tF = new TaggerFeature(current, current + numElements - 1, fK, maxentTagger.GetTagIndex(fK.tag), this);
							tFeature.AddPositions(current, current + numElements - 1, fK);
							current = current + numElements;
							feats.Add(tF);
						}
						else
						{
							foreach (int x in xValues)
							{
								fnumArr[x][y]++;
							}
							// this is the second time to write these values
							TaggerFeature tF = new TaggerFeature(positions[0], positions[1], fK, maxentTagger.GetTagIndex(fK.tag), this);
							feats.Add(tF);
						}
						// TODO: rearrange some of this code, such as not needing to
						// look up the tag # in the index
						if (maxentTagger.fAssociations.Count <= fK.num)
						{
							for (int i = maxentTagger.fAssociations.Count; i <= fK.num; ++i)
							{
								maxentTagger.fAssociations.Add(Generics.NewHashMap<string, int[]>());
							}
						}
						IDictionary<string, int[]> fValueAssociations = maxentTagger.fAssociations[fK.num];
						int[] fTagAssociations = fValueAssociations[fK.val];
						if (fTagAssociations == null)
						{
							fTagAssociations = new int[ySize];
							for (int i = 0; i < ySize; ++i)
							{
								fTagAssociations[i] = -1;
							}
							fValueAssociations[fK.val] = fTagAssociations;
						}
						fTagAssociations[maxentTagger.tags.GetIndex(fK.tag)] = numFeats;
						numFeats++;
					}
				}
				// foreach FeatureKey fK
				// read out the file and put everything in an array of ints stored in Feats
				tFeature.Release();
				feats.xIndexed = new int[current];
				hF.Seek(0);
				int current1 = 0;
				while (current1 < current)
				{
					feats.xIndexed[current1] = hF.ReadInt();
					current1++;
				}
				log.Info("  total feats: " + sTemplates.Count + ", populated: " + numFeats);
				hF.Close();
				hFile.Delete();
				// what is the maximum number of active features per pair
				int max = 0;
				int maxGt = 0;
				int numZeros = 0;
				for (int x_1 = 0; x_1 < xSize; x_1++)
				{
					int numGt = 0;
					for (int y = 0; y < ySize; y++)
					{
						if (fnumArr[x_1][y] > 0)
						{
							numGt++;
							if (max < fnumArr[x_1][y])
							{
								max = fnumArr[x_1][y];
							}
						}
						else
						{
							// if 00
							numZeros++;
						}
					}
					if (maxGt < numGt)
					{
						maxGt = numGt;
					}
				}
				// for x
				log.Info("  Max features per x,y pair: " + max);
				log.Info("  Max non-zero y values for an x: " + maxGt);
				log.Info("  Number of non-zero feature x,y pairs: " + (xSize * ySize - numZeros));
				log.Info("  Number of zero feature x,y pairs: " + numZeros);
				log.Info("end getFeaturesNew.");
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void HashHistories()
		{
			int fAll = maxentTagger.extractors.Size() + maxentTagger.extractorsRare.Size();
			int fGeneral = maxentTagger.extractors.Size();
			log.Info("Hashing histories ...");
			for (int x = 0; x < xSize; x++)
			{
				History h = tHistories.GetHistory(x);
				// It's the 2010s now and it doesn't take so long to featurize....
				// if (x > 0 && x % 10000 == 0) {
				//   System.err.printf("%d ",x);
				//   if (x % 100000 == 0) { log.info(); }
				// }
				int fSize = (maxentTagger.IsRare(ExtractorFrames.cWord.Extract(h)) ? fAll : fGeneral);
				for (int i = 0; i < fSize; i++)
				{
					tFeature.AddPrev(i, h);
				}
			}
			// for x
			// now for the populated ones
			// log.info();
			log.Info("Hashed " + xSize + " histories.");
			log.Info("Hashing populated histories ...");
			for (int x_1 = 0; x_1 < xSize; x_1++)
			{
				History h = tHistories.GetHistory(x_1);
				// It's the 2010s now and it doesn't take so long to featurize....
				// if (x > 0 && x % 10000 == 0) {
				//   log.info(x + " ");
				//   if (x % 100000 == 0) { log.info(); }
				// }
				int fSize = (maxentTagger.IsRare(ExtractorFrames.cWord.Extract(h)) ? fAll : fGeneral);
				for (int i = 0; i < fSize; i++)
				{
					tFeature.Add(i, h, x_1);
				}
			}
			// write this to check whether to add
			// for x
			// log.info();
			log.Info("Hashed populated histories.");
		}

		protected internal virtual bool Populated(int fNo, int size)
		{
			return IsPopulated(fNo, size, maxentTagger);
		}

		protected internal static bool IsPopulated(int fNo, int size, MaxentTagger maxentTagger)
		{
			// Feature number 0 is hard-coded as the current word feature, which has a special threshold
			if (fNo == 0)
			{
				return (size > maxentTagger.curWordMinFeatureThresh);
			}
			else
			{
				if (fNo < maxentTagger.extractors.Size())
				{
					return (size > maxentTagger.minFeatureThresh);
				}
				else
				{
					return (size > maxentTagger.rareWordMinFeatureThresh);
				}
			}
		}

		private void InitTemplatesNew()
		{
			maxentTagger.dict.SetAmbClasses(maxentTagger.ambClasses, maxentTagger.veryCommonWordThresh, maxentTagger.tags);
		}

		// Add a new feature key in a hashtable of feature templates
		private void AddTemplatesNew(History h, string tag)
		{
			// Feature templates general
			for (int i = 0; i < numFeatsGeneral; i++)
			{
				string s = maxentTagger.extractors.Extract(i, h);
				if (s.Equals(zeroSt))
				{
					continue;
				}
				//do not add the feature
				//iterate over tags in dictionary
				//only this tag
				FeatureKey key = new FeatureKey(i, s, tag);
				if (!maxentTagger.extractors.Get(i).Precondition(tag))
				{
					continue;
				}
				Add(key);
			}
		}

		private void AddRareTemplatesNew(History h, string tag)
		{
			// Feature templates rare
			if (!(maxentTagger.IsRare(ExtractorFrames.cWord.Extract(h))))
			{
				return;
			}
			int start = numFeatsGeneral;
			for (int i = start; i < numFeatsAll; i++)
			{
				string s = maxentTagger.extractorsRare.Extract(i - start, h);
				if (s.Equals(zeroSt))
				{
					continue;
				}
				//do not add the feature
				//only this tag
				FeatureKey key = new FeatureKey(i, s, tag);
				if (!maxentTagger.extractorsRare.Get(i - start).Precondition(tag))
				{
					continue;
				}
				Add(key);
			}
		}

		internal virtual HistoryTable GetHistoryTable()
		{
			return tHistories;
		}
		/*
		public String getY(int index) {
		return maxentTagger.tags.getTag(vArray[index][1]);
		}
		*/
		/*
		public static void main(String[] args) {
		int[] hPos = {0, 1, 2, -1, -2};
		boolean[] isTag = {false, false, false, true, true};
		maxentTagger.init();
		TaggerExperiments gophers = new TaggerExperiments("trainhuge.txt", null);
		//gophers.ptilde();
		}
		*/
	}
}
