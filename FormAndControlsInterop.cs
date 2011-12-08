using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedClasses
{
	public class FormAndControlsInterop
	{
		/// <summary>
		/// Gets the values of all the controls (in their current state)
		/// </summary>
		/// <param name="controls">The control list to get values from</param>
		/// <param name="HashTable">This parameter must just be a new blank HashTable</param>
		/// <returns>The HashTable with Original Control Values (Name and value)</returns>
		public static Dictionary<Control, object> GetHashTableWithOriginalControlValues(Control.ControlCollection controls, ref Dictionary<Control, object> HashTable)
		{
			Dictionary<Control, object> HashTableOut = HashTable;
			foreach (Control control in controls)
			{
				if (control is GroupBox || control is TabControl || control is TabPage) HashTableOut = GetHashTableWithOriginalControlValues(control.Controls, ref HashTableOut);
				else
				{
					if (control is TextBox)
						HashTableOut.Add(control, control.Text);
					else if (control is RadioButton)
						HashTableOut.Add(control, (control as RadioButton).Checked);
					else if (control is ComboBox)
						HashTableOut.Add(control, (control as ComboBox).SelectedItem);
					else if (control is CheckBox)
						HashTableOut.Add(control, (control as CheckBox).Checked);
					else if (control is ListBox)
					{ HashTableOut.Add(control, GetStringFromListbox(control as ListBox)); }
				}
			}
			return HashTableOut;
		}

		private static string GetStringFromListbox(ListBox listboxIn)
		{
			string tmpStr = "";
			foreach (string s in listboxIn.Items) tmpStr += (tmpStr.Length > 0 ? "," : "") + s;
			return tmpStr;
		}

		/// <summary>
		/// Uses a HashTable (Name, value) to check if the controls on a form has changed
		/// </summary>
		/// <param name="controls">The controls of the form</param>
		/// <param name="OriginalControlValuesIn">HashTable of original values, refer to using GetHashTableWithOriginalControlValues function when the form loads</param>
		/// <returns>True if controls changed, otherwise false</returns>
		public static Boolean CheckIfFormControlsChanged(Control.ControlCollection controls, Dictionary<Control, object> OriginalControlValuesIn)
		{
			foreach (Control control in controls)
			{
				if (control is GroupBox || control is TabControl || control is TabPage)
				{
					if (CheckIfFormControlsChanged(control.Controls, OriginalControlValuesIn))
						return true;
				}
				else
				{
					if (control is TextBox &&
							control.Text != OriginalControlValuesIn[control].ToString())
						return true;
					else if (control is RadioButton &&
							(control as RadioButton).Checked != Convert.ToBoolean(OriginalControlValuesIn[control]))
						return true;
					else if (control is ComboBox &&
							(control as ComboBox).SelectedItem != OriginalControlValuesIn[control])
						return true;
					else if (control is CheckBox &&
							(control as CheckBox).Checked != Convert.ToBoolean(OriginalControlValuesIn[control]))
						return true;
					else if (control is ListBox)
						if
						(GetStringFromListbox(control as ListBox) != OriginalControlValuesIn[control].ToString())
							return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Mainly for use with custom classes with fields inside them
		/// </summary>
		/// <param name="obj">The object (custom class object) to get fields of</param>
		/// <param name="ownFunctionList">A custom list of functions for Types that are not default (string, int, bool are some default Types)</param>
		/// <param name="GetFieldsTrue_GetPropertiesFalse">Set this parameter to TRUE to use the Fields, otherwise it will use the properties</param>
		/// <returns>A hashTable of all the field names (as Keys) and their values as strings</returns>
		public static Hashtable GetHashTableOfObjectFields(object obj, List<OwnTypeFunctions> ownFunctionList = null, Boolean GetFieldsTrue_GetPropertiesFalse = true)//, Hashtable HashTableIn)
		{
			List<string> MessagesDisplayed = new List<string>();
			Hashtable tmpHashTable = new Hashtable();
			string tmpClassName = obj.GetType().ToString();
			//tmpClassName = tmpClassName.Split('.')[tmpClassName.Split('.').Length - 1];
			//tmpClassName = tmpClassName.Split('+')[tmpClassName.Split('+').Length - 1];
			string tmpstrforownTypes;
			tmpHashTable.Add("ClassName", tmpClassName);
			if (GetFieldsTrue_GetPropertiesFalse)
			{
				foreach (System.Reflection.FieldInfo info in obj.GetType().GetFields())
					if (info.IsPublic)
					{
						if (
								info.FieldType == typeof(string) ||
								info.FieldType == typeof(bool) ||
								info.FieldType == typeof(int))// ||
							//info.FieldType == typeof(Enum))
							tmpHashTable.Add(info.Name, info.GetValue(obj));
						else if (info.FieldType == typeof(List<string>))
						{
							string tmpStr = "";
							foreach (string s in (List<string>)info.GetValue(obj))
								tmpStr += (tmpStr.Length > 0 ? "|" : "") + s;
							tmpHashTable.Add(info.Name, tmpStr);
						}
						else if (GetStringFromOwnFunctionList(info.GetValue(obj), info.FieldType, ownFunctionList, out tmpstrforownTypes))
						{
							//MessageBox.Show(info.FieldType.FullName);
							tmpHashTable.Add(info.Name, tmpstrforownTypes);
						}
						//else if (info.FieldType == typeof(Microsoft.Office.Interop.Outlook.MAPIFolder))
						//{
						//    tmpHashTable.Add(info.Name, ((Microsoft.Office.Interop.Outlook.MAPIFolder)info.GetValue(obj)).EntryID + ";" + ((Microsoft.Office.Interop.Outlook.MAPIFolder)info.GetValue(obj)).StoreID);
						//}
						//else if (info.FieldType == typeof(NSISclass.Compressor))
						//    tmpHashTable.Add(info.Name, ((NSISclass.Compressor)info.GetValue(obj.Tag)).CompressionMode + "," + ((NSISclass.Compressor)info.GetValue(obj.Tag)).Final + "," + ((NSISclass.Compressor)info.GetValue(obj.Tag)).Solid);
						//else if (info.FieldType == typeof(NSISclass.LicensePageDetails))
						//    tmpHashTable.Add(info.Name, ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).ShowLicensePage + "," + ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).LicenseFilePath + "," + ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).acceptWith);
						else
						{
							string msg = "The following type cannot be converted to a string (no function yet for it): " + info.FieldType.ToString();
							if (!MessagesDisplayed.Contains(msg))
							{
								MessageBox.Show(msg, "Invalid type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								MessagesDisplayed.Add(msg);
							}
						}
					}
			}
			else
			{
				foreach (System.Reflection.PropertyInfo info in obj.GetType().GetProperties())
					if (
							info.PropertyType == typeof(string) ||
							info.PropertyType == typeof(bool) ||
							info.PropertyType == typeof(int))// ||
						//info.FieldType == typeof(Enum))
						tmpHashTable.Add(info.Name, info.GetValue(obj, null));
					else if (info.PropertyType == typeof(List<string>))
					{
						string tmpStr = "";
						foreach (string s in (List<string>)info.GetValue(obj, null))
							tmpStr += (tmpStr.Length > 0 ? "|" : "") + s;
						tmpHashTable.Add(info.Name, tmpStr);
					}
					else if (GetStringFromOwnFunctionList(info.GetValue(obj, null), info.PropertyType, ownFunctionList, out tmpstrforownTypes))
					{
						//MessageBox.Show(info.FieldType.FullName);
						tmpHashTable.Add(info.Name, tmpstrforownTypes);
					}
					//else if (info.FieldType == typeof(Microsoft.Office.Interop.Outlook.MAPIFolder))
					//{
					//    tmpHashTable.Add(info.Name, ((Microsoft.Office.Interop.Outlook.MAPIFolder)info.GetValue(obj)).EntryID + ";" + ((Microsoft.Office.Interop.Outlook.MAPIFolder)info.GetValue(obj)).StoreID);
					//}
					//else if (info.FieldType == typeof(NSISclass.Compressor))
					//    tmpHashTable.Add(info.Name, ((NSISclass.Compressor)info.GetValue(obj.Tag)).CompressionMode + "," + ((NSISclass.Compressor)info.GetValue(obj.Tag)).Final + "," + ((NSISclass.Compressor)info.GetValue(obj.Tag)).Solid);
					//else if (info.FieldType == typeof(NSISclass.LicensePageDetails))
					//    tmpHashTable.Add(info.Name, ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).ShowLicensePage + "," + ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).LicenseFilePath + "," + ((NSISclass.LicensePageDetails)info.GetValue(obj.Tag)).acceptWith);
					else
					{
						string msg = "The following type cannot be converted to a string (no function yet for it): " + info.PropertyType.ToString();
						if (!MessagesDisplayed.Contains(msg))
						{
							MessageBox.Show(msg, "Invalid type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							MessagesDisplayed.Add(msg);
						}
					}
			}
			return tmpHashTable;
		}

		/// <summary>
		/// Sets a custom classess' fields by using a hashtable (fieldname, fieldvalue usually string)
		/// </summary>
		/// <param name="OriginalObjectToSet">The original object to which the fields must be modified (used for obtaining the class)</param>
		/// <param name="hashTableWithFieldValues">Hashtable with all the field name/value pairs</param>
		/// <param name="ownFunctionList">A custom list of functions for Types that are not default (string, int, bool are some default Types)</param>
		/// <returns>The original object with all the fields set according to the Hashtable</returns>
		public static object SetObjectFieldsWithHashTable(object OriginalObjectToSet, Hashtable hashTableWithFieldValues, List<OwnTypeFunctions> ownFunctionList = null, Boolean GetFieldsTrue_GetPropertiesFalse = true)
		{
			if (OriginalObjectToSet != null)
			{
				object tmpObjectForOwnTypes;
				if (GetFieldsTrue_GetPropertiesFalse)
				{
					foreach (FieldInfo info in OriginalObjectToSet.GetType().GetFields())
						if (info.IsPublic)
							if (info.FieldType == typeof(string))
								//tmpNode.Tag.GetType().GetField(info.Name).SetValue(tmpNode.Tag, tmpHashTable[info.Name]);
								info.SetValue(OriginalObjectToSet, hashTableWithFieldValues[info.Name]);
							else if (info.FieldType == typeof(int))
								info.SetValue(OriginalObjectToSet, Convert.ToInt32(hashTableWithFieldValues[info.Name]));
							else if (info.FieldType == typeof(bool))
								info.SetValue(OriginalObjectToSet, Convert.ToBoolean(hashTableWithFieldValues[info.Name]));
							else if (info.FieldType == typeof(List<string>))
							{
								List<string> tmpList = new List<string>();
								foreach (string s in hashTableWithFieldValues[info.Name].ToString().Split('|'))
									tmpList.Add(s);
								if (tmpList.Count == 0 || tmpList[0].Trim().Length == 0)
									tmpList = new List<string>();
								info.SetValue(OriginalObjectToSet, tmpList);
							}
							else if (GetObjectFromOwnFunctionList(hashTableWithFieldValues[info.Name], info.FieldType, ownFunctionList, out tmpObjectForOwnTypes))
							{
								//MessageBox.Show(info.FieldType.FullName);
								info.SetValue(OriginalObjectToSet, tmpObjectForOwnTypes);
							}
							else
								MessageBox.Show("The following type cannot be converted to a string (no function yet for it): " + info.FieldType.ToString(), "Invalid type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				else
				{
					foreach (PropertyInfo info in OriginalObjectToSet.GetType().GetProperties())
						if (info.PropertyType == typeof(string))
							//tmpNode.Tag.GetType().GetField(info.Name).SetValue(tmpNode.Tag, tmpHashTable[info.Name]);
							info.SetValue(OriginalObjectToSet, hashTableWithFieldValues[info.Name], null);
						else if (info.PropertyType == typeof(int))
							info.SetValue(OriginalObjectToSet, Convert.ToInt32(hashTableWithFieldValues[info.Name]), null);
						else if (info.PropertyType == typeof(bool))
							info.SetValue(OriginalObjectToSet, Convert.ToBoolean(hashTableWithFieldValues[info.Name]), null);
						else if (info.PropertyType == typeof(List<string>))
						{
							List<string> tmpList = new List<string>();
							foreach (string s in hashTableWithFieldValues[info.Name].ToString().Split('|'))
								tmpList.Add(s);
							if (tmpList.Count == 0 || tmpList[0].Trim().Length == 0)
								tmpList = new List<string>();
							info.SetValue(OriginalObjectToSet, tmpList, null);
						}
						else if (GetObjectFromOwnFunctionList(hashTableWithFieldValues[info.Name], info.PropertyType, ownFunctionList, out tmpObjectForOwnTypes))
						{
							//MessageBox.Show(info.FieldType.FullName);
							info.SetValue(OriginalObjectToSet, tmpObjectForOwnTypes, null);
						}
						else
							MessageBox.Show("The following type cannot be converted to a string (no function yet for it): " + info.PropertyType.ToString(), "Invalid type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			return OriginalObjectToSet;
		}

		private static bool GetStringFromOwnFunctionList(dynamic InputObject, Type InputObjectType, List<OwnTypeFunctions> ownFunctionListIn, out string ResultString)
		{
			if (ownFunctionListIn == null) { ResultString = null; return false; }

			foreach (OwnTypeFunctions owntypefunction in ownFunctionListIn)
			{
				//MessageBox.Show(owntypefunction.ownType.FullName);
				//MessageBox.Show(InputObject.GetType().FullName);
				if (owntypefunction.ownType == InputObjectType)
				{
					ResultString = owntypefunction.OwnFunctionToGetString(InputObject);
					return true;
				}
			}
			ResultString = null;
			return false;
		}

		private static bool GetObjectFromOwnFunctionList(object InputObject, Type InputObjectType, List<OwnTypeFunctions> ownFunctionListIn, out Object ResultObject)
		{
			if (ownFunctionListIn == null) { ResultObject = null; return false; }

			foreach (OwnTypeFunctions owntypefunction in ownFunctionListIn)
			{
				//MessageBox.Show(owntypefunction.ownType.FullName);
				//MessageBox.Show(InputObject.GetType().FullName);
				if (owntypefunction.ownType == InputObjectType)
				{
					ResultObject = owntypefunction.OwnFunctionToGetString(InputObject);
					return true;
				}
			}
			ResultObject = null;
			return false;
		}

		/// <summary>
		/// A class to define custom functions used for custom types when using the HashTable/Field functions
		/// </summary>
		public class OwnTypeFunctions
		{
			/// <summary>
			/// A custom function to obtain a custom object
			/// </summary>
			/// <param name="InputObject">The input object from which to obtain the resulting object</param>
			/// <returns>Returns the object as resulting from the given function</returns>
			public delegate dynamic OwnFunctionToGetStringDelegate(dynamic InputObject);

			/// <summary>
			/// A custom type (usually custom classes)
			/// </summary>
			public Type ownType;

			/// <summary>
			/// The delegate function which will be set by the constructor of this class (function used to do conversion)
			/// </summary>
			public OwnFunctionToGetStringDelegate OwnFunctionToGetString;

			/// <summary>
			/// Constructor of this class
			/// </summary>
			/// <param name="ownTypeIn">A custom type (usually custom classes)</param>
			/// <param name="OwnFunctionToGetStringIn">The delegate function which will be set by the constructor of this class (function used to do conversion)</param>
			public OwnTypeFunctions(Type ownTypeIn, OwnFunctionToGetStringDelegate OwnFunctionToGetStringIn)
			{
				ownType = ownTypeIn;
				OwnFunctionToGetString = OwnFunctionToGetStringIn;
			}
		}
	}
}
