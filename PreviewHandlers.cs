///Notes:
///It is very important with preview handlers to have their own DLL
///CS project must be compiled with unsafe
/// 
///To register, this DLL has the checkbox ticked "Register for COM interop"
///in Visual Studio "Build" settings

using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Wayloop.Highlight.Engines;
using Wayloop.Highlight;

namespace SharedClasses
{
	//public static class PreviewHandlers
	//{
	//    public static void RegisterThisDllPreviewHandlers()
	//    {
	//        var asm = Assembly.GetExecutingAssembly();
	//        string dllToRegister = asm.Location;

	//        var rootFrameworkDir = Directory.GetDirectories(@"c:\Windows\Microsoft.NET\Framework");
	//        var v4paths = rootFrameworkDir.Where(p => Path.GetFileName(p).StartsWith("v4.0.", StringComparison.InvariantCultureIgnoreCase)).ToArray();
	//        if (v4paths.Length > 0)
	//        {
	//            string RegAsmPath = Path.Combine(v4paths[0], "Regasm.exe");
	//            if (File.Exists(RegAsmPath))
	//            {
	//                List<string> outputs;
	//                List<string> errors;
	//                bool? runresult = ProcessesInterop.RunProcessCatchOutput(
	//                    new ProcessStartInfo(RegAsmPath,
	//                    //string.Format("\"{0}\" /codebase", dllToRegister)),
	//                    string.Format("\"{0}\" /unregister", dllToRegister)),
	//                    out outputs,
	//                    out errors);
	//                if (!runresult.HasValue || runresult.Value == false)
	//                    MessageBox.Show("Could not complete the registration: " + Environment.NewLine
	//                        + "ERRORS: " + string.Join(Environment.NewLine, errors) + "OUTPUTS: " + string.Join(Environment.NewLine, outputs), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
	//            }
	//            else
	//            {
	//                //What should happen here if v4.0. folder not found?,
	//                //It is required for RegAsm which registers the Preview Handlers in windows vista/7
	//            }
	//        }
	//        else
	//        {
	//            //What should happen here if v4.0. folder not found?,
	//            //It is required for RegAsm which registers the Preview Handlers in windows vista/7
	//        }
	//    }
	//}

	[PreviewHandler("Fjset Preview Handler", ".fjset", "{25601B81-7437-4201-A2C3-15FE4DDB232C}")]
	[ProgId("FJH.FjsetPreviewHandler")]
	[Guid("FB4D7EC7-E0E3-4625-B115-4248BA852640")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public sealed class FjsetPreviewHandler : StreamBasedPreviewHandler
	{
		protected override PreviewHandlerControl CreatePreviewHandlerControl()
		{
			return new CodePreviewHandlerControl("FJSET");
		}
	}

	[PreviewHandler("Json Preview Handler", ".json", "{CFCDCD72-26DE-4F6F-B6B1-C6ABA8BC5BD0}")]
	[ProgId("FJH.JsonPreviewHandler")]
	[Guid("D9B2E5DA-89B8-4F20-95DE-274F593FAA7D")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public sealed class JsonPreviewHandler : StreamBasedPreviewHandler
	{
		protected override PreviewHandlerControl CreatePreviewHandlerControl()
		{
			return new CodePreviewHandlerControl("JSON");
		}
	}

	#region RequiredClassesForPreviewHandlers

	public class CodePreviewHandlerControl : StreamBasedPreviewHandlerControl
	{
		readonly string definition = string.Empty;

		public CodePreviewHandlerControl(string definition)
		{
			this.definition = definition;
		}

		public override void Load(Stream stream)
		{
			string returnedHtml = null;

			using (var reader = new StreamReader(stream))
			{
				if (this.definition.Equals("FJSET", StringComparison.InvariantCultureIgnoreCase))
					returnedHtml = reader.ReadToEnd();//"<label style='color:orange'>This is a FJSET file 123</label>";
				else if (this.definition.Equals("JSON", StringComparison.InvariantCultureIgnoreCase))
				{
					JSON.SetDefaultJsonInstanceSettings();
					string jsontext = JSON.Instance.Beautify(reader.ReadToEnd());
					returnedHtml = HighlightHelpers.GetHighlightedHtml("JavaScript", jsontext);
				}
				else
					returnedHtml = "Unsupported in preview handler";//HighlightHelpers.GetHighlightedHtml(definition, stream);
			}

			var webBrowser = new WebBrowser
			{
				Dock = DockStyle.Fill,
				DocumentText = returnedHtml
			};
			Controls.Add(webBrowser);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class PreviewHandlerAttribute : Attribute
	{
		readonly string name;
		readonly string extension;
		readonly string appId;

		public PreviewHandlerAttribute(string name, string extension, string appId)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (extension == null) throw new ArgumentNullException("extension");
			if (appId == null) throw new ArgumentNullException("appId");
			this.name = name;
			this.extension = extension;
			this.appId = appId;
		}

		public string Name { get { return name; } }
		public string Extension { get { return extension; } }
		public string AppId { get { return appId; } }
	}

	public abstract class PreviewHandler : IPreviewHandler, IPreviewHandlerVisuals, IOleWindow, IObjectWithSite
	{
		bool showPreview;
		readonly PreviewHandlerControl previewControl;
		IntPtr parentHwnd;
		Rectangle windowBounds;
		object unkSite;
		IPreviewHandlerFrame frame;

		protected PreviewHandler()
		{
			previewControl = CreatePreviewHandlerControl(); // NOTE: shouldn't call virtual function from constructor; see article for more information
			previewControl.Handle.GetHashCode();
			previewControl.BackColor = SystemColors.Window;
		}

		protected abstract PreviewHandlerControl CreatePreviewHandlerControl();

		private void InvokeOnPreviewThread(MethodInvoker d)
		{
			previewControl.Invoke(d);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		private void UpdateWindowBounds()
		{
			if (!showPreview) return;

			InvokeOnPreviewThread(delegate
			{
				SetParent(previewControl.Handle, parentHwnd);
				previewControl.Bounds = windowBounds;
				previewControl.Visible = true;
			});
		}

		void IPreviewHandler.SetWindow(IntPtr hwnd, ref RECT rect)
		{
			parentHwnd = hwnd;
			windowBounds = rect.ToRectangle();
			UpdateWindowBounds();
		}

		void IPreviewHandler.SetRect(ref RECT rect)
		{
			windowBounds = rect.ToRectangle();
			UpdateWindowBounds();
		}

		protected abstract void Load(PreviewHandlerControl c);

		void IPreviewHandler.DoPreview()
		{
			showPreview = true;
			InvokeOnPreviewThread(delegate
			{
				try
				{
					Load(previewControl);
				}
				catch (Exception exc)
				{
					previewControl.Controls.Clear();
					var text = new TextBox
					{
						ReadOnly = true,
						Multiline = true,
						Dock = DockStyle.Fill,
						Text = exc.ToString()
					};
					previewControl.Controls.Add(text);
				}
				UpdateWindowBounds();
			});
		}

		void IPreviewHandler.Unload()
		{
			showPreview = false;
			InvokeOnPreviewThread(delegate
			{
				previewControl.Visible = false;
				previewControl.Unload();
			});
		}

		void IPreviewHandler.SetFocus()
		{
			InvokeOnPreviewThread(() => previewControl.Focus());
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetFocus();

		void IPreviewHandler.QueryFocus(out IntPtr phwnd)
		{
			var result = IntPtr.Zero;
			InvokeOnPreviewThread(delegate { result = GetFocus(); });
			phwnd = result;
			if (phwnd == IntPtr.Zero) throw new Win32Exception();
		}

		uint IPreviewHandler.TranslateAccelerator(ref MSG pmsg)
		{
			if (frame != null) return frame.TranslateAccelerator(ref pmsg);
			const uint S_FALSE = 1;
			return S_FALSE;
		}

		void IPreviewHandlerVisuals.SetBackgroundColor(COLORREF color)
		{
			var c = color.Color;
			InvokeOnPreviewThread(delegate { previewControl.BackColor = c; });
		}

		void IPreviewHandlerVisuals.SetTextColor(COLORREF color)
		{
			var c = color.Color;
			InvokeOnPreviewThread(delegate { previewControl.ForeColor = c; });
		}

		void IPreviewHandlerVisuals.SetFont(ref LOGFONT plf)
		{
			var f = Font.FromLogFont(plf);
			InvokeOnPreviewThread(delegate { previewControl.Font = f; });
		}

		void IOleWindow.GetWindow(out IntPtr phwnd)
		{
			phwnd = previewControl.Handle;
		}

		void IOleWindow.ContextSensitiveHelp(bool fEnterMode)
		{
			throw new NotImplementedException();
		}

		void IObjectWithSite.SetSite(object pUnkSite)
		{
			unkSite = pUnkSite;
			frame = unkSite as IPreviewHandlerFrame;
		}

		void IObjectWithSite.GetSite(ref Guid riid, out object ppvSite)
		{
			ppvSite = unkSite;
		}

		[ComRegisterFunction]
		public static void Register(Type t)
		{
			if (t == null) return;
			if (!t.IsSubclassOf(typeof(PreviewHandler))) return;

			var attrs = t.GetCustomAttributes(typeof(PreviewHandlerAttribute), true);
			if (attrs.Length != 1) return;

			var attr = (PreviewHandlerAttribute)attrs[0];
			RegisterPreviewHandler(attr.Name, attr.Extension, t.GUID.ToString("B"), attr.AppId);
		}

		[ComUnregisterFunction]
		public static void Unregister(Type t)
		{
			if (t == null) return;
			if (!t.IsSubclassOf(typeof(PreviewHandler))) return;

			var attrs = t.GetCustomAttributes(typeof(PreviewHandlerAttribute), true);
			if (attrs.Length != 1) return;

			var attr = (PreviewHandlerAttribute)attrs[0];
			UnregisterPreviewHandler(attr.Extension, t.GUID.ToString("B"), attr.AppId);
		}

		protected static void RegisterPreviewHandler(string name, string extensions, string previewerGuid, string appId)
		{
			// Create a new prevhost AppID so that this always runs in its own isolated process
			using (var appIdsKey = Registry.ClassesRoot.OpenSubKey("AppID", true))
			using (var appIdKey = appIdsKey.CreateSubKey(appId))
			{
				appIdKey.SetValue("DllSurrogate", @"%SystemRoot%\system32\prevhost.exe", RegistryValueKind.ExpandString);
			}

			// Add preview handler to preview handler list
			using (var handlersKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers", true))
			{
				handlersKey.SetValue(previewerGuid, name, RegistryValueKind.String);
			}

			// Modify preview handler registration
			using (var clsidKey = Registry.ClassesRoot.OpenSubKey("CLSID"))
			using (var idKey = clsidKey.OpenSubKey(previewerGuid, true))
			{
				idKey.SetValue("DisplayName", name, RegistryValueKind.String);
				idKey.SetValue("AppID", appId, RegistryValueKind.String);
				//idKey.SetValue("DisableLowILProcessIsolation", 1, RegistryValueKind.DWord);
			}

			foreach (var extension in extensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				Trace.WriteLine("Registering extension '" + extension + "' with previewer '" + previewerGuid + "'");

				// Set preview handler for specific extension
				using (var extensionKey = Registry.ClassesRoot.CreateSubKey(extension))
				using (var shellexKey = extensionKey.CreateSubKey("shellex"))
				using (var previewKey = shellexKey.CreateSubKey("{8895b1c6-b41f-4c1c-a562-0d564250836f}"))
				{
					previewKey.SetValue(null, previewerGuid, RegistryValueKind.String);
				}
			}
		}

		protected static void UnregisterPreviewHandler(string extensions, string previewerGuid, string appId)
		{
			foreach (var extension in extensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				Trace.WriteLine("Unregistering extension '" + extension + "' with previewer '" + previewerGuid + "'");
				using (var shellexKey = Registry.ClassesRoot.OpenSubKey(extension + "\\shellex", true))
				{
					try { shellexKey.DeleteSubKey("{8895b1c6-b41f-4c1c-a562-0d564250836f}"); }
					catch { }
				}
			}

			using (var appIdsKey = Registry.ClassesRoot.OpenSubKey("AppID", true))
			{
				try { appIdsKey.DeleteSubKey(appId); }
				catch { }
			}

			using (var classesKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers", true))
			{
				try { classesKey.DeleteValue(previewerGuid); }
				catch { }
			}
		}
	}

	public abstract class PreviewHandlerControl : Form
	{
		protected PreviewHandlerControl()
		{
			FormBorderStyle = FormBorderStyle.None;
		}

		public new abstract void Load(FileInfo file);
		public new abstract void Load(Stream stream);

		public virtual void Unload()
		{
			foreach (Control c in Controls) c.Dispose();
			Controls.Clear();
		}

		protected static string CreateTempPath(string extension)
		{
			return Path.GetTempPath() + Guid.NewGuid().ToString("N") + extension;
		}
	}

	public abstract class StreamBasedPreviewHandlerControl : PreviewHandlerControl
	{
		public sealed override void Load(FileInfo file)
		{
			using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
				Load(fs);
		}
	}

	public abstract class StreamBasedPreviewHandler : PreviewHandler, IInitializeWithStream
	{
		private IStream stream;

		void IInitializeWithStream.Initialize(IStream pstream, uint grfMode)
		{
			stream = pstream;
		}

		protected override void Load(PreviewHandlerControl c)
		{
			c.Load(new ReadOnlyIStreamStream(stream));
		}

		private class ReadOnlyIStreamStream : Stream
		{
			IStream stream;

			public ReadOnlyIStreamStream(IStream stream)
			{
				if (stream == null) throw new ArgumentNullException("stream");
				this.stream = stream;
			}

			protected override void Dispose(bool disposing)
			{
				stream = null;
				base.Dispose(disposing);
			}

			private void ThrowIfDisposed() { if (stream == null) throw new ObjectDisposedException(GetType().Name); }

			public unsafe override int Read(byte[] buffer, int offset, int count)
			{
				ThrowIfDisposed();

				if (buffer == null) throw new ArgumentNullException("buffer");
				if (offset < 0) throw new ArgumentNullException("offset");
				if (count < 0) throw new ArgumentNullException("count");

				var bytesRead = 0;
				if (count > 0)
				{
					var ptr = new IntPtr(&bytesRead);
					if (offset == 0)
					{
						if (count > buffer.Length) throw new ArgumentOutOfRangeException("count");
						stream.Read(buffer, count, ptr);
					}
					else
					{
						var tempBuffer = new byte[count];
						stream.Read(tempBuffer, count, ptr);
						if (bytesRead > 0) Array.Copy(tempBuffer, 0, buffer, offset, bytesRead);
					}
				}
				return bytesRead;
			}

			public override bool CanRead { get { return stream != null; } }
			public override bool CanSeek { get { return stream != null; } }
			public override bool CanWrite { get { return false; } }

			public override long Length
			{
				get
				{
					ThrowIfDisposed();
					const int STATFLAG_NONAME = 1;
					System.Runtime.InteropServices.ComTypes.STATSTG stats;
					stream.Stat(out stats, STATFLAG_NONAME);
					return stats.cbSize;
				}
			}

			public override long Position
			{
				get
				{
					ThrowIfDisposed();
					return Seek(0, SeekOrigin.Current);
				}
				set
				{
					ThrowIfDisposed();
					Seek(value, SeekOrigin.Begin);
				}
			}

			public override unsafe long Seek(long offset, SeekOrigin origin)
			{
				ThrowIfDisposed();
				long pos = 0;
				var posPtr = new IntPtr(&pos);
				stream.Seek(offset, (int)origin, posPtr);
				return pos;
			}

			public override void Flush() { ThrowIfDisposed(); }

			public override void SetLength(long value)
			{
				ThrowIfDisposed();
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				ThrowIfDisposed();
				throw new NotSupportedException();
			}
		}
	}

	public static class HighlightHelpers
	{
		public static string GetHighlightedHtml(string definition, Stream stream)
		{
			//Read the source code in
			string sourceText;
			using (var reader = new StreamReader(stream))
				sourceText = reader.ReadToEnd();

			return GetHighlightedHtml(definition, sourceText);
		}

		public static string GetHighlightedHtml(string definition, string source)
		{
			var engine = new HtmlEngine();
			var highlighter = new Highlighter(engine);
			var sourceHtml = highlighter.Highlight(definition, source);

			sourceHtml = string.Format("<pre>{0}</pre>", sourceHtml);

			return sourceHtml;
		}
	}

#region ComInterop
	[StructLayout(LayoutKind.Sequential)]
	internal struct COLORREF
	{
		public uint Dword;
		public Color Color
		{
			get
			{
				return Color.FromArgb(
				(int)(0x000000FFU & Dword),
				(int)(0x0000FF00U & Dword) >> 8,
				(int)(0x00FF0000U & Dword) >> 16);
			}
		}
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
	interface IInitializeWithFile
	{
		void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
	interface IInitializeWithStream
	{
		void Initialize(IStream pstream, uint grfMode);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("fc4801a3-2ba9-11cf-a229-00aa003d7352")]
	public interface IObjectWithSite
	{
		void SetSite([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkSite);
		void GetSite(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvSite);
	}

	[ComImport]
	[Guid("00000114-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IOleWindow
	{
		void GetWindow(out IntPtr phwnd);
		void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
	interface IPreviewHandler
	{
		void SetWindow(IntPtr hwnd, ref RECT rect);
		void SetRect(ref RECT rect);
		void DoPreview();
		void Unload();
		void SetFocus();
		void QueryFocus(out IntPtr phwnd);
		[PreserveSig]
		uint TranslateAccelerator(ref MSG pmsg);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("fec87aaf-35f9-447a-adb7-20234491401a")]
	interface IPreviewHandlerFrame
	{
		void GetWindowContext(IntPtr pinfo);
		[PreserveSig]
		uint TranslateAccelerator(ref MSG pmsg);
	};

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("8327b13c-b63f-4b24-9b8a-d010dcc3f599")]
	interface IPreviewHandlerVisuals
	{
		void SetBackgroundColor(COLORREF color);
		void SetFont(ref LOGFONT plf);
		void SetTextColor(COLORREF color);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal class LOGFONT
	{
		public int lfHeight;
		public int lfWidth;
		public int lfEscapement;
		public int lfOrientation;
		public int lfWeight;
		public byte lfItalic;
		public byte lfUnderline;
		public byte lfStrikeOut;
		public byte lfCharSet;
		public byte lfOutPrecision;
		public byte lfClipPrecision;
		public byte lfQuality;
		public byte lfPitchAndFamily;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName = string.Empty;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MSG
	{
		public IntPtr hwnd;
		public int message;
		public IntPtr wParam;
		public IntPtr lParam;
		public int time;
		public int pt_x;
		public int pt_y;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public readonly int left;
		public readonly int top;
		public readonly int right;
		public readonly int bottom;
		public Rectangle ToRectangle() { return Rectangle.FromLTRB(left, top, right, bottom); }
	}

#endregion ComInterop

	#endregion RequiredClassesForPreviewHandlers
}