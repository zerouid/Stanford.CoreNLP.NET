using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Applies the same orthographic transformations developed for ATB parse trees to flat
	/// MT input.
	/// </summary>
	/// <remarks>
	/// Applies the same orthographic transformations developed for ATB parse trees to flat
	/// MT input. This data set escapes IBM Arabic (for example, it removes explicit clitic markings).
	/// <p>
	/// NOTE: This class expects UTF-8 input (not Buckwalter)
	/// </remarks>
	/// <author>Spence Green</author>
	public class IBMMTArabicDataset : IDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.IBMMTArabicDataset));

		protected internal IMapper lexMapper = null;

		protected internal readonly IList<File> pathsToData;

		protected internal string outFileName;

		protected internal readonly Pattern fileNameNormalizer = Pattern.Compile("\\s+");

		protected internal readonly IBMArabicEscaper escaper;

		private static readonly Pattern utf8ArabicChart = Pattern.Compile("[\u0600-\u06FF]");

		protected internal readonly ICollection<string> configuredOptions;

		protected internal readonly ICollection<string> requiredOptions;

		protected internal readonly StringBuilder toStringBuffer;

		public IBMMTArabicDataset()
		{
			configuredOptions = Generics.NewHashSet();
			toStringBuffer = new StringBuilder();
			pathsToData = new List<File>();
			escaper = new IBMArabicEscaper(true);
			escaper.DisableWarnings();
			requiredOptions = Generics.NewHashSet();
			requiredOptions.Add(ConfigParser.paramName);
			requiredOptions.Add(ConfigParser.paramPath);
		}

		public virtual void Build()
		{
			LineNumberReader infile = null;
			PrintWriter outfile = null;
			string currentInfile = string.Empty;
			try
			{
				outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFileName), "UTF-8")));
				foreach (File path in pathsToData)
				{
					infile = new LineNumberReader(new BufferedReader(new InputStreamReader(new FileInputStream(path), "UTF-8")));
					currentInfile = path.GetPath();
					while (infile.Ready())
					{
						List<Word> sent = SentenceUtils.ToUntaggedList(infile.ReadLine().Split("\\s+"));
						foreach (Word token in sent)
						{
							Matcher hasArabic = utf8ArabicChart.Matcher(token.Word());
							if (hasArabic.Find())
							{
								token.SetWord(escaper.Apply(token.Word()));
								token.SetWord(lexMapper.Map(null, token.Word()));
							}
						}
						outfile.Println(SentenceUtils.ListToString(sent));
					}
					toStringBuffer.Append(string.Format(" Read %d input lines from %s", infile.GetLineNumber(), path.GetPath()));
				}
				infile.Close();
			}
			catch (UnsupportedEncodingException e)
			{
				System.Console.Error.Printf("%s: Filesystem does not support UTF-8 output\n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open %s for writing\n", this.GetType().FullName, outFileName);
			}
			catch (IOException)
			{
				System.Console.Error.Printf("%s: Error reading from %s (line %d)\n", this.GetType().FullName, currentInfile, infile.GetLineNumber());
			}
			catch (Exception e)
			{
				System.Console.Error.Printf("%s: Input sentence from %s contains token mapped to null (line %d)\n", this.GetType().FullName, currentInfile, infile.GetLineNumber());
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				if (outfile != null)
				{
					outfile.Close();
				}
			}
		}

		public virtual IList<string> GetFilenames()
		{
			IList<string> l = new List<string>();
			l.Add(outFileName);
			return l;
		}

		public override string ToString()
		{
			return toStringBuffer.ToString();
		}

		public virtual bool SetOptions(Properties opts)
		{
			foreach (string opt in opts.StringPropertyNames())
			{
				string value = opts.GetProperty(opt);
				if (value == null)
				{
					System.Console.Error.Printf("%s: Read parameter with null value (%s)\n", this.GetType().FullName, opt);
					continue;
				}
				configuredOptions.Add(opt);
				Matcher pathMatcher = ConfigParser.matchPath.Matcher(opt);
				if (pathMatcher.LookingAt())
				{
					pathsToData.Add(new File(value));
					configuredOptions.Add(ConfigParser.paramPath);
				}
				else
				{
					if (opt.Equals(ConfigParser.paramName))
					{
						Matcher inThisFilename = fileNameNormalizer.Matcher(value.Trim());
						outFileName = inThisFilename.ReplaceAll("-");
						toStringBuffer.Append(string.Format("Dataset Name: %s\n", value.Trim()));
					}
				}
			}
			if (!configuredOptions.ContainsAll(requiredOptions))
			{
				return false;
			}
			//Finalize the output file names
			outFileName += ".txt";
			//Used for codifying lexical hacks
			lexMapper = new DefaultLexicalMapper();
			return true;
		}
	}
}
