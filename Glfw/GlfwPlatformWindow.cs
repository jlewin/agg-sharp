//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using GLFW;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;
using MatterHackers.VectorMath;

namespace MatterHackers.GlfwProvider
{
	public class GlfwPlatformWindow : IPlatformWindow
	{
		private static bool firstWindow = true;

		private static MouseButtons mouseButton;

		private static double mouseX;

		private static double mouseY;

		private readonly Dictionary<MouseButton, int> clickCount = new Dictionary<MouseButton, int>();

		private readonly Dictionary<MouseButton, long> lastMouseDownTime = new Dictionary<MouseButton, long>();

		private string _title = "";

		private Window glfwWindow;

		private SystemWindow systemWindow;

		public GlfwPlatformWindow()
		{
		}

		public string Caption
		{
			get
			{
				return _title;
			}

			set
			{
				_title = value;
				Glfw.SetWindowTitle(glfwWindow, _title);
			}
		}

		public Point2D DesktopPosition
		{
			get
			{
				Glfw.GetWindowPosition(glfwWindow, out int x, out int y);

				return new Point2D(x, y);
			}

			set
			{
				Glfw.SetWindowPosition(glfwWindow, value.x, value.y);
			}
		}

		public bool Invalidated { get; set; } = true;

		public Vector2 MinimumSize
		{
			get
			{
				return this.systemWindow.MinimumSize;
			}

			set
			{
				this.systemWindow.MinimumSize = value;
				Glfw.SetWindowSizeLimits(glfwWindow,
					(int)systemWindow.MinimumSize.X,
					(int)systemWindow.MinimumSize.Y,
					-1,
					-1);
			}
		}

		public int TitleBarHeight => 45;

		public GlfwWindowProvider WindowProvider { get; set; }

		public void BringToFront()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
			throw new NotImplementedException();
		}

		public void CloseSystemWindow(SystemWindow systemWindow)
		{
			Glfw.SetWindowShouldClose(glfwWindow, true);
		}

		public void Invalidate(RectangleDouble rectToInvalidate)
		{
			Invalidated = true;
		}

		public Graphics2D NewGraphics2D()
		{
			// this is for testing the openGL implementation
			var graphics2D = new Graphics2DOpenGL((int)this.systemWindow.Width,
				(int)this.systemWindow.Height,
				GuiWidget.DeviceScale);
			graphics2D.PushTransform();

			return graphics2D;
		}

		public void SetCursor(Cursors cursorToSet)
		{
			Glfw.SetCursor(glfwWindow, MapCursor(cursorToSet));
		}

		public void ShowSystemWindow(SystemWindow systemWindow)
		{
			// Set the active SystemWindow & PlatformWindow references
			systemWindow.PlatformWindow = this;

			systemWindow.AnchorAll();

			if (firstWindow)
			{
				firstWindow = false;
				this.systemWindow = systemWindow;

				this.Show();
			}
			else
			{
				// Notify the embedded window of its new single windows parent size

				// If client code has called ShowSystemWindow and we're minimized, we must restore in order
				// to establish correct window bounds from ClientSize below. Otherwise we're zeroed out and
				// will create invalid surfaces of (0,0)
				// if (this.WindowState == FormWindowState.Minimized)
				{
					// this.WindowState = FormWindowState.Normal;
				}

				systemWindow.Size = new Vector2(this.systemWindow.Width, this.systemWindow.Height);
			}
		}

		private void CursorPositionCallback(IntPtr window, double x, double y)
		{
			mouseX = x;
			mouseY = systemWindow.Height - y;
			systemWindow.OnMouseMove(new MouseEventArgs(mouseButton, 0, mouseX, mouseY, 0));
		}

		private void ConditionalDrawAndRefresh(SystemWindow systemWindow)
		{
			if (this.Invalidated)
			{
				SetupViewport();

				this.Invalidated = false;
				Graphics2D graphics2D = new Graphics2DOpenGL((int)systemWindow.Width, (int)systemWindow.Height, GuiWidget.DeviceScale);
				graphics2D.PushTransform();
				systemWindow.OnDrawBackground(graphics2D);
				systemWindow.OnDraw(graphics2D);

				Glfw.SwapBuffers(glfwWindow);
			}
		}

		private void CharCallback(IntPtr window, uint codePoint)
		{
			systemWindow.OnKeyPress(new KeyPressEventArgs((char)codePoint));
		}

		public Agg.UI.Keys ModifierKeys { get; private set; } = Agg.UI.Keys.None;

		private void UpdateKeyboard(ModifierKeys theEvent)
		{
			int keys = (int)Agg.UI.Keys.None;

			var shiftKey = theEvent.HasFlag(GLFW.ModifierKeys.Shift);
			Keyboard.SetKeyDownState(Agg.UI.Keys.Shift, shiftKey);
			if (shiftKey)
			{
				keys |= (int)Agg.UI.Keys.Shift;
			}

			var controlKey = theEvent.HasFlag(GLFW.ModifierKeys.Control);
			Keyboard.SetKeyDownState(Agg.UI.Keys.Control, controlKey);
			if (controlKey)
			{
				keys |= (int)Agg.UI.Keys.Control;
			}

			var altKey = theEvent.HasFlag(GLFW.ModifierKeys.Alt);
			Keyboard.SetKeyDownState(Agg.UI.Keys.Alt, altKey);
			if (altKey)
			{
				keys |= (int)Agg.UI.Keys.Alt;
			}

			ModifierKeys = (Agg.UI.Keys)keys;
		}

		private HashSet<Agg.UI.Keys> suppressedKeyDowns = new HashSet<Agg.UI.Keys>();
		private bool alreadyClosing;

		private void KeyCallback(IntPtr windowIn, GLFW.Keys key, int scanCode, InputState state, ModifierKeys mods)
		{
			if (state == InputState.Press)
			{
				var keyData = MapKey(key, out bool _);
				Keyboard.SetKeyDownState(keyData, true);
				UpdateKeyboard(mods);

				var keyEvent = new Agg.UI.KeyEventArgs(keyData | ModifierKeys);
				systemWindow.OnKeyDown(keyEvent);

				if (keyEvent.SuppressKeyPress)
				{
					suppressedKeyDowns.Add(keyEvent.KeyCode);
				}
			}
			else if (state == InputState.Repeat)
			{

			}
			else if (state == InputState.Release)
			{
				var keyData = MapKey(key, out bool suppress);
				Keyboard.SetKeyDownState(keyData, false);
				UpdateKeyboard(mods);

				var keyEvent = new Agg.UI.KeyEventArgs(keyData | ModifierKeys);

				systemWindow.OnKeyUp(keyEvent);

				if (suppressedKeyDowns.Contains(keyEvent.KeyCode))
				{
					suppressedKeyDowns.Remove(keyEvent.KeyCode);
				}
			}
		}

		private Agg.UI.Keys MapKey(GLFW.Keys key, out bool suppress)
		{
			suppress = true;

			switch (key)
			{
				case GLFW.Keys.F1:
					return Agg.UI.Keys.F1;

				case GLFW.Keys.F2:
					return Agg.UI.Keys.F2;

				case GLFW.Keys.F3:
					return Agg.UI.Keys.F3;

				case GLFW.Keys.F4:
					return Agg.UI.Keys.F4;

				case GLFW.Keys.F5:
					return Agg.UI.Keys.F5;

				case GLFW.Keys.F6:
					return Agg.UI.Keys.F6;

				case GLFW.Keys.F7:
					return Agg.UI.Keys.F7;

				case GLFW.Keys.F8:
					return Agg.UI.Keys.F8;

				case GLFW.Keys.F9:
					return Agg.UI.Keys.F9;

				case GLFW.Keys.F10:
					return Agg.UI.Keys.F10;

				case GLFW.Keys.F11:
					return Agg.UI.Keys.F11;

				case GLFW.Keys.F12:
					return Agg.UI.Keys.F12;

				case GLFW.Keys.Home:
					return Agg.UI.Keys.Home;

				case GLFW.Keys.PageUp:
					return Agg.UI.Keys.PageUp;

				case GLFW.Keys.PageDown:
					return Agg.UI.Keys.PageDown;

				case GLFW.Keys.End:
					return Agg.UI.Keys.End;

				case GLFW.Keys.Escape:
					return Agg.UI.Keys.Escape;

				case GLFW.Keys.Left:
					return Agg.UI.Keys.Left;

				case GLFW.Keys.Right:
					return Agg.UI.Keys.Right;

				case GLFW.Keys.Up:
					return Agg.UI.Keys.Up;

				case GLFW.Keys.Down:
					return Agg.UI.Keys.Down;

				case GLFW.Keys.Backspace:
					return Agg.UI.Keys.Back;

				case GLFW.Keys.Delete:
					return Agg.UI.Keys.Delete;
			}

			suppress = false;

			switch (key)
			{
				/*
				case GLFW.Keys.D0:
					return Agg.UI.Keys.D0;
				case GLFW.Keys.D1:
					return Agg.UI.Keys.D1;
				case GLFW.Keys.D2:
					return Agg.UI.Keys.D2;
				case GLFW.Keys.D3:
					return Agg.UI.Keys.D3;
				case GLFW.Keys.D4:
					return Agg.UI.Keys.D4;
				case GLFW.Keys.D5:
					return Agg.UI.Keys.D5;
				case GLFW.Keys.D6:
					return Agg.UI.Keys.D6;
				case GLFW.Keys.D7:
					return Agg.UI.Keys.D7;
				case GLFW.Keys.D8:
					return Agg.UI.Keys.D8;
				case GLFW.Keys.D9:
					return Agg.UI.Keys.D9;
				*/

				case GLFW.Keys.Numpad0:
					return Agg.UI.Keys.NumPad0;
				case GLFW.Keys.Numpad1:
					return Agg.UI.Keys.NumPad1;
				case GLFW.Keys.Numpad2:
					return Agg.UI.Keys.NumPad2;
				case GLFW.Keys.Numpad3:
					return Agg.UI.Keys.NumPad3;
				case GLFW.Keys.Numpad4:
					return Agg.UI.Keys.NumPad4;
				case GLFW.Keys.Numpad5:
					return Agg.UI.Keys.NumPad5;
				case GLFW.Keys.Numpad6:
					return Agg.UI.Keys.NumPad6;
				case GLFW.Keys.Numpad7:
					return Agg.UI.Keys.NumPad7;
				case GLFW.Keys.Numpad8:
					return Agg.UI.Keys.NumPad8;
				case GLFW.Keys.Numpad9:
					return Agg.UI.Keys.NumPad9;
				case GLFW.Keys.NumpadEnter:
					return Agg.UI.Keys.Enter;

				case GLFW.Keys.Tab:
					return Agg.UI.Keys.Tab;
				case GLFW.Keys.Enter:
					return Agg.UI.Keys.Return;

				case GLFW.Keys.A:
					return Agg.UI.Keys.A;
				case GLFW.Keys.B:
					return Agg.UI.Keys.B;
				case GLFW.Keys.C:
					return Agg.UI.Keys.C;
				case GLFW.Keys.D:
					return Agg.UI.Keys.D;
				case GLFW.Keys.E:
					return Agg.UI.Keys.E;
				case GLFW.Keys.F:
					return Agg.UI.Keys.F;
				case GLFW.Keys.G:
					return Agg.UI.Keys.G;
				case GLFW.Keys.H:
					return Agg.UI.Keys.H;
				case GLFW.Keys.I:
					return Agg.UI.Keys.I;
				case GLFW.Keys.J:
					return Agg.UI.Keys.J;
				case GLFW.Keys.K:
					return Agg.UI.Keys.K;
				case GLFW.Keys.L:
					return Agg.UI.Keys.L;
				case GLFW.Keys.M:
					return Agg.UI.Keys.M;
				case GLFW.Keys.N:
					return Agg.UI.Keys.N;
				case GLFW.Keys.O:
					return Agg.UI.Keys.O;
				case GLFW.Keys.P:
					return Agg.UI.Keys.P;
				case GLFW.Keys.Q:
					return Agg.UI.Keys.Q;
				case GLFW.Keys.R:
					return Agg.UI.Keys.R;
				case GLFW.Keys.S:
					return Agg.UI.Keys.S;
				case GLFW.Keys.T:
					return Agg.UI.Keys.T;
				case GLFW.Keys.U:
					return Agg.UI.Keys.U;
				case GLFW.Keys.V:
					return Agg.UI.Keys.V;
				case GLFW.Keys.W:
					return Agg.UI.Keys.W;
				case GLFW.Keys.X:
					return Agg.UI.Keys.X;
				case GLFW.Keys.Y:
					return Agg.UI.Keys.Y;
				case GLFW.Keys.Z:
					return Agg.UI.Keys.Z;
			}

			return Agg.UI.Keys.BrowserStop;
		}

		private Cursor MapCursor(Cursors cursorToSet)
		{
			switch (cursorToSet)
			{
				case Cursors.Arrow:
					return Glfw.CreateStandardCursor(CursorType.Arrow);

				case Cursors.Cross:
				case Cursors.Default:
					return Glfw.CreateStandardCursor(CursorType.Arrow);

				case Cursors.Hand:
					return Glfw.CreateStandardCursor(CursorType.Hand);

				case Cursors.Help:
					return Glfw.CreateStandardCursor(CursorType.Arrow);

				case Cursors.HSplit:
					return Glfw.CreateStandardCursor(CursorType.ResizeVertical);

				case Cursors.IBeam:
					return Glfw.CreateStandardCursor(CursorType.Beam);

				case Cursors.No:
				case Cursors.NoMove2D:
				case Cursors.NoMoveHoriz:
				case Cursors.NoMoveVert:
				case Cursors.PanEast:
				case Cursors.PanNE:
				case Cursors.PanNorth:
				case Cursors.PanNW:
				case Cursors.PanSE:
				case Cursors.PanSouth:
				case Cursors.PanSW:
				case Cursors.PanWest:
				case Cursors.SizeAll:
				case Cursors.SizeNESW:
				case Cursors.SizeNS:
				case Cursors.SizeNWSE:
				case Cursors.SizeWE:
				case Cursors.UpArrow:
					return Glfw.CreateStandardCursor(CursorType.Arrow);

				case Cursors.VSplit:
					return Glfw.CreateStandardCursor(CursorType.ResizeHorizontal);

				case Cursors.WaitCursor:
					return Glfw.CreateStandardCursor(CursorType.Arrow);
			}

			return Glfw.CreateStandardCursor(CursorType.Arrow);
		}

		private void MouseButtonCallback(IntPtr window, MouseButton button, InputState state, ModifierKeys modifiers)
		{
			var now = UiThread.CurrentTimerMs;
			mouseButton = MouseButtons.Left;
			switch (button)
			{
				case MouseButton.Middle:
					mouseButton = MouseButtons.Middle;
					break;

				case MouseButton.Right:
					mouseButton = MouseButtons.Right;
					break;
			}

			if (state == InputState.Press)
			{
				clickCount[button] = 1;
				if (lastMouseDownTime.ContainsKey(button))
				{
					if (lastMouseDownTime[button] > now - 500)
					{
						clickCount[button] = 2;
					}
				}

				lastMouseDownTime[button] = now;
				systemWindow.OnMouseDown(new MouseEventArgs(mouseButton, clickCount[button], mouseX, mouseY, 0));
			}
			else if (state == InputState.Release)
			{
				systemWindow.OnMouseUp(new MouseEventArgs(mouseButton, clickCount[button], mouseX, mouseY, 0));
			}
		}

		private void ScrollCallback(IntPtr window, double x, double y)
		{
			systemWindow.OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, mouseX, mouseY, (int)(y * 120)));
		}

		private void SetupViewport()
		{
			// If this throws an assert, you are calling MakeCurrent() before the glControl is done being constructed.
			// Call this function you have called Show().
			int w = (int)systemWindow.Width;
			int h = (int)systemWindow.Height;
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, w, 0, h, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
			GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
		}

		private void Show()
		{
			// Glfw.WindowHint(Hint.Decorated, false);
			var config = AggContext.Config.GraphicsMode;
			Glfw.WindowHint(Hint.Samples, config.FSAASamples);
			Glfw.WindowHint(Hint.Visible, false);

			// Create window
			glfwWindow = Glfw.CreateWindow((int)systemWindow.Width, (int)systemWindow.Height, systemWindow.Title, Monitor.None, Window.None);
			Glfw.SetWindowSizeLimits(glfwWindow,
				(int)systemWindow.MinimumSize.X,
				(int)systemWindow.MinimumSize.Y,
				-1,
				-1);
			Glfw.MakeContextCurrent(glfwWindow);
			OpenGL.Gl.Import(Glfw.GetProcAddress);

			// set the gl renderer to the GLFW specific one rather than the OpenTk one
			var glfwGl = new GlfwGL();
			GL.Instance = glfwGl;

			// Effectively enables VSYNC by setting to 1.
			Glfw.SwapInterval(1);

			systemWindow.PlatformWindow = this;

			if (systemWindow.Maximized)
			{
				// TODO: make this right
				var screenSize = Glfw.PrimaryMonitor.WorkArea;
				var x = (screenSize.Width - (int)systemWindow.Width) / 2;
				var y = (screenSize.Height - (int)systemWindow.Height) / 2;
				Glfw.SetWindowPosition(glfwWindow, x, y);
				Glfw.MaximizeWindow(glfwWindow);
			}
			else if (systemWindow.InitialDesktopPosition == new Point2D(-1, -1))
			{
				// Find center position based on window and monitor sizes
				var screenSize = Glfw.PrimaryMonitor.WorkArea;
				var x = (screenSize.Width - (int)systemWindow.Width) / 2;
				var y = (screenSize.Height - (int)systemWindow.Height) / 2;
				Glfw.SetWindowPosition(glfwWindow, x, y);
			}
			else
			{
				Glfw.SetWindowPosition(glfwWindow,
					(int)systemWindow.InitialDesktopPosition.x,
					(int)systemWindow.InitialDesktopPosition.y);
			}

			Glfw.SetWindowSizeCallback(glfwWindow, SizeCallback);

			// Set a key callback
			Glfw.SetKeyCallback(glfwWindow, KeyCallback);
			Glfw.SetCharCallback(glfwWindow, CharCallback);
			Glfw.SetCursorPositionCallback(glfwWindow, CursorPositionCallback);
			Glfw.SetMouseButtonCallback(glfwWindow, MouseButtonCallback);
			Glfw.SetScrollCallback(glfwWindow, ScrollCallback);
			Glfw.SetCloseCallback(glfwWindow, CloseCallback);

			Glfw.ShowWindow(glfwWindow);

			var openTime = UiThread.CurrentTimerMs;
			while (!Glfw.WindowShouldClose(glfwWindow))
			{
				// Poll for OS events and swap front/back buffers
				UiThread.InvokePendingActions();

				if (UiThread.CurrentTimerMs > openTime + 500)
				{
					// wait for the window to finish opening
					Glfw.PollEvents();

					ConditionalDrawAndRefresh(systemWindow);
				}
			}
		}

		private void CloseCallback(IntPtr window)
		{
			var closing = new ClosingEventArgs();
			systemWindow.OnClosing(closing);
			if (closing.Cancel)
			{
				Glfw.SetWindowShouldClose(glfwWindow, false);
			}
		}

		private void SizeCallback(IntPtr window, int width, int height)
		{
			systemWindow.Size = new VectorMath.Vector2(width, height);
			GL.Viewport(0, 0, width, height); // Use all of the glControl painting area
			ConditionalDrawAndRefresh(systemWindow);
		}
	}
}