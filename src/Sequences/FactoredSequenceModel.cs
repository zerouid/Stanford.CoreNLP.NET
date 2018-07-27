using System;
using Edu.Stanford.Nlp.Math;


namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>
	/// grenager
	/// Date: Dec 14, 2004
	/// </author>
	/// <author>
	/// nmramesh
	/// Date: May 12, 2010
	/// </author>
	public class FactoredSequenceModel : ISequenceModel
	{
		private ISequenceModel model1;

		private ISequenceModel model2;

		private double model1Wt = 1.0;

		private double model2Wt = 1.0;

		private ISequenceModel[] models = null;

		private double[] wts = null;

		// todo: The current version has variables for a 2 model version and arrays for an n-model version.  Unify.
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double[] ScoresOf(int[] sequence, int pos)
		{
			if (models != null)
			{
				double[] dist = ArrayMath.Multiply(models[0].ScoresOf(sequence, pos), wts[0]);
				for (int i = 1; i < models.Length; i++)
				{
					double[] dist_i = models[i].ScoresOf(sequence, pos);
					ArrayMath.AddMultInPlace(dist, dist_i, wts[i]);
				}
				return dist;
			}
			double[] dist1 = model1.ScoresOf(sequence, pos);
			double[] dist2 = model2.ScoresOf(sequence, pos);
			double[] dist_1 = new double[dist1.Length];
			for (int i_1 = 0; i_1 < dist1.Length; i_1++)
			{
				dist_1[i_1] = model1Wt * dist1[i_1] + model2Wt * dist2[i_1];
			}
			return dist_1;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double ScoreOf(int[] sequence, int pos)
		{
			return ScoresOf(sequence, pos)[sequence[pos]];
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double ScoreOf(int[] sequence)
		{
			if (models != null)
			{
				double score = 0;
				for (int i = 0; i < models.Length; i++)
				{
					score += wts[i] * models[i].ScoreOf(sequence);
				}
				return score;
			}
			//return model1.scoreOf(sequence);
			return model1Wt * model1.ScoreOf(sequence) + model2Wt * model2.ScoreOf(sequence);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int Length()
		{
			if (models != null)
			{
				return models[0].Length();
			}
			return model1.Length();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int LeftWindow()
		{
			if (models != null)
			{
				return models[0].LeftWindow();
			}
			return model1.LeftWindow();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int RightWindow()
		{
			if (models != null)
			{
				return models[0].RightWindow();
			}
			return model1.RightWindow();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int[] GetPossibleValues(int position)
		{
			if (models != null)
			{
				return models[0].GetPossibleValues(position);
			}
			return model1.GetPossibleValues(position);
		}

		/// <summary>using this constructor results in a weighted addition of the two models' scores.</summary>
		/// <param name="model1"/>
		/// <param name="model2"/>
		/// <param name="wt1">weight of model1</param>
		/// <param name="wt2">weight of model2</param>
		public FactoredSequenceModel(ISequenceModel model1, ISequenceModel model2, double wt1, double wt2)
			: this(model1, model2)
		{
			this.model1Wt = wt1;
			this.model2Wt = wt2;
		}

		public FactoredSequenceModel(ISequenceModel model1, ISequenceModel model2)
		{
			//if (model1.leftWindow() != model2.leftWindow()) throw new RuntimeException("Two models must have same window size");
			if (model1.GetPossibleValues(0).Length != model2.GetPossibleValues(0).Length)
			{
				throw new Exception("Two models must have the same number of classes");
			}
			if (model1.Length() != model2.Length())
			{
				throw new Exception("Two models must have the same sequence length");
			}
			this.model1 = model1;
			this.model2 = model2;
		}

		public FactoredSequenceModel(ISequenceModel[] models, double[] weights)
		{
			this.models = models;
			this.wts = weights;
		}
		/*
		for(int i = 1; i < models.length; i++){
		if (models[0].getPossibleValues(0).length != models[i].getPossibleValues(0).length) throw new RuntimeException("All models must have the same number of classes");
		if(models[0].length() != models[i].length())
		throw new RuntimeException("All models must have the same sequence length");
		
		}
		*/
	}
}
