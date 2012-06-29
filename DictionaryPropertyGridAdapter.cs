using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>
/// Usage as follows:
/// IDictionary d = new Hashtable();
/// d["Hello"] = "World";
/// d["Meaning"] = 42;
/// d["Shade"] = Color.ForestGreen;
/// propertyGrid1.SelectedObject = new DictionaryPropertyGridAdapter(d);
/// </summary>
public class DictionaryPropertyGridAdapter : ICustomTypeDescriptor
{
	public Dictionary<string, ParameterNameAndType> _dictionary;

	public DictionaryPropertyGridAdapter(IDictionary d)
	{
		if (!(d is Dictionary<string, ParameterNameAndType>))
		{
			throw new Exception("Cannot use other dictionary formats than Dictionary<string, PropertyNameAndType>");
		}
		_dictionary = d as Dictionary<string, ParameterNameAndType>;
	}

	public string GetComponentName()
	{
		return TypeDescriptor.GetComponentName(this, true);
	}

	public EventDescriptor GetDefaultEvent()
	{
		return TypeDescriptor.GetDefaultEvent(this, true);
	}

	public string GetClassName()
	{
		return TypeDescriptor.GetClassName(this, true);
	}

	public EventDescriptorCollection GetEvents(Attribute[] attributes)
	{
		return TypeDescriptor.GetEvents(this, attributes, true);
	}

	EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents()
	{
		return TypeDescriptor.GetEvents(this, true);
	}

	public TypeConverter GetConverter()
	{
		return TypeDescriptor.GetConverter(this, true);
	}

	public object GetPropertyOwner(PropertyDescriptor pd)
	{
		return _dictionary;
	}

	public AttributeCollection GetAttributes()
	{
		return TypeDescriptor.GetAttributes(this, true);
	}

	public object GetEditor(Type editorBaseType)
	{
		return TypeDescriptor.GetEditor(this, editorBaseType, true);
	}

	public PropertyDescriptor GetDefaultProperty()
	{
		return null;
	}

	PropertyDescriptorCollection
			System.ComponentModel.ICustomTypeDescriptor.GetProperties()
	{
		return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
	}

	public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
	{
		ArrayList properties = new ArrayList();
		foreach (DictionaryEntry e in _dictionary as IDictionary)
		{
			properties.Add(new DictionaryPropertyDescriptor(_dictionary, e.Key.ToString()));
		}

		PropertyDescriptor[] props =
            (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

		return new PropertyDescriptorCollection(props);
	}
}

public class DictionaryPropertyDescriptor : PropertyDescriptor
{
	Dictionary<string, ParameterNameAndType> _dictionary;
	string _key;

	internal DictionaryPropertyDescriptor(Dictionary<string, ParameterNameAndType> d, string key)
		: base(key.ToString(), null)
	{
		_dictionary = d;
		_key = key;
	}

	public override Type PropertyType
	{
		get { return _dictionary[_key].type; }//.GetType(); }
	}

	public override void SetValue(object component, object value)
	{
		_dictionary[_key].Value = value;// as PropertyNameAndType;
	}

	public override object GetValue(object component)
	{
		return _dictionary[_key].Value;
	}

	public override bool IsReadOnly
	{
		get { return false; }
	}

	public override Type ComponentType
	{
		get { return null; }
	}

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override void ResetValue(object component)
	{
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}
}

public class ParameterNameAndType
{
	public string Name;
	public Type type;
	public object Value;
	public ParameterNameAndType(string NameIn, Type typeIn, object Value = null)
	{
		this.Name = NameIn;
		this.type = typeIn;
		this.Value = Value;
	}

	public void OverrideValue(object Value)
	{
		this.Value = Value;
	}
}