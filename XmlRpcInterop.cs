using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CookComputing.XmlRpc;
using SharedClasses;

//ClientOnServerSide side settings
//[XmlRpcUrl("http://fjh.dyndns.org:5678/DynamicCodeInvoking/xmlrpc")]
public interface Iclientside_DynamicCodeInvokingServerClass : IXmlRpcProxy
{
	[XmlRpcMethod("RunCodeDynamically")]
	DynamicCodeInvoking.RunCodeReturnStruct RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues);
	[XmlRpcMethod("RunBlockOfCode")]
	DynamicCodeInvoking.RunCodeReturnStruct RunBlockOfCode(string block);
	[XmlRpcMethod("SaveJsonStringToFile")]
	DynamicCodeInvoking.RunCodeReturnStruct SaveJsonStringToFile(string Category, string Name, string jsonString);
	[XmlRpcMethod("GetJsonStringFromFile")]
	DynamicCodeInvoking.RunCodeReturnStruct GetJsonStringFromFile(string Category, string Name);
	/*
	Rather use a "newest version" file than using XmlRpc
	[XmlRpcMethod("GetAutoSyncVersion")]
	DynamicCodeInvoking.RunCodeReturnStruct GetAutoSyncVersion(string UserFolderName);*/
	/*
	Rather upload a lock file than using XmlRpc
	[XmlRpcMethod("LockAutoSyncServer")]
	DynamicCodeInvoking.RunCodeReturnStruct LockAutoSyncServer(string UserFolderName);*/
}

public class XmlRpcInterop
{
	public static void UnregisterAllRegisteredChannels()
	{
		IChannel[] channels = ChannelServices.RegisteredChannels;
		foreach (IChannel chan in channels)
			ChannelServices.UnregisterChannel(chan);
	}

	private static ListDictionary CreateDefaultChannelProperties()
	{
		return new ListDictionary()
		{
			{ "port", GlobalSettings.TracXmlRpcInteropSettings.Instance.DynamicInvokationServer_PortNumber },//5678 }
		};
	}

	private static IClientChannelSinkProvider CreateDefaultClientProviderChain()
	{
		IClientChannelSinkProvider chain = new XmlRpcClientFormatterSinkProvider();
		IClientChannelSinkProvider sink = chain;
		sink.Next = new SoapClientFormatterSinkProvider();
		sink = sink.Next;
		return chain;
	}

	private static IServerChannelSinkProvider CreateDefaultServerProviderChain()
	{
		IServerChannelSinkProvider chain = new XmlRpcServerFormatterSinkProvider();
		IServerChannelSinkProvider sink = chain;
		sink.Next = new SoapServerFormatterSinkProvider();
		sink = sink.Next;
		return chain;
	}

	private static string GetDefaultRelativeUri()
	{
		string tmpUri = GlobalSettings.TracXmlRpcInteropSettings.Instance.DynamicInvokationServer_RelativePath;
		while (tmpUri.StartsWith("/")) tmpUri = tmpUri.Substring(1);
		return tmpUri;
	}

	public static bool StartDynamicCodeInvokingServer_XmlRpc()
	{
		//HttpServerChannel httpServerChannel = new HttpServerChannel(
		//	CreateDefaultChannelProperties(),
		//	CreateDefaultServerProviderChain();
		try
		{
			Lazy<HttpChannel> http = new Lazy<HttpChannel>(() => new HttpChannel());
			HttpChannel httpChannel = new HttpChannel(
				CreateDefaultChannelProperties(),
				CreateDefaultClientProviderChain(),
				CreateDefaultServerProviderChain());
			ChannelServices.RegisterChannel(httpChannel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(DynamicCodeInvokingServerClass),
				GetDefaultRelativeUri(),//"DynamicCodeInvoking/xmlrpc",
				WellKnownObjectMode.Singleton);
			return true;
		}
		catch (Exception exc)
		{
			UserMessages.ShowWarningMessage(string.Format("Unable to start XmlRpc server, an exceptions occurred: {0}", exc.Message));
			return false;
		}
	}

	//public static void TestFromClient_DynamicCodeInvokingServer()
	//{
	//	SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
	//	Iclientside_DynamicCodeInvokingServerClass proxy = XmlRpcProxyGen.Create<Iclientside_DynamicCodeInvokingServerClass>();
	//	proxy.Url = SharedClassesSettings.tracXmlRpcInteropSettings.GetCombinedUrlForDynamicInvokationServer();
	//	//string[] TypeStringArray;
	//	//object[] ParameterList;
	//	//DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, "Hallo", "Temp title", true);
	//	//DynamicCodeInvoking.RunCodeReturnStruct resultObj = proxy.RunCodeDynamically(
	//	//	typeof(UserMessages).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
	//	//	TypeStringArray,
	//	//	"ShowMessage",//"Show",
	//	//	ParameterList
	//	//	);
	//	//XmlRpcClientProtocol cp = new XmlRpcClientProtocol();
	//	//cp.Invoke(mi: typeof(Directory).GetMethod("GetFiles"), Parameters: new object[] { @"c:\", "*", SearchOption.TopDirectoryOnly });
	//	//return;
	//	//cannot be mapped to an XML-RPC type

	//	string[] TypeStringArray;
	//	object[] ParameterList;
	//	DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, @"c:\", "*", SearchOption.TopDirectoryOnly);
	//	DynamicCodeInvoking.RunCodeReturnStruct resultObj = proxy.RunCodeDynamically(
	//		typeof(Directory).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
	//		TypeStringArray,
	//		"GetFiles",//"Show",
	//		ParameterList
	//		);

	//	//string[] TypeStringArray;
	//	//object[] ParameterList;
	//	//DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, @"c:\111");
	//	//string retError = proxy.RunCodeDynamically(
	//	//	typeof(Directory).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
	//	//	TypeStringArray,
	//	//	"CreateDirectory",//"Show",
	//	//	ParameterList
	//	//	);
	//	if (resultObj.Success)
	//	{
	//		string successMsg = "Successfully performed command: ";
	//		if (resultObj.MethodInvokeResultingObject is string[])
	//			foreach (string s in resultObj.MethodInvokeResultingObject as string[])
	//				successMsg += Environment.NewLine + s;
	//		else if (resultObj.MethodInvokeResultingObject is List<string>)
	//			foreach (string s in resultObj.MethodInvokeResultingObject as List<string>)
	//				successMsg += Environment.NewLine + s;
	//		else successMsg += resultObj.ToString();
	//		UserMessages.ShowInfoMessage(successMsg);
	//	}
	//	else
	//		UserMessages.ShowErrorMessage("Return error string: " + resultObj.ErrorMessage);
	//}

	public class DynamicCodeInvokingServerClass : MarshalByRefObject
	{
		/// <summary>
		/// Returns the error string
		/// </summary>
		[XmlRpcMethod("RunCodeDynamically")]
		public DynamicCodeInvoking.RunCodeReturnStruct RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues)
		{
			return DynamicCodeInvoking.ServerRunCodeFromStaticClass(AssemblyQualifiedNameOfType, commandParameterTypesfullnames, methodName, parameterValues);
		}
		[XmlRpcMethod("RunBlockOfCode")]
		public DynamicCodeInvoking.RunCodeReturnStruct RunBlockOfCode(string block)
		{
			return DynamicCodeInvoking.ServerRunBlockOfCode(block);
		}

		[XmlRpcMethod("SaveJsonStringToFile")]
		public DynamicCodeInvoking.RunCodeReturnStruct SaveJsonStringToFile(string Category, string Name, string jsonString)
		{
			return DynamicCodeInvoking.ServerSaveJsonStringToFile(Category, Name, jsonString);
		}

		[XmlRpcMethod("GetJsonStringFromFile")]
		public DynamicCodeInvoking.RunCodeReturnStruct GetJsonStringFromFile(string Category, string Name)
		{
			return DynamicCodeInvoking.ServerGetJsonStringFromFile(Category, Name);
		}

		/*
		Rather use a "newest version" file than using XmlRpc
		[XmlRpcMethod("GetAutoSyncVersion")]
		public DynamicCodeInvoking.RunCodeReturnStruct GetAutoSyncVersion(string UserFolderName)
		{
			return DynamicCodeInvoking.ServerGetAutoSyncVersion(UserFolderName);
		}*/

		/*
		Rather upload a lock file than using XmlRpc
		[XmlRpcMethod("LockAutoSyncServer")]
		public DynamicCodeInvoking.RunCodeReturnStruct LockAutoSyncServer(string UserFolderName)
		{
			return DynamicCodeInvoking.ServerLockAutoSyncServer(UserFolderName);
		}*/
	}

	public class Tracer : XmlRpcLogger
	{
		protected override void OnRequest(object sender,
			XmlRpcRequestEventArgs e)
		{
			DumpStream(e.RequestStream);
		}

		protected override void OnResponse(object sender,
			XmlRpcResponseEventArgs e)
		{
			DumpStream(e.ResponseStream);
		}

		private void DumpStream(Stream stm)
		{
			stm.Position = 0;
			TextReader trdr = new StreamReader(stm);
			String s = trdr.ReadLine();
			while (s != null)
			{
				System.Diagnostics.Trace.WriteLine(s);
				s = trdr.ReadLine();
			}
		}
	}
}