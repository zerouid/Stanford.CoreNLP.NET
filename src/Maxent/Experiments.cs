using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Maxent
{
	/// <summary>This class represents the training samples.</summary>
	/// <remarks>
	/// This class represents the training samples. It can return statistics of them,
	/// for example the frequency of each x or y.
	/// in the training data.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class Experiments
	{
		/// <summary>vArray has dimensions [numTrainingDatums][2] and holds the x and y (word and tag index) for each training sample.</summary>
		/// <remarks>
		/// vArray has dimensions [numTrainingDatums][2] and holds the x and y (word and tag index) for each training sample.
		/// Its length is the number of data points.
		/// </remarks>
		protected internal int[][] vArray;

		/// <summary>px[x] holds the number of times the history x appeared in training data</summary>
		protected internal int[] px;

		/// <summary>py[y] holds the number of times the outcome y appeared in training data</summary>
		protected internal int[] py;

		protected internal int[] maxY;

		/// <summary>pxy[x][y]=# times (x,y) occurred in training</summary>
		protected internal int[][] pxy;

		public int xSize;

		public int ySize;

		/// <summary>v may hold the actual Experiments, i.e.</summary>
		/// <remarks>v may hold the actual Experiments, i.e. Objects of type Experiments</remarks>
		private List<Edu.Stanford.Nlp.Maxent.Experiments> v = new List<Edu.Stanford.Nlp.Maxent.Experiments>();

		/// <summary>Maximum ySize.</summary>
		/// <remarks>
		/// Maximum ySize.
		/// todo [CDM May 2007]: What is this and what does it control?  Why isn't it set dynamically?
		/// Is it the number of different y values that one x value can have?
		/// If so, although it was set to 5, it should be 7 for the WSJ PTB.
		/// But that doesn't solve the problem for the data set after that....
		/// See the commented out bits where it should exception if it overflows.
		/// Should just be able to make it dynamic
		/// </remarks>
		internal int dim = 7;

		/// <summary>The value of classification y for x.</summary>
		/// <remarks>
		/// The value of classification y for x.
		/// Used for ranking.
		/// </remarks>
		public double[][] values;

		public Experiments()
		{
		}

		/// <summary>
		/// If this constructor is used, the maximum possible class overall is found and all classes are assumed possible
		/// for all instances.
		/// </summary>
		public Experiments(int[][] vArray)
		{
			// todo [cdm 2013]: It might be better to change this to an IntPair[]
			// 4MB, may be compress it
			// for each x, which is the maximum possible y
			// TODO(horatio): pxy, xSize, ySize, and dim used to be static.
			// Changing them to non-static member variables did not break the
			// POS tagger, at least.  A few other places that use this code at a
			// fairly low level are:
			//
			// periphery/src/edu/stanford/nlp/redwoods/Utilities.java and
			//  ProblemSolverHSPG.java.
			// periphery/.../classify/internal/ILogisticRegressionFactory.java
			// core/.../classify/ClassifierTaggingExamples.java
			// core/.../propbank/srl/JointRerankTrainer.java
			//
			// It would be a good idea to test those to see if they still work
			// as well.
			// maybe there is a better way to keep that, if it is zero or 1 , else the number // check whether it is non-deterministic, and how much
			// was 5 before CDM fiddled
			this.vArray = vArray;
			Ptilde();
		}

		/// <summary>
		/// The number of possible classes for each instance is contained in the array maxYs
		/// then the possible classes for x are from 0 to maxYs[x]-1.
		/// </summary>
		public Experiments(int[][] vArray, int[] maxYs)
		{
			this.vArray = vArray;
			Ptilde();
			this.maxY = maxYs;
		}

		public Experiments(int[][] vArray, int ySize)
		{
			this.vArray = vArray;
			this.ySize = ySize;
			Ptilde(ySize);
		}

		public virtual IIndex<IntPair> CreateIndex()
		{
			IIndex<IntPair> index = new HashIndex<IntPair>();
			for (int x = 0; x < px.Length; x++)
			{
				int numberY = NumY(x);
				for (int y = 0; y < numberY; y++)
				{
					index.Add(new IntPair(x, y));
				}
			}
			return index;
		}

		/// <summary>
		/// The filename has format:
		/// <literal><data><xSize>xSize</xSize><ySize>ySize</ySize></literal>
		/// x1 y1
		/// x2 y2
		/// ..
		/// <literal></data></literal>
		/// ..
		/// </summary>
		public Experiments(string filename)
		{
			try
			{
				using (BufferedReader @in = IOUtils.ReaderFromString(filename))
				{
					Exception e1 = new Exception("Incorrect data file format");
					string head = @in.ReadLine();
					if (!head.Equals("<data>"))
					{
						throw e1;
					}
					string xLine = @in.ReadLine();
					if (!xLine.StartsWith("<xSize>"))
					{
						throw e1;
					}
					if (!xLine.EndsWith("</xSize>"))
					{
						throw e1;
					}
					int index1 = xLine.IndexOf('>');
					int index2 = xLine.LastIndexOf('<');
					string xSt = Sharpen.Runtime.Substring(xLine, index1 + 1, index2);
					System.Console.Out.WriteLine(xSt);
					xSize = System.Convert.ToInt32(xSt);
					System.Console.Out.WriteLine("xSize is " + xSize);
					string yLine = @in.ReadLine();
					if (!yLine.StartsWith("<ySize>"))
					{
						throw e1;
					}
					if (!yLine.EndsWith("</ySize>"))
					{
						throw e1;
					}
					index1 = yLine.IndexOf('>');
					index2 = yLine.LastIndexOf('<');
					ySize = System.Convert.ToInt32(Sharpen.Runtime.Substring(yLine, index1 + 1, index2));
					System.Console.Out.WriteLine("ySize is " + ySize);
					string nLine = @in.ReadLine();
					if (!nLine.StartsWith("<number>"))
					{
						throw e1;
					}
					if (!nLine.EndsWith("</number>"))
					{
						throw e1;
					}
					index1 = nLine.IndexOf('>');
					index2 = nLine.LastIndexOf('<');
					int number = System.Convert.ToInt32(Sharpen.Runtime.Substring(nLine, index1 + 1, index2));
					System.Console.Out.WriteLine("number is " + number);
					vArray = new int[number][];
					int current = 0;
					while (current < number)
					{
						string experiment = @in.ReadLine();
						int index = experiment.IndexOf(' ');
						int x = System.Convert.ToInt32(Sharpen.Runtime.Substring(experiment, 0, index));
						int y = System.Convert.ToInt32(Sharpen.Runtime.Substring(experiment, index + 1));
						vArray[current][0] = x;
						vArray[current][1] = y;
						current++;
					}
					Ptilde(ySize);
				}
			}
			catch (Exception e)
			{
				System.Console.Out.WriteLine("Incorrect data file format");
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public virtual void Add(Edu.Stanford.Nlp.Maxent.Experiments m)
		{
			v.Add(m);
		}

		public void Ptilde()
		{
			int maxX = 0;
			int maxY = 0;
			foreach (int[] sample in vArray)
			{
				if (maxX < sample[0])
				{
					maxX = sample[0];
				}
				if (maxY < sample[1])
				{
					maxY = sample[1];
				}
			}
			px = new int[maxX + 1];
			py = new int[maxY + 1];
			pxy = new int[][] {  };
			xSize = maxX + 1;
			ySize = maxY + 1;
			//GlobalHolder.xSize=xSize;
			//GlobalHolder.ySize=ySize;
			int[] yArr = new int[dim];
			foreach (int[] sample_1 in vArray)
			{
				int xC = sample_1[0];
				int yC = sample_1[1];
				px[xC]++;
				py[yC]++;
				for (int j = 0; j < dim; j++)
				{
					yArr[j] = pxy[xC][j] > 0 ? pxy[xC][j] % ySize : -1;
				}
				for (int j_1 = 0; j_1 < dim; j_1++)
				{
					if (yArr[j_1] == -1)
					{
						pxy[xC][j_1] = ySize + yC;
						break;
					}
					if (yC == yArr[j_1])
					{
						pxy[xC][j_1] += ySize;
						break;
					}
				}
			}
			// for dim
			//System.out.println(" Exception more than  "+dim);
			// for i
			// check for same x with different y
			for (int y = 0; y < ySize; y++)
			{
				double sum = 0.0;
				for (int x = 0; x < xSize; x++)
				{
					double p1 = PtildeXY(x, y);
					sum = sum + p1;
				}
				if (Math.Abs(PtildeY(y) - sum) > 0.00001)
				{
					System.Console.Out.WriteLine("Experiments error: for y=" + y + ", ptildeY(y)=" + PtildeY(y) + " but Sum_x ptildeXY(x,y)=" + sum);
				}
			}
			// for y
			this.maxY = new int[xSize];
			for (int j_2 = 0; j_2 < xSize; j_2++)
			{
				this.maxY[j_2] = ySize;
			}
		}

		// end ptilde()
		public virtual void SetMaxY(int[] maxY)
		{
			this.maxY = maxY;
		}

		public virtual int NumY(int x)
		{
			return maxY[x];
		}

		/// <summary>When we want a pre-given number of classes.</summary>
		public virtual void Ptilde(int ySize)
		{
			int maxX = 0;
			int maxY = 0;
			this.ySize = ySize;
			foreach (int[] sample in vArray)
			{
				if (maxX < sample[0])
				{
					maxX = sample[0];
				}
				if (maxY < sample[1])
				{
					maxY = sample[1];
				}
			}
			px = new int[maxX + 1];
			maxY = ySize - 1;
			py = new int[ySize];
			pxy = new int[][] {  };
			xSize = maxX + 1;
			ySize = maxY + 1;
			//GlobalHolder.xSize=xSize;
			//GlobalHolder.ySize=ySize;
			int[] yArr = new int[dim];
			foreach (int[] sample_1 in vArray)
			{
				int xC = sample_1[0];
				int yC = sample_1[1];
				px[xC]++;
				py[yC]++;
				for (int j = 0; j < dim; j++)
				{
					yArr[j] = pxy[xC][j] > 0 ? pxy[xC][j] % ySize : -1;
				}
				for (int j_1 = 0; j_1 < dim; j_1++)
				{
					if (yArr[j_1] == -1)
					{
						pxy[xC][j_1] = ySize + yC;
						break;
					}
					if (yC == yArr[j_1])
					{
						pxy[xC][j_1] += ySize;
						break;
					}
				}
			}
			// for dim
			//System.out.println(" Exception more than  "+dim);
			// for i
			// check for same x with different y
			System.Console.Out.WriteLine("ySize is" + ySize);
			for (int y = 0; y < ySize; y++)
			{
				double sum = 0.0;
				for (int x = 0; x < xSize; x++)
				{
					double p1 = PtildeXY(x, y);
					sum = sum + p1;
				}
				if (Math.Abs(PtildeY(y) - sum) > 0.00001)
				{
					System.Console.Out.WriteLine("Experiments error: for y=" + y + ", ptildeY(y)=" + PtildeY(y) + " but Sum_x ptildeXY(x,y)=" + sum);
				}
				else
				{
					System.Console.Out.WriteLine("Experiments: for y " + y + " Sum_x ptildeXY(x,y)=" + sum);
				}
			}
		}

		// for y
		public virtual double PtildeX(int x)
		{
			if (x > xSize - 1)
			{
				return 0.0;
			}
			return px[x] / (double)vArray.Length;
		}

		public virtual double PtildeY(int y)
		{
			if (y > ySize - 1)
			{
				return 0.0;
			}
			return py[y] / (double)Size();
		}

		public virtual double PtildeXY(int x, int y)
		{
			for (int j = 0; j < dim; j++)
			{
				if (y == pxy[x][j] % ySize)
				{
					return (pxy[x][j] / ySize) / (double)Size();
				}
			}
			return 0.0;
		}

		public virtual int[] Get(int index)
		{
			return vArray[index];
		}

		/// <summary>Returns the number of training data items.</summary>
		public virtual int Size()
		{
			return vArray.Length;
		}

		public virtual int GetNumber()
		{
			return vArray.Length;
		}

		public virtual void Print()
		{
			System.Console.Out.WriteLine(" Experiments : ");
			for (int i = 0; i < Size(); i++)
			{
				System.Console.Out.WriteLine(vArray[i][0] + " : " + vArray[i][1]);
			}
			System.Console.Out.WriteLine(" p(x) ");
			for (int i_1 = 0; i_1 < xSize; i_1++)
			{
				System.Console.Out.WriteLine(i_1 + " : " + PtildeX(i_1));
			}
			System.Console.Out.WriteLine(" p(y) ");
			for (int i_2 = 0; i_2 < ySize; i_2++)
			{
				System.Console.Out.WriteLine(i_2 + " : " + PtildeY(i_2));
			}
		}

		public virtual void Print(PrintFile pf)
		{
			pf.WriteLine(" Experiments : ");
			for (int i = 0; i < Size(); i++)
			{
				pf.WriteLine(vArray[i][0] + " : " + vArray[i][1]);
			}
			pf.WriteLine(" p(x) ");
			for (int i_1 = 0; i_1 < xSize; i_1++)
			{
				pf.WriteLine(i_1 + " : " + PtildeX(i_1));
			}
			pf.WriteLine(" p(y) ");
			for (int i_2 = 0; i_2 < ySize; i_2++)
			{
				pf.WriteLine(i_2 + " : " + PtildeY(i_2));
			}
		}
	}
}
