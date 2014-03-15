﻿using System;
using System.Drawing;
using BaseLib.Num.Api;
using BaseLib.Num.Vector;
using BaseLib.Param;

namespace BaseLib.Num.Kernel{
    [Serializable]
    public class SigmoidKernelFunction : IKernelFunction{
        private double Gamma { get; set; }
        private double Coef { get; set; }
        public SigmoidKernelFunction() : this(0.01, 0) {}

        public SigmoidKernelFunction(double gamma, double coef){
            Gamma = gamma;
            Coef = coef;
        }

        public bool UsesSquares{
            get { return false; }
        }

        public string Name{
            get { return "Sigmoid"; }
        }

        public Parameters Parameters{
            get { return new Parameters(new Parameter[]{new DoubleParam("Gamma", Gamma), new DoubleParam("Coef", Coef)}); }
            set{
                Gamma = value.GetDoubleParam("Gamma").Value;
                Coef = value.GetDoubleParam("Coef").Value;
            }
        }

        public double Evaluate(BaseVector xi, BaseVector xj, double xSquarei, double xSquarej){
            return Math.Tanh(Gamma*xi.Dot(xj) + Coef);
        }

        public object Clone(){
            return new SigmoidKernelFunction(Gamma, Coef);
        }

        public string Description { get { return ""; } }
        public float DisplayOrder { get { return 0; } }
        public bool IsActive { get { return true; } }
        public Bitmap DisplayImage { get { return null; } }
    }
}