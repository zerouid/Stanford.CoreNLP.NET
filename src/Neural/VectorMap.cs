using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;




namespace Edu.Stanford.Nlp.Neural
{
	/// <summary>A serializer for reading / writing word vectors.</summary>
	/// <remarks>
	/// A serializer for reading / writing word vectors.
	/// This is used to read word2vec in hcoref, and is primarily here
	/// for its efficient serialization / deserialization protocol, which
	/// saves/loads the vectors as 16 bit floats.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class VectorMap : Dictionary<string, float[]>
	{
		/// <summary>The integer type (i.e., number of bits per integer).</summary>
		[System.Serializable]
		private sealed class Itype
		{
			public static readonly VectorMap.Itype Int8 = new VectorMap.Itype();

			public static readonly VectorMap.Itype Int16 = new VectorMap.Itype();

			public static readonly VectorMap.Itype Int32 = new VectorMap.Itype();

			/// <summary>Get the minimum integer type that will fit this number.</summary>
			internal static VectorMap.Itype GetType(int num)
			{
				VectorMap.Itype t = VectorMap.Itype.Int32;
				if (num < short.MaxValue)
				{
					t = VectorMap.Itype.Int16;
				}
				if (num < byte.MaxValue)
				{
					t = VectorMap.Itype.Int8;
				}
				return t;
			}

			/// <summary>Read an integer of this type from the given input stream</summary>
			/// <exception cref="System.IO.IOException"/>
			public int Read(DataInputStream @in)
			{
				switch (this)
				{
					case VectorMap.Itype.Int8:
					{
						return @in.ReadByte();
					}

					case VectorMap.Itype.Int16:
					{
						return @in.ReadShort();
					}

					case VectorMap.Itype.Int32:
					{
						return @in.ReadInt();
					}

					default:
					{
						throw new Exception("Unknown itype: " + this);
					}
				}
			}

			/// <summary>Write an integer of this type to the given output stream</summary>
			/// <exception cref="System.IO.IOException"/>
			public void Write(DataOutputStream @out, int value)
			{
				switch (this)
				{
					case VectorMap.Itype.Int8:
					{
						@out.WriteByte(value);
						break;
					}

					case VectorMap.Itype.Int16:
					{
						@out.WriteShort(value);
						break;
					}

					case VectorMap.Itype.Int32:
					{
						@out.WriteInt(value);
						break;
					}

					default:
					{
						throw new Exception("Unknown itype: " + this);
					}
				}
			}
		}

		/// <summary>Create an empty word vector storage.</summary>
		public VectorMap()
			: base(1024)
		{
		}

		/// <summary>Initialize word vectors from a given map.</summary>
		/// <param name="vectors">The word vectors as a simple map.</param>
		public VectorMap(IDictionary<string, float[]> vectors)
			: base(vectors)
		{
		}

		/// <summary>Write the word vectors to a file.</summary>
		/// <param name="file">The file to write to.</param>
		/// <exception cref="System.IO.IOException">Thrown if the file could not be written to.</exception>
		public virtual void Serialize(string file)
		{
			using (OutputStream output = new BufferedOutputStream(new FileOutputStream(new File(file))))
			{
				if (file.EndsWith(".gz"))
				{
					using (GZIPOutputStream gzip = new GZIPOutputStream(output))
					{
						Serialize(gzip);
					}
				}
				else
				{
					Serialize(output);
				}
			}
		}

		/// <summary>Write the word vectors to an output stream.</summary>
		/// <remarks>
		/// Write the word vectors to an output stream. The stream is not closed on finishing
		/// the function.
		/// </remarks>
		/// <param name="out">The stream to write to.</param>
		/// <exception cref="System.IO.IOException">Thrown if the stream could not be written to.</exception>
		public virtual void Serialize(OutputStream @out)
		{
			DataOutputStream dataOut = new DataOutputStream(@out);
			// Write some length statistics
			int maxKeyLength = 0;
			int vectorLength = 0;
			foreach (KeyValuePair<string, float[]> entry in this)
			{
				maxKeyLength = Math.Max(Sharpen.Runtime.GetBytesForString(entry.Key).Length, maxKeyLength);
				vectorLength = entry.Value.Length;
			}
			VectorMap.Itype keyIntType = VectorMap.Itype.GetType(maxKeyLength);
			// Write the key length
			dataOut.WriteInt(maxKeyLength);
			// Write the vector dim
			dataOut.WriteInt(vectorLength);
			// Write the size of the dataset
			dataOut.WriteInt(this.Count);
			foreach (KeyValuePair<string, float[]> entry_1 in this)
			{
				// Write the length of the key
				byte[] key = Sharpen.Runtime.GetBytesForString(entry_1.Key);
				keyIntType.Write(dataOut, key.Length);
				dataOut.Write(key);
				// Write the vector
				foreach (float v in entry_1.Value)
				{
					dataOut.WriteShort(FromFloat(v));
				}
			}
		}

		/// <summary>Read word vectors from a file or classpath or url.</summary>
		/// <param name="file">The file to read from.</param>
		/// <returns>The vectors in the file.</returns>
		/// <exception cref="System.IO.IOException">Thrown if we could not read from the resource</exception>
		public static VectorMap Deserialize(string file)
		{
			using (InputStream input = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(file))
			{
				return Deserialize(input);
			}
		}

		/// <summary>Read word vectors from an input stream.</summary>
		/// <remarks>Read word vectors from an input stream. The stream is not closed on finishing the function.</remarks>
		/// <param name="in">The stream to read from. This is not closed.</param>
		/// <returns>The word vectors encoded on the stream.</returns>
		/// <exception cref="System.IO.IOException">Thrown if we could not read from the stream.</exception>
		public static VectorMap Deserialize(InputStream @in)
		{
			DataInputStream dataIn = new DataInputStream(@in);
			// Read the max key length
			VectorMap.Itype keyIntType = VectorMap.Itype.GetType(dataIn.ReadInt());
			// Read the vector dimensionality
			int dim = dataIn.ReadInt();
			// Read the size of the dataset
			int size = dataIn.ReadInt();
			// Read the vectors
			VectorMap vectors = new VectorMap();
			for (int i = 0; i < size; ++i)
			{
				// Read the key
				int strlen = keyIntType.Read(dataIn);
				byte[] buffer = new byte[strlen];
				if (dataIn.Read(buffer, 0, strlen) != strlen)
				{
					throw new IOException("Could not read string buffer fully!");
				}
				string key = Sharpen.Runtime.GetStringForBytes(buffer);
				// Read the vector
				float[] vector = new float[dim];
				for (int k = 0; k < vector.Length; ++k)
				{
					vector[k] = ToFloat(dataIn.ReadShort());
				}
				// Add the key/value
				vectors[key] = vector;
			}
			return vectors;
		}

		/// <summary>Read the Word2Vec word vector flat txt file.</summary>
		/// <param name="file">The word2vec text file.</param>
		/// <returns>The word vectors in the file.</returns>
		public static VectorMap ReadWord2Vec(string file)
		{
			VectorMap vectors = new VectorMap();
			int dim = -1;
			foreach (string line in IOUtils.ReadLines(file))
			{
				string[] split = line.ToLower().Split("\\s+");
				if (split.Length < 100)
				{
					continue;
				}
				float[] vector = new float[split.Length - 1];
				if (dim == -1)
				{
					dim = vector.Length;
				}
				System.Diagnostics.Debug.Assert(dim == vector.Length);
				for (int i = 1; i < split.Length; i++)
				{
					vector[i - 1] = float.ParseFloat(split[i]);
				}
				ArrayMath.L2normalize(vector);
				vectors[split[0]] = vector;
			}
			return vectors;
		}

		public override bool Equals(object other)
		{
			if (other is IDictionary)
			{
				try
				{
					IDictionary<string, float[]> otherMap = (IDictionary<string, float[]>)other;
					// Key sets have the same size
					if (this.Keys.Count != otherMap.Keys.Count)
					{
						return false;
					}
					// Entries are the same
					foreach (KeyValuePair<string, float[]> entry in this)
					{
						float[] otherValue = otherMap[entry.Key];
						// Null checks
						if (otherValue == null && entry.Value != null)
						{
							return false;
						}
						if (otherValue != null && entry.Value == null)
						{
							return false;
						}
						// Entries are the same
						//noinspection ConstantConditions
						if (entry.Value != null && otherValue != null)
						{
							// Vectors are the same length
							if (entry.Value.Length != otherValue.Length)
							{
								return false;
							}
							// Vectors are the same value
							for (int i = 0; i < otherValue.Length; ++i)
							{
								if (!SameFloat(entry.Value[i], otherValue[i]))
								{
									return false;
								}
							}
						}
					}
					return true;
				}
				catch (InvalidCastException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return Keys.GetHashCode();
		}

		public override string ToString()
		{
			return "VectorMap[" + this.Count + "]";
		}

		/// <summary>The check to see if two floats are "close enough."</summary>
		private static bool SameFloat(float a, float b)
		{
			float absDiff = System.Math.Abs(a - b);
			float absA = System.Math.Abs(a);
			float absB = System.Math.Abs(b);
			return absDiff < 1e-10 || absDiff < System.Math.Max(absA, absB) / 100.0f || (absA < 1e-5 && absB < 1e-5);
		}

		/// <summary>From  http://stackoverflow.com/questions/6162651/half-precision-floating-point-in-java</summary>
		private static float ToFloat(short hbits)
		{
			int mant = hbits & unchecked((int)(0x03ff));
			// 10 bits mantissa
			int exp = hbits & unchecked((int)(0x7c00));
			// 5 bits exponent
			if (exp == unchecked((int)(0x7c00)))
			{
				// NaN/Inf
				exp = unchecked((int)(0x3fc00));
			}
			else
			{
				// -> NaN/Inf
				if (exp != 0)
				{
					// normalized value
					exp += unchecked((int)(0x1c000));
					// exp - 15 + 127
					if (mant == 0 && exp > unchecked((int)(0x1c400)))
					{
						// smooth transition
						return Sharpen.Runtime.IntBitsToFloat((hbits & unchecked((int)(0x8000))) << 16 | exp << 13 | unchecked((int)(0x3ff)));
					}
				}
				else
				{
					if (mant != 0)
					{
						// && exp==0 -> subnormal
						exp = unchecked((int)(0x1c400));
						do
						{
							// make it normal
							mant <<= 1;
							// mantissa * 2
							exp -= unchecked((int)(0x400));
						}
						while ((mant & unchecked((int)(0x400))) == 0);
						// decrease exp by 1
						// while not normal
						mant &= unchecked((int)(0x3ff));
					}
				}
			}
			// discard subnormal bit
			// else +/-0 -> +/-0
			return Sharpen.Runtime.IntBitsToFloat((hbits & unchecked((int)(0x8000))) << 16 | (exp | mant) << 13);
		}

		// combine all parts
		// sign  << ( 31 - 15 )
		// value << ( 23 - 10 )
		/// <summary>From  http://stackoverflow.com/questions/6162651/half-precision-floating-point-in-java</summary>
		private static short FromFloat(float fval)
		{
			int fbits = Sharpen.Runtime.FloatToIntBits(fval);
			int sign = (int)(((uint)fbits) >> 16) & unchecked((int)(0x8000));
			// sign only
			int val = (fbits & unchecked((int)(0x7fffffff))) + unchecked((int)(0x1000));
			// rounded value
			if (val >= unchecked((int)(0x47800000)))
			{
				// might be or become NaN/Inf
				// avoid Inf due to rounding
				if ((fbits & unchecked((int)(0x7fffffff))) >= unchecked((int)(0x47800000)))
				{
					// is or must become NaN/Inf
					if (val < unchecked((int)(0x7f800000)))
					{
						// was value but too large
						return (short)(sign | unchecked((int)(0x7c00)));
					}
					// make it +/-Inf
					return (short)(sign | unchecked((int)(0x7c00)) | (int)(((uint)(fbits & unchecked((int)(0x007fffff)))) >> 13));
				}
				// remains +/-Inf or NaN
				// keep NaN (and Inf) bits
				return (short)(sign | unchecked((int)(0x7bff)));
			}
			// unrounded not quite Inf
			if (val >= unchecked((int)(0x38800000)))
			{
				// remains normalized value
				return (short)(sign | (int)(((uint)val - unchecked((int)(0x38000000))) >> 13));
			}
			// exp - 127 + 15
			if (val < unchecked((int)(0x33000000)))
			{
				// too small for subnormal
				return (short)sign;
			}
			// becomes +/-0
			val = (int)(((uint)(fbits & unchecked((int)(0x7fffffff)))) >> 23);
			// tmp exp for subnormal calc
			return (short)(sign | ((int)(((uint)(fbits & unchecked((int)(0x7fffff)) | unchecked((int)(0x800000))) + ((int)(((uint)unchecked((int)(0x800000))) >> val - 102))) >> 126 - val)));
		}
		// add subnormal bit
		// round depending on cut off
		// div by 2^(1-(exp-127+15)) and >> 13 | exp=0
	}
}
