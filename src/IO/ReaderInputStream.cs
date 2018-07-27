using System;
using System.IO;



namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Adapts a <code>Reader</code> as an <code>InputStream</code>.</summary>
	/// <remarks>
	/// Adapts a <code>Reader</code> as an <code>InputStream</code>.
	/// Adapted from <CODE>StringInputStream</CODE>.
	/// </remarks>
	public class ReaderInputStream : InputStream
	{
		/// <summary>Source Reader</summary>
		private Reader @in;

		private string encoding = Runtime.GetProperty("file.encoding");

		private byte[] slack;

		private int begin;

		/// <summary>
		/// Construct a <CODE>ReaderInputStream</CODE>
		/// for the specified <CODE>Reader</CODE>.
		/// </summary>
		/// <param name="reader"><CODE>Reader</CODE>.  Must not be <code>null</code>.</param>
		public ReaderInputStream(Reader reader)
		{
			/*
			* Copyright 2004-2005 The Apache Software Foundation.
			*
			*  Licensed under the Apache License, Version 2.0 (the "License");
			*  you may not use this file except in compliance with the License.
			*  You may obtain a copy of the License at
			*
			*      http://www.apache.org/licenses/LICENSE-2.0
			*
			*  Unless required by applicable law or agreed to in writing, software
			*  distributed under the License is distributed on an "AS IS" BASIS,
			*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
			*  See the License for the specific language governing permissions and
			*  limitations under the License.
			*
			*/
			@in = reader;
		}

		/// <summary>
		/// Construct a <CODE>ReaderInputStream</CODE>
		/// for the specified <CODE>Reader</CODE>,
		/// with the specified encoding.
		/// </summary>
		/// <param name="reader">non-null <CODE>Reader</CODE>.</param>
		/// <param name="encoding">non-null <CODE>String</CODE> encoding.</param>
		public ReaderInputStream(Reader reader, string encoding)
			: this(reader)
		{
			if (encoding == null)
			{
				throw new ArgumentException("encoding must not be null");
			}
			else
			{
				this.encoding = encoding;
			}
		}

		/// <summary>Reads from the <CODE>Reader</CODE>, returning the same value.</summary>
		/// <returns>the value of the next character in the <CODE>Reader</CODE>.</returns>
		/// <exception>
		/// IOException
		/// if the original <code>Reader</code> fails to be read
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public override int Read()
		{
			lock (this)
			{
				if (@in == null)
				{
					throw new IOException("Stream Closed");
				}
				byte result;
				if (slack != null && begin < slack.Length)
				{
					result = slack[begin];
					if (++begin == slack.Length)
					{
						slack = null;
					}
				}
				else
				{
					byte[] buf = new byte[1];
					if (Read(buf, 0, 1) <= 0)
					{
						result = unchecked((byte)(-1));
					}
					result = buf[0];
				}
				if (((sbyte)result) < -1)
				{
					result += 256;
				}
				return result;
			}
		}

		/// <summary>Reads from the <code>Reader</code> into a byte array</summary>
		/// <param name="b">the byte array to read into</param>
		/// <param name="off">the offset in the byte array</param>
		/// <param name="len">the length in the byte array to fill</param>
		/// <returns>
		/// the actual number read into the byte array, -1 at
		/// the end of the stream
		/// </returns>
		/// <exception>
		/// IOException
		/// if an error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public override int Read(byte[] b, int off, int len)
		{
			lock (this)
			{
				if (@in == null)
				{
					throw new IOException("Stream Closed");
				}
				while (slack == null)
				{
					char[] buf = new char[len];
					// might read too much
					int n = @in.Read(buf);
					if (n == -1)
					{
						return -1;
					}
					if (n > 0)
					{
						slack = Sharpen.Runtime.GetBytesForString(new string(buf, 0, n), encoding);
						begin = 0;
					}
				}
				if (len > slack.Length - begin)
				{
					len = slack.Length - begin;
				}
				System.Array.Copy(slack, begin, b, off, len);
				if ((begin += len) >= slack.Length)
				{
					slack = null;
				}
				return len;
			}
		}

		/// <summary>Marks the read limit of the StringReader.</summary>
		/// <param name="limit">
		/// the maximum limit of bytes that can be read before the
		/// mark position becomes invalid
		/// </param>
		public override void Mark(int limit)
		{
			lock (this)
			{
				try
				{
					@in.Mark(limit);
				}
				catch (IOException ioe)
				{
					throw new Exception(ioe.Message);
				}
			}
		}

		/// <returns>the current number of bytes ready for reading</returns>
		/// <exception>
		/// IOException
		/// if an error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public override int Available()
		{
			lock (this)
			{
				if (@in == null)
				{
					throw new IOException("Stream Closed");
				}
				if (slack != null)
				{
					return slack.Length - begin;
				}
				if (@in.Ready())
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}

		/// <returns>false - mark is not supported</returns>
		public override bool MarkSupported()
		{
			return false;
		}

		// would be imprecise
		/// <summary>Resets the StringReader.</summary>
		/// <exception>
		/// IOException
		/// if the StringReader fails to be reset
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public override void Reset()
		{
			lock (this)
			{
				if (@in == null)
				{
					throw new IOException("Stream Closed");
				}
				slack = null;
				@in.Reset();
			}
		}

		/// <summary>Closes the Stringreader.</summary>
		/// <exception>
		/// IOException
		/// if the original StringReader fails to be closed
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public override void Close()
		{
			lock (this)
			{
				if (@in != null)
				{
					@in.Close();
					slack = null;
					@in = null;
				}
			}
		}
	}
}
