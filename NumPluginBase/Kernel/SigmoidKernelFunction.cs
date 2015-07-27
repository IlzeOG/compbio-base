﻿using System;
using BaseLib.Param;
using BaseLibS.Api;
using BaseLibS.Num.Vector;
using BaseLibS.Param;

namespace NumPluginBase.Kernel{
	[Serializable]
	public class SigmoidKernelFunction : IKernelFunction{
		private double Gamma { get; set; }
		private double Offset { get; set; }
		public SigmoidKernelFunction() : this(0.01, 0) { }

		public SigmoidKernelFunction(double gamma, double coef){
			Gamma = gamma;
			Offset = coef;
		}

		public bool UsesSquares { get { return false; } }
		public string Name { get { return "Sigmoid"; } }

		public Parameters Parameters{
			get{
				return
					new Parameters(new Parameter[]{
						new DoubleParam("Gamma", Gamma){Help = "Coefficient in front of the scalar product."},
						new DoubleParam("Offset", Offset){Help = "Shift parameter."}
					});
			}
			set{
				Gamma = value.GetParam<double>("Gamma").Value;
				Offset = value.GetParam<double>("Offset").Value;
			}
		}

		public double Evaluate(BaseVector xi, BaseVector xj, double xSquarei, double xSquarej) { return Math.Tanh(Gamma*xi.Dot(xj) + Offset); }
		public object Clone() { return new SigmoidKernelFunction(Gamma, Offset); }
		public string Description { get { return ""; } }
		public float DisplayRank { get { return 3; } }
		public bool IsActive { get { return true; } }
	}
}