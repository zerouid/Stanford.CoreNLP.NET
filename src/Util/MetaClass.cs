using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;







namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A meta class 
	/// <remarks>
	/// A meta class 
	/// instance, or a factory, where each Class from the factory share their
	/// constructor parameters.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class MetaClass
	{
		[System.Serializable]
		public class ClassCreationException : Exception
		{
			private const long serialVersionUID = -5980065992461870357L;

			private ClassCreationException()
				: base()
			{
			}

			private ClassCreationException(string msg)
				: base(msg)
			{
			}

			private ClassCreationException(Exception cause)
				: base(cause)
			{
			}

			private ClassCreationException(string msg, Exception cause)
				: base(msg, cause)
			{
			}
		}

		[System.Serializable]
		public sealed class ConstructorNotFoundException : MetaClass.ClassCreationException
		{
			private const long serialVersionUID = -5980065992461870357L;

			private ConstructorNotFoundException()
				: base()
			{
			}

			private ConstructorNotFoundException(string msg)
				: base(msg)
			{
			}

			private ConstructorNotFoundException(Exception cause)
				: base(cause)
			{
			}

			private ConstructorNotFoundException(string msg, Exception cause)
				: base(msg, cause)
			{
			}
		}

		public sealed class ClassFactory<T>
		{
			private Type[] classParams;

			private Type cl;

			private Constructor<T> constructor;

			private static bool SamePrimitive(Type a, Type b)
			{
				if (!a.IsPrimitive && !b.IsPrimitive)
				{
					return false;
				}
				if (a.IsPrimitive)
				{
					try
					{
						Type type = (Type)b.GetField("TYPE").GetValue(null);
						return type.Equals(a);
					}
					catch (Exception)
					{
						return false;
					}
				}
				if (b.IsPrimitive)
				{
					try
					{
						Type type = (Type)a.GetField("TYPE").GetValue(null);
						return type.Equals(b);
					}
					catch (Exception)
					{
						return false;
					}
				}
				throw new InvalidOperationException("Impossible case");
			}

			private static int SuperDistance(Type candidate, Type target)
			{
				if (candidate == null)
				{
					// --base case: does not implement
					return int.MinValue;
				}
				else
				{
					if (candidate.Equals(target))
					{
						// --base case: exact match
						return 0;
					}
					else
					{
						if (SamePrimitive(candidate, target))
						{
							// --base case: primitive and wrapper
							return 0;
						}
						else
						{
							// --recursive case: try superclasses
							// case: direct superclass
							Type directSuper = candidate.BaseType;
							int superDist = SuperDistance(directSuper, target);
							if (superDist >= 0)
							{
								return superDist + 1;
							}
							// case: superclass distance
							// case: implementing interfaces
							Type[] interfaces = candidate.GetInterfaces();
							int minDist = int.MaxValue;
							foreach (Type i in interfaces)
							{
								superDist = SuperDistance(i, target);
								if (superDist >= 0)
								{
									minDist = Math.Min(minDist, superDist);
								}
							}
							if (minDist != int.MaxValue)
							{
								return minDist + 1;
							}
							else
							{
								// case: interface distance
								return -1;
							}
						}
					}
				}
			}

			// case: failure
			/// <exception cref="System.TypeLoadException"/>
			/// <exception cref="System.MissingMethodException"/>
			private void Construct(string classname, params Type[] @params)
			{
				// (save class parameters)
				this.classParams = @params;
				// (create class)
				try
				{
					this.cl = (Type)Sharpen.Runtime.GetType(classname);
				}
				catch (InvalidCastException)
				{
					throw new MetaClass.ClassCreationException("Class " + classname + " could not be cast to the correct type");
				}
				// --Find Constructor
				// (get constructors)
				Constructor<object>[] constructors = cl.GetDeclaredConstructors();
				Constructor<object>[] potentials = new Constructor<object>[constructors.Length];
				Type[][] constructorParams = new Type[constructors.Length][];
				int[] distances = new int[constructors.Length];
				//distance from base class
				// (filter: length)
				for (int i = 0; i < constructors.Length; i++)
				{
					constructorParams[i] = constructors[i].GetParameterTypes();
					if (@params.Length == constructorParams[i].Length)
					{
						// length is good
						potentials[i] = constructors[i];
						distances[i] = 0;
					}
					else
					{
						// length is bad
						potentials[i] = null;
						distances[i] = -1;
					}
				}
				// (filter:type)
				for (int paramIndex = 0; paramIndex < @params.Length; paramIndex++)
				{
					// for each parameter...
					Type target = @params[paramIndex];
					for (int conIndex = 0; conIndex < potentials.Length; conIndex++)
					{
						// for each constructor...
						if (potentials[conIndex] != null)
						{
							// if the constructor is in the pool...
							Type cand = constructorParams[conIndex][paramIndex];
							int dist = SuperDistance(target, cand);
							if (dist >= 0)
							{
								// and if the constructor matches...
								distances[conIndex] += dist;
							}
							else
							{
								// keep it
								potentials[conIndex] = null;
								// else, remove it from the pool
								distances[conIndex] = -1;
							}
						}
					}
				}
				// (filter:min)
				this.constructor = (Constructor<T>)Argmin(potentials, distances, 0);
				if (this.constructor == null)
				{
					StringBuilder b = new StringBuilder();
					b.Append(classname).Append("(");
					foreach (Type c in @params)
					{
						b.Append(c.FullName).Append(", ");
					}
					string target = b.Substring(0, @params.Length == 0 ? b.Length : b.Length - 2) + ")";
					throw new MetaClass.ConstructorNotFoundException("No constructor found to match: " + target);
				}
			}

			/// <exception cref="System.TypeLoadException"/>
			/// <exception cref="System.MissingMethodException"/>
			private ClassFactory(string classname, params Type[] @params)
			{
				// (generic construct)
				Construct(classname, @params);
			}

			/// <exception cref="System.TypeLoadException"/>
			/// <exception cref="System.MissingMethodException"/>
			private ClassFactory(string classname, params object[] @params)
			{
				// (convert class parameters)
				Type[] classParams = new Type[@params.Length];
				for (int i = 0; i < @params.Length; i++)
				{
					if (@params[i] == null)
					{
						throw new MetaClass.ClassCreationException("Argument " + i + " to class constructor is null");
					}
					classParams[i] = @params[i].GetType();
				}
				// (generic construct)
				Construct(classname, classParams);
			}

			/// <exception cref="System.TypeLoadException"/>
			/// <exception cref="System.MissingMethodException"/>
			private ClassFactory(string classname, params string[] @params)
			{
				// (convert class parameters)
				Type[] classParams = new Type[@params.Length];
				for (int i = 0; i < @params.Length; i++)
				{
					classParams[i] = Sharpen.Runtime.GetType(@params[i]);
				}
				// (generic construct)
				Construct(classname, classParams);
			}

			/// <summary>Creates an instance of the class produced in this factory</summary>
			/// <param name="params">
			/// The arguments to the constructor of the class NOTE: the
			/// resulting instance will [unlike java] invoke the most
			/// narrow constructor rather than the one which matches the
			/// signature passed to this function
			/// </param>
			/// <returns>An instance of the class</returns>
			public T CreateInstance(params object[] @params)
			{
				try
				{
					bool accessible = true;
					if (!constructor.IsAccessible())
					{
						accessible = false;
						constructor.SetAccessible(true);
					}
					T rtn = constructor.NewInstance(@params);
					if (!accessible)
					{
						constructor.SetAccessible(false);
					}
					return rtn;
				}
				catch (Exception e)
				{
					throw new MetaClass.ClassCreationException("MetaClass couldn't create " + constructor + " with args " + Arrays.ToString(@params), e);
				}
			}

			/// <summary>Returns the full class name for the objects being produced</summary>
			/// <returns>The class name for the objects produced</returns>
			public string GetName()
			{
				return cl.FullName;
			}

			public override string ToString()
			{
				StringBuilder b = new StringBuilder();
				b.Append(cl.FullName).Append('(');
				foreach (Type cl in classParams)
				{
					b.Append(' ').Append(cl.FullName).Append(',');
				}
				b.Replace(b.Length - 1, b.Length, " ");
				b.Append(')');
				return b.ToString();
			}

			public override bool Equals(object o)
			{
				if (o is MetaClass.ClassFactory)
				{
					MetaClass.ClassFactory other = (MetaClass.ClassFactory)o;
					if (!this.cl.Equals(other.cl))
					{
						return false;
					}
					for (int i = 0; i < classParams.Length; i++)
					{
						if (!this.classParams[i].Equals(other.classParams[i]))
						{
							return false;
						}
					}
					return true;
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				return cl.GetHashCode();
			}
		}

		private string classname;

		/// <summary>Creates a new MetaClass producing objects of the given type</summary>
		/// <param name="classname">The full classname of the objects to create</param>
		public MetaClass(string classname)
		{
			// end static class ClassFactory
			this.classname = classname;
		}

		/// <summary>Creates a new MetaClass producing objects of the given type</summary>
		/// <param name="classname">The class to create</param>
		public MetaClass(Type classname)
		{
			this.classname = classname.FullName;
		}

		/// <summary>
		/// Creates a factory for producing instances of this class from a
		/// constructor taking the given types as arguments
		/// </summary>
		/// <?/>
		/// <param name="classes">The types used in the constructor</param>
		/// <returns>A ClassFactory of the given type</returns>
		public virtual MetaClass.ClassFactory<E> CreateFactory<E>(params Type[] classes)
		{
			try
			{
				return new MetaClass.ClassFactory<E>(classname, classes);
			}
			catch (MetaClass.ClassCreationException e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new MetaClass.ClassCreationException(e);
			}
		}

		/// <summary>
		/// Creates a factory for producing instances of this class from a
		/// constructor taking the given types as arguments
		/// </summary>
		/// <?/>
		/// <param name="classes">The types used in the constructor</param>
		/// <returns>A ClassFactory of the given type</returns>
		public virtual MetaClass.ClassFactory<E> CreateFactory<E>(params string[] classes)
		{
			try
			{
				return new MetaClass.ClassFactory<E>(classname, classes);
			}
			catch (MetaClass.ClassCreationException e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new MetaClass.ClassCreationException(e);
			}
		}

		/// <summary>
		/// Creates a factory for producing instances of this class from a
		/// constructor taking objects of the types given
		/// </summary>
		/// <?/>
		/// <param name="objects">Instances of the types used in the constructor</param>
		/// <returns>A ClassFactory of the given type</returns>
		public virtual MetaClass.ClassFactory<E> CreateFactory<E>(params object[] objects)
		{
			try
			{
				return new MetaClass.ClassFactory<E>(classname, objects);
			}
			catch (MetaClass.ClassCreationException e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new MetaClass.ClassCreationException(e);
			}
		}

		/// <summary>
		/// Create an instance of the class, inferring the type automatically, and
		/// given an array of objects as constructor parameters NOTE: the resulting
		/// instance will [unlike java] invoke the most narrow constructor rather
		/// than the one which matches the signature passed to this function
		/// </summary>
		/// <?/>
		/// <param name="objects">The arguments to the constructor of the class</param>
		/// <returns>An instance of the class</returns>
		public virtual E CreateInstance<E>(params object[] objects)
		{
			MetaClass.ClassFactory<E> fact = CreateFactory(objects);
			return fact.CreateInstance(objects);
		}

		/// <summary>
		/// Creates an instance of the class, forcing a cast to a certain type and
		/// given an array of objects as constructor parameters NOTE: the resulting
		/// instance will [unlike java] invoke the most narrow constructor rather
		/// than the one which matches the signature passed to this function
		/// </summary>
		/// <?/>
		/// <param name="type">The class of the object returned</param>
		/// <param name="params">The arguments to the constructor of the class</param>
		/// <returns>An instance of the class</returns>
		public virtual F CreateInstance<E, F>(params object[] @params)
			where F : E
		{
			System.Type type = typeof(E);
			object obj = CreateInstance(@params);
			if (type.IsInstanceOfType(obj))
			{
				return (F)obj;
			}
			else
			{
				throw new MetaClass.ClassCreationException("Cannot cast " + classname + " into " + type.FullName);
			}
		}

		public virtual bool CheckConstructor(params object[] @params)
		{
			try
			{
				CreateInstance(@params);
				return true;
			}
			catch (MetaClass.ConstructorNotFoundException)
			{
				return false;
			}
		}

		public override string ToString()
		{
			return classname;
		}

		public override bool Equals(object o)
		{
			if (o is MetaClass)
			{
				return ((MetaClass)o).classname.Equals(this.classname);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return classname.GetHashCode();
		}

		/// <summary>Creates a new MetaClass (helper method)</summary>
		/// <param name="classname">The name of the class to create</param>
		/// <returns>A new MetaClass object of the given class</returns>
		public static MetaClass Create(string classname)
		{
			return new MetaClass(classname);
		}

		/// <summary>Creates a new MetaClass (helper method)</summary>
		/// <param name="clazz">The class to create</param>
		/// <returns>A new MetaClass object of the given class</returns>
		public static MetaClass Create(Type clazz)
		{
			return new MetaClass(clazz);
		}

		/// <summary>Utility method for cast</summary>
		/// <param name="type">The type to cast into a class</param>
		/// <returns>The class corresponding to the passed in type</returns>
		private static Type Type2class(IType type)
		{
			if (type is Type)
			{
				return (Type)type;
			}
			else
			{
				//base case
				if (type is IParameterizedType)
				{
					return Type2class(((IParameterizedType)type).GetRawType());
				}
				else
				{
					if (type is ITypeVariable<object>)
					{
						return Type2class(((ITypeVariable<object>)type).GetBounds()[0]);
					}
					else
					{
						if (type is IWildcardType)
						{
							return Type2class(((IWildcardType)type).GetUpperBounds()[0]);
						}
						else
						{
							throw new ArgumentException("Cannot convert type to class: " + type);
						}
					}
				}
			}
		}

		/// <summary>Cast a String representation of an object into that object.</summary>
		/// <remarks>
		/// Cast a String representation of an object into that object.
		/// E.g. "5.4" will be cast to a Double; "[1,2,3]" will be cast
		/// to an Integer[].
		/// NOTE: Date parses from a Long
		/// </remarks>
		/// <?/>
		/// <param name="value">The string representation of the object</param>
		/// <param name="type">The type (usually class) to be returned (same as E)</param>
		/// <returns>An object corresponding to the String value passed</returns>
		public static E Cast<E>(string value, IType type)
		{
			//--Get Type
			Type clazz;
			if (type is Type)
			{
				clazz = (Type)type;
			}
			else
			{
				if (type is IParameterizedType)
				{
					IParameterizedType pt = (IParameterizedType)type;
					clazz = (Type)pt.GetRawType();
				}
				else
				{
					throw new ArgumentException("Cannot cast to type (unhandled type): " + type);
				}
			}
			//--Cast
			if (typeof(string).IsAssignableFrom(clazz))
			{
				// (case: String)
				return (E)value;
			}
			else
			{
				if (typeof(bool).IsAssignableFrom(clazz) || typeof(bool).IsAssignableFrom(clazz))
				{
					//(case: boolean)
					if ("1".Equals(value))
					{
						return (E)true;
					}
					return (E)bool.ValueOf(bool.Parse(value));
				}
				else
				{
					if (typeof(int).IsAssignableFrom(clazz) || typeof(int).IsAssignableFrom(clazz))
					{
						//(case: integer)
						try
						{
							return (E)System.Convert.ToInt32(value);
						}
						catch (NumberFormatException)
						{
							return (E)(int)double.Parse(value);
						}
					}
					else
					{
						if (typeof(BigInteger).IsAssignableFrom(clazz))
						{
							//(case: biginteger)
							if (value == null)
							{
								return (E)BigInteger.Zero;
							}
							return (E)new BigInteger(value);
						}
						else
						{
							if (typeof(long).IsAssignableFrom(clazz) || typeof(long).IsAssignableFrom(clazz))
							{
								//(case: long)
								try
								{
									return (E)long.Parse(value);
								}
								catch (NumberFormatException)
								{
									return (E)(long)double.Parse(value);
								}
							}
							else
							{
								if (typeof(float).IsAssignableFrom(clazz) || typeof(float).IsAssignableFrom(clazz))
								{
									//(case: float)
									if (value == null)
									{
										return (E)float.NaN;
									}
									return (E)float.ParseFloat(value);
								}
								else
								{
									if (typeof(double).IsAssignableFrom(clazz) || typeof(double).IsAssignableFrom(clazz))
									{
										//(case: double)
										if (value == null)
										{
											return (E)double.NaN;
										}
										return (E)double.Parse(value);
									}
									else
									{
										if (typeof(BigDecimal).IsAssignableFrom(clazz))
										{
											//(case: bigdecimal)
											if (value == null)
											{
												return (E)BigDecimal.Zero;
											}
											return (E)new BigDecimal(value);
										}
										else
										{
											if (typeof(short).IsAssignableFrom(clazz) || typeof(short).IsAssignableFrom(clazz))
											{
												//(case: short)
												try
												{
													return (E)short.ParseShort(value);
												}
												catch (NumberFormatException)
												{
													return (E)(short)double.Parse(value);
												}
											}
											else
											{
												if (typeof(byte).IsAssignableFrom(clazz) || typeof(byte).IsAssignableFrom(clazz))
												{
													//(case: byte)
													try
													{
														return (E)byte.ParseByte(value);
													}
													catch (NumberFormatException)
													{
														return (E)unchecked((byte)double.Parse(value));
													}
												}
												else
												{
													if (typeof(char).IsAssignableFrom(clazz) || typeof(char).IsAssignableFrom(clazz))
													{
														//(case: char)
														return (E)(char)System.Convert.ToInt32(value);
													}
													else
													{
														if (typeof(Lazy).IsAssignableFrom(clazz))
														{
															//(case: Lazy)
															string v = value;
															return (E)Lazy.Of(null);
														}
														else
														{
															if (typeof(Optional).IsAssignableFrom(clazz))
															{
																//(case: Optional)
																return (E)((value == null || "null".Equals(value.ToLower()) || "empty".Equals(value.ToLower()) || "none".Equals(value.ToLower())) ? Optional.Empty() : Optional.Of(value));
															}
															else
															{
																if (typeof(DateTime).IsAssignableFrom(clazz))
																{
																	//(case: date)
																	try
																	{
																		return (E)new DateTime(long.Parse(value));
																	}
																	catch (NumberFormatException)
																	{
																		return null;
																	}
																}
																else
																{
																	if (typeof(Calendar).IsAssignableFrom(clazz))
																	{
																		//(case: date)
																		try
																		{
																			DateTime d = new DateTime(long.Parse(value));
																			GregorianCalendar cal = new GregorianCalendar();
																			cal.SetTime(d);
																			return (E)cal;
																		}
																		catch (NumberFormatException)
																		{
																			return null;
																		}
																	}
																	else
																	{
																		if (typeof(FileWriter).IsAssignableFrom(clazz))
																		{
																			try
																			{
																				return (E)new FileWriter(new File(value));
																			}
																			catch (IOException e)
																			{
																				throw new RuntimeIOException(e);
																			}
																		}
																		else
																		{
																			if (typeof(BufferedReader).IsAssignableFrom(clazz))
																			{
																				try
																				{
																					return (E)IOUtils.GetBufferedReaderFromClasspathOrFileSystem(value);
																				}
																				catch (IOException e)
																				{
																					throw new RuntimeIOException(e);
																				}
																			}
																			else
																			{
																				if (typeof(FileReader).IsAssignableFrom(clazz))
																				{
																					try
																					{
																						return (E)new FileReader(new File(value));
																					}
																					catch (IOException e)
																					{
																						throw new RuntimeIOException(e);
																					}
																				}
																				else
																				{
																					if (typeof(File).IsAssignableFrom(clazz))
																					{
																						return (E)new File(value);
																					}
																					else
																					{
																						if (typeof(Type).IsAssignableFrom(clazz))
																						{
																							try
																							{
																								return (E)Sharpen.Runtime.GetType(value);
																							}
																							catch (TypeLoadException)
																							{
																								return null;
																							}
																						}
																						else
																						{
																							if (clazz.IsArray)
																							{
																								if (value == null)
																								{
																									return null;
																								}
																								Type subType = clazz.GetElementType();
																								// (case: array)
																								string[] strings = StringUtils.DecodeArray(value);
																								object[] array = (object[])System.Array.CreateInstance(clazz.GetElementType(), strings.Length);
																								for (int i = 0; i < strings.Length; i++)
																								{
																									array[i] = Cast(strings[i], subType);
																								}
																								return (E)array;
																							}
																							else
																							{
																								if (typeof(IDictionary).IsAssignableFrom(clazz))
																								{
																									return (E)StringUtils.DecodeMap(value);
																								}
																								else
																								{
																									if (clazz.IsEnum())
																									{
																										// (case: enumeration)
																										Type c = (Type)clazz;
																										if (value == null)
																										{
																											return null;
																										}
																										if (value[0] == '"')
																										{
																											value = Sharpen.Runtime.Substring(value, 1);
																										}
																										if (value[value.Length - 1] == '"')
																										{
																											value = Sharpen.Runtime.Substring(value, 0, value.Length - 1);
																										}
																										try
																										{
																											return (E)Enum.ValueOf(c, value);
																										}
																										catch (Exception)
																										{
																											try
																											{
																												return (E)Enum.ValueOf(c, value.ToLower());
																											}
																											catch (Exception)
																											{
																												try
																												{
																													return (E)Enum.ValueOf(c, value.ToUpper());
																												}
																												catch (Exception)
																												{
																													return (E)Enum.ValueOf(c, (char.IsUpperCase(value[0]) ? char.ToLowerCase(value[0]) : char.ToUpperCase(value[0])) + Sharpen.Runtime.Substring(value, 1));
																												}
																											}
																										}
																									}
																									else
																									{
																										if (typeof(ObjectOutputStream).IsAssignableFrom(clazz))
																										{
																											// (case: object output stream)
																											try
																											{
																												return (E)new ObjectOutputStream((OutputStream)Cast(value, typeof(OutputStream)));
																											}
																											catch (IOException e)
																											{
																												throw new Exception(e);
																											}
																										}
																										else
																										{
																											if (typeof(ObjectInputStream).IsAssignableFrom(clazz))
																											{
																												// (case: object input stream)
																												try
																												{
																													return (E)new ObjectInputStream((InputStream)Cast(value, typeof(InputStream)));
																												}
																												catch (IOException e)
																												{
																													throw new Exception(e);
																												}
																											}
																											else
																											{
																												if (typeof(TextWriter).IsAssignableFrom(clazz))
																												{
																													// (case: input stream)
																													if (Sharpen.Runtime.EqualsIgnoreCase(value, "stdout") || Sharpen.Runtime.EqualsIgnoreCase(value, "out"))
																													{
																														return (E)System.Console.Out;
																													}
																													if (Sharpen.Runtime.EqualsIgnoreCase(value, "stderr") || Sharpen.Runtime.EqualsIgnoreCase(value, "err"))
																													{
																														return (E)System.Console.Error;
																													}
																													try
																													{
																														return (E)new TextWriter(new FileOutputStream(value));
																													}
																													catch (IOException e)
																													{
																														throw new Exception(e);
																													}
																												}
																												else
																												{
																													if (typeof(PrintWriter).IsAssignableFrom(clazz))
																													{
																														// (case: input stream)
																														if (Sharpen.Runtime.EqualsIgnoreCase(value, "stdout") || Sharpen.Runtime.EqualsIgnoreCase(value, "out"))
																														{
																															return (E)new PrintWriter(System.Console.Out);
																														}
																														if (Sharpen.Runtime.EqualsIgnoreCase(value, "stderr") || Sharpen.Runtime.EqualsIgnoreCase(value, "err"))
																														{
																															return (E)new PrintWriter(System.Console.Error);
																														}
																														try
																														{
																															return (E)IOUtils.GetPrintWriter(value);
																														}
																														catch (IOException e)
																														{
																															throw new Exception(e);
																														}
																													}
																													else
																													{
																														if (typeof(OutputStream).IsAssignableFrom(clazz))
																														{
																															// (case: output stream)
																															if (Sharpen.Runtime.EqualsIgnoreCase(value, "stdout") || Sharpen.Runtime.EqualsIgnoreCase(value, "out"))
																															{
																																return (E)System.Console.Out;
																															}
																															if (Sharpen.Runtime.EqualsIgnoreCase(value, "stderr") || Sharpen.Runtime.EqualsIgnoreCase(value, "err"))
																															{
																																return (E)System.Console.Error;
																															}
																															File toWriteTo = Cast(value, typeof(File));
																															try
																															{
																																if (toWriteTo == null || (!toWriteTo.Exists() && !toWriteTo.CreateNewFile()))
																																{
																																	throw new InvalidOperationException("Could not create output stream (cannot write file): " + value);
																																}
																																return (E)IOUtils.GetFileOutputStream(value);
																															}
																															catch (IOException e)
																															{
																																throw new Exception(e);
																															}
																														}
																														else
																														{
																															if (typeof(InputStream).IsAssignableFrom(clazz))
																															{
																																// (case: input stream)
																																if (Sharpen.Runtime.EqualsIgnoreCase(value, "stdin") || Sharpen.Runtime.EqualsIgnoreCase(value, "in"))
																																{
																																	return (E)Runtime.@in;
																																}
																																try
																																{
																																	return (E)IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(value);
																																}
																																catch (IOException e)
																																{
																																	throw new Exception(e);
																																}
																															}
																															else
																															{
																																try
																																{
																																	// (case: can parse from string)
																																	MethodInfo decode = clazz.GetMethod("fromString", typeof(string));
																																	return (E)decode.Invoke(MetaClass.Create(clazz), value);
																																}
																																catch (Exception)
																																{
																																}
																																// Silent errors for misc failures
																																// Pass 2: Guess what the object could be
																																if (typeof(Tree).IsAssignableFrom(clazz))
																																{
																																	// (case: reading a tree)
																																	try
																																	{
																																		return (E)new PennTreeReader(new StringReader(value), new LabeledScoredTreeFactory(CoreLabel.Factory())).ReadTree();
																																	}
																																	catch (IOException e)
																																	{
																																		throw new Exception(e);
																																	}
																																}
																																else
																																{
																																	if (typeof(ICollection).IsAssignableFrom(clazz))
																																	{
																																		// (case: reading a collection)
																																		ICollection rtn;
																																		if (Modifier.IsAbstract(clazz.GetModifiers()))
																																		{
																																			rtn = abstractToConcreteCollectionMap[clazz].CreateInstance();
																																		}
																																		else
																																		{
																																			rtn = MetaClass.Create(clazz).CreateInstance();
																																		}
																																		Type subType = clazz.GetElementType();
																																		string[] strings = StringUtils.DecodeArray(value);
																																		foreach (string @string in strings)
																																		{
																																			if (subType == null)
																																			{
																																				rtn.Add(CastWithoutKnowingType(@string));
																																			}
																																			else
																																			{
																																				rtn.Add(Cast(@string, subType));
																																			}
																																		}
																																		return (E)rtn;
																																	}
																																	else
																																	{
																																		// We could not cast this object
																																		return null;
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static E CastWithoutKnowingType<E>(string value)
		{
			Type[] typesToTry = new Type[] { typeof(int), typeof(double), typeof(File), typeof(DateTime), typeof(IList), typeof(ISet), typeof(IQueue), typeof(int[]), typeof(double[]), typeof(char[]), typeof(string) };
			foreach (Type toTry in typesToTry)
			{
				if (typeof(ICollection).IsAssignableFrom(toTry) && !value.Contains(",") || value.Contains(" "))
				{
					continue;
				}
				//noinspection EmptyCatchBlock
				try
				{
					object rtn;
					if ((rtn = Cast(value, toTry)) != null && (!typeof(File).IsAssignableFrom(rtn.GetType()) || ((File)rtn).Exists()))
					{
						return ErasureUtils.UncheckedCast(rtn);
					}
				}
				catch (NumberFormatException)
				{
				}
			}
			return null;
		}

		private static E Argmin<E>(E[] elems, int[] scores, int atLeast)
		{
			int argmin = Argmin(scores, atLeast);
			return argmin >= 0 ? elems[argmin] : null;
		}

		private static int Argmin(int[] scores, int atLeast)
		{
			int min = int.MaxValue;
			int argmin = -1;
			for (int i = 0; i < scores.Length; i++)
			{
				if (scores[i] < min && scores[i] >= atLeast)
				{
					min = scores[i];
					argmin = i;
				}
			}
			return argmin;
		}

		private static readonly Dictionary<Type, MetaClass> abstractToConcreteCollectionMap = new Dictionary<Type, MetaClass>();

		static MetaClass()
		{
			abstractToConcreteCollectionMap[typeof(ICollection)] = MetaClass.Create(typeof(ArrayList));
			abstractToConcreteCollectionMap[typeof(IList)] = MetaClass.Create(typeof(ArrayList));
			abstractToConcreteCollectionMap[typeof(ISet)] = MetaClass.Create(typeof(HashSet));
			abstractToConcreteCollectionMap[typeof(IQueue)] = MetaClass.Create(typeof(ArrayList));
			abstractToConcreteCollectionMap[typeof(IDeque)] = MetaClass.Create(typeof(ArrayList));
		}
	}
}
