using System.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class History
	{
		internal int start;

		internal int end;

		internal int current;

		internal readonly PairsHolder pairs;

		internal readonly Extractors extractors;

		internal History(PairsHolder pairs, Extractors extractors)
		{
			// this is the index of the first word of the sentence
			//this is the index of the last word in the sentence - the dot
			// this is the index of the current word
			this.pairs = pairs;
			this.extractors = extractors;
		}

		internal History(int start, int end, int current, PairsHolder pairs, Extractors extractors)
		{
			this.pairs = pairs;
			this.extractors = extractors;
			Init(start, end, current);
		}

		internal virtual void Init(int start, int end, int current)
		{
			this.start = start;
			this.end = end;
			this.current = current;
		}

		/*
		public void save(DataOutputStream rf) {
		try {
		rf.writeInt(start);
		rf.writeInt(end);
		rf.writeInt(current);
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		
		
		public void read(InDataStreamFile rf) {
		try {
		start = rf.readInt();
		end = rf.readInt();
		current = rf.readInt();
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		*/
		private string GetX(int index)
		{
			// get the string by the index in x
			return extractors.Get(index).Extract(this);
		}

		public virtual string[] GetX()
		{
			string[] x = new string[extractors.Size()];
			for (int i = 0; i < x.Length; i++)
			{
				x[i] = GetX(i);
			}
			return x;
		}

		internal virtual void Print(TextWriter ps)
		{
			string[] str = GetX();
			foreach (string aStr in str)
			{
				ps.Write(aStr);
				ps.Write('\t');
			}
			ps.WriteLine();
		}

		public virtual void PrintSent()
		{
			Print(System.Console.Out);
			for (int i = this.start; i < this.end; i++)
			{
				System.Console.Out.Write(pairs.GetTag(i) + ' ' + pairs.GetWord(i) + '\t');
			}
			System.Console.Out.WriteLine();
		}

		protected internal virtual void SetTag(int pos, string tag)
		{
			pairs.SetTag(pos + start, tag);
		}

		protected internal virtual void Set(int start, int end, int current)
		{
			this.start = start;
			this.end = end;
			this.current = current;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			string[] str = GetX();
			foreach (string aStr in str)
			{
				sb.Append(aStr).Append('\t');
			}
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < extractors.Size(); i++)
			{
				sb.Append(GetX(i));
			}
			return sb.ToString().GetHashCode();
		}

		public override bool Equals(object h1)
		{
			return h1 is Edu.Stanford.Nlp.Tagger.Maxent.History && extractors.Equals(this, (Edu.Stanford.Nlp.Tagger.Maxent.History)h1);
		}
	}
}
