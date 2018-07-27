// NERFeatureFactory -- features for a probabilistic Named Entity Recognizer
// Copyright (c) 2002-2008 Leland Stanford Junior University
// Additional features (c) 2003 The University of Edinburgh
//
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
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/CRF-NER.html
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Features for Named Entity Recognition.</summary>
	/// <remarks>
	/// Features for Named Entity Recognition.  The code here creates the features
	/// by processing Lists of CoreLabels.
	/// Look at
	/// <see cref="Edu.Stanford.Nlp.Sequences.SeqClassifierFlags"/>
	/// to see where the flags are set for
	/// what options to use for what flags.
	/// <p>
	/// To add a new feature extractor, you should do the following:
	/// <ol>
	/// <li>Add a variable (boolean, int, String, etc. as appropriate) to
	/// SeqClassifierFlags to mark if the new extractor is turned on or
	/// its value, etc. Add it at the <i>bottom</i> of the list of variables
	/// currently in the class (this avoids problems with older serialized
	/// files breaking). Make the default value of the variable false/null/0
	/// (this is again for backwards compatibility).</li>
	/// <li>Add a clause to the big if/then/else of setProperties(Properties) in
	/// SeqClassifierFlags.  Unless it is a macro option, make the option name
	/// the same as the variable name used in step 1.</li>
	/// <li>Add code to NERFeatureFactory for this feature. First decide which
	/// classes (hidden states) are involved in the feature.  If only the
	/// current class, you add the feature extractor to the
	/// <c>featuresC</c>
	/// code, if both the current and previous class,
	/// then
	/// <c>featuresCpC</c>
	/// , etc.</li>
	/// </ol>
	/// <p>
	/// Parameters can be defined using a Properties file
	/// (specified on the command-line with
	/// <c>-prop</c>
	/// <i>propFile</i>),
	/// or directly on the command line. The following properties are recognized:
	/// <table border="1">
	/// <tr><td><b>Property Name</b></td><td><b>Type</b></td><td><b>Default Value</b></td><td><b>Description</b></td></tr>
	/// <tr><td> loadClassifier </td><td>String</td><td>n/a</td><td>Path to serialized classifier to load</td></tr>
	/// <tr><td> loadAuxClassifier </td><td>String</td><td>n/a</td><td>Path to auxiliary classifier to load.</td></tr>
	/// <tr><td> serializeTo</td><td>String</td><td>n/a</td><td>Path to serialize classifier to</td></tr>
	/// <tr><td> trainFile</td><td>String</td><td>n/a</td><td>Path of file to use as training data</td></tr>
	/// <tr><td> testFile</td><td>String</td><td>n/a</td><td>Path of file to use as training data</td></tr>
	/// <tr><td> map</td><td>String</td><td>see below</td><td>This applies at training time or if testing on tab-separated column data.  It says what is in each column.  It doesn't apply when running on plain text data.  The simplest scenario for training is having words and classes in two column.  word=0,answer=1 is the default if conllNoTags is specified; otherwise word=0,tag=1,answer=2 is the default.  But you can add other columns, such as for a part-of-speech tag, presences in a lexicon, etc.  That would only be useful at runtime if you have part-of-speech information or whatever available and are passing it in with the tokens (that is, you can pass to classify CoreLabel tokens with additional fields stored in them).</td></tr>
	/// <tr><td> useWord</td><td>boolean</td><td>true</td><td>Gives you feature for w</td></tr>
	/// <tr><td> useBinnedLength</td><td>String</td><td>null</td><td>If non-null, treat as a sequence of comma separated integer bounds, where items above the previous bound up to the next bound are binned Len-<i>range</i></td></tr>
	/// <tr><td> useNGrams</td><td>boolean</td><td>false</td><td>Make features from letter n-grams, i.e., substrings of the word</td></tr>
	/// <tr><td> lowercaseNGrams</td><td>boolean</td><td>false</td><td>Make features from letter n-grams only lowercase</td></tr>
	/// <tr><td> dehyphenateNGrams</td><td>boolean</td><td>false</td><td>Remove hyphens before making features from letter n-grams</td></tr>
	/// <tr><td> conjoinShapeNGrams</td><td>boolean</td><td>false</td><td>Conjoin word shape and n-gram features</td></tr>
	/// <tr><td> useNeighborNGrams</td><td>boolean</td><td>false</td><td>Use letter n-grams for the previous and current words in the CpC clique.  This feature helps languages such as Chinese, but not so much for English</td></tr>
	/// <tr><td> useMoreNeighborNGrams</td><td>boolean</td><td>false</td><td>Use letter n-grams for the previous and next words in the C clique.  This feature helps languages such as Chinese, but not so much for English</td></tr>
	/// <tr><td> usePrev</td><td>boolean</td><td>false</td><td>Gives you feature for (pw,c), and together with other options enables other previous features, such as (pt,c) [with useTags)</td></tr>
	/// <tr><td> useNext</td><td>boolean</td><td>false</td><td>Gives you feature for (nw,c), and together with other options enables other next features, such as (nt,c) [with useTags)</td></tr>
	/// <tr><td> useTags</td><td>boolean</td><td>false</td><td>Gives you features for (t,c), (pt,c) [if usePrev], (nt,c) [if useNext]</td></tr>
	/// <tr><td> useWordPairs</td><td>boolean</td><td>false</td><td>Gives you
	/// features for (pw, w, c) and (w, nw, c)</td></tr>
	/// <tr><td> useGazettes</td><td>boolean</td><td>false</td><td>If true, use gazette features (defined by other flags)</td></tr>
	/// <tr><td> gazette</td><td>String</td><td>null</td><td>The value can be one or more filenames (names separated by a comma, semicolon or space).
	/// If provided gazettes are loaded from these files.  Each line should be an entity class name, followed by whitespace followed by an entity (which might be a phrase of several tokens with a single space between words).
	/// Giving this property turns on useGazettes, so you normally don't need to specify it (but can use it to turn off gazettes specified in a properties file).</td></tr>
	/// <tr><td> sloppyGazette</td><td>boolean</td><td>false</td><td>If true, a gazette feature fires when any token of a gazette entry matches</td></tr>
	/// <tr><td> cleanGazette</td><td>boolean</td><td>false</td><td>If true, a gazette feature fires when all tokens of a gazette entry match</td></tr>
	/// <tr><td> wordShape</td><td>String</td><td>none</td><td>Either "none" for no wordShape use, or the name of a word shape function recognized by
	/// <see cref="Edu.Stanford.Nlp.Process.WordShapeClassifier.LookupShaper(string)"/>
	/// </td></tr>
	/// <tr><td> useSequences</td><td>boolean</td><td>true</td><td>Does not use any class combination features if this is false</td></tr>
	/// <tr><td> usePrevSequences</td><td>boolean</td><td>false</td><td>Does not use any class combination features using previous classes if this is false</td></tr>
	/// <tr><td> useNextSequences</td><td>boolean</td><td>false</td><td>Does not use any class combination features using next classes if this is false</td></tr>
	/// <tr><td> useLongSequences</td><td>boolean</td><td>false</td><td>Use plain higher-order state sequences out to minimum of length or maxLeft</td></tr>
	/// <tr><td> useBoundarySequences</td><td>boolean</td><td>false</td><td>Use extra second order class sequence features when previous is CoNLL boundary, so entity knows it can span boundary.</td></tr>
	/// <tr><td> useTaggySequences</td><td>boolean</td><td>false</td><td>Use first, second, and third order class and tag sequence interaction features</td></tr>
	/// <tr><td> useExtraTaggySequences</td><td>boolean</td><td>false</td><td>Add in sequences of tags with just current class features</td></tr>
	/// <tr><td> useTaggySequencesShapeInteraction</td><td>boolean</td><td>false</td><td>Add in terms that join sequences of 2 or 3 tags with the current shape</td></tr>
	/// <tr><td> strictlyFirstOrder</td><td>boolean</td><td>false</td><td>As an override to whatever other options are in effect, deletes all features other than C and CpC clique features when building the classifier</td></tr>
	/// <tr><td> entitySubclassification</td><td>String</td><td>"IO"</td><td>If
	/// set, convert the labeling of classes (but not  the background) into
	/// one of several alternate encodings (IO, IOB1, IOB2, IOE1, IOE2, SBIEO, with
	/// a S(ingle), B(eginning),
	/// E(nding), I(nside) 4-way classification for each class.  By default, we
	/// either do no re-encoding, or the CoNLLDocumentIteratorFactory does a
	/// lossy encoding as IO.  Note that this is all CoNLL-specific, and depends on
	/// their way of prefix encoding classes, and is only implemented by
	/// the CoNLLDocumentIteratorFactory. </td></tr>
	/// <tr><td> useSum</td><td>boolean</td><td>false</td><td></td></tr>
	/// <tr><td> tolerance</td><td>double</td><td>1e-4</td><td>Convergence tolerance in optimization</td></tr>
	/// <tr><td> printFeatures</td><td>String</td><td>null</td><td>print out all the features generated by the classifier for a dataset to a file based on this name (starting with "features-", suffixed "-1" and "-2" for train and test). This simply prints the feature names, one per line.</td></tr>
	/// <tr><td> printFeaturesUpto</td><td>int</td><td>-1</td><td>Print out features for only the first this many datums, if the value is positive. </td></tr>
	/// <tr><td> useSymTags</td><td>boolean</td><td>false</td><td>Gives you
	/// features (pt, t, nt, c), (t, nt, c), (pt, t, c)</td></tr>
	/// <tr><td> useSymWordPairs</td><td>boolean</td><td>false</td><td>Gives you
	/// features (pw, nw, c)</td></tr>
	/// <tr><td> printClassifier</td><td>String</td><td>null</td><td>Style in which to print the classifier. One of: HighWeight, HighMagnitude, Collection, AllWeights, WeightHistogram</td></tr>
	/// <tr><td> printClassifierParam</td><td>int</td><td>100</td><td>A parameter
	/// to the printing style, which may give, for example the number of parameters
	/// to print</td></tr>
	/// <tr><td> intern</td><td>boolean</td><td>false</td><td>If true,
	/// (String) intern read in data and classes and feature (pre-)names such
	/// as substring features</td></tr>
	/// <tr><td> intern2</td><td>boolean</td><td>false</td><td>If true, intern all (final) feature names (if only current word and ngram features are used, these will already have been interned by intern, and this is an unnecessary no-op)</td></tr>
	/// <tr><td> cacheNGrams</td><td>boolean</td><td>false</td><td>If true,
	/// record the NGram features that correspond to a String (under the current
	/// option settings) and reuse rather than recalculating if the String is seen
	/// again.</td></tr>
	/// <tr><td> selfTest</td><td>boolean</td><td>false</td><td></td></tr>
	/// <tr><td> noMidNGrams</td><td>boolean</td><td>false</td><td>Do not include character n-gram features for n-grams that contain neither the beginning or end of the word</td></tr>
	/// <tr><td> maxNGramLeng</td><td>int</td><td>-1</td><td>If this number is
	/// positive, n-grams above this size will not be used in the model</td></tr>
	/// <tr><td> useReverse</td><td>boolean</td><td>false</td><td></td></tr>
	/// <tr><td> retainEntitySubclassification</td><td>boolean</td><td>false</td><td>If true, rather than undoing a recoding of entity tag subtypes (such as BIO variants), just leave them in the output.</td></tr>
	/// <tr><td> useLemmas</td><td>boolean</td><td>false</td><td>Include the lemma of a word as a feature.</td></tr>
	/// <tr><td> usePrevNextLemmas</td><td>boolean</td><td>false</td><td>Include the previous/next lemma of a word as a feature.</td></tr>
	/// <tr><td> useLemmaAsWord</td><td>boolean</td><td>false</td><td>Include the lemma of a word as a feature.</td></tr>
	/// <tr><td> normalizeTerms</td><td>boolean</td><td>false</td><td>If this is true, some words are normalized: day and month names are lowercased (as for normalizeTimex) and some British spellings are mapped to American English spellings (e.g., -our/-or, etc.).</td></tr>
	/// <tr><td> normalizeTimex</td><td>boolean</td><td>false</td><td>If this is true, capitalization of day and month names is normalized to lowercase</td></tr>
	/// <tr><td> useNB</td><td>boolean</td><td>false</td><td></td></tr>
	/// <tr><td> useTypeSeqs</td><td>boolean</td><td>false</td><td>Use basic zeroeth order word shape features.</td></tr>
	/// <tr><td> useTypeSeqs2</td><td>boolean</td><td>false</td><td>Add additional first and second order word shape features</td></tr>
	/// <tr><td> useTypeSeqs3</td><td>boolean</td><td>false</td><td>Adds one more first order shape sequence</td></tr>
	/// <tr><td> useDisjunctive</td><td>boolean</td><td>false</td><td>Include in features giving disjunctions of words anywhere in the left or right disjunctionWidth words (preserving direction but not position)</td></tr>
	/// <tr><td> disjunctionWidth</td><td>int</td><td>4</td><td>The number of words on each side of the current word that are included in the disjunction features</td></tr>
	/// <tr><td> useDisjunctiveShapeInteraction</td><td>boolean</td><td>false</td><td>Include in features giving disjunctions of words anywhere in the left or right disjunctionWidth words (preserving direction but not position) interacting with the word shape of the current word</td></tr>
	/// <tr><td> useWideDisjunctive</td><td>boolean</td><td>false</td><td>Include in features giving disjunctions of words anywhere in the left or right wideDisjunctionWidth words (preserving direction but not position)</td></tr>
	/// <tr><td> wideDisjunctionWidth</td><td>int</td><td>4</td><td>The number of words on each side of the current word that are included in the disjunction features</td></tr>
	/// <tr><td> usePosition</td><td>boolean</td><td>false</td><td>Use combination of position in sentence and class as a feature</td></tr>
	/// <tr><td> useBeginSent</td><td>boolean</td><td>false</td><td>Use combination of initial position in sentence and class (and word shape) as a feature.  (Doesn't seem to help.)</td></tr>
	/// <tr><td> useDisjShape</td><td>boolean</td><td>false</td><td>Include features giving disjunctions of word shapes anywhere in the left or right disjunctionWidth words (preserving direction but not position)</td></tr>
	/// <tr><td> useClassFeature</td><td>boolean</td><td>false</td><td>Include a feature for the class (as a class marginal).  Puts a prior on the classes which is equivalent to how often the feature appeared in the training data. This is the same thing as having a bias vector or having an always-on feature in a model.</td></tr>
	/// <tr><td> useShapeConjunctions</td><td>boolean</td><td>false</td><td>Conjoin shape with tag or position</td></tr>
	/// <tr><td> useWordTag</td><td>boolean</td><td>false</td><td>Include word and tag pair features</td></tr>
	/// <tr><td> useLastRealWord</td><td>boolean</td><td>false</td><td>Iff the prev word is of length 3 or less, add an extra feature that combines the word two back and the current word's shape. <i>Weird!</i></td></tr>
	/// <tr><td> useNextRealWord</td><td>boolean</td><td>false</td><td>Iff the next word is of length 3 or less, add an extra feature that combines the word after next and the current word's shape. <i>Weird!</i></td></tr>
	/// <tr><td> useTitle</td><td>boolean</td><td>false</td><td>Match a word against a list of name titles (Mr, Mrs, etc.). Doesn't really seem to help.</td></tr>
	/// <tr><td> useTitle2</td><td>boolean</td><td>false</td><td>Match a word against a better list of English name titles (Mr, Mrs, etc.). Still doesn't really seem to help.</td></tr>
	/// <tr><td> useDistSim</td><td>boolean</td><td>false</td><td>Load a file of distributional similarity classes (specified by
	/// <c>distSimLexicon</c>
	/// ) and use it for features</td></tr>
	/// <tr><td> distSimLexicon</td><td>String</td><td></td><td>The file to be loaded for distsim classes.</td></tr>
	/// <tr><td> distSimFileFormat</td><td>String</td><td>alexclark</td><td>Files should be formatted as tab separated rows where each row is a word/class pair.  alexclark=word first, terrykoo=class first</td></tr>
	/// <tr><td> useOccurrencePatterns</td><td>boolean</td><td>false</td><td>This is a very engineered feature designed to capture multiple references to names.  If the current word isn't capitalized, followed by a non-capitalized word, and preceded by a word with alphabetic characters, it returns NO-OCCURRENCE-PATTERN.  Otherwise, if the previous word is a capitalized NNP, then if in the next 150 words you find this PW-W sequence, you get XY-NEXT-OCCURRENCE-XY, else if you find W you get XY-NEXT-OCCURRENCE-Y.  Similarly for backwards and XY-PREV-OCCURRENCE-XY and XY-PREV-OCCURRENCE-Y.  Else (if the previous word isn't a capitalized NNP), under analogous rules you get one or more of X-NEXT-OCCURRENCE-YX, X-NEXT-OCCURRENCE-XY, X-NEXT-OCCURRENCE-X, X-PREV-OCCURRENCE-YX, X-PREV-OCCURRENCE-XY, X-PREV-OCCURRENCE-X.</td></tr>
	/// <tr><td> useTypeySequences</td><td>boolean</td><td>false</td><td>Some first order word shape patterns.</td></tr>
	/// <tr><td> useGenericFeatures</td><td>boolean</td><td>false</td><td>If true, any features you include in the map will be incorporated into the model with values equal to those given in the file; values are treated as strings unless you use the "realValued" option (described below)</td></tr>
	/// <tr><td> justify</td><td>boolean</td><td>false</td><td>Print out all
	/// feature/class pairs and their weight, and then for each input data
	/// point, print justification (weights) for active features. Only implemented for CMMClassifier.</td></tr>
	/// <tr><td> normalize</td><td>boolean</td><td>false</td><td>For the CMMClassifier (only) if this is true then the Scorer normalizes scores as probabilities.</td></tr>
	/// <tr><td> useHuber</td><td>boolean</td><td>false</td><td>Use a Huber loss prior rather than the default quadratic loss.</td></tr>
	/// <tr><td> useQuartic</td><td>boolean</td><td>false</td><td>Use a Quartic prior rather than the default quadratic loss.</td></tr>
	/// <tr><td> sigma</td><td>double</td><td>1.0</td><td></td></tr>
	/// <tr><td> epsilon</td><td>double</td><td>0.01</td><td>Used only as a parameter in the Huber loss: this is the distance from 0 at which the loss changes from quadratic to linear</td></tr>
	/// <tr><td> beamSize</td><td>int</td><td>30</td><td></td></tr>
	/// <tr><td> maxLeft</td><td>int</td><td>2</td><td>The number of things to the left that have to be cached to run the Viterbi algorithm: the maximum context of class features used.</td></tr>
	/// <tr><td> maxRight</td><td>int</td><td>2</td><td>The number of things to the right that have to be cached to run the Viterbi algorithm: the maximum context of class features used.  The maximum possible clique size to use is (maxLeft + maxRight + 1)</td></tr>
	/// <tr><td> dontExtendTaggy</td><td>boolean</td><td>false</td><td>Don't extend the range of useTaggySequences when maxLeft is increased.</td></tr>
	/// <tr><td> numFolds </td><td>int</td><td>1</td><td>The number of folds to use for cross-validation. CURRENTLY NOT IMPLEMENTED.</td></tr>
	/// <tr><td> startFold </td><td>int</td><td>1</td><td>The starting fold to run. CURRENTLY NOT IMPLEMENTED.</td></tr>
	/// <tr><td> endFold </td><td>int</td><td>1</td><td>The last fold to run. CURRENTLY NOT IMPLEMENTED.</td></tr>
	/// <tr><td> mergeTags </td><td>boolean</td><td>false</td><td>Whether to merge B- and I- tags.</td></tr>
	/// <tr><td> splitDocuments</td><td>boolean</td><td>true</td><td>Whether or not to split the data into separate documents for training/testing</td></tr>
	/// <tr><td> maxDocSize</td><td>int</td><td>10000</td><td>If this number is greater than 0, attempt to split documents bigger than this value into multiple documents at sentence boundaries during testing; otherwise do nothing.</td></tr>
	/// </table>
	/// <p>
	/// Note: flags/properties overwrite left to right.  That is, the parameter
	/// setting specified <i>last</i> is the one used.
	/// </p><p>
	/// <pre>
	/// DOCUMENTATION ON FEATURE TEMPLATES
	/// <br />
	/// w = word
	/// t = tag
	/// p = position (word index in sentence)
	/// c = class
	/// p = paren
	/// g = gazette
	/// a = abbrev
	/// s = shape
	/// r = regent (dependency governor)
	/// h = head word of phrase
	/// n(w) = ngrams from w
	/// g(w) = gazette entries containing w
	/// l(w) = length of w
	/// o(...) = occurrence patterns of words
	/// <br />
	/// useReverse reverses meaning of prev, next everywhere below (on in macro)
	/// <br />
	/// "Prolog" booleans: , = AND and ; = OR
	/// <br />
	/// Mac: Y = turned on in -macro,
	/// + = additional positive things relative to -macro for CoNLL NERFeatureFactory
	/// (perhaps none...)
	/// - = Known negative for CoNLL NERFeatureFactory relative to -macro
	/// <br />
	/// Bio: + = additional things that are positive for BioCreative
	/// - = things negative relative to -macro
	/// <br />
	/// HighMagnitude: There are no (0) to a few (+) to many (+++) high weight
	/// features of this template. (? = not used in goodCoNLL, but usually = 0)
	/// <br />
	/// Feature              Mac Bio CRFFlags                   HighMagnitude
	/// ---------------------------------------------------------------------
	/// w,c                    Y     useWord                    0 (useWord is almost useless with unlimited ngram features, but helps a fraction in goodCoNLL, if only because of prior fiddling
	/// p,c                          usePosition                ?
	/// p=0,c                        useBeginSent               ?
	/// p=0,s,c                      useBeginSent               ?
	/// t,c                    Y     useTags                    ++
	/// pw,c                   Y     usePrev                    +
	/// pt,c                   Y     usePrev,useTags            0
	/// nw,c                   Y     useNext                    ++
	/// nt,c                   Y     useNext,useTags            0
	/// pw,w,c                 Y     useWordPairs               +
	/// w,nw,c                 Y     useWordPairs               +
	/// pt,t,nt,c                    useSymTags                 ?
	/// t,nt,c                       useSymTags                 ?
	/// pt,t,c                       useSymTags                 ?
	/// pw,nw,c                      useSymWordPairs            ?
	/// <br />
	/// pc,c                   Y     usePrev,useSequences,usePrevSequences   +++
	/// pc,w,c                 Y     usePrev,useSequences,usePrevSequences   0
	/// nc,c                         useNext,useSequences,useNextSequences   ?
	/// w,nc,c                       useNext,useSequences,useNextSequences   ?
	/// pc,nc,c                      useNext,usePrev,useSequences,usePrevSequences,useNextSequences  ?
	/// w,pc,nc,c                    useNext,usePrev,useSequences,usePrevSequences,useNextSequences   ?
	/// <br />
	/// (pw;p2w;p3w;p4w),c        +  useDisjunctive  (out to disjunctionWidth now)   +++
	/// (nw;n2w;n3w;n4w),c        +  useDisjunctive  (out to disjunctionWidth now)   ++++
	/// (pw;p2w;p3w;p4w),s,c      +  useDisjunctiveShapeInteraction          ?
	/// (nw;n2w;n3w;n4w),s,c      +  useDisjunctiveShapeInteraction          ?
	/// (pw;p2w;p3w;p4w),c        +  useWideDisjunctive (to wideDisjunctionWidth)   ?
	/// (nw;n2w;n3w;n4w),c        +  useWideDisjunctive (to wideDisjunctionWidth)   ?
	/// (ps;p2s;p3s;p4s),c           useDisjShape  (out to disjunctionWidth now)   ?
	/// (ns;n2s;n3s;n4s),c           useDisjShape  (out to disjunctionWidth now)   ?
	/// <br />
	/// pt,pc,t,c              Y     useTaggySequences                        +
	/// p2t,p2c,pt,pc,t,c      Y     useTaggySequences,maxLeft&gt;=2          +
	/// p3t,p3c,p2t,p2c,pt,pc,t,c Y  useTaggySequences,maxLeft&gt;=3,!dontExtendTaggy   ?
	/// p2c,pc,c               Y     useLongSequences                         ++
	/// p3c,p2c,pc,c           Y     useLongSequences,maxLeft&gt;=3           ?
	/// p4c,p3c,p2c,pc,c       Y     useLongSequences,maxLeft&gt;=4           ?
	/// p2c,pc,c,pw=BOUNDARY         useBoundarySequences                     0 (OK, but!)
	/// <br />
	/// p2t,pt,t,c             -     useExtraTaggySequences                   ?
	/// p3t,p2t,pt,t,c         -     useExtraTaggySequences                   ?
	/// <br />
	/// p2t,pt,t,s,p2c,pc,c    -     useTaggySequencesShapeInteraction        ?
	/// p3t,p2t,pt,t,s,p3c,p2c,pc,c  useTaggySequencesShapeInteraction        ?
	/// <br />
	/// s,pc,c                 Y     useTypeySequences                        ++
	/// ns,pc,c                Y     useTypeySequences  // error for ps? not? 0
	/// ps,pc,s,c              Y     useTypeySequences                        0
	/// // p2s,p2c,ps,pc,s,c      Y     useTypeySequences,maxLeft&gt;=2 // duplicated a useTypeSeqs2 feature
	/// <br />
	/// n(w),c                 Y     useNGrams (noMidNGrams, MaxNGramLeng, lowercaseNGrams, dehyphenateNGrams)   +++
	/// n(w),s,c                     useNGrams,conjoinShapeNGrams             ?
	/// <br />
	/// g,c                        + useGazFeatures   // test refining this?   ?
	/// pg,pc,c                    + useGazFeatures                           ?
	/// ng,c                       + useGazFeatures                           ?
	/// // pg,g,c                    useGazFeatures                           ?
	/// // pg,g,ng,c                 useGazFeatures                           ?
	/// // p2g,p2c,pg,pc,g,c         useGazFeatures                           ?
	/// g,w,c                        useMoreGazFeatures                       ?
	/// pg,pc,g,c                    useMoreGazFeatures                       ?
	/// g,ng,c                       useMoreGazFeatures                       ?
	/// <br />
	/// g(w),c                       useGazette,sloppyGazette (contains same word)   ?
	/// g(w),[pw,nw,...],c           useGazette,cleanGazette (entire entry matches)   ?
	/// <br />
	/// s,c                    Y     wordShape &gt;= 0                       +++
	/// ps,c                   Y     wordShape &gt;= 0,useTypeSeqs           +
	/// ns,c                   Y     wordShape &gt;= 0,useTypeSeqs           +
	/// pw,s,c                 Y     wordShape &gt;= 0,useTypeSeqs           +
	/// s,nw,c                 Y     wordShape &gt;= 0,useTypeSeqs           +
	/// ps,s,c                 Y     wordShape &gt;= 0,useTypeSeqs           0
	/// s,ns,c                 Y     wordShape &gt;= 0,useTypeSeqs           ++
	/// ps,s,ns,c              Y     wordShape &gt;= 0,useTypeSeqs           ++
	/// pc,ps,s,c              Y     wordShape &gt;= 0,useTypeSeqs,useTypeSeqs2   0
	/// p2c,p2s,pc,ps,s,c      Y     wordShape &gt;= 0,useTypeSeqs,useTypeSeqs2,maxLeft&gt;=2   +++
	/// pc,ps,s,ns,c                 wordShape &gt;= 0,useTypeSeqs,useTypeSeqs3   ?
	/// <br />
	/// p2w,s,c if l(pw) &lt;= 3 Y     useLastRealWord // weird features, but work   0
	/// n2w,s,c if l(nw) &lt;= 3 Y     useNextRealWord                        ++
	/// o(pw,w,nw),c           Y     useOccurrencePatterns // don't fully grok but has to do with capitalized name patterns   ++
	/// <br />
	/// a,c                          useAbbr;useMinimalAbbr
	/// pa,a,c                       useAbbr
	/// a,na,c                       useAbbr
	/// pa,a,na,c                    useAbbr
	/// pa,pc,a,c                    useAbbr;useMinimalAbbr
	/// p2a,p2c,pa,pc,a              useAbbr
	/// w,a,c                        useMinimalAbbr
	/// p2a,p2c,a,c                  useMinimalAbbr
	/// <br />
	/// RESTR. w,(pw,pc;p2w,p2c;p3w,p3c;p4w,p4c)   + useParenMatching,maxLeft&gt;=n
	/// <br />
	/// c                          - useClassFeature
	/// <br />
	/// p,s,c                      - useShapeConjunctions
	/// t,s,c                      - useShapeConjunctions
	/// <br />
	/// w,t,c                      + useWordTag                      ?
	/// w,pt,c                     + useWordTag                      ?
	/// w,nt,c                     + useWordTag                      ?
	/// <br />
	/// r,c                          useNPGovernor (only for baseNP words)
	/// r,t,c                        useNPGovernor (only for baseNP words)
	/// h,c                          useNPHead (only for baseNP words)
	/// h,t,c                        useNPHead (only for baseNP words)
	/// <br />
	/// </pre>
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	/// <author>Shipra Dingare</author>
	/// <author>Huy Nguyen</author>
	/// <author>Mengqiu Wang</author>
	[System.Serializable]
	public class NERFeatureFactory<In> : FeatureFactory<In>
		where In : CoreLabel
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.NERFeatureFactory));

		private const long serialVersionUID = -2329726064739185544L;

		public NERFeatureFactory()
			: base()
		{
		}

		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
			InitGazette();
			if (flags.useDistSim)
			{
				InitLexicon(flags);
			}
		}

		/// <summary>Extracts all the features from the input data at a certain index.</summary>
		/// <param name="cInfo">The complete data set as a List of WordInfo</param>
		/// <param name="loc">The index at which to extract features.</param>
		public override ICollection<string> GetCliqueFeatures(PaddedList<In> cInfo, int loc, Clique clique)
		{
			ICollection<string> features = Generics.NewHashSet();
			string domain = cInfo[0].Get(typeof(CoreAnnotations.DomainAnnotation));
			bool doFE = domain != null;
			//    log.info(doFE+"\t"+domain);
			// there are two special cases below, because 2 cliques have 2 names
			ICollection<string> c;
			string suffix;
			if (clique == cliqueC)
			{
				//200710: tried making this clique null; didn't improve performance (rafferty)
				c = FeaturesC(cInfo, loc);
				suffix = "C";
			}
			else
			{
				if (clique == cliqueCpC)
				{
					c = FeaturesCpC(cInfo, loc);
					suffix = "CpC";
					AddAllInterningAndSuffixing(features, c, suffix);
					if (doFE)
					{
						AddAllInterningAndSuffixing(features, c, domain + '-' + suffix);
					}
					c = FeaturesCnC(cInfo, loc - 1);
					suffix = "CnC";
				}
				else
				{
					if (clique == cliqueCp2C)
					{
						c = FeaturesCp2C(cInfo, loc);
						suffix = "Cp2C";
					}
					else
					{
						if (clique == cliqueCp3C)
						{
							c = FeaturesCp3C(cInfo, loc);
							suffix = "Cp3C";
						}
						else
						{
							if (clique == cliqueCp4C)
							{
								c = FeaturesCp4C(cInfo, loc);
								suffix = "Cp4C";
							}
							else
							{
								if (clique == cliqueCp5C)
								{
									c = FeaturesCp5C(cInfo, loc);
									suffix = "Cp5C";
								}
								else
								{
									if (clique == cliqueCpCp2C)
									{
										c = FeaturesCpCp2C(cInfo, loc);
										suffix = "CpCp2C";
										AddAllInterningAndSuffixing(features, c, suffix);
										if (doFE)
										{
											AddAllInterningAndSuffixing(features, c, domain + '-' + suffix);
										}
										c = FeaturesCpCnC(cInfo, loc - 1);
										suffix = "CpCnC";
									}
									else
									{
										if (clique == cliqueCpCp2Cp3C)
										{
											c = FeaturesCpCp2Cp3C(cInfo, loc);
											suffix = "CpCp2Cp3C";
										}
										else
										{
											if (clique == cliqueCpCp2Cp3Cp4C)
											{
												c = FeaturesCpCp2Cp3Cp4C(cInfo, loc);
												suffix = "CpCp2Cp3Cp4C";
											}
											else
											{
												throw new ArgumentException("Unknown clique: " + clique);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			AddAllInterningAndSuffixing(features, c, suffix);
			if (doFE)
			{
				AddAllInterningAndSuffixing(features, c, domain + '-' + suffix);
			}
			// log.info(StringUtils.join(features,"\n")+"\n");
			return features;
		}

		private IDictionary<string, string> lexicon;

		// TODO: when breaking serialization, it seems like it would be better to
		// move the lexicon into (Abstract)SequenceClassifier and to do this
		// annotation as part of the ObjectBankWrapper.  But note that it is
		// serialized in this object currently and it would then need to be
		// serialized elsewhere or loaded each time
		private void InitLexicon(SeqClassifierFlags flags)
		{
			if (flags.distSimLexicon == null)
			{
				return;
			}
			if (lexicon != null)
			{
				return;
			}
			Timing timing = new Timing();
			lexicon = Generics.NewHashMap();
			bool terryKoo = "terryKoo".Equals(flags.distSimFileFormat);
			Pattern p = Pattern.Compile(terryKoo ? "\\t" : "\\s+");
			foreach (string line in ObjectBank.GetLineIterator(flags.distSimLexicon, flags.inputEncoding))
			{
				string word;
				string wordClass;
				if (terryKoo)
				{
					string[] bits = p.Split(line);
					word = bits[1];
					wordClass = bits[0];
					if (flags.distSimMaxBits > 0 && wordClass.Length > flags.distSimMaxBits)
					{
						wordClass = Sharpen.Runtime.Substring(wordClass, 0, flags.distSimMaxBits);
					}
				}
				else
				{
					// "alexClark"
					string[] bits = p.Split(line);
					word = bits[0];
					wordClass = bits[1];
				}
				if (!flags.casedDistSim)
				{
					word = word.ToLower();
				}
				if (flags.numberEquivalenceDistSim)
				{
					word = WordShapeClassifier.WordShape(word, WordShapeClassifier.Wordshapedigits);
				}
				lexicon[word] = wordClass;
			}
			timing.Done(log, "Loading distsim lexicon from " + flags.distSimLexicon);
		}

		public virtual string DescribeDistsimLexicon()
		{
			if (lexicon == null)
			{
				return "No distsim lexicon";
			}
			else
			{
				return "Distsim lexicon of size " + lexicon.Count;
			}
		}

		private void DistSimAnnotate(PaddedList<In> info)
		{
			foreach (CoreLabel fl in info)
			{
				if (fl.ContainsKey(typeof(CoreAnnotations.DistSimAnnotation)))
				{
					return;
				}
				string word = GetWord(fl);
				if (!flags.casedDistSim)
				{
					word = word.ToLower();
				}
				if (flags.numberEquivalenceDistSim)
				{
					word = WordShapeClassifier.WordShape(word, WordShapeClassifier.Wordshapedigits);
				}
				string distSim = lexicon[word];
				if (distSim == null)
				{
					distSim = flags.unknownWordDistSimClass;
				}
				fl.Set(typeof(CoreAnnotations.DistSimAnnotation), distSim);
			}
		}

		private IDictionary<string, ICollection<string>> wordToSubstrings = Generics.NewHashMap();

		public virtual void ClearMemory()
		{
			wordToSubstrings = Generics.NewHashMap();
			lexicon = null;
		}

		private static string Dehyphenate(string str)
		{
			// don't take out leading or ending ones, just internal
			// and remember padded with < > characters
			string retStr = str;
			int leng = str.Length;
			int hyphen = 2;
			do
			{
				hyphen = retStr.IndexOf('-', hyphen);
				if (hyphen >= 0 && hyphen < leng - 2)
				{
					retStr = Sharpen.Runtime.Substring(retStr, 0, hyphen) + Sharpen.Runtime.Substring(retStr, hyphen + 1);
				}
				else
				{
					hyphen = -1;
				}
			}
			while (hyphen >= 0);
			return retStr;
		}

		private static string Greekify(string str)
		{
			// don't take out leading or ending ones, just internal
			// and remember padded with < > characters
			string pattern = "(alpha)|(beta)|(gamma)|(delta)|(epsilon)|(zeta)|(kappa)|(lambda)|(rho)|(sigma)|(tau)|(upsilon)|(omega)";
			Pattern p = Pattern.Compile(pattern);
			Matcher m = p.Matcher(str);
			return m.ReplaceAll("~");
		}

		/* end methods that do transformations */
		/*
		* static booleans that check strings for certain qualities *
		*/
		// cdm: this could be improved to handle more name types, such as
		// O'Reilly, DeGuzman, etc. (need a little classifier?!?)
		private static bool IsNameCase(string str)
		{
			if (str.Length < 2)
			{
				return false;
			}
			if (!(char.IsUpperCase(str[0]) || char.IsTitleCase(str[0])))
			{
				return false;
			}
			for (int i = 1; i < str.Length; i++)
			{
				if (char.IsUpperCase(str[i]))
				{
					return false;
				}
			}
			return true;
		}

		private static bool NoUpperCase(string str)
		{
			if (str.Length < 1)
			{
				return false;
			}
			for (int i = 0; i < str.Length; i++)
			{
				if (char.IsUpperCase(str[i]))
				{
					return false;
				}
			}
			return true;
		}

		private static bool HasLetter(string str)
		{
			if (str.Length < 1)
			{
				return false;
			}
			for (int i = 0; i < str.Length; i++)
			{
				if (char.IsLetter(str[i]))
				{
					return true;
				}
			}
			return false;
		}

		private static readonly Pattern ordinalPattern = Pattern.Compile("(?:(?:first|second|third|fourth|fifth|" + "sixth|seventh|eighth|ninth|tenth|" + "eleventh|twelfth|thirteenth|" + "fourteenth|fifteenth|sixteenth|" + "seventeenth|eighteenth|nineteenth|"
			 + "twenty|twentieth|thirty|thirtieth|" + "forty|fortieth|fifty|fiftieth|" + "sixty|sixtieth|seventy|seventieth|" + "eighty|eightieth|ninety|ninetieth|" + "one|two|three|four|five|six|seven|" + "eight|nine|hundred|hundredth)-?)+|[0-9]+(?:st|nd|rd|th)"
			, Pattern.CaseInsensitive);

		private static readonly Pattern numberPattern = Pattern.Compile("[0-9]+");

		private static readonly Pattern ordinalEndPattern = Pattern.Compile("(?:st|nd|rd|th)", Pattern.CaseInsensitive);

		private bool IsOrdinal<_T0>(IList<_T0> wordInfos, int pos)
			where _T0 : CoreLabel
		{
			CoreLabel c = wordInfos[pos];
			string cWord = GetWord(c);
			Matcher m = ordinalPattern.Matcher(cWord);
			if (m.Matches())
			{
				return true;
			}
			m = numberPattern.Matcher(cWord);
			if (m.Matches())
			{
				if (pos + 1 < wordInfos.Count)
				{
					CoreLabel n = wordInfos[pos + 1];
					m = ordinalEndPattern.Matcher(GetWord(n));
					if (m.Matches())
					{
						return true;
					}
				}
				return false;
			}
			m = ordinalEndPattern.Matcher(cWord);
			if (m.Matches())
			{
				if (pos > 0)
				{
					CoreLabel p = wordInfos[pos - 1];
					m = numberPattern.Matcher(GetWord(p));
					if (m.Matches())
					{
						return true;
					}
				}
			}
			if (cWord.Equals("-"))
			{
				if (pos + 1 < wordInfos.Count && pos > 0)
				{
					CoreLabel p = wordInfos[pos - 1];
					CoreLabel n = wordInfos[pos + 1];
					m = ordinalPattern.Matcher(GetWord(p));
					if (m.Matches())
					{
						m = ordinalPattern.Matcher(GetWord(n));
						if (m.Matches())
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>Gazette Stuff.</summary>
		[System.Serializable]
		private class GazetteInfo
		{
			internal readonly string feature;

			internal readonly int loc;

			internal readonly string[] words;

			private const long serialVersionUID = -5903728481621584810L;

			public GazetteInfo(string feature, int loc, string[] words)
			{
				/* end static booleans that check strings for certain qualities */
				this.feature = feature;
				this.loc = loc;
				this.words = words;
			}
		}

		private IDictionary<string, ICollection<string>> wordToGazetteEntries = Generics.NewHashMap();

		private IDictionary<string, ICollection<NERFeatureFactory.GazetteInfo>> wordToGazetteInfos = Generics.NewHashMap();

		// end class GazetteInfo
		/// <summary>Reads a gazette file.</summary>
		/// <remarks>
		/// Reads a gazette file.  Each line of it consists of a class name
		/// (a String not containing whitespace characters), followed by whitespace
		/// characters followed by a phrase, which is one or more tokens separated
		/// by a single space.
		/// </remarks>
		/// <param name="in">Where to read the gazette from</param>
		/// <exception cref="System.IO.IOException">If IO errors</exception>
		private void ReadGazette(BufferedReader @in)
		{
			Pattern p = Pattern.Compile("^(\\S+)\\s+(.+)$");
			for (string line; (line = @in.ReadLine()) != null; )
			{
				Matcher m = p.Matcher(line);
				if (m.Matches())
				{
					string type = Intern(m.Group(1));
					string phrase = m.Group(2);
					string[] words = phrase.Split(" ");
					for (int i = 0; i < words.Length; i++)
					{
						string word = Intern(words[i]);
						if (flags.sloppyGazette)
						{
							ICollection<string> entries = wordToGazetteEntries[word];
							if (entries == null)
							{
								entries = Generics.NewHashSet();
								wordToGazetteEntries[word] = entries;
							}
							string feature = Intern(type + "-GAZ" + words.Length);
							entries.Add(feature);
							feature = Intern(type + "-GAZ");
							entries.Add(feature);
						}
						if (flags.cleanGazette)
						{
							ICollection<NERFeatureFactory.GazetteInfo> infos = wordToGazetteInfos[word];
							if (infos == null)
							{
								infos = Generics.NewHashSet();
								wordToGazetteInfos[word] = infos;
							}
							NERFeatureFactory.GazetteInfo info = new NERFeatureFactory.GazetteInfo(Intern(type + "-GAZ" + words.Length), i, words);
							infos.Add(info);
							info = new NERFeatureFactory.GazetteInfo(Intern(type + "-GAZ"), i, words);
							infos.Add(info);
						}
					}
				}
			}
		}

		private ICollection<Type> genericAnnotationKeys;

		// = null; //cache which keys are generic annotations so we don't have to do too many instanceof checks
		private void MakeGenericKeyCache(CoreLabel c)
		{
			genericAnnotationKeys = Generics.NewHashSet();
			foreach (Type key in c.KeySet())
			{
				if (CoreLabel.genericValues.Contains(key))
				{
					Type genKey = (Type)key;
					genericAnnotationKeys.Add(genKey);
				}
			}
		}

		private ICollection<string> lastNames;

		private ICollection<string> maleNames;

		private ICollection<string> femaleNames;

		private readonly Pattern titlePattern = Pattern.Compile("(?:Mr|Ms|Mrs|Dr|Miss|Sen|Judge|Sir)\\.?");

		private static readonly Pattern titlePattern2 = Pattern.Compile("(?i:Mr|Mrs|Ms|Miss|Drs?|Profs?|Sens?|Reps?|Attys?|Lt|Col|Gen|Messrs|Govs?|Adm|Rev|Maj|Sgt|Cpl|Pvt|Capt|Ste?|Ave|Pres|Lieut|Hon|Brig|Co?mdr|Pfc|Spc|Supts?|Det|Mt|Ft|Adj|Adv|Asst|Assoc|Ens|Insp|Mlle|Mme|Msgr|Sfc)\\.?"
			);

		private static readonly Pattern splitSlashHyphenWordsPattern = Pattern.Compile("[-/]");

		// = null;
		// = null;
		// = null;
		// todo: should make static final and add more titles
		private void GenerateSlashHyphenFeatures(string word, ICollection<string> featuresC, string fragSuffix, string wordSuffix)
		{
			string[] bits = splitSlashHyphenWordsPattern.Split(word);
			foreach (string bit in bits)
			{
				if (flags.slashHyphenTreatment == SeqClassifierFlags.SlashHyphenEnum.Wfrag)
				{
					featuresC.Add(bit + fragSuffix);
				}
				else
				{
					if (flags.slashHyphenTreatment == SeqClassifierFlags.SlashHyphenEnum.Both)
					{
						featuresC.Add(bit + fragSuffix);
						featuresC.Add(bit + wordSuffix);
					}
					else
					{
						// option WORD
						featuresC.Add(bit + wordSuffix);
					}
				}
			}
		}

		protected internal virtual ICollection<string> FeaturesC(PaddedList<In> cInfo, int loc)
		{
			CoreLabel p3 = cInfo[loc - 3];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel c = cInfo[loc];
			CoreLabel n = cInfo[loc + 1];
			CoreLabel n2 = cInfo[loc + 2];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string nWord = GetWord(n);
			string cShape = c.GetString<CoreAnnotations.ShapeAnnotation>();
			string pShape = p.GetString<CoreAnnotations.ShapeAnnotation>();
			string nShape = n.GetString<CoreAnnotations.ShapeAnnotation>();
			ICollection<string> featuresC = new List<string>();
			if (flags.useDistSim)
			{
				DistSimAnnotate(cInfo);
			}
			if (flags.useBagOfWords)
			{
				foreach (IN word in cInfo)
				{
					featuresC.Add(GetWord(word) + "-BAGOFWORDS");
				}
			}
			if (flags.useDistSim && flags.useMoreTags)
			{
				featuresC.Add(p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + cWord + "-PDISTSIM-CWORD");
			}
			if (flags.useDistSim)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM");
			}
			if (flags.useTitle)
			{
				Matcher m = titlePattern.Matcher(cWord);
				if (m.Matches())
				{
					featuresC.Add("IS_TITLE");
				}
			}
			else
			{
				if (flags.useTitle2)
				{
					Matcher m = titlePattern2.Matcher(cWord);
					if (m.Matches())
					{
						featuresC.Add("IS_TITLE");
					}
				}
			}
			if (flags.slashHyphenTreatment != SeqClassifierFlags.SlashHyphenEnum.None)
			{
				if (flags.useWord)
				{
					GenerateSlashHyphenFeatures(cWord, featuresC, "-WFRAG", "-WORD");
				}
			}
			if (flags.useInternal && flags.useExternal)
			{
				if (flags.useWord)
				{
					featuresC.Add(cWord + "-WORD");
				}
				if (flags.use2W)
				{
					featuresC.Add(GetWord(p2) + "-P2W");
					featuresC.Add(GetWord(n2) + "-N2W");
				}
				if (flags.useLC)
				{
					featuresC.Add(cWord.ToLower() + "-CL");
					featuresC.Add(pWord.ToLower() + "-PL");
					featuresC.Add(nWord.ToLower() + "-NL");
				}
				if (flags.useUnknown)
				{
					// for true casing
					featuresC.Add(c.Get(typeof(CoreAnnotations.UnknownAnnotation)) + "-UNKNOWN");
					featuresC.Add(p.Get(typeof(CoreAnnotations.UnknownAnnotation)) + "-PUNKNOWN");
					featuresC.Add(n.Get(typeof(CoreAnnotations.UnknownAnnotation)) + "-NUNKNOWN");
				}
				if (flags.useLemmas)
				{
					string lem = c.GetString<CoreAnnotations.LemmaAnnotation>();
					if (!string.Empty.Equals(lem))
					{
						featuresC.Add(lem + "-LEM");
					}
				}
				if (flags.usePrevNextLemmas)
				{
					string plem = p.GetString<CoreAnnotations.LemmaAnnotation>();
					string nlem = n.GetString<CoreAnnotations.LemmaAnnotation>();
					if (!string.Empty.Equals(plem))
					{
						featuresC.Add(plem + "-PLEM");
					}
					if (!string.Empty.Equals(nlem))
					{
						featuresC.Add(nlem + "-NLEM");
					}
				}
				if (flags.checkNameList)
				{
					try
					{
						if (lastNames == null)
						{
							lastNames = Generics.NewHashSet();
							foreach (string line in ObjectBank.GetLineIterator(flags.lastNameList))
							{
								string[] cols = line.Split("\\s+");
								lastNames.Add(cols[0]);
							}
						}
						if (maleNames == null)
						{
							maleNames = Generics.NewHashSet();
							foreach (string line in ObjectBank.GetLineIterator(flags.maleNameList))
							{
								string[] cols = line.Split("\\s+");
								maleNames.Add(cols[0]);
							}
						}
						if (femaleNames == null)
						{
							femaleNames = Generics.NewHashSet();
							foreach (string line in ObjectBank.GetLineIterator(flags.femaleNameList))
							{
								string[] cols = line.Split("\\s+");
								femaleNames.Add(cols[0]);
							}
						}
						string name = cWord.ToUpper();
						if (lastNames.Contains(name))
						{
							featuresC.Add("LAST_NAME");
						}
						if (maleNames.Contains(name))
						{
							featuresC.Add("MALE_NAME");
						}
						if (femaleNames.Contains(name))
						{
							featuresC.Add("FEMALE_NAME");
						}
					}
					catch (Exception e)
					{
						throw new Exception(e);
					}
				}
				if (flags.binnedLengths != null)
				{
					int len = cWord.Length;
					string featureName = null;
					for (int i = 0; i <= flags.binnedLengths.Length; i++)
					{
						if (i == flags.binnedLengths.Length)
						{
							featureName = "Len-" + flags.binnedLengths[flags.binnedLengths.Length - 1] + "-Inf";
						}
						else
						{
							if (len <= flags.binnedLengths[i])
							{
								featureName = "Len-" + ((i == 0) ? 1 : flags.binnedLengths[i - 1]) + '-' + flags.binnedLengths[i];
								break;
							}
						}
					}
					featuresC.Add(featureName);
				}
				if (flags.useABGENE)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbgeneAnnotation)) + "-ABGENE");
					featuresC.Add(p.Get(typeof(CoreAnnotations.AbgeneAnnotation)) + "-PABGENE");
					featuresC.Add(n.Get(typeof(CoreAnnotations.AbgeneAnnotation)) + "-NABGENE");
				}
				if (flags.useABSTRFreqDict)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-ABSTRACT" + c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TAG");
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-ABSTRACT" + c.Get(typeof(CoreAnnotations.DictAnnotation)) + "-DICT" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TAG");
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-ABSTRACT" + c.Get(typeof(CoreAnnotations.DictAnnotation)) + "-DICT" + c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation
						>() + "-TAG");
				}
				if (flags.useABSTR)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-ABSTRACT");
					featuresC.Add(p.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-PABSTRACT");
					featuresC.Add(n.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-NABSTRACT");
				}
				if (flags.useGENIA)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.GeniaAnnotation)) + "-GENIA");
					featuresC.Add(p.Get(typeof(CoreAnnotations.GeniaAnnotation)) + "-PGENIA");
					featuresC.Add(n.Get(typeof(CoreAnnotations.GeniaAnnotation)) + "-NGENIA");
				}
				if (flags.useWEBFreqDict)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.WebAnnotation)) + "-WEB" + c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TAG");
					featuresC.Add(c.Get(typeof(CoreAnnotations.WebAnnotation)) + "-WEB" + c.Get(typeof(CoreAnnotations.DictAnnotation)) + "-DICT" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TAG");
					featuresC.Add(c.Get(typeof(CoreAnnotations.WebAnnotation)) + "-WEB" + c.Get(typeof(CoreAnnotations.DictAnnotation)) + "-DICT" + c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() +
						 "-TAG");
				}
				if (flags.useWEB)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.WebAnnotation)) + "-WEB");
					featuresC.Add(p.Get(typeof(CoreAnnotations.WebAnnotation)) + "-PWEB");
					featuresC.Add(n.Get(typeof(CoreAnnotations.WebAnnotation)) + "-NWEB");
				}
				if (flags.useIsURL)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.IsURLAnnotation)) + "-ISURL");
				}
				if (flags.useEntityRule)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.EntityRuleAnnotation)) + "-ENTITYRULE");
				}
				if (flags.useEntityTypes)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) + "-ENTITYTYPE");
				}
				if (flags.useIsDateRange)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.IsDateRangeAnnotation)) + "-ISDATERANGE");
				}
				if (flags.useABSTRFreq)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbstrAnnotation)) + "-ABSTRACT" + c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ");
				}
				if (flags.useFREQ)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.FreqAnnotation)) + "-FREQ");
				}
				if (flags.useMoreTags)
				{
					featuresC.Add(p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + cWord + "-PTAG-CWORD");
				}
				if (flags.usePosition)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.PositionAnnotation)) + "-POSITION");
				}
				if (flags.useBeginSent)
				{
					string pos = c.Get(typeof(CoreAnnotations.PositionAnnotation));
					if ("0".Equals(pos))
					{
						featuresC.Add("BEGIN-SENT");
						featuresC.Add(cShape + "-BEGIN-SENT");
					}
					else
					{
						if (int.ToString(cInfo.Count - 1).Equals(pos))
						{
							featuresC.Add("END-SENT");
							featuresC.Add(cShape + "-END-SENT");
						}
						else
						{
							featuresC.Add("IN-SENT");
							featuresC.Add(cShape + "-IN-SENT");
						}
					}
				}
				if (flags.useTags)
				{
					featuresC.Add(c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TAG");
				}
				if (flags.useOrdinal)
				{
					if (IsOrdinal(cInfo, loc))
					{
						featuresC.Add("C_ORDINAL");
						if (IsOrdinal(cInfo, loc - 1))
						{
							//log.info(getWord(p) + " ");
							featuresC.Add("PC_ORDINAL");
						}
					}
					//log.info(cWord);
					if (IsOrdinal(cInfo, loc - 1))
					{
						featuresC.Add("P_ORDINAL");
					}
				}
				if (flags.usePrev)
				{
					featuresC.Add(pWord + "-PW");
					if (flags.useTags)
					{
						featuresC.Add(p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-PTAG");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-PDISTSIM");
					}
					if (flags.useIsURL)
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.IsURLAnnotation)) + "-PISURL");
					}
					if (flags.useEntityTypes)
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) + "-PENTITYTYPE");
					}
				}
				if (flags.useNext)
				{
					featuresC.Add(nWord + "-NW");
					if (flags.useTags)
					{
						featuresC.Add(n.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-NTAG");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(n.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-NDISTSIM");
					}
					if (flags.useIsURL)
					{
						featuresC.Add(n.Get(typeof(CoreAnnotations.IsURLAnnotation)) + "-NISURL");
					}
					if (flags.useEntityTypes)
					{
						featuresC.Add(n.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) + "-NENTITYTYPE");
					}
				}
				/*here, entityTypes refers to the type in the PASCAL IE challenge:
				* i.e. certain words are tagged "Date" or "Location" */
				if (flags.useEitherSideWord)
				{
					featuresC.Add(pWord + "-EW");
					featuresC.Add(nWord + "-EW");
				}
				if (flags.useWordPairs)
				{
					featuresC.Add(cWord + '-' + pWord + "-W-PW");
					featuresC.Add(cWord + '-' + nWord + "-W-NW");
				}
				if (flags.useSymTags)
				{
					if (flags.useTags)
					{
						featuresC.Add(p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + n.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-PCNTAGS");
						featuresC.Add(c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + n.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-CNTAGS");
						featuresC.Add(p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-PCTAGS");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-PCNDISTSIM");
						featuresC.Add(c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-CNDISTSIM");
						featuresC.Add(p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-PCDISTSIM");
					}
				}
				if (flags.useSymWordPairs)
				{
					featuresC.Add(pWord + '-' + nWord + "-SWORDS");
				}
				string pGazAnnotation = (flags.useGazFeatures || flags.useMoreGazFeatures) ? p.Get(typeof(CoreAnnotations.GazAnnotation)) : null;
				string nGazAnnotation = (flags.useGazFeatures || flags.useMoreGazFeatures) ? n.Get(typeof(CoreAnnotations.GazAnnotation)) : null;
				string cGazAnnotation = (flags.useGazFeatures || flags.useMoreGazFeatures) ? c.Get(typeof(CoreAnnotations.GazAnnotation)) : null;
				if (flags.useGazFeatures)
				{
					if (cGazAnnotation != null && !cGazAnnotation.Equals(flags.dropGaz))
					{
						featuresC.Add(cGazAnnotation + "-GAZ");
					}
					// n
					if (nGazAnnotation != null && !nGazAnnotation.Equals(flags.dropGaz))
					{
						featuresC.Add(nGazAnnotation + "-NGAZ");
					}
					// p
					if (pGazAnnotation != null && !pGazAnnotation.Equals(flags.dropGaz))
					{
						featuresC.Add(pGazAnnotation + "-PGAZ");
					}
				}
				if (flags.useMoreGazFeatures)
				{
					if (cGazAnnotation != null && !cGazAnnotation.Equals(flags.dropGaz))
					{
						featuresC.Add(cGazAnnotation + '-' + cWord + "-CG-CW-GAZ");
						// c-n
						if (nGazAnnotation != null && !nGazAnnotation.Equals(flags.dropGaz))
						{
							featuresC.Add(cGazAnnotation + '-' + nGazAnnotation + "-CNGAZ");
						}
						// p-c
						if (pGazAnnotation != null && !pGazAnnotation.Equals(flags.dropGaz))
						{
							featuresC.Add(pGazAnnotation + '-' + cGazAnnotation + "-PCGAZ");
						}
					}
				}
				if (flags.useAbbr || flags.useMinimalAbbr)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-ABBR");
				}
				if (flags.useAbbr1 || flags.useMinimalAbbr1)
				{
					if (!c.Get(typeof(CoreAnnotations.AbbrAnnotation)).Equals("XX"))
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-ABBR");
					}
				}
				if (flags.useAbbr)
				{
					featuresC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PCABBR");
					featuresC.Add(c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-CNABBR");
					featuresC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PCNABBR");
				}
				if (flags.useAbbr1)
				{
					if (!c.Get(typeof(CoreAnnotations.AbbrAnnotation)).Equals("XX"))
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PCABBR");
						featuresC.Add(c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-CNABBR");
						featuresC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PCNABBR");
					}
				}
				if (flags.useChunks)
				{
					featuresC.Add(p.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ChunkAnnotation)) + "-PCCHUNK");
					featuresC.Add(c.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.ChunkAnnotation)) + "-CNCHUNK");
					featuresC.Add(p.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.ChunkAnnotation)) + "-PCNCHUNK");
				}
				if (flags.useMinimalAbbr)
				{
					featuresC.Add(cWord + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-CWABB");
				}
				if (flags.useMinimalAbbr1)
				{
					if (!c.Get(typeof(CoreAnnotations.AbbrAnnotation)).Equals("XX"))
					{
						featuresC.Add(cWord + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-CWABB");
					}
				}
				string prevVB = string.Empty;
				string nextVB = string.Empty;
				if (flags.usePrevVB)
				{
					for (int j = loc - 1; ; j--)
					{
						CoreLabel wi = cInfo[j];
						if (wi == cInfo.GetPad())
						{
							prevVB = "X";
							featuresC.Add("X-PVB");
							break;
						}
						else
						{
							if (wi.GetString<CoreAnnotations.PartOfSpeechAnnotation>().StartsWith("VB"))
							{
								featuresC.Add(GetWord(wi) + "-PVB");
								prevVB = GetWord(wi);
								break;
							}
						}
					}
				}
				if (flags.useNextVB)
				{
					for (int j = loc + 1; ; j++)
					{
						CoreLabel wi = cInfo[j];
						if (wi == cInfo.GetPad())
						{
							featuresC.Add("X-NVB");
							nextVB = "X";
							break;
						}
						else
						{
							if (wi.GetString<CoreAnnotations.PartOfSpeechAnnotation>().StartsWith("VB"))
							{
								featuresC.Add(GetWord(wi) + "-NVB");
								nextVB = GetWord(wi);
								break;
							}
						}
					}
				}
				if (flags.useVB)
				{
					featuresC.Add(prevVB + '-' + nextVB + "-PNVB");
				}
				if (flags.useShapeConjunctions)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.PositionAnnotation)) + cShape + "-POS-SH");
					if (flags.useTags)
					{
						featuresC.Add(c.Tag() + cShape + "-TAG-SH");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + cShape + "-DISTSIM-SH");
					}
				}
				if (flags.useWordTag)
				{
					featuresC.Add(cWord + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-W-T");
					featuresC.Add(cWord + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-W-PT");
					featuresC.Add(cWord + '-' + n.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-W-NT");
				}
				if (flags.useNPHead)
				{
					// TODO: neat idea, but this would need to be set somewhere.
					// Probably should have its own annotation as this one would
					// be more narrow and would clobber other potential uses
					featuresC.Add(c.Get(typeof(CoreAnnotations.HeadWordStringAnnotation)) + "-HW");
					if (flags.useTags)
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.HeadWordStringAnnotation)) + "-" + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-HW-T");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.HeadWordStringAnnotation)) + "-" + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-HW-DISTSIM");
					}
				}
				if (flags.useNPGovernor)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.GovernorAnnotation)) + "-GW");
					if (flags.useTags)
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.GovernorAnnotation)) + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-GW-T");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(c.Get(typeof(CoreAnnotations.GovernorAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM-T1");
					}
				}
				if (flags.useHeadGov)
				{
					// TODO: neat idea, but this would need to be set somewhere.
					// Probably should have its own annotation as this one would
					// be more narrow and would clobber other potential uses
					featuresC.Add(c.Get(typeof(CoreAnnotations.HeadWordStringAnnotation)) + "-" + c.Get(typeof(CoreAnnotations.GovernorAnnotation)) + "-HW_GW");
				}
				if (flags.useClassFeature)
				{
					featuresC.Add("###");
				}
				if (flags.useFirstWord)
				{
					string firstWord = GetWord(cInfo[0]);
					featuresC.Add(firstWord);
				}
				if (flags.useNGrams)
				{
					ICollection<string> subs = null;
					if (flags.cacheNGrams)
					{
						subs = wordToSubstrings[cWord];
					}
					if (subs == null)
					{
						subs = new List<string>();
						string word = '<' + cWord + '>';
						if (flags.lowercaseNGrams)
						{
							word = word.ToLower();
						}
						if (flags.dehyphenateNGrams)
						{
							word = Dehyphenate(word);
						}
						if (flags.greekifyNGrams)
						{
							word = Greekify(word);
						}
						// minimum length substring is 2 letters (hardwired)
						// hoist flags.noMidNGrams so only linear in word length for that case
						if (flags.noMidNGrams)
						{
							int max = flags.maxNGramLeng >= 0 ? Math.Min(flags.maxNGramLeng, word.Length) : word.Length;
							for (int j = 2; j <= max; j++)
							{
								subs.Add(Intern('#' + Sharpen.Runtime.Substring(word, 0, j) + '#'));
							}
							int start = flags.maxNGramLeng >= 0 ? Math.Max(0, word.Length - flags.maxNGramLeng) : 0;
							int lenM1 = word.Length - 1;
							for (int i = start; i < lenM1; i++)
							{
								subs.Add(Intern('#' + Sharpen.Runtime.Substring(word, i) + '#'));
							}
						}
						else
						{
							for (int i = 0; i < word.Length; i++)
							{
								for (int j = i + 2; j <= max; j++)
								{
									if (flags.maxNGramLeng >= 0 && j - i > flags.maxNGramLeng)
									{
										continue;
									}
									subs.Add(Intern('#' + Sharpen.Runtime.Substring(word, i, j) + '#'));
								}
							}
						}
						if (flags.cacheNGrams)
						{
							wordToSubstrings[cWord] = subs;
						}
					}
					Sharpen.Collections.AddAll(featuresC, subs);
					if (flags.conjoinShapeNGrams)
					{
						foreach (string str in subs)
						{
							string feat = str + '-' + cShape + "-CNGram-CS";
							featuresC.Add(feat);
						}
					}
				}
				if (flags.useGazettes)
				{
					if (flags.sloppyGazette)
					{
						ICollection<string> entries = wordToGazetteEntries[cWord];
						if (entries != null)
						{
							Sharpen.Collections.AddAll(featuresC, entries);
						}
					}
					if (flags.cleanGazette)
					{
						ICollection<NERFeatureFactory.GazetteInfo> infos = wordToGazetteInfos[cWord];
						if (infos != null)
						{
							foreach (NERFeatureFactory.GazetteInfo gInfo in infos)
							{
								bool ok = true;
								for (int gLoc = 0; gLoc < gInfo.words.Length; gLoc++)
								{
									ok &= gInfo.words[gLoc].Equals(GetWord(cInfo[loc + gLoc - gInfo.loc]));
								}
								if (ok)
								{
									featuresC.Add(gInfo.feature);
								}
							}
						}
					}
				}
				if ((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings)
				{
					featuresC.Add(cShape + "-TYPE");
					if (flags.useTypeSeqs)
					{
						featuresC.Add(pShape + "-PTYPE");
						featuresC.Add(nShape + "-NTYPE");
						featuresC.Add(pWord + "..." + cShape + "-PW_CTYPE");
						featuresC.Add(cShape + "..." + nWord + "-NW_CTYPE");
						featuresC.Add(pShape + "..." + cShape + "-PCTYPE");
						featuresC.Add(cShape + "..." + nShape + "-CNTYPE");
						featuresC.Add(pShape + "..." + cShape + "..." + nShape + "-PCNTYPE");
					}
				}
				if (flags.useLastRealWord)
				{
					if (pWord.Length <= 3)
					{
						// extending this to check for 2 short words doesn't seem to help....
						featuresC.Add(GetWord(p2) + "..." + cShape + "-PPW_CTYPE");
					}
				}
				if (flags.useNextRealWord)
				{
					if (nWord.Length <= 3)
					{
						// extending this to check for 2 short words doesn't seem to help....
						featuresC.Add(GetWord(n2) + "..." + cShape + "-NNW_CTYPE");
					}
				}
				if (flags.useOccurrencePatterns)
				{
					Sharpen.Collections.AddAll(featuresC, OccurrencePatterns(cInfo, loc));
				}
				if (flags.useDisjunctive)
				{
					for (int i = 1; i <= flags.disjunctionWidth; i++)
					{
						CoreLabel dn = cInfo[loc + i];
						CoreLabel dp = cInfo[loc - i];
						featuresC.Add(GetWord(dn) + "-DISJN");
						if (flags.useDisjunctiveShapeInteraction)
						{
							featuresC.Add(GetWord(dn) + '-' + cShape + "-DISJN-CS");
						}
						featuresC.Add(GetWord(dp) + "-DISJP");
						if (flags.useDisjunctiveShapeInteraction)
						{
							featuresC.Add(GetWord(dp) + '-' + cShape + "-DISJP-CS");
						}
					}
				}
				if (flags.useUndirectedDisjunctive)
				{
					for (int i = 1; i <= flags.disjunctionWidth; i++)
					{
						CoreLabel dn = cInfo[loc + i];
						CoreLabel dp = cInfo[loc - i];
						featuresC.Add(GetWord(dn) + "-DISJ");
						featuresC.Add(GetWord(dp) + "-DISJ");
					}
				}
				if (flags.useWideDisjunctive)
				{
					for (int i = 1; i <= flags.wideDisjunctionWidth; i++)
					{
						featuresC.Add(GetWord(cInfo[loc + i]) + "-DISJWN");
						featuresC.Add(GetWord(cInfo[loc - i]) + "-DISJWP");
					}
				}
				if (flags.useEitherSideDisjunctive)
				{
					for (int i = 1; i <= flags.disjunctionWidth; i++)
					{
						featuresC.Add(GetWord(cInfo[loc + i]) + "-DISJWE");
						featuresC.Add(GetWord(cInfo[loc - i]) + "-DISJWE");
					}
				}
				if (flags.useDisjShape)
				{
					for (int i = 1; i <= flags.disjunctionWidth; i++)
					{
						featuresC.Add(cInfo[loc + i].Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-NDISJSHAPE");
						// featuresC.add(cInfo.get(loc - i).get(CoreAnnotations.ShapeAnnotation.class) + "-PDISJSHAPE");
						featuresC.Add(cShape + '-' + cInfo[loc + i].Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-CNDISJSHAPE");
					}
				}
				// featuresC.add(c.get(CoreAnnotations.ShapeAnnotation.class) + "-" + cInfo.get(loc - i).get(CoreAnnotations.ShapeAnnotation.class) + "-CPDISJSHAPE");
				if (flags.useExtraTaggySequences)
				{
					if (flags.useTags)
					{
						featuresC.Add(p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TTS");
						featuresC.Add(p3.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation
							>() + "-TTTS");
					}
					if (flags.useDistSim)
					{
						featuresC.Add(p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM_TTS1");
						featuresC.Add(p3.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM_TTTS1"
							);
					}
				}
				if (flags.useMUCFeatures)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.SectionAnnotation)) + "-SECTION");
					featuresC.Add(c.Get(typeof(CoreAnnotations.WordPositionAnnotation)) + "-WORD_POSITION");
					featuresC.Add(c.Get(typeof(CoreAnnotations.SentencePositionAnnotation)) + "-SENT_POSITION");
					featuresC.Add(c.Get(typeof(CoreAnnotations.ParaPositionAnnotation)) + "-PARA_POSITION");
					featuresC.Add(c.Get(typeof(CoreAnnotations.WordPositionAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-WORD_POSITION_SHAPE");
				}
			}
			else
			{
				if (flags.useInternal)
				{
					if (flags.useWord)
					{
						featuresC.Add(cWord + "-WORD");
					}
					if (flags.useNGrams)
					{
						ICollection<string> subs = wordToSubstrings[cWord];
						if (subs == null)
						{
							subs = new List<string>();
							string word = '<' + cWord + '>';
							if (flags.lowercaseNGrams)
							{
								word = word.ToLower();
							}
							if (flags.dehyphenateNGrams)
							{
								word = Dehyphenate(word);
							}
							if (flags.greekifyNGrams)
							{
								word = Greekify(word);
							}
							for (int i = 0; i < word.Length; i++)
							{
								for (int j = i + 2; j <= word.Length; j++)
								{
									if (flags.noMidNGrams && i != 0 && j != word.Length)
									{
										continue;
									}
									if (flags.maxNGramLeng >= 0 && j - i > flags.maxNGramLeng)
									{
										continue;
									}
									//subs.add(intern("#" + word.substring(i, j) + "#"));
									subs.Add(Intern('#' + Sharpen.Runtime.Substring(word, i, j) + '#'));
								}
							}
							if (flags.cacheNGrams)
							{
								wordToSubstrings[cWord] = subs;
							}
						}
						Sharpen.Collections.AddAll(featuresC, subs);
						if (flags.conjoinShapeNGrams)
						{
							string shape = c.Get(typeof(CoreAnnotations.ShapeAnnotation));
							foreach (string str in subs)
							{
								string feat = str + '-' + shape + "-CNGram-CS";
								featuresC.Add(feat);
							}
						}
					}
					if ((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings)
					{
						featuresC.Add(cShape + "-TYPE");
					}
					if (flags.useOccurrencePatterns)
					{
						Sharpen.Collections.AddAll(featuresC, OccurrencePatterns(cInfo, loc));
					}
				}
				else
				{
					if (flags.useExternal)
					{
						if (flags.usePrev)
						{
							featuresC.Add(pWord + "-PW");
						}
						if (flags.useNext)
						{
							featuresC.Add(nWord + "-NW");
						}
						if (flags.useWordPairs)
						{
							featuresC.Add(cWord + '-' + pWord + "-W-PW");
							featuresC.Add(cWord + '-' + nWord + "-W-NW");
						}
						if (flags.useSymWordPairs)
						{
							featuresC.Add(pWord + '-' + nWord + "-SWORDS");
						}
						if ((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings)
						{
							if (flags.useTypeSeqs)
							{
								featuresC.Add(pShape + "-PTYPE");
								featuresC.Add(nShape + "-NTYPE");
								featuresC.Add(pWord + "..." + cShape + "-PW_CTYPE");
								featuresC.Add(cShape + "..." + nWord + "-NW_CTYPE");
								if (flags.maxLeft > 0)
								{
									featuresC.Add(pShape + "..." + cShape + "-PCTYPE");
								}
								// this one just isn't useful, at least given c,pc,s,ps.  Might be useful 0th-order
								featuresC.Add(cShape + "..." + nShape + "-CNTYPE");
								featuresC.Add(pShape + "..." + cShape + "..." + nShape + "-PCNTYPE");
							}
						}
						if (flags.useLastRealWord)
						{
							if (pWord.Length <= 3)
							{
								featuresC.Add(GetWord(p2) + "..." + cShape + "-PPW_CTYPE");
							}
						}
						if (flags.useNextRealWord)
						{
							if (nWord.Length <= 3)
							{
								featuresC.Add(GetWord(n2) + "..." + cShape + "-NNW_CTYPE");
							}
						}
						if (flags.useDisjunctive)
						{
							for (int i = 1; i <= flags.disjunctionWidth; i++)
							{
								CoreLabel dn = cInfo[loc + i];
								CoreLabel dp = cInfo[loc - i];
								featuresC.Add(GetWord(dn) + "-DISJN");
								if (flags.useDisjunctiveShapeInteraction)
								{
									featuresC.Add(GetWord(dn) + '-' + cShape + "-DISJN-CS");
								}
								featuresC.Add(GetWord(dp) + "-DISJP");
								if (flags.useDisjunctiveShapeInteraction)
								{
									featuresC.Add(GetWord(dp) + '-' + cShape + "-DISJP-CS");
								}
							}
						}
						if (flags.useWideDisjunctive)
						{
							for (int i = 1; i <= flags.wideDisjunctionWidth; i++)
							{
								featuresC.Add(GetWord(cInfo[loc + i]) + "-DISJWN");
								featuresC.Add(GetWord(cInfo[loc - i]) + "-DISJWP");
							}
						}
						if (flags.useDisjShape)
						{
							for (int i = 1; i <= flags.disjunctionWidth; i++)
							{
								featuresC.Add(cInfo[loc + i].Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-NDISJSHAPE");
								// featuresC.add(cInfo.get(loc - i).get(CoreAnnotations.ShapeAnnotation.class) + "-PDISJSHAPE");
								featuresC.Add(c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + '-' + cInfo[loc + i].Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-CNDISJSHAPE");
							}
						}
					}
				}
			}
			// featuresC.add(c.get(CoreAnnotations.ShapeAnnotation.class) + "-" + cInfo.get(loc - i).get(CoreAnnotations.ShapeAnnotation.class) + "-CPDISJSHAPE");
			// Stuff to add binary features from the additional columns
			if (flags.twoStage)
			{
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin1Annotation)) + "-BIN1");
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin2Annotation)) + "-BIN2");
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin3Annotation)) + "-BIN3");
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin4Annotation)) + "-BIN4");
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin5Annotation)) + "-BIN5");
				featuresC.Add(c.Get(typeof(NERFeatureFactory.Bin6Annotation)) + "-BIN6");
			}
			if (flags.useIfInteger)
			{
				try
				{
					int val = System.Convert.ToInt32(cWord);
					if (val > 0)
					{
						featuresC.Add("POSITIVE_INTEGER");
					}
					else
					{
						if (val < 0)
						{
							featuresC.Add("NEGATIVE_INTEGER");
						}
					}
				}
				catch (NumberFormatException)
				{
				}
			}
			// log.info("FOUND INTEGER");
			// not an integer value, nothing to do
			//Stuff to add arbitrary features
			if (flags.useGenericFeatures)
			{
				//see if we need to cache the keys
				if (genericAnnotationKeys == null)
				{
					MakeGenericKeyCache(c);
				}
				//now look through the cached keys
				foreach (Type key in genericAnnotationKeys)
				{
					//log.info("Adding feature: " + CoreLabel.genericValues.get(key) + " with value " + c.get(key));
					if (c.Get(key) != null && c.Get(key) is ICollection)
					{
						foreach (object ob in (ICollection)c.Get(key))
						{
							featuresC.Add(ob + "-" + CoreLabel.genericValues[key]);
						}
					}
					else
					{
						featuresC.Add(c.Get(key) + "-" + CoreLabel.genericValues[key]);
					}
				}
			}
			if (flags.useTopics)
			{
				//featuresC.add(p.get(CoreAnnotations.TopicAnnotation.class) + '-' + cWord + "--CWORD");
				featuresC.Add(c.Get(typeof(CoreAnnotations.TopicAnnotation)) + "-TopicID");
				featuresC.Add(p.Get(typeof(CoreAnnotations.TopicAnnotation)) + "-PTopicID");
				featuresC.Add(n.Get(typeof(CoreAnnotations.TopicAnnotation)) + "-NTopicID");
			}
			//featuresC.add(p.get(CoreAnnotations.TopicAnnotation.class) + '-' + c.get(CoreAnnotations.TopicAnnotation.class) + '-' + n.get(CoreAnnotations.TopicAnnotation.class) + "-PCNTopicID");
			//featuresC.add(c.get(CoreAnnotations.TopicAnnotation.class) + '-' + n.get(CoreAnnotations.TopicAnnotation.class) + "-CNTopicID");
			//featuresC.add(p.get(CoreAnnotations.TopicAnnotation.class) + '-' + c.get(CoreAnnotations.TopicAnnotation.class) + "-PCTopicID");
			//featuresC.add(c.get(CoreAnnotations.TopicAnnotation.class) + cShape + "-TopicID-SH");
			//asdasd
			// todo [cdm 2014]: Have this guarded by a flag and things would be a little faster. Set flag in current uses of this annotation.
			// NER tag annotations from a previous NER system
			if (c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) != null)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-CStackedNERTag");
				featuresC.Add(cWord + "-" + c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-WCStackedNERTag");
				if (flags.useNext)
				{
					featuresC.Add(c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-CNStackedNERTag");
					featuresC.Add(cWord + "-" + c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-WCNStackedNERTag");
					if (flags.usePrev)
					{
						featuresC.Add(p.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-PCNStackedNERTag");
						featuresC.Add(p.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + cWord + " -" + c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-PWCNStackedNERTag"
							);
					}
				}
				if (flags.usePrev)
				{
					featuresC.Add(p.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation)) + "-PCStackedNERTag");
				}
			}
			if (flags.useWordnetFeatures)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.WordnetSynAnnotation)) + "-WordnetSyn");
			}
			if (flags.useProtoFeatures)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.ProtoAnnotation)) + "-Proto");
			}
			if (flags.usePhraseWordTags)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.PhraseWordsTagAnnotation)) + "-PhraseTag");
			}
			if (flags.usePhraseWords)
			{
				foreach (string w in c.Get(typeof(CoreAnnotations.PhraseWordsAnnotation)))
				{
					featuresC.Add(w + "-PhraseWord");
				}
			}
			if (flags.useCommonWordsFeature)
			{
				featuresC.Add(c.Get(typeof(CoreAnnotations.CommonWordsAnnotation)));
			}
			if (flags.useRadical && cWord.Length > 0)
			{
				// todo [cdm 2016]: Really all stuff in this file should be fixed to work with codepoints outside BMP
				if (cWord.Length == 1)
				{
					featuresC.Add(RadicalMap.GetRadical(cWord[0]) + "-SINGLE-CHAR-RADICAL");
				}
				else
				{
					featuresC.Add(RadicalMap.GetRadical(cWord[0]) + "-START-RADICAL");
					featuresC.Add(RadicalMap.GetRadical(cWord[cWord.Length - 1]) + "-END-RADICAL");
				}
				for (int i = 0; i < cWord.Length; ++i)
				{
					featuresC.Add(RadicalMap.GetRadical(cWord[i]) + "-RADICAL");
				}
			}
			if (flags.splitWordRegex != null && !flags.splitWordRegex.IsEmpty())
			{
				string[] ws = c.Word().Split(flags.splitWordRegex);
				foreach (string s in ws)
				{
					featuresC.Add(s + "-SPLITWORD");
				}
			}
			if (flags.useMoreNeighborNGrams)
			{
				int maxLen = pWord.Length;
				if (flags.maxNGramLeng >= 0 && flags.maxNGramLeng < maxLen)
				{
					maxLen = flags.maxNGramLeng;
				}
				for (int len = 1; len <= maxLen; ++len)
				{
					featuresC.Add(Sharpen.Runtime.Substring(pWord, 0, len) + "-PREV-PREFIX");
				}
				for (int pos = pWord.Length - maxLen; pos < pWord.Length; ++pos)
				{
					featuresC.Add(Sharpen.Runtime.Substring(pWord, pos, pWord.Length) + "-PREV-SUFFIX");
				}
				maxLen = nWord.Length;
				if (flags.maxNGramLeng >= 0 && flags.maxNGramLeng < maxLen)
				{
					maxLen = flags.maxNGramLeng;
				}
				for (int len_1 = 1; len_1 <= maxLen; ++len_1)
				{
					featuresC.Add(Sharpen.Runtime.Substring(nWord, 0, len_1) + "-NEXT-PREFIX");
				}
				for (int pos_1 = nWord.Length - maxLen; pos_1 < nWord.Length; ++pos_1)
				{
					featuresC.Add(Sharpen.Runtime.Substring(nWord, pos_1, nWord.Length) + "-NEXT-SUFFIX");
				}
			}
			return featuresC;
		}

		/// <summary>Binary feature annotations</summary>
		private class Bin1Annotation : ICoreAnnotation<string>
		{
			// end featuresC()
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class Bin2Annotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class Bin3Annotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class Bin4Annotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class Bin5Annotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class Bin6Annotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		protected internal virtual ICollection<string> FeaturesCpC(PaddedList<In> cInfo, int loc)
		{
			CoreLabel p = cInfo[loc - 1];
			CoreLabel c = cInfo[loc];
			CoreLabel n = cInfo[loc + 1];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string cDS = c.GetString<CoreAnnotations.DistSimAnnotation>();
			string pDS = p.GetString<CoreAnnotations.DistSimAnnotation>();
			string cShape = c.GetString<CoreAnnotations.ShapeAnnotation>();
			string pShape = p.GetString<CoreAnnotations.ShapeAnnotation>();
			ICollection<string> featuresCpC = new List<string>();
			if (flags.noEdgeFeature)
			{
				return featuresCpC;
			}
			if (flags.transitionEdgeOnly)
			{
				featuresCpC.Add("PSEQ");
				return featuresCpC;
			}
			if (flags.useNeighborNGrams)
			{
				int maxLen = pWord.Length;
				if (flags.maxNGramLeng >= 0 && flags.maxNGramLeng < maxLen)
				{
					maxLen = flags.maxNGramLeng;
				}
				for (int len = 1; len <= maxLen; ++len)
				{
					featuresCpC.Add(Sharpen.Runtime.Substring(pWord, 0, len) + "-PREVIOUS-PREFIX");
				}
				for (int pos = pWord.Length - maxLen; pos < pWord.Length; ++pos)
				{
					featuresCpC.Add(Sharpen.Runtime.Substring(pWord, pos, pWord.Length) + "-PREVIOUS-SUFFIX");
				}
				maxLen = cWord.Length;
				if (flags.maxNGramLeng >= 0 && flags.maxNGramLeng < maxLen)
				{
					maxLen = flags.maxNGramLeng;
				}
				for (int len_1 = 1; len_1 <= maxLen; ++len_1)
				{
					featuresCpC.Add(Sharpen.Runtime.Substring(cWord, 0, len_1) + "-CURRENT-PREFIX");
				}
				for (int pos_1 = cWord.Length - maxLen; pos_1 < cWord.Length; ++pos_1)
				{
					featuresCpC.Add(Sharpen.Runtime.Substring(cWord, pos_1, cWord.Length) + "-CURRENT-SUFFIX");
				}
			}
			if (flags.useInternal && flags.useExternal)
			{
				if (flags.useOrdinal)
				{
					if (IsOrdinal(cInfo, loc))
					{
						featuresCpC.Add("C_ORDINAL");
						if (IsOrdinal(cInfo, loc - 1))
						{
							featuresCpC.Add("PC_ORDINAL");
						}
					}
					if (IsOrdinal(cInfo, loc - 1))
					{
						featuresCpC.Add("P_ORDINAL");
					}
				}
				if (flags.useAbbr || flags.useMinimalAbbr)
				{
					featuresCpC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PABBRANS");
				}
				if (flags.useAbbr1 || flags.useMinimalAbbr1)
				{
					if (!c.Get(typeof(CoreAnnotations.AbbrAnnotation)).Equals("XX"))
					{
						featuresCpC.Add(p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-PABBRANS");
					}
				}
				if (flags.useChunkySequences)
				{
					featuresCpC.Add(p.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + n.Get(typeof(CoreAnnotations.ChunkAnnotation)) + "-PCNCHUNK");
				}
				if (flags.usePrev)
				{
					if (flags.useSequences && flags.usePrevSequences)
					{
						featuresCpC.Add("PSEQ");
						featuresCpC.Add(cWord + "-PSEQW");
						if (!flags.strictGoodCoNLL)
						{
							featuresCpC.Add(pWord + '-' + cWord + "-PSEQW2");
							// added later after goodCoNLL
							featuresCpC.Add(pWord + "-PSEQpW");
						}
						// added later after goodCoNLL
						if (flags.useDistSim)
						{
							featuresCpC.Add(pDS + "-PSEQpDS");
							featuresCpC.Add(cDS + "-PSEQcDS");
							featuresCpC.Add(pDS + '-' + cDS + "-PSEQpcDS");
						}
						if (((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings))
						{
							if (!flags.strictGoodCoNLL)
							{
								// These ones were added later after goodCoNLL
								featuresCpC.Add(pShape + "-PSEQpS");
								featuresCpC.Add(cShape + "-PSEQcS");
							}
							if (flags.strictGoodCoNLL && !flags.removeStrictGoodCoNLLDuplicates)
							{
								featuresCpC.Add(pShape + '-' + cShape + "-PSEQpcS");
							}
						}
					}
				}
				// Duplicate (in goodCoNLL orig, see -TYPES below)
				if (((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings) && flags.useTypeSeqs && (flags.useTypeSeqs2 || flags.useTypeSeqs3))
				{
					if (flags.useTypeSeqs3)
					{
						featuresCpC.Add(pShape + '-' + cShape + '-' + n.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-PCNSHAPES");
					}
					if (flags.useTypeSeqs2)
					{
						featuresCpC.Add(pShape + '-' + cShape + "-TYPES");
					}
					// this duplicates PSEQpcS above
					if (flags.useYetMoreCpCShapes)
					{
						string p2Shape = cInfo[loc - 2].GetString<CoreAnnotations.ShapeAnnotation>();
						featuresCpC.Add(p2Shape + '-' + pShape + '-' + cShape + "-YMS");
						featuresCpC.Add(pShape + '-' + cShape + "-" + n.GetString<CoreAnnotations.ShapeAnnotation>() + "-YMSPCN");
					}
				}
				if (flags.useTypeySequences)
				{
					featuresCpC.Add(cShape + "-TPS2");
					featuresCpC.Add(n.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-TNS1");
				}
				// featuresCpC.add(pShape) + "-" + cShape) + "-TPS"); // duplicates -TYPES, so now omitted; you may need to slightly increase sigma to duplicate previous results, however.
				if (flags.useTaggySequences)
				{
					if (flags.useTags)
					{
						featuresCpC.Add(p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TS");
					}
					if (flags.useDistSim)
					{
						featuresCpC.Add(p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM_TS1");
					}
				}
				if (flags.useParenMatching)
				{
					if (flags.useReverse)
					{
						if (cWord.Equals("(") || cWord.Equals("[") || cWord.Equals("-LRB-"))
						{
							if (pWord.Equals(")") || pWord.Equals("]") || pWord.Equals("-RRB-"))
							{
								featuresCpC.Add("PAREN-MATCH");
							}
						}
					}
					else
					{
						if (cWord.Equals(")") || cWord.Equals("]") || cWord.Equals("-RRB-"))
						{
							if (pWord.Equals("(") || pWord.Equals("[") || pWord.Equals("-LRB-"))
							{
								featuresCpC.Add("PAREN-MATCH");
							}
						}
					}
				}
				if (flags.useEntityTypeSequences)
				{
					featuresCpC.Add(p.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) + "-ETSEQ");
				}
				if (flags.useURLSequences)
				{
					featuresCpC.Add(p.Get(typeof(CoreAnnotations.IsURLAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.IsURLAnnotation)) + "-URLSEQ");
				}
			}
			else
			{
				if (flags.useInternal)
				{
					if (flags.useSequences && flags.usePrevSequences)
					{
						featuresCpC.Add("PSEQ");
						featuresCpC.Add(cWord + "-PSEQW");
					}
					if (flags.useTypeySequences)
					{
						featuresCpC.Add(cShape + "-TPS2");
					}
				}
				else
				{
					if (flags.useExternal)
					{
						if (((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings) && flags.useTypeSeqs && (flags.useTypeSeqs2 || flags.useTypeSeqs3))
						{
							if (flags.useTypeSeqs3)
							{
								featuresCpC.Add(pShape + '-' + cShape + '-' + n.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-PCNSHAPES");
							}
							if (flags.useTypeSeqs2)
							{
								featuresCpC.Add(pShape + '-' + cShape + "-TYPES");
							}
						}
						if (flags.useTypeySequences)
						{
							featuresCpC.Add(n.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-TNS1");
							featuresCpC.Add(pShape + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-TPS");
						}
					}
				}
			}
			return featuresCpC;
		}

		protected internal virtual ICollection<string> FeaturesCp2C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string p2Word = GetWord(p2);
			ICollection<string> featuresCp2C = new List<string>();
			if (flags.useMoreAbbr)
			{
				featuresCp2C.Add(p2.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-P2ABBRANS");
			}
			if (flags.useMinimalAbbr)
			{
				featuresCp2C.Add(p2.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-P2AP2CABB");
			}
			if (flags.useMinimalAbbr1)
			{
				if (!c.Get(typeof(CoreAnnotations.AbbrAnnotation)).Equals("XX"))
				{
					featuresCp2C.Add(p2.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-P2AP2CABB");
				}
			}
			if (flags.useParenMatching)
			{
				if (flags.useReverse)
				{
					if (cWord.Equals("(") || cWord.Equals("[") || cWord.Equals("-LRB-"))
					{
						if ((p2Word.Equals(")") || p2Word.Equals("]") || p2Word.Equals("-RRB-")) && !(pWord.Equals(")") || pWord.Equals("]") || pWord.Equals("-RRB-")))
						{
							featuresCp2C.Add("PAREN-MATCH");
						}
					}
				}
				else
				{
					if (cWord.Equals(")") || cWord.Equals("]") || cWord.Equals("-RRB-"))
					{
						if ((p2Word.Equals("(") || p2Word.Equals("[") || p2Word.Equals("-LRB-")) && !(pWord.Equals("(") || pWord.Equals("[") || pWord.Equals("-LRB-")))
						{
							featuresCp2C.Add("PAREN-MATCH");
						}
					}
				}
			}
			return featuresCp2C;
		}

		protected internal virtual ICollection<string> FeaturesCp3C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string p2Word = GetWord(p2);
			string p3Word = GetWord(p3);
			ICollection<string> featuresCp3C = new List<string>();
			if (flags.useParenMatching)
			{
				if (flags.useReverse)
				{
					if (cWord.Equals("(") || cWord.Equals("["))
					{
						if ((flags.maxLeft >= 3) && (p3Word.Equals(")") || p3Word.Equals("]")) && !(p2Word.Equals(")") || p2Word.Equals("]") || pWord.Equals(")") || pWord.Equals("]")))
						{
							featuresCp3C.Add("PAREN-MATCH");
						}
					}
				}
				else
				{
					if (cWord.Equals(")") || cWord.Equals("]"))
					{
						if ((flags.maxLeft >= 3) && (p3Word.Equals("(") || p3Word.Equals("[")) && !(p2Word.Equals("(") || p2Word.Equals("[") || pWord.Equals("(") || pWord.Equals("[")))
						{
							featuresCp3C.Add("PAREN-MATCH");
						}
					}
				}
			}
			return featuresCp3C;
		}

		protected internal virtual ICollection<string> FeaturesCp4C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			CoreLabel p4 = cInfo[loc - 4];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string p2Word = GetWord(p2);
			string p3Word = GetWord(p3);
			string p4Word = GetWord(p4);
			ICollection<string> featuresCp4C = new List<string>();
			if (flags.useParenMatching)
			{
				if (flags.useReverse)
				{
					if (cWord.Equals("(") || cWord.Equals("["))
					{
						if ((flags.maxLeft >= 4) && (p4Word.Equals(")") || p4Word.Equals("]")) && !(p3Word.Equals(")") || p3Word.Equals("]") || p2Word.Equals(")") || p2Word.Equals("]") || pWord.Equals(")") || pWord.Equals("]")))
						{
							featuresCp4C.Add("PAREN-MATCH");
						}
					}
				}
				else
				{
					if (cWord.Equals(")") || cWord.Equals("]"))
					{
						if ((flags.maxLeft >= 4) && (p4Word.Equals("(") || p4Word.Equals("[")) && !(p3Word.Equals("(") || p3Word.Equals("[") || p2Word.Equals("(") || p2Word.Equals("[") || pWord.Equals("(") || pWord.Equals("[")))
						{
							featuresCp4C.Add("PAREN-MATCH");
						}
					}
				}
			}
			return featuresCp4C;
		}

		protected internal virtual ICollection<string> FeaturesCp5C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			CoreLabel p4 = cInfo[loc - 4];
			CoreLabel p5 = cInfo[loc - 5];
			string cWord = GetWord(c);
			string pWord = GetWord(p);
			string p2Word = GetWord(p2);
			string p3Word = GetWord(p3);
			string p4Word = GetWord(p4);
			string p5Word = GetWord(p5);
			ICollection<string> featuresCp5C = new List<string>();
			if (flags.useParenMatching)
			{
				if (flags.useReverse)
				{
					if (cWord.Equals("(") || cWord.Equals("["))
					{
						if ((flags.maxLeft >= 5) && (p5Word.Equals(")") || p5Word.Equals("]")) && !(p4Word.Equals(")") || p4Word.Equals("]") || p3Word.Equals(")") || p3Word.Equals("]") || p2Word.Equals(")") || p2Word.Equals("]") || pWord.Equals(")") || pWord.Equals
							("]")))
						{
							featuresCp5C.Add("PAREN-MATCH");
						}
					}
				}
				else
				{
					if (cWord.Equals(")") || cWord.Equals("]"))
					{
						if ((flags.maxLeft >= 5) && (p5Word.Equals("(") || p5Word.Equals("[")) && !(p4Word.Equals("(") || p4Word.Equals("[") || p3Word.Equals("(") || p3Word.Equals("[") || p2Word.Equals("(") || p2Word.Equals("[") || pWord.Equals("(") || pWord.Equals
							("[")))
						{
							featuresCp5C.Add("PAREN-MATCH");
						}
					}
				}
			}
			return featuresCp5C;
		}

		protected internal virtual ICollection<string> FeaturesCpCp2C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			string pWord = GetWord(p);
			// String p2Word = getWord(p2);
			ICollection<string> featuresCpCp2C = new List<string>();
			if (flags.useInternal && flags.useExternal)
			{
				if (flags.strictGoodCoNLL && !flags.removeStrictGoodCoNLLDuplicates && flags.useTypeySequences && flags.maxLeft >= 2)
				{
					// this feature duplicates -TYPETYPES below, so probably don't include it, but it was in original tests of CMM goodCoNLL
					featuresCpCp2C.Add(p2.Get(typeof(CoreAnnotations.ShapeAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.ShapeAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-TTPS");
				}
				if (flags.useAbbr)
				{
					featuresCpCp2C.Add(p2.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.AbbrAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.AbbrAnnotation)) + "-2PABBRANS");
				}
				if (flags.useChunks)
				{
					featuresCpCp2C.Add(p2.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.ChunkAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ChunkAnnotation)) + "-2PCHUNKS");
				}
				if (flags.useLongSequences)
				{
					featuresCpCp2C.Add("PPSEQ");
				}
				if (flags.useBoundarySequences && pWord.Equals(CoNLLDocumentReaderAndWriter.Boundary))
				{
					featuresCpCp2C.Add("BNDRY-SPAN-PPSEQ");
				}
				// This more complex consistency checker didn't help!
				// if (flags.useBoundarySequences) {
				//   // try enforce consistency over "and" and "," as well as boundary
				//   if (pWord.equals(CoNLLDocumentIteratorFactory.BOUNDARY) ||
				//       pWord.equalsIgnoreCase("and") || pWord.equalsIgnoreCase("or") ||
				//       pWord.equals(",")) {
				//   }
				// }
				if (flags.useTaggySequences)
				{
					if (flags.useTags)
					{
						featuresCpCp2C.Add(p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + "-TTS");
						if (flags.useTaggySequencesShapeInteraction)
						{
							featuresCpCp2C.Add(p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation
								)) + "-TTS-CS");
						}
					}
					if (flags.useDistSim)
					{
						featuresCpCp2C.Add(p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + "-DISTSIM_TTS1");
						if (flags.useTaggySequencesShapeInteraction)
						{
							featuresCpCp2C.Add(p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-DISTSIM_TTS1-CS"
								);
						}
					}
				}
				if (((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings) && flags.useTypeSeqs && flags.useTypeSeqs2 && flags.maxLeft >= 2)
				{
					string cShape = c.Get(typeof(CoreAnnotations.ShapeAnnotation));
					string pShape = p.Get(typeof(CoreAnnotations.ShapeAnnotation));
					string p2Shape = p2.Get(typeof(CoreAnnotations.ShapeAnnotation));
					featuresCpCp2C.Add(p2Shape + '-' + pShape + '-' + cShape + "-TYPETYPES");
				}
			}
			else
			{
				if (flags.useInternal)
				{
					if (flags.useLongSequences)
					{
						featuresCpCp2C.Add("PPSEQ");
					}
				}
				else
				{
					if (flags.useExternal)
					{
						if (flags.useLongSequences)
						{
							featuresCpCp2C.Add("PPSEQ");
						}
						if (((flags.wordShape > WordShapeClassifier.Nowordshape) || flags.useShapeStrings) && flags.useTypeSeqs && flags.useTypeSeqs2 && flags.maxLeft >= 2)
						{
							string cShape = c.Get(typeof(CoreAnnotations.ShapeAnnotation));
							string pShape = p.Get(typeof(CoreAnnotations.ShapeAnnotation));
							string p2Shape = p2.Get(typeof(CoreAnnotations.ShapeAnnotation));
							featuresCpCp2C.Add(p2Shape + '-' + pShape + '-' + cShape + "-TYPETYPES");
						}
					}
				}
			}
			return featuresCpCp2C;
		}

		protected internal virtual ICollection<string> FeaturesCpCp2Cp3C(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			ICollection<string> featuresCpCp2Cp3C = new List<string>();
			if (flags.useTaggySequences)
			{
				if (flags.useTags)
				{
					if (flags.maxLeft >= 3 && !flags.dontExtendTaggy)
					{
						featuresCpCp2Cp3C.Add(p3.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation
							>() + "-TTTS");
						if (flags.useTaggySequencesShapeInteraction)
						{
							featuresCpCp2Cp3C.Add(p3.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p2.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + p.GetString<CoreAnnotations.PartOfSpeechAnnotation>() + '-' + c.GetString<CoreAnnotations.PartOfSpeechAnnotation
								>() + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-TTTS-CS");
						}
					}
				}
				if (flags.useDistSim)
				{
					if (flags.maxLeft >= 3 && !flags.dontExtendTaggy)
					{
						featuresCpCp2Cp3C.Add(p3.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation
							)) + "-DISTSIM_TTTS1");
						if (flags.useTaggySequencesShapeInteraction)
						{
							featuresCpCp2Cp3C.Add(p3.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p2.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + p.Get(typeof(CoreAnnotations.DistSimAnnotation)) + '-' + c.Get(typeof(CoreAnnotations.DistSimAnnotation
								)) + '-' + c.Get(typeof(CoreAnnotations.ShapeAnnotation)) + "-DISTSIM_TTTS1-CS");
						}
					}
				}
			}
			if (flags.maxLeft >= 3)
			{
				if (flags.useLongSequences)
				{
					featuresCpCp2Cp3C.Add("PPPSEQ");
				}
				if (flags.useBoundarySequences && GetWord(p).Equals(CoNLLDocumentReaderAndWriter.Boundary))
				{
					featuresCpCp2Cp3C.Add("BNDRY-SPAN-PPPSEQ");
				}
			}
			return featuresCpCp2Cp3C;
		}

		protected internal virtual ICollection<string> FeaturesCpCp2Cp3Cp4C(PaddedList<In> cInfo, int loc)
		{
			ICollection<string> featuresCpCp2Cp3Cp4C = new List<string>();
			CoreLabel p = cInfo[loc - 1];
			if (flags.maxLeft >= 4)
			{
				if (flags.useLongSequences)
				{
					featuresCpCp2Cp3Cp4C.Add("PPPPSEQ");
				}
				if (flags.useBoundarySequences && GetWord(p).Equals(CoNLLDocumentReaderAndWriter.Boundary))
				{
					featuresCpCp2Cp3Cp4C.Add("BNDRY-SPAN-PPPPSEQ");
				}
			}
			return featuresCpCp2Cp3Cp4C;
		}

		protected internal virtual ICollection<string> FeaturesCnC(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			ICollection<string> featuresCnC = new List<string>();
			if (flags.useNext)
			{
				if (flags.useSequences && flags.useNextSequences)
				{
					featuresCnC.Add("NSEQ");
					featuresCnC.Add(GetWord(c) + "-NSEQW");
				}
			}
			return featuresCnC;
		}

		protected internal virtual ICollection<string> FeaturesCpCnC(PaddedList<In> cInfo, int loc)
		{
			CoreLabel c = cInfo[loc];
			ICollection<string> featuresCpCnC = new List<string>();
			if (flags.useNext && flags.usePrev)
			{
				if (flags.useSequences && flags.usePrevSequences && flags.useNextSequences)
				{
					featuresCpCnC.Add("PNSEQ");
					featuresCpCnC.Add(GetWord(c) + "-PNSEQW");
				}
			}
			return featuresCpCnC;
		}

		private int Reverse(int i)
		{
			return (flags.useReverse ? -1 * i : i);
		}

		private ICollection<string> OccurrencePatterns(PaddedList<In> cInfo, int loc)
		{
			// features on last Cap
			string word = GetWord(cInfo[loc]);
			string nWord = GetWord(cInfo[loc + Reverse(1)]);
			CoreLabel p = cInfo[loc - Reverse(1)];
			string pWord = GetWord(p);
			// log.info(word+" "+nWord);
			if (!(IsNameCase(word) && NoUpperCase(nWord) && HasLetter(nWord) && HasLetter(pWord) && p != cInfo.GetPad()))
			{
				return Java.Util.Collections.SingletonList("NO-OCCURRENCE-PATTERN");
			}
			// log.info("LOOKING");
			ICollection<string> l = Generics.NewHashSet();
			if (cInfo[loc - Reverse(1)].GetString<CoreAnnotations.PartOfSpeechAnnotation>() != null && IsNameCase(pWord) && cInfo[loc - Reverse(1)].GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP"))
			{
				for (int jump = 3; jump < 150; jump++)
				{
					if (GetWord(cInfo[loc + Reverse(jump)]).Equals(word))
					{
						if (GetWord(cInfo[loc + Reverse(jump - 1)]).Equals(pWord))
						{
							l.Add("XY-NEXT-OCCURRENCE-XY");
						}
						else
						{
							l.Add("XY-NEXT-OCCURRENCE-Y");
						}
					}
				}
				for (int jump_1 = -3; jump_1 > -150; jump_1--)
				{
					if (GetWord(cInfo[loc + Reverse(jump_1)]).Equals(word))
					{
						if (GetWord(cInfo[loc + Reverse(jump_1 - 1)]).Equals(pWord))
						{
							l.Add("XY-PREV-OCCURRENCE-XY");
						}
						else
						{
							l.Add("XY-PREV-OCCURRENCE-Y");
						}
					}
				}
			}
			else
			{
				for (int jump = 3; jump < 150; jump++)
				{
					if (GetWord(cInfo[loc + Reverse(jump)]).Equals(word))
					{
						if (IsNameCase(GetWord(cInfo[loc + Reverse(jump - 1)])) && (cInfo[loc + Reverse(jump - 1)]).GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP"))
						{
							l.Add("X-NEXT-OCCURRENCE-YX");
						}
						else
						{
							// log.info(getWord(cInfo.get(loc+reverse(jump-1))));
							if (IsNameCase(GetWord(cInfo[loc + Reverse(jump + 1)])) && (cInfo[loc + Reverse(jump + 1)]).GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP"))
							{
								// log.info(getWord(cInfo.get(loc+reverse(jump+1))));
								l.Add("X-NEXT-OCCURRENCE-XY");
							}
							else
							{
								l.Add("X-NEXT-OCCURRENCE-X");
							}
						}
					}
				}
				for (int jump_1 = -3; jump_1 > -150; jump_1--)
				{
					if (GetWord(cInfo[loc + jump_1]) != null && GetWord(cInfo[loc + jump_1]).Equals(word))
					{
						if (IsNameCase(GetWord(cInfo[loc + Reverse(jump_1 + 1)])) && (cInfo[loc + Reverse(jump_1 + 1)]).GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP"))
						{
							l.Add("X-PREV-OCCURRENCE-YX");
						}
						else
						{
							// log.info(getWord(cInfo.get(loc+reverse(jump+1))));
							if (IsNameCase(GetWord(cInfo[loc + Reverse(jump_1 - 1)])) && cInfo[loc + Reverse(jump_1 - 1)].GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP"))
							{
								l.Add("X-PREV-OCCURRENCE-XY");
							}
							else
							{
								// log.info(getWord(cInfo.get(loc+reverse(jump-1))));
								l.Add("X-PREV-OCCURRENCE-X");
							}
						}
					}
				}
			}
			/*
			if (!l.isEmpty()) {
			log.info(pWord+" "+word+" "+nWord+" "+l);
			}
			*/
			return l;
		}

		private string Intern(string s)
		{
			if (flags.intern)
			{
				return string.Intern(s);
			}
			else
			{
				return s;
			}
		}

		private void InitGazette()
		{
			try
			{
				// read in gazettes
				if (flags.gazettes == null)
				{
					flags.gazettes = new List<string>();
				}
				IList<string> gazettes = flags.gazettes;
				foreach (string gazetteFile in gazettes)
				{
					using (BufferedReader r = IOUtils.ReaderFromString(gazetteFile, flags.inputEncoding))
					{
						ReadGazette(r);
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}
	}
}
