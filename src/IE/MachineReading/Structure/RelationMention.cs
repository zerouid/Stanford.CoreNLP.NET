using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Each relation has a type and set of arguments</summary>
	/// <author>Andrey Gusev</author>
	/// <author>Mihai</author>
	/// <author>David McClosky</author>
	[System.Serializable]
	public class RelationMention : ExtractionObject
	{
		private const long serialVersionUID = 8962289597607972827L;

		public static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention).FullName);

		private static int MentionCounter = 0;

		public const string Unrelated = "_NR";

		/// <summary>List of argument names in this relation</summary>
		protected internal IList<string> argNames;

		/// <summary>
		/// List of arguments in this relation
		/// If unnamed, arguments MUST be stored in semantic order, e.g., ARG0 must be a person in a employed-by relation
		/// </summary>
		protected internal IList<ExtractionObject> args;

		/// <summary>
		/// A signature for a given relation mention, e.g., a concatenation of type and argument strings
		/// This is used in KBP, where we merge all RelationMentions corresponding to the same abstract relation
		/// </summary>
		protected internal string signature;

		public RelationMention(string objectId, ICoreMap sentence, Span span, string type, string subtype, IList<ExtractionObject> args)
			: base(objectId, sentence, span, type, subtype)
		{
			// index of the next unique id
			this.args = args;
			this.argNames = null;
			this.signature = null;
		}

		public RelationMention(string objectId, ICoreMap sentence, Span span, string type, string subtype, IList<ExtractionObject> args, IList<string> argNames)
			: base(objectId, sentence, span, type, subtype)
		{
			this.args = args;
			this.argNames = argNames;
			this.signature = null;
		}

		public RelationMention(string objectId, ICoreMap sentence, Span span, string type, string subtype, params ExtractionObject[] args)
			: this(objectId, sentence, span, type, subtype, Arrays.AsList(args))
		{
		}

		public virtual bool ArgsMatch(Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention rel)
		{
			return ArgsMatch(rel.GetArgs());
		}

		public virtual bool ArgsMatch(params ExtractionObject[] inputArgs)
		{
			return ArgsMatch(Arrays.AsList(inputArgs));
		}

		/// <summary>Verifies if the two sets of arguments match</summary>
		/// <param name="inputArgs">List of arguments</param>
		public virtual bool ArgsMatch(IList<ExtractionObject> inputArgs)
		{
			if (inputArgs.Count != this.args.Count)
			{
				return false;
			}
			for (int ind = 0; ind < this.args.Count; ind++)
			{
				ExtractionObject a1 = this.args[ind];
				ExtractionObject a2 = inputArgs[ind];
				if (!a1.Equals(a2))
				{
					return false;
				}
			}
			return true;
		}

		public virtual IList<ExtractionObject> GetArgs()
		{
			return Java.Util.Collections.UnmodifiableList(this.args);
		}

		public virtual void SetArgs(IList<ExtractionObject> args)
		{
			this.args = args;
		}

		/// <summary>Fetches the arguments of this relation that are entity mentions</summary>
		/// <returns>List of entity-mention args sorted in semantic order</returns>
		public virtual IList<EntityMention> GetEntityMentionArgs()
		{
			IList<EntityMention> ents = new List<EntityMention>();
			foreach (ExtractionObject o in args)
			{
				if (o is EntityMention)
				{
					ents.Add((EntityMention)o);
				}
			}
			return ents;
		}

		public virtual ExtractionObject GetArg(int argpos)
		{
			return this.args[argpos];
		}

		public virtual IList<string> GetArgNames()
		{
			return argNames;
		}

		public virtual void SetArgNames(IList<string> argNames)
		{
			this.argNames = argNames;
		}

		public virtual void AddArg(ExtractionObject a)
		{
			this.args.Add(a);
		}

		public virtual bool IsNegativeRelation()
		{
			return IsUnrelatedLabel(GetType());
		}

		/// <summary>Find the left-most position of an argument's syntactic head</summary>
		public virtual int GetFirstSyntacticHeadPosition()
		{
			int pos = int.MaxValue;
			foreach (ExtractionObject obj in args)
			{
				if (obj is EntityMention)
				{
					EntityMention em = (EntityMention)obj;
					if (em.GetSyntacticHeadTokenPosition() < pos)
					{
						pos = em.GetSyntacticHeadTokenPosition();
					}
				}
			}
			if (pos != int.MaxValue)
			{
				return pos;
			}
			return -1;
		}

		/// <summary>Find the right-most position of an argument's syntactic head</summary>
		public virtual int GetLastSyntacticHeadPosition()
		{
			int pos = int.MinValue;
			foreach (ExtractionObject obj in args)
			{
				if (obj is EntityMention)
				{
					EntityMention em = (EntityMention)obj;
					if (em.GetSyntacticHeadTokenPosition() > pos)
					{
						pos = em.GetSyntacticHeadTokenPosition();
					}
				}
			}
			if (pos != int.MinValue)
			{
				return pos;
			}
			return -1;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("RelationMention [type=" + type + (subType != null ? ", subType=" + subType : string.Empty) + ", start=" + GetExtentTokenStart() + ", end=" + GetExtentTokenEnd());
			if (typeProbabilities != null)
			{
				sb.Append(", " + ProbsToString());
			}
			if (args != null)
			{
				for (int i = 0; i < args.Count; i++)
				{
					sb.Append("\n\t");
					if (argNames != null)
					{
						sb.Append(argNames[i] + " ");
					}
					sb.Append(args[i]);
				}
			}
			sb.Append("\n]");
			return sb.ToString();
		}

		/// <summary>
		/// Replaces the arguments of this relations with equivalent mentions from the predictedMentions list
		/// This works only for arguments that are EntityMention!
		/// </summary>
		/// <param name="predictedMentions"/>
		public virtual bool ReplaceGoldArgsWithPredicted(IList<EntityMention> predictedMentions)
		{
			IList<ExtractionObject> newArgs = new List<ExtractionObject>();
			foreach (ExtractionObject arg in args)
			{
				if (!(arg is EntityMention))
				{
					continue;
				}
				EntityMention goldEnt = (EntityMention)arg;
				EntityMention newArg = null;
				foreach (EntityMention pred in predictedMentions)
				{
					if (goldEnt.TextEquals(pred))
					{
						newArg = pred;
						break;
					}
				}
				if (newArg != null)
				{
					newArgs.Add(newArg);
					logger.Info("Replacing relation argument: [" + goldEnt + "] with predicted mention [" + newArg + "]");
				}
				else
				{
					/*
					logger.info("Failed to match relation argument: " + goldEnt);
					return false;
					*/
					newArgs.Add(goldEnt);
					predictedMentions.Add(goldEnt);
					logger.Info("Failed to match relation argument, so keeping gold: " + goldEnt);
				}
			}
			this.args = newArgs;
			return true;
		}

		public virtual void RemoveArgument(ExtractionObject argToRemove, bool removeParent)
		{
			ICollection<ExtractionObject> thisEvent = new IdentityHashSet<ExtractionObject>();
			thisEvent.Add(argToRemove);
			RemoveArguments(thisEvent, removeParent);
		}

		public virtual void RemoveArguments(ICollection<ExtractionObject> argsToRemove, bool removeParent)
		{
			IList<ExtractionObject> newArgs = new List<ExtractionObject>();
			IList<string> newArgNames = new List<string>();
			for (int i = 0; i < args.Count; i++)
			{
				ExtractionObject a = args[i];
				string n = argNames[i];
				if (!argsToRemove.Contains(a))
				{
					newArgs.Add(a);
					newArgNames.Add(n);
				}
				else
				{
					if (a is EventMention && removeParent)
					{
						((EventMention)a).RemoveParent(this);
					}
				}
			}
			args = newArgs;
			argNames = newArgNames;
		}

		public virtual bool PrintableObject(double beam)
		{
			return PrintableObject(beam, Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention.Unrelated);
		}

		public virtual void SetSignature(string s)
		{
			signature = s;
		}

		public virtual string GetSignature()
		{
			return signature;
		}

		/*
		* Static utility functions
		*/
		public static ICollection<Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention> FilterUnrelatedRelations(ICollection<Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention> relationMentions)
		{
			ICollection<Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention> filtered = new List<Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention>();
			foreach (Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention relation in relationMentions)
			{
				if (!relation.GetType().Equals(Unrelated))
				{
					filtered.Add(relation);
				}
			}
			return filtered;
		}

		/// <summary>Creates a new unique id for a relation mention</summary>
		/// <returns>the new id</returns>
		public static string MakeUniqueId()
		{
			lock (typeof(RelationMention))
			{
				MentionCounter++;
				return "RelationMention-" + MentionCounter;
			}
		}

		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention CreateUnrelatedRelation(RelationMentionFactory factory, params ExtractionObject[] args)
		{
			return CreateUnrelatedRelation(factory, string.Empty, args);
		}

		private static Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention CreateUnrelatedRelation(RelationMentionFactory factory, string type, params ExtractionObject[] args)
		{
			return factory.ConstructRelationMention(Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention.MakeUniqueId(), args[0].GetSentence(), ExtractionObject.GetSpan(args), Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention.Unrelated
				 + type, null, Arrays.AsList(args), null);
		}

		public static bool IsUnrelatedLabel(string label)
		{
			return label.StartsWith(Unrelated);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention))
			{
				return false;
			}
			if (!base.Equals(o))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention that = (Edu.Stanford.Nlp.IE.Machinereading.Structure.RelationMention)o;
			if (argNames != null ? !argNames.Equals(that.argNames) : that.argNames != null)
			{
				return false;
			}
			if (args != null ? !args.Equals(that.args) : that.args != null)
			{
				return false;
			}
			if (signature != null ? !signature.Equals(that.signature) : that.signature != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = argNames != null ? argNames.GetHashCode() : 0;
			result = 31 * result + (args != null ? args.GetHashCode() : 0);
			result = 31 * result + (signature != null ? signature.GetHashCode() : 0);
			return result;
		}
	}
}
