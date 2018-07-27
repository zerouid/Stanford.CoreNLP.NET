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
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Subclass of CRFClassifier that performs dropout feature-noising training.</summary>
	/// <author>Mengqiu Wang</author>
	public class CRFClassifierWithDropout<In> : CRFClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierWithDropout));

		private IList<IList<In>> unsupDocs;

		public CRFClassifierWithDropout(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		protected internal override ICollection<IList<In>> LoadAuxiliaryData(ICollection<IList<In>> docs, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			if (flags.unsupDropoutFile != null)
			{
				log.Info("Reading unsupervised dropout data from file: " + flags.unsupDropoutFile);
				Timing timer = new Timing();
				timer.Start();
				unsupDocs = new List<IList<In>>();
				ObjectBank<IList<In>> unsupObjBank = MakeObjectBankFromFile(flags.unsupDropoutFile, readerAndWriter);
				foreach (IList<In> doc in unsupObjBank)
				{
					foreach (IN tok in doc)
					{
						tok.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
						tok.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), flags.backgroundSymbol);
					}
					unsupDocs.Add(doc);
				}
				long elapsedMs = timer.Stop();
				log.Info("Time to read: : " + Timing.ToSecondsString(elapsedMs) + " seconds");
			}
			if (unsupDocs != null && flags.doFeatureDiscovery)
			{
				IList<IList<In>> totalDocs = new List<IList<In>>();
				Sharpen.Collections.AddAll(totalDocs, docs);
				Sharpen.Collections.AddAll(totalDocs, unsupDocs);
				return totalDocs;
			}
			else
			{
				return docs;
			}
		}

		protected internal override CRFLogConditionalObjectiveFunction GetObjectiveFunction(int[][][][] data, int[][] labels)
		{
			int[][][][] unsupDropoutData = null;
			if (unsupDocs != null)
			{
				Timing timer = new Timing();
				timer.Start();
				IList<Triple<int[][][], int[], double[][][]>> unsupDataAndLabels = DocumentsToDataAndLabelsList(unsupDocs);
				unsupDropoutData = new int[unsupDataAndLabels.Count][][][];
				for (int q = 0; q < unsupDropoutData.Length; q++)
				{
					unsupDropoutData[q] = unsupDataAndLabels[q].First();
				}
				long elapsedMs = timer.Stop();
				log.Info("Time to read unsupervised dropout data: " + Timing.ToSecondsString(elapsedMs) + " seconds, read " + unsupDropoutData.Length + " files");
			}
			return new CRFLogConditionalObjectiveFunctionWithDropout(data, labels, windowSize, classIndex, labelIndices, map, flags.priorType, flags.backgroundSymbol, flags.sigma, null, flags.dropoutRate, flags.dropoutScale, flags.multiThreadGrad, flags
				.dropoutApprox, flags.unsupDropoutScale, unsupDropoutData);
		}
	}
}
