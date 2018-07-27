using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Truecaser
{
	/// <summary>
	/// This utility takes the tokens in a data file and picks the most
	/// common casing of words.
	/// </summary>
	/// <remarks>
	/// This utility takes the tokens in a data file and picks the most
	/// common casing of words.  It then outputs the most common case for
	/// each word.
	/// </remarks>
	/// <author>Michel Galley</author>
	public class MixDisambiguation
	{
		private static IDictionary<string, ICounter<string>> map = Generics.NewHashMap();

		private static IDictionary<string, string> highest = Generics.NewHashMap();

		private MixDisambiguation()
		{
		}

		// static class
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			bool outputLowercase = true;
			foreach (string arg in args)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(arg, "-noLowercase"))
				{
					outputLowercase = false;
					continue;
				}
				// everything else is considered a filename
				BufferedReader @in = new BufferedReader(new FileReader(arg));
				for (string line; (line = @in.ReadLine()) != null; )
				{
					string[] toks = line.Split(" ");
					foreach (string tok in toks)
					{
						string lctok = tok.ToLower();
						ICounter<string> counter = map[lctok];
						if (counter == null)
						{
							counter = new ClassicCounter<string>();
							map[lctok] = counter;
						}
						counter.IncrementCount(tok);
					}
				}
			}
			foreach (string k in map.Keys)
			{
				ICounter<string> counter = map[k];
				string maxstr = string.Empty;
				int maxcount = -1;
				foreach (string str in counter.KeySet())
				{
					int count = (int)counter.GetCount(str);
					if (count > maxcount)
					{
						maxstr = str;
						maxcount = count;
					}
				}
				highest[k] = maxstr;
			}
			foreach (string k_1 in highest.Keys)
			{
				string cased = highest[k_1];
				if (!outputLowercase && k_1.Equals(cased))
				{
					continue;
				}
				System.Console.Out.Printf("%s\t%s\n", k_1, cased);
			}
		}
	}
}
