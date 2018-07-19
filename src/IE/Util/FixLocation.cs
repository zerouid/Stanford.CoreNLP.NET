using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Util
{
	/// <summary>
	/// A script that goes through a data file and looks for instances
	/// where place, place should have the , tagged as well.
	/// </summary>
	/// <author>jrfinkel</author>
	public class FixLocation
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Util.FixLocation));

		public static readonly BufferedReader @in = new BufferedReader(new InputStreamReader(Runtime.@in));

		internal static string inputFilename = null;

		internal static string outputFilename = null;

		private FixLocation()
		{
		}

		// static class
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				log.Info("Input filename?");
				inputFilename = @in.ReadLine();
			}
			else
			{
				inputFilename = args[0];
			}
			if (args.Length < 2)
			{
				log.Info("Output filename?");
				outputFilename = @in.ReadLine();
			}
			else
			{
				outputFilename = args[1];
			}
			string[][] cols = ReadFile(inputFilename);
			Fix(cols);
			Print(cols);
		}

		/// <exception cref="System.Exception"/>
		public static string[][] ReadFile(string filename)
		{
			string file = IOUtils.SlurpFile(filename);
			string[] lines = file.Split("\n");
			string[][] cols = new string[lines.Length][];
			for (int i = 0; i < lines.Length; i++)
			{
				cols[i] = lines[i].Split("\\s+");
			}
			return cols;
		}

		/// <exception cref="System.Exception"/>
		public static void Fix(string[][] cols)
		{
			for (int i = 1; i < cols.Length - 1; i++)
			{
				if (cols[i - 1].Length < 2)
				{
					continue;
				}
				if (cols[i].Length < 2)
				{
					continue;
				}
				if (cols[i + 1].Length < 2)
				{
					continue;
				}
				string prevLabel = cols[i - 1][1];
				string curWord = cols[i][0];
				string nextLabel = cols[i + 1][1];
				if (prevLabel.Equals("LOCATION") && nextLabel.Equals("LOCATION") && curWord.Equals(","))
				{
					Query(cols, i);
				}
			}
		}

		public static BufferedReader answers;

		static FixLocation()
		{
			try
			{
				answers = new BufferedReader(new FileReader("answers"));
			}
			catch (Exception)
			{
			}
		}

		private static IDictionary<string, string> cache = Generics.NewHashMap();

		/// <exception cref="System.Exception"/>
		public static void Query(string[][] cols, int pos)
		{
			string pre = string.Empty;
			if (cols[pos - 1][0].Matches("[-A-Z]*"))
			{
				cols[pos][1] = "LOCATION";
				return;
			}
			for (int i = pos - 1; i >= 0 && cols[i].Length >= 2; i--)
			{
				if (cols[i][1].Equals("LOCATION"))
				{
					if (pre.Equals(string.Empty))
					{
						pre = cols[i][0];
					}
					else
					{
						pre = cols[i][0] + " " + pre;
					}
				}
				else
				{
					break;
				}
			}
			string post = string.Empty;
			for (int i_1 = pos + 1; i_1 < cols.Length && cols[i_1].Length >= 2; i_1++)
			{
				if (cols[i_1][1].Equals("LOCATION"))
				{
					if (post.Equals(string.Empty))
					{
						post = cols[i_1][0];
					}
					else
					{
						post = post + " " + cols[i_1][0];
					}
				}
				else
				{
					break;
				}
			}
			string ans = (answers == null) ? string.Empty : answers.ReadLine();
			string loc = pre + "," + post + " ?";
			log.Info(loc);
			if (ans.Equals(loc))
			{
				string response = answers.ReadLine();
				log.Info(response);
				if (Sharpen.Runtime.EqualsIgnoreCase(ans, "Y"))
				{
					cols[pos][1] = "LOCATION";
				}
			}
			else
			{
				ans = cache[loc];
				if (ans == null)
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(@in.ReadLine(), "Y"))
					{
						cache[loc] = "Y";
						cols[pos][1] = "LOCATION";
					}
					else
					{
						cache[loc] = "N";
					}
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(ans, "Y"))
					{
						cols[pos][1] = "LOCATION";
						log.Info("Y");
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Print(string[][] cols)
		{
			BufferedWriter @out = new BufferedWriter(new FileWriter(outputFilename));
			foreach (string[] col in cols)
			{
				if (col.Length >= 2)
				{
					@out.Write(col[0] + "\t" + col[1] + "\n");
				}
				else
				{
					@out.Write("\n");
				}
			}
			@out.Flush();
			@out.Close();
		}
	}
}
