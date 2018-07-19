using System;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	public class CEDict
	{
		private const string defaultPath = "cedict_ts.u8";

		private const string defaultPath2 = "/u/nlp/data/chinese-english-dictionary/cedict_ts.u8";

		private const string EnvVariable = "CEDICT";

		public static string Path()
		{
			File f = new File(defaultPath);
			if (f.CanRead())
			{
				return defaultPath;
			}
			else
			{
				f = new File(defaultPath2);
				if (f.CanRead())
				{
					return defaultPath2;
				}
				else
				{
					string path = Runtime.Getenv(EnvVariable);
					f = new File(path);
					if (!f.CanRead())
					{
						throw new Exception("ChineseEnglishWordMap cannot find dictionary");
					}
					return path;
				}
			}
		}

		private CEDict()
		{
		}
		// static methods only
	}
}
