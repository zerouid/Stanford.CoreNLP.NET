using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A class to read some UCI datasets into RVFDatum.</summary>
	/// <remarks>A class to read some UCI datasets into RVFDatum. Will incrementally add formats.</remarks>
	/// <author>
	/// Kristina Toutanova
	/// Sep 14, 2004
	/// Made type-safe by Sarah Spikes (sdspikes@cs.stanford.edu)
	/// </author>
	public class NominalDataReader
	{
		internal IDictionary<string, IIndex<string>> indices = Generics.NewHashMap();

		internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(NominalDataReader));

		// an Index for each feature so that its values are coded as integers
		/// <summary>the class is the last column and it skips the next-to-last column because it is a unique id in the audiology data</summary>
		internal static RVFDatum<string, int> ReadDatum(string line, string separator, IDictionary<int, IIndex<string>> indices)
		{
			StringTokenizer st = new StringTokenizer(line, separator);
			//int fno = 0;
			List<string> tokens = new List<string>();
			while (st.HasMoreTokens())
			{
				string token = st.NextToken();
				tokens.Add(token);
			}
			string[] arr = Sharpen.Collections.ToArray(tokens, new string[tokens.Count]);
			ICollection<int> skip = Generics.NewHashSet();
			skip.Add(int.Parse(arr.Length - 2));
			return ReadDatum(arr, arr.Length - 1, skip, indices);
		}

		internal static RVFDatum<string, int> ReadDatum(string[] values, int classColumn, ICollection<int> skip, IDictionary<int, IIndex<string>> indices)
		{
			ClassicCounter<int> c = new ClassicCounter<int>();
			RVFDatum<string, int> d = new RVFDatum<string, int>(c);
			int attrNo = 0;
			for (int index = 0; index < values.Length; index++)
			{
				if (index == classColumn)
				{
					d.SetLabel(values[index]);
					continue;
				}
				if (skip.Contains(int.Parse(index)))
				{
					continue;
				}
				int featKey = int.Parse(attrNo);
				IIndex<string> ind = indices[featKey];
				if (ind == null)
				{
					ind = new HashIndex<string>();
					indices[featKey] = ind;
				}
				// MG: condition on isLocked is useless, since add(E) contains such a condition:
				//if (!ind.isLocked()) {
				ind.Add(values[index]);
				//}
				int valInd = ind.IndexOf(values[index]);
				if (valInd == -1)
				{
					valInd = 0;
					logger.Info("unknown attribute value " + values[index] + " of attribute " + attrNo);
				}
				c.IncrementCount(featKey, valInd);
				attrNo++;
			}
			return d;
		}

		/// <summary>Read the data as a list of RVFDatum objects.</summary>
		/// <remarks>Read the data as a list of RVFDatum objects. For the test set we must reuse the indices from the training set</remarks>
		internal static List<RVFDatum<string, int>> ReadData(string filename, IDictionary<int, IIndex<string>> indices)
		{
			try
			{
				string sep = ", ";
				List<RVFDatum<string, int>> examples = new List<RVFDatum<string, int>>();
				foreach (string line in ObjectBank.GetLineIterator(new File(filename)))
				{
					RVFDatum<string, int> next = ReadDatum(line, sep, indices);
					examples.Add(next);
				}
				return examples;
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}
	}
}
