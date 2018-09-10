using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Scoring of typed dependencies</summary>
	/// <author>danielcer</author>
	public class DependencyScoring
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.DependencyScoring));

		public const bool Verbose = false;

		public readonly IList<ICollection<TypedDependency>> goldDeps;

		public readonly IList<ICollection<TypedDependency>> goldDepsUnlabeled;

		public readonly bool ignorePunc;

		private static IList<ICollection<TypedDependency>> ToSets(ICollection<TypedDependency> depCollection)
		{
			ICollection<TypedDependency> depSet = Generics.NewHashSet();
			ICollection<TypedDependency> unlabeledDepSet = Generics.NewHashSet();
			foreach (TypedDependency dep in depCollection)
			{
				unlabeledDepSet.Add(new DependencyScoring.TypedDependencyStringEquality(null, dep.Gov(), dep.Dep()));
				depSet.Add(new DependencyScoring.TypedDependencyStringEquality(dep.Reln(), dep.Gov(), dep.Dep()));
			}
			IList<ICollection<TypedDependency>> l = new List<ICollection<TypedDependency>>(2);
			l.Add(depSet);
			l.Add(unlabeledDepSet);
			return l;
		}

		public DependencyScoring(IList<ICollection<TypedDependency>> goldDeps, bool ignorePunc)
		{
			this.goldDeps = new List<ICollection<TypedDependency>>(goldDeps.Count);
			this.goldDepsUnlabeled = new List<ICollection<TypedDependency>>(goldDeps.Count);
			this.ignorePunc = ignorePunc;
			foreach (ICollection<TypedDependency> depCollection in goldDeps)
			{
				IList<ICollection<TypedDependency>> sets = ToSets(depCollection);
				this.goldDepsUnlabeled.Add(sets[1]);
				this.goldDeps.Add(sets[0]);
			}
			if (ignorePunc)
			{
				RemoveHeadsAssignedToPunc(this.goldDeps);
				RemoveHeadsAssignedToPunc(this.goldDepsUnlabeled);
			}
		}

		private static void RemoveHeadsAssignedToPunc(ICollection<TypedDependency> depSet)
		{
			IList<TypedDependency> deps = new List<TypedDependency>(depSet);
			foreach (TypedDependency dep in deps)
			{
				if (LangIndependentPuncCheck(dep.Dep().Word()))
				{
					depSet.Remove(dep);
				}
			}
		}

		private static void RemoveHeadsAssignedToPunc(IList<ICollection<TypedDependency>> depSets)
		{
			foreach (ICollection<TypedDependency> depSet in depSets)
			{
				RemoveHeadsAssignedToPunc(depSet);
			}
		}

		public static bool LangIndependentPuncCheck(string token)
		{
			bool isNotWord = true;
			for (int offset = 0; offset < token.Length; )
			{
				int codepoint = token.CodePointAt(offset);
				if (char.IsLetterOrDigit(codepoint))
				{
					isNotWord = false;
				}
				offset += char.CharCount(codepoint);
			}
			return isNotWord;
		}

		public static Edu.Stanford.Nlp.Trees.DependencyScoring NewInstanceStringEquality(IList<ICollection<TypedDependency>> goldDeps, bool ignorePunc)
		{
			return new Edu.Stanford.Nlp.Trees.DependencyScoring(ConvertStringEquality(goldDeps), ignorePunc);
		}

		/// <exception cref="System.IO.IOException"/>
		public DependencyScoring(string filename, bool CoNLLX, bool ignorePunc)
			: this((CoNLLX ? ReadDepsCoNLLX(filename) : ReadDeps(filename)), ignorePunc)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public DependencyScoring(string filename)
			: this(filename, false, false)
		{
		}

		public static IList<ICollection<TypedDependency>> ConvertStringEquality(IList<ICollection<TypedDependency>> deps)
		{
			IList<ICollection<TypedDependency>> convertedDeps = new List<ICollection<TypedDependency>>();
			foreach (ICollection<TypedDependency> depSet in deps)
			{
				ICollection<TypedDependency> converted = Generics.NewHashSet();
				foreach (TypedDependency dep in depSet)
				{
					converted.Add(new DependencyScoring.TypedDependencyStringEquality(dep.Reln(), dep.Gov(), dep.Dep()));
				}
				convertedDeps.Add(converted);
			}
			return convertedDeps;
		}

		[System.Serializable]
		private class TypedDependencyStringEquality : TypedDependency
		{
			private const long serialVersionUID = 1L;

			public TypedDependencyStringEquality(GrammaticalRelation reln, IndexedWord gov, IndexedWord dep)
				: base(reln, gov, dep)
			{
			}

			public override bool Equals(object o)
			{
				// some parsers, like Relex, screw up the casing
				return o.ToString().ToLower().Equals(this.ToString().ToLower());
			}

			public override int GetHashCode()
			{
				return ToString().ToLower().GetHashCode();
			}
		}

		/// <summary>
		/// Normalize all number tokens to &lt;num&gt; in order to allow
		/// for proper scoring of MSTParser productions.
		/// </summary>
		protected internal static string NormalizeNumbers(string token)
		{
			string norm = token.ReplaceFirst("^([0-9]+)-([0-9]+)$", "<num>-$2");
			if (!norm.Equals(token))
			{
				System.Console.Error.Printf("Normalized numbers in token: %s => %s\n", token, norm);
			}
			return token;
		}

		/// <summary>Read in typed dependencies in CoNLLX format.</summary>
		/// <param name="filename"/>
		/// <exception cref="System.IO.IOException"/>
		protected internal static IList<ICollection<TypedDependency>> ReadDepsCoNLLX(string filename)
		{
			IList<GrammaticalStructure> gss = GrammaticalStructure.ReadCoNLLXGrammaticalStructureCollection(filename, new FakeShortNameToGRel(), new GraphLessGrammaticalStructureFactory());
			IList<ICollection<TypedDependency>> readDeps = new List<ICollection<TypedDependency>>(gss.Count);
			foreach (GrammaticalStructure gs in gss)
			{
				ICollection<TypedDependency> deps = gs.TypedDependencies();
				readDeps.Add(deps);
			}
			return readDeps;
		}

		/// <summary>Read in typed dependencies.</summary>
		/// <remarks>
		/// Read in typed dependencies. Warning created typed dependencies are not
		/// backed by any sort of a tree structure.
		/// </remarks>
		/// <param name="filename"/>
		/// <exception cref="System.IO.IOException"/>
		protected internal static IList<ICollection<TypedDependency>> ReadDeps(string filename)
		{
			LineNumberReader breader = new LineNumberReader(new FileReader(filename));
			IList<ICollection<TypedDependency>> readDeps = new List<ICollection<TypedDependency>>();
			ICollection<TypedDependency> deps = new List<TypedDependency>();
			for (string line = breader.ReadLine(); line != null; line = breader.ReadLine())
			{
				if (line.Equals("null(-0,-0)") || line.Equals("null(-1,-1)"))
				{
					readDeps.Add(deps);
					deps = new List<TypedDependency>();
					continue;
				}
				// relex parse error
				try
				{
					if (line.Equals(string.Empty))
					{
						if (deps.Count != 0)
						{
							//System.out.println(deps);
							readDeps.Add(deps);
							deps = new List<TypedDependency>();
						}
						continue;
					}
					int firstParen = line.IndexOf("(");
					int commaSpace = line.IndexOf(", ");
					string depName = Sharpen.Runtime.Substring(line, 0, firstParen);
					string govName = Sharpen.Runtime.Substring(line, firstParen + 1, commaSpace);
					string childName = Sharpen.Runtime.Substring(line, commaSpace + 2, line.Length - 1);
					GrammaticalRelation grel = GrammaticalRelation.ValueOf(depName);
					if (depName.StartsWith("prep_"))
					{
						string prep = Sharpen.Runtime.Substring(depName, 5);
						grel = EnglishGrammaticalRelations.GetPrep(prep);
					}
					if (depName.StartsWith("prepc_"))
					{
						string prepc = Sharpen.Runtime.Substring(depName, 6);
						grel = EnglishGrammaticalRelations.GetPrepC(prepc);
					}
					if (depName.StartsWith("conj_"))
					{
						string conj = Sharpen.Runtime.Substring(depName, 5);
						grel = EnglishGrammaticalRelations.GetConj(conj);
					}
					if (grel == null)
					{
						throw new Exception("Unknown grammatical relation '" + depName + "'");
					}
					//Word govWord = new Word(govName.substring(0, govDash));
					IndexedWord govWord = new IndexedWord();
					govWord.SetValue(NormalizeNumbers(govName));
					govWord.SetWord(govWord.Value());
					//Word childWord = new Word(childName.substring(0, childDash));
					IndexedWord childWord = new IndexedWord();
					childWord.SetValue(NormalizeNumbers(childName));
					childWord.SetWord(childWord.Value());
					TypedDependency dep = new DependencyScoring.TypedDependencyStringEquality(grel, govWord, childWord);
					deps.Add(dep);
				}
				catch (Exception e)
				{
					breader.Close();
					throw new Exception("Error on line " + breader.GetLineNumber() + ":\n\n" + e);
				}
			}
			if (deps.Count != 0)
			{
				readDeps.Add(deps);
			}
			//log.info("last: "+readDeps.get(readDeps.size()-1));
			breader.Close();
			return readDeps;
		}

		/// <summary>Score system typed dependencies</summary>
		/// <param name="system"/>
		/// <returns>
		/// a triple consisting of (labeled attachment, unlabeled attachment,
		/// label accuracy)
		/// </returns>
		public virtual DependencyScoring.Score Score(IList<ICollection<TypedDependency>> system)
		{
			int parserCnt = 0;
			int goldCnt = 0;
			int parserUnlabeledCnt = 0;
			int goldUnlabeledCnt = 0;
			int correctAttachment = 0;
			int correctUnlabeledAttachment = 0;
			int labelCnt = 0;
			int labelCorrect = 0;
			ClassicCounter<string> unlabeledErrorCounts = new ClassicCounter<string>();
			ClassicCounter<string> labeledErrorCounts = new ClassicCounter<string>();
			//System.out.println("Gold size: "+ goldDeps.size() + " System size: "+system.size());
			for (int i = 0; i < system.Count; i++)
			{
				IList<ICollection<TypedDependency>> l = ToSets(system[i]);
				if (ignorePunc)
				{
					RemoveHeadsAssignedToPunc(l[0]);
					RemoveHeadsAssignedToPunc(l[1]);
				}
				parserCnt += l[0].Count;
				goldCnt += goldDeps[i].Count;
				parserUnlabeledCnt += l[1].Count;
				goldUnlabeledCnt += goldDepsUnlabeled[i].Count;
				l[0].RetainAll(goldDeps[i]);
				l[1].RetainAll(goldDepsUnlabeled[i]);
				correctAttachment += l[0].Count;
				correctUnlabeledAttachment += l[1].Count;
				labelCnt += l[1].Count;
				labelCorrect += l[0].Count;
				//System.out.println(""+i+" Acc: "+(l.get(0).size())/(double)localCnt+" "+l.get(0).size()+"/"+localCnt);
				// identify errors
				IList<ICollection<TypedDependency>> errl = ToSets(system[i]);
				errl[0].RemoveAll(goldDeps[i]);
				errl[1].RemoveAll(goldDepsUnlabeled[i]);
				IDictionary<string, string> childCorrectWithLabel = Generics.NewHashMap();
				IDictionary<string, string> childCorrectWithOutLabel = Generics.NewHashMap();
				foreach (TypedDependency goldDep in goldDeps[i])
				{
					//System.out.print(goldDep);
					string sChild = goldDep.Dep().ToString().ReplaceFirst("-[^-]*$", string.Empty);
					string prefixLabeled = string.Empty;
					string prefixUnlabeled = string.Empty;
					if (childCorrectWithLabel.Contains(sChild))
					{
						prefixLabeled = childCorrectWithLabel[sChild] + ", ";
						prefixUnlabeled = childCorrectWithOutLabel[sChild] + ", ";
					}
					childCorrectWithLabel[sChild] = prefixLabeled + goldDep.Reln() + "(" + goldDep.Gov().ToString().ReplaceFirst("-[^-]*$", string.Empty) + ", " + sChild + ")";
					childCorrectWithOutLabel[sChild] = prefixUnlabeled + "dep(" + goldDep.Gov().ToString().ReplaceFirst("-[^-]*$", string.Empty) + ", " + sChild + ")";
				}
				foreach (TypedDependency labeledError in errl[0])
				{
					string sChild = labeledError.Dep().ToString().ReplaceFirst("-[^-]*$", string.Empty);
					string sGov = labeledError.Gov().ToString().ReplaceFirst("-[^-]*$", string.Empty);
					labeledErrorCounts.IncrementCount(labeledError.Reln().ToString() + "(" + sGov + ", " + sChild + ") <= " + childCorrectWithLabel[sChild]);
				}
				foreach (TypedDependency unlabeledError in errl[1])
				{
					string sChild = unlabeledError.Dep().ToString().ReplaceFirst("-[^-]*$", string.Empty);
					string sGov = unlabeledError.Gov().ToString().ReplaceFirst("-[^-]*$", string.Empty);
					unlabeledErrorCounts.IncrementCount("dep(" + sGov + ", " + sChild + ") <= " + childCorrectWithOutLabel[sChild]);
				}
			}
			return new DependencyScoring.Score(parserCnt, goldCnt, parserUnlabeledCnt, goldUnlabeledCnt, correctAttachment, correctUnlabeledAttachment, labelCnt, labelCorrect, labeledErrorCounts, unlabeledErrorCounts);
		}

		public class Score
		{
			internal readonly int parserCnt;

			internal readonly int goldCnt;

			internal readonly int parserUnlabeledCnt;

			internal readonly int goldUnlabeledCnt;

			internal readonly int correctAttachment;

			internal readonly int correctUnlabeledAttachment;

			internal readonly int labelCnt;

			internal readonly int labelCorrect;

			internal readonly ClassicCounter<string> unlabeledErrorCounts;

			internal readonly ClassicCounter<string> labeledErrorCounts;

			public Score(int parserCnt, int goldCnt, int parserUnlabeledCnt, int goldUnlabeledCnt, int correctAttachment, int correctUnlabeledAttachment, int labelCnt, int labelCorrect, ClassicCounter<string> labeledErrorCounts, ClassicCounter<string> unlabeledErrorCounts
				)
			{
				this.parserCnt = parserCnt;
				this.goldCnt = goldCnt;
				this.parserUnlabeledCnt = parserUnlabeledCnt;
				this.goldUnlabeledCnt = goldUnlabeledCnt;
				this.correctAttachment = correctAttachment;
				this.correctUnlabeledAttachment = correctUnlabeledAttachment;
				this.labelCnt = labelCnt;
				this.labelCorrect = labelCorrect;
				this.unlabeledErrorCounts = new ClassicCounter<string>(unlabeledErrorCounts);
				this.labeledErrorCounts = new ClassicCounter<string>(labeledErrorCounts);
			}

			public override string ToString()
			{
				return ToStringFScore(false, false);
			}

			public virtual string ToStringAttachmentScore(bool json)
			{
				if (parserCnt != goldCnt)
				{
					throw new Exception(string.Format("AttachmentScore cannot be used when count(gold deps:%d) != count(system deps:%d)", parserCnt, goldCnt));
				}
				double las = correctAttachment / (double)goldCnt;
				double uas = correctUnlabeledAttachment / (double)goldCnt;
				StringBuilder sbuild = new StringBuilder();
				if (json)
				{
					sbuild.Append("{");
					sbuild.Append(string.Format("'LAS' : %.3f, ", las));
					sbuild.Append(string.Format("'UAS' : %.3f, ", uas));
					sbuild.Append("}");
				}
				else
				{
					sbuild.Append(string.Format("|| Labeled Attachment Score   ||"));
					sbuild.Append(string.Format(" %.3f (%d/%d) ||\n", las, correctAttachment, goldCnt));
					sbuild.Append(string.Format("|| Unlabeled Attachment Score ||"));
					sbuild.Append(string.Format(" %.3f (%d/%d) ||\n", uas, correctUnlabeledAttachment, goldCnt));
				}
				return sbuild.ToString();
			}

			public virtual string ToStringFScore(bool verbose, bool json)
			{
				double lp = correctAttachment / (double)parserCnt;
				double lr = correctAttachment / (double)goldCnt;
				double lf = 2.0 * (lp * lr) / (lp + lr);
				/*sbuild.append(String.format("Labeled Attachment P: %.3f (%d/%d)\n", correctAttachment/(double)parserCnt, correctAttachment, parserCnt));
				sbuild.append(String.format("Labeled Attachment R: %.3f (%d/%d)\n", correctAttachment/(double)goldCnt, correctAttachment, goldCnt));
				*/
				double ulp = correctUnlabeledAttachment / (double)parserUnlabeledCnt;
				double ulr = correctUnlabeledAttachment / (double)goldUnlabeledCnt;
				double ulf = 2.0 * (ulp * ulr) / (ulp + ulr);
				/*
				sbuild.append(String.format("Unlabeled Attachment P: %.3f (%d/%d)\n", correctUnlabeledAttachment/(double)parserCnt, correctUnlabeledAttachment, parserCnt));
				sbuild.append(String.format("Unlabeled Attachment R: %.3f (%d/%d)\n", correctUnlabeledAttachment/(double)goldCnt, correctUnlabeledAttachment, goldCnt));
				sbuild.append(String.format("LabelAccuracy: %.3f (%d/%d)\n", labelCorrect/(double)labelCnt, labelCorrect, labelCnt));
				*/
				StringBuilder sbuild = new StringBuilder();
				if (json)
				{
					sbuild.Append("{");
					sbuild.Append(string.Format("'LF1' : %.3f, ", lf));
					sbuild.Append(string.Format("'LP' : %.3f, ", lp));
					sbuild.Append(string.Format("'LR' : %.3f, ", lr));
					sbuild.Append(string.Format("'UF1' : %.3f, ", ulf));
					sbuild.Append(string.Format("'UP' : %.3f, ", ulp));
					sbuild.Append(string.Format("'UR' : %.3f, ", ulr));
					sbuild.Append("}");
				}
				else
				{
					sbuild.Append(string.Format("|| Labeled Attachment   || F ||  P ||  R ||\n"));
					sbuild.Append(string.Format("||                      || %.3f || %.3f (%d/%d) || %.3f (%d/%d)||\n", lf, lp, correctAttachment, parserCnt, lr, correctAttachment, goldCnt));
					sbuild.Append(string.Format("|| Unlabeled Attachment || F ||  P ||  R ||\n"));
					sbuild.Append(string.Format("||                     || %.3f || %.3f (%d/%d) || %.3f (%d/%d)||\n", ulf, ulp, correctUnlabeledAttachment, parserCnt, ulr, correctUnlabeledAttachment, goldCnt));
					if (verbose)
					{
						sbuild.Append("\nLabeled Attachment Error Counts\n");
						sbuild.Append(Counters.ToSortedString(labeledErrorCounts, int.MaxValue, "\t%2$f\t%1$s", "\n"));
						sbuild.Append("\n");
						sbuild.Append("\nUnlabeled Attachment Error Counts\n");
						sbuild.Append(Counters.ToSortedString(unlabeledErrorCounts, int.MaxValue, "\t%2$f\t%1$s", "\n"));
					}
				}
				return sbuild.ToString();
			}
		}

		// end static class Score
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			bool verbose = bool.Parse(props.GetProperty("v", "False"));
			bool conllx = bool.Parse(props.GetProperty("conllx", "False"));
			bool jsonOutput = bool.Parse(props.GetProperty("jsonOutput", "False"));
			bool ignorePunc = bool.Parse(props.GetProperty("nopunc", "False"));
			string goldFilename = props.GetProperty("g");
			string systemFilename = props.GetProperty("s");
			if (goldFilename == null || systemFilename == null)
			{
				log.Info("Usage:\n\tjava ...DependencyScoring [-v True/False] [-conllx True/False] [-jsonOutput True/False] [-ignorePunc True/False] -g goldFile -s systemFile\n");
				log.Info("\nOptions:\n\t-v verbose output");
				System.Environment.Exit(-1);
			}
			DependencyScoring goldScorer = new DependencyScoring(goldFilename, conllx, ignorePunc);
			IList<ICollection<TypedDependency>> systemDeps;
			if (conllx)
			{
				systemDeps = DependencyScoring.ReadDepsCoNLLX(systemFilename);
			}
			else
			{
				systemDeps = DependencyScoring.ReadDeps(systemFilename);
			}
			DependencyScoring.Score score = goldScorer.Score(systemDeps);
			if (conllx)
			{
				System.Console.Out.WriteLine(score.ToStringAttachmentScore(jsonOutput));
			}
			else
			{
				System.Console.Out.WriteLine(score.ToStringFScore(verbose, jsonOutput));
			}
		}
	}

	internal class GraphLessGrammaticalStructureFactory : IGrammaticalStructureFromDependenciesFactory
	{
		public virtual GrammaticalStructure Build(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
		{
			return new GraphLessGrammaticalStructure(projectiveDependencies, root);
		}
	}

	[System.Serializable]
	internal class GraphLessGrammaticalStructure : GrammaticalStructure
	{
		private const long serialVersionUID = 1L;

		public GraphLessGrammaticalStructure(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
			: base(projectiveDependencies, root)
		{
		}
	}

	internal class FakeShortNameToGRel : IDictionary<string, GrammaticalRelation>
	{
		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		public virtual bool Contains(object o)
		{
			// since we generate grammatical relations dynamically, this "map" technically contains any String key
			if (o is string)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual bool ContainsValue(object o)
		{
			throw new NotSupportedException();
		}

		public virtual ICollection<KeyValuePair<string, GrammaticalRelation>> EntrySet()
		{
			throw new NotSupportedException();
		}

		public virtual GrammaticalRelation Get(object key)
		{
			if (!(key is string))
			{
				throw new NotSupportedException();
			}
			string strkey = (string)key;
			return new _GrammaticalRelation_504(Language.Any, strkey, null, GrammaticalRelation.Dependent);
		}

		private sealed class _GrammaticalRelation_504 : GrammaticalRelation
		{
			public _GrammaticalRelation_504(Language baseArg1, string baseArg2, string baseArg3, GrammaticalRelation baseArg4)
				: base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
				this.serialVersionUID = 1L;
			}

			public override bool Equals(object o)
			{
				if (o is GrammaticalRelation)
				{
					return this.GetShortName().Equals(((GrammaticalRelation)o).GetShortName());
				}
				return false;
			}

			public override int GetHashCode()
			{
				return this.GetShortName().GetHashCode();
			}
		}

		public virtual bool IsEmpty()
		{
			return false;
		}

		public virtual ICollection<string> Keys
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public virtual GrammaticalRelation Put(string key, GrammaticalRelation value)
		{
			throw new NotSupportedException();
		}

		public virtual void PutAll<_T0>(IDictionary<_T0> m)
			where _T0 : string
		{
			throw new NotSupportedException();
		}

		public virtual GrammaticalRelation Remove(object key)
		{
			throw new NotSupportedException();
		}

		public virtual int Count
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public virtual ICollection<GrammaticalRelation> Values
		{
			get
			{
				throw new NotSupportedException();
			}
		}
	}
}
