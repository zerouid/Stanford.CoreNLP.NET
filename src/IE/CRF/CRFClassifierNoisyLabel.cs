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
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Subclass of CRFClassifier for modeling noisy label</summary>
	/// <author>Mengqiu Wang</author>
	public class CRFClassifierNoisyLabel<In> : CRFClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierNoisyLabel));

		protected internal double[][] errorMatrix;

		public CRFClassifierNoisyLabel(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		internal static double[][] ReadErrorMatrix(string fileName, IIndex<string> tagIndex, bool useLogProb)
		{
			int numTags = tagIndex.Size();
			int matrixSize = numTags;
			string[] matrixLines = new string[matrixSize];
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(new File(fileName))));
				string line = null;
				int lineCount = 0;
				while ((line = br.ReadLine()) != null)
				{
					line = line.Trim();
					matrixLines[lineCount] = line;
					lineCount++;
				}
			}
			catch (Exception ex)
			{
				Sharpen.Runtime.PrintStackTrace(ex);
				System.Environment.Exit(-1);
			}
			double[][] matrix = ParseMatrix(matrixLines, tagIndex, matrixSize, false, useLogProb);
			log.Info("Error Matrix P(Observed|Truth): ");
			log.Info(ArrayUtils.ToString(matrix));
			return matrix;
		}

		protected internal override CRFLogConditionalObjectiveFunction GetObjectiveFunction(int[][][][] data, int[][] labels)
		{
			if (errorMatrix == null)
			{
				if (flags.errorMatrix != null)
				{
					if (tagIndex == null)
					{
						LoadTagIndex();
					}
					errorMatrix = ReadErrorMatrix(flags.errorMatrix, tagIndex, true);
				}
			}
			return new CRFLogConditionalObjectiveFunctionNoisyLabel(data, labels, windowSize, classIndex, labelIndices, map, flags.priorType, flags.backgroundSymbol, flags.sigma, null, flags.multiThreadGrad, errorMatrix);
		}
	}
}
