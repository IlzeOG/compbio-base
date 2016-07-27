using System;
using System.Threading;
using BaseLib.Forms.Base;
using BaseLibS.Graph;

namespace BaseLib.Forms.Scroll{
	internal sealed class HorizontalScrollBarView : BasicView{
		private readonly IScrollableControl main;
		private ScrollBarState state = ScrollBarState.Neutral;
		private Bitmap2 firstMark;
		private Bitmap2 firstMarkHighlight;
		private Bitmap2 firstMarkPress;
		private Bitmap2 secondMark;
		private Bitmap2 secondMarkHighlight;
		private Bitmap2 secondMarkPress;
		private Bitmap2 bar;
		private Bitmap2 barHighlight;
		private Bitmap2 barPress;
		private int dragStart = -1;
		private int visibleDragStart = -1;
		private Thread leftThread;
		private Thread rightThread;

		internal HorizontalScrollBarView(IScrollableControl main){
			this.main = main;
		}

		protected internal override void OnResize(EventArgs e, int width, int height){
			bar = null;
		}

		protected internal override void OnPaintBackground(IGraphics g, int width, int height){
			Pen2 p = new Pen2(Color2.FromArgb(172, 168, 153));
			g.DrawLine(p, 0, height - 1, width, height - 1);
			int[][] rgbs = GraphUtil.InterpolateRgb(243, 241, 236, 254, 254, 251,
				GraphUtil.scrollBarWidth - 2);
			for (int i = 0; i < GraphUtil.scrollBarWidth - 2; i++){
				p = new Pen2(Color2.FromArgb(rgbs[0][i], rgbs[1][i], rgbs[2][i]));
				g.DrawLine(p, 0, i + 1, width, i + 1);
			}
			p = new Pen2(Color2.FromArgb(238, 237, 229));
			g.DrawLine(p, 0, 0, width, 0);
			g.DrawLine(p, 0, height - 2, width, height - 2);
		}

		protected internal override void OnPaint(IGraphics g, int width, int height){
			PaintFirstMark(g, GraphUtil.scrollBarWidth);
			PaintSecondMark(g, GraphUtil.scrollBarWidth, width);
			PaintBar(g, GraphUtil.scrollBarWidth, width);
		}

		private void PaintBar(IGraphics g, int scrollBarWid, int width){
			if (!HasBar){
				return;
			}
			if (bar != null && CalcBarSize(width) != bar.Width){
				bar = null;
			}
			if (bar == null){
				CreateBar(scrollBarWid, width);
			}
			int h = CalcBarStart(width);
			switch (state){
				case ScrollBarState.HighlightBar:
					g.DrawImageUnscaled(barHighlight, h, 1);
					break;
				case ScrollBarState.PressBar:
					g.DrawImageUnscaled(barPress, h, 1);
					break;
				default:
					g.DrawImageUnscaled(bar, h, 1);
					break;
			}
		}

		private bool HasBar => main.VisibleWidth < main.TotalWidth();

		private void PaintFirstMark(IGraphics g, int scrollBarWid){
			if (firstMark == null){
				CreateFirstMark(scrollBarWid);
			}
			switch (state){
				case ScrollBarState.HighlightFirstBox:
					g.DrawImageUnscaled(firstMarkHighlight, 0, 1);
					break;
				case ScrollBarState.PressFirstBox:
					g.DrawImageUnscaled(firstMarkPress, 0, 1);
					break;
				default:
					g.DrawImageUnscaled(firstMark, 0, 1);
					break;
			}
		}

		private void PaintSecondMark(IGraphics g, int scrollBarWid, int width){
			if (secondMark == null){
				CreateSecondMark(scrollBarWid);
			}
			int h = scrollBarWid - 1;
			switch (state){
				case ScrollBarState.HighlightSecondBox:
					g.DrawImageUnscaled(secondMarkHighlight, width - h, 1);
					break;
				case ScrollBarState.PressSecondBox:
					g.DrawImageUnscaled(secondMarkPress, width - h, 1);
					break;
				default:
					g.DrawImageUnscaled(secondMark, width - h, 1);
					break;
			}
		}

		private void CreateBar(int scrollBarWid, int width){
			int w = CalcBarSize(width);
			int h = scrollBarWid - 2;
			Bitmap2 b = new Bitmap2(w, h);
			GraphUtil.FillShadedRectangle(b, w, h);
			bar = b;
			barHighlight = b.Lighter();
			barPress = b.Darker();
		}

		private int CalcBarStart(int width){
			int hx = width - 2* GraphUtil.scrollBarWidth + 2;
			if (hx <= 0){
				return GraphUtil.scrollBarWidth - 1;
			}
			int w = (int) Math.Round(hx*(main.VisibleX/(double) (main.TotalWidth() - 1)));
			w = Math.Min(w, hx - 5);
			return Math.Max(0, w) + GraphUtil.scrollBarWidth - 1;
		}

		private int CalcBarEnd(int width){
			int hx = width - 2* GraphUtil.scrollBarWidth + 2;
			if (hx <= 0){
				return GraphUtil.scrollBarWidth - 1;
			}
			return Math.Min(hx, (int) Math.Round(hx*((main.VisibleX + main.VisibleWidth)/(double) (main.TotalWidth() - 1)))) +
					GraphUtil.scrollBarWidth - 1;
		}

		private int CalcBarSize(int width){
			int hx = width - 2* GraphUtil.scrollBarWidth + 2;
			if (hx <= 0){
				return 5;
			}
			return Math.Max(5, CalcBarEnd(width) - CalcBarStart(width));
		}

		private void CreateFirstMark(int scrollBarWid){
			int w = scrollBarWid - 1;
			int h = scrollBarWid - 2;
			Bitmap2 b = new Bitmap2(w, h);
			GraphUtil.FillShadedRectangle(b, w, h);
			firstMark = b;
			//firstMark.MakeTransparent();
			firstMarkHighlight = b.Lighter();
			//firstMarkHighlight.MakeTransparent();
			firstMarkPress = b.Darker();
			//firstMarkPress.MakeTransparent();
		}

		private void CreateSecondMark(int scrollBarWid){
			int w = scrollBarWid - 1;
			int h = scrollBarWid - 2;
			Bitmap2 b = new Bitmap2(w, h);
			GraphUtil.FillShadedRectangle(b, w, h);
			secondMark = b;
			//secondMark.MakeTransparent();
			secondMarkHighlight = b.Lighter();
			//secondMarkHighlight.MakeTransparent();
			secondMarkPress = b.Darker();
			//secondMarkPress.MakeTransparent();
		}

		protected internal override void OnMouseMoved(BasicMouseEventArgs e){
			ScrollBarState newState = ScrollBarState.Neutral;
			if (e.X < GraphUtil.scrollBarWidth - 1){
				newState = ScrollBarState.HighlightFirstBox;
			} else if (e.X > e.Width - GraphUtil.scrollBarWidth){
				newState = ScrollBarState.HighlightSecondBox;
			} else if (HasBar){
				int s = CalcBarStart(e.Width);
				int l = CalcBarSize(e.Width);
				if (e.X >= s && e.X <= s + l){
					newState = ScrollBarState.HighlightBar;
				}
			}
			if (newState != state){
				state = newState;
				Invalidate();
			}
		}

		protected internal override void OnMouseIsDown(BasicMouseEventArgs e){
			ScrollBarState newState = ScrollBarState.Neutral;
			if (e.X < GraphUtil.scrollBarWidth - 1){
				newState = ScrollBarState.PressFirstBox;
				MoveLeft(main.DeltaX());
				leftThread = new Thread(() => WalkLeft(main.DeltaX()));
				leftThread.Start();
			} else if (e.X > e.Width - GraphUtil.scrollBarWidth){
				newState = ScrollBarState.PressSecondBox;
				MoveRight(main.DeltaX());
				rightThread = new Thread(() => WalkRight(main.DeltaX()));
				rightThread.Start();
			} else if (HasBar){
				int s = CalcBarStart(e.Width);
				int l = CalcBarSize(e.Width);
				if (e.X >= s && e.X <= s + l){
					newState = ScrollBarState.PressBar;
					dragStart = e.X;
					visibleDragStart = main.VisibleX;
				} else if (e.X < s){
					MoveLeft(main.VisibleWidth);
					leftThread = new Thread(() => WalkLeft(main.VisibleWidth));
					leftThread.Start();
				} else{
					MoveRight(main.VisibleWidth);
					rightThread = new Thread(() => WalkRight(main.VisibleWidth));
					rightThread.Start();
				}
			}
			if (newState != state){
				state = newState;
				Invalidate();
			}
		}

		private void MoveLeft(int delta){
			main.VisibleX = Math.Max(0, main.VisibleX - delta);
		}

		private void MoveRight(int delta){
			main.VisibleX = Math.Min(main.TotalWidth() - main.VisibleWidth, main.VisibleX + delta);
		}

		private void WalkLeft(int delta){
			Thread.Sleep(400);
			while (true){
				MoveLeft(delta);
				Invalidate();
				Thread.Sleep(150);
			}
			// ReSharper disable FunctionNeverReturns
		}

		// ReSharper restore FunctionNeverReturns
		private void WalkRight(int delta){
			Thread.Sleep(400);
			while (true){
				MoveRight(delta);
				Invalidate();
				Thread.Sleep(150);
			}
			// ReSharper disable FunctionNeverReturns
		}

		// ReSharper restore FunctionNeverReturns
		protected internal override void OnMouseDragged(BasicMouseEventArgs e){
			if (state != ScrollBarState.PressBar){
				return;
			}
			int hx = e.Width - 2* GraphUtil.scrollBarWidth + 2;
			int x = visibleDragStart + (int) Math.Round((e.X - dragStart)/(double) hx*main.TotalWidth());
			x = Math.Max(0, x);
			x = Math.Min(main.TotalWidth() - main.VisibleWidth, x);
			main.VisibleX = x;
			Invalidate();
		}

		protected internal override void OnMouseIsUp(BasicMouseEventArgs e){
			if (leftThread != null){
				leftThread.Abort();
				leftThread = null;
			}
			if (rightThread != null){
				rightThread.Abort();
				rightThread = null;
			}
			const ScrollBarState newState = ScrollBarState.Neutral;
			OnMouseMoved(e);
			state = newState;
			Invalidate();
		}

		protected internal override void OnMouseLeave(EventArgs e){
			if (state == ScrollBarState.PressBar){
				return;
			}
			state = ScrollBarState.Neutral;
			Invalidate();
		}
	}
}