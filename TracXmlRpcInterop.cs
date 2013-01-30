using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CookComputing.XmlRpc;
using SharedClasses;

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
			UserMessages.ShowWarningMessage("Could not parse string of datetime to DateTime8601: " + sinceDate.ToString());
			return null;
		}
		object obj = null;
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			try
			{
				obj = tracMonitorSystem.Wiki_GetRecentChanges(sinceDate);
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Cannot get recent changes in TracXml: " + exc.Message);
			}
		},
		ThreadName: "Wiki_GetRecentChanges");
		return obj;
	}

	public static object[] PerformSearch(string SearchQuery, string xmlRpcUrl, string[] SearchFilters = null, string Username = null, string Password = null)
	{
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);
		object[] objarray = null;
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			try
			{
				objarray = tracMonitorSystem.PerformSearch(SearchQuery, SearchFilters ?? new string[0]);
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Cannot perform TracXml search: " + exc.Message);
			}
		},
		ThreadName: "PerformSearch");
		return objarray;
	}

	public static List<string> GetFieldLables(string xmlRpcUrl, string Username = null, string Password = null)
	{
		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		List<string> returnList = new List<string>();
		try
		{
			object[] fields = new object[0];//
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				try
				{
					fields = tracMonitorSystem.GetTicketFields();
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Cannot get field labels in TracXml: " + exc.Message);
				}
			},
			ThreadName: "GetFieldLables");
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
			UserMessages.ShowErrorMessage("Error: " + exc.Message);
		}
		return returnList;
	}

	public static int[] _getTicketIds(string xmlRpcUrl, string Username = null, string Password = null, string queryString = null)
	{
		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		try
		{
			int[] ticketIDs = new int[0];//
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				try
				{
					if (queryString == null)
						ticketIDs = tracMonitorSystem.Query();
					else
						ticketIDs = tracMonitorSystem.Query(queryString);
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Cannot get ticket IDs in TracXml for xmlRpcUrl '" + xmlRpcUrl + "': " + exc.Message);
				}
			},
			ThreadName: "GetTicketIds");
			return ticketIDs;
		}
		catch (Exception exc)
		{
			UserMessages.ShowErrorMessage("Error: " + exc.Message);
		}
		return new int[0];
	}

	public static int[] GetOpenTicketIds(string xmlRpcUrl, string Username = null, string Password = null)
	{
		return _getTicketIds(xmlRpcUrl, Username, Password, null);//
	}
	public static int[] GetClosedTicketIds(string xmlRpcUrl, string Username = null, string Password = null)
	{
		return _getTicketIds(xmlRpcUrl, Username, Password, "status=closed");//
	}

	public static Dictionary<string, object> GetFieldValuesOfTicket(int ticketId, string xmlRpcUrl, string Username = null, string Password = null)
	{
		Dictionary<string, object> tmpDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		try
		{
			object[] IdTimecreatedTimechangedAttributes = new object[0];
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				try
				{
					IdTimecreatedTimechangedAttributes = tracMonitorSystem.TicketGet(ticketId);
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Cannot get field values of ticket in TracXml: " + exc.Message);
				}
			},
			ThreadName: "GetFieldValuesOfTicket");
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
			UserMessages.ShowErrorMessage("Error: " + ex.Message);
		}
		return tmpDict;
	}

	public static Dictionary<int, string> GetAllTicketDescriptions(string xmlRpcUrl, string Username = null, string Password = null)
	{
		Dictionary<int, string> tmpDict = new Dictionary<int, string>();

		int[] ticketIDs = GetOpenTicketIds(xmlRpcUrl, Username, Password);
		foreach (int id in ticketIDs)
		{
			Dictionary<string, object> fieldvalues = GetFieldValuesOfTicket(id, xmlRpcUrl, Username, Password);
			if (!fieldvalues.ContainsKey("description"))
				UserMessages.ShowWarningMessage("Could not find description for ticket #" + id);
			else tmpDict.Add(id, fieldvalues["description"].ToString());
		}

		return tmpDict;
	}

	public enum TicketTypeEnum { Bug, Improvement, NewFeature }
	private static bool CanParseStringToTicketTypeEnum(string ticketTypeString)
	{
		foreach (string name in Enum.GetNames(typeof(TicketTypeEnum)))
			if (name.ToLower() == ticketTypeString.ToLower())
				return true;
		return false;
	}

	public static TicketTypeEnum? ParseTicketTypeFromString(string ticketTypeString)
	{
		foreach (TicketTypeEnum ticketType in Enum.GetValues(typeof(TicketTypeEnum)))
			if (ticketType.ToString().ToLower() == ticketTypeString.ToLower())
				return ticketType;
		return null;
	}

	public class TracTicketDetails
	{
		public string Summary;
		public string Description;
		public TicketTypeEnum TicketType;
		public List<string> TicketComments;
		public TracTicketDetails(string Summary, string Description, TicketTypeEnum TicketType)
		{
			this.Summary = Summary;
			this.Description = Description;
			this.TicketType = TicketType;
		}
	}

	public class ChangeLogs
	{
		public string RootXmlRpcUrl;
		public string RootTracUrl;
		public Dictionary<int, TracXmlRpcInterop.TracTicketDetails> BugsFixed;
		public Dictionary<int, TracXmlRpcInterop.TracTicketDetails> Improvements;
		public Dictionary<int, TracXmlRpcInterop.TracTicketDetails> NewFeatures;

		public ChangeLogs(string RootXmlRpcUrl, Dictionary<int, TracXmlRpcInterop.TracTicketDetails> BugsFixed, Dictionary<int, TracXmlRpcInterop.TracTicketDetails> Improvements, Dictionary<int, TracXmlRpcInterop.TracTicketDetails> NewFeatures)
		{
			this.RootXmlRpcUrl = RootXmlRpcUrl;
			this.UpdateRootTracUrlFromRootXmlRpcUrl();
			this.BugsFixed = BugsFixed;
			this.Improvements = Improvements;
			this.NewFeatures = NewFeatures;
		}

		private void UpdateRootTracUrlFromRootXmlRpcUrl()
		{
			this.RootTracUrl = RootXmlRpcUrl.TrimEnd('/');

			List<string> endSectionsToRemove = new List<string>() { "/xmlrpc", "/rpc", "/login" };
			foreach (var endSec in endSectionsToRemove)
				if (RootTracUrl.EndsWith(endSec, StringComparison.InvariantCultureIgnoreCase))
					RootTracUrl = RootTracUrl.Substring(0, RootTracUrl.Length - endSec.Length);
			RootTracUrl = RootTracUrl.TrimEnd('/');
		}

		public string GetTicketUrl(int ticketID)
		{
			return string.Format("{0}/ticket/{1}", RootTracUrl, ticketID);
		}

		public static string GetTracBaseUrlForApplication(string applicationName)
		{
			string preformattedUrl = "http://fjh.dyndns.org/trac/{0}";
			return string.Format(preformattedUrl, applicationName.ToLower().Replace(" ", ""));
		}

		public static string GetTracXmlRpcUrlForApplication(string applicationName)
		{
			return GetTracBaseUrlForApplication(applicationName).TrimEnd('/') + "/login/xmlrpc";
		}
	}

	public static Dictionary<int, TracTicketDetails> GetAllClosedTicketDescriptionsAndTypes(string xmlRpcUrl, DateTime? sinceDate = null, string Username = null, string Password = null, TextFeedbackEventHandler textFeedbackEvent = null, object textfeedbackSenderObject = null)
	{
		Dictionary<int, TracTicketDetails> tmpDict = new Dictionary<int, TracTicketDetails>();

		List<int> closedTicketIDs = GetClosedTicketIds(xmlRpcUrl, Username, Password).ToList();//GetOpenTicketIds(xmlRpcUrl, Username, Password).ToList();
		if (sinceDate.HasValue)//Let us now filter only the tickets from the sinceDate
		{
			DateTime universalSinceDate = 
				sinceDate.Value
				.ToUniversalTime();

			ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);
			try
			{
				List<int> recentChangedIDs = tracMonitorSystem.Ticket_GetRecentChanges(universalSinceDate)
					.Select(ob => int.Parse(ob.ToString())).ToList();

				for (int i = closedTicketIDs.Count - 1; i >= 0; i--)
					if (!recentChangedIDs.Contains(closedTicketIDs[i]))
						closedTicketIDs.RemoveAt(i);//We remove this closed ticket as it was not recently changed
			}
			catch (Exception exc)
			{
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "Error finding recently changed Trac ticket IDs: " + exc.Message);
			}
		}

		foreach (int id in closedTicketIDs)
		{
			Dictionary<string, object> fieldvalues = GetFieldValuesOfTicket(id, xmlRpcUrl, Username, Password);
			if (!fieldvalues.ContainsKey("description"))
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find description for ticket #" + id, TextFeedbackType.Noteworthy);
			else if (!fieldvalues.ContainsKey("type"))
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find type for ticket #" + id, TextFeedbackType.Noteworthy);
			else if (!CanParseStringToTicketTypeEnum(fieldvalues["type"].ToString()))
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not parse Trac ticket type from string: " + fieldvalues["type"].ToString(), TextFeedbackType.Noteworthy);
			else
			{
				TicketTypeEnum? tempNullableTicketType = ParseTicketTypeFromString(fieldvalues["type"].ToString());
				if (tempNullableTicketType != null)
					tmpDict.Add(id, new TracTicketDetails(fieldvalues["summary"].ToString(), fieldvalues["description"].ToString(), (TicketTypeEnum)tempNullableTicketType));
			}
		}

		return tmpDict;
	}

	public static List<ChangeLogStruct> GetChangeLogs(int ticketId, string xmlRpcUrl, string Username = null, string Password = null)
	{
		List<ChangeLogStruct> tmpList = new List<ChangeLogStruct>();

		//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
		ITracServerFunctions tracMonitorSystem = InitializeTracServer(xmlRpcUrl, Username, Password);

		//time, author, field, oldvalue, newvalue, permanent
		object[] changelogArray = new object[0];
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			try
			{
				changelogArray = tracMonitorSystem.Ticket_ChangeLog(ticketId);
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Cannot get ChangeLog in TracXml: " + exc.Message);
			}
		},
		ThreadName: "ChangeLogs");

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
		string[] listmethods = new string[0];
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			try
			{
				listmethods = tracMonitorSystem.ListMethods();
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Cannot get list of methods in TracXml: " + exc.Message);
			}
		},
		ThreadName: "GetListOfMethods");
		return listmethods;
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

		[XmlRpcMethod("ticket.getRecentChanges")]
		object[] Ticket_GetRecentChanges(DateTime sinceDate);//Date must be UniversalDateTime (UTC)

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