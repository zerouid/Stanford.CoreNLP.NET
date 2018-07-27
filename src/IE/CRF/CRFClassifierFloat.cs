// CRFClassifier -- a probabilistic (CRF) sequence model, mainly used for NER.
// Copyright (c) 2002-2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
using System;
using System.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Subclass of CRFClassifier that performs dropout feature-noisying training</summary>
	/// <author>Mengqiu Wang</author>
	public class CRFClassifierFloat<In> : CRFClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierFloat));

		protected internal CRFClassifierFloat()
			: base(new SeqClassifierFlags())
		{
		}

		public CRFClassifierFloat(Properties props)
			: base(props)
		{
		}

		public CRFClassifierFloat(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		protected internal override double[] TrainWeights(int[][][][] data, int[][] labels, IEvaluator[] evaluators, int pruneFeatureItr, double[][][][] featureVals)
		{
			CRFLogConditionalObjectiveFloatFunction func = new CRFLogConditionalObjectiveFloatFunction(data, labels, windowSize, classIndex, labelIndices, map, flags.backgroundSymbol, flags.sigma);
			cliquePotentialFunctionHelper = func;
			QNMinimizer minimizer;
			if (flags.interimOutputFreq != 0)
			{
				IFloatFunction monitor = new ResultStoringFloatMonitor(flags.interimOutputFreq, flags.serializeTo);
				minimizer = new QNMinimizer(monitor);
			}
			else
			{
				minimizer = new QNMinimizer();
			}
			if (pruneFeatureItr == 0)
			{
				minimizer.SetM(flags.QNsize);
			}
			else
			{
				minimizer.SetM(flags.QNsize2);
			}
			float[] initialWeights;
			if (flags.initialWeights == null)
			{
				initialWeights = func.Initial();
			}
			else
			{
				try
				{
					log.Info("Reading initial weights from file " + flags.initialWeights);
					using (DataInputStream dis = new DataInputStream(new BufferedInputStream(new GZIPInputStream(new FileInputStream(flags.initialWeights)))))
					{
						initialWeights = ConvertByteArray.ReadFloatArr(dis);
					}
				}
				catch (IOException)
				{
					throw new Exception("Could not read from float initial weight file " + flags.initialWeights);
				}
			}
			log.Info("numWeights: " + initialWeights.Length);
			float[] weightsArray = minimizer.Minimize(func, (float)flags.tolerance, initialWeights);
			return ArrayMath.FloatArrayToDoubleArray(weightsArray);
		}
	}
}
