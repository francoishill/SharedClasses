using System;
using System.Diagnostics;

namespace SharedClasses
{
	public static class DeveloperCommunication
	{
		public static void RunMailto(string subject = null, string body = null)
		{
			Process.Start(string.Format("mailto:developer@firepuma.com?subject={0}&body={1}",
				subject,
				body));
		}
	}
}