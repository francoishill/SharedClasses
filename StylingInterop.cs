using System;
using System.Windows.Forms;

public class StylingInterop
{
	public static void SetVistaStyleOnControlHandle(IntPtr ControlHandle)
	{
		Win32Api.SetWindowTheme(ControlHandle, "explorer", null);
	}

	public static void SetTreeviewVistaStyle(TreeView treeview)
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)
		{
			Func<TreeView, bool> SetTreeviewVistaStyleNow = new Func<TreeView, bool>((tv) =>
			{
				int dw = Win32Api.SendMessage(tv.Handle, Win32Api.TVM_GETEXTENDEDSTYLE, 0, 0);

				// Update style
				dw |= Win32Api.TVS_EX_AUTOHSCROLL;       // autoscroll horizontaly
				dw |= Win32Api.TVS_EX_FADEINOUTEXPANDOS; // auto hide the +/- signs
				dw |= Win32Api.TVS_EX_DOUBLEBUFFER;			//Double buffering to avoid flickering

				// set style
				Win32Api.SendMessage(tv.Handle, Win32Api.TVM_SETEXTENDEDSTYLE, 0, dw);

				// little black/empty arrows and blue highlight on treenodes
				SetVistaStyleOnControlHandle(tv.Handle);
				return true;
			});

			if (treeview.Handle != IntPtr.Zero)
				SetTreeviewVistaStyleNow.Invoke(treeview);
			else
				treeview.HandleCreated += delegate { SetTreeviewVistaStyleNow(treeview); };
		}
	}
}