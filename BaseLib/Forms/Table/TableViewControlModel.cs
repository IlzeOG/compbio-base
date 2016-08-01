﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BaseLib.Graphic;
using BaseLib.Properties;
using BaseLib.Wpf;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Symbol;
using BaseLibS.Table;
using BaseLibS.Util;

namespace BaseLib.Forms.Table{
	public class TableViewControlModel : ICompoundScrollableControlModel{
		private const int rowHeight = 22;
		private static readonly Color2 gridColor = Color2.FromArgb(172, 168, 153);
		private static readonly Pen2 gridPen = new Pen2(gridColor);
		private static readonly Color2 headerGridColor = Color2.FromArgb(199, 197, 178);
		private static readonly Pen2 headerGridPen = new Pen2(headerGridColor);
		private static readonly Color2 shadow1Color = Color2.FromArgb(203, 199, 184);
		private static readonly Pen2 shadow1Pen = new Pen2(shadow1Color);
		private static readonly Color2 shadow2Color = Color2.FromArgb(214, 210, 194);
		private static readonly Pen2 shadow2Pen = new Pen2(shadow2Color);
		private static readonly Color2 shadow3Color = Color2.FromArgb(226, 222, 205);
		private static readonly Pen2 shadow3Pen = new Pen2(shadow3Color);
		private static readonly Color2 headerColor = Color2.FromArgb(235, 234, 219);
		private static readonly Brush2 headerBrush = new Brush2(headerColor);
		private static readonly Color2 oddBgColor = Color2.FromArgb(224, 224, 224);
		private static readonly Brush2 oddBgBrush = new Brush2(oddBgColor);
		private static readonly Color2 selectBgColor = Color2.FromArgb(49, 106, 197);
		private static readonly Brush2 selectBgBrush = new Brush2(selectBgColor);
		private static readonly Color2 selectHeader1Color = Color2.FromArgb(165, 165, 151);
		private static readonly Pen2 selectHeader1Pen = new Pen2(selectHeader1Color);
		private static readonly Color2 selectHeader2Color = Color2.FromArgb(193, 194, 184);
		private static readonly Pen2 selectHeader2Pen = new Pen2(selectHeader2Color);
		private static readonly Color2 selectHeader3Color = Color2.FromArgb(208, 209, 201);
		private static readonly Pen2 selectHeader3Pen = new Pen2(selectHeader3Color);
		private static readonly Color2 selectHeader4Color = Color2.FromArgb(222, 223, 216);
		private static readonly Brush2 selectHeader4Brush = new Brush2(selectHeader4Color);
		private bool multiSelect = true;
		public event EventHandler SelectionChanged;
		private ContextMenuStrip contextMenuStrip;
		private int[] columnWidthSums;
		private int[] columnWidthSumsOld;
		private ITableModel model;
		private static Font2 defaultFont = new Font2("Arial", 9);
		private Font2 textFont;
		private Font2 headerFont;
		private Brush2 textBrush = Brushes2.Black;
		private Color2 textColor = Color2.Black;
		private bool[] modelRowSel;
		private int[] order;
		private int[] inverseOrder;
		private int selectStart = -1;
		private int sortCol = -1;
		private int resizeCol = -1;
		private int helpCol = -1;
		private int currentRow = -1;
		private int currentCol = -1;
		private int currentX = -1;
		private int currentY = -1;
		private int toolTipX = -1;
		private int toolTipY = -1;
		private bool matrixHelp;
		private SortState sortState = SortState.Unsorted;
		private int colDragX = -1;
		private int deltaDragX;
		private bool hasShowInPerseus;
		private FindForm findForm;
		internal int origColumnHeaderHeight;
		private const int maxColHeaderStringSplits = 3;
		private bool sortable;
		public Action<string> SetCellText { get; set; }
		private ICompoundScrollableControl control;

		public void Register(ICompoundScrollableControl control1){
			control = control1;
			sortable = true;
			control1.RowHeaderWidth = 70;
			control1.ColumnHeaderHeight = 26;
			origColumnHeaderHeight = 26;
			InitContextMenu();
			float dpiScaleX = WpfUtils.GetDpiScaleX();
			defaultFont = new Font2("Arial", 9/dpiScaleX);
			textFont = defaultFont;
			headerFont = defaultFont;
			control1.OnMouseIsDownMainView = e =>{
				if (!control1.Enabled){
					return;
				}
				if (e.IsMainButton){
					try{
						int row = (control1.VisibleY + e.Y)/rowHeight;
						if (model == null || row >= model.RowCount || row < 0){
							return;
						}
						bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
						bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
						if (!ctrl || !multiSelect){
							ClearSelection();
						}
						int ox = order[row];
						if (ox < 0 || ox >= modelRowSel.Length){
							return;
						}
						modelRowSel[ox] = !ctrl || !modelRowSel[ox];
						if (multiSelect && shift && selectStart != -1){
							SelectRange(selectStart, row);
						} else{
							selectStart = row;
						}
						control1.Invalidate(true);
					} catch (Exception){}
					SelectionChanged?.Invoke(this, new EventArgs());
					if (SetCellText != null){
						int row = (control1.VisibleY + e.Y)/rowHeight;
						if (model == null || row >= model.RowCount || row < 0){
							return;
						}
						if (columnWidthSums == null){
							return;
						}
						int x1 = control1.VisibleX + e.X;
						int ind = ArrayUtils.CeilIndex(columnWidthSums, x1);
						int ox = order[row];
						SetCellText("" + TableModel.GetEntry(ox, ind));
					}
				}
			};
			control1.OnMouseDraggedRowHeaderView = e =>{
				if (!control1.Enabled){
					return;
				}
				control1.OnMouseDraggedMainView(e);
			};
			control1.OnMouseHoverMainView = e => { HandleToolTip(true); };
			control1.OnMouseDraggedMainView = e =>{
				if (!control1.Enabled){
					return;
				}
				if (!MultiSelect){
					control1.OnMouseIsDownMainView(e);
					return;
				}
				if (e.IsMainButton){
					if (modelRowSel == null){
						return;
					}
					int row = (control1.VisibleY + e.Y)/rowHeight;
					if (row >= modelRowSel.Length || row < 0){
						return;
					}
					modelRowSel[order[row]] = true;
					if (selectStart != row){
						selectStart = row;
						control1.Invalidate(true);
						SelectionChanged?.Invoke(this, new EventArgs());
					}
				}
			};
			control1.OnMouseDraggedColumnHeaderView = e =>{
				if (!control1.Enabled){
					return;
				}
				if (columnWidthSumsOld == null || columnWidthSums == null){
					return;
				}
				if (resizeCol != -1){
					int mind = resizeCol == 0
						? 6 - columnWidthSumsOld[0]
						: columnWidthSumsOld[resizeCol - 1] - columnWidthSumsOld[resizeCol] + 6;
					deltaDragX = Math.Max(mind, e.X - colDragX);
					for (int i = resizeCol; i < columnWidthSums.Length; i++){
						columnWidthSums[i] = columnWidthSumsOld[i] + deltaDragX;
					}
					control1.Invalidate(true);
				}
			};
			control1.OnMouseMoveMainView = e =>{
				currentX = e.X;
				currentY = e.Y;
				HandleToolTip(false);
				CalcCurrentRowAndColumn(e);
			};
			control1.OnMouseDoubleClickMainView = e =>{
				if (e.IsMainButton){
					int row = (control1.VisibleY + e.Y)/rowHeight;
					if (model == null || row >= model.RowCount || row < 0){
						return;
					}
				}
				//TODO: edit
			};
			control1.OnMouseIsDownRowHeaderView = e =>{
				if (!control1.Enabled){
					return;
				}
				control1.OnMouseIsDownMainView(e);
			};
			control1.OnMouseMoveColumnHeaderView = e =>{
				if (!control1.Enabled){
					return;
				}
				int x1 = control1.VisibleX + e.X;
				if (columnWidthSums == null){
					return;
				}
				int ind = ArrayUtils.ClosestIndex(columnWidthSums, x1);
				if (ind >= 0){
					if (Math.Abs(columnWidthSums[ind] - x1) < 5){
						Cursor.Current = Cursors.VSplit;
						resizeCol = ind;
					} else{
						Cursor.Current = Cursors.Default;
						resizeCol = -1;
					}
				}
				int indf = ArrayUtils.CeilIndex(columnWidthSums, x1);
				try{
					if (model != null){
						if (indf >= 0 && !string.IsNullOrEmpty(model.GetColumnDescription(indf)) &&
							Math.Abs((indf == 0 ? 0 : columnWidthSums[indf - 1]) + 9 - x1) < 4 && e.Y > 7 && e.Y < 17){
							if (helpCol != indf){
								helpCol = indf;
								control1.InvalidateColumnHeaderView();
							}
						} else{
							if (helpCol != -1){
								helpCol = -1;
								control1.InvalidateColumnHeaderView();
							}
						}
					}
				} catch (Exception){}
			};
			control1.OnMouseIsUpColumnHeaderView = e => { control1.HideColumnViewToolTip(); };
			control1.OnMouseIsUpCornerView = e => { control1.HideColumnViewToolTip(); };
			control1.OnMouseIsDownColumnHeaderView = e =>{
				if (!control1.Enabled){
					return;
				}
				if (columnWidthSums == null){
					return;
				}
				if (resizeCol != -1){
					colDragX = e.X;
					columnWidthSumsOld = (int[]) columnWidthSums.Clone();
					return;
				}
				if (helpCol >= 0){
					control1.SetColumnViewToolTipTitle(model.GetColumnName(helpCol));
					StringBuilder text = new StringBuilder();
					string[] wrapped = StringUtils.Wrap(model.GetColumnDescription(helpCol), 75);
					for (int i = 0; i < wrapped.Length; ++i){
						string s = wrapped[i];
						text.Append(s);
						if (i < wrapped.Length - 1){
							text.Append("\n");
						}
					}
					control1.ShowColumnViewToolTip(text.ToString(), e.X + 75, e.Y + 5);
					helpCol = -1;
					control1.InvalidateColumnHeaderView();
					return;
				}
				if (Sortable && e.IsMainButton){
					int ind = Math.Min(columnWidthSums.Length - 1, ArrayUtils.FloorIndex(columnWidthSums, control1.VisibleX + e.X) + 1);
					if (sortCol == ind){
						switch (sortState){
							case SortState.Unsorted:
								Sort();
								break;
							case SortState.Increasing:
								InvertOrder();
								break;
							case SortState.Decreasing:
								Unsort();
								break;
						}
					} else{
						sortCol = ind;
						Sort();
					}
				}
				control1.Invalidate(true);
			};
			control1.OnMouseIsDownCornerView = e =>{
				if (!control1.Enabled){
					return;
				}
				if (matrixHelp){
					control1.SetColumnViewToolTipTitle(model.Name);
					StringBuilder text = new StringBuilder();
					string[] wrapped = StringUtils.Wrap(model.Description, 75);
					for (int i = 0; i < wrapped.Length; ++i){
						string s = wrapped[i];
						text.Append(s);
						if (i < wrapped.Length - 1){
							text.Append("\n");
						}
					}
					control1.ShowColumnViewToolTip(text.ToString(), e.X + 75, e.Y + 5);
					matrixHelp = false;
					control1.InvalidateCornerView();
					return;
				}
				control1.Invalidate(true);
			};
			control1.OnMouseMoveCornerView = e =>{
				if (model == null){
					return;
				}
				int x1 = e.X;
				if (!string.IsNullOrEmpty(model.Description) && Math.Abs(9 - x1) < 4 && e.Y > 7 && e.Y < 17){
					if (!matrixHelp){
						matrixHelp = true;
						control1.InvalidateCornerView();
					} else{
						matrixHelp = false;
						control1.InvalidateCornerView();
					}
				}
			};
			control1.OnPaintRowHeaderView = (g, y, height) =>{
				if (model == null){
					return;
				}
				g.FillRectangle(headerBrush, 0, 0, control1.RowHeaderWidth - 1, height);
				g.DrawLine(gridPen, 0, 0, 0, height);
				g.DrawLine(gridPen, control1.RowHeaderWidth - 1, 0, control1.RowHeaderWidth - 1, height);
				g.DrawLine(Pens2.White, 1, 0, 1, height);
				g.DrawLine(shadow1Pen, control1.RowHeaderWidth - 2, 0, control1.RowHeaderWidth - 2, height);
				g.DrawLine(shadow2Pen, control1.RowHeaderWidth - 3, 0, control1.RowHeaderWidth - 3, height);
				g.DrawLine(shadow3Pen, control1.RowHeaderWidth - 4, 0, control1.RowHeaderWidth - 4, height);
				int offset = -y%rowHeight;
				for (int y1 = offset - rowHeight; y1 < height; y1 += rowHeight){
					int row = (y + y1)/rowHeight;
					if (model == null || row > model.RowCount){
						break;
					}
					g.DrawLine(headerGridPen, 5, y1 - 1, control1.RowHeaderWidth - 6, y1 - 1);
					g.DrawLine(Pens2.White, 5, y1, control1.RowHeaderWidth - 6, y1);
				}
				for (int y1 = offset - rowHeight; y1 < height; y1 += rowHeight){
					int row = (y + y1)/rowHeight;
					if (row < 0){
						continue;
					}
					if (model == null || row >= model.RowCount){
						break;
					}
					if (ViewRowIsSelected(row)){
						g.DrawLine(selectHeader1Pen, 2, y1 + 1, control1.RowHeaderWidth - 2, y1 + 1);
						g.DrawLine(selectHeader1Pen, 2, y1 + rowHeight, control1.RowHeaderWidth - 2, y1 + rowHeight);
						g.DrawLine(selectHeader1Pen, control1.RowHeaderWidth - 2, y1 + 1, control1.RowHeaderWidth - 2, y1 + rowHeight);
						g.DrawLine(selectHeader2Pen, 2, y1 + 2, control1.RowHeaderWidth - 3, y1 + 2);
						g.DrawLine(selectHeader2Pen, 2, y1 + 2, 2, y1 + rowHeight - 1);
						g.DrawLine(selectHeader3Pen, 3, y1 + 3, control1.RowHeaderWidth - 3, y1 + 3);
						g.DrawLine(selectHeader3Pen, 3, y1 + 3, 3, y1 + rowHeight - 1);
						g.FillRectangle(selectHeader4Brush, 4, y1 + 4, control1.RowHeaderWidth - 6, rowHeight - 4);
					}
					g.DrawString("" + (row + 1), textFont, Brushes2.Black, 5, y1 + 4);
				}
			};
			control1.OnPaintColumnHeaderView = (g, x, width) =>{
				if (model == null){
					return;
				}
				g.FillRectangle(headerBrush, 0, 0, width, control1.ColumnHeaderHeight - 1);
				g.DrawLine(gridPen, 0, 0, width, 0);
				g.DrawLine(gridPen, 0, control1.ColumnHeaderHeight - 1, width, control1.ColumnHeaderHeight - 1);
				g.DrawLine(Pens2.White, 0, 1, width, 1);
				g.DrawLine(shadow1Pen, 0, control1.ColumnHeaderHeight - 2, width, control1.ColumnHeaderHeight - 2);
				g.DrawLine(shadow2Pen, 0, control1.ColumnHeaderHeight - 3, width, control1.ColumnHeaderHeight - 3);
				g.DrawLine(shadow3Pen, 0, control1.ColumnHeaderHeight - 4, width, control1.ColumnHeaderHeight - 4);
				if (columnWidthSums != null){
					int startInd = ArrayUtils.CeilIndex(columnWidthSums, x);
					int endInd = ArrayUtils.FloorIndex(columnWidthSums, x + width);
					if (startInd >= 0){
						for (int i = startInd; i <= endInd; i++){
							int x1 = columnWidthSums[i] - x;
							g.DrawLine(headerGridPen, x1, 5, x1, control1.ColumnHeaderHeight - 6);
							g.DrawLine(Pens2.White, x1 + 1, 5, x1 + 1, control1.ColumnHeaderHeight - 6);
						}
					}
				}
				if (columnWidthSums != null && columnWidthSums.Length > 0){
					int startInd = Math.Max(0, ArrayUtils.CeilIndex(columnWidthSums, x) - 1);
					int endInd = Math.Min(columnWidthSums.Length - 1, ArrayUtils.FloorIndex(columnWidthSums, x + width) + 1);
					if (startInd >= 0){
						for (int i = startInd; i <= endInd; i++){
							int x1 = (i > 0 ? columnWidthSums[i - 1] : 0) - x;
							int w = i == 0 ? columnWidthSums[0] : columnWidthSums[i] - columnWidthSums[i - 1];
							string[] q = GetStringHeader(g, i, w, headerFont);
							for (int j = 0; j < q.Length; j++){
								g.DrawString(q[j], headerFont, Brushes2.Black, x1 + 3, 4 + 11*j);
							}
						}
						if (sortCol != -1 && sortState != SortState.Unsorted){
							int x1 = columnWidthSums[sortCol] - x - 11;
							if (x1 >= -15 && x1 <= width){
								g.DrawImage(
									sortState == SortState.Increasing
										? GraphUtils.ToBitmap2(Resources.arrowDown1)
										: GraphUtils.ToBitmap2(Resources.arrowUp1), x1, 6, 9, 13);
							}
						}
						if (helpCol != -1){
							int x1 = (helpCol == 0 ? 0 : columnWidthSums[helpCol - 1]) - x + 5;
							if (x1 >= -15 && x1 <= width){
								g.DrawImage(GraphUtils.ToBitmap2(Resources.question12), x1, 7, 10, 10);
							}
						}
						if (model != null && model.AnnotationRowsCount > 0){
							for (int i = startInd; i <= endInd; i++){
								int x1 = (i > 0 ? columnWidthSums[i - 1] : 0) - x;
								int x2 = (i >= 0 ? columnWidthSums[i] : 0) - x;
								for (int k = 0; k < model.AnnotationRowsCount; k++){
									int y1 = origColumnHeaderHeight + k*rowHeight;
									g.DrawLine(headerGridPen, x1 + 5, y1 - 1, x2 - 6, y1 - 1);
									g.DrawLine(Pens2.White, x1 + 5, y1, x2 - 6, y1);
									string s = (string) model.GetAnnotationRowValue(k, i);
									if (s != null){
										g.DrawString("" + GetStringValue(g, s, x2 - x1 - 2, headerFont), textFont, Brushes2.Black, x1 + 3, y1 + 3);
									}
								}
							}
						}
					}
				}
			};
			control1.OnPaintCornerView = g =>{
				if (model == null){
					return;
				}
				g.FillRectangle(headerBrush, 0, 0, control1.RowHeaderWidth - 1, control1.ColumnHeaderHeight - 1);
				g.DrawRectangle(gridPen, 0, 0, control1.RowHeaderWidth - 1, control1.ColumnHeaderHeight - 1);
				g.DrawLine(Pens2.White, 1, 1, control1.RowHeaderWidth - 2, 1);
				g.DrawLine(Pens2.White, 1, 1, 1, control1.ColumnHeaderHeight - 2);
				if (matrixHelp){
					g.DrawImage(GraphUtils.ToBitmap2(Resources.question12), 7, 7, 10, 10);
				}
				if (model != null && model.AnnotationRowsCount > 0){
					for (int i = 0; i < model.AnnotationRowsCount; i++){
						int y1 = origColumnHeaderHeight + i*rowHeight;
						g.DrawLine(headerGridPen, 5, y1 - 1, control1.RowHeaderWidth - 6, y1 - 1);
						g.DrawLine(Pens2.White, 5, y1, control1.RowHeaderWidth - 6, y1);
						string s = model.GetAnnotationRowName(i);
						if (s != null){
							g.DrawString("" + GetStringValue(g, s, control1.RowHeaderWidth - 6, headerFont), textFont, Brushes2.Black, 3,
								y1 + 3);
						}
					}
				}
			};
			control1.OnPaintMainView = (g, x, y, width, height) =>{
				if (model == null){
					return;
				}
				try{
					CheckSizes();
					g.FillRectangle(Brushes2.White, 0, 0, width, height);
					int offset = -y%rowHeight;
					if (columnWidthSums == null || columnWidthSums.Length == 0){
						return;
					}
					for (int y1 = offset; y1 < height; y1 += rowHeight){
						int xmax = Math.Min(width, columnWidthSums[columnWidthSums.Length - 1] - x);
						g.DrawLine(gridPen, 0, y1, xmax, y1);
						int row = (y + y1)/rowHeight;
						if (row < 0){
							continue;
						}
						if (model == null || row >= model.RowCount){
							break;
						}
						bool sel = ViewRowIsSelected(row);
						if (sel){
							g.FillRectangle(selectBgBrush, 0, y1 + 1, xmax, rowHeight - 1);
						} else if (row%2 == 1){
							g.FillRectangle(oddBgBrush, 0, y1 + 1, xmax, rowHeight - 1);
						}
						int startInd = Math.Max(0, ArrayUtils.CeilIndex(columnWidthSums, x) - 1);
						int endInd = Math.Min(columnWidthSums.Length - 1, ArrayUtils.FloorIndex(columnWidthSums, x + width) + 1);
						if (order.Length == 0){
							return;
						}
						if (startInd >= 0 && endInd >= 0){
							for (int i = startInd; i <= endInd; i++){
								if (i >= columnWidthSums.Length){
									continue;
								}
								int x1 = (i > 0 ? columnWidthSums[i - 1] : 0) - x;
								int w;
								if (i == 0){
									w = columnWidthSums[0];
								} else{
									if (i >= columnWidthSums.Length || i - 1 < 0){
										continue;
									}
									w = columnWidthSums[i] - columnWidthSums[i - 1];
								}
								RenderCell(g, sel, order[row], i, w, x1, y1);
							}
						}
					}
					{
						int startInd = ArrayUtils.CeilIndex(columnWidthSums, x);
						int endInd = ArrayUtils.FloorIndex(columnWidthSums, x + width);
						if (startInd >= 0 && endInd >= 0){
							int ymax = Math.Min(height, rowHeight*model.RowCount - y);
							for (int i = startInd; i <= endInd; i++){
								if (i >= columnWidthSums.Length || i < 0){
									continue;
								}
								int x1 = columnWidthSums[i] - x;
								g.DrawLine(gridPen, x1, 0, x1, ymax);
							}
						}
					}
				} catch (Exception){
					//This is an exceptional case where we put an unspecific try/catch block around code. Everything is working fine
					//except for extremely rare index out of bounds crashes, which are probably due to lack of thread safety. This avoilds
					//crashes of the MaxQuant interface during very long running times.
				}
			};
			control1.TotalWidth = () =>{
				if (model == null){
					return 0;
				}
				if (columnWidthSums == null){
					return 0;
				}
				int ind = model.ColumnCount - 1;
				if (ind < 0 || ind >= columnWidthSums.Length){
					return 0;
				}
				return columnWidthSums[ind] + 5;
			};
			control1.TotalHeight = () => rowHeight*model?.RowCount + 5 ?? 0;
			control1.DeltaX = () => 40;
			control1.DeltaY = () => rowHeight;
			control1.DeltaUpToSelection = () =>{
				int[] inds = GetSelectedRowsView();
				if (inds.Length == 0){
					return control1.VisibleY;
				}
				int visRow = (control1.VisibleY + rowHeight - 1)/rowHeight;
				int ind = Array.BinarySearch(inds, visRow);
				if (ind >= 0){
					if (ind < 1){
						return control1.VisibleY;
					}
					return (visRow - inds[ind - 1])*rowHeight;
				}
				ind = -1 - ind;
				if (ind < 1){
					return control1.VisibleY;
				}
				return (visRow - inds[ind - 1])*rowHeight;
			};
			control1.DeltaDownToSelection = () =>{
				int[] inds = GetSelectedRowsView();
				if (inds.Length == 0){
					return control1.TotalHeight() - control1.VisibleY - control1.VisibleHeight;
				}
				int visRow = (control1.VisibleY + rowHeight - 1)/rowHeight;
				int ind = Array.BinarySearch(inds, visRow);
				if (ind >= 0){
					if (ind >= inds.Length - 1){
						return control1.TotalHeight() - control1.VisibleY - control1.VisibleHeight;
					}
					return (inds[ind + 1] - visRow)*rowHeight;
				}
				ind = -1 - ind;
				if (ind >= inds.Length){
					return control1.TotalHeight() - control1.VisibleY - control1.VisibleHeight;
				}
				return (inds[ind] - visRow)*rowHeight;
			};
		}

		public bool Sortable{
			get { return sortable; }
			set{
				sortable = value;
				InitContextMenu();
			}
		}

		public bool HasShowInPerseus{
			get { return hasShowInPerseus; }
			set{
				hasShowInPerseus = value;
				InitContextMenu();
			}
		}

		public void InitContextMenu(){
			contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Items.Add(InitMenuItem("Find...", FindToolStripMenuItemClick));
			if (sortable){
				contextMenuStrip.Items.Add(InitMenuItem("Select all", SelectAllToolStripMenuItemClick));
				contextMenuStrip.Items.Add(InitMenuItem("Clear selection", ClearSelectionToolStripMenuItemClick));
				if (multiSelect){
					contextMenuStrip.Items.Add(InitMenuItem("Invert selection", InvertSelectionToolStripMenuItemClick));
				}
				contextMenuStrip.Items.Add(InitMenuItem("Bring selection to top", SelectionTopToolStripMenuItemClick));
			}
			contextMenuStrip.Items.Add(InitMenuItem("Paste selection...", PasteSelectionToolStripMenuItemClick));
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(InitMenuItem("Font...", FontsToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Monospace font", MonospaceToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Default font", DefaultToolStripMenuItemClick));
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(InitMenuItem("Plain matrix export...", ExportToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Copy selected rows", CopySelectedRowsToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Copy cell", CopyCellToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Copy column (full)", CopyColumnFullToolStripMenuItemClick));
			contextMenuStrip.Items.Add(InitMenuItem("Copy column (selected rows)", CopyColumnSelectionToolStripMenuItemClick));
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			if (hasShowInPerseus){
				contextMenuStrip.Items.Add(new ToolStripSeparator());
				contextMenuStrip.Items.Add(InitMenuItem("Show in Perseus (all)", ShowAllPerseusToolStripMenuItemClick));
				contextMenuStrip.Items.Add(InitMenuItem("Show in Perseus (selected rows)", ShowSelectedPerseusToolStripMenuItemClick));
				contextMenuStrip.Items.Add(InitMenuItem("Perseus properties", PerseusPropertiesToolStripMenuItemClick));
			}
			//contextMenuStrip.Size = new Size(210, 142);
			//perseusToolStripSeparator.Size = new Size(206, 6);
			//TODO
			((Control) control).ContextMenuStrip = contextMenuStrip;
		}

		private static ToolStripMenuItem InitMenuItem(string text, EventHandler action){
			ToolStripMenuItem menuItem = new ToolStripMenuItem{Size = new Size(209, 22), Text = text};
			menuItem.Click += action;
			return menuItem;
		}

		private void PerseusPropertiesToolStripMenuItemClick(object sender, EventArgs e){
			//TODO
		}

		private void ShowSelectedPerseusToolStripMenuItemClick(object sender, EventArgs e){
			//TODO
		}

		private void ShowAllPerseusToolStripMenuItemClick(object sender, EventArgs e){
			//TODO
		}

		public bool MultiSelect{
			get { return multiSelect; }
			set{
				multiSelect = value;
				InitContextMenu();
			}
		}

		public bool HasHelp { get; set; } = true;

		public void SetSelectedRow(int row){
			SetSelectedRows(new[]{row}, false, true);
		}

		public void SetSelectedRow(int row, bool add, bool fire){
			SetSelectedRows(new[]{row}, add, fire);
		}

		public void SetSelectedRows(IList<int> rows){
			SetSelectedRows(rows, false, true);
		}

		public void SetSelectedRows(IList<int> rows, bool add, bool fire){
			if (!add || modelRowSel == null){
				modelRowSel = new bool[RowCount];
			}
			foreach (int row in rows.Where(row => row >= 0)){
				modelRowSel[row] = !add || !modelRowSel[row];
			}
			if (fire){
				SelectionChanged?.Invoke(this, new EventArgs());
			}
		}

		public void FireSelectionChange(){
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		public void SetSelectedRowAndMove(int row){
			SetSelectedRowsAndMove(new[]{row});
		}

		public void SetSelectedRowsAndMove(IList<int> rows){
			modelRowSel = new bool[RowCount];
			foreach (int row in rows){
				modelRowSel[row] = true;
			}
			CheckSizes();
			if (rows.Count > 0){
				ScrollToRow(inverseOrder[rows[0]]);
			} else{
				control.Invalidate(true);
			}
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		public int GetSelectedRow(){
			int[] sel = GetSelectedRows();
			if (sel.Length == 0){
				return -1;
			}
			return sel[0];
		}

		public int[] GetSelectedRows(){
			if (model == null){
				return new int[0];
			}
			CheckSizes();
			List<int> result = new List<int>();
			for (int i = 0; i < model.RowCount; i++){
				if (modelRowSel[i]){
					result.Add(i);
				}
			}
			return result.ToArray();
		}

		public bool HasSelectedRows(){
			if (model == null){
				return false;
			}
			CheckSizes();
			for (int i = 0; i < model.RowCount; i++){
				if (modelRowSel[i]){
					return true;
				}
			}
			return false;
		}

		public int[] GetSelectedRowsView(){
			if (model == null){
				return new int[0];
			}
			CheckSizes();
			List<int> result = new List<int>();
			for (int i = 0; i < model.RowCount; i++){
				if (modelRowSel[order[i]]){
					result.Add(i);
				}
			}
			return result.ToArray();
		}

		public ITableModel TableModel{
			get { return model; }
			set{
				model = value;
				if (model == null){
					return;
				}
				control.VisibleX = 0;
				control.VisibleY = 0;
				modelRowSel = new bool[model.RowCount];
				order = ArrayUtils.ConsecutiveInts(model.RowCount);
				inverseOrder = ArrayUtils.ConsecutiveInts(model.RowCount);
				columnWidthSums = new int[model.ColumnCount];
				if (model.ColumnCount > 0){
					columnWidthSums[0] = model.GetColumnWidth(0);
				}
				for (int i = 1; i < model.ColumnCount; i++){
					columnWidthSums[i] = columnWidthSums[i - 1] + model.GetColumnWidth(i);
				}
				if (model.AnnotationRowsCount > 0){
					control.ColumnHeaderHeight = (origColumnHeaderHeight + model.AnnotationRowsCount*rowHeight);
				}
			}
		}

		public int RowCount => model?.RowCount ?? 0;

		public bool ViewRowIsSelected(int row){
			if (modelRowSel == null){
				return false;
			}
			if (row < 0 || order == null || row >= order.Length){
				return false;
			}
			try{
				return modelRowSel[order[row]];
			} catch (IndexOutOfRangeException){
				return false;
			}
		}

		public bool ModelRowIsSelected(int row){
			if (modelRowSel == null){
				return false;
			}
			if (row < 0 || row >= modelRowSel.Length){
				return false;
			}
			try{
				return modelRowSel[row];
			} catch (IndexOutOfRangeException){
				return false;
			}
		}

		public static bool IsMulti(ColumnType columnType){
			return columnType == ColumnType.MultiNumeric || columnType == ColumnType.MultiInteger;
		}

		public static bool IsNumeric(ColumnType columnType){
			return columnType == ColumnType.Numeric || columnType == ColumnType.Integer || columnType == ColumnType.MultiInteger ||
					columnType == ColumnType.MultiNumeric;
		}

		public void ScrollToRow(int row){
			if ((RowCount - row)*rowHeight < control.VisibleHeight){
				ScrollToEnd();
			} else{
				control.VisibleY = row*rowHeight;
			}
		}

		public void ScrollToEnd(){
			if (RowCount*rowHeight < control.VisibleHeight){
				control.VisibleY = 0;
			} else{
				control.VisibleY = (RowCount - (int) (control.VisibleHeight/(double) rowHeight))*rowHeight;
			}
		}

		public int GetModelIndex(int rowIndView){
			return order[rowIndView];
		}

		public int GetViewIndex(int rowIndModel){
			return inverseOrder[rowIndModel];
		}

		public void ClearSelection(){
			if (modelRowSel == null){
				return;
			}
			for (int i = 0; i < modelRowSel.Length; i++){
				modelRowSel[i] = false;
			}
		}

		public void ClearSelectionFire(){
			ClearSelection();
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		public void SelectAll(){
			if (!MultiSelect){
				return;
			}
			CheckSizes();
			bool[] x = new bool[RowCount];
			for (int i = 0; i < x.Length; i++){
				x[i] = true;
			}
			SetSelection(x);
		}

		public long SelectedCount{
			get{
				if (modelRowSel == null){
					return 0;
				}
				long count = 0;
				foreach (bool b in modelRowSel){
					if (b){
						count++;
					}
				}
				return count;
			}
		}

		public void SetSelectedViewIndex(int index){
			CheckSizes();
			if (!multiSelect){
				ClearSelection();
			}
			modelRowSel[order[index]] = true;
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		public void SetSelectedIndex(int index){
			SetSelectedIndex(index, this);
		}

		public void SetSelectedIndex(int index, object sender){
			CheckSizes();
			if (!multiSelect){
				ClearSelection();
			}
			modelRowSel[index] = true;
			SelectionChanged?.Invoke(sender, new EventArgs());
		}

		public void SetSelection(bool[] s){
			modelRowSel = s;
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		public object GetEntry(int row, int col){
			CheckSizes();
			return model?.GetEntry(order[row], col);
		}

		private void FindToolStripMenuItemClick(object sender, EventArgs e){
			Find();
		}

		private void SelectAllToolStripMenuItemClick(object sender, EventArgs e){
			SelectAll();
			control.Invalidate(true);
		}

		private void ClearSelectionToolStripMenuItemClick(object sender, EventArgs e){
			ClearSelectionFire();
			control.Invalidate(true);
		}

		private void FontsToolStripMenuItemClick(object sender, EventArgs e){
			ChangeFonts();
		}

		private void MonospaceToolStripMenuItemClick(object sender, EventArgs e){
			MonospaceFont();
		}

		private void DefaultToolStripMenuItemClick(object sender, EventArgs e){
			DefaultFont1();
		}

		private void Find(){
			if (model == null){
				return;
			}
			if (findForm == null){
				findForm = new FindForm(this, control);
			}
			findForm.Visible = false;
			findForm.BringToFront();
			findForm.Show();
			findForm.FocusInputField();
			findForm.Activate();
		}

		private void MonospaceFont(){
			textFont = new Font2("Courier", 9);
			control.Invalidate(true);
		}

		private void DefaultFont1(){
			textFont = defaultFont;
			control.Invalidate(true);
		}

		private void ChangeFonts(){
			if (model == null){
				return;
			}
			FontDialog fontDialog = new FontDialog{
				ShowColor = true,
				Font = GraphUtils.ToFont(textFont),
				Color = Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B)
			};
			if (fontDialog.ShowDialog() != DialogResult.Cancel){
				textFont = GraphUtils.ToFont2(fontDialog.Font);
				textColor = Color2.FromArgb(fontDialog.Color.A, fontDialog.Color.R, fontDialog.Color.G, fontDialog.Color.B);
				textBrush = new Brush2(textColor);
			}
			fontDialog.Dispose();
			control.Invalidate(true);
		}

		public void BringSelectionToTop(){
			if (model == null){
				return;
			}
			if (modelRowSel == null){
				return;
			}
			List<int> l1 = new List<int>();
			List<int> l2 = new List<int>();
			for (int i = 0; i < modelRowSel.Length; i++){
				if (modelRowSel[i]){
					l1.Add(i);
				} else{
					l2.Add(i);
				}
			}
			order = ArrayUtils.Concat(l1, l2);
			control.VisibleY = 0;
			control.Invalidate(true);
		}

		private void InvertSelectionToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			if (modelRowSel == null){
				return;
			}
			for (int i = 0; i < modelRowSel.Length; i++){
				modelRowSel[i] = !modelRowSel[i];
			}
			control.Invalidate(true);
			SelectionChanged?.Invoke(this, new EventArgs());
		}

		private void SelectionTopToolStripMenuItemClick(object sender, EventArgs e){
			BringSelectionToTop();
		}

		private void CopySelectedRowsToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			Copy();
		}

		private void CopyCellToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			Point p = contextMenuStrip.PointToScreen(new Point(0, 0));
			Tuple<int, int> q = control.GetOrigin();
			int cx = p.X - q.Item1 - control.RowHeaderWidth;
			int cy = p.Y - q.Item2 - control.ColumnHeaderHeight;
			int x1 = control.VisibleX + cx;
			if (columnWidthSums == null){
				return;
			}
			int ind = ArrayUtils.CeilIndex(columnWidthSums, x1);
			int row = (control.VisibleY + cy)/rowHeight;
			if (model == null || row >= model.RowCount || row < 0){
				return;
			}
			int ox = order[row];
			Clipboard.Clear();
			Clipboard.SetDataObject("" + TableModel.GetEntry(ox, ind));
		}

		private void CopyColumnFullToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			Point p = contextMenuStrip.PointToScreen(new Point(0, 0));
			Tuple<int, int> q = control.GetOrigin();
			int cx = p.X - q.Item1 - control.RowHeaderWidth;
			int x1 = control.VisibleX + cx;
			if (columnWidthSums == null){
				return;
			}
			int ind = ArrayUtils.CeilIndex(columnWidthSums, x1);
			if (model == null){
				return;
			}
			StringBuilder str = new StringBuilder();
			for (int i = 0; i < order.Length; i++){
				str.Append(TableModel.GetEntry(order[i], ind));
				if (i != order.Length - 1){
					str.Append("\n");
				}
			}
			Clipboard.Clear();
			Clipboard.SetDataObject(str.ToString());
		}

		private void CopyColumnSelectionToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			Point p = contextMenuStrip.PointToScreen(new Point(0, 0));
			Tuple<int, int> q = control.GetOrigin();
			int cx = p.X - q.Item1 - control.RowHeaderWidth;
			int x1 = control.VisibleX + cx;
			if (columnWidthSums == null){
				return;
			}
			int ind = ArrayUtils.CeilIndex(columnWidthSums, x1);
			if (model == null){
				return;
			}
			StringBuilder str = new StringBuilder();
			int[] selection = GetSelectedRows();
			for (int i = 0; i < selection.Length; i++){
				int t = selection[i];
				str.Append(TableModel.GetEntry(t, ind));
				if (i != selection.Length - 1){
					str.Append("\n");
				}
			}
			Clipboard.Clear();
			Clipboard.SetDataObject(str.ToString());
		}

		private void PasteSelectionToolStripMenuItemClick(object sender, EventArgs e){
			string text = Clipboard.GetText();
			if (string.IsNullOrEmpty(text)){
				MessageBox.Show("Clipboard is empty.");
				return;
			}
			string[] lines = text.Split('\r', '\n');
			for (int i = 0; i < lines.Length; i++){
				lines[i] = lines[i].Trim();
			}
			int ncols = StringUtils.AllIndicesOf(lines[0], "\t").Length + 1;
			if (ncols > 2){
				MessageBox.Show("At most two selection columns are allowed.");
				return;
			}
			string[][] colData;
			if (ncols == 1){
				colData = new[]{lines};
			} else{
				colData = new string[ncols][];
				for (int i = 0; i < colData.Length; i++){
					colData[i] = new string[lines.Length];
				}
				for (int i = 0; i < lines.Length; i++){
					string[] w = lines[i].Split('\t');
					for (int j = 0; j < ncols; j++){
						colData[j][i] = j < w.Length ? w[j] : "";
					}
				}
			}
			PasteSelectionWindow psw = new PasteSelectionWindow(ncols, GetColumnNames());
			psw.ShowDialog();
			if (!psw.Ok){
				return;
			}
			int[] colInds = psw.GetSelectedIndices();
			int[] sel = GetSelection(ncols, colData, colInds);
			SetSelectedRows(sel);
			control.Invalidate(true);
		}

		private int[] GetSelection(int ncols, IList<string[]> colData, IList<int> colInds){
			switch (ncols){
				case 1:
					return GetSelection1(colData[0], colInds[0]);
				case 2:
					return GetSelection2(colData, colInds);
				default:
					throw new ArgumentException("Never get here");
			}
		}

		private int[] GetSelection2(IList<string[]> colData, IList<int> colInds){
			HashSet<Tuple<string, string>> x = GetValues2(colData);
			List<int> sel = new List<int>();
			for (int i = 0; i < TableModel.RowCount; i++){
				object value1 = TableModel.GetEntry(i, colInds[0]);
				object value2 = TableModel.GetEntry(i, colInds[1]);
				bool match = Matches2(value1, value2, x);
				if (match){
					sel.Add(i);
				}
			}
			return sel.ToArray();
		}

		private int[] GetSelection1(IEnumerable<string> colData, int colInd){
			HashSet<string> x = GetValues1(colData);
			List<int> sel = new List<int>();
			for (int i = 0; i < TableModel.RowCount; i++){
				object value = TableModel.GetEntry(i, colInd);
				bool match = Matches1(value, x);
				if (match){
					sel.Add(i);
				}
			}
			return sel.ToArray();
		}

		private static HashSet<Tuple<string, string>> GetValues2(IList<string[]> colData){
			HashSet<Tuple<string, string>> x = new HashSet<Tuple<string, string>>();
			for (int i = 0; i < colData[0].Length; i++){
				string s1 = colData[0][i];
				string s2 = colData[1][i];
				if (s1 == null || s2 == null){
					continue;
				}
				s1 = s1.Trim();
				s2 = s2.Trim();
				if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)){
					continue;
				}
				string[] q1 = s1.Split(';');
				string[] q2 = s2.Split(';');
				foreach (string r1 in q1){
					string t1 = r1.Trim();
					if (string.IsNullOrEmpty(t1)){
						continue;
					}
					foreach (string r2 in q2){
						string t2 = r2.Trim();
						if (string.IsNullOrEmpty(t2)){
							continue;
						}
						x.Add(new Tuple<string, string>(t1, t2));
					}
				}
			}
			return x;
		}

		private static HashSet<string> GetValues1(IEnumerable<string> colData){
			HashSet<string> x = new HashSet<string>();
			foreach (string s in colData){
				string t = s?.Trim();
				if (string.IsNullOrEmpty(t)){
					continue;
				}
				string[] q = t.Split(';');
				foreach (string s1 in q){
					string s2 = s1.Trim();
					if (!string.IsNullOrEmpty(s2)){
						x.Add(s2);
					}
				}
			}
			return x;
		}

		private static bool Matches2(object value1, object value2, ICollection<Tuple<string, string>> colSet){
			if (value1 == null || value1 is DBNull || value2 == null || value2 is DBNull){
				return false;
			}
			string s1 = value1.ToString();
			string s2 = value2.ToString();
			if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)){
				return false;
			}
			string[] q1 = s1.Split(';');
			string[] q2 = s2.Split(';');
			foreach (string p1 in q1){
				string r1 = p1.Trim();
				foreach (string p2 in q2){
					string r2 = p2.Trim();
					if (colSet.Contains(new Tuple<string, string>(r1, r2))){
						return true;
					}
				}
			}
			return false;
		}

		private static bool Matches1(object value, ICollection<string> colSet){
			if (value == null || value is DBNull){
				return false;
			}
			string s = value.ToString();
			if (string.IsNullOrEmpty(s)){
				return false;
			}
			string[] q = s.Split(';');
			foreach (string p in q){
				string r = p.Trim();
				if (colSet.Contains(r)){
					return true;
				}
			}
			return false;
		}

		private string[] GetColumnNames(){
			string[] result = new string[TableModel.ColumnCount];
			for (int i = 0; i < result.Length; i++){
				result[i] = TableModel.GetColumnName(i);
			}
			return result;
		}

		private void ExportToolStripMenuItemClick(object sender, EventArgs e){
			if (model == null){
				return;
			}
			SaveFileDialog ofd = new SaveFileDialog{Filter = "Text File (*.txt)|*.txt"};
			if (ofd.ShowDialog() == DialogResult.OK){
				ExportMatrix(ofd.FileName);
			}
		}

		private void ExportMatrix(string fileName){
			StreamWriter writer = new StreamWriter(fileName);
			StringBuilder line = new StringBuilder();
			if (model.ColumnCount > 0){
				line.Append(model.GetColumnName(0));
			}
			for (int i = 1; i < model.ColumnCount; i++){
				line.Append("\t" + model.GetColumnName(i));
			}
			writer.WriteLine(line.ToString());
			for (int j = 0; j < model.RowCount; j++){
				line = new StringBuilder();
				if (model.ColumnCount > 0){
					line.Append(model.GetEntry(j, 0));
				}
				for (int i = 1; i < model.ColumnCount; i++){
					line.Append("\t" + model.GetEntry(j, i));
				}
				writer.WriteLine(line.ToString());
			}
			writer.Close();
		}

		private int[] GetUnselectedRows(){
			if (model == null){
				return new int[0];
			}
			CheckSizes();
			List<int> result = new List<int>();
			for (int i = 0; i < model.RowCount; i++){
				if (!modelRowSel[i]){
					result.Add(i);
				}
			}
			return result.ToArray();
		}

		private void RenderCell(IGraphics g, bool selected, int row, int col, int width, int x1, int y1){
			object o = model.GetEntry(row, col);
			RenderTableCell render = model.GetColumnRenderer(col);
			if (render != null){
				render(g, selected, o, width, x1, y1);
				return;
			}
			ColumnType type = model.GetColumnType(col);
			switch (type){
				case ColumnType.Color:
					RenderCellColor(g, selected, o, x1, y1);
					break;
				case ColumnType.DashStyle:
					RenderCellDashStyle(g, selected, o, x1, y1);
					break;
				default:
					RenderCellString(g, selected, o, width, x1, y1);
					break;
			}
		}

		private static void RenderCellDashStyle(IGraphics g, bool selected, object o, int x1, int y1){
			int dashInd = (int) o;
			Pen2 p = selected ? Pens2.White : Pens2.Black;
			p = new Pen2(p.Color){DashStyle = DashStyles.DashStyleFromIndex(dashInd), Width = 2};
			g.DrawLine(p, x1 + 4, y1 + 11, x1 + 40, y1 + 11);
		}

		private static void RenderCellColor(IGraphics g, bool selected, object o, int x1, int y1){
			Color2 c = (Color2) o;
			Brush2 b = new Brush2(c);
			Pen2 p = selected ? Pens2.White : Pens2.Black;
			const int w = 14;
			g.FillRectangle(b, x1 + 3, y1 + 4, w, w);
			g.DrawRectangle(p, x1 + 3, y1 + 4, w, w);
		}

		private void RenderCellString(IGraphics g, bool selected, object o, int width, int x1, int y1){
			g.DrawString(GetStringValue(g, o, width, textFont), textFont, selected ? Brushes2.White : textBrush, x1 + 3, y1 + 4);
		}

		private static string GetStringValue(IGraphics g, object o, int width, Font2 font){
			if (o is double){
				double x = (double) o;
				if (double.IsNaN(x)){
					o = "NaN";
				}
			}
			string s = "" + o;
			return GraphUtil.GetStringValue(g, s, width, font);
		}

		private void CheckSizes(){
			if (model == null){
				return;
			}
			if (order == null || order.Length != model.RowCount){
				order = ArrayUtils.ConsecutiveInts(model.RowCount);
				inverseOrder = ArrayUtils.ConsecutiveInts(model.RowCount);
				sortState = SortState.Unsorted;
				sortCol = -1;
			}
			if (modelRowSel == null || modelRowSel.Length != model.RowCount){
				modelRowSel = new bool[model.RowCount];
			}
		}

		private string[] GetStringHeader(IGraphics g, int col, int width, Font2 font){
			string s = model.GetColumnName(col);
			string[] q = GraphUtil.WrapString(g, s, width, font);
			if (q.Length > maxColHeaderStringSplits){
				Array.Resize(ref q, maxColHeaderStringSplits);
			}
			for (int i = 0; i < q.Length; i++){
				q[i] = GetStringValue(g, q[i], width, font);
			}
			return q;
		}

		private void Unsort(){
			sortState = SortState.Unsorted;
			for (int i = 0; i < order.Length; i++){
				order[i] = i;
				inverseOrder[i] = i;
			}
		}

		private void InvertOrder(){
			sortState = SortState.Decreasing;
			ArrayUtils.Revert(order);
			inverseOrder = new int[order.Length];
			for (int i = 0; i < order.Length; i++){
				inverseOrder[order[i]] = i;
			}
		}

		private void Sort(){
			sortState = SortState.Increasing;
			ColumnType type = model.GetColumnType(sortCol);
			switch (type){
				case ColumnType.Integer:
					order = SortInt();
					break;
				case ColumnType.Numeric:
					order = SortDouble();
					break;
				default:
					order = SortString();
					break;
			}
			inverseOrder = new int[order.Length];
			for (int i = 0; i < order.Length; i++){
				inverseOrder[order[i]] = i;
			}
		}

		private int[] SortInt(){
			int[] data = new int[model.RowCount];
			for (int i = 0; i < data.Length; i++){
				object o = model.GetEntry(i, sortCol);
				if (o == null || o is DBNull){
					data[i] = int.MaxValue;
				} else{
					data[i] = o as int? ?? (o as ushort? ?? int.MaxValue);
				}
			}
			return ArrayUtils.Order(data);
		}

		private int[] SortDouble(){
			double[] data = new double[model.RowCount];
			for (int i = 0; i < data.Length; i++){
				object o = model.GetEntry(i, sortCol);
				if (o == null || o is DBNull){
					data[i] = double.MaxValue;
				} else{
					data[i] = o as double? ?? (o as float? ?? (o as int? ?? double.NaN));
				}
			}
			return ArrayUtils.Order(data);
		}

		private int[] SortString(){
			string[] data = new string[model.RowCount];
			for (int i = 0; i < data.Length; i++){
				object o = model.GetEntry(i, sortCol);
				data[i] = o?.ToString() ?? "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";
			}
			return ArrayUtils.Order(data);
		}

		private void SelectRange(int start, int end){
			for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++){
				modelRowSel[order[i]] = true;
			}
		}

		private void Goto(){
			//TODO
		}

		private void Copy(){
			StringBuilder str = new StringBuilder();
			// store the column-names
			str.Append(TableModel.GetColumnName(0));
			for (int i = 1; i < TableModel.ColumnCount; ++i){
				str.Append("\t");
				str.Append(TableModel.GetColumnName(i));
			}
			str.Append("\n");
			// store the selected rows
			int[] selection = GetSelectedRows();
			for (int i = 0; i < selection.Length; i++){
				int t = selection[i];
				str.Append(TableModel.GetEntry(t, 0));
				for (int col = 1; col < TableModel.ColumnCount; ++col){
					str.Append("\t");
					str.Append(TableModel.GetEntry(t, col));
				}
				if (i != selection.Length - 1){
					str.Append("\n");
				}
			}
			Clipboard.Clear();
			Clipboard.SetDataObject(str.ToString());
		}

		private void HandleToolTip(bool hover){
			if (currentX < 0 || currentY < 0 || currentRow < 0 || currentCol < 0 || model == null || currentRow >= model.RowCount ||
				currentCol >= model.ColumnCount){
				//TODO
				//mainViewToolTip.Hide(this);
				return;
			}
			if (order.Length != model.RowCount){
				return;
			}
			object o = model.GetEntry(order[currentRow], currentCol);
			string s = o?.ToString();
			if (s?.Length > 0 && (hover || currentX != toolTipX || currentY != toolTipY)){
				//TODO
				//mainViewToolTip.Show(s, this, currentX, currentY, 5000);
				toolTipX = currentX;
				toolTipY = currentY;
			}
		}

		public void CalcCurrentRowAndColumn(BasicMouseEventArgs e){
			int x1 = control.VisibleX + e.X;
			int y1 = control.VisibleY + e.Y;
			try{
				int indf = ArrayUtils.CeilIndex(columnWidthSums, x1);
				if (model != null){
					if (indf >= 0){
						currentCol = indf;
					} else{
						currentCol = -1;
					}
				}
			} catch (Exception){}
			currentRow = y1/rowHeight;
		}

		public void Dispose(){
			if (findForm != null){
				findForm.Close();
				findForm.Dispose();
			}
		}

		public void ProcessCmdKey(Keys2 keyData){
			switch (keyData){
				case Keys2.Shift | Keys2.Home:
				case Keys2.Home:{
					control.VisibleX = 0;
					control.Invalidate(true);
				}
					break;
				case Keys2.Shift | Keys2.End:
				case Keys2.End:{
					ScrollToEnd();
					control.Invalidate(true);
				}
					break;
				case Keys2.Shift | Keys2.Down:
				case Keys2.Down:{
					int[] selection = GetSelectedRowsView();
					if (selection != null && selection.Length != 0 && selection[selection.Length - 1] < RowCount - 1){
						if (keyData == Keys2.Down){
							modelRowSel[order[selection[selection.Length - 1]]] = false;
						}
						SetSelectedViewIndex(selection[selection.Length - 1] + 1);
						control.MoveDown(rowHeight);
						control.Invalidate(true);
					}
				}
					break;
				case Keys2.Shift | Keys2.Up:
				case Keys2.Up:{
					int[] selection = GetSelectedRowsView();
					if (selection != null && selection.Length != 0 && selection[0] > 0){
						if (keyData == Keys2.Up){
							modelRowSel[order[selection[0]]] = false;
						}
						SetSelectedViewIndex(selection[0] - 1);
						control.MoveUp(rowHeight);
						control.Invalidate(true);
					}
				}
					break;
				case Keys2.Control | Keys2.A:
					SelectAll();
					control.Invalidate(true);
					break;
				case Keys2.Control | Keys2.C:
					Copy();
					break;
				case Keys2.Control | Keys2.F:
					Find();
					break;
				case Keys2.Control | Keys2.G:
					Goto();
					break;
			}
		}

		public void InvalidateBackgroundImages(){}
		public void OnSizeChanged(){}
	}
}