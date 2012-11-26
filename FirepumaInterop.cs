using System;

namespace SharedClasses
{
	public static class FirepumaInterop
	{
		public static string GetPrivateKey(Action<string> actionOnStatus, Action<string> actionOnError)
		{
			if (actionOnStatus == null) actionOnStatus = delegate { };
			if (actionOnError == null) actionOnError = delegate { };

			try
			{
				actionOnStatus("Obtaining pvt key...");
				string tmpkey = null;

				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					string tmpResult;
					if (WebInterop.PostPHP(PhpInterop.ServerAddress + "/generateprivatekey.php", "username=" + PhpInterop.Username + "&password=" + PhpInterop.Password, out tmpResult))
						tmpkey = tmpResult;
					else
						actionOnStatus(tmpResult);
				});

				string tmpSuccessKeyString = "Success: Key=";
				if (tmpkey != null && tmpkey.Length > 0 && tmpkey.ToUpper().StartsWith(tmpSuccessKeyString.ToUpper()))
				{
					tmpkey = tmpkey.Substring(tmpSuccessKeyString.Length).Replace("\n", "").Replace("\r", "");
					actionOnStatus(tmpkey);
				}
				return tmpkey;
			}
			catch (Exception exc)
			{
				actionOnError("Obtain private key exception: " + exc.Message);
				return null;
			}
		}
	}
}