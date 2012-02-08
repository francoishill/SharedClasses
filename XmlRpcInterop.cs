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

	public static void StartDynamicCodeInvokingServer_XmlRpc()
	{
		//HttpServerChannel httpServerChannel = new HttpServerChannel(
		//	CreateDefaultChannelProperties(),
		//	CreateDefaultServerProviderChain();
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
	}

	//public static void TestFromClient_DynamicCodeInvokingServer()
	//{
	//	SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
	//	Iclientside_DynamicCodeInvokingServerClass proxy = XmlRpcProxyGen.Create<Iclientside_DynamicCodeInvokingServerClass>();
	//	proxy.Url = SharedClassesSettings.tracXmlRpcInteropSettings.GetCominedUrlForDynamicInvokationServer();
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
			return DynamicCodeInvoking.RunCodeFromStaticClass(AssemblyQualifiedNameOfType, commandParameterTypesfullnames, methodName, parameterValues);
		}
		[XmlRpcMethod("RunBlockOfCode")]
		public DynamicCodeInvoking.RunCodeReturnStruct RunBlockOfCode(string block)
		{
			return DynamicCodeInvoking.RunBlockOfCode(block);
		}
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