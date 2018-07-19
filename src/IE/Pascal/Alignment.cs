using System.IO;
using System.Text;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>Container class for aligning acronyms.</summary>
	/// <author>Jamie Nicolson</author>
	public class Alignment
	{
		public char[] longForm;

		public char[] shortForm;

		public int[] pointers;

		public Alignment(char[] longForm, char[] shortForm, int[] pointers)
		{
			this.longForm = longForm;
			this.shortForm = shortForm;
			this.pointers = pointers;
		}

		public virtual void Serialize(PrintWriter writer)
		{
			writer.Println(new string(longForm));
			writer.Println(new string(shortForm));
			StringBuilder sb = new StringBuilder();
			foreach (int pointer in pointers)
			{
				sb.Append(pointer + " ");
			}
			writer.Println(sb.ToString());
		}

		/// <exception cref="System.IO.IOException"/>
		public Alignment(BufferedReader reader)
		{
			string line;
			line = reader.ReadLine();
			if (line == null)
			{
				throw new IOException();
			}
			longForm = line.ToCharArray();
			line = reader.ReadLine();
			if (line == null)
			{
				throw new IOException();
			}
			shortForm = line.ToCharArray();
			line = reader.ReadLine();
			if (line == null)
			{
				throw new IOException();
			}
			string[] pstrings = line.Split("\\s+");
			if (pstrings.Length != shortForm.Length)
			{
				throw new IOException("Number of pointers != size of short form");
			}
			pointers = new int[pstrings.Length];
			for (int i = 0; i < pointers.Length; ++i)
			{
				pointers[i] = System.Convert.ToInt32(pstrings[i]);
			}
		}

		public virtual void Print()
		{
			System.Console.Out.WriteLine(ToString());
		}

		public override string ToString()
		{
			return ToString(string.Empty);
		}

		private static readonly char[] spaces = "                      ".ToCharArray();

		public virtual string ToString(string prefix)
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(prefix);
			buf.Append(longForm);
			buf.Append("\n");
			buf.Append(spaces, 0, prefix.Length);
			int l = 0;
			for (int s = 0; s < shortForm.Length; ++s)
			{
				if (pointers[s] == -1)
				{
					continue;
				}
				for (; l < longForm.Length && pointers[s] != l; ++l)
				{
					buf.Append(" ");
				}
				if (l < longForm.Length)
				{
					buf.Append(shortForm[s]);
					++l;
				}
			}
			return buf.ToString();
		}

		public override bool Equals(object o)
		{
			if (o == null || !(o is Edu.Stanford.Nlp.IE.Pascal.Alignment))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Pascal.Alignment cmp = (Edu.Stanford.Nlp.IE.Pascal.Alignment)o;
			return Arrays.Equals(longForm, cmp.longForm) && Arrays.Equals(shortForm, cmp.shortForm) && Arrays.Equals(pointers, cmp.pointers);
		}

		public override int GetHashCode()
		{
			int code = 0;
			foreach (int pointer in pointers)
			{
				code += pointer;
				code *= 31;
			}
			return code;
		}
	}
}
