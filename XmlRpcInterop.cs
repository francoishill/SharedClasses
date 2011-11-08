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

	public static void SampleServer()
	{
		RemotingConfiguration.Configure("SumAndDiff.exe.config", false);
		// for CookComputing.XmlRpc
		//RemotingConfiguration.Configure("SumAndDiff.exe.config"); 
		RemotingConfiguration.RegisterWellKnownServiceType(
		typeof(SampleServer_ClassWithFunctions),
			"SumAndDiff.rem",
			WellKnownObjectMode.Singleton);
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

	public struct SampleStruct
	{
		public int sum;
		public int difference;
	}

	[XmlRpcUrl("http://fjh.dyndns.org:5678/sumAndDiff.rem")]
	//[XmlRpcUrl("http://www.cookcomputing.com/sumAndDiff.rem")]
	public interface ISampleServer_ClassWithFunctions : IXmlRpcProxy
	{
		[XmlRpcMethod("sumAndDifference")]
		SampleStruct SumAndDifference(int x, int y);
	}
}