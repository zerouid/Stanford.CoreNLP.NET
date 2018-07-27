using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	public class WordlistTest : NodeTest
	{
		public enum TYPE
		{
			lemma,
			current_lasttoken,
			lemma_and_currlast,
			word,
			pos
		}

		private WordlistTest.TYPE type;

		private string resourceID;

		private string myID;

		public WordlistTest(string myID, string resourceID, string type, string matchName)
			: base(matchName)
		{
			this.resourceID = resourceID;
			this.myID = myID;
			this.type = WordlistTest.TYPE.ValueOf(type);
		}

		/// <summary>Checks to see if the given node's field matches the resource</summary>
		/// <exception cref="System.Exception"/>
		protected internal override bool Evaluate(IndexedWord node)
		{
			SsurgeonWordlist wl = Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.Inst().GetResource(resourceID);
			if (wl == null)
			{
				throw new Exception("No wordlist resource with ID=" + resourceID);
			}
			if (type == WordlistTest.TYPE.lemma)
			{
				return wl.Contains(node.Lemma().ToLower());
			}
			if (type == WordlistTest.TYPE.current_lasttoken)
			{
				// This is done in special case, where tokens are collapsed.  Here, we
				// take the last token of the current value for the node and compare against
				// that.
				string[] tokens = node.OriginalText().Split("\\s+");
				string lastCurrent = tokens[tokens.Length - 1].ToLower();
				return wl.Contains(lastCurrent);
			}
			else
			{
				if (type == WordlistTest.TYPE.lemma_and_currlast)
				{
					// test against both the lemma and the last current token
					string[] tokens = node.OriginalText().Split("\\s+");
					string lastCurrent = tokens[tokens.Length - 1].ToLower();
					return wl.Contains(node.Lemma().ToLower()) || wl.Contains(lastCurrent);
				}
				else
				{
					if (type == WordlistTest.TYPE.word)
					{
						return wl.Contains(node.Word());
					}
					else
					{
						if (type == WordlistTest.TYPE.pos)
						{
							return wl.Contains(node.Tag());
						}
						else
						{
							return false;
						}
					}
				}
			}
		}

		public override string GetDisplayName()
		{
			return "wordlist-test :type " + type + " :resourceID " + resourceID;
		}

		public override string GetID()
		{
			return myID;
		}
	}
}
