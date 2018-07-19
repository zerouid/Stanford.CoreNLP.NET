using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <summary>
	/// Base class for feature factories
	/// Created by Sonal Gupta.
	/// </summary>
	public abstract class RelationFeatureFactory
	{
		public enum DEPENDENCY_TYPE
		{
			Basic,
			Collapsed,
			CollapsedCcprocessed
		}

		/// <summary>If true, it does not create any lexicalized features from the first argument (needed for KBP)</summary>
		protected internal bool doNotLexicalizeFirstArg;

		/// <summary>Which dependencies to use for feature extraction</summary>
		protected internal RelationFeatureFactory.DEPENDENCY_TYPE dependencyType;

		public abstract IDatum<string, string> CreateDatum(RelationMention rel, string label);

		public abstract IDatum<string, string> CreateDatum(RelationMention rel);

		public virtual void SetDoNotLexicalizeFirstArgument(bool doNotLexicalizeFirstArg)
		{
			this.doNotLexicalizeFirstArg = doNotLexicalizeFirstArg;
		}

		public abstract string GetFeature(RelationMention rel, string dependency_path_lowlevel);

		public abstract ICollection<string> GetFeatures(RelationMention rel, string dependency_path_words);

		/*
		* If in case, creating test datum is different.
		*/
		public abstract IDatum<string, string> CreateTestDatum(RelationMention rel, Logger logger);
	}
}
