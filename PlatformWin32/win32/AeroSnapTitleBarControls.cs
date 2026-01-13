/*
MIT License

Copyright (c) 2023 MakaroffEgor

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MatterHackers.Agg.UI
{
	public class AeroSnapTitleBarControls : NativeWindow
	{
		private Form form;

		protected override void WndProc(ref Message m)
		{
			const int WM_NCCALCSIZE = 0x0083;
			const int WM_NCHITTEST = 0x0084;
			const int HTCLIENT = 1;
			const int HTTOP = 12;

			//cursor status title bar edge change
			if (m.Msg == WM_NCHITTEST)
			{
				base.WndProc(ref m);

				if (form.WindowState == FormWindowState.Normal)
				{
					if ((int)m.Result == HTCLIENT)
					{
						m.Result = (IntPtr)HTTOP;
					}
				}
				return;
			}

			//cursor status borders change
			if (m.Msg == WM_NCCALCSIZE)
			{
				NCCALCSIZE_PARAMS nccsp = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
				nccsp.rect0.Left += 8;
				nccsp.rect0.Right -= 8;
				nccsp.rect0.Bottom -= 8;

				Marshal.StructureToPtr(nccsp, m.LParam, false);
				return;
			}
			base.WndProc(ref m);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct NCCALCSIZE_PARAMS
		{
			public RECT rect0;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
		private extern static void ReleaseCapture();
		[DllImport("user32.DLL", EntryPoint = "SendMessage")]
		private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

		public AeroSnapTitleBarControls(Form form)
		{
			this.form = form;
			form.Padding = new Padding(form.Padding.Left, +2, form.Padding.Right, form.Padding.Bottom);
			AssignHandle(form.Handle);
		}

		// 2 click titleBar --> full screan form
		private DateTime lastClickTime = DateTime.MinValue;
		private const int doubleClickInterval = 350;
		private int clickCounter = 0;

		public void AeroSnapTitleBar()
		{
			ReleaseCapture();
			SendMessage(form.Handle, 0x112, 0xf012, 0);

			var elapsed = (DateTime.Now - lastClickTime).TotalMilliseconds;
			if (elapsed < doubleClickInterval)
			{
				if (clickCounter % 2 == 1)
				{
					if (form is WinformsSystemWindow parentWindow)
					{
						parentWindow.ToggleMaximize();
					}
				}
				clickCounter++;
			}
			else
			{
				clickCounter = 1;
			}
			lastClickTime = DateTime.Now;
		}
	}
}
