using System.IO;
using Java.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>This implements an unordered word-list resource for Ssurgeon</summary>
	/// <author>Eric Yeh</author>
	public class SsurgeonWordlist
	{
		private const string WordElt = "word";

		private string id;

		private HashSet<string> words = new HashSet<string>();

		public override string ToString()
		{
			StringWriter buf = new StringWriter();
			buf.Write("Ssurgeon Wordlist Resource, id=");
			buf.Write(id);
			buf.Write(", elements=(");
			foreach (string word in words)
			{
				buf.Write(" ");
				buf.Write(word);
			}
			buf.Write(")");
			return buf.ToString();
		}

		public virtual string GetID()
		{
			return id;
		}

		/// <summary>Reconstructs the resource from the XML file</summary>
		public SsurgeonWordlist(IElement rootElt)
		{
			id = rootElt.GetAttribute("id");
			INodeList wordEltNL = rootElt.GetElementsByTagName(WordElt);
			for (int i = 0; i < wordEltNL.GetLength(); i++)
			{
				INode node = wordEltNL.Item(i);
				if (node.GetNodeType() == NodeConstants.ElementNode)
				{
					string word = Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.GetEltText((IElement)node);
					words.Add(word);
				}
			}
		}

		public virtual bool Contains(string testWord)
		{
			return words.Contains(testWord);
		}

		public static void Main(string[] args)
		{
		}
		// TODO Auto-generated method stub
	}
}
