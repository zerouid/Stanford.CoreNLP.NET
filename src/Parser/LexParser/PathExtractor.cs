using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Extracts raw Nary rules from a treebank.</summary>
	/// <remarks>
	/// Extracts raw Nary rules from a treebank. They are returned as a Map from
	/// passive constituents to Lists of right-hand side rule "paths", each of which is a List.
	/// </remarks>
	internal class PathExtractor : AbstractTreeExtractor<IDictionary<string, IList<IList<string>>>>
	{
		private const string End = "END";

		private IDictionary<string, IList<IList<string>>> allPaths = Generics.NewHashMap();

		private IHeadFinder hf;

		public PathExtractor(IHeadFinder hf, Options op)
			: base(op)
		{
			//protected final Index<String> stateIndex;
			this.hf = hf;
		}

		private IList<IList<string>> GetList(string key)
		{
			IList<IList<string>> result = allPaths[key];
			if (result == null)
			{
				result = new List<IList<string>>();
				allPaths[key] = result;
			}
			return result;
		}

		protected internal override void TallyInternalNode(Tree lt, double weight)
		{
			Tree[] children = lt.Children();
			Tree headChild = hf.DetermineHead(lt);
			if (children.Length == 1)
			{
				return;
			}
			IList<string> path = new List<string>();
			// determine which is the head
			int headLoc = -1;
			for (int i = 0; i < children.Length; i++)
			{
				if (children[i] == headChild)
				{
					headLoc = i;
				}
			}
			path.Add(children[headLoc].Label().Value());
			if (headLoc == 0)
			{
				// we are finishing on the right
				for (int i_1 = headLoc + 1; i_1 < children.Length - 1; i_1++)
				{
					path.Add(children[i_1].Label().Value() + ">");
				}
				if (op.trainOptions.markFinalStates)
				{
					path.Add(children[children.Length - 1].Label().Value() + "]");
				}
				else
				{
					path.Add(children[children.Length - 1].Label().Value() + ">");
				}
			}
			else
			{
				// we are finishing on the left
				for (int i_1 = headLoc + 1; i_1 < children.Length; i_1++)
				{
					path.Add(children[i_1].Label().Value() + ">");
				}
				for (int i_2 = headLoc - 1; i_2 > 0; i_2--)
				{
					path.Add(children[i_2].Label().Value() + "<");
				}
				if (op.trainOptions.markFinalStates)
				{
					path.Add(children[0].Label().Value() + "[");
				}
				else
				{
					path.Add(children[0].Label().Value() + "<");
				}
			}
			path.Add(End);
			// add epsilon at the end
			string label = lt.Label().Value();
			IList<IList<string>> l = GetList(label);
			l.Add(path);
		}

		public override IDictionary<string, IList<IList<string>>> FormResult()
		{
			return allPaths;
		}
	}
}
