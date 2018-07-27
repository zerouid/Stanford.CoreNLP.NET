using System;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;


namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class NonLinearCliquePotentialFunction : ICliquePotentialFunction
	{
		private readonly double[][] linearWeights;

		private readonly double[][] inputLayerWeights;

		private readonly double[][] outputLayerWeights;

		private readonly SeqClassifierFlags flags;

		private double[] layerOneCache;

		private double[] hiddenLayerCache;

		// first index is number of hidden units in layer one, second index is the input feature indices
		// first index is the output class, second index is the number of hidden units
		private static double Sigmoid(double x)
		{
			return 1 / (1 + Math.Exp(-x));
		}

		public NonLinearCliquePotentialFunction(double[][] linearWeights, double[][] inputLayerWeights, double[][] outputLayerWeights, SeqClassifierFlags flags)
		{
			this.linearWeights = linearWeights;
			this.inputLayerWeights = inputLayerWeights;
			this.outputLayerWeights = outputLayerWeights;
			this.flags = flags;
		}

		public virtual double[] HiddenLayerOutput(double[][] inputLayerWeights, int[] nodeCliqueFeatures, SeqClassifierFlags aFlag, double[] featureVal)
		{
			int layerOneSize = inputLayerWeights.Length;
			if (layerOneCache == null || layerOneSize != layerOneCache.Length)
			{
				layerOneCache = new double[layerOneSize];
			}
			for (int i = 0; i < layerOneSize; i++)
			{
				double[] ws = inputLayerWeights[i];
				double lOneW = 0;
				for (int m = 0; m < nodeCliqueFeatures.Length; m++)
				{
					double dotProd = ws[nodeCliqueFeatures[m]];
					if (featureVal != null)
					{
						dotProd *= featureVal[m];
					}
					lOneW += dotProd;
				}
				layerOneCache[i] = lOneW;
			}
			if (!aFlag.useHiddenLayer)
			{
				return layerOneCache;
			}
			// transform layer one through hidden
			if (hiddenLayerCache == null || layerOneSize != hiddenLayerCache.Length)
			{
				hiddenLayerCache = new double[layerOneSize];
			}
			for (int i_1 = 0; i_1 < layerOneSize; i_1++)
			{
				if (aFlag.useSigmoid)
				{
					hiddenLayerCache[i_1] = Sigmoid(layerOneCache[i_1]);
				}
				else
				{
					hiddenLayerCache[i_1] = Math.Tanh(layerOneCache[i_1]);
				}
			}
			return hiddenLayerCache;
		}

		public virtual double ComputeCliquePotential(int cliqueSize, int labelIndex, int[] cliqueFeatures, double[] featureVal, int posInSent)
		{
			double output = 0.0;
			if (cliqueSize > 1)
			{
				// linear potential for edge cliques
				foreach (int cliqueFeature in cliqueFeatures)
				{
					output += linearWeights[cliqueFeature][labelIndex];
				}
			}
			else
			{
				// non-linear potential for node cliques
				double[] hiddenLayer = HiddenLayerOutput(inputLayerWeights, cliqueFeatures, flags, featureVal);
				int outputLayerSize = inputLayerWeights.Length / outputLayerWeights[0].Length;
				// transform the hidden layer to output layer through linear transformation
				if (flags.useOutputLayer)
				{
					double[] outputWs;
					// initialized immediately below
					if (flags.tieOutputLayer)
					{
						outputWs = outputLayerWeights[0];
					}
					else
					{
						outputWs = outputLayerWeights[labelIndex];
					}
					if (flags.softmaxOutputLayer)
					{
						outputWs = ArrayMath.Softmax(outputWs);
					}
					for (int i = 0; i < inputLayerWeights.Length; i++)
					{
						if (flags.sparseOutputLayer || flags.tieOutputLayer)
						{
							if (i % outputLayerSize == labelIndex)
							{
								output += outputWs[i / outputLayerSize] * hiddenLayer[i];
							}
						}
						else
						{
							output += outputWs[i] * hiddenLayer[i];
						}
					}
				}
				else
				{
					output = hiddenLayer[labelIndex];
				}
			}
			return output;
		}
	}
}
