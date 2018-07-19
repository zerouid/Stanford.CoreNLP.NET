using System;
using System.Collections.Generic;
using Java.Awt;
using Java.Awt.Event;
using Java.IO;
using Java.Util;
using Java.Util.Zip;
using Javax.Swing;
using Sharpen;

namespace Edu.Stanford.Nlp.UI
{
	/// <summary>
	/// This class shows a dialog which lets the user select a file from
	/// among a list of files contained in a given jar file.
	/// </summary>
	/// <remarks>
	/// This class shows a dialog which lets the user select a file from
	/// among a list of files contained in a given jar file.  (This should
	/// work for zip files as well, actually.)
	/// </remarks>
	/// <author>John Bauer</author>
	public class JarFileChooser
	{
		internal string pattern;

		internal Frame frame;

		internal JPanel panel;

		public JarFileChooser(string pattern, JPanel panel)
		{
			this.pattern = pattern;
			this.panel = panel;
		}

		public virtual string Show(string filename, Point location)
		{
			File jarFile = new File(filename);
			if (!jarFile.Exists())
			{
				JOptionPane.ShowMessageDialog(panel, "Filename " + jarFile + " does not exist", null, JOptionPane.ErrorMessage);
				return null;
			}
			IList<string> files;
			try
			{
				files = GetFiles(jarFile);
			}
			catch (Exception e)
			{
				// Something went wrong reading the file.
				JOptionPane.ShowMessageDialog(panel, "Filename " + jarFile + " had an error:\n" + e, null, JOptionPane.ErrorMessage);
				return null;
			}
			if (files.Count == 0)
			{
				JOptionPane.ShowMessageDialog(panel, "Filename " + jarFile + " does not contain any models", null, JOptionPane.ErrorMessage);
				return null;
			}
			return ShowListSelectionDialog(files, location);
		}

		public virtual string ShowListSelectionDialog(IList<string> files, Point location)
		{
			Frame frame = new Frame();
			//System.out.println(location);
			//frame.setLocation(location);
			JDialog dialog = new JDialog(frame, "Jar File Chooser", true);
			dialog.SetLocation(location);
			JList fileList = new JList(new Vector<string>(files));
			fileList.SetSelectionMode(ListSelectionModelConstants.SingleSelection);
			IMouseListener mouseListener = new _MouseAdapter_68(dialog);
			// double clicked
			fileList.AddMouseListener(mouseListener);
			JScrollPane scroll = new JScrollPane(fileList);
			JButton okay = new JButton();
			okay.SetText("Okay");
			okay.SetToolTipText("Okay");
			okay.AddActionListener(null);
			JButton cancel = new JButton();
			cancel.SetText("Cancel");
			cancel.SetToolTipText("Cancel");
			cancel.AddActionListener(null);
			GridBagLayout gridbag = new GridBagLayout();
			GridBagConstraints constraints = new GridBagConstraints();
			dialog.SetLayout(gridbag);
			constraints.gridwidth = GridBagConstraints.Remainder;
			constraints.fill = GridBagConstraints.Both;
			constraints.weightx = 1.0;
			constraints.weighty = 1.0;
			gridbag.SetConstraints(scroll, constraints);
			dialog.Add(scroll);
			constraints.gridwidth = GridBagConstraints.Relative;
			constraints.fill = GridBagConstraints.None;
			constraints.weighty = 0.0;
			gridbag.SetConstraints(okay, constraints);
			dialog.Add(okay);
			constraints.gridwidth = GridBagConstraints.Remainder;
			gridbag.SetConstraints(cancel, constraints);
			dialog.Add(cancel);
			dialog.Pack();
			dialog.SetSize(dialog.GetPreferredSize());
			dialog.SetVisible(true);
			if (fileList.IsSelectionEmpty())
			{
				return null;
			}
			return files[fileList.GetSelectedIndex()];
		}

		private sealed class _MouseAdapter_68 : MouseAdapter
		{
			public _MouseAdapter_68(JDialog dialog)
			{
				this.dialog = dialog;
			}

			public override void MouseClicked(MouseEvent e)
			{
				if (e.GetClickCount() == 2)
				{
					dialog.SetVisible(false);
				}
			}

			private readonly JDialog dialog;
		}

		/// <exception cref="Java.Util.Zip.ZipException"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual IList<string> GetFiles(File jarFile)
		{
			//System.out.println("Looking at " + jarFile);
			IList<string> files = new List<string>();
			ZipFile zin = new ZipFile(jarFile);
			IEnumeration<ZipEntry> entries = zin.Entries();
			while (entries.MoveNext())
			{
				ZipEntry entry = entries.Current;
				string name = entry.GetName();
				if (name.Matches(pattern))
				{
					files.Add(name);
				}
			}
			files.Sort();
			return files;
		}
	}
}
