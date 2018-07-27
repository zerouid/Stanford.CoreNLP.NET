using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.French.Pipeline
{
	/// <summary>
	/// Produces the pre-processed version of the FTB used in the experiments of
	/// Green et al.
	/// </summary>
	/// <remarks>
	/// Produces the pre-processed version of the FTB used in the experiments of
	/// Green et al. (2011).
	/// </remarks>
	/// <author>Spence Green</author>
	public class FTBDataset : AbstractDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Pipeline.FTBDataset));

		private bool CcTagset = false;

		private ICollection<string> splitSet;

		public FTBDataset()
			: base()
		{
			//Need to use a MemoryTreebank so that we can compute gross corpus
			//stats for MWE pre-processing
			// The treebank may be reset if setOptions changes CC_TAGSET
			treebank = new MemoryTreebank(new FrenchXMLTreeReaderFactory(CcTagset), FrenchTreebankLanguagePack.FtbEncoding);
			treeFileExtension = "xml";
		}

		/// <summary>Return the ID of this tree according to the Candito split files.</summary>
		private string GetCanditoTreeID(Tree t)
		{
			string canditoName = null;
			if (t.Label() is CoreLabel)
			{
				string fileName = ((CoreLabel)t.Label()).DocID();
				fileName = Sharpen.Runtime.Substring(fileName, 0, fileName.LastIndexOf('.'));
				string ftbID = ((CoreLabel)t.Label()).Get(typeof(CoreAnnotations.SentenceIDAnnotation));
				if (fileName != null && ftbID != null)
				{
					canditoName = fileName + "-" + ftbID;
				}
				else
				{
					throw new ArgumentNullException("fileName " + fileName + ", ftbID " + ftbID);
				}
			}
			else
			{
				throw new ArgumentException("Trees constructed without CoreLabels! Can't extract metadata!");
			}
			return canditoName;
		}

		public override void Build()
		{
			foreach (File path in pathsToData)
			{
				treebank.LoadPath(path, treeFileExtension, false);
			}
			PrintWriter outfile = null;
			PrintWriter flatFile = null;
			try
			{
				outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFileName), "UTF-8")));
				flatFile = (makeFlatFile) ? new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(flatFileName), "UTF-8"))) : null;
				outputFileList.Add(outFileName);
				if (makeFlatFile)
				{
					outputFileList.Add(flatFileName);
					toStringBuffer.Append(" Made flat files\n");
				}
				PreprocessMWEs();
				IList<TregexPattern> badTrees = new List<TregexPattern>();
				//These trees appear in the Candito training set
				//They are mangled by the TreeCorrector, so discard them ahead of time.
				badTrees.Add(TregexPattern.Compile("@SENT <: @PUNC"));
				badTrees.Add(TregexPattern.Compile("@SENT <1 @PUNC <2 @PUNC !<3 __"));
				//wsg2011: This filters out tree #552 in the Candito test set. We saved this tree for the
				//EMNLP2011 paper, but since it consists entirely of punctuation, it won't be evaluated anyway.
				//Since we aren't doing the split in this data set, just remove the tree.
				badTrees.Add(TregexPattern.Compile("@SENT <1 @PUNC <2 @PUNC <3 @PUNC <4 @PUNC !<5 __"));
				foreach (Tree t in treebank)
				{
					//Filter out bad trees
					bool skipTree = false;
					foreach (TregexPattern p in badTrees)
					{
						skipTree = p.Matcher(t).Find();
						if (skipTree)
						{
							break;
						}
					}
					if (skipTree)
					{
						log.Info("Discarding tree: " + t.ToString());
						continue;
					}
					// Filter out trees that aren't in this part of the split
					if (splitSet != null)
					{
						string canditoTreeID = GetCanditoTreeID(t);
						if (!splitSet.Contains(canditoTreeID))
						{
							continue;
						}
					}
					if (customTreeVisitor != null)
					{
						customTreeVisitor.VisitTree(t);
					}
					// outfile.printf("%s\t%s%n",treeName,t.toString());
					outfile.Println(t.ToString());
					if (makeFlatFile)
					{
						string flatString = (removeEscapeTokens) ? ATBTreeUtils.UnEscape(ATBTreeUtils.FlattenTree(t)) : ATBTreeUtils.FlattenTree(t);
						flatFile.Println(flatString);
					}
				}
			}
			catch (UnsupportedEncodingException e)
			{
				System.Console.Error.Printf("%s: Filesystem does not support UTF-8 output%n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open %s for writing%n", this.GetType().FullName, outFileName);
			}
			catch (TregexParseException e)
			{
				System.Console.Error.Printf("%s: Could not compile Tregex expressions%n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				if (outfile != null)
				{
					outfile.Close();
				}
				if (flatFile != null)
				{
					flatFile.Close();
				}
			}
		}

		/// <summary>Corrects MWE annotations that lack internal POS labels.</summary>
		private void PreprocessMWEs()
		{
			TwoDimensionalCounter<string, string> labelTerm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> termLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> labelPreterm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> pretermLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> unigramTagger = new TwoDimensionalCounter<string, string>();
			foreach (Tree t in treebank)
			{
				MWEPreprocessor.CountMWEStatistics(t, unigramTagger, labelPreterm, pretermLabel, labelTerm, termLabel);
			}
			foreach (Tree t_1 in treebank)
			{
				MWEPreprocessor.TraverseAndFix(t_1, pretermLabel, unigramTagger);
			}
		}

		public override bool SetOptions(Properties opts)
		{
			bool ret = base.SetOptions(opts);
			if (opts.Contains(ConfigParser.paramSplit))
			{
				string splitFileName = opts.GetProperty(ConfigParser.paramSplit);
				splitSet = MakeSplitSet(splitFileName);
			}
			CcTagset = PropertiesUtils.GetBool(opts, ConfigParser.paramCCTagset, false);
			treebank = new MemoryTreebank(new FrenchXMLTreeReaderFactory(CcTagset), FrenchTreebankLanguagePack.FtbEncoding);
			if (lexMapper == null)
			{
				lexMapper = new DefaultMapper();
				lexMapper.Setup(null, lexMapOptions.Split(","));
			}
			if (pathsToMappings.Count != 0)
			{
				if (posMapper == null)
				{
					posMapper = new DefaultMapper();
				}
				foreach (File path in pathsToMappings)
				{
					posMapper.Setup(path);
				}
			}
			return ret;
		}

		private ICollection<string> MakeSplitSet(string splitFileName)
		{
			splitFileName = DataFilePaths.Convert(splitFileName);
			ICollection<string> splitSet = Generics.NewHashSet();
			LineNumberReader reader = null;
			try
			{
				reader = new LineNumberReader(new FileReader(splitFileName));
				for (string line; (line = reader.ReadLine()) != null; )
				{
					splitSet.Add(line.Trim());
				}
				reader.Close();
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				System.Console.Error.Printf("%s: Error reading %s (line %d)%n", this.GetType().FullName, splitFileName, reader.GetLineNumber());
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return splitSet;
		}
	}
}
