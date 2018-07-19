using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>
	/// Primarily for debugging, PrettyLogger helps you dump various collection
	/// objects in a reasonably structured way via Redwood logging.
	/// </summary>
	/// <remarks>
	/// Primarily for debugging, PrettyLogger helps you dump various collection
	/// objects in a reasonably structured way via Redwood logging. It has support
	/// for many built in collection types (Mapping, Iterable, arrays, Properties) as
	/// well as anything that implements PrettyLoggable.
	/// </remarks>
	/// <seealso cref="IPrettyLoggable"/>
	/// <author>David McClosky</author>
	/// <author>Gabor Angeli (+ primitive arrays; dictionaries)</author>
	public class PrettyLogger
	{
		private static readonly Redwood.RedwoodChannels DefaultChannels = new Redwood.RedwoodChannels(Redwood.Force);

		/// <summary>Static class.</summary>
		private PrettyLogger()
		{
		}

		// TODO Should have an optional maximum depth, perhaps
		/*
		* Main entry methods and utilities
		*/
		/// <summary>Pretty log an object.</summary>
		/// <remarks>
		/// Pretty log an object. It will be logged to the default channel. Its class
		/// name will be used as a description.
		/// </remarks>
		/// <param name="obj">the object to be pretty logged</param>
		public static void Log(object obj)
		{
			Log(obj.GetType().GetSimpleName(), obj);
		}

		/// <summary>Pretty log an object along with its description.</summary>
		/// <remarks>
		/// Pretty log an object along with its description. It will be logged to the
		/// default channel.
		/// </remarks>
		/// <param name="description">denote the object in the logs (via a track name, etc.).</param>
		/// <param name="obj">the object to be pretty logged</param>
		public static void Log(string description, object obj)
		{
			Log(DefaultChannels, description, obj);
		}

		/// <summary>Pretty log an object.</summary>
		/// <remarks>Pretty log an object. Its class name will be used as a description.</remarks>
		/// <param name="channels">the channels to pretty log to</param>
		/// <param name="obj">the object to be pretty logged</param>
		public static void Log(Redwood.RedwoodChannels channels, object obj)
		{
			Log(channels, obj.GetType().GetSimpleName(), obj);
		}

		/// <summary>Pretty log an object.</summary>
		/// <param name="channels">the channels to pretty log to</param>
		/// <param name="description">denote the object in the logs (via a track name, etc.).</param>
		/// <param name="obj">the object to be pretty logged</param>
		public static void Log<T>(Redwood.RedwoodChannels channels, string description, object obj)
		{
			// TODO perhaps some reflection magic can simplify this process?
			if (obj is IDictionary)
			{
				Log(channels, description, (IDictionary)obj);
			}
			else
			{
				if (obj is IPrettyLoggable)
				{
					((IPrettyLoggable)obj).PrettyLog(channels, description);
				}
				else
				{
					if (obj is Dictionary)
					{
						Log(channels, description, (Dictionary)obj);
					}
					else
					{
						if (obj is IEnumerable)
						{
							Log(channels, description, (IEnumerable)obj);
						}
						else
						{
							if (obj.GetType().IsArray)
							{
								object[] arrayCopy;
								// the array to log
								if (obj.GetType().GetElementType().IsPrimitive)
								{
									//(case: a primitive array)
									Type componentClass = obj.GetType().GetElementType();
									if (typeof(bool).IsAssignableFrom(componentClass))
									{
										arrayCopy = new object[((bool[])obj).Length];
										for (int i = 0; i < arrayCopy.Length; i++)
										{
											arrayCopy[i] = ((bool[])obj)[i];
										}
									}
									else
									{
										if (typeof(byte).IsAssignableFrom(componentClass))
										{
											arrayCopy = new object[((byte[])obj).Length];
											for (int i = 0; i < arrayCopy.Length; i++)
											{
												arrayCopy[i] = ((byte[])obj)[i];
											}
										}
										else
										{
											if (typeof(char).IsAssignableFrom(componentClass))
											{
												arrayCopy = new object[((char[])obj).Length];
												for (int i = 0; i < arrayCopy.Length; i++)
												{
													arrayCopy[i] = ((char[])obj)[i];
												}
											}
											else
											{
												if (typeof(short).IsAssignableFrom(componentClass))
												{
													arrayCopy = new object[((short[])obj).Length];
													for (int i = 0; i < arrayCopy.Length; i++)
													{
														arrayCopy[i] = ((short[])obj)[i];
													}
												}
												else
												{
													if (typeof(int).IsAssignableFrom(componentClass))
													{
														arrayCopy = new object[((int[])obj).Length];
														for (int i = 0; i < arrayCopy.Length; i++)
														{
															arrayCopy[i] = ((int[])obj)[i];
														}
													}
													else
													{
														if (typeof(long).IsAssignableFrom(componentClass))
														{
															arrayCopy = new object[((long[])obj).Length];
															for (int i = 0; i < arrayCopy.Length; i++)
															{
																arrayCopy[i] = ((long[])obj)[i];
															}
														}
														else
														{
															if (typeof(float).IsAssignableFrom(componentClass))
															{
																arrayCopy = new object[((float[])obj).Length];
																for (int i = 0; i < arrayCopy.Length; i++)
																{
																	arrayCopy[i] = ((float[])obj)[i];
																}
															}
															else
															{
																if (typeof(double).IsAssignableFrom(componentClass))
																{
																	arrayCopy = new object[((double[])obj).Length];
																	for (int i = 0; i < arrayCopy.Length; i++)
																	{
																		arrayCopy[i] = ((double[])obj)[i];
																	}
																}
																else
																{
																	throw new InvalidOperationException("I forgot about the primitive class: " + componentClass);
																}
															}
														}
													}
												}
											}
										}
									}
								}
								else
								{
									//(case: a regular array)
									arrayCopy = (T[])obj;
								}
								Log(channels, description, arrayCopy);
							}
							else
							{
								if (!description.Equals(string.Empty))
								{
									description += ": ";
								}
								channels.Log(description + obj);
							}
						}
					}
				}
			}
		}

		/// <summary>Returns true if an object has special logic for pretty logging (e.g.</summary>
		/// <remarks>
		/// Returns true if an object has special logic for pretty logging (e.g.
		/// implements PrettyLoggable). If so, we ask it to pretty log itself. If not,
		/// we can safely use its toString() in logs.
		/// </remarks>
		/// <param name="obj">The object to test</param>
		/// <returns>true if the object is dispatchable</returns>
		public static bool Dispatchable(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			return obj is IPrettyLoggable || obj is IDictionary || obj is Dictionary || obj is IEnumerable || obj.GetType().IsArray;
		}

		/*
		* Mappings
		*/
		private static void Log<K, V>(Redwood.RedwoodChannels channels, string description, IDictionary<K, V> mapping)
		{
			Redwood.StartTrack(description);
			if (mapping == null)
			{
				channels.Log("(mapping is null)");
			}
			else
			{
				if (mapping.IsEmpty())
				{
					channels.Log("(empty)");
				}
				else
				{
					// convert keys to sorted list, if possible
					IList<K> keys = new LinkedList<K>();
					foreach (K key in mapping.Keys)
					{
						keys.Add(key);
					}
					keys.Sort(null);
					// log key/value pairs
					int entryCounter = 0;
					foreach (K key_1 in keys)
					{
						V value = mapping[key_1];
						if (!Dispatchable(key_1) && Dispatchable(value))
						{
							Log(channels, key_1.ToString(), value);
						}
						else
						{
							if (Dispatchable(key_1) || Dispatchable(value))
							{
								Redwood.StartTrack("Entry " + entryCounter);
								if (Dispatchable(key_1))
								{
									Log(channels, "Key", key_1);
								}
								else
								{
									channels.Logf("Key %s", key_1);
								}
								if (Dispatchable(value))
								{
									Log(channels, "Value", value);
								}
								else
								{
									channels.Logf("Value %s", value);
								}
								Redwood.EndTrack("Entry " + entryCounter);
							}
							else
							{
								channels.Logf("%s = %s", key_1, value);
							}
						}
						entryCounter++;
					}
				}
			}
			Redwood.EndTrack(description);
		}

		/*
		* Dictionaries (notably, Properties) -- convert them to Maps and dispatch
		*/
		private static void Log<K, V>(Redwood.RedwoodChannels channels, string description, Dictionary<K, V> dict)
		{
			//(a real data structure)
			IDictionary<K, V> map = Generics.NewHashMap();
			//(copy to map)
			IEnumeration<K> keys = dict.Keys;
			while (keys.MoveNext())
			{
				K key = keys.Current;
				V value = dict[key];
				map[key] = value;
			}
			//(log like normal)
			Log(channels, description, map);
		}

		/*
		* Iterables (includes Collection, List, Set, etc.)
		*/
		private static void Log<T>(Redwood.RedwoodChannels channels, string description, IEnumerable<T> iterable)
		{
			Redwood.StartTrack(description);
			if (iterable == null)
			{
				channels.Log("(iterable is null)");
			}
			else
			{
				int index = 0;
				foreach (T item in iterable)
				{
					if (Dispatchable(item) && item != iterable)
					{
						Log(channels, "Index " + index, item);
					}
					else
					{
						channels.Logf("Index %d: %s", index, item == iterable ? "...<infinite loop>" : item);
					}
					index++;
				}
				if (index == 0)
				{
					channels.Log("(empty)");
				}
			}
			Redwood.EndTrack(description);
		}

		/*
		* Arrays
		*/
		private static void Log<T>(Redwood.RedwoodChannels channels, string description, T[] array)
		{
			Redwood.StartTrack(description);
			if (array == null)
			{
				channels.Log("(array is null)");
			}
			else
			{
				if (array.Length == 0)
				{
					channels.Log("(empty)");
				}
				else
				{
					int index = 0;
					foreach (T item in array)
					{
						if (Dispatchable(item))
						{
							Log(channels, "Index " + index, item);
						}
						else
						{
							channels.Logf("Index %d: %s", index, item);
						}
						index++;
					}
				}
			}
			Redwood.EndTrack(description);
		}
	}
}
