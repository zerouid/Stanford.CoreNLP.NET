using System.Collections.Generic;
using Edu.Stanford.Nlp.Neural;
using MathNet.Numerics;


namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>
	/// Stores the weights and implements the matrix operations used by a
	/// <see cref="NeuralCorefAlgorithm"/>
	/// </summary>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class NeuralCorefModel
	{
		private const long serialVersionUID = 2139427931784505653L;

		private readonly SimpleMatrix antecedentMatrix;

		private readonly SimpleMatrix anaphorMatrix;

		private readonly SimpleMatrix pairFeaturesMatrix;

		private readonly SimpleMatrix pairwiseFirstLayerBias;

		private readonly IList<SimpleMatrix> anaphoricityModel;

		private readonly IList<SimpleMatrix> pairwiseModel;

		private readonly Embedding wordEmbeddings;

		public NeuralCorefModel(SimpleMatrix antecedentMatrix, SimpleMatrix anaphorMatrix, SimpleMatrix pairFeaturesMatrix, SimpleMatrix pairwiseFirstLayerBias, IList<SimpleMatrix> anaphoricityModel, IList<SimpleMatrix> pairwiseModel, Embedding wordEmbeddings
			)
		{
			this.antecedentMatrix = antecedentMatrix;
			this.anaphorMatrix = anaphorMatrix;
			this.pairFeaturesMatrix = pairFeaturesMatrix;
			this.pairwiseFirstLayerBias = pairwiseFirstLayerBias;
			this.anaphoricityModel = anaphoricityModel;
			this.pairwiseModel = pairwiseModel;
			this.wordEmbeddings = wordEmbeddings;
		}

		public virtual double GetAnaphoricityScore(SimpleMatrix mentionEmbedding, SimpleMatrix anaphoricityFeatures)
		{
			return Score(NeuralUtils.Concatenate(mentionEmbedding, anaphoricityFeatures), anaphoricityModel);
		}

		public virtual double GetPairwiseScore(SimpleMatrix antecedentEmbedding, SimpleMatrix anaphorEmbedding, SimpleMatrix pairFeatures)
		{
			SimpleMatrix firstLayerOutput = NeuralUtils.ElementwiseApplyReLU(antecedentEmbedding.Plus(anaphorEmbedding).Plus(pairFeaturesMatrix.Mult(pairFeatures)).Plus(pairwiseFirstLayerBias));
			return Score(firstLayerOutput, pairwiseModel);
		}

		private static double Score(SimpleMatrix features, IList<SimpleMatrix> weights)
		{
			for (int i = 0; i < weights.Count; i += 2)
			{
				features = weights[i].Mult(features).Plus(weights[i + 1]);
				if (weights[i].NumRows() > 1)
				{
					features = NeuralUtils.ElementwiseApplyReLU(features);
				}
			}
			return features.ElementSum();
		}

		public virtual SimpleMatrix GetAnaphorEmbedding(SimpleMatrix mentionEmbedding)
		{
			return anaphorMatrix.Mult(mentionEmbedding);
		}

		public virtual SimpleMatrix GetAntecedentEmbedding(SimpleMatrix mentionEmbedding)
		{
			return antecedentMatrix.Mult(mentionEmbedding);
		}

		public virtual Embedding GetWordEmbeddings()
		{
			return wordEmbeddings;
		}
	}
}
