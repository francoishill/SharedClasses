using System;
using System.Linq;
using System.IO;

namespace SharedClasses
{
	public static class CommandlineArgumentsInterop
	{
		private static bool GetCommandAndArgumentsFromString_DoNotValidate(string originalString, out string command_orErrorIfFailed, out string arguments)
		{
			if (string.IsNullOrWhiteSpace(originalString) || originalString.Length < 3)
			{
				command_orErrorIfFailed = "Original string IsNullOrWhiteSpace OR length < 3: " + originalString;
				arguments = null;
				return false;
			}

			originalString = originalString.Trim(' ');//Just remove the spaces at beginning and end

			if (!originalString.StartsWith("\"")
				&& !originalString.StartsWith("'")
				&& !originalString.Contains(' '))
			{
				command_orErrorIfFailed = originalString;
				arguments = null;
				return true;
			}

			if (originalString.StartsWith("\"") || originalString.StartsWith("'"))
			{//Possible to be: "C:\Windows\calc.exe" | 'C:\Windows\calc.exe' | "C:\Program files (x86)\myapp.exe"
				char quoteChar = originalString[0];
				int indexOfClosingQuote =originalString.IndexOf(quoteChar, 1);
				if (indexOfClosingQuote == -1)//We did not find a closing quote, invalid commandline
				{
					command_orErrorIfFailed = "Invalid commandline, there is not a closing quote for the opening quote as the first character: " + originalString;
					arguments = null;
					return false;
				}

				if (indexOfClosingQuote == originalString.Length - 1)//The closing (second) quote is the last character so we do not have commandline-arguments
				{
					command_orErrorIfFailed =
						originalString
						.Trim(quoteChar);
					arguments = null;
					return true;
				}
				else if (originalString[indexOfClosingQuote + 1] == ' ')//We have a space after the closing quote (as we expected to have)
				{
					command_orErrorIfFailed =
						originalString
						.Substring(0, indexOfClosingQuote + 1)
						.Trim(quoteChar);
					arguments =
						originalString
						.Substring(indexOfClosingQuote + 2);//We do not trim quote characters for arguments
					return true;
				}
				else//if (originalString[indexOfClosingQuote + 1] != ' ')// WE do not have a space after our quote, assume it's invalid
				{
					command_orErrorIfFailed = "Invalid commandline, the character after the second quote is expected to be a space: " + originalString;
					arguments = null;
					return false;
				}
			}
			else if (originalString.Contains(' '))//Because our command is not wrapped in quotes, we will see everything after the first space as the arguments
			{//Possible to be: C:\Windows\calc.exe --scientific | C:\Windows\notepad.exe "c:\program files\my text file.txt" --arg2
				int indexOfSpace = originalString.IndexOf(' ');
				command_orErrorIfFailed = originalString.Substring(0, indexOfSpace);
				arguments = originalString.Substring(indexOfSpace + 1);
				return true;
			}
			else//We do not have quotes nor a space character, so the string is the command only (no arguments)
			{//Possible to be C:\Windows\calc.exe
				command_orErrorIfFailed = originalString;
				arguments = null;
				return true;
			}
		}

		public static bool GetCommandAndArgumentsFromString(string originalString, out string command_orErrorIfFailed, out string arguments)
		{
			if (!GetCommandAndArgumentsFromString_DoNotValidate(originalString, out command_orErrorIfFailed, out arguments))
				return false;

			//We just validate to make sure we have an existing file/directory as the 'command'
			bool isValid = 
				File.Exists(Environment.ExpandEnvironmentVariables(command_orErrorIfFailed))
				|| Directory.Exists(Environment.ExpandEnvironmentVariables(command_orErrorIfFailed));
			if (!isValid)
				command_orErrorIfFailed = "Invalid file/directory (does not exist): " + command_orErrorIfFailed;
			return isValid;
		}
	}
}