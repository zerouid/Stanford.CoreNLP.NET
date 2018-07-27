using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Alex Kleeman</author>
	public class StochasticDiffFunctionTester
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.StochasticDiffFunctionTester));

		private static double Eps = 1e-8;

		private static bool quiet = false;

		protected internal int testBatchSize;

		protected internal int numBatches;

		protected internal AbstractStochasticCachingDiffFunction thisFunc;

		internal double[] approxGrad;

		internal double[] fullGrad;

		internal double[] diff;

		internal double[] Hv;

		internal double[] HvFD;

		internal double[] v;

		internal double[] curGrad;

		internal double[] gradFD;

		internal double diffNorm;

		internal double diffValue;

		internal double fullValue;

		internal double approxValue;

		internal double diffGrad;

		internal double maxGradDiff = 0.0;

		internal double maxHvDiff = 0.0;

		internal Random generator;

		private static NumberFormat nf = new DecimalFormat("00.0");

		public StochasticDiffFunctionTester(IFunction function)
		{
			// check for derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				log.Info("Attempt to test non stochastic function using StochasticDiffFunctionTester");
				throw new NotSupportedException();
			}
			thisFunc = (AbstractStochasticCachingDiffFunction)function;
			// Make sure the function is Stochastic
			generator = new Random(Runtime.CurrentTimeMillis());
			// used to generate random test vectors
			//  Look for a good batchSize to test with by getting factors
			testBatchSize = (int)GetTestBatchSize(thisFunc.DataDimension());
			//  Again make sure that our calculated batchSize is actually valid
			if (testBatchSize < 0 || testBatchSize > thisFunc.DataDimension() || (thisFunc.DataDimension() % testBatchSize != 0))
			{
				log.Info("Invalid testBatchSize found, testing aborted.  Data size: " + thisFunc.DataDimension() + " batchSize: " + testBatchSize);
				System.Environment.Exit(1);
			}
			numBatches = thisFunc.DataDimension() / testBatchSize;
			Sayln("StochasticDiffFunctionTester created with:");
			Sayln("   data dimension  = " + thisFunc.DataDimension());
			Sayln("   batch size = " + testBatchSize);
			Sayln("   number of batches = " + numBatches);
		}

		private void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}

		//  Get Prime Factors of an integer ....
		//  Code was originally from    http://www.idinews.com/sourcecode/IntegerFunction.html
		//  Decompose integer into prime factors
		//  ------------------------------------
		//  Upon return result[0] contains the number of factors (0 if N is 0), and
		//  result[1] . . . result[result[0]] contain the factors in ascending order.
		private static long[] PrimeFactors(long N)
		{
			long[] fctr = new long[64];
			//  Result array
			long n = Math.Abs(N);
			//  Guard against negative
			short fctrIndex = 0;
			if (n > 0)
			{
				//  Guard against zero
				//  First do special cases 2 and 3
				while (n % 2 == 0)
				{
					fctr[++fctrIndex] = 2;
					n /= 2;
				}
				while (n % 3 == 0)
				{
					fctr[++fctrIndex] = 3;
					n /= 3;
				}
				//  Then every 6n-1 and 6n+1 until the divisor exceeds the square root
				//  of the current quotient.  NOTE:  Some trial divisors will be
				//  non-primes, e.g. 25, 35, 49, 55.  They have no effect, however,
				//  since their prime factors will already have been tried.
				for (int k = 5; k * k <= n; k += 6)
				{
					for (int dvsr = k; dvsr <= k + 2; dvsr += 2)
					{
						while (n % dvsr == 0)
						{
							fctr[++fctrIndex] = dvsr;
							n /= dvsr;
						}
					}
				}
				if (n > 1)
				{
					fctr[++fctrIndex] = n;
				}
			}
			//  Store final factor, if any
			fctr[0] = fctrIndex;
			//  Store number of factors
			return fctr;
		}

		/// <summary>
		/// getTestBatchSize - This function takes as input the size of the data and returns the largest factor of the data size
		/// this is done so that when testing the function we are gaurenteed to have equally sized batches, and that the fewest
		/// number of evaluations needs to be made in order to test the function.
		/// </summary>
		/// <param name="size">- The size of the current data set</param>
		/// <returns>The largest factor of the data size</returns>
		private static long GetTestBatchSize(long size)
		{
			long testBatchSize = 1;
			long[] factors = PrimeFactors(size);
			long factorCount = factors[0];
			// Calculate the batchsize for the factors
			if (factorCount == 0)
			{
				log.Info("Attempt to test function on data of prime dimension.  This would involve a batchSize of 1 and may take a very long time.");
				System.Environment.Exit(1);
			}
			else
			{
				if (factorCount == 2)
				{
					testBatchSize = (int)factors[1];
				}
				else
				{
					//  find the largest factor.
					for (int f = 1; f < factorCount; f++)
					{
						testBatchSize *= factors[f];
					}
				}
			}
			return testBatchSize;
		}

		/// <summary>
		/// This function tests to make sure that the sum of the stochastic calculated gradients is equal to the
		/// full gradient.
		/// </summary>
		/// <remarks>
		/// This function tests to make sure that the sum of the stochastic calculated gradients is equal to the
		/// full gradient.  This requires using ordered sampling, so if the ObjectiveFunction itself randomizes
		/// the inputs this function will likely fail.
		/// </remarks>
		/// <param name="x">is the point to evaluate the function at</param>
		/// <param name="functionTolerance">is the tolerance to place on the infinity norm of the gradient and value</param>
		/// <returns>boolean indicating success or failure.</returns>
		public virtual bool TestSumOfBatches(double[] x, double functionTolerance)
		{
			bool ret = false;
			log.Info("Making sure that the sum of stochastic gradients equals the full gradient");
			AbstractStochasticCachingDiffFunction.SamplingMethod tmpSampleMethod = thisFunc.sampleMethod;
			StochasticCalculateMethods tmpMethod = thisFunc.method;
			//Make sure that our function is using ordered sampling.  Otherwise we have no gaurentees.
			thisFunc.sampleMethod = AbstractStochasticCachingDiffFunction.SamplingMethod.Ordered;
			if (thisFunc.method == StochasticCalculateMethods.NoneSpecified)
			{
				log.Info("No calculate method has been specified");
			}
			approxValue = 0;
			approxGrad = new double[x.Length];
			curGrad = new double[x.Length];
			fullGrad = new double[x.Length];
			double percent = 0.0;
			//This loop runs through all the batches and sums of the calculations to compare against the full gradient
			for (int i = 0; i < numBatches; i++)
			{
				percent = 100 * ((double)i) / (numBatches);
				//  update the value
				approxValue += thisFunc.ValueAt(x, v, testBatchSize);
				//  update the gradient
				thisFunc.returnPreviousValues = true;
				System.Array.Copy(thisFunc.DerivativeAt(x, v, testBatchSize), 0, curGrad, 0, curGrad.Length);
				//Update Approximate
				approxGrad = ArrayMath.PairwiseAdd(approxGrad, curGrad);
				double norm = ArrayMath.Norm(approxGrad);
				System.Console.Error.Printf("%5.1f percent complete  %6.2f \n", percent, norm);
			}
			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			// Get the full gradient and value, these should equal the approximates
			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			log.Info("About to calculate the full derivative and value");
			System.Array.Copy(thisFunc.DerivativeAt(x), 0, fullGrad, 0, fullGrad.Length);
			thisFunc.returnPreviousValues = true;
			fullValue = thisFunc.ValueAt(x);
			diff = new double[x.Length];
			if ((ArrayMath.Norm_inf(diff = ArrayMath.PairwiseSubtract(fullGrad, approxGrad))) < functionTolerance)
			{
				Sayln(string.Empty);
				Sayln("Success: sum of batch gradients equals full gradient");
				ret = true;
			}
			else
			{
				diffNorm = ArrayMath.Norm(diff);
				Sayln(string.Empty);
				Sayln("Failure: sum of batch gradients minus full gradient has norm " + diffNorm);
				ret = false;
			}
			if (System.Math.Abs(approxValue - fullValue) < functionTolerance)
			{
				Sayln(string.Empty);
				Sayln("Success: sum of batch values equals full value");
				ret = true;
			}
			else
			{
				Sayln(string.Empty);
				Sayln("Failure: sum of batch values minus full value has norm " + System.Math.Abs(approxValue - fullValue));
				ret = false;
			}
			thisFunc.sampleMethod = tmpSampleMethod;
			thisFunc.method = tmpMethod;
			return ret;
		}

		/// <summary>
		/// This function tests to make sure that the sum of the stochastic calculated gradients is equal to the
		/// full gradient.
		/// </summary>
		/// <remarks>
		/// This function tests to make sure that the sum of the stochastic calculated gradients is equal to the
		/// full gradient.  This requires using ordered sampling, so if the ObjectiveFunction itself randomizes
		/// the inputs this function will likely fail.
		/// </remarks>
		/// <param name="x">is the point to evaluate the function at</param>
		/// <param name="functionTolerance">is the tolerance to place on the infinity norm of the gradient and value</param>
		/// <returns>boolean indicating success or failure.</returns>
		public virtual bool TestDerivatives(double[] x, double functionTolerance)
		{
			bool ret = false;
			bool compareHess = true;
			log.Info("Making sure that the stochastic derivatives are ok.");
			AbstractStochasticCachingDiffFunction.SamplingMethod tmpSampleMethod = thisFunc.sampleMethod;
			StochasticCalculateMethods tmpMethod = thisFunc.method;
			//Make sure that our function is using ordered sampling.  Otherwise we have no gaurentees.
			thisFunc.sampleMethod = AbstractStochasticCachingDiffFunction.SamplingMethod.Ordered;
			if (thisFunc.method == StochasticCalculateMethods.NoneSpecified)
			{
				log.Info("No calculate method has been specified");
			}
			else
			{
				if (!thisFunc.method.CalculatesHessianVectorProduct())
				{
					compareHess = false;
				}
			}
			approxValue = 0;
			approxGrad = new double[x.Length];
			curGrad = new double[x.Length];
			Hv = new double[x.Length];
			double percent = 0.0;
			//This loop runs through all the batches and sums of the calculations to compare against the full gradient
			for (int i = 0; i < numBatches; i++)
			{
				percent = 100 * ((double)i) / (numBatches);
				//Can't figure out how to get a carriage return???  ohh well
				System.Console.Error.Printf("%5.1f percent complete\n", percent);
				//  update the "hopefully" correct Hessian
				thisFunc.method = tmpMethod;
				System.Array.Copy(thisFunc.HdotVAt(x, v, testBatchSize), 0, Hv, 0, Hv.Length);
				//  Now get the hessian through finite difference
				thisFunc.method = StochasticCalculateMethods.ExternalFiniteDifference;
				System.Array.Copy(thisFunc.DerivativeAt(x, v, testBatchSize), 0, gradFD, 0, gradFD.Length);
				thisFunc.recalculatePrevBatch = true;
				System.Array.Copy(thisFunc.HdotVAt(x, v, gradFD, testBatchSize), 0, HvFD, 0, HvFD.Length);
				//Compare the difference
				double DiffHv = ArrayMath.Norm_inf(ArrayMath.PairwiseSubtract(Hv, HvFD));
				//Keep track of the biggest H.v error
				if (DiffHv > maxHvDiff)
				{
					maxHvDiff = DiffHv;
				}
			}
			if (maxHvDiff < functionTolerance)
			{
				Sayln(string.Empty);
				Sayln("Success: Hessian approximations lined up");
				ret = true;
			}
			else
			{
				Sayln(string.Empty);
				Sayln("Failure: Hessian approximation at somepoint was off by " + maxHvDiff);
				ret = false;
			}
			thisFunc.sampleMethod = tmpSampleMethod;
			thisFunc.method = tmpMethod;
			return ret;
		}

		/*
		This function is used to get a lower bound on the condition number.  as it stands this is pretty straight forward:
		
		a random point (x) and vector (v) are generated, the Raleigh quotient ( v.H(x).v / v.v ) is then taken which provides both
		a lower bound on the largest eigenvalue, and an upper bound on the smallest eigenvalue.  This can then be used to
		come up with a lower bound on the condition number of the hessian.
		*/
		public virtual double TestConditionNumber(int samples)
		{
			double maxSeen = 0.0;
			double minSeen = 0.0;
			double[] thisV = new double[thisFunc.DomainDimension()];
			double[] thisX = new double[thisV.Length];
			gradFD = new double[thisV.Length];
			HvFD = new double[thisV.Length];
			double thisVHV;
			bool isNeg = false;
			bool isPos = false;
			bool isSemi = false;
			thisFunc.method = StochasticCalculateMethods.ExternalFiniteDifference;
			for (int j = 0; j < samples; j++)
			{
				for (int i = 0; i < thisV.Length; i++)
				{
					thisV[i] = generator.NextDouble();
				}
				for (int i_1 = 0; i_1 < thisX.Length; i_1++)
				{
					thisX[i_1] = generator.NextDouble();
				}
				log.Info("Evaluating Hessian Product");
				System.Array.Copy(thisFunc.DerivativeAt(thisX, thisV, testBatchSize), 0, gradFD, 0, gradFD.Length);
				thisFunc.recalculatePrevBatch = true;
				System.Array.Copy(thisFunc.HdotVAt(thisX, thisV, gradFD, testBatchSize), 0, HvFD, 0, HvFD.Length);
				thisVHV = ArrayMath.InnerProduct(thisV, HvFD);
				if (System.Math.Abs(thisVHV) > maxSeen)
				{
					maxSeen = System.Math.Abs(thisVHV);
				}
				if (System.Math.Abs(thisVHV) < minSeen)
				{
					minSeen = System.Math.Abs(thisVHV);
				}
				if (thisVHV < 0)
				{
					isNeg = true;
				}
				if (thisVHV > 0)
				{
					isPos = true;
				}
				if (thisVHV == 0)
				{
					isSemi = true;
				}
				log.Info("It:" + j + "  C:" + maxSeen / minSeen + "N:" + isNeg + "P:" + isPos + "S:" + isSemi);
			}
			System.Console.Out.WriteLine("Condition Number of: " + maxSeen / minSeen);
			System.Console.Out.WriteLine("Is negative: " + isNeg);
			System.Console.Out.WriteLine("Is positive: " + isPos);
			System.Console.Out.WriteLine("Is semi:     " + isSemi);
			return maxSeen / minSeen;
		}

		public virtual double[] GetVariance(double[] x)
		{
			return GetVariance(x, testBatchSize);
		}

		public virtual double[] GetVariance(double[] x, int batchSize)
		{
			double[] ret = new double[4];
			double[] fullHx = new double[thisFunc.DomainDimension()];
			double[] thisHx = new double[x.Length];
			double[] thisGrad = new double[x.Length];
			IList<double[]> HxList = new List<double[]>();
			/*
			PrintWriter file = null;
			NumberFormat nf = new DecimalFormat("0.000E0");
			
			try{
			file = new PrintWriter(new FileOutputStream("var.out"),true);
			}
			catch (IOException e){
			log.info("Caught IOException outputing List to file: " + e.getMessage());
			System.exit(1);
			}
			*/
			//get the full hessian
			thisFunc.sampleMethod = AbstractStochasticCachingDiffFunction.SamplingMethod.Ordered;
			System.Array.Copy(thisFunc.DerivativeAt(x, x, thisFunc.DataDimension()), 0, thisGrad, 0, thisGrad.Length);
			System.Array.Copy(thisFunc.HdotVAt(x, x, thisGrad, thisFunc.DataDimension()), 0, fullHx, 0, fullHx.Length);
			double fullNorm = ArrayMath.Norm(fullHx);
			double hessScale = ((double)thisFunc.DataDimension()) / ((double)batchSize);
			thisFunc.sampleMethod = AbstractStochasticCachingDiffFunction.SamplingMethod.RandomWithReplacement;
			int n = 100;
			double simDelta;
			double ratDelta;
			double simMean = 0;
			double ratMean = 0;
			double simS = 0;
			double ratS = 0;
			int k = 0;
			log.Info(fullHx[4] + "  " + x[4]);
			for (int i = 0; i < n; i++)
			{
				System.Array.Copy(thisFunc.DerivativeAt(x, x, batchSize), 0, thisGrad, 0, thisGrad.Length);
				System.Array.Copy(thisFunc.HdotVAt(x, x, thisGrad, batchSize), 0, thisHx, 0, thisHx.Length);
				ArrayMath.MultiplyInPlace(thisHx, hessScale);
				double thisNorm = ArrayMath.Norm(thisHx);
				double sim = ArrayMath.InnerProduct(thisHx, fullHx) / (thisNorm * fullNorm);
				double rat = thisNorm / fullNorm;
				k += 1;
				simDelta = sim - simMean;
				simMean += simDelta / k;
				simS += simDelta * (sim - simMean);
				ratDelta = rat - ratMean;
				ratMean += ratDelta / k;
				ratS += ratDelta * (rat - ratMean);
			}
			//file.println( nf.format(sim) + " , " + nf.format(rat));
			double simVar = simS / (k - 1);
			double ratVar = ratS / (k - 1);
			//file.close();
			ret[0] = simMean;
			ret[1] = simVar;
			ret[2] = ratMean;
			ret[3] = ratVar;
			return ret;
		}

		public virtual void TestVariance(double[] x)
		{
			int[] batchSizes = new int[] { 10, 20, 35, 50, 75, 150, 300, 500, 750, 1000, 5000, 10000 };
			double[] varResult;
			PrintWriter file = null;
			NumberFormat nf = new DecimalFormat("0.000E0");
			try
			{
				file = new PrintWriter(new FileOutputStream("var.out"), true);
			}
			catch (IOException e)
			{
				log.Info("Caught IOException outputing List to file: " + e.Message);
				System.Environment.Exit(1);
			}
			foreach (int bSize in batchSizes)
			{
				varResult = GetVariance(x, bSize);
				file.Println(bSize + "," + nf.Format(varResult[0]) + "," + nf.Format(varResult[1]) + "," + nf.Format(varResult[2]) + "," + nf.Format(varResult[3]));
				log.Info("Batch size of: " + bSize + "   " + varResult[0] + "," + nf.Format(varResult[1]) + "," + nf.Format(varResult[2]) + "," + nf.Format(varResult[3]));
			}
			file.Close();
		}

		/*
		public double getNormVariance(List<double[]> thisList){
		double[] ratio = new double[thisList.size()];
		double[] mean = new double[thisList.get(0).length];
		double sizeInv = 1/( (double) thisList.size() );
		
		for(double[] arr:thisList){
		for(int i=0;i<arr.length;i++){
		mean[i] += arr[i]*sizeInv;
		}
		}
		
		double meanNorm = ArrayMath.norm(mean);
		
		for(int i=0;i<thisList.size();i++){
		ratio[i] = (ArrayMath.norm(thisList.get(i))/ meanNorm);
		}
		
		arrayToFile(ratio,"ratio.out");
		
		return ArrayMath.variance(ratio);
		
		}
		
		public double getSimVariance(List<double[]> thisList){
		
		double[] ang = new double[thisList.size()];
		double[] mean = new double[thisList.get(0).length];
		double sizeInv = 1/( (double) thisList.size() );
		
		for(double[] arr:thisList){
		for(int i=0;i<arr.length;i++){
		mean[i] += arr[i]*sizeInv;
		}
		}
		
		double meanNorm = ArrayMath.norm(mean);
		
		for(int i=0;i<thisList.size();i++){
		ang[i] = ArrayMath.innerProduct(thisList.get(i),mean);
		ang[i] = ang[i]/ ( meanNorm * ArrayMath.norm(thisList.get(i)));
		}
		
		arrayToFile(ang,"angle.out");
		
		return ArrayMath.variance(ang);
		}
		*/
		public virtual void ListToFile(IList<double[]> thisList, string fileName)
		{
			PrintWriter file = null;
			NumberFormat nf = new DecimalFormat("0.000E0");
			try
			{
				file = new PrintWriter(new FileOutputStream(fileName), true);
			}
			catch (IOException e)
			{
				log.Info("Caught IOException outputing List to file: " + e.Message);
				System.Environment.Exit(1);
			}
			foreach (double[] element in thisList)
			{
				foreach (double val in element)
				{
					file.Print(nf.Format(val) + "  ");
				}
				file.Println(string.Empty);
			}
			file.Close();
		}

		public virtual void ArrayToFile(double[] thisArray, string fileName)
		{
			PrintWriter file = null;
			NumberFormat nf = new DecimalFormat("0.000E0");
			try
			{
				file = new PrintWriter(new FileOutputStream(fileName), true);
			}
			catch (IOException e)
			{
				log.Info("Caught IOException outputing List to file: " + e.Message);
				System.Environment.Exit(1);
			}
			foreach (double element in thisArray)
			{
				file.Print(nf.Format(element) + "  ");
			}
			file.Close();
		}
		/*
		public boolean testObjectiveFunction(Function function, double[] x, double functionTolerance){
		
		
		
		approxGrad = new double[x.length];
		curGrad = new double[x.length];
		approxValue = 0;
		
		//Generate the initial vectors
		for (int i = 0; i < x.length; i ++){
		approxGrad[i] = 0;
		v[i] = generator.nextDouble() ;
		}
		
		
		//This loop runs through all the batches and sums of the calculations to compare against the full gradient
		for (int i = 0; i < numBatches ; i ++){
		
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		// Perform calculation using IncorporatedFiniteDifference
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		
		dfunction.method = StochasticCalculateMethods.IncorporatedFiniteDifference;
		
		//  update the value
		approxValue += dfunction.valueAt(x,v,testBatchSize);
		
		//  update the gradient
		dfunction.returnPreviousValues = true;
		System.arraycopy(dfunction.derivativeAt(x,v,testBatchSize ), 0,curGrad, 0, curGrad.length);
		
		//  update the Hessian
		dfunction.returnPreviousValues = true;
		System.arraycopy(dfunction.HdotVAt(x,v,testBatchSize),0,HvAD,0,HvAD.length);
		
		//Update Approximate
		approxGrad = ArrayMath.pairwiseAdd(approxGrad,curGrad);
		
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		// Perform calculations using external finite difference
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		
		dfunction.method = StochasticCalculateMethods.ExternalFiniteDifference;
		
		dfunction.recalculatePrevBatch = true;
		System.arraycopy(dfunction.derivativeAt(x,v,testBatchSize ), 0,gradFD, 0, gradFD.length);
		
		dfunction.recalculatePrevBatch = true;
		System.arraycopy(dfunction.HdotVAt(x,v,gradFD,testBatchSize),0,HvFD,0,HvFD.length);
		
		double DiffGrad = ArrayMath.norm_inf(ArrayMath.pairwiseSubtract(gradFD,curGrad));
		
		// Keep track of the biggest error.
		if (DiffGrad > maxGradDiff){maxGradDiff = DiffGrad;}
		
		double DiffHv = ArrayMath.norm_inf(ArrayMath.pairwiseSubtract(HvAD,HvFD));
		
		//Keep track of the biggest H.v error
		if (DiffHv > maxHvDiff){maxHvDiff = DiffHv;}
		}
		
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		// Get the full gradient and value, these should equal the approximates
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		System.arraycopy(dfunction.derivativeAt(x),0,fullGrad,0,fullGrad.length);
		fullValue = dfunction.valueAt(x);
		
		
		if(ArrayMath.norm_inf(ArrayMath.pairwiseSubtract(fullGrad,approxGrad)) < functionTolerance){
		sayln("");
		sayln("  Gradient is looking good");
		}else{
		diff = new double[x.length];
		diff = ArrayMath.pairwiseSubtract(approxGrad,fullGrad);
		diffNorm = ArrayMath.norm(diff);
		sayln("");
		sayln("  Seems there is a problem.  Gradient is off by norm of " + diffNorm);
		};
		
		if( maxGradDiff < functionTolerance ){
		sayln("");
		sayln("  Both gradients are the same");
		}else{
		diffValue = approxValue - fullValue;
		sayln("");
		sayln("  Seems there is a problem.  The two methods of calculating the gradient are different  max |AD-FD|_inf Error of " + maxGradDiff);
		};
		
		
		if( Math.abs(fullValue - approxValue) < functionTolerance){
		sayln("");
		sayln("  Value is looking good");
		}else{
		diffValue = approxValue - fullValue;
		sayln("");
		sayln("  Seems there is a problem.  Value is off by " + diffValue);
		};
		
		if(maxHvDiff < functionTolerance){
		sayln("");
		sayln("  Hv Approimations line up well");
		}else{
		sayln("");
		sayln("    Seems there is a problem.  Hv approximations aren't quite close enough -- max |AD-FD|_inf Error of " + maxHvDiff);
		}
		
		return true;
		}
		*/
	}
}
