using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

public class StartupbatInterop
{
	public static void PerformStartupbatCommand(TextBox messagesTextbox, string filePath, string comm)
	{
		if (!File.Exists(filePath))
			Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "File not found: " + filePath);
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
				Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, (counter++) + ": " + line);
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
					if (line.ToLower().Contains(searchstr)) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, counter + ": " + line);
					counter++;
					line = sr.ReadLine();
				}
				sr.Close();
			}
			else Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Getline search string not defined (must be i.e. getline skype): " + comm);
		}
		else if (comm.StartsWith("comment"))
		{
			string linenumstr = comm.Substring(7).Trim();
			int linenum;
			if (!int.TryParse(linenumstr, out linenum)) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Cannot obtain line number from: " + comm.Substring(7));
			else
			{
				Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Commenting line number " + linenum.ToString());
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
				Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Successfully commented line number " + linenum.ToString());
			}
		}
		else if (comm.StartsWith("uncomment"))
		{
			string linenumstr = comm.Substring(9).Trim();
			int linenum;
			if (!int.TryParse(linenumstr, out linenum)) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Cannot obtain line number from: " + comm.Substring(9));
			else
			{
				Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Unommenting line number " + linenum.ToString());
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
				Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Successfully uncommented line number " + linenum.ToString());
			}
		}
	}
}