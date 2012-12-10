using System;
using System.Diagnostics;

namespace SharedClasses
{
	public static class DeveloperCommunication
	{
		public static void RunMailto()
		{
			Process.Start("mailto:developer@firepuma.com");
		}
	}
}