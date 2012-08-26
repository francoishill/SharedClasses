using System;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class WinMMinterop
	{
		#region
		private const int BufferLength = 256;
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct WaveInCaps
		{
			public short wMid;
			public short wPid;
			public int vDriverVersion;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public char[] szPname;
			public uint dwFormats;
			public short wChannels;
			public short wReserved1;
		}

		[DllImport("winmm.dll")]
		public static extern int mciSendString(string lpszCommand, string lpszReturnString, uint cchReturn, IntPtr hwndCallback);
		[DllImport("winmm.dll", CharSet = CharSet.Ansi, BestFitMapping = true, ThrowOnUnmappableChar = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool mciGetErrorString(uint mcierr, [MarshalAs(UnmanagedType.LPStr)]System.Text.StringBuilder pszText, uint cchText);
		[DllImport("winmm.dll")]
		public static extern int waveInGetNumDevs();//return total Sound Recording Devices
		[DllImport("winmm.dll", EntryPoint = "waveInGetDevCaps")]
		public static extern int waveInGetDevCapsA(int uDeviceID, ref WaveInCaps lpCaps, int uSize);//return spesific Sound Recording Devices spec
		#endregion

		public static int GetRecordingDeviceCount()
		{
			return waveInGetNumDevs();
		}
		public static List<string> GetRecordingDevicesNames()
		{
			List<string> tmplist = new List<string>();
			int waveInDevicesCount = waveInGetNumDevs(); //get total
			if (waveInDevicesCount > 0)
			{
				for (int uDeviceID = 0; uDeviceID < waveInDevicesCount; uDeviceID++)
				{
					WaveInCaps waveInCaps = new WaveInCaps();
					waveInGetDevCapsA(uDeviceID, ref waveInCaps,
									  Marshal.SizeOf(typeof(WaveInCaps)));
					string deviceName = new string(waveInCaps.szPname).Remove(new string(waveInCaps.szPname).IndexOf('\0')).Trim();
					tmplist.Add(deviceName);
					//clean garbage
				}
			}
			return tmplist;
		}

		public static bool mciExecute(string s_cmd, ref Action<string> actionOnError, string returnString = null, uint cchReturn = 0, IntPtr? hwndCallback = null)
		{
			int err = mciSendString(s_cmd, returnString, cchReturn, hwndCallback.HasValue ? hwndCallback.Value : IntPtr.Zero);
			if (err != 0)
			{
				if (actionOnError != null)
					actionOnError(GetError((uint)err));
				return false;
			}
			else
				return true;
		}

		private static string GetError(uint errCode)
		{
			StringBuilder str = new StringBuilder(BufferLength);
			mciGetErrorString(errCode, str, BufferLength);
			return str.ToString();
		}

		public class Recorder
		{
			#region Enums
			/// <summary>
			/// Below 1-6 has a lot of noise, from 9-12 almost sounds the same
			/// 9 is suggested quality, space saving but good quality
			/// </summary>
			public enum Qualities
			{
				[Description("alignment 4 bitspersample 16 samplespersec 44100 channels 2 bytespersec 176400")]
				_12_Bytespersec176400_Alignment4_Bitspersample16_Samplespersec44100_Channels2,//MB/hour = 660
				[Description("alignment 2 bitspersample 16 samplespersec 44100 channels 1 bytespersec 88200")]
				_11_Bytespersec88200_Alignment2_Bitspersample16_Samplespersec44100_Channels1,//MB/hour = 303
				[Description("alignment 4 bitspersample 16 samplespersec 22050 channels 2 bytespersec 88200")]
				_10_Bytespersec88200_Alignment4_Bitspersample16_Samplespersec22050_Channels2,//MB/hour = 303
				[Description("alignment 2 bitspersample 16 samplespersec 22050 channels 1 bytespersec 44100")]
				_09_Bytespersec44100_Alignment2_Bitspersample16_Samplespersec22050_Channels1,//MB/hour = 151
				[Description("alignment 4 bitspersample 16 samplespersec 11025 channels 2 bytespersec 44100")]
				_08_Bytespersec44100_Alignment4_Bitspersample16_Samplespersec11025_Channels2,//MB/hour = 151
				[Description("alignment 2 bitspersample 16 samplespersec 11025 channels 1 bytespersec 22050")]
				_07_Bytespersec22050_Alignment2_Bitspersample16_Samplespersec11025_Channels1,//MB/hour = 76
				[Description("alignment 2 bitspersample 8 samplespersec 11025 channels 2 bytespersec 22050")]
				_06_Bytespersec22050_Alignment2_Bitspersample8_Samplespersec11025_Channels2,//MB/hour = 76
				[Description("alignment 2 bitspersample 8 samplespersec 8000 channels 2 bytespersec 16000")]
				_05_Bytespersec16000_Alignment2_Bitspersample8_Samplespersec8000_Channels2,//MB/hour = 55
				[Description("alignment 2 bitspersample 8 samplespersec 6000 channels 2 bytespersec 12000")]
				_04_Bytespersec12000_Alignment2_Bitspersample8_Samplespersec6000_Channels2,//MB/hour = 41
				[Description("alignment 1 bitspersample 8 samplespersec 11025 channels 1 bytespersec 11025")]
				_03_Bytespersec11025_Alignment1_Bitspersample8_Samplespersec11025_Channels1,//MB/hour = 38
				[Description("alignment 1 bitspersample 8 samplespersec 8000 channels 1 bytespersec 8000")]
				_02_Bytespersec8000_Alignment1_Bitspersample8_Samplespersec8000_Channels1,//MB/hour = 27
				[Description("alignment 1 bitspersample 8 samplespersec 6000 channels 1 bytespersec 6000")]
				_01_Bytespersec6000_Alignment1_Bitspersample8_Samplespersec6000_Channels1//MB/hour = 21
			};
			#endregion Enums


			private static Recorder _instance;
			public static Recorder Instance { get { if (_instance == null) _instance = new Recorder(null); return _instance; } }

			private string guidID;
			private Action<string> actionOnError;
			private Qualities Quality;
			//private int DeviceNumber = 0;
			private bool isOpen = false;

			public Recorder(Action<string> actionOnError, Qualities Quality = Qualities._09_Bytespersec44100_Alignment2_Bitspersample16_Samplespersec22050_Channels1, bool OpenImmediately = true)//, uint bitsPerSample = 16, uint samplesPerSecond = 44100)
			{
				this.actionOnError = actionOnError;
				this.Quality = Quality;
				this.guidID = Guid.NewGuid().ToString();
				if (OpenImmediately)
					OpenNow();
			}
			private void OpenNow()
			{
				mciExecute("open new type waveaudio alias " + guidID + " wait", ref actionOnError);
				isOpen = true;

				mciExecute(
					string.Format("set {0} {1}",
					guidID,
					Quality.GetEnumDescription()),
					ref actionOnError);
			}
			public void StartRecording()
			{
				if (!isOpen)
					OpenNow();
				mciExecute("record " + guidID, ref actionOnError);
			}
			public void SetAction(Action<string> actionOnError)
			{
				this.actionOnError = actionOnError;
			}
			//public void SetDeviceNumberToUse(int newDeviceID)
			//{
			//    this.DeviceNumber = newDeviceID;
			//}
			public void SetQuality(Qualities newQuality)
			{
				this.Quality = newQuality;
			}
			public void StopAndSave(string filepath)
			{
				mciExecute("stop " + guidID + " wait", ref actionOnError);
				mciExecute("save " + guidID + " \"" + filepath + "\" wait", ref actionOnError);
				mciExecute("delete " + guidID + " wait", ref actionOnError);
				mciExecute("close " + guidID + " wait", ref actionOnError);
				isOpen = false;
			}
		}

		public static class PlayFunctions
		{
			public static void OpenCDdoor(ref Action<string> actionOnError)
			{
				WinMMinterop.mciExecute("set CDAudio door open", ref actionOnError, "", 127, IntPtr.Zero);
			}

			public static void CloseCDdoor(ref Action<string> actionOnError)
			{
				WinMMinterop.mciExecute("set CDAudio door closed", ref actionOnError, "", 127, IntPtr.Zero);
			}
		}
	}
}