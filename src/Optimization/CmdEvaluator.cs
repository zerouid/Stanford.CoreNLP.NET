using System;
using System.IO;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Runs a cmdline to evaluate a dataset (assumes cmd takes input from stdin)</summary>
	/// <author>Angel Chang</author>
	public abstract class CmdEvaluator : IEvaluator
	{
		private static readonly Pattern cmdSplitPattern = Pattern.Compile("\\s+");

		protected internal bool saveOutput = false;

		private string outString;

		private string errString;

		protected internal string description;

		//import edu.stanford.nlp.util.StreamGobbler;
		public abstract void SetValues(double[] x);

		public abstract string[] GetCmd();

		public abstract void OutputToCmd(OutputStream outputStream);

		protected internal static string[] GetCmd(string cmdStr)
		{
			if (cmdStr == null)
			{
				return null;
			}
			return cmdSplitPattern.Split(cmdStr);
		}

		public virtual string GetOutput()
		{
			return outString;
		}

		public virtual string GetError()
		{
			return errString;
		}

		public override string ToString()
		{
			return description;
		}

		public virtual void EvaluateCmd(string[] cmd)
		{
			try
			{
				SystemUtils.ProcessOutputStream outputStream;
				StringWriter outSw = null;
				StringWriter errSw = null;
				if (saveOutput)
				{
					outSw = new StringWriter();
					errSw = new StringWriter();
					outputStream = new SystemUtils.ProcessOutputStream(cmd, outSw, errSw);
				}
				else
				{
					outputStream = new SystemUtils.ProcessOutputStream(cmd, new PrintWriter(System.Console.Error));
				}
				OutputToCmd(outputStream);
				outputStream.Close();
				if (saveOutput)
				{
					outString = outSw.ToString();
					errString = errSw.ToString();
				}
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		public virtual double Evaluate(double[] x)
		{
			SetValues(x);
			EvaluateCmd(GetCmd());
			return 0;
		}
	}
}
