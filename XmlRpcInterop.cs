using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

		//string[] TypeStringArray;
		//object[] ParameterList;
		//DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, "Hallo", "Temp title", true);
		//DynamicCodeInvoking.RunCodeReturnStruct resultObj = proxy.RunCodeDynamically(
		//	typeof(UserMessages).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
		//	TypeStringArray,
		//	"ShowMessage",//"Show",
		//	ParameterList
		//	);
		//XmlRpcClientProtocol cp = new XmlRpcClientProtocol();
		//cp.Invoke(mi: typeof(Directory).GetMethod("GetFiles"), Parameters: new object[] { @"c:\", "*", SearchOption.TopDirectoryOnly });
		//return;
		//cannot be mapped to an XML-RPC type

		string[] TypeStringArray;
		object[] ParameterList;
		DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, @"c:\", "*", SearchOption.TopDirectoryOnly);
		DynamicCodeInvoking.RunCodeReturnStruct resultObj = proxy.RunCodeDynamically(
			typeof(Directory).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
			TypeStringArray,
			"GetFiles",//"Show",
			ParameterList
			);

		//string[] TypeStringArray;
		//object[] ParameterList;
		//DynamicCodeInvoking.GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, @"c:\111");
		//string retError = proxy.RunCodeDynamically(
		//	typeof(Directory).AssemblyQualifiedName,//typeof(MessageBox).AssemblyQualifiedName
		//	TypeStringArray,
		//	"CreateDirectory",//"Show",
		//	ParameterList
		//	);
		if (resultObj.Success)
		{
			string successMsg = "Successfully performed command: ";
			if (resultObj.MethodInvokeResultingObject is string[])
				foreach (string s in resultObj.MethodInvokeResultingObject as string[])
					successMsg += Environment.NewLine + s;
			else if (resultObj.MethodInvokeResultingObject is List<string>)
				foreach (string s in resultObj.MethodInvokeResultingObject as List<string>)
					successMsg += Environment.NewLine + s;
			else successMsg += resultObj.ToString();
			UserMessages.ShowInfoMessage(successMsg);
		}
		else
			UserMessages.ShowErrorMessage("Return error string: " + resultObj.ErrorMessage);
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
		public DynamicCodeInvoking.RunCodeReturnStruct RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues)
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
		DynamicCodeInvoking.RunCodeReturnStruct RunCodeDynamically(string AssemblyQualifiedNameOfType, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues);
	}
}