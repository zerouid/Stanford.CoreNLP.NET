


namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// Stores a triple of an extractor ID, a feature value (derived from history)
	/// and a y (tag) value.
	/// </summary>
	/// <remarks>
	/// Stores a triple of an extractor ID, a feature value (derived from history)
	/// and a y (tag) value.  Used to compute a feature number in the loglinear
	/// model.
	/// </remarks>
	/// <author>Kristina Toutanova, with minor changes by Daniel Cer</author>
	/// <version>1.0</version>
	public class FeatureKey
	{
		internal int num;

		internal string val;

		internal string tag;

		public FeatureKey()
		{
		}

		protected internal FeatureKey(int num, string val, string tag)
		{
			// this object is used as a hash key and such instances should be treated as read-only
			// TODO: refactor code so that FeatureKeys are immutable? Or is the object reuse in a tight loop worth it?
			this.num = num;
			this.val = val;
			this.tag = tag;
		}

		public override string ToString()
		{
			return int.ToString(num) + ' ' + val + ' ' + tag;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void Save(DataOutputStream f)
		{
			f.WriteInt(num);
			f.WriteUTF(val);
			f.WriteUTF(tag);
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void Read(DataInputStream inf)
		{
			num = inf.ReadInt();
			// mg2008: slight speedup:
			val = inf.ReadUTF();
			// intern the tag strings as they are read, since there are few of them. This saves tons of memory.
			tag = inf.ReadUTF();
			hashCode = 0;
		}

		private int hashCode = 0;

		/* --------------------
		* this was to clean-up some empties left from before
		*
		String cleanup(String val) {
		
		int index = val.indexOf('!');
		if (index > -1) {
		String first = val.substring(0, index);
		String last = val.substring(index + 1);
		System.out.println("in " + first + " " + last);
		first = TestSentence.toNice(first);
		last = TestSentence.toNice(last);
		System.out.println("out " + first + " " + last);
		return first + '!' + last;
		} else {
		return val;
		}
		}
		
		---------- */
		public override int GetHashCode()
		{
			/* I'm not sure why this is happening, and i really don't want to
			spend a month tracing it down. -wmorgan. */
			//if (val == null) return num << 16 ^ 1 << 5 ^ tag.hashCode();
			//return num << 16 ^ val.hashCode() << 5 ^ tag.hashCode();
			if (hashCode == 0)
			{
				int hNum = int.RotateLeft(num, 16);
				int hVal = int.RotateLeft(val.GetHashCode(), 5);
				hashCode = hNum ^ hVal ^ tag.GetHashCode();
			}
			return hashCode;
		}

		public override bool Equals(object o)
		{
			System.Diagnostics.Debug.Assert((o is Edu.Stanford.Nlp.Tagger.Maxent.FeatureKey));
			Edu.Stanford.Nlp.Tagger.Maxent.FeatureKey f1 = (Edu.Stanford.Nlp.Tagger.Maxent.FeatureKey)o;
			return (num == f1.num) && (tag.Equals(f1.tag)) && (val.Equals(f1.val));
		}
	}
}
