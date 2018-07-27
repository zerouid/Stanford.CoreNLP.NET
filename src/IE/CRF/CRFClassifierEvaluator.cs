using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Evaluates a CRFClassifier on a set of data.</summary>
	/// <remarks>
	/// Evaluates a CRFClassifier on a set of data.
	/// This can be called by QNMinimizer periodically.
	/// If evalCmd is set, it runs the command line specified by evalCmd,
	/// otherwise it does evaluation internally.
	/// NOTE: when running conlleval with exec on Linux, linux will first
	/// fork process by duplicating memory of current process.  So if the
	/// JVM has lots of memory, it will all be duplicated when
	/// child process is initially forked, which can be unfortunate.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class CRFClassifierEvaluator<In> : CmdEvaluator
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierEvaluator));

		private readonly CRFClassifier<In> classifier;

		/// <summary>NOTE: Default uses -r, specify without -r if IOB.</summary>
		private string cmdStr = "/u/nlp/bin/conlleval -r";

		private string[] cmd;

		internal ICollection<IList<In>> data;

		internal IList<Triple<int[][][], int[], double[][][]>> featurizedData;

		public CRFClassifierEvaluator(string description, CRFClassifier<In> classifier, ICollection<IList<In>> data, IList<Triple<int[][][], int[], double[][][]>> featurizedData)
		{
			// TODO: Use data structure to hold data + features
			// Cache already featurized documents
			// Original object bank
			// Featurized data
			this.description = description;
			this.classifier = classifier;
			this.data = data;
			this.featurizedData = featurizedData;
			cmd = GetCmd(cmdStr);
			saveOutput = true;
		}

		public CRFClassifierEvaluator(string description, CRFClassifier<In> classifier)
		{
			this.description = description;
			this.classifier = classifier;
			saveOutput = true;
		}

		/// <summary>Set the data to test on</summary>
		public virtual void SetTestData(ICollection<IList<In>> data, IList<Triple<int[][][], int[], double[][][]>> featurizedData)
		{
			this.data = data;
			this.featurizedData = featurizedData;
		}

		/// <summary>Set the evaluation command (set to null to skip evaluation using command line)</summary>
		/// <param name="evalCmd"/>
		public virtual void SetEvalCmd(string evalCmd)
		{
			log.Info("setEvalCmd to " + evalCmd);
			this.cmdStr = evalCmd;
			if (cmdStr != null)
			{
				cmdStr = cmdStr.Trim();
				if (cmdStr.IsEmpty())
				{
					cmdStr = null;
				}
			}
			cmd = GetCmd(cmdStr);
		}

		public override void SetValues(double[] x)
		{
			classifier.UpdateWeightsForTest(x);
		}

		public override string[] GetCmd()
		{
			return cmd;
		}

		private double InterpretCmdOutput()
		{
			string output = GetOutput();
			string[] parts = output.Split("\\s+");
			int fScoreIndex = 0;
			for (; fScoreIndex < parts.Length; fScoreIndex++)
			{
				if (parts[fScoreIndex].Equals("FB1:"))
				{
					break;
				}
			}
			fScoreIndex += 1;
			if (fScoreIndex < parts.Length)
			{
				return double.ParseDouble(parts[fScoreIndex]);
			}
			else
			{
				log.Error("in CRFClassifierEvaluator.interpretCmdOutput(), cannot find FB1 score in output:\n" + output);
				return -1;
			}
		}

		public override void OutputToCmd(OutputStream outputStream)
		{
			try
			{
				PrintWriter pw = IOUtils.EncodedOutputStreamPrintWriter(outputStream, null, true);
				classifier.ClassifyAndWriteAnswers(data, featurizedData, pw, classifier.MakeReaderAndWriter());
			}
			catch (IOException ex)
			{
				throw new RuntimeIOException(ex);
			}
		}

		public override double Evaluate(double[] x)
		{
			double score;
			// initialized below
			SetValues(x);
			if (GetCmd() != null)
			{
				EvaluateCmd(GetCmd());
				score = InterpretCmdOutput();
			}
			else
			{
				try
				{
					// TODO: Classify in memory instead of writing to tmp file
					File f = File.CreateTempFile("CRFClassifierEvaluator", "txt");
					f.DeleteOnExit();
					OutputStream outputStream = new BufferedOutputStream(new FileOutputStream(f));
					PrintWriter pw = IOUtils.EncodedOutputStreamPrintWriter(outputStream, null, true);
					classifier.ClassifyAndWriteAnswers(data, featurizedData, pw, classifier.MakeReaderAndWriter());
					outputStream.Close();
					BufferedReader br = new BufferedReader(new FileReader(f));
					MultiClassChunkEvalStats stats = new MultiClassChunkEvalStats("O");
					score = stats.Score(br, "\t");
					log.Info(stats.GetConllEvalString());
					f.Delete();
				}
				catch (Exception ex)
				{
					throw new Exception(ex);
				}
			}
			return score;
		}
	}
}
