using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using NU.Xom;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An outputter to XML format.</summary>
	/// <remarks>
	/// An outputter to XML format.
	/// This is not intended to be de-serialized back into annotations; for that,
	/// see
	/// <see cref="AnnotationSerializer"/>
	/// ; e.g.,
	/// <see cref="ProtobufAnnotationSerializer"/>
	/// .
	/// </remarks>
	public class XMLOutputter : AnnotationOutputter
	{
		private static readonly string NamespaceUri = null;

		private const string StylesheetName = "CoreNLP-to-HTML.xsl";

		public XMLOutputter()
		{
		}

		// the namespace is set in the XSLT file
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation annotation, OutputStream os, AnnotationOutputter.Options options)
		{
			Document xmlDoc = AnnotationToDoc(annotation, options);
			Serializer ser = new Serializer(os, options.encoding);
			if (options.pretty)
			{
				ser.SetIndent(2);
			}
			else
			{
				ser.SetIndent(0);
			}
			ser.SetMaxLength(0);
			ser.Write(xmlDoc);
			ser.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void XmlPrint(Annotation annotation, OutputStream os)
		{
			new Edu.Stanford.Nlp.Pipeline.XMLOutputter().Print(annotation, os);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void XmlPrint(Annotation annotation, OutputStream os, StanfordCoreNLP pipeline)
		{
			new Edu.Stanford.Nlp.Pipeline.XMLOutputter().Print(annotation, os, pipeline);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void XmlPrint(Annotation annotation, OutputStream os, AnnotationOutputter.Options options)
		{
			new Edu.Stanford.Nlp.Pipeline.XMLOutputter().Print(annotation, os, options);
		}

		/// <summary>Converts the given annotation to an XML document using options taken from the StanfordCoreNLP pipeline</summary>
		public static Document AnnotationToDoc(Annotation annotation, StanfordCoreNLP pipeline)
		{
			AnnotationOutputter.Options options = GetOptions(pipeline);
			return AnnotationToDoc(annotation, options);
		}

		/// <summary>Converts the given annotation to an XML document using the specified options</summary>
		public static Document AnnotationToDoc(Annotation annotation, AnnotationOutputter.Options options)
		{
			//
			// create the XML document with the root node pointing to the namespace URL
			//
			Element root = new Element("root", NamespaceUri);
			Document xmlDoc = new Document(root);
			ProcessingInstruction pi = new ProcessingInstruction("xml-stylesheet", "href=\"" + StylesheetName + "\" type=\"text/xsl\"");
			xmlDoc.InsertChild(pi, 0);
			Element docElem = new Element("document", NamespaceUri);
			root.AppendChild(docElem);
			SetSingleElement(docElem, "docId", NamespaceUri, annotation.Get(typeof(CoreAnnotations.DocIDAnnotation)));
			SetSingleElement(docElem, "docDate", NamespaceUri, annotation.Get(typeof(CoreAnnotations.DocDateAnnotation)));
			SetSingleElement(docElem, "docSourceType", NamespaceUri, annotation.Get(typeof(CoreAnnotations.DocSourceTypeAnnotation)));
			SetSingleElement(docElem, "docType", NamespaceUri, annotation.Get(typeof(CoreAnnotations.DocTypeAnnotation)));
			SetSingleElement(docElem, "author", NamespaceUri, annotation.Get(typeof(CoreAnnotations.AuthorAnnotation)));
			SetSingleElement(docElem, "location", NamespaceUri, annotation.Get(typeof(CoreAnnotations.LocationAnnotation)));
			if (options.includeText)
			{
				SetSingleElement(docElem, "text", NamespaceUri, annotation.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			Element sentencesElem = new Element("sentences", NamespaceUri);
			docElem.AppendChild(sentencesElem);
			//
			// save the info for each sentence in this doc
			//
			if (annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null)
			{
				int sentCount = 1;
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					Element sentElem = new Element("sentence", NamespaceUri);
					sentElem.AddAttribute(new Attribute("id", int.ToString(sentCount)));
					int lineNumber = sentence.Get(typeof(CoreAnnotations.LineNumberAnnotation));
					if (lineNumber != null)
					{
						sentElem.AddAttribute(new Attribute("line", int.ToString(lineNumber)));
					}
					sentCount++;
					// add the word table with all token-level annotations
					Element wordTable = new Element("tokens", NamespaceUri);
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					for (int j = 0; j < tokens.Count; j++)
					{
						Element wordInfo = new Element("token", NamespaceUri);
						AddWordInfo(wordInfo, tokens[j], j + 1, NamespaceUri);
						wordTable.AppendChild(wordInfo);
					}
					sentElem.AppendChild(wordTable);
					// add tree info
					Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					if (tree != null)
					{
						// add the constituent tree for this sentence
						Element parseInfo = new Element("parse", NamespaceUri);
						AddConstituentTreeInfo(parseInfo, tree, options.constituentTreePrinter);
						sentElem.AppendChild(parseInfo);
					}
					SemanticGraph basicDependencies = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					if (basicDependencies != null)
					{
						// add the dependencies for this sentence
						Element depInfo = BuildDependencyTreeInfo("basic-dependencies", sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)), tokens, NamespaceUri);
						if (depInfo != null)
						{
							sentElem.AppendChild(depInfo);
						}
						depInfo = BuildDependencyTreeInfo("collapsed-dependencies", sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation)), tokens, NamespaceUri);
						if (depInfo != null)
						{
							sentElem.AppendChild(depInfo);
						}
						depInfo = BuildDependencyTreeInfo("collapsed-ccprocessed-dependencies", sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation)), tokens, NamespaceUri);
						if (depInfo != null)
						{
							sentElem.AppendChild(depInfo);
						}
						depInfo = BuildDependencyTreeInfo("enhanced-dependencies", sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)), tokens, NamespaceUri);
						if (depInfo != null)
						{
							sentElem.AppendChild(depInfo);
						}
						depInfo = BuildDependencyTreeInfo("enhanced-plus-plus-dependencies", sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)), tokens, NamespaceUri);
						if (depInfo != null)
						{
							sentElem.AppendChild(depInfo);
						}
					}
					// add Open IE triples
					ICollection<RelationTriple> openieTriples = sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation));
					if (openieTriples != null)
					{
						Element openieElem = new Element("openie", NamespaceUri);
						AddTriples(openieTriples, openieElem, NamespaceUri);
						sentElem.AppendChild(openieElem);
					}
					// add KBP triples
					ICollection<RelationTriple> kbpTriples = sentence.Get(typeof(CoreAnnotations.KBPTriplesAnnotation));
					if (kbpTriples != null)
					{
						Element kbpElem = new Element("kbp", NamespaceUri);
						AddTriples(kbpTriples, kbpElem, NamespaceUri);
						sentElem.AppendChild(kbpElem);
					}
					// add the MR entities and relations
					IList<EntityMention> entities = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
					IList<RelationMention> relations = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
					if (entities != null && !entities.IsEmpty())
					{
						Element mrElem = new Element("MachineReading", NamespaceUri);
						Element entElem = new Element("entities", NamespaceUri);
						AddEntities(entities, entElem, NamespaceUri);
						mrElem.AppendChild(entElem);
						if (relations != null)
						{
							Element relElem = new Element("relations", NamespaceUri);
							AddRelations(relations, relElem, NamespaceUri, options.relationsBeam);
							mrElem.AppendChild(relElem);
						}
						sentElem.AppendChild(mrElem);
					}
					// Adds sentiment as an attribute of this sentence.
					Tree sentimentTree = sentence.Get(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree));
					if (sentimentTree != null)
					{
						int sentiment = RNNCoreAnnotations.GetPredictedClass(sentimentTree);
						sentElem.AddAttribute(new Attribute("sentimentValue", int.ToString(sentiment)));
						string sentimentClass = sentence.Get(typeof(SentimentCoreAnnotations.SentimentClass));
						sentElem.AddAttribute(new Attribute("sentiment", sentimentClass.ReplaceAll(" ", string.Empty)));
					}
					// add the sentence to the root
					sentencesElem.AppendChild(sentElem);
				}
			}
			//
			// add the coref graph
			//
			IDictionary<int, CorefChain> corefChains = annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			if (corefChains != null)
			{
				IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
				Element corefInfo = new Element("coreference", NamespaceUri);
				AddCorefGraphInfo(options, corefInfo, sentences, corefChains, NamespaceUri);
				docElem.AppendChild(corefInfo);
			}
			//
			// save any document-level annotations here
			//
			return xmlDoc;
		}

		/// <summary>Generates the XML content for a list of OpenIE triples.</summary>
		private static void AddTriples(ICollection<RelationTriple> openieTriples, Element top, string namespaceUri)
		{
			foreach (RelationTriple triple in openieTriples)
			{
				top.AppendChild(ToXML(triple, namespaceUri));
			}
		}

		/// <summary>Generates the XML content for a constituent tree</summary>
		private static void AddConstituentTreeInfo(Element treeInfo, Tree tree, TreePrint constituentTreePrinter)
		{
			StringWriter treeStrWriter = new StringWriter();
			constituentTreePrinter.PrintTree(tree, new PrintWriter(treeStrWriter, true));
			string temp = treeStrWriter.ToString();
			//log.info(temp);
			treeInfo.AppendChild(temp);
		}

		private static Element BuildDependencyTreeInfo(string dependencyType, SemanticGraph graph, IList<CoreLabel> tokens, string curNS)
		{
			if (graph != null)
			{
				Element depInfo = new Element("dependencies", curNS);
				depInfo.AddAttribute(new Attribute("type", dependencyType));
				// The SemanticGraph doesn't explicitly encode the ROOT node,
				// so we print that out ourselves
				foreach (IndexedWord root in graph.GetRoots())
				{
					string rel = GrammaticalRelation.Root.GetLongName();
					rel = rel.ReplaceAll("\\s+", string.Empty);
					// future proofing
					int source = 0;
					int target = root.Index();
					string sourceWord = "ROOT";
					string targetWord = tokens[target - 1].Word();
					bool isExtra = false;
					AddDependencyInfo(depInfo, rel, isExtra, source, sourceWord, null, target, targetWord, null, curNS);
				}
				foreach (SemanticGraphEdge edge in graph.EdgeListSorted())
				{
					string rel = edge.GetRelation().ToString();
					rel = rel.ReplaceAll("\\s+", string.Empty);
					int source = edge.GetSource().Index();
					int target = edge.GetTarget().Index();
					string sourceWord = tokens[source - 1].Word();
					string targetWord = tokens[target - 1].Word();
					int sourceCopy = edge.GetSource().CopyCount();
					int targetCopy = edge.GetTarget().CopyCount();
					bool isExtra = edge.IsExtra();
					AddDependencyInfo(depInfo, rel, isExtra, source, sourceWord, sourceCopy, target, targetWord, targetCopy, curNS);
				}
				return depInfo;
			}
			return null;
		}

		private static void AddDependencyInfo(Element depInfo, string rel, bool isExtra, int source, string sourceWord, int sourceCopy, int target, string targetWord, int targetCopy, string curNS)
		{
			Element depElem = new Element("dep", curNS);
			depElem.AddAttribute(new Attribute("type", rel));
			if (isExtra)
			{
				depElem.AddAttribute(new Attribute("extra", "true"));
			}
			Element govElem = new Element("governor", curNS);
			govElem.AddAttribute(new Attribute("idx", int.ToString(source)));
			govElem.AppendChild(sourceWord);
			if (sourceCopy != null && sourceCopy > 0)
			{
				govElem.AddAttribute(new Attribute("copy", int.ToString(sourceCopy)));
			}
			depElem.AppendChild(govElem);
			Element dependElem = new Element("dependent", curNS);
			dependElem.AddAttribute(new Attribute("idx", int.ToString(target)));
			dependElem.AppendChild(targetWord);
			if (targetCopy != null && targetCopy > 0)
			{
				dependElem.AddAttribute(new Attribute("copy", int.ToString(targetCopy)));
			}
			depElem.AppendChild(dependElem);
			depInfo.AppendChild(depElem);
		}

		/// <summary>Generates the XML content for MachineReading entities.</summary>
		private static void AddEntities(IList<EntityMention> entities, Element top, string curNS)
		{
			foreach (EntityMention e in entities)
			{
				Element ee = ToXML(e, curNS);
				top.AppendChild(ee);
			}
		}

		/// <summary>Generates the XML content for MachineReading relations.</summary>
		private static void AddRelations(IList<RelationMention> relations, Element top, string curNS, double beam)
		{
			foreach (RelationMention r in relations)
			{
				if (r.PrintableObject(beam))
				{
					Element re = ToXML(r, curNS);
					top.AppendChild(re);
				}
			}
		}

		/// <summary>Generates the XML content for the coreference chain object.</summary>
		private static bool AddCorefGraphInfo(AnnotationOutputter.Options options, Element corefInfo, IList<ICoreMap> sentences, IDictionary<int, CorefChain> corefChains, string curNS)
		{
			bool foundCoref = false;
			foreach (CorefChain chain in corefChains.Values)
			{
				if (!options.printSingletons && chain.GetMentionsInTextualOrder().Count <= 1)
				{
					continue;
				}
				foundCoref = true;
				Element chainElem = new Element("coreference", curNS);
				CorefChain.CorefMention source = chain.GetRepresentativeMention();
				AddCorefMention(options, chainElem, curNS, sentences, source, true);
				foreach (CorefChain.CorefMention mention in chain.GetMentionsInTextualOrder())
				{
					if (mention == source)
					{
						continue;
					}
					AddCorefMention(options, chainElem, curNS, sentences, mention, false);
				}
				corefInfo.AppendChild(chainElem);
			}
			return foundCoref;
		}

		private static void AddCorefMention(AnnotationOutputter.Options options, Element chainElem, string curNS, IList<ICoreMap> sentences, CorefChain.CorefMention mention, bool representative)
		{
			Element mentionElem = new Element("mention", curNS);
			if (representative)
			{
				mentionElem.AddAttribute(new Attribute("representative", "true"));
			}
			SetSingleElement(mentionElem, "sentence", curNS, int.ToString(mention.sentNum));
			SetSingleElement(mentionElem, "start", curNS, int.ToString(mention.startIndex));
			SetSingleElement(mentionElem, "end", curNS, int.ToString(mention.endIndex));
			SetSingleElement(mentionElem, "head", curNS, int.ToString(mention.headIndex));
			string text = mention.mentionSpan;
			SetSingleElement(mentionElem, "text", curNS, text);
			// Do you want context with your coreference?
			if (sentences != null && options.coreferenceContextSize > 0)
			{
				// If so use sentences to get so context from sentences
				IList<CoreLabel> tokens = sentences[mention.sentNum - 1].Get(typeof(CoreAnnotations.TokensAnnotation));
				int contextStart = Math.Max(mention.startIndex - 1 - 5, 0);
				int contextEnd = Math.Min(mention.endIndex - 1 + 5, tokens.Count);
				string leftContext = StringUtils.JoinWords(tokens, " ", contextStart, mention.startIndex - 1);
				string rightContext = StringUtils.JoinWords(tokens, " ", mention.endIndex - 1, contextEnd);
				SetSingleElement(mentionElem, "leftContext", curNS, leftContext);
				SetSingleElement(mentionElem, "rightContext", curNS, rightContext);
			}
			chainElem.AppendChild(mentionElem);
		}

		private static void AddWordInfo(Element wordInfo, ICoreMap token, int id, string curNS)
		{
			// store the position of this word in the sentence
			wordInfo.AddAttribute(new Attribute("id", int.ToString(id)));
			SetSingleElement(wordInfo, "word", curNS, token.Get(typeof(CoreAnnotations.TextAnnotation)));
			SetSingleElement(wordInfo, "lemma", curNS, token.Get(typeof(CoreAnnotations.LemmaAnnotation)));
			if (token.ContainsKey(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) && token.ContainsKey(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
			{
				SetSingleElement(wordInfo, "CharacterOffsetBegin", curNS, int.ToString(token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation))));
				SetSingleElement(wordInfo, "CharacterOffsetEnd", curNS, int.ToString(token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation))));
			}
			if (token.ContainsKey(typeof(CoreAnnotations.PartOfSpeechAnnotation)))
			{
				SetSingleElement(wordInfo, "POS", curNS, token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			}
			if (token.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
			{
				SetSingleElement(wordInfo, "NER", curNS, token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
			}
			if (token.ContainsKey(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)))
			{
				SetSingleElement(wordInfo, "NormalizedNER", curNS, token.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)));
			}
			if (token.ContainsKey(typeof(CoreAnnotations.SpeakerAnnotation)))
			{
				SetSingleElement(wordInfo, "Speaker", curNS, token.Get(typeof(CoreAnnotations.SpeakerAnnotation)));
			}
			if (token.ContainsKey(typeof(TimeAnnotations.TimexAnnotation)))
			{
				Timex timex = token.Get(typeof(TimeAnnotations.TimexAnnotation));
				Element timexElem = new Element("Timex", curNS);
				timexElem.AddAttribute(new Attribute("tid", timex.Tid()));
				timexElem.AddAttribute(new Attribute("type", timex.TimexType()));
				timexElem.AppendChild(timex.Value());
				wordInfo.AppendChild(timexElem);
			}
			if (token.ContainsKey(typeof(CoreAnnotations.TrueCaseAnnotation)))
			{
				Element cur = new Element("TrueCase", curNS);
				cur.AppendChild(token.Get(typeof(CoreAnnotations.TrueCaseAnnotation)));
				wordInfo.AppendChild(cur);
			}
			if (token.ContainsKey(typeof(CoreAnnotations.TrueCaseTextAnnotation)))
			{
				Element cur = new Element("TrueCaseText", curNS);
				cur.AppendChild(token.Get(typeof(CoreAnnotations.TrueCaseTextAnnotation)));
				wordInfo.AppendChild(cur);
			}
			if (token.ContainsKey(typeof(SentimentCoreAnnotations.SentimentClass)))
			{
				Element cur = new Element("sentiment", curNS);
				cur.AppendChild(token.Get(typeof(SentimentCoreAnnotations.SentimentClass)));
				wordInfo.AppendChild(cur);
			}
			if (token.ContainsKey(typeof(CoreAnnotations.WikipediaEntityAnnotation)))
			{
				Element cur = new Element("entitylink", curNS);
				cur.AppendChild(token.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)));
				wordInfo.AppendChild(cur);
			}
		}

		//    IntTuple corefDest;
		//    if((corefDest = label.get(CorefDestAnnotation.class)) != null){
		//      Element cur = new Element("coref", curNS);
		//      String value = Integer.toString(corefDest.get(0)) + "." + Integer.toString(corefDest.get(1));
		//      cur.setText(value);
		//      wordInfo.addContent(cur);
		//    }
		/// <summary>Helper method for addWordInfo().</summary>
		/// <remarks>
		/// Helper method for addWordInfo().  If the value is not null,
		/// creates an element of the given name and namespace and adds it to the
		/// tokenElement.
		/// </remarks>
		/// <param name="tokenElement">This is the element to which the newly created element will be added</param>
		/// <param name="elemName">This is the name for the new XML element</param>
		/// <param name="curNS">The current namespace</param>
		/// <param name="value">This is its value</param>
		private static void SetSingleElement(Element tokenElement, string elemName, string curNS, string value)
		{
			if (value != null)
			{
				Element cur = new Element(elemName, curNS);
				cur.AppendChild(value);
				tokenElement.AppendChild(cur);
			}
		}

		private static Element ToXML(RelationTriple triple, string curNS)
		{
			Element top = new Element("triple", curNS);
			top.AddAttribute(new Attribute("confidence", triple.ConfidenceGloss()));
			// Create the subject
			Element subject = new Element("subject", curNS);
			subject.AddAttribute(new Attribute("begin", int.ToString(triple.SubjectTokenSpan().first)));
			subject.AddAttribute(new Attribute("end", int.ToString(triple.SubjectTokenSpan().second)));
			Element text = new Element("text", curNS);
			text.AppendChild(triple.SubjectGloss());
			Element lemma = new Element("lemma", curNS);
			lemma.AppendChild(triple.SubjectLemmaGloss());
			subject.AppendChild(text);
			subject.AppendChild(lemma);
			top.AppendChild(subject);
			// Create the relation
			Element relation = new Element("relation", curNS);
			relation.AddAttribute(new Attribute("begin", int.ToString(triple.RelationTokenSpan().first)));
			relation.AddAttribute(new Attribute("end", int.ToString(triple.RelationTokenSpan().second)));
			text = new Element("text", curNS);
			text.AppendChild(triple.RelationGloss());
			lemma = new Element("lemma", curNS);
			lemma.AppendChild(triple.RelationLemmaGloss());
			relation.AppendChild(text);
			relation.AppendChild(lemma);
			top.AppendChild(relation);
			// Create the object
			Element @object = new Element("object", curNS);
			@object.AddAttribute(new Attribute("begin", int.ToString(triple.ObjectTokenSpan().first)));
			@object.AddAttribute(new Attribute("end", int.ToString(triple.ObjectTokenSpan().second)));
			text = new Element("text", curNS);
			text.AppendChild(triple.ObjectGloss());
			lemma = new Element("lemma", curNS);
			lemma.AppendChild(triple.ObjectLemmaGloss());
			@object.AppendChild(text);
			@object.AppendChild(lemma);
			top.AppendChild(@object);
			return top;
		}

		private static Element ToXML(EntityMention entity, string curNS)
		{
			Element top = new Element("entity", curNS);
			top.AddAttribute(new Attribute("id", entity.GetObjectId()));
			Element type = new Element("type", curNS);
			type.AppendChild(entity.GetType());
			top.AppendChild(entity.GetType());
			if (entity.GetNormalizedName() != null)
			{
				Element nm = new Element("normalized", curNS);
				nm.AppendChild(entity.GetNormalizedName());
				top.AppendChild(nm);
			}
			if (entity.GetSubType() != null)
			{
				Element subtype = new Element("subtype", curNS);
				subtype.AppendChild(entity.GetSubType());
				top.AppendChild(subtype);
			}
			Element span = new Element("span", curNS);
			span.AddAttribute(new Attribute("start", int.ToString(entity.GetHeadTokenStart())));
			span.AddAttribute(new Attribute("end", int.ToString(entity.GetHeadTokenEnd())));
			top.AppendChild(span);
			top.AppendChild(MakeProbabilitiesElement(entity, curNS));
			return top;
		}

		private static Element ToXML(RelationMention relation, string curNS)
		{
			Element top = new Element("relation", curNS);
			top.AddAttribute(new Attribute("id", relation.GetObjectId()));
			Element type = new Element("type", curNS);
			type.AppendChild(relation.GetType());
			top.AppendChild(relation.GetType());
			if (relation.GetSubType() != null)
			{
				Element subtype = new Element("subtype", curNS);
				subtype.AppendChild(relation.GetSubType());
				top.AppendChild(relation.GetSubType());
			}
			IList<EntityMention> mentions = relation.GetEntityMentionArgs();
			Element args = new Element("arguments", curNS);
			foreach (EntityMention e in mentions)
			{
				args.AppendChild(ToXML(e, curNS));
			}
			top.AppendChild(args);
			top.AppendChild(MakeProbabilitiesElement(relation, curNS));
			return top;
		}

		private static Element MakeProbabilitiesElement(ExtractionObject @object, string curNS)
		{
			Element probs = new Element("probabilities", curNS);
			if (@object.GetTypeProbabilities() != null)
			{
				IList<Pair<string, double>> sorted = Counters.ToDescendingMagnitudeSortedListWithCounts(@object.GetTypeProbabilities());
				foreach (Pair<string, double> lv in sorted)
				{
					Element prob = new Element("probability", curNS);
					Element label = new Element("label", curNS);
					label.AppendChild(lv.first);
					Element value = new Element("value", curNS);
					value.AppendChild(lv.second.ToString());
					prob.AppendChild(label);
					prob.AppendChild(value);
					probs.AppendChild(prob);
				}
			}
			return probs;
		}
	}
}
