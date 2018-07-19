// MaxentTaggerGUI -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2008 Leland Stanford Junior University
//
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    http://www-nlp.stanford.edu/software/tagger.shtml
using System;
using Java.Awt;
using Java.Awt.Event;
using Java.Lang;
using Javax.Swing;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>A very simple GUI for illustrating the POS tagger tagging text.</summary>
	/// <remarks>
	/// A very simple GUI for illustrating the POS tagger tagging text.
	/// Simple usage: <br />
	/// <code>java -mx300m edu.stanford.nlp.tagger.maxent.MaxentTaggerGUI pathToPOSTaggerModel</code>
	/// <p>
	/// <i>Note:</i> Could still use a fair bit of work, but probably a reasonable demo as of 16 Jan 08 (Anna).
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <author>Anna Rafferty (improvements on original gui)</author>
	/// <version>1.1</version>
	[System.Serializable]
	public class MaxentTaggerGUI : JFrame
	{
		private const long serialVersionUID = -2574711492469740892L;

		private readonly JTextArea inputBox = new JTextArea();

		private readonly JTextArea outputBox = new JTextArea();

		private readonly JButton tagButton = new JButton();

		private static MaxentTagger tagger;

		public MaxentTaggerGUI()
			: base("Maximum Entropy Part of Speech Tagger")
		{
			// TODO: not likely to be an issue, but this should not be static...
			try
			{
				JbInit();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Run the simple tagger GUI.</summary>
		/// <remarks>
		/// Run the simple tagger GUI. Usage:<br /><code>
		/// java edu.stanford.nlp.tagger.maxent.MaxentTaggerGUI [modelPath]
		/// </code><br />
		/// If you don't specify a model, the code looks for one in a couple of
		/// canonical places.
		/// </remarks>
		/// <param name="args">None or a modelPath, as above</param>
		public static void Main(string[] args)
		{
			Thread t = new _Thread_78(args);
			t.Start();
			Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerGUI mainFrame1 = new Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerGUI();
			mainFrame1.SetPreferredSize(new Dimension(400, 200));
			mainFrame1.Pack();
			mainFrame1.SetVisible(true);
		}

		private sealed class _Thread_78 : Thread
		{
			public _Thread_78(string[] args)
			{
				this.args = args;
			}

			public override void Run()
			{
				string file;
				try
				{
					if (args.Length > 0)
					{
						file = args[args.Length - 1];
					}
					else
					{
						file = MaxentTagger.DefaultDistributionPath;
					}
					Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerGUI.tagger = new MaxentTagger(file);
				}
				catch (Exception e)
				{
					try
					{
						file = MaxentTagger.DefaultNlpGroupModelPath;
						Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerGUI.tagger = new MaxentTagger(file);
					}
					catch (Exception)
					{
						Sharpen.Runtime.PrintStackTrace(e);
					}
				}
			}

			private readonly string[] args;
		}

		private void JbInit()
		{
			//pf = new PrintFile("out");
			this.AddWindowListener(new _WindowAdapter_110());
			//Set up the input/output fields and let them scroll.
			inputBox.SetLineWrap(true);
			inputBox.SetWrapStyleWord(true);
			outputBox.SetLineWrap(true);
			outputBox.SetWrapStyleWord(true);
			outputBox.SetEditable(false);
			JScrollPane scroll1 = new JScrollPane(inputBox);
			JScrollPane scroll2 = new JScrollPane(outputBox);
			scroll1.SetBorder(BorderFactory.CreateTitledBorder(BorderFactory.CreateEtchedBorder(), "Type a sentence to tag: "));
			scroll2.SetBorder(BorderFactory.CreateTitledBorder(BorderFactory.CreateEtchedBorder(), "Tagged sentence: "));
			//Set up the button for starting tagging
			JPanel buttonPanel = new JPanel();
			buttonPanel.SetBackground(Color.White);
			buttonPanel.ApplyComponentOrientation(ComponentOrientation.RightToLeft);
			FlowLayout fl = new FlowLayout();
			fl.SetAlignment(FlowLayout.Right);
			buttonPanel.SetLayout(fl);
			tagButton.SetText("Tag sentence!");
			tagButton.SetBackground(Color.White);
			buttonPanel.Add(tagButton);
			tagButton.AddActionListener(null);
			//Lay it all out
			this.SetLayout(new GridBagLayout());
			GridBagConstraints c = new GridBagConstraints();
			c.fill = GridBagConstraints.Both;
			c.gridwidth = GridBagConstraints.Remainder;
			c.weightx = 4.0;
			c.weighty = 4.0;
			this.Add(scroll1, c);
			c.weighty = 1.0;
			this.Add(buttonPanel, c);
			c.weighty = 4.0;
			c.gridheight = GridBagConstraints.Remainder;
			this.Add(scroll2, c);
		}

		private sealed class _WindowAdapter_110 : WindowAdapter
		{
			public _WindowAdapter_110()
			{
			}

			public override void WindowClosing(WindowEvent e)
			{
				System.Environment.Exit(0);
			}
		}

		private void PerformTagAction(ActionEvent e)
		{
			string s = inputBox.GetText();
			Thread t = new _Thread_162(s);
			t.Start();
		}

		private sealed class _Thread_162 : Thread
		{
			public _Thread_162(string s)
			{
				this.s = s;
			}

			public override void Run()
			{
				string taggedStr = Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerGUI.tagger.TagString(s);
				SwingUtilities.InvokeLater(null);
			}

			private readonly string s;
		}
	}
}
