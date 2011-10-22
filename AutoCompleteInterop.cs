using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
public class AutoCompleteInterop
{
	private static bool StringArrayContains_CaseInsensitive(string[] stringArray, string stringToSearchFor)
	{
		return Array.Find(stringArray, s => s.ToLower().Equals(stringToSearchFor.ToLower())) != null;
	}

	public static string[] GetWordlistOfFileContents(string fileContents)
	{
		string textToProcess = fileContents;
		textToProcess = textToProcess.Replace('\t', ' ');
		textToProcess = textToProcess.Replace("\n\r", " ");
		textToProcess = textToProcess.Replace("\r\n", " ");
		textToProcess = textToProcess.Replace("\r", " ");
		textToProcess = textToProcess.Replace("\n", " ");
		string[] allWordsIncludingDuplicates = textToProcess.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> wordsWithDuplicatesRemoved = new List<string>();
		foreach (string word in allWordsIncludingDuplicates)
		{
			string wordContainingOnlyLettersAndNumbers = "";
			foreach (char chr in word)
				if (char.IsLetterOrDigit(chr))
					wordContainingOnlyLettersAndNumbers += chr;
			if (!StringArrayContains_CaseInsensitive(wordsWithDuplicatesRemoved.ToArray(), wordContainingOnlyLettersAndNumbers))
				wordsWithDuplicatesRemoved.Add(wordContainingOnlyLettersAndNumbers);
		}
		return wordsWithDuplicatesRemoved.ToArray();
		//foreach (string word in System.Text.wor
	}

	private static string GetWordBeforeTextCaret(RichTextBox autoCompleteRichTextbox)
	{
		if (autoCompleteRichTextbox.SelectionStart == 0) return null;
		if (autoCompleteRichTextbox.Text[autoCompleteRichTextbox.SelectionStart - 1] == ' ') return null;
		string tmpWord = "";
		for (int i = autoCompleteRichTextbox.SelectionStart - 1; i >= 0; i--)
		{
			if (!char.IsLetterOrDigit(autoCompleteRichTextbox.Text, i)) break;
			tmpWord = autoCompleteRichTextbox.Text[i].ToString() + tmpWord;
		}
		return tmpWord == "" ? null : tmpWord;
	}

	private static void ReplaceWordBeforeCaterWithNewWord(RichTextBox autoCompleteRichTextbox, string newWord)
	{
		string wordBeforeCaret = GetWordBeforeTextCaret(autoCompleteRichTextbox);
		if (wordBeforeCaret != null)
		{
			int positionOfWordStart = autoCompleteRichTextbox.SelectionStart - wordBeforeCaret.Length;
			autoCompleteRichTextbox.Text = autoCompleteRichTextbox.Text.Remove(positionOfWordStart, wordBeforeCaret.Length);
			autoCompleteRichTextbox.Text = autoCompleteRichTextbox.Text.Insert(positionOfWordStart, newWord);
			autoCompleteRichTextbox.SelectionStart = positionOfWordStart + newWord.Length;
		}
	}

	public static void SetFullAutocompleteListOfRichTextbox(RichTextBox richTextbox, string[] newFullListOfAutocompleteItems)
	{
		if (!richTextboxListWithAutocompleteEnabled.ContainsKey(richTextbox) &&
			UserMessages.ShowWarningMessage("Cannot find linked autocomplete popup form for richTextbox: " + richTextbox.Name))
			return;
		richTextboxListWithAutocompleteEnabled[richTextbox].completeAutocompleteList = newFullListOfAutocompleteItems;
	}

	private static void UpdatePositionOfPopupWindow(RichTextBox richTextbox)
	{
		Console.WriteLine("Updating position for: " + richTextbox.Name);
		Point popupPosition = richTextbox.GetPositionFromCharIndex(richTextbox.SelectionStart);
		popupPosition.X -= (int)(richTextbox.CreateGraphics().MeasureString(richTextbox.Text[richTextbox.SelectionStart - 1].ToString(), richTextbox.Font).Width / 2);
		popupPosition.Y += (int)(richTextbox.Font.GetHeight()) + 3;
		if (richTextboxListWithAutocompleteEnabled.ContainsKey(richTextbox))
			richTextboxListWithAutocompleteEnabled[richTextbox].Location = richTextbox.PointToScreen(popupPosition);
	}

	private static void UpdateHeightOfPopupWindow(RichTextBox thisRichTextbox)
	{
		if (!richTextboxListWithAutocompleteEnabled.ContainsKey(thisRichTextbox)) return;
		AutocompletePopupTreeviewForm autocompletePopupForm = richTextboxListWithAutocompleteEnabled[thisRichTextbox];
		int popupHeight = (int)(autocompletePopupForm.treeView1.Nodes.Count * autocompletePopupForm.treeView1.ItemHeight) + 10;
		autocompletePopupForm.Height = popupHeight < 500 ? popupHeight : 500;
	}

	private static Dictionary<RichTextBox, AutocompletePopupTreeviewForm> richTextboxListWithAutocompleteEnabled = new Dictionary<RichTextBox, AutocompletePopupTreeviewForm>();
	private static Dictionary<RichTextBox, Form> parentFormListOfRichTextboxes = new Dictionary<RichTextBox, Form>();
	public static void EnableRichTextboxAutocomplete(RichTextBox richTextbox, string[] fullListOfAutocompleteItems = null)
	{
		if (richTextboxListWithAutocompleteEnabled.ContainsKey(richTextbox))
			UserMessages.ShowWarningMessage("Autocomplete was already enabled on richtextbox: " + richTextbox.Name);
		else
		{
			Form parentFormOfRichTextbox = richTextbox.FindForm();
			if (parentFormOfRichTextbox != null)
			{
				if (!parentFormListOfRichTextboxes.ContainsKey(richTextbox))
					parentFormListOfRichTextboxes.Add(richTextbox, parentFormOfRichTextbox);

				parentFormOfRichTextbox.Move += (snder, evtargs) =>
				{
					foreach (RichTextBox rb in parentFormListOfRichTextboxes.Keys)
					{
						Console.WriteLine("Checking for " + rb.Name);
						if (parentFormListOfRichTextboxes[rb] == snder as Form)
						{
							Kry die reg dit werk nie
							UpdatePositionOfPopupWindow(rb);
							//break;
						}
					}
					//UpdatePositionOfPopupWindow(parentFormListOfRichTextboxes[snder as Form]);
				};
			}

			richTextbox.Move += (snder, evtargs) =>
			{
				UpdatePositionOfPopupWindow(snder as RichTextBox);
			};

			richTextbox.Disposed += (snder, evtargs) =>
			{
				AutocompletePopupTreeviewForm thisAutocompletePopupTreeviewForm = richTextboxListWithAutocompleteEnabled[snder as RichTextBox];
				if (thisAutocompletePopupTreeviewForm != null && !thisAutocompletePopupTreeviewForm.Disposing && !thisAutocompletePopupTreeviewForm.IsDisposed)
					thisAutocompletePopupTreeviewForm.Close();
			};

			richTextbox.LostFocus += (snder, evtargs) =>
			{
				AutocompletePopupTreeviewForm thisAutocompletePopupTreeviewForm = richTextboxListWithAutocompleteEnabled[snder as RichTextBox];
				if (!thisAutocompletePopupTreeviewForm.treeView1.Focused)
					if (thisAutocompletePopupTreeviewForm != null && !thisAutocompletePopupTreeviewForm.Disposing && !thisAutocompletePopupTreeviewForm.IsDisposed)
						thisAutocompletePopupTreeviewForm.Hide();
			};

			richTextbox.KeyDown += (snder, evtargs) =>
			{
				if (!richTextboxListWithAutocompleteEnabled.ContainsKey(snder as RichTextBox))
					UserMessages.ShowWarningMessage("Could not find attached autocomplete popup for richTextbox");
				else
				{
					AutocompletePopupTreeviewForm thisAutocompletePopupForm = richTextboxListWithAutocompleteEnabled[snder as RichTextBox];
					if (thisAutocompletePopupForm.treeView1.Nodes.Count > 0 &&
						(evtargs.KeyCode == Keys.Down ||
						evtargs.KeyCode == Keys.Up ||
						evtargs.KeyCode == Keys.PageDown ||
						evtargs.KeyCode == Keys.PageUp))
					{
						thisAutocompletePopupForm.Activate();
					}
					//AutocompletePopupTreeviewForm form2 = (sn
					if (evtargs.KeyCode == Keys.Escape)
					{
						if (thisAutocompletePopupForm.Visible) thisAutocompletePopupForm.Hide();
					}
					else if (evtargs.KeyCode == Keys.Down)
					{
						if (thisAutocompletePopupForm.Visible)
						{
							int newSelectedIndex = (thisAutocompletePopupForm.treeView1.SelectedNode == null ? -1 : thisAutocompletePopupForm.treeView1.SelectedNode.Index) + 1;
							if (newSelectedIndex < thisAutocompletePopupForm.treeView1.Nodes.Count)
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[newSelectedIndex];
							evtargs.Handled = true;
						}
					}
					else if (evtargs.KeyCode == Keys.Up)
					{
						if (thisAutocompletePopupForm.Visible)
						{
							int newSelectedIndex =
									(thisAutocompletePopupForm.treeView1.SelectedNode == null ? thisAutocompletePopupForm.treeView1.Nodes.Count : thisAutocompletePopupForm.treeView1.SelectedNode.Index) - 1;
							if (newSelectedIndex >= 0)
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[newSelectedIndex];
							evtargs.Handled = true;
						}
					}
					else if (evtargs.KeyCode == Keys.PageDown)
					{
						if (thisAutocompletePopupForm.Visible)
						{
							int newSelectedIndex = (thisAutocompletePopupForm.treeView1.SelectedNode == null ? -1 : thisAutocompletePopupForm.treeView1.SelectedNode.Index) + 5;
							if (newSelectedIndex < thisAutocompletePopupForm.treeView1.Nodes.Count)
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[newSelectedIndex];
							else
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[thisAutocompletePopupForm.treeView1.Nodes.Count - 1];
							evtargs.Handled = true;
						}
					}
					else if (evtargs.KeyCode == Keys.PageUp)
					{
						if (thisAutocompletePopupForm.Visible)
						{
							int newSelectedIndex = (thisAutocompletePopupForm.treeView1.SelectedNode == null ? thisAutocompletePopupForm.treeView1.Nodes.Count : thisAutocompletePopupForm.treeView1.SelectedNode.Index) - 5;
							if (newSelectedIndex >= 0)
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[newSelectedIndex];
							else
								thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[0];
							evtargs.Handled = true;
						}
					}
				}
			};

			richTextbox.TextChanged += (snder, evtargs) =>
			{
				if (!richTextboxListWithAutocompleteEnabled.ContainsKey(snder as RichTextBox))
					UserMessages.ShowWarningMessage("Could not find attached autocomplete popup for richTextbox");
				else
				{
					AutocompletePopupTreeviewForm thisAutocompletePopupForm = richTextboxListWithAutocompleteEnabled[snder as RichTextBox];
					RichTextBox thisRichTextbox = snder as RichTextBox;
					if (thisRichTextbox.SelectionStart > 0 && thisRichTextbox.Text[thisRichTextbox.SelectionStart - 1] != ' ')
					{
						UpdatePositionOfPopupWindow(thisRichTextbox);
						if (thisAutocompletePopupForm.treeView1.SelectedNode == null && thisAutocompletePopupForm.treeView1.Nodes.Count > 0)
							thisAutocompletePopupForm.treeView1.SelectedNode = thisAutocompletePopupForm.treeView1.Nodes[0];
						string word = GetWordBeforeTextCaret(snder as RichTextBox);
						if (word != null)
						{
							if (thisAutocompletePopupForm.completeAutocompleteList != null)
							{
								string[] newAutocompleteList = Array.FindAll(thisAutocompletePopupForm.completeAutocompleteList,
								 s =>
									 s.Trim().ToUpper().StartsWith(word.Trim().ToUpper())//The item starts with (case-insensitive) the last types word at the cursor
									 );

								if (newAutocompleteList != null && newAutocompleteList.Length > 0)
								{
									thisAutocompletePopupForm.treeView1.Nodes.Clear();
									foreach (string wordInList in newAutocompleteList)
										thisAutocompletePopupForm.treeView1.Nodes.Add(wordInList, wordInList);
									thisAutocompletePopupForm.filteredAutocompleteList = newAutocompleteList;

									thisAutocompletePopupForm.Show();
									(snder as RichTextBox).FindForm().Activate();
								}
								else thisAutocompletePopupForm.Hide();
							}
							//int popupHeight = (int)(thisAutocompletePopupForm.treeView1.Nodes.Count * thisAutocompletePopupForm.treeView1.Font.GetHeight());
							UpdateHeightOfPopupWindow(thisRichTextbox);
						}
					}
					else if (thisAutocompletePopupForm.Visible) thisAutocompletePopupForm.Hide();
					//toolStripStatusLabel1.Text = richTextBox1.SelectionStart.ToString();
				}
			};

			AutocompletePopupTreeviewForm autoCompletePopupForm = new AutocompletePopupTreeviewForm(fullListOfAutocompleteItems);
			autoCompletePopupForm.Font = richTextbox.Font;

			autoCompletePopupForm.treeView1.KeyDown += (snder, evtargs) =>
			{
				if (evtargs.KeyCode == Keys.Enter)
				{
					evtargs.Handled = true;
					if ((snder as TreeView).SelectedNode != null)
					{
						AutocompletePopupTreeviewForm thisAutocompletePopupForm = ((snder as TreeView).FindForm() as AutocompletePopupTreeviewForm);
						if (richTextboxListWithAutocompleteEnabled.ContainsValue(thisAutocompletePopupForm))
						{
							foreach (RichTextBox rt in richTextboxListWithAutocompleteEnabled.Keys)
							{
								if (richTextboxListWithAutocompleteEnabled[rt] == thisAutocompletePopupForm)
								{
									ReplaceWordBeforeCaterWithNewWord(rt, (snder as TreeView).SelectedNode.Text.ToString());
									break;
								}
							}
						}
					}
				}
				else if (evtargs.KeyCode == Keys.Escape)
				{
					evtargs.Handled = true;
					(snder as TreeView).FindForm().Hide();
				}
			};
			autoCompletePopupForm.treeView1.KeyPress += (snder, evtargs) =>
			{
				if (evtargs.KeyChar == (char)27 || evtargs.KeyChar == (char)13)
					evtargs.Handled = true;
			};
			richTextboxListWithAutocompleteEnabled.Add(richTextbox, autoCompletePopupForm);
		}
	}

	private class AutocompletePopupTreeviewForm : Form
	{
		public string[] completeAutocompleteList;
		public string[] filteredAutocompleteList;

		public AutocompletePopupTreeviewForm(string[] fullListOfAutocompleteItems)
		{
			InitializeComponent();
			completeAutocompleteList = fullListOfAutocompleteItems;
		}

		private void Form2_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				this.Hide();
			}
		}

		private void Form2_Shown(object sender, EventArgs e)
		{
			StylingInterop.SetTreeviewVistaStyle(treeView1);
		}

		//private void treeView1_KeyDown(object sender, KeyEventArgs e)
		//{
		//  if (e.KeyCode == Keys.Escape)
		//  {
		//    e.Handled = true;
		//    this.Hide();
		//  }
		//}

		//private void treeView1_KeyPress(object sender, KeyPressEventArgs e)
		//{
		//  if (e.KeyChar == (char)27 || e.KeyChar == (char)13)
		//    e.Handled = true;
		//}

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.SuspendLayout();
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.HideSelection = false;
			this.treeView1.HotTracking = true;
			this.treeView1.Indent = 5;
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Margin = new System.Windows.Forms.Padding(0);
			this.treeView1.Name = "treeView1";
			this.treeView1.ShowLines = false;
			this.treeView1.ShowPlusMinus = false;
			this.treeView1.ShowRootLines = false;
			this.treeView1.Size = new System.Drawing.Size(284, 262);
			this.treeView1.TabIndex = 0;
			//this.treeView1.DrawMode = TreeViewDrawMode.OwnerDrawAll;
			//this.treeView1.DrawNode += new DrawTreeNodeEventHandler(treeView1_DrawNode);
			//this.treeView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView1_KeyDown);
			//this.treeView1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.treeView1_KeyPress);
			// 
			// Form2
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.ControlBox = false;
			this.Controls.Add(this.treeView1);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MinimumSize = new System.Drawing.Size(200, 50);
			this.Name = "Form2";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Form2";
			this.TopMost = true;
			this.TransparencyKey = System.Drawing.SystemColors.Control;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form2_FormClosing);
			this.Shown += new System.EventHandler(this.Form2_Shown);
			this.ResumeLayout(false);

		}

		/*Font tagFont = new Font("Helvetica", 8, FontStyle.Bold);
		private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			// Draw the background and node text for a selected node.
			if ((e.State & TreeNodeStates.Selected) != 0)
			{
				// Draw the background of the selected node. The NodeBounds
				// method makes the highlight rectangle large enough to
				// include the text of a node tag, if one is present.
				LinearGradientBrush myBrush = new LinearGradientBrush(e.Bounds, Color.FromArgb(120, 120, 155), Color.FromArgb(150, 150, 170), 90f);
				e.Graphics.FillRectangle(myBrush, Rectangle.Inflate(NodeBounds(e.Node), 10, 0));

				// Retrieve the node font. If the node font has not been set,
				// use the TreeView font.
				Font nodeFont = e.Node.NodeFont;
				if (nodeFont == null) nodeFont = ((TreeView)sender).Font;

				// Draw the node text.
				e.Graphics.DrawString(e.Node.Text, nodeFont, Brushes.White,
						e.Bounds);//Rectangle.Inflate(e.Bounds, -20, 0));
			}

			// Use the default background and node text.
			else
			{
				e.DrawDefault = true;
			}

			// If a node tag is present, draw its string representation 
			// to the right of the label text.
			if (e.Node.Tag != null)
			{
				e.Graphics.DrawString(e.Node.Tag.ToString(), tagFont,
						Brushes.Yellow, e.Bounds.Right + 2, e.Bounds.Top);
			}

			// If the node has focus, draw the focus rectangle large, making
			// it large enough to include the text of the node tag, if present.
			if ((e.State & TreeNodeStates.Focused) != 0)
			{
				using (Pen focusPen = new Pen(Color.Black))
				{
					focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
					Rectangle focusBounds = NodeBounds(e.Node);
					focusBounds.Size = new Size(focusBounds.Width - 1,
					focusBounds.Height - 1);
					e.Graphics.DrawRectangle(focusPen, focusBounds);
				}
			}
		}

		// Returns the bounds of the specified node, including the region 
		// occupied by the node label and any node tag displayed.
		private Rectangle NodeBounds(TreeNode node)
		{
			// Set the return value to the normal node bounds.
			Rectangle bounds = node.Bounds;
			if (node.Tag != null)
			{
				// Retrieve a Graphics object from the TreeView handle
				// and use it to calculate the display width of the tag.
				Graphics g = this.treeView1.CreateGraphics();
				int tagWidth = (int)g.MeasureString
						(node.Tag.ToString(), tagFont).Width + 6;

				// Adjust the node bounds using the calculated value.
				bounds.Offset(tagWidth / 2, 0);
				bounds = Rectangle.Inflate(bounds, tagWidth / 2, 0);
				g.Dispose();
			}

			return bounds;

		}*/

		#endregion

		public System.Windows.Forms.TreeView treeView1;



	}
}