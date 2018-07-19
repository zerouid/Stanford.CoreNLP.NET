using System;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify.Demo
{
	/// <summary>
	/// This class converts the MNIST data set from Yann LeCun's distributed binary
	/// form to the tab-separated column format of ColumnDataClassifier.
	/// </summary>
	/// <remarks>
	/// This class converts the MNIST data set from Yann LeCun's distributed binary
	/// form to the tab-separated column format of ColumnDataClassifier.
	/// The converted files are huge (100MB of train data) compared to the compact original format.
	/// Site for data: http://yann.lecun.com/exdb/mnist/ .
	/// Commands:
	/// java edu.stanford.nlp.classify.demo.MnistConverter train-images-idx3-ubyte.gz train-labels-idx1-ubyte.gz MNIST-train.tsv MNIST.prop
	/// java edu.stanford.nlp.classify.demo.MnistConverter t10k-images-idx3-ubyte.gz  t10k-labels-idx1-ubyte.gz MNIST-test.tsv /dev/null
	/// java -Xrunhprof:cpu=samples,depth=12,interval=2,file=hprof.txt edu.stanford.nlp.classify.ColumnDataClassifier -prop MNIST.prop -trainFile MNIST-train.tsv -testFile MNIST-test.tsv
	/// ...
	/// Accuracy/micro-averaged F1: 0.92140
	/// Macro-averaged F1: 0.92025
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class MnistConverter
	{
		internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.Demo.MnistConverter));

		private MnistConverter()
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length != 4)
			{
				logger.Info("Usage: MnistConverter dataFile labelFile outFile propsFile");
				return;
			}
			DataInputStream xStream = IOUtils.GetDataInputStream(args[0]);
			DataInputStream yStream = IOUtils.GetDataInputStream(args[1]);
			PrintWriter oStream = new PrintWriter(new FileWriter(args[2]));
			PrintWriter pStream = new PrintWriter(new FileWriter(args[3]));
			int xMagic = xStream.ReadInt();
			if (xMagic != 2051)
			{
				throw new Exception("Bad format of xStream");
			}
			int yMagic = yStream.ReadInt();
			if (yMagic != 2049)
			{
				throw new Exception("Bad format of yStream");
			}
			int xNumImages = xStream.ReadInt();
			int yNumLabels = yStream.ReadInt();
			if (xNumImages != yNumLabels)
			{
				throw new Exception("x and y sizes don't match");
			}
			logger.Info("Images and label file both contain " + xNumImages + " entries.");
			int xRows = xStream.ReadInt();
			int xColumns = xStream.ReadInt();
			for (int i = 0; i < xNumImages; i++)
			{
				int label = yStream.ReadUnsignedByte();
				int[] matrix = new int[xRows * xColumns];
				for (int j = 0; j < xRows * xColumns; j++)
				{
					matrix[j] = xStream.ReadUnsignedByte();
				}
				oStream.Print(label);
				foreach (int k in matrix)
				{
					oStream.Print('\t');
					oStream.Print(k);
				}
				oStream.Println();
			}
			logger.Info("Converted.");
			xStream.Close();
			yStream.Close();
			oStream.Close();
			// number from 1; column 0 is the class
			pStream.Println("goldAnswerColumn = 0");
			pStream.Println("useClassFeature = true");
			pStream.Println("sigma = 10");
			// not optimized, but weak regularization seems appropriate when much data, few features
			for (int j_1 = 0; j_1 < xRows * xColumns; j_1++)
			{
				pStream.Println((j_1 + 1) + ".realValued = true");
			}
			pStream.Close();
		}
	}
}
