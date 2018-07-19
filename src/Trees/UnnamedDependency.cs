using System;
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
	/// in a sentence, then each can be a LabeledConstituent.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class UnnamedDependency : IDependency<ILabel, ILabel, object>
	{
		private const long serialVersionUID = -3768440215342256085L;

		protected internal readonly string regentText;

		protected internal readonly string dependentText;

		private readonly ILabel regent;

		private readonly ILabel dependent;

		public UnnamedDependency(string regent, string dependent)
		{
			// We store the text of the labels separately because it looks like
			// it is possible for an object to request a hash code using itself
			// in a partially reconstructed state when unserializing.  For
			// example, a TreeGraphNode might ask for the hash code of an
			// UnnamedDependency, which then uses an unfilled member of the same
			// TreeGraphNode to get the hash code.  Keeping the text of the
			// labels breaks that possible cycle.
			if (regent == null || dependent == null)
			{
				throw new ArgumentException("governor or dependent cannot be null");
			}
			CoreLabel headLabel = new CoreLabel();
			headLabel.SetValue(regent);
			headLabel.SetWord(regent);
			this.regent = headLabel;
			CoreLabel depLabel = new CoreLabel();
			depLabel.SetValue(dependent);
			depLabel.SetWord(dependent);
			this.dependent = depLabel;
			regentText = regent;
			dependentText = dependent;
		}

		public UnnamedDependency(ILabel regent, ILabel dependent)
		{
			if (regent == null || dependent == null)
			{
				throw new ArgumentException("governor or dependent cannot be null");
			}
			this.regent = regent;
			this.dependent = dependent;
			regentText = GetText(regent);
			dependentText = GetText(dependent);
		}

		public virtual ILabel Governor()
		{
			return regent;
		}

		public virtual ILabel Dependent()
		{
			return dependent;
		}

		public virtual object Name()
		{
			return null;
		}

		protected internal virtual string GetText(ILabel label)
		{
			if (label is IHasWord)
			{
				string word = ((IHasWord)label).Word();
				if (word != null)
				{
					return word;
				}
			}
			return label.Value();
		}

		public override int GetHashCode()
		{
			return regentText.GetHashCode() ^ dependentText.GetHashCode();
		}

		public override bool Equals(object o)
		{
			return EqualsIgnoreName(o);
		}

		public virtual bool EqualsIgnoreName(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (!(o is Edu.Stanford.Nlp.Trees.UnnamedDependency))
				{
					return false;
				}
			}
			Edu.Stanford.Nlp.Trees.UnnamedDependency d = (Edu.Stanford.Nlp.Trees.UnnamedDependency)o;
			string thisHeadWord = regentText;
			string thisDepWord = dependentText;
			string headWord = d.regentText;
			string depWord = d.dependentText;
			return thisHeadWord.Equals(headWord) && thisDepWord.Equals(depWord);
		}

		public override string ToString()
		{
			return string.Format("%s --> %s", regentText, dependentText);
		}

		/// <summary>Provide different printing options via a String keyword.</summary>
		/// <remarks>
		/// Provide different printing options via a String keyword.
		/// The recognized options are currently "xml", and "predicate".
		/// Otherwise the default toString() is used.
		/// </remarks>
		public virtual string ToString(string format)
		{
			switch (format)
			{
				case "xml":
				{
					return "  <dep>\n    <governor>" + XMLUtils.EscapeXML(Governor().Value()) + "</governor>\n    <dependent>" + XMLUtils.EscapeXML(Dependent().Value()) + "</dependent>\n  </dep>";
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

		public virtual IDependencyFactory DependencyFactory()
		{
			return UnnamedDependency.DependencyFactoryHolder.df;
		}

		public static IDependencyFactory Factory()
		{
			return UnnamedDependency.DependencyFactoryHolder.df;
		}

		private class DependencyFactoryHolder
		{
			private static readonly IDependencyFactory df = new UnnamedDependency.UnnamedDependencyFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// A <code>DependencyFactory</code> acts as a factory for creating objects
		/// of class <code>Dependency</code>
		/// </summary>
		private class UnnamedDependencyFactory : IDependencyFactory
		{
			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent)
			{
				return NewDependency(regent, dependent, null);
			}

			/// <summary>Create a new <code>Dependency</code>.</summary>
			public virtual IDependency<ILabel, ILabel, object> NewDependency(ILabel regent, ILabel dependent, object name)
			{
				return new UnnamedDependency(regent, dependent);
			}
		}
	}
}
