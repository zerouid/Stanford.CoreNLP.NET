using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Exports CRF features for use with other programs.</summary>
	/// <remarks>
	/// Exports CRF features for use with other programs.
	/// Usage: CRFFeatureExporter -prop crfClassifierPropFile -trainFile inputFile -exportFeatures outputFile
	/// - Output file is automatically gzipped/b2zipped if ending in gz/bz2
	/// - bzip2 requires that bzip2 is available via command line
	/// - Currently exports features in a format that can be read by a modified crfsgd
	/// (crfsgd assumes features are gzipped)
	/// TODO: Support other formats (like crfsuite)
	/// </remarks>
	/// <author>Angel Chang</author>
	public class CRFFeatureExporter<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFFeatureExporter));

		private char delimiter = '\t';

		private static readonly string eol = Runtime.LineSeparator();

		private CRFClassifier<In> classifier;

		public CRFFeatureExporter(CRFClassifier<In> classifier)
		{
			this.classifier = classifier;
		}

		/// <summary>
		/// Prefix features with U- (for unigram) features
		/// or B- (for bigram) features
		/// </summary>
		/// <param name="feat">String representing the feature</param>
		/// <returns>new prefixed feature string</returns>
		private static string UbPrefixFeatureString(string feat)
		{
			if (feat.EndsWith("|C"))
			{
				return "U-" + feat;
			}
			else
			{
				if (feat.EndsWith("|CpC"))
				{
					return "B-" + feat;
				}
				else
				{
					return feat;
				}
			}
		}

		/// <summary>
		/// Constructs a big string representing the input list of CoreLabel,
		/// with one line per token using the following format
		/// word label feat1 feat2 ...
		/// </summary>
		/// <remarks>
		/// Constructs a big string representing the input list of CoreLabel,
		/// with one line per token using the following format
		/// word label feat1 feat2 ...
		/// (where each space is actually a tab).
		/// Assumes that CoreLabel has both TextAnnotation and AnswerAnnotation.
		/// </remarks>
		/// <param name="document">
		/// List of CoreLabel
		/// (does not have to represent a "document", just a sequence of text,
		/// like a sentence or a paragraph)
		/// </param>
		/// <returns>String representation of features</returns>
		private string GetFeatureString(IList<In> document)
		{
			int docSize = document.Count;
			if (classifier.flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			StringBuilder sb = new StringBuilder();
			for (int j = 0; j < docSize; j++)
			{
				IN token = document[j];
				sb.Append(token.Get(typeof(CoreAnnotations.TextAnnotation)));
				sb.Append(delimiter);
				sb.Append(token.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				CRFDatum<IList<string>, CRFLabel> d = classifier.MakeDatum(document, j, classifier.featureFactories);
				IList<IList<string>> features = d.AsFeatures();
				foreach (ICollection<string> cliqueFeatures in features)
				{
					IList<string> sortedFeatures = new List<string>(cliqueFeatures);
					sortedFeatures.Sort();
					foreach (string feat in sortedFeatures)
					{
						feat = UbPrefixFeatureString(feat);
						sb.Append(delimiter);
						sb.Append(feat);
					}
				}
				sb.Append(eol);
			}
			if (classifier.flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Output features that have already been converted into features
		/// (using documentToDataAndLabels) in format suitable for CRFSuite.
		/// </summary>
		/// <remarks>
		/// Output features that have already been converted into features
		/// (using documentToDataAndLabels) in format suitable for CRFSuite.
		/// Format is with one line per token using the following format
		/// label feat1 feat2 ...
		/// (where each space is actually a tab)
		/// Each document is separated by an empty line.
		/// </remarks>
		/// <param name="exportFile">file to export the features to</param>
		/// <param name="docsData">array of document features</param>
		/// <param name="labels">correct labels indexed by document, and position within document</param>
		public virtual void PrintFeatures(string exportFile, int[][][][] docsData, int[][] labels)
		{
			try
			{
				PrintWriter pw = IOUtils.GetPrintWriter(exportFile);
				for (int i = 0; i < docsData.Length; i++)
				{
					for (int j = 0; j < docsData[i].Length; j++)
					{
						StringBuilder sb = new StringBuilder();
						int label = labels[i][j];
						sb.Append(classifier.classIndex.Get(label));
						for (int k = 0; k < docsData[i][j].Length; k++)
						{
							for (int m = 0; m < docsData[i][j][k].Length; m++)
							{
								string feat = classifier.featureIndex.Get(docsData[i][j][k][m]);
								feat = UbPrefixFeatureString(feat);
								sb.Append(delimiter);
								sb.Append(feat);
							}
						}
						pw.Println(sb.ToString());
					}
					pw.Println();
				}
				pw.Close();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>
		/// Output features from a collection of documents to a file
		/// Format is with one line per token using the following format
		/// word label feat1 feat2 ...
		/// </summary>
		/// <remarks>
		/// Output features from a collection of documents to a file
		/// Format is with one line per token using the following format
		/// word label feat1 feat2 ...
		/// (where each space is actually a tab)
		/// Each document is separated by an empty line
		/// This format is suitable for modified crfsgd.
		/// </remarks>
		/// <param name="exportFile">file to export the features to</param>
		/// <param name="documents">input collection of documents</param>
		public virtual void PrintFeatures(string exportFile, ICollection<IList<In>> documents)
		{
			try
			{
				PrintWriter pw = IOUtils.GetPrintWriter(exportFile);
				foreach (IList<In> doc in documents)
				{
					string str = GetFeatureString(doc);
					pw.Println(str);
				}
				pw.Close();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			CRFClassifier<CoreLabel> crf = new CRFClassifier<CoreLabel>(props);
			string inputFile = crf.flags.trainFile;
			if (inputFile == null)
			{
				log.Info("Please provide input file using -trainFile");
				System.Environment.Exit(-1);
			}
			string outputFile = crf.flags.exportFeatures;
			if (outputFile == null)
			{
				log.Info("Please provide output file using -exportFeatures");
				System.Environment.Exit(-1);
			}
			Edu.Stanford.Nlp.IE.Crf.CRFFeatureExporter<CoreLabel> featureExporter = new Edu.Stanford.Nlp.IE.Crf.CRFFeatureExporter<CoreLabel>(crf);
			ICollection<IList<CoreLabel>> docs = crf.MakeObjectBankFromFile(inputFile, crf.MakeReaderAndWriter());
			crf.MakeAnswerArraysAndTagIndex(docs);
			featureExporter.PrintFeatures(outputFile, docs);
		}
	}
}
