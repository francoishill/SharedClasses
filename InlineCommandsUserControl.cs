using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICommandWithHandler = InlineCommandToolkit.InlineCommands.ICommandWithHandler;
using InlineCommandToolkit;

namespace SharedClasses
{
	public partial class InlineCommandsUserControl : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;

		public InlineCommandsUserControl()
		{
			InitializeComponent();
		}

		private bool textFeedbackEventInitialized = false;
		public void InitializeTreeViewNodes()
		{
			if (!textFeedbackEventInitialized)
			{
				textFeedbackEvent += (snder, evtargs) =>
				{
					ThreadingInterop.UpdateGuiFromThread(this, () =>
					{
						textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
							+ evtargs.FeedbackText;
					}
					);
				};
				textFeedbackEventInitialized = true;
			}

			StylingInterop.SetTreeviewVistaStyle(treeView_CommandList);
			treeView_CommandList.Nodes.Clear();
			foreach (ICommandWithHandler comm in InlineCommandToolkit.CommandsManagerClass.ListOfInitializedCommandInterfaces)
				treeView_CommandList.Nodes.Add(new TreeNode()
				{
					Name = comm.CommandName,
					Text = comm.DisplayName,
					Tag = comm
				});
		}

		private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				e.Handled = true;
				if (treeView_CommandList.SelectedNode != null && treeView_CommandList.SelectedNode.Tag is ICommandWithHandler)
				{
					ICommandWithHandler comm = treeView_CommandList.SelectedNode.Tag as ICommandWithHandler;
					InlineCommandToolkit.CommandsManagerClass.PerformCommandFromString(
						comm,
						textFeedbackEvent,
						null,
						richTextBox_CommandLine.Text);
				}
			}
		}

		private void treeView_CommandList_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node != null && e.Node.Tag is ICommandWithHandler)
			{
				ICommandWithHandler comm = e.Node.Tag as ICommandWithHandler;
				label_ArgumentsExample.Text = comm.ArgumentsExample.Replace("\n", "  ");
				toolTip_EntireUserControl.SetToolTip(label_ArgumentsExample, label_ArgumentsExample.Text);
			}
		}
	}
}
