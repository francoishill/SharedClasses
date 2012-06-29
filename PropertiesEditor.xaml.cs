using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Data;
using UriProtocol = SharedClasses.GlobalSettings.VisualStudioInteropSettings.UriProtocol;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for PropertiesEditor.xaml
	/// </summary>
	public partial class PropertiesEditor : Window
	{
		//const string String_InlineTemplateKey = "String_InlineTemplate";
		//const string String_ExtendedTemplateKey = "String_ExtendedTemplate";

		//const string StringList_InlineTemplateKey = "StringList_InlineTemplateKey";
		//const string StringList_ExtendedTemplateKey = "StringList_ExtendedTemplateKey";

		public PropertiesEditor(List<object> objectsToView)
		{
			InitializeComponent();

			propertyGrid2.EditorDefinitions.Add(new Xceed.Wpf.Toolkit.PropertyGrid.EditorDefinition() { EditorTemplate = (DataTemplate)this.Resources["StringListEditor"] });

			//propertyGrid2.

			//List<string> ResourceKeys = new List<string>();
			//foreach (string s in this.Resources.Keys)
			//	ResourceKeys.Add(s);

			//if (!ResourceKeys.Contains(String_InlineTemplateKey))
			//	UserMessages.ShowWarningMessage("Could not find inline template key = " + String_InlineTemplateKey);
			//if (!ResourceKeys.Contains(String_ExtendedTemplateKey))
			//	UserMessages.ShowWarningMessage("Could not find inline template key = " + String_ExtendedTemplateKey);


			//if (!ResourceKeys.Contains(StringList_InlineTemplateKey))
			//	UserMessages.ShowWarningMessage("Could not find inline template key = " + StringList_InlineTemplateKey);
			//if (!ResourceKeys.Contains(StringList_ExtendedTemplateKey))
			//	UserMessages.ShowWarningMessage("Could not find inline template key = " + StringList_ExtendedTemplateKey);

			//System.Windows.Forms.MessageBox.Show(Resources[StringList_ExtendedTemplateKey].GetType().Name);
			//propertyGrid1.Editors.Add(new TypeEditor(typeof(string[]), Resources[StringList_InlineTemplateKey], Resources[StringList_ExtendedTemplateKey]));


			//foreach (Type type in new Type[] { typeof(String), typeof(Int16?), typeof(Int32?), typeof(Int64?), typeof(Double?), typeof(Boolean?), typeof(UriProtocol?), typeof(Int16), typeof(Int32), typeof(Int64), typeof(Double), typeof(Boolean), typeof(UriProtocol) })
			//	propertyGrid1.Editors.Add(new TypeEditor(type, Resources[String_InlineTemplateKey], Resources[String_ExtendedTemplateKey]));

			////tabControl1.ItemsSource = new ObservableCollection<object>() { selectedObject };
			//propertyGrid1.SelectedObject = null;

			listBox1.Items.Clear();
			foreach (object obj in objectsToView)
				listBox1.Items.Add(obj);
			//propertyGrid1.SelectedObject = selectedObject;
		}

		private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;
			//propertyGrid1.SelectedObject = e.AddedItems[0];
			propertyGrid2.SelectedObject = e.AddedItems[0];
		}
	}

	//public class MyCollectionEditor : CollectionEditor
	//{
	//	public delegate void MyFormClosedEventHandler(object sender, FormClosedEventArgs e);

	//	public static event MyFormClosedEventHandler MyFormClosed;

	//	public MyCollectionEditor(Type type) : base(type) { }
	//	protected override CollectionForm CreateCollectionForm()
	//	{
	//		CollectionForm collectionForm = base.CreateCollectionForm();
	//		collectionForm.FormClosed += new FormClosedEventHandler(collection_FormClosed);
	//		return collectionForm;
	//	}

	//	void collection_FormClosed(object sender, FormClosedEventArgs e)
	//	{
	//		if (MyFormClosed != null)
	//		{
	//			MyFormClosed(this, e);
	//		}
	//	}
	//}

	public class PipesToNewlinesConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
				return null;
			return value.ToString().Replace("|", Environment.NewLine);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
				return null;
			return value.ToString().Replace(Environment.NewLine, "|");
		}
	}

	//[ContentProperty("Items")]
	//public class CollectionOfTExtension : CollectionOfTExtensionBase<IList>
	//{
	//	protected override Type GetCollectionType(Type typeArgument)
	//	{
	//		return typeof(Collection<>).MakeGenericType(typeArgument);
	//	}
	//}

	//[ContentProperty("Items")]
	//public class ListOfTExtension : CollectionOfTExtensionBase<IList>
	//{
	//	protected override Type GetCollectionType(Type typeArgument)
	//	{
	//		return typeof(List<>).MakeGenericType(typeArgument);
	//	}
	//}

	////
	//// MarkupExtension that creates an ObservableCollection<T>
	////
	//[ContentProperty("Items")]
	//public class ObservableCollectionOfTExtension : CollectionOfTExtensionBase<IList>
	//{
	//	protected override Type GetCollectionType(Type typeArgument)
	//	{
	//		return typeof(ObservableCollection<>).MakeGenericType(typeArgument);
	//	}
	//}

	////
	//// MarkupExtension that creates an Dictionary<Object,T>
	//// (Items cannot be the [ContentProperty]).
	////
	//public class DictionaryOfTExtension : CollectionOfTExtensionBase<IDictionary>
	//{
	//	protected override Type GetCollectionType(Type typeArgument)
	//	{
	//		return typeof(Dictionary<,>).MakeGenericType(typeof(Object), typeArgument);
	//	}

	//	protected virtual void CopyItems(IDictionary oldItems)
	//	{
	//		IDictionary oldItemsAsDictionary = oldItems as IDictionary;
	//		IDictionary newItemsAsDictionary = Items as IDictionary;

	//		foreach (DictionaryEntry entry in oldItemsAsDictionary)
	//		{
	//			newItemsAsDictionary[entry.Key] = oldItemsAsDictionary[entry.Key];
	//		}
	//	}
	//}

	//public abstract class CollectionOfTExtensionBase<CollectionType> : MarkupExtension
	//	where CollectionType : class
	//{
	//	public CollectionOfTExtensionBase(Type typeArgument)
	//	{
	//		_typeArgument = typeArgument;
	//	}

	//	// Default the collection to typeof(Object)
	//	public CollectionOfTExtensionBase()
	//		: this(typeof(Object))
	//	{
	//	}

	//	// Items is the actual collection we'll return from ProvideValue.
	//	protected CollectionType _items;
	//	public CollectionType Items
	//	{
	//		get
	//		{
	//			if (_items == null)
	//			{
	//				Type collectionType = GetCollectionType(TypeArgument);
	//				_items = Activator.CreateInstance(collectionType) as CollectionType;
	//			}
	//			return _items;
	//		}
	//	}

	//	// TypeArgument is the "T" in e.g. Collection<T>
	//	private Type _typeArgument;
	//	public Type TypeArgument
	//	{
	//		get { return _typeArgument; }
	//		set
	//		{
	//			_typeArgument = value;

	//			// If the TypeArgument doesn'buildTask get set until after
	//			// items have been added, we need to re-create items
	//			// to be the right type.
	//			if (_items != null)
	//			{
	//				object oldItems = _items;
	//				_items = null;
	//				CopyItems(oldItems);
	//			}
	//		}
	//	}

	//	// Default implementation of CopyItems that works for Collection/List
	//	// (but not Dictionary).
	//	protected virtual void CopyItems(object oldItems)
	//	{
	//		IList oldItemsAsList = oldItems as IList;
	//		IList newItemsAsList = Items as IList;

	//		for (int i = 0; i < oldItemsAsList.Count; i++)
	//		{
	//			newItemsAsList.Add(oldItemsAsList[i]);
	//		}
	//	}

	//	// Get the generic type, e.g. typeof(Collection<>), aka Collection`1.
	//	protected abstract Type GetCollectionType(Type typeArgument);


	//	// Provide the collection instance.
	//	public override object ProvideValue(IServiceProvider serviceProvider)
	//	{
	//		return _items;
	//	}
	//}

	//public class FontStyleConverter : IValueConverter
	//{
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		FontStyle fs = (FontStyle)value;
	//		return fs == FontStyles.Italic;
	//	}

	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		if (value != null)
	//		{
	//			bool isSet = (bool)value;

	//			if (isSet)
	//			{
	//				return FontStyles.Italic;
	//			}
	//		}

	//		return FontStyles.Normal;
	//	}
	//}

	//public class FontWeightConverter : IValueConverter
	//{
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		var fs = (FontWeight)value;
	//		return fs == FontWeights.LargerSize;
	//	}

	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		if (value != null)
	//		{
	//			bool isSet = (bool)value;

	//			if (isSet)
	//			{
	//				return FontWeights.LargerSize;
	//			}
	//		}

	//		return FontWeights.Normal;
	//	}
	//}

	//public class FontList : ObservableCollection<FontFamily>
	//{
	//	public FontList()
	//	{
	//		foreach (var ff in Fonts.SystemFontFamilies)
	//		{
	//			Add(ff);
	//		}
	//	}
	//}

	//public class FontSizeList : ObservableCollection<double>
	//{
	//	public FontSizeList()
	//	{
	//		Add(8);
	//		Add(9);
	//		Add(10);
	//		Add(11);
	//		Add(12);
	//		Add(14);
	//		Add(16);
	//		Add(18);
	//		Add(20);
	//	}
	//}
}
