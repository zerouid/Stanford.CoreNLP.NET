using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








using Org.W3c.Dom;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>This is the primary class for loading and saving out Ssurgeon patterns.</summary>
	/// <remarks>
	/// This is the primary class for loading and saving out Ssurgeon patterns.
	/// This is also the class that maintains the current list of resources loaded into Ssurgeon: any pattern
	/// loaded can reference these resources.
	/// </remarks>
	/// <author>Eric Yeh</author>
	public class Ssurgeon
	{
		private const bool Verbose = false;

		private static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon instance = null;

		private Ssurgeon()
		{
		}

		// singleton, to ensure all use the same resources
		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon Inst()
		{
			lock (typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon))
			{
				if (instance == null)
				{
					instance = new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon();
				}
			}
			return instance;
		}

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon));

		private string logPrefix = null;

		// Logging to file facilities.
		// The prefix is used to append stuff in front of the logging messages
		/// <exception cref="System.IO.IOException"/>
		public virtual void InitLog(File logFilePath)
		{
			RedwoodConfiguration.Empty().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.ShowAllChannels(), RedwoodConfiguration.Handlers.stderr), RedwoodConfiguration.Handlers.File(logFilePath.ToString())).Apply();
			// fh.setFormatter(new NewlineLogFormatter());
			System.Console.Out.WriteLine("Starting Ssurgeon log, at " + logFilePath.GetAbsolutePath() + " date=" + DateFormat.GetDateInstance(DateFormat.Full).Format(new DateTime()));
			log.Info("Starting Ssurgeon log, date=" + DateFormat.GetDateInstance(DateFormat.Full).Format(new DateTime()));
		}

		public virtual void SetLogPrefix(string logPrefix)
		{
			this.logPrefix = logPrefix;
		}

		/// <summary>
		/// Given a list of SsurgeonPattern edit scripts, and a SemanticGraph
		/// to operate over, returns a list of expansions of that graph, with
		/// the result of each edit applied against a copy of the graph.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public virtual IList<SemanticGraph> ExpandFromPatterns(IList<SsurgeonPattern> patternList, SemanticGraph sg)
		{
			IList<SemanticGraph> retList = new List<SemanticGraph>();
			foreach (SsurgeonPattern pattern in patternList)
			{
				ICollection<SemanticGraph> generated = pattern.Execute(sg);
				foreach (SemanticGraph orderedGraph in generated)
				{
					//orderedGraph.vertexList(true);
					//orderedGraph.edgeList(true);
					retList.Add(orderedGraph);
					System.Console.Out.WriteLine("\ncompact = " + orderedGraph.ToCompactString());
					System.Console.Out.WriteLine("regular=" + orderedGraph);
				}
				if (generated.Count > 0)
				{
					if (log != null)
					{
						log.Info("* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
						log.Info("Pre remove duplicates, num=" + generated.Count);
					}
					SemanticGraphUtils.RemoveDuplicates(generated, sg);
					if (log != null)
					{
						log.Info("Expand from patterns");
						if (logPrefix != null)
						{
							log.Info(logPrefix);
						}
						log.Info("Pattern = '" + pattern.GetUID() + "' generated " + generated.Count + " matches");
						log.Info("= = = = = = = = = =\nSrc graph:\n" + sg + "\n= = = = = = = = = =\n");
						int index = 1;
						foreach (SemanticGraph genSg in generated)
						{
							log.Info("REWRITE " + (index++));
							log.Info(genSg.ToString());
							log.Info(". . . . .\n");
						}
					}
				}
			}
			return retList;
		}

		/// <summary>
		/// Similar to the expandFromPatterns, but performs an exhaustive
		/// search, performing simplifications on the graphs until exhausted.
		/// </summary>
		/// <remarks>
		/// Similar to the expandFromPatterns, but performs an exhaustive
		/// search, performing simplifications on the graphs until exhausted.
		/// TODO: ensure cycles do not occur
		/// NOTE: put in an arbitrary depth limit of 3, to prevent churning way too much (heuristic)
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual ICollection<SemanticGraph> ExhaustFromPatterns(IList<SsurgeonPattern> patternList, SemanticGraph sg)
		{
			ICollection<SemanticGraph> generated = ExhaustFromPatterns(patternList, sg, 1);
			if (generated.Count > 1)
			{
				if (log != null)
				{
					log.Info("Before remove dupe, size=" + generated.Count);
				}
				generated = SemanticGraphUtils.RemoveDuplicates(generated, sg);
				if (log != null)
				{
					log.Info("AFTER remove dupe, size=" + generated.Count);
				}
			}
			return generated;
		}

		/// <exception cref="System.Exception"/>
		private IList<SemanticGraph> ExhaustFromPatterns(IList<SsurgeonPattern> patternList, SemanticGraph sg, int depth)
		{
			IList<SemanticGraph> retList = new List<SemanticGraph>();
			foreach (SsurgeonPattern pattern in patternList)
			{
				ICollection<SemanticGraph> generated = pattern.Execute(sg);
				foreach (SemanticGraph modGraph in generated)
				{
					//modGraph = SemanticGraphUtils.resetVerticeOrdering(modGraph);
					//modGraph.vertexList(true);
					//modGraph.edgeList(true);
					retList.Add(modGraph);
				}
				if (log != null && generated.Count > 0)
				{
					log.Info("* * * * * * * * * ** * * * * * * * * *");
					log.Info("Exhaust from patterns, depth=" + depth);
					if (logPrefix != null)
					{
						log.Info(logPrefix);
					}
					log.Info("Pattern = '" + pattern.GetUID() + "' generated " + generated.Count + " matches");
					log.Info("= = = = = = = = = =\nSrc graph:\n" + sg.ToString() + "\n= = = = = = = = = =\n");
					int index = 1;
					foreach (SemanticGraph genSg in generated)
					{
						log.Info("REWRITE " + (index++));
						log.Info(genSg.ToString());
						log.Info(". . . . .\n");
					}
				}
			}
			if (retList.Count > 0)
			{
				IList<SemanticGraph> referenceList = new List<SemanticGraph>(retList);
				foreach (SemanticGraph childGraph in referenceList)
				{
					if (depth < 3)
					{
						Sharpen.Collections.AddAll(retList, ExhaustFromPatterns(patternList, childGraph, depth + 1));
					}
				}
			}
			return retList;
		}

		/// <summary>
		/// Given a path to a file, converts it into a SsurgeonPattern
		/// TODO: finish implementing this stub.
		/// </summary>
		public static SsurgeonPattern GetOperationFromFile(string path)
		{
			return null;
		}

		private IDictionary<string, SsurgeonWordlist> wordListResources = Generics.NewHashMap();

		//
		// Resource management
		//
		/// <summary>Places the given word list resource under the given ID.</summary>
		/// <remarks>
		/// Places the given word list resource under the given ID.
		/// Note: can overwrite existing one in place.
		/// </remarks>
		private void AddResource(SsurgeonWordlist resource)
		{
			wordListResources[resource.GetID()] = resource;
		}

		/// <summary>Returns the given resource with the id.</summary>
		/// <remarks>
		/// Returns the given resource with the id.
		/// If does not exist, will throw exception.
		/// </remarks>
		public virtual SsurgeonWordlist GetResource(string id)
		{
			return wordListResources[id];
		}

		public virtual ICollection<SsurgeonWordlist> GetResources()
		{
			return wordListResources.Values;
		}

		public const string GovNodenameArg = "-gov";

		public const string DepNodenameArg = "-dep";

		public const string EdgeNameArg = "-edge";

		public const string NodenameArg = "-node";

		public const string RelnArg = "-reln";

		public const string NodeProtoArg = "-nodearg";

		public const string WeightArg = "-weight";

		public const string NameArg = "-name";

		protected internal class SsurgeonArgs
		{
			public string govNodeName = null;

			public string dep = null;

			public string edge = null;

			public string reln = null;

			public string node = null;

			public string nodeString = null;

			public double weight = 1.0;

			public string name = null;
			// args for Ssurgeon edits, allowing us to not
			// worry about arg order (and to make things appear less confusing)
			// Below are values keyed by Semgrex name
			// below are string representations of the intended values
		}

		/// <summary>
		/// This is a specialized args parser, as we want to split on
		/// whitespace, but retain everything inside quotes, so we can pass
		/// in hashmaps in String form.
		/// </summary>
		private static string[] ParseArgs(string argsString)
		{
			IList<string> retList = new List<string>();
			string patternString = "(?:[^\\s\\\"]++|\\\"[^\\\"]*+\\\"|(\\\"))++";
			Pattern pattern = Pattern.Compile(patternString);
			Matcher matcher = pattern.Matcher(argsString);
			while (matcher.Find())
			{
				if (matcher.Group(1) == null)
				{
					string matched = matcher.Group();
					if (matched[0] == '"' && matched[matched.Length - 1] == '"')
					{
						retList.Add(Sharpen.Runtime.Substring(matched, 1, matched.Length - 1));
					}
					else
					{
						retList.Add(matched);
					}
				}
				else
				{
					throw new ArgumentException("Unmatched quote in string to parse");
				}
			}
			return Sharpen.Collections.ToArray(retList, StringUtils.EmptyStringArray);
		}

		/// <summary>Given a string entry, converts it into a SsurgeonEdit object.</summary>
		public static SsurgeonEdit ParseEditLine(string editLine)
		{
			// Extract the operation name first
			string[] tuples1 = editLine.Split("\\s+", 2);
			if (tuples1.Length < 2)
			{
				throw new ArgumentException("Error in SsurgeonEdit.parseEditLine: invalid number of arguments");
			}
			string command = tuples1[0];
			string[] argsArray = ParseArgs(tuples1[1]);
			Ssurgeon.SsurgeonArgs argsBox = new Ssurgeon.SsurgeonArgs();
			for (int argIndex = 0; argIndex < argsArray.Length; ++argIndex)
			{
				switch (argsArray[argIndex])
				{
					case GovNodenameArg:
					{
						argsBox.govNodeName = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case DepNodenameArg:
					{
						argsBox.dep = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case EdgeNameArg:
					{
						argsBox.edge = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case RelnArg:
					{
						argsBox.reln = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case NodenameArg:
					{
						argsBox.node = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case NodeProtoArg:
					{
						argsBox.nodeString = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					case WeightArg:
					{
						argsBox.weight = double.ValueOf(argsArray[argIndex + 1]);
						argIndex += 2;
						break;
					}

					case NameArg:
					{
						argsBox.name = argsArray[argIndex + 1];
						argIndex += 2;
						break;
					}

					default:
					{
						throw new ArgumentException("Parsing Ssurgeon args: unknown flag " + argsArray[argIndex]);
					}
				}
			}
			// Parse the arguments based upon the type of command to execute.
			// TODO: this logic really should be moved into the individual classes.  The string-->class
			// mappings should also be stored in more appropriate data structure.
			SsurgeonEdit retEdit;
			if (Sharpen.Runtime.EqualsIgnoreCase(command, AddDep.Label))
			{
				retEdit = AddDep.CreateEngAddDep(argsBox.govNodeName, argsBox.reln, argsBox.nodeString);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(command, AddNode.Label))
				{
					retEdit = AddNode.CreateAddNode(argsBox.nodeString, argsBox.name);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(command, AddEdge.Label))
					{
						retEdit = AddEdge.CreateEngAddEdge(argsBox.govNodeName, argsBox.dep, argsBox.reln);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(command, DeleteGraphFromNode.Label))
						{
							retEdit = new DeleteGraphFromNode(argsBox.node);
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(command, RemoveEdge.Label))
							{
								retEdit = new RemoveEdge(GrammaticalRelation.ValueOf(argsBox.reln), argsBox.govNodeName, argsBox.dep);
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(command, RemoveNamedEdge.Label))
								{
									retEdit = new RemoveNamedEdge(argsBox.edge, argsBox.govNodeName, argsBox.dep);
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(command, SetRoots.Label))
									{
										string[] names = tuples1[1].Split("\\s+");
										IList<string> newRoots = Arrays.AsList(names);
										retEdit = new SetRoots(newRoots);
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(command, KillNonRootedNodes.Label))
										{
											retEdit = new KillNonRootedNodes();
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(command, KillAllIncomingEdges.Label))
											{
												retEdit = new KillAllIncomingEdges(argsBox.node);
											}
											else
											{
												throw new ArgumentException("Error in SsurgeonEdit.parseEditLine: command '" + command + "' is not supported");
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return retEdit;
		}

		//public static SsurgeonPattern fromXML(String xmlString) throws Exception {
		//SAXBuilder builder = new SAXBuilder();
		//Document jdomDoc = builder.build(xmlString);
		//jdomDoc.getRootElement().getChildren(SsurgeonPattern.SSURGEON_ELEM_TAG);
		//}
		/// <summary>Given a target filepath and a list of Ssurgeon patterns, writes them out as XML forms.</summary>
		public static void WriteToFile(File tgtFile, IList<SsurgeonPattern> patterns)
		{
			try
			{
				IDocument domDoc = CreatePatternXMLDoc(patterns);
				if (domDoc != null)
				{
					Transformer tformer = TransformerFactory.NewInstance().NewTransformer();
					tformer.SetOutputProperty(OutputKeys.Indent, "yes");
					tformer.Transform(new DOMSource(domDoc), new StreamResult(tgtFile));
				}
				else
				{
					log.Warning("Was not able to create XML document for pattern list, file not written.");
				}
			}
			catch (Exception e)
			{
				log.Error(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon).FullName, "writeToFile");
				log.Error(e);
			}
		}

		public static string WriteToString(SsurgeonPattern pattern)
		{
			try
			{
				IList<SsurgeonPattern> patterns = new LinkedList<SsurgeonPattern>();
				patterns.Add(pattern);
				IDocument domDoc = CreatePatternXMLDoc(patterns);
				if (domDoc != null)
				{
					Transformer tformer = TransformerFactory.NewInstance().NewTransformer();
					tformer.SetOutputProperty(OutputKeys.Indent, "yes");
					StringWriter sw = new StringWriter();
					tformer.Transform(new DOMSource(domDoc), new StreamResult(sw));
					return sw.ToString();
				}
				else
				{
					log.Warning("Was not able to create XML document for pattern list.");
				}
			}
			catch (Exception e)
			{
				log.Info("Error in writeToString, could not process pattern=" + pattern);
				log.Info(e);
				return null;
			}
			return string.Empty;
		}

		private static IDocument CreatePatternXMLDoc(IList<SsurgeonPattern> patterns)
		{
			try
			{
				DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
				DocumentBuilder db = dbf.NewDocumentBuilder();
				IDocument domDoc = db.NewDocument();
				IElement rootElt = domDoc.CreateElement(SsurgeonPattern.EltListTag);
				domDoc.AppendChild(rootElt);
				int ordinal = 1;
				foreach (SsurgeonPattern pattern in patterns)
				{
					IElement patElt = domDoc.CreateElement(SsurgeonPattern.SsurgeonElemTag);
					patElt.SetAttribute(SsurgeonPattern.OrdinalAttr, ordinal.ToString());
					IElement semgrexElt = domDoc.CreateElement(SsurgeonPattern.SemgrexElemTag);
					semgrexElt.AppendChild(domDoc.CreateTextNode(pattern.GetSemgrexPattern().Pattern()));
					patElt.AppendChild(semgrexElt);
					IElement uidElem = domDoc.CreateElement(SsurgeonPattern.UidElemTag);
					uidElem.AppendChild(domDoc.CreateTextNode(pattern.GetUID()));
					patElt.AppendChild(uidElem);
					IElement notesElem = domDoc.CreateElement(SsurgeonPattern.NotesElemTag);
					notesElem.AppendChild(domDoc.CreateTextNode(pattern.GetNotes()));
					patElt.AppendChild(notesElem);
					SemanticGraph semgrexGraph = pattern.GetSemgrexGraph();
					if (semgrexGraph != null)
					{
						IElement patNode = domDoc.CreateElement(SsurgeonPattern.SemgrexGraphElemTag);
						patNode.AppendChild(domDoc.CreateTextNode(semgrexGraph.ToCompactString()));
					}
					IElement editList = domDoc.CreateElement(SsurgeonPattern.EditListElemTag);
					patElt.AppendChild(editList);
					int editOrdinal = 1;
					foreach (SsurgeonEdit edit in pattern.GetEditScript())
					{
						IElement editElem = domDoc.CreateElement(SsurgeonPattern.EditElemTag);
						editElem.SetAttribute(SsurgeonPattern.OrdinalAttr, editOrdinal.ToString());
						editElem.AppendChild(domDoc.CreateTextNode(edit.ToEditString()));
						editList.AppendChild(editElem);
						editOrdinal++;
					}
					rootElt.AppendChild(patElt);
					ordinal++;
				}
				return domDoc;
			}
			catch (Exception e)
			{
				log.Error(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon).FullName, "createPatternXML");
				log.Error(e);
				return null;
			}
		}

		/// <summary>
		/// Given a path to a file containing a list of SsurgeonPatterns, returns
		/// TODO: deal with resources
		/// </summary>
		/// <exception cref="System.Exception"/>
		public virtual IList<SsurgeonPattern> ReadFromFile(File file)
		{
			IList<SsurgeonPattern> retList = new List<SsurgeonPattern>();
			IDocument doc = DocumentBuilderFactory.NewInstance().NewDocumentBuilder().Parse(file);
			INodeList patternNodes = doc.GetElementsByTagName(SsurgeonPattern.SsurgeonElemTag);
			for (int i = 0; i < patternNodes.GetLength(); i++)
			{
				INode node = patternNodes.Item(i);
				if (node.GetNodeType() == NodeConstants.ElementNode)
				{
					IElement elt = (IElement)node;
					SsurgeonPattern pattern = SsurgeonPatternFromXML(elt);
					retList.Add(pattern);
				}
			}
			INodeList resourceNodes = doc.GetElementsByTagName(SsurgeonPattern.ResourceTag);
			for (int i_1 = 0; i_1 < resourceNodes.GetLength(); i_1++)
			{
				INode node = patternNodes.Item(i_1);
				if (node.GetNodeType() == NodeConstants.ElementNode)
				{
					IElement resourceElt = (IElement)node;
					SsurgeonWordlist wlRsrc = new SsurgeonWordlist(resourceElt);
					AddResource(wlRsrc);
				}
			}
			return retList;
		}

		/// <summary>Reads all Ssurgeon patterns from file.</summary>
		/// <exception cref="System.Exception"/>
		public virtual IList<SsurgeonPattern> ReadFromDirectory(File dir)
		{
			if (!dir.IsDirectory())
			{
				throw new Exception("Given path not a directory, path=" + dir.GetAbsolutePath());
			}
			File[] files = dir.ListFiles(null);
			IList<SsurgeonPattern> patterns = new List<SsurgeonPattern>();
			foreach (File file in files)
			{
				try
				{
					Sharpen.Collections.AddAll(patterns, ReadFromFile(file));
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}
			return patterns;
		}

		/// <summary>
		/// Given the root Element for a SemgrexPattern (SSURGEON_ELEM_TAG), converts
		/// it into its corresponding SemgrexPattern object.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public static SsurgeonPattern SsurgeonPatternFromXML(IElement elt)
		{
			string uid = GetTagText(elt, SsurgeonPattern.UidElemTag);
			string notes = GetTagText(elt, SsurgeonPattern.NotesElemTag);
			string semgrexString = GetTagText(elt, SsurgeonPattern.SemgrexElemTag);
			SemgrexPattern semgrexPattern = SemgrexPattern.Compile(semgrexString);
			SsurgeonPattern retPattern = new SsurgeonPattern(uid, semgrexPattern);
			retPattern.SetNotes(notes);
			INodeList editNodes = elt.GetElementsByTagName(SsurgeonPattern.EditListElemTag);
			for (int i = 0; i < editNodes.GetLength(); i++)
			{
				INode node = editNodes.Item(i);
				if (node.GetNodeType() == NodeConstants.ElementNode)
				{
					IElement editElt = (IElement)node;
					string editVal = GetEltText(editElt);
					retPattern.AddEdit(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.ParseEditLine(editVal));
				}
			}
			// If predicate available, parse
			IElement predElt = GetFirstTag(elt, SsurgeonPattern.PredicateTag);
			if (predElt != null)
			{
				ISsurgPred pred = AssemblePredFromXML(GetFirstChildElement(predElt));
				retPattern.SetPredicate(pred);
			}
			return retPattern;
		}

		/// <summary>
		/// Constructs a
		/// <c>SsurgPred</c>
		/// structure from file, given the root element.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public static ISsurgPred AssemblePredFromXML(IElement elt)
		{
			string eltName = elt.GetTagName();
			switch (eltName)
			{
				case SsurgeonPattern.PredicateAndTag:
				{
					SsurgAndPred andPred = new SsurgAndPred();
					foreach (IElement childElt in GetChildElements(elt))
					{
						ISsurgPred childPred = AssemblePredFromXML(childElt);
						andPred.Add(childPred);
						return andPred;
					}
					break;
				}

				case SsurgeonPattern.PredicateOrTag:
				{
					SsurgOrPred orPred = new SsurgOrPred();
					foreach (IElement childElt_1 in GetChildElements(elt))
					{
						ISsurgPred childPred = AssemblePredFromXML(childElt_1);
						orPred.Add(childPred);
						return orPred;
					}
					break;
				}

				case SsurgeonPattern.PredWordlistTestTag:
				{
					string id = elt.GetAttribute(SsurgeonPattern.PredIdAttr);
					string resourceID = elt.GetAttribute("resourceID");
					string typeStr = elt.GetAttribute("type");
					string matchName = GetEltText(elt).Trim();
					// node name to match on
					if (matchName == null)
					{
						throw new Exception("Could not find match name for " + elt);
					}
					if (id == null)
					{
						throw new Exception("No ID attribute for element = " + elt);
					}
					return new WordlistTest(id, resourceID, typeStr, matchName);
				}
			}
			// Not a valid node, error out!
			throw new Exception("Invalid node encountered during Ssurgeon predicate processing, node name=" + eltName);
		}

		/// <summary>Reads in the test file and prints readable to string (for debugging).</summary>
		/// <remarks>
		/// Reads in the test file and prints readable to string (for debugging).
		/// Input file consists of semantic graphs, in compact form.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual void TestRead(File tgtDirPath)
		{
			IList<SsurgeonPattern> patterns = ReadFromDirectory(tgtDirPath);
			System.Console.Out.WriteLine("Patterns, num = " + patterns.Count);
			int num = 1;
			foreach (SsurgeonPattern pattern in patterns)
			{
				System.Console.Out.WriteLine("\n# " + (num++));
				System.Console.Out.WriteLine(pattern);
			}
			System.Console.Out.WriteLine("\n\nRESOURCES ");
			foreach (SsurgeonWordlist rsrc in Inst().GetResources())
			{
				System.Console.Out.WriteLine(rsrc + "* * * * *");
			}
			BufferedReader @in = new BufferedReader(new InputStreamReader(Runtime.@in));
			bool runFlag = true;
			Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().InitLog(new File("./ssurgeon_run.log"));
			while (runFlag)
			{
				try
				{
					System.Console.Out.WriteLine("Enter a sentence:");
					string line = @in.ReadLine();
					if (line.IsEmpty())
					{
						System.Environment.Exit(0);
					}
					System.Console.Out.WriteLine("Parsing...");
					SemanticGraph sg = SemanticGraph.ValueOf(line);
					System.Console.Out.WriteLine("Graph = " + sg);
					ICollection<SemanticGraph> generated = Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().ExhaustFromPatterns(patterns, sg);
					System.Console.Out.WriteLine("# generated = " + generated.Count);
					int index = 1;
					foreach (SemanticGraph gsg in generated)
					{
						System.Console.Out.WriteLine("\n# " + index);
						System.Console.Out.WriteLine(gsg);
						index++;
					}
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}
		}

		/*
		* XML convenience routines
		*/
		// todo [cdm 2016]: Aren't some of these methods available as generic XML methods elsewhere??
		/// <summary>
		/// For the given element, returns the text for the first child Element with
		/// the given tag.
		/// </summary>
		public static string GetTagText(IElement element, string tag)
		{
			try
			{
				// From root element, identify first with tag, then find the
				// first child under that, which we treat as a TEXT node.
				IElement firstElt = GetFirstTag(element, tag);
				if (firstElt == null)
				{
					return string.Empty;
				}
				return GetEltText(firstElt);
			}
			catch (Exception)
			{
				log.Warning("Exception thrown attempting to get tag text for tag=" + tag + ", from element=" + element);
			}
			return string.Empty;
		}

		/// <summary>
		/// For a given Element, treats the first child as a text element
		/// and returns its value.
		/// </summary>
		public static string GetEltText(IElement element)
		{
			try
			{
				INodeList childNodeList = element.GetChildNodes();
				if (childNodeList.GetLength() == 0)
				{
					return string.Empty;
				}
				return childNodeList.Item(0).GetNodeValue();
			}
			catch (Exception e)
			{
				log.Warning("Exception e=" + e.Message + " thrown calling getEltText on element=" + element);
			}
			return string.Empty;
		}

		/// <summary>For the given element, finds the first child Element with the given tag.</summary>
		private static IElement GetFirstTag(IElement element, string tag)
		{
			try
			{
				INodeList nodeList = element.GetElementsByTagName(tag);
				if (nodeList.GetLength() == 0)
				{
					return null;
				}
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					INode node = nodeList.Item(i);
					if (node.GetNodeType() == NodeConstants.ElementNode)
					{
						return (IElement)node;
					}
				}
			}
			catch (Exception)
			{
				log.Warning("Error getting first tag " + tag + " under element=" + element);
			}
			return null;
		}

		/// <summary>Returns the first child whose node type is Element under the given Element.</summary>
		private static IElement GetFirstChildElement(IElement element)
		{
			try
			{
				INodeList nodeList = element.GetChildNodes();
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					INode node = nodeList.Item(i);
					if (node.GetNodeType() == NodeConstants.ElementNode)
					{
						return (IElement)node;
					}
				}
			}
			catch (Exception e)
			{
				log.Warning("Error getting first child Element for element=" + element + ", exception=" + e);
			}
			return null;
		}

		/// <summary>Returns all of the Element typed children from the given element.</summary>
		/// <remarks>
		/// Returns all of the Element typed children from the given element.  Note: disregards
		/// other node types.
		/// </remarks>
		private static IList<IElement> GetChildElements(IElement element)
		{
			LinkedList<IElement> childElements = new LinkedList<IElement>();
			try
			{
				INodeList nodeList = element.GetChildNodes();
				for (int i = 0; i < nodeList.GetLength(); i++)
				{
					INode node = nodeList.Item(i);
					if (node.GetNodeType() == NodeConstants.ElementNode)
					{
						childElements.Add((IElement)node);
					}
				}
			}
			catch (Exception e)
			{
				log.Warning("Exception thrown getting all children for element=" + element + ", e=" + e);
			}
			return childElements;
		}

		public enum RUNTYPE
		{
			interactive,
			testinfo
		}

		public class ArgsBox
		{
			public Ssurgeon.RUNTYPE type = Ssurgeon.RUNTYPE.interactive;

			public string patternDirStr = null;

			public File patternDir = null;

			public string info = null;

			public File infoPath = null;

			/*
			* Main class evocation stuff
			*/
			// interactively test contents of pattern directory against entered sentences
			// test against a given infofile (RTE), generating rewrites for hypotheses
			public virtual void Init()
			{
				patternDir = new File(patternDirStr);
				if (type == Ssurgeon.RUNTYPE.testinfo)
				{
					infoPath = new File(info);
				}
			}

			public override string ToString()
			{
				StringWriter buf = new StringWriter();
				buf.Write("type =" + type + "\n");
				buf.Write("pattern dir = " + patternDir.GetAbsolutePath());
				if (type == Ssurgeon.RUNTYPE.testinfo)
				{
					buf.Write("info file = " + info);
					if (info != null)
					{
						buf.Write(", path = " + infoPath.GetAbsolutePath());
					}
				}
				return buf.ToString();
			}
		}

		protected internal static Ssurgeon.ArgsBox argsBox = new Ssurgeon.ArgsBox();

		/// <summary>Performs a simple test and print of a given file.</summary>
		/// <remarks>
		/// Performs a simple test and print of a given file.
		/// Usage Ssurgeon [-info infoFile] -patterns patternDir [-type interactive|testinfo]
		/// </remarks>
		public static void Main(string[] args)
		{
			for (int argIndex = 0; argIndex < args.Length; ++argIndex)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-info"))
				{
					argsBox.info = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-patterns"))
					{
						argsBox.patternDirStr = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-type"))
						{
							argsBox.type = Ssurgeon.RUNTYPE.ValueOf(args[argIndex + 1]);
							argIndex += 2;
						}
					}
				}
			}
			if (argsBox.patternDirStr == null)
			{
				throw new ArgumentException("Need to give a pattern location with -patterns");
			}
			argsBox.Init();
			System.Console.Out.WriteLine(argsBox);
			try
			{
				if (argsBox.type == Ssurgeon.RUNTYPE.interactive)
				{
					Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().TestRead(argsBox.patternDir);
				}
			}
			catch (Exception e)
			{
				log.Error(e);
			}
		}
	}
}
