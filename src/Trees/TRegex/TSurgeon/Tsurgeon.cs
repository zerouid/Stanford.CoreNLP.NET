// Tsurgeon
// Copyright (c) 2004-2016 The Board of Trustees of
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
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: parser-user@lists.stanford.edu
//    Licensing: parser-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/tregex.html
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// Tsurgeon provides a way of editing trees based on a set of operations that
	/// are applied to tree locations matching a tregex pattern.
	/// </summary>
	/// <remarks>
	/// Tsurgeon provides a way of editing trees based on a set of operations that
	/// are applied to tree locations matching a tregex pattern.
	/// A simple example from the command-line:
	/// <blockquote>
	/// java edu.stanford.nlp.trees.tregex.tsurgeon.Tsurgeon -treeFile aTree
	/// exciseNP renameVerb
	/// </blockquote>
	/// The file
	/// <c>aTree</c>
	/// has Penn Treebank (S-expression) format trees.
	/// The other (here, two) files have Tsurgeon operations.  These consist of
	/// a list of pairs of a tregex expression on one or more
	/// lines, a blank line, and then some number of lines of Tsurgeon operations and then
	/// another blank line.
	/// <p>
	/// Tsurgeon uses the Tregex engine to match tree patterns on trees;
	/// for more information on Tregex's tree-matching functionality,
	/// syntax, and semantics, please see the documentation for the
	/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
	/// class.
	/// <p>
	/// If you want to use Tsurgeon as an API, the relevant method is
	/// <see cref="ProcessPattern(Edu.Stanford.Nlp.Trees.Tregex.TregexPattern, TsurgeonPattern, Edu.Stanford.Nlp.Trees.Tree)"/>
	/// .  You will also need to look at the
	/// <see cref="TsurgeonPattern"/>
	/// class and the
	/// <see cref="ParseOperation(string)"/>
	/// method.
	/// <p>
	/// Here's the simplest form of invocation on a single Tree:
	/// <pre>
	/// Tree t = Tree.valueOf("(ROOT (S (NP (NP (NNP Bank)) (PP (IN of) (NP (NNP America)))) (VP (VBD called)) (. .)))");
	/// TregexPattern pat = TregexPattern.compile("NP &lt;1 (NP &lt;&lt; Bank) &lt;2 PP=remove");
	/// TsurgeonPattern surgery = Tsurgeon.parseOperation("excise remove remove");
	/// Tsurgeon.processPattern(pat, surgery, t).pennPrint();
	/// </pre>
	/// <p>
	/// Here is another sample invocation:
	/// <pre>
	/// TregexPattern matchPattern = TregexPattern.compile("SQ=sq &lt; (/^WH/ $++ VP)");
	/// List<TsurgeonPattern> ps = new ArrayList<TsurgeonPattern>();
	/// TsurgeonPattern p = Tsurgeon.parseOperation("relabel sq S");
	/// ps.add(p);
	/// Treebank lTrees;
	/// List<Tree> result = Tsurgeon.processPatternOnTrees(matchPattern,Tsurgeon.collectOperations(ps),lTrees);
	/// </pre>
	/// <p>
	/// <i>Note:</i> If you want to apply multiple surgery patterns, you
	/// will not want to call processPatternOnTrees, for each individual
	/// pattern.  Rather, you should either call processPatternsOnTree and
	/// loop through the trees yourself, or, as above, use
	/// <c>collectOperations</c>
	/// to collect all the surgery patterns
	/// into one TsurgeonPattern, and then to call processPatternOnTrees.
	/// Either of these latter methods is much faster.
	/// </p><p>
	/// The parser also has the ability to collect multiple
	/// TsurgeonPatterns into one pattern by itself by enclosing each
	/// pattern in
	/// <c>[ ... ]</c>
	/// .  For example,
	/// <br />
	/// <c>Tsurgeon.parseOperation("[relabel foo BAR] [prune bar]")</c>
	/// </p><p>
	/// For more information on using Tsurgeon from the command line,
	/// see the
	/// <see cref="Main(string[])"/>
	/// method and the package Javadoc.
	/// </remarks>
	/// <author>Roger Levy</author>
	public class Tsurgeon
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon));

		private const bool Debug = false;

		internal static bool verbose;

		private static readonly Pattern emptyLinePattern = Pattern.Compile("^\\s*$");

		private const string commentIntroducingCharacter = "%";

		private static readonly Pattern commentPattern = Pattern.Compile("(?<!\\\\)%.*$");

		private static readonly Pattern escapedCommentCharacterPattern = Pattern.Compile("\\\\" + commentIntroducingCharacter);

		private Tsurgeon()
		{
		}

		// = false;
		// not an instantiable class
		/// <summary>Usage: java edu.stanford.nlp.trees.tregex.tsurgeon.Tsurgeon [-s] -treeFile file-with-trees [-po matching-pattern operation] operation-file-1 operation-file-2 ...</summary>
		/// <remarks>
		/// Usage: java edu.stanford.nlp.trees.tregex.tsurgeon.Tsurgeon [-s] -treeFile file-with-trees [-po matching-pattern operation] operation-file-1 operation-file-2 ... operation-file-n
		/// <h4>Arguments:</h4>
		/// Each argument should be the name of a transformation file that contains a list of pattern
		/// and transformation operation list pairs.  That is, it is a sequence of pairs of a
		/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
		/// pattern on one or more lines, then a
		/// blank line (empty or whitespace), then a list of transformation operations one per line
		/// (as specified by <b>Legal operation syntax</b> below) to apply when the pattern is matched,
		/// and then another blank line (empty or whitespace).
		/// Note the need for blank lines: The code crashes if they are not present as separators
		/// (although the blank line at the end of the file can be omitted).
		/// The script file can include comment lines, either whole comment lines or
		/// trailing comments introduced by %, which extend to the end of line.  A needed percent
		/// mark can be escaped by a preceding backslash.
		/// <p>
		/// For example, if you want to excise an SBARQ node whenever it is the parent of an SQ node,
		/// and relabel the SQ node to S, your transformation file would look like this:
		/// <blockquote>
		/// <code>
		/// SBARQ=n1 &lt; SQ=n2<br />
		/// <br />
		/// excise n1 n1<br />
		/// relabel n2 S
		/// </code>
		/// </blockquote>
		/// <h4>Options:</h4>
		/// <ul>
		/// <li>
		/// <c>-treeFile &lt;filename&gt;</c>
		/// specify the name of the file that has the trees you want to transform.
		/// <li>
		/// <c>-po &lt;matchPattern&gt; &lt;operation&gt;</c>
		/// Apply a single operation to every tree using the specified match pattern and the specified operation.  Use this option
		/// when you want to quickly try the effect of one pattern/surgery combination, and are too lazy to write a transformation file.
		/// <li>
		/// <c>-s</c>
		/// Print each output tree on one line (default is pretty-printing).
		/// <li>
		/// <c>-m</c>
		/// For every tree that had a matching pattern, print "before" (prepended as "Operated on:") and "after" (prepended as "Result:").  Unoperated on trees just pass through the transducer as usual.
		/// <li>
		/// <c>-encoding X</c>
		/// Uses character set X for input and output of trees.
		/// <li>
		/// <c>-macros &lt;filename&gt;</c>
		/// A file of macros to use on the tregex pattern.  Macros should be one per line, with original and replacement separated by tabs.
		/// <li>
		/// <c>-hf &lt;headFinder-class-name&gt;</c>
		/// use the specified
		/// <see cref="Edu.Stanford.Nlp.Trees.IHeadFinder"/>
		/// class to determine headship relations.
		/// <li>
		/// <c>-hfArg &lt;string&gt;</c>
		/// pass a string argument in to the
		/// <see cref="Edu.Stanford.Nlp.Trees.IHeadFinder"/>
		/// class's constructor.
		/// <c>-hfArg</c>
		/// can be used multiple times to pass in multiple arguments.
		/// <li>
		/// <c>-trf &lt;TreeReaderFactory-class-name&gt;</c>
		/// use the specified
		/// <see cref="Edu.Stanford.Nlp.Trees.ITreeReaderFactory"/>
		/// class to read trees from files.
		/// </ul>
		/// <h4>Legal operation syntax:</h4>
		/// <ul>
		/// <li>
		/// <c>delete &lt;name&gt;</c>
		/// deletes the node and everything below it.
		/// <li>
		/// <c>prune &lt;name&gt;</c>
		/// Like delete, but if, after the pruning, the parent has no children anymore, the parent is pruned too.  Pruning continues to affect all ancestors until one is found with remaining children.  This may result in a null tree.
		/// <li>
		/// <c>excise &lt;name1&gt; &lt;name2&gt;</c>
		/// The name1 node should either dominate or be the same as the name2 node.  This excises out everything from
		/// name1 to name2.  All the children of name2 go into the parent of name1, where name1 was.
		/// <li>
		/// <c>relabel &lt;name&gt; &lt;new-label&gt;</c>
		/// Relabels the node to have the new label. <br />
		/// There are three possible forms: <br />
		/// <c>relabel nodeX VP</c>
		/// - for changing a node label to an
		/// alphanumeric string <br />
		/// <c>relabel nodeX /''/</c>
		/// - for relabeling a node to
		/// something that isn't a valid identifier without quoting <br />
		/// <c>relabel nodeX /^VB(.*)$/verb\\/$1/</c>
		/// - for regular
		/// expression based relabeling. In this case, all matches of the
		/// regular expression against the node label are replaced with the
		/// replacement String.  This has the semantics of Java/Perl's
		/// replaceAll: you may use capturing groups and put them in
		/// replacements with $n. For example, if the pattern is /foo/bar/
		/// and the node matched is "foo", the replaceAll semantics result in
		/// "barbar".  If the pattern is /^foo(.*)$/bar$1/ and node matched is
		/// "foofoo", relabel will result in "barfoo".  <br />
		/// When using the regex replacement method, you can also use the
		/// sequences ={node} and %{var} in the replacement string to use
		/// captured nodes or variable strings in the replacement string.
		/// For example, if the Tregex pattern was "duck=bar" and the relabel
		/// is /foo/={bar}/, "foofoo" will be replaced with "duckduck". <br />
		/// To concatenate two nodes named in the tregex pattern, for
		/// example, you can use the pattern /^.*$/={foo}={bar}/.  Note that
		/// the ^.*$ is necessary to make sure the regex pattern only matches
		/// and replaces once on the entire node name. <br />
		/// To get an "=" or a "%" in the replacement, using \ escaping.
		/// Also, as in the example you can escape a slash in the middle of
		/// the second and third forms with \\/ and \\\\. <br />
		/// <li>
		/// <c>insert &lt;name&gt; &lt;position&gt;</c>
		/// or
		/// <c>insert &lt;tree&gt; &lt;position&gt;</c>
		/// inserts the named node or tree into the position specified.
		/// <li>
		/// <c>move &lt;name&gt; &lt;position&gt;</c>
		/// moves the named node into the specified position.
		/// <p>Right now the  only ways to specify position are:
		/// <p>
		/// <c>$+ &lt;name&gt;</c>
		/// the left sister of the named node<br />
		/// <c>$- &lt;name&gt;</c>
		/// the right sister of the named node<br />
		/// <c>&gt;i &lt;name&gt;</c>
		/// the i_th daughter of the named node<br />
		/// <c>&gt;-i &lt;name&gt;</c>
		/// the i_th daughter, counting from the right, of the named node.
		/// <li>
		/// <c>replace &lt;name1&gt; &lt;name2&gt;</c>
		/// deletes name1 and inserts a copy of name2 in its place.
		/// <li>
		/// <c>replace &lt;name&gt; &lt;tree&gt; &lt;tree2&gt;...</c>
		/// deletes name and inserts the new tree(s) in its place.  If
		/// more than one replacement tree is given, each of the new
		/// subtrees will be added in order where the old tree was.
		/// Multiple subtrees at the root is an illegal operation and
		/// will throw an exception.
		/// <li>
		/// <c>createSubtree &lt;auxiliary-tree-or-label&gt; &lt;name1&gt; [&lt;name2&gt;]</c>
		/// Create a subtree out of all the nodes from
		/// <c>&lt;name1&gt;</c>
		/// through
		/// <c>&lt;name2&gt;</c>
		/// . The subtree is moved to the foot of the given
		/// auxiliary tree, and the tree is inserted where the nodes of
		/// the subtree used to reside. If a simple label is provided as
		/// the first argument, the subtree is given a single parent with
		/// a name corresponding to the label.  To limit the operation to
		/// just one node, elide
		/// <c>&lt;name2&gt;</c>
		/// .
		/// <li>
		/// <c>adjoin &lt;auxiliary_tree&gt; &lt;name&gt;</c>
		/// Adjoins the specified auxiliary tree into the named node.
		/// The daughters of the target node will become the daughters of the foot of the auxiliary tree.
		/// <li>
		/// <c>adjoinH &lt;auxiliary_tree&gt; &lt;name&gt;</c>
		/// Similar to adjoin, but preserves the target node
		/// and makes it the root of
		/// <c>&lt;tree&gt;</c>
		/// . (It is still accessible as
		/// <c>name</c>
		/// .  The root of the
		/// auxiliary tree is ignored.)
		/// <li>
		/// <c>adjoinF &lt;auxiliary_tree&gt; &lt;name&gt;</c>
		/// Similar to adjoin,
		/// but preserves the target node and makes it the foot of
		/// <c>&lt;tree&gt;</c>
		/// .
		/// (It is still accessible as
		/// <c>name</c>
		/// , and retains its status as parent of its children.
		/// The root of the auxiliary tree is ignored.)
		/// <li> <dt>
		/// <c>coindex &lt;name1&gt; &lt;name2&gt; ... &lt;nameM&gt;</c>
		/// Puts a (Penn Treebank style)
		/// coindexation suffix of the form "-N" on each of nodes name_1 through name_m.  The value of N will be
		/// automatically generated in reference to the existing coindexations in the tree, so that there is never
		/// an accidental clash of indices across things that are not meant to be coindexed.
		/// </ul>
		/// <p>
		/// In the context of
		/// <c>adjoin</c>
		/// ,
		/// <c>adjoinH</c>
		/// ,
		/// <c>adjoinF</c>
		/// , and
		/// <c>createSubtree</c>
		/// , an auxiliary
		/// tree is a tree in Penn Treebank format with
		/// <c>@</c>
		/// on
		/// exactly one of the leaves denoting the foot of the tree.
		/// The operations which use the foot use the labeled node.
		/// For example:
		/// </p>
		/// <blockquote>
		/// Tsurgeon:
		/// <c>adjoin (FOO (BAR@)) foo</c>
		/// <br />
		/// Tregex:
		/// <c>B=foo</c>
		/// <br />
		/// Input:
		/// <c>(A (B 1 2))</c>
		/// Output:
		/// <c>(A (FOO (BAR 1 2)))</c>
		/// </blockquote>
		/// <p>
		/// Tsurgeon applies the same operation to the same tree for as long
		/// as the given tregex operation matches.  This means that infinite
		/// loops are very easy to cause.  One common situation where this comes up
		/// is with an insert operation will repeats infinitely many times
		/// unless you add an expression to the tregex that matches against
		/// the inserted pattern.  For example, this pattern will infinite loop:
		/// </p>
		/// <blockquote>
		/// <code>
		/// TregexPattern tregex = TregexPattern.compile("S=node &lt;&lt; NP"); <br />
		/// TsurgeonPattern tsurgeon = Tsurgeon.parseOperation("insert (NP foo) &gt;-1 node");
		/// </code>
		/// </blockquote>
		/// <p>
		/// This pattern, though, will terminate:
		/// </p>
		/// <blockquote>
		/// <code>
		/// TregexPattern tregex = TregexPattern.compile("S=node &lt;&lt; NP !&lt;&lt; foo"); <br />
		/// TsurgeonPattern tsurgeon = Tsurgeon.parseOperation("insert (NP foo) &gt;-1 node");
		/// </code>
		/// </blockquote>
		/// <p>
		/// Tsurgeon has (very) limited support for conditional statements.
		/// If a pattern is prefaced with
		/// <c>if exists &lt;name&gt;</c>
		/// ,
		/// the rest of the pattern will only execute if
		/// the named node was found in the corresponding TregexMatcher.
		/// </p>
		/// </remarks>
		/// <param name="args">
		/// a list of names of files each of which contains a single tregex matching pattern plus a list, one per line,
		/// of transformation operations to apply to the matched pattern.
		/// </param>
		/// <exception cref="System.Exception">If an I/O or pattern syntax error</exception>
		public static void Main(string[] args)
		{
			string headFinderClassName = null;
			string headFinderOption = "-hf";
			string[] headFinderArgs = null;
			string headFinderArgOption = "-hfArg";
			string encoding = "UTF-8";
			string encodingOption = "-encoding";
			if (args.Length == 0)
			{
				log.Info("Usage: java edu.stanford.nlp.trees.tregex.tsurgeon.Tsurgeon [-s] -treeFile <file-with-trees> [-po <matching-pattern> <operation>] <operation-file-1> <operation-file-2> ... <operation-file-n>");
				System.Environment.Exit(0);
			}
			string treePrintFormats;
			string singleLineOption = "-s";
			string verboseOption = "-v";
			string matchedOption = "-m";
			// if set, then print original form of trees that are matched & thus operated on
			string patternOperationOption = "-po";
			string treeFileOption = "-treeFile";
			string trfOption = "-trf";
			string macroOption = "-macros";
			string macroFilename = string.Empty;
			IDictionary<string, int> flagMap = Generics.NewHashMap();
			flagMap[patternOperationOption] = 2;
			flagMap[treeFileOption] = 1;
			flagMap[trfOption] = 1;
			flagMap[singleLineOption] = 0;
			flagMap[encodingOption] = 1;
			flagMap[headFinderOption] = 1;
			flagMap[macroOption] = 1;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, flagMap);
			args = argsMap[null];
			if (argsMap.Contains(headFinderOption))
			{
				headFinderClassName = argsMap[headFinderOption][0];
			}
			if (argsMap.Contains(headFinderArgOption))
			{
				headFinderArgs = argsMap[headFinderArgOption];
			}
			if (argsMap.Contains(verboseOption))
			{
				verbose = true;
			}
			if (argsMap.Contains(singleLineOption))
			{
				treePrintFormats = "oneline,";
			}
			else
			{
				treePrintFormats = "penn,";
			}
			if (argsMap.Contains(encodingOption))
			{
				encoding = argsMap[encodingOption][0];
			}
			if (argsMap.Contains(macroOption))
			{
				macroFilename = argsMap[macroOption][0];
			}
			TreePrint tp = new TreePrint(treePrintFormats, new PennTreebankLanguagePack());
			PrintWriter pwOut = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
			ITreeReaderFactory trf;
			if (argsMap.Contains(trfOption))
			{
				string trfClass = argsMap[trfOption][0];
				trf = ReflectionLoading.LoadByReflection(trfClass);
			}
			else
			{
				trf = new TregexPattern.TRegexTreeReaderFactory();
			}
			Treebank trees = new DiskTreebank(trf, encoding);
			if (argsMap.Contains(treeFileOption))
			{
				trees.LoadPath(argsMap[treeFileOption][0]);
			}
			if (trees.IsEmpty())
			{
				log.Info("Warning: No trees specified to operate on.  Use -treeFile path option.");
			}
			TregexPatternCompiler compiler;
			if (headFinderClassName == null)
			{
				compiler = new TregexPatternCompiler();
			}
			else
			{
				IHeadFinder hf;
				if (headFinderArgs == null)
				{
					hf = ReflectionLoading.LoadByReflection(headFinderClassName);
				}
				else
				{
					hf = ReflectionLoading.LoadByReflection(headFinderClassName, (object[])headFinderArgs);
				}
				compiler = new TregexPatternCompiler(hf);
			}
			Macros.AddAllMacros(compiler, macroFilename, encoding);
			IList<Pair<TregexPattern, TsurgeonPattern>> ops = new List<Pair<TregexPattern, TsurgeonPattern>>();
			if (argsMap.Contains(patternOperationOption))
			{
				TregexPattern matchPattern = compiler.Compile(argsMap[patternOperationOption][0]);
				TsurgeonPattern p = ParseOperation(argsMap[patternOperationOption][1]);
				ops.Add(new Pair<TregexPattern, TsurgeonPattern>(matchPattern, p));
			}
			else
			{
				foreach (string arg in args)
				{
					IList<Pair<TregexPattern, TsurgeonPattern>> pairs = GetOperationsFromFile(arg, encoding, compiler);
					foreach (Pair<TregexPattern, TsurgeonPattern> pair in pairs)
					{
						if (verbose)
						{
							log.Info(pair.Second());
						}
						ops.Add(pair);
					}
				}
			}
			foreach (Tree t in trees)
			{
				Tree original = t.DeepCopy();
				Tree result = ProcessPatternsOnTree(ops, t);
				if (argsMap.Contains(matchedOption) && matchedOnTree)
				{
					pwOut.Println("Operated on: ");
					DisplayTree(original, tp, pwOut);
					pwOut.Println("Result: ");
				}
				DisplayTree(result, tp, pwOut);
			}
		}

		private static void DisplayTree(Tree t, TreePrint tp, PrintWriter pw)
		{
			if (t == null)
			{
				pw.Println("null");
			}
			else
			{
				tp.PrintTree(t, pw);
			}
		}

		/// <summary>
		/// Parses a tsurgeon script text input and compiles a tregex pattern and a list
		/// of tsurgeon operations into a pair.
		/// </summary>
		/// <param name="reader">Reader to read patterns from</param>
		/// <returns>
		/// A pair of a tregex and tsurgeon pattern read from a file, or
		/// <see langword="null"/>
		/// when the operations present in the Reader have been exhausted
		/// </returns>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static Pair<TregexPattern, TsurgeonPattern> GetOperationFromReader(BufferedReader reader, TregexPatternCompiler compiler)
		{
			string patternString = GetTregexPatternFromReader(reader);
			// log.info("Read tregex pattern: " + patternString);
			if (patternString.IsEmpty())
			{
				return null;
			}
			TregexPattern matchPattern = compiler.Compile(patternString);
			TsurgeonPattern collectedPattern = GetTsurgeonOperationsFromReader(reader);
			return new Pair<TregexPattern, TsurgeonPattern>(matchPattern, collectedPattern);
		}

		/// <summary>
		/// Assumes that we are at the beginning of a tsurgeon script file and gets the string for the
		/// tregex pattern leading the file.
		/// </summary>
		/// <returns>tregex pattern string. May be empty, never null</returns>
		/// <exception cref="System.IO.IOException">If the usual kinds of IO errors occur</exception>
		public static string GetTregexPatternFromReader(BufferedReader reader)
		{
			StringBuilder matchString = new StringBuilder();
			for (string thisLine; (thisLine = reader.ReadLine()) != null; )
			{
				if (matchString.Length > 0 && emptyLinePattern.Matcher(thisLine).Matches())
				{
					// A blank line after getting some real content (not just comments or nothing)
					break;
				}
				thisLine = RemoveComments(thisLine);
				if (!emptyLinePattern.Matcher(thisLine).Matches())
				{
					matchString.Append(thisLine);
				}
			}
			return matchString.ToString();
		}

		/// <summary>
		/// Assumes the given reader has only tsurgeon operations (not a tregex pattern), and parses
		/// these out, collecting them into one operation.
		/// </summary>
		/// <remarks>
		/// Assumes the given reader has only tsurgeon operations (not a tregex pattern), and parses
		/// these out, collecting them into one operation.  Stops on a whitespace line.
		/// </remarks>
		/// <exception cref="System.IO.IOException">If the usual kinds of IO errors occur</exception>
		public static TsurgeonPattern GetTsurgeonOperationsFromReader(BufferedReader reader)
		{
			IList<TsurgeonPattern> operations = new List<TsurgeonPattern>();
			for (string thisLine; (thisLine = reader.ReadLine()) != null; )
			{
				if (emptyLinePattern.Matcher(thisLine).Matches())
				{
					break;
				}
				thisLine = RemoveComments(thisLine);
				if (emptyLinePattern.Matcher(thisLine).Matches())
				{
					continue;
				}
				// log.info("Read tsurgeon op: " + thisLine);
				operations.Add(ParseOperation(thisLine));
			}
			if (operations.IsEmpty())
			{
				throw new TsurgeonParseException("No Tsurgeon operation provided.");
			}
			return CollectOperations(operations);
		}

		private static string RemoveComments(string line)
		{
			Matcher m = commentPattern.Matcher(line);
			line = m.ReplaceFirst(string.Empty);
			Matcher m1 = escapedCommentCharacterPattern.Matcher(line);
			line = m1.ReplaceAll(commentIntroducingCharacter);
			return line;
		}

		/// <summary>
		/// Assumes the given reader has only tsurgeon operations (not a tregex pattern), and returns
		/// them as a String, mirroring the way the strings appear in the file.
		/// </summary>
		/// <remarks>
		/// Assumes the given reader has only tsurgeon operations (not a tregex pattern), and returns
		/// them as a String, mirroring the way the strings appear in the file. This is helpful
		/// for lazy evaluation of the operations, as in a GUI,
		/// because you do not parse the operations on load.  Comments are still excised.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static string GetTsurgeonTextFromReader(BufferedReader reader)
		{
			StringBuilder sb = new StringBuilder();
			for (string thisLine; (thisLine = reader.ReadLine()) != null; )
			{
				thisLine = RemoveComments(thisLine);
				if (emptyLinePattern.Matcher(thisLine).Matches())
				{
					continue;
				}
				sb.Append(thisLine);
				sb.Append('\n');
			}
			return sb.ToString();
		}

		/// <summary>
		/// Parses a tsurgeon script file and compiles all operations in the file into a list
		/// of pairs of tregex and tsurgeon patterns.
		/// </summary>
		/// <param name="filename">A file, classpath resource or URL (perhaps gzipped) containing the tsurgeon script</param>
		/// <returns>A pair of a tregex and tsurgeon pattern read from a file</returns>
		/// <exception cref="System.IO.IOException">If there is any I/O problem</exception>
		public static IList<Pair<TregexPattern, TsurgeonPattern>> GetOperationsFromFile(string filename, string encoding, TregexPatternCompiler compiler)
		{
			BufferedReader reader = IOUtils.ReaderFromString(filename, encoding);
			IList<Pair<TregexPattern, TsurgeonPattern>> operations = GetOperationsFromReader(reader, compiler);
			reader.Close();
			return operations;
		}

		/// <summary>
		/// Parses and compiles all operations from a BufferedReader into a list
		/// of pairs of tregex and tsurgeon patterns.
		/// </summary>
		/// <param name="reader">A BufferedReader to read the operations</param>
		/// <returns>A pair of a tregex and tsurgeon pattern read from reader</returns>
		/// <exception cref="System.IO.IOException">If there is any I/O problem</exception>
		public static IList<Pair<TregexPattern, TsurgeonPattern>> GetOperationsFromReader(BufferedReader reader, TregexPatternCompiler compiler)
		{
			IList<Pair<TregexPattern, TsurgeonPattern>> operations = new List<Pair<TregexPattern, TsurgeonPattern>>();
			for (; ; )
			{
				Pair<TregexPattern, TsurgeonPattern> operation = GetOperationFromReader(reader, compiler);
				if (operation == null)
				{
					break;
				}
				operations.Add(operation);
			}
			return operations;
		}

		/// <summary>Applies {#processPattern} to a collection of trees.</summary>
		/// <param name="matchPattern">
		/// A
		/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
		/// to be matched against a
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// .
		/// </param>
		/// <param name="p">
		/// A
		/// <see cref="TsurgeonPattern"/>
		/// to apply.
		/// </param>
		/// <param name="inputTrees">The input trees to be processed</param>
		/// <returns>A List of the transformed trees</returns>
		public static IList<Tree> ProcessPatternOnTrees(TregexPattern matchPattern, TsurgeonPattern p, ICollection<Tree> inputTrees)
		{
			IList<Tree> result = inputTrees.Stream().Map(null).Collect(Collectors.ToList());
			return result;
		}

		/// <summary>Tries to match a pattern against a tree.</summary>
		/// <remarks>
		/// Tries to match a pattern against a tree.  If it succeeds, apply the surgical operations contained in a
		/// <see cref="TsurgeonPattern"/>
		/// .
		/// </remarks>
		/// <param name="matchPattern">
		/// A
		/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexPattern"/>
		/// to be matched against a
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// .
		/// </param>
		/// <param name="p">
		/// A
		/// <see cref="TsurgeonPattern"/>
		/// to apply.
		/// </param>
		/// <param name="t">
		/// the
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// to match against and perform surgery on.
		/// </param>
		/// <returns>t, which has been surgically modified.</returns>
		public static Tree ProcessPattern(TregexPattern matchPattern, TsurgeonPattern p, Tree t)
		{
			TregexMatcher m = matchPattern.Matcher(t);
			TsurgeonMatcher tsm = p.Matcher();
			while (m.Find())
			{
				t = tsm.Evaluate(t, m);
				if (t == null)
				{
					break;
				}
				m = matchPattern.Matcher(t);
			}
			return t;
		}

		private static bool matchedOnTree;

		// hack-in field for seeing whether there was a match.
		public static Tree ProcessPatternsOnTree(IList<Pair<TregexPattern, TsurgeonPattern>> ops, Tree t)
		{
			matchedOnTree = false;
			foreach (Pair<TregexPattern, TsurgeonPattern> op in ops)
			{
				try
				{
					TregexMatcher m = op.First().Matcher(t);
					TsurgeonMatcher tsm = op.Second().Matcher();
					while (m.Find())
					{
						matchedOnTree = true;
						t = tsm.Evaluate(t, m);
						if (t == null)
						{
							return null;
						}
						m = op.First().Matcher(t);
					}
				}
				catch (ArgumentNullException npe)
				{
					throw new Exception("Tsurgeon.processPatternsOnTree failed to match label for pattern: " + op.First() + ", " + op.Second(), npe);
				}
			}
			return t;
		}

		/// <summary>
		/// Parses an operation string into a
		/// <see cref="TsurgeonPattern"/>
		/// .  Throws an
		/// <see cref="TsurgeonParseException"/>
		/// if
		/// the operation string is ill-formed.
		/// <p>
		/// Example of use:
		/// <p>
		/// <tt>
		/// TsurgeonPattern p = Tsurgeon.parseOperation("prune ed");
		/// </tt>
		/// </summary>
		/// <param name="operationString">The operation to perform, as a text string</param>
		/// <returns>the operation pattern.</returns>
		public static TsurgeonPattern ParseOperation(string operationString)
		{
			try
			{
				TsurgeonParser parser = new TsurgeonParser(new StringReader(operationString + '\n'));
				return parser.Root();
			}
			catch (Exception e)
			{
				throw new TsurgeonParseException("Error parsing Tsurgeon expression: " + operationString, e);
			}
		}

		/// <summary>Collects a list of operation patterns into a sequence of operations to be applied.</summary>
		/// <remarks>
		/// Collects a list of operation patterns into a sequence of operations to be applied.  Required to keep track of global properties
		/// across a sequence of operations.  For example, if you want to insert a named node and then coindex it with another node,
		/// you will need to collect the insertion and coindexation operations into a single TsurgeonPattern so that tsurgeon is aware
		/// of the name of the new node and coindexation becomes possible.
		/// </remarks>
		/// <param name="patterns">
		/// a list of
		/// <see cref="TsurgeonPattern"/>
		/// operations that you want to collect together into a single compound operation
		/// </param>
		/// <returns>
		/// a new
		/// <see cref="TsurgeonPattern"/>
		/// that performs all the operations in the sequence of the
		/// <paramref name="patterns"/>
		/// argument
		/// </returns>
		public static TsurgeonPattern CollectOperations(IList<TsurgeonPattern> patterns)
		{
			return new TsurgeonPatternRoot(Sharpen.Collections.ToArray(patterns, new TsurgeonPattern[patterns.Count]));
		}
	}
}
