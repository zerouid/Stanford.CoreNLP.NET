using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>Affixation information.</summary>
	/// <author>Huihsin Tseng</author>
	/// <author>Pichuan Chang</author>
	public class AffixDictionary
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.AffixDictionary));

		public ICollection<string> ins;

		public AffixDictionary(string affixFilename)
		{
			//public Set ctbIns, asbcIns, hkIns, pkIns, msrIns;
			ins = ReadDict(affixFilename);
		}

		private ICollection<string> GetInDict()
		{
			return ins;
		}

		private static ICollection<string> ReadDict(string filename)
		{
			ICollection<string> a = Generics.NewHashSet();
			try
			{
				/*
				if(filename.endsWith("in.as") ||filename.endsWith("in.city") ){
				aDetectorReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "Big5_HKSCS"));
				}else{ aDetectorReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "GB18030"));
				}
				*/
				BufferedReader aDetectorReader = IOUtils.ReaderFromString(filename, "UTF-8");
				//logger.debug("DEBUG: in affDict readDict");
				for (string aDetectorLine; (aDetectorLine = aDetectorReader.ReadLine()) != null; )
				{
					//logger.debug("DEBUG: affDict: "+filename+" "+aDetectorLine);
					a.Add(aDetectorLine);
				}
				aDetectorReader.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			//logger.info("XM:::readDict(filename: " + filename + ")");
			logger.Info("Loading affix dictionary from " + filename + " [done].");
			return a;
		}

		public virtual string GetInDict(string a1)
		{
			if (GetInDict().Contains(a1))
			{
				return "1";
			}
			return "0";
		}
		//end of class
	}
}
