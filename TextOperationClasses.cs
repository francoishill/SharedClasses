using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using eisiWare;
using System.ComponentModel;
using System.Collections.ObjectModel;
using DataGridView = System.Windows.Forms.DataGridView;
using ITextOperation = SharedClasses.TextOperations.ITextOperation;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;

namespace SharedClasses
{
	#region Extension functions
	public static class ExtensionsToTextOperations
	{
		/// <summary>
		/// The actionToPerform will be performed and status will be updated before and after (including the time taken).
		/// </summary>
		/// <param name="actionToPerform">The action to perform.</param>
		/// <param name="StatusToSayStarting">The status text to say the action is starting, please wait.</param>
		/// <param name="StatusToSayCompletedAddZeroParameter">The status text to say it is completed i.e. "Action completed in {0} seconds.</param>
		/*/// <param name="MakeProgressbarVisibleDuring">Whether the progressbar should be made visible during the action.</param>*/
		/// <param name="completionMessageTimeout">The timeout after how long the message showed for completion is hidden.</param>
		public static void PerformTimedActionAndUpdateStatus(this Action actionToPerform, TextFeedbackEventHandler textFeedbackEvent, string StatusToSayStarting, string StatusToSayCompletedAddZeroParameter, /*bool MakeProgressbarVisibleDuring, */int completionMessageTimeout = 0, params Func<string>[] AdditionalArguments)
		{
			if (textFeedbackEvent == null)
				textFeedbackEvent = new TextFeedbackEventHandler(delegate { });

			textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(StatusToSayStarting));

			DoEvents();
			Stopwatch sw = Stopwatch.StartNew();
			actionToPerform();
			sw.Stop();
			//string completionMessage = string.Format(StatusToSayCompletedAddZeroParameter, Math.Round(sw.Elapsed.TotalSeconds, 3));
			string completionMessage = StatusToSayCompletedAddZeroParameter.Replace("{0}", Math.Round(sw.Elapsed.TotalSeconds, 3).ToString());
			for (int i = 0; i < AdditionalArguments.Length; i++)
				completionMessage = completionMessage.Replace("{" + (i + 1) + "}", AdditionalArguments[i]());
			textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(completionMessage));
			if (completionMessageTimeout > 0)
			{
				System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
				timer.Interval = completionMessageTimeout;
				timer.Tag = completionMessage;
				timer.Tick += (s, e) =>
				{
					System.Windows.Forms.Timer t = s as System.Windows.Forms.Timer;
					if (t == null) return;
					t.Stop();
					if (t.Tag != null && t.Tag.ToString() == completionMessage)
						textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(null));
					t.Dispose();
					t = null;
				};
				timer.Start();
			}
			DoEvents();
		}

		public static void DoEvents()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
		}

		public static bool ContainsChildInTree(this ITextOperation haystack, ref ITextOperation needle)
		{
			if (haystack == null || needle == null)
				return false;
			foreach (ITextOperation child in haystack.Children)
				if (child.ContainsChildInTree(ref needle))
					return true;
				else if (child == needle)
					return true;
			return false;
		}

		public static void CopyControlValue(this Control control, ref Control otherControl)
		{
			if (control == null || otherControl == null)
				return;
			if (control.GetType() != otherControl.GetType())
				return;

			if (control is TextBox)
				(otherControl as TextBox).Text = (control as TextBox).Text;
			else if (control is NumericUpDown)
				(otherControl as NumericUpDown).Value = (control as NumericUpDown).Value;
			else if (control is CheckBox)
				(otherControl as CheckBox).IsChecked = (control as CheckBox).IsChecked;
			else
				UserMessages.ShowWarningMessage(string.Format("Currently control of type '{0}' is currently not supported in cloning."));
		}
	}
	#endregion Extension functions

	#region Other static functions
	public static class TextOperationsUI
	{
		public static bool ImportProcessFile(IList<ITextOperation> CurrentList, TreeView mainTreeview, out string url)
		{
			url = null;

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Xml files (*.xml)|*.xml";
			if (ofd.ShowDialog().Value)
			{
				if (mainTreeview.Items.Count == 0 ||UserMessages.Confirm("The operation list is currently not empty and will be cleared when importing file, continue?"))
				{
					//treeView1.Items.Clear();
					CurrentList.Clear();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(ofd.FileName);
					XmlNode tv = xmlDoc.SelectSingleNode("TreeView");
					for (int i = 0; i < tv.Attributes.Count; i++)
						if (string.Equals(tv.Attributes[i].Name, "Url", StringComparison.InvariantCultureIgnoreCase))
							url = tv.Attributes[i].Value;

					XmlNodeList tvitems = xmlDoc.SelectNodes("TreeView/TreeViewItem");
					foreach (XmlNode node in tvitems)
						TextOperationsUI.AddNodeAndSubNodesToTreeviewItem(CurrentList, mainTreeview, null, node);
					return true;
				}
			}
			return false;
		}

		public static void ExportProcessFile(IList<ITextOperation> CurrentList, TreeView mainTreeview, string url = null)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Xml files (*.xml)|*.xml";
			if (sfd.ShowDialog().Value)
			{
				using (var xw = new XmlTextWriter(sfd.FileName, System.Text.Encoding.ASCII) { Formatting = Formatting.Indented })
				{
					xw.WriteStartElement("TreeView");
					xw.WriteAttributeString("Url", url ?? "");
					foreach (ITextOperation ddo in mainTreeview.Items)
						WriteTreeViewItemToXmlTextWriter(mainTreeview.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, xw);
					xw.WriteEndElement();
				}
			}
		}

		public static void WriteTreeViewItemToXmlTextWriter(TreeViewItem tvi, XmlTextWriter xmltextWriter)
		{
			ITextOperation textop = tvi.Header as ITextOperation;

			xmltextWriter.WriteStartElement("TreeViewItem");
			xmltextWriter.WriteAttributeString("DisplayName", textop.DisplayName);
			WriteInputControlsToXmlTextWriter(textop, xmltextWriter);
			if (textop != null)
				xmltextWriter.WriteAttributeString("TypeName", textop.GetType().FullName);
			foreach (ITextOperation ddo1 in tvi.Items)
				WriteTreeViewItemToXmlTextWriter(tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem, xmltextWriter);
			xmltextWriter.WriteEndElement();
		}

		private static void WriteInputControlsToXmlTextWriter(ITextOperation textop, XmlTextWriter xmltextWriter)
		{
			List<string> tmpNameList = new List<string>();
			if (textop == null || !textop.HasInputControls)
				return;
			foreach (Control control in textop.InputControls)
			{
				string InputControlValue = "";
				if (control is TextBox)
					InputControlValue = (control as TextBox).Text;
				else if (control is NumericUpDown)
					InputControlValue = (control as NumericUpDown).Value.ToString();
				else if (control is CheckBox)
					InputControlValue = (control as CheckBox).IsChecked == null ? false.ToString() : (control as CheckBox).IsChecked.Value.ToString();
				else
					UserMessages.ShowWarningMessage("Input control type not supported: " + control.GetType().Name);

				if (string.IsNullOrWhiteSpace(control.Name))
					UserMessages.ShowWarningMessage("No name found for InputControl in " + textop.DisplayName);
				else if (tmpNameList.Contains(control.Name))
					UserMessages.ShowWarningMessage("Duplicate control names found: " + control.Name);
				else
				{
					tmpNameList.Add(control.Name);
					xmltextWriter.WriteAttributeString(control.Name, InputControlValue);
				}
			}
		}

		public static void AddNodeAndSubNodesToTreeviewItem(IList<ITextOperation> CurrentList, TreeView mainTreeview, TreeViewItem baseTreeViewItem, XmlNode xmlnode)
		{
			string tmpName = xmlnode.Attributes["DisplayName"].Value;
			if (string.IsNullOrWhiteSpace(tmpName))
				UserMessages.ShowWarningMessage("Cannot read TreeViewItem name: " + xmlnode.OuterXml);
			else
			{
				string typeFullName = xmlnode.Attributes["TypeName"].Value;
				if (string.IsNullOrWhiteSpace(typeFullName))
					UserMessages.ShowWarningMessage("Cannot read TypeName from '" + tmpName + "' node: " + xmlnode.OuterXml);
				else
				{
					TextOperations.ITextOperation to = GetNewITextOperationFromTypeFullName(typeFullName);
					if (to == null)
						UserMessages.ShowWarningMessage("Could not create new object from FullName = " + typeFullName);
					else
					{
						PopulateInputControlsFromXmlNode(to, xmlnode);
						//ITextOperation ddo = new ITextOperation(to);

						if (baseTreeViewItem == null)
						{
							//treeView1.Items.Add(ddo);
							CurrentList.Add(to);
							mainTreeview.UpdateLayout();
							XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
							foreach (XmlNode node in subnodes)
								AddNodeAndSubNodesToTreeviewItem(CurrentList, mainTreeview, mainTreeview.ItemContainerGenerator.ContainerFromItem(to) as TreeViewItem, node);
						}
						else
						{
							//baseTreeViewItem.Items.Add(ddo);
							(baseTreeViewItem.Header as ITextOperation).Children.Add(to);
							baseTreeViewItem.IsExpanded = true;
							baseTreeViewItem.UpdateLayout();
							XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
							foreach (XmlNode node in subnodes)
								AddNodeAndSubNodesToTreeviewItem(CurrentList, mainTreeview, baseTreeViewItem.ItemContainerGenerator.ContainerFromItem(to) as TreeViewItem, node);
						}
					}
				}
			}
		}

		public static void PopulateInputControlsFromXmlNode(TextOperations.ITextOperation to, XmlNode xmlnode)
		{
			foreach (Control control in to.InputControls)
			{
				string tmpControlValue = xmlnode.Attributes[control.Name].Value;
				//if (string.IsNullOrWhiteSpace(tmpControlName))
				if (string.IsNullOrEmpty(tmpControlValue))//Do not use IsNullOrWhiteSpace otherwise if for instance the SplitUsingString textbox value was " " it will warn
					UserMessages.ShowWarningMessage("Could not populate control value, cannot find attribute '" + control.Name + "': " + xmlnode.OuterXml);
				else
				{
					if (control is TextBox)
						(control as TextBox).Text = tmpControlValue;
					else if (control is NumericUpDown)
					{
						int intval;
						if (!int.TryParse(tmpControlValue, out intval))
							UserMessages.ShowWarningMessage("Invalid numeric value for " + control.Name + ": " + xmlnode.OuterXml);
						else
						{
							(control as NumericUpDown).Value = intval;
						}
					}
					else if (control is CheckBox)
					{
						bool boolval;
						if (!bool.TryParse(tmpControlValue, out boolval))
						{
							UserMessages.ShowWarningMessage("Invalid string for checkbox checked (boolean) value: '" + tmpControlValue + "', will use false");
							(control as CheckBox).IsChecked = false;
						}
						else
							(control as CheckBox).IsChecked = boolval;
					}
					else
						UserMessages.ShowWarningMessage("Input control type not supported: " + control.GetType().Name);
				}
			}
		}

		private static TextOperations.ITextOperation GetNewITextOperationFromTypeFullName(string typeName)
		{
			foreach (Type to in typeof(TextOperations).GetNestedTypes())
			{
				if (to.IsClass && !to.IsAbstract)
					if (to.GetInterface(typeof(TextOperations.ITextOperation).Name) != null)//<dynamic>).Name) != null)
					{
						if (to.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
						{
							return to.GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperations.ITextOperation;
						}
					}
			}
			return null;
		}

		private static int currentGridRow = 0;
		private static int currentGridColumn = 0;
		private static bool _isBusyProcessing = false;
		public static bool ProcessInputTextToGrid(string textToSearchIn, IList<ITextOperation> itemList, System.Windows.Forms.DataGridView dataGridview, TextFeedbackEventHandler textFeedbackEvent)
		{
			if (textFeedbackEvent == null) textFeedbackEvent = delegate { };

			if (_isBusyProcessing)
			{
				UserMessages.ShowWarningMessage("Already busy processing, please wait for it to finish.");
				return false;
			}
			else
			{
				_isBusyProcessing = true;

				try
				{
					if (itemList.Count == 0)
						UserMessages.ShowWarningMessage("There is no processing tree to process");

					var tmpRowHeaderSizeMode = dataGridview.RowHeadersWidthSizeMode;
					var tmpAutoSizeColumnsMode = dataGridview.AutoSizeColumnsMode;
					dataGridview.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader;
					dataGridview.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
					dataGridview.SuspendLayout();
					new Action(delegate
					{
						dataGridview.Rows.Clear();
						//dataGrid1.Rows.Add();
						currentGridRow = 0;
						dataGridview.Columns.Clear();
						currentGridColumn = 0;

						string tempText = textToSearchIn;
						foreach (ITextOperation ddo in itemList)
						{
							//TreeViewItem tvi = mainTreeview.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem;
							//ProcessTreeViewItem(dataGridview, tvi, ref tempText, IntegerRange.Full);
							ProcessTreeViewItem(dataGridview, ddo, ref tempText, IntegerRange.Full);
						}
					}).PerformTimedActionAndUpdateStatus(
						textFeedbackEvent,
						"Processing text, please wait...",
						"Processing completed with duration of {0} seconds. Total row count = {1}",
						7000,
						new Func<string>(() => (dataGridview.RowCount - 1).ToString()));
					dataGridview.ResumeLayout();
					dataGridview.RowHeadersWidthSizeMode = tmpRowHeaderSizeMode;

					int[] colwidths = new int[dataGridview.ColumnCount];
					for (int i = 0; i < colwidths.Length; i++)
						colwidths[i] = dataGridview.Columns[i].Width;
					dataGridview.AutoSizeColumnsMode = tmpAutoSizeColumnsMode;
					for (int i = 0; i < colwidths.Length; i++)
						dataGridview.Columns[i].Width = colwidths[i];
					return true;
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Error processing steps: " + exc.Message);
					return false;
				}
				finally
				{
					_isBusyProcessing = false;
				}
			}
		}

		private static void ProcessTreeViewItem(System.Windows.Forms.DataGridView dataGridview, ITextOperation item, ref string usedText, IntegerRange textRangeToUse)
		{
			//if (tvi == null) return;
			//ITextOperation textop = tvi.Header as ITextOperation;
			//if (textop == null) return;

			if (item is TextOperations.ITextOperation)//<string>)
			{
				TextOperations.ITextOperation textOperation = item as TextOperations.ITextOperation;
				TextOperations.TextOperationWithDataGridView toWithDg = textOperation as TextOperations.TextOperationWithDataGridView;

				if (toWithDg != null)
					toWithDg.SetDataGridAndProperties(ref dataGridview, currentGridColumn, currentGridRow);

				IntegerRange[] rangesToUse = textOperation.ProcessText(ref usedText, textRangeToUse);

				if (toWithDg != null)
				{
					currentGridColumn = toWithDg.GetNewColumnIndex();
					currentGridRow = toWithDg.GetNewRowIndex();
				}

				foreach (IntegerRange ir in rangesToUse)
					foreach (ITextOperation ddo1 in item.Children)
					{
						//TreeViewItem tvi1 = tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem;
						//ProcessTreeViewItem(dataGridview, tvi1, ref usedText, ir);
						ProcessTreeViewItem(dataGridview, ddo1, ref usedText, ir);
					}
			}
		}
	}
#endregion Other static functions

	public class IntegerRange
	{
		public int? Start;
		public int? Length;
		private IntegerRange(int? Start, int? Length)//Only used for static Empty and Full
		{
			this.Start = Start;
			this.Length = Length;
		}
		public IntegerRange(uint Start, uint Length)
		{
			this.Start = (int)Start;
			this.Length = (int)Length;
		}

		public bool IsFull() { return Start == 0 && Length == null; }
		public bool IsEmpty() { return Start == null && Length == null; }
		public static IntegerRange Empty { get { return new IntegerRange(null, null); } }
		public static IntegerRange Full { get { return new IntegerRange(0, null); } }
	}

	public static class TextOperations
	{
		public interface ITextOperation
		{
			string DisplayName { get; }
			string Tooltip { get; }
			Control[] InputControls { get; }
			bool HasInputControls { get; }
			/// <summary>
			/// The actual processing of the text.
			/// </summary>
			/// <param name="UsedText">The reference to the string object to use.</param>
			/// <param name="textRange">The range in this string to use.</param>
			/// <param name="AdditionalObject">An additional object used in the routine.</param>
			/// <returns>Returns the resulting ranges of the text as a result of its processing.</returns>
			IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			ITextOperation Clone();
			IList<ITextOperation> Children { get; set; }
			bool IsExpanded { get; set; }
		}

		public abstract class TextOperation : ITextOperation
		{
			public const RegexOptions RegexOptionsToUse = RegexOptions.Singleline;//.Multiline;
			public virtual string DisplayName { get { return this.GetType().Name.InsertSpacesBeforeCamelCase(); } }
			public abstract string Tooltip { get; }
			public virtual Control[] InputControls { get { return new Control[0]; } }
			public bool HasInputControls { get { return InputControls != null && InputControls.Length > 0; } }
			public abstract IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			public virtual ITextOperation Clone()
			{
				TextOperation to = this.GetType().GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperation;
				for (int i = 0; i < this.InputControls.Length; i++)
					this.InputControls[i].CopyControlValue(ref to.InputControls[i]);
				return to;
			}

			private IList<ITextOperation> children;
			public IList<ITextOperation> Children
			{
				get { if (children == null) children = new ObservableCollection<ITextOperation>(); return children; }
				set { children = value; }
			}

			public TextOperation()
			{
				IsExpanded = true;
				//if (HasInputControls)
				//    foreach (var ic in InputControls)
				//    {
				//        ic.MouseDown += (s, e) => { e.Handled = true; };
				//        ic.MouseMove += (s, e) => { e.Handled = true; };
				//        ic.PreviewMouseDown += (s, e) => { e.Handled = true; };
				//        ic.PreviewMouseMove += (s, e) => { e.Handled = true; };
				//    }
			}

			public bool IsExpanded { get; set; }
		}

		public abstract class TextOperationWithDataGridView : TextOperation
		{
			public override abstract IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);

			public DataGridView dataGridView { protected get; set; }
			protected int CurrentGridColumn { get; set; }
			protected int CurrentGridRow { get; set; }
			public void SetDataGridAndProperties(ref DataGridView dataGridView, int CurrentGridColumn, int CurrentGridRow)
			{
				this.dataGridView = dataGridView;
				this.CurrentGridColumn = CurrentGridColumn;
				this.CurrentGridRow = CurrentGridRow;
			}
			public int GetNewColumnIndex() { return CurrentGridColumn; }
			public int GetNewRowIndex() { return CurrentGridRow; }
		}

		public class ForEachLine : TextOperation
		{
			public override string Tooltip { get { return "Breaks the 'current' text (of the input text) up into lines."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> tmpRanges = new List<IntegerRange>();

				int nextStartPos = 0;
				for (int chr = 0; chr < UsedText.Length; chr++)
				{
					if (chr < nextStartPos)
						continue;
					//if (chr > 0 && chr < usedText.Length - 1 && (usedText.Substring(chr, 2) == Environment.NewLine || chr == usedText.Length - 2))
					if (chr > 0 && (UsedText[chr] == '\n' || chr == UsedText.Length - 1))
					{
						tmpRanges.Add(new IntegerRange((uint)nextStartPos, (uint)(chr - nextStartPos)));
						IntegerRange lineRange = new IntegerRange((uint)nextStartPos, (uint)(chr - nextStartPos));

						nextStartPos = chr + 1;//2;
					}
				}

				return tmpRanges.ToArray();
			}
		}

		public class ForEachRegex : TextOperation
		{
			public override string Tooltip { get { return "Breaks the 'current' text up into matches of this Regular Expression."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 200 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				try
				{
					var matches = Regex.Matches(
							textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
							: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
							RegularExpression.Text,
							RegexOptionsToUse);
					var ranges = new IntegerRange[matches.Count];
					for (int i = 0; i < matches.Count; i++)
						ranges[i] = new IntegerRange((uint)(textRange.Start + matches[i].Index),
							(uint)(matches[i].Length));
					return ranges;
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Cannot use regular expression: " + exc.Message);
					return new IntegerRange[0];
				}
				//if (Regex.IsMatch(
				//    textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
				//        : UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
				//    RegularExpression.Text, RegexOptionsToUse))
				//    return new IntegerRange[] { textRange };
				//else return new IntegerRange[0];
			}
		}

		public class IfItContains : TextOperation
		{
			public override string Tooltip { get { return "If the 'current' text contains this text."; } }
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SearchForText }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				if (
					(textRange.IsFull() ? UsedText
						: textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value))

					.Contains(SearchForText.Text))
					return new IntegerRange[] { textRange };
				else return new IntegerRange[0];
			}
		}

		public class ExtractTextRange : TextOperation
		{
			public override string Tooltip { get { return "Extract a range, if -1 is specified for Length it will extract up to the end."; } }
			private NumericUpDown StartPosition = new NumericUpDown() { Name = "StartPosition", Width = 50, MinValue = 0 };
			private NumericUpDown Length = new NumericUpDown() { Name = "Length", Width = 50, MinValue = 0 };
			public override Control[] InputControls { get { return new Control[] { StartPosition, Length }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				return new IntegerRange[]
				{
					//textRange.Length not used here, so if Length.Value is larger than
					//textRange.Length, it will work but is actually wrong..?
					new IntegerRange(
						(uint)(textRange.Start + StartPosition.Value),
						(uint)(Length.Value == -1
						? (int)(textRange.Start.Value + textRange.Length.Value - textRange.Start - StartPosition.Value)
						: Length.Value))
				};
			}
		}

		public class ExtractRegex : TextOperation
		{
			public override string Tooltip { get { return "Extract the first match of a Regular Expression in the 'current' text."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 200 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				try
				{

					var match = Regex.Match(
						textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
						RegularExpression.Text,
						RegexOptionsToUse);
					if (match.Success)
						return new IntegerRange[]
						{
							new IntegerRange((uint)(textRange.Start + match.Index),
							(uint)(match.Length))
						};
					else
						return new IntegerRange[0];
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Cannot use regular expression: " + exc.Message);
					return new IntegerRange[0];
				}
			}
		}

		public class SplitUsingString : TextOperation
		{
			public override string Tooltip { get { return "Split the 'current' text into two strings using this text."; } }
			private TextBox SplitTextOrChar = new TextBox() { Name = "SplitTextOrChar", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SplitTextOrChar }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				List<IntegerRange> rangeList = new List<IntegerRange>();
				int maxEndpoint = (int)(textRange.Start + textRange.Length);
				int startIndex = (int)textRange.Start;
				while (startIndex <= textRange.Start + textRange.Length)
				{
					int splitstringPos = UsedText.IndexOf(
						SplitTextOrChar.Text,
						startIndex,
						(int)(textRange.Start + textRange.Length - startIndex));
					if (splitstringPos == -1)
					{
						if ((int)(textRange.Start + textRange.Length - startIndex) > 0)
							rangeList.Add(new IntegerRange((uint)startIndex, (uint)(maxEndpoint - startIndex)));
						break;
					}
					else
					{
						rangeList.Add(new IntegerRange((uint)startIndex, (uint)(splitstringPos - startIndex)));
						startIndex = splitstringPos + 1;
					}
				}

				return rangeList.ToArray();
			}
		}

		public class SplitUsingCharacters : TextOperation
		{
			public override string Tooltip { get { return "Split the 'current' text into multiple strings using these characters."; } }
			private TextBox SplitCharacters = new TextBox() { Name = "SplitCharacters", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SplitCharacters }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				List<IntegerRange> rangeList = new List<IntegerRange>();
				int maxEndpoint = (int)(textRange.Start + textRange.Length);
				int startIndex = (int)textRange.Start;
				while (startIndex <= textRange.Start + textRange.Length)
				{
					int splitstringPos = UsedText.IndexOfAny(
						SplitCharacters.Text.ToCharArray(),
						startIndex,
						(int)(textRange.Start + textRange.Length - startIndex));
					if (splitstringPos == -1)
					{
						if ((int)(textRange.Start + textRange.Length - startIndex) > 0)
							rangeList.Add(new IntegerRange((uint)startIndex, (uint)(maxEndpoint - startIndex)));
						break;
					}
					else
					{
						rangeList.Add(new IntegerRange((uint)startIndex, (uint)(splitstringPos - startIndex)));
						startIndex = splitstringPos + 1;
					}
				}

				return rangeList.ToArray();
			}
		}

		public class Trim : TextOperation
		{
			public override string Tooltip { get { return "Trim of the spaces at begin&end of 'current' text."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int tmpStartPos = textRange.Start.Value;
				int tmpEndPos = textRange.Start.Value + textRange.Length.Value - 1;
				while (UsedText[tmpStartPos] == ' ' && tmpStartPos < textRange.Start.Value + textRange.Length)
					tmpStartPos++;
				while (UsedText[tmpEndPos] == ' ' && tmpEndPos >= textRange.Start.Value)
					tmpEndPos--;
				return new IntegerRange[] { new IntegerRange((uint)tmpStartPos, (uint)(tmpEndPos - tmpStartPos + 1)) };
			}
		}

		public class GetPreviousLine : TextOperation
		{
			public override string Tooltip { get { return "Get the previous line before the 'current' text."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int prevLineStartPos = -1;
				int prevLineEndPos = -1;
				for (int i = textRange.Start.Value - 1; i >= 0; i--)
				{
					if (UsedText[i] == '\n')
					{
						if (prevLineEndPos == -1)
							prevLineEndPos = i - 1;
						else
							prevLineStartPos = i + 1;
					}
					if (prevLineStartPos != -1 && prevLineEndPos != -1)
						return new IntegerRange[] { new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)) };
				}
				return new IntegerRange[0];
			}
		}

		public class GetPreviousNumberOfLines : TextOperation
		{
			public override string Tooltip { get { return "Get a number of lines before the 'current' text."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> ranges = new List<IntegerRange>();

				int startSeekPos = textRange.Start.Value - 1;
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int prevLineStartPos = -1;
					int prevLineEndPos = -1;
					for (int i = startSeekPos; i >= 0; i--)
					{
						if (UsedText[i] == '\n')
						{
							if (prevLineEndPos == -1)
								prevLineEndPos = i - 1;
							else
								prevLineStartPos = i + 1;
						}

						if (prevLineStartPos != -1 && prevLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							ranges.Add(new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)));
							startSeekPos = i;
							break;
						}
					}
				}
				return ranges.ToArray();
			}
		}

		public class IfPreviousNumberOfLinesContains : TextOperation
		{
			public override string Tooltip { get { return "Checks if the previous lines (before 'current' text) contains this text. Optionally a blank can be returned if no match."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			private CheckBox ReturnBlankIfNotFound = new CheckBox() { Name = "ReturnBlankIfNotFound", IsChecked = true };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines, SearchForText, ReturnBlankIfNotFound }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				int startSeekPos = textRange.Start.Value - 1;
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int prevLineStartPos = -1;
					int prevLineEndPos = -1;
					for (int i = startSeekPos; i >= 0; i--)
					{
						if (UsedText[i] == '\n')
						{
							if (prevLineEndPos == -1)
								prevLineEndPos = i - 1;
							else
								prevLineStartPos = i + 1;
						}

						if (prevLineStartPos != -1 && prevLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							if (UsedText.Substring(prevLineStartPos, prevLineEndPos - prevLineStartPos + 1).Contains(SearchForText.Text))
								return new IntegerRange[] { new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)) };
						}
					}
				}

				if (ReturnBlankIfNotFound.IsChecked == true)
					return new IntegerRange[] { IntegerRange.Empty };
				else
					return new IntegerRange[0];
				//if (
				//    (textRange.IsFull() ? UsedText
				//        : textRange.IsEmpty() ? ""
				//        : UsedText.Substring(textRange.Start.Value, textRange.Length.Value))

				//    .Contains(SearchForText.Text))
				//    return new IntegerRange[] { textRange };
				//else return new IntegerRange[0];
			}
		}

		public class GetNextLine : TextOperation
		{
			public override string Tooltip { get { return "Get the next line after the 'current' text."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int nextLineStartPos = -1;
				int nextLineEndPos = -1;
				for (int i = (int)(textRange.Start.Value + textRange.Length); i < UsedText.Length; i++)
				{
					if (UsedText[i] == '\n')
					{
						if (nextLineStartPos == -1)
							nextLineStartPos = i + 1;
						else
							nextLineEndPos = i - 1;
					}
					if (nextLineStartPos != -1 && nextLineEndPos != -1)
						return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
				}
				return new IntegerRange[0];
			}
		}

		public class GetNextNumberOfLines : TextOperation
		{
			public override string Tooltip { get { return "Get the next number of lines after the 'current' text."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> ranges = new List<IntegerRange>();

				int startSeekPos = (int)(textRange.Start + textRange.Length);
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int nextLineStartPos = -1;
					int nextLineEndPos = -1;
					for (int i = startSeekPos; i < UsedText.Length; i++)
					{
						if (UsedText[i] == '\n')
						{
							if (nextLineStartPos == -1)
								nextLineStartPos = i + 1;
							else
								nextLineEndPos = i - 1;
						}
						if (nextLineStartPos != -1 && nextLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							ranges.Add(new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)));
							startSeekPos = i;
							break;
						}
					}
				}
				return ranges.ToArray();
			}
		}

		public class IfNextNumberOfLinesContains : TextOperation
		{
			public override string Tooltip { get { return "Checks if the next number of lines (after the 'current' text) contains this text. Optionally a blank can be returned if not match."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			private CheckBox ReturnBlankIfNotFound = new CheckBox() { Name = "ReturnBlankIfNotFound", IsChecked = true };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines, SearchForText, ReturnBlankIfNotFound }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				int startSeekPos = (int)(textRange.Start + textRange.Length);
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int nextLineStartPos = -1;
					int nextLineEndPos = -1;
					for (int i = startSeekPos; i < UsedText.Length; i++)
					{
						if (UsedText[i] == '\n')
						{
							if (nextLineStartPos == -1)
								nextLineStartPos = i + 1;
							else
								nextLineEndPos = i - 1;
						}
						if (nextLineStartPos != -1 && nextLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							if (UsedText.Substring(nextLineStartPos, nextLineEndPos - nextLineStartPos + 1).Contains(SearchForText.Text))
								return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							startSeekPos = i;
							break;
						}
					}
				}

				if (ReturnBlankIfNotFound.IsChecked == true)
					return new IntegerRange[] { new IntegerRange(0, 0) };
				else
					return new IntegerRange[0];
			}
		}

		public class MatchesRegularExpression : TextOperation
		{
			public override string Tooltip { get { return "Checks if the 'current' text matches a Regular Expression."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (Regex.IsMatch(
					textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
					RegularExpression.Text,
					RegexOptionsToUse))
					return new IntegerRange[] { textRange };
				else return new IntegerRange[0];
			}
		}

		public class WriteCompleteCell : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text in the 'current' cell of the Grid and then moves to the next cell."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value =
					textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0);
				CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class WriteSameCell : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text in the 'current' cell in the Grid and stays in this cell."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value +=
					textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0);
				//CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class WriteSameCellWithPrefix : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text (with specified prefix) in the 'current' cell in the Grid and stays in this cell."; } }
			private TextBox PrefixText = new TextBox() { Name = "PrefixText", MinWidth = 30 };
			public override Control[] InputControls { get { return new Control[] { PrefixText }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value +=
					(PrefixText.Text ?? "") +
					(textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0));
				//CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class GotoNextRow : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Moves to the next row in the Grid."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				int newRowIndex = dataGridView.Rows.Add();
				dataGridView.Rows[newRowIndex].HeaderCell.Value = (++CurrentGridRow + 1).ToString();
				//CurrentGridRow++;
				CurrentGridColumn = 0;
				return new IntegerRange[] { textRange };
			}
		}

		public class IfContainsThenExtractLength : TextOperation
		{
			public override string Tooltip { get { return "If the 'current' text contains this search text, then extract from where the match starts for the length specified (use -1 to extract up to end of 'current' text)."; } }
			private TextBox TextToSeek = new TextBox() { Name = "TextToSeek", Width = 50 };
			private NumericUpDown LengthToExtract = new NumericUpDown() { Name = "LengthToExtract", Width = 50, MinValue = 0 };
			public override Control[] InputControls { get { return new Control[] { TextToSeek, LengthToExtract }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				int startIndexOfTextToSeek = (textRange.IsFull() ? UsedText
						: textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value)).IndexOf(TextToSeek.Text);
				if (startIndexOfTextToSeek != -1)
					return new IntegerRange[]
					{
						//textRange.Length not used here, so if Length.Value is larger than
					//textRange.Length, it will work but is actually wrong..?
						new IntegerRange(
							(uint)(textRange.Start + startIndexOfTextToSeek),
							(uint)(LengthToExtract.Value == -1
							? (int)(textRange.Start.Value + textRange.Length.Value - textRange.Start - startIndexOfTextToSeek)
							: LengthToExtract.Value))
					};
				else
					return new IntegerRange[0];
			}
		}
	}
}