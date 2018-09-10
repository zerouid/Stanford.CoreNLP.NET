//
// StanfordCoreNLP -- a suite of NLP tools.
// Copyright (c) 2009-2017 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This is a pipeline that takes in a string and returns various analyzed
	/// linguistic forms.
	/// </summary>
	/// <remarks>
	/// This is a pipeline that takes in a string and returns various analyzed
	/// linguistic forms.
	/// The String is tokenized via a tokenizer (using a TokenizerAnnotator), and
	/// then other sequence model style annotation can be used to add things like
	/// lemmas, POS tags, and named entities.  These are returned as a list of CoreLabels.
	/// Other analysis components build and store parse trees, dependency graphs, etc.
	/// This class is designed to apply multiple Annotators
	/// to an Annotation.  The idea is that you first
	/// build up the pipeline by adding Annotators, and then
	/// you take the objects you wish to annotate and pass
	/// them in and get in return a fully annotated object.
	/// At the command-line level you can, e.g., tokenize text with StanfordCoreNLP with a command like:
	/// <br/><pre>
	/// java edu.stanford.nlp.pipeline.StanfordCoreNLP -annotators tokenize,ssplit -file document.txt
	/// </pre><br/>
	/// Please see the package level javadoc for sample usage
	/// and a more complete description.
	/// The main entry point for the API is StanfordCoreNLP.process() .
	/// <i>Implementation note:</i> There are other annotation pipelines, but they
	/// don't extend this one. Look for classes that implement Annotator and which
	/// have "Pipeline" in their name.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Anna Rafferty</author>
	/// <author>Christopher Manning</author>
	/// <author>Mihai Surdeanu</author>
	/// <author>Steven Bethard</author>
	public class StanfordCoreNLP : AnnotationPipeline
	{
		public enum OutputFormat
		{
			Text,
			Xml,
			Json,
			Conll,
			Conllu,
			Serialized,
			Custom
		}

		/// <summary>An annotator name and its associated signature.</summary>
		/// <remarks>
		/// An annotator name and its associated signature.
		/// Used in
		/// <see cref="StanfordCoreNLP.GlobalAnnotatorCache"/>
		/// .
		/// </remarks>
		public class AnnotatorSignature
		{
			public readonly string name;

			public readonly string signature;

			public AnnotatorSignature(string name, string signature)
			{
				// import static edu.stanford.nlp.util.logging.Redwood.Util.*;
				this.name = name;
				this.signature = signature;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				StanfordCoreNLP.AnnotatorSignature that = (StanfordCoreNLP.AnnotatorSignature)o;
				return Objects.Equals(name, that.name) && Objects.Equals(signature, that.signature);
			}

			public override int GetHashCode()
			{
				return Objects.Hash(name, signature);
			}

			public override string ToString()
			{
				return "AnnotatorSignature{name='" + name + "', signature='" + signature + "'}";
			}
		}

		/// <summary>A global cache of annotators, so we don't have to re-create one if there's enough memory floating around.</summary>
		public static readonly IDictionary<StanfordCoreNLP.AnnotatorSignature, Lazy<IAnnotator>> GlobalAnnotatorCache = new ConcurrentHashMap<StanfordCoreNLP.AnnotatorSignature, Lazy<IAnnotator>>();

		public const string CustomAnnotatorPrefix = "customAnnotatorClass.";

		private const string PropsSuffix = ".properties";

		public const string NewlineSplitterProperty = "ssplit.eolonly";

		public const string NewlineIsSentenceBreakProperty = "ssplit.newlineIsSentenceBreak";

		public const string DefaultNewlineIsSentenceBreak = "never";

		public static readonly string DefaultOutputFormat = IsXMLOutputPresent() ? "xml" : "text";

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(StanfordCoreNLP));

		/// <summary>Formats the constituent parse trees for display.</summary>
		private TreePrint constituentTreePrinter;

		/// <summary>Formats the dependency parse trees for human-readable display.</summary>
		private TreePrint dependencyTreePrinter;

		/// <summary>Stores the overall number of words processed.</summary>
		private int numWords;

		/// <summary>Stores the time (in milliseconds) required to construct the last pipeline.</summary>
		private long pipelineSetupTime;

		private Properties properties;

		private Semaphore availableProcessors;

		/// <summary>The annotator pool we should be using to get annotators.</summary>
		public readonly AnnotatorPool pool;

		/// <summary>Constructs a pipeline using as properties the properties file found in the classpath</summary>
		public StanfordCoreNLP()
			: this((Properties)null)
		{
		}

		/// <summary>Construct a basic pipeline.</summary>
		/// <remarks>
		/// Construct a basic pipeline. The Properties will be used to determine
		/// which annotators to create, and a default AnnotatorPool will be used
		/// to create the annotators.
		/// </remarks>
		public StanfordCoreNLP(Properties props)
			: this(props, (props == null || PropertiesUtils.GetBool(props, "enforceRequirements", true)))
		{
		}

		/// <summary>Construct a CoreNLP with a custom Annotator Pool.</summary>
		public StanfordCoreNLP(Properties props, AnnotatorPool annotatorPool)
			: this(props, (props == null || PropertiesUtils.GetBool(props, "enforceRequirements", true)), annotatorPool)
		{
		}

		public StanfordCoreNLP(Properties props, bool enforceRequirements)
			: this(props, enforceRequirements, null)
		{
		}

		public StanfordCoreNLP(Properties props, bool enforceRequirements, AnnotatorPool annotatorPool)
		{
			// end static class AnnotatorSignature
			// other constants
			// cdm [2017]: constructAnnotatorPool (PropertiesUtils.getSignature) requires non-null Properties
			if (props == null)
			{
				props = new Properties();
			}
			this.pool = annotatorPool != null ? annotatorPool : ConstructAnnotatorPool(props, GetAnnotatorImplementations());
			Construct(props, enforceRequirements, GetAnnotatorImplementations());
		}

		/// <summary>Constructs a pipeline with the properties read from this file, which must be found in the classpath</summary>
		/// <param name="propsFileNamePrefix">Filename/resource name of properties file without extension</param>
		public StanfordCoreNLP(string propsFileNamePrefix)
			: this(propsFileNamePrefix, true)
		{
		}

		public StanfordCoreNLP(string propsFileNamePrefix, bool enforceRequirements)
		{
			Properties props = LoadProperties(propsFileNamePrefix);
			if (props == null)
			{
				throw new RuntimeIOException("ERROR: cannot find properties file \"" + propsFileNamePrefix + "\" in the classpath!");
			}
			this.pool = ConstructAnnotatorPool(props, GetAnnotatorImplementations());
			Construct(props, enforceRequirements, GetAnnotatorImplementations());
		}

		//
		// @Override-able methods to change pipeline behavior
		//
		/// <summary>Get the implementation of each relevant annotator in the pipeline.</summary>
		/// <remarks>
		/// Get the implementation of each relevant annotator in the pipeline.
		/// The primary use of this method is to be overwritten by subclasses of StanfordCoreNLP
		/// to call different annotators that obey the exact same contract as the default
		/// annotator.
		/// The canonical use case for this is as an implementation of the Curator server,
		/// where the annotators make server calls rather than calling each annotator locally.
		/// </remarks>
		/// <returns>
		/// A class which specifies the actual implementation of each of the annotators called
		/// when creating the annotator pool. The canonical annotators are defaulted to in
		/// <see cref="AnnotatorImplementations"/>
		/// .
		/// </returns>
		protected internal virtual AnnotatorImplementations GetAnnotatorImplementations()
		{
			return new AnnotatorImplementations();
		}

		//
		// property-specific methods
		//
		private static string GetRequiredProperty(Properties props, string name)
		{
			string val = props.GetProperty(name);
			if (val == null)
			{
				logger.Error("Missing property \"" + name + "\"!");
				PrintRequiredProperties(System.Console.Error);
				throw new Exception("Missing property: \"" + name + '\"');
			}
			return val;
		}

		/// <summary>Finds the properties file in the classpath and loads the properties from there.</summary>
		/// <returns>The found properties object (must be not-null)</returns>
		/// <exception cref="System.Exception">If no properties file can be found on the classpath</exception>
		private static Properties LoadPropertiesFromClasspath()
		{
			IList<string> validNames = Arrays.AsList("StanfordCoreNLP", "edu.stanford.nlp.pipeline.StanfordCoreNLP");
			foreach (string name in validNames)
			{
				Properties props = LoadProperties(name);
				if (props != null)
				{
					return props;
				}
			}
			throw new Exception("ERROR: Could not find properties file in the classpath!");
		}

		private static Properties LoadProperties(string name)
		{
			return LoadProperties(name, Thread.CurrentThread().GetContextClassLoader());
		}

		private static Properties LoadProperties(string name, ClassLoader loader)
		{
			if (name.EndsWith(PropsSuffix))
			{
				name = Sharpen.Runtime.Substring(name, 0, name.Length - PropsSuffix.Length);
			}
			name = name.Replace('.', '/');
			name += PropsSuffix;
			Properties result = null;
			// Returns null on lookup failures
			InputStream @in = loader.GetResourceAsStream(name);
			try
			{
				if (@in != null)
				{
					InputStreamReader reader = new InputStreamReader(@in, "utf-8");
					result = new Properties();
					result.Load(reader);
				}
			}
			catch (IOException)
			{
				// Can throw IOException
				result = null;
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(@in);
			}
			if (result != null)
			{
				logger.Info("Searching for resource: " + name + " ... found.");
			}
			else
			{
				logger.Info("Searching for resource: " + name + " ... not found.");
			}
			return result;
		}

		/// <summary>Fetches the Properties object used to construct this Annotator</summary>
		public virtual Properties GetProperties()
		{
			return properties;
		}

		public virtual TreePrint GetConstituentTreePrinter()
		{
			return constituentTreePrinter;
		}

		public virtual TreePrint GetDependencyTreePrinter()
		{
			return dependencyTreePrinter;
		}

		public virtual double GetBeamPrintingOption()
		{
			return PropertiesUtils.GetDouble(properties, "printable.relation.beam", 0.0);
		}

		/// <summary>If true, signal for outputters to pretty-print the output.</summary>
		/// <remarks>
		/// If true, signal for outputters to pretty-print the output.
		/// If false, the outputter will try to minimize the size of the output.
		/// </remarks>
		public virtual bool GetPrettyPrint()
		{
			return PropertiesUtils.GetBool(properties, "prettyPrint", true);
		}

		public virtual string GetEncoding()
		{
			return properties.GetProperty("encoding", "UTF-8");
		}

		public virtual bool GetPrintSingletons()
		{
			return PropertiesUtils.GetBool(properties, "output.printSingletonEntities", false);
		}

		public virtual bool GetIncludeText()
		{
			return PropertiesUtils.GetBool(properties, "includeText", false);
		}

		/// <summary>
		/// Take a collection of requested annotators, and produce a list of annotators such that all of the
		/// prerequisites for each of the annotators in the input is met.
		/// </summary>
		/// <remarks>
		/// Take a collection of requested annotators, and produce a list of annotators such that all of the
		/// prerequisites for each of the annotators in the input is met.
		/// For example, if the user requests lemma, ensure that pos is also run because lemma depends on
		/// pos. As a side effect, this function orders the annotators in the proper order.
		/// Note that this is not guaranteed to return a valid set of annotators,
		/// as properties passed to the annotators can change their requirements.
		/// </remarks>
		/// <param name="annotators">The annotators the user has requested.</param>
		/// <returns>A sanitized annotators string with all prerequisites met.</returns>
		public static string EnsurePrerequisiteAnnotators(string[] annotators, Properties props)
		{
			// Get an unordered set of annotators
			ICollection<string> unorderedAnnotators = new LinkedHashSet<string>();
			// linked to preserve order
			Java.Util.Collections.AddAll(unorderedAnnotators, annotators);
			foreach (string annotator in annotators)
			{
				// Add the annotator
				if (!GetNamedAnnotators().Contains(annotator.ToLower()))
				{
					throw new ArgumentException("Unknown annotator: " + annotator);
				}
				// Add its transitive dependencies
				unorderedAnnotators.Add(annotator.ToLower());
				if (!AnnotatorConstants.DefaultRequirements.Contains(annotator.ToLower()))
				{
					throw new ArgumentException("Cannot infer requirements for annotator: " + annotator);
				}
				IQueue<string> fringe = new LinkedList<string>(AnnotatorConstants.DefaultRequirements[annotator.ToLower()]);
				int ticks = 0;
				while (!fringe.IsEmpty())
				{
					ticks += 1;
					if (ticks == 1000000)
					{
						throw new InvalidOperationException("[INTERNAL ERROR] Annotators have a circular dependency.");
					}
					string prereq = fringe.Poll();
					unorderedAnnotators.Add(prereq);
					Sharpen.Collections.AddAll(fringe, AnnotatorConstants.DefaultRequirements[prereq.ToLower()]);
				}
			}
			// Order the annotators
			IList<string> orderedAnnotators = new List<string>();
			while (!unorderedAnnotators.IsEmpty())
			{
				bool somethingAdded = false;
				// to make sure the dependencies are satisfiable
				// Loop over candidate annotators to add
				IEnumerator<string> iter = unorderedAnnotators.GetEnumerator();
				while (iter.MoveNext())
				{
					string candidate = iter.Current;
					// Are the requirements satisfied?
					bool canAdd = true;
					foreach (string prereq in AnnotatorConstants.DefaultRequirements[candidate.ToLower()])
					{
						if (!orderedAnnotators.Contains(prereq))
						{
							canAdd = false;
							break;
						}
					}
					// If so, add the annotator
					if (canAdd)
					{
						orderedAnnotators.Add(candidate);
						iter.Remove();
						somethingAdded = true;
					}
				}
				// Make sure we're making progress every iteration, to prevent an infinite loop
				if (!somethingAdded)
				{
					throw new ArgumentException("Unsatisfiable annotator list: " + StringUtils.Join(annotators, ","));
				}
			}
			// Remove depparse + parse -- these are redundant
			if (orderedAnnotators.Contains(AnnotatorConstants.StanfordParse) && !ArrayUtils.Contains(annotators, AnnotatorConstants.StanfordDependencies))
			{
				orderedAnnotators.Remove(AnnotatorConstants.StanfordDependencies);
			}
			// Tweak the properties, if necessary
			// (set the mention annotator to use dependency trees, if appropriate)
			if ((orderedAnnotators.Contains(AnnotatorConstants.StanfordCorefMention) || orderedAnnotators.Contains(AnnotatorConstants.StanfordCoref)) && !orderedAnnotators.Contains(AnnotatorConstants.StanfordParse) && !props.Contains("coref.md.type"))
			{
				props.SetProperty("coref.md.type", "dep");
			}
			// (ensure regexner is after ner)
			if (orderedAnnotators.Contains(AnnotatorConstants.StanfordNer) && orderedAnnotators.Contains(AnnotatorConstants.StanfordRegexner))
			{
				orderedAnnotators.Remove(AnnotatorConstants.StanfordRegexner);
				int nerIndex = orderedAnnotators.IndexOf(AnnotatorConstants.StanfordNer);
				orderedAnnotators.Add(nerIndex + 1, AnnotatorConstants.StanfordRegexner);
			}
			// (ensure coref is before openie)
			if (orderedAnnotators.Contains(AnnotatorConstants.StanfordCoref) && orderedAnnotators.Contains(AnnotatorConstants.StanfordOpenie))
			{
				int maxIndex = Math.Max(orderedAnnotators.IndexOf(AnnotatorConstants.StanfordOpenie), orderedAnnotators.IndexOf(AnnotatorConstants.StanfordCoref));
				if (Objects.Equals(orderedAnnotators[maxIndex], AnnotatorConstants.StanfordOpenie))
				{
					orderedAnnotators.Add(maxIndex, AnnotatorConstants.StanfordCoref);
					orderedAnnotators.Remove(AnnotatorConstants.StanfordCoref);
				}
				else
				{
					orderedAnnotators.Add(maxIndex + 1, AnnotatorConstants.StanfordOpenie);
					orderedAnnotators.Remove(AnnotatorConstants.StanfordOpenie);
				}
			}
			// Return
			return StringUtils.Join(orderedAnnotators, ",");
		}

		/// <summary>Check if we can construct an XML outputter.</summary>
		/// <returns>Whether we can construct an XML outputter.</returns>
		private static bool IsXMLOutputPresent()
		{
			try
			{
				Sharpen.Runtime.GetType("edu.stanford.nlp.pipeline.XMLOutputter");
			}
			catch
			{
				return false;
			}
			return true;
		}

		//
		// AnnotatorPool construction support
		//
		private void Construct(Properties props, bool enforceRequirements, AnnotatorImplementations annotatorImplementations)
		{
			Timing tim = new Timing();
			this.numWords = 0;
			this.constituentTreePrinter = new TreePrint("penn");
			this.dependencyTreePrinter = new TreePrint("typedDependenciesCollapsed");
			if (props == null)
			{
				// if undefined, find the properties file in the classpath
				props = LoadPropertiesFromClasspath();
			}
			else
			{
				if (props.GetProperty("annotators") == null)
				{
					// this happens when some command line options are specified (e.g just "-filelist") but no properties file is.
					// we use the options that are given and let them override the default properties from the class path properties.
					Properties fromClassPath = LoadPropertiesFromClasspath();
					fromClassPath.PutAll(props);
					props = fromClassPath;
				}
			}
			this.properties = props;
			// Set threading
			if (this.properties.Contains("threads"))
			{
				ArgumentParser.threads = PropertiesUtils.GetInt(this.properties, "threads");
				this.availableProcessors = new Semaphore(ArgumentParser.threads);
			}
			else
			{
				this.availableProcessors = new Semaphore(1);
			}
			// now construct the annotators from the given properties in the given order
			IList<string> annoNames = Arrays.AsList(GetRequiredProperty(props, "annotators").Split("[, \t]+"));
			ICollection<string> alreadyAddedAnnoNames = Generics.NewHashSet();
			ICollection<Type> requirementsSatisfied = Generics.NewHashSet();
			foreach (string name in annoNames)
			{
				name = name.Trim();
				if (name.IsEmpty())
				{
					continue;
				}
				logger.Info("Adding annotator " + name);
				IAnnotator an = pool.Get(name);
				this.AddAnnotator(an);
				if (enforceRequirements)
				{
					ICollection<Type> allRequirements = an.Requires();
					foreach (Type requirement in allRequirements)
					{
						if (!requirementsSatisfied.Contains(requirement))
						{
							string fmt = "annotator \"%s\" requires annotation \"%s\". The usual requirements for this annotator are: %s";
							throw new ArgumentException(string.Format(fmt, name, requirement.GetSimpleName(), StringUtils.Join(AnnotatorConstants.DefaultRequirements.GetOrDefault(name, Java.Util.Collections.Singleton("unknown")), ",")));
						}
					}
					Sharpen.Collections.AddAll(requirementsSatisfied, an.RequirementsSatisfied());
				}
				alreadyAddedAnnoNames.Add(name);
			}
			// Sanity check
			if (!alreadyAddedAnnoNames.Contains(AnnotatorConstants.StanfordSsplit))
			{
				Runtime.SetProperty(NewlineSplitterProperty, "false");
			}
			this.pipelineSetupTime = tim.Report();
		}

		/// <summary>
		/// Call this if you are no longer using StanfordCoreNLP and want to
		/// release the memory associated with the annotators.
		/// </summary>
		public static void ClearAnnotatorPool()
		{
			lock (typeof(StanfordCoreNLP))
			{
				logger.Warn("Clearing CoreNLP annotation pool; this should be unnecessary in production");
				GlobalAnnotatorCache.Clear();
			}
		}

		/// <summary>
		/// This function defines the list of named annotators in CoreNLP, along with how to construct
		/// them.
		/// </summary>
		/// <returns>A map from annotator name, to the function which constructs that annotator.</returns>
		private static IDictionary<string, IBiFunction<Properties, AnnotatorImplementations, IAnnotator>> GetNamedAnnotators()
		{
			IDictionary<string, IBiFunction<Properties, AnnotatorImplementations, IAnnotator>> pool = new Dictionary<string, IBiFunction<Properties, AnnotatorImplementations, IAnnotator>>();
			pool[AnnotatorConstants.StanfordTokenize] = null;
			pool[AnnotatorConstants.StanfordCleanXml] = null;
			pool[AnnotatorConstants.StanfordSsplit] = null;
			pool[AnnotatorConstants.StanfordPos] = null;
			pool[AnnotatorConstants.StanfordLemma] = null;
			pool[AnnotatorConstants.StanfordNer] = null;
			pool[AnnotatorConstants.StanfordTokensregex] = null;
			pool[AnnotatorConstants.StanfordRegexner] = null;
			pool[AnnotatorConstants.StanfordEntityMentions] = null;
			pool[AnnotatorConstants.StanfordGender] = null;
			pool[AnnotatorConstants.StanfordTruecase] = null;
			pool[AnnotatorConstants.StanfordParse] = null;
			pool[AnnotatorConstants.StanfordCorefMention] = null;
			pool[AnnotatorConstants.StanfordDeterministicCoref] = null;
			pool[AnnotatorConstants.StanfordCoref] = null;
			pool[AnnotatorConstants.StanfordRelation] = null;
			pool[AnnotatorConstants.StanfordSentiment] = null;
			pool[AnnotatorConstants.StanfordColumnDataClassifier] = null;
			pool[AnnotatorConstants.StanfordDependencies] = null;
			pool[AnnotatorConstants.StanfordNatlog] = null;
			pool[AnnotatorConstants.StanfordOpenie] = null;
			pool[AnnotatorConstants.StanfordQuote] = null;
			pool[AnnotatorConstants.StanfordQuoteAttribution] = null;
			pool[AnnotatorConstants.StanfordUdFeatures] = null;
			pool[AnnotatorConstants.StanfordLink] = null;
			pool[AnnotatorConstants.StanfordKbp] = null;
			return pool;
		}

		/// <summary>
		/// Construct the default annotator pool, and save it as the static annotator pool
		/// for CoreNLP.
		/// </summary>
		/// <seealso cref="ConstructAnnotatorPool(Java.Util.Properties, AnnotatorImplementations)"/>
		public static AnnotatorPool GetDefaultAnnotatorPool(Properties inputProps, AnnotatorImplementations annotatorImplementation)
		{
			lock (typeof(StanfordCoreNLP))
			{
				// if the pool already exists reuse!
				AnnotatorPool pool = AnnotatorPool.Singleton;
				foreach (KeyValuePair<string, IBiFunction<Properties, AnnotatorImplementations, IAnnotator>> entry in GetNamedAnnotators())
				{
					StanfordCoreNLP.AnnotatorSignature key = new StanfordCoreNLP.AnnotatorSignature(entry.Key, PropertiesUtils.GetSignature(entry.Key, inputProps));
					pool.Register(entry.Key, inputProps, GlobalAnnotatorCache.ComputeIfAbsent(key, null));
				}
				RegisterCustomAnnotators(pool, annotatorImplementation, inputProps);
				return pool;
			}
		}

		/// <summary>register any custom annotators defined in the input properties, and add them to the pool.</summary>
		/// <param name="pool">The annotator pool to add the new custom annotators to.</param>
		/// <param name="annotatorImplementation">The implementation thunk to use to create any new annotators.</param>
		/// <param name="inputProps">The properties to read new annotator definitions from.</param>
		private static void RegisterCustomAnnotators(AnnotatorPool pool, AnnotatorImplementations annotatorImplementation, Properties inputProps)
		{
			// add annotators loaded via reflection from class names specified
			// in the properties
			foreach (string property in inputProps.StringPropertyNames())
			{
				if (property.StartsWith(CustomAnnotatorPrefix))
				{
					string customName = Sharpen.Runtime.Substring(property, CustomAnnotatorPrefix.Length);
					string customClassName = inputProps.GetProperty(property);
					logger.Info("Registering annotator " + customName + " with class " + customClassName);
					StanfordCoreNLP.AnnotatorSignature key = new StanfordCoreNLP.AnnotatorSignature(customName, PropertiesUtils.GetSignature(customName, inputProps));
					pool.Register(customName, inputProps, GlobalAnnotatorCache.ComputeIfAbsent(key, null));
				}
			}
		}

		/// <summary>
		/// Construct the default annotator pool from the passed in properties, and overwriting annotators which have changed
		/// since the last call.
		/// </summary>
		/// <param name="inputProps"/>
		/// <param name="annotatorImplementation"/>
		/// <returns>A populated AnnotatorPool</returns>
		private static AnnotatorPool ConstructAnnotatorPool(Properties inputProps, AnnotatorImplementations annotatorImplementation)
		{
			AnnotatorPool pool = new AnnotatorPool();
			foreach (KeyValuePair<string, IBiFunction<Properties, AnnotatorImplementations, IAnnotator>> entry in GetNamedAnnotators())
			{
				StanfordCoreNLP.AnnotatorSignature key = new StanfordCoreNLP.AnnotatorSignature(entry.Key, PropertiesUtils.GetSignature(entry.Key, inputProps));
				pool.Register(entry.Key, inputProps, GlobalAnnotatorCache.ComputeIfAbsent(key, null));
			}
			RegisterCustomAnnotators(pool, annotatorImplementation, inputProps);
			return pool;
		}

		public static IAnnotator GetExistingAnnotator(string name)
		{
			lock (typeof(StanfordCoreNLP))
			{
				Optional<IAnnotator> annotator = GlobalAnnotatorCache.Stream().Filter(null).Map(null).Filter(null).Map(null).FindFirst();
				if (annotator.IsPresent())
				{
					return annotator.Get();
				}
				else
				{
					logger.Error("Attempted to fetch annotator \"" + name + "\" but the annotator pool does not store any such type!");
					return null;
				}
			}
		}

		/// <summary>annotate the CoreDocument wrapper</summary>
		public virtual void Annotate(CoreDocument document)
		{
			// annotate the underlying Annotation
			this.Annotate(document.annotationDocument);
			// wrap the sentences and entity mentions post annotation
			document.WrapAnnotations();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void Annotate(Annotation annotation)
		{
			base.Annotate(annotation);
			IList<CoreLabel> words = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (words != null)
			{
				numWords += words.Count;
			}
		}

		public virtual void Annotate(Annotation annotation, IConsumer<Annotation> callback)
		{
			if (PropertiesUtils.GetInt(properties, "threads", 1) == 1)
			{
				Annotate(annotation);
				callback.Accept(annotation);
			}
			else
			{
				try
				{
					availableProcessors.Acquire();
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
				new Thread(null).Start();
			}
		}

		/// <summary>
		/// Determines whether the parser annotator should default to
		/// producing binary trees.
		/// </summary>
		/// <remarks>
		/// Determines whether the parser annotator should default to
		/// producing binary trees.  Currently there is only one condition
		/// under which this is true: the sentiment annotator is used.
		/// </remarks>
		public static bool UsesBinaryTrees(Properties props)
		{
			ICollection<string> annoNames = Generics.NewHashSet(Arrays.AsList(props.GetProperty("annotators", string.Empty).Split("[, \t]+")));
			return annoNames.Contains(AnnotatorConstants.StanfordSentiment);
		}

		/// <summary>Runs the entire pipeline on the content of the given text passed in.</summary>
		/// <param name="text">The text to process</param>
		/// <returns>An Annotation object containing the output of all annotators</returns>
		public virtual Annotation Process(string text)
		{
			Annotation annotation = new Annotation(text);
			Annotate(annotation);
			return annotation;
		}

		//
		// output and formatting methods (including XML-specific methods)
		//
		/// <summary>Displays the output of all annotators in a format easily readable by people.</summary>
		/// <param name="annotation">Contains the output of all annotators</param>
		/// <param name="os">The output stream</param>
		public virtual void PrettyPrint(Annotation annotation, OutputStream os)
		{
			TextOutputter.PrettyPrint(annotation, os, this);
		}

		/// <summary>Displays the output of all annotators in a format easily readable by people.</summary>
		/// <param name="annotation">Contains the output of all annotators</param>
		/// <param name="os">The output stream</param>
		public virtual void PrettyPrint(Annotation annotation, PrintWriter os)
		{
			TextOutputter.PrettyPrint(annotation, os, this);
		}

		/// <summary>Wrapper around xmlPrint(Annotation, OutputStream).</summary>
		/// <remarks>
		/// Wrapper around xmlPrint(Annotation, OutputStream).
		/// Added for backward compatibility.
		/// </remarks>
		/// <param name="annotation">The Annotation to print</param>
		/// <param name="w">The Writer to send the output to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void XmlPrint(Annotation annotation, TextWriter w)
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			XmlPrint(annotation, os);
			// this builds it as the encoding specified in the properties
			w.Write(Sharpen.Runtime.GetStringForBytes(os.ToByteArray(), GetEncoding()));
			w.Flush();
		}

		/// <summary>Displays the output of all annotators in JSON format.</summary>
		/// <param name="annotation">Contains the output of all annotators</param>
		/// <param name="w">The Writer to send the output to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void JsonPrint(Annotation annotation, TextWriter w)
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			JSONOutputter.JsonPrint(annotation, os, this);
			w.Write(Sharpen.Runtime.GetStringForBytes(os.ToByteArray(), GetEncoding()));
			w.Flush();
		}

		/// <summary>Displays the output of many annotators in CoNLL format.</summary>
		/// <remarks>
		/// Displays the output of many annotators in CoNLL format.
		/// (Only used by CoreNLPServelet.)
		/// </remarks>
		/// <param name="annotation">Contains the output of all annotators</param>
		/// <param name="w">The Writer to send the output to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ConllPrint(Annotation annotation, TextWriter w)
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			CoNLLOutputter.ConllPrint(annotation, os, this);
			w.Write(Sharpen.Runtime.GetStringForBytes(os.ToByteArray(), GetEncoding()));
			w.Flush();
		}

		/// <summary>Displays the output of all annotators in XML format.</summary>
		/// <param name="annotation">Contains the output of all annotators</param>
		/// <param name="os">The output stream</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void XmlPrint(Annotation annotation, OutputStream os)
		{
			try
			{
				Type clazz = Sharpen.Runtime.GetType("edu.stanford.nlp.pipeline.XMLOutputter");
				MethodInfo method = clazz.GetMethod("xmlPrint", typeof(Annotation), typeof(OutputStream), typeof(StanfordCoreNLP));
				method.Invoke(null, annotation, os, this);
			}
			catch (ReflectiveOperationException e)
			{
				throw new Exception(e);
			}
		}

		//
		// runtime, shell-specific, and help menu methods
		//
		/// <summary>Prints the list of properties required to run the pipeline</summary>
		/// <param name="os">PrintStream to print usage to</param>
		/// <param name="helpTopic">a topic to print help about (or null for general options)</param>
		protected internal static void PrintHelp(TextWriter os, string helpTopic)
		{
			if (helpTopic.ToLower().StartsWith("pars"))
			{
				os.WriteLine("StanfordCoreNLP currently supports the following parsers:");
				os.WriteLine("\tstanford - Stanford lexicalized parser (default)");
				os.WriteLine("\tcharniak - Charniak and Johnson reranking parser (sold separately)");
				os.WriteLine();
				os.WriteLine("General options: (all parsers)");
				os.WriteLine("\tparse.type - selects the parser to use");
				os.WriteLine("\tparse.model - path to model file for parser");
				os.WriteLine("\tparse.maxlen - maximum sentence length");
				os.WriteLine();
				os.WriteLine("Stanford Parser-specific options:");
				os.WriteLine("(In general, you shouldn't need to set this flags)");
				os.WriteLine("\tparse.flags - extra flags to the parser (default: -retainTmpSubcategories)");
				os.WriteLine("\tparse.debug - set to true to make the parser slightly more verbose");
				os.WriteLine();
				os.WriteLine("Charniak and Johnson parser-specific options:");
				os.WriteLine("\tparse.executable - path to the parseIt binary or parse.sh script");
			}
			else
			{
				// argsToProperties will set the value of a -h or -help to "true" if no arguments are given
				if (!Sharpen.Runtime.EqualsIgnoreCase(helpTopic, "true"))
				{
					os.WriteLine("Unknown help topic: " + helpTopic);
					os.WriteLine("See -help for a list of all help topics.");
				}
				else
				{
					PrintRequiredProperties(os);
				}
			}
		}

		/// <summary>Prints the list of properties required to run the pipeline</summary>
		/// <param name="os">PrintStream to print usage to</param>
		private static void PrintRequiredProperties(TextWriter os)
		{
			// TODO some annotators (ssplit, regexner, gender, some parser options, dcoref?) are not documented
			os.WriteLine("The following properties can be defined:");
			os.WriteLine("(if -props or -annotators is not passed in, default properties will be loaded via the classpath)");
			os.WriteLine("\t\"props\" - path to file with configuration properties");
			os.WriteLine("\t\"annotators\" - comma separated list of annotators");
			os.WriteLine("\tThe following annotators are supported: cleanxml, tokenize, quote, ssplit, pos, lemma, ner, truecase, parse, hcoref, relation");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"tokenize\" is defined:");
			os.WriteLine("\t\"tokenize.options\" - PTBTokenizer options (see edu.stanford.nlp.process.PTBTokenizer for details)");
			os.WriteLine("\t\"tokenize.whitespace\" - If true, just use whitespace tokenization");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"cleanxml\" is defined:");
			os.WriteLine("\t\"clean.xmltags\" - regex of tags to extract text from");
			os.WriteLine("\t\"clean.sentenceendingtags\" - regex of tags which mark sentence endings");
			os.WriteLine("\t\"clean.allowflawedxml\" - if set to true, don't complain about XML errors");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"pos\" is defined:");
			os.WriteLine("\t\"pos.maxlen\" - maximum length of sentence to POS tag");
			os.WriteLine("\t\"pos.model\" - path towards the POS tagger model");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"ner\" is defined:");
			os.WriteLine("\t\"ner.model\" - paths for the ner models.  By default, the English 3 class, 7 class, and 4 class models are used.");
			os.WriteLine("\t\"ner.useSUTime\" - Whether or not to use sutime (English specific)");
			os.WriteLine("\t\"ner.applyNumericClassifiers\" - whether or not to use any numeric classifiers (English specific)");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"truecase\" is defined:");
			os.WriteLine("\t\"truecase.model\" - path towards the true-casing model; default: " + DefaultPaths.DefaultTruecaseModel);
			os.WriteLine("\t\"truecase.bias\" - class bias of the true case model; default: " + TrueCaseAnnotator.DefaultModelBias);
			os.WriteLine("\t\"truecase.mixedcasefile\" - path towards the mixed case file; default: " + DefaultPaths.DefaultTruecaseDisambiguationList);
			os.WriteLine();
			os.WriteLine("\tIf annotator \"relation\" is defined:");
			os.WriteLine("\t\"sup.relation.verbose\" - whether verbose or not");
			os.WriteLine("\t\"sup.relation.model\" - path towards the relation extraction model");
			os.WriteLine();
			os.WriteLine("\tIf annotator \"parse\" is defined:");
			os.WriteLine("\t\"parse.model\" - path towards the PCFG parser model");
			/* XXX: unstable, do not use for now
			os.println();
			os.println("\tIf annotator \"srl\" is defined:");
			os.println("\t\"srl.verb.args\" - path to the file listing verbs and their core arguments (\"verbs.core_args\")");
			os.println("\t\"srl.model.id\" - path prefix for the role identification model (adds \".model.gz\" and \".fe\" to this prefix)");
			os.println("\t\"srl.model.cls\" - path prefix for the role classification model (adds \".model.gz\" and \".fe\" to this prefix)");
			os.println("\t\"srl.model.jic\" - path to the directory containing the joint model's \"model.gz\", \"fe\" and \"je\" files");
			os.println("\t                  (if not specified, the joint model will not be used)");
			*/
			os.WriteLine();
			os.WriteLine("Command line properties:");
			os.WriteLine("\t\"file\" - run the pipeline on the content of this file, or on the content of the files in this directory");
			os.WriteLine("\t         XML output is generated for every input file \"file\" as file.xml");
			os.WriteLine("\t\"extension\" - if -file used with a directory, process only the files with this extension");
			os.WriteLine("\t\"filelist\" - run the pipeline on the list of files given in this file");
			os.WriteLine("\t             output is generated for every input file as file.outputExtension");
			os.WriteLine("\t\"outputDirectory\" - where to put output (defaults to the current directory)");
			os.WriteLine("\t\"outputExtension\" - extension to use for the output file (defaults to \".xml\" for XML, \".ser.gz\" for serialized).  Don't forget the dot!");
			os.WriteLine("\t\"outputFormat\" - \"xml\" (usual default), \"text\" (default for REPL or if no XML), \"json\", \"conll\", \"conllu\", \"serialized\", or \"custom\"");
			os.WriteLine("\t\"customOutputter\" - specify a class to a custom outputter instead of a pre-defined output format");
			os.WriteLine("\t\"serializer\" - Class of annotation serializer to use when outputFormat is \"serialized\".  By default, uses ProtobufAnnotationSerializer.");
			os.WriteLine("\t\"replaceExtension\" - flag to chop off the last extension before adding outputExtension to file");
			os.WriteLine("\t\"noClobber\" - don't automatically override (clobber) output files that already exist");
			os.WriteLine("\t\"threads\" - multithread on this number of threads");
			os.WriteLine();
			os.WriteLine("If none of the above are present, run the pipeline in an interactive shell (default properties will be loaded from the classpath).");
			os.WriteLine("The shell accepts input from stdin and displays the output at stdout.");
			os.WriteLine();
			os.WriteLine("Run with -help [topic] for more help on a specific topic.");
			os.WriteLine("Current topics include: parser");
			os.WriteLine();
		}

		/// <summary><inheritDoc/></summary>
		public override string TimingInformation()
		{
			StringBuilder sb = new StringBuilder(base.TimingInformation());
			if (Time && numWords >= 0)
			{
				long total = this.GetTotalTime();
				sb.Append(" for ").Append(this.numWords).Append(" tokens at ");
				sb.Append(string.Format("%.1f", numWords / (((double)total) / 1000)));
				sb.Append(" tokens/sec.");
			}
			return sb.ToString();
		}

		/// <summary>Runs an interactive shell where input text is processed with the given pipeline.</summary>
		/// <param name="pipeline">The pipeline to be used</param>
		/// <exception cref="System.IO.IOException">If IO problem with stdin</exception>
		private static void Shell(StanfordCoreNLP pipeline)
		{
			string encoding = pipeline.GetEncoding();
			BufferedReader r = new BufferedReader(IOUtils.EncodedInputStreamReader(Runtime.@in, encoding));
			System.Console.Error.WriteLine("Entering interactive shell. Type q RETURN or EOF to quit.");
			StanfordCoreNLP.OutputFormat outputFormat = StanfordCoreNLP.OutputFormat.ValueOf(pipeline.properties.GetProperty("outputFormat", "text").ToUpper());
			while (true)
			{
				System.Console.Error.Write("NLP> ");
				string line = r.ReadLine();
				if (line == null || Sharpen.Runtime.EqualsIgnoreCase(line, "q"))
				{
					break;
				}
				if (!line.IsEmpty())
				{
					Annotation anno = pipeline.Process(line);
					switch (outputFormat)
					{
						case StanfordCoreNLP.OutputFormat.Xml:
						{
							pipeline.XmlPrint(anno, System.Console.Out);
							break;
						}

						case StanfordCoreNLP.OutputFormat.Json:
						{
							new JSONOutputter().Print(anno, System.Console.Out, pipeline);
							System.Console.Out.WriteLine();
							break;
						}

						case StanfordCoreNLP.OutputFormat.Conll:
						{
							new CoNLLOutputter().Print(anno, System.Console.Out, pipeline);
							System.Console.Out.WriteLine();
							break;
						}

						case StanfordCoreNLP.OutputFormat.Conllu:
						{
							new CoNLLUOutputter().Print(anno, System.Console.Out, pipeline);
							break;
						}

						case StanfordCoreNLP.OutputFormat.Text:
						{
							pipeline.PrettyPrint(anno, System.Console.Out);
							break;
						}

						case StanfordCoreNLP.OutputFormat.Custom:
						{
							AnnotationOutputter outputter = ReflectionLoading.LoadByReflection(pipeline.properties.GetProperty("customOutputter"));
							outputter.Print(anno, System.Console.Out, pipeline);
							break;
						}

						default:
						{
							throw new ArgumentException("Cannot output in format " + outputFormat + " from the interactive shell");
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal static ICollection<File> ReadFileList(string fileName)
		{
			return ObjectBank.GetLineIterator(fileName, new ObjectBank.PathToFileFunction());
		}

		private static AnnotationSerializer LoadSerializer(string serializerClass, string name, Properties properties)
		{
			AnnotationSerializer serializer;
			// initialized below
			try
			{
				// Try loading with properties
				serializer = ReflectionLoading.LoadByReflection(serializerClass, name, properties);
			}
			catch (ReflectionLoading.ReflectionLoadingException)
			{
				// Try loading with just default constructor
				serializer = ReflectionLoading.LoadByReflection(serializerClass);
			}
			return serializer;
		}

		/// <summary>Process a collection of files.</summary>
		/// <param name="base">The base input directory to process from.</param>
		/// <param name="files">The files to process.</param>
		/// <param name="numThreads">The number of threads to annotate on.</param>
		/// <param name="clearPool">Whether or not to clear pool when process is done</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ProcessFiles(string @base, ICollection<File> files, int numThreads, bool clearPool, Optional<Timing> tim)
		{
			AnnotationOutputter.Options options = AnnotationOutputter.GetOptions(this);
			StanfordCoreNLP.OutputFormat outputFormat = StanfordCoreNLP.OutputFormat.ValueOf(properties.GetProperty("outputFormat", DefaultOutputFormat).ToUpper());
			ProcessFiles(@base, files, numThreads, properties, null, CreateOutputter(properties, options), outputFormat, clearPool, Optional.Of(this), tim);
		}

		/// <summary>
		/// Create an outputter to be passed into
		/// <see cref="ProcessFiles(string, System.Collections.Generic.ICollection{E}, int, Java.Util.Properties, Java.Util.Function.IBiConsumer{T, U}, Java.Util.Function.IBiConsumer{T, U}, OutputFormat, bool)"/>
		/// .
		/// </summary>
		/// <param name="properties">The properties file to use.</param>
		/// <param name="outputOptions">The means of creating output options</param>
		/// <returns>A consumer that can be passed into the processFiles method.</returns>
		public static IBiConsumer<Annotation, OutputStream> CreateOutputter(Properties properties, AnnotationOutputter.Options outputOptions)
		{
			StanfordCoreNLP.OutputFormat outputFormat = StanfordCoreNLP.OutputFormat.ValueOf(properties.GetProperty("outputFormat", DefaultOutputFormat).ToUpper());
			string serializerClass = properties.GetProperty("serializer", typeof(ProtobufAnnotationSerializer).FullName);
			string outputSerializerClass = properties.GetProperty("outputSerializer", serializerClass);
			string outputSerializerName = (serializerClass.Equals(outputSerializerClass)) ? "serializer" : "outputSerializer";
			string outputFormatOptions = properties.GetProperty("outputFormatOptions");
			return null;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal static void ProcessFiles(string @base, ICollection<File> files, int numThreads, Properties properties, IBiConsumer<Annotation, IConsumer<Annotation>> annotate, IBiConsumer<Annotation, OutputStream> print, StanfordCoreNLP.OutputFormat
			 outputFormat, bool clearPool)
		{
			ProcessFiles(@base, files, numThreads, properties, annotate, print, outputFormat, clearPool, Optional.Empty(), Optional.Empty());
		}

		/// <summary>Helper method for printing out timing info after an annotation run</summary>
		/// <param name="pipeline">the StanfordCoreNLP pipeline to log timing info for</param>
		/// <param name="tim">the Timing object to log timing info</param>
		protected internal static void LogTimingInfo(StanfordCoreNLP pipeline, Timing tim)
		{
			logger.Info(string.Empty);
			// puts blank line in logging output
			logger.Info(pipeline.TimingInformation());
			logger.Info("Pipeline setup: " + Timing.ToSecondsString(pipeline.pipelineSetupTime) + " sec.");
			logger.Info("Total time for StanfordCoreNLP pipeline: " + Timing.ToSecondsString(pipeline.pipelineSetupTime + tim.Report()) + " sec.");
		}

		/// <summary>
		/// A common method for processing a set of files, used in both
		/// <see cref="StanfordCoreNLP"/>
		/// as well as
		/// <see cref="StanfordCoreNLPClient"/>
		/// .
		/// </summary>
		/// <param name="base">The base input directory to process from.</param>
		/// <param name="files">The files to process.</param>
		/// <param name="numThreads">The number of threads to annotate on.</param>
		/// <param name="properties">
		/// The properties file to use during annotation.
		/// This should match the properties file used in the implementation of the annotate function.
		/// </param>
		/// <param name="annotate">The function used to annotate a document.</param>
		/// <param name="print">The function used to print a document.</param>
		/// <param name="outputFormat">The format used for printing out documents</param>
		/// <param name="clearPool">Whether or not to clear the pool when done</param>
		/// <param name="pipeline">the pipeline annotating the objects</param>
		/// <param name="tim">the Timing object for this annotation run</param>
		/// <exception cref="System.IO.IOException"/>
		protected internal static void ProcessFiles(string @base, ICollection<File> files, int numThreads, Properties properties, IBiConsumer<Annotation, IConsumer<Annotation>> annotate, IBiConsumer<Annotation, OutputStream> print, StanfordCoreNLP.OutputFormat
			 outputFormat, bool clearPool, Optional<StanfordCoreNLP> pipeline, Optional<Timing> tim)
		{
			// List<Runnable> toRun = new LinkedList<>();
			// Process properties here
			string baseOutputDir = properties.GetProperty("outputDirectory", ".");
			string baseInputDir = properties.GetProperty("inputDirectory", @base);
			// Set of files to exclude
			string excludeFilesParam = properties.GetProperty("excludeFiles");
			ICollection<string> excludeFiles = new HashSet<string>();
			if (excludeFilesParam != null)
			{
				IEnumerable<string> lines = IOUtils.ReadLines(excludeFilesParam);
				foreach (string line in lines)
				{
					string name = line.Trim();
					if (!name.IsEmpty())
					{
						excludeFiles.Add(name);
					}
				}
			}
			//(file info)
			string serializerClass = properties.GetProperty("serializer", typeof(GenericAnnotationSerializer).FullName);
			string inputSerializerClass = properties.GetProperty("inputSerializer", serializerClass);
			string inputSerializerName = (serializerClass.Equals(inputSerializerClass)) ? "serializer" : "inputSerializer";
			string defaultExtension;
			switch (outputFormat)
			{
				case StanfordCoreNLP.OutputFormat.Xml:
				{
					defaultExtension = ".xml";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Json:
				{
					defaultExtension = ".json";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Conll:
				{
					defaultExtension = ".conll";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Conllu:
				{
					defaultExtension = ".conllu";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Text:
				{
					defaultExtension = ".out";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Serialized:
				{
					defaultExtension = ".ser.gz";
					break;
				}

				case StanfordCoreNLP.OutputFormat.Custom:
				{
					defaultExtension = ".out";
					break;
				}

				default:
				{
					throw new ArgumentException("Unknown output format " + outputFormat);
				}
			}
			string extension = properties.GetProperty("outputExtension", defaultExtension);
			bool replaceExtension = bool.Parse(properties.GetProperty("replaceExtension", "false"));
			bool continueOnAnnotateError = bool.Parse(properties.GetProperty("continueOnAnnotateError", "false"));
			bool noClobber = bool.Parse(properties.GetProperty("noClobber", "false"));
			// final boolean randomize = Boolean.parseBoolean(properties.getProperty("randomize", "false"));
			MutableInteger totalProcessed = new MutableInteger(0);
			MutableInteger totalSkipped = new MutableInteger(0);
			MutableInteger totalErrorAnnotating = new MutableInteger(0);
			//for each file...
			foreach (File file in files)
			{
				// Determine if there is anything to be done....
				if (excludeFiles.Contains(file.GetName()))
				{
					logger.Err("Skipping excluded file " + file.GetName());
					totalSkipped.IncValue(1);
					continue;
				}
				//--Get Output File Info
				//(filename)
				string outputDir = baseOutputDir;
				if (baseInputDir != null)
				{
					// Get input file name relative to base
					string relDir = file.GetParent().ReplaceFirst(Pattern.Quote(baseInputDir), string.Empty);
					outputDir = outputDir + File.separator + relDir;
				}
				// Make sure output directory exists
				new File(outputDir).Mkdirs();
				string outputFilename = new File(outputDir, file.GetName()).GetPath();
				if (replaceExtension)
				{
					int lastDot = outputFilename.LastIndexOf('.');
					// for paths like "./zzz", lastDot will be 0
					if (lastDot > 0)
					{
						outputFilename = Sharpen.Runtime.Substring(outputFilename, 0, lastDot);
					}
				}
				// ensure we don't make filenames with doubled extensions like .xml.xml
				if (!outputFilename.EndsWith(extension))
				{
					outputFilename += extension;
				}
				// normalize filename for the upcoming comparison
				outputFilename = new File(outputFilename).GetCanonicalPath();
				//--Conditions For Skipping The File
				// TODO this could fail if there are softlinks, etc. -- need some sort of sameFile tester
				//      Java 7 will have a Files.isSymbolicLink(file) method
				if (outputFilename.Equals(file.GetCanonicalPath()))
				{
					logger.Err("Skipping " + file.GetName() + ": output file " + outputFilename + " has the same filename as the input file -- assuming you don't actually want to do this.");
					totalSkipped.IncValue(1);
					continue;
				}
				if (noClobber && new File(outputFilename).Exists())
				{
					logger.Err("Skipping " + file.GetName() + ": output file " + outputFilename + " as it already exists.  Don't use the noClobber option to override this.");
					totalSkipped.IncValue(1);
					continue;
				}
				string finalOutputFilename = outputFilename;
				//register a task...
				//catching exceptions...
				try
				{
					// Check whether this file should be skipped again
					if (noClobber && new File(finalOutputFilename).Exists())
					{
						logger.Err("Skipping " + file.GetName() + ": output file " + finalOutputFilename + " as it already exists.  Don't use the noClobber option to override this.");
						lock (totalSkipped)
						{
							totalSkipped.IncValue(1);
						}
						return;
					}
					logger.Info("Processing file " + file.GetAbsolutePath() + " ... writing to " + finalOutputFilename);
					//--Process File
					Annotation annotation = null;
					if (file.GetAbsolutePath().EndsWith(".ser.gz"))
					{
						// maybe they want to continue processing a partially processed annotation
						try
						{
							// Create serializers
							if (inputSerializerClass != null)
							{
								AnnotationSerializer inputSerializer = LoadSerializer(inputSerializerClass, inputSerializerName, properties);
								InputStream @is = new BufferedInputStream(new FileInputStream(file));
								Pair<Annotation, InputStream> pair = inputSerializer.Read(@is);
								pair.second.Close();
								annotation = pair.first;
								IOUtils.CloseIgnoringExceptions(@is);
							}
							else
							{
								annotation = IOUtils.ReadObjectFromFile(file);
							}
						}
						catch (IOException)
						{
						}
						catch (TypeLoadException e)
						{
							// guess that's not what they wanted
							// We hide IOExceptions because ones such as file not
							// found will be thrown again in a moment.  Note that
							// we are intentionally letting class cast exceptions
							// and class not found exceptions go through.
							throw new Exception(e);
						}
					}
					//(read file)
					if (annotation == null)
					{
						string encoding = properties.GetProperty("encoding", "UTF-8");
						string text = IOUtils.SlurpFile(file.GetAbsoluteFile(), encoding);
						annotation = new Annotation(text);
						annotation.Set(typeof(CoreAnnotations.DocIDAnnotation), file.GetName());
					}
					Timing timing = new Timing();
					annotate.Accept(annotation, null);
				}
				catch (IOException e)
				{
					//--Output File
					// check we've processed or errored on every file, handle tasks to run after last document
					// clear pool if necessary
					// print out timing info
					// Error annotating but still wanna continue
					// (maybe in the middle of long job and maybe next one will be okay)
					// check we've processed or errored on every file, handle tasks to run after last document
					// clear pool if necessary
					// print out timing info
					// if stopping due to error, make sure to clear the pool
					throw new RuntimeIOException(e);
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ProcessFiles(ICollection<File> files, int numThreads, bool clearPool, Optional<Timing> tim)
		{
			ProcessFiles(null, files, numThreads, clearPool, tim);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ProcessFiles(ICollection<File> files, bool clearPool, Optional<Timing> tim)
		{
			ProcessFiles(files, 1, clearPool, tim);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Run()
		{
			Run(false);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Run(bool clearPool)
		{
			Timing tim = new Timing();
			StanfordRedwoodConfiguration.MinimalSetup();
			// multithreading thread count
			string numThreadsString = (this.properties == null) ? null : this.properties.GetProperty("threads");
			int numThreads = 1;
			try
			{
				if (numThreadsString != null)
				{
					numThreads = System.Convert.ToInt32(numThreadsString);
				}
			}
			catch (NumberFormatException)
			{
				logger.Err("-threads [number]: was not given a valid number: " + numThreadsString);
			}
			// blank line after all the loading statements to make output more readable
			logger.Info(string.Empty);
			//
			// Process one file or a directory of files
			//
			if (properties.Contains("file") || properties.Contains("textFile"))
			{
				string fileName = properties.GetProperty("file");
				if (fileName == null)
				{
					fileName = properties.GetProperty("textFile");
				}
				ICollection<File> files = new FileSequentialCollection(new File(fileName), properties.GetProperty("extension"), true);
				this.ProcessFiles(null, files, numThreads, clearPool, Optional.Of(tim));
			}
			else
			{
				//
				// Process a list of files
				//
				if (properties.Contains("filelist"))
				{
					string fileName = properties.GetProperty("filelist");
					ICollection<File> inputFiles = ReadFileList(fileName);
					ICollection<File> files = new List<File>(inputFiles.Count);
					foreach (File file in inputFiles)
					{
						if (file.IsDirectory())
						{
							Sharpen.Collections.AddAll(files, new FileSequentialCollection(new File(fileName), properties.GetProperty("extension"), true));
						}
						else
						{
							files.Add(file);
						}
					}
					this.ProcessFiles(null, files, numThreads, clearPool, Optional.Of(tim));
				}
				else
				{
					//
					// Run the interactive shell
					//
					Shell(this);
				}
			}
		}

		/// <summary>This can be used just for testing or for command-line text processing.</summary>
		/// <remarks>
		/// This can be used just for testing or for command-line text processing.
		/// This runs the pipeline you specify on the
		/// text in the file that you specify and sends some results to stdout.
		/// The current code in this main method assumes that each line of the file
		/// is to be processed separately as a single sentence.
		/// <p>
		/// Example usage:<br />
		/// java -mx6g edu.stanford.nlp.pipeline.StanfordCoreNLP properties
		/// </remarks>
		/// <param name="args">List of required properties</param>
		/// <exception cref="System.IO.IOException">If IO problem</exception>
		/// <exception cref="System.TypeLoadException">If class loading problem</exception>
		public static void Main(string[] args)
		{
			//
			// process the arguments
			//
			// Extract all the properties from the command line.
			// As well as command-line properties, the processor will search for the properties file in the classpath
			Properties props = new Properties();
			if (args.Length > 0)
			{
				props = StringUtils.ArgsToProperties(args);
				// handle new fileList by making sure filelist is also set
				if (props.Contains("fileList"))
				{
					props.SetProperty("filelist", props.GetProperty("fileList"));
				}
				bool hasH = props.Contains("h");
				bool hasHelp = props.Contains("help");
				if (hasH || hasHelp)
				{
					string helpValue = hasH ? props.GetProperty("h") : props.GetProperty("help");
					PrintHelp(System.Console.Error, helpValue);
					return;
				}
			}
			// Run the pipeline
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Run(true);
			// clear the pool if not running in multi-thread mode
			if (!props.Contains("threads") || System.Convert.ToInt32(props.GetProperty("threads")) <= 1)
			{
				pipeline.pool.Clear();
			}
		}
	}
}
