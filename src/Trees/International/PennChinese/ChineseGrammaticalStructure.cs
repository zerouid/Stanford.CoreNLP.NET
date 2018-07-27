using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A GrammaticalStructure for Chinese.</summary>
	/// <author>Galen Andrew</author>
	/// <author>Pi-Chuan Chang</author>
	/// <author>
	/// Daniel Cer - support for printing CoNLL-X format, encoding update,
	/// and preliminary changes to make
	/// ChineseGrammaticalStructure behave more like
	/// EnglishGrammaticalStructure on the command line
	/// (ultimately, both classes should probably use the same
	/// abstract main method).
	/// </author>
	[System.Serializable]
	public class ChineseGrammaticalStructure : GrammaticalStructure
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseGrammaticalStructure));

		private static IHeadFinder shf = new ChineseSemanticHeadFinder();

		/// <summary>
		/// Construct a new <code>GrammaticalStructure</code> from an
		/// existing parse tree.
		/// </summary>
		/// <remarks>
		/// Construct a new <code>GrammaticalStructure</code> from an
		/// existing parse tree.  The new <code>GrammaticalStructure</code>
		/// has the same tree structure and label values as the given tree
		/// (but no shared storage).  As part of construction, the parse tree
		/// is analyzed using definitions from
		/// <see cref="Edu.Stanford.Nlp.Trees.GrammaticalRelation"><code>GrammaticalRelation</code></see>
		/// to populate the new
		/// <code>GrammaticalStructure</code> with as many labeled
		/// grammatical relations as it can.
		/// </remarks>
		/// <param name="t">Tree to process</param>
		public ChineseGrammaticalStructure(Tree t)
			: this(t, new ChineseTreebankLanguagePack().PunctuationWordRejectFilter())
		{
		}

		public ChineseGrammaticalStructure(Tree t, IPredicate<string> puncFilter)
			: this(t, puncFilter, shf)
		{
		}

		public ChineseGrammaticalStructure(Tree t, IHeadFinder hf)
			: this(t, null, hf)
		{
		}

		public ChineseGrammaticalStructure(Tree t, IPredicate<string> puncFilter, IHeadFinder hf)
			: base(t, ChineseGrammaticalRelations.Values(), ChineseGrammaticalRelations.ValuesLock(), null, hf, puncFilter, Filters.AcceptFilter())
		{
		}

		/// <summary>Used for postprocessing CoNLL X dependencies</summary>
		public ChineseGrammaticalStructure(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
			: base(projectiveDependencies, root)
		{
		}

		//private static HeadFinder shf = new ChineseHeadFinder();
		protected internal override void CollapseDependencies(IList<TypedDependency> list, bool CCprocess, GrammaticalStructure.Extras includeExtras)
		{
			//      collapseConj(list);
			CollapsePrepAndPoss(list);
		}

		//      collapseMultiwordPreps(list);
		private static void CollapsePrepAndPoss(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			// Construct a map from words to the set of typed
			// dependencies in which the word appears as governor.
			IDictionary<IndexedWord, ICollection<TypedDependency>> map = Generics.NewHashMap();
			foreach (TypedDependency typedDep in list)
			{
				if (!map.Contains(typedDep.Gov()))
				{
					map[typedDep.Gov()] = Generics.NewHashSet<TypedDependency>();
				}
				map[typedDep.Gov()].Add(typedDep);
			}
			//log.info("here's the map: " + map);
			foreach (TypedDependency td1 in list)
			{
				if (td1.Reln() != GrammaticalRelation.Kill)
				{
					IndexedWord td1Dep = td1.Dep();
					string td1DepPOS = td1Dep.Tag();
					// find all other typedDeps having our dep as gov
					ICollection<TypedDependency> possibles = map[td1Dep];
					if (possibles != null)
					{
						// look for the "second half"
						foreach (TypedDependency td2 in possibles)
						{
							// TreeGraphNode td2Dep = td2.dep();
							// String td2DepPOS = td2Dep.parent().value();
							if (td1.Reln() == GrammaticalRelation.Dependent && td2.Reln() == GrammaticalRelation.Dependent && td1DepPOS.Equals("P"))
							{
								GrammaticalRelation td3reln = ChineseGrammaticalRelations.ValueOf(td1Dep.Value());
								if (td3reln == null)
								{
									td3reln = GrammaticalRelation.ValueOf(Language.Chinese, td1Dep.Value());
								}
								TypedDependency td3 = new TypedDependency(td3reln, td1.Gov(), td2.Dep());
								//log.info("adding: " + td3);
								newTypedDeps.Add(td3);
								td1.SetReln(GrammaticalRelation.Kill);
								// remember these are "used up"
								td2.SetReln(GrammaticalRelation.Kill);
							}
						}
						// remember these are "used up"
						// Now we need to see if there any TDs that will be "orphaned"
						// by this collapse.  Example: if we have:
						//   dep(drew, on)
						//   dep(on, book)
						//   dep(on, right)
						// the first two will be collapsed to on(drew, book), but then
						// the third one will be orphaned, since its governor no
						// longer appears.  So, change its governor to 'drew'.
						if (td1.Reln().Equals(GrammaticalRelation.Kill))
						{
							foreach (TypedDependency td2_1 in possibles)
							{
								if (!td2_1.Reln().Equals(GrammaticalRelation.Kill))
								{
									//log.info("td1 & td2: " + td1 + " & " + td2);
									td2_1.SetGov(td1.Gov());
								}
							}
						}
					}
				}
			}
			// now copy remaining unkilled TDs from here to new
			foreach (TypedDependency td in list)
			{
				if (!td.Reln().Equals(GrammaticalRelation.Kill))
				{
					newTypedDeps.Add(td);
				}
			}
			list.Clear();
			// forget all (esp. killed) TDs
			Sharpen.Collections.AddAll(list, newTypedDeps);
		}

		public static void Main(string[] args)
		{
			Properties @params = StringUtils.ArgsToProperties(args);
			if (@params.GetProperty("sentFile") != null)
			{
				log.Error("Parsing sentences to constituency trees is not supported for Chinese. " + "Please parse your sentences first and then convert them to dependency trees using the -treeFile option.");
				return;
			}
			GrammaticalStructureConversionUtils.ConvertTrees(args, "zh-sd");
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<GrammaticalStructure> ReadCoNLLXGrammaticalStructureCollection(string fileName)
		{
			return ReadCoNLLXGrammaticalStructureCollection(fileName, ChineseGrammaticalRelations.shortNameToGRel, new ChineseGrammaticalStructure.FromDependenciesFactory());
		}

		public static Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseGrammaticalStructure BuildCoNLLXGrammaticalStructure(IList<IList<string>> tokenFields)
		{
			return (Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseGrammaticalStructure)BuildCoNLLXGrammaticalStructure(tokenFields, ChineseGrammaticalRelations.shortNameToGRel, new ChineseGrammaticalStructure.FromDependenciesFactory());
		}

		public class FromDependenciesFactory : IGrammaticalStructureFromDependenciesFactory
		{
			public virtual ChineseGrammaticalStructure Build(IList<TypedDependency> tdeps, TreeGraphNode root)
			{
				return new ChineseGrammaticalStructure(tdeps, root);
			}
		}

		private const long serialVersionUID = 8877651855167458256L;
	}
}
