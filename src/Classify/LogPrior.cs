using System;
using Edu.Stanford.Nlp.Math;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A Prior for functions.</summary>
	/// <remarks>A Prior for functions.  Immutable.</remarks>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class LogPrior
	{
		private const long serialVersionUID = 7826853908892790965L;

		public enum LogPriorType
		{
			Null,
			Quadratic,
			Huber,
			Quartic,
			Cosh,
			Adapt,
			MultipleQuadratic
		}

		public static LogPrior.LogPriorType GetType(string name)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(name, "null"))
			{
				return LogPrior.LogPriorType.Null;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(name, "quadratic"))
				{
					return LogPrior.LogPriorType.Quadratic;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(name, "huber"))
					{
						return LogPrior.LogPriorType.Huber;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(name, "quartic"))
						{
							return LogPrior.LogPriorType.Quartic;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(name, "cosh"))
							{
								return LogPrior.LogPriorType.Cosh;
							}
							else
							{
								//    else if (name.equalsIgnoreCase("multiple")) { return LogPriorType.MULTIPLE; }
								throw new Exception("Unknown LogPriorType: " + name);
							}
						}
					}
				}
			}
		}

		private double[] means = null;

		private Edu.Stanford.Nlp.Classify.LogPrior otherPrior = null;

		// these fields are just for the ADAPT prior -
		// is there a better way to do this?
		public static Edu.Stanford.Nlp.Classify.LogPrior GetAdaptationPrior(double[] means, Edu.Stanford.Nlp.Classify.LogPrior otherPrior)
		{
			Edu.Stanford.Nlp.Classify.LogPrior lp = new Edu.Stanford.Nlp.Classify.LogPrior(LogPrior.LogPriorType.Adapt);
			lp.means = means;
			lp.otherPrior = otherPrior;
			return lp;
		}

		public virtual LogPrior.LogPriorType GetType()
		{
			return type;
		}

		private readonly LogPrior.LogPriorType type;

		public LogPrior()
			: this(LogPrior.LogPriorType.Quadratic)
		{
		}

		public LogPrior(int intPrior)
			: this(intPrior, 1.0, 0.1)
		{
		}

		public LogPrior(LogPrior.LogPriorType type)
			: this(type, 1.0, 0.1)
		{
		}

		// why isn't this functionality in enum?
		private static LogPrior.LogPriorType IntToType(int intPrior)
		{
			LogPrior.LogPriorType[] values = LogPrior.LogPriorType.Values();
			foreach (LogPrior.LogPriorType val in values)
			{
				if ((int)(val) == intPrior)
				{
					return val;
				}
			}
			throw new ArgumentException(intPrior + " is not a legal LogPrior.");
		}

		public LogPrior(int intPrior, double sigma, double epsilon)
			: this(IntToType(intPrior), sigma, epsilon)
		{
		}

		public LogPrior(LogPrior.LogPriorType type, double sigma, double epsilon)
		{
			this.type = type;
			if (type != LogPrior.LogPriorType.Adapt)
			{
				SetSigma(sigma);
				SetEpsilon(epsilon);
			}
		}

		private double[] sigmaSqM = null;

		private double[] sigmaQuM = null;

		/// <summary>
		/// IMPORTANT NOTE: This constructor allows non-uniform regularization, but it
		/// transforms the inputs C (like the machine learning people like) to sigma
		/// (like we NLP folks like).
		/// </summary>
		/// <remarks>
		/// IMPORTANT NOTE: This constructor allows non-uniform regularization, but it
		/// transforms the inputs C (like the machine learning people like) to sigma
		/// (like we NLP folks like).  C = 1/\sigma^2
		/// </remarks>
		public LogPrior(double[] C)
		{
			// this is the C variable in CSFoo's MM paper C = 1/\sigma^2
			//  private double[] regularizationHyperparameters = null;
			//  public double[] getRegularizationHyperparameters() {
			//    return regularizationHyperparameters;
			//  }
			//
			//  public void setRegularizationHyperparameters(
			//      double[] regularizationHyperparameters) {
			//    this.regularizationHyperparameters = regularizationHyperparameters;
			//  }
			this.type = LogPrior.LogPriorType.MultipleQuadratic;
			double[] sigmaSqM = new double[C.Length];
			for (int i = 0; i < C.Length; i++)
			{
				sigmaSqM[i] = 1.0 / C[i];
			}
			this.sigmaSqM = sigmaSqM;
			SetSigmaSquaredM(sigmaSqM);
		}

		private double sigmaSq;

		private double sigmaQu;

		private double epsilon;

		//    this.regularizationHyperparameters = regularizationHyperparameters;
		//  private double sigma;
		public virtual double GetSigma()
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				return otherPrior.GetSigma();
			}
			else
			{
				return Math.Sqrt(sigmaSq);
			}
		}

		public virtual double GetSigmaSquared()
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				return otherPrior.GetSigmaSquared();
			}
			else
			{
				return sigmaSq;
			}
		}

		public virtual double[] GetSigmaSquaredM()
		{
			if (type == LogPrior.LogPriorType.MultipleQuadratic)
			{
				return sigmaSqM;
			}
			else
			{
				throw new Exception("LogPrior.getSigmaSquaredM is undefined for any prior but MULTIPLE_QUADRATIC" + this);
			}
		}

		public virtual double GetEpsilon()
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				return otherPrior.GetEpsilon();
			}
			else
			{
				return epsilon;
			}
		}

		public virtual void SetSigma(double sigma)
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				otherPrior.SetSigma(sigma);
			}
			else
			{
				//    this.sigma = sigma;
				this.sigmaSq = sigma * sigma;
				this.sigmaQu = sigmaSq * sigmaSq;
			}
		}

		//  public void setSigmaM(double[] sigmaM) {
		//    if (type == LogPriorType.MULTIPLE_QUADRATIC) {
		//      //    this.sigma = Math.sqrt(sigmaSq);
		//      double[] sigmaSqM = new double[sigmaM.length];
		//      double[] sigmaQuM = new double[sigmaM.length];
		//
		//      for (int i = 0;i<sigmaM.length;i++){
		//        sigmaSqM[i] = sigmaM[i] * sigmaM[i];
		//      }
		//      this.sigmaSqM = sigmaSqM;
		//
		//      for (int i = 0;i<sigmaSqM.length;i++){
		//        sigmaQuM[i] = sigmaSqM[i] * sigmaSqM[i];
		//      }
		//      this.sigmaQuM = sigmaQuM;
		//
		//    } else {
		//      throw new RuntimeException("LogPrior.getSigmaSquaredM is undefined for any prior but MULTIPLE_QUADRATIC" + this);
		//    }
		//  }
		public virtual void SetSigmaSquared(double sigmaSq)
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				otherPrior.SetSigmaSquared(sigmaSq);
			}
			else
			{
				//    this.sigma = Math.sqrt(sigmaSq);
				this.sigmaSq = sigmaSq;
				this.sigmaQu = sigmaSq * sigmaSq;
			}
		}

		public virtual void SetSigmaSquaredM(double[] sigmaSq)
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				otherPrior.SetSigmaSquaredM(sigmaSq);
			}
			if (type == LogPrior.LogPriorType.MultipleQuadratic)
			{
				//    this.sigma = Math.sqrt(sigmaSq);
				this.sigmaSqM = sigmaSq.MemberwiseClone();
				double[] sigmaQuM = new double[sigmaSq.Length];
				for (int i = 0; i < sigmaSq.Length; i++)
				{
					sigmaQuM[i] = sigmaSqM[i] * sigmaSqM[i];
				}
				this.sigmaQuM = sigmaQuM;
			}
			else
			{
				throw new Exception("LogPrior.getSigmaSquaredM is undefined for any prior but MULTIPLE_QUADRATIC" + this);
			}
		}

		public virtual void SetEpsilon(double epsilon)
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				otherPrior.SetEpsilon(epsilon);
			}
			else
			{
				this.epsilon = epsilon;
			}
		}

		public virtual double ComputeStochastic(double[] x, double[] grad, double fractionOfData)
		{
			if (type == LogPrior.LogPriorType.Adapt)
			{
				double[] newX = ArrayMath.PairwiseSubtract(x, means);
				return otherPrior.ComputeStochastic(newX, grad, fractionOfData);
			}
			else
			{
				if (type == LogPrior.LogPriorType.MultipleQuadratic)
				{
					double[] sigmaSquaredOld = GetSigmaSquaredM();
					double[] sigmaSquaredTemp = sigmaSquaredOld.MemberwiseClone();
					for (int i = 0; i < x.Length; i++)
					{
						sigmaSquaredTemp[i] /= fractionOfData;
					}
					SetSigmaSquaredM(sigmaSquaredTemp);
					double val = Compute(x, grad);
					SetSigmaSquaredM(sigmaSquaredOld);
					return val;
				}
				else
				{
					double sigmaSquaredOld = GetSigmaSquared();
					SetSigmaSquared(sigmaSquaredOld / fractionOfData);
					double val = Compute(x, grad);
					SetSigmaSquared(sigmaSquaredOld);
					return val;
				}
			}
		}

		/// <summary>
		/// Adjust the given grad array by adding the prior's gradient component
		/// and return the value of the logPrior
		/// </summary>
		/// <param name="x">the input point</param>
		/// <param name="grad">the gradient array</param>
		/// <returns>the value</returns>
		public virtual double Compute(double[] x, double[] grad)
		{
			double val = 0.0;
			switch (type)
			{
				case LogPrior.LogPriorType.Null:
				{
					return val;
				}

				case LogPrior.LogPriorType.Quadratic:
				{
					for (int i = 0; i < x.Length; i++)
					{
						val += x[i] * x[i] / 2.0 / sigmaSq;
						grad[i] += x[i] / sigmaSq;
					}
					return val;
				}

				case LogPrior.LogPriorType.Huber:
				{
					// P.J. Huber. 1973. Robust regression: Asymptotics, conjectures and
					// Monte Carlo. The Annals of Statistics 1: 799-821.
					// See also:
					// P. J. Huber. Robust Statistics. John Wiley & Sons, New York, 1981.
					for (int i_1 = 0; i_1 < x.Length; i_1++)
					{
						if (x[i_1] < -epsilon)
						{
							val += (-x[i_1] - epsilon / 2.0) / sigmaSq;
							grad[i_1] += -1.0 / sigmaSq;
						}
						else
						{
							if (x[i_1] < epsilon)
							{
								val += x[i_1] * x[i_1] / 2.0 / epsilon / sigmaSq;
								grad[i_1] += x[i_1] / epsilon / sigmaSq;
							}
							else
							{
								val += (x[i_1] - epsilon / 2.0) / sigmaSq;
								grad[i_1] += 1.0 / sigmaSq;
							}
						}
					}
					return val;
				}

				case LogPrior.LogPriorType.Quartic:
				{
					for (int i_2 = 0; i_2 < x.Length; i_2++)
					{
						val += (x[i_2] * x[i_2]) * (x[i_2] * x[i_2]) / 2.0 / sigmaQu;
						grad[i_2] += x[i_2] / sigmaQu;
					}
					return val;
				}

				case LogPrior.LogPriorType.Adapt:
				{
					double[] newX = ArrayMath.PairwiseSubtract(x, means);
					val += otherPrior.Compute(newX, grad);
					return val;
				}

				case LogPrior.LogPriorType.Cosh:
				{
					double norm = ArrayMath.Norm_1(x) / sigmaSq;
					double d;
					if (norm > 30.0)
					{
						val = norm - System.Math.Log(2);
						d = 1.0 / sigmaSq;
					}
					else
					{
						val = System.Math.Log(System.Math.Cosh(norm));
						d = (2 * (1 / (System.Math.Exp(-2.0 * norm) + 1)) - 1.0) / sigmaSq;
					}
					for (int i_3 = 0; i_3 < x.Length; i_3++)
					{
						grad[i_3] += System.Math.Signum(x[i_3]) * d;
					}
					return val;
				}

				case LogPrior.LogPriorType.MultipleQuadratic:
				{
					//        for (int i = 0; i < x.length; i++) {
					//          val += x[i] * x[i]* 1/2 * regularizationHyperparameters[i];
					//          grad[i] += x[i] * regularizationHyperparameters[i];
					//        }
					for (int i_4 = 0; i_4 < x.Length; i_4++)
					{
						val += x[i_4] * x[i_4] / 2.0 / sigmaSqM[i_4];
						grad[i_4] += x[i_4] / sigmaSqM[i_4];
					}
					return val;
				}

				default:
				{
					throw new Exception("LogPrior.valueAt is undefined for prior of type " + this);
				}
			}
		}
	}
}
