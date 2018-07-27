using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	public class CacheParseHypotheses
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Dvparser.CacheParseHypotheses));

		private static readonly ITreeReaderFactory trf = new LabeledScoredTreeReaderFactory(CoreLabel.Factory(), new TreeNormalizer());

		internal readonly BasicCategoryTreeTransformer treeBasicCategories;

		public readonly IPredicate<Tree> treeFilter;

		public CacheParseHypotheses(LexicalizedParser parser)
		{
			treeBasicCategories = new BasicCategoryTreeTransformer(parser.TreebankLanguagePack());
			treeFilter = new FilterConfusingRules(parser);
		}

		public virtual byte[] ConvertToBytes(IList<Tree> input)
		{
			try
			{
				ByteArrayOutputStream bos = new ByteArrayOutputStream();
				GZIPOutputStream gos = new GZIPOutputStream(bos);
				ObjectOutputStream oos = new ObjectOutputStream(gos);
				IList<Tree> transformed = CollectionUtils.TransformAsList(input, treeBasicCategories);
				IList<Tree> filtered = CollectionUtils.FilterAsList(transformed, treeFilter);
				oos.WriteObject(filtered.Count);
				foreach (Tree tree in filtered)
				{
					oos.WriteObject(tree.ToString());
				}
				oos.Close();
				gos.Close();
				bos.Close();
				return bos.ToByteArray();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual IdentityHashMap<Tree, byte[]> ConvertToBytes(IdentityHashMap<Tree, IList<Tree>> uncompressed)
		{
			IdentityHashMap<Tree, byte[]> compressed = Generics.NewIdentityHashMap();
			foreach (KeyValuePair<Tree, IList<Tree>> entry in uncompressed)
			{
				compressed[entry.Key] = ConvertToBytes(entry.Value);
			}
			return compressed;
		}

		public static IList<Tree> ConvertToTrees(byte[] input)
		{
			try
			{
				IList<Tree> output = new List<Tree>();
				ByteArrayInputStream bis = new ByteArrayInputStream(input);
				GZIPInputStream gis = new GZIPInputStream(bis);
				ObjectInputStream ois = new ObjectInputStream(gis);
				int size = ErasureUtils.UncheckedCast<int>(ois.ReadObject());
				for (int i = 0; i < size; ++i)
				{
					string rawTree = ErasureUtils.UncheckedCast(ois.ReadObject());
					Tree tree = Tree.ValueOf(rawTree, trf);
					tree.SetSpans();
					output.Add(tree);
				}
				ois.Close();
				gis.Close();
				bis.Close();
				return output;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
		}

		public static IdentityHashMap<Tree, IList<Tree>> ConvertToTrees(IdentityHashMap<Tree, byte[]> compressed, int numThreads)
		{
			return ConvertToTrees(compressed.Keys, compressed, numThreads);
		}

		internal class DecompressionProcessor : IThreadsafeProcessor<byte[], IList<Tree>>
		{
			public virtual IList<Tree> Process(byte[] input)
			{
				return ConvertToTrees(input);
			}

			public virtual IThreadsafeProcessor<byte[], IList<Tree>> NewInstance()
			{
				// should be threadsafe
				return this;
			}
		}

		public static IdentityHashMap<Tree, IList<Tree>> ConvertToTrees(ICollection<Tree> keys, IdentityHashMap<Tree, byte[]> compressed, int numThreads)
		{
			IdentityHashMap<Tree, IList<Tree>> uncompressed = Generics.NewIdentityHashMap();
			MulticoreWrapper<byte[], IList<Tree>> wrapper = new MulticoreWrapper<byte[], IList<Tree>>(numThreads, new CacheParseHypotheses.DecompressionProcessor());
			foreach (Tree tree in keys)
			{
				wrapper.Put(compressed[tree]);
			}
			foreach (Tree tree_1 in keys)
			{
				if (!wrapper.Peek())
				{
					wrapper.Join();
				}
				uncompressed[tree_1] = wrapper.Poll();
			}
			return uncompressed;
		}

		internal class CacheProcessor : IThreadsafeProcessor<Tree, Pair<Tree, byte[]>>
		{
			internal CacheParseHypotheses cacher;

			internal LexicalizedParser parser;

			internal int dvKBest;

			internal ITreeTransformer transformer;

			public CacheProcessor(CacheParseHypotheses cacher, LexicalizedParser parser, int dvKBest, ITreeTransformer transformer)
			{
				this.cacher = cacher;
				this.parser = parser;
				this.dvKBest = dvKBest;
				this.transformer = transformer;
			}

			public virtual Pair<Tree, byte[]> Process(Tree tree)
			{
				IList<Tree> topParses = DVParser.GetTopParsesForOneTree(parser, dvKBest, tree, transformer);
				// this block is a test to make sure the conversion code is working...
				IList<Tree> converted = CacheParseHypotheses.ConvertToTrees(cacher.ConvertToBytes(topParses));
				IList<Tree> simplified = CollectionUtils.TransformAsList(topParses, cacher.treeBasicCategories);
				simplified = CollectionUtils.FilterAsList(simplified, cacher.treeFilter);
				if (simplified.Count != topParses.Count)
				{
					log.Info("Filtered " + (topParses.Count - simplified.Count) + " trees");
					if (simplified.Count == 0)
					{
						log.Info(" WARNING: filtered all trees for " + tree);
					}
				}
				if (!simplified.Equals(converted))
				{
					if (converted.Count != simplified.Count)
					{
						throw new AssertionError("horrible error: tree sizes not equal, " + converted.Count + " vs " + simplified.Count);
					}
					for (int i = 0; i < converted.Count; ++i)
					{
						if (!simplified[i].Equals(converted[i]))
						{
							System.Console.Out.WriteLine("=============================");
							System.Console.Out.WriteLine(simplified[i]);
							System.Console.Out.WriteLine("=============================");
							System.Console.Out.WriteLine(converted[i]);
							System.Console.Out.WriteLine("=============================");
							throw new AssertionError("horrible error: tree " + i + " not equal for base tree " + tree);
						}
					}
				}
				return Pair.MakePair(tree, cacher.ConvertToBytes(topParses));
			}

			public virtual IThreadsafeProcessor<Tree, Pair<Tree, byte[]>> NewInstance()
			{
				// should be threadsafe
				return this;
			}
		}

		/// <summary>
		/// An example of a command line is
		/// <br />
		/// java -mx1g edu.stanford.nlp.parser.dvparser.CacheParseHypotheses -model /scr/horatio/dvparser/wsjPCFG.nocompact.simple.ser.gz -output cached9.simple.ser.gz  -treebank /afs/ir/data/linguistic-data/Treebank/3/parsed/mrg/wsj 200-202
		/// <br />
		/// java -mx4g edu.stanford.nlp.parser.dvparser.CacheParseHypotheses -model ~/scr/dvparser/wsjPCFG.nocompact.simple.ser.gz -output cached.train.simple.ser.gz -treebank /afs/ir/data/linguistic-data/Treebank/3/parsed/mrg/wsj 200-2199 -numThreads 6
		/// <br />
		/// java -mx4g edu.stanford.nlp.parser.dvparser.CacheParseHypotheses -model ~/scr/dvparser/chinese/xinhuaPCFG.ser.gz -output cached.xinhua.train.ser.gz -treebank /afs/ir/data/linguistic-data/Chinese-Treebank/6/data/utf8/bracketed  026-270,301-499,600-999
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string parserModel = null;
			string output = null;
			IList<Pair<string, IFileFilter>> treebanks = Generics.NewArrayList();
			int dvKBest = 200;
			int numThreads = 1;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-dvKBest"))
				{
					dvKBest = System.Convert.ToInt32(args[argIndex + 1]);
					argIndex += 2;
					continue;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parser") || args[argIndex].Equals("-model"))
				{
					parserModel = args[argIndex + 1];
					argIndex += 2;
					continue;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
				{
					output = args[argIndex + 1];
					argIndex += 2;
					continue;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank"))
				{
					Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-treebank");
					argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
					treebanks.Add(treebankDescription);
					continue;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-numThreads"))
				{
					numThreads = System.Convert.ToInt32(args[argIndex + 1]);
					argIndex += 2;
					continue;
				}
				throw new ArgumentException("Unknown argument " + args[argIndex]);
			}
			if (parserModel == null)
			{
				throw new ArgumentException("Need to supply a parser model with -model");
			}
			if (output == null)
			{
				throw new ArgumentException("Need to supply an output filename with -output");
			}
			if (treebanks.IsEmpty())
			{
				throw new ArgumentException("Need to supply a treebank with -treebank");
			}
			log.Info("Writing output to " + output);
			log.Info("Loading parser model " + parserModel);
			log.Info("Writing " + dvKBest + " hypothesis trees for each tree");
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(parserModel, "-dvKBest", int.ToString(dvKBest)));
			CacheParseHypotheses cacher = new CacheParseHypotheses(parser);
			ITreeTransformer transformer = DVParser.BuildTrainTransformer(parser.GetOp());
			IList<Tree> sentences = new List<Tree>();
			foreach (Pair<string, IFileFilter> description in treebanks)
			{
				log.Info("Reading trees from " + description.first);
				Treebank treebank = parser.GetOp().tlpParams.MemoryTreebank();
				treebank.LoadPath(description.first, description.second);
				treebank = treebank.Transform(transformer);
				Sharpen.Collections.AddAll(sentences, treebank);
			}
			log.Info("Processing " + sentences.Count + " trees");
			IList<Pair<Tree, byte[]>> cache = Generics.NewArrayList();
			transformer = new SynchronizedTreeTransformer(transformer);
			MulticoreWrapper<Tree, Pair<Tree, byte[]>> wrapper = new MulticoreWrapper<Tree, Pair<Tree, byte[]>>(numThreads, new CacheParseHypotheses.CacheProcessor(cacher, parser, dvKBest, transformer));
			foreach (Tree tree in sentences)
			{
				wrapper.Put(tree);
				while (wrapper.Peek())
				{
					cache.Add(wrapper.Poll());
					if (cache.Count % 10 == 0)
					{
						System.Console.Out.WriteLine("Processed " + cache.Count + " trees");
					}
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				cache.Add(wrapper.Poll());
				if (cache.Count % 10 == 0)
				{
					System.Console.Out.WriteLine("Processed " + cache.Count + " trees");
				}
			}
			System.Console.Out.WriteLine("Finished processing " + cache.Count + " trees");
			IOUtils.WriteObjectToFile(cache, output);
		}
	}
}
