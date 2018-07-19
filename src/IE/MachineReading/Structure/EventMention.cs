using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <author>Andrey Gusev</author>
	/// <author>Mihai</author>
	[System.Serializable]
	public class EventMention : RelationMention
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention));

		private const long serialVersionUID = 1L;

		/// <summary>Modifier argument: used for BioNLP</summary>
		private string eventModification;

		private readonly ExtractionObject anchor;

		private ICollection<ExtractionObject> parents;

		public EventMention(string objectId, ICoreMap sentence, Span span, string type, string subtype, ExtractionObject anchor, IList<ExtractionObject> args, IList<string> argNames)
			: base(objectId, sentence, span, type, subtype, args, argNames)
		{
			// this is set if we're a subevent
			// we might have multiple parents for the same event (at least in the reader before sanity check 4)!
			this.anchor = anchor;
			this.parents = new IdentityHashSet<ExtractionObject>();
			// set ourselves as the parent of any EventMentions in our args 
			foreach (ExtractionObject arg in args)
			{
				if (arg is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)
				{
					((Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)arg).AddParent(this);
				}
			}
		}

		public virtual void ResetArguments()
		{
			args = new List<ExtractionObject>();
			argNames = new List<string>();
		}

		public virtual void RemoveFromParents()
		{
			// remove this from the arg list of all parents
			foreach (ExtractionObject parent in parents)
			{
				if (parent is RelationMention)
				{
					((RelationMention)parent).RemoveArgument(this, false);
				}
			}
			// reset the parent links
			parents.Clear();
		}

		public virtual void RemoveParent(ExtractionObject p)
		{
			parents.Remove(p);
		}

		public virtual string GetModification()
		{
			return eventModification;
		}

		public virtual void SetModification(string eventModification)
		{
			this.eventModification = eventModification;
		}

		public virtual ExtractionObject GetAnchor()
		{
			return anchor;
		}

		/// <summary>If this EventMention is a subevent, this will return the parent event.</summary>
		/// <returns>the parent EventMention or null if this isn't a subevent.</returns>
		public virtual ICollection<ExtractionObject> GetParents()
		{
			return parents;
		}

		public virtual ExtractionObject GetSingleParent(ICoreMap sentence)
		{
			if (GetParents().Count > 1)
			{
				ICollection<ExtractionObject> parents = GetParents();
				log.Info("This event has multiple parents: " + this);
				int count = 1;
				foreach (ExtractionObject po in parents)
				{
					log.Info("PARENT #" + count + ": " + po);
					count++;
				}
				log.Info("DOC " + sentence.Get(typeof(CoreAnnotations.DocIDAnnotation)));
				log.Info("SENTENCE:");
				foreach (CoreLabel t in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					log.Info(" " + t.Word());
				}
				log.Info("EVENTS IN SENTENCE:");
				count = 1;
				foreach (Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention e in sentence.Get(typeof(MachineReadingAnnotations.EventMentionsAnnotation)))
				{
					log.Info("EVENT #" + count + ": " + e);
					count++;
				}
			}
			System.Diagnostics.Debug.Assert((GetParents().Count <= 1));
			foreach (ExtractionObject p in GetParents())
			{
				return p;
			}
			return null;
		}

		public virtual void AddParent(Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention p)
		{
			parents.Add(p);
		}

		public override string ToString()
		{
			return "EventMention [objectId=" + GetObjectId() + ", type=" + type + ", subType=" + subType + ", start=" + GetExtentTokenStart() + ", end=" + GetExtentTokenEnd() + (anchor != null ? ", anchor=" + anchor : string.Empty) + (args != null ? ", args="
				 + args : string.Empty) + (argNames != null ? ", argNames=" + argNames : string.Empty) + "]";
		}

		public virtual bool Contains(Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention e)
		{
			if (this == e)
			{
				return true;
			}
			foreach (ExtractionObject a in GetArgs())
			{
				if (a is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)
				{
					Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention ea = (Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)a;
					if (ea.Contains(e))
					{
						return true;
					}
				}
			}
			return false;
		}

		public virtual void AddArg(ExtractionObject a, string an, bool discardSameArgDifferentName)
		{
			// only add if not already an argument
			for (int i = 0; i < GetArgs().Count; i++)
			{
				ExtractionObject myArg = GetArg(i);
				string myArgName = GetArgNames()[i];
				if (myArg == a)
				{
					if (myArgName.Equals(an))
					{
						// safe to discard this arg: we already have it with the same name
						return;
					}
					else
					{
						logger.Info("Trying to add one argument: " + a + " with name " + an + " when this already exists with a different name: " + this + " in sentence: " + GetSentence().Get(typeof(CoreAnnotations.TextAnnotation)));
						if (discardSameArgDifferentName)
						{
							return;
						}
					}
				}
			}
			this.args.Add(a);
			this.argNames.Add(an);
			if (a is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)
			{
				((Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)a).AddParent(this);
			}
		}

		public override void SetArgs(IList<ExtractionObject> args)
		{
			this.args = args;
			// set ourselves as the parent of any EventMentions in our args 
			foreach (ExtractionObject arg in args)
			{
				if (arg is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)
				{
					((Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)arg).AddParent(this);
				}
			}
		}

		public virtual void AddArgs(IList<ExtractionObject> args, IList<string> argNames, bool discardSameArgDifferentName)
		{
			if (args == null)
			{
				return;
			}
			System.Diagnostics.Debug.Assert((args.Count == argNames.Count));
			for (int i = 0; i < args.Count; i++)
			{
				AddArg(args[i], argNames[i], discardSameArgDifferentName);
			}
		}

		public virtual void MergeEvent(Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention e, bool discardSameArgDifferentName)
		{
			// merge types if necessary
			string oldType = type;
			type = ExtractionObject.ConcatenateTypes(type, e.GetType());
			if (!type.Equals(oldType))
			{
				// This is not important: we use anchor types in the parser, not event types
				// This is done just for completeness of code
				logger.Fine("Type changed from " + oldType + " to " + type + " during check 3 merge.");
			}
			// add e's arguments
			for (int i = 0; i < e.GetArgs().Count; i++)
			{
				ExtractionObject a = e.GetArg(i);
				string an = e.GetArgNames()[i];
				// TODO: we might need more complex cycle detection than just contains()...
				if (a is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention && ((Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)a).Contains(this))
				{
					logger.Info("Found event cycle during merge between e1 " + this + " and e2 " + e);
				}
				else
				{
					// remove e from a's parents
					if (a is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)
					{
						((Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)a).RemoveParent(e);
					}
					// add a as an arg to this
					AddArg(a, an, discardSameArgDifferentName);
				}
			}
			// remove e's arguments. they are now attached to this, so we don't want them moved around during removeEvents
			e.ResetArguments();
			// remove e from its parent(s) to avoid using this argument in other merges of the parent
			e.RemoveFromParents();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention))
			{
				return false;
			}
			if (!base.Equals(o))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention that = (Edu.Stanford.Nlp.IE.Machinereading.Structure.EventMention)o;
			if (anchor != null ? !anchor.Equals(that.anchor) : that.anchor != null)
			{
				return false;
			}
			if (eventModification != null ? !eventModification.Equals(that.eventModification) : that.eventModification != null)
			{
				return false;
			}
			if (parents != null ? !parents.Equals(that.parents) : that.parents != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			result = 31 * result + (eventModification != null ? eventModification.GetHashCode() : 0);
			result = 31 * result + (anchor != null ? anchor.GetHashCode() : 0);
			return result;
		}
	}
}
