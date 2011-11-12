using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Windows.Forms;
using CookComputing.XmlRpc;

public class XmlRpcInterop
{
	public static void UnregisterAllRegisteredChannels()
	{
		IChannel[] channels = ChannelServices.RegisteredChannels;
		foreach (IChannel chan in channels)
			ChannelServices.UnregisterChannel(chan);
	}

	public static void StartDynamicCodeInvokingServer_XmlRpc()
	{
		RemotingConfiguration.Configure("DynamicCodeInvokingServerSettings.config", false);
		RemotingConfiguration.RegisterWellKnownServiceType(
			typeof(DynamicCodeInvokingServerClass),
			"DynamicCodeInvoking.rem",
			WellKnownObjectMode.Singleton);
	}

	public static void SampleServer()
	{
		RemotingConfiguration.Configure("SumAndDiff.exe.config", false);
		// for CookComputing.XmlRpc
		//RemotingConfiguration.Configure("SumAndDiff.exe.config"); 
		RemotingConfiguration.RegisterWellKnownServiceType(
			typeof(SampleServer_ClassWithFunctions),
			"sumAndDiff.rem",
			WellKnownObjectMode.Singleton);
	}

	public static void TestFromClient_DynamicCodeInvokingServer()
	{
		Iclientside_DynamicCodeInvokingServerClass proxy = XmlRpcProxyGen.Create<Iclientside_DynamicCodeInvokingServerClass>();
		string retError = proxy.RunCodeDynamically(
			typeof(UserMessages).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
			new string[] { typeof(string).AssemblyQualifiedName, typeof(string).AssemblyQualifiedName, typeof(IWin32Window).AssemblyQualifiedName, typeof(bool).AssemblyQualifiedName },//new string[] { typeof(string).AssemblyQualifiedName }
			"ShowMessage",//"Show",
			"Hallo", "Temp title", null, true
			);
		if (string.IsNullOrWhiteSpace(retError)) UserMessages.ShowInfoMessage("Successfully performed command");
		else UserMessages.ShowErrorMessage("Return error string: " + retError);
	}

	public static void SampleClient()
	{
		ISampleServer_ClassWithFunctions proxy = XmlRpcProxyGen.Create<ISampleServer_ClassWithFunctions>();
		SampleStruct ret = proxy.SumAndDifference(2, 3);
		MessageBox.Show(ret.difference.ToString() + ", " + ret.sum);
		//for version 1 of Xml-Rpc.Net: ISumAndDiff proxy = (ISumAndDiff)XmlRpcProxyGen.Create(typeof(ISumAndDiff));
	}

	public class SampleServer_ClassWithFunctions : MarshalByRefObject
	{
		[XmlRpcMethod("sumAndDifference")]
		public SampleStruct SumAndDifference(int x, int y)
		{
			SampleStruct ret;
			ret.sum = x + y;
			ret.difference = x - y;
			return ret;
		}
	}

	public class DynamicCodeInvokingServerClass : MarshalByRefObject
	{
		/// <summary>
		/// Returns the error string
		/// </summary>
		[XmlRpcMethod("RunCodeDynamically")]
		public string RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues)
		{
			return DynamicCodeInvoking.RunCodeFromStaticClass(AssemblyQualifiedNameOfType, commandParameterTypesfullnames, methodName, parameterValues);
		}
	}

	public struct SampleStruct
	{
		public int sum;
		public int difference;
	}

	//Client side settings
	[XmlRpcUrl("http://fjh.dyndns.org:5678/sumAndDiff.rem")]
	//[XmlRpcUrl("http://www.cookcomputing.com/sumAndDiff.rem")]
	public interface ISampleServer_ClassWithFunctions : IXmlRpcProxy
	{
		[XmlRpcMethod("sumAndDifference")]
		SampleStruct SumAndDifference(int x, int y);
	}

	//Client side settings
	[XmlRpcUrl("http://localhost:5678/DynamicCodeInvoking.rem")]//")]fjh.dyndns.org:5678/DynamicCodeInvoking.rem")]
	public interface Iclientside_DynamicCodeInvokingServerClass : IXmlRpcProxy
	{
		[XmlRpcMethod("RunCodeDynamically")]
		string RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues);
	}
}