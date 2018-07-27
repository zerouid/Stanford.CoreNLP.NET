using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An individual dependency between a head and a dependent.</summary>
	/// <remarks>
	/// An individual dependency between a head and a dependent. The dependency
	/// is associated with the token indices of the lexical items.
	/// <p>
	/// A key difference between this class and UnnamedDependency is the equals()
	/// method. Equality of two UnnamedConcreteDependency objects is defined solely
	/// with respect to the indices. The surface forms are not considered. This permits
	/// a use case in which dependencies in two different parse trees have slightly different
	/// pre-processing, possibly due to pre-processing.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class UnnamedConcreteDependency : UnnamedDependency
	{
		private const long serialVersionUID = -8836949694741145222L;

		private readonly int headIndex;

		private readonly int depIndex;

		public UnnamedConcreteDependency(string regent, int regentIndex, string dependent, int dependentIndex)
			: base(regent, dependent)
		{
			headIndex = regentIndex;
			depIndex = dependentIndex;
		}

		public UnnamedConcreteDependency(ILabel regent, int regentIndex, ILabel dependent, int dependentIndex)
			: base(regent, dependent)
		{
			headIndex = regentIndex;
			depIndex = dependentIndex;
		}

		public UnnamedConcreteDependency(ILabel regent, ILabel dependent)
			: base(regent, dependent)
		{
			if (Governor() is IHasIndex)
			{
				headIndex = ((IHasIndex)Governor()).Index();
			}
			else
			{
				throw new ArgumentException("Label argument lacks IndexAnnotation.");
			}
			if (Dependent() is IHasIndex)
			{
				depIndex = ((IHasIndex)Dependent()).Index();
			}
			else
			{
				throw new ArgumentException("Label argument lacks IndexAnnotation.");
			}
		}

		public virtual int GetGovernorIndex()
		{
			return headIndex;
		}

		public virtual int GetDependentIndex()
		{
			return depIndex;
		}

		public override int GetHashCode()
		{
			return headIndex * (depIndex << 16);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (!(o is Edu.Stanford.Nlp.Trees.UnnamedConcreteDependency))
				{
					return false;
				}
			}
			Edu.Stanford.Nlp.Trees.UnnamedConcreteDependency d = (Edu.Stanford.Nlp.Trees.UnnamedConcreteDependency)o;
			return headIndex == d.headIndex && depIndex == d.depIndex;
		}

		public override string ToString()
		{
			string headWord = GetText(Governor());
			string depWord = GetText(Dependent());
			return string.Format("%s [%d] --> %s [%d]", headWord, headIndex, depWord, depIndex);
		}

		/// <summary>Provide different printing options via a String keyword.</summary>
		/// <remarks>
		/// Provide different printing options via a String keyword.
		/// The recognized options are currently "xml", and "predicate".
		/// Otherwise the default toString() is used.
		/// </remarks>
		public override string ToString(string format)
		{
			switch (format)
			{
				case "xml":
				{
					string govIdxStr = " idx=\"" + headIndex + "\"";
					string depIdxStr = " idx=\"" + depIndex + "\"";
					return "  <dep>\n    <governor" + govIdxStr + ">" + XMLUtils.EscapeXML(Governor().Value()) + "</governor>\n    <dependent" + depIdxStr + ">" + XMLUtils.EscapeXML(Dependent().Value()) + "</dependent>\n  </dep>";
				}

				case "predicate":
				{
					return "dep(" + Governor() + "," + Dependent() + ")";
				}

				default:
				{
					return ToString();
				}
			}
		}

		public override IDependencyFactory DependencyFactory()
		{
			return UnnamedConcreteDependency.DependencyFactoryHolder.df;
		}

		public static IDependencyFactory Factory()
		{
			return UnnamedConcreteDependency.DependencyFactoryHolder.df;
		}

		private class DependencyFactoryHolder
		{
			private static readonly IDependencyFactory df = new UnnamedConcreteDependency.UnnamedConcreteDependencyFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// A <code>DependencyFactory</code> acts as a factory for creating objects
		/// of class <code>Dependency</code>
		/// </summary>
		private class UnnamedConcreteDependencyFactory : IDependencyFactory
		{
			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent)
			{
				return NewDependency(regent, dependent, null);
			}

			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent, object name)
			{
				return new UnnamedConcreteDependency(regent, dependent);
			}
		}
	}
}
