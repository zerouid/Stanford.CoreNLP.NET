using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Arabic.Pipeline;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// Compares the output of the JFlex-based ArabicTokenizer to DefaultLexicalMapper, which
	/// is used in the parser and elsewhere.
	/// </summary>
	/// <author>Spence Green</author>
	public class ArabicTokenizerTester
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ArabicTokenizerTester));

		/// <summary>
		/// arg[0] := tokenizer options
		/// args[1] := file to tokenize
		/// </summary>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				System.Console.Out.Printf("Usage: java %s OPTS filename%n", typeof(ArabicTokenizerTester).FullName);
				System.Environment.Exit(-1);
			}
			string tokOptions = args[0];
			File path = new File(args[1]);
			log.Info("Reading from: " + path.GetPath());
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(path), "UTF-8"));
				ITokenizerFactory<CoreLabel> tf = ArabicTokenizer.Factory();
				tf.SetOptions(tokOptions);
				IMapper lexMapper = new DefaultLexicalMapper();
				lexMapper.Setup(null, "StripSegMarkersInUTF8", "StripMorphMarkersInUTF8");
				int lineId = 0;
				for (string line; (line = br.ReadLine()) != null; lineId++)
				{
					line = line.Trim();
					// Tokenize with the tokenizer
					IList<CoreLabel> tokenizedLine = tf.GetTokenizer(new StringReader(line)).Tokenize();
					System.Console.Out.WriteLine(SentenceUtils.ListToString(tokenizedLine));
					// Tokenize with the mapper
					StringBuilder sb = new StringBuilder();
					string[] toks = line.Split("\\s+");
					foreach (string tok in toks)
					{
						string mappedTok = lexMapper.Map(null, tok);
						sb.Append(mappedTok).Append(" ");
					}
					IList<string> mappedToks = Arrays.AsList(sb.ToString().Trim().Split("\\s+"));
					// Evaluate the output
					if (mappedToks.Count != tokenizedLine.Count)
					{
						System.Console.Error.Printf("Line length mismatch:%norig: %s%ntok: %s%nmap: %s%n%n", line, SentenceUtils.ListToString(tokenizedLine), SentenceUtils.ListToString(mappedToks));
					}
					else
					{
						bool printLines = false;
						for (int i = 0; i < mappedToks.Count; ++i)
						{
							string mappedTok = mappedToks[i];
							string tokenizedTok = tokenizedLine[i].Word();
							if (!mappedTok.Equals(tokenizedTok))
							{
								System.Console.Error.Printf("Token mismatch:%nmap: %s%ntok: %s%n", mappedTok, tokenizedTok);
								printLines = true;
							}
						}
						if (printLines)
						{
							System.Console.Error.Printf("orig: %s%ntok: %s%nmap: %s%n%n", line, SentenceUtils.ListToString(tokenizedLine), SentenceUtils.ListToString(mappedToks));
						}
					}
				}
				System.Console.Error.Printf("Read %d lines.%n", lineId);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
