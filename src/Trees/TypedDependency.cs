using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>TypedDependency</code> is a relation between two words in a
	/// <code>GrammaticalStructure</code>.
	/// </summary>
	/// <remarks>
	/// A <code>TypedDependency</code> is a relation between two words in a
	/// <code>GrammaticalStructure</code>.  Each <code>TypedDependency</code>
	/// consists of a governor word, a dependent word, and a relation, which is
	/// normally an instance of
	/// <see cref="GrammaticalRelation"><code>GrammaticalRelation</code></see>
	/// .
	/// </remarks>
	/// <author>Bill MacCartney</author>
	[System.Serializable]
	public class TypedDependency : IComparable<Edu.Stanford.Nlp.Trees.TypedDependency>
	{
		private const long serialVersionUID = -7690294213151279779L;

		private GrammaticalRelation reln;

		private IndexedWord gov;

		private IndexedWord dep;

		private bool extra;

		public TypedDependency(GrammaticalRelation reln, IndexedWord gov, IndexedWord dep)
		{
			// TODO FIXME: these should all be final.  That they are mutable is
			// awful design.  Awful.  It means that underlying data structures
			// can be mutated in ways you don't intend.  For example, there was
			// a time when you could call typedDependenciesCollapsed() and it
			// would change the GrammaticalStructure because of the way that
			// object mutated its TypedDependency objects.
			// = false; // to code whether the dependency preserves the tree structure or not
			// cdm: todo: remove this field and use typing on reln?  Expand implementation of SEMANTIC_DEPENDENT
			this.reln = reln;
			this.gov = gov;
			this.dep = dep;
		}

		public TypedDependency(Edu.Stanford.Nlp.Trees.TypedDependency other)
		{
			this.reln = other.reln;
			this.gov = other.gov;
			this.dep = other.dep;
			this.extra = other.extra;
		}

		public virtual GrammaticalRelation Reln()
		{
			return reln;
		}

		public virtual void SetGov(IndexedWord gov)
		{
			this.gov = gov;
		}

		public virtual void SetDep(IndexedWord dep)
		{
			this.dep = dep;
		}

		public virtual IndexedWord Gov()
		{
			return gov;
		}

		public virtual IndexedWord Dep()
		{
			return dep;
		}

		public virtual bool Extra()
		{
			return extra;
		}

		public virtual void SetReln(GrammaticalRelation reln)
		{
			this.reln = reln;
		}

		public virtual void SetExtra()
		{
			this.extra = true;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Trees.TypedDependency))
			{
				return false;
			}
			Edu.Stanford.Nlp.Trees.TypedDependency typedDep = (Edu.Stanford.Nlp.Trees.TypedDependency)o;
			if (reln != null ? !reln.Equals(typedDep.reln) : typedDep.reln != null)
			{
				return false;
			}
			if (gov != null ? !gov.Equals(typedDep.gov) : typedDep.gov != null)
			{
				return false;
			}
			if (dep != null ? !dep.Equals(typedDep.dep) : typedDep.dep != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = (reln != null ? reln.GetHashCode() : 17);
			result = 29 * result + (gov != null ? gov.GetHashCode() : 0);
			result = 29 * result + (dep != null ? dep.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return ToString(CoreLabel.OutputFormat.ValueIndex);
		}

		public virtual string ToString(CoreLabel.OutputFormat format)
		{
			return reln + "(" + gov.ToString(format) + ", " + dep.ToString(format) + ")";
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Trees.TypedDependency tdArg)
		{
			IndexedWord depArg = tdArg.Dep();
			IndexedWord depThis = this.Dep();
			int indexArg = depArg.Index();
			int indexThis = depThis.Index();
			if (indexThis > indexArg)
			{
				return 1;
			}
			else
			{
				if (indexThis < indexArg)
				{
					return -1;
				}
			}
			// dependent indices are equal, check governor
			int govIndexArg = tdArg.Gov().Index();
			int govIndexThis = this.Gov().Index();
			if (govIndexThis > govIndexArg)
			{
				return 1;
			}
			else
			{
				if (govIndexThis < govIndexArg)
				{
					return -1;
				}
			}
			// dependent and governor indices equal, the relation decides
			return this.Reln().CompareTo(tdArg.Reln());
		}
	}
}
