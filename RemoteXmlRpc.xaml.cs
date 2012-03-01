using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using Microsoft.Win32;
using SharedClasses;

namespace TestingSharedClasses
{
	/// <summary>
	/// Interaction logic for RemoteXmlRpc.xaml
	/// </summary>
	public partial class RemoteXmlRpc : Window
	{
		ObservableCollection<AssemblyClass> assemblies = null;

		public RemoteXmlRpc()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);

			assemblies = new ObservableCollection<AssemblyClass>(new ClassWithAllAssemblies().Assemblies);
			treeViewAssemblies.ItemsSource = assemblies;
			treeViewAssemblies.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeviewItemExpanded));
		}

		private ObservableCollection<MethodClass> ImportFromFile(string filepath = null)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select a file to load from";
			ofd.Filter = "Xml files (*.xml)|*.xml";
			if (filepath != null || ofd.ShowDialog().Value)
			{
				if (filepath != null)
					ofd.FileName = filepath;

				var tmpMethodToRunList = new ObservableCollection<MethodClass>();

				string tmpUsedClass_AssemblyQualifiedName = null;
				string tmpMethodName = null;

				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(ofd.FileName);
				XmlNodeList methodsToRun = xmlDoc.SelectNodes("ListOfMethodToRun/MethodToRun");
				foreach (XmlNode methodNode in methodsToRun)
				{
					tmpUsedClass_AssemblyQualifiedName = methodNode.Attributes["UsedClass_AssemblyQualifiedName"].Value;
					tmpMethodName = methodNode.Attributes["MethodName"].Value;
					if (string.IsNullOrWhiteSpace(tmpUsedClass_AssemblyQualifiedName))
						UserMessages.ShowWarningMessage("Cannot read MethodToRun attribute 'UsedClass_AssemblyQualifiedName': " + methodNode.OuterXml);
					else if (string.IsNullOrWhiteSpace(tmpMethodName))
						UserMessages.ShowWarningMessage("Cannot read MethodToRun attribute 'MethodName': " + methodNode.OuterXml);
					else
					{
						Dictionary<string, ParameterNameAndType> tmpParams = new Dictionary<string, ParameterNameAndType>();

						bool errorOccurred = false;
						XmlNodeList parameters = methodNode.SelectNodes("Parameters/Parameter");
						foreach (XmlNode paramNode in parameters)
						{
							string tmpParamName = paramNode.Attributes["Name"].Value;
							string tmpParamAssemblyQualifiedName = paramNode.Attributes["AssemblyQualifiedName"].Value;
							object tmpParamValue = paramNode.InnerText;
							if (string.IsNullOrWhiteSpace(tmpParamName))
								UserMessages.ShowWarningMessage("Cannot read Parameter attribute 'Name': " + paramNode.OuterXml);
							else if (string.IsNullOrWhiteSpace(tmpParamAssemblyQualifiedName))
								UserMessages.ShowWarningMessage("Cannot read Parameter attribute 'AssemblyQualifiedName': " + paramNode.OuterXml);
							else if (tmpParamValue == null)//It might? be empty...?
								UserMessages.ShowWarningMessage("Cannot read Parameter value: " + paramNode.OuterXml);
							else//Successful
							{
								var tmpparamtype = DynamicCodeInvoking.GetTypeFromSimpleString(tmpParamAssemblyQualifiedName.Substring(0, tmpParamAssemblyQualifiedName.IndexOf(',')));
								var tmpParam = new ParameterNameAndType(tmpParamName, tmpparamtype);
								tmpParamValue = Convert.ChangeType(tmpParamValue, tmpparamtype);
								tmpParam.OverrideValue(tmpParamValue);
								tmpParams.Add(tmpParamName, tmpParam);
								continue;
							}
							errorOccurred = true;//This will be skipped be the above "continue" if successful
						}

						if (!errorOccurred)
						{
							tmpMethodToRunList.Add(new MethodClass(
								tmpParams,
								tmpUsedClass_AssemblyQualifiedName,
								tmpMethodName));
						}
					}
				}
				return tmpMethodToRunList;
			}
			return null;
		}

		private void ExportToFile(IEnumerable<MethodClass> methodList, string filepath = null)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Title = "Select a file to save to";
			sfd.Filter = "Xml files (*.xml)|*.xml";
			if (filepath != null || sfd.ShowDialog().Value)
			{
				if (filepath != null)
					sfd.FileName = filepath;
				using (var xw = new XmlTextWriter(sfd.FileName, System.Text.Encoding.ASCII) { Formatting = Formatting.Indented })
				{
					xw.WriteStartElement("ListOfMethodToRun");
					foreach (MethodClass mc in methodList)
					{
						xw.WriteStartElement("MethodToRun");
						xw.WriteAttributeString("UsedClass_AssemblyQualifiedName", mc.ParentClass.ClassType.AssemblyQualifiedName);
						xw.WriteAttributeString("MethodName", mc.Methodinfo.Name);
						xw.WriteStartElement("Parameters");
						//var keys = mc.PropertyGridAdapter._dictionary.Keys.ToArray();
						foreach (var pi in mc.Parameters)
						{
							xw.WriteStartElement("Parameter");
							xw.WriteAttributeString("Name", pi.Name);
							xw.WriteAttributeString("AssemblyQualifiedName", pi.ParameterType.AssemblyQualifiedName);
							xw.WriteValue(mc.GetParameterValue(pi));
							xw.WriteEndElement();
						}
						xw.WriteEndElement();//Parameters
						xw.WriteEndElement();//MethodToRun
					}
					xw.WriteEndElement();//ListOfMethodToRun
				}
			}
		}

		const string appname = "RemoteXmlRpc";
		const string listsSubfolder = "SavedListsOfMethodsToRun";
		private ObservableCollection<MethodClass> LoadListFromName()
		{
			string tmpdir = SettingsInterop.LocalAppdataPath(appname) + "\\" + listsSubfolder;
			string[] tmpNamelist = Directory.GetFiles(tmpdir).Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray();
			if (tmpNamelist == null || tmpNamelist.Length == 0)
			{
				if (UserMessages.Confirm("There are no lists saved yet, import from custom file rather?"))
					ImportFromFile();
			}
			else
			{
				//string NameToUse = InputBoxWPF.Prompt("Please enter the desired name of this list to load", "List name to load");
				//string NameToUse = PickItemForm.PickItem<string>(tmpNamelist, "Please choose the list to load", null);
				string NameToUse = PickItemWPF.PickItem(typeof(string), tmpNamelist, "Please choose the list to load", null) as string;
				if (NameToUse != null)
				{
					if (string.IsNullOrWhiteSpace(NameToUse))
						UserMessages.ShowWarningMessage("Cannot use a blank string for a name");
					else
						return ImportFromFile(SettingsInterop.GetFullFilePathInLocalAppdata(NameToUse + ".mtrl", appname, listsSubfolder));
				}
			}
			return null;
		}

		private void SaveListWithName(IEnumerable<MethodClass> methodList)
		{
			string NameToUse = InputBoxWPF.Prompt("Please enter the desired name of this list to save to", "Name the list");
			if (NameToUse != null)
			{
				if (string.IsNullOrWhiteSpace(NameToUse))
					UserMessages.ShowWarningMessage("Cannot use a blank string for a name");
				else
				{
					string tmpfilepath = SettingsInterop.GetFullFilePathInLocalAppdata(NameToUse + ".mtrl", "RemoteXmlRpc", "SavedListsOfMethodsToRun");
					if (!File.Exists(tmpfilepath) || UserMessages.Confirm("The list name already exists, overwrite it?"))
						ExportToFile(methodList, tmpfilepath);
				}
			}
		}

		private void MenuitemExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown(0);
		}

		private void MethodBorder_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Border borderRightClicked = sender as Border;
			if (borderRightClicked == null)
				return;

			MethodClass methodClass = borderRightClicked.DataContext as MethodClass;
			if (methodClass == null)
				return;

			borderRightClicked.ContextMenu.DataContext = methodClass;
			borderRightClicked.ContextMenu.IsOpen = true;

			//System.Windows.Forms.MessageBox.Show(newMethodClass.ParentClass.GetType().ToString());
			//Continue here



			//DynamicCodeInvoking.RunSelectedFunction(
			//	newMethodClass.PropertyGridAdapter._dictionary,
			//	newMethodClass.ParentClass.ClassType.AssemblyQualifiedName,
			//	newMethodClass.Methodinfo.Name);
		}

		private void ListBoxMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			(sender as ListBox).SelectedItem = null;
		}

		private void MenuItemRunRemotely_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menuItem = sender as MenuItem;
			treeViewAssemblies.Focus();
			MethodClass methodClass = menuItem.DataContext as MethodClass;
			DynamicCodeInvoking.RunCodeReturnStruct result = methodClass.Run();
		}

		private void MethodBorder_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			//if (e.Key == System.Windows.Input.Key.Down
			//	&& Keyboard.Modifiers == ModifierKeys.Control)
			//{
			//	Border borderRightClicked = sender as Border;
			//	if (borderRightClicked == null)
			//		return;

			//	MethodClass newMethodClass = borderRightClicked.DataContext as MethodClass;
			//	if (newMethodClass == null)
			//		return;

			//	var enumerator = newMethodClass.ParentClass.Methods.GetEnumerator();
			//	while (enumerator.Current != newMethodClass && enumerator.MoveNext())
			//	{ }
			//	if (enumerator.Current == newMethodClass)
			//		if (enumerator.MoveNext())
			//		//Continue here
			//		{ }


			//	borderRightClicked.ContextMenu.DataContext = newMethodClass;
			//	borderRightClicked.ContextMenu.IsOpen = true;
			//}
		}

		private void treeViewAssemblies_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			MethodClass oldMethodClass = e.OldValue as MethodClass;
			if (oldMethodClass != null)
				oldMethodClass.IsSelected = false;

			MethodClass newMethodClass = e.NewValue as MethodClass;
			if (newMethodClass != null)
			{
				newMethodClass.IsSelected = true;
				TreeViewItem tvi = treeViewAssemblies.ContainerFromItem(newMethodClass);
				if (tvi != null)
					tvi.BringIntoView(new Rect(new Size(200, 200)));
			}
		}

		private void TreeviewItemExpanded(object sender, RoutedEventArgs e)
		{
			TreeViewItem item = e.OriginalSource as TreeViewItem;
			if (item != null)
				item.BringIntoView();
		}

		private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			Visibility HiddenVisibility = System.Windows.Visibility.Collapsed;

			bool typeMatch = false;
			bool methodMatches = false;

			foreach (AssemblyClass ass in assemblies)
			{
				typeMatch = false;
				foreach (ClassWithStaticMethods type in ass.Types)
				{
					methodMatches = false;
					foreach (MethodClass method in type.Methods)
					{
						if (method.Methodinfo.Name.IndexOf(textBoxFilter.Text, StringComparison.InvariantCultureIgnoreCase) != -1)
						{
							methodMatches = true;
							//method.Visibility = System.Windows.Visibility.Visible;
						}
						//else
						//	method.Visibility = HiddenVisibility;
					}

					if (methodMatches || type.ClassType.FullName.IndexOf(textBoxFilter.Text, StringComparison.InvariantCultureIgnoreCase) != -1)
					{
						typeMatch = true;
						type.Visibility = System.Windows.Visibility.Visible;
					}
					else
						type.Visibility = HiddenVisibility;
				}
				if (typeMatch || ass.ThisAssembly.FullName.IndexOf(textBoxFilter.Text, StringComparison.InvariantCultureIgnoreCase) != -1)
					ass.Visibility = System.Windows.Visibility.Visible;
				else
					ass.Visibility = HiddenVisibility;
			}

			treeViewAssemblies.UpdateLayout();
		}
	}

	//public class MethodToRun
	//{
	//	public Dictionary<string, ParameterNameAndType> Parameters { get; set; }
	//	public string UsedClass_AssemblyQualifiedName { get; set; }
	//	public string MethodName { get; set; }
	//	public MethodToRun(Dictionary<string, ParameterNameAndType> Parameters, string UsedClass_AssemblyQualifiedName, string MethodName)
	//	{
	//		this.Parameters = Parameters;
	//		this.UsedClass_AssemblyQualifiedName = UsedClass_AssemblyQualifiedName;
	//		this.MethodName = MethodName;
	//	}

	//	public string UsedClass_HumanName
	//	{
	//		get
	//		{
	//			if (string.IsNullOrEmpty(UsedClass_AssemblyQualifiedName))
	//				return "";
	//			else
	//			{
	//				string partbeforeFirstComma = UsedClass_AssemblyQualifiedName.Substring(0, UsedClass_AssemblyQualifiedName.IndexOf(','));
	//				return partbeforeFirstComma.Substring(partbeforeFirstComma.LastIndexOf('.') + 1);
	//			}
	//		}
	//	}
	//}

	#region Extension methods
	public static class ExtensionMethods
	{
		#region Recursive Checking For TreeViewItem
		/// <summary>
		/// Recursively checks for a treeview Item
		/// </summary>
		public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
		{
			TreeViewItem containerThatMightContainItem = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(item);
			if (containerThatMightContainItem != null)
				return containerThatMightContainItem;
			else
				return ContainerFromItem(treeView.ItemContainerGenerator, treeView.Items, item);
		}

		private static TreeViewItem ContainerFromItem(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, object item)
		{
			foreach (object curChildItem in itemCollection)
			{
				TreeViewItem parentContainer = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);
				if (parentContainer == null)
					continue;
				TreeViewItem containerThatMightContainItem = (TreeViewItem)parentContainer.ItemContainerGenerator.ContainerFromItem(item);
				if (containerThatMightContainItem != null)
					return containerThatMightContainItem;
				TreeViewItem recursionResult = ContainerFromItem(parentContainer.ItemContainerGenerator, parentContainer.Items, item);
				if (recursionResult != null)
					return recursionResult;
			}
			return null;
		}
		#endregion Recursive Checking For TreeViewItem
	}
	#endregion Extension methods

	#region Converters
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return !(value is bool) || ((bool)value) == false ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class MethodInfoToParameterListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			MethodInfo mi = value as MethodInfo;
			if (mi == null)
				return "";
			string tmpStr = "";
			foreach (ParameterInfo pi in mi.GetParameters())
				tmpStr += (tmpStr.Length > 0 ? ", " : "") + pi.Name;//string.Format("{0} ({1})", pi.Name, pi.ParameterType.ToString());
			return tmpStr;//string.Join(",", mi.GetParameters().ToList());
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class TooltipMethodInfoToParameterListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			MethodInfo mi = value as MethodInfo;
			if (mi == null)
				return "";
			string tmpStr = "";
			foreach (ParameterInfo pi in mi.GetParameters())
				tmpStr += (tmpStr.Length > 0 ? Environment.NewLine : "") + string.Format("{0} is {1}", pi.Name, pi.ParameterType.ToString());
			return tmpStr;//string.Join(",", mi.GetParameters().ToList());
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	#endregion Converters
}
