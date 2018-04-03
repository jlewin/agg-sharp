﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

#define DO_LIGHTING

using System;
using System.Collections.Generic;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;
using MatterHackers.VectorMath.TrackBall;

namespace MatterHackers.Agg.OpenGlGui
{
	public static class ExtensionMethods
	{
		public static void RenderDebugAABB(this WorldView worldView, Graphics2D graphics2D, AxisAlignedBoundingBox bounds)
		{
			Vector3 renderPosition = bounds.Center;
			Vector2 objectCenterScreenSpace = worldView.GetScreenPosition(renderPosition);
			Point2D screenPositionOfObject3D = new Point2D((int)objectCenterScreenSpace.X, (int)objectCenterScreenSpace.Y);

			graphics2D.Circle(objectCenterScreenSpace, 5, Color.Magenta);

			for (int i = 0; i < 4; i++)
			{
				graphics2D.Circle(worldView.GetScreenPosition(bounds.GetTopCorner(i)), 5, Color.Magenta);
				graphics2D.Circle(worldView.GetScreenPosition(bounds.GetBottomCorner(i)), 5, Color.Magenta);
			}

			RectangleDouble screenBoundsOfObject3D = RectangleDouble.ZeroIntersection;
			for (int i = 0; i < 4; i++)
			{
				screenBoundsOfObject3D.ExpandToInclude(worldView.GetScreenPosition(bounds.GetTopCorner(i)));
				screenBoundsOfObject3D.ExpandToInclude(worldView.GetScreenPosition(bounds.GetBottomCorner(i)));
			}

			graphics2D.Circle(screenBoundsOfObject3D.Left, screenBoundsOfObject3D.Bottom, 5, Color.Cyan);
			graphics2D.Circle(screenBoundsOfObject3D.Left, screenBoundsOfObject3D.Top, 5, Color.Cyan);
			graphics2D.Circle(screenBoundsOfObject3D.Right, screenBoundsOfObject3D.Bottom, 5, Color.Cyan);
			graphics2D.Circle(screenBoundsOfObject3D.Right, screenBoundsOfObject3D.Top, 5, Color.Cyan);
		}
	}

	public class TrackballTumbleWidget : GuiWidget
	{
		public TrackBallTransformType TransformState { get; set; }

		private List<IVertexSource> insideArrows = new List<IVertexSource>();
		private List<IVertexSource> outsideArrows = new List<IVertexSource>();

		private WorldView world;

		public TrackBallController TrackBallController { get; }

		public bool LockTrackBall { get; set; }

		private GuiWidget sourceWidget;

		public TrackballTumbleWidget(WorldView world, GuiWidget sourceWidget)
		{
			AnchorAll();
			TrackBallController = new TrackBallController(world);
			this.world = world;
			this.sourceWidget = sourceWidget;
		}

		public Func<Vector2, Vector3> GetBedIntersection
		{
			get => TrackBallController.GetBedIntersection;
			set => TrackBallController.GetBedIntersection = value;
		}

		public override void OnBoundsChanged(EventArgs e)
		{
			Vector2 screenCenter = new Vector2(Width / 2, Height / 2);
			double trackingRadius = Math.Min(Width * .45, Height * .45);
			world.ScreenCenter = screenCenter;

			TrackBallController.TrackBallRadius = trackingRadius;

			this.world.CalculateProjectionMatrix(sourceWidget.Width, sourceWidget.Height);

			this.world.CalculateModelviewMatrix();

			MakeArrowIcons();

			base.OnBoundsChanged(e);
		}

		private void MakeArrowIcons()
		{
			var center = this.world.ScreenCenter;
			var radius = TrackBallController.TrackBallRadius;
			insideArrows.Clear();
			// create the inside arrows
			{
				var svg = new VertexStorage("M560.512 0.570216 C560.512 2.05696 280.518 560.561 280.054 560 C278.498 558.116 0 0.430888 0.512416 0.22416 C0.847112 0.089136 63.9502 27.1769 140.742 60.4192 C140.742 60.4192 280.362 120.86 280.362 120.86 C280.362 120.86 419.756 60.4298 419.756 60.4298 C496.422 27.1934 559.456 0 559.831 0 C560.205 0 560.512 0.2566 560.512 0.570216 Z");
				RectangleDouble bounds = svg.GetBounds();
				double arrowWidth = radius / 10;
				var centered = Affine.NewTranslation(-bounds.Center);
				var scaledTo1 = Affine.NewScaling(1 / bounds.Width);
				var scaledToSize = Affine.NewScaling(arrowWidth);
				var moveToRadius = Affine.NewTranslation(new Vector2(0, radius * 9 / 10));
				var moveToScreenCenter = Affine.NewTranslation(center);
				for (int i = 0; i < 4; i++)
				{
					var arrowLeftTransform = centered * scaledTo1 * scaledToSize * moveToRadius * Affine.NewRotation(MathHelper.Tau / 4 * i) * moveToScreenCenter;
					insideArrows.Add(new VertexSourceApplyTransform(svg, arrowLeftTransform));
				}
			}

			outsideArrows.Clear();
			// and the outside arrows
			{
				//var svg = new PathStorage("m 271.38288,545.86543 c -10.175,-4.94962 -23,-11.15879 -28.5,-13.79816 -5.5,-2.63937 -24.34555,-11.82177 -41.87901,-20.40534 -17.53346,-8.58356 -32.21586,-15.60648 -32.62756,-15.60648 -0.4117,0 -1.28243,-0.64329 -1.93495,-1.42954 -0.98148,-1.1826 -0.0957,-1.94177 5.12755,-4.39484 3.47268,-1.63091 16.21397,-7.7909 28.31397,-13.68887 12.1,-5.89797 30.55,-14.8788 41,-19.9574 10.45,-5.07859 25.64316,-12.49628 33.76258,-16.48374 8.11942,-3.98746 15.43192,-6.99308 16.25,-6.67916 2.02527,0.77717 1.8755,5.19031 -0.56452,16.63355 -1.11411,5.225 -2.29208,10.9625 -2.6177,12.75 l -0.59204,3.25 80.19823,0 c 75.90607,0 80.17104,-0.0937 79.69036,-1.75 -2.47254,-8.51983 -5.62648,-24.42623 -5.62674,-28.37756 -3.6e-4,-5.51447 1.61726,-5.18356 21.01872,4.29961 10.16461,4.96833 22.98111,11.1892 28.48111,13.82415 5.5,2.63496 24.34555,11.81375 41.87901,20.39732 17.53346,8.58356 32.21586,15.60648 32.62756,15.60648 0.4117,0 1.28243,0.64329 1.93495,1.42954 0.98144,1.18256 0.0956,1.94283 -5.12755,4.40048 -3.47268,1.63401 -15.98897,7.68875 -27.81397,13.45496 -11.825,5.76621 -31.625,15.41743 -44,21.44716 -12.375,6.02972 -27.79146,13.55332 -34.2588,16.71911 -6.99025,3.42175 -12.41867,5.50276 -13.38597,5.13157 -2.11241,-0.81061 -1.37413,-8.85503 2.14722,-23.39653 1.37365,-5.67253 2.49755,-10.73503 2.49755,-11.25 0,-0.57397 -31.15148,-0.93629 -80.5,-0.93629 -76.11526,0 -80.5,0.0957 -80.5,1.7566 0,0.96613 0.45587,3.32863 1.01304,5.25 1.68077,5.79599 4.98696,23.01922 4.98696,25.97902 0,5.59974 -1.53004,5.29551 -21,-4.17564 z");
				var svg = new VertexStorage("M560.512 0.570216 C560.512 2.05696 280.518 560.561 280.054 560 C278.498 558.116 0 0.430888 0.512416 0.22416 C0.847112 0.089136 63.9502 27.1769 140.742 60.4192 C140.742 60.4192 280.362 120.86 280.362 120.86 C280.362 120.86 419.756 60.4298 419.756 60.4298 C496.422 27.1934 559.456 0 559.831 0 C560.205 0 560.512 0.2566 560.512 0.570216 Z");
				RectangleDouble bounds = svg.GetBounds();
				double arrowWidth = radius / 15;
				var centered = Affine.NewTranslation(-bounds.Center);
				var scaledTo1 = Affine.NewScaling(1 / bounds.Width);
				var scaledToSize = Affine.NewScaling(arrowWidth);
				var moveToRadius = Affine.NewTranslation(new Vector2(0, radius * 16 / 15));
				var moveToScreenCenter = Affine.NewTranslation(center);
				for (int i = 0; i < 4; i++)
				{
					var arrowLeftTransform = centered * scaledTo1 * scaledToSize * Affine.NewRotation(MathHelper.Tau / 4) * moveToRadius * Affine.NewRotation(MathHelper.Tau / 8 + MathHelper.Tau / 4 * i + MathHelper.Tau / 80) * moveToScreenCenter;
					outsideArrows.Add(new VertexSourceApplyTransform(svg, arrowLeftTransform));

					var arrowRightTransform = centered * scaledTo1 * scaledToSize * Affine.NewRotation(-MathHelper.Tau / 4) * moveToRadius * Affine.NewRotation(MathHelper.Tau / 8 + MathHelper.Tau / 4 * i - MathHelper.Tau / 80) * moveToScreenCenter;
					outsideArrows.Add(new VertexSourceApplyTransform(svg, arrowRightTransform));
				}
			}
		}

		internal class MotionQueue
		{
			internal struct TimeAndPosition
			{
				internal TimeAndPosition(Vector2 position, long timeMs)
				{
					this.timeMs = timeMs;
					this.position = position;
				}

				internal long timeMs;
				internal Vector2 position;
			}

			List<TimeAndPosition> motionQueue = new List<TimeAndPosition>();

			internal void AddMoveToMotionQueue(Vector2 position, long timeMs)
			{
				if (motionQueue.Count > 4)
				{
					// take off the last one
					motionQueue.RemoveAt(0);
				}

				motionQueue.Add(new TimeAndPosition(position, timeMs));
			}

			internal void Clear()
			{
				motionQueue.Clear();
			}

			internal Vector2 GetVelocityPixelsPerMs()
			{
				if (motionQueue.Count > 1)
				{
					// Get all the movement that is less 100 ms from the last time (the mouse up)
					TimeAndPosition lastTime = motionQueue[motionQueue.Count - 1];
					int firstTimeIndex = motionQueue.Count - 1;
					while (firstTimeIndex > 0 && motionQueue[firstTimeIndex - 1].timeMs + 100 > lastTime.timeMs)
					{
						firstTimeIndex--;
					}

					TimeAndPosition firstTime = motionQueue[firstTimeIndex];

					double milliseconds = lastTime.timeMs - firstTime.timeMs;
					if (milliseconds > 0)
					{
						Vector2 pixels = lastTime.position - firstTime.position;
						Vector2 pixelsPerSecond = pixels / milliseconds;

						return pixelsPerSecond;
					}
				}

				return Vector2.Zero;
			}
		}

		MotionQueue motionQueue = new MotionQueue();

		double startAngle = 0;
		double startDistanceBetweenPoints = 1;
		double pinchStartScale = 1;

		public override void OnMouseDown(MouseEventArgs mouseEvent)
		{
			base.OnMouseDown(mouseEvent);

			if (!LockTrackBall && MouseCaptured)
			{
				Vector2 currentMousePosition;
				if (mouseEvent.NumPositions == 1)
				{
					currentMousePosition.X = mouseEvent.X;
					currentMousePosition.Y = mouseEvent.Y;
				}
				else
				{
					currentMousePosition = (mouseEvent.GetPosition(1) + mouseEvent.GetPosition(0)) / 2;
				}

				currentVelocityPerMs = Vector2.Zero;
				motionQueue.Clear();
				motionQueue.AddMoveToMotionQueue(currentMousePosition, UiThread.CurrentTimerMs);

				if (mouseEvent.NumPositions > 1)
				{
					Vector2 position0 = mouseEvent.GetPosition(0);
					Vector2 position1 = mouseEvent.GetPosition(1);
					startDistanceBetweenPoints = (position1 - position0).Length;
					pinchStartScale = world.Scale;

					startAngle = Math.Atan2(position1.Y - position0.Y, position1.X - position0.X);

					if (TransformState != TrackBallTransformType.None)
					{
						if (!LockTrackBall && TrackBallController.CurrentTrackingType != TrackBallTransformType.None)
						{
							TrackBallController.OnMouseUp();
						}
						TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Translation);
					}
				}

				if (mouseEvent.Button == MouseButtons.Left)
				{
					if (TrackBallController.CurrentTrackingType == TrackBallTransformType.None)
					{
						switch (TransformState)
						{
							case TrackBallTransformType.Rotation:
								TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Rotation);
								break;

							case TrackBallTransformType.Translation:
								TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Translation);
								break;

							case TrackBallTransformType.Scale:
								TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Scale);
								break;
						}
					}
				}
				else if (mouseEvent.Button == MouseButtons.Middle)
				{
					if (TrackBallController.CurrentTrackingType == TrackBallTransformType.None)
					{
						TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Translation);
					}
				}
				else if (mouseEvent.Button == MouseButtons.Right)
				{
					if (TrackBallController.CurrentTrackingType == TrackBallTransformType.None)
					{
						TrackBallController.OnMouseDown(currentMousePosition, Matrix4X4.Identity, TrackBallTransformType.Rotation);
					}
				}
			}
		}

		public override void OnMouseMove(MouseEventArgs mouseEvent)
		{
			base.OnMouseMove(mouseEvent);

			Vector2 currentMousePosition;
			if (mouseEvent.NumPositions == 1)
			{
				currentMousePosition.X = mouseEvent.X;
				currentMousePosition.Y = mouseEvent.Y;
			}
			else
			{
				currentMousePosition = (mouseEvent.GetPosition(1) + mouseEvent.GetPosition(0)) / 2;
			}

			motionQueue.AddMoveToMotionQueue(currentMousePosition, UiThread.CurrentTimerMs);

			if (!LockTrackBall && TrackBallController.CurrentTrackingType != TrackBallTransformType.None)
			{
				TrackBallController.OnMouseMove(currentMousePosition);
				Invalidate();
			}

			// check if we should do some scaling or rotation
			if (TransformState != TrackBallTransformType.None
				&& mouseEvent.NumPositions > 1
				&& startDistanceBetweenPoints > 0)
			{
				Vector2 position0 = mouseEvent.GetPosition(0);
				Vector2 position1 = mouseEvent.GetPosition(1);
				double curDistanceBetweenPoints = (position1 - position0).Length;

				double scaleAmount = pinchStartScale * curDistanceBetweenPoints / startDistanceBetweenPoints;
				this.world.Scale = scaleAmount;

				double angle = Math.Atan2(position1.Y - position0.Y, position1.X - position0.X);
			}
		}

		Vector2 currentVelocityPerMs = new Vector2();
		public void ZeroVelocity()
		{
			currentVelocityPerMs = Vector2.Zero;
		}

		public override void OnMouseUp(MouseEventArgs mouseEvent)
		{
			if (!LockTrackBall && TrackBallController.CurrentTrackingType != TrackBallTransformType.None)
			{
				if (TrackBallController.CurrentTrackingType == TrackBallTransformType.Rotation)
				{
					// try and preserve some of the velocity
					motionQueue.AddMoveToMotionQueue(mouseEvent.Position, UiThread.CurrentTimerMs);

					if (!Keyboard.IsKeyDown(Keys.ShiftKey))
					{
						currentVelocityPerMs = motionQueue.GetVelocityPixelsPerMs();
						if (currentVelocityPerMs.LengthSquared > 0)
						{
							UiThread.SetInterval(ApplyVelocity, 1.0 / updatesPerSecond, () => !HasBeenClosed && currentVelocityPerMs.LengthSquared > 0);
						}
					}
				}

				TrackBallController.OnMouseUp();
			}

			base.OnMouseUp(mouseEvent);
		}

		int updatesPerSecond = 30;
		private void ApplyVelocity()
		{
			double msPerUpdate = 1000.0 / updatesPerSecond;
			if (currentVelocityPerMs.LengthSquared > 0)
			{
				if (TrackBallController.CurrentTrackingType == TrackBallTransformType.None)
				{
					Vector2 center = LocalBounds.Center;
					TrackBallController.OnMouseDown(center, Matrix4X4.Identity, TrackBallTransformType.Rotation);
					TrackBallController.OnMouseMove(center + currentVelocityPerMs * msPerUpdate);
					TrackBallController.OnMouseUp();
					Invalidate();

					currentVelocityPerMs *= .85;
					if (currentVelocityPerMs.LengthSquared < .01 / msPerUpdate)
					{
						currentVelocityPerMs = Vector2.Zero;
					}
				}
			}
		}

		public override void OnMouseWheel(MouseEventArgs mouseEvent)
		{
			if (!LockTrackBall && ContainsFirstUnderMouseRecursive())
			{
				TrackBallController.OnMouseWheel(mouseEvent.WheelDelta, mouseEvent.Position);
				Invalidate();
			}
			base.OnMouseWheel(mouseEvent);
		}
	}
}