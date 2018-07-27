using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Dcoref;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Dcoref.Util
{
	/// <summary>
	/// This tool converts the gender file from the following:
	/// <br />
	/// w1 w2...
	/// </summary>
	/// <remarks>
	/// This tool converts the gender file from the following:
	/// <br />
	/// w1 w2... TAB male female neutral <br />
	/// etc <br />
	/// <br />
	/// into a serialized data structure which should take much less time to load.
	/// </remarks>
	/// <author>John Bauer</author>
	public class ConvertGenderFile
	{
		private ConvertGenderFile()
		{
		}

		// static class
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string input = null;
			string output = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
				{
					input = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
					{
						output = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						throw new ArgumentException("Unknown argument " + args[argIndex]);
					}
				}
			}
			if (input == null)
			{
				throw new ArgumentException("Must specify input with -input");
			}
			if (output == null)
			{
				throw new ArgumentException("Must specify output with -output");
			}
			IDictionary<IList<string>, Dictionaries.Gender> genderNumber = Generics.NewHashMap();
			BufferedReader reader = IOUtils.ReaderFromString(input);
			for (string line; (line = reader.ReadLine()) != null; )
			{
				string[] split = line.Split("\t");
				string[] countStr = split[1].Split(" ");
				int male = System.Convert.ToInt32(countStr[0]);
				int female = System.Convert.ToInt32(countStr[1]);
				int neutral = System.Convert.ToInt32(countStr[2]);
				Dictionaries.Gender gender = Dictionaries.Gender.Unknown;
				if (male * 0.5 > female + neutral && male > 2)
				{
					gender = Dictionaries.Gender.Male;
				}
				else
				{
					if (female * 0.5 > male + neutral && female > 2)
					{
						gender = Dictionaries.Gender.Female;
					}
					else
					{
						if (neutral * 0.5 > male + female && neutral > 2)
						{
							gender = Dictionaries.Gender.Neutral;
						}
					}
				}
				if (gender == Dictionaries.Gender.Unknown)
				{
					continue;
				}
				string[] words = split[0].Split(" ");
				IList<string> tokens = Arrays.AsList(words);
				genderNumber[tokens] = gender;
			}
			IOUtils.WriteObjectToFile(genderNumber, output);
		}
	}
}
