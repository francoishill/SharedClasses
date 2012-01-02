using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using CookComputing.XmlRpc;

public class TracXmlRpcInterop
{
	//public const string MonitorSystemXmlRpcUrl = "https://francoishill.devguard.com/trac/monitorsystem/login/xmlrpc";
	//public const string QuickAccessXmlRpcUrl = "https://francoishill.devguard.com/trac/quickaccess/login/xmlrpc";

	public static object Wiki_GetRecentChanges(DateTime sinceDate, string xmlRpcUrl, string Username = null, string Password = null)
	{
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		DateTime dt;
		if (!DateTime8601.TryParseDateTime8601(sinceDate.ToString("s", CultureInfo.InvariantCulture), out dt))
		{
			MessageBox.Show("Could not parse string of datetime to DateTime8601: " + sinceDate.ToString());
			return null;
		}
		return tracMonitorSystem.Wiki_GetRecentChanges(sinceDate);
	}

	public static object[] PerformSearch(string SearchQuery, string xmlRpcUrl, string[] SearchFilters = null, string Username = null, string Password = null)
	{
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);
		return tracMonitorSystem.PerformSearch(SearchQuery, SearchFilters ?? new string[0]);
	}

	public static List<string> GetFieldLables(string xmlRpcUrl, string Username = null, string Password = null)
	{
		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		List<string> returnList = new List<string>();
		try
		{
			object[] fields = tracMonitorSystem.GetTicketFields();
			foreach (object field in fields)
				if (field is XmlRpcStruct)
				{
					XmlRpcStruct hashTable = field as XmlRpcStruct;
					if (hashTable.ContainsKey("label"))
						returnList.Add(hashTable["label"].ToString());
				}
		}
		catch (Exception exc)
		{
			MessageBox.Show("Error: " + exc.Message);
		}
		return returnList;
	}

	public static int[] GetTicketIds(string xmlRpcUrl, string Username = null, string Password = null)
	{
		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		try
		{
			int[] ticketIDs = tracMonitorSystem.Query();
			return ticketIDs;
		}
		catch (Exception exc)
		{
			MessageBox.Show("Error: " + exc.Message);
		}
		return new int[0];
	}

	public static Dictionary<string, object> GetFieldValuesOfTicket(int ticketId, string xmlRpcUrl, string Username = null, string Password = null)
	{
		Dictionary<string, object> tmpDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		try
		{
			object[] IdTimecreatedTimechangedAttributes = tracMonitorSystem.TicketGet(ticketId);
			foreach (object obj in IdTimecreatedTimechangedAttributes)
			{
				if ((obj is XmlRpcStruct))
				{
					XmlRpcStruct tmpHashTable = obj as XmlRpcStruct;
					foreach (object key in tmpHashTable.Keys)
						tmpDict.Add(key.ToString(), tmpHashTable[key]);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error: " + ex.Message);
		}
		return tmpDict;
	}

	public static List<ChangeLogStruct> ChangeLogs(int ticketId, string xmlRpcUrl, string Username = null, string Password = null)
	{
		List<ChangeLogStruct> tmpList = new List<ChangeLogStruct>();

		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		//time, author, field, oldvalue, newvalue, permanent
		object[] changelogArray = tracMonitorSystem.Ticket_ChangeLog(ticketId);
		foreach (object obj in changelogArray)
		{
			if (obj is object[]
				&& (obj as object[]).Length == 6)
			{
				object[] objarr = obj as object[];
				if (objarr[0] is DateTime
					&& objarr[1] is string
					&& objarr[2] is string
					&& objarr[3] is string
					&& objarr[4] is string
					&& objarr[5] is int)//It is a ChangeLogStruct
				{
					tmpList.Add(new ChangeLogStruct()
					{
						ChangedDateTime = (DateTime)objarr[0],
						Author = objarr[1] as string,
						Field = objarr[2] as string,
						OldValue = objarr[3] as string,
						NewValue = objarr[4] as string,
						Permanent = (int)objarr[5]
					});
				}
			}
		}
		return tmpList;
	}

	public static string[] GetListOfMethods(string xmlRpcUrl, string Username = null, string Password = null)
	{
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);
		return tracMonitorSystem.ListMethods();
	}

	private static ITracServerFunctions InitializeTracServer(string xmlRpcUrl, string Username, string Password)
	{
		ITracServerFunctions tracMonitorSystem;
		tracMonitorSystem = XmlRpcProxyGen.Create<ITracServerFunctions>();
		tracMonitorSystem.Url = xmlRpcUrl;
		tracMonitorSystem.PreAuthenticate = true;

		tracMonitorSystem.Credentials = new System.Net.NetworkCredential(
			Username ?? GlobalSettings.TracXmlRpcInteropSettings.Instance.Username,
			Password ?? GlobalSettings.TracXmlRpcInteropSettings.Instance.Password);
		return tracMonitorSystem;
	}

	//[XmlRpcUrl("https://francoishill.devguard.com/trac/quickaccess/login/xmlrpc")]
	public interface ITracServerFunctions : IXmlRpcProxy
	{
		[XmlRpcMethod("wiki.getRecentChanges")]
		object Wiki_GetRecentChanges(DateTime sinceDate);//DateTime8601 since);

		[XmlRpcMethod("wiki.getRPCVersionSupported")]
		int GetRPCVersionSupported();

		[XmlRpcMethod("system.listMethods")]
		string[] ListMethods();

		[XmlRpcMethod("wiki.getPage")]
		string GetPage(string pagename, int version = 0);

		[XmlRpcMethod("wiki.getAllPages")]
		string[] GetAllPages();

		[XmlRpcMethod("search.performSearch")]
		object[] PerformSearch(string query, string[] filters = null);

		[XmlRpcMethod("ticket.query")]
		int[] Query(string qstr = "status!=closed");

		[XmlRpcMethod("ticket.status.get")]
		string TicketStatusGet(string name);

		[XmlRpcMethod("ticket.get")]
		object[] TicketGet(int id);

		[XmlRpcMethod("ticket.getTicketFields")]
		object[] GetTicketFields();

		[XmlRpcMethod("ticket.changeLog")]
		object[] Ticket_ChangeLog(int id, int when = 0);

		[XmlRpcMethod("system.listMethods")]
		string[] System_ListMethods();
	}

	public struct ChangeLogStruct
	{
		public DateTime ChangedDateTime;
		public string Author;
		public string Field;
		public string OldValue;
		public string NewValue;
		public int Permanent;//Used to distinguish collateral changes that are not yet immutable (like attachments, currently)
	}
}