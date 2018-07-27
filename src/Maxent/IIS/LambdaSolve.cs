using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Maxent;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Maxent.Iis
{
	/// <summary>This is the main class that does the core computation in IIS.</summary>
	/// <remarks>
	/// This is the main class that does the core computation in IIS.
	/// (Parts of it still get invoked in the POS tagger, even when not using IIS.)
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class LambdaSolve
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve));

		/// <summary>These are the model parameters that have to be learned.</summary>
		/// <remarks>
		/// These are the model parameters that have to be learned.
		/// This field is used at runtime in all tagger and other IIS/Kristina code.
		/// </remarks>
		public double[] lambda;

		/// <summary>Only allocated and used in the IIS optimization routines.</summary>
		protected internal bool[] lambda_converged;

		/// <summary>Only used in the IIS optimization routines.</summary>
		/// <remarks>Only used in the IIS optimization routines. Convergence threshold / allowed "newtonErr"</remarks>
		protected internal double eps;

		/// <summary>This flag is true if all (x,y) have the same f# in which case the newton equation solving is avoided.</summary>
		private bool fixedFnumXY;

		protected internal Problem p;

		/// <summary>Conditional probabilities.</summary>
		protected internal double[][] probConds;

		/// <summary>Normalization factors, one for each x.</summary>
		/// <remarks>
		/// Normalization factors, one for each x.  (CDM questions 2008: Are these
		/// only at training time?  Can we not allocate it at test time (unlike
		/// what LambdaSolveTagger now does)?  Is the place where it is set from
		/// ySize wrong?
		/// </remarks>
		protected internal double[] zlambda;

		/// <summary>This contains the number of features active for each pair (x,y)</summary>
		protected internal byte[][] fnumArr;

		/// <summary>This is an array of empirical expectations for the features</summary>
		protected internal double[] ftildeArr;

		private const bool smooth = false;

		private const bool Verbose = false;

		/// <summary>
		/// If this is true, assume that active features are binary, and one
		/// does not have to multiply in a feature value.
		/// </summary>
		private bool AssumeBinary = false;

		private double[] aux;

		private double[][] sum;

		private double[][] sub;

		public bool weightRanks = false;

		private bool convertValues = false;

		public LambdaSolve(Problem p1, double eps1, double nerr1)
		{
			// protected double newtonerr;
			// auxiliary array used by some procedures for computing objective functions and their derivatives
			// auxiliary array
			// auxiliary array
			p = p1;
			eps = eps1;
			// newtonerr = nerr1;
			// lambda = new double[p.fSize];
			probConds = new double[p.data.xSize][];
			log.Info("xSize is " + p.data.xSize);
			for (int i = 0; i < p.data.xSize; i++)
			{
				probConds[i] = new double[p.data.NumY(i)];
			}
			fnumArr = new byte[p.data.xSize][];
			for (int i_1 = 0; i_1 < p.data.xSize; i_1++)
			{
				fnumArr[i_1] = new byte[p.data.NumY(i_1)];
			}
			zlambda = new double[p.data.xSize];
			ftildeArr = new double[p.fSize];
			InitCondsZlambdaEtc();
			if (convertValues)
			{
				TransformValues();
			}
		}

		/// <summary>Reads the lambda parameters from a file.</summary>
		/// <param name="filename">File to read from</param>
		public LambdaSolve(string filename)
		{
			this.ReadL(filename);
		}

		public LambdaSolve()
		{
		}

		public virtual void SetNonBinary()
		{
			AssumeBinary = false;
		}

		public virtual void SetBinary()
		{
			AssumeBinary = true;
		}

		/// <summary>
		/// This is a specialized procedure to change the values
		/// of parses for semantic ranking.
		/// </summary>
		/// <remarks>
		/// This is a specialized procedure to change the values
		/// of parses for semantic ranking.
		/// The highest value is changed to 2/3
		/// and values of 1 are changed to 1/(3*numones). 0 is unchanged
		/// this is used to rank higher the ordering for the best parse
		/// values are in p.data.values
		/// </remarks>
		public virtual void TransformValues()
		{
			for (int x = 0; x < p.data.values.Length; x++)
			{
				double highest = p.data.values[x][0];
				double sumhighest = 0;
				double sumrest = 0;
				for (int y = 0; y < p.data.values[x].Length; y++)
				{
					if (p.data.values[x][y] > highest)
					{
						highest = p.data.values[x][y];
					}
				}
				for (int y_1 = 0; y_1 < p.data.values[x].Length; y_1++)
				{
					if (p.data.values[x][y_1] == highest)
					{
						sumhighest += highest;
					}
					else
					{
						sumrest += p.data.values[x][y_1];
					}
				}
				if (sumrest == 0)
				{
					continue;
				}
				// do not change , makes no difference
				//now change them
				for (int y_2 = 0; y_2 < p.data.values[x].Length; y_2++)
				{
					if (p.data.values[x][y_2] == highest)
					{
						p.data.values[x][y_2] = .7 * highest / sumhighest;
					}
					else
					{
						p.data.values[x][y_2] = .3 * p.data.values[x][y_2] / sumrest;
					}
				}
			}
		}

		/// <summary>
		/// Initializes the model parameters, empirical expectations of the
		/// features, and f#(x,y).
		/// </summary>
		internal virtual void InitCondsZlambdaEtc()
		{
			// init pcond
			for (int x = 0; x < p.data.xSize; x++)
			{
				for (int y = 0; y < probConds[x].Length; y++)
				{
					probConds[x][y] = 1.0 / probConds[x].Length;
				}
			}
			// init zlambda
			for (int x_1 = 0; x_1 < p.data.xSize; x_1++)
			{
				zlambda[x_1] = probConds[x_1].Length;
			}
			// init ftildeArr
			for (int i = 0; i < p.fSize; i++)
			{
				ftildeArr[i] = p.functions.Get(i).Ftilde();
				p.functions.Get(i).SetSum();
				// if the expectation of a feature is zero make sure we are not
				// trying to find a lambda for it
				// if (ftildeArr[i] == 0) {
				//   lambda_converged[i]=true;
				//   lambda[i]=0;
				// }
				//dumb smoothing that is not sound and doesn't seem to work
				Feature f = p.functions.Get(i);
				//collecting f#(x,y)
				for (int j = 0; j < f.Len(); j++)
				{
					int x_2 = f.GetX(j);
					int y = f.GetY(j);
					fnumArr[x_2][y] += f.GetVal(j);
				}
			}
			//j
			//i
			int constAll = fnumArr[0][0];
			fixedFnumXY = true;
			for (int x_3 = 0; x_3 < p.data.xSize; x_3++)
			{
				for (int y = 0; y < fnumArr[x_3].Length; y++)
				{
					if (fnumArr[x_3][y] != constAll)
					{
						fixedFnumXY = false;
						break;
					}
				}
			}
		}

		//x
		/// <summary>Iterate until convergence.</summary>
		/// <remarks>
		/// Iterate until convergence.  I usually use the other method that
		/// does a fixed number of iterations.
		/// </remarks>
		public virtual void ImprovedIterative()
		{
			bool flag;
			int iterations = 0;
			lambda_converged = new bool[p.fSize];
			int numNConverged = p.fSize;
			do
			{
				flag = false;
				iterations++;
				for (int i = 0; i < lambda.Length; i++)
				{
					if (lambda_converged[i])
					{
						continue;
					}
					MutableDouble deltaI = new MutableDouble();
					bool fl = Iterate(i, eps, deltaI);
					if (fl)
					{
						flag = true;
						UpdateConds(i, deltaI);
					}
					else
					{
						// checkCorrectness();
						//lambda_converged[i]=true;
						numNConverged--;
					}
				}
			}
			while ((flag) && (iterations < 1000));
		}

		/// <summary>Does a fixed number of IIS iterations.</summary>
		/// <param name="iters">Number of iterations to run</param>
		public virtual void ImprovedIterative(int iters)
		{
			int iterations = 0;
			lambda_converged = new bool[p.fSize];
			int numNConverged = p.fSize;
			do
			{
				//double lOld=logLikelihood();
				iterations++;
				for (int i = 0; i < lambda.Length; i++)
				{
					if (lambda_converged[i])
					{
						continue;
					}
					MutableDouble deltaI = new MutableDouble();
					bool fl = Iterate(i, eps, deltaI);
					if (fl)
					{
						UpdateConds(i, deltaI);
					}
					else
					{
						// checkCorrectness();
						//lambda_converged[i]=true;
						numNConverged--;
					}
				}
				/*
				double lNew=logLikelihood();
				double gain=(lNew-lOld);
				if(gain<0) {
				log.info(" Likelihood decreased by "+ (-gain));
				System.exit(1);
				}
				if(Math.abs(gain)<eps){
				log.info("Converged");
				break;
				}
				
				if(VERBOSE)
				log.info("Likelihood "+lNew+" "+" gain "+gain);
				lOld=lNew;
				*/
				if (iterations % 100 == 0)
				{
					Save_lambdas(iterations + ".lam");
				}
				log.Info(iterations);
			}
			while (iterations < iters);
		}

		/// <summary>Iteration for lambda[index].</summary>
		/// <remarks>
		/// Iteration for lambda[index].
		/// Returns true if this lambda hasn't converged. A lambda is deemed
		/// converged if the change found for it is smaller then the parameter eps.
		/// </remarks>
		internal virtual bool Iterate(int index, double err, MutableDouble ret)
		{
			double deltaL = 0.0;
			deltaL = Newton(deltaL, index, err);
			//log.info("delta is "+deltaL+" feature "+index+" expectation "+ftildeArr[index]);
			if (Math.Abs(deltaL + lambda[index]) > 200)
			{
				if ((deltaL + lambda[index]) > 200)
				{
					deltaL = 200 - lambda[index];
				}
				else
				{
					deltaL = -lambda[index] - 200;
				}
				log.Info("set delta to smth " + deltaL);
			}
			lambda[index] = lambda[index] + deltaL;
			if (double.IsNaN(deltaL))
			{
				log.Info(" NaN " + index + ' ' + deltaL);
			}
			ret.Set(deltaL);
			return (Math.Abs(deltaL) >= eps);
		}

		/*
		* Finds the root of an equation by Newton's method.
		* This is my implementation. It might be improved
		* if we looked at some official library for numerical methods.
		*/
		internal virtual double Newton(double lambda0, int index, double err)
		{
			double lambdaN = lambda0;
			int i = 0;
			if (fixedFnumXY)
			{
				double plambda = FExpected(p.functions.Get(index));
				return (1 / (double)fnumArr[0][0]) * (Math.Log(this.ftildeArr[index]) - Math.Log(plambda));
			}
			do
			{
				i++;
				double lambdaP = lambdaN;
				double gPrimeVal = Gprime(lambdaP, index);
				if (double.IsNaN(gPrimeVal))
				{
					log.Info("gPrime of " + lambdaP + " " + index + " is NaN " + gPrimeVal);
				}
				//lambda_converged[index]=true;
				//   System.exit(1);
				double gVal = G(lambdaP, index);
				if (gPrimeVal == 0.0)
				{
					return 0.0;
				}
				lambdaN = lambdaP - gVal / gPrimeVal;
				if (double.IsNaN(lambdaN))
				{
					log.Info("the division of " + gVal + " " + gPrimeVal + " " + index + " is NaN " + lambdaN);
					//lambda_converged[index]=true;
					return 0;
				}
				if (Math.Abs(lambdaN - lambdaP) < err)
				{
					return lambdaN;
				}
				if (i > 100)
				{
					if (Math.Abs(gVal) > 0.01)
					{
						return 0;
					}
					return lambdaN;
				}
			}
			while (true);
		}

		/// <summary>
		/// This method updates the conditional probabilities in the model, resulting from the
		/// update of lambda[index] to lambda[index]+deltaL .
		/// </summary>
		internal virtual void UpdateConds(int index, double deltaL)
		{
			//  for each x that (x,y)=true / exists y
			//  recalculate pcond(y,x) for all y
			for (int i = 0; i < p.functions.Get(index).Len(); i++)
			{
				// update for this x
				double s = 0;
				int x = p.functions.Get(index).GetX(i);
				int y = p.functions.Get(index).GetY(i);
				double val = p.functions.Get(index).GetVal(i);
				double zlambdaX = zlambda[x] + Pcond(y, x) * zlambda[x] * (Math.Exp(deltaL * val) - 1);
				for (int y1 = 0; y1 < probConds[x].Length; y1++)
				{
					probConds[x][y1] = (probConds[x][y1] * zlambda[x]) / zlambdaX;
					s = s + probConds[x][y1];
				}
				s = s - probConds[x][y];
				probConds[x][y] = probConds[x][y] * Math.Exp(deltaL * val);
				s = s + probConds[x][y];
				zlambda[x] = zlambdaX;
				if (Math.Abs(s - 1) > 0.001)
				{
				}
			}
		}

		//log.info(x+" index "+i+" deltaL " +deltaL+" tag "+yTag+" zlambda "+zlambda[x]);
		public virtual double Pcond(int y, int x)
		{
			return probConds[x][y];
		}

		protected internal virtual double Fnum(int x, int y)
		{
			return fnumArr[x][y];
		}

		internal virtual double G(double lambdaP, int index)
		{
			double s = 0.0;
			for (int i = 0; i < p.functions.Get(index).Len(); i++)
			{
				int y = p.functions.Get(index).GetY(i);
				int x = p.functions.Get(index).GetX(i);
				double exponent = Math.Exp(lambdaP * Fnum(x, y));
				s = s + p.data.PtildeX(x) * Pcond(y, x) * p.functions.Get(index).GetVal(i) * exponent;
			}
			s = s - ftildeArr[index];
			return s;
		}

		internal virtual double Gprime(double lambdaP, int index)
		{
			double s = 0.0;
			for (int i = 0; i < p.functions.Get(index).Len(); i++)
			{
				int y = ((p.functions.Get(index))).GetY(i);
				int x = p.functions.Get(index).GetX(i);
				s = s + p.data.PtildeX(x) * Pcond(y, x) * p.functions.Get(index).GetVal(i) * Math.Exp(lambdaP * Fnum(x, y)) * Fnum(x, y);
			}
			return s;
		}

		/// <summary>Computes the expected value of a feature for the current model.</summary>
		/// <param name="f">a feature</param>
		/// <returns>The expectation of f according to p(y|x)</returns>
		internal virtual double FExpected(Feature f)
		{
			double s = 0.0;
			for (int i = 0; i < f.Len(); i++)
			{
				int x = f.GetX(i);
				int y = f.GetY(i);
				s += p.data.PtildeX(x) * Pcond(y, x) * f.GetVal(i);
			}
			//for
			return s;
		}

		/// <summary>Check whether the constraints are satisfied, the probabilities sum to one, etc.</summary>
		/// <remarks>
		/// Check whether the constraints are satisfied, the probabilities sum to one, etc. Prints out a message
		/// if there is something wrong.
		/// </remarks>
		public virtual bool CheckCorrectness()
		{
			bool flag = true;
			for (int f = 0; f < lambda.Length; f++)
			{
				if (Math.Abs(lambda[f]) > 100)
				{
					log.Info("lambda " + f + " too big " + lambda[f]);
					log.Info("empirical " + ftildeArr[f] + " expected " + FExpected(p.functions.Get(f)));
				}
			}
			log.Info(" x size" + p.data.xSize + " " + " ysize " + p.data.ySize);
			double summAllExp = 0;
			for (int i = 0; i < ftildeArr.Length; i++)
			{
				double exp = Math.Abs(ftildeArr[i] - FExpected(p.functions.Get(i)));
				summAllExp += ftildeArr[i];
				if (exp > 0.001)
				{
					//if(true)
					flag = false;
					log.Info("Constraint not satisfied  " + i + " " + FExpected(p.functions.Get(i)) + " " + ftildeArr[i] + " lambda " + lambda[i]);
				}
			}
			log.Info(" The sum of all empirical expectations is " + summAllExp);
			for (int x = 0; x < p.data.xSize; x++)
			{
				double s = 0.0;
				for (int y = 0; y < probConds[x].Length; y++)
				{
					s = s + probConds[x][y];
				}
				if (Math.Abs(s - 1) > 0.0001)
				{
					for (int y_1 = 0; y_1 < probConds[x].Length; y_1++)
					{
						//log.info(y+" : "+ probConds[x][y]);
						log.Info("probabilities do not sum to one " + x + " " + (float)s);
					}
				}
			}
			return flag;
		}

		internal virtual double ZAlfa(double alfa, Feature f, int x)
		{
			double s = 0.0;
			for (int y = 0; y < probConds[x].Length; y++)
			{
				s = s + Pcond(y, x) * Math.Exp(alfa * f.GetVal(x, y));
			}
			return s;
		}

		internal virtual double Gsf(double alfa, Feature f, int index)
		{
			double s = 0.0;
			for (int x = 0; x < p.data.xSize; x++)
			{
				s = s - p.data.PtildeX(x) * Math.Log(ZAlfa(alfa, f, x));
			}
			return s + alfa * ftildeArr[index];
		}

		internal virtual double Gsf(double alfa, Feature f)
		{
			double s = 0.0;
			for (int x = 0; x < p.data.xSize; x++)
			{
				s = s - p.data.PtildeX(x) * Math.Log(ZAlfa(alfa, f, x));
			}
			return s + alfa * f.Ftilde();
		}

		internal virtual double PcondFAlfa(double alfa, int x, int y, Feature f)
		{
			double s;
			s = (1 / ZAlfa(alfa, f, x)) * Pcond(y, x) * Math.Exp(alfa * f.GetVal(x, y));
			return s;
		}

		internal virtual double GSFPrime(double alfa, Feature f, int index)
		{
			double s = 0.0;
			s = s + ftildeArr[index];
			for (int x1 = 0; x1 < f.indexedValues.Length; x1++)
			{
				double s1 = 0.0;
				int x = f.GetX(x1);
				int y = f.GetY(x1);
				s1 = s1 + PcondFAlfa(alfa, x, y, f) * f.GetVal(x1);
				s = s - p.data.PtildeX(x) * s1;
			}
			return s;
		}

		internal virtual double GSFPrime(double alfa, Feature f)
		{
			double s = 0.0;
			s = s + f.Ftilde();
			for (int x1 = 0; x1 < f.indexedValues.Length; x1++)
			{
				double s1 = 0.0;
				int x = f.GetX(x1);
				int y = f.GetY(x1);
				s1 = s1 + PcondFAlfa(alfa, x, y, f) * f.GetVal(x1);
				s = s - p.data.PtildeX(x) * s1;
			}
			return s;
		}

		internal virtual double GSFSecond(double alfa, Feature f)
		{
			double s = 0.0;
			for (int x = 0; x < p.data.xSize; x++)
			{
				double s1 = 0.0;
				double psff = 0.0;
				for (int y1 = 0; y1 < p.data.ySize; y1++)
				{
					psff = psff + PcondFAlfa(alfa, x, y1, f) * f.GetVal(x, y1);
				}
				for (int y = 0; y < probConds[x].Length; y++)
				{
					s1 = s1 + PcondFAlfa(alfa, x, y, f) * (f.GetVal(x, y) - psff) * (f.GetVal(x, y) - psff);
				}
				s = s - s1 * p.data.PtildeX(x);
			}
			return s;
		}

		/// <summary>Computes the gain from a feature.</summary>
		/// <remarks>Computes the gain from a feature. Used for feature selection.</remarks>
		public virtual double GainCompute(Feature f, double errorGain)
		{
			double r = (f.Ftilde() > FExpected(f) ? 1.0 : -1.0);
			f.InitHashVals();
			int iterations = 0;
			double alfa = 0.0;
			Gsf(alfa, f);
			double gsfValNew = 0.0;
			while (iterations < 30)
			{
				iterations++;
				double alfanext = alfa + r * Math.Log(1 - r * GSFPrime(alfa, f) / GSFSecond(alfa, f));
				gsfValNew = Gsf(alfanext, f);
				if (Math.Abs(alfanext - alfa) < errorGain)
				{
					return gsfValNew;
				}
				alfa = alfanext;
			}
			return gsfValNew;
		}

		/// <summary>Print out p(y|x) for all pairs to the standard output.</summary>
		public virtual void Print()
		{
			for (int i = 0; i < p.data.xSize; i++)
			{
				for (int j = 0; j < probConds[i].Length; j++)
				{
					System.Console.Out.WriteLine("P(" + j + " | " + i + ") = " + Pcond(j, i));
				}
			}
		}

		/// <summary>Writes the lambda feature weights to the file.</summary>
		/// <remarks>
		/// Writes the lambda feature weights to the file.
		/// Can be read later with readL.
		/// This method opens a new file and closes it after writing it.
		/// </remarks>
		/// <param name="filename">The file to write the weights to.</param>
		public virtual void Save_lambdas(string filename)
		{
			try
			{
				DataOutputStream rf = IOUtils.GetDataOutputStream(filename);
				Save_lambdas(rf, lambda);
				rf.Close();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Writes the lambdas to a stream.</summary>
		public static void Save_lambdas(DataOutputStream rf, double[] lambdas)
		{
			try
			{
				ObjectOutputStream oos = new ObjectOutputStream(rf);
				oos.WriteObject(lambdas);
				oos.Flush();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Read the lambdas from the file.</summary>
		/// <remarks>
		/// Read the lambdas from the file.
		/// The file contains the number of lambda weights (int) followed by
		/// the weights.
		/// <i>Historical note:</i> The file does not contain
		/// xSize and ySize as for the method read(String).
		/// </remarks>
		/// <param name="filename">The file to read from</param>
		public virtual void ReadL(string filename)
		{
			try
			{
				DataInputStream rf = IOUtils.GetDataInputStream(filename);
				lambda = Read_lambdas(rf);
				rf.Close();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Read the lambdas from the file.</summary>
		/// <param name="modelFilename">A filename. It will be read and closed</param>
		/// <returns>An array of lambda values read from the file.</returns>
		internal static double[] Read_lambdas(string modelFilename)
		{
			try
			{
				DataInputStream rf = IOUtils.GetDataInputStream(modelFilename);
				double[] lamb = Read_lambdas(rf);
				rf.Close();
				return lamb;
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}

		/// <summary>Read the lambdas from the stream.</summary>
		/// <param name="rf">Stream to read from.</param>
		/// <returns>An array of lambda values read from the stream.</returns>
		public static double[] Read_lambdas(DataInputStream rf)
		{
			try
			{
				ObjectInputStream ois = new ObjectInputStream(rf);
				object o = ois.ReadObject();
				if (o is double[])
				{
					return (double[])o;
				}
				throw new RuntimeIOException("Failed to read lambdas from given input stream");
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// This method writes the problem data into a file, which is good for reading
		/// with MatLab.
		/// </summary>
		/// <remarks>
		/// This method writes the problem data into a file, which is good for reading
		/// with MatLab.  It could also have other applications,
		/// like reducing the memory requirements
		/// </remarks>
		internal virtual void Save_problem(string filename)
		{
			try
			{
				PrintFile pf = new PrintFile(filename);
				int N = p.data.xSize;
				int M = p.data.ySize;
				int F = p.fSize;
				// byte[] nl = "\n".getBytes();
				// byte[] dotsp = ". ".getBytes();
				// int space = (int) ' ';
				// write the sizes of X, Y, and F( number of features );
				pf.WriteLine(N);
				pf.WriteLine(M);
				pf.WriteLine(F);
				// save the objective vector like 1.c0, ... ,N*M. cN*M-1
				for (int i = 0; i < N * M; i++)
				{
					pf.Write(i + 1);
					pf.Write(". ");
					pf.WriteLine(p.data.PtildeX(i / M));
				}
				// for i
				// save the constraints matrix B
				// for each feature , save its row
				for (int i_1 = 0; i_1 < p.fSize; i_1++)
				{
					int[] values = p.functions.Get(i_1).indexedValues;
					foreach (int value in values)
					{
						pf.Write(i_1 + 1);
						pf.Write(". ");
						pf.Write(value);
						pf.Write(" ");
						pf.WriteLine(1);
					}
				}
				// k
				// i
				// save the constraints vector
				// for each feature, save its empirical expectation
				for (int i_2 = 0; i_2 < p.fSize; i_2++)
				{
					pf.Write(i_2 + 1);
					pf.Write(". ");
					pf.WriteLine(ftildeArr[i_2]);
				}
				// end
				pf.Close();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <returns>The loglikelihood of the empirical distribution as predicted by the model p.</returns>
		public virtual double LogLikelihood()
		{
			//L=sumx,y log(p(y|x))*#x,y
			double sum = 0.0;
			int sz = p.data.Size();
			for (int index = 0; index < sz; index++)
			{
				int[] example = p.data.Get(index);
				sum += Math.Log(Pcond(example[1], example[0]));
			}
			// index
			return sum / sz;
		}

		/// <summary>
		/// Given a numerator and denominator in log form, this calculates
		/// the conditional model probabilities.
		/// </summary>
		/// <returns>Math.exp(first)/Math.exp(second);</returns>
		public static double Divide(double first, double second)
		{
			return Math.Exp(first - second);
		}

		// cpu samples #3,#14: 5.3%
		/// <summary>
		/// With arguments, this will print out the lambda parameters of a
		/// bunch of .lam files (which are assumed to all be the same size).
		/// </summary>
		/// <remarks>
		/// With arguments, this will print out the lambda parameters of a
		/// bunch of .lam files (which are assumed to all be the same size).
		/// (Without arguments, it does some creaky old self-test.)
		/// </remarks>
		/// <param name="args">command line arguments</param>
		public static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				NumberFormat nf = NumberFormat.GetNumberInstance();
				nf.SetMaximumFractionDigits(6);
				nf.SetMinimumFractionDigits(6);
				Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve[] lambdas = new Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve[args.Length];
				System.Console.Out.Write("           ");
				for (int i = 0; i < args.Length; i++)
				{
					lambdas[i] = new Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve();
					lambdas[i].ReadL(args[i]);
					System.Console.Out.Write("  " + args[i]);
				}
				System.Console.Out.WriteLine();
				int numLambda = lambdas[0].lambda.Length;
				for (int j = 0; j < numLambda; j++)
				{
					System.Console.Out.Write("lambda[" + j + "] = ");
					for (int i_1 = 0; i_1 < args.Length; i_1++)
					{
						System.Console.Out.Write(nf.Format(lambdas[i_1].lambda[j]) + "  ");
					}
					System.Console.Out.WriteLine();
				}
			}
			else
			{
				Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve prob = new Edu.Stanford.Nlp.Maxent.Iis.LambdaSolve("trainhuge.txt.holder.prob");
				prob.Save_lambdas("trainhuge.txt.holder.prob");
				prob.ReadL("trainhuge.txt.holder.prob");
			}
		}

		/// <summary>
		/// Calculate the log-likelihood from scratch, hashing the conditional
		/// probabilities in pcond, which we will use later.
		/// </summary>
		/// <remarks>
		/// Calculate the log-likelihood from scratch, hashing the conditional
		/// probabilities in pcond, which we will use later. This is for
		/// a different model, in which all features effectively get negative weights
		/// this model is easier to use for heauristic search
		/// p(ti|s)=exp(sum_j{-(e^lambda_j)*f_j(ti)})
		/// </remarks>
		/// <returns>The negative log likelihood of the data</returns>
		public virtual double LogLikelihoodNeg()
		{
			// zero all the variables
			double s = 0;
			for (int i = 0; i < probConds.Length; i++)
			{
				for (int j = 0; j < probConds[i].Length; j++)
				{
					probConds[i][j] = 0;
				}
				zlambda[i] = 0;
			}
			//add up in pcond y|x the unnormalized scores
			for (int fNo = 0; fNo < fSize; fNo++)
			{
				// add for all occurences of the function the values to probConds
				Feature f = p.functions.Get(fNo);
				double fLambda = -Math.Exp(lambda[fNo]);
				double sum = ftildeArr[fNo];
				//if(sum==0){continue;}
				sum *= p.data.GetNumber();
				s -= sum * fLambda;
				if (Math.Abs(fLambda) > 200)
				{
					// was 50
					log.Info("lambda " + fNo + " too big: " + fLambda);
				}
				for (int i_1 = 0; i_1 < length; i_1++)
				{
					int x = f.GetX(i_1);
					int y = f.GetY(i_1);
					if (AssumeBinary)
					{
						probConds[x][y] += fLambda;
					}
					else
					{
						double val = f.GetVal(i_1);
						probConds[x][y] += (val * fLambda);
					}
				}
			}
			//for
			//for fNo
			for (int x_1 = 0; x_1 < probConds.Length; x_1++)
			{
				//again
				zlambda[x_1] = ArrayMath.LogSum(probConds[x_1]);
				// cpu samples #4,#15: 4.5%
				//log.info("zlambda "+x+" "+zlambda[x]);
				s += zlambda[x_1] * p.data.PtildeX(x_1) * p.data.GetNumber();
				for (int y = 0; y < probConds[x_1].Length; y++)
				{
					probConds[x_1][y] = Divide(probConds[x_1][y], zlambda[x_1]);
				}
			}
			// cpu samples #13: 1.6%
			//log.info("prob "+x+" "+y+" "+probConds[x][y]);
			//y
			//x
			if (s < 0)
			{
				throw new InvalidOperationException("neg log lik smaller than 0: " + s);
			}
			return s;
		}

		// -- stuff for CG version below -------
		/// <summary>
		/// calculate the log likelihood from scratch, hashing the conditional
		/// probabilities in pcond which we will use for the derivative later.
		/// </summary>
		/// <returns>The log likelihood of the data</returns>
		public virtual double LogLikelihoodScratch()
		{
			// zero all the variables
			double s = 0;
			for (int i = 0; i < probConds.Length; i++)
			{
				for (int j = 0; j < probConds[i].Length; j++)
				{
					probConds[i][j] = 0;
				}
				zlambda[i] = 0;
			}
			//add up in pcond y|x the unnormalized scores
			Experiments exp = p.data;
			for (int fNo = 0; fNo < fSize; fNo++)
			{
				// add for all occurences of the function the values to probConds
				Feature f = p.functions.Get(fNo);
				double fLambda = lambda[fNo];
				double sum = ftildeArr[fNo];
				//if(sum==0){continue;}
				sum *= exp.GetNumber();
				s -= sum * fLambda;
				if (System.Math.Abs(fLambda) > 200)
				{
					// was 50
					log.Info("lambda " + fNo + " too big: " + fLambda);
				}
				for (int i_1 = 0; i_1 < length; i_1++)
				{
					int x = f.GetX(i_1);
					int y = f.GetY(i_1);
					if (AssumeBinary)
					{
						probConds[x][y] += fLambda;
					}
					else
					{
						double val = f.GetVal(i_1);
						probConds[x][y] += (val * fLambda);
					}
				}
			}
			//for
			//for fNo
			for (int x_1 = 0; x_1 < probConds.Length; x_1++)
			{
				//again
				zlambda[x_1] = ArrayMath.LogSum(probConds[x_1]);
				// cpu samples #4,#15: 4.5%
				//log.info("zlambda "+x+" "+zlambda[x]);
				s += zlambda[x_1] * exp.PtildeX(x_1) * exp.GetNumber();
				for (int y = 0; y < probConds[x_1].Length; y++)
				{
					probConds[x_1][y] = Divide(probConds[x_1][y], zlambda[x_1]);
				}
			}
			// cpu samples #13: 1.6%
			//log.info("prob "+x+" "+y+" "+probConds[x][y]);
			//y
			//x
			if (s < 0)
			{
				throw new InvalidOperationException("neg log lik smaller than 0: " + s);
			}
			return s;
		}

		/// <summary>
		/// assuming we have the lambdas in the array and we need only the
		/// derivatives now.
		/// </summary>
		public virtual double[] GetDerivatives()
		{
			double[] drvs = new double[lambda.Length];
			Experiments exp = p.data;
			for (int fNo = 0; fNo < drvs.Length; fNo++)
			{
				// cpu samples #2,#10,#12: 27.3%
				Feature f = p.functions.Get(fNo);
				double sum = ftildeArr[fNo] * exp.GetNumber();
				drvs[fNo] = -sum;
				for (int index = 0; index < length; index++)
				{
					int x = f.GetX(index);
					int y = f.GetY(index);
					if (AssumeBinary)
					{
						drvs[fNo] += probConds[x][y] * exp.PtildeX(x) * exp.GetNumber();
					}
					else
					{
						double val = f.GetVal(index);
						drvs[fNo] += probConds[x][y] * val * exp.PtildeX(x) * exp.GetNumber();
					}
				}
			}
			//for
			//if(sum==0){drvs[fNo]=0;}
			return drvs;
		}

		/// <summary>
		/// assuming we have the lambdas in the array and we need only the
		/// derivatives now.
		/// </summary>
		/// <remarks>
		/// assuming we have the lambdas in the array and we need only the
		/// derivatives now.
		/// this is for the case where the model is parameterezied such that all weights are negative
		/// see also logLikelihoodNeg
		/// </remarks>
		public virtual double[] GetDerivativesNeg()
		{
			double[] drvs = new double[lambda.Length];
			Experiments exp = p.data;
			for (int fNo = 0; fNo < drvs.Length; fNo++)
			{
				// cpu samples #2,#10,#12: 27.3%
				Feature f = p.functions.Get(fNo);
				double sum = ftildeArr[fNo] * exp.GetNumber();
				double lam = -System.Math.Exp(lambda[fNo]);
				drvs[fNo] = -sum * lam;
				for (int index = 0; index < length; index++)
				{
					int x = f.GetX(index);
					int y = f.GetY(index);
					if (AssumeBinary)
					{
						drvs[fNo] += probConds[x][y] * exp.PtildeX(x) * exp.GetNumber() * lam;
					}
					else
					{
						double val = f.GetVal(index);
						drvs[fNo] += probConds[x][y] * val * exp.PtildeX(x) * exp.GetNumber() * lam;
					}
				}
			}
			//for
			//if(sum==0){drvs[fNo]=0;}
			return drvs;
		}

		/// <summary>Each pair x,y has a value in p.data.values[x][y]</summary>
		/// <returns>- expected value of corpus -sum_xy (ptilde(x,y)*value(x,y)*pcond(x,y))</returns>
		public virtual double ExpectedValue()
		{
			// zero all the variables
			double s = 0;
			aux = new double[probConds.Length];
			for (int i = 0; i < probConds.Length; i++)
			{
				for (int j = 0; j < probConds[i].Length; j++)
				{
					probConds[i][j] = 0;
				}
				zlambda[i] = 0;
			}
			//add up in pcond y|x the unnormalized scores
			for (int fNo = 0; fNo < fSize; fNo++)
			{
				// add for all occurrences of the function the values to probConds
				Feature f = p.functions.Get(fNo);
				double fLambda = lambda[fNo];
				if (System.Math.Abs(fLambda) > 200)
				{
					// was 50
					log.Info("lambda " + fNo + " too big: " + fLambda);
				}
				for (int i_1 = 0; i_1 < length; i_1++)
				{
					int x = f.GetX(i_1);
					int y = f.GetY(i_1);
					if (AssumeBinary)
					{
						probConds[x][y] += fLambda;
					}
					else
					{
						double val = f.GetVal(i_1);
						probConds[x][y] += (val * fLambda);
					}
				}
			}
			//for
			//for fNo
			Experiments exp = p.data;
			for (int x_1 = 0; x_1 < probConds.Length; x_1++)
			{
				//again
				zlambda[x_1] = ArrayMath.LogSum(probConds[x_1]);
				// cpu samples #4,#15: 4.5%
				//log.info("zlambda "+x+" "+zlambda[x]);
				for (int y = 0; y < probConds[x_1].Length; y++)
				{
					probConds[x_1][y] = Divide(probConds[x_1][y], zlambda[x_1]);
					// cpu samples #13: 1.6%
					//log.info("prob "+x+" "+y+" "+probConds[x][y]);
					s -= exp.values[x_1][y] * probConds[x_1][y] * exp.PtildeX(x_1) * exp.GetNumber();
					aux[x_1] += exp.values[x_1][y] * probConds[x_1][y];
				}
			}
			//x
			return s;
		}

		/// <summary>assuming we have the probConds[x][y] , compute the derivatives for the expectedValue function</summary>
		/// <returns>The derivatives of the expected</returns>
		public virtual double[] GetDerivativesExpectedValue()
		{
			double[] drvs = new double[lambda.Length];
			Experiments exp = p.data;
			for (int fNo = 0; fNo < drvs.Length; fNo++)
			{
				// cpu samples #2,#10,#12: 27.3%
				Feature f = p.functions.Get(fNo);
				//double sum = ftildeArr[fNo] * exp.getNumber();
				//drvs[fNo] = -sum;
				for (int index = 0; index < length; index++)
				{
					int x = f.GetX(index);
					int y = f.GetY(index);
					double val = f.GetVal(index);
					double mult = val * probConds[x][y] * exp.PtildeX(x) * exp.GetNumber();
					drvs[fNo] -= mult * exp.values[x][y];
					drvs[fNo] += mult * aux[x];
				}
			}
			//for
			//if(sum==0){drvs[fNo]=0;}
			return drvs;
		}

		/// <summary>
		/// calculate the loss for Dom ranking
		/// using the numbers in p.data.values to determine domination relationships in the graphs
		/// if values[x][y]&gt; values[x][y'] then there is an edge (x,y)-&gt;(x,y')
		/// </summary>
		/// <returns>The loss</returns>
		public virtual double LossDomination()
		{
			// zero all the variables
			double s = 0;
			for (int i = 0; i < probConds.Length; i++)
			{
				for (int j = 0; j < probConds[i].Length; j++)
				{
					probConds[i][j] = 0;
				}
				zlambda[i] = 0;
			}
			//add up in pcond y|x the unnormalized scores
			for (int fNo = 0; fNo < fSize; fNo++)
			{
				// add for all occurrences of the function the values to probConds
				Feature f = p.functions.Get(fNo);
				double fLambda = lambda[fNo];
				//if(sum==0){continue;}
				if (System.Math.Abs(fLambda) > 200)
				{
					// was 50
					log.Info("lambda " + fNo + " too big: " + fLambda);
				}
				for (int i_1 = 0; i_1 < length; i_1++)
				{
					int x = f.GetX(i_1);
					int y = f.GetY(i_1);
					if (AssumeBinary)
					{
						probConds[x][y] += fLambda;
					}
					else
					{
						double val = f.GetVal(i_1);
						probConds[x][y] += (val * fLambda);
					}
				}
			}
			//for
			//for fNo
			//will use zlambda[x] for the number of domination graphs for x
			// keeping track of other arrays as well - sum[x][y], and sub[x][y]
			//now two double loops over (x,y) to collect zlambda[x], sum[x][y], and sub[x][y];
			sum = new double[probConds.Length][];
			sub = new double[probConds.Length][];
			for (int x_1 = 0; x_1 < probConds.Length; x_1++)
			{
				sum[x_1] = new double[probConds[x_1].Length];
				sub[x_1] = new double[probConds[x_1].Length];
				double localloss = 0;
				for (int u = 0; u < sum[x_1].Length; u++)
				{
					bool hasgraph = false;
					for (int v = 0; v < sum[x_1].Length; v++)
					{
						//see if u dominates v
						if (p.data.values[x_1][u] > p.data.values[x_1][v])
						{
							hasgraph = true;
							sum[x_1][u] += System.Math.Exp(probConds[x_1][v] - probConds[x_1][u]);
						}
					}
					sum[x_1][u] += 1;
					double weight = 1;
					if (weightRanks)
					{
						weight = p.data.values[x_1][u];
					}
					if (hasgraph)
					{
						zlambda[x_1] += weight;
					}
					localloss += weight * System.Math.Log(sum[x_1][u]);
				}
				//another loop to get the sub[x][y]
				for (int u_1 = 0; u_1 < sum[x_1].Length; u_1++)
				{
					for (int v = 0; v < sum[x_1].Length; v++)
					{
						//see if u dominates v
						if (p.data.values[x_1][u_1] > p.data.values[x_1][v])
						{
							double weight = 1;
							if (weightRanks)
							{
								weight = p.data.values[x_1][u_1];
							}
							sub[x_1][v] += weight * System.Math.Exp(probConds[x_1][v] - probConds[x_1][u_1]) / sum[x_1][u_1];
						}
					}
				}
				log.Info(" for x " + x_1 + " number graphs " + zlambda[x_1]);
				if (zlambda[x_1] > 0)
				{
					localloss /= zlambda[x_1];
					s += p.data.PtildeX(x_1) * p.data.GetNumber() * localloss;
				}
			}
			//x
			return s;
		}

		/// <summary>
		/// Using the arrays calculated when computing the loss, it should not be
		/// too hard to get the derivatives.
		/// </summary>
		/// <returns>The derivative of the loss</returns>
		public virtual double[] GetDerivativesLossDomination()
		{
			double[] drvs = new double[lambda.Length];
			for (int fNo = 0; fNo < drvs.Length; fNo++)
			{
				// cpu samples #2,#10,#12: 27.3%
				Feature f = p.functions.Get(fNo);
				for (int index = 0; index < length; index++)
				{
					int x = f.GetX(index);
					int y = f.GetY(index);
					double val = f.GetVal(index);
					//add the sub and sum components
					if (zlambda[x] == 0)
					{
						continue;
					}
					double mult = val * p.data.PtildeX(x) * p.data.GetNumber() * (1 / zlambda[x]);
					double weight = 1;
					if (weightRanks)
					{
						weight = p.data.values[x][y];
					}
					drvs[fNo] += mult * sub[x][y];
					drvs[fNo] -= mult * weight * (sum[x][y] - 1) / sum[x][y];
				}
			}
			//for
			//if(sum==0){drvs[fNo]=0;}
			return drvs;
		}
	}
}
