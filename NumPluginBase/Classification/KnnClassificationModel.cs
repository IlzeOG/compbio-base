﻿using System;
using System.Collections.Generic;
using BaseLib.Api;
using BaseLibS.Num.Vector;
using BaseLibS.Util;

namespace NumPluginBase.Classification{
	[Serializable]
	public class KnnClassificationModel : ClassificationModel{
		private readonly BaseVector[] x;
		private readonly int[][] y;
		private readonly int ngroups;
		private readonly int k;
		private readonly IDistance distance;

		public KnnClassificationModel(BaseVector[] x, int[][] y, int ngroups, int k, IDistance distance){
			this.x = x;
			this.y = y;
			this.ngroups = ngroups;
			this.k = k;
			this.distance = distance;
		}

		public override float[] PredictStrength(BaseVector xTest){
			int[] inds = GetNeighborInds(x, xTest, k, distance);
			float[] result = new float[ngroups];
			foreach (int ind in inds){
				foreach (int i in y[ind]){
					result[i]++;
				}
			}
			for (int i = 0; i < ngroups; i++){
				result[i] /= k;
			}
			return result;
		}

		public static int[] GetNeighborInds(IList<BaseVector> x, BaseVector xTest, int k, IDistance distance){
			double[] d = CalcDistances(x, xTest, distance);
			int[] o = ArrayUtils.Order(d);
			List<int> result = new List<int>();
			for (int i = 0; i < d.Length; i++){
				if (!double.IsNaN(d[o[i]]) && !double.IsInfinity(d[o[i]])){
					result.Add(o[i]);
				}
				if (result.Count >= k){
					break;
				}
			}
			return result.ToArray();
		}

		private static double[] CalcDistances(IList<BaseVector> x, BaseVector xTest, IDistance distance){
			double[] result = new double[x.Count];
			for (int i = 0; i < x.Count; i++){
				result[i] = distance.Get(x[i], xTest);
			}
			return result;
		}
	}
}