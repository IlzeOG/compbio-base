﻿using System;
using BaseLibC.Forms;
using BaseLibS.Param;

namespace BaseLibC.Param{
	[Serializable]
	internal class FolderParamWf : FolderParam{
		[NonSerialized] private FolderParameterControl control;
		internal FolderParamWf(string name) : base(name){}
		internal FolderParamWf(string name, string value) : base(name, value){}
		public override ParamType Type => ParamType.WinForms;

		public override void SetValueFromControl(){
			Value = control.Text1;
		}

		public override void UpdateControlFromValue(){
			if (control == null){
				return;
			}
			control.Text1 = Value;
		}

		public override object CreateControl(){
			return control = new FolderParameterControl{Text1 = Value};
		}
	}
}