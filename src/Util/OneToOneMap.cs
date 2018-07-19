using System;
using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>One to one map that allows to get a value for a key and a key for a value in O(1).</summary>
	/// <author>jonathanberant</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class OneToOneMap<L, R>
	{
		[System.Serializable]
		public class OneToOneMapException : Exception
		{
			public OneToOneMapException(string iDesc)
				: base(iDesc)
			{
			}

			private const long serialVersionUID = 7743164489912070054L;
		}

		private IDictionary<L, R> m_leftAsKey;

		private IDictionary<R, L> m_rightAsKey;

		public OneToOneMap()
		{
			//------------------------------------------------------------
			m_leftAsKey = Generics.NewHashMap();
			m_rightAsKey = Generics.NewHashMap();
		}

		public virtual bool IsEmpty()
		{
			return m_leftAsKey.IsEmpty();
		}

		public virtual int Size()
		{
			return m_leftAsKey.Count;
		}

		/// <exception cref="Edu.Stanford.Nlp.Util.OneToOneMap.OneToOneMapException"/>
		public virtual void Put(L l, R r)
		{
			bool hasLeft = m_leftAsKey.Contains(l);
			bool hasRight = m_rightAsKey.Contains(r);
			if (hasLeft != hasRight)
			{
				throw new OneToOneMap.OneToOneMapException("Error: cannot insert multiple keys with the same value");
			}
			m_leftAsKey[l] = r;
			m_rightAsKey[r] = l;
		}

		public virtual R GetLeftAsKey(L l)
		{
			return m_leftAsKey[l];
		}

		public virtual L GetRightAsKey(R r)
		{
			return m_rightAsKey[r];
		}

		public virtual R RemoveLeftAsKey(L l)
		{
			R r = Sharpen.Collections.Remove(m_leftAsKey, l);
			if (r != null)
			{
				Sharpen.Collections.Remove(m_rightAsKey, r);
			}
			return r;
		}

		public virtual L RemoveRightAsKey(R r)
		{
			L l = Sharpen.Collections.Remove(m_rightAsKey, r);
			if (l != null)
			{
				Sharpen.Collections.Remove(m_leftAsKey, l);
			}
			return l;
		}

		public virtual ICollection<R> ValuesLeftAsKey()
		{
			return m_leftAsKey.Values;
		}

		public virtual ICollection<L> ValuesRightAsKey()
		{
			return m_rightAsKey.Values;
		}

		public virtual ICollection<KeyValuePair<L, R>> EntrySetLeftAsKey()
		{
			return m_leftAsKey;
		}

		public virtual ICollection<KeyValuePair<R, L>> EntrySetRightAsKey()
		{
			return m_rightAsKey;
		}

		public virtual bool ContainsLeftAsKey(L l)
		{
			return m_leftAsKey.Contains(l);
		}

		public virtual bool ContainsRightAsKey(R r)
		{
			return m_rightAsKey.Contains(r);
		}

		public virtual void Clear()
		{
			m_leftAsKey.Clear();
			m_rightAsKey.Clear();
		}

		private const long serialVersionUID = 1L;
	}
}
