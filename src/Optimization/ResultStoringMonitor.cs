using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Galen Andrew</author>
	public class ResultStoringMonitor : Func
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.ResultStoringMonitor));

		internal int i = 0;

		internal readonly int outputFreq;

		internal readonly string filename;

		public ResultStoringMonitor(int outputFreq, string filename)
		{
			if (filename.LastIndexOf('.') >= 0)
			{
				this.filename = Sharpen.Runtime.Substring(filename, 0, filename.LastIndexOf('.')) + ".ddat";
			}
			else
			{
				this.filename = filename + ".ddat";
			}
			this.outputFreq = outputFreq;
		}

		public virtual double ValueAt(double[] x)
		{
			if (++i % outputFreq == 0)
			{
				log.Info("Storing interim (double) weights to " + filename + " ... ");
				try
				{
					DataOutputStream dos = new DataOutputStream(new BufferedOutputStream(new GZIPOutputStream(new FileOutputStream(filename))));
					ConvertByteArray.SaveDoubleArr(dos, x);
					dos.Close();
				}
				catch (IOException)
				{
					log.Error("!");
					return 1;
				}
				log.Info("DONE.");
			}
			return 0;
		}

		public virtual int DomainDimension()
		{
			return 0;
		}
	}
}
