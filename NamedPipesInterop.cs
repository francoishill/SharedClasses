using System;
using System.Text;
using System.IO.Pipes;
using System.Collections.Generic;

namespace SharedClasses
{
	public enum PipeMessageTypes { ClientRegistrationRequest, AcknowledgeClientRegistration };

	public class NamedPipesInterop
	{
		public const string APPMANAGER_PIPE_NAME = "Application Manager Pipe Name";
	}

	public static class NamedPipesExtensions
	{
		public static Encoding encoding = Encoding.UTF8;
		private static Decoder decoder = encoding.GetDecoder();

		public static bool ReadMessage(this PipeStream pipeStream, out string messageOrErrorOut)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				try
				{
					byte[] bytes = new byte[1024];
					int numread = pipeStream.Read(bytes, 0, 1024);
					sb.Append(encoding.GetString(bytes, 0, numread));
				}
				catch (Exception exc)
				{
					Console.WriteLine("Exception reading message: " + exc.Message);
					messageOrErrorOut = exc.Message;
					return false;
				}
			}
			while (!pipeStream.IsMessageComplete);
			messageOrErrorOut = sb.ToString();
			return true;
		}

		public static bool WriteMessage(this PipeStream pipeStream, string message, out string errorMessage)
		{
			try
			{
				byte[] bytes = encoding.GetBytes(message);
				pipeStream.Write(bytes, 0, bytes.Length);
				pipeStream.Flush();
				pipeStream.WaitForPipeDrain();
				errorMessage = null;
				return true;
			}
			catch (Exception exc)
			{
				errorMessage = exc.Message;
				return false;
			}
		}

		public static bool WriteMessage(this PipeStream pipeStream, string message)
		{
			string tmpstr;
			return pipeStream.WriteMessage(message, out tmpstr);
		}

		//public static IEnumerable<string> GetMessages(
		//    this PipeStream pipeStream)//NamedPipeClientStream pipeStream)
		//{
		//    const int BufferSize = 256;
		//    byte[] bytes = new byte[BufferSize];
		//    char[] chars = new char[BufferSize];
		//    int numBytes = 0;
		//    StringBuilder msg = new StringBuilder();
		//    do
		//    {
		//        msg.Length = 0;
		//        do
		//        {
		//            numBytes = pipeStream.Read(bytes, 0, BufferSize);
		//            if (numBytes > 0)
		//            {
		//                int numChars = decoder.GetCharCount(bytes, 0, numBytes);
		//                decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
		//                msg.Append(chars, 0, numChars);
		//            }
		//        } while (numBytes > 0 && !pipeStream.IsMessageComplete);
		//        decoder.Reset();
		//        if (numBytes > 0)
		//        {
		//            // we've got a message - yield it!
		//            yield return msg.ToString();
		//        }
		//    } while (numBytes != 0);
		//}
	}
}