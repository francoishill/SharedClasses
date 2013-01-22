using System;
using System.Windows.Forms;
using SharedClasses;

public static class TreeviewExtensions
{
	public static void SetVistaStyle(this TreeView tv)
	{
		StylingInterop.SetTreeviewVistaStyle(tv);
	}
}

public class StylingInterop
{
	public static void SetVistaStyleOnControlHandle(IntPtr ControlHandle)
	{
		Win32Api.SetWindowTheme(ControlHandle, "explorer", null);
	}

	public static void SetTreeviewVistaStyle(Control control)
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)
		{
			Func<Control, bool> SetTreeviewVistaStyleNow = new Func<Control, bool>((tv) =>
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

			if (control.Handle != IntPtr.Zero)
				SetTreeviewVistaStyleNow(control);
			else
				control.HandleCreated += delegate { SetTreeviewVistaStyleNow(control); };
		}
	}
}