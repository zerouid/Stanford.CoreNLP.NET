// StanfordLexicalizedParser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002, 2003, 2004, 2005 The Board of Trustees of
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
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO.UI;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Swing;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.UI;
using Edu.Stanford.Nlp.Util.Logging;











namespace Edu.Stanford.Nlp.Parser.UI
{
	/// <summary>Provides a simple GUI Panel for Parsing.</summary>
	/// <remarks>
	/// Provides a simple GUI Panel for Parsing.  Allows a user to load a parser
	/// created using lexparser.LexicalizedParser, load a text data file or type
	/// in text, parse sentences within the input text, and view the resultant
	/// parse tree.
	/// </remarks>
	/// <author>Huy Nguyen (htnguyen@cs.stanford.edu)</author>
	[System.Serializable]
	public class ParserPanel : JPanel
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.UI.ParserPanel));

		private const long serialVersionUID = -2118491857333662471L;

		public const int UntokenizedEnglish = 0;

		public const int TokenizedChinese = 1;

		public const int UntokenizedChinese = 2;

		private static ITreebankLanguagePack tlp;

		private string encoding = "UTF-8";

		private const int OneSecond = 1000;

		private const int ParserLoadTime = 60;

		private const int ParseTime = 30;

		private const int SeekForward = 1;

		private const int SeekBack = -1;

		private readonly JFileChooser jfc;

		private readonly ParserPanel.JFileChooserLocation jfcLocation;

		private readonly JarFileChooser chooseJarParser;

		private OpenPageDialog pageDialog;

		private SimpleAttributeSet normalStyle;

		private SimpleAttributeSet highlightStyle;

		private int startIndex;

		private int endIndex;

		private TreeJPanel treePanel;

		private LexicalizedParser parser;

		private ParserPanel.LoadParserThread lpThread;

		private ParserPanel.ParseThread parseThread;

		private Timer timer;

		private int count;

		private Component glassPane;

		/// <summary>Whether to scroll one sentence forward after parsing.</summary>
		private bool scrollWhenDone;

		/// <summary>Creates new form ParserPanel</summary>
		public ParserPanel()
		{
			// constants for language specification
			// one second in milliseconds
			// parser takes approximately a minute to load
			// parser takes 5-60 seconds to parse a sentence
			// constants for finding nearest sentence boundary
			// for highlighting
			// worker threads to handle long operations
			// to monitor progress of long operations
			//private ProgressMonitor progressMonitor;
			// progress count
			// use glass pane to block input to components other than progressMonitor
			InitComponents();
			// create dialogs for file selection
			jfc = new JFileChooser(Runtime.GetProperty("user.dir"));
			pageDialog = new OpenPageDialog(new Frame(), true);
			pageDialog.SetFileChooser(jfc);
			jfcLocation = new ParserPanel.JFileChooserLocation(jfc);
			tlp = new PennTreebankLanguagePack();
			encoding = tlp.GetEncoding();
			SetFont();
			// create a timer
			timer = new Timer(OneSecond, new ParserPanel.TimerListener(this));
			// for (un)highlighting text
			highlightStyle = new SimpleAttributeSet();
			normalStyle = new SimpleAttributeSet();
			StyleConstants.SetBackground(highlightStyle, Color.yellow);
			StyleConstants.SetBackground(normalStyle, textPane.GetBackground());
			this.chooseJarParser = new JarFileChooser(".*\\.ser\\.gz", this);
		}

		/// <summary>Scrolls back one sentence in the text</summary>
		public virtual void ScrollBack()
		{
			HighlightSentence(startIndex - 1);
			// scroll to highlight location
			textPane.SetCaretPosition(startIndex);
		}

		/// <summary>Scrolls forward one sentence in the text</summary>
		public virtual void ScrollForward()
		{
			HighlightSentence(endIndex + 1);
			// scroll to highlight location
			textPane.SetCaretPosition(startIndex);
		}

		/// <summary>Highlights specified text region by changing the character attributes</summary>
		private void HighlightText(int start, int end, SimpleAttributeSet style)
		{
			if (start < end)
			{
				textPane.GetStyledDocument().SetCharacterAttributes(start, end - start + 1, style, false);
			}
		}

		/// <summary>
		/// Finds the sentence delimited by the closest sentence delimiter preceding
		/// start and closest period following start.
		/// </summary>
		private void HighlightSentence(int start)
		{
			HighlightSentence(start, -1);
		}

		/// <summary>
		/// Finds the sentence delimited by the closest sentence delimiter preceding
		/// start and closest period following end.
		/// </summary>
		/// <remarks>
		/// Finds the sentence delimited by the closest sentence delimiter preceding
		/// start and closest period following end.  If end is less than start
		/// (or -1), sets right boundary as closest period following start.
		/// Actually starts search for preceding sentence delimiter at (start-1)
		/// </remarks>
		private void HighlightSentence(int start, int end)
		{
			// clears highlight.  paints over entire document because the document may have changed
			HighlightText(0, textPane.GetText().Length, normalStyle);
			// if start<1 set startIndex to 0, otherwise set to index following closest preceding period
			startIndex = (start < 1) ? 0 : NearestDelimiter(textPane.GetText(), start, SeekBack) + 1;
			// if end<startIndex, set endIndex to closest period following startIndex
			// else, set it to closest period following end
			endIndex = NearestDelimiter(textPane.GetText(), (end < startIndex) ? startIndex : end, SeekForward);
			if (endIndex == -1)
			{
				endIndex = textPane.GetText().Length - 1;
			}
			HighlightText(startIndex, endIndex, highlightStyle);
			// enable/disable scroll buttons as necessary
			backButton.SetEnabled(startIndex != 0);
			forwardButton.SetEnabled(endIndex != textPane.GetText().Length - 1);
			parseNextButton.SetEnabled(forwardButton.IsEnabled() && parser != null);
		}

		/// <summary>Finds the nearest delimiter starting from index start.</summary>
		/// <remarks>
		/// Finds the nearest delimiter starting from index start. If <tt>seekDir</tt>
		/// is SEEK_FORWARD, finds the nearest delimiter after start.  Else, if it is
		/// SEEK_BACK, finds the nearest delimiter before start.
		/// </remarks>
		private int NearestDelimiter(string text, int start, int seekDir)
		{
			if (seekDir != SeekBack && seekDir != SeekForward)
			{
				throw new ArgumentException("Unknown seek direction " + seekDir);
			}
			StringReader reader = new StringReader(text);
			DocumentPreprocessor processor = new DocumentPreprocessor(reader);
			ITokenizerFactory<IHasWord> tf = tlp.GetTokenizerFactory();
			processor.SetTokenizerFactory(tf);
			IList<int> boundaries = new List<int>();
			foreach (IList<IHasWord> sentence in processor)
			{
				if (sentence.Count == 0)
				{
					continue;
				}
				if (!(sentence[0] is IHasOffset))
				{
					throw new InvalidCastException("Expected HasOffsets from the " + "DocumentPreprocessor");
				}
				if (boundaries.Count == 0)
				{
					boundaries.Add(0);
				}
				else
				{
					IHasOffset first = (IHasOffset)sentence[0];
					boundaries.Add(first.BeginPosition());
				}
			}
			boundaries.Add(text.Length);
			for (int i = 0; i < boundaries.Count - 1; ++i)
			{
				if (boundaries[i] <= start && start < boundaries[i + 1])
				{
					if (seekDir == SeekBack)
					{
						return boundaries[i] - 1;
					}
					else
					{
						if (seekDir == SeekForward)
						{
							return boundaries[i + 1] - 1;
						}
					}
				}
			}
			// The cursor position at the end is actually one past the text length.
			// We might as well highlight the last interval in that case.
			if (boundaries.Count >= 2 && start >= text.Length)
			{
				if (seekDir == SeekBack)
				{
					return boundaries[boundaries.Count - 2] - 1;
				}
				else
				{
					if (seekDir == SeekForward)
					{
						return boundaries[boundaries.Count - 1] - 1;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Highlights the sentence that is currently being selected by user
		/// (via mouse highlight)
		/// </summary>
		private void HighlightSelectedSentence()
		{
			HighlightSentence(textPane.GetSelectionStart(), textPane.GetSelectionEnd());
		}

		/// <summary>Highlights the sentence that is currently being edited</summary>
		private void HighlightEditedSentence()
		{
			HighlightSentence(textPane.GetCaretPosition());
		}

		/// <summary>Sets the status text at the bottom of the ParserPanel.</summary>
		public virtual void SetStatus(string status)
		{
			statusLabel.SetText(status);
		}

		private void SetFont()
		{
			if (tlp is ChineseTreebankLanguagePack)
			{
				SetChineseFont();
			}
			else
			{
				textPane.SetFont(new Font("Sans Serif", Font.Plain, 14));
				treePanel.SetFont(new Font("Sans Serif", Font.Plain, 14));
			}
		}

		private void SetChineseFont()
		{
			IList<Font> fonts = FontDetector.SupportedFonts(FontDetector.Chinese);
			if (fonts.Count > 0)
			{
				Font font = new Font(fonts[0].GetName(), Font.Plain, 14);
				textPane.SetFont(font);
				treePanel.SetFont(font);
				log.Info("Selected font " + font);
			}
			else
			{
				if (FontDetector.HasFont("Watanabe Mincho"))
				{
					textPane.SetFont(new Font("Watanabe Mincho", Font.Plain, 14));
					treePanel.SetFont(new Font("Watanabe Mincho", Font.Plain, 14));
				}
				else
				{
					textPane.SetFont(new Font("Sans Serif", Font.Plain, 14));
					treePanel.SetFont(new Font("Sans Serif", Font.Plain, 14));
				}
			}
		}

		/// <summary>
		/// Tokenizes the highlighted text (using a tokenizer appropriate for the
		/// selected language, and initiates the ParseThread to parse the tokenized
		/// text.
		/// </summary>
		public virtual void Parse()
		{
			if (textPane.GetText().Length == 0)
			{
				return;
			}
			// use endIndex+1 because substring subtracts 1
			string text = Sharpen.Runtime.Substring(textPane.GetText(), startIndex, endIndex + 1).Trim();
			if (parser != null && text.Length > 0)
			{
				//Tokenizer<? extends HasWord> toke = tlp.getTokenizerFactory().getTokenizer(new CharArrayReader(text.toCharArray()));
				ITokenizer<IHasWord> toke = tlp.GetTokenizerFactory().GetTokenizer(new StringReader(text));
				IList<IHasWord> wordList = toke.Tokenize();
				parseThread = new ParserPanel.ParseThread(this, wordList);
				parseThread.Start();
				StartProgressMonitor("Parsing", ParseTime);
			}
		}

		/// <summary>Opens dialog to load a text data file</summary>
		public virtual void LoadFile()
		{
			// centers dialog in panel
			pageDialog.SetLocation(GetLocationOnScreen().x + (GetWidth() - pageDialog.GetWidth()) / 2, GetLocationOnScreen().y + (GetHeight() - pageDialog.GetHeight()) / 2);
			pageDialog.SetVisible(true);
			if (pageDialog.GetStatus() == OpenPageDialog.ApproveOption)
			{
				LoadFile(pageDialog.GetPage());
			}
		}

		/// <summary>Loads a text or html file from a file path or URL.</summary>
		/// <remarks>
		/// Loads a text or html file from a file path or URL.  Treats anything
		/// beginning with <tt>http:\\</tt>,<tt>.htm</tt>, or <tt>.html</tt> as an
		/// html file, and strips all tags from the document
		/// </remarks>
		public virtual void LoadFile(string filename)
		{
			if (filename == null)
			{
				return;
			}
			File file = new File(filename);
			string urlOrFile = filename;
			// if file can't be found locally, try prepending http:// and looking on web
			if (!file.Exists() && filename.IndexOf("://") == -1)
			{
				urlOrFile = "http://" + filename;
			}
			else
			{
				// else prepend file:// to handle local html file urls
				if (filename.IndexOf("://") == -1)
				{
					urlOrFile = "file://" + filename;
				}
			}
			// TODO: why do any of this instead of just reading the file?  THIS SHOULD BE UPDATED FOR 2017!
			// Also, is this working correctly still?
			// load the document
			IDocument<object, Word, Word> doc;
			try
			{
				if (urlOrFile.StartsWith("http://") || urlOrFile.EndsWith(".htm") || urlOrFile.EndsWith(".html"))
				{
					// strip tags from html documents
					IDocument<object, Word, Word> docPre = new BasicDocument<object>().Init(new URL(urlOrFile));
					IDocumentProcessor<Word, Word, object, Word> noTags = new StripTagsProcessor<object, Word>();
					doc = noTags.ProcessDocument(docPre);
				}
				else
				{
					doc = new BasicDocument<object>(Edu.Stanford.Nlp.Parser.UI.ParserPanel.GetTokenizerFactory()).Init(new InputStreamReader(new FileInputStream(filename), encoding));
				}
			}
			catch (Exception e)
			{
				JOptionPane.ShowMessageDialog(this, "Could not load file " + filename + "\n" + e, null, JOptionPane.ErrorMessage);
				Sharpen.Runtime.PrintStackTrace(e);
				SetStatus("Error loading document");
				return;
			}
			// load the document into the text pane
			StringBuilder docStr = new StringBuilder();
			foreach (Word aDoc in doc)
			{
				if (docStr.Length > 0)
				{
					docStr.Append(' ');
				}
				docStr.Append(aDoc.ToString());
			}
			textPane.SetText(docStr.ToString());
			dataFileLabel.SetText(urlOrFile);
			HighlightSentence(0);
			forwardButton.SetEnabled(endIndex != textPane.GetText().Length - 1);
			// scroll to top of document
			textPane.SetCaretPosition(0);
			SetStatus("Done");
		}

		// TreebankLanguagePack returns a TokenizerFactory<? extends HasWord>
		// which isn't close enough in the type system, but is probably okay in practice
		private static ITokenizerFactory<Word> GetTokenizerFactory()
		{
			return (ITokenizerFactory<Word>)tlp.GetTokenizerFactory();
		}

		/// <summary>
		/// Opens a dialog and saves the output of the parser on the current
		/// text.
		/// </summary>
		/// <remarks>
		/// Opens a dialog and saves the output of the parser on the current
		/// text.  If there is no current text, yell at the user and make
		/// them feel bad instead.
		/// </remarks>
		public virtual void SaveOutput()
		{
			if (textPane.GetText().Trim().Length == 0)
			{
				JOptionPane.ShowMessageDialog(this, "No text to parse ", null, JOptionPane.ErrorMessage);
				return;
			}
			jfc.SetDialogTitle("Save file");
			int status = jfc.ShowSaveDialog(this);
			if (status == JFileChooser.ApproveOption)
			{
				SaveOutput(jfc.GetSelectedFile().GetPath());
			}
		}

		/// <summary>
		/// Saves the results of applying the parser to the current text to
		/// the specified filename.
		/// </summary>
		public virtual void SaveOutput(string filename)
		{
			if (filename == null || filename.Equals(string.Empty))
			{
				return;
			}
			string text = textPane.GetText();
			StringReader reader = new StringReader(text);
			DocumentPreprocessor processor = new DocumentPreprocessor(reader);
			ITokenizerFactory<IHasWord> tf = tlp.GetTokenizerFactory();
			processor.SetTokenizerFactory(tf);
			IList<IList<IHasWord>> sentences = new List<IList<IHasWord>>();
			foreach (IList<IHasWord> sentence in processor)
			{
				sentences.Add(sentence);
			}
			JProgressBar progress = new JProgressBar(0, sentences.Count);
			JButton cancel = new JButton();
			JDialog dialog = new JDialog(new Frame(), "Parser Progress", true);
			dialog.SetSize(300, 150);
			dialog.Add(BorderLayout.North, new JLabel("Parsing " + sentences.Count + " sentences"));
			dialog.Add(BorderLayout.Center, progress);
			dialog.Add(BorderLayout.South, cancel);
			//dialog.add(progress);
			ParserPanel.SaveOutputThread thread = new ParserPanel.SaveOutputThread(this, filename, progress, dialog, cancel, sentences);
			cancel.SetText("Cancel");
			cancel.SetToolTipText("Cancel");
			cancel.AddActionListener(null);
			thread.Start();
			dialog.SetVisible(true);
		}

		/// <summary>This class does the processing of the dialog box to a file.</summary>
		/// <remarks>
		/// This class does the processing of the dialog box to a file.  It
		/// also checks the cancelled variable after each processing to see
		/// if the user has chosen to cancel.  After running, it changes the
		/// label on the "cancel" button, waits a couple seconds, and then
		/// hides whatever dialog was passed in when originally created.
		/// </remarks>
		internal class SaveOutputThread : Thread
		{
			internal string filename;

			internal JProgressBar progress;

			internal JDialog dialog;

			internal JButton button;

			internal IList<IList<IHasWord>> sentences;

			internal bool cancelled;

			public SaveOutputThread(ParserPanel _enclosing, string filename, JProgressBar progress, JDialog dialog, JButton button, IList<IList<IHasWord>> sentences)
			{
				this._enclosing = _enclosing;
				this.filename = filename;
				this.progress = progress;
				this.dialog = dialog;
				this.button = button;
				this.sentences = sentences;
			}

			public override void Run()
			{
				int failures = 0;
				try
				{
					FileOutputStream fos = new FileOutputStream(this.filename);
					OutputStreamWriter ow = new OutputStreamWriter(fos, "utf-8");
					BufferedWriter bw = new BufferedWriter(ow);
					foreach (IList<IHasWord> sentence in this.sentences)
					{
						Tree tree = this._enclosing.parser.ParseTree(sentence);
						if (tree == null)
						{
							++failures;
							ParserPanel.log.Info("Failed on sentence " + sentence);
						}
						else
						{
							bw.Write(tree.ToString());
							bw.NewLine();
						}
						this.progress.SetValue(this.progress.GetValue() + 1);
						if (this.cancelled)
						{
							break;
						}
					}
					bw.Flush();
					bw.Close();
					ow.Close();
					fos.Close();
				}
				catch (IOException e)
				{
					JOptionPane.ShowMessageDialog(this._enclosing, "Could not save file " + this.filename + "\n" + e, null, JOptionPane.ErrorMessage);
					Sharpen.Runtime.PrintStackTrace(e);
					this._enclosing.SetStatus("Error saving parsed document");
				}
				if (failures == 0)
				{
					this.button.SetText("Success!");
				}
				else
				{
					this.button.SetText("Done.  " + failures + " parses failed");
				}
				if (this.cancelled && failures == 0)
				{
					this.dialog.SetVisible(false);
				}
				else
				{
					this.button.AddActionListener(null);
				}
			}

			private readonly ParserPanel _enclosing;
		}

		// end class SaveOutputThread
		/// <summary>Opens dialog to load a serialized parser</summary>
		public virtual void LoadParser()
		{
			jfc.SetDialogTitle("Load parser");
			int status = jfc.ShowOpenDialog(this);
			if (status == JFileChooser.ApproveOption)
			{
				string filename = jfc.GetSelectedFile().GetPath();
				if (filename.EndsWith(".jar"))
				{
					string model = chooseJarParser.Show(filename, jfcLocation.location);
					if (model != null)
					{
						LoadJarParser(filename, model);
					}
				}
				else
				{
					LoadParser(filename);
				}
			}
		}

		public virtual void LoadJarParser(string jarFile, string model)
		{
			lpThread = new ParserPanel.LoadParserThread(this, jarFile, model);
			lpThread.Start();
			StartProgressMonitor("Loading Parser", ParserLoadTime);
		}

		/// <summary>Loads a serialized parser specified by given path</summary>
		public virtual void LoadParser(string filename)
		{
			if (filename == null)
			{
				return;
			}
			// check if file exists before we start the worker thread and progress monitor
			File file = new File(filename);
			if (file.Exists())
			{
				lpThread = new ParserPanel.LoadParserThread(this, filename);
				lpThread.Start();
				StartProgressMonitor("Loading Parser", ParserLoadTime);
			}
			else
			{
				JOptionPane.ShowMessageDialog(this, "Could not find file " + filename, null, JOptionPane.ErrorMessage);
				SetStatus("Error loading parser");
			}
		}

		/// <summary>
		/// Initializes the progress bar with the status text, and the expected
		/// number of seconds the process will take, and starts the timer.
		/// </summary>
		private void StartProgressMonitor(string text, int maxCount)
		{
			if (glassPane == null)
			{
				if (GetRootPane() != null)
				{
					glassPane = GetRootPane().GetGlassPane();
					glassPane.AddMouseListener(new _MouseAdapter_607());
				}
			}
			if (glassPane != null)
			{
				glassPane.SetVisible(true);
			}
			// block input to components
			statusLabel.SetText(text);
			progressBar.SetMaximum(maxCount);
			progressBar.SetValue(0);
			count = 0;
			timer.Start();
			progressBar.SetVisible(true);
		}

		private sealed class _MouseAdapter_607 : MouseAdapter
		{
			public _MouseAdapter_607()
			{
			}

			public override void MouseClicked(MouseEvent evt)
			{
				Toolkit.GetDefaultToolkit().Beep();
			}
		}

		/// <summary>At the end of a task, shut down the progress monitor</summary>
		private void StopProgressMonitor()
		{
			timer.Stop();
			/*if(progressMonitor!=null) {
			progressMonitor.setProgress(progressMonitor.getMaximum());
			progressMonitor.close();
			}*/
			progressBar.SetVisible(false);
			if (glassPane != null)
			{
				glassPane.SetVisible(false);
			}
			// restore input to components
			lpThread = null;
			parseThread = null;
		}

		/// <summary>Worker thread for loading the parser.</summary>
		/// <remarks>
		/// Worker thread for loading the parser.  Loading a parser usually
		/// takes ~15s
		/// </remarks>
		private class LoadParserThread : Thread
		{
			internal readonly string zipFilename;

			internal readonly string filename;

			internal LoadParserThread(ParserPanel _enclosing, string filename)
			{
				this._enclosing = _enclosing;
				this.filename = filename;
				this.zipFilename = null;
			}

			internal LoadParserThread(ParserPanel _enclosing, string zipFilename, string filename)
			{
				this._enclosing = _enclosing;
				this.zipFilename = zipFilename;
				this.filename = filename;
			}

			public override void Run()
			{
				try
				{
					if (this.zipFilename != null)
					{
						this._enclosing.parser = LexicalizedParser.LoadModelFromZip(this.zipFilename, this.filename);
					}
					else
					{
						this._enclosing.parser = ((LexicalizedParser)LexicalizedParser.LoadModel(this.filename));
					}
				}
				catch (Exception)
				{
					JOptionPane.ShowMessageDialog(this._enclosing, "Error loading parser: " + this.filename, null, JOptionPane.ErrorMessage);
					this._enclosing.SetStatus("Error loading parser");
					this._enclosing.parser = null;
				}
				catch (OutOfMemoryException)
				{
					JOptionPane.ShowMessageDialog(this._enclosing, "Could not load parser. Out of memory.", null, JOptionPane.ErrorMessage);
					this._enclosing.SetStatus("Error loading parser");
					this._enclosing.parser = null;
				}
				this._enclosing.StopProgressMonitor();
				if (this._enclosing.parser != null)
				{
					this._enclosing.SetStatus("Loaded parser.");
					this._enclosing.parserFileLabel.SetText("Parser: " + this.filename);
					this._enclosing.parseButton.SetEnabled(true);
					this._enclosing.parseNextButton.SetEnabled(true);
					this._enclosing.saveOutputButton.SetEnabled(true);
					ParserPanel.tlp = this._enclosing.parser.GetOp().Langpack();
					this._enclosing.encoding = ParserPanel.tlp.GetEncoding();
				}
			}

			private readonly ParserPanel _enclosing;
		}

		/// <summary>Worker thread for parsing.</summary>
		/// <remarks>Worker thread for parsing.  Parsing a sentence usually takes ~5-60 sec</remarks>
		private class ParseThread : Thread
		{
			internal IList<IHasWord> sentence;

			public ParseThread(ParserPanel _enclosing, IList<IHasWord> sentence)
			{
				this._enclosing = _enclosing;
				this.sentence = sentence;
			}

			public override void Run()
			{
				bool successful;
				IParserQuery parserQuery = this._enclosing.parser.ParserQuery();
				try
				{
					successful = parserQuery.Parse(this.sentence);
				}
				catch (Exception)
				{
					this._enclosing.StopProgressMonitor();
					JOptionPane.ShowMessageDialog(this._enclosing, "Could not parse selected sentence\n(sentence probably too long)", null, JOptionPane.ErrorMessage);
					this._enclosing.SetStatus("Error parsing");
					return;
				}
				this._enclosing.StopProgressMonitor();
				this._enclosing.SetStatus("Done");
				if (successful)
				{
					// display the best parse
					Tree tree = parserQuery.GetBestParse();
					//tree.pennPrint();
					this._enclosing.treePanel.SetTree(tree);
					this._enclosing.clearButton.SetEnabled(true);
				}
				else
				{
					JOptionPane.ShowMessageDialog(this._enclosing, "Could not parse selected sentence", null, JOptionPane.ErrorMessage);
					this._enclosing.SetStatus("Error parsing");
					this._enclosing.treePanel.SetTree(null);
					this._enclosing.clearButton.SetEnabled(false);
				}
				if (this._enclosing.scrollWhenDone)
				{
					this._enclosing.ScrollForward();
				}
			}

			private readonly ParserPanel _enclosing;
		}

		private class JFileChooserLocation : IAncestorListener
		{
			internal Point location;

			internal JFileChooser jfc;

			internal JFileChooserLocation(JFileChooser jfc)
			{
				this.jfc = jfc;
				jfc.AddAncestorListener(this);
			}

			public virtual void AncestorAdded(AncestorEvent @event)
			{
				location = jfc.GetTopLevelAncestor().GetLocationOnScreen();
			}

			public virtual void AncestorMoved(AncestorEvent @event)
			{
				location = jfc.GetTopLevelAncestor().GetLocationOnScreen();
			}

			public virtual void AncestorRemoved(AncestorEvent @event)
			{
			}
		}

		/// <summary>Simulates a timer to update the progress monitor</summary>
		private class TimerListener : IActionListener
		{
			public virtual void ActionPerformed(ActionEvent e)
			{
				//progressMonitor.setProgress(Math.min(count++,progressMonitor.getMaximum()-1));
				this._enclosing.progressBar.SetValue(Math.Min(this._enclosing.count++, this._enclosing.progressBar.GetMaximum() - 1));
			}

			internal TimerListener(ParserPanel _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly ParserPanel _enclosing;
		}

		/// <summary>
		/// This method is called from within the constructor to
		/// initialize the form.
		/// </summary>
		/// <remarks>
		/// This method is called from within the constructor to
		/// initialize the form.
		/// WARNING: Do NOT modify this code. The content of this method is
		/// always regenerated by the Form Editor.
		/// </remarks>
		private void InitComponents()
		{
			//GEN-BEGIN:initComponents
			splitPane = new JSplitPane();
			topPanel = new JPanel();
			buttonsAndFilePanel = new JPanel();
			loadButtonPanel = new JPanel();
			loadFileButton = new JButton();
			loadParserButton = new JButton();
			saveOutputButton = new JButton();
			buttonPanel = new JPanel();
			backButton = new JButton();
			if (GetType().GetResource("/edu/stanford/nlp/parser/ui/leftarrow.gif") != null)
			{
				backButton.SetIcon(new ImageIcon(GetType().GetResource("/edu/stanford/nlp/parser/ui/leftarrow.gif")));
			}
			else
			{
				backButton.SetText("< Prev");
			}
			forwardButton = new JButton();
			if (GetType().GetResource("/edu/stanford/nlp/parser/ui/rightarrow.gif") != null)
			{
				forwardButton.SetIcon(new ImageIcon(GetType().GetResource("/edu/stanford/nlp/parser/ui/rightarrow.gif")));
			}
			else
			{
				forwardButton.SetText("Next >");
			}
			parseButton = new JButton();
			parseNextButton = new JButton();
			clearButton = new JButton();
			dataFilePanel = new JPanel();
			dataFileLabel = new JLabel();
			textScrollPane = new JScrollPane();
			textPane = new JTextPane();
			treeContainer = new JPanel();
			parserFilePanel = new JPanel();
			parserFileLabel = new JLabel();
			statusPanel = new JPanel();
			statusLabel = new JLabel();
			progressBar = new JProgressBar();
			progressBar.SetVisible(false);
			SetLayout(new BorderLayout());
			splitPane.SetOrientation(JSplitPane.VerticalSplit);
			topPanel.SetLayout(new BorderLayout());
			buttonsAndFilePanel.SetLayout(new BoxLayout(buttonsAndFilePanel, BoxLayout.YAxis));
			loadButtonPanel.SetLayout(new FlowLayout(FlowLayout.Left));
			loadFileButton.SetText("Load File");
			loadFileButton.SetToolTipText("Load a data file.");
			loadFileButton.AddActionListener(null);
			loadButtonPanel.Add(loadFileButton);
			loadParserButton.SetText("Load Parser");
			loadParserButton.SetToolTipText("Load a serialized parser.");
			loadParserButton.AddActionListener(null);
			loadButtonPanel.Add(loadParserButton);
			saveOutputButton.SetText("Save Output");
			saveOutputButton.SetToolTipText("Save the processed output.");
			saveOutputButton.SetEnabled(false);
			saveOutputButton.AddActionListener(null);
			loadButtonPanel.Add(saveOutputButton);
			buttonsAndFilePanel.Add(loadButtonPanel);
			buttonPanel.SetLayout(new FlowLayout(FlowLayout.Left));
			backButton.SetToolTipText("Scroll backward one sentence.");
			backButton.SetEnabled(false);
			backButton.AddActionListener(null);
			buttonPanel.Add(backButton);
			forwardButton.SetToolTipText("Scroll forward one sentence.");
			forwardButton.SetEnabled(false);
			forwardButton.AddActionListener(null);
			buttonPanel.Add(forwardButton);
			parseButton.SetText("Parse");
			parseButton.SetToolTipText("Parse selected sentence.");
			parseButton.SetEnabled(false);
			parseButton.AddActionListener(null);
			buttonPanel.Add(parseButton);
			parseNextButton.SetText("Parse >");
			parseNextButton.SetToolTipText("Parse selected sentence and then scrolls forward one sentence.");
			parseNextButton.SetEnabled(false);
			parseNextButton.AddActionListener(null);
			buttonPanel.Add(parseNextButton);
			clearButton.SetText("Clear");
			clearButton.SetToolTipText("Clears parse tree.");
			clearButton.SetEnabled(false);
			clearButton.AddActionListener(null);
			buttonPanel.Add(clearButton);
			buttonsAndFilePanel.Add(buttonPanel);
			dataFilePanel.SetLayout(new FlowLayout(FlowLayout.Left));
			dataFilePanel.Add(dataFileLabel);
			buttonsAndFilePanel.Add(dataFilePanel);
			topPanel.Add(buttonsAndFilePanel, BorderLayout.North);
			textPane.SetPreferredSize(new Dimension(250, 250));
			textPane.AddFocusListener(new _FocusAdapter_888(this));
			textPane.AddMouseListener(new _MouseAdapter_895(this));
			textPane.AddMouseMotionListener(new _MouseMotionAdapter_902(this));
			textScrollPane.SetViewportView(textPane);
			topPanel.Add(textScrollPane, BorderLayout.Center);
			splitPane.SetLeftComponent(topPanel);
			treeContainer.SetLayout(new BorderLayout());
			treeContainer.SetBackground(new Color(255, 255, 255));
			treeContainer.SetBorder(new BevelBorder(BevelBorder.Raised));
			treeContainer.SetForeground(new Color(0, 0, 0));
			treeContainer.SetPreferredSize(new Dimension(200, 200));
			treePanel = new TreeJPanel();
			treeContainer.Add("Center", treePanel);
			treePanel.SetBackground(Color.white);
			parserFilePanel.SetLayout(new FlowLayout(FlowLayout.Left));
			parserFilePanel.SetBackground(new Color(255, 255, 255));
			parserFileLabel.SetText("Parser: None");
			parserFilePanel.Add(parserFileLabel);
			treeContainer.Add(parserFilePanel, BorderLayout.North);
			splitPane.SetRightComponent(treeContainer);
			Add(splitPane, BorderLayout.Center);
			statusPanel.SetLayout(new FlowLayout(FlowLayout.Left));
			statusLabel.SetText("Ready");
			statusPanel.Add(statusLabel);
			progressBar.SetName(string.Empty);
			statusPanel.Add(progressBar);
			Add(statusPanel, BorderLayout.South);
		}

		private sealed class _FocusAdapter_888 : FocusAdapter
		{
			public _FocusAdapter_888(ParserPanel _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void FocusLost(FocusEvent evt)
			{
				this._enclosing.TextPaneFocusLost(evt);
			}

			private readonly ParserPanel _enclosing;
		}

		private sealed class _MouseAdapter_895 : MouseAdapter
		{
			public _MouseAdapter_895(ParserPanel _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void MouseClicked(MouseEvent evt)
			{
				this._enclosing.TextPaneMouseClicked(evt);
			}

			private readonly ParserPanel _enclosing;
		}

		private sealed class _MouseMotionAdapter_902 : MouseMotionAdapter
		{
			public _MouseMotionAdapter_902(ParserPanel _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void MouseDragged(MouseEvent evt)
			{
				this._enclosing.TextPaneMouseDragged(evt);
			}

			private readonly ParserPanel _enclosing;
		}

		//Roger -- test to see if I can get a bit of a fix with new font
		//GEN-END:initComponents
		private void TextPaneFocusLost(FocusEvent evt)
		{
			//GEN-FIRST:event_textPaneFocusLost
			//GEN-HEADEREND:event_textPaneFocusLost
			// highlights the sentence containing the current location of the cursor
			// note that the cursor is set to the beginning of the sentence when scrolling
			HighlightEditedSentence();
		}

		//GEN-LAST:event_textPaneFocusLost
		private void ParseNextButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_parseNextButtonActionPerformed
			//GEN-HEADEREND:event_parseNextButtonActionPerformed
			Parse();
			scrollWhenDone = true;
		}

		//GEN-LAST:event_parseNextButtonActionPerformed
		private void ClearButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_clearButtonActionPerformed
			//GEN-HEADEREND:event_clearButtonActionPerformed
			treePanel.SetTree(null);
			clearButton.SetEnabled(false);
		}

		//GEN-LAST:event_clearButtonActionPerformed
		private void TextPaneMouseDragged(MouseEvent evt)
		{
			//GEN-FIRST:event_textPaneMouseDragged
			//GEN-HEADEREND:event_textPaneMouseDragged
			HighlightSelectedSentence();
		}

		//GEN-LAST:event_textPaneMouseDragged
		private void TextPaneMouseClicked(MouseEvent evt)
		{
			//GEN-FIRST:event_textPaneMouseClicked
			//GEN-HEADEREND:event_textPaneMouseClicked
			HighlightSelectedSentence();
		}

		//GEN-LAST:event_textPaneMouseClicked
		private void ParseButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_parseButtonActionPerformed
			//GEN-HEADEREND:event_parseButtonActionPerformed
			Parse();
			scrollWhenDone = false;
		}

		//GEN-LAST:event_parseButtonActionPerformed
		private void LoadParserButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_loadParserButtonActionPerformed
			//GEN-HEADEREND:event_loadParserButtonActionPerformed
			LoadParser();
		}

		//GEN-LAST:event_loadParserButtonActionPerformed
		private void SaveOutputButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_saveOutputButtonActionPerformed
			//GEN-HEADEREND:event_saveOutputButtonActionPerformed
			SaveOutput();
		}

		//GEN-LAST:event_saveOutputButtonActionPerformed
		private void LoadFileButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_loadFileButtonActionPerformed
			//GEN-HEADEREND:event_loadFileButtonActionPerformed
			LoadFile();
		}

		//GEN-LAST:event_loadFileButtonActionPerformed
		private void BackButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_backButtonActionPerformed
			//GEN-HEADEREND:event_backButtonActionPerformed
			ScrollBack();
		}

		//GEN-LAST:event_backButtonActionPerformed
		private void ForwardButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_forwardButtonActionPerformed
			//GEN-HEADEREND:event_forwardButtonActionPerformed
			ScrollForward();
		}

		private JLabel dataFileLabel;

		private JPanel treeContainer;

		private JPanel topPanel;

		private JScrollPane textScrollPane;

		private JButton backButton;

		private JLabel statusLabel;

		private JButton loadFileButton;

		private JPanel loadButtonPanel;

		private JPanel buttonsAndFilePanel;

		private JButton parseButton;

		private JButton parseNextButton;

		private JButton forwardButton;

		private JLabel parserFileLabel;

		private JButton clearButton;

		private JSplitPane splitPane;

		private JPanel statusPanel;

		private JPanel dataFilePanel;

		private JPanel buttonPanel;

		private JTextPane textPane;

		private JProgressBar progressBar;

		private JPanel parserFilePanel;

		private JButton loadParserButton;

		private JButton saveOutputButton;
		//GEN-LAST:event_forwardButtonActionPerformed
		// Variables declaration - do not modify//GEN-BEGIN:variables
		// End of variables declaration//GEN-END:variables
	}
}
