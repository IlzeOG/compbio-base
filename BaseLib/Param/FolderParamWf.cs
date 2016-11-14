﻿using System;
using BaseLib.Forms;
using BaseLibS.Param;

namespace BaseLib.Param{
	[Serializable]
	internal class FolderParamWf : FolderParam{
		[NonSerialized] private FolderParameterControl control;
		internal FolderParamWf(string name) : base(name){}
		internal FolderParamWf(string name, string value) : base(name, value){}
		public override ParamType Type => ParamType.WinForms;

		public override void SetValueFromControl(){
			Value = control.Text;
		}

		public override void UpdateControlFromValue(){
			if (control == null){
				return;
			}
			control.Text = Value;
		}

		public override object CreateControl(){
			return control = new FolderParameterControl{Text1 = Value};
		}
	}
}