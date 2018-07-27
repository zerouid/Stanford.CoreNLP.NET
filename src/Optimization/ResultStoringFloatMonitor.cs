using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Galen Andrew</author>
	public class ResultStoringFloatMonitor : IFloatFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.ResultStoringFloatMonitor));

		internal int i = 0;

		internal readonly int outputFreq;

		internal readonly string filename;

		public ResultStoringFloatMonitor(int outputFreq, string filename)
		{
			if (filename.LastIndexOf('.') >= 0)
			{
				this.filename = Sharpen.Runtime.Substring(filename, 0, filename.LastIndexOf('.')) + ".fdat";
			}
			else
			{
				this.filename = filename + ".fdat";
			}
			this.outputFreq = outputFreq;
		}

		public virtual float ValueAt(float[] x)
		{
			if (++i % outputFreq == 0)
			{
				log.Info("Storing interim (float) weights to " + filename + " ... ");
				try
				{
					DataOutputStream dos = new DataOutputStream(new BufferedOutputStream(new GZIPOutputStream(new FileOutputStream(filename))));
					ConvertByteArray.SaveFloatArr(dos, x);
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
