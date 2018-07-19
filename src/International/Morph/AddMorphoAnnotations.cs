using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Morph
{
	/// <summary>Reads in the tree files without any kind of pre-processing.</summary>
	/// <remarks>
	/// Reads in the tree files without any kind of pre-processing. Assumes that the trees
	/// have been processed separately.
	/// <p>
	/// TODO: wsg2011 Extend to other languages. Only supports Arabic right now.
	/// </remarks>
	/// <author>Spence Green</author>
	public sealed class AddMorphoAnnotations
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AddMorphoAnnotations));

		private const int minArgs = 2;

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(string.Format("Usage: java %s [OPTS] morph_file lemma_file < tree_file \n\n", typeof(AddMorphoAnnotations).FullName));
			sb.Append("Options:\n");
			sb.Append("  -e enc     : Encoding.\n");
			sb.Append("  -g         : Morph file is gold tree file with morph analyses in the pre-terminals.");
			return sb.ToString();
		}

		private static IDictionary<string, int> ArgSpec()
		{
			IDictionary<string, int> argSpec = Generics.NewHashMap();
			argSpec["g"] = 0;
			argSpec["e"] = 1;
			return argSpec;
		}

		/// <summary>Iterate over either strings or leaves.</summary>
		/// <author>Spence Green</author>
		private class YieldIterator : IEnumerator<IList<string>>
		{
			private IList<string> nextYield = null;

			internal BufferedReader fileReader = null;

			internal ITreeReader treeReader = null;

			public YieldIterator(string fileName, bool isTree)
			{
				try
				{
					if (isTree)
					{
						ITreeReaderFactory trf = new ArabicTreeReaderFactory.ArabicRawTreeReaderFactory(true);
						treeReader = trf.NewTreeReader(new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
					}
					else
					{
						fileReader = new BufferedReader(new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
					}
				}
				catch (UnsupportedEncodingException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
				catch (FileNotFoundException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
				PrimeNext();
			}

			private void PrimeNext()
			{
				try
				{
					if (treeReader != null)
					{
						Tree tree = treeReader.ReadTree();
						if (tree == null)
						{
							nextYield = null;
						}
						else
						{
							IList<CoreLabel> mLabeledLeaves = tree.TaggedLabeledYield();
							nextYield = new List<string>(mLabeledLeaves.Count);
							foreach (CoreLabel label in mLabeledLeaves)
							{
								nextYield.Add(label.Tag());
							}
						}
					}
					else
					{
						string line = fileReader.ReadLine();
						if (line == null)
						{
							nextYield = null;
						}
						else
						{
							nextYield = Arrays.AsList(line.Split("\\s+"));
						}
					}
				}
				catch (IOException e)
				{
					nextYield = null;
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}

			public virtual bool MoveNext()
			{
				return nextYield != null;
			}

			public virtual IList<string> Current
			{
				get
				{
					if (nextYield == null)
					{
						try
						{
							if (fileReader != null)
							{
								fileReader.Close();
								fileReader = null;
							}
							else
							{
								if (treeReader != null)
								{
									treeReader.Close();
									treeReader = null;
								}
							}
						}
						catch (IOException e)
						{
							Sharpen.Runtime.PrintStackTrace(e);
						}
						return null;
					}
					else
					{
						IList<string> next = nextYield;
						PrimeNext();
						return next;
					}
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, ArgSpec());
			string encoding = options.GetProperty("e", "UTF-8");
			bool isMorphTreeFile = PropertiesUtils.GetBool(options, "g", false);
			string[] parsedArgs = options.GetProperty(string.Empty).Split("\\s+");
			if (parsedArgs.Length != 2)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			AddMorphoAnnotations.YieldIterator morphIter = new AddMorphoAnnotations.YieldIterator(parsedArgs[0], isMorphTreeFile);
			AddMorphoAnnotations.YieldIterator lemmaIter = new AddMorphoAnnotations.YieldIterator(parsedArgs[1], false);
			Pattern pParenStripper = Pattern.Compile("[\\(\\)]");
			try
			{
				BufferedReader brIn = new BufferedReader(new InputStreamReader(Runtime.@in, encoding));
				ITreeReaderFactory trf = new ArabicTreeReaderFactory.ArabicRawTreeReaderFactory(true);
				int nTrees = 0;
				for (string line; (line = brIn.ReadLine()) != null; ++nTrees)
				{
					Tree tree = trf.NewTreeReader(new StringReader(line)).ReadTree();
					IList<Tree> leaves = tree.GetLeaves();
					if (!morphIter.MoveNext())
					{
						throw new Exception("Mismatch between number of morpho analyses and number of input lines.");
					}
					IList<string> morphTags = morphIter.Current;
					if (!lemmaIter.MoveNext())
					{
						throw new Exception("Mismatch between number of lemmas and number of input lines.");
					}
					IList<string> lemmas = lemmaIter.Current;
					// Sanity checks
					System.Diagnostics.Debug.Assert(morphTags.Count == lemmas.Count);
					System.Diagnostics.Debug.Assert(lemmas.Count == leaves.Count);
					for (int i = 0; i < leaves.Count; ++i)
					{
						string morphTag = morphTags[i];
						if (pParenStripper.Matcher(morphTag).Find())
						{
							morphTag = pParenStripper.Matcher(morphTag).ReplaceAll(string.Empty);
						}
						string newLeaf = string.Format("%s%s%s%s%s", leaves[i].Value(), MorphoFeatureSpecification.MorphoMark, lemmas[i], MorphoFeatureSpecification.LemmaMark, morphTag);
						leaves[i].SetValue(newLeaf);
					}
					System.Console.Out.WriteLine(tree.ToString());
				}
				// Sanity checks
				System.Diagnostics.Debug.Assert(!morphIter.MoveNext());
				System.Diagnostics.Debug.Assert(!lemmaIter.MoveNext());
				System.Console.Error.Printf("Processed %d trees%n", nTrees);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
