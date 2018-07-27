using System;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>A simple data structure for some tag counts.</summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class CountWrapper
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.CountWrapper));

		private string word;

		private int countPart;

		private int countThat;

		private int countIn;

		private int countRB;

		public CountWrapper()
		{
		}

		protected internal CountWrapper(string word, int countPart, int countThat, int countIn, int countRB)
		{
			//private Dictionary dictLocal = new Dictionary();
			//private static final String rpTag = "RP";
			//private static final String inTag = "IN";
			//private static final String rbTag = "RB";
			System.Diagnostics.Debug.Assert((word != null));
			this.word = word;
			this.countPart = countPart;
			this.countThat = countThat;
			this.countIn = countIn;
			this.countRB = countRB;
		}

		protected internal virtual void IncThat()
		{
			this.countThat++;
		}

		public virtual int GetCountPart()
		{
			return countPart;
		}

		public virtual int GetCountThat()
		{
			return countThat;
		}

		public virtual int GetCountIn()
		{
			return countIn;
		}

		public virtual int GetCountRB()
		{
			return countRB;
		}

		public virtual string GetWord()
		{
			return word;
		}

		public override int GetHashCode()
		{
			return word.GetHashCode();
		}

		/// <summary>
		/// Equality is tested only on the word, and not the various counts
		/// that are maintained.
		/// </summary>
		/// <param name="obj">Item tested for equality</param>
		/// <returns>Whether equal</returns>
		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!(obj is Edu.Stanford.Nlp.Tagger.Maxent.CountWrapper))
			{
				return false;
			}
			Edu.Stanford.Nlp.Tagger.Maxent.CountWrapper cw = (Edu.Stanford.Nlp.Tagger.Maxent.CountWrapper)obj;
			return word.Equals(cw.word);
		}

		protected internal virtual void Save(DataOutputStream rf)
		{
			try
			{
				rf.WriteInt(word.Length);
				rf.Write(Sharpen.Runtime.GetBytesForString(word));
				rf.WriteInt(countPart);
				rf.WriteInt(countThat);
				rf.WriteInt(countIn);
				rf.WriteInt(countRB);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		protected internal virtual void Read(DataInputStream rf)
		{
			try
			{
				int len = rf.ReadInt();
				byte[] buff = new byte[len];
				if (rf.Read(buff) != len)
				{
					log.Info("Error: rewrite CountWrapper.read");
				}
				word = Sharpen.Runtime.GetStringForBytes(buff);
				System.Diagnostics.Debug.Assert((word != null));
				countPart = rf.ReadInt();
				countThat = rf.ReadInt();
				countIn = rf.ReadInt();
				countRB = rf.ReadInt();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
