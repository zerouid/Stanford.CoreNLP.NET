using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Net;
using Java.Nio.File;
using Java.Util;
using Java.Util.Jar;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A class to set command line options.</summary>
	/// <remarks>
	/// A class to set command line options. To use, create a static class into which you'd like
	/// to put your properties. Then, for each field, set the annotation:
	/// <pre><code>
	/// import edu.stanford.nlp.util.ArgumentParser.Option
	/// class Props {
	/// &#64;Option(name="anIntOption", required=false, gloss="This is an int")
	/// public static int anIntOption = 7; // default value is 7
	/// &#64;Option(name="anotherOption", required=false)
	/// public static File aCastableOption = new File("/foo");
	/// }
	/// </code></pre>
	/// You can then set options with
	/// <see cref="FillOptions(string[])"/>
	/// ,
	/// or with
	/// <see cref="FillOptions(Java.Util.Properties)"/>
	/// .
	/// If your default classpath has many classes in it, you can select a subset of them
	/// by using
	/// <see cref="FillOptions(System.Type{T}[], Java.Util.Properties)"/>
	/// , or some variant.
	/// A complete toy example looks like this:
	/// <pre><code>
	/// import java.util.Properties;
	/// import edu.stanford.nlp.util.ArgumentParser;
	/// import edu.stanford.nlp.util.StringUtils;
	/// public class Foo {
	/// &#64;ArgumentParser.Option(name="bar", gloss="This is a string option.", required=true)
	/// private static String BAR = null;
	/// public static void main(String[] args) {
	/// // Parse the arguments
	/// Properties props = StringUtils.argsToProperties(args);
	/// ArgumentParser.fillOptions(new Class[]{ Foo.class, ArgumentParser.class }, props);
	/// log.info(INPUT);
	/// }
	/// }
	/// </code></pre>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class ArgumentParser
	{
		private ArgumentParser()
		{
		}

		private static readonly string[] IgnoredJars = new string[] {  };

		private static readonly Type[] BootstrapClasses = new Type[] { typeof(Edu.Stanford.Nlp.Util.ArgumentParser) };

		public static Type[] optionClasses;

		public static int threads = Runtime.GetRuntime().AvailableProcessors();

		public static string host = "(unknown)";

		private static bool strict = false;

		private static bool verbose = false;

		static ArgumentParser()
		{
			// static class
			// = null;
			try
			{
				host = InetAddress.GetLocalHost().GetHostName();
			}
			catch (Exception)
			{
			}
		}

		/*
		* ----------
		* OPTIONS
		* ----------
		*/
		private static void FillField(object instance, FieldInfo f, string value)
		{
			//--Verbose
			if (verbose)
			{
				ArgumentParser.Option opt = f.GetAnnotation<ArgumentParser.Option>();
				StringBuilder b = new StringBuilder("setting ").Append(f.DeclaringType.FullName).Append('#').Append(f.Name).Append(' ');
				if (opt != null)
				{
					b.Append('[').Append(opt.Name()).Append("] ");
				}
				b.Append("to: ").Append(value);
				Redwood.Util.Log(b.ToString());
			}
			try
			{
				//--Permissions
				bool accessState = true;
				if (Modifier.IsFinal(f.GetModifiers()))
				{
					Redwood.Util.RuntimeException("Option cannot be final: " + f);
				}
				if (!f.IsAccessible())
				{
					accessState = false;
					f.SetAccessible(true);
				}
				//--Set Value
				object objVal = MetaClass.Cast(value, f.GetGenericType());
				if (objVal != null)
				{
					if (objVal.GetType().IsArray)
					{
						//(case: array)
						object[] array = (object[])objVal;
						// error check
						if (!f.GetType().IsArray)
						{
							Redwood.Util.RuntimeException("Setting an array to a non-array field. field: " + f + " value: " + Arrays.ToString(array) + " src: " + value);
						}
						// create specific array
						object toSet = System.Array.CreateInstance(f.GetType().GetElementType(), array.Length);
						for (int i = 0; i < array.Length; i++)
						{
							Sharpen.Runtime.SetArrayValue(toSet, i, array[i]);
						}
						// set value
						f.SetValue(instance, toSet);
					}
					else
					{
						//case: not array
						f.SetValue(instance, objVal);
					}
				}
				else
				{
					Redwood.Util.RuntimeException("Cannot assign option field: " + f + " value: " + value + "; invalid type");
				}
				//--Permissions
				if (!accessState)
				{
					f.SetAccessible(false);
				}
			}
			catch (ArgumentException e)
			{
				Redwood.Util.Err(e);
				Redwood.Util.RuntimeException("Cannot assign option field: " + f.DeclaringType.GetCanonicalName() + '.' + f.Name + " value: " + value + " cause: " + e.Message);
			}
			catch (MemberAccessException e)
			{
				Redwood.Util.Err(e);
				Redwood.Util.RuntimeException("Cannot access option field: " + f.DeclaringType.GetCanonicalName() + '.' + f.Name);
			}
			catch (Exception e)
			{
				Redwood.Util.Err(e);
				Redwood.Util.RuntimeException("Cannot assign option field: " + f.DeclaringType.GetCanonicalName() + '.' + f.Name + " value: " + value + " cause: " + e.Message);
			}
		}

		private static Type FilePathToClass(string cpEntry, string path)
		{
			if (path.Length <= cpEntry.Length)
			{
				throw new ArgumentException("Illegal path: cp=" + cpEntry + " path=" + path);
			}
			if (path[cpEntry.Length] != '/')
			{
				throw new ArgumentException("Illegal path: cp=" + cpEntry + " path=" + path);
			}
			path = Sharpen.Runtime.Substring(path, cpEntry.Length + 1);
			path = Sharpen.Runtime.Substring(path.ReplaceAll("/", "."), 0, path.Length - 6);
			try
			{
				return Sharpen.Runtime.GetType(path, false, ClassLoader.GetSystemClassLoader());
			}
			catch (TypeLoadException)
			{
				throw Redwood.Util.Fail("Could not load class at path: " + path);
			}
			catch (NoClassDefFoundError)
			{
				Redwood.Util.Warn("Class at path " + path + " is unloadable");
				return null;
			}
		}

		private static bool IsIgnored(string path)
		{
			return Arrays.Stream(IgnoredJars).AnyMatch(null);
		}

		private static Type[] GetVisibleClasses()
		{
			//--Variables
			IList<Type> classes = new List<Type>();
			// (get classpath)
			string pathSep = Runtime.GetProperty("path.separator");
			string[] cp = Runtime.GetProperties().GetProperty("java.class.path", null).Split(pathSep);
			// --Fill Options
			// (get classes)
			foreach (string entry in cp)
			{
				Redwood.Util.Log("Checking cp " + entry);
				//(should skip?)
				if (entry.Equals(".") || entry.Trim().IsEmpty())
				{
					continue;
				}
				//(no, don't skip)
				File f = new File(entry);
				if (f.IsDirectory())
				{
					// --Case: Files
					try
					{
						using (IDirectoryStream<IPath> stream = Files.NewDirectoryStream(f.ToPath(), "*.class"))
						{
							foreach (IPath p in stream)
							{
								//(get the associated class)
								Type clazz = FilePathToClass(entry, p.ToString());
								if (clazz != null)
								{
									//(add the class if it's valid)
									classes.Add(clazz);
								}
							}
						}
					}
					catch (IOException ioe)
					{
						Redwood.Util.Error(ioe);
					}
				}
				else
				{
					//noinspection StatementWithEmptyBody
					if (!IsIgnored(entry))
					{
						// --Case: Jar
						try
						{
							using (JarFile jar = new JarFile(f))
							{
								IEnumeration<JarEntry> e = ((IEnumeration<JarEntry>)jar.Entries());
								while (e.MoveNext())
								{
									//(for each jar file element)
									JarEntry jarEntry = e.Current;
									string clazz = jarEntry.GetName();
									if (clazz.Matches(".*class$"))
									{
										//(if it's a class)
										clazz = Sharpen.Runtime.Substring(clazz, 0, clazz.Length - 6).ReplaceAll("/", ".");
										//(add it)
										try
										{
											classes.Add(Sharpen.Runtime.GetType(clazz, false, ClassLoader.GetSystemClassLoader()));
										}
										catch (TypeLoadException)
										{
											Redwood.Util.Warn("Could not load class in jar: " + f + " at path: " + clazz);
										}
										catch (NoClassDefFoundError)
										{
											Redwood.Util.Debug("Could not scan class: " + clazz + " (in jar: " + f + ')');
										}
									}
								}
							}
						}
						catch (IOException)
						{
							Redwood.Util.Warn("Could not open jar file: " + f + "(are you sure the file exists?)");
						}
					}
				}
			}
			//case: ignored jar
			return Sharpen.Collections.ToArray(classes, new Type[classes.Count]);
		}

		/// <summary>Get all the declared fields of this class and all super classes.</summary>
		/// <exception cref="System.Exception"/>
		private static FieldInfo[] ScrapeFields(Type clazz)
		{
			IList<FieldInfo> fields = new List<FieldInfo>();
			while (clazz != null && !clazz.Equals(typeof(object)))
			{
				Sharpen.Collections.AddAll(fields, Arrays.AsList(Sharpen.Runtime.GetDeclaredFields(clazz)));
				clazz = clazz.BaseType;
			}
			return Sharpen.Collections.ToArray(fields, new FieldInfo[fields.Count]);
		}

		private static string ThreadRootClass()
		{
			StackTraceElement[] trace = Thread.CurrentThread().GetStackTrace();
			int i = trace.Length - 1;
			while (i > 0 && (trace[i].GetClassName().StartsWith("com.intellij") || trace[i].GetClassName().StartsWith("java.") || trace[i].GetClassName().StartsWith("sun.")))
			{
				i -= 1;
			}
			StackTraceElement elem = trace[i];
			return elem.GetClassName();
		}

		private static string BufferString(string raw, int minLength)
		{
			StringBuilder b = new StringBuilder(raw);
			for (int i = raw.Length; i < minLength; ++i)
			{
				b.Append(' ');
			}
			return b.ToString();
		}

		private static IDictionary<string, FieldInfo> FillOptionsImpl(object[] instances, Type[] classes, Properties options, bool ensureAllOptions, bool isBootstrap)
		{
			// Print usage, if applicable
			if (!isBootstrap)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("true", options.GetProperty("usage", "false")) || Sharpen.Runtime.EqualsIgnoreCase("true", options.GetProperty("help", "false")))
				{
					ICollection<Type> allClasses = new HashSet<Type>();
					Java.Util.Collections.AddAll(allClasses, classes);
					if (instances != null)
					{
						foreach (object o in instances)
						{
							allClasses.Add(o.GetType());
						}
					}
					System.Console.Error.WriteLine(Usage(Sharpen.Collections.ToArray(allClasses, new Type[0])));
					System.Environment.Exit(0);
				}
			}
			//--Create Class->Object Mapping
			IDictionary<Type, object> class2object = new Dictionary<Type, object>();
			if (instances != null)
			{
				for (int i = 0; i < classes.Length; ++i)
				{
					System.Diagnostics.Debug.Assert(instances[i].GetType() == classes[i]);
					class2object[classes[i]] = instances[i];
					Type mySuper = instances[i].GetType().BaseType;
					while (mySuper != null && !mySuper.Equals(typeof(object)))
					{
						if (!class2object.Contains(mySuper))
						{
							class2object[mySuper] = instances[i];
						}
						mySuper = mySuper.BaseType;
					}
				}
			}
			//--Get Fillable Options
			IDictionary<string, FieldInfo> canFill = new Dictionary<string, FieldInfo>();
			IDictionary<string, Pair<bool, bool>> required = new Dictionary<string, Pair<bool, bool>>();
			/* <exists, is_set> */
			IDictionary<string, string> interner = new Dictionary<string, string>();
			foreach (Type c in classes)
			{
				FieldInfo[] fields;
				try
				{
					fields = ScrapeFields(c);
				}
				catch (Exception e)
				{
					Redwood.Util.Debug("Could not check fields for class: " + c.FullName + "  (caused by " + e.GetType() + ": " + e.Message + ')');
					continue;
				}
				bool someOptionFilled = false;
				bool someOptionFound = false;
				foreach (FieldInfo f in fields)
				{
					ArgumentParser.Option o = f.GetAnnotation<ArgumentParser.Option>();
					if (o != null)
					{
						someOptionFound = true;
						//(check if field is static)
						if ((f.GetModifiers() & Modifier.Static) == 0 && instances == null)
						{
							continue;
						}
						someOptionFilled = true;
						//(required marker)
						Pair<bool, bool> mark = Pair.MakePair(false, false);
						if (o.Required())
						{
							mark = Pair.MakePair(true, false);
						}
						//(add main name)
						string name = o.Name().ToLower();
						if (name.IsEmpty())
						{
							name = f.Name.ToLower();
						}
						if (canFill.Contains(name))
						{
							string name1 = canFill[name].DeclaringType.GetCanonicalName() + '.' + canFill[name].Name;
							string name2 = f.DeclaringType.GetCanonicalName() + '.' + f.Name;
							if (!name1.Equals(name2))
							{
								Redwood.Util.RuntimeException("Multiple declarations of option " + name + ": " + name1 + " and " + name2);
							}
							else
							{
								Redwood.Util.Err("Class is in classpath multiple times: " + canFill[name].DeclaringType.GetCanonicalName());
							}
						}
						canFill[name] = f;
						required[name] = mark;
						interner[name] = name;
						//(add alternate names)
						if (!o.Alt().IsEmpty())
						{
							foreach (string alt in o.Alt().Split(" *, *"))
							{
								alt = alt.ToLower();
								if (canFill.Contains(alt) && !alt.Equals(name))
								{
									throw new ArgumentException("Multiple declarations of option " + alt + ": " + canFill[alt] + " and " + f);
								}
								canFill[alt] = f;
								if (mark.first)
								{
									required[alt] = mark;
								}
								interner[alt] = name;
							}
						}
					}
				}
				//(check to ensure that something got filled, if any @Option annotation was found)
				if (someOptionFound && !someOptionFilled)
				{
					Redwood.Util.Warn("found @Option annotations in class " + c + ", but didn't set any of them (all options were instance variables and no instance given?)");
				}
			}
			//--Fill Options
			foreach (KeyValuePair<object, object> entry in options)
			{
				string rawKeyStr = entry.Key.ToString();
				string key = rawKeyStr.ToLower();
				// (get values)
				string value = entry.Value.ToString();
				System.Diagnostics.Debug.Assert(value != null);
				FieldInfo target = canFill[key];
				// (mark required option as fulfilled)
				Pair<bool, bool> mark = required[key];
				if (mark != null && mark.first)
				{
					required[key] = Pair.MakePair(true, true);
				}
				// (fill the field)
				if (target != null)
				{
					// (case: declared option)
					FillField(class2object[target.DeclaringType], target, value);
				}
				else
				{
					if (ensureAllOptions)
					{
						// (case: undeclared option)
						// split the key
						int lastDotIndex = rawKeyStr.LastIndexOf('.');
						if (lastDotIndex < 0)
						{
							Redwood.Util.Err("Unrecognized option: " + key);
							continue;
						}
						if (!rawKeyStr.StartsWith("log."))
						{
							// ignore Redwood options
							string className = Sharpen.Runtime.Substring(rawKeyStr, 0, lastDotIndex);
							// get the class
							Type clazz = null;
							try
							{
								clazz = ClassLoader.GetSystemClassLoader().LoadClass(className);
							}
							catch (Exception)
							{
								Redwood.Util.Err("Could not set option: " + entry.Key + "; either the option is mistyped, not defined, or the class " + className + " does not exist.");
							}
							// get the field
							if (clazz != null)
							{
								string fieldName = Sharpen.Runtime.Substring(rawKeyStr, lastDotIndex + 1);
								try
								{
									target = clazz.GetField(fieldName);
								}
								catch (Exception)
								{
									Redwood.Util.Err("Could not set option: " + entry.Key + "; no such field: " + fieldName + " in class: " + className);
								}
								if (target != null)
								{
									Redwood.Util.Log("option overrides " + target + " to '" + value + '\'');
									FillField(class2object[target.DeclaringType], target, value);
								}
								else
								{
									Redwood.Util.Err("Could not set option: " + entry.Key + "; no such field: " + fieldName + " in class: " + className);
								}
							}
						}
					}
				}
			}
			//--Ensure Required
			bool good = true;
			foreach (KeyValuePair<string, Pair<bool, bool>> entry_1 in required)
			{
				string key = entry_1.Key;
				Pair<bool, bool> mark = entry_1.Value;
				if (mark.first && !mark.second)
				{
					Redwood.Util.Err("Missing required option: " + interner[key] + "   <in class: " + canFill[key].DeclaringType + '>');
					required[key] = Pair.MakePair(true, true);
					//don't duplicate error messages
					good = false;
				}
			}
			if (!good)
			{
				throw new Exception("Specified properties are not parsable or not valid!");
			}
			//System.exit(1);
			return canFill;
		}

		private static IDictionary<string, FieldInfo> FillOptionsImpl(object[] instances, Type[] classes, Properties options)
		{
			return FillOptionsImpl(instances, classes, options, strict, false);
		}

		/*
		* ----------
		* EXECUTION
		* ----------
		*/
		/// <summary>
		/// Populate all static options in the given set of classes, as defined by the given
		/// properties.
		/// </summary>
		/// <param name="classes">
		/// The classes to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="options">The properties to use to fill these fields.</param>
		public static void FillOptions(Type[] classes, Properties options)
		{
			FillOptionsImpl(null, classes, options);
		}

		/// <summary>
		/// Populate all static
		/// <see cref="Option"/>
		/// -tagged fields in the given classes with the given Properties.
		/// Then, fill in additional (or overwrite existing) properties with the given (String) command-line arguments.
		/// </summary>
		/// <param name="optionClasses">
		/// The classes to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="props">The properties to use to fill these fields.</param>
		/// <param name="args">The command-line arguments to use to fill in additional properties.</param>
		public static void FillOptions(Type[] optionClasses, Properties props, params string[] args)
		{
			Edu.Stanford.Nlp.Util.ArgumentParser.optionClasses = optionClasses;
			FillOptions(props, args);
		}

		/// <summary>
		/// Populate with the given command-line arguments all static
		/// <see cref="Option"/>
		/// -tagged fields in
		/// the given classes.
		/// </summary>
		/// <param name="classes">
		/// The classes to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="args">The command-line arguments to use to fill in additional properties.</param>
		public static void FillOptions(Type[] classes, params string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args);
			//get options
			FillOptionsImpl(null, BootstrapClasses, options, false, true);
			//bootstrap
			FillOptionsImpl(null, classes, options);
		}

		/// <summary>
		/// Populate all static options in the given class, as defined by the given
		/// properties.
		/// </summary>
		/// <param name="clazz">
		/// The class to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="options">The properties to use to fill these fields.</param>
		public static void FillOptions(Type clazz, Properties options)
		{
			FillOptionsImpl(null, new Type[] { clazz }, options);
		}

		/// <summary>Populate all static options in the given class, as defined by the given properties.</summary>
		/// <remarks>
		/// Populate all static options in the given class, as defined by the given properties.
		/// Then, fill in additional (or overwrite existing) properties with the given (String) command-line arguments.
		/// </remarks>
		/// <param name="clazz">
		/// The class to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="props">The properties to use to fill these fields.</param>
		/// <param name="args">Additional command-line options to fill these fields.</param>
		public static void FillOptions(Type clazz, Properties props, params string[] args)
		{
			Properties allProperties = UpdatePropertiesWithOptions(props, args);
			FillOptionsImpl(null, new Type[] { clazz }, allProperties);
		}

		/// <summary>
		/// Populate all static options in the given class, as defined by the given
		/// command-line arguments.
		/// </summary>
		/// <param name="clazz">
		/// The class to populate static
		/// <see cref="Option"/>
		/// -tagged fields in.
		/// </param>
		/// <param name="args">The command-line arguments to use to fill these fields.</param>
		public static void FillOptions(Type clazz, params string[] args)
		{
			Type[] classes = new Type[1];
			classes[0] = clazz;
			FillOptions(classes, args);
		}

		/// <summary>Populate with the given properties all static options in all classes in the current classpath.</summary>
		/// <remarks>
		/// Populate with the given properties all static options in all classes in the current classpath.
		/// Note that this may take a while if the classpath is large.
		/// </remarks>
		/// <param name="props">The properties to use to fill fields in the various classes.</param>
		public static void FillOptions(Properties props)
		{
			FillOptions(props, StringUtils.EmptyStringArray);
		}

		/// <summary>
		/// Populate with the given command-line arguments all static options in all
		/// classes in the current classpath.
		/// </summary>
		/// <remarks>
		/// Populate with the given command-line arguments all static options in all
		/// classes in the current classpath.
		/// Note that this may take a while if the classpath is large.
		/// </remarks>
		/// <param name="args">The command-line arguments to use to fill options.</param>
		public static void FillOptions(params string[] args)
		{
			FillOptions(StringUtils.ArgsToProperties(args), StringUtils.EmptyStringArray);
		}

		/// <summary>
		/// Populate all static
		/// <see cref="Option"/>
		/// -tagged fields in the given classes with the given Properties.
		/// Then, fill in additional (or overwrite existing) properties with the given (String) command-line arguments.
		/// Note that this may take a while if the classpath is large.
		/// </summary>
		/// <param name="props">The properties to use to fill fields in the various classes.</param>
		/// <param name="args">The command-line arguments to use to fill in additional properties.</param>
		public static void FillOptions(Properties props, params string[] args)
		{
			//(convert to map)
			Properties allProperties = UpdatePropertiesWithOptions(props, args);
			//(bootstrap)
			IDictionary<string, FieldInfo> bootstrapMap = FillOptionsImpl(null, BootstrapClasses, allProperties, false, true);
			bootstrapMap.Keys.ForEach(null);
			//(fill options)
			Type[] visibleClasses = optionClasses;
			if (visibleClasses == null)
			{
				visibleClasses = GetVisibleClasses();
			}
			//get classes
			FillOptionsImpl(null, visibleClasses, allProperties);
		}

		//fill
		/// <summary>
		/// Fill all non-static
		/// <see cref="Option"/>
		/// -tagged fields in the given set of objects with the given
		/// properties.
		/// </summary>
		/// <param name="instances">
		/// The object instances containing
		/// <see cref="Option"/>
		/// -tagged fields which we should fill.
		/// </param>
		/// <param name="options">The properties to use to fill these fields.</param>
		public static void FillOptions(object[] instances, Properties options)
		{
			Type[] classes = Arrays.Stream(instances).Map(null).ToArray(null);
			FillOptionsImpl(instances, classes, options);
		}

		/// <summary>
		/// Fill all non-static
		/// <see cref="Option"/>
		/// -tagged fields in the given set of objects with the given
		/// command-line arguments.
		/// </summary>
		/// <param name="instances">
		/// The object instances containing
		/// <see cref="Option"/>
		/// -tagged fields which we should fill.
		/// </param>
		/// <param name="args">The command-line arguments to use to fill these fields.</param>
		public static void FillOptions(object[] instances, string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args);
			//get options
			FillOptionsImpl(null, BootstrapClasses, options, false, true);
			//bootstrap
			Type[] classes = Arrays.Stream(instances).Map(null).ToArray(null);
			FillOptionsImpl(instances, classes, options);
		}

		/// <summary>
		/// Fill all non-static
		/// <see cref="Option"/>
		/// -tagged fields in the given object with the given
		/// properties.
		/// </summary>
		/// <param name="instance">
		/// The object instance containing
		/// <see cref="Option"/>
		/// -tagged fields which we should fill.
		/// </param>
		/// <param name="options">The properties to use to fill these fields.</param>
		public static void FillOptions(object instance, Properties options)
		{
			FillOptions(new object[] { instance }, options);
		}

		/// <summary>Populate all static options in the given class, as defined by the given properties.</summary>
		/// <remarks>
		/// Populate all static options in the given class, as defined by the given properties.
		/// Then, fill in additional (or overwrite existing) properties with the given (String) command-line arguments.
		/// </remarks>
		/// <param name="instance">
		/// The object instance containing
		/// <see cref="Option"/>
		/// -tagged fields which we should fill.
		/// </param>
		/// <param name="props">The properties to use to fill these fields.</param>
		/// <param name="args">Additional command-line options to fill these fields.</param>
		public static void FillOptions(object instance, Properties props, params string[] args)
		{
			Properties allProperties = UpdatePropertiesWithOptions(props, args);
			FillOptions(new object[] { instance }, allProperties);
		}

		/// <summary>
		/// Fill all non-static
		/// <see cref="Option"/>
		/// -tagged fields in the given object with the given
		/// command-line arguments.
		/// </summary>
		/// <param name="instance">
		/// The object instance containing
		/// <see cref="Option"/>
		/// -tagged fields which we should fill.
		/// </param>
		/// <param name="args">The command-line arguments to use to fill these fields.</param>
		public static void FillOptions(object instance, params string[] args)
		{
			FillOptions(new object[] { instance }, args);
		}

		/// <summary>Fill all the options for a given subcomponent.</summary>
		/// <remarks>
		/// Fill all the options for a given subcomponent.
		/// This assumes that the subcomponent takes properties with a prefix, so that, for example,
		/// if the subcomponent is
		/// <c>parse</c>
		/// then it takes a property
		/// <c>parse.maxlen</c>
		/// for instance.
		/// </remarks>
		/// <param name="subcomponent">The subcomponent to fill options for.</param>
		/// <param name="subcomponentName">The name of the subcomponent, for parsing properties.</param>
		/// <param name="props">The properties to fill the options in the subcomponent with.</param>
		public static void FillOptions(object subcomponent, string subcomponentName, Properties props)
		{
			Edu.Stanford.Nlp.Util.ArgumentParser.FillOptions(subcomponent, props);
			Properties withoutPrefix = new Properties();
			string prefixString = subcomponentName + '.';
			foreach (DictionaryEntry entry in props)
			{
				string key = entry.Key.ToString();
				withoutPrefix.SetProperty(key.Replace(prefixString, string.Empty), entry.Value.ToString());
			}
			Edu.Stanford.Nlp.Util.ArgumentParser.FillOptions(subcomponent, withoutPrefix);
		}

		private static Properties UpdatePropertiesWithOptions(Properties props, string[] args)
		{
			Properties allProperties = new Properties();
			// copy it so props isn't changed but can be overridden by args
			foreach (string key in props.StringPropertyNames())
			{
				allProperties.SetProperty(key, props.GetProperty(key));
			}
			Properties options = StringUtils.ArgsToProperties(args);
			foreach (string key_1 in options.StringPropertyNames())
			{
				allProperties.SetProperty(key_1, options.GetProperty(key_1));
			}
			return allProperties;
		}

		/// <summary>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given set of classes.
		/// </summary>
		/// <remarks>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given set of classes.
		/// This will print both the static options, and the non-static options.
		/// </remarks>
		/// <param name="optionsClasses">The classes defining the options being used by this program.</param>
		/// <returns>A String describing the usage of the class.</returns>
		public static string Usage(Type[] optionsClasses)
		{
			string mainClass = ThreadRootClass();
			StringBuilder b = new StringBuilder();
			b.Append("Usage: ").Append(mainClass).Append(' ');
			IList<Pair<ArgumentParser.Option, FieldInfo>> options = new List<Pair<ArgumentParser.Option, FieldInfo>>();
			foreach (Type clazz in optionsClasses)
			{
				try
				{
					Sharpen.Collections.AddAll(options, Arrays.Stream(ScrapeFields(clazz)).Map(null).Filter(null).Collect(Collectors.ToList()));
				}
				catch (Exception)
				{
					return b.Append("<unknown>").ToString();
				}
			}
			int longestOptionName = options.Stream().Map(null).Max(IComparer.ComparingInt(null)).OrElse(10);
			int longestOptionType = options.Stream().Map(null).Max(IComparer.ComparingInt(null)).OrElse(10) + 1;
			options.Stream().Filter(null).ForEach(null);
			options.Stream().Filter(null).ForEach(null);
			return b.ToString();
		}

		/// <summary>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given set of objects.
		/// </summary>
		/// <remarks>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given set of objects.
		/// This will print both the static options, and the non-static options.
		/// </remarks>
		/// <param name="optionsClasses">The objects defining the options being used by this program.</param>
		/// <returns>A String describing the usage of the class.</returns>
		public static string Usage(object[] optionsClasses)
		{
			return Usage(Arrays.Stream(optionsClasses).Map(null).ToArray(null));
		}

		/// <summary>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given class.
		/// </summary>
		/// <remarks>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given class.
		/// This will print both the static options, and the non-static options.
		/// </remarks>
		/// <param name="optionsClass">The class defining the options being used by this program.</param>
		/// <returns>A String describing the usage of the class.</returns>
		public static string Usage(Type optionsClass)
		{
			return Usage(new Type[] { optionsClass });
		}

		/// <summary>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given object.
		/// </summary>
		/// <remarks>
		/// Return a string describing the usage of the program this method is called from, given the
		/// options declared in the given object.
		/// This will print both the static options, and the non-static options.
		/// </remarks>
		/// <param name="optionsClass">The object defining the options being used by this program.</param>
		/// <returns>A String describing the usage of the class.</returns>
		public static string Usage(object optionsClass)
		{
			return Usage(new Type[] { optionsClass.GetType() });
		}
	}
}
