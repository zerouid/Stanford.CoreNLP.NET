using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Common;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Org.W3c.Dom;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>DOM reader for an ACE specification.</summary>
	/// <author>David McClosky</author>
	public class AceDomReader : DomReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AceDomReader));

		private static AceCharSeq ParseCharSeq(INode node)
		{
			INode child = GetChildByName(node, "charseq");
			string start = GetAttributeValue(child, "START");
			string end = GetAttributeValue(child, "END");
			string text = child.GetFirstChild().GetNodeValue();
			return new AceCharSeq(text, System.Convert.ToInt32(start), System.Convert.ToInt32(end));
		}

		/// <summary>Extracts one entity mention</summary>
		private static AceEntityMention ParseEntityMention(INode node)
		{
			string id = GetAttributeValue(node, "ID");
			string type = GetAttributeValue(node, "TYPE");
			string ldctype = GetAttributeValue(node, "LDCTYPE");
			AceCharSeq extent = ParseCharSeq(GetChildByName(node, "extent"));
			AceCharSeq head = ParseCharSeq(GetChildByName(node, "head"));
			return (new AceEntityMention(id, type, ldctype, extent, head));
		}

		/// <summary>Extracts info about one relation mention</summary>
		private static AceRelationMention ParseRelationMention(INode node, AceDocument doc)
		{
			string id = GetAttributeValue(node, "ID");
			AceCharSeq extent = ParseCharSeq(GetChildByName(node, "extent"));
			string lc = GetAttributeValue(node, "LEXICALCONDITION");
			// create the mention
			AceRelationMention mention = new AceRelationMention(id, extent, lc);
			// find the mention args
			IList<INode> args = GetChildrenByName(node, "relation_mention_argument");
			foreach (INode arg in args)
			{
				string role = GetAttributeValue(arg, "ROLE");
				string refid = GetAttributeValue(arg, "REFID");
				AceEntityMention am = doc.GetEntityMention(refid);
				if (am != null)
				{
					am.AddRelationMention(mention);
					if (Sharpen.Runtime.EqualsIgnoreCase(role, "arg-1"))
					{
						mention.GetArgs()[0] = new AceRelationMentionArgument(role, am);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(role, "arg-2"))
						{
							mention.GetArgs()[1] = new AceRelationMentionArgument(role, am);
						}
						else
						{
							throw new Exception("Invalid relation mention argument role: " + role);
						}
					}
				}
			}
			return mention;
		}

		/// <summary>Extracts info about one relation mention</summary>
		private static AceEventMention ParseEventMention(INode node, AceDocument doc)
		{
			string id = GetAttributeValue(node, "ID");
			AceCharSeq extent = ParseCharSeq(GetChildByName(node, "extent"));
			AceCharSeq anchor = ParseCharSeq(GetChildByName(node, "anchor"));
			// create the mention
			AceEventMention mention = new AceEventMention(id, extent, anchor);
			// find the mention args
			IList<INode> args = GetChildrenByName(node, "event_mention_argument");
			foreach (INode arg in args)
			{
				string role = GetAttributeValue(arg, "ROLE");
				string refid = GetAttributeValue(arg, "REFID");
				AceEntityMention am = doc.GetEntityMention(refid);
				if (am != null)
				{
					am.AddEventMention(mention);
					mention.AddArg(am, role);
				}
			}
			return mention;
		}

		/// <summary>Parses one ACE specification</summary>
		/// <returns>Simply displays the events to stdout</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static AceDocument ParseDocument(File f)
		{
			// parse the Dom document
			IDocument document = ReadDocument(f);
			//
			// create the ACE document object
			//
			INode docElement = document.GetElementsByTagName("document").Item(0);
			AceDocument aceDoc = new AceDocument(GetAttributeValue(docElement, "DOCID"));
			//
			// read all entities
			//
			INodeList entities = document.GetElementsByTagName("entity");
			int entityCount = 0;
			for (int i = 0; i < entities.GetLength(); i++)
			{
				INode node = entities.Item(i);
				//
				// the entity type and subtype
				//
				string id = GetAttributeValue(node, "ID");
				string type = GetAttributeValue(node, "TYPE");
				string subtype = GetAttributeValue(node, "SUBTYPE");
				string cls = GetAttributeValue(node, "CLASS");
				// create the entity
				AceEntity entity = new AceEntity(id, type, subtype, cls);
				aceDoc.AddEntity(entity);
				// fetch all mentions of this event
				IList<INode> mentions = GetChildrenByName(node, "entity_mention");
				// parse all its mentions
				foreach (INode mention1 in mentions)
				{
					AceEntityMention mention = ParseEntityMention(mention1);
					entity.AddMention(mention);
					aceDoc.AddEntityMention(mention);
				}
				entityCount++;
			}
			//log.info("Parsed " + entityCount + " XML entities.");
			//
			// read all relations
			//
			INodeList relations = document.GetElementsByTagName("relation");
			for (int i_1 = 0; i_1 < relations.GetLength(); i_1++)
			{
				INode node = relations.Item(i_1);
				//
				// the relation type, subtype, tense, and modality
				//
				string id = GetAttributeValue(node, "ID");
				string type = GetAttributeValue(node, "TYPE");
				string subtype = GetAttributeValue(node, "SUBTYPE");
				string modality = GetAttributeValue(node, "MODALITY");
				string tense = GetAttributeValue(node, "TENSE");
				// create the relation
				AceRelation relation = new AceRelation(id, type, subtype, modality, tense);
				aceDoc.AddRelation(relation);
				// XXX: fetch relation_arguments here!
				// fetch all mentions of this relation
				IList<INode> mentions = GetChildrenByName(node, "relation_mention");
				// traverse all mentions
				foreach (INode mention1 in mentions)
				{
					AceRelationMention mention = ParseRelationMention(mention1, aceDoc);
					relation.AddMention(mention);
					aceDoc.AddRelationMention(mention);
				}
			}
			//
			// read all events
			//
			INodeList events = document.GetElementsByTagName("event");
			for (int i_2 = 0; i_2 < events.GetLength(); i_2++)
			{
				INode node = events.Item(i_2);
				//
				// the event type, subtype, tense, and modality
				//
				string id = GetAttributeValue(node, "ID");
				string type = GetAttributeValue(node, "TYPE");
				string subtype = GetAttributeValue(node, "SUBTYPE");
				string modality = GetAttributeValue(node, "MODALITY");
				string polarity = GetAttributeValue(node, "POLARITY");
				string genericity = GetAttributeValue(node, "GENERICITY");
				string tense = GetAttributeValue(node, "TENSE");
				// create the event
				AceEvent @event = new AceEvent(id, type, subtype, modality, polarity, genericity, tense);
				aceDoc.AddEvent(@event);
				// fetch all mentions of this relation
				IList<INode> mentions = GetChildrenByName(node, "event_mention");
				// traverse all mentions
				foreach (INode mention1 in mentions)
				{
					AceEventMention mention = ParseEventMention(mention1, aceDoc);
					@event.AddMention(mention);
					aceDoc.AddEventMention(mention);
				}
			}
			return aceDoc;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] argv)
		{
			if (argv.Length != 1)
			{
				log.Info("Usage: java AceDomReader <APF file>");
				System.Environment.Exit(1);
			}
			File f = new File(argv[0]);
			AceDocument doc = ParseDocument(f);
			System.Console.Out.WriteLine("Processed ACE document:\n" + doc);
			List<List<AceRelationMention>> r = doc.GetAllRelationMentions();
			System.Console.Out.WriteLine("size: " + r.Count);
		}
	}
}
