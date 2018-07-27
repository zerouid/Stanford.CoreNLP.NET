using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A CoreLabel represents a single word with ancillary information
	/// attached using CoreAnnotations.
	/// </summary>
	/// <remarks>
	/// A CoreLabel represents a single word with ancillary information
	/// attached using CoreAnnotations.
	/// A CoreLabel also provides convenient methods to access tags,
	/// lemmas, etc. (if the proper annotations are set).
	/// <p>
	/// A CoreLabel is a Map from keys (which are Class objects) to values,
	/// whose type is determined by the key.  That is, it is a heterogeneous
	/// typesafe Map (see Josh Bloch, Effective Java, 2nd edition).
	/// <p>
	/// The CoreLabel class in particular bridges the gap between old-style JavaNLP
	/// Labels and the new CoreMap infrastructure.  Instances of this class can be
	/// used (almost) anywhere that the now-defunct FeatureLabel family could be
	/// used.  This data structure is backed by an
	/// <see cref="Edu.Stanford.Nlp.Util.ArrayCoreMap"/>
	/// .
	/// </remarks>
	/// <author>dramage</author>
	/// <author>rafferty</author>
	[System.Serializable]
	public class CoreLabel : ArrayCoreMap, IAbstractCoreLabel, IHasCategory
	{
		private const long serialVersionUID = 2L;

		/// <summary>Default constructor, calls super()</summary>
		public CoreLabel()
			: base()
		{
		}

		/// <summary>
		/// Initializes this CoreLabel, pre-allocating arrays to hold
		/// up to capacity key,value pairs.
		/// </summary>
		/// <remarks>
		/// Initializes this CoreLabel, pre-allocating arrays to hold
		/// up to capacity key,value pairs.  This array will grow if necessary.
		/// </remarks>
		/// <param name="capacity">Initial capacity of object in key,value pairs</param>
		public CoreLabel(int capacity)
			: base(capacity)
		{
		}

		/// <summary>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// CoreLabel.
		/// </summary>
		/// <remarks>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// CoreLabel.  It copies the contents of the other CoreLabel.
		/// <i>Implementation note:</i> this is a the same as the constructor
		/// that takes a CoreMap, but is needed to ensure unique most specific
		/// type inference for selecting a constructor at compile-time.
		/// </remarks>
		/// <param name="label">The CoreLabel to copy</param>
		public CoreLabel(Edu.Stanford.Nlp.Ling.CoreLabel label)
			: this((ICoreMap)label)
		{
		}

		/// <summary>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// CoreMap.
		/// </summary>
		/// <remarks>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// CoreMap.  It copies the contents of the other CoreMap.
		/// </remarks>
		/// <param name="label">The CoreMap to copy</param>
		public CoreLabel(ICoreMap label)
			: base(label.Size())
		{
			/* , HasContext */
			// /**
			//  * Should warnings be printed when converting from MapLabel family.
			//  */
			// private static final boolean VERBOSE = false;
			IConsumer<Type> savedListener = ArrayCoreMap.listener;
			// don't listen to the clone operation
			ArrayCoreMap.listener = null;
			foreach (Type key in label.KeySet())
			{
				Set(key, label.Get(key));
			}
			ArrayCoreMap.listener = savedListener;
		}

		/// <summary>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// label.
		/// </summary>
		/// <remarks>
		/// Returns a new CoreLabel instance based on the contents of the given
		/// label.   Warning: The behavior of this method is a bit disjunctive!
		/// If label is a CoreMap (including CoreLabel), then its entire
		/// contents is copied into this label.
		/// If label is an IndexedWord, then the backing label is copied over
		/// entirely.
		/// But, otherwise, just the
		/// value() and word iff it implements
		/// <see cref="IHasWord"/>
		/// is copied.
		/// </remarks>
		/// <param name="label">Basis for this label</param>
		public CoreLabel(ILabel label)
			: base(0)
		{
			if (label is ICoreMap)
			{
				ICoreMap cl = (ICoreMap)label;
				SetCapacity(cl.Size());
				foreach (Type key in cl.KeySet())
				{
					Set(key, cl.Get(key));
				}
			}
			else
			{
				if (label is IndexedWord)
				{
					ICoreMap cl = ((IndexedWord)label).BackingLabel();
					SetCapacity(cl.Size());
					foreach (Type key in cl.KeySet())
					{
						Set(key, cl.Get(key));
					}
				}
				else
				{
					if (label is IHasWord)
					{
						SetWord(((IHasWord)label).Word());
					}
					SetValue(label.Value());
				}
			}
		}

		/// <summary>
		/// This constructor attempts to parse the String keys
		/// into Class keys.
		/// </summary>
		/// <remarks>
		/// This constructor attempts to parse the String keys
		/// into Class keys.  It's mainly useful for reading from
		/// a file.  A best effort attempt is made to correctly
		/// parse the keys according to the String lookup function
		/// in
		/// <see cref="CoreAnnotations"/>
		/// .
		/// </remarks>
		/// <param name="keys">Array of Strings that are class names</param>
		/// <param name="values">Array of values (as String)</param>
		public CoreLabel(string[] keys, string[] values)
			: base(keys.Length)
		{
			//this.map = new ArrayCoreMap();
			InitFromStrings(keys, values);
		}

		/// <summary>This constructor attempts uses preparsed Class keys.</summary>
		/// <remarks>
		/// This constructor attempts uses preparsed Class keys.
		/// It's mainly useful for reading from a file.
		/// </remarks>
		/// <param name="keys">Array of key classes</param>
		/// <param name="values">Array of values (as String)</param>
		public CoreLabel(Type[] keys, string[] values)
			: base(keys.Length)
		{
			//this.map = new ArrayCoreMap();
			InitFromStrings(keys, values);
		}

		/// <summary>This is provided as a simple way to make a CoreLabel for a word from a String.</summary>
		/// <remarks>
		/// This is provided as a simple way to make a CoreLabel for a word from a String.
		/// It's often useful in fixup or test code. It sets all three of the Text, OriginalText,
		/// and Value annotations to the given value.
		/// </remarks>
		/// <param name="word">The word string to make a CoreLabel for</param>
		/// <returns>A CoreLabel for this word string</returns>
		public static Edu.Stanford.Nlp.Ling.CoreLabel WordFromString(string word)
		{
			Edu.Stanford.Nlp.Ling.CoreLabel cl = new Edu.Stanford.Nlp.Ling.CoreLabel();
			cl.SetWord(word);
			cl.SetOriginalText(word);
			cl.SetValue(word);
			return cl;
		}

		/// <summary>Class that all "generic" annotations extend.</summary>
		/// <remarks>
		/// Class that all "generic" annotations extend.
		/// This allows you to read in arbitrary values from a file as features, for example.
		/// </remarks>
		public interface IGenericAnnotation<T> : ICoreAnnotation<T>
		{
		}

		public static readonly IDictionary<string, Type> genericKeys = Generics.NewHashMap();

		public static readonly IDictionary<Type, string> genericValues = Generics.NewHashMap();

		//Unchecked is below because eclipse can't handle the level of type inference if we correctly parametrize GenericAnnotation with String
		private void InitFromStrings(string[] keys, string[] values)
		{
			if (keys.Length != values.Length)
			{
				throw new NotSupportedException("Argument array lengths differ: " + Arrays.ToString(keys) + " vs. " + Arrays.ToString(values));
			}
			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				string value = values[i];
				Type coreKeyClass = AnnotationLookup.ToCoreKey(key);
				//now work with the key we got above
				if (coreKeyClass == null)
				{
					if (key != null)
					{
						throw new NotSupportedException("Unknown key " + key);
					}
				}
				else
				{
					// It used to be that the following code let you put unknown keys
					// in the CoreLabel.  However, you can't create classes dynamically
					// at run time, which meant only one of these classes could ever
					// exist, which meant multiple unknown keys would clobber each
					// other and be very annoying.  It's easier just to not allow
					// it at all.
					// If it becomes possible to create classes dynamically,
					// we could add this code back.
					//if(genericKeys.containsKey(key)) {
					//  this.set(genericKeys.get(key), value);
					//} else {
					//  GenericAnnotation<String> newKey = new GenericAnnotation<String>() {
					//    public Class<String> getType() { return String.class;} };
					//  this.set(newKey.getClass(), values[i]);
					//  genericKeys.put(keys[i], newKey.getClass());
					//  genericValues.put(newKey.getClass(), keys[i]);
					//}
					// unknown key; ignore
					//if (VERBOSE) {
					//  log.info("CORE: CoreLabel.fromAbstractMapLabel: " +
					//      "Unknown key "+key);
					//}
					try
					{
						Type valueClass = AnnotationLookup.GetValueType(coreKeyClass);
						if (valueClass.Equals(typeof(string)))
						{
							this.Set(coreKeyClass, values[i]);
						}
						else
						{
							if (valueClass == typeof(int))
							{
								this.Set(coreKeyClass, System.Convert.ToInt32(values[i]));
							}
							else
							{
								if (valueClass == typeof(double))
								{
									this.Set(coreKeyClass, double.ParseDouble(values[i]));
								}
								else
								{
									if (valueClass == typeof(long))
									{
										this.Set(coreKeyClass, long.Parse(values[i]));
									}
									else
									{
										throw new Exception("Can't handle " + valueClass);
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						// unexpected value type
						throw new NotSupportedException("CORE: CoreLabel.initFromStrings: " + "Bad type for " + key + ". Value was: " + value + "; expected " + AnnotationLookup.GetValueType(coreKeyClass), e);
					}
				}
			}
		}

		public static Type[] ParseStringKeys(string[] keys)
		{
			Type[] classes = new Type[keys.Length];
			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				classes[i] = AnnotationLookup.ToCoreKey(key);
				// now work with the key we got above
				if (classes[i] == null)
				{
					throw new NotSupportedException("Unknown key " + key);
				}
			}
			return classes;
		}

		private void InitFromStrings(Type[] keys, string[] values)
		{
			if (keys.Length != values.Length)
			{
				throw new NotSupportedException("Argument array lengths differ: " + Arrays.ToString(keys) + " vs. " + Arrays.ToString(values));
			}
			for (int i = 0; i < keys.Length; i++)
			{
				Type coreKeyClass = keys[i];
				string value = values[i];
				try
				{
					Type valueClass = AnnotationLookup.GetValueType(coreKeyClass);
					if (valueClass.Equals(typeof(string)))
					{
						this.Set(coreKeyClass, values[i]);
					}
					else
					{
						if (valueClass == typeof(int))
						{
							this.Set(coreKeyClass, System.Convert.ToInt32(values[i]));
						}
						else
						{
							if (valueClass == typeof(double))
							{
								this.Set(coreKeyClass, double.ParseDouble(values[i]));
							}
							else
							{
								if (valueClass == typeof(long))
								{
									this.Set(coreKeyClass, long.Parse(values[i]));
								}
								else
								{
									throw new Exception("Can't handle " + valueClass);
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					// unexpected value type
					throw new NotSupportedException("CORE: CoreLabel.initFromStrings: " + "Bad type for " + coreKeyClass.GetSimpleName() + ". Value was: " + value + "; expected " + AnnotationLookup.GetValueType(coreKeyClass), e);
				}
			}
		}

		private class CoreLabelFactory : ILabelFactory
		{
			public virtual ILabel NewLabel(string labelStr)
			{
				CoreLabel label = new CoreLabel();
				label.SetValue(labelStr);
				return label;
			}

			public virtual ILabel NewLabel(string labelStr, int options)
			{
				return NewLabel(labelStr);
			}

			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				if (oldLabel is CoreLabel)
				{
					return new CoreLabel((CoreLabel)oldLabel);
				}
				else
				{
					//Map the old interfaces to the correct key/value pairs
					//Don't need to worry about HasIndex, which doesn't appear in any legacy code
					CoreLabel label = new CoreLabel();
					if (oldLabel is IHasWord)
					{
						label.SetWord(((IHasWord)oldLabel).Word());
					}
					if (oldLabel is IHasTag)
					{
						label.SetTag(((IHasTag)oldLabel).Tag());
					}
					if (oldLabel is IHasOffset)
					{
						label.SetBeginPosition(((IHasOffset)oldLabel).BeginPosition());
						label.SetEndPosition(((IHasOffset)oldLabel).EndPosition());
					}
					if (oldLabel is IHasCategory)
					{
						label.SetCategory(((IHasCategory)oldLabel).Category());
					}
					if (oldLabel is IHasIndex)
					{
						label.SetIndex(((IHasIndex)oldLabel).Index());
					}
					label.SetValue(oldLabel.Value());
					return label;
				}
			}

			public virtual ILabel NewLabelFromString(string encodedLabelStr)
			{
				throw new NotSupportedException("This code branch left blank" + " because we do not understand what this method should do.");
			}
		}

		/// <summary>Return a factory for this kind of label</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return new CoreLabel.CoreLabelFactory();
		}

		/// <summary><inheritDoc/></summary>
		public virtual ILabelFactory LabelFactory()
		{
			return CoreLabel.Factory();
		}

		/// <summary><inheritDoc/></summary>
		public virtual string GetString<Key>()
			where Key : TypesafeMap.IKey<string>
		{
			System.Type key = typeof(KEY);
			return this.GetString(key, string.Empty);
		}

		public virtual string GetString<Key>(string def)
			where Key : TypesafeMap.IKey<string>
		{
			System.Type key = typeof(KEY);
			string value = Get(key);
			if (value == null)
			{
				return def;
			}
			return value;
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetFromString(string labelStr)
		{
			throw new NotSupportedException("Cannot set from string");
		}

		/// <summary><inheritDoc/></summary>
		public void SetValue(string value)
		{
			Set(typeof(CoreAnnotations.ValueAnnotation), value);
		}

		/// <summary><inheritDoc/></summary>
		public string Value()
		{
			return Get(typeof(CoreAnnotations.ValueAnnotation));
		}

		/// <summary>Set the word value for the label.</summary>
		/// <remarks>
		/// Set the word value for the label.  Also, clears the lemma, since
		/// that may have changed if the word changed.
		/// </remarks>
		public virtual void SetWord(string word)
		{
			string originalWord = Get(typeof(CoreAnnotations.TextAnnotation));
			Set(typeof(CoreAnnotations.TextAnnotation), word);
			// Pado feb 09: if you change the word, delete the lemma.
			// Gabor dec 2012: check if there was a real change -- this remove is actually rather expensive if it gets called a lot
			// todo [cdm 2015]: probably no one now knows why this was even needed, but maybe it should just be removed. It's kind of weird.
			if (word != null && !word.Equals(originalWord) && ContainsKey(typeof(CoreAnnotations.LemmaAnnotation)))
			{
				Remove(typeof(CoreAnnotations.LemmaAnnotation));
			}
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Word()
		{
			return Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetTag(string tag)
		{
			Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), tag);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Tag()
		{
			return Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetCategory(string category)
		{
			Set(typeof(CoreAnnotations.CategoryAnnotation), category);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Category()
		{
			return Get(typeof(CoreAnnotations.CategoryAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetAfter(string after)
		{
			Set(typeof(CoreAnnotations.AfterAnnotation), after);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string After()
		{
			return GetString<CoreAnnotations.AfterAnnotation>();
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetBefore(string before)
		{
			Set(typeof(CoreAnnotations.BeforeAnnotation), before);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Before()
		{
			return GetString<CoreAnnotations.BeforeAnnotation>();
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetOriginalText(string originalText)
		{
			Set(typeof(CoreAnnotations.OriginalTextAnnotation), originalText);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string OriginalText()
		{
			return GetString<CoreAnnotations.OriginalTextAnnotation>();
		}

		/// <summary><inheritDoc/></summary>
		public virtual string DocID()
		{
			return Get(typeof(CoreAnnotations.DocIDAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetDocID(string docID)
		{
			Set(typeof(CoreAnnotations.DocIDAnnotation), docID);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Ner()
		{
			return Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetNER(string ner)
		{
			Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), ner);
		}

		/// <summary><inheritDoc/></summary>
		public virtual string Lemma()
		{
			return Get(typeof(CoreAnnotations.LemmaAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetLemma(string lemma)
		{
			Set(typeof(CoreAnnotations.LemmaAnnotation), lemma);
		}

		/// <summary>Get value of IsNewlineAnnotation</summary>
		/// <returns>value of IsNewlineAnnotation</returns>
		public virtual bool IsNewline()
		{
			return Get(typeof(CoreAnnotations.IsNewlineAnnotation));
		}

		/// <summary><inheritDoc/></summary>
		public virtual int Index()
		{
			int n = Get(typeof(CoreAnnotations.IndexAnnotation));
			if (n == null)
			{
				return -1;
			}
			return n;
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetIndex(int index)
		{
			Set(typeof(CoreAnnotations.IndexAnnotation), index);
		}

		/// <summary><inheritDoc/></summary>
		public virtual int SentIndex()
		{
			int n = Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
			if (n == null)
			{
				return -1;
			}
			return n;
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetSentIndex(int sentIndex)
		{
			Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentIndex);
		}

		/// <summary><inheritDoc/></summary>
		public virtual int BeginPosition()
		{
			int i = Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			if (i != null)
			{
				return i;
			}
			return -1;
		}

		/// <summary><inheritDoc/></summary>
		public virtual int EndPosition()
		{
			int i = Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			if (i != null)
			{
				return i;
			}
			return -1;
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetBeginPosition(int beginPos)
		{
			Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), beginPos);
		}

		/// <summary><inheritDoc/></summary>
		public virtual void SetEndPosition(int endPos)
		{
			Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), endPos);
		}

		/// <summary>Tag separator to use by default</summary>
		public const string TagSeparator = "/";

		public enum OutputFormat
		{
			ValueIndex,
			Value,
			ValueTag,
			ValueTagIndex,
			Map,
			ValueMap,
			ValueIndexMap,
			Word,
			WordIndex,
			ValueTagNer,
			LemmaIndex,
			All
		}

		public static readonly CoreLabel.OutputFormat DefaultFormat = CoreLabel.OutputFormat.ValueIndex;

		public override string ToString()
		{
			return ToString(DefaultFormat);
		}

		/// <summary>Returns a formatted string representing this label.</summary>
		/// <remarks>
		/// Returns a formatted string representing this label.  The
		/// desired format is passed in as a
		/// <c>String</c>
		/// .
		/// Currently supported formats include:
		/// <ul>
		/// <li>"value": just prints the value</li>
		/// <li>"{map}": prints the complete map</li>
		/// <li>"value{map}": prints the value followed by the contained
		/// map (less the map entry containing key
		/// <c>CATEGORY_KEY</c>
		/// )</li>
		/// <li>"value-index": extracts a value and an integer index from
		/// the contained map using keys
		/// <c>INDEX_KEY</c>
		/// ,
		/// respectively, and prints them with a hyphen in between</li>
		/// <li>"value-tag"
		/// <li>"value-tag-index"
		/// <li>"value-index{map}": a combination of the above; the index is
		/// displayed first and then not shown in the map that is displayed</li>
		/// <li>"word": Just the value of HEAD_WORD_KEY in the map</li>
		/// </ul>
		/// <p>
		/// Map is printed in alphabetical order of keys.
		/// </remarks>
		public virtual string ToString(CoreLabel.OutputFormat format)
		{
			StringBuilder buf = new StringBuilder();
			switch (format)
			{
				case CoreLabel.OutputFormat.Value:
				{
					buf.Append(Value());
					break;
				}

				case CoreLabel.OutputFormat.Map:
				{
					IDictionary map2 = new SortedList();
					foreach (Type key in this.KeySet())
					{
						map2[key.FullName] = Get(key);
					}
					buf.Append(map2);
					break;
				}

				case CoreLabel.OutputFormat.ValueMap:
				{
					buf.Append(Value());
					IDictionary map2 = new SortedList(asClassComparator);
					foreach (Type key in this.KeySet())
					{
						map2[key] = Get(key);
					}
					Sharpen.Collections.Remove(map2, typeof(CoreAnnotations.ValueAnnotation));
					buf.Append(map2);
					break;
				}

				case CoreLabel.OutputFormat.ValueIndex:
				{
					buf.Append(Value());
					int index = this.Get(typeof(CoreAnnotations.IndexAnnotation));
					if (index != null)
					{
						buf.Append('-').Append((index));
					}
					break;
				}

				case CoreLabel.OutputFormat.ValueTag:
				{
					buf.Append(Value());
					string tag = Tag();
					if (tag != null)
					{
						buf.Append(TagSeparator).Append(tag);
					}
					break;
				}

				case CoreLabel.OutputFormat.ValueTagIndex:
				{
					buf.Append(Value());
					string tag = Tag();
					if (tag != null)
					{
						buf.Append(TagSeparator).Append(tag);
					}
					int index = this.Get(typeof(CoreAnnotations.IndexAnnotation));
					if (index != null)
					{
						buf.Append('-').Append((index));
					}
					break;
				}

				case CoreLabel.OutputFormat.ValueIndexMap:
				{
					buf.Append(Value());
					int index = this.Get(typeof(CoreAnnotations.IndexAnnotation));
					if (index != null)
					{
						buf.Append('-').Append((index));
					}
					IDictionary<string, object> map2 = new SortedDictionary<string, object>();
					foreach (Type key in this.KeySet())
					{
						string cls = key.FullName;
						// special shortening of all the Annotation classes
						int idx = cls.IndexOf('$');
						if (idx >= 0)
						{
							cls = Sharpen.Runtime.Substring(cls, idx + 1);
						}
						map2[cls] = this.Get(key);
					}
					Sharpen.Collections.Remove(map2, "IndexAnnotation");
					Sharpen.Collections.Remove(map2, "ValueAnnotation");
					if (!map2.IsEmpty())
					{
						buf.Append(map2);
					}
					break;
				}

				case CoreLabel.OutputFormat.Word:
				{
					// TODO: maybe we should unify word() and value(). [cdm 2015] I think not, rather maybe remove value and redefine category.
					buf.Append(Word());
					break;
				}

				case CoreLabel.OutputFormat.WordIndex:
				{
					buf.Append(this.Get(typeof(CoreAnnotations.TextAnnotation)));
					int index = this.Get(typeof(CoreAnnotations.IndexAnnotation));
					if (index != null)
					{
						buf.Append('-').Append((index));
					}
					break;
				}

				case CoreLabel.OutputFormat.ValueTagNer:
				{
					buf.Append(Value());
					string tag = Tag();
					if (tag != null)
					{
						buf.Append(TagSeparator).Append(tag);
					}
					if (Ner() != null)
					{
						buf.Append(TagSeparator).Append(Ner());
					}
					break;
				}

				case CoreLabel.OutputFormat.LemmaIndex:
				{
					buf.Append(Lemma());
					int index_1 = this.Get(typeof(CoreAnnotations.IndexAnnotation));
					if (index_1 != null)
					{
						buf.Append('-').Append((index_1));
					}
					break;
				}

				case CoreLabel.OutputFormat.All:
				{
					foreach (Type en in this.KeySet())
					{
						buf.Append(';').Append(en).Append(':').Append(this.Get(en));
					}
					break;
				}

				default:
				{
					throw new ArgumentException("Unknown format " + format);
				}
			}
			return buf.ToString();
		}

		private static readonly IComparator<Type> asClassComparator = IComparer.Comparing(null);
	}
}
