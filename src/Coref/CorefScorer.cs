using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>Utilities for running coref evaluation scripts and printing the results</summary>
	/// <author>Heeyoung Lee</author>
	/// <author>Kevin Clark</author>
	public class CorefScorer
	{
		/// <exception cref="System.IO.IOException"/>
		public static string GetEvalSummary(string evalScript, string goldFile, string predictFile)
		{
			ProcessBuilder process = new ProcessBuilder(evalScript, "all", goldFile, predictFile, "none");
			StringOutputStream errSos = new StringOutputStream();
			StringOutputStream outSos = new StringOutputStream();
			PrintWriter @out = new PrintWriter(outSos);
			PrintWriter err = new PrintWriter(errSos);
			SystemUtils.Run(process, @out, err);
			@out.Close();
			err.Close();
			string summary = outSos.ToString();
			string errStr = errSos.ToString();
			if (!errStr.IsEmpty())
			{
				summary += "\nERROR: " + errStr;
			}
			Pattern pattern = Pattern.Compile("\\d+\\.\\d\\d\\d+");
			DecimalFormat df = new DecimalFormat("#.##");
			Matcher matcher = pattern.Matcher(summary);
			while (matcher.Find())
			{
				string number = matcher.Group();
				summary = summary.ReplaceFirst(number, df.Format(double.Parse(number)));
			}
			return summary;
		}

		public static void PrintScoreSummary(string summary, Logger logger, bool afterPostProcessing)
		{
			string[] lines = summary.Split("\n");
			if (!afterPostProcessing)
			{
				foreach (string line in lines)
				{
					if (line.StartsWith("Identification of Mentions"))
					{
						Redwood.Log(line);
						return;
					}
				}
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				foreach (string line in lines)
				{
					if (line.StartsWith("METRIC"))
					{
						sb.Append(line);
					}
					if (!line.StartsWith("Identification of Mentions") && line.Contains("Recall"))
					{
						sb.Append(line).Append("\n");
					}
				}
				Redwood.Log(sb.ToString());
			}
		}

		public static double GetFinalConllScore(string summary)
		{
			Pattern f1 = Pattern.Compile("Coreference:.*F1: (.*)%");
			Matcher f1Matcher = f1.Matcher(summary);
			double[] F1s = new double[5];
			int i = 0;
			while (f1Matcher.Find())
			{
				F1s[i++] = double.Parse(f1Matcher.Group(1));
			}
			double finalScore = (F1s[0] + F1s[1] + F1s[3]) / 3;
			return finalScore;
		}

		public static void PrintFinalConllScore(string summary)
		{
			double finalScore = GetFinalConllScore(summary);
			Redwood.Log("Final conll score ((muc+bcub+ceafe)/3) = " + (new DecimalFormat("#.##")).Format(finalScore));
		}

		public static double GetFinalConllScoreFromOutputDir(string corefOutputDir, string scorerPath)
		{
			File baseFolder = new File(corefOutputDir);
			File[] filesInBaseFolder = baseFolder.ListFiles();
			string baseName = corefOutputDir;
			foreach (File outputFile in filesInBaseFolder)
			{
				string outputFileName = outputFile.GetName();
				baseName = baseName + "/" + outputFileName.Split("\\.")[0];
				break;
			}
			string goldOutput = baseName + ".gold.txt";
			string afterCorefOutput = baseName + ".coref.predicted.txt";
			try
			{
				string summary = CorefScorer.GetEvalSummary(scorerPath, goldOutput, afterCorefOutput);
				double finalScore = GetFinalConllScore(summary);
				return finalScore;
			}
			catch (IOException)
			{
				Redwood.Log("Error: failed to get coref score from directory");
				return -1;
			}
		}
	}
}
