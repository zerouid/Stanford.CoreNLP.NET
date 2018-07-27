using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IE.Ner;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.IE
{
	/// <author>Jenny Finkel</author>
	public class NERGUI
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(NERGUI));

		private AbstractSequenceClassifier<object> classifier;

		private JFrame frame;

		private JEditorPane editorPane;

		private JToolBar tagPanel;

		private const int Height = 600;

		private const int Width = 650;

		private IDictionary<string, Color> tagToColorMap;

		private JFileChooser fileChooser = new JFileChooser(Runtime.GetProperty("user.dir"));

		private IMutableAttributeSet defaultAttrSet = new SimpleAttributeSet();

		private IActionListener actor;

		private File loadedFile;

		private string untaggedContents = null;

		private string taggedContents = null;

		private string htmlContents = null;

		private JMenuItem saveUntagged = null;

		private JMenuItem saveTaggedAs = null;

		private void CreateAndShowGUI()
		{
			//Make sure we have nice window decorations.
			JFrame.SetDefaultLookAndFeelDecorated(true);
			//Create and set up the window.
			frame = new JFrame("Stanford Named Entity Recognizer");
			frame.SetDefaultCloseOperation(WindowConstantsConstants.ExitOnClose);
			frame.GetContentPane().SetLayout(new BorderLayout());
			frame.GetContentPane().SetSize(Width, Height);
			frame.SetJMenuBar(AddMenuBar());
			//frame.setSize(new Dimension(WIDTH, HEIGHT));
			frame.SetSize(Width, Height);
			BuildTagPanel();
			BuildContentPanel();
			//Display the window.
			frame.Pack();
			frame.SetSize(Width, Height);
			frame.SetVisible(true);
		}

		private JMenuBar AddMenuBar()
		{
			JMenuBar menubar = new JMenuBar();
			JMenu fileMenu = new JMenu("File");
			menubar.Add(fileMenu);
			JMenu editMenu = new JMenu("Edit");
			menubar.Add(editMenu);
			JMenu classifierMenu = new JMenu("Classifier");
			menubar.Add(classifierMenu);
			int menuMask = Toolkit.GetDefaultToolkit().GetMenuShortcutKeyMask();
			/*
			* FILE MENU
			*/
			JMenuItem openFile = new JMenuItem("Open File");
			openFile.SetMnemonic('O');
			openFile.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkF, menuMask));
			openFile.AddActionListener(actor);
			fileMenu.Add(openFile);
			JMenuItem loadURL = new JMenuItem("Load URL");
			loadURL.SetMnemonic('L');
			loadURL.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkU, menuMask));
			loadURL.AddActionListener(actor);
			fileMenu.Add(loadURL);
			fileMenu.Add(new JSeparator());
			saveUntagged = new JMenuItem("Save Untagged File");
			saveUntagged.SetMnemonic('S');
			saveUntagged.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkS, menuMask));
			saveUntagged.AddActionListener(actor);
			saveUntagged.SetEnabled(false);
			fileMenu.Add(saveUntagged);
			JMenuItem saveUntaggedAs = new JMenuItem("Save Untagged File As ...");
			saveUntaggedAs.SetMnemonic('U');
			saveUntaggedAs.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkU, menuMask));
			saveUntaggedAs.AddActionListener(actor);
			fileMenu.Add(saveUntaggedAs);
			saveTaggedAs = new JMenuItem("Save Tagged File As ...");
			saveTaggedAs.SetMnemonic('T');
			saveTaggedAs.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkT, menuMask));
			saveTaggedAs.AddActionListener(actor);
			saveTaggedAs.SetEnabled(false);
			fileMenu.Add(saveTaggedAs);
			fileMenu.Add(new JSeparator());
			JMenuItem exit = new JMenuItem("Exit");
			exit.SetMnemonic('x');
			exit.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkQ, menuMask));
			exit.AddActionListener(actor);
			fileMenu.Add(exit);
			/*
			* EDIT MENU
			*/
			JMenuItem clear = new JMenuItem("Clear");
			clear.SetMnemonic('C');
			clear.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkL, menuMask));
			clear.AddActionListener(actor);
			editMenu.Add(clear);
			/*
			* CLASSIFIER MENU
			*/
			JMenuItem loadCRF = new JMenuItem("Load CRF From File");
			loadCRF.SetMnemonic('R');
			loadCRF.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkR, menuMask));
			loadCRF.AddActionListener(actor);
			classifierMenu.Add(loadCRF);
			JMenuItem loadDefaultCRF = new JMenuItem("Load Default CRF");
			loadDefaultCRF.SetMnemonic('L');
			loadDefaultCRF.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkL, menuMask));
			loadDefaultCRF.AddActionListener(actor);
			classifierMenu.Add(loadDefaultCRF);
			JMenuItem loadCMM = new JMenuItem("Load CMM From File");
			loadCMM.SetMnemonic('M');
			loadCMM.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkM, menuMask));
			loadCMM.AddActionListener(actor);
			classifierMenu.Add(loadCMM);
			JMenuItem loadDefaultCMM = new JMenuItem("Load Default CMM");
			loadDefaultCMM.SetMnemonic('D');
			loadDefaultCMM.SetAccelerator(KeyStroke.GetKeyStroke(KeyEvent.VkD, menuMask));
			loadDefaultCMM.AddActionListener(actor);
			classifierMenu.Add(loadDefaultCMM);
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
						this._enclosing.Exit();
						break;
					}

					case "Clear":
					{
						this._enclosing.ClearDocument();
						break;
					}

					case "Load CRF From File":
					{
						File file = this._enclosing.GetFile(true);
						if (file != null)
						{
							this._enclosing.LoadClassifier(file, true);
						}
						break;
					}

					case "Load CMM From File":
					{
						File file = this._enclosing.GetFile(true);
						if (file != null)
						{
							this._enclosing.LoadClassifier(file, false);
						}
						break;
					}

					case "Load Default CRF":
					{
						this._enclosing.LoadDefaultClassifier(true);
						break;
					}

					case "Load Default CMM":
					{
						this._enclosing.LoadDefaultClassifier(false);
						break;
					}

					case "Extract":
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
						NERGUI.SaveFile(this._enclosing.GetFile(false), this._enclosing.taggedContents);
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

		public virtual void SaveUntaggedContents(File file)
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

		public static void SaveFile(File file, string contents)
		{
			StringUtils.PrintToFile(file, contents);
		}

		public virtual string GetURL()
		{
			string url = JOptionPane.ShowInputDialog(frame, "URL: ", "Load URL", JOptionPane.QuestionMessage);
			return url;
		}

		public virtual bool CheckFile(File file)
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

		public virtual void DisplayError(string title, string message)
		{
			JOptionPane.ShowMessageDialog(frame, message, title, JOptionPane.ErrorMessage);
		}

		public virtual void LoadClassifier(File file, bool crf)
		{
			try
			{
				if (crf)
				{
					classifier = CRFClassifier.GetClassifier(file);
				}
				else
				{
					classifier = CMMClassifier.GetClassifier(file);
				}
			}
			catch (Exception e)
			{
				string message = "Error loading " + (crf ? "CRF" : "CMM") + ": " + file.GetAbsolutePath();
				string title = (crf ? "CRF" : "CMM") + " Load Error";
				message += "\nMessage: " + e.Message;
				DisplayError(title, message);
				return;
			}
			RemoveTags();
			BuildTagPanel();
			BuildExtractButton();
		}

		public virtual void LoadDefaultClassifier(bool crf)
		{
			try
			{
				if (crf)
				{
					classifier = CRFClassifier.GetDefaultClassifier();
				}
				else
				{
					classifier = CMMClassifier.GetDefaultClassifier();
				}
			}
			catch (Exception e)
			{
				string message = "Error loading default " + (crf ? "CRF" : "CMM");
				string title = (crf ? "CRF" : "CMM") + " Load Error";
				message += "\nMessage: " + e.Message;
				DisplayError(title, message);
				return;
			}
			RemoveTags();
			BuildTagPanel();
			BuildExtractButton();
		}

		public virtual void OpenFile(File file)
		{
			string encoding = (classifier == null) ? "utf-8" : classifier.flags.inputEncoding;
			string text = IOUtils.SlurpFileNoExceptions(file.GetPath(), encoding);
			System.Console.Out.WriteLine(text);
			editorPane.SetContentType("text/plain");
			editorPane.SetText(text);
			System.Console.Out.WriteLine(editorPane.GetText());
			loadedFile = file;
			Redraw();
			saveUntagged.SetEnabled(true);
		}

		public virtual void OpenURL(string url)
		{
			try
			{
				editorPane.SetPage(url);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				DisplayError("Error Loading URL " + url, "Message: " + e.ToString());
				return;
			}
			loadedFile = null;
			Redraw();
		}

		public virtual void Redraw()
		{
			string text = editorPane.GetText();
			taggedContents = null;
			untaggedContents = null;
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
				editorPane.SetText(htmlContents);
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
					throw new Exception(e);
				}
				string labeledText = classifier.ClassifyWithInlineXML(text);
				taggedContents = labeledText;
				untaggedContents = text;
				ICollection<string> tags = classifier.Labels();
				string background = classifier.BackgroundSymbol();
				string tagPattern = string.Empty;
				foreach (string tag in tags)
				{
					if (background.Equals(tag))
					{
						continue;
					}
					if (tagPattern.Length > 0)
					{
						tagPattern += "|";
					}
					tagPattern += tag;
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
							throw new Exception(ex);
						}
						log.Info(tag_1 + ": " + Sharpen.Runtime.Substring(finalText, start, end));
					}
					// print error message
					m = startPattern.Matcher(finalText);
				}
				editorPane.Revalidate();
				editorPane.Repaint();
			}
			else
			{
				untaggedContents = editorPane.GetText();
				taggedContents = classifier.ClassifyWithInlineXML(untaggedContents);
				ICollection<string> tags = classifier.Labels();
				string background = classifier.BackgroundSymbol();
				string tagPattern = string.Empty;
				foreach (string tag in tags)
				{
					if (background.Equals(tag))
					{
						continue;
					}
					if (tagPattern.Length > 0)
					{
						tagPattern += "|";
					}
					tagPattern += tag;
				}
				Pattern startPattern = Pattern.Compile("<(" + tagPattern + ")>");
				Pattern endPattern = Pattern.Compile("</(" + tagPattern + ")>");
				string finalText = taggedContents;
				Matcher m = startPattern.Matcher(finalText);
				while (m.Find())
				{
					string tag_1 = m.Group(1);
					string color = ColorToHTML(tagToColorMap[tag_1]);
					string newTag = "<span style=\"background-color: " + color + "; color: white\">";
					finalText = m.ReplaceFirst(newTag);
					int start = m.Start() + newTag.Length;
					Matcher m1 = endPattern.Matcher(finalText);
					m1.Find(m.End());
					string entity = Sharpen.Runtime.Substring(finalText, start, m1.Start());
					log.Info(tag_1 + ": " + entity);
					finalText = m1.ReplaceFirst("</span>");
					m = startPattern.Matcher(finalText);
				}
				System.Console.Out.WriteLine(finalText);
				editorPane.SetText(finalText);
				editorPane.Revalidate();
				editorPane.Repaint();
				log.Info(finalText);
			}
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

		public virtual void ClearDocument()
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
			untaggedContents = null;
			htmlContents = null;
			loadedFile = null;
		}

		public virtual void Exit()
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
			StyleConstants.SetFontFamily(defaultAttrSet, "Lucinda Sans");
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
			if (r.Length == 0)
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
			if (g.Length == 0)
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
			if (b.Length == 0)
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

		internal JButton extractButton = null;

		private void BuildExtractButton()
		{
			if (extractButton == null)
			{
				JPanel buttonPanel = new JPanel();
				extractButton = new JButton("Extract");
				buttonPanel.Add(extractButton);
				frame.Add(buttonPanel, BorderLayout.South);
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
		public static Color[] GetNColors(int n)
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

		/// <summary>Run the GUI.</summary>
		/// <remarks>
		/// Run the GUI.  This program accepts no command-line arguments.
		/// Everything is entered into the GUI.
		/// </remarks>
		public static void Main(string[] args)
		{
			//Schedule a job for the event-dispatching thread:
			//creating and showing this application's GUI.
			SwingUtilities.InvokeLater(null);
		}

		public NERGUI()
		{
			actor = new NERGUI.ActionPerformer(this);
		}
	}
}
