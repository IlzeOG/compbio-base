﻿using BaseLib.Param;
using BaseLibS.Num.Vector;

namespace BaseLib.Api{
    public interface IClassificationFeatureRankingMethod : INamedListItem{
		int[] Rank(BaseVector[] x, int[][] y, int ngroups, Parameters param, IGroupDataProvider data, int nthreads);
        Parameters GetParameters(IGroupDataProvider data);
    }
}