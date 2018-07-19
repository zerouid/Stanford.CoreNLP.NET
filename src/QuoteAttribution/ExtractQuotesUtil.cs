using System;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by michaelf on 5/30/16.</summary>
	public class ExtractQuotesUtil
	{
		//return true if one is contained in the other.
		public static bool RangeContains(Pair<int, int> r1, Pair<int, int> r2)
		{
			return ((r1.first <= r2.first && r1.second >= r2.first) || (r1.first <= r2.second && r1.second >= r2.second));
		}

		public static Annotation ReadSerializedProtobufFile(File fileIn)
		{
			Annotation annotation;
			try
			{
				ProtobufAnnotationSerializer pas = new ProtobufAnnotationSerializer();
				InputStream @is = new BufferedInputStream(new FileInputStream(fileIn));
				Pair<Annotation, InputStream> pair = pas.Read(@is);
				pair.second.Close();
				annotation = pair.first;
				IOUtils.CloseIgnoringExceptions(@is);
				return annotation;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}
	}
}
