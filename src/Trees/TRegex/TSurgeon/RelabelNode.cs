using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;




namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class RelabelNode : TsurgeonPattern
	{
		internal const string regexPatternString = "((?:(?:[^/]*[^/\\\\])|\\\\/)*(?:\\\\\\\\)*)";

		internal static readonly Pattern regexPattern = Pattern.Compile("/" + regexPatternString + "/");

		/// <summary>This pattern finds relabel snippets that use a named node.</summary>
		internal const string nodePatternString = "(=\\{[a-zA-Z0-9_]+\\})";

		internal static readonly Pattern nodePattern = Pattern.Compile(nodePatternString);

		/// <summary>This pattern finds relabel snippets that use a captured variable.</summary>
		internal const string variablePatternString = "(%\\{[a-zA-Z0-9_]+\\})";

		internal static readonly Pattern variablePattern = Pattern.Compile(variablePatternString);

		/// <summary>
		/// Finds one chunk of a general relabel operation, either named node
		/// or captured variable
		/// </summary>
		internal const string oneGeneralReplacement = ("(" + nodePatternString + "|" + variablePatternString + ")");

		internal static readonly Pattern oneGeneralReplacementPattern = Pattern.Compile(oneGeneralReplacement);

		/// <summary>Identifies a node using the regex replacement strategy.</summary>
		internal static readonly Pattern substPattern = Pattern.Compile("/" + regexPatternString + "/(.*)/");

		internal enum RelabelMode
		{
			Fixed,
			Regex
		}

		private readonly RelabelNode.RelabelMode mode;

		private readonly string newLabel;

		private readonly Pattern labelRegex;

		private readonly string replacementString;

		private readonly IList<string> replacementPieces;

		public RelabelNode(TsurgeonPattern child, string newLabel)
			: base("relabel", new TsurgeonPattern[] { child })
		{
			// Overly complicated pattern to identify regexes surrounded by /,
			// possibly with / escaped inside the regex.  
			// The purpose of the [^/]*[^/\\\\] is to match characters that
			// aren't / and to allow escaping of other characters.
			// The purpose of the \\\\/ is to allow escaped / inside the pattern.
			// The purpose of the \\\\\\\\ is to allow escaped \ at the end of
			// the pattern, so you can match, for example, /\\/.  There need to
			// be 8x\ because both java and regexes need escaping, resulting in 4x.
			Java.Util.Regex.Matcher m1 = substPattern.Matcher(newLabel);
			if (m1.Matches())
			{
				mode = RelabelNode.RelabelMode.Regex;
				this.labelRegex = Pattern.Compile(m1.Group(1));
				this.replacementString = m1.Group(2);
				replacementPieces = new List<string>();
				Java.Util.Regex.Matcher generalMatcher = oneGeneralReplacementPattern.Matcher(m1.Group(2));
				int lastPosition = 0;
				while (generalMatcher.Find())
				{
					if (generalMatcher.Start() > lastPosition)
					{
						replacementPieces.Add(Sharpen.Runtime.Substring(replacementString, lastPosition, generalMatcher.Start()));
					}
					lastPosition = generalMatcher.End();
					string piece = generalMatcher.Group();
					if (piece.Equals(string.Empty))
					{
						continue;
					}
					replacementPieces.Add(generalMatcher.Group());
				}
				if (lastPosition < replacementString.Length)
				{
					replacementPieces.Add(Sharpen.Runtime.Substring(replacementString, lastPosition));
				}
				this.newLabel = null;
			}
			else
			{
				mode = RelabelNode.RelabelMode.Fixed;
				Java.Util.Regex.Matcher m2 = regexPattern.Matcher(newLabel);
				if (m2.Matches())
				{
					// fixed relabel but surrounded by regex slashes
					string unescapedLabel = m2.Group(1);
					this.newLabel = RemoveEscapeSlashes(unescapedLabel);
				}
				else
				{
					// just a node name to relabel to
					this.newLabel = newLabel;
				}
				this.replacementString = null;
				this.replacementPieces = null;
				this.labelRegex = null;
			}
		}

		private static string RemoveEscapeSlashes(string @in)
		{
			StringBuilder @out = new StringBuilder();
			int len = @in.Length;
			bool lastIsBackslash = false;
			for (int i = 0; i < len; i++)
			{
				char ch = @in[i];
				if (ch == '\\')
				{
					if (lastIsBackslash || i == len - 1)
					{
						@out.Append(ch);
						lastIsBackslash = false;
					}
					else
					{
						lastIsBackslash = true;
					}
				}
				else
				{
					@out.Append(ch);
					lastIsBackslash = false;
				}
			}
			return @out.ToString();
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new RelabelNode.RelabelMatcher(this, newNodeNames, coindexer);
		}

		private class RelabelMatcher : TsurgeonMatcher
		{
			public RelabelMatcher(RelabelNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree nodeToRelabel = this.childMatcher[0].Evaluate(tree, tregex);
				switch (this._enclosing.mode)
				{
					case RelabelNode.RelabelMode.Fixed:
					{
						nodeToRelabel.Label().SetValue(this._enclosing.newLabel);
						break;
					}

					case RelabelNode.RelabelMode.Regex:
					{
						Matcher m = this._enclosing.labelRegex.Matcher(nodeToRelabel.Label().Value());
						StringBuilder label = new StringBuilder();
						foreach (string chunk in this._enclosing.replacementPieces)
						{
							if (RelabelNode.variablePattern.Matcher(chunk).Matches())
							{
								string name = Sharpen.Runtime.Substring(chunk, 2, chunk.Length - 1);
								label.Append(Matcher.QuoteReplacement(tregex.GetVariableString(name)));
							}
							else
							{
								if (RelabelNode.nodePattern.Matcher(chunk).Matches())
								{
									string name = Sharpen.Runtime.Substring(chunk, 2, chunk.Length - 1);
									label.Append(Matcher.QuoteReplacement(tregex.GetNode(name).Value()));
								}
								else
								{
									label.Append(chunk);
								}
							}
						}
						nodeToRelabel.Label().SetValue(m.ReplaceAll(label.ToString()));
						break;
					}

					default:
					{
						throw new AssertionError("Unsupported relabel mode " + this._enclosing.mode);
					}
				}
				return tree;
			}

			private readonly RelabelNode _enclosing;
		}

		public override string ToString()
		{
			string result;
			switch (mode)
			{
				case RelabelNode.RelabelMode.Fixed:
				{
					return label + '(' + children[0].ToString() + ',' + newLabel + ')';
				}

				case RelabelNode.RelabelMode.Regex:
				{
					return label + '(' + children[0].ToString() + ',' + labelRegex.ToString() + ',' + replacementString + ')';
				}

				default:
				{
					throw new AssertionError("Unsupported relabel mode " + mode);
				}
			}
		}
	}
}
