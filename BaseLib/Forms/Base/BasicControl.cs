using System;
using System.Windows.Forms;
using BaseLib.Graphic;
using BaseLibS.Graph;

namespace BaseLib.Forms.Base{
	public class BasicControl : ScrollableControl{
		private ToolTip tip;
		private bool mouseDown;
		public BasicView view;

		public BasicControl(){
			//view = new BasicView();
			DoubleBuffered = true;
			ResizeRedraw = true;
			Margin = new Padding(0);
			Dock = DockStyle.Fill;
		}

		public void Print(IGraphics g){
			view?.OnPaint(g, Width, Height);
		}

		protected sealed override void OnPaint(PaintEventArgs e){
			base.OnPaint(e);
			if (view != null){
				try{
					view.OnPaint(new CGraphics(e.Graphics), Width, Height);
				} catch (Exception e1){
					MessageBox.Show(e1.Message + "\n" + e1.StackTrace);
				}
			}
		}

		protected sealed override void OnPaintBackground(PaintEventArgs e){
			base.OnPaintBackground(e);
			view?.OnPaintBackground(new CGraphics(e.Graphics), Width, Height);
		}

		protected sealed override void OnMouseDown(MouseEventArgs e){
			base.OnMouseDown(e);
			mouseDown = true;
			view.OnMouseIsDown(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
				() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
		}

		protected sealed override void OnMouseUp(MouseEventArgs e){
			base.OnMouseUp(e);
			mouseDown = false;
			view.OnMouseIsUp(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
				() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
		}

		protected sealed override void OnMouseMove(MouseEventArgs e){
			SetStyle(ControlStyles.Selectable, true);
			Focus();
			base.OnMouseMove(e);
			if (mouseDown){
				view.OnMouseDragged(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
					() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
			} else{
				view.OnMouseMoved(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
					() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
			}
		}

		protected sealed override void OnMouseLeave(EventArgs e){
			base.OnMouseLeave(e);
			view?.OnMouseLeave(e);
		}

		protected sealed override void OnMouseClick(MouseEventArgs e){
			base.OnMouseClick(e);
			view?.OnMouseClick(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
				() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
		}

		protected sealed override void OnMouseDoubleClick(MouseEventArgs e){
			base.OnMouseDoubleClick(e);
			view?.OnMouseDoubleClick(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
				() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
		}

		protected sealed override void OnMouseHover(EventArgs e){
			base.OnMouseHover(e);
			view?.OnMouseHover(e);
		}

		protected sealed override void OnMouseCaptureChanged(EventArgs e){
			base.OnMouseCaptureChanged(e);
			view?.OnMouseCaptureChanged(e);
		}

		protected sealed override void OnMouseEnter(EventArgs e){
			base.OnMouseEnter(e);
			view?.OnMouseEnter(e);
		}

		protected sealed override void OnMouseWheel(MouseEventArgs e){
			base.OnMouseWheel(e);
			view?.OnMouseWheel(new BasicMouseEventArgs(e.X, e.Y, e.Button == MouseButtons.Left, Width, Height,
				() => (ModifierKeys & Keys.Control) == Keys.Control, ViewToolTip));
		}

		protected sealed override void OnResize(EventArgs e){
			base.OnResize(e);
			view?.OnResize(e, Width, Height);
		}

		protected sealed override void Dispose(bool disposing){
			base.Dispose(disposing);
			view?.Dispose(disposing);
		}

		private void ViewToolTip(string text, int x, int y, int duration){
			if (tip == null){
				tip = new ToolTip();
			}
			tip.Show(text, this, x, y, duration);
		}
	}
}