using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An individual dependency between a head and a dependent.</summary>
	/// <remarks>
	/// An individual dependency between a head and a dependent.
	/// The head and dependent are represented as a Label.
	/// For example, these can be a
	/// Word or a WordTag.  If one wishes the dependencies to preserve positions
	/// in a sentence, then each can be a NamedConstituent.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class NamedDependency : UnnamedDependency
	{
		private const long serialVersionUID = -1635646451505721133L;

		private readonly object name;

		public NamedDependency(string regent, string dependent, object name)
			: base(regent, dependent)
		{
			this.name = name;
		}

		public NamedDependency(ILabel regent, ILabel dependent, object name)
			: base(regent, dependent)
		{
			this.name = name;
		}

		public override object Name()
		{
			return name;
		}

		public override int GetHashCode()
		{
			return regentText.GetHashCode() ^ dependentText.GetHashCode() ^ name.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (!(o is Edu.Stanford.Nlp.Trees.NamedDependency))
				{
					return false;
				}
			}
			Edu.Stanford.Nlp.Trees.NamedDependency d = (Edu.Stanford.Nlp.Trees.NamedDependency)o;
			return EqualsIgnoreName(o) && name.Equals(d.name);
		}

		public override string ToString()
		{
			return string.Format("%s --%s--> %s", regentText, name.ToString(), dependentText);
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
					return "  <dep>\n    <governor>" + XMLUtils.EscapeXML(Governor().Value()) + "</governor>\n    <dependent>" + XMLUtils.EscapeXML(Dependent().Value()) + "</dependent>\n  </dep>";
				}

				case "predicate":
				{
					return "dep(" + Governor() + "," + Dependent() + "," + Name() + ")";
				}

				default:
				{
					return ToString();
				}
			}
		}

		public override IDependencyFactory DependencyFactory()
		{
			return NamedDependency.DependencyFactoryHolder.df;
		}

		public static IDependencyFactory Factory()
		{
			return NamedDependency.DependencyFactoryHolder.df;
		}

		private class DependencyFactoryHolder
		{
			private static readonly IDependencyFactory df = new NamedDependency.NamedDependencyFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// A <code>DependencyFactory</code> acts as a factory for creating objects
		/// of class <code>Dependency</code>
		/// </summary>
		private class NamedDependencyFactory : IDependencyFactory
		{
			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent)
			{
				return NewDependency(regent, dependent, null);
			}

			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent, object name)
			{
				return new NamedDependency(regent, dependent, name);
			}
		}
	}
}
