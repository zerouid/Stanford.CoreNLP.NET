using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Spanish;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Scripts
{
	/// <summary>This script converts a PTB tree into TSV suitable for NER classification.</summary>
	/// <remarks>
	/// This script converts a PTB tree into TSV suitable for NER classification. The
	/// input is an AnCora treebank file with NER tags, and the output is a TSV file
	/// with tab-seperated word-class pairs, one word per file. These can be used with
	/// the CRFClassifier for training or testing.
	/// </remarks>
	public class TreeToTSV
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TreeToTSV));

		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				System.Console.Error.Printf("Usage: java %s tree_file%n", typeof(TreeToTSV).FullName);
				System.Environment.Exit(-1);
			}
			string treeFile = args[0];
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new SpanishTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				StringBuilder sb = new StringBuilder();
				string nl = Runtime.GetProperty("line.separator");
				Pattern nePattern = Pattern.Compile("^grup\\.nom\\.");
				Pattern npPattern = Pattern.Compile("^np0000.$");
				for (Tree tree; (tree = tr.ReadTree()) != null; )
				{
					foreach (Tree t in tree)
					{
						if (!t.IsPreTerminal())
						{
							continue;
						}
						char type = 'O';
						Tree grandma = t.Ancestor(1, tree);
						string grandmaValue = ((CoreLabel)grandma.Label()).Value();
						// grup.nom.x
						if (nePattern.Matcher(grandmaValue).Find())
						{
							type = grandmaValue[9];
						}
						else
						{
							// else check the pos for np0000x or not
							string pos = ((CoreLabel)t.Label()).Value();
							if (npPattern.Matcher(pos).Find())
							{
								type = pos[6];
							}
						}
						Tree wordNode = t.FirstChild();
						string word = ((CoreLabel)wordNode.Label()).Value();
						sb.Append(word).Append("\t");
						switch (type)
						{
							case 'p':
							{
								sb.Append("PERS");
								break;
							}

							case 'l':
							{
								sb.Append("LUG");
								break;
							}

							case 'o':
							{
								sb.Append("ORG");
								break;
							}

							case '0':
							{
								sb.Append("OTROS");
								break;
							}

							default:
							{
								sb.Append("O");
								break;
							}
						}
						sb.Append(nl);
					}
					sb.Append(nl);
				}
				System.Console.Out.Write(sb.ToString());
				tr.Close();
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
