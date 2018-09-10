// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002 - 2014 The Board of Trustees of
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
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class provides the top-level API and command-line interface to a set
	/// of reasonably good treebank-trained parsers.
	/// </summary>
	/// <remarks>
	/// This class provides the top-level API and command-line interface to a set
	/// of reasonably good treebank-trained parsers.  The name reflects the main
	/// factored parsing model, which provides a lexicalized PCFG parser
	/// implemented as a product
	/// model of a plain PCFG parser and a lexicalized dependency parser.
	/// But you can also run either component parser alone.  In particular, it
	/// is often useful to do unlexicalized PCFG parsing by using just that
	/// component parser.
	/// <p>
	/// See the package documentation for more details and examples of use.
	/// <p>
	/// For information on invoking the parser from the command-line, and for
	/// a more detailed list of options, see the
	/// <see cref="Main(string[])"/>
	/// method.
	/// <p>
	/// Note that training on a 1 million word treebank requires a fair amount of
	/// memory to run.  Try -mx1500m to increase the memory allocated by the JVM.
	/// </remarks>
	/// <author>Dan Klein (original version)</author>
	/// <author>Christopher Manning (better features, ParserParams, serialization)</author>
	/// <author>Roger Levy (internationalization)</author>
	/// <author>Teg Grenager (grammar compaction, tokenization, etc.)</author>
	/// <author>Galen Andrew (considerable refactoring)</author>
	/// <author>John Bauer (made threadsafe)</author>
	[System.Serializable]
	public class LexicalizedParser : ParserGrammar
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser));

		public ILexicon lex;

		public BinaryGrammar bg;

		public UnaryGrammar ug;

		public IDependencyGrammar dg;

		public IIndex<string> stateIndex;

		public IIndex<string> wordIndex;

		public IIndex<string> tagIndex;

		private Options op;

		public override Options GetOp()
		{
			return op;
		}

		public IReranker reranker;

		// = null;
		public override ITreebankLangParserParams GetTLPParams()
		{
			return op.tlpParams;
		}

		public override ITreebankLanguagePack TreebankLanguagePack()
		{
			return GetTLPParams().TreebankLanguagePack();
		}

		public override string[] DefaultCoreNLPFlags()
		{
			return GetTLPParams().DefaultCoreNLPFlags();
		}

		public override bool RequiresTags()
		{
			return false;
		}

		private const string SerializedParserProperty = "edu.stanford.nlp.SerializedLexicalizedParser";

		public static readonly string DefaultParserLoc = ((Runtime.Getenv("NLP_PARSER") != null) ? Runtime.Getenv("NLP_PARSER") : "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz");

		/// <summary>
		/// Construct a new LexicalizedParser object from a previously
		/// serialized grammar read from a System property
		/// <c>edu.stanford.nlp.SerializedLexicalizedParser</c>
		/// , or a
		/// default classpath location
		/// (
		/// <c>edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz</c>
		/// ).
		/// </summary>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModel()
		{
			return LoadModel(new Options());
		}

		/// <summary>
		/// Construct a new LexicalizedParser object from a previously
		/// serialized grammar read from a System property
		/// <c>edu.stanford.nlp.SerializedLexicalizedParser</c>
		/// , or a
		/// default classpath location
		/// (
		/// <c>edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz</c>
		/// ).
		/// </summary>
		/// <param name="op">
		/// Options to the parser.  These get overwritten by the
		/// Options read from the serialized parser; I think the only
		/// thing determined by them is the encoding of the grammar
		/// iff it is a text grammar
		/// </param>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModel(Options op, params string[] extraFlags)
		{
			string source = Runtime.GetProperty(SerializedParserProperty);
			if (source == null)
			{
				source = DefaultParserLoc;
			}
			return LoadModel(source, op, extraFlags);
		}

		public static ParserGrammar LoadModel(string parserFileOrUrl, params string[] extraFlags)
		{
			return LoadModel(parserFileOrUrl, new Options(), extraFlags);
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModel(string parserFileOrUrl, IList<string> extraFlags)
		{
			string[] flags = new string[extraFlags.Count];
			Sharpen.Collections.ToArray(extraFlags, flags);
			return ((Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser)LoadModel(parserFileOrUrl, flags));
		}

		/// <summary>Construct a new LexicalizedParser.</summary>
		/// <remarks>
		/// Construct a new LexicalizedParser.  This loads a grammar
		/// that was previously assembled and stored as a serialized file.
		/// </remarks>
		/// <param name="parserFileOrUrl">Filename/URL to load parser from</param>
		/// <param name="op">
		/// Options for this parser. These will normally be overwritten
		/// by options stored in the file
		/// </param>
		/// <exception cref="System.ArgumentException">If parser data cannot be loaded</exception>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModel(string parserFileOrUrl, Options op, params string[] extraFlags)
		{
			//    log.info("Loading parser from file " + parserFileOrUrl);
			Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser parser = GetParserFromFile(parserFileOrUrl, op);
			if (extraFlags.Length > 0)
			{
				parser.SetOptionFlags(extraFlags);
			}
			return parser;
		}

		/// <summary>
		/// Reads one object from the given ObjectInputStream, which is
		/// assumed to be a LexicalizedParser.
		/// </summary>
		/// <remarks>
		/// Reads one object from the given ObjectInputStream, which is
		/// assumed to be a LexicalizedParser.  Throws a ClassCastException
		/// if this is not true.  The stream is not closed.
		/// </remarks>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModel(ObjectInputStream ois)
		{
			try
			{
				object o = ois.ReadObject();
				if (o is Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser)
				{
					return (Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser)o;
				}
				throw new InvalidCastException("Wanted LexicalizedParser, got " + o.GetType());
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

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser LoadModelFromZip(string zipFilename, string modelName)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser parser = null;
			try
			{
				File file = new File(zipFilename);
				if (file.Exists())
				{
					ZipFile zin = new ZipFile(file);
					ZipEntry zentry = zin.GetEntry(modelName);
					if (zentry != null)
					{
						InputStream @in = zin.GetInputStream(zentry);
						// gunzip it if necessary
						if (modelName.EndsWith(".gz"))
						{
							@in = new GZIPInputStream(@in);
						}
						ObjectInputStream ois = new ObjectInputStream(@in);
						parser = LoadModel(ois);
						ois.Close();
						@in.Close();
					}
					zin.Close();
				}
				else
				{
					throw new FileNotFoundException("Could not find " + modelName + " inside " + zipFilename);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return parser;
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser CopyLexicalizedParser(Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser parser)
		{
			return new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser(parser.lex, parser.bg, parser.ug, parser.dg, parser.stateIndex, parser.wordIndex, parser.tagIndex, parser.op);
		}

		public LexicalizedParser(ILexicon lex, BinaryGrammar bg, UnaryGrammar ug, IDependencyGrammar dg, IIndex<string> stateIndex, IIndex<string> wordIndex, IIndex<string> tagIndex, Options op)
		{
			this.lex = lex;
			this.bg = bg;
			this.ug = ug;
			this.dg = dg;
			this.stateIndex = stateIndex;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			this.op = op;
		}

		/// <summary>Construct a new LexicalizedParser.</summary>
		/// <param name="trainTreebank">a treebank to train from</param>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser TrainFromTreebank(Treebank trainTreebank, GrammarCompactor compactor, Options op)
		{
			return GetParserFromTreebank(trainTreebank, null, 1.0, compactor, op, null, null);
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser TrainFromTreebank(string treebankPath, IFileFilter filt, Options op)
		{
			return TrainFromTreebank(MakeTreebank(treebankPath, op, filt), op);
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser TrainFromTreebank(Treebank trainTreebank, Options op)
		{
			return TrainFromTreebank(trainTreebank, null, op);
		}

		/// <summary>
		/// Will process a list of strings into a list of HasWord and return
		/// the parse tree associated with that list.
		/// </summary>
		public virtual Tree ParseStrings(IList<string> lst)
		{
			IList<Word> words = new List<Word>();
			foreach (string word in lst)
			{
				words.Add(new Word(word));
			}
			return Parse(words);
		}

		/// <summary>Parses the list of HasWord.</summary>
		/// <remarks>
		/// Parses the list of HasWord.  If the parse fails for some reason,
		/// an X tree is returned instead of barfing.
		/// </remarks>
		public override Tree Parse<_T0>(IList<_T0> lst)
		{
			try
			{
				IParserQuery pq = ParserQuery();
				if (pq.Parse(lst))
				{
					Tree bestparse = pq.GetBestParse();
					// -10000 denotes unknown words
					bestparse.SetScore(pq.GetPCFGScore() % -10000.0);
					return bestparse;
				}
			}
			catch (Exception e)
			{
				log.Info("Following exception caught during parsing:");
				Sharpen.Runtime.PrintStackTrace(e);
				log.Info("Recovering using fall through strategy: will construct an (X ...) tree.");
			}
			// if can't parse or exception, fall through
			return ParserUtils.XTree(lst);
		}

		public virtual IList<Tree> ParseMultiple<_T0>(IList<_T0> sentences)
			where _T0 : IList<IHasWord>
		{
			IList<Tree> trees = new List<Tree>();
			foreach (IList<IHasWord> sentence in sentences)
			{
				trees.Add(Parse(sentence));
			}
			return trees;
		}

		/// <summary>
		/// Will launch multiple threads which calls
		/// <c>parse</c>
		/// on
		/// each of the
		/// <paramref name="sentences"/>
		/// in order, returning the
		/// resulting parse trees in the same order.
		/// </summary>
		public virtual IList<Tree> ParseMultiple<_T0>(IList<_T0> sentences, int nthreads)
			where _T0 : IList<IHasWord>
		{
			MulticoreWrapper<IList<IHasWord>, Tree> wrapper = new MulticoreWrapper<IList<IHasWord>, Tree>(nthreads, new _IThreadsafeProcessor_330(this));
			IList<Tree> trees = new List<Tree>();
			foreach (IList<IHasWord> sentence in sentences)
			{
				wrapper.Put(sentence);
				while (wrapper.Peek())
				{
					trees.Add(wrapper.Poll());
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				trees.Add(wrapper.Poll());
			}
			return trees;
		}

		private sealed class _IThreadsafeProcessor_330 : IThreadsafeProcessor<IList<IHasWord>, Tree>
		{
			public _IThreadsafeProcessor_330(LexicalizedParser _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public Tree Process<_T0>(IList<_T0> sentence)
				where _T0 : IHasWord
			{
				return this._enclosing.Parse(sentence);
			}

			public IThreadsafeProcessor<IList<IHasWord>, Tree> NewInstance()
			{
				return this;
			}

			private readonly LexicalizedParser _enclosing;
		}

		/// <summary>Return a TreePrint for formatting parsed output trees.</summary>
		/// <returns>A TreePrint for formatting parsed output trees.</returns>
		public virtual TreePrint GetTreePrint()
		{
			return op.testOptions.TreePrint(op.tlpParams);
		}

		/// <summary>Similar to parse(), but instead of returning an X tree on failure, returns null.</summary>
		public virtual Tree ParseTree<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			IParserQuery pq = ParserQuery();
			if (pq.Parse(sentence))
			{
				return pq.GetBestParse();
			}
			else
			{
				return null;
			}
		}

		public override IList<IEval> GetExtraEvals()
		{
			if (reranker != null)
			{
				return reranker.GetEvals();
			}
			else
			{
				return Java.Util.Collections.EmptyList();
			}
		}

		public override IList<IParserQueryEval> GetParserQueryEvals()
		{
			return Java.Util.Collections.EmptyList();
		}

		public override IParserQuery ParserQuery()
		{
			if (reranker == null)
			{
				return new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParserQuery(this);
			}
			else
			{
				return new RerankingParserQuery(op, new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParserQuery(this), reranker);
			}
		}

		public virtual Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParserQuery LexicalizedParserQuery()
		{
			return new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParserQuery(this);
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser GetParserFromFile(string parserFileOrUrl, Options op)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser pd = GetParserFromSerializedFile(parserFileOrUrl);
			if (pd == null)
			{
				pd = GetParserFromTextFile(parserFileOrUrl, op);
			}
			return pd;
		}

		private static Treebank MakeTreebank(string treebankPath, Options op, IFileFilter filt)
		{
			log.Info("Training a parser from treebank dir: " + treebankPath);
			Treebank trainTreebank = op.tlpParams.DiskTreebank();
			log.Info("Reading trees...");
			if (filt == null)
			{
				trainTreebank.LoadPath(treebankPath);
			}
			else
			{
				trainTreebank.LoadPath(treebankPath, filt);
			}
			Timing.Tick("done [read " + trainTreebank.Count + " trees].");
			return trainTreebank;
		}

		private static DiskTreebank MakeSecondaryTreebank(string treebankPath, Options op, IFileFilter filt)
		{
			log.Info("Additionally training using secondary disk treebank: " + treebankPath + ' ' + filt);
			DiskTreebank trainTreebank = op.tlpParams.DiskTreebank();
			log.Info("Reading trees...");
			if (filt == null)
			{
				trainTreebank.LoadPath(treebankPath);
			}
			else
			{
				trainTreebank.LoadPath(treebankPath, filt);
			}
			Timing.Tick("done [read " + trainTreebank.Count + " trees].");
			return trainTreebank;
		}

		public virtual ILexicon GetLexicon()
		{
			return lex;
		}

		/// <summary>Saves the parser defined by pd to the given filename.</summary>
		/// <remarks>
		/// Saves the parser defined by pd to the given filename.
		/// If there is an error, a RuntimeIOException is thrown.
		/// </remarks>
		public virtual void SaveParserToSerialized(string filename)
		{
			try
			{
				log.Info("Writing parser in serialized format to file " + filename + ' ');
				ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
				@out.WriteObject(this);
				@out.Close();
				log.Info("done.");
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException(ioe);
			}
		}

		/// <summary>Saves the parser defined by pd to the given filename.</summary>
		/// <remarks>
		/// Saves the parser defined by pd to the given filename.
		/// If there is an error, a RuntimeIOException is thrown.
		/// </remarks>
		public virtual void SaveParserToTextFile(string filename)
		{
			// todo: [cdm 2015] This doesn't use character encoding and it should!
			if (reranker != null)
			{
				throw new NotSupportedException("Sorry, but parsers with rerankers cannot be saved to text file");
			}
			try
			{
				log.Info("Writing parser in text grammar format to file " + filename);
				OutputStream os;
				if (filename.EndsWith(".gz"))
				{
					// it's faster to do the buffering _outside_ the gzipping as here
					os = new BufferedOutputStream(new GZIPOutputStream(new FileOutputStream(filename)));
				}
				else
				{
					os = new BufferedOutputStream(new FileOutputStream(filename));
				}
				PrintWriter @out = new PrintWriter(os);
				string prefix = "BEGIN ";
				@out.Println(prefix + "OPTIONS");
				op.WriteData(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "STATE_INDEX");
				stateIndex.SaveToWriter(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "WORD_INDEX");
				wordIndex.SaveToWriter(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "TAG_INDEX");
				tagIndex.SaveToWriter(@out);
				@out.Println();
				log.Info(".");
				string uwmClazz = ((lex.GetUnknownWordModel() == null) ? "null" : lex.GetUnknownWordModel().GetType().GetCanonicalName());
				@out.Println(prefix + "LEXICON " + uwmClazz);
				lex.WriteData(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "UNARY_GRAMMAR");
				ug.WriteData(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "BINARY_GRAMMAR");
				bg.WriteData(@out);
				@out.Println();
				log.Info(".");
				@out.Println(prefix + "DEPENDENCY_GRAMMAR");
				if (dg != null)
				{
					dg.WriteData(@out);
				}
				@out.Println();
				log.Info(".");
				@out.Flush();
				@out.Close();
				log.Info("done.");
			}
			catch (IOException e)
			{
				log.Info("Trouble saving parser data to ASCII format.");
				throw new RuntimeIOException(e);
			}
		}

		private static void ConfirmBeginBlock(string file, string line)
		{
			if (line == null)
			{
				throw new Exception(file + ": expecting BEGIN block; got end of file.");
			}
			else
			{
				if (!line.StartsWith("BEGIN"))
				{
					throw new Exception(file + ": expecting BEGIN block; got " + line);
				}
			}
		}

		protected internal static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser GetParserFromTextFile(string textFileOrUrl, Options op)
		{
			try
			{
				using (BufferedReader @in = IOUtils.ReaderFromString(textFileOrUrl))
				{
					Timing tim = new Timing();
					Timing.StartTime();
					string line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					op.ReadData(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					IIndex<string> stateIndex = HashIndex.LoadFromReader(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					IIndex<string> wordIndex = HashIndex.LoadFromReader(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					IIndex<string> tagIndex = HashIndex.LoadFromReader(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					ILexicon lex = op.tlpParams.Lex(op, wordIndex, tagIndex);
					string uwmClazz = line.Split(" +")[2];
					if (!uwmClazz.Equals("null"))
					{
						IUnknownWordModel model = ReflectionLoading.LoadByReflection(uwmClazz, op, lex, wordIndex, tagIndex);
						lex.SetUnknownWordModel(model);
					}
					lex.ReadData(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					UnaryGrammar ug = new UnaryGrammar(stateIndex);
					ug.ReadData(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					BinaryGrammar bg = new BinaryGrammar(stateIndex);
					bg.ReadData(@in);
					line = @in.ReadLine();
					ConfirmBeginBlock(textFileOrUrl, line);
					IDependencyGrammar dg = new MLEDependencyGrammar(op.tlpParams, op.directional, op.distance, op.coarseDistance, op.trainOptions.basicCategoryTagsInDependencyGrammar, op, wordIndex, tagIndex);
					dg.ReadData(@in);
					log.Info("Loading parser from text file " + textFileOrUrl + " ... done [" + tim.ToSecondsString() + " sec].");
					return new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser(lex, bg, ug, dg, stateIndex, wordIndex, tagIndex, op);
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser GetParserFromSerializedFile(string serializedFileOrUrl)
		{
			try
			{
				Timing tim = new Timing();
				ObjectInputStream @in = IOUtils.ReadStreamFromString(serializedFileOrUrl);
				Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser pd = LoadModel(@in);
				@in.Close();
				log.Info("Loading parser from serialized file " + serializedFileOrUrl + " ... done [" + tim.ToSecondsString() + " sec].");
				return pd;
			}
			catch (InvalidClassException ice)
			{
				// For this, it's not a good idea to continue and try it as a text file!
				throw new Exception("Invalid class in file: " + serializedFileOrUrl, ice);
			}
			catch (FileNotFoundException fnfe)
			{
				// For this, it's not a good idea to continue and try it as a text file!
				throw new Exception("File not found: " + serializedFileOrUrl, fnfe);
			}
			catch (StreamCorruptedException sce)
			{
				// suppress error message, on the assumption that we've really got
				// a text grammar, and that'll be tried next
				log.Info("Attempting to load " + serializedFileOrUrl + " as a serialized grammar caused error below, but this may just be because it's a text grammar!");
				log.Info(sce);
			}
			catch (Exception e)
			{
				log.Error(e);
			}
			return null;
		}

		private static void PrintOptions(bool train, Options op)
		{
			op.Display();
			if (train)
			{
				op.trainOptions.Display();
			}
			else
			{
				op.testOptions.Display();
			}
			op.tlpParams.Display();
		}

		public static TreeAnnotatorAndBinarizer BuildTrainBinarizer(Options op)
		{
			ITreebankLangParserParams tlpParams = op.tlpParams;
			if (!op.trainOptions.leftToRight)
			{
				return new TreeAnnotatorAndBinarizer(tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), !op.trainOptions.predictSplits, op);
			}
			else
			{
				return new TreeAnnotatorAndBinarizer(tlpParams.HeadFinder(), new LeftHeadFinder(), tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), !op.trainOptions.predictSplits, op);
			}
		}

		public static CompositeTreeTransformer BuildTrainTransformer(Options op)
		{
			TreeAnnotatorAndBinarizer binarizer = BuildTrainBinarizer(op);
			return BuildTrainTransformer(op, binarizer);
		}

		// todo [cdm2015]: This method should be used in TreeAnnotatorAndBinarizer#getAnnotatedBinaryTreebankFromTreebank and moved to that class
		public static CompositeTreeTransformer BuildTrainTransformer(Options op, TreeAnnotatorAndBinarizer binarizer)
		{
			ITreebankLangParserParams tlpParams = op.tlpParams;
			ITreebankLanguagePack tlp = tlpParams.TreebankLanguagePack();
			CompositeTreeTransformer trainTransformer = new CompositeTreeTransformer();
			if (op.trainOptions.preTransformer != null)
			{
				trainTransformer.AddTransformer(op.trainOptions.preTransformer);
			}
			if (op.trainOptions.collinsPunc)
			{
				CollinsPuncTransformer collinsPuncTransformer = new CollinsPuncTransformer(tlp);
				trainTransformer.AddTransformer(collinsPuncTransformer);
			}
			trainTransformer.AddTransformer(binarizer);
			if (op.wordFunction != null)
			{
				ITreeTransformer wordFunctionTransformer = new TreeLeafLabelTransformer(op.wordFunction);
				trainTransformer.AddTransformer(wordFunctionTransformer);
			}
			return trainTransformer;
		}

		/// <returns>A triple of binaryTrainTreebank, binarySecondaryTrainTreebank, binaryTuneTreebank.</returns>
		public static Triple<Treebank, Treebank, Treebank> GetAnnotatedBinaryTreebankFromTreebank(Treebank trainTreebank, Treebank secondaryTreebank, Treebank tuneTreebank, Options op)
		{
			// todo [cdm2015]: This method should be difference-resolved with TreeAnnotatorAndBinarizer#getAnnotatedBinaryTreebankFromTreebank and then deleted
			// setup tree transforms
			ITreebankLangParserParams tlpParams = op.tlpParams;
			ITreebankLanguagePack tlp = tlpParams.TreebankLanguagePack();
			if (op.testOptions.verbose)
			{
				PrintWriter pwErr = tlpParams.Pw(System.Console.Error);
				pwErr.Print("Training ");
				pwErr.Println(trainTreebank.TextualSummary(tlp));
				if (secondaryTreebank != null)
				{
					pwErr.Print("Secondary training ");
					pwErr.Println(secondaryTreebank.TextualSummary(tlp));
				}
			}
			log.Info("Binarizing trees...");
			TreeAnnotatorAndBinarizer binarizer = BuildTrainBinarizer(op);
			CompositeTreeTransformer trainTransformer = BuildTrainTransformer(op, binarizer);
			Treebank wholeTreebank;
			if (secondaryTreebank == null)
			{
				wholeTreebank = trainTreebank;
			}
			else
			{
				wholeTreebank = new CompositeTreebank(trainTreebank, secondaryTreebank);
			}
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(wholeTreebank, op.trainOptions.tagSelectiveSplit, 0, op.trainOptions.selectiveSplitCutOff, op.trainOptions.tagSelectiveSplitCutOff, tlp);
				RemoveDeleteSplittersFromSplitters(tlp, op);
				if (op.testOptions.verbose)
				{
					IList<string> list = new List<string>(op.trainOptions.splitters);
					list.Sort();
					log.Info("Parent split categories: " + list);
				}
			}
			if (op.trainOptions.selectivePostSplit)
			{
				// Do all the transformations once just to learn selective splits on annotated categories
				ITreeTransformer myTransformer = new TreeAnnotator(tlpParams.HeadFinder(), tlpParams, op);
				wholeTreebank = wholeTreebank.Transform(myTransformer);
				op.trainOptions.postSplitters = ParentAnnotationStats.GetSplitCategories(wholeTreebank, true, 0, op.trainOptions.selectivePostSplitCutOff, op.trainOptions.tagSelectivePostSplitCutOff, tlp);
				if (op.testOptions.verbose)
				{
					log.Info("Parent post annotation split categories: " + op.trainOptions.postSplitters);
				}
			}
			if (op.trainOptions.hSelSplit)
			{
				// We run through all the trees once just to gather counts for hSelSplit!
				int ptt = op.trainOptions.printTreeTransformations;
				op.trainOptions.printTreeTransformations = 0;
				binarizer.SetDoSelectiveSplit(false);
				foreach (Tree tree in wholeTreebank)
				{
					trainTransformer.TransformTree(tree);
				}
				binarizer.SetDoSelectiveSplit(true);
				op.trainOptions.printTreeTransformations = ptt;
			}
			// we've done all the setup now. here's where the train treebank is transformed.
			trainTreebank = trainTreebank.Transform(trainTransformer);
			if (secondaryTreebank != null)
			{
				secondaryTreebank = secondaryTreebank.Transform(trainTransformer);
			}
			if (op.trainOptions.printAnnotatedStateCounts)
			{
				binarizer.PrintStateCounts();
			}
			if (op.trainOptions.printAnnotatedRuleCounts)
			{
				binarizer.PrintRuleCounts();
			}
			if (tuneTreebank != null)
			{
				tuneTreebank = tuneTreebank.Transform(trainTransformer);
			}
			Timing.Tick("done.");
			if (op.testOptions.verbose)
			{
				binarizer.DumpStats();
			}
			return new Triple<Treebank, Treebank, Treebank>(trainTreebank, secondaryTreebank, tuneTreebank);
		}

		private static void RemoveDeleteSplittersFromSplitters(ITreebankLanguagePack tlp, Options op)
		{
			if (op.trainOptions.deleteSplitters != null)
			{
				IList<string> deleted = new List<string>();
				foreach (string del in op.trainOptions.deleteSplitters)
				{
					string baseDel = tlp.BasicCategory(del);
					bool checkBasic = del.Equals(baseDel);
					for (IEnumerator<string> it = op.trainOptions.splitters.GetEnumerator(); it.MoveNext(); )
					{
						string elem = it.Current;
						string baseElem = tlp.BasicCategory(elem);
						bool delStr = checkBasic && baseElem.Equals(baseDel) || elem.Equals(del);
						if (delStr)
						{
							it.Remove();
							deleted.Add(elem);
						}
					}
				}
				if (op.testOptions.verbose)
				{
					log.Info("Removed from vertical splitters: " + deleted);
				}
			}
		}

		// TODO: Make below method work with arbitrarily large secondary treebank via iteration
		// TODO: Have weight implemented for training lexicon
		/// <summary>
		/// A method for training from two different treebanks, the second of which is presumed
		/// to be orders of magnitude larger.
		/// </summary>
		/// <remarks>
		/// A method for training from two different treebanks, the second of which is presumed
		/// to be orders of magnitude larger.
		/// <p/>
		/// Trees are not read into memory but processed as they are read from disk.
		/// <p/>
		/// A weight (typically &lt;= 1) can be put on the second treebank.
		/// </remarks>
		/// <param name="trainTreebank">A treebank to train from</param>
		/// <param name="secondaryTrainTreebank">Another treebank to train from</param>
		/// <param name="weight">
		/// A weight factor to give the secondary treebank. If the weight
		/// is 0.25, each example in the secondaryTrainTreebank will be treated as
		/// 1/4 of an example sentence.
		/// </param>
		/// <param name="compactor">A class for compacting grammars. May be null.</param>
		/// <param name="op">Options for how the grammar is built from the treebank</param>
		/// <param name="tuneTreebank">A treebank to tune free params on (may be null)</param>
		/// <param name="extraTaggedWords">A list of words to add to the Lexicon</param>
		/// <returns>The trained LexicalizedParser</returns>
		public static Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser GetParserFromTreebank(Treebank trainTreebank, Treebank secondaryTrainTreebank, double weight, GrammarCompactor compactor, Options op, Treebank tuneTreebank, IList<IList<TaggedWord
			>> extraTaggedWords)
		{
			// log.info("Currently " + new Date()); // now printed when command-line args are printed
			PrintOptions(true, op);
			Timing.StartTime();
			Triple<Treebank, Treebank, Treebank> treebanks = TreeAnnotatorAndBinarizer.GetAnnotatedBinaryTreebankFromTreebank(trainTreebank, secondaryTrainTreebank, tuneTreebank, op);
			Timing.Tick("done.");
			Treebank trainTreebankRaw = trainTreebank;
			trainTreebank = treebanks.First();
			secondaryTrainTreebank = treebanks.Second();
			tuneTreebank = treebanks.Third();
			// +1 to account for the boundary symbol
			trainTreebank = new FilteringTreebank(trainTreebank, new LengthTreeFilter(op.trainOptions.trainLengthLimit + 1));
			if (secondaryTrainTreebank != null)
			{
				secondaryTrainTreebank = new FilteringTreebank(secondaryTrainTreebank, new LengthTreeFilter(op.trainOptions.trainLengthLimit + 1));
			}
			if (tuneTreebank != null)
			{
				tuneTreebank = new FilteringTreebank(tuneTreebank, new LengthTreeFilter(op.trainOptions.trainLengthLimit + 1));
			}
			IIndex<string> stateIndex;
			IIndex<string> wordIndex;
			IIndex<string> tagIndex;
			Pair<UnaryGrammar, BinaryGrammar> bgug;
			ILexicon lex;
			if (op.trainOptions.predictSplits)
			{
				SplittingGrammarExtractor extractor = new SplittingGrammarExtractor(op);
				log.Info("Extracting PCFG...");
				// TODO: make use of the tagged text
				if (secondaryTrainTreebank == null)
				{
					extractor.Extract(trainTreebank);
				}
				else
				{
					extractor.Extract(trainTreebank, 1.0, secondaryTrainTreebank, weight);
				}
				bgug = extractor.bgug;
				lex = extractor.lex;
				stateIndex = extractor.stateIndex;
				wordIndex = extractor.wordIndex;
				tagIndex = extractor.tagIndex;
				Timing.Tick("done.");
			}
			else
			{
				stateIndex = new HashIndex<string>();
				wordIndex = new HashIndex<string>();
				tagIndex = new HashIndex<string>();
				// extract grammars
				BinaryGrammarExtractor bgExtractor = new BinaryGrammarExtractor(op, stateIndex);
				// Extractor lexExtractor = new LexiconExtractor();
				//TreeExtractor uwmExtractor = new UnknownWordModelExtractor(trainTreebank.size());
				log.Info("Extracting PCFG...");
				if (secondaryTrainTreebank == null)
				{
					bgug = bgExtractor.Extract(trainTreebank);
				}
				else
				{
					bgug = bgExtractor.Extract(trainTreebank, 1.0, secondaryTrainTreebank, weight);
				}
				Timing.Tick("done.");
				log.Info("Extracting Lexicon...");
				lex = op.tlpParams.Lex(op, wordIndex, tagIndex);
				double trainSize = trainTreebank.Count;
				if (secondaryTrainTreebank != null)
				{
					trainSize += (secondaryTrainTreebank.Count * weight);
				}
				if (extraTaggedWords != null)
				{
					trainSize += extraTaggedWords.Count;
				}
				lex.InitializeTraining(trainSize);
				// wsg2012: The raw treebank has CoreLabels, which we need for FactoredLexicon
				// training. If TreeAnnotator is updated so that it produces CoreLabels, then we can
				// remove the trainTreebankRaw.
				lex.Train(trainTreebank, trainTreebankRaw);
				if (secondaryTrainTreebank != null)
				{
					lex.Train(secondaryTrainTreebank, weight);
				}
				if (extraTaggedWords != null)
				{
					foreach (IList<TaggedWord> sentence in extraTaggedWords)
					{
						// TODO: specify a weight?
						lex.TrainUnannotated(sentence, 1.0);
					}
				}
				lex.FinishTraining();
				Timing.Tick("done.");
			}
			//TODO: wsg2011 Not sure if this should come before or after
			//grammar compaction
			if (op.trainOptions.ruleSmoothing)
			{
				log.Info("Smoothing PCFG...");
				Func<Pair<UnaryGrammar, BinaryGrammar>, Pair<UnaryGrammar, BinaryGrammar>> smoother = new LinearGrammarSmoother(op.trainOptions, stateIndex, tagIndex);
				bgug = smoother.Apply(bgug);
				Timing.Tick("done.");
			}
			if (compactor != null)
			{
				log.Info("Compacting grammar...");
				Triple<IIndex<string>, UnaryGrammar, BinaryGrammar> compacted = compactor.CompactGrammar(bgug, stateIndex);
				stateIndex = compacted.First();
				bgug.SetFirst(compacted.Second());
				bgug.SetSecond(compacted.Third());
				Timing.Tick("done.");
			}
			log.Info("Compiling grammar...");
			BinaryGrammar bg = bgug.second;
			bg.SplitRules();
			UnaryGrammar ug = bgug.first;
			ug.PurgeRules();
			Timing.Tick("done");
			IDependencyGrammar dg = null;
			if (op.doDep)
			{
				log.Info("Extracting Dependencies...");
				AbstractTreeExtractor<IDependencyGrammar> dgExtractor = new MLEDependencyGrammarExtractor(op, wordIndex, tagIndex);
				if (secondaryTrainTreebank == null)
				{
					dg = dgExtractor.Extract(trainTreebank);
				}
				else
				{
					dg = dgExtractor.Extract(trainTreebank, 1.0, secondaryTrainTreebank, weight);
				}
				//log.info("Extracting Unknown Word Model...");
				//UnknownWordModel uwm = (UnknownWordModel)uwmExtractor.extract(trainTreebank);
				//Timing.tick("done.");
				Timing.Tick("done.");
				if (tuneTreebank != null)
				{
					log.Info("Tuning Dependency Model...");
					dg.SetLexicon(lex);
					// MG2008: needed if using PwGt model
					dg.Tune(tuneTreebank);
					Timing.Tick("done.");
				}
			}
			log.Info("Done training parser.");
			if (op.trainOptions.trainTreeFile != null)
			{
				try
				{
					log.Info("Writing out binary trees to " + op.trainOptions.trainTreeFile + "...");
					IOUtils.WriteObjectToFile(trainTreebank, op.trainOptions.trainTreeFile);
					IOUtils.WriteObjectToFile(secondaryTrainTreebank, op.trainOptions.trainTreeFile);
					Timing.Tick("done.");
				}
				catch (Exception)
				{
					log.Info("Problem writing out binary trees.");
				}
			}
			return new Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser(lex, bg, ug, dg, stateIndex, wordIndex, tagIndex, op);
		}

		/// <summary>
		/// This will set options to the parser, in a way exactly equivalent to
		/// passing in the same sequence of command-line arguments.
		/// </summary>
		/// <remarks>
		/// This will set options to the parser, in a way exactly equivalent to
		/// passing in the same sequence of command-line arguments.  This is a useful
		/// convenience method when building a parser programmatically. The options
		/// passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign.
		/// <p/>
		/// <i>Notes:</i> This can be used to set parsing-time flags for a
		/// serialized parser.  You can also still change things serialized
		/// in Options, but this will probably degrade parsing performance.
		/// The vast majority of command line flags can be passed to this
		/// method, but you cannot pass in options that specify the treebank
		/// or grammar to be loaded, the grammar to be written, trees or
		/// files to be parsed or details of their encoding, nor the
		/// TreebankLangParserParams (
		/// <c>-tLPP</c>
		/// ) to use. The
		/// TreebankLangParserParams should be set up on construction of a
		/// LexicalizedParser, by constructing an Options that uses
		/// the required TreebankLangParserParams, and passing that to a
		/// LexicalizedParser constructor.  Note that despite this
		/// method being an instance method, many flags are actually set as
		/// static class variables.
		/// </remarks>
		/// <param name="flags">
		/// Arguments to the parser, for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		public override void SetOptionFlags(params string[] flags)
		{
			op.SetOptions(flags);
		}

		/// <summary>A main program for using the parser with various options.</summary>
		/// <remarks>
		/// A main program for using the parser with various options.
		/// This program can be used for building and serializing
		/// a parser from treebank data, for parsing sentences from a file
		/// or URL using a serialized or text grammar parser,
		/// and (mainly for parser quality testing)
		/// for training and testing a parser on a treebank all in one go.
		/// <p>
		/// Sample Usages:
		/// <ul>
		/// <li> <b>Train a parser (saved to <i>serializedGrammarFilename</i>)
		/// from a directory of trees (<i>trainFilesPath</i>, with an optional <i>fileRange</i>, e.g., 0-1000):</b>
		/// <c>java -mx1500m edu.stanford.nlp.parser.lexparser.LexicalizedParser [-v] -train trainFilesPath [fileRange] -saveToSerializedFile serializedGrammarFilename</c>
		/// </li>
		/// <li> <b>Train a parser (not saved) from a directory of trees, and test it (reporting scores) on a directory of trees</b>
		/// <c>java -mx1500m edu.stanford.nlp.parser.lexparser.LexicalizedParser [-v] -train trainFilesPath [fileRange] -testTreebank testFilePath [fileRange]</c>
		/// </li>
		/// <li> <b>Parse one or more files, given a serialized grammar and a list of files</b>
		/// <c>java -mx512m edu.stanford.nlp.parser.lexparser.LexicalizedParser [-v] serializedGrammarPath filename [filename]*</c>
		/// </li>
		/// <li> <b>Test and report scores for a serialized grammar on trees in an output directory</b>
		/// <c>java -mx512m edu.stanford.nlp.parser.lexparser.LexicalizedParser [-v] -loadFromSerializedFile serializedGrammarPath -testTreebank testFilePath [fileRange]</c>
		/// </li>
		/// </ul>
		/// <p>
		/// If the
		/// <c>serializedGrammarPath</c>
		/// ends in
		/// <c>.gz</c>
		/// ,
		/// then the grammar is written and read as a compressed file (GZip).
		/// If the
		/// <c>serializedGrammarPath</c>
		/// is a URL, starting with
		/// <c>http://</c>
		/// , then the parser is read from the URL.
		/// A fileRange specifies a numeric value that must be included within a
		/// filename for it to be used in training or testing (this works well with
		/// most current treebanks).  It can be specified like a range of pages to be
		/// printed, for instance as
		/// <c>200-2199</c>
		/// or
		/// <c>1-300,500-725,9000</c>
		/// or just as
		/// <c>1</c>
		/// (if all your
		/// trees are in a single file, either omit this parameter or just give a dummy
		/// argument such as
		/// <c>0</c>
		/// ).
		/// If the filename to parse is "-" then the parser parses from stdin.
		/// If no files are supplied to parse, then a hardwired sentence
		/// is parsed.
		/// <p>
		/// The parser can write a grammar as either a serialized Java object file
		/// or in a text format (or as both), specified with the following options:
		/// <blockquote>
		/// <c>
		/// java edu.stanford.nlp.parser.lexparser.LexicalizedParser
		/// [-v] -train
		/// trainFilesPath [fileRange] [-saveToSerializedFile grammarPath]
		/// [-saveToTextFile grammarPath]
		/// </c>
		/// </blockquote>
		/// <p>
		/// In the same position as the verbose flag (
		/// <c>-v</c>
		/// ), many other
		/// options can be specified.  The most useful to an end user are:
		/// <ul>
		/// <LI>
		/// <c>-tLPP class</c>
		/// Specify a different
		/// TreebankLangParserParams, for when using a different language or
		/// treebank (the default is English Penn Treebank). <i>This option MUST occur
		/// before any other language-specific options that are used (or else they
		/// are ignored!).</i>
		/// (It's usually a good idea to specify this option even when loading a
		/// serialized grammar; it is necessary if the language pack specifies a
		/// needed character encoding or you wish to specify language-specific
		/// options on the command line.)</LI>
		/// <LI>
		/// <c>-encoding charset</c>
		/// Specify the character encoding of the
		/// input and output files.  This will override the value in the
		/// <c>TreebankLangParserParams</c>
		/// , provided this option appears
		/// <i>after</i> any
		/// <c>-tLPP</c>
		/// option.</LI>
		/// <LI>
		/// <c>-tokenized</c>
		/// Says that the input is already separated
		/// into whitespace-delimited tokens.  If this option is specified, any
		/// tokenizer specified for the language is ignored, and a universal (Unicode)
		/// tokenizer, which divides only on whitespace, is used.
		/// Unless you also specify
		/// <c>-escaper</c>
		/// , the tokens <i>must</i> all be correctly
		/// tokenized tokens of the appropriate treebank for the parser to work
		/// well (for instance, if using the Penn English Treebank, you must have
		/// coded "(" as "-LRB-", etc.). (Note: we do not use the backslash escaping
		/// in front of / and * that appeared in Penn Treebank releases through 1999.)</li>
		/// <li>
		/// <c>-escaper class</c>
		/// Specify a class of type
		/// <see cref="Java.Util.Function.Func{T, R}"/>
		/// &lt;List&lt;HasWord&gt;,List&lt;HasWord&gt;&gt; to do
		/// customized escaping of tokenized text.  This class will be run over the
		/// tokenized text and can fix the representation of tokens. For instance,
		/// it could change "(" to "-LRB-" for the Penn English Treebank.  A
		/// provided escaper that does such things for the Penn English Treebank is
		/// <c>edu.stanford.nlp.process.PTBEscapingProcessor</c>
		/// <li>
		/// <c>-tokenizerFactory class</c>
		/// Specifies a
		/// TokenizerFactory class to be used for tokenization</li>
		/// <li>
		/// <c>-tokenizerOptions options</c>
		/// Specifies options to a
		/// TokenizerFactory class to be used for tokenization.   A comma-separated
		/// list. For PTBTokenizer, options of interest include
		/// <c>americanize=false</c>
		/// and
		/// <c>asciiQuotes</c>
		/// (for German).
		/// Note that any choice of tokenizer options that conflicts with the
		/// tokenization used in the parser training data will likely degrade parser
		/// performance. </li>
		/// <li>
		/// <c>-sentences token</c>
		/// Specifies a token that marks sentence
		/// boundaries.  A value of
		/// <c>newline</c>
		/// causes sentence breaking on
		/// newlines.  A value of
		/// <c>onePerElement</c>
		/// causes each element
		/// (using the XML
		/// <c>-parseInside</c>
		/// option) to be treated as a
		/// sentence. All other tokens will be interpreted literally, and must be
		/// exactly the same as tokens returned by the tokenizer.  For example,
		/// you might specify "|||" and put that symbol sequence as a token between
		/// sentences.
		/// If no explicit sentence breaking option is chosen, sentence breaking
		/// is done based on a set of language-particular sentence-ending patterns.
		/// </li>
		/// <LI>
		/// <c>-parseInside element</c>
		/// Specifies that parsing should only
		/// be done for tokens inside the indicated XML-style
		/// elements (done as simple pattern matching, rather than XML parsing).
		/// For example, if this is specified as
		/// <c>sentence</c>
		/// , then
		/// the text inside the
		/// <c>sentence</c>
		/// element
		/// would be parsed.
		/// Using "-parseInside s" gives you support for the input format of
		/// Charniak's parser. Sentences cannot span elements. Whether the
		/// contents of the element are treated as one sentence or potentially
		/// multiple sentences is controlled by the
		/// <c>-sentences</c>
		/// flag.
		/// The default is potentially multiple sentences.
		/// This option gives support for extracting and parsing
		/// text from very simple SGML and XML documents, and is provided as a
		/// user convenience for that purpose. If you want to really parse XML
		/// documents before NLP parsing them, you should use an XML parser, and then
		/// call to a LexicalizedParser on appropriate CDATA.
		/// <LI>
		/// <c>-tagSeparator char</c>
		/// Specifies to look for tags on words
		/// following the word and separated from it by a special character
		/// <c>char</c>
		/// .  For instance, many tagged corpora have the
		/// representation "house/NN" and you would use
		/// <c>-tagSeparator /</c>
		/// .
		/// Notes: This option requires that the input be pretokenized.
		/// The separator has to be only a single character, and there is no
		/// escaping mechanism. However, splitting is done on the <i>last</i>
		/// instance of the character in the token, so that cases like
		/// "3\/4/CD" are handled correctly.  The parser will in all normal
		/// circumstances use the tag you provide, but will override it in the
		/// case of very common words in cases where the tag that you provide
		/// is not one that it regards as a possible tagging for the word.
		/// The parser supports a format where only some of the words in a sentence
		/// have a tag (if you are calling the parser programmatically, you indicate
		/// them by having them implement the
		/// <c>HasTag</c>
		/// interface).
		/// You can do this at the command-line by only having tags after some words,
		/// but you are limited by the fact that there is no way to escape the
		/// tagSeparator character.</LI>
		/// <LI>
		/// <c>-maxLength leng</c>
		/// Specify the longest sentence that
		/// will be parsed (and hence indirectly the amount of memory
		/// needed for the parser). If this is not specified, the parser will
		/// try to dynamically grow its parse chart when long sentence are
		/// encountered, but may run out of memory trying to do so.</LI>
		/// <LI>
		/// <c>-outputFormat styles</c>
		/// Choose the style(s) of output
		/// sentences:
		/// <c>penn</c>
		/// for prettyprinting as in the Penn
		/// treebank files, or
		/// <c>oneline</c>
		/// for printing sentences one
		/// per line,
		/// <c>words</c>
		/// ,
		/// <c>wordsAndTags</c>
		/// ,
		/// <c>dependencies</c>
		/// ,
		/// <c>typedDependencies</c>
		/// ,
		/// or
		/// <c>typedDependenciesCollapsed</c>
		/// .
		/// Multiple options may be specified as a comma-separated
		/// list.  See TreePrint class for further documentation.</LI>
		/// <LI>
		/// <c>-outputFormatOptions</c>
		/// Provide options that control the
		/// behavior of various
		/// <c>-outputFormat</c>
		/// choices, such as
		/// <c>lexicalize</c>
		/// ,
		/// <c>stem</c>
		/// ,
		/// <c>markHeadNodes</c>
		/// ,
		/// or
		/// <c>xml</c>
		/// .
		/// <see cref="Edu.Stanford.Nlp.Trees.TreePrint"/>
		/// Options are specified as a comma-separated list.</LI>
		/// <LI>
		/// <c>-writeOutputFiles</c>
		/// Write output files corresponding
		/// to the input files, with the same name but a
		/// <c>".stp"</c>
		/// file extension.  The format of these files depends on the
		/// <c>outputFormat</c>
		/// option.  (If not specified, output is sent
		/// to stdout.)</LI>
		/// <LI>
		/// <c>-outputFilesExtension</c>
		/// The extension that is appended to
		/// the filename that is being parsed to produce an output file name (with the
		/// -writeOutputFiles option). The default is
		/// <c>stp</c>
		/// .  Don't
		/// include the period.
		/// <LI>
		/// <c>-outputFilesDirectory</c>
		/// The directory in which output
		/// files are written (when the -writeOutputFiles option is specified).
		/// If not specified, output files are written in the same directory as the
		/// input files.
		/// <LI>
		/// <c>-nthreads</c>
		/// Parsing files and testing on treebanks
		/// can use multiple threads.  This option tells the parser how many
		/// threads to use.  A negative number indicates to use as many
		/// threads as the machine has cores.
		/// </ul>
		/// See also the package documentation for more details and examples of use.
		/// </remarks>
		/// <param name="args">Command line arguments, as above</param>
		public static void Main(string[] args)
		{
			bool train = false;
			bool saveToSerializedFile = false;
			bool saveToTextFile = false;
			string serializedInputFileOrUrl = null;
			string textInputFileOrUrl = null;
			string serializedOutputFileOrUrl = null;
			string textOutputFileOrUrl = null;
			string treebankPath = null;
			Treebank testTreebank = null;
			Treebank tuneTreebank = null;
			string testPath = null;
			IFileFilter testFilter = null;
			string tunePath = null;
			IFileFilter tuneFilter = null;
			IFileFilter trainFilter = null;
			string secondaryTreebankPath = null;
			double secondaryTreebankWeight = 1.0;
			IFileFilter secondaryTrainFilter = null;
			// variables needed to process the files to be parsed
			ITokenizerFactory<IHasWord> tokenizerFactory = null;
			string tokenizerOptions = null;
			string tokenizerFactoryClass = null;
			string tokenizerMethod = null;
			bool tokenized = false;
			// whether or not the input file has already been tokenized
			Func<IList<IHasWord>, IList<IHasWord>> escaper = null;
			string tagDelimiter = null;
			string sentenceDelimiter = null;
			string elementDelimiter = null;
			int argIndex = 0;
			if (args.Length < 1)
			{
				log.Info("Basic usage (see Javadoc for more): java edu.stanford.nlp.parser.lexparser.LexicalizedParser parserFileOrUrl filename*");
				return;
			}
			Options op = new Options();
			IList<string> optionArgs = new List<string>();
			string encoding = null;
			// while loop through option arguments
			while (argIndex < args.Length && args[argIndex][0] == '-')
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-trainTreebank"))
				{
					train = true;
					Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-train");
					argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
					treebankPath = treebankDescription.First();
					trainFilter = treebankDescription.Second();
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train2"))
					{
						// train = true;     // cdm july 2005: should require -train for this
						Triple<string, IFileFilter, double> treebankDescription = ArgUtils.GetWeightedTreebankDescription(args, argIndex, "-train2");
						argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
						secondaryTreebankPath = treebankDescription.First();
						secondaryTrainFilter = treebankDescription.Second();
						secondaryTreebankWeight = treebankDescription.Third();
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tLPP") && (argIndex + 1 < args.Length))
						{
							try
							{
								op.tlpParams = (ITreebankLangParserParams)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[argIndex + 1]));
							}
							catch (TypeLoadException e)
							{
								log.Info("Class not found: " + args[argIndex + 1]);
								throw new Exception(e);
							}
							catch (InstantiationException e)
							{
								log.Info("Couldn't instantiate: " + args[argIndex + 1] + ": " + e.ToString());
								throw new Exception(e);
							}
							catch (MemberAccessException e)
							{
								log.Info("Illegal access" + e);
								throw new Exception(e);
							}
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-encoding"))
							{
								// sets encoding for TreebankLangParserParams
								// redone later to override any serialized parser one read in
								encoding = args[argIndex + 1];
								op.tlpParams.SetInputEncoding(encoding);
								op.tlpParams.SetOutputEncoding(encoding);
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tokenized"))
								{
									tokenized = true;
									argIndex += 1;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-escaper"))
									{
										try
										{
											escaper = ReflectionLoading.LoadByReflection(args[argIndex + 1]);
										}
										catch (Exception e)
										{
											log.Info("Couldn't instantiate escaper " + args[argIndex + 1] + ": " + e);
										}
										argIndex += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tokenizerOptions"))
										{
											tokenizerOptions = args[argIndex + 1];
											argIndex += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tokenizerFactory"))
											{
												tokenizerFactoryClass = args[argIndex + 1];
												argIndex += 2;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tokenizerMethod"))
												{
													tokenizerMethod = args[argIndex + 1];
													argIndex += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-sentences"))
													{
														sentenceDelimiter = args[argIndex + 1];
														if (Sharpen.Runtime.EqualsIgnoreCase(sentenceDelimiter, "newline"))
														{
															sentenceDelimiter = "\n";
														}
														argIndex += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parseInside"))
														{
															elementDelimiter = args[argIndex + 1];
															argIndex += 2;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tagSeparator"))
															{
																tagDelimiter = args[argIndex + 1];
																argIndex += 2;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-loadFromSerializedFile") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
																{
																	// load the parser from a binary serialized file
																	// the next argument must be the path to the parser file
																	serializedInputFileOrUrl = args[argIndex + 1];
																	argIndex += 2;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-loadFromTextFile"))
																	{
																		// load the parser from declarative text file
																		// the next argument must be the path to the parser file
																		textInputFileOrUrl = args[argIndex + 1];
																		argIndex += 2;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveToSerializedFile"))
																		{
																			saveToSerializedFile = true;
																			if (ArgUtils.NumSubArgs(args, argIndex) < 1)
																			{
																				log.Info("Missing path: -saveToSerialized filename");
																			}
																			else
																			{
																				serializedOutputFileOrUrl = args[argIndex + 1];
																			}
																			argIndex += 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveToTextFile"))
																			{
																				// save the parser to declarative text file
																				saveToTextFile = true;
																				textOutputFileOrUrl = args[argIndex + 1];
																				argIndex += 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveTrainTrees"))
																				{
																					// save the training trees to a binary file
																					op.trainOptions.trainTreeFile = args[argIndex + 1];
																					argIndex += 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-testTreebank") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-test"))
																					{
																						Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-test");
																						argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
																						testPath = treebankDescription.First();
																						testFilter = treebankDescription.Second();
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tune"))
																						{
																							Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-tune");
																							argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
																							tunePath = treebankDescription.First();
																							tuneFilter = treebankDescription.Second();
																						}
																						else
																						{
																							int oldIndex = argIndex;
																							argIndex = op.SetOptionOrWarn(args, argIndex);
																							Sharpen.Collections.AddAll(optionArgs, Arrays.AsList(args).SubList(oldIndex, argIndex));
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			// end while loop through arguments
			// all other arguments are order dependent and
			// are processed in order below
			if (tuneFilter != null || tunePath != null)
			{
				if (tunePath == null)
				{
					if (treebankPath == null)
					{
						throw new Exception("No tune treebank path specified...");
					}
					else
					{
						log.Info("No tune treebank path specified.  Using train path: \"" + treebankPath + '\"');
						tunePath = treebankPath;
					}
				}
				tuneTreebank = op.tlpParams.TestMemoryTreebank();
				tuneTreebank.LoadPath(tunePath, tuneFilter);
			}
			if (!train && op.testOptions.verbose)
			{
				StringUtils.LogInvocationString(log, args);
			}
			Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser lp;
			// always initialized in next if-then-else block
			if (train)
			{
				StringUtils.LogInvocationString(log, args);
				// so we train a parser using the treebank
				GrammarCompactor compactor = null;
				if (op.trainOptions.CompactGrammar() == 3)
				{
					compactor = new ExactGrammarCompactor(op, false, false);
				}
				Treebank trainTreebank = MakeTreebank(treebankPath, op, trainFilter);
				Treebank secondaryTrainTreebank = null;
				if (secondaryTreebankPath != null)
				{
					secondaryTrainTreebank = MakeSecondaryTreebank(secondaryTreebankPath, op, secondaryTrainFilter);
				}
				IList<IList<TaggedWord>> extraTaggedWords = null;
				if (op.trainOptions.taggedFiles != null)
				{
					extraTaggedWords = new List<IList<TaggedWord>>();
					IList<TaggedFileRecord> fileRecords = TaggedFileRecord.CreateRecords(new Properties(), op.trainOptions.taggedFiles);
					foreach (TaggedFileRecord record in fileRecords)
					{
						foreach (IList<TaggedWord> sentence in record.Reader())
						{
							extraTaggedWords.Add(sentence);
						}
					}
				}
				lp = GetParserFromTreebank(trainTreebank, secondaryTrainTreebank, secondaryTreebankWeight, compactor, op, tuneTreebank, extraTaggedWords);
			}
			else
			{
				if (textInputFileOrUrl != null)
				{
					// so we load the parser from a text grammar file
					lp = GetParserFromTextFile(textInputFileOrUrl, op);
				}
				else
				{
					// so we load a serialized parser
					if (serializedInputFileOrUrl == null && argIndex < args.Length)
					{
						// the next argument must be the path to the serialized parser
						serializedInputFileOrUrl = args[argIndex];
						argIndex++;
					}
					if (serializedInputFileOrUrl == null)
					{
						log.Info("No grammar specified, exiting...");
						return;
					}
					string[] extraArgs = new string[optionArgs.Count];
					extraArgs = Sharpen.Collections.ToArray(optionArgs, extraArgs);
					try
					{
						lp = LoadModel(serializedInputFileOrUrl, op, extraArgs);
						op = lp.op;
					}
					catch (ArgumentException e)
					{
						log.Info("Error loading parser, exiting...");
						throw;
					}
				}
			}
			// set up tokenizerFactory with options if provided
			if (tokenizerFactoryClass != null || tokenizerOptions != null)
			{
				try
				{
					if (tokenizerFactoryClass != null)
					{
						Type clazz = ErasureUtils.UncheckedCast(Sharpen.Runtime.GetType(tokenizerFactoryClass));
						MethodInfo factoryMethod;
						if (tokenizerOptions != null)
						{
							factoryMethod = clazz.GetMethod(tokenizerMethod != null ? tokenizerMethod : "newWordTokenizerFactory", typeof(string));
							tokenizerFactory = ErasureUtils.UncheckedCast(factoryMethod.Invoke(null, tokenizerOptions));
						}
						else
						{
							factoryMethod = clazz.GetMethod(tokenizerMethod != null ? tokenizerMethod : "newTokenizerFactory");
							tokenizerFactory = ErasureUtils.UncheckedCast(factoryMethod.Invoke(null));
						}
					}
					else
					{
						// have options but no tokenizer factory.  use the parser
						// langpack's factory and set its options
						tokenizerFactory = lp.op.Langpack().GetTokenizerFactory();
						tokenizerFactory.SetOptions(tokenizerOptions);
					}
				}
				catch (ReflectiveOperationException e)
				{
					log.Info("Couldn't instantiate TokenizerFactory " + tokenizerFactoryClass + " with options " + tokenizerOptions);
					throw new Exception(e);
				}
			}
			// the following has to go after reading parser to make sure
			// op and tlpParams are the same for train and test
			// THIS IS BUTT UGLY BUT IT STOPS USER SPECIFIED ENCODING BEING
			// OVERWRITTEN BY ONE SPECIFIED IN SERIALIZED PARSER
			if (encoding != null)
			{
				op.tlpParams.SetInputEncoding(encoding);
				op.tlpParams.SetOutputEncoding(encoding);
			}
			if (testFilter != null || testPath != null)
			{
				if (testPath == null)
				{
					if (treebankPath == null)
					{
						throw new Exception("No test treebank path specified...");
					}
					else
					{
						log.Info("No test treebank path specified.  Using train path: \"" + treebankPath + '\"');
						testPath = treebankPath;
					}
				}
				testTreebank = op.tlpParams.TestMemoryTreebank();
				testTreebank.LoadPath(testPath, testFilter);
			}
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(op.tlpParams.SisterSplitters()));
			// at this point we should be sure that op.tlpParams is
			// set appropriately (from command line, or from grammar file),
			// and will never change again.  -- Roger
			// Now what do we do with the parser we've made
			if (saveToTextFile)
			{
				// save the parser to textGrammar format
				if (textOutputFileOrUrl != null)
				{
					lp.SaveParserToTextFile(textOutputFileOrUrl);
				}
				else
				{
					log.Info("Usage: must specify a text grammar output path");
				}
			}
			if (saveToSerializedFile)
			{
				if (serializedOutputFileOrUrl != null)
				{
					lp.SaveParserToSerialized(serializedOutputFileOrUrl);
				}
				else
				{
					if (textOutputFileOrUrl == null && testTreebank == null)
					{
						// no saving/parsing request has been specified
						log.Info("usage: " + "java edu.stanford.nlp.parser.lexparser.LexicalizedParser " + "-train trainFilesPath [fileRange] -saveToSerializedFile serializedParserFilename");
					}
				}
			}
			if (op.testOptions.verbose || train)
			{
				// Tell the user a little or a lot about what we have made
				// get lexicon size separately as it may have its own prints in it....
				string lexNumRules = lp.lex != null ? int.ToString(lp.lex.NumRules()) : string.Empty;
				log.Info("Grammar\tStates\tTags\tWords\tUnaryR\tBinaryR\tTaggings");
				log.Info("Grammar\t" + lp.stateIndex.Size() + '\t' + lp.tagIndex.Size() + '\t' + lp.wordIndex.Size() + '\t' + (lp.ug != null ? lp.ug.NumRules() : string.Empty) + '\t' + (lp.bg != null ? lp.bg.NumRules() : string.Empty) + '\t' + lexNumRules);
				log.Info("ParserPack is " + op.tlpParams.GetType().FullName);
				log.Info("Lexicon is " + lp.lex.GetType().FullName);
				if (op.testOptions.verbose)
				{
					log.Info("Tags are: " + lp.tagIndex);
				}
				// log.info("States are: " + lp.pd.stateIndex); // This is too verbose. It was already printed out by the below printOptions command if the flag -printStates is given (at training time)!
				PrintOptions(false, op);
			}
			if (testTreebank != null)
			{
				// test parser on treebank
				EvaluateTreebank evaluator = new EvaluateTreebank(lp);
				evaluator.TestOnTreebank(testTreebank);
			}
			else
			{
				if (argIndex >= args.Length)
				{
					// no more arguments, so we just parse our own test sentence
					PrintWriter pwOut = op.tlpParams.Pw();
					PrintWriter pwErr = op.tlpParams.Pw(System.Console.Error);
					IParserQuery pq = lp.ParserQuery();
					if (pq.Parse(op.tlpParams.DefaultTestSentence()))
					{
						lp.GetTreePrint().PrintTree(pq.GetBestParse(), pwOut);
					}
					else
					{
						pwErr.Println("Error. Can't parse test sentence: " + op.tlpParams.DefaultTestSentence());
					}
				}
				else
				{
					// We parse filenames given by the remaining arguments
					ParseFiles.ParseFiles(args, argIndex, tokenized, tokenizerFactory, elementDelimiter, sentenceDelimiter, escaper, tagDelimiter, op, lp.GetTreePrint(), lp);
				}
			}
		}

		private const long serialVersionUID = 2;
		// end main
	}
}
