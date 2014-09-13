using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BaseLib.Forms.Base;
using BaseLib.Graphic;
using BaseLib.Util;
using BaseLibS.Util;

namespace BaseLib.Forms.Colors{
	public enum Arrows{
		None,
		First,
		Second,
		Both
	}

	public sealed class ColorStripView : BasicView{
		private const int triangleHeight = 8;
		private const int triangleBase2 = 5;
		internal event ColorChangeHandler OnColorChange;
		internal bool fireChange;
		private Color[] precalcColors;
		private Pen[] precalcPens;
		private int oldLength = -1;
		internal bool refreshColors = true;
		internal Color StartupColorMin { get; set; }
		internal Color StartupColorMax { get; set; }
		internal bool Vertical { get; set; }
		internal float Weight1 { get; set; }
		internal float Weight2 { get; set; }
		internal Arrows Arrow { get; set; }
		internal int StripWidth { get; set; }
		public List<Color> Colors { get; private set; }
		public List<double> Positions { get; private set; }
		internal int mouseOverIndex = -1;
		internal int mouseDragIndex = -1;
		internal int mouseStartPos = -1;
		internal bool hasMoved;

		public ColorStripView(){
			Positions = new List<double>();
			Colors = new List<Color>();
			if (Colors.Count == 0){
				AddColor(Color.White, 0.2);
				AddColor(Color.Yellow, 0.4);
				AddColor(Color.Green, 0.85);
			}
		}

		public ColorStripView(Color c1, Color c2){
			Positions = new List<double>();
			Colors = new List<Color>();
			if (Colors.Count == 0){
				AddColor(c1, 0);
				AddColor(c2, 1.0);
			}
			Arrow = Arrows.Second;
			//Size = new Size(1000, 30);
			StripWidth = 10;
			Vertical = false;
			Weight1 = 1F;
			Weight2 = 0F;
			InitColors(Colors.ToArray(), Positions.ToArray());
		}

		internal void FireColorChanged(){
			if (OnColorChange != null){
				OnColorChange();
			}
		}

		public Color GetColorAt(double x, int width, int height){
			if (precalcColors == null){
				CalcGradient(width, height);
			}
			x = Math.Min(1, Math.Max(0, x));
			int i = (int) (x*GetLength(width, height));
			if (i == GetLength(width, height)){
				i = GetLength(width, height) - 1;
			}
			try{
				return precalcColors[i];
			} catch (Exception){
				return Color.White;
			}
		}

		public Pen GetPenAt(double x, int width, int height){
			if (precalcPens == null){
				CalcGradient(width, height);
			}
			x = Math.Min(1, Math.Max(0, x));
			int i = (int) (x*GetLength(width, height));
			if (i == GetLength(width, height)){
				i = GetLength(width, height) - 1;
			}
			return precalcPens[i];
		}

		internal void AddColor(Color color, double position){
			Colors.Add(color);
			Positions.Add(position);
		}

		public void InitColors(Color[] newColors, double[] newPositions){
			Colors.Clear();
			Colors.AddRange(newColors);
			Positions.Clear();
			Positions.AddRange(newPositions);
			precalcColors = null;
		}

		protected internal override void OnPaint(IGraphics g, int width, int height) {
			if (oldLength != GetLength(width, height)){
				refreshColors = true;
			}
			g.SmoothingMode = SmoothingMode.AntiAlias;
			int remainder = GetBreadth(width, height) - StripWidth;
			int w1 = (int) Math.Round(remainder*Weight1/(Weight1 + Weight2));
			w1 = Math.Max(1, w1);
			w1 = Math.Min(remainder - 2, w1);
			PaintStrip(g, w1, width, height);
			PaintMarkers(g, w1, width, height);
			oldLength = GetLength(width, height);
		}

		private void PaintStrip(IGraphics g, int off, int width, int height){
			Pen fgPen = new Pen(ForeColor);
			if (Vertical){
				g.DrawLine(fgPen, off - 1, 0, off - 1, height - 1);
				g.DrawLine(fgPen, off + StripWidth + 1, 0, off + StripWidth + 1, height - 1);
			} else{
				g.DrawLine(fgPen, 0, off - 1, width - 1, off - 1);
				g.DrawLine(fgPen, 0, off + StripWidth + 1, width - 1, off + StripWidth + 1);
			}
			if (refreshColors){
				CalcGradient(width, height);
				if (fireChange){
					FireColorChanged();
					fireChange = false;
				}
			}
			PaintGradient(g, off, width, height);
			refreshColors = false;
		}

		private void CalcGradient(int width, int height){
			precalcColors = new Color[GetLength(width, height)];
			precalcPens = new Pen[GetLength(width, height)];
			int[] o = ArrayUtils.Order(Positions.ToArray());
			CalcGradient(Colors[o[0]], Colors[o[0]], -1, ModelToView(Positions[o[0]], width, height));
			for (int i = 0; i < o.Length - 1; i++){
				CalcGradient(Colors[o[i]], Colors[o[i + 1]], ModelToView(Positions[o[i]], width, height),
					ModelToView(Positions[o[i + 1]], width, height));
			}
			CalcGradient(Colors[o[o.Length - 1]], Colors[o[o.Length - 1]], ModelToView(Positions[o[o.Length - 1]], width, height),
				GetLength(width, height) - 1);
		}

		private void CalcGradient(Color c1, Color c2, int a1, int a2){
			for (int j = a1 + 1; j <= a2; j++){
				double w1 = Math.Abs(a2 - j);
				double w2 = Math.Abs(j - a1);
				byte rr = (byte) Math.Round((c1.R*w1 + c2.R*w2)/(w1 + w2));
				byte gg = (byte) Math.Round((c1.G*w1 + c2.G*w2)/(w1 + w2));
				byte bb = (byte) Math.Round((c1.B*w1 + c2.B*w2)/(w1 + w2));
				precalcColors[j] = Color.FromArgb(rr, gg, bb);
				precalcPens[j] = new Pen(precalcColors[j]);
			}
		}

		private void PaintGradient(IGraphics g, int off, int width, int height){
			for (int j = 0; j < GetLength(width, height); j++){
				Pen x = precalcPens[j];
				if (Vertical){
					g.DrawLine(x, off, j, off + StripWidth, j);
				} else{
					g.DrawLine(x, j, off, j, off + StripWidth);
				}
			}
		}

		private void PaintMarkers(IGraphics g, int off, int width, int height){
			Pen fgPen = new Pen(ForeColor);
			for (int i = 0; i < Colors.Count; i++){
				Pen p = new Pen(Colors[i]);
				int a = ModelToView(Positions[i], width, height);
				int d = ((i == mouseOverIndex) && (Arrow == Arrows.First || Arrow == Arrows.Both)) ? triangleHeight : 0;
				if (Vertical){
					int e = ((i == mouseOverIndex)) && (Arrow == Arrows.Second || Arrow == Arrows.Both)
						? width - 1 - triangleHeight : width - 1;
					g.DrawLine(p, 0, a, width - 1, a);
					g.DrawLine(fgPen, d, a - 1, off - 1, a - 1);
					g.DrawLine(fgPen, d, a + 1, off - 1, a + 1);
					g.DrawLine(fgPen, off + StripWidth + 1, a - 1, e, a - 1);
					g.DrawLine(fgPen, off + StripWidth + 1, a + 1, e, a + 1);
				} else{
					int e = ((i == mouseOverIndex)) && (Arrow == Arrows.Second || Arrow == Arrows.Both)
						? height - 1 - triangleHeight : height - 1;
					g.DrawLine(p, a, 0, a, height - 1);
					g.DrawLine(fgPen, a - 1, d, a - 1, off - 1);
					g.DrawLine(fgPen, a + 1, d, a + 1, off - 1);
					g.DrawLine(fgPen, a - 1, off + StripWidth + 1, a - 1, e);
					g.DrawLine(fgPen, a + 1, off + StripWidth + 1, a + 1, e);
				}
				if (i == mouseOverIndex){
					Brush b = new SolidBrush(p.Color);
					if (Vertical){
						if (Arrow == Arrows.Second || Arrow == Arrows.Both){
							Point[] points = new[]{
								new Point(width - 1 - triangleHeight, a - 1), new Point(width - 1 - triangleHeight, a - triangleBase2),
								new Point(width - 1, a), new Point(width - 1 - triangleHeight, a + triangleBase2),
								new Point(width - 1 - triangleHeight, a + 1)
							};
							g.FillClosedCurve(b, points);
							g.DrawCurve(fgPen, points);
						}
						if (Arrow == Arrows.First || Arrow == Arrows.Both){
							Point[] points = new[]{
								new Point(triangleHeight, a - 1), new Point(triangleHeight, a - triangleBase2), new Point(0, a),
								new Point(triangleHeight, a + triangleBase2), new Point(triangleHeight, a + 1)
							};
							g.FillClosedCurve(b, points);
							g.DrawCurve(fgPen, points);
						}
					} else{
						Point[] points = new[]{
							new Point(a - 1, height - 1 - triangleHeight), new Point(a - triangleBase2, height - 1 - triangleHeight),
							new Point(a, height - 1), new Point(a + triangleBase2, height - 1 - triangleHeight),
							new Point(a + 1, height - 1 - triangleHeight)
						};
						g.FillClosedCurve(b, points);
						g.DrawCurve(fgPen, points);
						points = new[]{
							new Point(a - 1, triangleHeight), new Point(a - triangleBase2, triangleHeight), new Point(a, 0),
							new Point(a + triangleBase2, triangleHeight), new Point(a + 1, triangleHeight)
						};
						g.FillClosedCurve(b, points);
						g.DrawCurve(fgPen, points);
					}
				}
			}
		}

		internal int GetLength(int width, int height){
			return Vertical ? height : width;
		}

		private int GetBreadth(int width, int height){
			return Vertical ? width : height;
		}

		internal int ModelToView(double val, int width, int height){
			int x = (int) Math.Round(val*(GetLength(width, height) - 1));
			return x;
		}

		internal double ViewToModel(int x, int width, int height){
			return x/(double) (GetLength(width, height) - 1);
		}

		protected internal override void OnMouseIsDown(BasicMouseEventArgs e){
			mouseDragIndex = mouseOverIndex;
			mouseStartPos = Vertical ? e.Y : e.X;
			hasMoved = false;
		}

		protected internal override void OnMouseIsUp(BasicMouseEventArgs e){
			if (!hasMoved){
				if (e.IsMainButton){
					ColorDialog cd = new ColorDialog();
					if (cd.ShowDialog() == DialogResult.OK){
						if (mouseOverIndex != -1){
							Colors[mouseOverIndex] = cd.Color;
						} else{
							Colors.Add(cd.Color);
							Positions.Add(ViewToModel(Vertical ? e.Y : e.X, e.Width, e.Height));
						}
						refreshColors = true;
						fireChange = true;
						Invalidate();
					}
				} else{
					if (mouseOverIndex != -1 && Colors.Count > 2){
						Colors.RemoveAt(mouseOverIndex);
						Positions.RemoveAt(mouseOverIndex);
						refreshColors = true;
						fireChange = true;
						Invalidate();
					}
				}
			} else{
				FireColorChanged();
			}
			mouseDragIndex = -1;
		}

		protected internal override void OnMouseDragged(BasicMouseEventArgs e){
			if (e.IsMainButton){
				int w = Vertical ? e.Y : e.X;
				if (w != mouseStartPos){
					hasMoved = true;
				}
				if (mouseDragIndex != -1){
					w = Math.Max(0, w);
					w = Math.Min(w, GetLength(e.Width, e.Height) - 1);
					Positions[mouseDragIndex] = ViewToModel(w, e.Width, e.Height);
					refreshColors = true;
					Invalidate();
				}
			}
		}

		protected internal override void OnMouseMoved(BasicMouseEventArgs e){
			int x = Vertical ? e.Y : e.X;
			for (int i = 0; i < Positions.Count; i++){
				int p = ModelToView(Positions[i], e.Width, e.Height);
				if (p == x){
					int d = mouseOverIndex - i;
					mouseOverIndex = i;
					if (d != 0){
						Invalidate();
					}
					return;
				}
			}
			for (int i = 0; i < Positions.Count; i++){
				int p = ModelToView(Positions[i], e.Width, e.Height);
				if (Math.Abs(p - x) < 2){
					int d = mouseOverIndex - i;
					mouseOverIndex = i;
					if (d != 0){
						Invalidate();
					}
					return;
				}
			}
			int f = mouseOverIndex + 1;
			mouseOverIndex = -1;
			if (f != 0){
				Invalidate();
			}
		}
	}
}