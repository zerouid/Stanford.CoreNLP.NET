// MaxentTagger -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2016 Leland Stanford Junior University
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
// For more information, bug reports, fixes, contact:
// Christopher Manning
// Dept of Computer Science, Gates 2A
// Stanford CA 94305-9020
// USA
// Support/Questions: stanford-nlp on SO or java-nlp-user@lists.stanford.edu
// Licensing: java-nlp-support@lists.stanford.edu
// http://nlp.stanford.edu/software/tagger.html
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Maxent;
using Edu.Stanford.Nlp.Maxent.Iis;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>The main class for users to run, train, and test the part of speech tagger.</summary>
	/// <remarks>
	/// The main class for users to run, train, and test the part of speech tagger.
	/// You can tag things through the Java API or from the command line.
	/// The two English taggers included in this distribution are:
	/// <ul>
	/// <li> A bi-directional dependency network tagger in
	/// <c>edu/stanford/nlp/models/pos-tagger/english-left3words/english-bidirectional-distsim.tagger</c>
	/// .
	/// Its accuracy was 97.32% on Penn Treebank WSJ secs. 22-24.</li>
	/// <li> A model using only left second-order sequence information and similar but less
	/// unknown words and lexical features as the previous model in
	/// <c>edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger</c>
	/// This tagger runs a lot faster, and is recommended for general use.
	/// Its accuracy was 96.92% on Penn Treebank WSJ secs. 22-24.</li>
	/// </ul>
	/// <h3>Using the Java API</h3>
	/// <dl>
	/// <dt>
	/// A MaxentTagger can be made with a constructor taking as argument the location of parameter files for a trained tagger: </dt>
	/// <dd>
	/// <c>MaxentTagger tagger = new MaxentTagger("models/left3words-wsj-0-18.tagger");</c>
	/// </dd>
	/// <p>
	/// <dt>A default path is provided for the location of the tagger on the Stanford NLP machines:</dt>
	/// <dd>
	/// <c>MaxentTagger tagger = new MaxentTagger(DEFAULT_NLP_GROUP_MODEL_PATH);</c>
	/// </dd>
	/// <p>
	/// <dt>If you set the NLP_DATA_HOME environment variable,
	/// DEFAULT_NLP_GROUP_MODEL_PATH will instead point to the directory
	/// given in NLP_DATA_HOME.</dt>
	/// <p>
	/// <dt>To tag a List of HasWord and get a List of TaggedWord, you can use one of: </dt>
	/// <dd>
	/// <c>List&lt;TaggedWord&gt; taggedSentence = tagger.tagSentence(List&lt;? extends HasWord&gt; sentence)</c>
	/// </dd>
	/// <dd>
	/// <c>List&lt;TaggedWord&gt; taggedSentence = tagger.apply(List&lt;? extends HasWord&gt; sentence)</c>
	/// </dd>
	/// <p>
	/// <dt>To tag a list of sentences and get back a list of tagged sentences:
	/// <dd>
	/// <c>List taggedList = tagger.process(List sentences)</c>
	/// </dd>
	/// <p>
	/// <dt>To tag a String of text and to get back a String with tagged words:</dt>
	/// <dd>
	/// <c>String taggedString = tagger.tagString("Here's a tagged string.")</c>
	/// </dd>
	/// <p>
	/// <dt>To tag a string of <i>correctly tokenized</i>, whitespace-separated words and get a string of tagged words back:</dt>
	/// <dd>
	/// <c>String taggedString = tagger.tagTokenizedString("Here 's a tagged string .")</c>
	/// </dd>
	/// </dl>
	/// The
	/// <c>tagString</c>
	/// method uses the default tokenizer (PTBTokenizer).
	/// If you wish to control tokenization, you may wish to call
	/// <see cref="TokenizeText(Java.IO.Reader, Edu.Stanford.Nlp.Process.ITokenizerFactory{T})"/>
	/// and then to call
	/// <c>process()</c>
	/// on the result.
	/// <h3>Using the command line</h3>
	/// Tagging, testing, and training can all also be done via the command line.
	/// <h3>Training from the command line</h3>
	/// To train a model from the command line, first generate a property file:
	/// <pre>java edu.stanford.nlp.tagger.maxent.MaxentTagger -genprops </pre>
	/// This gets you a default properties file with descriptions of each parameter you can set in
	/// your trained model.  You can modify the properties file, or use the default options.  To train, run:
	/// <pre>java -mx1g edu.stanford.nlp.tagger.maxent.MaxentTagger -props myPropertiesFile.props </pre>
	/// with the appropriate properties file specified. Any argument you give in the properties file can also
	/// be specified on the command line.  You must have specified a model using -model, either in the properties file
	/// or on the command line, as well as a file containing tagged words using -trainFile.
	/// Useful flags for controlling the amount of output are -verbose, which prints extra debugging information,
	/// and -verboseResults, which prints full information about intermediate results.  -verbose defaults to false
	/// and -verboseResults defaults to true.
	/// <h3>Tagging and Testing from the command line</h3>
	/// Usage:
	/// For tagging (plain text):
	/// <pre>java edu.stanford.nlp.tagger.maxent.MaxentTagger -model &lt;modelFile&gt; -textFile &lt;textfile&gt; </pre>
	/// For testing (evaluating against tagged text):
	/// <pre>java edu.stanford.nlp.tagger.maxent.MaxentTagger -model &lt;modelFile&gt; -testFile &lt;testfile&gt; </pre>
	/// You can use the same properties file as for training
	/// if you pass it in with the "-props" argument. The most important
	/// arguments for tagging (besides "model" and "file") are "tokenize"
	/// and "tokenizerFactory". See below for more details.
	/// Note that the tagger assumes input has not yet been tokenized and
	/// by default tokenizes it using a default English tokenizer.  If your
	/// input has already been tokenized, use the flag "-tokenize false".
	/// Parameters can be defined using a Properties file
	/// (specified on the command-line with
	/// <c>-prop</c>
	/// <i>propFile</i>),
	/// or directly on the command line (by preceding their name with a minus sign
	/// ("-") to turn them into a flag. The following properties are recognized:
	/// <table border="1">
	/// <tr><td><b>Property Name</b></td><td><b>Type</b></td><td><b>Default Value</b></td><td><b>Relevant Phase(s)</b></td><td><b>Description</b></td></tr>
	/// <tr><td>model</td><td>String</td><td>N/A</td><td>All</td><td>Path and filename where you would like to save the model (training) or where the model should be loaded from (testing, tagging).</td></tr>
	/// <tr><td>trainFile</td><td>String</td><td>N/A</td><td>Train</td>
	/// <td>
	/// Path to the file holding the training data; specifying this option puts the tagger in training mode.  Only one of 'trainFile','testFile','textFile', and 'dump' may be specified.<br />
	/// There are three formats possible.  The first is a text file of tagged data. Each line is considered a separate sentence.  In each sentence, words are separated by whitespace.
	/// Each word must have a tag, which is separated from the token using the specified
	/// <c>tagSeparator</c>
	/// .  This format, called TEXT, is the default format. <br />
	/// The second format is a file of Penn Treebank formatted (i.e., s-expression) tree files.  Trees are loaded one at a time and the tagged words in a tree are used as a training sentence.
	/// To specify this format, preface the filename with "
	/// <c>format=TREES,</c>
	/// ".  <br />
	/// The final possible format is TSV files (tab-separated columns).  To specify a TSV file, set
	/// <c>trainFile</c>
	/// to "
	/// <c>format=TSV,wordColumn=x,tagColumn=y,filename</c>
	/// ".
	/// Column numbers are indexed from 0, and sentences are separated with blank lines. The default wordColumn is 0 and default tagColumn is 1.
	/// <br />
	/// A file can be in a different character set encoding than the tagger's default encoding by prefacing the filename with
	/// <c>"encoding=ENC,"</c>
	/// .
	/// You can specify the tagSeparator character in a TEXT file by prefacing the filename with "tagSeparator=c,". <br />
	/// Tree files can be fed through TreeTransformers and TreeNormalizers.  To specify a transformer, preface the filename with "treeTransformer=CLASSNAME,".
	/// To specify a normalizer, preface the filename with "treeNormalizer=CLASSNAME,".
	/// You can also filter trees using a
	/// <c>Filter&lt;Tree&gt;</c>
	/// , which can be specified with "treeFilter=CLASSNAME,".
	/// A specific range of trees to be used can be specified with treeRange=X-Y.  Multiple parts of the range can be separated by : as opposed to the normal separator of ,.
	/// For example, one could use the argument "-treeRange=25-50:75-100".
	/// You can specify a TreeReaderFactory by prefacing the filename with "trf=CLASSNAME,". Note: If it includes a TreeNormalizer, you want to specify it as the treeNormalizer as well.<br />
	/// Multiple files can be specified by making a semicolon separated list of files.  Each file can have its own format specifiers as above.<br />
	/// You will note that none of , ; or = can be in filenames.
	/// </td>
	/// </tr>
	/// <tr><td>testFile</td><td>String</td><td>N/A</td><td>Test</td><td>Path to the file holding the test data; specifying this option puts the tagger in testing mode.  Only one of 'trainFile','testFile','textFile', and 'dump' may be specified.  The same format as trainFile applies, but only one file can be specified.</td></tr>
	/// <tr><td>textFile</td><td>String</td><td>N/A</td><td>Tag</td><td>Path to the file holding the text to tag; specifying this option puts the tagger in tagging mode.  Only one of 'trainFile','testFile','textFile', and 'dump' may be specified.  No file reading options may be specified for textFile</td></tr>
	/// <tr><td>dump</td><td>String</td><td>N/A</td><td>Dump</td><td>Path to the file holding the model to dump; specifying this option puts the tagger in dumping mode.  Only one of 'trainFile','testFile','textFile', and 'dump' may be specified.</td></tr>
	/// <tr><td>genprops</td><td>boolean</td><td>N/A</td><td>N/A</td><td>Use this option to output a default properties file, containing information about each of the possible configuration options.</td></tr>
	/// <tr><td>tagSeparator</td><td>char</td><td>/</td><td>All</td><td>Separator character that separates word and part of speech tags, such as out/IN or out_IN.  For training and testing, this is the separator used in the train/test files.  For tagging, this is the character that will be inserted between words and tags in the output.</td></tr>
	/// <tr><td>encoding</td><td>String</td><td>UTF-8</td><td>All</td><td>Encoding of the read files (training, testing) and the output text files.</td></tr>
	/// <tr><td>tokenize</td><td>boolean</td><td>true</td><td>Tag,Test</td><td>Whether or not the file needs to be tokenized.  If this is false, the tagger assumes that white space separates words if and only if they should be tagged as separate tokens, and that the input is strictly one sentence per line.</td></tr>
	/// <tr><td>tokenizerFactory</td><td>String</td><td>edu.stanford.nlp.<br />process.PTBTokenizer</td><td>Tag,Test</td><td>Fully qualified class name of the tokenizer to use.  edu.stanford.nlp.process.PTBTokenizer does basic English tokenization.</td></tr>
	/// <tr><td>tokenizerOptions</td><td>String</td><td></td><td>Tag,Test</td><td>Known options for the particular tokenizer used. A comma-separated list. For PTBTokenizer, options of interest include
	/// <c>americanize=false</c>
	/// and
	/// <c>asciiQuotes</c>
	/// (for German). Note that any choice of tokenizer options that conflicts with the tokenization used in the tagger training data will likely degrade tagger performance.</td></tr>
	/// <tr><td>sentenceDelimiter</td><td>String</td><td>null</td><td>Tag,Test</td><td>A marker used to separate a text into sentences. If not set (equal to
	/// <see langword="null"/>
	/// ), sentence breaking is done by content (looking for periods, etc.) Otherwise, it will break on this String, except that if the String is "newline", it breaks on the String "\\n".</td></tr>
	/// <tr><td>arch</td><td>String</td><td>generic</td><td>Train</td><td>Architecture of the model, as a comma-separated list of options, some with a parenthesized integer argument written k here: this determines what features are used to build your model.  See
	/// <see cref="ExtractorFrames"/>
	/// and
	/// <see cref="ExtractorFramesRare"/>
	/// for more information.</td></tr>
	/// <tr><td>wordFunction</td><td>String</td><td>(none)</td><td>Train</td><td>A function to apply to the text before training or testing.  Must inherit from edu.stanford.nlp.util.Function&lt;String, String&gt;.  Can be blank.</td></tr>
	/// <tr><td>lang</td><td>String</td><td>english</td><td>Train</td><td>Language from which the part of speech tags are drawn. This option determines which tags are considered closed-class (only fixed set of words can be tagged with a closed-class tag, such as prepositions). Defined languages are 'english' (Penn tag set), 'polish' (very rudimentary), 'french', 'chinese', 'arabic', 'german', and 'medline'.  </td></tr>
	/// <tr><td>openClassTags</td><td>String</td><td>N/A</td><td>Train</td><td>Space separated list of tags that should be considered open-class.  All tags encountered that are not in this list are considered closed-class.  E.g. format: "NN VB"</td></tr>
	/// <tr><td>closedClassTags</td><td>String</td><td>N/A</td><td>Train</td><td>Space separated list of tags that should be considered closed-class.  All tags encountered that are not in this list are considered open-class.</td></tr>
	/// <tr><td>learnClosedClassTags</td><td>boolean</td><td>false</td><td>Train</td><td>If true, induce which tags are closed-class by counting as closed-class tags all those tags which have fewer unique word tokens than closedClassTagThreshold. </td></tr>
	/// <tr><td>closedClassTagThreshold</td><td>int</td><td>int</td><td>Train</td><td>Number of unique word tokens that a tag may have and still be considered closed-class; relevant only if learnClosedClassTags is true.</td></tr>
	/// <tr><td>sgml</td><td>boolean</td><td>false</td><td>Tag, Test</td><td>Very basic tagging of the contents of all sgml fields; for more complex mark-up, consider using the xmlInput option.</td></tr>
	/// <tr><td>xmlInput</td><td>String</td><td></td><td>Tag, Test</td><td>Give a space separated list of tags in an XML file whose content you would like tagged.  Any internal tags that appear in the content of fields you would like tagged will be discarded; the rest of the XML will be preserved and the original text of specified fields will be replaced with the tagged text.</td></tr>
	/// <tr><td>outputFile</td><td>String</td><td>""</td><td>Tag</td><td>Path to write output to.  If blank, stdout is used.</td></tr>
	/// <tr><td>outputFormat</td><td>String</td><td>""</td><td>Tag</td><td>Output format. One of: slashTags (default), xml (or inlineXML as a synonym), or tsv</td></tr>
	/// <tr><td>outputFormatOptions</td><td>String</td><td>""</td><td>Tag</td><td>Output format options. Currently used: lemmatize, verbose, keepEmptySentences</td></tr>
	/// <tr><td>tagInside</td><td>String</td><td>""</td><td>Tag</td><td>Tags inside elements that match the regular expression given in the String.</td></tr>
	/// <tr><td>search</td><td>String</td><td>cg</td><td>Train</td><td>Specify the search method to be used in the optimization method for training.  Options are 'cg' (conjugate gradient), 'iis' (improved iterative scaling), or 'qn' (quasi-newton).</td></tr>
	/// <tr><td>sigmaSquared</td><td>double</td><td>0.5</td><td>Train</td><td>Sigma-squared smoothing/regularization parameter to be used for conjugate gradient search.  Default usually works reasonably well.</td></tr>
	/// <tr><td>iterations</td><td>int</td><td>100</td><td>Train</td><td>Number of iterations to be used for improved iterative scaling.</td></tr>
	/// <tr><td>rareWordThresh</td><td>int</td><td>5</td><td>Train</td><td>Words that appear fewer than this number of times during training are considered rare words and use extra rare word features.</td></tr>
	/// <tr><td>minFeatureThreshold</td><td>int</td><td>5</td><td>Train</td><td>Features whose history appears fewer than this number of times are discarded.</td></tr>
	/// <tr><td>curWordMinFeatureThreshold</td><td>int</td><td>2</td><td>Train</td><td>Words that occur more than this number of times will generate features with all of the tags they've been seen with.</td></tr>
	/// <tr><td>rareWordMinFeatureThresh</td><td>int</td><td>10</td><td>Train</td><td>Features of rare words whose histories occur fewer than this number of times are discarded.</td></tr>
	/// <tr><td>veryCommonWordThresh</td><td>int</td><td>250</td><td>Train</td><td>Words that occur more than this number of times form an equivalence class by themselves.  Ignored unless you are using ambiguity classes.</td></tr>
	/// <tr><td>debug</td><td>boolean</td><td>boolean</td><td>All</td><td>Whether to write debugging information (words, top words, unknown words, confusion matrix).  Useful for error analysis.</td></tr>
	/// <tr><td>debugPrefix</td><td>String</td><td>N/A</td><td>All</td><td>File (path) prefix for where to write out the debugging information (relevant only if debug=true).</td></tr>
	/// <tr><td>nthreads</td><td>int</td><td>1</td><td>Test,Text</td><td>Number of threads to use when processing text.</td></tr>
	/// </table>
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <author>Miler Lee</author>
	/// <author>Joseph Smarr</author>
	/// <author>Anna Rafferty</author>
	/// <author>Michel Galley</author>
	/// <author>Christopher Manning</author>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class MaxentTagger : Edu.Stanford.Nlp.Tagger.Common.Tagger, IListProcessor<IList<IHasWord>, IList<TaggedWord>>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger));

		/// <summary>
		/// The directory from which to get taggers when using
		/// DEFAULT_NLP_GROUP_MODEL_PATH.
		/// </summary>
		/// <remarks>
		/// The directory from which to get taggers when using
		/// DEFAULT_NLP_GROUP_MODEL_PATH.  Normally set to the location of
		/// the latest left3words tagger on the NLP machines, but can be
		/// changed by setting the environment variable NLP_DATA_HOME.
		/// </remarks>
		public const string BaseTaggerHome = "$NLP_DATA_HOME/data/pos-tagger/distrib";

		public static readonly string TaggerHome = DataFilePaths.Convert(BaseTaggerHome);

		public static readonly string DefaultNlpGroupModelPath = new File(TaggerHome, "english-left3words-distsim.tagger").GetPath();

		public const string DefaultJarPath = "edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger";

		public const string DefaultDistributionPath = "models/english-left3words-distsim.tagger";

		public MaxentTagger()
		{
		}

		public MaxentTagger(TaggerConfig config)
			: this(config.GetModel(), config)
		{
		}

		/// <summary>
		/// Constructor for a tagger, loading a model stored in a particular file,
		/// classpath resource, or URL.
		/// </summary>
		/// <remarks>
		/// Constructor for a tagger, loading a model stored in a particular file,
		/// classpath resource, or URL.
		/// The tagger data is loaded when the constructor is called (this can be
		/// slow). This constructor first constructs a TaggerConfig object, which
		/// loads the tagger options from the modelFile.
		/// </remarks>
		/// <param name="modelFile">Filename, classpath resource, or URL for the trained model</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		public MaxentTagger(string modelFile)
			: this(modelFile, StringUtils.ArgsToProperties("-model", modelFile), true)
		{
		}

		/// <summary>
		/// Constructor for a tagger, loading a model stored in a particular file,
		/// classpath resource, or URL.
		/// </summary>
		/// <remarks>
		/// Constructor for a tagger, loading a model stored in a particular file,
		/// classpath resource, or URL.
		/// The tagger data is loaded when the constructor is called (this can be
		/// slow). This constructor first constructs a TaggerConfig object, which
		/// loads the tagger options from the modelFile.
		/// </remarks>
		/// <param name="modelStream">The InputStream from which to read the model</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		public MaxentTagger(InputStream modelStream)
			: this(modelStream, new Properties(), true)
		{
		}

		/// <summary>
		/// Constructor for a tagger using a model stored in a particular file,
		/// with options taken from the supplied TaggerConfig.
		/// </summary>
		/// <remarks>
		/// Constructor for a tagger using a model stored in a particular file,
		/// with options taken from the supplied TaggerConfig.
		/// The tagger data is loaded when the
		/// constructor is called (this can be slow).
		/// This version assumes that the tagger options in the modelFile have
		/// already been loaded into the TaggerConfig (if that is desired).
		/// </remarks>
		/// <param name="modelFile">Filename, classpath resource, or URL for the trained model</param>
		/// <param name="config">The configuration for the tagger</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		public MaxentTagger(string modelFile, Properties config)
			: this(modelFile, config, true)
		{
		}

		/// <summary>Initializer that loads the tagger.</summary>
		/// <param name="modelFile">
		/// Where to initialize the tagger from.
		/// Most commonly, this is the filename of the trained model, for example,
		/// <c>/u/nlp/data/pos-tagger/wsj3t0-18-left3words/left3words-wsj-0-18.tagger</c>
		/// .
		/// However, if it starts with "https?://" it will be interpreted as a URL.
		/// One can also load models directly from the classpath, as in loading from
		/// <c>edu/stanford/nlp/models/pos-tagger/wsj3t0-18-bidirectional/bidirectional-distsim-wsj-0-18.tagger</c>
		/// .
		/// </param>
		/// <param name="config">TaggerConfig based on command-line arguments</param>
		/// <param name="printLoading">Whether to print a message saying what model file is being loaded and how long it took when finished.</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		public MaxentTagger(string modelFile, Properties config, bool printLoading)
		{
			// todo: maybe this shouldn't do this but replace the zero arg constructor.
			// i.e., call init() not readModelAndInit(). This method is currently UNUSED. Make non-public.
			ReadModelAndInit(config, modelFile, printLoading);
		}

		/// <summary>Initializer that loads the tagger.</summary>
		/// <param name="modelStream">An InputStream for reading the model file</param>
		/// <param name="config">TaggerConfig based on command-line arguments</param>
		/// <param name="printLoading">Whether to print a message saying what model file is being loaded and how long it took when finished.</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		public MaxentTagger(InputStream modelStream, Properties config, bool printLoading)
		{
			ReadModelAndInit(config, modelStream, printLoading);
		}

		internal readonly Dictionary dict = new Dictionary();

		internal TTags tags;

		/// <summary>Will return the index of a tag, adding it if it doesn't already exist</summary>
		public virtual int AddTag(string tag)
		{
			return tags.Add(tag);
		}

		/// <summary>Will return the index of a tag if known, -1 if not already known</summary>
		public virtual int GetTagIndex(string tag)
		{
			return tags.GetIndex(tag);
		}

		public virtual int NumTags()
		{
			return tags.GetSize();
		}

		public virtual string GetTag(int index)
		{
			return tags.GetTag(index);
		}

		public virtual ICollection<string> TagSet()
		{
			return tags.TagSet();
		}

		private LambdaSolveTagger prob;

		internal IList<IDictionary<string, int[]>> fAssociations = Generics.NewArrayList();

		internal Extractors extractors;

		internal Extractors extractorsRare;

		internal AmbiguityClasses ambClasses;

		internal readonly bool alltags = false;

		internal readonly IDictionary<string, ICollection<string>> tagTokens = Generics.NewHashMap();

		private static readonly int RareWordThresh = System.Convert.ToInt32(TaggerConfig.RareWordThresh);

		private static readonly int MinFeatureThresh = System.Convert.ToInt32(TaggerConfig.MinFeatureThresh);

		private static readonly int CurWordMinFeatureThresh = System.Convert.ToInt32(TaggerConfig.CurWordMinFeatureThresh);

		private static readonly int RareWordMinFeatureThresh = System.Convert.ToInt32(TaggerConfig.RareWordMinFeatureThresh);

		private static readonly int VeryCommonWordThresh = System.Convert.ToInt32(TaggerConfig.VeryCommonWordThresh);

		private static readonly bool OccurringTagsOnly = bool.ParseBoolean(TaggerConfig.OccurringTagsOnly);

		private static readonly bool PossibleTagsOnly = bool.ParseBoolean(TaggerConfig.PossibleTagsOnly);

		private double defaultScore;

		private double[] defaultScores;

		internal int leftContext;

		internal int rightContext;

		internal TaggerConfig config;

		/// <summary>Determines which words are considered rare.</summary>
		/// <remarks>
		/// Determines which words are considered rare.  All words with count
		/// in the training data strictly less than this number (standardly, &lt; 5) are
		/// considered rare.
		/// </remarks>
		private int rareWordThresh = RareWordThresh;

		/// <summary>Determines which features are included in the model.</summary>
		/// <remarks>
		/// Determines which features are included in the model.  The model
		/// includes features that occurred strictly more times than this number
		/// (standardly, &gt; 5) in the training data.  Here I look only at the
		/// history (not the tag), so the history appearing this often is enough.
		/// </remarks>
		internal int minFeatureThresh = MinFeatureThresh;

		/// <summary>This is a special threshold for the current word feature.</summary>
		/// <remarks>
		/// This is a special threshold for the current word feature.
		/// Only words that have occurred strictly &gt; this number of times
		/// in total will generate word features with all of their occurring tags.
		/// The traditional default was 2.
		/// </remarks>
		internal int curWordMinFeatureThresh = CurWordMinFeatureThresh;

		/// <summary>Determines which rare word features are included in the model.</summary>
		/// <remarks>
		/// Determines which rare word features are included in the model.
		/// The features for rare words have a strictly higher support than
		/// this number are included. Traditional default is 10.
		/// </remarks>
		internal int rareWordMinFeatureThresh = RareWordMinFeatureThresh;

		/// <summary>
		/// If using tag equivalence classes on following words, words that occur
		/// strictly more than this number of times (in total with any tag)
		/// are sufficiently frequent to form an equivalence class
		/// by themselves.
		/// </summary>
		/// <remarks>
		/// If using tag equivalence classes on following words, words that occur
		/// strictly more than this number of times (in total with any tag)
		/// are sufficiently frequent to form an equivalence class
		/// by themselves. (Not used unless using equivalence classes.)
		/// There are places in the code (ExtractorAmbiguityClass.java, for one)
		/// that assume this value is constant over the life of a tagger.
		/// </remarks>
		internal int veryCommonWordThresh = VeryCommonWordThresh;

		internal int xSize;

		internal int ySize;

		internal bool occurringTagsOnly = OccurringTagsOnly;

		internal bool possibleTagsOnly = PossibleTagsOnly;

		private bool initted = false;

		internal bool Verbose = false;

		/// <summary>
		/// This is a function used to preprocess all text before applying
		/// the tagger to it.
		/// </summary>
		/// <remarks>
		/// This is a function used to preprocess all text before applying
		/// the tagger to it.  For example, it could be a function to
		/// lowercase text, such as edu.stanford.nlp.util.LowercaseFunction
		/// (which makes the tagger case insensitive).  It is applied in
		/// ReadDataTagged, which loads in the training data, and in
		/// TestSentence, which processes sentences for new queries.  If any
		/// other classes are added or modified which use raw text, they must
		/// also use this function to keep results consistent.
		/// <br />
		/// An alternate design would have been to use the function at a
		/// lower level, such as at the extractor level.  That would have
		/// require more invasive changes to the tagger, though, because
		/// other data structures such as the Dictionary would then be using
		/// raw text as well.  This is also more efficient, in that the
		/// function is applied once at the start of the process.
		/// </remarks>
		internal IFunction<string, string> wordFunction;

		// For each extractor index, we have a map from possible extracted
		// features to an array which maps from tag number to feature weight index in the lambdas array.
		//PairsHolder pairs = new PairsHolder();
		// = null;
		/* Package access - shouldn't be part of public API. */
		internal virtual LambdaSolve GetLambdaSolve()
		{
			return prob;
		}

		// TODO: make these constructors instead of init methods?
		internal virtual void Init(TaggerConfig config)
		{
			if (initted)
			{
				return;
			}
			// TODO: why not reinit?
			this.config = config;
			string lang;
			string arch;
			string[] openClassTags;
			string[] closedClassTags;
			if (config == null)
			{
				lang = "english";
				arch = "left3words";
				openClassTags = StringUtils.EmptyStringArray;
				closedClassTags = StringUtils.EmptyStringArray;
				wordFunction = null;
			}
			else
			{
				this.Verbose = config.GetVerbose();
				lang = config.GetLang();
				arch = config.GetArch();
				openClassTags = config.GetOpenClassTags();
				closedClassTags = config.GetClosedClassTags();
				if (!config.GetWordFunction().Equals(string.Empty))
				{
					wordFunction = ReflectionLoading.LoadByReflection(config.GetWordFunction());
				}
				if (((openClassTags.Length > 0) && !lang.Equals(string.Empty)) || ((closedClassTags.Length > 0) && !lang.Equals(string.Empty)) || ((closedClassTags.Length > 0) && (openClassTags.Length > 0)))
				{
					throw new Exception("At least two of lang (\"" + lang + "\"), openClassTags (length " + openClassTags.Length + ": " + Arrays.ToString(openClassTags) + ")," + "and closedClassTags (length " + closedClassTags.Length + ": " + Arrays.ToString(closedClassTags
						) + ") specified---you must choose one!");
				}
				else
				{
					if ((openClassTags.Length == 0) && lang.Equals(string.Empty) && (closedClassTags.Length == 0) && !config.GetLearnClosedClassTags())
					{
						log.Info("warning: no language set, no open-class tags specified, and no closed-class tags specified; assuming ALL tags are open class tags");
					}
				}
			}
			if (openClassTags.Length > 0)
			{
				tags = new TTags();
				tags.SetOpenClassTags(openClassTags);
			}
			else
			{
				if (closedClassTags.Length > 0)
				{
					tags = new TTags();
					tags.SetClosedClassTags(closedClassTags);
				}
				else
				{
					tags = new TTags(lang);
				}
			}
			defaultScore = lang.Equals("english") ? 1.0 : 0.0;
			if (config != null)
			{
				rareWordThresh = config.GetRareWordThresh();
				minFeatureThresh = config.GetMinFeatureThresh();
				curWordMinFeatureThresh = config.GetCurWordMinFeatureThresh();
				rareWordMinFeatureThresh = config.GetRareWordMinFeatureThresh();
				veryCommonWordThresh = config.GetVeryCommonWordThresh();
				occurringTagsOnly = config.OccurringTagsOnly();
				possibleTagsOnly = config.PossibleTagsOnly();
				// log.info("occurringTagsOnly: "+occurringTagsOnly);
				// log.info("possibleTagsOnly: "+possibleTagsOnly);
				if (config.GetDefaultScore() >= 0)
				{
					defaultScore = config.GetDefaultScore();
				}
			}
			// just in case, reset the defaultScores array so it will be
			// recached later when needed.  can't initialize it now in case we
			// don't know ysize yet
			defaultScores = null;
			if (config == null || config.GetMode() == TaggerConfig.Mode.Train)
			{
				// initialize the extractors based on the arch variable
				// you only need to do this when training; otherwise they will be
				// restored from the serialized file
				extractors = new Extractors(ExtractorFrames.GetExtractorFrames(arch));
				extractorsRare = new Extractors(ExtractorFramesRare.GetExtractorFramesRare(arch, tags));
				SetExtractorsGlobal();
			}
			ambClasses = new AmbiguityClasses(tags);
			initted = true;
		}

		private void InitDefaultScores()
		{
			lock (this)
			{
				if (defaultScores == null)
				{
					defaultScores = new double[ySize + 1];
					for (int i = 0; i < ySize + 1; ++i)
					{
						defaultScores[i] = Math.Log(i * defaultScore);
					}
				}
			}
		}

		/// <summary>Caches a math log operation to save a tiny bit of time</summary>
		internal virtual double GetInactiveTagDefaultScore(int nDefault)
		{
			if (defaultScores == null)
			{
				InitDefaultScores();
			}
			return defaultScores[nDefault];
		}

		internal virtual bool HasApproximateScoring()
		{
			return defaultScore > 0.0;
		}

		/// <summary>
		/// Figures out what tokenizer factory might be described by the
		/// config.
		/// </summary>
		/// <remarks>
		/// Figures out what tokenizer factory might be described by the
		/// config.  If it's described by name in the config, uses reflection
		/// to get the factory (which may cause an exception, of course...)
		/// </remarks>
		protected internal virtual ITokenizerFactory<IHasWord> ChooseTokenizerFactory()
		{
			return ChooseTokenizerFactory(config.GetTokenize(), config.GetTokenizerFactory(), config.GetTokenizerOptions(), config.GetTokenizerInvertible());
		}

		protected internal static ITokenizerFactory<IHasWord> ChooseTokenizerFactory(bool tokenize, string tokenizerFactory, string tokenizerOptions, bool invertible)
		{
			if (tokenize && tokenizerFactory.Trim().Length != 0)
			{
				//return (TokenizerFactory<? extends HasWord>) Class.forName(getTokenizerFactory()).newInstance();
				try
				{
					Type clazz = (Type)Sharpen.Runtime.GetType(tokenizerFactory.Trim());
					MethodInfo factoryMethod = clazz.GetMethod("newTokenizerFactory");
					ITokenizerFactory<IHasWord> factory = (ITokenizerFactory<IHasWord>)factoryMethod.Invoke(tokenizerOptions);
					return factory;
				}
				catch (Exception e)
				{
					throw new Exception("Could not load tokenizer factory", e);
				}
			}
			else
			{
				if (tokenize)
				{
					if (invertible)
					{
						if (tokenizerOptions.Equals(string.Empty))
						{
							tokenizerOptions = "invertible=true";
						}
						else
						{
							if (!tokenizerOptions.Matches("(^|.*,)invertible=true"))
							{
								tokenizerOptions += ",invertible=true";
							}
						}
						return PTBTokenizer.PTBTokenizerFactory.NewCoreLabelTokenizerFactory(tokenizerOptions);
					}
					else
					{
						return PTBTokenizer.PTBTokenizerFactory.NewWordTokenizerFactory(tokenizerOptions);
					}
				}
				else
				{
					return WhitespaceTokenizer.Factory();
				}
			}
		}

		/// <summary>Serialize the ExtractorFrames and ExtractorFramesRare to os.</summary>
		/// <exception cref="System.IO.IOException"/>
		private void SaveExtractors(OutputStream os)
		{
			ObjectOutputStream @out = new ObjectOutputStream(os);
			@out.WriteObject(extractors);
			@out.WriteObject(extractorsRare);
			@out.Flush();
		}

		/// <summary>Read the extractors from a stream.</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadExtractors(InputStream file)
		{
			ObjectInputStream @in = new ObjectInputStream(file);
			extractors = (Extractors)@in.ReadObject();
			extractorsRare = (Extractors)@in.ReadObject();
			extractors.InitTypes();
			extractorsRare.InitTypes();
			int left = extractors.LeftContext();
			int left_u = extractorsRare.LeftContext();
			if (left_u > left)
			{
				left = left_u;
			}
			leftContext = left;
			int right = extractors.RightContext();
			int right_u = extractorsRare.RightContext();
			if (right_u > right)
			{
				right = right_u;
			}
			rightContext = right;
			SetExtractorsGlobal();
		}

		// Sometimes there is data associated with the tagger (such as a
		// dictionary) that we don't want saved with each extractor.  This
		// call lets those extractors get that information from the tagger
		// after being loaded from a data file.
		private void SetExtractorsGlobal()
		{
			extractors.SetGlobalHolder(this);
			extractorsRare.SetGlobalHolder(this);
		}

		/// <summary>
		/// Removes features that never have a non-zero weight for any tag from
		/// the fAssociations' appropriate Map.
		/// </summary>
		private void RemoveDeadRules()
		{
			foreach (IDictionary<string, int[]> fAssociation in fAssociations)
			{
				IList<string> deadRules = Generics.NewArrayList();
				foreach (KeyValuePair<string, int[]> entry in fAssociation)
				{
					string value = entry.Key;
					int[] fAssociations = entry.Value;
					bool found = false;
					for (int index = 0; index < ySize; ++index)
					{
						int fNum = fAssociations[index];
						if (fNum > -1)
						{
							if (GetLambdaSolve().lambda[fNum] != 0.0)
							{
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
						deadRules.Add(value);
					}
				}
				foreach (string rule in deadRules)
				{
					Sharpen.Collections.Remove(fAssociation, rule);
				}
			}
		}

		/// <summary>Searching the lambda array for 0 entries, removes them.</summary>
		/// <remarks>
		/// Searching the lambda array for 0 entries, removes them.  This
		/// saves a large chunk of space in the tagger models which are build
		/// with L1 regularization.
		/// <br />
		/// After removing the zeros, go through the feature arrays and
		/// reindex the pointers into the lambda array.  This saves some time
		/// later on at runtime.
		/// </remarks>
		private void SimplifyLambda()
		{
			double[] lambda = GetLambdaSolve().lambda;
			int[] map = new int[lambda.Length];
			int current = 0;
			for (int index = 0; index < lambda.Length; ++index)
			{
				if (lambda[index] == 0.0)
				{
					map[index] = -1;
				}
				else
				{
					map[index] = current;
					current++;
				}
			}
			double[] condensedLambda = new double[current];
			for (int i = 0; i < lambda.Length; ++i)
			{
				if (map[i] != -1)
				{
					condensedLambda[map[i]] = lambda[i];
				}
			}
			foreach (IDictionary<string, int[]> featureMap in fAssociations)
			{
				foreach (KeyValuePair<string, int[]> entry in featureMap)
				{
					int[] fAssociations = entry.Value;
					for (int index_1 = 0; index_1 < ySize; ++index_1)
					{
						if (fAssociations[index_1] >= 0)
						{
							fAssociations[index_1] = map[fAssociations[index_1]];
						}
					}
				}
			}
			prob = new LambdaSolveTagger(condensedLambda);
		}

		protected internal virtual void SaveModel(string filename)
		{
			try
			{
				DataOutputStream file = IOUtils.GetDataOutputStream(filename);
				SaveModel(file);
				file.Close();
			}
			catch (IOException ioe)
			{
				log.Info("Error saving tagger to file " + filename);
				throw new RuntimeIOException(ioe);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void SaveModel(DataOutputStream file)
		{
			config.SaveConfig(file);
			file.WriteInt(xSize);
			file.WriteInt(ySize);
			dict.Save(file);
			tags.Save(file, tagTokens);
			SaveExtractors(file);
			int sizeAssoc = 0;
			foreach (IDictionary<string, int[]> fValueAssociations in fAssociations)
			{
				foreach (int[] fTagAssociations in fValueAssociations.Values)
				{
					foreach (int association in fTagAssociations)
					{
						if (association >= 0)
						{
							++sizeAssoc;
						}
					}
				}
			}
			file.WriteInt(sizeAssoc);
			for (int i = 0; i < fAssociations.Count; ++i)
			{
				IDictionary<string, int[]> fValueAssociations_1 = fAssociations[i];
				foreach (KeyValuePair<string, int[]> item in fValueAssociations_1)
				{
					string featureValue = item.Key;
					int[] fTagAssociations = item.Value;
					for (int j = 0; j < fTagAssociations.Length; ++j)
					{
						int association = fTagAssociations[j];
						if (association >= 0)
						{
							file.WriteInt(association);
							FeatureKey fk = new FeatureKey(i, featureValue, tags.GetTag(j));
							fk.Save(file);
						}
					}
				}
			}
			LambdaSolve.Save_lambdas(file, prob.lambda);
		}

		/// <summary>
		/// This reads the complete tagger from a single model stored in a file, at a URL,
		/// or as a resource in a jar file, and initializes the tagger using a
		/// combination of the properties passed in and parameters from the file.
		/// </summary>
		/// <remarks>
		/// This reads the complete tagger from a single model stored in a file, at a URL,
		/// or as a resource in a jar file, and initializes the tagger using a
		/// combination of the properties passed in and parameters from the file.
		/// <p>
		/// <i>Note for the future:</i> This assumes that the TaggerConfig in the file
		/// has already been read and used.  This work is done inside the
		/// constructor of TaggerConfig.  It might be better to refactor
		/// things so that is all done inside this method, but for the moment
		/// it seemed better to leave working code alone [cdm 2008].
		/// </remarks>
		/// <param name="config">The tagger config</param>
		/// <param name="modelFileOrUrl">The name of the model file. This routine opens and closes it.</param>
		/// <param name="printLoading">Whether to print a message saying what model file is being loaded and how long it took when finished.</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		protected internal virtual void ReadModelAndInit(Properties config, string modelFileOrUrl, bool printLoading)
		{
			try
			{
				using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(modelFileOrUrl))
				{
					ReadModelAndInit(config, @is, printLoading);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Error while loading a tagger model (probably missing model file)", e);
			}
		}

		/// <summary>
		/// This reads the complete tagger from a single model provided as an InputStream,
		/// and initializes the tagger using a
		/// combination of the properties passed in and parameters from the file.
		/// </summary>
		/// <remarks>
		/// This reads the complete tagger from a single model provided as an InputStream,
		/// and initializes the tagger using a
		/// combination of the properties passed in and parameters from the file.
		/// <p>
		/// <i>Note for the future:</i> This assumes that the TaggerConfig in the file
		/// has already been read and used.  This work is done inside the
		/// constructor of TaggerConfig.  It might be better to refactor
		/// things so that is all done inside this method, but for the moment
		/// it seemed better to leave working code alone [cdm 2008].
		/// </remarks>
		/// <param name="config">The tagger config</param>
		/// <param name="modelStream">The model provided as an InputStream</param>
		/// <param name="printLoading">Whether to print a message saying what model file is being loaded and how long it took when finished.</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		protected internal virtual void ReadModelAndInit(Properties config, InputStream modelStream, bool printLoading)
		{
			try
			{
				// first check can open file ... or else leave with exception
				DataInputStream rf = new DataInputStream(modelStream);
				ReadModelAndInit(config, rf, printLoading);
				rf.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Error while loading a tagger model (probably missing model file)", e);
			}
		}

		/// <summary>
		/// This reads the complete tagger from a single model file, and inits
		/// the tagger using a combination of the properties passed in and
		/// parameters from the file.
		/// </summary>
		/// <remarks>
		/// This reads the complete tagger from a single model file, and inits
		/// the tagger using a combination of the properties passed in and
		/// parameters from the file.
		/// <p>
		/// <i>Note for the future: This assumes that the TaggerConfig in the file
		/// has already been read and used.  It might be better to refactor
		/// things so that is all done inside this method, but for the moment
		/// it seemed better to leave working code alone [cdm 2008].</i>
		/// </remarks>
		/// <param name="config">The tagger config</param>
		/// <param name="rf">DataInputStream to read from.  It's the caller's job to open and close this stream.</param>
		/// <param name="printLoading">Whether to print a message saying what model file is being loaded and how long it took when finished.</param>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException">if I/O errors or serialization errors</exception>
		protected internal virtual void ReadModelAndInit(Properties config, DataInputStream rf, bool printLoading)
		{
			try
			{
				Timing t = new Timing();
				string source = null;
				if (printLoading)
				{
					if (config != null)
					{
						// TODO: "model"
						source = config.GetProperty("model");
					}
					if (source == null)
					{
						source = "data stream";
					}
				}
				TaggerConfig taggerConfig = TaggerConfig.ReadConfig(rf);
				if (config != null)
				{
					taggerConfig.SetProperties(config);
				}
				// then init tagger
				Init(taggerConfig);
				xSize = rf.ReadInt();
				ySize = rf.ReadInt();
				// dict = new Dictionary();  // this method is called in constructor, and it's initialized as empty already
				dict.Read(rf);
				if (Verbose)
				{
					log.Info("Tagger dictionary read.");
				}
				tags.Read(rf);
				ReadExtractors(rf);
				dict.SetAmbClasses(ambClasses, veryCommonWordThresh, tags);
				int[] numFA = new int[extractors.Size() + extractorsRare.Size()];
				int sizeAssoc = rf.ReadInt();
				fAssociations = Generics.NewArrayList();
				for (int i = 0; i < extractors.Size() + extractorsRare.Size(); ++i)
				{
					fAssociations.Add(Generics.NewHashMap<string, int[]>());
				}
				if (Verbose)
				{
					log.Info("Reading %d feature keys...%n", sizeAssoc);
				}
				PrintFile pfVP = null;
				if (Verbose)
				{
					pfVP = new PrintFile("pairs.txt");
				}
				for (int i_1 = 0; i_1 < sizeAssoc; i_1++)
				{
					int numF = rf.ReadInt();
					FeatureKey fK = new FeatureKey();
					fK.Read(rf);
					numFA[fK.num]++;
					// TODO: rewrite the writing / reading code to store
					// fAssociations in a cleaner manner?  Only do this when
					// rebuilding all the tagger models anyway.  When we do that, we
					// can get rid of FeatureKey
					IDictionary<string, int[]> fValueAssociations = fAssociations[fK.num];
					int[] fTagAssociations = fValueAssociations[fK.val];
					if (fTagAssociations == null)
					{
						fTagAssociations = new int[ySize];
						for (int j = 0; j < ySize; ++j)
						{
							fTagAssociations[j] = -1;
						}
						fValueAssociations[fK.val] = fTagAssociations;
					}
					fTagAssociations[tags.GetIndex(fK.tag)] = numF;
				}
				if (Verbose)
				{
					IOUtils.CloseIgnoringExceptions(pfVP);
				}
				if (Verbose)
				{
					for (int k = 0; k < numFA.Length; k++)
					{
						log.Info("Number of features of kind " + k + ' ' + numFA[k]);
					}
				}
				prob = new LambdaSolveTagger(rf);
				if (Verbose)
				{
					log.Info("prob read ");
				}
				if (printLoading)
				{
					t.Done(log, "Loading POS tagger from " + source);
				}
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Error while loading a tagger model (probably missing model file)", e);
			}
		}

		protected internal virtual void DumpModel(TextWriter @out)
		{
			@out.WriteLine("Features: template featureValue tag: lambda");
			NumberFormat nf = new DecimalFormat(" 0.000000;-0.000000");
			for (int i = 0; i < fAssociations.Count; ++i)
			{
				IDictionary<string, int[]> fValueAssociations = fAssociations[i];
				IList<string> features = Generics.NewArrayList();
				features.Sort();
				foreach (string featureValue in features)
				{
					int[] fTagAssociations = fValueAssociations[featureValue];
					for (int j = 0; j < fTagAssociations.Length; ++j)
					{
						int association = fTagAssociations[j];
						if (association >= 0)
						{
							FeatureKey fk = new FeatureKey(i, featureValue, tags.GetTag(j));
							@out.WriteLine((fk.num < extractors.Size() ? extractors.Get(fk.num) : extractorsRare.Get(fk.num - extractors.Size())) + " " + fk.val + " " + fk.tag + ": " + nf.Format(GetLambdaSolve().lambda[association]));
						}
					}
				}
			}
		}

		/* Package access so it doesn't appear in public API. */
		internal virtual bool IsRare(string word)
		{
			return dict.Sum(word) < rareWordThresh;
		}

		/// <summary>Tags the tokenized input string and returns the tagged version.</summary>
		/// <remarks>
		/// Tags the tokenized input string and returns the tagged version.
		/// This method requires the input to already be tokenized.
		/// The tagger wants input that is whitespace separated tokens, tokenized
		/// according to the conventions of the training data. (For instance,
		/// for the Penn Treebank, punctuation marks and possessive "'s" should
		/// be separated from words.)
		/// </remarks>
		/// <param name="toTag">The untagged input String</param>
		/// <returns>The same string with tags inserted in the form word/tag</returns>
		public virtual string TagTokenizedString(string toTag)
		{
			IList<Word> sent = SentenceUtils.ToUntaggedList(Arrays.AsList(toTag.Split("\\s+")));
			TestSentence testSentence = new TestSentence(this);
			testSentence.TagSentence(sent, false);
			return testSentence.GetTaggedNice();
		}

		/// <summary>Tags the input string and returns the tagged version.</summary>
		/// <remarks>
		/// Tags the input string and returns the tagged version.
		/// This method tokenizes the input into words in perhaps multiple sentences
		/// and then tags those sentences.  The default (PTB English)
		/// tokenizer is used.
		/// </remarks>
		/// <param name="toTag">The untagged input String</param>
		/// <returns>A String of sentences with tags inserted in the form word/tag</returns>
		public virtual string TagString(string toTag)
		{
			MaxentTagger.TaggerWrapper tw = new MaxentTagger.TaggerWrapper(this);
			return tw.Apply(toTag);
		}

		/// <summary>Expects a sentence and returns a tagged sentence.</summary>
		/// <param name="in">This needs to be a sentence (List of words)</param>
		/// <returns>A sentence of TaggedWord</returns>
		public override IList<TaggedWord> Apply<_T0>(IList<_T0> @in)
		{
			TestSentence testSentence = new TestSentence(this);
			return testSentence.TagSentence(@in, false);
		}

		/// <summary>
		/// Tags the Words in each Sentence in the given List with their
		/// grammatical part-of-speech.
		/// </summary>
		/// <remarks>
		/// Tags the Words in each Sentence in the given List with their
		/// grammatical part-of-speech. The returned List contains Sentences
		/// consisting of TaggedWords.
		/// <p><b>NOTE: </b>The input document must contain sentences as its elements,
		/// not words. To turn a Document of words into a Document of sentences, run
		/// it through
		/// <see cref="Edu.Stanford.Nlp.Process.WordToSentenceProcessor{IN}"/>
		/// .
		/// </remarks>
		/// <param name="sentences">A List of Sentence</param>
		/// <returns>A List of Sentence of TaggedWord</returns>
		public virtual IList<IList<TaggedWord>> Process<_T0>(IList<_T0> sentences)
			where _T0 : IList<IHasWord>
		{
			IList<IList<TaggedWord>> taggedSentences = Generics.NewArrayList();
			TestSentence testSentence = new TestSentence(this);
			foreach (IList<IHasWord> sentence in sentences)
			{
				taggedSentences.Add(testSentence.TagSentence(sentence, false));
			}
			return taggedSentences;
		}

		/// <summary>
		/// Returns a new Sentence that is a copy of the given sentence with all the
		/// words tagged with their part-of-speech.
		/// </summary>
		/// <remarks>
		/// Returns a new Sentence that is a copy of the given sentence with all the
		/// words tagged with their part-of-speech. Convenience method when you only
		/// want to tag a single List instead of a Document of sentences.
		/// </remarks>
		/// <param name="sentence">sentence to tag</param>
		/// <returns>tagged sentence</returns>
		public virtual IList<TaggedWord> TagSentence<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			TestSentence testSentence = new TestSentence(this);
			return testSentence.TagSentence(sentence, false);
		}

		/// <summary>
		/// Returns a new Sentence that is a copy of the given sentence with all the
		/// words tagged with their part-of-speech.
		/// </summary>
		/// <remarks>
		/// Returns a new Sentence that is a copy of the given sentence with all the
		/// words tagged with their part-of-speech. Convenience method when you only
		/// want to tag a single List instead of a List of Lists.  If you
		/// supply tagSentence with a List of HasTag, and set reuseTags to
		/// true, the tagger will reuse the supplied tags.
		/// </remarks>
		/// <param name="sentence">sentence to tag</param>
		/// <param name="reuseTags">whether or not to reuse the given tag</param>
		/// <returns>tagged sentence</returns>
		public virtual IList<TaggedWord> TagSentence<_T0>(IList<_T0> sentence, bool reuseTags)
			where _T0 : IHasWord
		{
			TestSentence testSentence = new TestSentence(this);
			return testSentence.TagSentence(sentence, reuseTags);
		}

		/// <summary>
		/// Takes a sentence composed of CoreLabels and add the tags to the
		/// CoreLabels, modifying the input sentence.
		/// </summary>
		public virtual void TagCoreLabels(IList<CoreLabel> sentence)
		{
			TagCoreLabels(sentence, false);
		}

		/// <summary>
		/// Takes a sentence composed of CoreLabels and add the tags to the
		/// CoreLabels, modifying the input sentence.
		/// </summary>
		/// <remarks>
		/// Takes a sentence composed of CoreLabels and add the tags to the
		/// CoreLabels, modifying the input sentence.  If reuseTags is set to
		/// true, any tags supplied with the CoreLabels are taken as correct.
		/// </remarks>
		public virtual void TagCoreLabels(IList<CoreLabel> sentence, bool reuseTags)
		{
			IList<TaggedWord> taggedWords = TagSentence(sentence, reuseTags);
			if (taggedWords.Count != sentence.Count)
			{
				throw new AssertionError("Tagged word list not the same length " + "as the original sentence");
			}
			for (int i = 0; i < size; ++i)
			{
				sentence[i].SetTag(taggedWords[i].Tag());
			}
		}

		/// <summary>
		/// Adds lemmas to the given list of CoreLabels, using the given
		/// Morphology object.
		/// </summary>
		/// <remarks>
		/// Adds lemmas to the given list of CoreLabels, using the given
		/// Morphology object.  The input list must already have tags set.
		/// </remarks>
		public static void Lemmatize(IList<CoreLabel> sentence, Morphology morpha)
		{
			foreach (CoreLabel label in sentence)
			{
				morpha.Stem(label);
			}
		}

		/// <summary>
		/// Casts a list of HasWords, which we secretly know to be
		/// CoreLabels, to a list of CoreLabels.
		/// </summary>
		/// <remarks>
		/// Casts a list of HasWords, which we secretly know to be
		/// CoreLabels, to a list of CoreLabels.  Barfs if you didn't
		/// actually give it CoreLabels.
		/// </remarks>
		private static IList<CoreLabel> CastCoreLabels<_T0>(IList<_T0> sent)
			where _T0 : IHasWord
		{
			IList<CoreLabel> coreLabels = Generics.NewArrayList();
			foreach (IHasWord word in sent)
			{
				if (!(word is CoreLabel))
				{
					throw new InvalidCastException("Expected CoreLabels");
				}
				coreLabels.Add((CoreLabel)word);
			}
			return coreLabels;
		}

		/// <summary>
		/// Reads data from r, tokenizes it with the default (Penn Treebank)
		/// tokenizer, and returns a List of Sentence objects, which can
		/// then be fed into tagSentence.
		/// </summary>
		/// <param name="r">Reader where untokenized text is read</param>
		/// <returns>List of tokenized sentences</returns>
		public static IList<IList<IHasWord>> TokenizeText(Reader r)
		{
			return TokenizeText(r, null);
		}

		/// <summary>
		/// Reads data from r, tokenizes it with the given tokenizer, and
		/// returns a List of Lists of (extends) HasWord objects, which can then be
		/// fed into tagSentence.
		/// </summary>
		/// <param name="r">Reader where untokenized text is read</param>
		/// <param name="tokenizerFactory">
		/// Tokenizer.  This can be <code>null</code> in which case
		/// the default English tokenizer (PTBTokenizerFactory) is used.
		/// </param>
		/// <returns>List of tokenized sentences</returns>
		public static IList<IList<IHasWord>> TokenizeText<_T0>(Reader r, ITokenizerFactory<_T0> tokenizerFactory)
			where _T0 : IHasWord
		{
			DocumentPreprocessor documentPreprocessor = new DocumentPreprocessor(r);
			if (tokenizerFactory != null)
			{
				documentPreprocessor.SetTokenizerFactory(tokenizerFactory);
			}
			IList<IList<IHasWord>> @out = Generics.NewArrayList();
			foreach (IList<IHasWord> item in documentPreprocessor)
			{
				@out.Add(item);
			}
			return @out;
		}

		private static void DumpModel(TaggerConfig config)
		{
			try
			{
				Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger tagger = new Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger(config.GetModel(), config, false);
				System.Console.Out.WriteLine("Serialized tagger built with config:");
				tagger.config.Dump(System.Console.Out);
				tagger.DumpModel(System.Console.Out);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Tests a tagger on data with gold tags available.</summary>
		/// <remarks>Tests a tagger on data with gold tags available.  This is TEST mode.</remarks>
		/// <param name="config">Properties giving parameters for the testing run</param>
		private static void RunTest(TaggerConfig config)
		{
			if (config.GetVerbose())
			{
				log.Info("## tagger testing invoked at " + new DateTime() + " with arguments:");
				config.Dump();
			}
			try
			{
				Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger tagger = new Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger(config.GetModel(), config);
				Timing t = new Timing();
				TestClassifier testClassifier = new TestClassifier(tagger);
				long millis = t.Stop();
				PrintErrWordsPerSec(millis, testClassifier.GetNumWords());
				testClassifier.PrintModelAndAccuracy(tagger);
			}
			catch (Exception e)
			{
				log.Info("An error occurred while testing the tagger.");
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Reads in the training corpus from a filename and trains the tagger</summary>
		/// <param name="config">Configuration parameters for training a model (filename, etc.</param>
		/// <exception cref="System.IO.IOException">If IO problem</exception>
		private static void TrainAndSaveModel(TaggerConfig config)
		{
			string modelName = config.GetModel();
			Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger maxentTagger = new Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger();
			maxentTagger.Init(config);
			// Allow clobbering.  You want it all the time when running experiments.
			TaggerExperiments samples = new TaggerExperiments(config, maxentTagger);
			TaggerFeatures feats = samples.GetTaggerFeatures();
			byte[][] fnumArr = samples.GetFnumArr();
			log.Info("Samples from " + config.GetFile());
			log.Info("Number of features: " + feats.Size());
			log.Info("Tag set: " + maxentTagger.tags.TagSet());
			Problem p = new Problem(samples, feats);
			LambdaSolveTagger prob = new LambdaSolveTagger(p, 0.0001, fnumArr);
			maxentTagger.prob = prob;
			if (config.GetSearch().Equals("owlqn"))
			{
				CGRunner runner = new CGRunner(prob, config.GetModel(), config.GetSigmaSquared());
				runner.SolveL1(config.GetRegL1());
			}
			else
			{
				if (config.GetSearch().Equals("owlqn2"))
				{
					CGRunner runner = new CGRunner(prob, config.GetModel(), config.GetSigmaSquared());
					runner.SolveOWLQN2(config.GetRegL1());
				}
				else
				{
					if (config.GetSearch().Equals("cg"))
					{
						CGRunner runner = new CGRunner(prob, config.GetModel(), config.GetSigmaSquared());
						runner.SolveCG();
					}
					else
					{
						if (config.GetSearch().Equals("qn"))
						{
							CGRunner runner = new CGRunner(prob, config.GetModel(), config.GetSigmaSquared());
							runner.SolveQN();
						}
						else
						{
							prob.ImprovedIterative(config.GetIterations());
						}
					}
				}
			}
			if (prob.CheckCorrectness())
			{
				log.Info("Model is correct [empirical expec = model expec]");
			}
			else
			{
				log.Info("Model is not correct");
			}
			// Some of the rules may have been optimized so they don't have
			// any effect on the final scores.  Eliminating those rules
			// entirely saves space and runtime
			maxentTagger.RemoveDeadRules();
			// If any of the features have been optimized to 0, we can remove
			// them from the LambdaSolve.  This will save quite a bit of space
			// depending on the optimization used
			maxentTagger.SimplifyLambda();
			maxentTagger.SaveModel(modelName);
			log.Info("Extractors list:");
			log.Info(maxentTagger.extractors.ToString() + "\nrare" + maxentTagger.extractorsRare.ToString());
		}

		/// <summary>Trains a tagger model.</summary>
		/// <param name="config">Properties giving parameters for the training run</param>
		/// <exception cref="System.IO.IOException"/>
		private static void RunTraining(TaggerConfig config)
		{
			DateTime now = new DateTime();
			log.Info("## tagger training invoked at " + now + " with arguments:");
			config.Dump();
			Timing tim = new Timing();
			PrintFile log = new PrintFile(config.GetModel() + ".props");
			log.WriteLine("## tagger training invoked at " + now + " with arguments:");
			config.Dump(log);
			log.Close();
			TrainAndSaveModel(config);
			tim.Done("Training POS tagger");
		}

		private static void PrintErrWordsPerSec(long milliSec, int numWords)
		{
			double wordsPerSec = numWords / (((double)milliSec) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			log.Info("Tagged " + numWords + " words at " + nf.Format(wordsPerSec) + " words per second.");
		}

		internal class TaggerWrapper : IFunction<string, string>
		{
			private readonly TaggerConfig config;

			private readonly MaxentTagger tagger;

			private ITokenizerFactory<IHasWord> tokenizerFactory;

			private int sentNum;

			private readonly bool tokenize;

			private readonly bool outputVerbosity;

			private readonly bool outputLemmas;

			private readonly PlainTextDocumentReaderAndWriter.OutputStyle outputStyle;

			private readonly Morphology morpha;

			protected internal TaggerWrapper(MaxentTagger tagger)
			{
				// not so much a wrapper as a class with some various functionality
				// extending the MaxentTagger...
				// TODO: can we get rid of this? [cdm: sure. I'm not quite sure why Anna added it.  It seems like it could just be inside MaxentTagger]
				// = 0;
				// private final String tagSeparator;
				this.tagger = tagger;
				this.config = tagger.config;
				try
				{
					tokenizerFactory = ChooseTokenizerFactory(config.GetTokenize(), config.GetTokenizerFactory(), config.GetTokenizerOptions(), config.GetTokenizerInvertible());
				}
				catch (Exception e)
				{
					log.Info("Error in tokenizer factory instantiation for class: " + config.GetTokenizerFactory());
					Sharpen.Runtime.PrintStackTrace(e);
					tokenizerFactory = PTBTokenizer.PTBTokenizerFactory.NewWordTokenizerFactory(config.GetTokenizerOptions());
				}
				outputStyle = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(config.GetOutputFormat());
				outputVerbosity = config.GetOutputVerbosity();
				outputLemmas = config.GetOutputLemmas();
				morpha = (outputLemmas) ? new Morphology() : null;
				tokenize = config.GetTokenize();
			}

			// tagSeparator = config.getTagSeparator();
			public virtual string Apply(string o)
			{
				StringWriter taggedResults = new StringWriter();
				IList<IList<IHasWord>> sentences;
				if (tokenize)
				{
					sentences = TokenizeText(new StringReader(o), tokenizerFactory);
				}
				else
				{
					sentences = Generics.NewArrayList();
					sentences.Add(SentenceUtils.ToWordList(o.Split("\\s+")));
				}
				// TODO: there is another almost identical block of code elsewhere.  Refactor
				if (config.GetNThreads() != 1)
				{
					MulticoreWrapper<IList<IHasWord>, IList<IHasWord>> wrapper = new MulticoreWrapper<IList<IHasWord>, IList<IHasWord>>(config.GetNThreads(), new MaxentTagger.SentenceTaggingProcessor(tagger, outputLemmas));
					foreach (IList<IHasWord> sentence in sentences)
					{
						wrapper.Put(sentence);
						while (wrapper.Peek())
						{
							IList<IHasWord> taggedSentence = wrapper.Poll();
							tagger.OutputTaggedSentence(taggedSentence, outputLemmas, outputStyle, outputVerbosity, sentNum++, " ", taggedResults);
						}
					}
					wrapper.Join();
					while (wrapper.Peek())
					{
						IList<IHasWord> taggedSentence = wrapper.Poll();
						tagger.OutputTaggedSentence(taggedSentence, outputLemmas, outputStyle, outputVerbosity, sentNum++, " ", taggedResults);
					}
				}
				else
				{
					// there is only one thread
					foreach (IList<IHasWord> sent in sentences)
					{
						// Morphology morpha = (outputLemmas) ? new Morphology() : null;
						sent = tagger.TagCoreLabelsOrHasWords(sent, morpha, outputLemmas);
						tagger.OutputTaggedSentence(sent, outputLemmas, outputStyle, outputVerbosity, sentNum++, " ", taggedResults);
					}
				}
				return taggedResults.ToString();
			}
		}

		// end class TaggerWrapper
		private static string GetXMLWords<_T0>(IList<_T0> sentence, int sentNum, bool outputLemmas)
			where _T0 : IHasWord
		{
			bool hasCoreLabels = (sentence != null && sentence.Count > 0 && sentence[0] is CoreLabel);
			StringBuilder sb = new StringBuilder();
			sb.Append("<sentence id=\"").Append(sentNum).Append("\">\n");
			int wordIndex = 0;
			foreach (IHasWord hw in sentence)
			{
				string word = hw.Word();
				if (!(hw is IHasTag))
				{
					throw new ArgumentException("Expected HasTags, got " + hw.GetType());
				}
				string tag = ((IHasTag)hw).Tag();
				sb.Append("  <word wid=\"").Append(wordIndex).Append("\" pos=\"").Append(XMLUtils.EscapeAttributeXML(tag)).Append("\"");
				if (outputLemmas && hasCoreLabels)
				{
					if (!(hw is CoreLabel))
					{
						throw new ArgumentException("You mixed CoreLabels with " + hw.GetType() + "?  " + "Why would you do that?");
					}
					CoreLabel label = (CoreLabel)hw;
					string lemma = label.Lemma();
					if (lemma != null)
					{
						sb.Append(" lemma=\"").Append(XMLUtils.EscapeElementXML(lemma)).Append('\"');
					}
				}
				sb.Append(">").Append(XMLUtils.EscapeElementXML(word)).Append("</word>\n");
				++wordIndex;
			}
			sb.Append("</sentence>\n");
			return sb.ToString();
		}

		private static string GetTsvWords<_T0>(bool verbose, bool outputLemmas, IList<_T0> sentence)
			where _T0 : IHasWord
		{
			StringBuilder sb = new StringBuilder();
			if (verbose && sentence.Count > 0 && sentence[0] is CoreLabel)
			{
				foreach (IHasWord hw in sentence)
				{
					if (!(hw is CoreLabel))
					{
						throw new ArgumentException("You mixed CoreLabels with " + hw.GetType() + "?  " + "Why would you do that?");
					}
					CoreLabel label = (CoreLabel)hw;
					sb.Append(label.Word());
					sb.Append("\t");
					sb.Append(label.OriginalText());
					sb.Append("\t");
					if (outputLemmas)
					{
						sb.Append(label.Lemma());
						sb.Append("\t");
					}
					sb.Append(label.Tag());
					sb.Append("\t");
					sb.Append(label.BeginPosition());
					sb.Append("\t");
					sb.Append(label.EndPosition());
					sb.Append("\n");
				}
				sb.Append('\n');
				return sb.ToString();
			}
			// otherwise, fall through
			// either not verbose, or not CoreLabels
			foreach (IHasWord hw_1 in sentence)
			{
				string word = hw_1.Word();
				if (!(hw_1 is IHasTag))
				{
					throw new ArgumentException("Expected HasTags, got " + hw_1.GetType());
				}
				string tag = ((IHasTag)hw_1).Tag();
				sb.Append(word).Append('\t').Append(tag).Append('\n');
			}
			sb.Append('\n');
			return sb.ToString();
		}

		/// <summary>Takes a tagged sentence and writes out the xml version.</summary>
		/// <param name="w">Where to write the output to</param>
		/// <param name="sent">A tagged sentence</param>
		/// <param name="sentNum">The sentence index for XML printout</param>
		/// <param name="outputLemmas">Whether to write the lemmas of words</param>
		private static void WriteXMLSentence<_T0>(TextWriter w, IList<_T0> sent, int sentNum, bool outputLemmas)
			where _T0 : IHasWord
		{
			try
			{
				w.Write(GetXMLWords(sent, sentNum, outputLemmas));
			}
			catch (IOException e)
			{
				log.Info("Error writing sentence " + sentNum + ": " + SentenceUtils.ListToString(sent));
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// Uses an XML transformer to turn an input stream into a bunch of
		/// output.
		/// </summary>
		/// <remarks>
		/// Uses an XML transformer to turn an input stream into a bunch of
		/// output.  Tags all of the text between xmlTags.
		/// The difference between using this and using runTagger in XML mode
		/// is that this preserves the XML structure outside of the list of
		/// elements to tag, whereas the runTagger method throws away all of
		/// the surrounding structure and returns tagged plain text.
		/// </remarks>
		public virtual void TagFromXML(InputStream input, TextWriter writer, params string[] xmlTags)
		{
			PlainTextDocumentReaderAndWriter.OutputStyle outputStyle = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(config.GetOutputFormat());
			TransformXML<string> txml = new TransformXML<string>();
			switch (outputStyle)
			{
				case PlainTextDocumentReaderAndWriter.OutputStyle.Xml:
				case PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml:
				{
					txml.TransformXML(xmlTags, new MaxentTagger.TaggerWrapper(this), input, writer, new TransformXML.NoEscapingSAXInterface<string>());
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.SlashTags:
				case PlainTextDocumentReaderAndWriter.OutputStyle.Tsv:
				{
					txml.TransformXML(xmlTags, new MaxentTagger.TaggerWrapper(this), input, writer, new TransformXML.SAXInterface<string>());
					break;
				}

				default:
				{
					throw new Exception("Unexpected format " + outputStyle);
				}
			}
		}

		public virtual void TagFromXML(Reader input, TextWriter writer, params string[] xmlTags)
		{
			PlainTextDocumentReaderAndWriter.OutputStyle outputStyle = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(config.GetOutputFormat());
			TransformXML<string> txml = new TransformXML<string>();
			switch (outputStyle)
			{
				case PlainTextDocumentReaderAndWriter.OutputStyle.Xml:
				case PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml:
				{
					txml.TransformXML(xmlTags, new MaxentTagger.TaggerWrapper(this), input, writer, new TransformXML.NoEscapingSAXInterface<string>());
					break;
				}

				case PlainTextDocumentReaderAndWriter.OutputStyle.SlashTags:
				case PlainTextDocumentReaderAndWriter.OutputStyle.Tsv:
				{
					txml.TransformXML(xmlTags, new MaxentTagger.TaggerWrapper(this), input, writer, new TransformXML.SAXInterface<string>());
					break;
				}

				default:
				{
					throw new Exception("Unexpected format " + outputStyle);
				}
			}
		}

		private void TagFromXML()
		{
			Reader reader = null;
			TextWriter w = null;
			try
			{
				// todo [cdm dec 13]: change to use the IOUtils read-from-anywhere routines
				reader = new BufferedReader(new InputStreamReader(new FileInputStream(config.GetFile()), config.GetEncoding()));
				string outFile = config.GetOutputFile();
				if (outFile.Length > 0)
				{
					w = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFile), config.GetEncoding()));
				}
				else
				{
					w = new BufferedWriter(new OutputStreamWriter(System.Console.Out, config.GetEncoding()));
				}
				w.Write("<?xml version=\"1.0\" encoding=\"" + config.GetEncoding() + "\"?>\n");
				TagFromXML(reader, w, config.GetXMLInput());
			}
			catch (FileNotFoundException e)
			{
				log.Info("Input file not found: " + config.GetFile());
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException ioe)
			{
				log.Info("tagFromXML: mysterious IO Exception");
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(reader);
				IOUtils.CloseIgnoringExceptions(w);
			}
		}

		/// <summary>Loads the tagger from a config file and then runs it in TAG mode.</summary>
		/// <param name="config">The configuration parameters for the run.</param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		private static void RunTagger(TaggerConfig config)
		{
			if (config.GetVerbose())
			{
				DateTime now = new DateTime();
				log.Info("## tagger invoked at " + now + " with arguments:");
				config.Dump();
			}
			MaxentTagger tagger = new MaxentTagger(config.GetModel(), config);
			tagger.RunTagger();
		}

		private static readonly Pattern formatPattern = Pattern.Compile("format=[a-zA-Z]+,");

		/// <summary>Runs the tagger when we're in TAG mode.</summary>
		/// <remarks>
		/// Runs the tagger when we're in TAG mode.
		/// In this mode, the config contains either the name of the file to
		/// tag or stdin.  That file or input is then tagged.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		private void RunTagger()
		{
			string[] xmlInput = config.GetXMLInput();
			if (xmlInput.Length > 0)
			{
				if (xmlInput.Length > 1 || !xmlInput[0].Equals("null"))
				{
					TagFromXML();
					return;
				}
			}
			BufferedWriter writer = null;
			BufferedReader br = null;
			try
			{
				string outFile = config.GetOutputFile();
				if (outFile.Length > 0)
				{
					writer = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFile), config.GetEncoding()));
				}
				else
				{
					writer = new BufferedWriter(new OutputStreamWriter(System.Console.Out, config.GetEncoding()));
				}
				//Now determine if we're tagging from stdin or from a file,
				//construct a reader accordingly
				bool stdin = config.UseStdin();
				PlainTextDocumentReaderAndWriter.OutputStyle outputStyle = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(config.GetOutputFormat());
				if (!stdin)
				{
					string filename = config.GetFile();
					if (formatPattern.Matcher(filename).Find())
					{
						TaggedFileRecord record = TaggedFileRecord.CreateRecord(config, filename);
						RunTagger(record.Reader(), writer, outputStyle);
					}
					else
					{
						br = IOUtils.ReaderFromString(config.GetFile(), config.GetEncoding());
						RunTagger(br, writer, config.GetTagInside(), outputStyle);
					}
				}
				else
				{
					log.Info("Type some text to tag, then EOF.");
					log.Info("  (For EOF, use Return, Ctrl-D on Unix; Enter, Ctrl-Z, Enter on Windows.)");
					br = new BufferedReader(new InputStreamReader(Runtime.@in));
					RunTaggerStdin(br, writer, outputStyle);
				}
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(br);
				IOUtils.CloseIgnoringExceptions(writer);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void RunTaggerStdin(BufferedReader reader, BufferedWriter writer, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle)
		{
			ITokenizerFactory<IHasWord> tokenizerFactory = ChooseTokenizerFactory();
			//Counts
			long totalMillis = 0;
			int numWords = 0;
			int numSentences = 0;
			bool outputVerbosity = config.GetOutputVerbosity();
			bool outputLemmas = config.GetOutputLemmas();
			Morphology morpha = (outputLemmas) ? new Morphology() : null;
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("<?xml version=\"1.0\" encoding=\"" + config.GetEncoding() + "\"?>\n");
				writer.Write("<pos>\n");
			}
			string sentenceDelimiter = config.GetSentenceDelimiter();
			if (sentenceDelimiter != null && sentenceDelimiter.Equals("newline"))
			{
				sentenceDelimiter = "\n";
			}
			while (true)
			{
				//Now we do everything through the doc preprocessor
				DocumentPreprocessor docProcessor;
				string line = reader.ReadLine();
				// this happens when we reach end of file
				if (line == null)
				{
					break;
				}
				docProcessor = new DocumentPreprocessor(new StringReader(line));
				docProcessor.SetTokenizerFactory(tokenizerFactory);
				docProcessor.SetSentenceDelimiter(sentenceDelimiter);
				if (config.KeepEmptySentences())
				{
					docProcessor.SetKeepEmptySentences(true);
				}
				foreach (IList<IHasWord> sentence in docProcessor)
				{
					numWords += sentence.Count;
					Timing t = new Timing();
					TagAndOutputSentence(sentence, outputLemmas, morpha, outputStyle, outputVerbosity, numSentences, string.Empty, writer);
					totalMillis += t.Stop();
					writer.NewLine();
					writer.Flush();
					numSentences++;
				}
			}
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("</pos>\n");
			}
			writer.Flush();
			PrintErrWordsPerSec(totalMillis, numWords);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void RunTaggerSGML(BufferedReader reader, BufferedWriter writer, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle)
		{
			Timing t = new Timing();
			//Counts
			int numWords = 0;
			int numSentences = 0;
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("<?xml version=\"1.0\" encoding=\"" + config.GetEncoding() + "\"?>\n");
				writer.Write("<pos>\n");
			}
			// this uses NER codebase technology to read/write SGML-ish files
			PlainTextDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
			ObjectBank<IList<CoreLabel>> ob = new ObjectBank<IList<CoreLabel>>(new ReaderIteratorFactory(reader), readerAndWriter);
			PrintWriter pw = new PrintWriter(writer);
			foreach (IList<CoreLabel> sentence in ob)
			{
				IList<CoreLabel> s = Generics.NewArrayList();
				numWords += s.Count;
				IList<TaggedWord> taggedSentence = TagSentence(s, false);
				IEnumerator<CoreLabel> origIter = sentence.GetEnumerator();
				foreach (TaggedWord tw in taggedSentence)
				{
					CoreLabel cl = origIter.Current;
					cl.Set(typeof(CoreAnnotations.AnswerAnnotation), tw.Tag());
				}
				readerAndWriter.PrintAnswers(sentence, pw, outputStyle, true);
				++numSentences;
			}
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("</pos>\n");
			}
			writer.Flush();
			long millis = t.Stop();
			PrintErrWordsPerSec(millis, numWords);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void RunTagger<X>(IEnumerable<IList<X>> document, BufferedWriter writer, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle)
			where X : IHasWord
		{
			Timing t = new Timing();
			//Counts
			int numWords = 0;
			int numSentences = 0;
			bool outputVerbosity = config.GetOutputVerbosity();
			bool outputLemmas = config.GetOutputLemmas();
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("<?xml version=\"1.0\" encoding=\"" + config.GetEncoding() + "\"?>\n");
				writer.Write("<pos>\n");
			}
			if (config.GetNThreads() != 1)
			{
				MulticoreWrapper<IList<IHasWord>, IList<IHasWord>> wrapper = new MulticoreWrapper<IList<IHasWord>, IList<IHasWord>>(config.GetNThreads(), new MaxentTagger.SentenceTaggingProcessor(this, outputLemmas));
				foreach (IList<X> sentence in document)
				{
					wrapper.Put(sentence);
					while (wrapper.Peek())
					{
						IList<IHasWord> taggedSentence = wrapper.Poll();
						numWords += taggedSentence.Count;
						OutputTaggedSentence(taggedSentence, outputLemmas, outputStyle, outputVerbosity, numSentences, "\n", writer);
						numSentences++;
					}
				}
				wrapper.Join();
				while (wrapper.Peek())
				{
					IList<IHasWord> taggedSentence = wrapper.Poll();
					numWords += taggedSentence.Count;
					OutputTaggedSentence(taggedSentence, outputLemmas, outputStyle, outputVerbosity, numSentences, "\n", writer);
					numSentences++;
				}
			}
			else
			{
				Morphology morpha = (outputLemmas) ? new Morphology() : null;
				foreach (IList<X> sentence in document)
				{
					numWords += sentence.Count;
					TagAndOutputSentence(sentence, outputLemmas, morpha, outputStyle, outputVerbosity, numSentences, "\n", writer);
					numSentences++;
				}
			}
			if (outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.Xml || outputStyle == PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml)
			{
				writer.Write("</pos>\n");
			}
			writer.Flush();
			long millis = t.Stop();
			PrintErrWordsPerSec(millis, numWords);
		}

		/// <summary>This method runs the tagger on the provided reader and writer.</summary>
		/// <remarks>
		/// This method runs the tagger on the provided reader and writer.
		/// It takes input from the given
		/// <paramref name="reader"/>
		/// , applies the
		/// tagger to it one sentence at a time (determined using
		/// documentPreprocessor), and writes the output to the given
		/// <paramref name="writer"/>
		/// .
		/// The document is broken into sentences using the sentence
		/// processor determined in the tagger's TaggerConfig.
		/// <paramref name="tagInside"/>
		/// makes the tagger run in XML mode.... If set
		/// to non-empty, instead of processing the document as one large
		/// text blob, it considers each region in between the given tag to
		/// be a separate text blob.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual void RunTagger(BufferedReader reader, BufferedWriter writer, string tagInside, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle)
		{
			string sentenceDelimiter = config.GetSentenceDelimiter();
			if (sentenceDelimiter != null && sentenceDelimiter.Equals("newline"))
			{
				sentenceDelimiter = "\n";
			}
			ITokenizerFactory<IHasWord> tokenizerFactory = ChooseTokenizerFactory();
			//Now we do everything through the doc preprocessor
			DocumentPreprocessor docProcessor;
			if (tagInside.Length > 0)
			{
				docProcessor = new DocumentPreprocessor(reader, DocumentPreprocessor.DocType.Xml);
				docProcessor.SetElementDelimiter(tagInside);
				if (config.KeepEmptySentences())
				{
					docProcessor.SetKeepEmptySentences(true);
				}
			}
			else
			{
				docProcessor = new DocumentPreprocessor(reader);
				docProcessor.SetSentenceDelimiter(sentenceDelimiter);
				if (config.KeepEmptySentences())
				{
					docProcessor.SetKeepEmptySentences(true);
				}
			}
			docProcessor.SetTokenizerFactory(tokenizerFactory);
			RunTagger(docProcessor, writer, outputStyle);
		}

		public virtual IList<IHasWord> TagCoreLabelsOrHasWords<_T0>(IList<_T0> sentence, Morphology morpha, bool outputLemmas)
			where _T0 : IHasWord
		{
			if (sentence.Count > 0 && sentence[0] is CoreLabel)
			{
				IList<CoreLabel> coreLabels = CastCoreLabels(sentence);
				TagCoreLabels(coreLabels);
				if (outputLemmas)
				{
					// We may want to lemmatize things without using an existing
					// Morphology object, as Morphology objects are not
					// thread-safe, so we would make a new one here
					if (morpha == null)
					{
						morpha = new Morphology();
					}
					Lemmatize(coreLabels, morpha);
				}
				return coreLabels;
			}
			else
			{
				IList<TaggedWord> taggedSentence = TagSentence(sentence, false);
				return taggedSentence;
			}
		}

		public virtual void TagAndOutputSentence<_T0>(IList<_T0> sentence, bool outputLemmas, Morphology morpha, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle, bool outputVerbosity, int numSentences, string separator, TextWriter writer)
			where _T0 : IHasWord
		{
			sentence = TagCoreLabelsOrHasWords(sentence, morpha, outputLemmas);
			OutputTaggedSentence(sentence, outputLemmas, outputStyle, outputVerbosity, numSentences, separator, writer);
		}

		public virtual void OutputTaggedSentence<_T0>(IList<_T0> sentence, bool outputLemmas, PlainTextDocumentReaderAndWriter.OutputStyle outputStyle, bool outputVerbosity, int numSentences, string separator, TextWriter writer)
			where _T0 : IHasWord
		{
			try
			{
				switch (outputStyle)
				{
					case PlainTextDocumentReaderAndWriter.OutputStyle.Tsv:
					{
						writer.Write(GetTsvWords(outputVerbosity, outputLemmas, sentence));
						break;
					}

					case PlainTextDocumentReaderAndWriter.OutputStyle.Xml:
					case PlainTextDocumentReaderAndWriter.OutputStyle.InlineXml:
					{
						WriteXMLSentence(writer, sentence, numSentences, outputLemmas);
						break;
					}

					case PlainTextDocumentReaderAndWriter.OutputStyle.SlashTags:
					{
						writer.Write(SentenceUtils.ListToString(sentence, false, config.GetTagSeparator()));
						writer.Write(separator);
						break;
					}

					default:
					{
						throw new ArgumentException("Unsupported output style " + outputStyle);
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Command-line tagger interface.</summary>
		/// <remarks>
		/// Command-line tagger interface.
		/// Can be used to train or test taggers, or to tag text, taking input from
		/// stdin or a file.
		/// See class documentation for usage.
		/// </remarks>
		/// <param name="args">Command-line arguments</param>
		/// <exception cref="System.IO.IOException">If any file problems</exception>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			TaggerConfig config = new TaggerConfig(args);
			if (config.GetMode() == TaggerConfig.Mode.Train)
			{
				RunTraining(config);
			}
			else
			{
				if (config.GetMode() == TaggerConfig.Mode.Tag)
				{
					RunTagger(config);
				}
				else
				{
					if (config.GetMode() == TaggerConfig.Mode.Test)
					{
						RunTest(config);
					}
					else
					{
						if (config.GetMode() == TaggerConfig.Mode.Dump)
						{
							DumpModel(config);
						}
						else
						{
							log.Info("Impossible: nothing to do. None of train, tag, test, or dump was specified.");
						}
					}
				}
			}
		}

		internal class SentenceTaggingProcessor : IThreadsafeProcessor<IList<IHasWord>, IList<IHasWord>>
		{
			internal MaxentTagger maxentTagger;

			internal bool outputLemmas;

			internal SentenceTaggingProcessor(MaxentTagger maxentTagger, bool outputLemmas)
			{
				// end main()
				this.maxentTagger = maxentTagger;
				this.outputLemmas = outputLemmas;
			}

			public virtual IList<IHasWord> Process<_T0>(IList<_T0> sentence)
				where _T0 : IHasWord
			{
				return maxentTagger.TagCoreLabelsOrHasWords(sentence, null, outputLemmas);
			}

			public virtual IThreadsafeProcessor<IList<IHasWord>, IList<IHasWord>> NewInstance()
			{
				// MaxentTagger is threadsafe
				return this;
			}
		}

		private const long serialVersionUID = 2;
	}
}
