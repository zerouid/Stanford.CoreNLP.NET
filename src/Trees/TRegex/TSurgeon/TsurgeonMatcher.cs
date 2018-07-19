using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// An object factored out to keep the state of a <code>Tsurgeon</code>
	/// operation separate from the <code>TsurgeonPattern</code> objects.
	/// </summary>
	/// <remarks>
	/// An object factored out to keep the state of a <code>Tsurgeon</code>
	/// operation separate from the <code>TsurgeonPattern</code> objects.
	/// This makes it easier to reset state between invocations and makes
	/// it easier to use in a threadsafe manner.
	/// <br />
	/// TODO: it would be nice to go through all the patterns and make sure
	/// they update <code>newNodeNames</code> or look for appropriate nodes
	/// in <code>newNodeNames</code> when possible.
	/// <br />
	/// It would also be nicer if the call to <code>matcher()</code> took
	/// the tree &amp; tregex instead of <code>evaluate()</code>, but that
	/// is a little more complicated because of the way the
	/// <code>TsurgeonMatcher</code> is used in <code>Tsurgeon</code>.
	/// Basically, you would need to move that code from
	/// <code>Tsurgeon</code> to <code>TsurgeonMatcher</code>.
	/// </remarks>
	/// <author>John Bauer</author>
	public abstract class TsurgeonMatcher
	{
		internal IDictionary<string, Tree> newNodeNames;

		internal CoindexationGenerator coindexer;

		internal Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonMatcher[] childMatcher;

		public TsurgeonMatcher(TsurgeonPattern pattern, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			// TODO: ideally we should have the tree and the tregex matcher be
			// part of this as well.  That would involve putting some of the
			// functionality in Tsurgeon.java in this object
			this.newNodeNames = newNodeNames;
			this.coindexer = coindexer;
			this.childMatcher = new Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.TsurgeonMatcher[pattern.children.Length];
			for (int i = 0; i < pattern.children.Length; ++i)
			{
				this.childMatcher[i] = pattern.children[i].Matcher(newNodeNames, coindexer);
			}
		}

		/// <summary>
		/// Evaluates the surgery pattern against a
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// and a
		/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexMatcher"/>
		/// that has been successfully matched against the tree.
		/// </summary>
		/// <param name="tree">
		/// The
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// that has been matched upon; typically this tree will be destructively modified.
		/// </param>
		/// <param name="tregex">
		/// The successfully matched
		/// <see cref="Edu.Stanford.Nlp.Trees.Tregex.TregexMatcher"/>
		/// .
		/// </param>
		/// <returns>Some node in the tree; depends on implementation and use of the specific subclass.</returns>
		public abstract Tree Evaluate(Tree tree, TregexMatcher tregex);
	}
}
