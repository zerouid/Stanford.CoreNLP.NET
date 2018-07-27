using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;








namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Output an Annotation to human readable JSON.</summary>
	/// <remarks>
	/// Output an Annotation to human readable JSON.
	/// This is not a lossless operation; for more strict serialization,
	/// see
	/// <see cref="AnnotationSerializer"/>
	/// ; e.g.,
	/// <see cref="ProtobufAnnotationSerializer"/>
	/// .
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class JSONOutputter : AnnotationOutputter
	{
		protected internal const string IndentChar = "  ";

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options)
		{
			// It's lying; we need the "redundant" casts (as of 2014-09-08)
			PrintWriter writer = new PrintWriter(IOUtils.EncodedOutputStreamWriter(target, options.encoding));
			JSONOutputter.JSONWriter l0 = new JSONOutputter.JSONWriter(writer, options);
			l0.Object(null);
			// Add annotations attached to a Document
			// Add sentences
			// Add a single sentence
			// (metadata)
			// (constituency tree)
			// note the '==' -- we're overwriting the default, but only if it was not explicitly set otherwise
			// strip the trailing newline
			// (dependency trees)
			// (sentiment)
			// (openie)
			// (kbp)
			// (entity mentions)
			//l3.set("originalText", m.get(CoreAnnotations.OriginalTextAnnotation.class));
			//l3.set("lemma", m.get(CoreAnnotations.LemmaAnnotation.class));
			//l3.set("pos", m.get(CoreAnnotations.PartOfSpeechAnnotation.class));
			// Timex
			// (add tokens)
			// Add a single token
			// Timex
			// Add coref values
			// quotes
			// sections
			// Set char start
			// Set char end
			// Set author
			// Set date time
			// add the sentence indexes for the sentences in this section
			l0.Flush();
		}

		// flush
		private static void WriteTriples(JSONOutputter.IWriter l2, string key, ICollection<RelationTriple> triples)
		{
			if (triples != null)
			{
				l2.Set(key, triples.Stream().Map(null));
			}
		}

		private static void WriteTime(JSONOutputter.IWriter l3, Timex time)
		{
			if (time != null)
			{
				Timex.Range range = time.Range();
				l3.Set("timex", (IConsumer<JSONOutputter.IWriter>)null);
			}
		}

		/// <summary>
		/// Convert a dependency graph to a format expected as input to
		/// <see cref="IWriter.Set(string, object)"/>
		/// .
		/// </summary>
		private static object BuildDependencyTree(SemanticGraph graph)
		{
			// It's lying; we need the "redundant" casts (as of 2014-09-08)
			if (graph != null)
			{
				return IStream.Concat(graph.GetRoots().Stream().Map(null), graph.EdgeListSorted().Stream().Map(null));
			}
			else
			{
				// Roots
				// Regular edges
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static string JsonPrint(Annotation annotation)
		{
			StringOutputStream os = new StringOutputStream();
			new JSONOutputter().Print(annotation, os);
			return os.ToString();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void JsonPrint(Annotation annotation, OutputStream os)
		{
			new JSONOutputter().Print(annotation, os);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void JsonPrint(Annotation annotation, OutputStream os, StanfordCoreNLP pipeline)
		{
			new JSONOutputter().Print(annotation, os, pipeline);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void JsonPrint(Annotation annotation, OutputStream os, AnnotationOutputter.Options options)
		{
			new JSONOutputter().Print(annotation, os, options);
		}

		/// <summary>Our very own little JSON writing class.</summary>
		/// <remarks>
		/// Our very own little JSON writing class.
		/// For usage, see the test cases in JSONOutputterTest.
		/// For the love of all that is holy, don't try to write JSON multithreaded.
		/// It should go without saying that this is not threadsafe.
		/// </remarks>
		public class JSONWriter
		{
			protected internal readonly PrintWriter writer;

			protected internal readonly AnnotationOutputter.Options options;

			public JSONWriter(PrintWriter writer, AnnotationOutputter.Options options)
			{
				this.writer = writer;
				this.options = options;
			}

			private void RouteObject(int indent, object value)
			{
				if (value is string)
				{
					// Case: simple string (this is easy!)
					writer.Write("\"");
					writer.Write(StringUtils.EscapeJsonString(value.ToString()));
					writer.Write("\"");
				}
				else
				{
					if (value is ICollection)
					{
						// Case: collection
						writer.Write("[");
						Newline();
						IEnumerator<object> elems = ((ICollection<object>)value).GetEnumerator();
						while (elems.MoveNext())
						{
							Indent(indent + 1);
							RouteObject(indent + 1, elems.Current);
							if (elems.MoveNext())
							{
								writer.Write(",");
							}
							Newline();
						}
						Indent(indent);
						writer.Write("]");
					}
					else
					{
						if (value is Enum)
						{
							// Case: enumeration constant
							writer.Write("\"");
							writer.Write(StringUtils.EscapeJsonString(((Enum)value).Name()));
							writer.Write("\"");
						}
						else
						{
							if (value is Pair)
							{
								RouteObject(indent, Arrays.AsList(((Pair)value).first, ((Pair)value).second));
							}
							else
							{
								if (value is Span)
								{
									writer.Write("[");
									writer.Write(int.ToString(((Span)value).Start()));
									writer.Write(",");
									Space();
									writer.Write(int.ToString(((Span)value).End()));
									writer.Write("]");
								}
								else
								{
									if (value is IConsumer)
									{
										Object(indent, (IConsumer<JSONOutputter.IWriter>)value);
									}
									else
									{
										if (value is IStream)
										{
											RouteObject(indent, ((IStream)value).Collect(Collectors.ToList()));
										}
										else
										{
											if (value.GetType().IsArray)
											{
												// Arrays make life miserable in Java
												Type componentType = value.GetType().GetElementType();
												if (componentType.IsPrimitive)
												{
													if (typeof(int).IsAssignableFrom(componentType))
													{
														List<int> lst = new List<int>();
														//noinspection Convert2streamapi
														foreach (int elem in ((int[])value))
														{
															lst.Add(elem);
														}
														RouteObject(indent, lst);
													}
													else
													{
														if (typeof(short).IsAssignableFrom(componentType))
														{
															List<short> lst = new List<short>();
															foreach (short elem in ((short[])value))
															{
																lst.Add(elem);
															}
															RouteObject(indent, lst);
														}
														else
														{
															if (typeof(byte).IsAssignableFrom(componentType))
															{
																List<byte> lst = new List<byte>();
																foreach (byte elem in ((byte[])value))
																{
																	lst.Add(elem);
																}
																RouteObject(indent, lst);
															}
															else
															{
																if (typeof(long).IsAssignableFrom(componentType))
																{
																	List<long> lst = new List<long>();
																	//noinspection Convert2streamapi
																	foreach (long elem in ((long[])value))
																	{
																		lst.Add(elem);
																	}
																	RouteObject(indent, lst);
																}
																else
																{
																	if (typeof(char).IsAssignableFrom(componentType))
																	{
																		List<char> lst = new List<char>();
																		foreach (char elem in ((char[])value))
																		{
																			lst.Add(elem);
																		}
																		RouteObject(indent, lst);
																	}
																	else
																	{
																		if (typeof(float).IsAssignableFrom(componentType))
																		{
																			List<float> lst = new List<float>();
																			foreach (float elem in ((float[])value))
																			{
																				lst.Add(elem);
																			}
																			RouteObject(indent, lst);
																		}
																		else
																		{
																			if (typeof(double).IsAssignableFrom(componentType))
																			{
																				List<double> lst = new List<double>();
																				//noinspection Convert2streamapi
																				foreach (double elem in ((double[])value))
																				{
																					lst.Add(elem);
																				}
																				RouteObject(indent, lst);
																			}
																			else
																			{
																				if (typeof(bool).IsAssignableFrom(componentType))
																				{
																					List<bool> lst = new List<bool>();
																					foreach (bool elem in ((bool[])value))
																					{
																						lst.Add(elem);
																					}
																					RouteObject(indent, lst);
																				}
																				else
																				{
																					throw new InvalidOperationException("Unhandled primitive type in array: " + componentType);
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
													RouteObject(indent, Arrays.AsList((object[])value));
												}
											}
											else
											{
												if (value is int)
												{
													writer.Write(int.ToString((int)value));
												}
												else
												{
													if (value is short)
													{
														writer.Write(short.ToString((short)value));
													}
													else
													{
														if (value is byte)
														{
															writer.Write(byte.ToString((byte)value));
														}
														else
														{
															if (value is long)
															{
																writer.Write(System.Convert.ToString((long)value));
															}
															else
															{
																if (value is char)
																{
																	writer.Write(char.ToString((char)(char)value));
																}
																else
																{
																	if (value is float)
																	{
																		writer.Write(new DecimalFormat("0.#######").Format(value));
																	}
																	else
																	{
																		if (value is double)
																		{
																			writer.Write(new DecimalFormat("0.##############").Format(value));
																		}
																		else
																		{
																			if (value is bool)
																			{
																				writer.Write(bool.ToString((bool)value));
																			}
																			else
																			{
																				if (typeof(int).IsAssignableFrom(value.GetType()))
																				{
																					RouteObject(indent, int.Parse((int)value));
																				}
																				else
																				{
																					if (typeof(short).IsAssignableFrom(value.GetType()))
																					{
																						RouteObject(indent, short.ValueOf((short)value));
																					}
																					else
																					{
																						if (typeof(byte).IsAssignableFrom(value.GetType()))
																						{
																							RouteObject(indent, byte.ValueOf(unchecked((byte)value)));
																						}
																						else
																						{
																							if (typeof(long).IsAssignableFrom(value.GetType()))
																							{
																								RouteObject(indent, long.ValueOf((long)value));
																							}
																							else
																							{
																								if (typeof(char).IsAssignableFrom(value.GetType()))
																								{
																									RouteObject(indent, char.ValueOf((char)value));
																								}
																								else
																								{
																									if (typeof(float).IsAssignableFrom(value.GetType()))
																									{
																										RouteObject(indent, float.ValueOf((float)value));
																									}
																									else
																									{
																										if (typeof(double).IsAssignableFrom(value.GetType()))
																										{
																											RouteObject(indent, double.ValueOf((double)value));
																										}
																										else
																										{
																											if (typeof(bool).IsAssignableFrom(value.GetType()))
																											{
																												RouteObject(indent, bool.ValueOf((bool)value));
																											}
																											else
																											{
																												throw new Exception("Unknown object to serialize: " + value);
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

			public virtual void Object(int indent, IConsumer<JSONOutputter.IWriter> callback)
			{
				writer.Write("{");
				Pointer<bool> firstCall = new Pointer<bool>(true);
				callback.Accept(null);
				// First call overhead
				// Write the key
				// Write the value
				Newline();
				Indent(indent);
				writer.Write("}");
			}

			public virtual void Object(IConsumer<JSONOutputter.IWriter> callback)
			{
				Object(0, callback);
			}

			private void Indent(int num)
			{
				if (options.pretty)
				{
					for (int i = 0; i < num; ++i)
					{
						writer.Write(IndentChar);
					}
				}
			}

			public virtual void Flush()
			{
				writer.Flush();
			}

			private void Space()
			{
				if (options.pretty)
				{
					writer.Write(" ");
				}
			}

			private void Newline()
			{
				if (options.pretty)
				{
					writer.Write("\n");
				}
			}

			public static string ObjectToJSON(IConsumer<JSONOutputter.IWriter> callback)
			{
				OutputStream os = new ByteArrayOutputStream();
				PrintWriter @out = new PrintWriter(os);
				new JSONOutputter.JSONWriter(@out, new AnnotationOutputter.Options()).Object(callback);
				@out.Close();
				return os.ToString();
			}
		}

		/// <summary>A tiny little functional interface for writing a (key, value) pair.</summary>
		/// <remarks>
		/// A tiny little functional interface for writing a (key, value) pair.
		/// The key should always be a String, the value can be either a String,
		/// a Collection of valid values, or a Callback taking a Writer (this is how
		/// we represent objects while creating JSON).
		/// </remarks>
		public interface IWriter
		{
			/// <summary>Set a (key, value) pair in a JSON object.</summary>
			/// <remarks>
			/// Set a (key, value) pair in a JSON object.
			/// Note that if either the key or the value is null, nothing will be set.
			/// </remarks>
			/// <param name="key">The key of the object.</param>
			/// <param name="value">The value of the object.</param>
			void Set(string key, object value);
		}
	}
}
