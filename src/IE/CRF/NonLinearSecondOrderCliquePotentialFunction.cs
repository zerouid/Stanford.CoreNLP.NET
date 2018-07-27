using System;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;


namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class NonLinearSecondOrderCliquePotentialFunction : ICliquePotentialFunction
	{
		private readonly double[][] inputLayerWeights4Edge;

		private readonly double[][] outputLayerWeights4Edge;

		private readonly double[][] inputLayerWeights;

		private readonly double[][] outputLayerWeights;

		private double[] layerOneCache;

		private double[] hiddenLayerCache;

		private double[] layerOneCache4Edge;

		private double[] hiddenLayerCache4Edge;

		private readonly SeqClassifierFlags flags;

		public NonLinearSecondOrderCliquePotentialFunction(double[][] inputLayerWeights4Edge, double[][] outputLayerWeights4Edge, double[][] inputLayerWeights, double[][] outputLayerWeights, SeqClassifierFlags flags)
		{
			// first index is number of hidden units in layer one, second index is the input feature indices
			// first index is the output class, second index is the number of hidden units
			// first index is number of hidden units in layer one, second index is the input feature indices
			// first index is the output class, second index is the number of hidden units
			this.inputLayerWeights4Edge = inputLayerWeights4Edge;
			this.outputLayerWeights4Edge = outputLayerWeights4Edge;
			this.inputLayerWeights = inputLayerWeights;
			this.outputLayerWeights = outputLayerWeights;
			this.flags = flags;
		}

		public virtual double[] HiddenLayerOutput(double[][] inputLayerWeights, int[] nodeCliqueFeatures, SeqClassifierFlags aFlag, double[] featureVal, int cliqueSize)
		{
			double[] layerCache = null;
			double[] hlCache = null;
			int layerOneSize = inputLayerWeights.Length;
			if (cliqueSize > 1)
			{
				if (layerOneCache4Edge == null || layerOneSize != layerOneCache4Edge.Length)
				{
					layerOneCache4Edge = new double[layerOneSize];
				}
				layerCache = layerOneCache4Edge;
			}
			else
			{
				if (layerOneCache == null || layerOneSize != layerOneCache.Length)
				{
					layerOneCache = new double[layerOneSize];
				}
				layerCache = layerOneCache;
			}
			for (int i = 0; i < layerOneSize; i++)
			{
				double[] ws = inputLayerWeights[i];
				double lOneW = 0;
				double dotProd = 0;
				for (int m = 0; m < nodeCliqueFeatures.Length; m++)
				{
					dotProd = ws[nodeCliqueFeatures[m]];
					if (featureVal != null)
					{
						dotProd *= featureVal[m];
					}
					lOneW += dotProd;
				}
				layerCache[i] = lOneW;
			}
			if (!aFlag.useHiddenLayer)
			{
				return layerCache;
			}
			// transform layer one through hidden
			if (cliqueSize > 1)
			{
				if (hiddenLayerCache4Edge == null || layerOneSize != hiddenLayerCache4Edge.Length)
				{
					hiddenLayerCache4Edge = new double[layerOneSize];
				}
				hlCache = hiddenLayerCache4Edge;
			}
			else
			{
				if (hiddenLayerCache == null || layerOneSize != hiddenLayerCache.Length)
				{
					hiddenLayerCache = new double[layerOneSize];
				}
				hlCache = hiddenLayerCache;
			}
			for (int i_1 = 0; i_1 < layerOneSize; i_1++)
			{
				if (aFlag.useSigmoid)
				{
					hlCache[i_1] = Sigmoid(layerCache[i_1]);
				}
				else
				{
					hlCache[i_1] = Math.Tanh(layerCache[i_1]);
				}
			}
			return hlCache;
		}

		private static double Sigmoid(double x)
		{
			return 1 / (1 + Math.Exp(-x));
		}

		public virtual double ComputeCliquePotential(int cliqueSize, int labelIndex, int[] cliqueFeatures, double[] featureVal, int posInSent)
		{
			double output = 0.0;
			double[][] inputWeights;
			double[][] outputWeights = null;
			if (cliqueSize > 1)
			{
				inputWeights = inputLayerWeights4Edge;
				outputWeights = outputLayerWeights4Edge;
			}
			else
			{
				inputWeights = inputLayerWeights;
				outputWeights = outputLayerWeights;
			}
			double[] hiddenLayer = HiddenLayerOutput(inputWeights, cliqueFeatures, flags, featureVal, cliqueSize);
			int outputLayerSize = inputWeights.Length / outputWeights[0].Length;
			// transform the hidden layer to output layer through linear transformation
			if (flags.useOutputLayer)
			{
				double[] outputWs = null;
				if (flags.tieOutputLayer)
				{
					outputWs = outputWeights[0];
				}
				else
				{
					outputWs = outputWeights[labelIndex];
				}
				if (flags.softmaxOutputLayer)
				{
					outputWs = ArrayMath.Softmax(outputWs);
				}
				for (int i = 0; i < inputWeights.Length; i++)
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
			return output;
		}
	}
}
