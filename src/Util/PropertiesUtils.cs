using System;
using System.Collections.Generic;
using System.IO;
using Java.IO;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Utilities methods for standard (but woeful) Java Properties objects.</summary>
	/// <author>Sarah Spikes</author>
	/// <author>David McClosky</author>
	public class PropertiesUtils
	{
		private PropertiesUtils()
		{
		}

		/// <summary>
		/// Returns true iff the given Properties contains a property with the given
		/// key (name), and its value is not "false" or "no" or "off".
		/// </summary>
		/// <param name="props">Properties object</param>
		/// <param name="key">The key to test</param>
		/// <returns>
		/// true iff the given Properties contains a property with the given
		/// key (name), and its value is not "false" or "no" or "off".
		/// </returns>
		public static bool HasProperty(Properties props, string key)
		{
			string value = props.GetProperty(key);
			if (value == null)
			{
				return false;
			}
			value = value.ToLower();
			return !(value.Equals("false") || value.Equals("no") || value.Equals("off"));
		}

		public static bool HasPropertyPrefix(Properties props, string prefix)
		{
			foreach (object o in props.Keys)
			{
				if (o is string && ((string)o).StartsWith(prefix))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Create a Properties object from the passed in String arguments.</summary>
		/// <remarks>
		/// Create a Properties object from the passed in String arguments.
		/// The odd numbered arguments are the names of keys, and the even
		/// numbered arguments are the value of the preceding key
		/// </remarks>
		/// <param name="args">An even-length list of alternately key and value</param>
		public static Properties AsProperties(params string[] args)
		{
			if (args.Length % 2 != 0)
			{
				throw new ArgumentException("Need an even number of arguments but there were " + args.Length);
			}
			Properties properties = new Properties();
			for (int i = 0; i < args.Length; i += 2)
			{
				properties.SetProperty(args[i], args[i + 1]);
			}
			return properties;
		}

		/// <summary>Convert from Properties to String.</summary>
		public static string AsString(Properties props)
		{
			try
			{
				StringWriter sw = new StringWriter();
				props.Store(sw, null);
				return sw.ToString();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Convert from String to Properties.</summary>
		public static Properties FromString(string str)
		{
			try
			{
				StringReader sr = new StringReader(str);
				Properties props = new Properties();
				props.Load(sr);
				return props;
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		// printing -------------------------------------------------------------------
		public static void PrintProperties(string message, Properties properties, TextWriter stream)
		{
			if (message != null)
			{
				stream.WriteLine(message);
			}
			if (properties.IsEmpty())
			{
				stream.WriteLine("  [empty]");
			}
			else
			{
				IList<KeyValuePair<string, string>> entries = GetSortedEntries(properties);
				foreach (KeyValuePair<string, string> entry in entries)
				{
					if (!string.Empty.Equals(entry.Key))
					{
						stream.Format("  %-30s = %s%n", entry.Key, entry.Value);
					}
				}
			}
			stream.WriteLine();
		}

		public static void PrintProperties(string message, Properties properties)
		{
			PrintProperties(message, properties, System.Console.Out);
		}

		/// <summary>
		/// Tired of Properties not behaving like
		/// <c>Map&lt;String,String&gt;</c>
		/// s?  This method will solve that problem for you.
		/// </summary>
		public static IDictionary<string, string> AsMap(Properties properties)
		{
			IDictionary<string, string> map = Generics.NewHashMap();
			foreach (KeyValuePair<object, object> entry in properties)
			{
				map[(string)entry.Key] = (string)entry.Value;
			}
			return map;
		}

		public static IList<KeyValuePair<string, string>> GetSortedEntries(Properties properties)
		{
			return Maps.SortedEntries(AsMap(properties));
		}

		/// <summary>
		/// Checks to make sure that all properties specified in
		/// <paramref name="properties"/>
		/// are known to the program by checking that each simply overrides
		/// a default value.
		/// </summary>
		/// <param name="properties">Current properties</param>
		/// <param name="defaults">Default properties which lists all known keys</param>
		public static void CheckProperties(Properties properties, Properties defaults)
		{
			ICollection<string> names = Generics.NewHashSet();
			Sharpen.Collections.AddAll(names, properties.StringPropertyNames());
			foreach (string defaultName in defaults.StringPropertyNames())
			{
				names.Remove(defaultName);
			}
			if (!names.IsEmpty())
			{
				if (names.Count == 1)
				{
					throw new ArgumentException("Unknown property: " + names.GetEnumerator().Current);
				}
				else
				{
					throw new ArgumentException("Unknown properties: " + names);
				}
			}
		}

		/// <summary>
		/// Build a
		/// <c>Properties</c>
		/// object containing key-value pairs from
		/// the given data where the keys are prefixed with the given
		/// <paramref name="prefix"/>
		/// . The keys in the returned object will be stripped
		/// of their common prefix.
		/// </summary>
		/// <param name="properties">Key-value data from which to extract pairs</param>
		/// <param name="prefix">
		/// Key-value pairs where the key has this prefix will
		/// be retained in the returned
		/// <c>Properties</c>
		/// object
		/// </param>
		/// <returns>
		/// A Properties object containing those key-value pairs from
		/// <paramref name="properties"/>
		/// where the key was prefixed by
		/// <paramref name="prefix"/>
		/// . This prefix is removed from all keys in
		/// the returned structure.
		/// </returns>
		public static Properties ExtractPrefixedProperties(Properties properties, string prefix)
		{
			return ExtractPrefixedProperties(properties, prefix, false);
		}

		/// <summary>
		/// Build a
		/// <c>Properties</c>
		/// object containing key-value pairs from
		/// the given data where the keys are prefixed with the given
		/// <paramref name="prefix"/>
		/// . The keys in the returned object will be stripped
		/// of their common prefix.
		/// </summary>
		/// <param name="properties">Key-value data from which to extract pairs</param>
		/// <param name="prefix">
		/// Key-value pairs where the key has this prefix will
		/// be retained in the returned
		/// <c>Properties</c>
		/// object
		/// </param>
		/// <param name="keepPrefix">whether the prefix should be kept in the key</param>
		/// <returns>
		/// A Properties object containing those key-value pairs from
		/// <paramref name="properties"/>
		/// where the key was prefixed by
		/// <paramref name="prefix"/>
		/// . If keepPrefix is false, the prefix is removed from all keys in
		/// the returned structure.
		/// </returns>
		public static Properties ExtractPrefixedProperties(Properties properties, string prefix, bool keepPrefix)
		{
			Properties ret = new Properties();
			foreach (string keyStr in properties.StringPropertyNames())
			{
				if (keyStr.StartsWith(prefix))
				{
					if (keepPrefix)
					{
						ret.SetProperty(keyStr, properties.GetProperty(keyStr));
					}
					else
					{
						string newStr = Sharpen.Runtime.Substring(keyStr, prefix.Length);
						ret.SetProperty(newStr, properties.GetProperty(keyStr));
					}
				}
			}
			return ret;
		}

		/// <summary>
		/// Build a
		/// <c>Properties</c>
		/// object containing key-value pairs from
		/// the given properties whose keys are in a list to keep.
		/// </summary>
		/// <param name="properties">Key-value data from which to extract pairs</param>
		/// <param name="keptProperties">Key names to keep (by exact match).</param>
		/// <returns>
		/// A Properties object containing those key-value pairs from
		/// <paramref name="properties"/>
		/// where the key was in keptProperties
		/// </returns>
		public static Properties ExtractSelectedProperties(Properties properties, ICollection<string> keptProperties)
		{
			Properties ret = new Properties();
			foreach (string keyStr in properties.StringPropertyNames())
			{
				if (keptProperties.Contains(keyStr))
				{
					ret.SetProperty(keyStr, properties.GetProperty(keyStr));
				}
			}
			return ret;
		}

		/// <summary>Get the value of a property and automatically cast it to a specific type.</summary>
		/// <remarks>
		/// Get the value of a property and automatically cast it to a specific type.
		/// This differs from the original Properties.getProperty() method in that you
		/// need to specify the desired type (e.g. Double.class) and the default value
		/// is an object of that type, i.e. a double 0.0 instead of the String "0.0".
		/// </remarks>
		public static E Get<E>(Properties props, string key, E defaultValue, IType type)
		{
			string value = props.GetProperty(key);
			if (value == null)
			{
				return defaultValue;
			}
			else
			{
				return (E)MetaClass.Cast(value, type);
			}
		}

		/// <summary>Get the value of a property.</summary>
		/// <remarks>
		/// Get the value of a property.  If the key is not present, returns defaultValue.
		/// This is just equivalent to props.getProperty(key, defaultValue).
		/// </remarks>
		public static string GetString(Properties props, string key, string defaultValue)
		{
			return props.GetProperty(key, defaultValue);
		}

		/// <summary>Load an integer property.</summary>
		/// <remarks>Load an integer property.  If the key is not present, returns 0.</remarks>
		public static int GetInt(Properties props, string key)
		{
			return GetInt(props, key, 0);
		}

		/// <summary>Load an integer property.</summary>
		/// <remarks>Load an integer property.  If the key is not present, returns defaultValue.</remarks>
		public static int GetInt(Properties props, string key, int defaultValue)
		{
			string value = props.GetProperty(key);
			if (value != null)
			{
				return System.Convert.ToInt32(value);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>Load an integer property as a long.</summary>
		/// <remarks>
		/// Load an integer property as a long.
		/// If the key is not present, returns defaultValue.
		/// </remarks>
		public static long GetLong(Properties props, string key, long defaultValue)
		{
			string value = props.GetProperty(key);
			if (value != null)
			{
				return long.Parse(value);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>Load a double property.</summary>
		/// <remarks>Load a double property.  If the key is not present, returns 0.0.</remarks>
		public static double GetDouble(Properties props, string key)
		{
			return GetDouble(props, key, 0.0);
		}

		/// <summary>Load a double property.</summary>
		/// <remarks>Load a double property.  If the key is not present, returns defaultValue.</remarks>
		public static double GetDouble(Properties props, string key, double defaultValue)
		{
			string value = props.GetProperty(key);
			if (value != null)
			{
				return double.ParseDouble(value);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>Load a boolean property.</summary>
		/// <remarks>Load a boolean property.  If the key is not present, returns false.</remarks>
		public static bool GetBool(Properties props, string key)
		{
			return GetBool(props, key, false);
		}

		/// <summary>Load a boolean property.</summary>
		/// <remarks>Load a boolean property.  If the key is not present, returns defaultValue.</remarks>
		public static bool GetBool(Properties props, string key, bool defaultValue)
		{
			string value = props.GetProperty(key);
			if (value != null)
			{
				return bool.ParseBoolean(value);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>Loads a comma-separated list of integers from Properties.</summary>
		/// <remarks>Loads a comma-separated list of integers from Properties.  The list cannot include any whitespace.</remarks>
		public static int[] GetIntArray(Properties props, string key)
		{
			int[] result = MetaClass.Cast(props.GetProperty(key), typeof(int[]));
			return ArrayUtils.ToPrimitive(result);
		}

		/// <summary>Loads a comma-separated list of doubles from Properties.</summary>
		/// <remarks>Loads a comma-separated list of doubles from Properties.  The list cannot include any whitespace.</remarks>
		public static double[] GetDoubleArray(Properties props, string key)
		{
			double[] result = MetaClass.Cast(props.GetProperty(key), typeof(double[]));
			return ArrayUtils.ToPrimitive(result);
		}

		/// <summary>Loads a comma-separated list of strings from Properties.</summary>
		/// <remarks>
		/// Loads a comma-separated list of strings from Properties.  Commas may be quoted if needed, e.g.:
		/// property1 = value1,value2,"a quoted value",'another quoted value'
		/// getStringArray(props, "property1") should return the same thing as
		/// new String[] { "value1", "value2", "a quoted value", "another quoted value" };
		/// </remarks>
		/// <returns>An array of Strings value for the given key in the Properties. May be empty. Never null.</returns>
		public static string[] GetStringArray(Properties props, string key)
		{
			string val = props.GetProperty(key);
			string[] results;
			if (val == null)
			{
				results = StringUtils.EmptyStringArray;
			}
			else
			{
				results = StringUtils.DecodeArray(val);
				if (results == null)
				{
					results = StringUtils.EmptyStringArray;
				}
			}
			// System.out.printf("Called with prop key and value %s %s, returned %s.%n", key, val, Arrays.toString(results));
			return results;
		}

		public static string[] GetStringArray(Properties props, string key, string[] defaults)
		{
			string[] results = MetaClass.Cast(props.GetProperty(key), typeof(string[]));
			if (results == null)
			{
				results = defaults;
			}
			return results;
		}

		// add ovp's key values to bp, overwrite if necessary , this is a helper
		public static Properties OverWriteProperties(Properties bp, Properties ovp)
		{
			foreach (string propertyName in ovp.StringPropertyNames())
			{
				bp.SetProperty(propertyName, ovp.GetProperty(propertyName));
			}
			return bp;
		}

		//  add ovp's key values to bp, don't overwrite if there is already a value
		public static Properties NoClobberWriteProperties(Properties bp, Properties ovp)
		{
			foreach (string propertyName in ovp.StringPropertyNames())
			{
				if (bp.Contains(propertyName))
				{
					continue;
				}
				bp.SetProperty(propertyName, ovp.GetProperty(propertyName));
			}
			return bp;
		}

		public class Property
		{
			private readonly string name;

			private readonly string defaultValue;

			private readonly string description;

			public Property(string name, string defaultValue, string description)
			{
				this.name = name;
				this.defaultValue = defaultValue;
				this.description = description;
			}

			public virtual string Name()
			{
				return name;
			}

			public virtual string DefaultValue()
			{
				return defaultValue;
			}
		}

		// This is CoreNLP-specific-ish and now unused. Delete?
		public static string GetSignature(string name, Properties properties, PropertiesUtils.Property[] supportedProperties)
		{
			string prefix = (name != null && !name.IsEmpty()) ? name + '.' : string.Empty;
			// keep track of all relevant properties for this annotator here!
			StringBuilder sb = new StringBuilder();
			foreach (PropertiesUtils.Property p in supportedProperties)
			{
				string pname = prefix + p.Name();
				string pvalue = properties.GetProperty(pname, p.DefaultValue());
				sb.Append(pname).Append(':').Append(pvalue).Append(';');
			}
			return sb.ToString();
		}

		public static string GetSignature(string name, Properties properties)
		{
			string[] prefixes = new string[] { (name != null && !name.IsEmpty()) ? name + '.' : string.Empty };
			// TODO(gabor) This is a hack, as tokenize and ssplit depend on each other so heavily
			if ("tokenize".Equals(name) || "ssplit".Equals(name))
			{
				prefixes = new string[] { "tokenize", "ssplit" };
			}
			// TODO [chris 2017]: Another hack. Traditionally, we have called the cleanxml properties clean!
			if ("clean".Equals(name) || "cleanxml".Equals(name))
			{
				prefixes = new string[] { "clean", "cleanxml" };
			}
			if ("mention".Equals(name))
			{
				prefixes = new string[] { "mention", "coref" };
			}
			if ("ner".Equals(name))
			{
				prefixes = new string[] { "ner", "sutime" };
			}
			Properties propertiesCopy = new Properties();
			propertiesCopy.PutAll(properties);
			// handle special case of implied properties (e.g. sentiment implies parse should set parse.binaryTrees = true
			// TODO(jb) This is a hack: handle implied need for binary trees if sentiment annotator is present
			ICollection<string> annoNames = Generics.NewHashSet(Arrays.AsList(properties.GetProperty("annotators", string.Empty).Split("[, \t]+")));
			if ("parse".Equals(name) && annoNames.Contains("sentiment") && !properties.Contains("parse.binaryTrees"))
			{
				propertiesCopy.SetProperty("parse.binaryTrees", "true");
			}
			// keep track of all relevant properties for this annotator here!
			StringBuilder sb = new StringBuilder();
			foreach (string pname in propertiesCopy.StringPropertyNames())
			{
				foreach (string prefix in prefixes)
				{
					if (pname.StartsWith(prefix))
					{
						string pvalue = propertiesCopy.GetProperty(pname);
						sb.Append(pname).Append(':').Append(pvalue).Append(';');
					}
				}
			}
			return sb.ToString();
		}
	}
}
