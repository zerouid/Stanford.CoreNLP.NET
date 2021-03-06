





namespace Edu.Stanford.Nlp.IO.UI
{
	/// <summary>Simple dialog to ask user for url</summary>
	/// <author>Huy Nguyen</author>
	[System.Serializable]
	public class OpenPageDialog : JDialog
	{
		private const long serialVersionUID = -7987625449997527926L;

		public const int CancelOption = 0;

		public const int ApproveOption = 1;

		private JFileChooser jfc;

		private int status;

		/// <summary>Creates new form OpenPageDialog</summary>
		public OpenPageDialog(Frame parent, bool modal)
			: base(parent, modal)
		{
			InitComponents();
			jfc = new JFileChooser();
			AddWindowListener(new _WindowAdapter_33(this));
		}

		private sealed class _WindowAdapter_33 : WindowAdapter
		{
			public _WindowAdapter_33(OpenPageDialog _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void WindowClosing(WindowEvent we)
			{
				this._enclosing.status = Edu.Stanford.Nlp.IO.UI.OpenPageDialog.CancelOption;
			}

			private readonly OpenPageDialog _enclosing;
		}

		/// <summary>Sets the file chooser used by the Browse button</summary>
		public virtual void SetFileChooser(JFileChooser jfc)
		{
			this.jfc = jfc;
		}

		/* return URL in text field of dialog */
		public virtual string GetPage()
		{
			return urlTextField.GetText();
		}

		/* returns the status of the dialog (APPROVE_OPTION, CANCEL_OPTION) */
		public virtual int GetStatus()
		{
			return status;
		}

		/* use JFileChooser jfc to browse files */
		private void BrowseFiles()
		{
			jfc.SetDialogTitle("Open file");
			int status = jfc.ShowOpenDialog(this);
			if (status == JFileChooser.ApproveOption)
			{
				urlTextField.SetText(jfc.GetSelectedFile().GetPath());
				openButton.SetEnabled(true);
			}
		}

		private void Approve()
		{
			status = ApproveOption;
			CloseDialog(null);
		}

		/* Enables the open button if the urlTextField is non-empty.  Disables it otherwise */
		private void EnableOpenButton()
		{
			openButton.SetEnabled(urlTextField.GetText().Length > 0);
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
			jPanel1 = new JPanel();
			jLabel2 = new JLabel();
			jPanel3 = new JPanel();
			jLabel1 = new JLabel();
			urlTextField = new JTextField();
			jPanel2 = new JPanel();
			openButton = new JButton();
			cancelButton = new JButton();
			browseButton = new JButton();
			AddWindowListener(new _WindowAdapter_96(this));
			jPanel1.SetLayout(new BoxLayout(jPanel1, BoxLayout.YAxis));
			jLabel2.SetText("Type in the internet address of a document or web page.");
			jPanel1.Add(jLabel2);
			jLabel1.SetText("Open");
			jPanel3.Add(jLabel1);
			urlTextField.SetMinimumSize(new Dimension(100, 20));
			urlTextField.SetPreferredSize(new Dimension(300, 20));
			urlTextField.GetDocument().AddDocumentListener(new _IDocumentListener_113(this));
			urlTextField.AddActionListener(null);
			jPanel3.Add(urlTextField);
			jPanel1.Add(jPanel3);
			GetContentPane().Add(jPanel1, BorderLayout.North);
			openButton.SetText("Open");
			openButton.SetEnabled(false);
			openButton.AddActionListener(null);
			jPanel2.Add(openButton);
			cancelButton.SetText("Cancel");
			cancelButton.AddActionListener(null);
			jPanel2.Add(cancelButton);
			browseButton.SetText("Browse");
			browseButton.AddActionListener(null);
			jPanel2.Add(browseButton);
			GetContentPane().Add(jPanel2, BorderLayout.South);
			Pack();
		}

		private sealed class _WindowAdapter_96 : WindowAdapter
		{
			public _WindowAdapter_96(OpenPageDialog _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void WindowClosing(WindowEvent evt)
			{
				this._enclosing.CloseDialog(evt);
			}

			private readonly OpenPageDialog _enclosing;
		}

		private sealed class _IDocumentListener_113 : IDocumentListener
		{
			public _IDocumentListener_113(OpenPageDialog _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void ChangedUpdate(IDocumentEvent e)
			{
				this._enclosing.EnableOpenButton();
			}

			public void InsertUpdate(IDocumentEvent e)
			{
				this._enclosing.EnableOpenButton();
			}

			public void RemoveUpdate(IDocumentEvent e)
			{
				this._enclosing.EnableOpenButton();
			}

			private readonly OpenPageDialog _enclosing;
		}

		//GEN-END:initComponents
		private void UrlTextFieldActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_urlTextFieldActionPerformed
			//GEN-HEADEREND:event_urlTextFieldActionPerformed
			if (urlTextField.GetText().Length > 0)
			{
				Approve();
			}
		}

		//GEN-LAST:event_urlTextFieldActionPerformed
		private void BrowseButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_browseButtonActionPerformed
			//GEN-HEADEREND:event_browseButtonActionPerformed
			BrowseFiles();
		}

		//GEN-LAST:event_browseButtonActionPerformed
		private void CancelButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_cancelButtonActionPerformed
			//GEN-HEADEREND:event_cancelButtonActionPerformed
			status = CancelOption;
			CloseDialog(null);
		}

		//GEN-LAST:event_cancelButtonActionPerformed
		private void OpenButtonActionPerformed(ActionEvent evt)
		{
			//GEN-FIRST:event_openButtonActionPerformed
			//GEN-HEADEREND:event_openButtonActionPerformed
			Approve();
		}

		//GEN-LAST:event_openButtonActionPerformed
		/// <summary>Closes the dialog</summary>
		private void CloseDialog(WindowEvent evt)
		{
			//GEN-FIRST:event_closeDialog
			SetVisible(false);
			Dispose();
		}

		//GEN-LAST:event_closeDialog
		/// <param name="args">the command line arguments</param>
		public static void Main(string[] args)
		{
			new Edu.Stanford.Nlp.IO.UI.OpenPageDialog(new JFrame(), true).SetVisible(true);
		}

		private JTextField urlTextField;

		private JButton openButton;

		private JLabel jLabel1;

		private JPanel jPanel3;

		private JLabel jLabel2;

		private JPanel jPanel2;

		private JButton cancelButton;

		private JButton browseButton;

		private JPanel jPanel1;
		// Variables declaration - do not modify//GEN-BEGIN:variables
		// End of variables declaration//GEN-END:variables
	}
}
