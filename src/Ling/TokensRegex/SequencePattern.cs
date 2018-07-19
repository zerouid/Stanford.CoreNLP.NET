using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Generic Sequence Pattern for regular expressions.</summary>
	/// <remarks>
	/// Generic Sequence Pattern for regular expressions.
	/// <p>
	/// Similar to Java's
	/// <see cref="Java.Util.Regex.Pattern"/>
	/// except it is for sequences over arbitrary types T instead
	/// of just characters.
	/// <p> A regular expression must first be compiled into
	/// an instance of this class.  The resulting pattern can then be used to create
	/// a
	/// <see cref="SequenceMatcher{T}"/>
	/// object that can match arbitrary sequences of type T
	/// against the regular expression.  All of the state involved in performing a match
	/// resides in the matcher, so many matchers can share the same pattern.
	/// <p>
	/// To support sequence matching on a new type T, the following is needed:
	/// <ul>
	/// <li>Implement a
	/// <see cref="NodePattern{T}">for matching type T</see>
	/// </li>
	/// <li>Optionally define a language for node matches and implement
	/// <see cref="IParser{T}"/>
	/// to compile a
	/// regular expression into a SequencePattern.
	/// </li>
	/// <li>Optionally implement a
	/// <see cref="INodePatternTrigger{T}"/>
	/// for optimizing matches across multiple patterns</li>
	/// <li>Optionally implement a
	/// <see cref="INodesMatchChecker{T}"/>
	/// to support backreferences</li>
	/// </ul>
	/// See
	/// <see cref="TokenSequencePattern"/>
	/// for an example of how this class can be extended
	/// to support a specific type
	/// <c>T</c>
	/// .
	/// <p>
	/// To use
	/// <pre>
	/// <c>
	/// SequencePattern p = SequencePattern.compile("....");
	/// SequenceMatcher m = p.getMatcher(tokens);
	/// while (m.find()) ....
	/// </c>
	/// </pre>
	/// <p>
	/// To support a new type
	/// <c>T</c>
	/// :
	/// <ol>
	/// <li> For a type
	/// <c>T</c>
	/// to be matchable, it has to have a corresponding
	/// <c>NodePattern&lt;T&gt;</c>
	/// that indicates
	/// whether a node is matched or not  (see
	/// <c>CoreMapNodePattern</c>
	/// for example)</li>
	/// <li> To compile a string into corresponding pattern, will need to create a parser
	/// (see inner class
	/// <c>Parser</c>
	/// ,
	/// <c>TokenSequencePattern</c>
	/// and
	/// <c>TokenSequenceParser.jj</c>
	/// )</li>
	/// </ol>
	/// <p>
	/// SequencePattern supports the following standard regex features:
	/// <ul>
	/// <li>Concatenation </li>
	/// <li>Or </li>
	/// <li>Groups  (capturing  / noncapturing )  </li>
	/// <li>Quantifiers (greedy / nongreedy) </li>
	/// </ul>
	/// <p>
	/// SequencePattern also supports the following less standard features:
	/// <ol>
	/// <li> Environment (see
	/// <see cref="Env"/>
	/// ) with respect to which the patterns are compiled</li>
	/// <li> Binding of variables
	/// <br />Use
	/// <see cref="Env"/>
	/// to bind variables for use when compiling patterns
	/// <br />Can also bind names to groups (see
	/// <see cref="ISequenceMatchResult{T}"/>
	/// for accessor methods to retrieve matched groups)
	/// </li>
	/// <li> Backreference matches - need to specify how back references are to be matched using
	/// <see cref="INodesMatchChecker{T}"/>
	/// </li>
	/// <li> Multinode matches - for matching of multiple nodes using non-regex (at least not regex over nodes) patterns
	/// (need to have corresponding
	/// <see cref="MultiNodePattern{T}"/>
	/// ,
	/// see
	/// <see cref="MultiCoreMapNodePattern"/>
	/// for example) </li>
	/// <li> Conjunctions - conjunctions of sequence patterns (works for some cases)</li>
	/// </ol>
	/// <p>Note that this and the inherited classes do not implement any custom equals and hashCode functions.
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <seealso cref="SequenceMatcher{T}"/>
	[System.Serializable]
	public class SequencePattern<T>
	{
		private const long serialVersionUID = 3484918485303693833L;

		private string patternStr;

		private SequencePattern.PatternExpr patternExpr;

		private ISequenceMatchAction<T> action;

		internal SequencePattern.State root;

		internal int totalGroups = 0;

		internal SequencePattern.VarGroupBindings varGroupBindings;

		internal double priority = 0.0;

		internal double weight = 0.0;

		protected internal SequencePattern(SequencePattern.PatternExpr nodeSequencePattern)
			: this(null, nodeSequencePattern)
		{
		}

		protected internal SequencePattern(string patternStr, SequencePattern.PatternExpr nodeSequencePattern)
			: this(patternStr, nodeSequencePattern, null)
		{
		}

		protected internal SequencePattern(string patternStr, SequencePattern.PatternExpr nodeSequencePattern, ISequenceMatchAction<T> action)
		{
			// TODO:
			//  1. Validate backref capture groupid
			//  2. Actions
			//  3. Inconsistent templating with T
			//  4. Update TokensSequenceParser to handle backref of other attributes (\9{attr1,attr2,...})
			//  5. Improve nested capture groups (in matchresult) for other node types such as conjunctions/disjunctions
			// binding of group number to variable name
			// Priority associated with the pattern (higher priority patterns should take precedence over lower priority ones)
			// Weight associated with the pattern
			this.patternStr = patternStr;
			this.patternExpr = nodeSequencePattern;
			this.action = action;
			nodeSequencePattern = new SequencePattern.GroupPatternExpr(nodeSequencePattern, true);
			nodeSequencePattern = nodeSequencePattern.Optimize();
			this.totalGroups = nodeSequencePattern.AssignGroupIds(0);
			SequencePattern.Frag f = nodeSequencePattern.Build();
			f.Connect(MatchState);
			this.root = f.start;
			varGroupBindings = new SequencePattern.VarGroupBindings(totalGroups + 1);
			nodeSequencePattern.UpdateBindings(varGroupBindings);
		}

		public override string ToString()
		{
			return this.Pattern();
		}

		public virtual Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T2> Transform<T2>(INodePatternTransformer<T, T2> transformer)
		{
			if (action != null)
			{
				throw new NotSupportedException("transform on actions not yet implemented");
			}
			SequencePattern.PatternExpr transformedPattern = this.patternExpr.Transform(transformer);
			// TODO: Make string unique by indicating this pattern was transformed
			return new Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T2>(this.patternStr, transformedPattern, null);
		}

		public virtual string Pattern()
		{
			return patternStr;
		}

		protected internal virtual SequencePattern.PatternExpr GetPatternExpr()
		{
			return patternExpr;
		}

		public virtual double GetPriority()
		{
			return priority;
		}

		public virtual void SetPriority(double priority)
		{
			this.priority = priority;
		}

		public virtual double GetWeight()
		{
			return weight;
		}

		public virtual void SetWeight(double weight)
		{
			this.weight = weight;
		}

		public virtual ISequenceMatchAction<T> GetAction()
		{
			return action;
		}

		public virtual void SetAction(ISequenceMatchAction<T> action)
		{
			this.action = action;
		}

		public virtual int GetTotalGroups()
		{
			return totalGroups;
		}

		// Compiles string (regex) to NFA for doing pattern simulation
		public static Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T> Compile<T>(Env env, string @string)
		{
			try
			{
				Pair<SequencePattern.PatternExpr, ISequenceMatchAction<T>> p = env.parser.ParseSequenceWithAction(env, @string);
				return new Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T>(@string, p.First(), p.Second());
			}
			catch (Exception)
			{
				throw new Exception("Error compiling " + @string + " using environment " + env);
			}
		}

		//throw new UnsupportedOperationException("Compile from string not implemented");
		protected internal static Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T> Compile<T>(SequencePattern.PatternExpr nodeSequencePattern)
		{
			return new Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern<T>(nodeSequencePattern);
		}

		public virtual SequenceMatcher<T> GetMatcher<_T0>(IList<_T0> tokens)
			where _T0 : T
		{
			return new SequenceMatcher<T>(this, tokens);
		}

		public virtual OUT FindNodePattern<Out>(IFunction<NodePattern<T>, OUT> filter)
		{
			IQueue<SequencePattern.State> todo = new LinkedList<SequencePattern.State>();
			ICollection<SequencePattern.State> seen = new HashSet<SequencePattern.State>();
			todo.Add(root);
			seen.Add(root);
			while (!todo.IsEmpty())
			{
				SequencePattern.State state = todo.Poll();
				if (state is SequencePattern.NodePatternState)
				{
					NodePattern<T> pattern = ((SequencePattern.NodePatternState)state).pattern;
					OUT res = filter.Apply(pattern);
					if (res != null)
					{
						return res;
					}
				}
				if (state.next != null)
				{
					foreach (SequencePattern.State s in state.next)
					{
						if (!seen.Contains(s))
						{
							seen.Add(s);
							todo.Add(s);
						}
					}
				}
			}
			return null;
		}

		public virtual ICollection<OUT> FindNodePatterns<Out>(IFunction<NodePattern<T>, OUT> filter, bool allowOptional, bool allowBranching)
		{
			IList<OUT> outList = new List<OUT>();
			IQueue<SequencePattern.State> todo = new LinkedList<SequencePattern.State>();
			ICollection<SequencePattern.State> seen = new HashSet<SequencePattern.State>();
			todo.Add(root);
			seen.Add(root);
			while (!todo.IsEmpty())
			{
				SequencePattern.State state = todo.Poll();
				if ((allowOptional || !state.isOptional) && (state is SequencePattern.NodePatternState))
				{
					NodePattern<T> pattern = ((SequencePattern.NodePatternState)state).pattern;
					OUT res = filter.Apply(pattern);
					if (res != null)
					{
						outList.Add(res);
					}
				}
				if (state.next != null)
				{
					bool addNext = allowBranching || state.next.Count == 1;
					if (addNext)
					{
						foreach (SequencePattern.State s in state.next)
						{
							if (!seen.Contains(s))
							{
								seen.Add(s);
								todo.Add(s);
							}
						}
					}
				}
			}
			return outList;
		}

		public interface IParser<T>
		{
			// Parses string to PatternExpr
			/// <exception cref="System.Exception"/>
			SequencePattern.PatternExpr ParseSequence(Env env, string s);

			/// <exception cref="System.Exception"/>
			Pair<SequencePattern.PatternExpr, ISequenceMatchAction<T>> ParseSequenceWithAction(Env env, string s);

			/// <exception cref="System.Exception"/>
			SequencePattern.PatternExpr ParseNode(Env env, string s);
		}

		internal class VarGroupBindings
		{
			internal readonly string[] varnames;

			protected internal VarGroupBindings(int size)
			{
				// Binding of variable names to groups
				// matches the group indices
				// Assumes number of groups low
				varnames = new string[size];
			}

			protected internal virtual void Set(int index, string name)
			{
				varnames[index] = name;
			}
		}

		protected internal interface INodesMatchChecker<T>
		{
			// Interface indicating when two nodes match
			bool Matches(T o1, T o2);
		}

		private sealed class _INodesMatchChecker_281 : SequencePattern.INodesMatchChecker<object>
		{
			public _INodesMatchChecker_281()
			{
			}

			public bool Matches(object o1, object o2)
			{
				return o1.Equals(o2);
			}
		}

		public static readonly SequencePattern.INodesMatchChecker<object> NodesEqualChecker = new _INodesMatchChecker_281();

		public static readonly SequencePattern.PatternExpr AnyNodePatternExpr = new SequencePattern.NodePatternExpr(NodePattern.AnyNode);

		public static readonly SequencePattern.PatternExpr SeqBeginPatternExpr = new SequencePattern.SequenceStartPatternExpr();

		public static readonly SequencePattern.PatternExpr SeqEndPatternExpr = new SequencePattern.SequenceEndPatternExpr();

		/// <summary>Represents a sequence pattern expressions (before translating into NFA).</summary>
		[System.Serializable]
		public abstract class PatternExpr
		{
			private const long serialVersionUID = 7610237291757954879L;

			protected internal abstract SequencePattern.Frag Build();

			/// <summary>
			/// Assigns group ids to groups embedded in this patterns starting with at the specified number,
			/// returns the next available group id.
			/// </summary>
			/// <param name="start">Group id to start with</param>
			/// <returns>The next available group id</returns>
			protected internal abstract int AssignGroupIds(int start);

			/// <summary>Make a deep copy of the sequence pattern expressions</summary>
			protected internal abstract SequencePattern.PatternExpr Copy();

			/// <summary>Updates the binding of group to variable name</summary>
			/// <param name="bindings"/>
			protected internal abstract void UpdateBindings(SequencePattern.VarGroupBindings bindings);

			protected internal virtual object Value()
			{
				return null;
			}

			/// <summary>Returns an optimized version of this pattern - default is a noop</summary>
			protected internal virtual SequencePattern.PatternExpr Optimize()
			{
				return this;
			}

			protected internal abstract SequencePattern.PatternExpr Transform(INodePatternTransformer transformer);
		}

		/// <summary>Represents one element to be matched.</summary>
		[System.Serializable]
		public class NodePatternExpr : SequencePattern.PatternExpr
		{
			internal readonly NodePattern nodePattern;

			public NodePatternExpr(NodePattern nodePattern)
			{
				this.nodePattern = nodePattern;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.State s = new SequencePattern.NodePatternState(nodePattern);
				return new SequencePattern.Frag(s);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.NodePatternExpr(nodePattern);
			}

			protected internal override int AssignGroupIds(int start)
			{
				return start;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.NodePatternExpr(transformer.Transform(nodePattern));
			}

			public override string ToString()
			{
				return nodePattern.ToString();
			}
		}

		/// <summary>Represents a pattern that can match multiple nodes.</summary>
		[System.Serializable]
		public class MultiNodePatternExpr : SequencePattern.PatternExpr
		{
			private readonly MultiNodePattern multiNodePattern;

			public MultiNodePatternExpr(MultiNodePattern nodePattern)
			{
				this.multiNodePattern = nodePattern;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.State s = new SequencePattern.MultiNodePatternState(multiNodePattern);
				return new SequencePattern.Frag(s);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.MultiNodePatternExpr(multiNodePattern);
			}

			protected internal override int AssignGroupIds(int start)
			{
				return start;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.MultiNodePatternExpr(transformer.Transform(multiNodePattern));
			}

			public override string ToString()
			{
				return multiNodePattern.ToString();
			}
		}

		/// <summary>Represents one element to be matched.</summary>
		[System.Serializable]
		public class SpecialNodePatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = 3347587132602082616L;

			private readonly string name;

			internal IFactory<SequencePattern.State> stateFactory;

			public SpecialNodePatternExpr(string name)
				: this(name, null)
			{
			}

			public SpecialNodePatternExpr(string name, IFactory<SequencePattern.State> stateFactory)
			{
				this.name = name;
				this.stateFactory = stateFactory;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.State s = stateFactory.Create();
				return new SequencePattern.Frag(s);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.SpecialNodePatternExpr(name, stateFactory);
			}

			protected internal override int AssignGroupIds(int start)
			{
				return start;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.SpecialNodePatternExpr(name, stateFactory);
			}

			public override string ToString()
			{
				return name;
			}
		}

		[System.Serializable]
		public class SequenceStartPatternExpr : SequencePattern.SpecialNodePatternExpr, IFactory<SequencePattern.State>
		{
			public SequenceStartPatternExpr()
				: base("SEQ_START")
			{
				this.stateFactory = this;
			}

			public virtual SequencePattern.State Create()
			{
				return new SequencePattern.SeqStartState();
			}
		}

		[System.Serializable]
		public class SequenceEndPatternExpr : SequencePattern.SpecialNodePatternExpr, IFactory<SequencePattern.State>
		{
			public SequenceEndPatternExpr()
				: base("SEQ_END")
			{
				this.stateFactory = this;
			}

			public virtual SequencePattern.State Create()
			{
				return new SequencePattern.SeqEndState();
			}
		}

		/// <summary>Represents a sequence of patterns to be matched.</summary>
		[System.Serializable]
		public class SequencePatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = 7446769896088599604L;

			internal readonly IList<SequencePattern.PatternExpr> patterns;

			public SequencePatternExpr(IList<SequencePattern.PatternExpr> patterns)
			{
				this.patterns = patterns;
			}

			public SequencePatternExpr(params SequencePattern.PatternExpr[] patterns)
			{
				this.patterns = Arrays.AsList(patterns);
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.Frag frag = null;
				if (patterns.Count > 0)
				{
					SequencePattern.PatternExpr first = patterns[0];
					frag = first.Build();
					for (int i = 1; i < patterns.Count; i++)
					{
						SequencePattern.PatternExpr pattern = patterns[i];
						SequencePattern.Frag f = pattern.Build();
						frag.Connect(f);
					}
				}
				return frag;
			}

			protected internal override int AssignGroupIds(int start)
			{
				int nextId = start;
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					nextId = pattern.AssignGroupIds(nextId);
				}
				return nextId;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					pattern.UpdateBindings(bindings);
				}
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Copy());
				}
				return new SequencePattern.SequencePatternExpr(newPatterns);
			}

			protected internal override SequencePattern.PatternExpr Optimize()
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Optimize());
				}
				return new SequencePattern.SequencePatternExpr(newPatterns);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Transform(transformer));
				}
				return new SequencePattern.SequencePatternExpr(newPatterns);
			}

			public override string ToString()
			{
				return StringUtils.Join(patterns, " ");
			}
		}

		[System.Serializable]
		public class BackRefPatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = -4649629486266561619L;

			private readonly SequencePattern.INodesMatchChecker matcher;

			private readonly int captureGroupId;

			public BackRefPatternExpr(SequencePattern.INodesMatchChecker matcher, int captureGroupId)
			{
				// Expression that indicates a back reference
				// Need to match a previously matched group somehow
				// How a match is determined
				// Indicates the previously matched group this need to match
				if (captureGroupId <= 0)
				{
					throw new ArgumentException("Invalid captureGroupId=" + captureGroupId);
				}
				this.captureGroupId = captureGroupId;
				this.matcher = matcher;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.State s = new SequencePattern.BackRefState(matcher, captureGroupId);
				return new SequencePattern.Frag(s);
			}

			protected internal override int AssignGroupIds(int start)
			{
				return start;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.BackRefPatternExpr(matcher, captureGroupId);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				// TODO: Implement me!!!
				throw new NotSupportedException("BackRefPatternExpr.transform not implemented yet!!! Please implement me!!!");
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				if (captureGroupId >= 0)
				{
					sb.Append('\\').Append(captureGroupId);
				}
				else
				{
					sb.Append('\\');
				}
				sb.Append('{').Append(matcher).Append('}');
				return sb.ToString();
			}
		}

		[System.Serializable]
		public class ValuePatternExpr : SequencePattern.PatternExpr
		{
			private readonly SequencePattern.PatternExpr expr;

			private readonly object value;

			public ValuePatternExpr(SequencePattern.PatternExpr expr, object value)
			{
				this.expr = expr;
				this.value = value;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.Frag frag = expr.Build();
				frag.Connect(new SequencePattern.ValueState(value));
				return frag;
			}

			protected internal override int AssignGroupIds(int start)
			{
				return expr.AssignGroupIds(start);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.ValuePatternExpr(expr.Copy(), value);
			}

			protected internal override SequencePattern.PatternExpr Optimize()
			{
				return new SequencePattern.ValuePatternExpr(expr.Optimize(), value);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.ValuePatternExpr(expr.Transform(transformer), value);
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				expr.UpdateBindings(bindings);
			}
		}

		/// <summary>Expression that represents a group.</summary>
		[System.Serializable]
		public class GroupPatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = -6477601300665620926L;

			private readonly SequencePattern.PatternExpr pattern;

			private readonly bool capture;

			private int captureGroupId;

			private readonly string varname;

			public GroupPatternExpr(SequencePattern.PatternExpr pattern)
				: this(pattern, true)
			{
			}

			public GroupPatternExpr(SequencePattern.PatternExpr pattern, bool capture)
				: this(pattern, capture, -1, null)
			{
			}

			public GroupPatternExpr(SequencePattern.PatternExpr pattern, string varname)
				: this(pattern, true, -1, varname)
			{
			}

			private GroupPatternExpr(SequencePattern.PatternExpr pattern, bool capture, int captureGroupId, string varname)
			{
				// Do capture or not?  If do capture, an capture group id will be assigned
				// -1 if this pattern is not part of a capture group or capture group not yet assigned,
				// otherwise, capture group number
				// Alternate variable with which to refer to this group
				this.pattern = pattern;
				this.capture = capture;
				this.captureGroupId = captureGroupId;
				this.varname = varname;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.Frag f = pattern.Build();
				SequencePattern.Frag frag = new SequencePattern.Frag(new SequencePattern.GroupStartState(captureGroupId, f.start), f.@out);
				frag.Connect(new SequencePattern.GroupEndState(captureGroupId));
				return frag;
			}

			protected internal override int AssignGroupIds(int start)
			{
				int nextId = start;
				if (capture)
				{
					captureGroupId = nextId;
					nextId++;
				}
				return pattern.AssignGroupIds(nextId);
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				if (varname != null)
				{
					bindings.Set(captureGroupId, varname);
				}
				pattern.UpdateBindings(bindings);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.GroupPatternExpr(pattern.Copy(), capture, captureGroupId, varname);
			}

			protected internal override SequencePattern.PatternExpr Optimize()
			{
				return new SequencePattern.GroupPatternExpr(pattern.Optimize(), capture, captureGroupId, varname);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.GroupPatternExpr(pattern.Transform(transformer), capture, captureGroupId, varname);
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append('(');
				if (!capture)
				{
					sb.Append("?: ");
				}
				else
				{
					if (varname != null)
					{
						sb.Append('?').Append(varname).Append(' ');
					}
				}
				sb.Append(pattern);
				sb.Append(')');
				return sb.ToString();
			}
		}

		/// <summary>Expression that represents a pattern that repeats for a number of times.</summary>
		[System.Serializable]
		public class RepeatPatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = 3935482630250147745L;

			private readonly SequencePattern.PatternExpr pattern;

			private readonly int minMatch;

			private readonly int maxMatch;

			private readonly bool greedyMatch;

			public RepeatPatternExpr(SequencePattern.PatternExpr pattern, int minMatch, int maxMatch)
				: this(pattern, minMatch, maxMatch, true)
			{
			}

			public RepeatPatternExpr(SequencePattern.PatternExpr pattern, int minMatch, int maxMatch, bool greedy)
			{
				if (minMatch < 0)
				{
					throw new ArgumentException("Invalid minMatch=" + minMatch);
				}
				if (maxMatch >= 0 && minMatch > maxMatch)
				{
					throw new ArgumentException("Invalid minMatch=" + minMatch + ", maxMatch=" + maxMatch);
				}
				this.pattern = pattern;
				this.minMatch = minMatch;
				this.maxMatch = maxMatch;
				this.greedyMatch = greedy;
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.Frag f = pattern.Build();
				if (minMatch == 1 && maxMatch == 1)
				{
					return f;
				}
				else
				{
					if (minMatch <= 5 && maxMatch <= 5 && greedyMatch)
					{
						// Make copies if number of matches is low
						// Doesn't handle nongreedy matches yet
						// For non greedy match need to move curOut before the recursive connect
						// Create NFA fragment that
						// have child pattern repeating for minMatch times
						if (minMatch > 0)
						{
							//  frag.start -> pattern NFA -> pattern NFA ->
							for (int i = 0; i < minMatch - 1; i++)
							{
								SequencePattern.Frag f2 = pattern.Build();
								f.Connect(f2);
							}
						}
						else
						{
							// minMatch is 0
							// frag.start ->
							f = new SequencePattern.Frag(new SequencePattern.State());
						}
						if (maxMatch < 0)
						{
							// Unlimited (loop back to self)
							//        --------
							//       \|/     |
							// ---> pattern NFA --->
							ICollection<SequencePattern.State> curOut = f.@out;
							SequencePattern.Frag f2 = pattern.Build();
							f2.Connect(f2);
							f.Connect(f2);
							f.Add(curOut);
						}
						else
						{
							// Limited number of times this pattern repeat,
							// just keep add pattern (with option of being done) until maxMatch reached
							// ----> pattern NFA ----> pattern NFA --->
							//   |                |
							//   -->              --->
							for (int i = minMatch; i < maxMatch; i++)
							{
								ICollection<SequencePattern.State> curOut = f.@out;
								SequencePattern.Frag f2 = pattern.Build();
								f.Connect(f2);
								f.Add(curOut);
							}
						}
						if (minMatch == 0)
						{
							f.start.MarkOptional(true);
						}
						return f;
					}
					else
					{
						// More general but more expensive matching (when branching, need to keep state explicitly)
						SequencePattern.State s = new SequencePattern.RepeatState(f.start, minMatch, maxMatch, greedyMatch);
						f.Connect(s);
						return new SequencePattern.Frag(s);
					}
				}
			}

			protected internal override int AssignGroupIds(int start)
			{
				return pattern.AssignGroupIds(start);
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				pattern.UpdateBindings(bindings);
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				return new SequencePattern.RepeatPatternExpr(pattern.Copy(), minMatch, maxMatch, greedyMatch);
			}

			protected internal override SequencePattern.PatternExpr Optimize()
			{
				return new SequencePattern.RepeatPatternExpr(pattern.Optimize(), minMatch, maxMatch, greedyMatch);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				return new SequencePattern.RepeatPatternExpr(pattern.Transform(transformer), minMatch, maxMatch, greedyMatch);
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(pattern);
				sb.Append('{').Append(minMatch).Append(',').Append(maxMatch).Append('}');
				if (!greedyMatch)
				{
					sb.Append('?');
				}
				return sb.ToString();
			}
		}

		/// <summary>Expression that represents a disjunction.</summary>
		[System.Serializable]
		public class OrPatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = 2566259662702631896L;

			private readonly IList<SequencePattern.PatternExpr> patterns;

			public OrPatternExpr(IList<SequencePattern.PatternExpr> patterns)
			{
				this.patterns = patterns;
			}

			public OrPatternExpr(params SequencePattern.PatternExpr[] patterns)
			{
				this.patterns = Arrays.AsList(patterns);
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.Frag frag = new SequencePattern.Frag();
				frag.start = new SequencePattern.State();
				// Create NFA fragment that
				// have one starting state that branches out to NFAs created by the children expressions
				//  ---> pattern 1 --->
				//   |
				//   ---> pattern 2 --->
				//   ...
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					// Build child NFA
					SequencePattern.Frag f = pattern.Build();
					if (pattern.Value() != null)
					{
						// Add value state to child NFA
						f.Connect(new SequencePattern.ValueState(pattern.Value()));
					}
					// Add child NFA to next states of fragment start
					frag.start.Add(f.start);
					// Add child NFA out (unlinked) states to out (unlinked) states of this fragment
					frag.Add(f.@out);
				}
				frag.start.MarkOptional(true);
				return frag;
			}

			protected internal override int AssignGroupIds(int start)
			{
				int nextId = start;
				// assign group ids of child expressions
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					nextId = pattern.AssignGroupIds(nextId);
				}
				return nextId;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				// update bindings of child expressions
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					pattern.UpdateBindings(bindings);
				}
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Copy());
				}
				return new SequencePattern.OrPatternExpr(newPatterns);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Transform(transformer));
				}
				return new SequencePattern.OrPatternExpr(newPatterns);
			}

			public override string ToString()
			{
				return StringUtils.Join(patterns, " | ");
			}

			private const int OptimizeMinSize = 5;

			// minimize size of or clauses to trigger optimization
			protected internal override SequencePattern.PatternExpr Optimize()
			{
				if (patterns.Count <= OptimizeMinSize)
				{
					// Not enough patterns for fancy optimization
					IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
					foreach (SequencePattern.PatternExpr p in patterns)
					{
						newPatterns.Add(p.Optimize());
					}
					return new SequencePattern.OrPatternExpr(newPatterns);
				}
				else
				{
					// More fancy optimization
					return OptimizeOr();
				}
			}

			private SequencePattern.PatternExpr OptimizeOr()
			{
				SequencePattern.PatternExpr optimizedStringSeqs = OptimizeOrStringSeqs();
				// Go through patterns and get candidate sequences with the same start...
				return optimizedStringSeqs;
			}

			private SequencePattern.PatternExpr OptimizeOrStringSeqs()
			{
				// Try to collapse OR of NodePattern with just strings into a StringInSetAnnotationPattern
				IList<SequencePattern.PatternExpr> opts = new List<SequencePattern.PatternExpr>(patterns.Count);
				// Map from annotation key (Class), ignoreCase (Boolean) to set of patterns/strings
				IDictionary<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>>> stringPatterns = new Dictionary<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>>>();
				IDictionary<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>>> stringSeqPatterns = new Dictionary<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>>>();
				// Go through patterns and get candidates for optimization
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					SequencePattern.PatternExpr opt = p.Optimize();
					opts.Add(opt);
					// Check for special patterns that we can optimize
					if (opt is SequencePattern.NodePatternExpr)
					{
						Pair<Type, ComplexNodePattern.StringAnnotationPattern> pair = _getStringAnnotation_(opt);
						if (pair != null)
						{
							bool ignoreCase = pair.second.IgnoreCase();
							string target = pair.second.target;
							Pair<Type, bool> key = Pair.MakePair(pair.first, ignoreCase);
							Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>> saved = stringPatterns[key];
							if (saved == null)
							{
								saved = new Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>>(new List<SequencePattern.PatternExpr>(), new HashSet<string>());
								stringPatterns[key] = saved;
							}
							saved.first.Add(opt);
							saved.second.Add(target);
						}
					}
					else
					{
						if (opt is SequencePattern.SequencePatternExpr)
						{
							SequencePattern.SequencePatternExpr seq = (SequencePattern.SequencePatternExpr)opt;
							if (seq.patterns.Count > 0)
							{
								bool isStringSeq = true;
								Pair<Type, bool> key = null;
								IList<string> strings = null;
								foreach (SequencePattern.PatternExpr sp in seq.patterns)
								{
									// check if string match over same key
									Pair<Type, ComplexNodePattern.StringAnnotationPattern> pair = _getStringAnnotation_(sp);
									if (pair != null)
									{
										if (key != null)
										{
											// check key
											if (key.first.Equals(pair.first) && key.second.Equals(pair.second.IgnoreCase()))
											{
											}
											else
											{
												// okay
												isStringSeq = false;
												break;
											}
										}
										else
										{
											key = Pair.MakePair(pair.first, pair.second.IgnoreCase());
											strings = new List<string>();
										}
										strings.Add(pair.second.target);
									}
									else
									{
										isStringSeq = false;
										break;
									}
								}
								if (isStringSeq)
								{
									Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>> saved = stringSeqPatterns[key];
									if (saved == null)
									{
										saved = new Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>>(new List<SequencePattern.PatternExpr>(), new HashSet<IList<string>>());
										stringSeqPatterns[key] = saved;
									}
									saved.first.Add(opt);
									saved.second.Add(strings);
								}
							}
						}
					}
				}
				// Go over our maps and see if any of these strings should be optimized away
				// Keep track of things we have optimized away
				IDictionary<SequencePattern.PatternExpr, bool> alreadyOptimized = new IdentityHashMap<SequencePattern.PatternExpr, bool>();
				IList<SequencePattern.PatternExpr> finalOptimizedPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				// optimize strings
				foreach (KeyValuePair<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>>> entry in stringPatterns)
				{
					Pair<ICollection<SequencePattern.PatternExpr>, ICollection<string>> saved = entry.Value;
					ICollection<string> set = saved.second;
					int flags = (entry.Key.second) ? (NodePattern.CaseInsensitive | NodePattern.UnicodeCase) : 0;
					if (set.Count > OptimizeMinSize)
					{
						SequencePattern.PatternExpr optimized = new SequencePattern.NodePatternExpr(new CoreMapNodePattern(entry.Key.first, new ComplexNodePattern.StringInSetAnnotationPattern(set, flags)));
						finalOptimizedPatterns.Add(optimized);
						foreach (SequencePattern.PatternExpr p_1 in saved.first)
						{
							alreadyOptimized[p_1] = true;
						}
					}
				}
				// optimize string sequences
				foreach (KeyValuePair<Pair<Type, bool>, Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>>> entry_1 in stringSeqPatterns)
				{
					Pair<ICollection<SequencePattern.PatternExpr>, ICollection<IList<string>>> saved = entry_1.Value;
					ICollection<IList<string>> set = saved.second;
					if (set.Count > OptimizeMinSize)
					{
						Pair<Type, bool> key = entry_1.Key;
						SequencePattern.PatternExpr optimized = new SequencePattern.MultiNodePatternExpr(new MultiCoreMapNodePattern.StringSequenceAnnotationPattern(key.First(), set, key.Second()));
						finalOptimizedPatterns.Add(optimized);
						foreach (SequencePattern.PatternExpr p_1 in saved.first)
						{
							alreadyOptimized[p_1] = true;
						}
					}
				}
				// Add back original stuff that we didn't optimize
				foreach (SequencePattern.PatternExpr p_2 in opts)
				{
					bool included = alreadyOptimized[p_2];
					if (included == null || !included)
					{
						finalOptimizedPatterns.Add(p_2);
					}
				}
				return new SequencePattern.OrPatternExpr(finalOptimizedPatterns);
			}

			private static Pair<Type, ComplexNodePattern.StringAnnotationPattern> _getStringAnnotation_(SequencePattern.PatternExpr p)
			{
				if (p is SequencePattern.NodePatternExpr)
				{
					NodePattern nodePattern = ((SequencePattern.NodePatternExpr)p).nodePattern;
					if (nodePattern is CoreMapNodePattern)
					{
						IList<Pair<Type, NodePattern>> annotationPatterns = ((CoreMapNodePattern)nodePattern).GetAnnotationPatterns();
						if (annotationPatterns.Count == 1)
						{
							// Check if it is a string annotation pattern
							Pair<Type, NodePattern> pair = annotationPatterns[0];
							if (pair.second is ComplexNodePattern.StringAnnotationPattern)
							{
								return Pair.MakePair(pair.first, (ComplexNodePattern.StringAnnotationPattern)pair.second);
							}
						}
					}
				}
				return null;
			}
		}

		[System.Serializable]
		public class AndPatternExpr : SequencePattern.PatternExpr
		{
			private const long serialVersionUID = -5470437627660213806L;

			private readonly IList<SequencePattern.PatternExpr> patterns;

			public AndPatternExpr(IList<SequencePattern.PatternExpr> patterns)
			{
				// Expression that represents a conjunction
				this.patterns = patterns;
			}

			public AndPatternExpr(params SequencePattern.PatternExpr[] patterns)
			{
				this.patterns = Arrays.AsList(patterns);
			}

			protected internal override SequencePattern.Frag Build()
			{
				SequencePattern.ConjStartState conjStart = new SequencePattern.ConjStartState(patterns.Count);
				SequencePattern.Frag frag = new SequencePattern.Frag();
				frag.start = conjStart;
				// Create NFA fragment that
				// have one starting state that branches out to NFAs created by the children expressions
				// AND START ---> pattern 1 --->  AND END (0/n)
				//            |
				//             ---> pattern 2 ---> AND END (1/n)
				//             ...
				for (int i = 0; i < patterns.Count; i++)
				{
					SequencePattern.PatternExpr pattern = patterns[i];
					// Build child NFA
					SequencePattern.Frag f = pattern.Build();
					// Add child NFA to next states of fragment start
					frag.start.Add(f.start);
					f.Connect(new SequencePattern.ConjEndState(conjStart, i));
					// Add child NFA out (unlinked) states to out (unlinked) states of this fragment
					frag.Add(f.@out);
				}
				return frag;
			}

			protected internal override int AssignGroupIds(int start)
			{
				int nextId = start;
				// assign group ids of child expressions
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					nextId = pattern.AssignGroupIds(nextId);
				}
				return nextId;
			}

			protected internal override void UpdateBindings(SequencePattern.VarGroupBindings bindings)
			{
				// update bindings of child expressions
				foreach (SequencePattern.PatternExpr pattern in patterns)
				{
					pattern.UpdateBindings(bindings);
				}
			}

			protected internal override SequencePattern.PatternExpr Copy()
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Copy());
				}
				return new SequencePattern.AndPatternExpr(newPatterns);
			}

			protected internal override SequencePattern.PatternExpr Optimize()
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Optimize());
				}
				return new SequencePattern.AndPatternExpr(newPatterns);
			}

			protected internal override SequencePattern.PatternExpr Transform(INodePatternTransformer transformer)
			{
				IList<SequencePattern.PatternExpr> newPatterns = new List<SequencePattern.PatternExpr>(patterns.Count);
				foreach (SequencePattern.PatternExpr p in patterns)
				{
					newPatterns.Add(p.Transform(transformer));
				}
				return new SequencePattern.AndPatternExpr(newPatterns);
			}

			public override string ToString()
			{
				return StringUtils.Join(patterns, " & ");
			}
		}

		/// <summary>An accepting matching state</summary>
		protected internal static readonly SequencePattern.State MatchState = new SequencePattern.MatchState();

		/// <summary>Represents a state in the NFA corresponding to a regular expression for matching a sequence</summary>
		internal class State
		{
			/// <summary>Set of next states from this current state.</summary>
			/// <remarks>
			/// Set of next states from this current state.
			/// NOTE: Most of the time, next is just one state.
			/// </remarks>
			internal ICollection<SequencePattern.State> next;

			internal bool hasSavedValue;

			internal bool isOptional;

			protected internal State()
			{
			}

			/* ----- NFA states for matching sequences ----- */
			// Patterns are converted to the NFA states
			// Assumes the matcher will step through the NFA states one token at a time
			// is this state optional
			/// <summary>Update the set of out states by unlinked states from this state</summary>
			/// <param name="out">- Current set of out states (to be updated by this function)</param>
			protected internal virtual void UpdateOutStates(ICollection<SequencePattern.State> @out)
			{
				if (next == null)
				{
					@out.Add(this);
				}
				else
				{
					foreach (SequencePattern.State s in next)
					{
						s.UpdateOutStates(@out);
					}
				}
			}

			/// <summary>Non-consuming match.</summary>
			/// <param name="bid">- Branch id</param>
			/// <param name="matchedStates">- State of the matching so far (to be updated by the matching process)</param>
			/// <returns>true if match</returns>
			protected internal virtual bool Match0<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates)
			{
				return Match(bid, matchedStates, false);
			}

			/// <summary>Consuming match.</summary>
			/// <param name="bid">- Branch id</param>
			/// <param name="matchedStates">- State of the matching so far (to be updated by the matching process)</param>
			/// <returns>true if match</returns>
			protected internal virtual bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates)
			{
				return Match(bid, matchedStates, true);
			}

			protected internal virtual bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume)
			{
				return Match(bid, matchedStates, consume, null);
			}

			/// <summary>Given the current matched states, attempts to run NFA from this state.</summary>
			/// <remarks>
			/// Given the current matched states, attempts to run NFA from this state.
			/// If consuming:  tries to match the next element - goes through states until an element is consumed or match is false
			/// If non-consuming: does not match the next element - goes through non element consuming states
			/// In both cases, matchedStates should be updated as follows:
			/// - matchedStates should be updated with the next state to be processed.
			/// </remarks>
			/// <param name="bid">- Branch id</param>
			/// <param name="matchedStates">- State of the matching so far (to be updated by the matching process)</param>
			/// <param name="consume">- Whether to consume the next element or not</param>
			/// <returns>true if match</returns>
			protected internal virtual bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				bool match = false;
				if (next != null)
				{
					int i = 0;
					foreach (SequencePattern.State s in next)
					{
						i++;
						bool m = s.Match(matchedStates.branchStates.GetBranchId(bid, i, next.Count), matchedStates, consume, this);
						if (m)
						{
							// NOTE: We don't break because other branches may have useful state information
							match = true;
						}
					}
				}
				return match;
			}

			/// <summary>Add state to the set of next states.</summary>
			/// <param name="nextState">- state to add</param>
			protected internal virtual void Add(SequencePattern.State nextState)
			{
				if (next == null)
				{
					next = new LinkedHashSet<SequencePattern.State>();
				}
				next.Add(nextState);
			}

			public virtual object Value<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates)
			{
				if (hasSavedValue)
				{
					IHasInterval<int> matchedInterval = matchedStates.GetBranchStates().GetMatchedInterval(bid, this);
					if (matchedInterval != null && matchedInterval is ValuedInterval)
					{
						return ((ValuedInterval)matchedInterval).GetValue();
					}
				}
				return null;
			}

			public virtual void MarkOptional(bool propagate)
			{
				this.isOptional = true;
				if (propagate && next != null)
				{
					Stack<SequencePattern.State> todo = new Stack<SequencePattern.State>();
					ICollection<SequencePattern.State> seen = new HashSet<SequencePattern.State>();
					Sharpen.Collections.AddAll(todo, next);
					while (!todo.Empty())
					{
						SequencePattern.State s = todo.Pop();
						s.isOptional = true;
						seen.Add(s);
						if (next != null)
						{
							foreach (SequencePattern.State n in next)
							{
								if (!seen.Contains(n))
								{
									todo.Push(n);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Final accepting state.</summary>
		private class MatchState : SequencePattern.State
		{
			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// Always add this state back (effectively looping forever in this matching state)
				matchedStates.AddState(bid, this);
				return false;
			}
		}

		/// <summary>State with associated value.</summary>
		private class ValueState : SequencePattern.State
		{
			internal readonly object value;

			private ValueState(object value)
			{
				this.value = value;
			}

			public override object Value<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates)
			{
				return value;
			}
		}

		/// <summary>State for matching one element/node.</summary>
		private class NodePatternState : SequencePattern.State
		{
			internal readonly NodePattern pattern;

			protected internal NodePatternState(NodePattern p)
			{
				this.pattern = p;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				if (consume)
				{
					// Get element and return if it matched or not
					T node = matchedStates.Get();
					// TODO: Fix type checking
					if (matchedStates.matcher.matchWithResult)
					{
						object obj = pattern.MatchWithResult(node);
						if (obj != null)
						{
							if (obj != true)
							{
								matchedStates.branchStates.SetMatchedResult(bid, matchedStates.curPosition, obj);
							}
							// If matched, need to add next states to the queue of states to be processed
							matchedStates.AddStates(bid, next);
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						if (node != null && pattern.Match(node))
						{
							// If matched, need to add next states to the queue of states to be processed
							matchedStates.AddStates(bid, next);
							return true;
						}
						else
						{
							return false;
						}
					}
				}
				else
				{
					// Not consuming element - add this state back to queue of states to be processed
					// This state was not successfully matched
					matchedStates.AddState(bid, this);
					return false;
				}
			}
		}

		/// <summary>State for matching multiple elements/nodes.</summary>
		private class MultiNodePatternState : SequencePattern.State
		{
			private readonly MultiNodePattern pattern;

			protected internal MultiNodePatternState(MultiNodePattern p)
			{
				this.pattern = p;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				if (consume)
				{
					IHasInterval<int> matchedInterval = matchedStates.GetBranchStates().GetMatchedInterval(bid, this);
					int cur = matchedStates.curPosition;
					if (matchedInterval == null)
					{
						// Haven't tried to match this node before, try now
						// Get element and return if it matched or not
						IList<T> nodes = matchedStates.Elements();
						// TODO: Fix type checking
						ICollection<IHasInterval<int>> matched = pattern.Match(nodes, cur);
						// Order matches
						if (pattern.IsGreedyMatch())
						{
							// Sort from long to short
							matched = CollectionUtils.Sorted(matched, Interval.LengthGtComparator);
						}
						else
						{
							// Sort from short to long
							matched = CollectionUtils.Sorted(matched, Interval.LengthLtComparator);
						}
						// TODO: Check intervals are valid?   Start at cur and ends after?
						if (matched != null && matched.Count > 0)
						{
							int nBranches = matched.Count;
							int i = 0;
							foreach (IHasInterval<int> interval in matched)
							{
								i++;
								int bid2 = matchedStates.GetBranchStates().GetBranchId(bid, i, nBranches);
								matchedStates.GetBranchStates().SetMatchedInterval(bid2, this, interval);
								// If matched, need to add next states to the queue of states to be processed
								// keep in current state until end node reached
								if (interval.GetInterval().GetEnd() - 1 <= cur)
								{
									matchedStates.AddStates(bid2, next);
								}
								else
								{
									matchedStates.AddState(bid2, this);
								}
							}
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						// Previously matched this state - just need to step through until we get to end of matched interval
						if (matchedInterval.GetInterval().GetEnd() - 1 <= cur)
						{
							matchedStates.AddStates(bid, next);
						}
						else
						{
							matchedStates.AddState(bid, this);
						}
						return true;
					}
				}
				else
				{
					// Not consuming element - add this state back to queue of states to be processed
					// This state was not successfully matched
					matchedStates.AddState(bid, this);
					return false;
				}
			}
		}

		/// <summary>State that matches a pattern that can occur multiple times.</summary>
		private class RepeatState : SequencePattern.State
		{
			private readonly SequencePattern.State repeatStart;

			private readonly int minMatch;

			private readonly int maxMatch;

			private readonly bool greedyMatch;

			public RepeatState(SequencePattern.State start, int minMatch, int maxMatch, bool greedyMatch)
			{
				this.repeatStart = start;
				this.minMatch = minMatch;
				this.maxMatch = maxMatch;
				this.greedyMatch = greedyMatch;
				if (minMatch < 0)
				{
					throw new ArgumentException("Invalid minMatch=" + minMatch);
				}
				if (maxMatch >= 0 && minMatch > maxMatch)
				{
					throw new ArgumentException("Invalid minMatch=" + minMatch + ", maxMatch=" + maxMatch);
				}
				this.isOptional = this.minMatch <= 0;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// Get how many times this states has already been matched
				int matchedCount = matchedStates.GetBranchStates().EndMatchedCountInc(bid, this);
				// Get the minimum number of times we still need to match this state
				int minMatchLeft = minMatch - matchedCount;
				if (minMatchLeft < 0)
				{
					minMatchLeft = 0;
				}
				// Get the maximum number of times we can match this state
				int maxMatchLeft;
				if (maxMatch < 0)
				{
					// Indicate unlimited matching
					maxMatchLeft = maxMatch;
				}
				else
				{
					maxMatchLeft = maxMatch - matchedCount;
					if (maxMatch < 0)
					{
						// Already exceeded the maximum number of times we can match this state
						// indicate state not matched
						return false;
					}
				}
				bool match = false;
				// See how many branching options there are...
				int totalBranches = 0;
				if (minMatchLeft == 0 && next != null)
				{
					totalBranches += next.Count;
				}
				if (maxMatchLeft != 0)
				{
					totalBranches++;
				}
				int i = 0;
				// branch index
				// Check if there we have met the minimum number of matches
				// If so, go ahead and try to match next state
				//  (if we need to consume an element or end a group)
				if (minMatchLeft == 0 && next != null)
				{
					foreach (SequencePattern.State s in next)
					{
						i++;
						// Increment branch index
						// Depending on greedy match or not, different priority to branches
						int pi = (greedyMatch && maxMatchLeft != 0) ? i + 1 : i;
						int bid2 = matchedStates.GetBranchStates().GetBranchId(bid, pi, totalBranches);
						matchedStates.GetBranchStates().ClearMatchedCount(bid2, this);
						bool m = s.Match(bid2, matchedStates, consume);
						if (m)
						{
							match = true;
						}
					}
				}
				// Check if we have the option of matching more
				// (maxMatchLeft < 0 indicate unlimited, maxMatchLeft > 0 indicate we are still allowed more matches)
				if (maxMatchLeft != 0)
				{
					i++;
					// Increment branch index
					// Depending on greedy match or not, different priority to branches
					int pi = greedyMatch ? 1 : i;
					int bid2 = matchedStates.GetBranchStates().GetBranchId(bid, pi, totalBranches);
					if (consume)
					{
						// Premark many times we have matched this pattern
						matchedStates.GetBranchStates().StartMatchedCountInc(bid2, this);
						// Consuming - try to see if repeating this pattern does anything
						bool m = repeatStart.Match(bid2, matchedStates, consume);
						if (m)
						{
							match = true;
						}
						else
						{
							// Didn't match - decrement how many times we have matched this pattern
							matchedStates.GetBranchStates().StartMatchedCountDec(bid2, this);
						}
					}
					else
					{
						// Not consuming - don't do anything, just add this back to list of states to be processed
						matchedStates.AddState(bid2, this);
					}
				}
				return match;
			}
		}

		/// <summary>State for matching previously matched group.</summary>
		internal class BackRefState : SequencePattern.State
		{
			private readonly SequencePattern.INodesMatchChecker matcher;

			private readonly int captureGroupId;

			public BackRefState(SequencePattern.INodesMatchChecker matcher, int captureGroupId)
			{
				this.matcher = matcher;
				this.captureGroupId = captureGroupId;
			}

			protected internal virtual bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, BasicSequenceMatchResult.MatchedGroup matchedGroup, int matchedNodes)
			{
				T node = matchedStates.Get();
				if (matcher.Matches(node, matchedStates.Elements()[matchedGroup.matchBegin + matchedNodes]))
				{
					matchedNodes++;
					matchedStates.GetBranchStates().SetMatchStateInfo(bid, this, new Pair<BasicSequenceMatchResult.MatchedGroup, int>(matchedGroup, matchedNodes));
					int len = matchedGroup.matchEnd - matchedGroup.matchBegin;
					if (len == matchedNodes)
					{
						matchedStates.AddStates(bid, next);
					}
					else
					{
						matchedStates.AddState(bid, this);
					}
					return true;
				}
				return false;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// Try to match previous node/nodes exactly
				if (consume)
				{
					// First element is group that is matched, second is number of nodes matched so far
					Pair<BasicSequenceMatchResult.MatchedGroup, int> backRefState = (Pair<BasicSequenceMatchResult.MatchedGroup, int>)matchedStates.GetBranchStates().GetMatchStateInfo(bid, this);
					if (backRefState == null)
					{
						// Haven't tried to match this node before, try now
						// Get element and return if it matched or not
						BasicSequenceMatchResult.MatchedGroup matchedGroup = matchedStates.GetBranchStates().GetMatchedGroup(bid, captureGroupId);
						if (matchedGroup != null)
						{
							// See if the first node matches
							if (matchedGroup.matchEnd > matchedGroup.matchBegin)
							{
								bool matched = Match(bid, matchedStates, matchedGroup, 0);
								return matched;
							}
							else
							{
								// TODO: Check handling of previous nodes that are zero elements?
								return base.Match(bid, matchedStates, consume, prevState);
							}
						}
						return false;
					}
					else
					{
						BasicSequenceMatchResult.MatchedGroup matchedGroup = backRefState.First();
						int matchedNodes = backRefState.Second();
						bool matched = Match(bid, matchedStates, matchedGroup, matchedNodes);
						return matched;
					}
				}
				else
				{
					// Not consuming, just add this state back to list of states to be processed
					matchedStates.AddState(bid, this);
					return false;
				}
			}
		}

		/// <summary>State for matching the start of a group.</summary>
		internal class GroupStartState : SequencePattern.State
		{
			private readonly int captureGroupId;

			public GroupStartState(int captureGroupId, SequencePattern.State startState)
			{
				this.captureGroupId = captureGroupId;
				Add(startState);
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// We only mark start when about to consume elements
				if (consume)
				{
					// Start of group, mark start
					matchedStates.SetGroupStart(bid, captureGroupId);
					return base.Match(bid, matchedStates, consume, prevState);
				}
				else
				{
					// Not consuming, just add this state back to list of states to be processed
					matchedStates.AddState(bid, this);
					return false;
				}
			}
		}

		/// <summary>State for matching the end of a group.</summary>
		internal class GroupEndState : SequencePattern.State
		{
			private readonly int captureGroupId;

			public GroupEndState(int captureGroupId)
			{
				this.captureGroupId = captureGroupId;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// Opposite of GroupStartState
				// Mark the end of the group
				object v = (prevState != null) ? prevState.Value(bid, matchedStates) : null;
				if (consume)
				{
					// We are consuming so the curPosition isn't part of our group
					matchedStates.SetGroupEnd(bid, captureGroupId, matchedStates.curPosition - 1, v);
				}
				else
				{
					matchedStates.SetGroupEnd(bid, captureGroupId, v);
				}
				return base.Match(bid, matchedStates, consume, prevState);
			}
		}

		internal class ConjMatchStateInfo
		{
			/// <summary>The branch id when the conjunction state is entered</summary>
			private readonly int startBid;

			/// <summary>The node index when the conjunction state is entered</summary>
			private readonly int startPos;

			/// <summary>The number of child expressions making up the conjunction</summary>
			private readonly int childCount;

			/// <summary>
			/// For each child expression, we keep track of the
			/// set of branch ids that causes the child expression to
			/// be satisfied (and their corresponding node index
			/// when the expression is satisfied)
			/// </summary>
			private readonly ICollection<Pair<int, int>>[] reachableChildBids;

			private ConjMatchStateInfo(int startBid, int childCount, int startPos)
			{
				// A conjunction consists of several child expressions
				//  When the conjunction state is entered,
				//    we keep track of the branch id and the node index
				//     we are on at that time (startBid and startPos)
				this.startBid = startBid;
				this.startPos = startPos;
				this.childCount = childCount;
				this.reachableChildBids = new ISet[childCount];
			}

			private void AddChildBid(int i, int bid, int pos)
			{
				if (reachableChildBids[i] == null)
				{
					reachableChildBids[i] = new ArraySet<Pair<int, int>>();
				}
				reachableChildBids[i].Add(new Pair<int, int>(bid, pos));
			}

			private bool IsAllChildMatched()
			{
				foreach (ICollection<Pair<int, int>> v in reachableChildBids)
				{
					if (v == null || v.IsEmpty())
					{
						return false;
					}
				}
				return true;
			}

			/// <summary>
			/// Returns true if there is a feasible combination of child branch ids that
			/// causes all child expressions to be satisfied with
			/// respect to the specified child expression
			/// (assuming satisfaction with the specified branch and node index)
			/// For other child expressions to have a compatible satisfiable branch,
			/// that branch must also terminate with the same node index as this one.
			/// </summary>
			/// <param name="index">- Index of the child expression</param>
			/// <param name="bid">- Branch id that causes the indexed child to be satisfied</param>
			/// <param name="pos">- Node index that causes the indexed child to be satisfied</param>
			/// <returns>
			/// whether there is a feasible combination that causes all
			/// children to be satisfied with respect to specified child.
			/// </returns>
			private bool IsAllChildMatched(int index, int bid, int pos)
			{
				for (int i = 0; i < reachableChildBids.Length; i++)
				{
					ICollection<Pair<int, int>> v = reachableChildBids[i];
					if (v == null || v.IsEmpty())
					{
						return false;
					}
					if (i != index)
					{
						bool ok = false;
						foreach (Pair<int, int> p in v)
						{
							if (p.Second() == pos)
							{
								ok = true;
								break;
							}
						}
						if (!ok)
						{
							return false;
						}
					}
				}
				return true;
			}

			/// <summary>
			/// Returns array of child branch ids that
			/// causes all child expressions to be satisfied with
			/// respect to the specified child expression
			/// (assuming satisfaction with the specified branch and node index).
			/// </summary>
			/// <remarks>
			/// Returns array of child branch ids that
			/// causes all child expressions to be satisfied with
			/// respect to the specified child expression
			/// (assuming satisfaction with the specified branch and node index).
			/// For other child expressions to have a compatible satisfiable branch,
			/// that branch must also terminate with the same node index as this one.
			/// </remarks>
			/// <param name="index">- Index of the child expression</param>
			/// <param name="bid">- Branch id that causes the indexed child to be satisfied</param>
			/// <param name="pos">- Node index that causes the indexed child to be satisfied</param>
			/// <returns>
			/// array of child branch ids if there is a valid combination
			/// null otherwise
			/// </returns>
			private int[] GetAllChildMatchedBids(int index, int bid, int pos)
			{
				int[] matchedBids = new int[reachableChildBids.Length];
				for (int i = 0; i < reachableChildBids.Length; i++)
				{
					ICollection<Pair<int, int>> v = reachableChildBids[i];
					if (v == null || v.IsEmpty())
					{
						return null;
					}
					if (i != index)
					{
						bool ok = false;
						foreach (Pair<int, int> p in v)
						{
							if (p.Second() == pos)
							{
								ok = true;
								matchedBids[i] = p.First();
								break;
							}
						}
						if (!ok)
						{
							return null;
						}
					}
					else
					{
						matchedBids[i] = bid;
					}
				}
				return matchedBids;
			}

			protected internal virtual void UpdateKeepBids(BitSet bids)
			{
				// TODO: Is there a point when we don't need to keep these bids anymore?
				foreach (ICollection<Pair<int, int>> v in reachableChildBids)
				{
					if (v != null)
					{
						foreach (Pair<int, int> p in v)
						{
							bids.Set(p.First());
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream ois)
		{
			patternStr = (string)ois.ReadObject();
			patternExpr = (SequencePattern.PatternExpr)ois.ReadObject();
			//this.patternStr = patternStr;
			//this.patternExpr = nodeSequencePattern;
			action = (ISequenceMatchAction)ois.ReadObject();
			patternExpr = new SequencePattern.GroupPatternExpr(patternExpr, true);
			patternExpr = patternExpr.Optimize();
			this.totalGroups = patternExpr.AssignGroupIds(0);
			SequencePattern.Frag f = patternExpr.Build();
			f.Connect(MatchState);
			this.root = f.start;
			varGroupBindings = new SequencePattern.VarGroupBindings(totalGroups + 1);
			patternExpr.UpdateBindings(varGroupBindings);
		}

		/// <exception cref="System.IO.IOException"/>
		private void WriteObject(ObjectOutputStream oos)
		{
			oos.WriteObject(ToString());
			oos.WriteObject(this.GetPatternExpr());
			oos.WriteObject(this.GetAction());
		}

		/// <summary>State for matching a conjunction</summary>
		internal class ConjStartState : SequencePattern.State
		{
			private readonly int childCount;

			public ConjStartState(int childCount)
			{
				//  public void writeObject()
				// States for matching conjunctions
				// - Basic, not well tested implementation that may not work for all cases ...
				// - Can be optimized to terminate earlier if one branch of the conjunction is known not to succeed
				// - May cause lots of states to be kept (not efficient)
				// - priority should be specified for conjunction branches (there can be conflicting greedy/nongreedy patterns)
				//   (should we prioritize by order?) - currently behavior is not well defined
				// Number of children that this conjunction consists of
				this.childCount = childCount;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				matchedStates.GetBranchStates().SetMatchStateInfo(bid, this, new SequencePattern.ConjMatchStateInfo(bid, childCount, matchedStates.curPosition));
				// Start of conjunction, mark start
				bool allMatch = true;
				if (next != null)
				{
					int i = 0;
					foreach (SequencePattern.State s in next)
					{
						i++;
						bool m = s.Match(matchedStates.GetBranchStates().GetBranchId(bid, i, next.Count), matchedStates, consume);
						if (!m)
						{
							allMatch = false;
							break;
						}
					}
				}
				return allMatch;
			}
		}

		/// <summary>State for matching the end of a conjunction.</summary>
		internal class ConjEndState : SequencePattern.State
		{
			private readonly SequencePattern.ConjStartState startState;

			private readonly int childIndex;

			public ConjEndState(SequencePattern.ConjStartState startState, int childIndex)
			{
				this.startState = startState;
				this.childIndex = childIndex;
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				// Opposite of ConjStartState
				// Don't do anything when we are about to consume an element
				// Only we are done consuming, and preparing to go on to the next element
				// do we check if all branches matched
				if (consume)
				{
					return false;
				}
				else
				{
					// NOTE: There is a delayed matched here, in that we actually want to remember
					//  which of the incoming branches succeeded
					// Use the bid of the corresponding ConjAndState?
					SequencePattern.ConjMatchStateInfo stateInfo = (SequencePattern.ConjMatchStateInfo)matchedStates.GetBranchStates().GetMatchStateInfo(bid, startState);
					if (stateInfo != null)
					{
						stateInfo.AddChildBid(childIndex, bid, matchedStates.curPosition);
						int[] matchedBids = stateInfo.GetAllChildMatchedBids(childIndex, bid, matchedStates.curPosition);
						if (matchedBids != null)
						{
							matchedStates.GetBranchStates().AddBidsToCollapse(bid, matchedBids);
							return base.Match(bid, matchedStates, consume, prevState);
						}
					}
					return false;
				}
			}
		}

		/// <summary>State for matching start of sequence.</summary>
		internal class SeqStartState : SequencePattern.State
		{
			public SeqStartState()
			{
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				if (consume)
				{
					if (matchedStates.curPosition == 0)
					{
						// Okay - try next
						return base.Match(bid, matchedStates, consume, this);
					}
				}
				return false;
			}
		}

		/// <summary>State for matching end of sequence.</summary>
		internal class SeqEndState : SequencePattern.State
		{
			public SeqEndState()
			{
			}

			protected internal override bool Match<T>(int bid, SequenceMatcher.MatchedStates<T> matchedStates, bool consume, SequencePattern.State prevState)
			{
				if (!consume)
				{
					if (matchedStates.curPosition == matchedStates.Elements().Count - 1)
					{
						// Okay - try next
						return base.Match(bid, matchedStates, consume, this);
					}
				}
				return false;
			}
		}

		/// <summary>Represents a incomplete NFS with start State and a set of unlinked out states.</summary>
		private class Frag
		{
			internal SequencePattern.State start;

			internal ICollection<SequencePattern.State> @out;

			protected internal Frag()
			{
			}

			protected internal Frag(SequencePattern.State start)
			{
				//     this(new State());
				this.start = start;
				this.@out = new LinkedHashSet<SequencePattern.State>();
				start.UpdateOutStates(@out);
			}

			protected internal Frag(SequencePattern.State start, ICollection<SequencePattern.State> @out)
			{
				this.start = start;
				this.@out = @out;
			}

			protected internal virtual void Add(SequencePattern.State outState)
			{
				if (@out == null)
				{
					@out = new LinkedHashSet<SequencePattern.State>();
				}
				@out.Add(outState);
			}

			protected internal virtual void Add(ICollection<SequencePattern.State> outStates)
			{
				if (@out == null)
				{
					@out = new LinkedHashSet<SequencePattern.State>();
				}
				Sharpen.Collections.AddAll(@out, outStates);
			}

			// Connect frag f to the out states of this frag
			// the out states of this frag is updated to be the out states of f
			protected internal virtual void Connect(SequencePattern.Frag f)
			{
				foreach (SequencePattern.State s in @out)
				{
					s.Add(f.start);
				}
				@out = f.@out;
			}

			// Connect state to the out states of this frag
			// the out states of this frag is updated to be the out states of state
			protected internal virtual void Connect(SequencePattern.State state)
			{
				foreach (SequencePattern.State s in @out)
				{
					s.Add(state);
				}
				@out = new LinkedHashSet<SequencePattern.State>();
				state.UpdateOutStates(@out);
			}
			/*      if (state.next != null) {
			out.addAll(state.next);
			} else {
			out.add(state);
			} */
		}
		// end static class Frag
	}
}
