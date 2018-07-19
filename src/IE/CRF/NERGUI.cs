// NERGUI -- a GUI for a probabilistic (CRF) sequence model for NER.
// Copyright (c) 2002-2008, 2018 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
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
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/CRF-NER.html
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Awt;
using Java.Awt.Event;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Javax.Swing;
using Javax.Swing.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>A GUI for Named Entity sequence classifiers.</summary>
	/// <remarks>
	/// A GUI for Named Entity sequence classifiers.
	/// This version only supports the CRF.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	public class NERGUI
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(NERGUI));

		private AbstractSequenceClassifier<CoreLabel> classifier;

		private JFrame frame;

		private JEditorPane editorPane;

		private JToolBar tagPanel;

		private const int Height = 600;

		private const int Width = 650;

		private IDictionary<string, Color> tagToColorMap;

		private JFileChooser fileChooser = new JFileChooser();

		private IMutableAttributeSet defaultAttrSet = new SimpleAttributeSet();

		private IActionListener actor;

		private File loadedFile;

		private string taggedContents;

		private string htmlContents;

		private JMenuItem saveUntagged;

		private JMenuItem saveTaggedAs;

		private JButton extractButton;

		private JMenuItem extract;

		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		private void CreateAndShowGUI()
		{
			//Make sure we have nice window decorations.
			JFrame.SetDefaultLookAndFeelDecorated(true);
			//Create and set up the window.
			frame = new JFrame("Stanford Named Entity Recognizer");
			frame.SetDefaultCloseOperation(WindowConstantsConstants.ExitOnClose);
			frame.GetContentPane().SetLayout(new BorderLayout());
			frame.GetContentPane().SetPreferredSize(new Dimension(Width, Height));
			frame.SetJMenuBar(AddMenuBar());
			BuildTagPanel();
			BuildContentPanel();
			BuildExtractButton();
			extractButton.SetEnabled(false);
			extract.SetEnabled(false);
			//Display the window.
			frame.Pack();
			frame.SetVisible(true);
		}

		private JMenuBar AddMenuBar()
		{
			JMenuBar menubar = new JMenuBar();
			int shortcutMask = Toolkit.GetDefaultToolkit().GetMenuShortcutKeyMask();
			int shiftShortcutMask = Toolkit.GetDefaultToolkit().GetMenuShortcutKeyMask() | InputEvent.ShiftDownMask;
			JMenu fileMenu = new JMenu("File");
			menubar.Add(fileMenu);
			JMenu editMenu = new JMenu("Edit");
			menubar.Add(editMenu);
			JMenu classifierMenu = new JMenu("Classifier");
			menubar.Add(classifierMenu);
			/*
			* FILE MENU
			*/
			JMenuItem openFile = new JMenuItem("Open File");
			openFile.SetMnemonic('O');
			openFile.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkO, shortcutMask));
			openFile.AddActionListener(actor);
			fileMenu.Add(openFile);
			JMenuItem loadURL = new JMenuItem("Load URL");
			loadURL.SetMnemonic('L');
			loadURL.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkU, shortcutMask));
			loadURL.AddActionListener(actor);
			fileMenu.Add(loadURL);
			fileMenu.Add(new JSeparator());
			saveUntagged = new JMenuItem("Save Untagged File");
			saveUntagged.SetMnemonic('S');
			saveUntagged.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkS, shortcutMask));
			saveUntagged.AddActionListener(actor);
			saveUntagged.SetEnabled(false);
			fileMenu.Add(saveUntagged);
			JMenuItem saveUntaggedAs = new JMenuItem("Save Untagged File As ...");
			saveUntaggedAs.SetMnemonic('U');
			saveUntaggedAs.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkS, shiftShortcutMask));
			saveUntaggedAs.AddActionListener(actor);
			fileMenu.Add(saveUntaggedAs);
			saveTaggedAs = new JMenuItem("Save Tagged File As ...");
			saveTaggedAs.SetMnemonic('T');
			saveTaggedAs.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkT, shortcutMask));
			saveTaggedAs.AddActionListener(actor);
			saveTaggedAs.SetEnabled(false);
			fileMenu.Add(saveTaggedAs);
			if (!IsMacOSX())
			{
				// don't need if on Mac, since it has its own Quit on application menu!
				fileMenu.Add(new JSeparator());
				JMenuItem exit = new JMenuItem("Exit");
				exit.SetMnemonic('x');
				exit.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkQ, shortcutMask));
				exit.AddActionListener(actor);
				fileMenu.Add(exit);
			}
			/*
			* EDIT MENU
			*/
			JMenuItem cut = new JMenuItem("Cut");
			cut.SetMnemonic('X');
			cut.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkX, shortcutMask));
			cut.AddActionListener(actor);
			editMenu.Add(cut);
			JMenuItem copy = new JMenuItem("Copy");
			copy.SetMnemonic('C');
			copy.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkC, shortcutMask));
			copy.AddActionListener(actor);
			editMenu.Add(copy);
			JMenuItem paste = new JMenuItem("Paste");
			paste.SetMnemonic('V');
			paste.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkV, shortcutMask));
			paste.AddActionListener(actor);
			editMenu.Add(paste);
			JMenuItem clear = new JMenuItem("Clear");
			clear.SetMnemonic('C');
			// clear.setAccelerator(KeyStroke.getKeyStroke(java.awt.event.KeyEvent.VK_L, shortcutMask)); // used for load CRF
			clear.AddActionListener(actor);
			editMenu.Add(clear);
			/*
			* CLASSIFIER MENU
			*/
			JMenuItem loadCRF = new JMenuItem("Load CRF from File");
			loadCRF.SetMnemonic('R');
			loadCRF.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkR, shortcutMask));
			loadCRF.AddActionListener(actor);
			classifierMenu.Add(loadCRF);
			JMenuItem loadResourceCRF = new JMenuItem("Load CRF from Classpath");
			// loadCRF.setMnemonic('R');
			// loadCRF.setAccelerator(KeyStroke.getKeyStroke(java.awt.event.KeyEvent.VK_R, shortcutMask));
			loadResourceCRF.AddActionListener(actor);
			classifierMenu.Add(loadResourceCRF);
			JMenuItem loadDefaultCRF = new JMenuItem("Load Default CRF");
			loadDefaultCRF.SetMnemonic('L');
			loadDefaultCRF.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkL, shortcutMask));
			loadDefaultCRF.AddActionListener(actor);
			classifierMenu.Add(loadDefaultCRF);
			extract = new JMenuItem("Run NER");
			extract.SetMnemonic('N');
			extract.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkN, shortcutMask));
			extract.AddActionListener(actor);
			classifierMenu.Add(extract);
			return menubar;
		}

		private class InputListener : IKeyListener
		{
			public virtual void KeyPressed(KeyEvent e)
			{
			}

			public virtual void KeyReleased(KeyEvent e)
			{
			}

			public virtual void KeyTyped(KeyEvent e)
			{
				this._enclosing.saveTaggedAs.SetEnabled(false);
			}

			internal InputListener(NERGUI _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly NERGUI _enclosing;
		}

		private class ActionPerformer : IActionListener
		{
			public virtual void ActionPerformed(ActionEvent e)
			{
				string com = e.GetActionCommand();
				switch (com)
				{
					case "Open File":
					{
						File file = this._enclosing.GetFile(true);
						if (file != null)
						{
							this._enclosing.OpenFile(file);
						}
						break;
					}

					case "Load URL":
					{
						string url = this._enclosing.GetURL();
						if (url != null)
						{
							this._enclosing.OpenURL(url);
						}
						break;
					}

					case "Exit":
					{
						NERGUI.Exit();
						break;
					}

					case "Clear":
					{
						this._enclosing.ClearDocument();
						break;
					}

					case "Cut":
					{
						this._enclosing.CutDocument();
						break;
					}

					case "Copy":
					{
						this._enclosing.CopyDocument();
						break;
					}

					case "Paste":
					{
						this._enclosing.PasteDocument();
						break;
					}

					case "Load CRF from File":
					{
						File file = this._enclosing.GetFile(true);
						if (file != null)
						{
							this._enclosing.LoadClassifier(file);
						}
						break;
					}

					case "Load CRF from Classpath":
					{
						string text = JOptionPane.ShowInputDialog(this._enclosing.frame, "Enter a classpath resource for an NER classifier");
						if (text != null)
						{
							// User didn't click cancel
							this._enclosing.LoadClassifier(text);
						}
						break;
					}

					case "Load Default CRF":
					{
						this._enclosing.LoadClassifier((File)null);
						break;
					}

					case "Run NER":
					{
						this._enclosing.Extract();
						break;
					}

					case "Save Untagged File":
					{
						this._enclosing.SaveUntaggedContents(this._enclosing.loadedFile);
						break;
					}

					case "Save Untagged File As ...":
					{
						this._enclosing.SaveUntaggedContents(this._enclosing.GetFile(false));
						break;
					}

					case "Save Tagged File As ...":
					{
						File f = this._enclosing.GetFile(false);
						if (f != null)
						{
							// i.e., they didn't cancel out of the file dialog
							NERGUI.SaveFile(f, this._enclosing.taggedContents);
						}
						break;
					}

					default:
					{
						NERGUI.log.Info("Unknown Action: " + e);
						break;
					}
				}
			}

			internal ActionPerformer(NERGUI _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly NERGUI _enclosing;
		}

		public virtual File GetFile(bool open)
		{
			File file = null;
			int returnVal;
			if (open)
			{
				returnVal = fileChooser.ShowOpenDialog(frame);
			}
			else
			{
				returnVal = fileChooser.ShowSaveDialog(frame);
			}
			if (returnVal == JFileChooser.ApproveOption)
			{
				file = fileChooser.GetSelectedFile();
				if (open && !CheckFile(file))
				{
					file = null;
				}
			}
			return file;
		}

		private void SaveUntaggedContents(File file)
		{
			try
			{
				string contents;
				if (editorPane.GetContentType().Equals("text/html"))
				{
					contents = editorPane.GetText();
				}
				else
				{
					IDocument doc = editorPane.GetDocument();
					contents = doc.GetText(0, doc.GetLength());
				}
				SaveFile(file, contents);
				saveUntagged.SetEnabled(true);
				loadedFile = file;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		private static void SaveFile(File file, string contents)
		{
			StringUtils.PrintToFile(file, contents);
		}

		public virtual string GetURL()
		{
			return JOptionPane.ShowInputDialog(frame, "URL: ", "Load URL", JOptionPane.QuestionMessage);
		}

		private bool CheckFile(File file)
		{
			if (file.IsFile())
			{
				fileChooser.SetCurrentDirectory(file.GetParentFile());
				return true;
			}
			else
			{
				string message = "File Not Found: " + file.GetAbsolutePath();
				DisplayError("File Not Found Error", message);
				return false;
			}
		}

		private void DisplayError(string title, string message)
		{
			JOptionPane.ShowMessageDialog(frame, message, title, JOptionPane.ErrorMessage);
		}

		/// <summary>Load a classifier from a file or the default.</summary>
		/// <remarks>
		/// Load a classifier from a file or the default.
		/// The default is specified by passing in
		/// <see langword="null"/>
		/// .
		/// </remarks>
		public virtual void LoadClassifier(File file)
		{
			try
			{
				if (file != null)
				{
					classifier = CRFClassifier.GetClassifier(file);
				}
				else
				{
					// default classifier in jar
					classifier = CRFClassifier.GetDefaultClassifier();
				}
			}
			catch (Exception e)
			{
				// we catch Throwable, since we'd also like to be able to get an OutOfMemoryError
				string message;
				if (file != null)
				{
					message = "Error loading CRF: " + file.GetAbsolutePath();
				}
				else
				{
					message = "Error loading default CRF";
				}
				log.Info(message);
				string title = "CRF Load Error";
				string msg = e.ToString();
				if (msg != null)
				{
					message += '\n' + msg;
				}
				DisplayError(title, message);
				return;
			}
			RemoveTags();
			BuildTagPanel();
			// buildExtractButton();
			extractButton.SetEnabled(true);
			extract.SetEnabled(true);
		}

		/// <summary>Load a classifier from a file or the default.</summary>
		/// <remarks>
		/// Load a classifier from a file or the default.
		/// The default is specified by passing in
		/// <see langword="null"/>
		/// .
		/// </remarks>
		public virtual void LoadClassifier(string resource)
		{
			try
			{
				if (resource != null)
				{
					classifier = CRFClassifier.GetClassifier(resource);
				}
				else
				{
					// default classifier in jar
					classifier = CRFClassifier.GetDefaultClassifier();
				}
			}
			catch (Exception e)
			{
				// we catch Throwable, since we'd also like to be able to get an OutOfMemoryError
				string message;
				if (resource != null)
				{
					message = "Error loading classpath CRF: " + resource;
				}
				else
				{
					message = "Error loading default CRF";
				}
				log.Info(message);
				string title = "CRF Load Error";
				string msg = e.ToString();
				if (msg != null)
				{
					message += '\n' + msg;
				}
				DisplayError(title, message);
				return;
			}
			RemoveTags();
			BuildTagPanel();
			// buildExtractButton();
			extractButton.SetEnabled(true);
			extract.SetEnabled(true);
		}

		public virtual void OpenFile(File file)
		{
			OpenURL(file.ToURI().ToString());
			loadedFile = file;
			saveUntagged.SetEnabled(true);
		}

		private void OpenURL(string url)
		{
			try
			{
				editorPane.SetPage(url);
			}
			catch (Exception e)
			{
				log.Info("Error loading |" + url + '|');
				log.Warn(e);
				DisplayError("Error Loading URL " + url, "Message: " + e);
				return;
			}
			loadedFile = null;
			string text = editorPane.GetText();
			taggedContents = null;
			if (!editorPane.GetContentType().Equals("text/html"))
			{
				editorPane.SetContentType("text/rtf");
				IDocument doc = editorPane.GetDocument();
				try
				{
					doc.InsertString(0, text, defaultAttrSet);
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
				editorPane.Revalidate();
				editorPane.Repaint();
				editorPane.SetEditable(true);
				htmlContents = null;
			}
			else
			{
				editorPane.SetEditable(false);
				htmlContents = editorPane.GetText();
			}
			saveUntagged.SetEnabled(false);
			saveTaggedAs.SetEnabled(false);
		}

		private void RemoveTags()
		{
			if (editorPane.GetContentType().Equals("text/html"))
			{
				if (htmlContents != null)
				{
					editorPane.SetText(htmlContents);
				}
				editorPane.Revalidate();
				editorPane.Repaint();
			}
			else
			{
				DefaultStyledDocument doc = (DefaultStyledDocument)editorPane.GetDocument();
				SimpleAttributeSet attr = new SimpleAttributeSet();
				StyleConstants.SetForeground(attr, Color.Black);
				StyleConstants.SetBackground(attr, Color.White);
				doc.SetCharacterAttributes(0, doc.GetLength(), attr, false);
			}
			saveTaggedAs.SetEnabled(false);
		}

		private void Extract()
		{
			log.Info("content type: " + editorPane.GetContentType());
			if (!editorPane.GetContentType().Equals("text/html"))
			{
				DefaultStyledDocument doc = (DefaultStyledDocument)editorPane.GetDocument();
				string text = null;
				try
				{
					text = doc.GetText(0, doc.GetLength());
				}
				catch (Exception e)
				{
					log.Err(e);
				}
				string labeledText = classifier.ClassifyWithInlineXML(text);
				taggedContents = labeledText;
				ICollection<string> tags = classifier.Labels();
				string background = classifier.BackgroundSymbol();
				StringBuilder tagPattern = new StringBuilder();
				foreach (string tag in tags)
				{
					if (background.Equals(tag))
					{
						continue;
					}
					if (tagPattern.Length > 0)
					{
						tagPattern.Append('|');
					}
					tagPattern.Append(tag);
				}
				Pattern startPattern = Pattern.Compile("<(" + tagPattern + ")>");
				Pattern endPattern = Pattern.Compile("</(" + tagPattern + ")>");
				string finalText = labeledText;
				Matcher m = startPattern.Matcher(finalText);
				while (m.Find())
				{
					int start = m.Start();
					finalText = m.ReplaceFirst(string.Empty);
					m = endPattern.Matcher(finalText);
					if (m.Find())
					{
						int end = m.Start();
						string tag_1 = m.Group(1);
						finalText = m.ReplaceFirst(string.Empty);
						IAttributeSet attSet = GetAttributeSet(tag_1);
						try
						{
							string entity = Sharpen.Runtime.Substring(finalText, start, end);
							doc.SetCharacterAttributes(start, entity.Length, attSet, false);
						}
						catch (Exception ex)
						{
							log.Err(ex);
							System.Environment.Exit(-1);
						}
						log.Info(tag_1 + ": " + Sharpen.Runtime.Substring(finalText, start, end));
					}
					else
					{
						log.Info("Couldn't find end pattern!");
					}
					m = startPattern.Matcher(finalText);
				}
				editorPane.Revalidate();
				editorPane.Repaint();
			}
			else
			{
				string untaggedContents = editorPane.GetText();
				if (untaggedContents == null)
				{
					untaggedContents = string.Empty;
				}
				taggedContents = classifier.ClassifyWithInlineXML(untaggedContents);
				ICollection<string> tags = classifier.Labels();
				string background = classifier.BackgroundSymbol();
				StringBuilder tagPattern = new StringBuilder();
				foreach (string tag in tags)
				{
					if (background.Equals(tag))
					{
						continue;
					}
					if (tagPattern.Length > 0)
					{
						tagPattern.Append('|');
					}
					tagPattern.Append(tag);
				}
				Pattern startPattern = Pattern.Compile("<(" + tagPattern + ")>");
				Pattern endPattern = Pattern.Compile("</(" + tagPattern + ")>");
				string finalText = taggedContents;
				Matcher m = startPattern.Matcher(finalText);
				while (m.Find())
				{
					string tag_1 = m.Group(1);
					Color col = tagToColorMap[tag_1];
					if (col != null)
					{
						string color = ColorToHTML(col);
						string newTag = "<span style=\"background-color: " + color + "; color: white\">";
						finalText = m.ReplaceFirst(newTag);
						int start = m.Start() + newTag.Length;
						Matcher m1 = endPattern.Matcher(finalText);
						if (m1.Find(m.End()))
						{
							string entity = Sharpen.Runtime.Substring(finalText, start, m1.Start());
							log.Info(tag_1 + ": " + entity);
						}
						else
						{
							log.Warn("Failed to find end for " + tag_1);
						}
						finalText = m1.ReplaceFirst("</span>");
						m = startPattern.Matcher(finalText);
					}
				}
				// System.out.println(finalText);
				editorPane.SetText(finalText);
				editorPane.Revalidate();
				editorPane.Repaint();
			}
			// log.info(finalText);
			saveTaggedAs.SetEnabled(true);
		}

		private IAttributeSet GetAttributeSet(string tag)
		{
			IMutableAttributeSet attr = new SimpleAttributeSet();
			Color color = tagToColorMap[tag];
			StyleConstants.SetBackground(attr, color);
			StyleConstants.SetForeground(attr, Color.White);
			return attr;
		}

		private void ClearDocument()
		{
			editorPane.SetContentType("text/rtf");
			IDocument doc = new DefaultStyledDocument();
			editorPane.SetDocument(doc);
			//    defaultAttrSet = ((StyledEditorKit)editorPane.getEditorKit()).getInputAttributes();
			//    StyleConstants.setFontFamily(defaultAttrSet, "Lucinda Sans Unicode");
			log.Info("attr: " + defaultAttrSet);
			try
			{
				doc.InsertString(0, " ", defaultAttrSet);
			}
			catch (Exception ex)
			{
				throw new Exception(ex);
			}
			editorPane.SetEditable(true);
			editorPane.Revalidate();
			editorPane.Repaint();
			saveUntagged.SetEnabled(false);
			saveTaggedAs.SetEnabled(false);
			taggedContents = null;
			htmlContents = null;
			loadedFile = null;
		}

		private void CutDocument()
		{
			editorPane.Cut();
			saveTaggedAs.SetEnabled(false);
		}

		private void CopyDocument()
		{
			editorPane.Copy();
		}

		private void PasteDocument()
		{
			editorPane.Paste();
			saveTaggedAs.SetEnabled(false);
		}

		internal static void Exit()
		{
			// ask if they're sure?
			System.Environment.Exit(-1);
		}

		private const string initText = "In bringing his distinct vision to the Western genre, writer-director Jim Jarmusch has created a quasi-mystical avant-garde drama that remains a deeply spiritual viewing experience. After losing his parents and fianc\u00E9e, a Cleveland accountant named William Blake (a remarkable Johnny Depp) spends all his money and takes a train to the frontier town of Machine in order to work at a factory. Upon arriving in Machine, he is denied his expected job and finds himself a fugitive after murdering a man in self-defense. Wounded and helpless, Blake is befriended by Nobody (Gary Farmer), a wandering Native American who considers him to be a ghostly manifestation of the famous poet. Nobody aids Blake in his flight from three bumbling bounty hunters, preparing him for his final journey--a return to the world of the spirits.";

		//  private String initText = "In";
		private void BuildContentPanel()
		{
			editorPane = new JEditorPane();
			editorPane.SetContentType("text/rtf");
			editorPane.AddKeyListener(new NERGUI.InputListener(this));
			//    defaultAttrSet = ((StyledEditorKit)editorPane.getEditorKit()).getInputAttributes();
			StyleConstants.SetFontFamily(defaultAttrSet, "Lucida Sans");
			IDocument doc = new DefaultStyledDocument();
			editorPane.SetDocument(doc);
			try
			{
				doc.InsertString(0, initText, defaultAttrSet);
			}
			catch (Exception ex)
			{
				throw new Exception(ex);
			}
			JScrollPane scrollPane = new JScrollPane(editorPane);
			frame.GetContentPane().Add(scrollPane, BorderLayout.Center);
			editorPane.SetEditable(true);
		}

		public static string ColorToHTML(Color color)
		{
			string r = int.ToHexString(color.GetRed());
			if (r.IsEmpty())
			{
				r = "00";
			}
			else
			{
				if (r.Length == 1)
				{
					r = "0" + r;
				}
				else
				{
					if (r.Length > 2)
					{
						throw new ArgumentException("invalid hex color for red" + r);
					}
				}
			}
			string g = int.ToHexString(color.GetGreen());
			if (g.IsEmpty())
			{
				g = "00";
			}
			else
			{
				if (g.Length == 1)
				{
					g = "0" + g;
				}
				else
				{
					if (g.Length > 2)
					{
						throw new ArgumentException("invalid hex color for green" + g);
					}
				}
			}
			string b = int.ToHexString(color.GetBlue());
			if (b.IsEmpty())
			{
				b = "00";
			}
			else
			{
				if (b.Length == 1)
				{
					b = "0" + b;
				}
				else
				{
					if (b.Length > 2)
					{
						throw new ArgumentException("invalid hex color for blue" + b);
					}
				}
			}
			return "#" + r + g + b;
		}

		internal class ColorIcon : IIcon
		{
			internal Color color;

			public ColorIcon(Color c)
			{
				color = c;
			}

			public virtual void PaintIcon(Component c, Graphics g, int x, int y)
			{
				g.SetColor(color);
				g.FillRect(x, y, GetIconWidth(), GetIconHeight());
			}

			public virtual int GetIconWidth()
			{
				return 10;
			}

			public virtual int GetIconHeight()
			{
				return 10;
			}
		}

		private void BuildExtractButton()
		{
			if (extractButton == null)
			{
				JPanel buttonPanel = new JPanel();
				extractButton = new JButton("Run NER");
				buttonPanel.Add(extractButton);
				frame.GetContentPane().Add(buttonPanel, BorderLayout.South);
				extractButton.AddActionListener(actor);
			}
		}

		private void BuildTagPanel()
		{
			if (tagPanel == null)
			{
				tagPanel = new JToolBar(SwingConstantsConstants.Vertical);
				tagPanel.SetFloatable(false);
				frame.GetContentPane().Add(tagPanel, BorderLayout.East);
			}
			else
			{
				tagPanel.RemoveAll();
			}
			if (classifier != null)
			{
				MakeTagMaps();
				ICollection<string> tags = classifier.Labels();
				string backgroundSymbol = classifier.BackgroundSymbol();
				foreach (string tag in tags)
				{
					if (backgroundSymbol.Equals(tag))
					{
						continue;
					}
					Color color = tagToColorMap[tag];
					JButton b = new JButton(tag, new NERGUI.ColorIcon(color));
					tagPanel.Add(b);
				}
			}
			tagPanel.Revalidate();
			tagPanel.Repaint();
		}

		private void MakeTagMaps()
		{
			ICollection<string> tags = classifier.Labels();
			string backgroundSymbol = classifier.BackgroundSymbol();
			tagToColorMap = MakeTagToColorMap(tags, backgroundSymbol);
		}

		public static IDictionary<string, Color> MakeTagToColorMap(ICollection<string> tags, string backgroundSymbol)
		{
			int numColors = tags.Count - 1;
			Color[] colors = GetNColors(numColors);
			IDictionary<string, Color> result = Generics.NewHashMap();
			int i = 0;
			foreach (string tag in tags)
			{
				if (backgroundSymbol.Equals(tag))
				{
					continue;
				}
				if (result[tag] != null)
				{
					continue;
				}
				result[tag] = colors[i++];
			}
			return result;
		}

		private static Color[] basicColors = new Color[] { new Color(204, 102, 0), new Color(102, 0, 102), new Color(204, 0, 102), new Color(153, 0, 0), new Color(153, 0, 204), new Color(255, 102, 0), new Color(255, 102, 153), new Color(204, 152, 255
			), new Color(102, 102, 255), new Color(153, 102, 0), new Color(51, 102, 51), new Color(0, 102, 255) };

		//   private static Color[] basicColors = new Color[]{new Color(153, 102, 153),
		//                                                    new Color(102, 153, 153),
		//                                                    new Color(153, 153, 102),
		//                                                    new Color(102, 102, 102),
		//                                                    new Color(102, 153, 102),
		//                                                    new Color(153, 102, 102),
		//                                                    new Color(204, 153, 51),
		//                                                    new Color(204, 51, 102),
		//                                                    new Color(255, 204, 0),
		//                                                    new Color(153, 0, 255),
		//                                                    new Color(204, 204, 204),
		//                                                    new Color(0, 255, 153)};
		//   private static Color[] basicColors = new Color[]{Color.BLUE,
		//                                     Color.GREEN,
		//                                     Color.RED,
		//                                     Color.ORANGE,
		//                                     Color.LIGHT_GRAY,
		//                                     Color.CYAN,
		//                                     Color.MAGENTA,
		//                                     Color.YELLOW,
		//                                     Color.RED,
		//                                     Color.GRAY,
		//                                     Color.PINK,
		//                                     Color.DARK_GRAY};
		private static Color[] GetNColors(int n)
		{
			Color[] colors = new Color[n];
			if (n <= basicColors.Length)
			{
				System.Array.Copy(basicColors, 0, colors, 0, n);
			}
			else
			{
				int s = 255 / (int)Math.Ceil(Math.Pow(n, (1.0 / 3.0)));
				int index = 0;
				for (int i = 0; i < 256; i += s)
				{
					for (int j = 0; j < 256; j += s)
					{
						for (int k = 0; k < 256; k += s)
						{
							colors[index++] = new Color(i, j, k);
							if (index == n)
							{
								goto OUTER_break;
							}
						}
					}
OUTER_continue: ;
				}
OUTER_break: ;
			}
			return colors;
		}

		private static bool IsMacOSX()
		{
			return Runtime.GetProperty("os.name").ToLower().StartsWith("mac os x");
		}

		/// <summary>Run the GUI.</summary>
		/// <remarks>
		/// Run the GUI.  This program accepts no command-line arguments.
		/// Everything is entered into the GUI.
		/// </remarks>
		public static void Main(string[] args)
		{
			//Schedule a job for the event-dispatching thread:
			//creating and showing this application's GUI.
			if (IsMacOSX())
			{
				Runtime.SetProperty("apple.laf.useScreenMenuBar", "true");
			}
			SwingUtilities.InvokeLater(null);
		}

		public NERGUI()
		{
			actor = new NERGUI.ActionPerformer(this);
		}
	}
}
