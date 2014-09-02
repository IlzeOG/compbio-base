using System;
using System.Collections.Generic;
using BaseLib.Api;
using BaseLib.Num;
using BaseLib.Param;

namespace NumPluginBase.Distance{
	[Serializable]
	public class PearsonCorrelationDistance : IDistance{
		public Parameters Parameters { set { } get { return new Parameters(); } }
		public double Get(IList<float> x, IList<float> y) { return Calc(x, y); }
		public double Get(IList<double> x, IList<double> y) { return Calc(x, y); }
		//TODO
		public double Get(float[,] data1, float[,] data2, int index1, int index2, MatrixAccess access){
			if (access == MatrixAccess.Rows){
				int n = data1.GetLength(1);
				float[] x = new float[n];
				float[] y = new float[n];
				for (int i = 0; i < n; i++){
					x[i] = data1[index1, i];
					y[i] = data2[index2, i];
				}
				return Get(x, y);
			} else{
				int n = data1.GetLength(0);
				float[] x = new float[n];
				float[] y = new float[n];
				for (int i = 0; i < n; i++){
					x[i] = data1[i, index1];
					y[i] = data2[i, index2];
				}
				return Get(x, y);
			}
		}

		//TODO
		public double Get(double[,] data1, double[,] data2, int index1, int index2, MatrixAccess access){
			if (access == MatrixAccess.Rows){
				int n = data1.GetLength(1);
				double[] x = new double[n];
				double[] y = new double[n];
				for (int i = 0; i < n; i++){
					x[i] = data1[index1, i];
					y[i] = data2[index2, i];
				}
				return Get(x, y);
			} else{
				int n = data1.GetLength(0);
				double[] x = new double[n];
				double[] y = new double[n];
				for (int i = 0; i < n; i++){
					x[i] = data1[i, index1];
					y[i] = data2[i, index2];
				}
				return Get(x, y);
			}
		}

		public static double Calc(IList<double> x, IList<double> y){
			int n = x.Count;
			double mx = 0;
			double my = 0;
			int c = 0;
			for (int i = 0; i < n; i++){
				double d = x[i] - y[i];
				if (double.IsNaN(d)){
					continue;
				}
				mx += x[i];
				my += y[i];
				c++;
			}
			if (c < 3){
				return double.NaN;
			}
			mx /= c;
			my /= c;
			double sx = 0;
			double sy = 0;
			double sxy = 0;
			for (int i = 0; i < n; i++){
				double d = x[i] - y[i];
				if (double.IsNaN(d)){
					continue;
				}
				double wx = x[i] - mx;
				double wy = y[i] - my;
				sx += wx*wx;
				sy += wy*wy;
				sxy += wx*wy;
			}
			sx /= c;
			sy /= c;
			sxy /= c;
			double corr = sxy/Math.Sqrt(sx*sy);
			return 1 - corr;
		}

		public static double Calc(IList<float> x, IList<float> y){
			int n = x.Count;
			double mx = 0;
			double my = 0;
			int c = 0;
			for (int i = 0; i < n; i++){
				double d = x[i] - y[i];
				if (double.IsNaN(d)){
					continue;
				}
				mx += x[i];
				my += y[i];
				c++;
			}
			if (c < 3){
				return double.NaN;
			}
			mx /= c;
			my /= c;
			double sx = 0;
			double sy = 0;
			double sxy = 0;
			for (int i = 0; i < n; i++){
				double d = x[i] - y[i];
				if (double.IsNaN(d)){
					continue;
				}
				double wx = x[i] - mx;
				double wy = y[i] - my;
				sx += wx*wx;
				sy += wy*wy;
				sxy += wx*wy;
			}
			sx /= c;
			sy /= c;
			sxy /= c;
			double corr = sxy/Math.Sqrt(sx*sy);
			return 1 - corr;
		}

		public object Clone() { return new PearsonCorrelationDistance(); }
		public string Name { get { return "Pearson correlation"; } }
		public string Description { get { return ""; } }
		public float DisplayRank { get { return 4; } }
		public bool IsActive { get { return true; } }
	}
}