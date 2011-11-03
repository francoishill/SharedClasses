using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

public class StartupbatInterop
{
	public static void PerformStartupbatCommand(string filePath, string comm, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		if (!File.Exists(filePath))
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "File not found: " + filePath);
			//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "File not found: " + filePath);
		else if (comm.StartsWith("open"))
		{
			System.Diagnostics.Process.Start("notepad", filePath);
		}
		else if (comm.StartsWith("getall"))
		{
			StreamReader sr = new StreamReader(filePath);
			string line = sr.ReadLine();
			int counter = 1;
			while (!sr.EndOfStream)
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, (counter++) + ": " + line);
				//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, (counter++) + ": " + line);
				line = sr.ReadLine();
			}
			sr.Close();
		}
		else if (comm.StartsWith("getline"))
		{
			if (comm.StartsWith("getline ") && comm.Length >= 9)
			{
				string searchstr = comm.Substring(8);//comm.Split('\'')[1];
				StreamReader sr = new StreamReader(filePath);
				string line = sr.ReadLine();
				int counter = 1;
				while (!sr.EndOfStream)
				{
					if (line.ToLower().Contains(searchstr)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, counter + ": " + line);
					counter++;
					line = sr.ReadLine();
				}
				sr.Close();
			}
			else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Getline search string not defined (must be i.e. getline skype): " + comm);
		}
		else if (comm.StartsWith("comment"))
		{
			string linenumstr = comm.Substring(7).Trim();
			int linenum;
			if (!int.TryParse(linenumstr, out linenum)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Cannot obtain line number from: " + comm.Substring(7));
			else
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Commenting line number " + linenum.ToString());
				List<string> tmpLines = new List<string>();
				StreamReader sr = new StreamReader(filePath);
				string line = sr.ReadLine();
				int counter = 1;
				while (!sr.EndOfStream)
				{
					if (counter == linenum && !line.Trim().StartsWith("::")) line = "::" + line;
					tmpLines.Add(line);
					counter++;
					line = sr.ReadLine();
				}
				sr.Close();
				StreamWriter sw = new StreamWriter(filePath);
				try
				{
					foreach (string s in tmpLines) sw.WriteLine(s);
				}
				finally { sw.Close(); }
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Successfully commented line number " + linenum.ToString());
			}
		}
		else if (comm.StartsWith("uncomment"))
		{
			string linenumstr = comm.Substring(9).Trim();
			int linenum;
			if (!int.TryParse(linenumstr, out linenum)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Cannot obtain line number from: " + comm.Substring(9));
			else
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Unommenting line number " + linenum.ToString());
				List<string> tmpLines = new List<string>();
				StreamReader sr = new StreamReader(filePath);
				string line = sr.ReadLine();
				int counter = 1;
				while (!sr.EndOfStream)
				{
					if (counter == linenum && line.Trim().StartsWith("::")) line = line.Substring(2);
					tmpLines.Add(line);
					counter++;
					line = sr.ReadLine();
				}
				sr.Close();
				StreamWriter sw = new StreamWriter(filePath);
				try
				{
					foreach (string s in tmpLines) sw.WriteLine(s);
				}
				finally { sw.Close(); }
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Successfully uncommented line number " + linenum.ToString());
			}
		}
	}
}