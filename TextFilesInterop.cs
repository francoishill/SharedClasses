using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MonitorSystem
{
	public class TextFilesInterop
	{
		public static List<string> GetLinesFromTextFile(string FullFilePath, Boolean ShowErrorMessage = true)
		{
			if (System.IO.File.Exists(FullFilePath))
			{
				try
				{
					List<string> tmpList = new List<string>();
					using (System.IO.StreamReader reader = new System.IO.StreamReader(FullFilePath))
					{
						string line;
						while ((line = reader.ReadLine()) != null) tmpList.Add(line);
						reader.Close();
					}
					return tmpList;
				}
				catch (Exception exc)
				{
					if (ShowErrorMessage) System.Windows.Forms.MessageBox.Show("Error occurred when reading file " + FullFilePath + Environment.NewLine + "Error: " + exc.Message, "File read error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
					return new List<string>();
				}
			}
			else
			{
				if (ShowErrorMessage) System.Windows.Forms.MessageBox.Show("The file does not exist: " + FullFilePath, "File not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return new List<string>();
			}
		}

		public static void WriteLinesToTextFile(string FullFilePath, List<string> LinesToWrite)
		{
			using (StreamWriter writer = new StreamWriter(FullFilePath))
			{
				foreach (string s in LinesToWrite)
					writer.WriteLine(s);
			}
		}
	}
}
