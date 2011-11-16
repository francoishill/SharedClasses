using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICommandWithHandler = TempNewCommandsManagerClass.ICommandWithHandler;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for InlineCommandsUserControlWPF.xaml
	/// </summary>
	public partial class InlineCommandsUserControlWPF : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;

		public InlineCommandsUserControlWPF()
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
					//ThreadingInterop.UpdateGuiFromThread(this, () =>
					//{
						textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
							+ evtargs.FeedbackText;
					//}
					//);
				};
				textFeedbackEventInitialized = true;
			}

			treeView_CommandList.Items.Clear();
			foreach (ICommandWithHandler comm in TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces)
				treeView_CommandList.Items.Add(comm);
		}

		private void textBox_CommandLine_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
				{
					ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
					TempNewCommandsManagerClass.PerformCommandFromString(
						comm,
						textFeedbackEvent,
						textBox_CommandLine.Text);
				}
			}
		}

		private void treeView_CommandList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue != null && e.NewValue is ICommandWithHandler)
			{
				ICommandWithHandler comm = e.NewValue as ICommandWithHandler;
				label_ArgumentsExample.Content = label_ArgumentsExample.ToolTip = comm.ArgumentsExample.Replace("\n", "  ");
			}
		}
	}
}
