using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Misc
{
	/// <summary>
	/// Parses the output of DependencyExtractor into a tree, and constructs
	/// transitive dependency closures of any set of classes.
	/// </summary>
	/// <author>Jamie Nicolson (nicolson@cs.stanford.edu)</author>
	public class DependencyAnalyzer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Misc.DependencyAnalyzer));

		/// <summary>Make true to record the dependencies as they are calculated.</summary>
		private const bool Verbose = false;

		/// <summary>Represents a package, class, method, or field in the dependency tree.</summary>
		internal class Identifier : IComparable<DependencyAnalyzer.Identifier>
		{
			public string name;

			/// <summary>The set of Identifiers that are directly dependent on this one.</summary>
			public ICollection<DependencyAnalyzer.Identifier> ingoingDependencies = Generics.NewHashSet();

			/// <summary>
			/// The set of Identifiers upon which this Identifier is directly
			/// dependent.
			/// </summary>
			public ICollection<DependencyAnalyzer.Identifier> outgoingDependencies = Generics.NewHashSet();

			/// <summary>True if this Identifier represents a class.</summary>
			/// <remarks>
			/// True if this Identifier represents a class. It might be nicer
			/// to use an enumerated type for all the types of Identifiers, but
			/// for now all we care about is whether it is a class.
			/// </remarks>
			internal bool isClass = false;

			public Identifier(string name)
			{
				this.name = name;
			}

			/// <summary>
			/// Two identifiers are equal() if and only if their fully-qualified
			/// names are the same.
			/// </summary>
			public override bool Equals(object obj)
			{
				return (obj != null) && (obj is DependencyAnalyzer.Identifier) && ((DependencyAnalyzer.Identifier)obj).name.Equals(name);
			}

			public override int GetHashCode()
			{
				return name.GetHashCode();
			}

			public virtual int CompareTo(DependencyAnalyzer.Identifier o)
			{
				return string.CompareOrdinal(name, o.name);
			}

			public override string ToString()
			{
				return name;
			}
		}

		private IDictionary<string, DependencyAnalyzer.Identifier> identifiers = Generics.NewHashMap();

		// end static class Identifier
		/// <summary>Adds the starting classes to depQueue and closure.</summary>
		/// <remarks>
		/// Adds the starting classes to depQueue and closure.
		/// Allows * as a wildcard for class names.
		/// </remarks>
		internal virtual void AddStartingClasses(LinkedList<DependencyAnalyzer.Identifier> depQueue, ICollection<DependencyAnalyzer.Identifier> closure, IList<string> startingClasses)
		{
			// build patterns out of the given class names
			// escape . and $, turn * into .* for a regular expression
			Pattern[] startingPatterns = new Pattern[startingClasses.Count];
			bool[] matched = new bool[startingClasses.Count];
			for (int i = 0; i < startingClasses.Count; ++i)
			{
				string startingClass = startingClasses[i];
				startingClass = startingClass.ReplaceAll("\\.", "\\\\\\.");
				startingClass = startingClass.ReplaceAll("\\$", "\\\\\\$");
				startingClass = startingClass.ReplaceAll("\\*", ".*");
				startingPatterns[i] = Pattern.Compile(startingClass);
				matched[i] = false;
			}
			// must iterate over every identifier, since we don't know which
			// ones will match any given expression
			foreach (DependencyAnalyzer.Identifier id in identifiers.Values)
			{
				if (!id.isClass)
				{
					continue;
				}
				for (int i_1 = 0; i_1 < startingClasses.Count; ++i_1)
				{
					if (startingPatterns[i_1].Matcher(id.name).Matches())
					{
						depQueue.AddLast(id);
						closure.Add(id);
						matched[i_1] = true;
						break;
					}
				}
			}
			for (int i_2 = 0; i_2 < startingClasses.Count; ++i_2)
			{
				if (!matched[i_2])
				{
					log.Info("Warning: pattern " + startingClasses[i_2] + " matched nothing");
				}
			}
		}

		/// <summary>
		/// Constructs the transitive closure of outgoing dependencies starting
		/// from the given classes.
		/// </summary>
		/// <remarks>
		/// Constructs the transitive closure of outgoing dependencies starting
		/// from the given classes. That is, the returned collection is all the
		/// classes that might be needed in order to use the given classes.
		/// If none of the given classes are found, an empty collection is returned.
		/// </remarks>
		/// <param name="startingClassNames">
		/// A Collection of Strings, each the
		/// fully-qualified name of a class. These are the starting elements of
		/// the transitive closure.
		/// </param>
		/// <returns>
		/// A collection of Identifiers, each representing a class,
		/// that are the transitive closure of the starting classes.
		/// </returns>
		public virtual ICollection<DependencyAnalyzer.Identifier> TransitiveClosure(IList<string> startingClassNames)
		{
			ICollection<DependencyAnalyzer.Identifier> closure = Generics.NewHashSet();
			// The depQueue is the queue of items in the closure whose dependencies
			// have yet to be scanned.
			LinkedList<DependencyAnalyzer.Identifier> depQueue = new LinkedList<DependencyAnalyzer.Identifier>();
			// add all the starting classes to the closure and the depQueue
			AddStartingClasses(depQueue, closure, startingClassNames);
			// Now work through the dependency queue, adding dependencies until
			// there are none left.
			while (!depQueue.IsEmpty())
			{
				DependencyAnalyzer.Identifier id = depQueue.RemoveFirst();
				foreach (DependencyAnalyzer.Identifier outgoingDependency in id.outgoingDependencies)
				{
					if (outgoingDependency.isClass && !closure.Contains(outgoingDependency))
					{
						depQueue.AddLast(outgoingDependency);
						closure.Add(outgoingDependency);
					}
				}
			}
			return closure;
		}

		public static readonly Pattern pkgLine = Pattern.Compile("(\\S*)(?:\\s+\\*)?\\s*");

		public static readonly Pattern classLine = Pattern.Compile("    ([^<]\\S*)(?:\\s+\\*)?\\s*");

		public static readonly Pattern memberLine = Pattern.Compile("        ([a-zA-Z_\\$]{1}.*)");

		public static readonly Pattern inDepLine = Pattern.Compile("\\s*<-- (.*)");

		public static readonly Pattern outDepLine = Pattern.Compile("\\s*--> (.*)");

		public static readonly Pattern bothDepLine = Pattern.Compile("\\s*<-> (.*)");

		//
		// These regular expressions are used to parse the raw output
		// of DependencyExtractor.
		//
		/// <summary>
		/// Takes a dependency closure generated by DependencyExtractor, and prints out the class names of exactly
		/// those classes in the closure that are in an <code>edu.stanford.nlp</code>-prepended package.
		/// </summary>
		/// <param name="args">
		/// takes one argument: the name of a file that contains the output of a run of
		/// DependencyExtractor
		/// </param>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			DependencyAnalyzer da = new DependencyAnalyzer(args[0]);
			List<string> startingClasses = new List<string>(args.Length - 1);
			for (int i = 1; i < args.Length; ++i)
			{
				startingClasses.Add(args[i]);
			}
			ICollection<DependencyAnalyzer.Identifier> closure = da.TransitiveClosure(startingClasses);
			List<DependencyAnalyzer.Identifier> sortedClosure = new List<DependencyAnalyzer.Identifier>(closure);
			sortedClosure.Sort();
			ICollection<string> alreadyOutput = Generics.NewHashSet();
			foreach (DependencyAnalyzer.Identifier identifier in sortedClosure)
			{
				string name = identifier.name;
				if (name.StartsWith("edu.stanford.nlp"))
				{
					name = name.Replace('.', '/') + ".class";
					// no need to output [] in the class names
					name = name.ReplaceAll("\\[\\]", string.Empty);
					// filter by uniqueness in case there were array classes found
					if (alreadyOutput.Contains(name))
					{
						continue;
					}
					alreadyOutput.Add(name);
					System.Console.Out.WriteLine(name);
				}
			}
		}

		public static string PrependPackage(string pkgname, string classname)
		{
			if (pkgname.Equals(string.Empty))
			{
				return classname;
			}
			else
			{
				return pkgname + "." + classname;
			}
		}

		/// <summary>Constructs a DependencyAnalyzer from the output of DependencyExtractor.</summary>
		/// <remarks>
		/// Constructs a DependencyAnalyzer from the output of DependencyExtractor.
		/// The data will be converted into a dependency tree.
		/// </remarks>
		/// <param name="filename">
		/// The path of a file containing the output of a run
		/// of DependencyExtractor.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		public DependencyAnalyzer(string filename)
		{
			BufferedReader input = new BufferedReader(new FileReader(filename));
			string line;
			DependencyAnalyzer.Identifier curPackage = null;
			DependencyAnalyzer.Identifier curClass = null;
			while ((line = input.ReadLine()) != null)
			{
				Matcher matcher = pkgLine.Matcher(line);
				string name;
				if (matcher.Matches())
				{
					name = matcher.Group(1);
					curPackage = CanonicalIdentifier(name);
					curClass = null;
				}
				else
				{
					//log.info("Found package " + curPackage.name);
					matcher = classLine.Matcher(line);
					if (matcher.Matches())
					{
						name = PrependPackage(curPackage.name, matcher.Group(1));
						curClass = CanonicalIdentifier(name);
						curClass.isClass = true;
					}
					else
					{
						//curPackage.classes.add(curClass);
						//log.info("Found class " + curClass.name);
						matcher = memberLine.Matcher(line);
						if (matcher.Matches())
						{
							name = curClass.name + "." + matcher.Group(1);
						}
						else
						{
							//log.info("Found member: " + name );
							matcher = inDepLine.Matcher(line);
							if (matcher.Matches())
							{
								name = matcher.Group(1);
								DependencyAnalyzer.Identifier inDep = CanonicalIdentifier(name);
								if (curClass != null)
								{
									curClass.ingoingDependencies.Add(inDep);
								}
							}
							else
							{
								//log.info("Found ingoing depedency: " +
								//    name);
								matcher = outDepLine.Matcher(line);
								if (matcher.Matches())
								{
									name = matcher.Group(1);
									DependencyAnalyzer.Identifier outDep = CanonicalIdentifier(name);
									if (curClass != null)
									{
										curClass.outgoingDependencies.Add(outDep);
									}
								}
								else
								{
									//log.info("Found outgoing dependency: " +
									//    name);
									matcher = bothDepLine.Matcher(line);
									if (matcher.Matches())
									{
										name = matcher.Group(1);
										DependencyAnalyzer.Identifier dep = CanonicalIdentifier(name);
										if (curClass != null)
										{
											curClass.ingoingDependencies.Add(dep);
											curClass.outgoingDependencies.Add(dep);
										}
									}
									else
									{
										log.Info("Found unmatching line: " + line);
									}
								}
							}
						}
					}
				}
			}
			// After reading the dependencies, as a post-processing step we
			// connect all inner classes and outer classes with each other.
			foreach (string className in identifiers.Keys)
			{
				DependencyAnalyzer.Identifier classId = identifiers[className];
				if (!classId.isClass)
				{
					continue;
				}
				int baseIndex = className.IndexOf("$");
				if (baseIndex < 0)
				{
					continue;
				}
				string baseName = Sharpen.Runtime.Substring(className, 0, baseIndex);
				DependencyAnalyzer.Identifier baseId = identifiers[baseName];
				if (baseId == null)
				{
					continue;
				}
				baseId.ingoingDependencies.Add(classId);
				baseId.outgoingDependencies.Add(classId);
				classId.ingoingDependencies.Add(baseId);
				classId.outgoingDependencies.Add(baseId);
			}
		}

		/// <summary>Returns the canonical Identifier with the given name.</summary>
		/// <param name="name">The name of an Identifier.</param>
		/// <returns>
		/// The Identifier, which will have been newly created if it
		/// did not already exist.
		/// </returns>
		private DependencyAnalyzer.Identifier CanonicalIdentifier(string name)
		{
			DependencyAnalyzer.Identifier ident = identifiers[name];
			if (ident == null)
			{
				ident = new DependencyAnalyzer.Identifier(name);
				identifiers[name] = ident;
			}
			return ident;
		}
	}
}
