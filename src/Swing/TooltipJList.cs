




namespace Edu.Stanford.Nlp.Swing
{
	/// <summary>Simple list class that extends JList and adds tool tip functionality to the list.</summary>
	/// <remarks>
	/// Simple list class that extends JList and adds tool tip functionality to the list.  Tool tips are automatically
	/// wrapped to a specific length (default 80 chars) while preserving word boundaries.
	/// </remarks>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class TooltipJList : JList
	{
		private static int ProblemLineLength = 80;

		public TooltipJList()
			: base()
		{
		}

		public TooltipJList(IListModel model)
			: this(model, ProblemLineLength)
		{
		}

		public TooltipJList(IListModel model, int lineWrapLength)
			: base(model)
		{
			// todo: generify once we move to Java 8, but JList wasn't generic in Java 6 so can't do now.
			ProblemLineLength = lineWrapLength;
		}

		public override string GetToolTipText(MouseEvent evt)
		{
			int index = LocationToIndex(evt.GetPoint());
			if (-1 < index)
			{
				StringBuilder s = new StringBuilder();
				string text = GetModel().GetElementAt(index).ToString();
				s.Append("<html>");
				//separate out into lines
				string textLeft = text;
				bool isFirstLine = true;
				while (textLeft.Length > 0)
				{
					string curLine = string.Empty;
					if (textLeft.Length > ProblemLineLength)
					{
						curLine = Sharpen.Runtime.Substring(textLeft, 0, ProblemLineLength);
						textLeft = Sharpen.Runtime.Substring(textLeft, ProblemLineLength, textLeft.Length);
						//check if we're at the end of a word - if not, get us there
						while (curLine[curLine.Length - 1] != ' ' && textLeft.Length > 0)
						{
							curLine = curLine + Sharpen.Runtime.Substring(textLeft, 0, 1);
							textLeft = Sharpen.Runtime.Substring(textLeft, 1, textLeft.Length);
						}
					}
					else
					{
						curLine = textLeft;
						textLeft = string.Empty;
					}
					if (!isFirstLine)
					{
						s.Append("<br>");
					}
					s.Append(curLine);
					if (!isFirstLine)
					{
						s.Append("</br>");
					}
					else
					{
						isFirstLine = false;
					}
				}
				s.Append("</html>");
				return s.ToString();
			}
			else
			{
				return null;
			}
		}
	}
}
