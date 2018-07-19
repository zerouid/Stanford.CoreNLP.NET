// Stanford Classifier, ColumnDataClassifier - a multiclass maxent classifier
// Copyright (c) 2003-2012 The Board of Trustees of
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
// This code is a parameter language for front-end feature
// generation for the loglinear model classification code in
// the Stanford Classifier package (mainly written by Dan Klein).
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/classifier.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// ColumnDataClassifier provides a command-line interface for doing
	/// context-free (independent) classification of a series of data items,
	/// where each data item is represented by a line of
	/// a file, as a list of String variables, in tab-separated columns.
	/// </summary>
	/// <remarks>
	/// ColumnDataClassifier provides a command-line interface for doing
	/// context-free (independent) classification of a series of data items,
	/// where each data item is represented by a line of
	/// a file, as a list of String variables, in tab-separated columns.  Some
	/// features will interpret these variables as numbers, but
	/// the code is mainly oriented towards generating features for string
	/// classification.  To designate a real-valued feature, use the realValued
	/// option described below. The classifier by default is a maxent classifier
	/// (also known as a softmax classifier or a discriminative loglinear classifier;
	/// equivalent to multiclass logistic regression apart from a slightly different
	/// symmetric parameterization. It also implements a Bernoulli Naive
	/// Bayes model and can implement an SVM by an external call to SVMlight.
	/// You can also use ColumnDataClassifier programmatically, where its main
	/// usefulness beyond simply building your own LinearClassifier is that it
	/// provides easy conversion of data items into features, using the same
	/// properties as the command-line version. You can see example of usage in
	/// the class
	/// <see cref="Edu.Stanford.Nlp.Classify.Demo.ClassifierDemo"/>
	/// .
	/// Input files are expected to
	/// be one data item per line with two or more columns indicating the class
	/// of the item and one or more predictive features.  Columns are
	/// separated by tab characters.  Tab and newline characters cannot occur
	/// inside field values (there is no escaping mechanism); any other characters
	/// are legal in field values.
	/// Typical usage:
	/// <c>java edu.stanford.nlp.classify.ColumnDataClassifier -prop propFile</c>
	/// or
	/// <c>
	/// java -mx300m edu.stanford.nlp.classify.ColumnDataClassifier
	/// -trainFile trainFile -testFile testFile -useNGrams|... &gt; output
	/// </c>
	/// (Note that for large data sets, you may wish to specify
	/// the amount of memory available to Java, such
	/// as in the second example above.)
	/// In the simplest case, there are just two tab-separated columns in the
	/// training input: the first for the class, and the second for the String
	/// datum which has that class.   In more complex uses, each datum can
	/// be multidimensional, and there are many columns of data attributes.
	/// To illustrate simple uses, and the behavior of Naive Bayes and Maximum
	/// entropy classifiers, example files corresponding to the examples from the
	/// Manning and Klein maxent classifier tutorial, slides 46-49, available at
	/// https://nlp.stanford.edu/software/classifier.html are included in the
	/// classify package source directory (files starting with "easy").  Other
	/// examples appear in the
	/// <c>examples</c>
	/// directory of the distributed
	/// classifier.
	/// In many instances, parameters can either be given on the command line
	/// or provided using a Properties file
	/// (specified on the command-line with
	/// <c>-prop</c>
	/// <i>propFile</i>).
	/// Option names are the same as property names with a preceding dash.  Boolean
	/// properties can simply be turned on via the command line.  Parameters of
	/// types int, String, and double take a following argument after the option.
	/// Command-line parameters can only define features for the first column
	/// describing the datum.  If you have multidimensional data, you need to use
	/// a properties file.  Property names, as below, are either global (things
	/// like the testFile name) or are seen as properties that define features
	/// for the first data column (NOTE: we count columns from 0 - unlike the Unix cut
	/// command!).  To specify features for a particular data column, precede a
	/// feature by a column number and then a period (for example,
	/// <c>3.wordShape=chris4</c>
	/// ).  If no number is specified, then the
	/// default interpretation is column 0. Note that in properties files you must
	/// give a value to boolean properties (e.g.,
	/// <c>2.useString=true</c>
	/// );
	/// just giving the property name (as
	/// <c>2.useString</c>
	/// ) isn't
	/// sufficient.
	/// The following properties are recognized:
	/// <table border="1">
	/// <caption>Properties for ColumnDataClassifier</caption>
	/// <tr><th><b>Property Name</b></th><th><b>Type</b></th><th><b>Default Value</b></th><th><b>Description</b></th><th><b>FeatName</b></th></tr>
	/// <tr><td> loadClassifier </td><td>String</td><td>n/a</td><td>Path of serialized classifier file to load</td></tr>
	/// <tr><td> serializeTo</td><td>String</td><td>n/a</td><td>Path to serialize classifier to</td></tr>
	/// <tr><td> printTo</td><td>String</td><td>n/a</td><td>Path to print a text representation of the linear classifier to</td></tr>
	/// <tr><td> trainFile</td><td>String</td><td>n/a</td><td>Path of file to use as training data</td></tr>
	/// <tr><td> testFile</td><td>String</td><td>n/a</td><td>Path of file to use as test data</td></tr>
	/// <tr><td> encoding</td><td>String</td><td><i>utf-8</i></td><td>Character encoding of training and test file, e.g., utf-8, GB18030, or iso-8859-1</td></tr>
	/// <tr><td> displayedColumn</td><td>int</td><td>1</td><td>Column number that will be printed out to stdout in the output next to the gold class and the chosen class.  This is just an aide memoire.  If the value is negative, nothing is printed. </td></tr>
	/// <tr><td> displayAllAnswers</td><td>boolean</td><td>false</td><td>If true, print all classes and their probability, sorted by probability, rather than just the highest scoring and correct classes. </td></tr>
	/// <tr><td> goldAnswerColumn</td><td>int</td><td>0</td><td>Column number that contains the correct class for each data item (again, columns are numbered from 0 up).</td></tr>
	/// <tr><td> groupingColumn</td><td>int</td><td>-1</td><td>Column for grouping multiple data items for the purpose of computing ranking accuracy.  This is appropriate when only one datum in a group can be correct, and the intention is to choose the highest probability one, rather than accepting all above a threshold.  Multiple items in the same group must be contiguous in the test file (otherwise it would be necessary to cache probabilities and groups for the entire test file to check matches).  If it is negative, no grouping column is used, and no ranking accuracy is reported.</td></tr>
	/// <tr><td> rankingScoreColumn</td><td>int</td><td>-1</td><td>If this parameter is non-negative and a groupingColumn is defined, then an average ranking score will be calculated by scoring the chosen candidate from a group according to its value in this column (for instance, the values of this column can be set to a mean reciprocal rank of 1.0 for the best answer, 0.5 for the second best and so on, or the value of this column can be a similarity score reflecting the similarity of the answer to the true answer.</td></tr>
	/// <tr><td> rankingAccuracyClass</td><td>String</td><td>null</td><td>If this and groupingColumn are defined (positive), then the system will compute a ranking accuracy under the assumption that there is (at most) one assignment of this class for each group, and ranking accuracy counts the classifier as right if that datum is the one with highest probability according to the model.</td></tr>
	/// <tr></tr>
	/// <tr><td> useString</td><td>boolean</td><td>false</td><td>Gives you a feature for whole string s</td><td>S-<i>str</i></td></tr>
	/// <tr><td> useClassFeature</td><td>boolean</td><td>false</td><td>Include a feature for the class (as a class marginal).  This is the same thing as having a bias vector or having an always-on feature in a model.</td><td>CLASS</td></tr>
	/// <tr><td> binnedLengths</td><td>String</td><td>null</td><td>If non-null, treat as a sequence of comma separated integer bounds, where items above the previous bound (if any) up to the next bound (inclusive) are binned (e.g., "1,5,15,30,60"). The feature represents the length of the String in this column.</td><td>Len-<i>range</i></td></tr>
	/// <tr><td> binnedLengthsStatistics</td><td>boolean</td><td>false</td><td>If true, print to stderr contingency table of statistics for binnedLengths.</td><td></td></tr>
	/// <tr><td> binnedValues</td><td>String</td><td>null</td><td>If non-null, treat as a sequence of comma separated double bounds, where data items above the previous bound up to the next bound (inclusive) are binned. If a value in this column isn't a legal
	/// <c>double</c>
	/// , then the value is treated as
	/// <c>binnedValuesNaN</c>
	/// .</td><td>Val-<i>range</i></td></tr>
	/// <tr><td> binnedValuesNaN</td><td>double</td><td>-1.0</td><td>If the value of a numeric binnedValues field is not a number, it will be given this value.</td></tr>
	/// <tr><td> binnedValuesStatistics</td><td>boolean</td><td>false</td><td>If true, print to stderr a contingency table of statistics for binnedValues.</td><td></td></tr>
	/// <tr><td> countChars</td><td>String</td><td>null</td><td>If non-null, count the number of occurrences of each character in the String, and make a feature for each character, binned according to
	/// <c>countCharsBins</c>
	/// </td><td>Char-<i>ch</i>-<i>range</i></td></tr>
	/// <tr><td> countCharsBins</td><td>String</td><td>"0,1"</td><td>Treat as a sequence of comma separated integer bounds, where character counts above the previous bound up to and including the next bound are binned. For instance, a value of "0,2" will give 3 bins, dividing a character count into bins of 0, 1-or-2, and 3-or-more occurrences.</td><td></td></tr>
	/// <tr><td> splitWordsRegexp</td><td>String</td><td>null</td><td>If defined, use this as a regular expression on which to split the whole string (as in the String.split() function, which will return the things between delimiters, and discard the delimiters).  The resulting split-up "words" will be used in classifier features iff one of the other "useSplit" options is turned on.</td></tr>
	/// <tr><td> splitWordsTokenizerRegexp</td><td>String</td><td>null</td><td>If defined, use this as a regular expression to cut initial pieces off a String.  Either this regular expression or
	/// <c>splitWordsIgnoreRegexp</c>
	/// <i>should always match</i> the start of the String, and the size of the token is the number of characters matched.  So, for example, one can group letter and number characters but do nothing else with a regular expression like
	/// <c>([A-Za-z]+|[0-9]+|.)</c>
	/// , where the last disjunct will match any other single character.  (If neither regular expression matches, the first character of the string is treated as a one character word, and then matching is tried again, but in this case a warning message is printed.)  Note that, for Java regular expressions with disjunctions like this, the match is the first matching disjunction, not the longest matching disjunction, so patterns with common prefixes need to be ordered from most specific (longest) to least specific (shortest).)  The resulting split up "words" will be used in classifier features iff one of the other "useSplit" options is turned on.  Note that as usual for Java String processing, backslashes must be doubled in the regular expressions that you write.</td></tr>
	/// <tr><td> splitWordsIgnoreRegexp</td><td>String</td><td>\\s+</td><td>If non-empty, this regexp is used to determine character sequences which should not be returned as tokens when using
	/// <c>splitWordsTokenizerRegexp</c>
	/// or
	/// <c>splitWordsRegexp</c>
	/// . With the former, first the program attempts to match this regular expression at the start of the string (with
	/// <c>lookingAt()</c>
	/// ) and if it matches, those characters are discarded, but if it doesn't match then
	/// <c>splitWordsTokenizerRegexp</c>
	/// is tried. With
	/// <c>splitWordsRegexp</c>
	/// , this is used to filter tokens (with
	/// <c>matches()</c>
	/// resulting from the splitting.  By default this regular expression is set to be all whitespace tokens (i.e., \\s+). Set it to an empty string to get all tokens returned.</td></tr>
	/// <tr><td> splitWordsWithPTBTokenizer</td><td>boolean</td><td>false</td><td>If true, and
	/// <c>splitWordsRegexp</c>
	/// and
	/// <c>splitWordsTokenizerRegexp</c>
	/// are false, then will tokenize using the
	/// <c>PTBTokenizer</c>
	/// </td></tr>
	/// <tr><td> useSplitWords</td><td>boolean</td><td>false</td><td>Make features from the "words" that are returned by dividing the string on splitWordsRegexp or splitWordsTokenizerRegexp.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>SW-<i>str</i></td></tr>
	/// <tr><td> useLowercaseSplitWords</td><td>boolean</td><td>false</td><td>Make features from the "words" that are returned by dividing the string on splitWordsRegexp or splitWordsTokenizerRegexp and then lowercasing the result.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.  Note that this can be specified independently of useSplitWords. You can put either or both original cased and lowercased words in as features.</td><td>LSW-<i>str</i></td></tr>
	/// <tr><td> useSplitWordPairs</td><td>boolean</td><td>false</td><td>Make features from the pairs of adjacent "words" that are returned by dividing the string into splitWords.  Requires splitWordsRegexp or splitWordsTokenizerRegexp. This doesn't add features for the first and last words being next to a boundary; if you want those, also set useSplitFirstLastWords.</td><td>SWP-<i>str1</i>-<i>str2</i></td></tr>
	/// <tr><td> useLowercaseSplitWordPairs</td><td>boolean</td><td>false</td><td>Make features from the lowercased form of the pairs of adjacent "words" that are returned by dividing the string into splitWords.  Requires splitWordsRegexp or splitWordsTokenizerRegexp. This doesn't add features for the first and last words being next to a boundary; if you want those, also set useLowercaseSplitFirstLastWords.</td><td>LSWP-<i>str1</i>-<i>str2</i></td></tr>
	/// <tr><td> useAllSplitWordPairs</td><td>boolean</td><td>false</td><td>Make features from all pairs of "words" that are returned by dividing the string into splitWords.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>ASWP-<i>str1</i>-<i>str2</i></td></tr>
	/// <tr><td> useAllSplitWordTriples</td><td>boolean</td><td>false</td><td>Make features from all triples of "words" that are returned by dividing the string into splitWords.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>ASWT-<i>str1</i>-<i>str2</i>-<i>str3</i></td></tr>
	/// <tr><td> useSplitWordNGrams</td><td>boolean</td><td>false</td><td>Make features of adjacent word n-grams of lengths between minWordNGramLeng and maxWordNGramLeng inclusive. Note that these are word sequences, not character n-grams.</td><td>SW#-<i>str1-str2-strN</i></td></tr>
	/// <tr><td> splitWordCount</td><td>boolean</td><td>false</td><td>If true, the value of this real-valued feature is the number of split word tokens in the column.</td><td>SWNUM</td></tr>
	/// <tr><td> logSplitWordCount</td><td>boolean</td><td>false</td><td>If true, the value of this real-valued feature is the log of the number of split word tokens in the column.</td><td>LSWNUM</td></tr>
	/// <tr><td> binnedSplitWordCounts</td><td>String</td><td>null</td><td>If non-null, treat as a sequence of comma-separated integer bounds, where items above the previous bound (if any) up to the next bound (inclusive) are binned (e.g., "1,5,15,30,60"). The feature represents the number of split words in this column.</td><td>SWNUMBIN-<i>range</i></td></tr>
	/// <tr><td> maxWordNGramLeng</td><td>int</td><td>-1</td><td>If this number is positive, word n-grams above this size will not be used in the model</td></tr>
	/// <tr><td> minWordNGramLeng</td><td>int</td><td>1</td><td>Must be positive. word n-grams below this size will not be used in the model</td></tr>
	/// <tr><td> wordNGramBoundaryRegexp</td><td>String</td><td>null</td><td>If this is defined and the regexp matches, then the ngram stops</td></tr>
	/// <tr><td> useSplitFirstLastWords</td><td>boolean</td><td>false</td><td>Make a feature from each of the first and last "words" that are returned as splitWords.  This is equivalent to having word bigrams with boundary tokens at each end of the sequence (they get a special feature).  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>SFW-<i>str</i>, SLW-<i>str</i></td></tr>
	/// <tr><td> useLowercaseSplitFirstLastWords</td><td>boolean</td><td>false</td><td>Make a feature from each of the first and last "words" that are returned as splitWords.  This is equivalent to having word bigrams with boundary tokens at each end of the sequence (they get a special feature).  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>LSFW-<i>str</i>, LSLW-<i>str</i></td></tr>
	/// <tr><td> useSplitNGrams</td><td>boolean</td><td>false</td><td>Make features from letter n-grams - internal as well as edge all treated the same - after the data string has been split into tokens.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>S#-<i>str</i></td></tr>
	/// <tr><td> useSplitPrefixSuffixNGrams</td><td>boolean</td><td>false</td><td>Make features from prefixes and suffixes of each token, after splitting string with splitWordsRegexp.  Requires splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>S#B-<i>str</i>, S#E-<i>str</i></td></tr>
	/// <tr><td> useSplitWordVectors</td><td>String</td><td>null</td><td>(If non-null) load word vectors from this file and add their average over all split words as real-valued features.  Requires splitWordsRegexp or splitWordsTokenizerRegexp. Note that for best results you need a close match between your tokenization and that used by the word vectors.</td><td>SWV-<i>num</i></td></tr>
	/// <tr><td> useNGrams</td><td>boolean</td><td>false</td><td>Make features from letter n-grams - internal as well as edge all treated the same.</td><td>#-<i>str</i></td></tr>
	/// <tr><td> usePrefixSuffixNGrams</td><td>boolean</td><td>false</td><td>Make features from prefix and suffix substrings of the string.</td><td>#B-<i>str</i>, #E-<i>str</i></td></tr>
	/// <tr><td> lowercase</td><td>boolean</td><td>false</td><td>Make the input string lowercase so all features work uncased</td></tr>
	/// <tr><td> lowercaseNGrams</td><td>boolean</td><td>false</td><td>Make features from letter n-grams all lowercase (for all of useNGrams, usePrefixSuffixNGrams, useSplitNGrams, and useSplitPrefixSuffixNGrams)</td></tr>
	/// <tr><td> maxNGramLeng</td><td>int</td><td>-1</td><td>If this number is positive, n-grams above this size will not be used in the model</td></tr>
	/// <tr><td> minNGramLeng</td><td>int</td><td>2</td><td>Must be positive. n-grams below this size will not be used in the model</td></tr>
	/// <tr><td> partialNGramRegexp</td><td>String</td><td>null</td><td>If this is defined and the regexp matches, then n-grams are made only from the matching text (if no capturing groups are defined) or from the first capturing group of the regexp, if there is one.  This substring is used for both useNGrams and usePrefixSuffixNGrams.</td></tr>
	/// <tr><td> realValued</td><td>boolean</td><td>false</td><td>Treat this column as real-valued and do not perform any transforms on the feature value.</td><td>Value</td></tr>
	/// <tr><td> logTransform</td><td>boolean</td><td>false</td><td>Treat this column as real-valued and use the log of the value as the feature value.</td><td>Log</td></tr>
	/// <tr><td> logitTransform</td><td>boolean</td><td>false</td><td>Treat this column as real-valued and use the logit of the value as the feature value.</td><td>Logit</td></tr>
	/// <tr><td> sqrtTransform</td><td>boolean</td><td>false</td><td>Treat this column as real-valued and use the square root of the value as the feature value.</td><td>Sqrt</td></tr>
	/// <tr><td> filename</td><td>boolean</td><td>false</td><td>Treat this column as a filename (path) and then use the contents of that file (assumed to be plain text) in the calculation of features according to other flag specifications.</td><td></td></tr>
	/// <tr><td> wordShape</td><td>String</td><td>none</td><td>Either "none" for no wordShape use, or the name of a word shape function recognized by
	/// <see cref="Edu.Stanford.Nlp.Process.WordShapeClassifier.LookupShaper(string)"/>
	/// , such as "dan1" or "chris4".  WordShape functions equivalence-class strings based on the pattern of letter, digit, and symbol characters that they contain.  The details depend on the particular function chosen.</td><td>SHAPE-<i>str</i></td></tr>
	/// <tr><td> splitWordShape</td><td>String</td><td>none</td><td>Either "none" for no wordShape or the name of a word shape function recognized by
	/// <see cref="Edu.Stanford.Nlp.Process.WordShapeClassifier.LookupShaper(string)"/>
	/// .  This is applied to each "word" found by splitWordsRegexp or splitWordsTokenizerRegexp.</td><td>SSHAPE-<i>str</i></td></tr>
	/// <tr></tr>
	/// <tr><td> featureMinimumSupport</td><td>int</td><td>0</td><td>A feature, that is, an (observed,class) pair, will only be included in the model providing it is seen a minimum of this number of times in the training data.</td></tr>
	/// <tr><td> biasedHyperplane</td><td>String</td><td>null</td><td>If non-null, a sequence of comma-separated pairs of <i>className prob</i>.  An item will only be classified to a certain class <i>className</i> if its probability of class membership exceeds the given conditional probability <i>prob</i>; otherwise it will be assigned to a different class.  If this list of classes is exhaustive, and no condition is satisfied, then the most probable class is chosen.</td></tr>
	/// <tr><td> printFeatures</td><td>String</td><td>null</td><td>Print out the features and their values for each instance to a file based on this name.</td></tr>
	/// <tr><td> printClassifier</td><td>String</td><td>null</td><td>Style in which to print the classifier. One of: HighWeight, HighMagnitude, AllWeights, WeightHistogram, WeightDistribution. See LinearClassifier class for details.</td></tr>
	/// <tr><td> printClassifierParam</td><td>int</td><td>100</td><td>A parameter to the printing style, which may give, for example the number of parameters to print (for HighWeight or HighMagnitude).</td></tr>
	/// <tr><td> justify</td><td>boolean</td><td>false</td><td>For each test data item, print justification (weights) for active features used in classification.</td></tr>
	/// <tr><td> exitAfterTrainingFeaturization</td><td>boolean</td><td>false</td><td>If true, the program exits after reading the training data (trainFile) and before building a classifier.  This is useful in conjunction with printFeatures, if one only wants to convert data to features for use with another classifier.</td></tr>
	/// <tr><td> verboseOptimization</td><td>boolean</td><td>false</td><td>If true, print much more detail about classifier optimization.</td></tr>
	/// <tr></tr>
	/// <tr><td> intern</td><td>boolean</td><td>false</td><td>If true, (String) intern all of the (final) feature names.  Recommended (this saves memory, but slows down feature generation in training).</td></tr>
	/// <tr><td> cacheNGrams</td><td>boolean</td><td>false</td><td>If true, record the NGram features that correspond to a String (under the current option settings and reuse rather than recalculating if the String is seen again.  <b>Disrecommended (speeds training but can require enormous amounts of memory).</b></td></tr>
	/// <tr></tr>
	/// <tr><td> useNB</td><td>boolean</td><td>false</td><td>Use a Naive Bayes generative classifier (over all features) rather than a discriminative logistic regression classifier.  (Set
	/// <c>useClass</c>
	/// to true to get a prior term.)</td></tr>
	/// <tr><td> useBinary</td><td>boolean</td><td>false</td><td>Use the binary classifier (i.e. use LogisticClassifierFactory, rather than LinearClassifierFactory) to get classifier</td></tr>
	/// <tr><td> l1reg</td><td>double</td><td>0.0</td><td>If set to be larger than 0, uses L1 regularization</td></tr>
	/// <tr><td> useAdaptL1</td><td>boolean</td><td>false</td><td>If true, uses adaptive L1 regularization to find value of l1reg that gives the desired number of features set by limitFeatures</td></tr>
	/// <tr><td> l1regmin</td><td>double</td><td>0.0</td><td>Minimum L1 in search</td></tr>
	/// <tr><td> l1regmax</td><td>double</td><td>500.0</td><td>Maximum L1 in search</td></tr>
	/// <tr><td> featureWeightThreshold</td><td>double</td><td>0.0</td><td>Threshold of model weight at which feature is kept. "Unimportant" low weight features are discarded. (Currently only implemented for adaptL1.)</td></tr>
	/// <tr><td> limitFeaturesLabels</td><td>String</td><td>null</td><td>If set, only include features for these labels in the desired number of features</td></tr>
	/// <tr><td> limitFeatures</td><td>int</td><td>0</td><td>If set to be larger than 0, uses adaptive L1 regularization to find value of l1reg that gives the desired number of features</td></tr>
	/// <tr><td>prior</td><td>String/int</td><td>quadratic</td><td>Type of prior (regularization penalty on weights). Possible values are null, "no", "quadratic", "huber", "quartic", "cosh", or "adapt". See
	/// <see cref="LogPrior">LogPrior</see>
	/// for more information.</td></tr>
	/// <tr><td> useSum</td><td>boolean</td><td>false</td><td>Do optimization via summed conditional likelihood, rather than the product.  (This is expensive, non-standard, and somewhat unstable, but can be quite effective: see Klein and Manning 2002 EMNLP paper.)</td></tr>
	/// <tr><td> tolerance</td><td>double</td><td>1e-4</td><td>Convergence tolerance in parameter optimization</td></tr>
	/// <tr><td> sigma</td><td>double</td><td>1.0</td><td>A parameter to several of the smoothing (i.e., regularization) methods, usually giving a degree of smoothing as a standard deviation (with small positive values being stronger smoothing, and bigger values weaker smoothing). However, for Naive Bayes models it is the amount of add-sigma smoothing, so a bigger number is more smoothing.</td></tr>
	/// <tr><td> epsilon</td><td>double</td><td>0.01</td><td>Used only as a parameter in the Huber loss: this is the distance from 0 at which the loss changes from quadratic to linear</td></tr>
	/// <tr><td>useQN</td><td>boolean</td><td>true</td><td>Use Quasi-Newton optimization if true, otherwise use Conjugate Gradient optimization.  Recommended.</td></tr>
	/// <tr><td>QNsize</td><td>int</td><td>15</td><td>Number of previous iterations of Quasi-Newton to store (this increases memory use, but speeds convergence by letting the Quasi-Newton optimization more effectively approximate the second derivative).</td></tr>
	/// <tr><td>featureFormat</td><td>boolean</td><td>false</td><td>Assumes the input file isn't text strings but already featurized.  One column is treated as the class column (as defined by
	/// <c>goldAnswerColumn</c>
	/// , and all other columns are treated as features of the instance.  (If answers are not present, set
	/// <c>goldAnswerColumn</c>
	/// to a negative number.)</td></tr>
	/// <tr><td>trainFromSVMLight</td><td>boolean</td><td>false</td><td>Assumes the trainFile is in SVMLight format (see <a href="http://svmlight.joachims.org/">SVMLight web page</a> for more information)</td></tr>
	/// <tr><td>testFromSVMLight</td><td>boolean</td><td>false</td><td>Assumes the testFile is in SVMLight format</td></tr>
	/// <tr><td>printSVMLightFormatTo</td><td>String</td><td>null</td><td>If non-null, print the featurized training data to an SVMLight format file (usually used with exitAfterTrainingFeaturization). This is just an option to write out data in a particular format. After that, you're on your own using some other piece of software that reads SVMlight format files.</td></tr>
	/// <tr><td>crossValidationFolds</td><td>int</td><td>-1</td><td>If positive, the training data is divided in to this many folds and cross-validation is done on the training data (prior to testing on test data, if it is also specified)</td></tr>
	/// <tr><td>printCrossValidationDecisions</td><td>boolean</td><td>false</td><td>Whether to print the individual classification decisions in cross-validation training, if crossValidationFolds is positive.</td></tr>
	/// <tr><td>shuffleTrainingData</td><td>boolean</td><td>false</td><td>If true, the training data is shuffled prior to training and cross-validation. This is vital in cross-validation if the training data is otherwise sorted by class.</td></tr>
	/// <tr><td>shuffleSeed</td><td>long</td><td>0</td><td>If non-zero, and the training data is being shuffled, this is used as the seed for the Random. Otherwise, System.nanoTime() is used.</td></tr>
	/// <tr><td>csvInput</td><td>boolean</td><td>false</td><td>If true, reads train and test file in csv format, with support for quoted fields.</td></tr>
	/// <tr><td>inputFormat</td><td>String</td><td>null</td><td>If "header" then reads file with first line treated as header; if "comments" treats lines starting with # as comments; else treated as "plain" tsv/csv file</td></tr>
	/// <tr><td>csvOutput</td><td>String</td><td>null</td><td>If non-null, used to format the output of the classifier. This is a printf-style format specification where %0 through %9 can print columns of the input, %c prints the assigned class and %n a newline character. This option can produce Kaggle-format output files!</td></tr>
	/// </table>
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Anna Rafferty</author>
	/// <author>Angel Chang (added options for using l1reg)</author>
	public class ColumnDataClassifier
	{
		private const double DefaultValue = 1.0;

		private const string DefaultIgnoreRegexp = "\\s+";

		private readonly ColumnDataClassifier.Flags[] flags;

		private readonly ColumnDataClassifier.Flags globalFlags;

		private IClassifier<string, string> classifier;

		private ITokenizerFactory<Word> ptbFactory;

		private enum InputFormat
		{
			Plain,
			Comments,
			Header
		}

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.ColumnDataClassifier));

		// default value for setting categorical, boolean features
		// simply points to flags[0]
		// really only assigned once too (either in train or load in setProperties)
		/// <summary>
		/// Entry point for taking a String (formatted as a line of a TSV file) and
		/// translating it into a Datum of features.
		/// </summary>
		/// <remarks>
		/// Entry point for taking a String (formatted as a line of a TSV file) and
		/// translating it into a Datum of features. If real-valued features are used,
		/// this method returns an RVFDatum; otherwise, categorical features are used.
		/// </remarks>
		/// <param name="line">Line of file</param>
		/// <returns>A Datum (may be an RVFDatum; never null)</returns>
		public virtual IDatum<string, string> MakeDatumFromLine(string line)
		{
			return MakeDatumFromStrings(SplitLineToFields(line));
		}

		/// <summary>Takes a String[] of elements and translates them into a Datum of features.</summary>
		/// <remarks>
		/// Takes a String[] of elements and translates them into a Datum of features.
		/// If real-valued features are used, this method accesses makeRVFDatumFromLine
		/// and returns an RVFDatum; otherwise, categorical features are used.
		/// </remarks>
		/// <param name="strings">The elements that features are made from (the columns of a TSV/CSV file)</param>
		/// <returns>A Datum (may be an RVFDatum; never null)</returns>
		public virtual IDatum<string, string> MakeDatumFromStrings(string[] strings)
		{
			if (globalFlags.usesRealValues)
			{
				return MakeRVFDatumFromStrings(strings);
			}
			if (globalFlags.featureFormat)
			{
				ICollection<string> theFeatures = new List<string>();
				for (int i = 0; i < strings.Length; i++)
				{
					if (i != globalFlags.goldAnswerColumn)
					{
						if (globalFlags.significantColumnId)
						{
							theFeatures.Add(string.Format("%d:%s", i, strings[i]));
						}
						else
						{
							theFeatures.Add(strings[i]);
						}
					}
				}
				return new BasicDatum<string, string>(theFeatures, strings[globalFlags.goldAnswerColumn]);
			}
			else
			{
				//logger.info("Read in " + strings);
				return MakeDatum(strings);
			}
		}

		private static bool IsRealValued(ColumnDataClassifier.Flags flags)
		{
			return flags != null && (flags.isRealValued || flags.logTransform || flags.logitTransform || flags.sqrtTransform);
		}

		private RVFDatum<string, string> MakeRVFDatumFromStrings(string[] strings)
		{
			if (globalFlags.featureFormat)
			{
				ClassicCounter<string> theFeatures = new ClassicCounter<string>();
				for (int i = 0; i < strings.Length; i++)
				{
					if (i != globalFlags.goldAnswerColumn)
					{
						if (IsRealValued(flags[i]))
						{
							AddFeatureValue(strings[i], flags[i], theFeatures);
						}
						else
						{
							theFeatures.SetCount(strings[i], 1.0);
						}
					}
				}
				return new RVFDatum<string, string>(theFeatures, strings[globalFlags.goldAnswerColumn]);
			}
			else
			{
				//logger.info("Read in " + strings);
				return MakeRVFDatum(strings);
			}
		}

		/// <summary>
		/// Read a set of training examples from a file, and return the data in a
		/// featurized form.
		/// </summary>
		/// <remarks>
		/// Read a set of training examples from a file, and return the data in a
		/// featurized form. If feature selection is asked for, the returned
		/// featurized form is after feature selection has been applied.
		/// </remarks>
		/// <param name="fileName">File with supervised training examples.</param>
		/// <returns>A GeneralDataset, where the labels and features are Strings.</returns>
		public virtual GeneralDataset<string, string> ReadTrainingExamples(string fileName)
		{
			return ReadAndReturnTrainingExamples(fileName).First();
		}

		/// <summary>
		/// Read a set of training examples from a file, and return the data in a
		/// featurized form and in String form.
		/// </summary>
		/// <remarks>
		/// Read a set of training examples from a file, and return the data in a
		/// featurized form and in String form. If feature selection is asked for, the returned
		/// featurized form is after feature selection has been applied.
		/// (Note that at present we sometimes need the String form, e.g., for cross-validation, and so
		/// we always pass to readDataset that we are inTestPhase (even though we are training), so this second
		/// element is filled in.)
		/// </remarks>
		/// <param name="fileName">File with supervised training examples.</param>
		/// <returns>A Pair of a GeneralDataset, where the labels and features are Strings and a List of the input examples</returns>
		public virtual Pair<GeneralDataset<string, string>, IList<string[]>> ReadAndReturnTrainingExamples(string fileName)
		{
			if (globalFlags.printFeatures != null)
			{
				NewFeaturePrinter(globalFlags.printFeatures, "train", ColumnDataClassifier.Flags.encoding);
			}
			Pair<GeneralDataset<string, string>, IList<string[]>> dataInfo = ReadDataset(fileName, true);
			GeneralDataset<string, string> train = dataInfo.First();
			if (globalFlags.featureMinimumSupport > 1)
			{
				logger.Info("Removing Features with counts < " + globalFlags.featureMinimumSupport);
				train.ApplyFeatureCountThreshold(globalFlags.featureMinimumSupport);
			}
			train.SummaryStatistics();
			return dataInfo;
		}

		/// <summary>Read a data set from a file at test time, and return it.</summary>
		/// <param name="filename">The file to read the examples from.</param>
		/// <returns>
		/// A Pair. The first item of the pair is the featurized data set,
		/// ready for passing to the classifier.  The second item of the pair
		/// is simply each line of the file split into tab-separated columns.
		/// This is at present necessary for the built-in evaluation, which uses
		/// the gold class from here, and may also be helpful when wanting to
		/// print extra output about the classification process.
		/// </returns>
		public virtual Pair<GeneralDataset<string, string>, IList<string[]>> ReadTestExamples(string filename)
		{
			return ReadDataset(filename, true);
		}

		private static IList<string[]> MakeSVMLightLineInfos(IList<string> lines)
		{
			IList<string[]> lineInfos = new List<string[]>(lines.Count);
			foreach (string line in lines)
			{
				line = line.ReplaceFirst("#.*$", string.Empty);
				// remove any trailing comments
				// in principle, it'd be nice to save the comment, though, for possible use as a displayedColumn - make it column 1??
				lineInfos.Add(line.Split("\\s+"));
			}
			return lineInfos;
		}

		/// <summary>
		/// NB: This is meant to do splitting strictly only on tabs, and to thus
		/// work with things that are exactly TSV files.
		/// </summary>
		/// <remarks>
		/// NB: This is meant to do splitting strictly only on tabs, and to thus
		/// work with things that are exactly TSV files.  It shouldn't split on
		/// all whitespace, because it is useful to be able to have spaces inside
		/// fields for short text documents, and then to be able to split them into
		/// words with features like useSplitWords.
		/// </remarks>
		private static readonly Pattern tab = Pattern.Compile("\\t");

		/// <summary>Read a data set from a file and convert it into a Dataset object.</summary>
		/// <remarks>
		/// Read a data set from a file and convert it into a Dataset object.
		/// In test phase, returns the
		/// <c>List&lt;String[]&gt;</c>
		/// with the data columns for printing purposes.
		/// Otherwise, returns
		/// <see langword="null"/>
		/// for the second item.
		/// </remarks>
		/// <param name="filename">Where to read data from</param>
		/// <param name="inTestPhase">Whether to return the read String[] for each data item</param>
		/// <returns>A Pair of a GeneralDataSet of Datums and a List of datums in String form.</returns>
		private Pair<GeneralDataset<string, string>, IList<string[]>> ReadDataset(string filename, bool inTestPhase)
		{
			Timing tim = new Timing();
			GeneralDataset<string, string> dataset;
			IList<string[]> lineInfos = null;
			if ((inTestPhase && ColumnDataClassifier.Flags.testFromSVMLight) || (!inTestPhase && ColumnDataClassifier.Flags.trainFromSVMLight))
			{
				IList<string> lines = null;
				if (inTestPhase)
				{
					lines = new List<string>();
				}
				if (globalFlags.usesRealValues)
				{
					dataset = RVFDataset.ReadSVMLightFormat(filename, lines);
				}
				else
				{
					dataset = Dataset.ReadSVMLightFormat(filename, lines);
				}
				if (lines != null)
				{
					lineInfos = MakeSVMLightLineInfos(lines);
				}
			}
			else
			{
				try
				{
					if (inTestPhase)
					{
						lineInfos = new List<string[]>();
					}
					if (globalFlags.usesRealValues)
					{
						dataset = new RVFDataset<string, string>();
					}
					else
					{
						dataset = new Dataset<string, string>();
					}
					int lineNo = 0;
					int minColumns = int.MaxValue;
					int maxColumns = 0;
					foreach (string line in ObjectBank.GetLineIterator(new File(filename), ColumnDataClassifier.Flags.encoding))
					{
						lineNo++;
						if (ColumnDataClassifier.Flags.inputFormat == ColumnDataClassifier.InputFormat.Header)
						{
							if (lineNo == 1)
							{
								if (storedHeader == null)
								{
									storedHeader = line;
								}
								// store it because need elements of it to print header in output
								continue;
							}
						}
						else
						{
							if (ColumnDataClassifier.Flags.inputFormat == ColumnDataClassifier.InputFormat.Comments)
							{
								if (line.Matches("\\s#.*"))
								{
									continue;
								}
							}
						}
						string[] strings = SplitLineToFields(line);
						if (strings.Length < 2)
						{
							throw new Exception("Line format error at line " + lineNo + ": " + line);
						}
						if (strings.Length < minColumns)
						{
							minColumns = strings.Length;
						}
						if (strings.Length > maxColumns)
						{
							maxColumns = strings.Length;
						}
						if (inTestPhase)
						{
							lineInfos.Add(strings);
						}
						if (strings.Length < flags.Length)
						{
							throw new Exception("Error: Line has too few tab-separated columns (" + maxColumns + ") for " + flags.Length + " columns required by specified properties: " + line);
						}
						dataset.Add(MakeDatumFromStrings(strings));
					}
					if (lineNo > 0 && minColumns != maxColumns)
					{
						logger.Info("WARNING: Number of tab-separated columns in " + filename + " varies between " + minColumns + " and " + maxColumns);
					}
				}
				catch (Exception e)
				{
					throw new Exception("Dataset could not be loaded", e);
				}
			}
			logger.Info("Reading dataset from " + filename + " ... done [" + tim.ToSecondsString() + "s, " + dataset.Size() + " items].");
			return new Pair<GeneralDataset<string, string>, IList<string[]>>(dataset, lineInfos);
		}

		/// <summary>Split according to whether we are using tsv file (default) or csv files.</summary>
		private string[] SplitLineToFields(string line)
		{
			if (globalFlags.csvInput)
			{
				string[] strings = StringUtils.SplitOnCharWithQuoting(line, ',', '"', '"');
				for (int i = 0; i < strings.Length; ++i)
				{
					if (strings[i].StartsWith("\"") && strings[i].EndsWith("\""))
					{
						strings[i] = Sharpen.Runtime.Substring(strings[i], 1, strings[i].Length - 1);
					}
				}
				return strings;
			}
			else
			{
				return tab.Split(line);
			}
		}

		/// <summary>Write summary statistics about a group of answers.</summary>
		private Pair<double, double> WriteResultsSummary(int num, ICounter<string> contingency, ICollection<string> labels)
		{
			logger.Info(string.Empty);
			string message = string.Empty;
			message += num + " examples";
			if (globalFlags.groupingColumn >= 0 && globalFlags.rankingAccuracyClass != null)
			{
				message += " and " + numGroups + " ranking groups";
			}
			logger.Info(message + " in test set");
			int numClasses = 0;
			double microAccuracy = 0.0;
			double macroF1 = 0.0;
			foreach (string key in labels)
			{
				numClasses++;
				int tp = (int)contingency.GetCount(key + "|TP");
				int fn = (int)contingency.GetCount(key + "|FN");
				int fp = (int)contingency.GetCount(key + "|FP");
				int tn = (int)contingency.GetCount(key + "|TN");
				double p = (tp + fp == 0) ? 1.0 : ((double)tp) / (tp + fp);
				// If nothing selected, then vacuous 1.0
				double r = (tp + fn == 0) ? 1.0 : ((double)tp) / (tp + fn);
				// If nothing to find, then vacuous 1.0
				double f = (p == 0.0 && r == 0.0) ? 0.0 : 2 * p * r / (p + r);
				double acc = ((double)tp + tn) / num;
				macroF1 += f;
				microAccuracy += tp;
				logger.Info("Cls " + key + ": TP=" + tp + " FN=" + fn + " FP=" + fp + " TN=" + tn + "; Acc " + nf.Format(acc) + " P " + nf.Format(p) + " R " + nf.Format(r) + " F1 " + nf.Format(f));
			}
			if (globalFlags.groupingColumn >= 0 && globalFlags.rankingAccuracyClass != null)
			{
				double cor = (int)contingency.GetCount("Ranking|Correct");
				double err = (int)contingency.GetCount("Ranking|Error");
				double rankacc = (cor + err == 0) ? 0 : cor / (cor + err);
				logger.Info("Ranking accuracy: " + nf.Format(rankacc));
				double cov = (int)contingency.GetCount("Ranking|Covered");
				double coverr = (int)contingency.GetCount("Ranking|Uncovered");
				double covacc = (cov + coverr == 0) ? 0 : cov / (cov + coverr);
				if (coverr > 0.5)
				{
					double ce = (int)(contingency.GetCount("Ranking|Error") - contingency.GetCount("Ranking|Uncovered"));
					double crankacc = (cor + ce == 0) ? 0 : cor / (cor + ce);
					logger.Info(" (on " + nf.Format(covacc) + " of groups with correct answer: " + nf.Format(crankacc) + ')');
				}
				else
				{
					logger.Info(string.Empty);
				}
				if (globalFlags.rankingScoreColumn >= 0)
				{
					double totalSim = contingency.GetCount("Ranking|Score");
					double ranksim = (cor + err == 0) ? 0 : totalSim / (cor + err);
					logger.Info("Ranking average score: " + nf.Format(ranksim));
				}
			}
			microAccuracy = microAccuracy / num;
			macroF1 = macroF1 / numClasses;
			NumberFormat nf2 = new DecimalFormat("0.00000");
			logger.Info("Accuracy/micro-averaged F1: " + nf2.Format(microAccuracy));
			logger.Info("Macro-averaged F1: " + nf2.Format(macroF1));
			return new Pair<double, double>(microAccuracy, macroF1);
		}

		private static int numGroups = 0;

		private static string lastGroup = string.Empty;

		private static int numInGroup = 0;

		private static double bestProb = 0.0;

		private static double bestSim = 0.0;

		private static bool currentHighestProbCorrect = false;

		private static bool foundAnswerInGroup = false;

		private static string storedHeader;

		private static readonly NumberFormat nf = new DecimalFormat("0.000");

		// These variables are only used by the private methods used by main() for displaying
		// performance statistics when running the command-line version. So their being
		// static does no harm.
		/// <summary>Write out an answer, and update statistics.</summary>
		private void WriteAnswer(string[] strs, string clAnswer, Distribution<string> cntr)
		{
			string goldAnswer = globalFlags.goldAnswerColumn < strs.Length ? strs[globalFlags.goldAnswerColumn] : string.Empty;
			string printedText = string.Empty;
			if (globalFlags.displayedColumn >= 0)
			{
				printedText = strs[globalFlags.displayedColumn];
			}
			string results;
			if (globalFlags.displayAllAnswers)
			{
				// sort the labels by probability
				TreeSet<Pair<double, string>> sortedLabels = new TreeSet<Pair<double, string>>();
				foreach (string key in cntr.KeySet())
				{
					sortedLabels.Add(new Pair<double, string>(cntr.ProbabilityOf(key), key));
				}
				StringBuilder builder = new StringBuilder();
				foreach (Pair<double, string> pair in sortedLabels.DescendingSet())
				{
					if (builder.Length > 0)
					{
						builder.Append('\t');
					}
					builder.Append(pair.First()).Append('\t').Append(pair.Second());
				}
				results = builder.ToString();
			}
			else
			{
				results = clAnswer + '\t' + nf.Format(cntr.ProbabilityOf(clAnswer)) + '\t' + nf.Format(cntr.ProbabilityOf(goldAnswer));
			}
			string line;
			if (printedText.IsEmpty())
			{
				line = goldAnswer + '\t' + results;
			}
			else
			{
				line = printedText + '\t' + goldAnswer + '\t' + results;
			}
			System.Console.Out.WriteLine(line);
		}

		private void UpdatePerformanceStatistics(string[] strs, string clAnswer, Distribution<string> cntr, ICounter<string> contingency, IClassifier<string, string> c, double sim)
		{
			string goldAnswer = globalFlags.goldAnswerColumn < strs.Length ? strs[globalFlags.goldAnswerColumn] : string.Empty;
			// NB: This next bit judges correctness by surface String equality, not our internal indices, so strs has to be right even for svmlightFormat
			foreach (string next in c.Labels())
			{
				if (next.Equals(goldAnswer))
				{
					if (next.Equals(clAnswer))
					{
						contingency.IncrementCount(next + "|TP");
					}
					else
					{
						contingency.IncrementCount(next + "|FN");
					}
				}
				else
				{
					if (next.Equals(clAnswer))
					{
						contingency.IncrementCount(next + "|FP");
					}
					else
					{
						contingency.IncrementCount(next + "|TN");
					}
				}
			}
			if (globalFlags.groupingColumn >= 0 && globalFlags.rankingAccuracyClass != null)
			{
				string group = strs[globalFlags.groupingColumn];
				// logger.info("Group is " + group);
				if (group.Equals(lastGroup))
				{
					numInGroup++;
					double prob = cntr.ProbabilityOf(globalFlags.rankingAccuracyClass);
					// logger.info("  same group; prob is " + prob);
					if (prob > bestProb)
					{
						bestProb = prob;
						bestSim = sim;
						// logger.info("  better prob than before");
						currentHighestProbCorrect = goldAnswer.Equals(globalFlags.rankingAccuracyClass);
					}
					if (globalFlags.rankingAccuracyClass.Equals(goldAnswer))
					{
						foundAnswerInGroup = true;
					}
				}
				else
				{
					FinishRanking(contingency, bestSim);
					numGroups++;
					lastGroup = group;
					bestProb = cntr.ProbabilityOf(globalFlags.rankingAccuracyClass);
					bestSim = sim;
					// logger.info("  different; prob is " + bestProb);
					numInGroup = 1;
					currentHighestProbCorrect = goldAnswer.Equals(globalFlags.rankingAccuracyClass);
					foundAnswerInGroup = globalFlags.rankingAccuracyClass.Equals(goldAnswer);
				}
			}
		}

		private void FinishRanking(ICounter<string> contingency, double sim)
		{
			if (numInGroup > 0)
			{
				if (globalFlags.justify)
				{
					string message = "Previous group of " + numInGroup + ": ";
					if (!foundAnswerInGroup)
					{
						message += "no correct answer; ";
					}
					message += "highest ranked guess was: " + ((currentHighestProbCorrect ? "correct" : "incorrect"));
					logger.Info(message);
					logger.Info(" (sim. = " + nf.Format(sim) + ')');
				}
				if (currentHighestProbCorrect)
				{
					contingency.IncrementCount("Ranking|Correct");
				}
				else
				{
					contingency.IncrementCount("Ranking|Error");
				}
				if (foundAnswerInGroup)
				{
					contingency.IncrementCount("Ranking|Covered");
				}
				else
				{
					contingency.IncrementCount("Ranking|Uncovered");
				}
				contingency.IncrementCount("Ranking|Score", sim);
			}
		}

		/// <summary>Test and evaluate classifier on examples with their String representation and gold classification available.</summary>
		/// <param name="cl">The classifier to test</param>
		/// <param name="test">The dataset to test on</param>
		/// <param name="lineInfos">Duplicate List of the items to be classified, each an array of Strings (like a line of a TSV file)</param>
		/// <returns>A Pair consisting of the accuracy (micro-averaged F1) and macro-averaged F1 for the dataset</returns>
		private Pair<double, double> TestExamples(IClassifier<string, string> cl, GeneralDataset<string, string> test, IList<string[]> lineInfos)
		{
			// usually suppress item level printing in crossvalidation
			if (!(globalFlags.crossValidationFolds > 0 && !globalFlags.printCrossValidationDecisions))
			{
				string message = string.Empty;
				if (globalFlags.csvOutput != null)
				{
					message += FormatCsv(globalFlags.csvOutput, storedHeader.Split("\t"), null);
				}
				else
				{
					message += "Output format: ";
					if (globalFlags.displayedColumn >= 0)
					{
						message += "dataColumn" + globalFlags.displayedColumn + '\t';
					}
					message += "goldAnswer\t";
					if (globalFlags.displayAllAnswers)
					{
						logger.Info(message + "[P(class) class]+ {sorted by probability}");
					}
					else
					{
						logger.Info(message + "classifierAnswer\tP(clAnswer)\tP(goldAnswer)");
					}
				}
			}
			ICounter<string> contingency = new ClassicCounter<string>();
			// store tp,fp,fn,tn
			for (int i = 0; i < sz; i++)
			{
				TestExample(cl, test, lineInfos, contingency, i);
			}
			if (globalFlags.groupingColumn >= 0 && globalFlags.rankingAccuracyClass != null)
			{
				FinishRanking(contingency, bestSim);
			}
			return WriteResultsSummary(test.Size(), contingency, cl.Labels());
		}

		private void TestExample(IClassifier<string, string> cl, GeneralDataset<string, string> test, IList<string[]> lineInfos, ICounter<string> contingency, int i)
		{
			string[] example = lineInfos[i];
			IDatum<string, string> d;
			if (globalFlags.usesRealValues)
			{
				d = test.GetRVFDatum(i);
			}
			else
			{
				d = test.GetDatum(i);
			}
			if (globalFlags.justify)
			{
				logger.Info("### Test item " + i);
				logger.Info(StringUtils.Join(example, "\t"));
				if (cl is LinearClassifier)
				{
					((LinearClassifier<string, string>)cl).JustificationOf(d);
				}
				logger.Info();
			}
			ICounter<string> logScores;
			if (globalFlags.usesRealValues)
			{
				logScores = ErasureUtils.UncheckedCast<IRVFClassifier<string, string>>(cl).ScoresOf((RVFDatum<string, string>)d);
			}
			else
			{
				logScores = cl.ScoresOf(d);
			}
			Distribution<string> dist = Distribution.DistributionFromLogisticCounter(logScores);
			string answer = null;
			if (globalFlags.biasedHyperplane != null)
			{
				// logger.info("Biased using counter: " +
				//         globalFlags.biasedHyperplane);
				IList<string> biggestKeys = new List<string>(logScores.KeySet());
				biggestKeys.Sort(Counters.ToComparatorDescending(logScores));
				foreach (string key in biggestKeys)
				{
					double prob = dist.ProbabilityOf(key);
					double threshold = globalFlags.biasedHyperplane.GetCount(key);
					// logger.info("  Trying " + key + " prob is " + prob +
					//           " threshold is " + threshold);
					if (prob > threshold)
					{
						answer = key;
						break;
					}
				}
			}
			if (answer == null)
			{
				if (globalFlags.usesRealValues)
				{
					answer = ErasureUtils.UncheckedCast<IRVFClassifier<string, string>>(cl).ClassOf((RVFDatum<string, string>)d);
				}
				else
				{
					answer = cl.ClassOf(d);
				}
			}
			double sim = 0.0;
			if (globalFlags.rankingScoreColumn >= 0)
			{
				try
				{
					sim = double.ParseDouble(example[globalFlags.rankingScoreColumn]);
				}
				catch (NumberFormatException)
				{
				}
			}
			// just don't print it
			if (!(globalFlags.crossValidationFolds > 0 && !globalFlags.printCrossValidationDecisions))
			{
				if (globalFlags.csvOutput != null)
				{
					System.Console.Out.Write(FormatCsv(globalFlags.csvOutput, example, answer));
				}
				else
				{
					WriteAnswer(example, answer, dist);
				}
			}
			UpdatePerformanceStatistics(example, answer, dist, contingency, cl, sim);
		}

		private string FormatCsv(string format, string[] fields, string answer)
		{
			StringBuilder @out = new StringBuilder();
			for (int i = 0; i < len; i++)
			{
				char ch = format[i];
				if (ch == '%' && i + 1 < len)
				{
					char ch2 = format[i + 1];
					if (ch2 >= '0' && ch2 <= '9')
					{
						int field = ch2 - '0';
						if (field < fields.Length)
						{
							@out.Append(fields[field]);
						}
						else
						{
							throw new ArgumentException("Not enough columns for format " + format);
						}
					}
					else
					{
						if (ch2 == 'c')
						{
							if (answer != null)
							{
								@out.Append(answer);
							}
							else
							{
								if (globalFlags.goldAnswerColumn < fields.Length)
								{
									@out.Append(fields[globalFlags.goldAnswerColumn]);
								}
								else
								{
									@out.Append("Class");
								}
							}
						}
						else
						{
							if (ch2 == 'n')
							{
								@out.Append('\n');
							}
							else
							{
								throw new ArgumentException("Unrecognized format specification in " + format);
							}
						}
					}
					i++;
				}
				else
				{
					// have also dealt with next character giving format
					@out.Append(ch);
				}
			}
			return @out.ToString();
		}

		/// <summary>Extracts all the features from a certain input datum.</summary>
		/// <param name="strs">The data String[] to extract features from</param>
		/// <returns>The constructed Datum</returns>
		private IDatum<string, string> MakeDatum(string[] strs)
		{
			string goldAnswer = globalFlags.goldAnswerColumn < strs.Length ? strs[globalFlags.goldAnswerColumn] : string.Empty;
			IList<string> theFeatures = new List<string>();
			ICollection<string> globalFeatures = Generics.NewHashSet();
			if (globalFlags.useClassFeature)
			{
				globalFeatures.Add("CLASS");
			}
			AddAllInterningAndPrefixing(theFeatures, globalFeatures, string.Empty);
			for (int i = 0; i < flags.Length; i++)
			{
				ICollection<string> featuresC = Generics.NewHashSet();
				//important that this is a hash set to prevent same feature from being added multiple times
				MakeDatum(strs[i], flags[i], featuresC, goldAnswer);
				AddAllInterningAndPrefixing(theFeatures, featuresC, i + "-");
			}
			if (globalFlags.printFeatures != null)
			{
				PrintFeatures(strs, theFeatures);
			}
			//System.out.println("Features are: " + theFeatures);
			return new BasicDatum<string, string>(theFeatures, goldAnswer);
		}

		/// <summary>
		/// Extracts all the features from a certain input array and makes
		/// a real valued feature datum; those features that are not real valued
		/// are given value 1.0.
		/// </summary>
		/// <param name="strs">The data String[] to extract features from</param>
		/// <returns>The constructed RVFDatum</returns>
		private RVFDatum<string, string> MakeRVFDatum(string[] strs)
		{
			string goldAnswer = globalFlags.goldAnswerColumn < strs.Length ? strs[globalFlags.goldAnswerColumn] : string.Empty;
			ClassicCounter<string> theFeatures = new ClassicCounter<string>();
			ClassicCounter<string> globalFeatures = new ClassicCounter<string>();
			if (globalFlags.useClassFeature)
			{
				globalFeatures.SetCount("CLASS", 1.0);
			}
			AddAllInterningAndPrefixingRVF(theFeatures, globalFeatures, string.Empty);
			for (int i = 0; i < flags.Length; i++)
			{
				ClassicCounter<string> featuresC = new ClassicCounter<string>();
				MakeDatum(strs[i], flags[i], featuresC, goldAnswer);
				AddAllInterningAndPrefixingRVF(theFeatures, featuresC, i + "-");
			}
			if (globalFlags.printFeatures != null)
			{
				PrintFeatures(strs, theFeatures);
			}
			//System.out.println("Features are: " + theFeatures);
			return new RVFDatum<string, string>(theFeatures, goldAnswer);
		}

		private void AddAllInterningAndPrefixingRVF(ClassicCounter<string> accumulator, ClassicCounter<string> addend, string prefix)
		{
			System.Diagnostics.Debug.Assert(prefix != null);
			foreach (string protoFeat in addend.KeySet())
			{
				double count = addend.GetCount(protoFeat);
				if (!prefix.IsEmpty())
				{
					protoFeat = prefix + protoFeat;
				}
				if (globalFlags.intern)
				{
					protoFeat = string.Intern(protoFeat);
				}
				accumulator.IncrementCount(protoFeat, count);
			}
		}

		private void AddAllInterningAndPrefixing(ICollection<string> accumulator, ICollection<string> addend, string prefix)
		{
			System.Diagnostics.Debug.Assert(prefix != null);
			foreach (string protoFeat in addend)
			{
				if (!prefix.IsEmpty())
				{
					protoFeat = prefix + protoFeat;
				}
				if (globalFlags.intern)
				{
					protoFeat = string.Intern(protoFeat);
				}
				accumulator.Add(protoFeat);
			}
		}

		/// <summary>
		/// This method takes care of adding features to the collection-ish object features when
		/// the value of the feature must be parsed as a real number, including performing
		/// math transforms.
		/// </summary>
		private static void AddFeatureValue(string cWord, ColumnDataClassifier.Flags flags, object featuresC)
		{
			double value = double.ValueOf(cWord);
			if (flags.logTransform)
			{
				double log = Math.Log(value);
				if (double.IsInfinite(log) || double.IsNaN(log))
				{
					logger.Info("WARNING: Log transform attempted on out of range value; feature ignored");
				}
				else
				{
					AddFeature(featuresC, "Log", log);
				}
			}
			else
			{
				if (flags.logitTransform)
				{
					double logit = Math.Log(value / (1 - value));
					if (double.IsInfinite(logit) || double.IsNaN(logit))
					{
						logger.Info("WARNING: Logit transform attempted on out of range value; feature ignored");
					}
					else
					{
						AddFeature(featuresC, "Logit", logit);
					}
				}
				else
				{
					if (flags.sqrtTransform)
					{
						double sqrt = Math.Sqrt(value);
						AddFeature(featuresC, "Sqrt", sqrt);
					}
					else
					{
						AddFeature(featuresC, ColumnDataClassifier.Flags.realValuedFeaturePrefix, value);
					}
				}
			}
		}

		/// <summary>
		/// This method takes care of adding features to the collection-ish object features via
		/// instanceof checks.
		/// </summary>
		/// <remarks>
		/// This method takes care of adding features to the collection-ish object features via
		/// instanceof checks.  Features must be a type of collection or a counter, and value is used
		/// iff it is a counter
		/// </remarks>
		private static void AddFeature<F>(object features, F newFeature, double value)
		{
			if (features is ICounter<object>)
			{
				ErasureUtils.UncheckedCast<ICounter<F>>(features).SetCount(newFeature, value);
			}
			else
			{
				if (features is ICollection<object>)
				{
					ErasureUtils.UncheckedCast<ICollection<F>>(features).Add(newFeature);
				}
				else
				{
					throw new Exception("addFeature was called with a features object that is neither a counter nor a collection!");
				}
			}
		}

		/// <summary>Extracts all the features from a certain input column.</summary>
		/// <param name="cWord">The String to extract data from</param>
		/// <param name="flags">Flags specifying which features to extract</param>
		/// <param name="featuresC">Some kind of Collection or Counter to put features into</param>
		/// <param name="goldAns">
		/// The goldAnswer for this whole datum or emptyString if none.
		/// This is used only for filling in the binned lengths histogram counters
		/// </param>
		private void MakeDatum(string cWord, ColumnDataClassifier.Flags flags, object featuresC, string goldAns)
		{
			//logger.info("Making features for " + cWord + " flags " + flags);
			if (flags == null)
			{
				// no features for this column
				return;
			}
			if (flags.filename)
			{
				cWord = IOUtils.SlurpFileNoExceptions(cWord);
			}
			if (flags.lowercase)
			{
				cWord = cWord.ToLower(Locale.English);
			}
			if (flags.useString)
			{
				AddFeature(featuresC, "S-" + cWord, DefaultValue);
			}
			if (flags.binnedLengths != null)
			{
				int len = cWord.Length;
				string featureName = null;
				for (int i = 0; i <= flags.binnedLengths.Length; i++)
				{
					if (i == flags.binnedLengths.Length || len <= flags.binnedLengths[i])
					{
						featureName = "Len-" + ((i == 0) ? 0 : (flags.binnedLengths[i - 1] + 1)) + '-' + ((i == flags.binnedLengths.Length) ? "Inf" : int.ToString(flags.binnedLengths[i]));
						if (flags.binnedLengthsCounter != null)
						{
							flags.binnedLengthsCounter.IncrementCount(featureName, goldAns);
						}
						break;
					}
				}
				AddFeature(featuresC, featureName, DefaultValue);
			}
			if (flags.binnedValues != null)
			{
				double val = flags.binnedValuesNaN;
				try
				{
					val = double.ParseDouble(cWord);
				}
				catch (NumberFormatException)
				{
				}
				// do nothing -- keeps value of flags.binnedValuesNaN
				string featureName = null;
				for (int i = 0; i <= flags.binnedValues.Length; i++)
				{
					if (i == flags.binnedValues.Length || val <= flags.binnedValues[i])
					{
						featureName = "Val-(" + ((i == 0) ? "-Inf" : double.ToString(flags.binnedValues[i - 1])) + ',' + ((i == flags.binnedValues.Length) ? "Inf" : double.ToString(flags.binnedValues[i])) + ']';
						if (flags.binnedValuesCounter != null)
						{
							flags.binnedValuesCounter.IncrementCount(featureName, goldAns);
						}
						break;
					}
				}
				AddFeature(featuresC, featureName, DefaultValue);
			}
			if (flags.countChars != null)
			{
				int[] cnts = new int[flags.countChars.Length];
				for (int i = 0; i < cnts.Length; i++)
				{
					cnts[i] = 0;
				}
				for (int i_1 = 0; i_1 < len; i_1++)
				{
					char ch = cWord[i_1];
					for (int j = 0; j < cnts.Length; j++)
					{
						if (ch == flags.countChars[j])
						{
							cnts[j]++;
						}
					}
				}
				for (int j_1 = 0; j_1 < cnts.Length; j_1++)
				{
					string featureName = null;
					for (int i_2 = 0; i_2 <= flags.countCharsBins.Length; i_2++)
					{
						if (i_2 == flags.countCharsBins.Length || cnts[j_1] <= flags.countCharsBins[i_2])
						{
							featureName = "Char-" + flags.countChars[j_1] + '-' + ((i_2 == 0) ? 0 : (flags.countCharsBins[i_2 - 1] + 1)) + '-' + ((i_2 == flags.countCharsBins.Length) ? "Inf" : int.ToString(flags.countCharsBins[i_2]));
							break;
						}
					}
					AddFeature(featuresC, featureName, DefaultValue);
				}
			}
			if (flags.splitWordsPattern != null || flags.splitWordsTokenizerPattern != null || flags.splitWordsWithPTBTokenizer)
			{
				string[] bits;
				if (flags.splitWordsTokenizerPattern != null)
				{
					bits = RegexpTokenize(flags.splitWordsTokenizerPattern, flags.splitWordsIgnorePattern, cWord);
				}
				else
				{
					if (flags.splitWordsPattern != null)
					{
						bits = SplitTokenize(flags.splitWordsPattern, flags.splitWordsIgnorePattern, cWord);
					}
					else
					{
						//PTB tokenizer
						bits = PtbTokenize(cWord);
					}
				}
				if (flags.showTokenization)
				{
					logger.Info("Tokenization: " + Arrays.ToString(bits));
				}
				if (flags.splitWordCount)
				{
					AddFeature(featuresC, "SWNUM", bits.Length);
				}
				if (flags.logSplitWordCount)
				{
					AddFeature(featuresC, "LSWNUM", Math.Log(bits.Length));
				}
				if (flags.binnedSplitWordCounts != null)
				{
					string featureName = null;
					for (int i = 0; i <= flags.binnedSplitWordCounts.Length; i++)
					{
						if (i == flags.binnedSplitWordCounts.Length || bits.Length <= flags.binnedSplitWordCounts[i])
						{
							featureName = "SWNUMBIN-" + ((i == 0) ? 0 : (flags.binnedSplitWordCounts[i - 1] + 1)) + '-' + ((i == flags.binnedSplitWordCounts.Length) ? "Inf" : int.ToString(flags.binnedSplitWordCounts[i]));
							break;
						}
					}
					AddFeature(featuresC, featureName, DefaultValue);
				}
				// add features over splitWords
				for (int i_1 = 0; i_1 < bits.Length; i_1++)
				{
					if (flags.useSplitWords)
					{
						AddFeature(featuresC, "SW-" + bits[i_1], DefaultValue);
					}
					if (flags.useLowercaseSplitWords)
					{
						AddFeature(featuresC, "LSW-" + bits[i_1].ToLower(), DefaultValue);
					}
					if (flags.useSplitWordPairs)
					{
						if (i_1 + 1 < bits.Length)
						{
							AddFeature(featuresC, "SWP-" + bits[i_1] + '-' + bits[i_1 + 1], DefaultValue);
						}
					}
					if (flags.useLowercaseSplitWordPairs)
					{
						if (i_1 + 1 < bits.Length)
						{
							AddFeature(featuresC, "LSWP-" + bits[i_1].ToLower() + '-' + bits[i_1 + 1].ToLower(), DefaultValue);
						}
					}
					if (flags.useAllSplitWordPairs)
					{
						for (int j = i_1 + 1; j < bits.Length; j++)
						{
							// sort lexicographically
							if (string.CompareOrdinal(bits[i_1], bits[j]) < 0)
							{
								AddFeature(featuresC, "ASWP-" + bits[i_1] + '-' + bits[j], DefaultValue);
							}
							else
							{
								AddFeature(featuresC, "ASWP-" + bits[j] + '-' + bits[i_1], DefaultValue);
							}
						}
					}
					if (flags.useAllSplitWordTriples)
					{
						for (int j = i_1 + 1; j < bits.Length; j++)
						{
							for (int k = j + 1; k < bits.Length; k++)
							{
								// sort lexicographically
								string[] triple = new string[3];
								triple[0] = bits[i_1];
								triple[1] = bits[j];
								triple[2] = bits[k];
								Arrays.Sort(triple);
								AddFeature(featuresC, "ASWT-" + triple[0] + '-' + triple[1] + '-' + triple[2], DefaultValue);
							}
						}
					}
					if (flags.useSplitWordNGrams)
					{
						StringBuilder sb = new StringBuilder("SW#");
						for (int j = i_1; j < i_1 + flags.minWordNGramLeng - 1 && j < bits.Length; j++)
						{
							sb.Append('-');
							sb.Append(bits[j]);
						}
						int maxIndex = (flags.maxWordNGramLeng > 0) ? Math.Min(bits.Length, i_1 + flags.maxWordNGramLeng) : bits.Length;
						for (int j_1 = i_1 + flags.minWordNGramLeng - 1; j_1 < maxIndex; j_1++)
						{
							if (flags.wordNGramBoundaryRegexp != null)
							{
								if (flags.wordNGramBoundaryPattern.Matcher(bits[j_1]).Matches())
								{
									break;
								}
							}
							sb.Append('-');
							sb.Append(bits[j_1]);
							AddFeature(featuresC, sb.ToString(), DefaultValue);
						}
					}
					// this is equivalent to having boundary tokens in splitWordPairs -- they get a special feature
					if (flags.useSplitFirstLastWords)
					{
						if (i_1 == 0)
						{
							AddFeature(featuresC, "SFW-" + bits[i_1], DefaultValue);
						}
						else
						{
							if (i_1 == bits.Length - 1)
							{
								AddFeature(featuresC, "SLW-" + bits[i_1], DefaultValue);
							}
						}
					}
					if (flags.useLowercaseSplitFirstLastWords)
					{
						if (i_1 == 0)
						{
							AddFeature(featuresC, "LSFW-" + bits[i_1].ToLower(), DefaultValue);
						}
						else
						{
							if (i_1 == bits.Length - 1)
							{
								AddFeature(featuresC, "SLW-" + bits[i_1].ToLower(), DefaultValue);
							}
						}
					}
					if (flags.useSplitNGrams || flags.useSplitPrefixSuffixNGrams)
					{
						ICollection<string> featureNames = MakeNGramFeatures(bits[i_1], flags, true, "S#");
						foreach (string featureName in featureNames)
						{
							AddFeature(featuresC, featureName, DefaultValue);
						}
					}
					if (flags.splitWordShape > WordShapeClassifier.Nowordshape)
					{
						string shape = WordShapeClassifier.WordShape(bits[i_1], flags.splitWordShape);
						// logger.info("Shaper is " + flags.splitWordShape + " word len " + bits[i].length() + " shape is " + shape);
						AddFeature(featuresC, "SSHAPE-" + shape, DefaultValue);
					}
				}
				// for bits
				if (flags.wordVectors != null)
				{
					double[] averages = null;
					foreach (string bit in bits)
					{
						float[] wv = flags.wordVectors[bit];
						if (wv != null)
						{
							if (averages == null)
							{
								averages = new double[wv.Length];
								for (int j = 0; j < wv.Length; j++)
								{
									averages[j] += wv[j];
								}
							}
						}
					}
					if (averages != null)
					{
						for (int j = 0; j < averages.Length; j++)
						{
							averages[j] /= bits.Length;
							AddFeature(featuresC, "SWV-" + j, averages[j]);
						}
					}
				}
			}
			// } else {
			//   logger.info("No word vectors found for words in |" + cWord + '|');
			// end if wordVectors
			// end if uses some split words features
			if (flags.wordShape > WordShapeClassifier.Nowordshape)
			{
				string shape = WordShapeClassifier.WordShape(cWord, flags.wordShape);
				AddFeature(featuresC, "SHAPE-" + shape, DefaultValue);
			}
			if (flags.useNGrams || flags.usePrefixSuffixNGrams)
			{
				ICollection<string> featureNames = MakeNGramFeatures(cWord, flags, false, "#");
				foreach (string featureName in featureNames)
				{
					AddFeature(featuresC, featureName, DefaultValue);
				}
			}
			if (IsRealValued(flags))
			{
				AddFeatureValue(cWord, flags, featuresC);
			}
		}

		//logger.info("Made featuresC " + featuresC);
		//end makeDatum
		/// <summary>Return the tokens using PTB tokenizer.</summary>
		/// <param name="str">String to tokenize</param>
		/// <returns>List of tokens</returns>
		private string[] PtbTokenize(string str)
		{
			// todo [cdm 2017]: Someday should generalize this to allow use of other tokenizers
			if (ptbFactory == null)
			{
				ptbFactory = PTBTokenizer.Factory();
			}
			ITokenizer<Word> tokenizer = ptbFactory.GetTokenizer(new StringReader(str));
			IList<Word> words = tokenizer.Tokenize();
			string[] res = new string[words.Count];
			for (int i = 0; i < sz; i++)
			{
				res[i] = words[i].Word();
			}
			return res;
		}

		/// <summary>Caches a hash of word to all substring features.</summary>
		/// <remarks>
		/// Caches a hash of word to all substring features.  Uses a <i>lot</i> of memory!
		/// If the String space is large, you shouldn't turn this on.
		/// </remarks>
		private static readonly IDictionary<string, ICollection<string>> wordToSubstrings = new ConcurrentHashMap<string, ICollection<string>>();

		private string Intern(string s)
		{
			if (globalFlags.intern)
			{
				return string.Intern(s);
			}
			return s;
		}

		/// <summary>Return a Collection of NGrams from the input String.</summary>
		private ICollection<string> MakeNGramFeatures(string input, ColumnDataClassifier.Flags flags, bool useSplit, string featPrefix)
		{
			string toNGrams = input;
			bool internalNGrams;
			bool prefixSuffixNGrams;
			if (useSplit)
			{
				internalNGrams = flags.useSplitNGrams;
				prefixSuffixNGrams = flags.useSplitPrefixSuffixNGrams;
			}
			else
			{
				internalNGrams = flags.useNGrams;
				prefixSuffixNGrams = flags.usePrefixSuffixNGrams;
			}
			if (flags.lowercaseNGrams)
			{
				toNGrams = toNGrams.ToLower(Locale.English);
			}
			if (flags.partialNGramRegexp != null)
			{
				Matcher m = flags.partialNGramPattern.Matcher(toNGrams);
				// log.info("Matching |" + flags.partialNGramRegexp +
				//                "| against |" + toNGrams + "|");
				if (m.Find())
				{
					if (m.GroupCount() > 0)
					{
						toNGrams = m.Group(1);
					}
					else
					{
						toNGrams = m.Group();
					}
				}
			}
			// log.info(" Matched |" + toNGrams + "|");
			// logger.info();
			ICollection<string> subs = null;
			if (flags.cacheNGrams)
			{
				subs = wordToSubstrings[toNGrams];
			}
			if (subs == null)
			{
				subs = new List<string>();
				string strN = featPrefix + '-';
				string strB = featPrefix + "B-";
				string strE = featPrefix + "E-";
				int wleng = toNGrams.Length;
				for (int i = 0; i < wleng; i++)
				{
					for (int j = i + flags.minNGramLeng; j <= min; j++)
					{
						if (prefixSuffixNGrams)
						{
							if (i == 0)
							{
								subs.Add(Intern(strB + Sharpen.Runtime.Substring(toNGrams, i, j)));
							}
							if (j == wleng)
							{
								subs.Add(Intern(strE + Sharpen.Runtime.Substring(toNGrams, i, j)));
							}
						}
						if (internalNGrams)
						{
							subs.Add(Intern(strN + Sharpen.Runtime.Substring(toNGrams, i, j)));
						}
					}
				}
				if (flags.cacheNGrams)
				{
					wordToSubstrings[toNGrams] = subs;
				}
			}
			return subs;
		}

		private static PrintWriter cliqueWriter;

		private static void NewFeaturePrinter(string prefix, string suffix, string encoding)
		{
			if (cliqueWriter != null)
			{
				CloseFeaturePrinter();
			}
			try
			{
				cliqueWriter = IOUtils.GetPrintWriter(prefix + '.' + suffix, encoding);
			}
			catch (IOException)
			{
				cliqueWriter = null;
			}
		}

		private static void CloseFeaturePrinter()
		{
			cliqueWriter.Close();
			cliqueWriter = null;
		}

		private static void PrintFeatures(string[] wi, ClassicCounter<string> features)
		{
			if (cliqueWriter != null)
			{
				for (int i = 0; i < wi.Length; i++)
				{
					if (i > 0)
					{
						cliqueWriter.Print("\t");
					}
					cliqueWriter.Print(wi[i]);
				}
				foreach (string feat in features.KeySet())
				{
					cliqueWriter.Print("\t");
					cliqueWriter.Print(feat);
					cliqueWriter.Print("\t");
					cliqueWriter.Print(features.GetCount(feat));
				}
				cliqueWriter.Println();
			}
		}

		private static void PrintFeatures(string[] wi, IList<string> features)
		{
			if (cliqueWriter != null)
			{
				for (int i = 0; i < wi.Length; i++)
				{
					if (i > 0)
					{
						cliqueWriter.Print("\t");
					}
					cliqueWriter.Print(wi[i]);
				}
				foreach (string feat in features)
				{
					cliqueWriter.Print("\t");
					cliqueWriter.Print(feat);
				}
				cliqueWriter.Println();
			}
		}

		/// <summary>Creates a classifier from training data.</summary>
		/// <remarks>
		/// Creates a classifier from training data.  A specialized training regimen.
		/// It searches for an appropriate l1reg parameter to use to get specified number of features.
		/// This is called from makeClassifier() when certain properties are set.
		/// </remarks>
		/// <param name="train">training data</param>
		/// <returns>trained classifier</returns>
		private IClassifier<string, string> MakeClassifierAdaptL1(GeneralDataset<string, string> train)
		{
			System.Diagnostics.Debug.Assert((globalFlags.useAdaptL1 && globalFlags.limitFeatures > 0));
			IClassifier<string, string> lc = null;
			double l1reg = globalFlags.l1reg;
			double l1regmax = globalFlags.l1regmax;
			double l1regmin = globalFlags.l1regmin;
			if (globalFlags.l1reg <= 0.0)
			{
				logger.Info("WARNING: useAdaptL1 set and limitFeatures to " + globalFlags.limitFeatures + ", but invalid value of l1reg=" + globalFlags.l1reg + ", defaulting to " + globalFlags.l1regmax);
				l1reg = l1regmax;
			}
			else
			{
				logger.Info("TRAIN: useAdaptL1 set and limitFeatures to " + globalFlags.limitFeatures + ", l1reg=" + globalFlags.l1reg + ", l1regmax=" + globalFlags.l1regmax + ", l1regmin=" + globalFlags.l1regmin);
			}
			ICollection<string> limitFeatureLabels = null;
			if (globalFlags.limitFeaturesLabels != null)
			{
				string[] labels = globalFlags.limitFeaturesLabels.Split(",");
				limitFeatureLabels = Generics.NewHashSet();
				foreach (string label in labels)
				{
					limitFeatureLabels.Add(label.Trim());
				}
			}
			// Do binary search starting with specified l1reg to find reasonable value of l1reg that gives desired number of features
			double l1regtop = l1regmax;
			double l1regbottom = l1regmin;
			int limitFeatureTol = 5;
			double l1regminchange = 0.05;
			while (true)
			{
				logger.Info("Training: l1reg=" + l1reg + ", threshold=" + globalFlags.featureWeightThreshold + ", target=" + globalFlags.limitFeatures);
				LinearClassifierFactory<string, string> lcf;
				IMinimizer<IDiffFunction> minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", l1reg);
				lcf = new LinearClassifierFactory<string, string>(minim, globalFlags.tolerance, globalFlags.useSum, globalFlags.prior, globalFlags.sigma, globalFlags.epsilon);
				int featureCount = -1;
				try
				{
					LinearClassifier<string, string> c = lcf.TrainClassifier(train);
					lc = c;
					featureCount = c.GetFeatureCount(limitFeatureLabels, globalFlags.featureWeightThreshold, false);
					/*useMagnitude*/
					logger.Info("Training Done: l1reg=" + l1reg + ", threshold=" + globalFlags.featureWeightThreshold + ", features=" + featureCount + ", target=" + globalFlags.limitFeatures);
					//         String classifierDesc = c.toString(globalFlags.printClassifier, globalFlags.printClassifierParam);
					IList<Triple<string, string, double>> topFeatures = c.GetTopFeatures(limitFeatureLabels, globalFlags.featureWeightThreshold, false, globalFlags.limitFeatures, true);
					/*useMagnitude*/
					/*descending order*/
					string classifierDesc = c.TopFeaturesToString(topFeatures);
					logger.Info("Printing top " + globalFlags.limitFeatures + " features with weights above " + globalFlags.featureWeightThreshold);
					if (globalFlags.limitFeaturesLabels != null)
					{
						logger.Info("  Limited to labels: " + globalFlags.limitFeaturesLabels);
					}
					logger.Info(classifierDesc);
				}
				catch (Exception ex)
				{
					if (ex.Message != null && ex.Message.StartsWith("L-BFGS chose a non-descent direction"))
					{
						logger.Info("Error in optimization, will try again with different l1reg");
						Sharpen.Runtime.PrintStackTrace(ex, System.Console.Error);
					}
					else
					{
						throw;
					}
				}
				if (featureCount < 0 || featureCount < globalFlags.limitFeatures - limitFeatureTol)
				{
					// Too few features or some other bad thing happened => decrease l1reg
					l1regtop = l1reg;
					l1reg = 0.5 * (l1reg + l1regbottom);
					if (l1regtop - l1reg < l1regminchange)
					{
						logger.Info("Stopping: old l1reg  " + l1regtop + "- new l1reg " + l1reg + ", difference less than " + l1regminchange);
						break;
					}
				}
				else
				{
					if (featureCount > globalFlags.limitFeatures + limitFeatureTol)
					{
						// Too many features => increase l1reg
						l1regbottom = l1reg;
						l1reg = 0.5 * (l1reg + l1regtop);
						if (l1reg - l1regbottom < l1regminchange)
						{
							logger.Info("Stopping: new l1reg  " + l1reg + "- old l1reg " + l1regbottom + ", difference less than " + l1regminchange);
							break;
						}
					}
					else
					{
						logger.Info("Stopping: # of features within " + limitFeatureTol + " of target");
						break;
					}
				}
			}
			// Update flags for later serialization
			globalFlags.l1reg = l1reg;
			return lc;
		}

		/// <summary>Creates a classifier from training data.</summary>
		/// <param name="train">training data</param>
		/// <returns>trained classifier</returns>
		public virtual IClassifier<string, string> MakeClassifier(GeneralDataset<string, string> train)
		{
			IClassifier<string, string> lc;
			if (globalFlags.useClassifierFactory != null)
			{
				IClassifierFactory<string, string, IClassifier<string, string>> cf;
				if (globalFlags.classifierFactoryArgs != null)
				{
					cf = ReflectionLoading.LoadByReflection(globalFlags.useClassifierFactory, globalFlags.classifierFactoryArgs);
				}
				else
				{
					cf = ReflectionLoading.LoadByReflection(globalFlags.useClassifierFactory);
				}
				lc = cf.TrainClassifier(train);
			}
			else
			{
				if (globalFlags.useNB)
				{
					double sigma = (globalFlags.prior == 0) ? 0.0 : globalFlags.sigma;
					lc = new NBLinearClassifierFactory<string, string>(sigma, globalFlags.useClassFeature).TrainClassifier(train);
				}
				else
				{
					if (globalFlags.useBinary)
					{
						LogisticClassifierFactory<string, string> lcf = new LogisticClassifierFactory<string, string>();
						LogPrior prior = new LogPrior(globalFlags.prior, globalFlags.sigma, globalFlags.epsilon);
						lc = lcf.TrainClassifier(train, globalFlags.l1reg, globalFlags.tolerance, prior, globalFlags.biased);
					}
					else
					{
						if (globalFlags.biased)
						{
							LogisticClassifierFactory<string, string> lcf = new LogisticClassifierFactory<string, string>();
							LogPrior prior = new LogPrior(globalFlags.prior, globalFlags.sigma, globalFlags.epsilon);
							lc = lcf.TrainClassifier(train, prior, true);
						}
						else
						{
							if (globalFlags.useAdaptL1 && globalFlags.limitFeatures > 0)
							{
								lc = MakeClassifierAdaptL1(train);
							}
							else
							{
								LinearClassifierFactory<string, string> lcf;
								if (globalFlags.l1reg > 0.0)
								{
									IMinimizer<IDiffFunction> minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", globalFlags.l1reg);
									lcf = new LinearClassifierFactory<string, string>(minim, globalFlags.tolerance, globalFlags.useSum, globalFlags.prior, globalFlags.sigma, globalFlags.epsilon);
								}
								else
								{
									lcf = new LinearClassifierFactory<string, string>(globalFlags.tolerance, globalFlags.useSum, globalFlags.prior, globalFlags.sigma, globalFlags.epsilon, globalFlags.QNsize);
								}
								lcf.SetVerbose(globalFlags.verboseOptimization);
								if (!globalFlags.useQN)
								{
									lcf.UseConjugateGradientAscent();
								}
								lc = lcf.TrainClassifier(train);
							}
						}
					}
				}
			}
			return lc;
		}

		private static string[] RegexpTokenize(Pattern tokenizerRegexp, Pattern ignoreRegexp, string inWord)
		{
			IList<string> al = new List<string>();
			string word = inWord;
			while (!word.IsEmpty())
			{
				// logger.info("String to match on is " + word);
				Matcher mig = null;
				if (ignoreRegexp != null)
				{
					mig = ignoreRegexp.Matcher(word);
				}
				if (mig != null && mig.LookingAt())
				{
					word = Sharpen.Runtime.Substring(word, mig.End());
				}
				else
				{
					Matcher m = tokenizerRegexp.Matcher(word);
					if (m.LookingAt())
					{
						// Logging.logger(ColumnDataClassifier.class).info("Matched " + m.end() + " chars: " +
						//		       word.substring(0, m.end()));
						al.Add(Sharpen.Runtime.Substring(word, 0, m.End()));
						word = Sharpen.Runtime.Substring(word, m.End());
					}
					else
					{
						logger.Info("Warning: regexpTokenize pattern " + tokenizerRegexp + " didn't match on |" + Sharpen.Runtime.Substring(word, 0, 1) + "| of |" + word + '|');
						// logger.info("Default matched 1 char: " +
						//		       word.substring(0, 1));
						al.Add(Sharpen.Runtime.Substring(word, 0, 1));
						word = Sharpen.Runtime.Substring(word, 1);
					}
				}
			}
			string[] bits = Sharpen.Collections.ToArray(al, new string[al.Count]);
			return bits;
		}

		private static string[] SplitTokenize(Pattern splitRegexp, Pattern ignoreRegexp, string cWord)
		{
			string[] bits = splitRegexp.Split(cWord);
			if (ignoreRegexp != null)
			{
				IList<string> keepBits = new List<string>(bits.Length);
				foreach (string bit in bits)
				{
					if (!ignoreRegexp.Matcher(bit).Matches())
					{
						keepBits.Add(bit);
					}
				}
				if (keepBits.Count != bits.Length)
				{
					bits = new string[keepBits.Count];
					Sharpen.Collections.ToArray(keepBits, bits);
				}
			}
			return bits;
		}

		private static IDictionary<string, float[]> LoadWordVectors(string filename)
		{
			Timing timing = new Timing();
			IDictionary<string, float[]> map = new Dictionary<string, float[]>(10000);
			// presumably they'll load a fair-sized vocab!?
			try
			{
				using (BufferedReader br = IOUtils.ReaderFromString(filename))
				{
					int numDimensions = -1;
					bool warned = false;
					for (string line; (line = br.ReadLine()) != null; )
					{
						string[] fields = line.Split("\\s+");
						if (numDimensions < 0)
						{
							numDimensions = fields.Length - 1;
						}
						else
						{
							if (numDimensions != fields.Length - 1 && !warned)
							{
								logger.Info("loadWordVectors: Inconsistent vector size: " + numDimensions + " vs. " + (fields.Length - 1));
								warned = true;
							}
						}
						float[] vector = new float[fields.Length - 1];
						for (int i = 1; i < fields.Length; i++)
						{
							vector[i - 1] = float.ParseFloat(fields[i]);
						}
						map[fields[0]] = vector;
					}
				}
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException("Couldn't load word vectors", ioe);
			}
			timing.Done("Loading word vectors from " + filename + " ... ");
			return map;
		}

		/// <summary>Initialize using values in Properties file.</summary>
		/// <param name="props">Properties, with the special format of column.flag used in ColumnDataClassifier</param>
		/// <returns>An array of flags for each data column, with additional global flags in element [0]</returns>
		private static Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> SetProperties(Properties props)
		{
			ColumnDataClassifier.Flags[] myFlags;
			IClassifier<string, string> classifier = null;
			bool myUsesRealValues = false;
			Pattern prefix;
			try
			{
				prefix = Pattern.Compile("([0-9]+)\\.(.*)");
			}
			catch (PatternSyntaxException pse)
			{
				throw new Exception(pse);
			}
			// if we are loading a classifier then we have to load it before we do anything
			// else and have its Properties be the defaults that can then be overridden by
			// other command-line arguments
			string loadPath = props.GetProperty("loadClassifier");
			if (loadPath != null)
			{
				Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> pair = LoadClassifier(loadPath);
				myFlags = pair.First();
				classifier = pair.Second();
			}
			else
			{
				myFlags = new ColumnDataClassifier.Flags[1];
				myFlags[0] = new ColumnDataClassifier.Flags();
			}
			// initialize zero column flags used for global flags; it can't be null
			logger.Info("Setting ColumnDataClassifier properties");
			foreach (string key in props.StringPropertyNames())
			{
				string val = props.GetProperty(key);
				logger.Info(key + " = " + val);
				int col = 0;
				// the default (first after class)
				Matcher matcher = prefix.Matcher(key);
				if (matcher.Matches())
				{
					col = System.Convert.ToInt32(matcher.Group(1));
					key = matcher.Group(2);
				}
				if (col >= myFlags.Length)
				{
					myFlags = Arrays.CopyOf(myFlags, col + 1);
				}
				if (myFlags[col] == null)
				{
					myFlags[col] = new ColumnDataClassifier.Flags();
				}
				if (key.Equals("useString"))
				{
					myFlags[col].useString = bool.ParseBoolean(val);
				}
				else
				{
					if (key.Equals("binnedLengths"))
					{
						if (val != null)
						{
							string[] binnedLengthStrs = val.Split("[, ]+");
							myFlags[col].binnedLengths = new int[binnedLengthStrs.Length];
							for (int i = 0; i < myFlags[col].binnedLengths.Length; i++)
							{
								myFlags[col].binnedLengths[i] = System.Convert.ToInt32(binnedLengthStrs[i]);
							}
						}
					}
					else
					{
						if (key.Equals("binnedLengthsStatistics"))
						{
							if (bool.ParseBoolean(val))
							{
								myFlags[col].binnedLengthsCounter = new TwoDimensionalCounter<string, string>();
							}
						}
						else
						{
							if (key.Equals("splitWordCount"))
							{
								myFlags[col].splitWordCount = bool.ParseBoolean(val);
							}
							else
							{
								if (key.Equals("logSplitWordCount"))
								{
									myFlags[col].logSplitWordCount = bool.ParseBoolean(val);
								}
								else
								{
									if (key.Equals("binnedSplitWordCounts"))
									{
										if (val != null)
										{
											string[] binnedSplitWordCountStrs = val.Split("[, ]+");
											myFlags[col].binnedSplitWordCounts = new int[binnedSplitWordCountStrs.Length];
											for (int i = 0; i < myFlags[col].binnedSplitWordCounts.Length; i++)
											{
												myFlags[col].binnedSplitWordCounts[i] = System.Convert.ToInt32(binnedSplitWordCountStrs[i]);
											}
										}
									}
									else
									{
										if (key.Equals("countChars"))
										{
											myFlags[col].countChars = val.ToCharArray();
										}
										else
										{
											if (key.Equals("countCharsBins"))
											{
												if (val != null)
												{
													string[] binnedCountStrs = val.Split("[, ]+");
													myFlags[col].countCharsBins = new int[binnedCountStrs.Length];
													for (int i = 0; i < binnedCountStrs.Length; i++)
													{
														myFlags[col].countCharsBins[i] = System.Convert.ToInt32(binnedCountStrs[i]);
													}
												}
											}
											else
											{
												if (key.Equals("binnedValues"))
												{
													if (val != null)
													{
														string[] binnedValuesStrs = val.Split("[, ]+");
														myFlags[col].binnedValues = new double[binnedValuesStrs.Length];
														for (int i = 0; i < myFlags[col].binnedValues.Length; i++)
														{
															myFlags[col].binnedValues[i] = double.ParseDouble(binnedValuesStrs[i]);
														}
													}
												}
												else
												{
													if (key.Equals("binnedValuesNaN"))
													{
														myFlags[col].binnedValuesNaN = double.ParseDouble(val);
													}
													else
													{
														if (key.Equals("binnedValuesStatistics"))
														{
															if (bool.ParseBoolean(val))
															{
																myFlags[col].binnedValuesCounter = new TwoDimensionalCounter<string, string>();
															}
														}
														else
														{
															if (key.Equals("useNGrams"))
															{
																myFlags[col].useNGrams = bool.ParseBoolean(val);
															}
															else
															{
																if (key.Equals("usePrefixSuffixNGrams"))
																{
																	myFlags[col].usePrefixSuffixNGrams = bool.ParseBoolean(val);
																}
																else
																{
																	if (key.Equals("useSplitNGrams"))
																	{
																		myFlags[col].useSplitNGrams = bool.ParseBoolean(val);
																	}
																	else
																	{
																		if (key.Equals("wordShape"))
																		{
																			myFlags[col].wordShape = WordShapeClassifier.LookupShaper(val);
																		}
																		else
																		{
																			if (key.Equals("splitWordShape"))
																			{
																				myFlags[col].splitWordShape = WordShapeClassifier.LookupShaper(val);
																			}
																			else
																			{
																				if (key.Equals("useSplitPrefixSuffixNGrams"))
																				{
																					myFlags[col].useSplitPrefixSuffixNGrams = bool.ParseBoolean(val);
																				}
																				else
																				{
																					if (key.Equals("lowercaseNGrams"))
																					{
																						myFlags[col].lowercaseNGrams = bool.ParseBoolean(val);
																					}
																					else
																					{
																						if (key.Equals("lowercase"))
																						{
																							myFlags[col].lowercase = bool.ParseBoolean(val);
																						}
																						else
																						{
																							if (key.Equals("useLowercaseSplitWords"))
																							{
																								myFlags[col].useLowercaseSplitWords = bool.ParseBoolean(val);
																							}
																							else
																							{
																								if (key.Equals("useSum"))
																								{
																									myFlags[col].useSum = bool.ParseBoolean(val);
																								}
																								else
																								{
																									if (key.Equals("tolerance"))
																									{
																										myFlags[col].tolerance = double.ParseDouble(val);
																									}
																									else
																									{
																										if (key.Equals("printFeatures"))
																										{
																											myFlags[col].printFeatures = val;
																										}
																										else
																										{
																											if (key.Equals("printClassifier"))
																											{
																												myFlags[col].printClassifier = val;
																											}
																											else
																											{
																												if (key.Equals("printClassifierParam"))
																												{
																													myFlags[col].printClassifierParam = System.Convert.ToInt32(val);
																												}
																												else
																												{
																													if (key.Equals("exitAfterTrainingFeaturization"))
																													{
																														myFlags[col].exitAfterTrainingFeaturization = bool.ParseBoolean(val);
																													}
																													else
																													{
																														if (key.Equals("intern") || key.Equals("intern2"))
																														{
																															myFlags[col].intern = bool.ParseBoolean(val);
																														}
																														else
																														{
																															if (key.Equals("cacheNGrams"))
																															{
																																myFlags[col].cacheNGrams = bool.ParseBoolean(val);
																															}
																															else
																															{
																																if (key.Equals("useClassifierFactory"))
																																{
																																	myFlags[col].useClassifierFactory = val;
																																}
																																else
																																{
																																	if (key.Equals("classifierFactoryArgs"))
																																	{
																																		myFlags[col].classifierFactoryArgs = val;
																																	}
																																	else
																																	{
																																		if (key.Equals("useNB"))
																																		{
																																			myFlags[col].useNB = bool.ParseBoolean(val);
																																		}
																																		else
																																		{
																																			if (key.Equals("useBinary"))
																																			{
																																				myFlags[col].useBinary = bool.ParseBoolean(val);
																																			}
																																			else
																																			{
																																				if (key.Equals("l1reg"))
																																				{
																																					myFlags[col].l1reg = double.ParseDouble(val);
																																				}
																																				else
																																				{
																																					if (key.Equals("useAdaptL1"))
																																					{
																																						myFlags[col].useAdaptL1 = bool.ParseBoolean(val);
																																					}
																																					else
																																					{
																																						if (key.Equals("limitFeatures"))
																																						{
																																							myFlags[col].limitFeatures = System.Convert.ToInt32(val);
																																						}
																																						else
																																						{
																																							if (key.Equals("l1regmin"))
																																							{
																																								myFlags[col].l1regmin = double.ParseDouble(val);
																																							}
																																							else
																																							{
																																								if (key.Equals("l1regmax"))
																																								{
																																									myFlags[col].l1regmax = double.ParseDouble(val);
																																								}
																																								else
																																								{
																																									if (key.Equals("limitFeaturesLabels"))
																																									{
																																										myFlags[col].limitFeaturesLabels = val;
																																									}
																																									else
																																									{
																																										if (key.Equals("featureWeightThreshold"))
																																										{
																																											myFlags[col].featureWeightThreshold = double.ParseDouble(val);
																																										}
																																										else
																																										{
																																											if (key.Equals("useClassFeature"))
																																											{
																																												myFlags[col].useClassFeature = bool.ParseBoolean(val);
																																											}
																																											else
																																											{
																																												if (key.Equals("featureMinimumSupport"))
																																												{
																																													myFlags[col].featureMinimumSupport = System.Convert.ToInt32(val);
																																												}
																																												else
																																												{
																																													if (key.Equals("prior"))
																																													{
																																														if (Sharpen.Runtime.EqualsIgnoreCase(val, "no"))
																																														{
																																															myFlags[col].prior = (int)(LogPrior.LogPriorType.Null);
																																														}
																																														else
																																														{
																																															if (Sharpen.Runtime.EqualsIgnoreCase(val, "huber"))
																																															{
																																																myFlags[col].prior = (int)(LogPrior.LogPriorType.Huber);
																																															}
																																															else
																																															{
																																																if (Sharpen.Runtime.EqualsIgnoreCase(val, "quadratic"))
																																																{
																																																	myFlags[col].prior = (int)(LogPrior.LogPriorType.Quadratic);
																																																}
																																																else
																																																{
																																																	if (Sharpen.Runtime.EqualsIgnoreCase(val, "quartic"))
																																																	{
																																																		myFlags[col].prior = (int)(LogPrior.LogPriorType.Quartic);
																																																	}
																																																	else
																																																	{
																																																		try
																																																		{
																																																			myFlags[col].prior = System.Convert.ToInt32(val);
																																																		}
																																																		catch (NumberFormatException)
																																																		{
																																																			logger.Info("Unknown prior " + val + "; using none.");
																																																		}
																																																	}
																																																}
																																															}
																																														}
																																													}
																																													else
																																													{
																																														if (key.Equals("sigma"))
																																														{
																																															myFlags[col].sigma = double.ParseDouble(val);
																																														}
																																														else
																																														{
																																															if (key.Equals("epsilon"))
																																															{
																																																myFlags[col].epsilon = double.ParseDouble(val);
																																															}
																																															else
																																															{
																																																if (key.Equals("maxNGramLeng"))
																																																{
																																																	myFlags[col].maxNGramLeng = System.Convert.ToInt32(val);
																																																}
																																																else
																																																{
																																																	if (key.Equals("minNGramLeng"))
																																																	{
																																																		myFlags[col].minNGramLeng = System.Convert.ToInt32(val);
																																																	}
																																																	else
																																																	{
																																																		if (key.Equals("partialNGramRegexp"))
																																																		{
																																																			myFlags[col].partialNGramRegexp = val;
																																																			try
																																																			{
																																																				myFlags[col].partialNGramPattern = Pattern.Compile(myFlags[col].partialNGramRegexp);
																																																			}
																																																			catch (PatternSyntaxException)
																																																			{
																																																				logger.Info("Ill-formed partialNGramPattern: " + myFlags[col].partialNGramPattern);
																																																				myFlags[col].partialNGramRegexp = null;
																																																			}
																																																		}
																																																		else
																																																		{
																																																			if (key.Equals("splitWordsRegexp"))
																																																			{
																																																				try
																																																				{
																																																					myFlags[col].splitWordsPattern = Pattern.Compile(val);
																																																				}
																																																				catch (PatternSyntaxException)
																																																				{
																																																					logger.Info("Ill-formed splitWordsRegexp: " + val);
																																																				}
																																																			}
																																																			else
																																																			{
																																																				if (key.Equals("splitWordsTokenizerRegexp"))
																																																				{
																																																					try
																																																					{
																																																						myFlags[col].splitWordsTokenizerPattern = Pattern.Compile(val);
																																																					}
																																																					catch (PatternSyntaxException)
																																																					{
																																																						logger.Info("Ill-formed splitWordsTokenizerRegexp: " + val);
																																																					}
																																																				}
																																																				else
																																																				{
																																																					if (key.Equals("splitWordsIgnoreRegexp"))
																																																					{
																																																						string trimVal = val.Trim();
																																																						if (trimVal.IsEmpty())
																																																						{
																																																							myFlags[col].splitWordsIgnorePattern = null;
																																																						}
																																																						else
																																																						{
																																																							try
																																																							{
																																																								myFlags[col].splitWordsIgnorePattern = Pattern.Compile(trimVal);
																																																							}
																																																							catch (PatternSyntaxException)
																																																							{
																																																								logger.Info("Ill-formed splitWordsIgnoreRegexp: " + trimVal);
																																																							}
																																																						}
																																																					}
																																																					else
																																																					{
																																																						if (key.Equals("useSplitWords"))
																																																						{
																																																							myFlags[col].useSplitWords = bool.ParseBoolean(val);
																																																						}
																																																						else
																																																						{
																																																							if (key.Equals("useSplitWordPairs"))
																																																							{
																																																								myFlags[col].useSplitWordPairs = bool.ParseBoolean(val);
																																																							}
																																																							else
																																																							{
																																																								if (key.Equals("useLowercaseSplitWordPairs"))
																																																								{
																																																									myFlags[col].useLowercaseSplitWordPairs = bool.ParseBoolean(val);
																																																								}
																																																								else
																																																								{
																																																									if (key.Equals("useAllSplitWordPairs"))
																																																									{
																																																										myFlags[col].useAllSplitWordPairs = bool.ParseBoolean(val);
																																																									}
																																																									else
																																																									{
																																																										if (key.Equals("useAllSplitWordTriples"))
																																																										{
																																																											myFlags[col].useAllSplitWordTriples = bool.ParseBoolean(val);
																																																										}
																																																										else
																																																										{
																																																											if (key.Equals("useSplitWordNGrams"))
																																																											{
																																																												myFlags[col].useSplitWordNGrams = bool.ParseBoolean(val);
																																																											}
																																																											else
																																																											{
																																																												if (key.Equals("maxWordNGramLeng"))
																																																												{
																																																													myFlags[col].maxWordNGramLeng = System.Convert.ToInt32(val);
																																																												}
																																																												else
																																																												{
																																																													if (key.Equals("minWordNGramLeng"))
																																																													{
																																																														myFlags[col].minWordNGramLeng = System.Convert.ToInt32(val);
																																																														if (myFlags[col].minWordNGramLeng < 1)
																																																														{
																																																															logger.Info("minWordNGramLeng set to " + myFlags[col].minWordNGramLeng + ", resetting to 1");
																																																															myFlags[col].minWordNGramLeng = 1;
																																																														}
																																																													}
																																																													else
																																																													{
																																																														if (key.Equals("wordNGramBoundaryRegexp"))
																																																														{
																																																															myFlags[col].wordNGramBoundaryRegexp = val;
																																																															try
																																																															{
																																																																myFlags[col].wordNGramBoundaryPattern = Pattern.Compile(myFlags[col].wordNGramBoundaryRegexp);
																																																															}
																																																															catch (PatternSyntaxException)
																																																															{
																																																																logger.Info("Ill-formed wordNGramBoundary regexp: " + myFlags[col].wordNGramBoundaryRegexp);
																																																																myFlags[col].wordNGramBoundaryRegexp = null;
																																																															}
																																																														}
																																																														else
																																																														{
																																																															if (key.Equals("useSplitFirstLastWords"))
																																																															{
																																																																myFlags[col].useSplitFirstLastWords = bool.ParseBoolean(val);
																																																															}
																																																															else
																																																															{
																																																																if (key.Equals("useLowercaseSplitFirstLastWords"))
																																																																{
																																																																	myFlags[col].useLowercaseSplitFirstLastWords = bool.ParseBoolean(val);
																																																																}
																																																																else
																																																																{
																																																																	if (key.Equals("loadClassifier"))
																																																																	{
																																																																		myFlags[col].loadClassifier = val;
																																																																	}
																																																																	else
																																																																	{
																																																																		if (key.Equals("serializeTo"))
																																																																		{
																																																																			ColumnDataClassifier.Flags.serializeTo = val;
																																																																		}
																																																																		else
																																																																		{
																																																																			if (key.Equals("printTo"))
																																																																			{
																																																																				ColumnDataClassifier.Flags.printTo = val;
																																																																			}
																																																																			else
																																																																			{
																																																																				if (key.Equals("trainFile"))
																																																																				{
																																																																					ColumnDataClassifier.Flags.trainFile = val;
																																																																				}
																																																																				else
																																																																				{
																																																																					if (key.Equals("displayAllAnswers"))
																																																																					{
																																																																						ColumnDataClassifier.Flags.displayAllAnswers = bool.ParseBoolean(val);
																																																																					}
																																																																					else
																																																																					{
																																																																						if (key.Equals("testFile"))
																																																																						{
																																																																							myFlags[col].testFile = val;
																																																																						}
																																																																						else
																																																																						{
																																																																							if (key.Equals("trainFromSVMLight"))
																																																																							{
																																																																								ColumnDataClassifier.Flags.trainFromSVMLight = bool.ParseBoolean(val);
																																																																							}
																																																																							else
																																																																							{
																																																																								if (key.Equals("testFromSVMLight"))
																																																																								{
																																																																									ColumnDataClassifier.Flags.testFromSVMLight = bool.ParseBoolean(val);
																																																																								}
																																																																								else
																																																																								{
																																																																									if (key.Equals("encoding"))
																																																																									{
																																																																										ColumnDataClassifier.Flags.encoding = val;
																																																																									}
																																																																									else
																																																																									{
																																																																										if (key.Equals("printSVMLightFormatTo"))
																																																																										{
																																																																											ColumnDataClassifier.Flags.printSVMLightFormatTo = val;
																																																																										}
																																																																										else
																																																																										{
																																																																											if (key.Equals("displayedColumn"))
																																																																											{
																																																																												myFlags[col].displayedColumn = System.Convert.ToInt32(val);
																																																																											}
																																																																											else
																																																																											{
																																																																												if (key.Equals("groupingColumn"))
																																																																												{
																																																																													myFlags[col].groupingColumn = System.Convert.ToInt32(val);
																																																																												}
																																																																												else
																																																																												{
																																																																													// logger.info("Grouping column is " + (myFlags[col].groupingColumn));
																																																																													if (key.Equals("rankingScoreColumn"))
																																																																													{
																																																																														myFlags[col].rankingScoreColumn = System.Convert.ToInt32(val);
																																																																													}
																																																																													else
																																																																													{
																																																																														// logger.info("Ranking score column is " + (myFlags[col].rankingScoreColumn));
																																																																														if (key.Equals("rankingAccuracyClass"))
																																																																														{
																																																																															myFlags[col].rankingAccuracyClass = val;
																																																																														}
																																																																														else
																																																																														{
																																																																															if (key.Equals("goldAnswerColumn"))
																																																																															{
																																																																																myFlags[col].goldAnswerColumn = System.Convert.ToInt32(val);
																																																																															}
																																																																															else
																																																																															{
																																																																																// logger.info("Gold answer column is " + (myFlags[col].goldAnswerColumn));  // it's a nuisance to print this when used programmatically
																																																																																if (key.Equals("useQN"))
																																																																																{
																																																																																	myFlags[col].useQN = bool.ParseBoolean(val);
																																																																																}
																																																																																else
																																																																																{
																																																																																	if (key.Equals("QNsize"))
																																																																																	{
																																																																																		myFlags[col].QNsize = System.Convert.ToInt32(val);
																																																																																	}
																																																																																	else
																																																																																	{
																																																																																		if (key.Equals("featureFormat"))
																																																																																		{
																																																																																			myFlags[col].featureFormat = bool.ParseBoolean(val);
																																																																																		}
																																																																																		else
																																																																																		{
																																																																																			if (key.Equals("significantColumnId"))
																																																																																			{
																																																																																				myFlags[col].significantColumnId = bool.ParseBoolean(val);
																																																																																			}
																																																																																			else
																																																																																			{
																																																																																				if (key.Equals("justify"))
																																																																																				{
																																																																																					myFlags[col].justify = bool.ParseBoolean(val);
																																																																																				}
																																																																																				else
																																																																																				{
																																																																																					if (key.Equals("verboseOptimization"))
																																																																																					{
																																																																																						myFlags[col].verboseOptimization = bool.ParseBoolean(val);
																																																																																					}
																																																																																					else
																																																																																					{
																																																																																						if (key.Equals("realValued"))
																																																																																						{
																																																																																							myFlags[col].isRealValued = bool.ParseBoolean(val);
																																																																																							myUsesRealValues = myUsesRealValues || myFlags[col].isRealValued;
																																																																																						}
																																																																																						else
																																																																																						{
																																																																																							if (key.Equals("logTransform"))
																																																																																							{
																																																																																								myFlags[col].logTransform = bool.ParseBoolean(val);
																																																																																								myUsesRealValues = myUsesRealValues || myFlags[col].logTransform;
																																																																																							}
																																																																																							else
																																																																																							{
																																																																																								if (key.Equals("logitTransform"))
																																																																																								{
																																																																																									myFlags[col].logitTransform = bool.ParseBoolean(val);
																																																																																									myUsesRealValues = myUsesRealValues || myFlags[col].logitTransform;
																																																																																								}
																																																																																								else
																																																																																								{
																																																																																									if (key.Equals("sqrtTransform"))
																																																																																									{
																																																																																										myFlags[col].sqrtTransform = bool.ParseBoolean(val);
																																																																																										myUsesRealValues = myUsesRealValues || myFlags[col].sqrtTransform;
																																																																																									}
																																																																																									else
																																																																																									{
																																																																																										if (key.Equals("filename"))
																																																																																										{
																																																																																											myFlags[col].filename = bool.ParseBoolean(val);
																																																																																										}
																																																																																										else
																																																																																										{
																																																																																											if (key.Equals("biased"))
																																																																																											{
																																																																																												myFlags[col].biased = bool.ParseBoolean(val);
																																																																																											}
																																																																																											else
																																																																																											{
																																																																																												if (key.Equals("biasedHyperplane"))
																																																																																												{
																																																																																													// logger.info("Constraints is " + constraints);
																																																																																													if (val != null && val.Trim().Length > 0)
																																																																																													{
																																																																																														string[] bits = val.Split("[, ]+");
																																																																																														myFlags[col].biasedHyperplane = new ClassicCounter<string>();
																																																																																														for (int i = 0; i < bits.Length; i += 2)
																																																																																														{
																																																																																															myFlags[col].biasedHyperplane.SetCount(bits[i], double.ParseDouble(bits[i + 1]));
																																																																																														}
																																																																																													}
																																																																																												}
																																																																																												else
																																																																																												{
																																																																																													// logger.info("Biased Hyperplane is " + biasedHyperplane);
																																																																																													if (key.Equals("crossValidationFolds"))
																																																																																													{
																																																																																														myFlags[col].crossValidationFolds = System.Convert.ToInt32(val);
																																																																																													}
																																																																																													else
																																																																																													{
																																																																																														if (key.Equals("printCrossValidationDecisions"))
																																																																																														{
																																																																																															myFlags[col].printCrossValidationDecisions = bool.ParseBoolean(val);
																																																																																														}
																																																																																														else
																																																																																														{
																																																																																															if (key.Equals("shuffleTrainingData"))
																																																																																															{
																																																																																																myFlags[col].shuffleTrainingData = bool.ParseBoolean(val);
																																																																																															}
																																																																																															else
																																																																																															{
																																																																																																if (key.Equals("shuffleSeed"))
																																																																																																{
																																																																																																	myFlags[col].shuffleSeed = long.Parse(val);
																																																																																																}
																																																																																																else
																																																																																																{
																																																																																																	if (key.Equals("csvInput"))
																																																																																																	{
																																																																																																		myFlags[col].csvInput = bool.ParseBoolean(val);
																																																																																																	}
																																																																																																	else
																																																																																																	{
																																																																																																		if (key.Equals("inputFormat"))
																																																																																																		{
																																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(val, "header"))
																																																																																																			{
																																																																																																				myFlags[col].inputFormat = ColumnDataClassifier.InputFormat.Header;
																																																																																																			}
																																																																																																			else
																																																																																																			{
																																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(val, "comments"))
																																																																																																				{
																																																																																																					myFlags[col].inputFormat = ColumnDataClassifier.InputFormat.Comments;
																																																																																																				}
																																																																																																				else
																																																																																																				{
																																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(val, "plain"))
																																																																																																					{
																																																																																																						myFlags[col].inputFormat = ColumnDataClassifier.InputFormat.Plain;
																																																																																																					}
																																																																																																					else
																																																																																																					{
																																																																																																						logger.Info("Unknown inputFormat: " + val);
																																																																																																					}
																																																																																																				}
																																																																																																			}
																																																																																																		}
																																																																																																		else
																																																																																																		{
																																																																																																			if (key.Equals("splitWordsWithPTBTokenizer"))
																																																																																																			{
																																																																																																				// System.out.println("splitting with ptb tokenizer");
																																																																																																				myFlags[col].splitWordsWithPTBTokenizer = bool.ParseBoolean(val);
																																																																																																			}
																																																																																																			else
																																																																																																			{
																																																																																																				if (key.Equals("useSplitWordVectors"))
																																																																																																				{
																																																																																																					myFlags[col].wordVectors = LoadWordVectors(val);
																																																																																																					myUsesRealValues = true;
																																																																																																				}
																																																																																																				else
																																																																																																				{
																																																																																																					if (key.Equals("showTokenization"))
																																																																																																					{
																																																																																																						myFlags[col].showTokenization = bool.ParseBoolean(val);
																																																																																																					}
																																																																																																					else
																																																																																																					{
																																																																																																						if (key.Equals("csvOutput"))
																																																																																																						{
																																																																																																							myFlags[col].csvOutput = val;
																																																																																																						}
																																																																																																						else
																																																																																																						{
																																																																																																							if (!key.IsEmpty() && !key.Equals("prop"))
																																																																																																							{
																																																																																																								logger.Info("Unknown property: |" + key + '|');
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
			}
			myFlags[0].usesRealValues = myUsesRealValues;
			return new Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>>(myFlags, classifier);
		}

		/// <summary>Construct a ColumnDataClassifier.</summary>
		/// <param name="filename">
		/// The file with properties which specifies all aspects of behavior.
		/// See the class documentation for details of the properties.
		/// </param>
		public ColumnDataClassifier(string filename)
			: this(StringUtils.PropFileToProperties(filename))
		{
		}

		/// <summary>Construct a ColumnDataClassifier.</summary>
		/// <param name="props">
		/// The properties object specifies all aspects of its behavior.
		/// See the class documentation for details of the properties.
		/// </param>
		public ColumnDataClassifier(Properties props)
			: this(SetProperties(props))
		{
		}

		/// <summary>Construct a ColumnDataClassifier.</summary>
		/// <param name="flagsClassifierPair">
		/// A Pair of a Flags object array specifies all aspects of featurization
		/// and other behavior and a Classifier that will be used.
		/// </param>
		public ColumnDataClassifier(Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> flagsClassifierPair)
		{
			flags = flagsClassifierPair.First();
			globalFlags = flags[0];
			classifier = flagsClassifierPair.Second();
		}

		private static Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> LoadClassifier(string path)
		{
			Timing t = new Timing();
			try
			{
				using (ObjectInputStream ois = IOUtils.ReadStreamFromString(path))
				{
					Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> pair = LoadClassifier(ois);
					t.Done(logger, "Loading classifier from " + path);
					return pair;
				}
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Error loading classifier from " + path, e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private static Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>> LoadClassifier(ObjectInputStream ois)
		{
			// load the classifier
			IClassifier<string, string> classifier = ErasureUtils.UncheckedCast<LinearClassifier<string, string>>(ois.ReadObject());
			ColumnDataClassifier.Flags[] myFlags = (ColumnDataClassifier.Flags[])ois.ReadObject();
			System.Diagnostics.Debug.Assert(myFlags.Length > 0);
			return new Pair<ColumnDataClassifier.Flags[], IClassifier<string, string>>(myFlags, classifier);
		}

		/// <summary>Return a new ColumnDataClassifier object based on a serialized object.</summary>
		/// <remarks>
		/// Return a new ColumnDataClassifier object based on a serialized object.
		/// The serialized object stores both a Flags[] that specifies feature extraction and
		/// other properties of the classifier and a Classifier object.
		/// </remarks>
		/// <param name="path">A classpath resource, URL, or file system path</param>
		/// <returns>The ColumnDataClassifier</returns>
		public static Edu.Stanford.Nlp.Classify.ColumnDataClassifier GetClassifier(string path)
		{
			return new Edu.Stanford.Nlp.Classify.ColumnDataClassifier(LoadClassifier(path));
		}

		/// <summary>Return a new ColumnDataClassifier object based on a serialized object.</summary>
		/// <remarks>
		/// Return a new ColumnDataClassifier object based on a serialized object.
		/// The serialized stream stores both a Flags[] that specifies feature extraction and
		/// other properties of the classifier and a Classifier object.
		/// </remarks>
		/// <param name="ois">Where to read a serialized classifier from</param>
		/// <returns>The ColumnDataClassifier</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.Classify.ColumnDataClassifier GetClassifier(ObjectInputStream ois)
		{
			return new Edu.Stanford.Nlp.Classify.ColumnDataClassifier(LoadClassifier(ois));
		}

		/// <summary>Runs the ColumnDataClassifier from the command-line.</summary>
		/// <remarks>
		/// Runs the ColumnDataClassifier from the command-line.  Usage:
		/// <c>
		/// java edu.stanford.nlp.classify.ColumnDataClassifier -trainFile trainFile
		/// -testFile testFile [-useNGrams|-useString|-sigma sigma|...]
		/// </c>
		/// or
		/// <c>java ColumnDataClassifier -prop propFile</c>
		/// </remarks>
		/// <param name="args">Command line arguments, as described in the class documentation</param>
		/// <exception cref="System.IO.IOException">If IO problems</exception>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(logger, args);
			// the constructor will load a classifier if one is specified with loadClassifier
			Edu.Stanford.Nlp.Classify.ColumnDataClassifier cdc = new Edu.Stanford.Nlp.Classify.ColumnDataClassifier(StringUtils.ArgsToProperties(args));
			string testFile = cdc.globalFlags.testFile;
			// check that we have roughly sensible options or else warn and exit
			if ((testFile == null && ColumnDataClassifier.Flags.serializeTo == null && cdc.globalFlags.crossValidationFolds < 2) || (ColumnDataClassifier.Flags.trainFile == null && cdc.globalFlags.loadClassifier == null))
			{
				logger.Info("usage: java edu.stanford.nlp.classify.ColumnDataClassifier -prop propFile");
				logger.Info("  and/or: -trainFile trainFile -testFile testFile|-serializeTo modelFile [-useNGrams|-sigma sigma|...]");
				return;
			}
			// ENDS PROCESSING
			if (cdc.globalFlags.loadClassifier == null)
			{
				// Otherwise we attempt to train one and exit if we don't succeed
				if (!cdc.TrainClassifier(ColumnDataClassifier.Flags.trainFile))
				{
					return;
				}
			}
			if (testFile != null)
			{
				cdc.TestClassifier(testFile);
			}
		}

		// end main()
		/// <exception cref="System.IO.IOException"/>
		public virtual bool TrainClassifier(string path)
		{
			// build dataset of training data featurized
			Pair<GeneralDataset<string, string>, IList<string[]>> dataInfo = ReadAndReturnTrainingExamples(path);
			GeneralDataset<string, string> train = dataInfo.First();
			IList<string[]> lineInfos = dataInfo.Second();
			// For things like cross validation, we may well need to sort data!  Data sets are often ordered by class.
			if (globalFlags.shuffleTrainingData)
			{
				long seed;
				if (globalFlags.shuffleSeed != 0)
				{
					seed = globalFlags.shuffleSeed;
				}
				else
				{
					seed = Runtime.NanoTime();
				}
				train.ShuffleWithSideInformation(seed, lineInfos);
			}
			// print any binned value histograms
			for (int i = 0; i < flags.Length; i++)
			{
				if (flags[i] != null && flags[i].binnedValuesCounter != null)
				{
					logger.Info("BinnedValuesStatistics for column " + i);
					logger.Info(flags[i].binnedValuesCounter.ToString());
				}
			}
			// print any binned length histograms
			for (int i_1 = 0; i_1 < flags.Length; i_1++)
			{
				if (flags[i_1] != null && flags[i_1].binnedLengthsCounter != null)
				{
					logger.Info("BinnedLengthsStatistics for column " + i_1);
					logger.Info(flags[i_1].binnedLengthsCounter.ToString());
				}
			}
			// print the training data in SVMlight format if desired
			if (ColumnDataClassifier.Flags.printSVMLightFormatTo != null)
			{
				PrintWriter pw = IOUtils.GetPrintWriter(ColumnDataClassifier.Flags.printSVMLightFormatTo, ColumnDataClassifier.Flags.encoding);
				train.PrintSVMLightFormat(pw);
				IOUtils.CloseIgnoringExceptions(pw);
				train.FeatureIndex().SaveToFilename(ColumnDataClassifier.Flags.printSVMLightFormatTo + ".featureIndex");
				train.LabelIndex().SaveToFilename(ColumnDataClassifier.Flags.printSVMLightFormatTo + ".labelIndex");
			}
			if (globalFlags.crossValidationFolds > 1)
			{
				CrossValidate(train, lineInfos);
			}
			if (globalFlags.exitAfterTrainingFeaturization)
			{
				return false;
			}
			// ENDS PROCESSING
			// build the classifier
			classifier = MakeClassifier(train);
			PrintClassifier(classifier);
			// serialize the classifier
			string serializeTo = ColumnDataClassifier.Flags.serializeTo;
			if (serializeTo != null)
			{
				SerializeClassifier(serializeTo);
			}
			return true;
		}

		/// <summary>Serialize a classifier to a file.</summary>
		/// <remarks>
		/// Serialize a classifier to a file. This writes to the file both a LinearClassifier and the
		/// Flags[] object from a ColumnDataClassifier. The latter captures all the information about
		/// how the ColumnDataClassifier is extracting features from data items for the classifier.
		/// </remarks>
		/// <param name="serializeTo">Filename to serialize the classifier to</param>
		/// <exception cref="System.IO.IOException">If any IO error</exception>
		public virtual void SerializeClassifier(string serializeTo)
		{
			logger.Info("Serializing classifier to " + serializeTo + "...");
			ObjectOutputStream oos = IOUtils.WriteStreamFromString(serializeTo);
			SerializeClassifier(oos);
			oos.Close();
			logger.Info("Done.");
		}

		/// <summary>Serialize a classifier to an ObjectOutputStream.</summary>
		/// <remarks>
		/// Serialize a classifier to an ObjectOutputStream. This writes to the file both a LinearClassifier and the
		/// Flags[] object from a ColumnDataClassifier. The latter captures all the information about
		/// how the ColumnDataClassifier is extracting features from data items for the classifier.
		/// </remarks>
		/// <param name="oos">ObjectOutputStream to serialize the classifier to</param>
		/// <exception cref="System.IO.IOException">If any IO error</exception>
		public virtual void SerializeClassifier(ObjectOutputStream oos)
		{
			oos.WriteObject(classifier);
			// Fiddle: Don't write a testFile to the serialized classifier.  It makes no sense and confuses people
			string testFile = globalFlags.testFile;
			globalFlags.testFile = null;
			oos.WriteObject(flags);
			globalFlags.testFile = testFile;
		}

		private void PrintClassifier(IClassifier classifier)
		{
			string classString;
			if (classifier is LinearClassifier<object, object>)
			{
				classString = ((LinearClassifier<object, object>)classifier).ToString(globalFlags.printClassifier, globalFlags.printClassifierParam);
			}
			else
			{
				classString = classifier.ToString();
			}
			if (ColumnDataClassifier.Flags.printTo != null)
			{
				PrintWriter fw = null;
				try
				{
					fw = IOUtils.GetPrintWriter(ColumnDataClassifier.Flags.printTo, ColumnDataClassifier.Flags.encoding);
					fw.Write(classString);
					fw.Println();
				}
				catch (IOException ioe)
				{
					logger.Warn(ioe);
				}
				finally
				{
					IOUtils.CloseIgnoringExceptions(fw);
				}
				logger.Info("Built classifier described in file " + ColumnDataClassifier.Flags.printTo);
			}
			else
			{
				logger.Info("Built this classifier: " + classString);
			}
		}

		/// <summary>Test and evaluate classifier on examples available in a file (or URL, classpath resource, etc.)</summary>
		/// <param name="testFile">The path, classpath resource or URL to load TSV data from</param>
		/// <returns>A Pair consisting of the accuracy (micro-averaged F1) and macro-averaged F1 for the dataset</returns>
		public virtual Pair<double, double> TestClassifier(string testFile)
		{
			if (globalFlags.printFeatures != null)
			{
				NewFeaturePrinter(globalFlags.printFeatures, "test", ColumnDataClassifier.Flags.encoding);
			}
			Pair<GeneralDataset<string, string>, IList<string[]>> testInfo = ReadTestExamples(testFile);
			GeneralDataset<string, string> test = testInfo.First();
			IList<string[]> lineInfos = testInfo.Second();
			Pair<double, double> pair = TestExamples(classifier, test, lineInfos);
			// ((LinearClassifier) classifier).dumpSorted();
			if (globalFlags.printFeatures != null)
			{
				CloseFeaturePrinter();
			}
			return pair;
		}

		/// <summary>Run cross-validation on a dataset, and return accuracy and macro-F1 scores.</summary>
		/// <remarks>
		/// Run cross-validation on a dataset, and return accuracy and macro-F1 scores.
		/// The number of folds is given by the crossValidationFolds property.
		/// </remarks>
		/// <param name="dataset">The dataset of examples to cross-validate on.</param>
		/// <param name="lineInfos">The String form of the items in the dataset. (Must be present.)</param>
		/// <returns>Accuracy and macro F1</returns>
		public virtual Pair<double, double> CrossValidate(GeneralDataset<string, string> dataset, IList<string[]> lineInfos)
		{
			int numFolds = globalFlags.crossValidationFolds;
			double accuracySum = 0.0;
			double macroF1Sum = 0.0;
			for (int fold = 0; fold < numFolds; fold++)
			{
				logger.Info(string.Empty);
				logger.Info("### Fold " + fold);
				Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> split = dataset.SplitOutFold(fold, numFolds);
				GeneralDataset<string, string> devTrain = split.First();
				GeneralDataset<string, string> devTest = split.Second();
				IClassifier<string, string> cl = MakeClassifier(devTrain);
				PrintClassifier(cl);
				int normalFoldSize = lineInfos.Count / numFolds;
				int start = normalFoldSize * fold;
				int end = start + normalFoldSize;
				if (fold == (numFolds - 1))
				{
					end = lineInfos.Count;
				}
				IList<string[]> devTestLineInfos = lineInfos.SubList(start, end);
				Pair<double, double> accuracies = TestExamples(cl, devTest, devTestLineInfos);
				accuracySum += accuracies.First();
				macroF1Sum += accuracies.Second();
			}
			double averageAccuracy = accuracySum / numFolds;
			double averageMacroF1 = macroF1Sum / numFolds;
			NumberFormat nf2 = new DecimalFormat("0.00000");
			logger.Info("Average accuracy/micro-averaged F1: " + nf2.Format(averageAccuracy));
			logger.Info("Average macro-averaged F1: " + nf2.Format(averageMacroF1));
			logger.Info(string.Empty);
			return new Pair<double, double>(averageAccuracy, averageMacroF1);
		}

		public virtual string ClassOf(IDatum<string, string> example)
		{
			if (classifier == null)
			{
				throw new Exception("Classifier is not initialized");
			}
			return classifier.ClassOf(example);
		}

		public virtual ICounter<string> ScoresOf(IDatum<string, string> example)
		{
			if (classifier == null)
			{
				throw new Exception("Classifier is not initialized");
			}
			return classifier.ScoresOf(example);
		}

		public virtual IClassifier<string, string> GetClassifier()
		{
			if (classifier == null)
			{
				throw new Exception("Classifier is not initialized");
			}
			return classifier;
		}

		[System.Serializable]
		internal class Flags
		{
			private const long serialVersionUID = -7076671761070232566L;

			internal bool useNGrams = false;

			internal bool usePrefixSuffixNGrams = false;

			internal bool lowercaseNGrams = false;

			internal bool lowercase;

			internal bool useSplitNGrams = false;

			internal bool useSplitPrefixSuffixNGrams = false;

			internal bool cacheNGrams = false;

			internal int maxNGramLeng = -1;

			internal int minNGramLeng = 2;

			internal string partialNGramRegexp = null;

			internal Pattern partialNGramPattern = null;

			internal bool useSum = false;

			internal double tolerance = 1e-4;

			internal string printFeatures = null;

			internal string printClassifier = null;

			internal int printClassifierParam = 100;

			internal bool exitAfterTrainingFeaturization = false;

			internal bool intern = false;

			internal Pattern splitWordsPattern = null;

			internal Pattern splitWordsTokenizerPattern = null;

			internal Pattern splitWordsIgnorePattern = Pattern.Compile(DefaultIgnoreRegexp);

			internal bool useSplitWords = false;

			internal bool useSplitWordPairs = false;

			internal bool useLowercaseSplitWordPairs = false;

			internal bool useSplitFirstLastWords = false;

			internal bool useLowercaseSplitWords = false;

			internal bool useLowercaseSplitFirstLastWords = false;

			internal int wordShape = WordShapeClassifier.Nowordshape;

			internal int splitWordShape = WordShapeClassifier.Nowordshape;

			internal bool useString = false;

			internal bool useClassFeature = false;

			internal int[] binnedLengths = null;

			internal TwoDimensionalCounter<string, string> binnedLengthsCounter = null;

			internal double[] binnedValues = null;

			internal TwoDimensionalCounter<string, string> binnedValuesCounter = null;

			internal double binnedValuesNaN = -1.0;

			internal bool isRealValued = false;

			public const string realValuedFeaturePrefix = "Value";

			internal bool logitTransform = false;

			internal bool logTransform = false;

			internal bool sqrtTransform = false;

			internal char[] countChars = null;

			internal int[] countCharsBins = new int[] { 0, 1 };

			internal ClassicCounter<string> biasedHyperplane = null;

			internal bool justify = false;

			internal bool featureFormat = false;

			internal bool significantColumnId = false;

			internal string useClassifierFactory;

			internal string classifierFactoryArgs;

			internal bool useNB = false;

			internal bool useQN = true;

			internal int QNsize = 15;

			internal int prior = (int)(LogPrior.LogPriorType.Quadratic);

			internal double sigma = 1.0;

			internal double epsilon = 0.01;

			internal int featureMinimumSupport = 0;

			internal int displayedColumn = 1;

			internal int groupingColumn = -1;

			internal int rankingScoreColumn = -1;

			internal string rankingAccuracyClass = null;

			internal int goldAnswerColumn = 0;

			internal bool biased;

			internal bool useSplitWordNGrams = false;

			internal int maxWordNGramLeng = -1;

			internal int minWordNGramLeng = 1;

			internal bool useBinary = false;

			internal double l1reg = 0.0;

			internal string wordNGramBoundaryRegexp;

			internal Pattern wordNGramBoundaryPattern;

			internal bool useAdaptL1 = false;

			internal int limitFeatures = 0;

			internal string limitFeaturesLabels = null;

			internal double l1regmin = 0.0;

			internal double l1regmax = 500.0;

			internal double featureWeightThreshold = 0;

			internal string testFile = null;

			internal string loadClassifier = null;

			internal static string trainFile = null;

			internal static string serializeTo = null;

			internal static string printTo = null;

			internal static bool trainFromSVMLight = false;

			internal static bool testFromSVMLight = false;

			internal static string encoding = null;

			internal static string printSVMLightFormatTo;

			internal static bool displayAllAnswers = false;

			internal bool usesRealValues;

			internal bool filename;

			internal bool useAllSplitWordPairs;

			internal bool useAllSplitWordTriples;

			internal bool showTokenization = false;

			internal int crossValidationFolds = -1;

			internal bool shuffleTrainingData = false;

			internal long shuffleSeed = 0;

			internal static bool csvInput = false;

			internal static ColumnDataClassifier.InputFormat inputFormat = ColumnDataClassifier.InputFormat.Plain;

			internal bool splitWordsWithPTBTokenizer = false;

			internal bool splitWordCount;

			internal bool logSplitWordCount;

			internal int[] binnedSplitWordCounts;

			internal IDictionary<string, float[]> wordVectors;

			internal static string csvOutput = null;

			internal bool printCrossValidationDecisions = false;

			internal bool verboseOptimization = false;

			// PLEASE ADD NEW FLAGS AT THE END OF THIS CLASS (SO AS TO NOT UNNECESSARILY BREAK SERIALIZED CLASSIFIERS)
			//true iff this feature is real valued
			//prefix to add before column number for denoting real valued features
			// = 2nd column of data file! (Because we count from 0.)
			// this one would be better off static (we avoid serializing it)
			// this one could also be static
			// these are static because we don't want them serialized
			//train file is in SVMLight format
			//test file is in SVMLight format
			// Distinguishes whether this file has real valued features or if the more efficient non-RVF representation can be used.
			// This is set as a summary flag in globalFeatures based on whether anything uses real values.
			//train and test files are in csv format
			public override string ToString()
			{
				return "Flags[" + "goldAnswerColumn = " + goldAnswerColumn + ", useString = " + useString + ", useNGrams = " + useNGrams + ", usePrefixSuffixNGrams = " + usePrefixSuffixNGrams + ']';
			}
		}
		// end class Flags
	}
}
