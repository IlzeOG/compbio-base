﻿using System;
using BaseLib.Wpf;
using BaseLibS.Param;

namespace BaseLib.Param{
	[Serializable]
	internal class Ms1LabelParamWf : Ms1LabelParam{
		[NonSerialized] private Ms1LabelPanel control;
		internal Ms1LabelParamWf(string name, int[][] value) : base(name, value){}
		public override ParamType Type => ParamType.WinForms;

		public override void SetValueFromControl(){
			Value = control.SelectedIndices;
		}

		public override void UpdateControlFromValue(){
			control.SelectedIndices = Value;
		}

		public override object CreateControl(){
			return control = new Ms1LabelPanel(Multiplicity, Values){SelectedIndices = Value};
		}
	}
}