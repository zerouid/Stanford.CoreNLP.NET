using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>A general class that scores precision, recall, and F1.</summary>
	/// <remarks>
	/// A general class that scores precision, recall, and F1. The package supports incremental
	/// computation of these statistics via the
	/// <see cref="Update(double, double, double, double)"/>
	/// method. Formatted output reflecting
	/// the current values of the statistics may be obtained via the
	/// <see cref="ToString()"/>
	/// method.
	/// </remarks>
	/// <seealso cref="Evalb"/>
	/// <author>Spence Green</author>
	public class EvaluationMetric
	{
		private double numTestInstances = 0.0;

		private double exact = 0.0;

		private double precisions = 0.0;

		private double precisions2 = 0.0;

		private double recalls = 0.0;

		private double recalls2 = 0.0;

		private double pnums2 = 0.0;

		private double rnums2 = 0.0;

		private double f1s = 0.0;

		/// <summary>Updates the evaluation statistics.</summary>
		/// <remarks>
		/// Updates the evaluation statistics. Should be called once for each test example
		/// (e.g., sentence, parse tree, etc.).
		/// </remarks>
		/// <param name="curP">Precision of the current test example</param>
		/// <param name="curPnum">The denominator used to calculate the current precision</param>
		/// <param name="curR">Recall of the current test example</param>
		/// <param name="curRnum">The denominator used to calculate the current recall</param>
		public virtual void Update(double curP, double curPnum, double curR, double curRnum)
		{
			numTestInstances += 1.0;
			double curF1 = (curP > 0.0 && curR > 0.0) ? 2.0 / ((1.0 / curP) + (1.0 / curR)) : 0.0;
			if (curF1 >= 0.9999)
			{
				exact += 1.0;
			}
			precisions += curP;
			recalls += curR;
			f1s += curF1;
			precisions2 += curPnum * curP;
			pnums2 += curPnum;
			recalls2 += curRnum * curR;
			rnums2 += curRnum;
			//Update for the toString() method to be called during running average output
			lastP = curP;
			lastR = curR;
			lastF1 = curF1;
		}

		/// <summary>Returns the components of the precision.</summary>
		/// <returns>
		/// A
		/// <see cref="Edu.Stanford.Nlp.Util.Pair{T1, T2}"/>
		/// with the numerator of the precision in the first element
		/// and the denominator of the precision in the second element.
		/// </returns>
		public virtual Pair<double, double> GetPFractionals()
		{
			return new Pair<double, double>(precisions2, pnums2);
		}

		/// <summary>Returns the components of the recall.</summary>
		/// <returns>
		/// A
		/// <see cref="Edu.Stanford.Nlp.Util.Pair{T1, T2}"/>
		/// with the numerator of the recall in the first element
		/// and the denominator of the recall in the second element.
		/// </returns>
		public virtual Pair<double, double> GetRFractionals()
		{
			return new Pair<double, double>(recalls2, rnums2);
		}

		/// <summary>
		/// Returns the number of test instances (e.g., parse trees or sentences) used in
		/// the calculation of the statistics.
		/// </summary>
		/// <remarks>
		/// Returns the number of test instances (e.g., parse trees or sentences) used in
		/// the calculation of the statistics. This value corresponds to the number of calls
		/// to
		/// <see cref="Update(double, double, double, double)"/>
		/// .
		/// </remarks>
		/// <returns>The number of test instances</returns>
		public virtual double GetTestInstances()
		{
			return numTestInstances;
		}

		/// <summary>
		/// A convenience method that returns the number of true positive examples from
		/// among the test instances.
		/// </summary>
		/// <remarks>
		/// A convenience method that returns the number of true positive examples from
		/// among the test instances. Mathematically, this value is the denominator of the recall.
		/// </remarks>
		/// <returns>Number of true positive examples</returns>
		public virtual double NumRelevantExamples()
		{
			return rnums2;
		}

		private double lastP = 0.0;

		private double lastR = 0.0;

		private double lastF1 = 0.0;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			double pSent = (numTestInstances > 0.0) ? precisions / numTestInstances : 0.0;
			double pEvalB = (pnums2 > 0.0) ? precisions2 / pnums2 : 0.0;
			sb.Append(string.Format("P: %.2f (sent ave: %.2f) (evalb: %.2f)%n", lastP * 100.0, pSent * 100.0, pEvalB * 100.0));
			double rSent = (numTestInstances > 0.0) ? recalls / numTestInstances : 0.0;
			double rEvalB = (rnums2 > 0.0) ? recalls2 / rnums2 : 0.0;
			sb.Append(string.Format("R: %.2f (sent ave: %.2f) (evalb: %.2f)%n", lastR * 100.0, rSent * 100.0, rEvalB * 100.0));
			double f1Sent = (numTestInstances > 0.0) ? f1s / numTestInstances : 0.0;
			double f1EvalB = (pEvalB > 0.0 && rEvalB > 0.0) ? 2.0 / ((1.0 / pEvalB) + (1.0 / rEvalB)) : 0.0;
			sb.Append(string.Format("F1: %.2f (sent ave: %.2f) (evalb: %.2f)%n", lastF1 * 100.0, f1Sent * 100.0, f1EvalB * 100.0));
			sb.Append(string.Format("Num:\t%.2f (test instances)%n", numTestInstances));
			sb.Append(string.Format("Rel:\t%.0f (relevant examples)%n", rnums2));
			sb.Append(string.Format("Exact:\t%.2f (test instances)%n", exact));
			return sb.ToString();
		}
	}
}
