﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BaseLib.Util;

namespace BaseLib.Param{
	[Serializable]
	public class MultiStringParam : Parameter{
		public string[] Value { get; set; }
		public string[] Default { get; private set; }
		public MultiStringParam(string name) : this(name, new string[0]) {}

		public MultiStringParam(string name, string[] value) : base(name){
			Value = value;
			Default = value;
		}

		public override string StringValue{
			get { return StringUtils.Concat(",", Value); }
			set{
				if (value.Trim().Length == 0){
					Value = new string[0];
					return;
				}
				Value = value.Split(',');
			}
		}
		public string[] Value2{
			get{
				SetValueFromControl();
				return Value;
			}
		}

		public override void ResetValue(){
			Value = Default;
		}

		public override void ResetDefault(){
			Default = Value;
		}

		public override bool IsModified { get { return !ArrayUtils.EqualArrays(Value, Default); } }

		public override void SetValueFromControl(){
			TextBox tb = (TextBox) control;
			string text = tb.Text;
			string[] b = text.Split('\n');
			List<string> result = new List<string>();
			foreach (string x in b){
				string y = x.Trim();
				if (y.Length > 0){
					result.Add(y);
				}
			}
			Value = result.ToArray();
		}

		public override void Clear(){
			Value = new string[0];
		}

		public override void UpdateControlFromValue(){
			if (control == null){
				return;
			}
			TextBox tb = (TextBox) control;
			tb.Text = StringUtils.Concat("\n", Value);
		}

		protected override FrameworkElement Control{
			get{
				TextBox tb = new TextBox();
				tb.Text = StringUtils.Concat("\n", Value);
				return tb;
			}
		}

		public override object Clone(){
			return new MultiStringParam(Name, Value){Help = Help, Visible = Visible, Default = Default};
		}

		public override float Height { get { return 150f; } }
	}
}