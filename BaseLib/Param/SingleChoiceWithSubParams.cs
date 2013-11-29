﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BaseLib.Wpf;

namespace BaseLib.Param{
	[Serializable]
	public class SingleChoiceWithSubParams : ParameterWithSubParams{
		public int Value { get; set; }
		public int Default { get; private set; }
		public IList<string> Values { get; set; }
		public IList<Parameters> SubParams { get; set; }
		public float paramNameWidth = 250F;
		public float ParamNameWidth { get { return paramNameWidth; } set { paramNameWidth = value; } }
		public float totalWidth = 1000F;
		public float TotalWidth { get { return totalWidth; } set { totalWidth = value; } }
		public SingleChoiceWithSubParams(string name) : this(name, 0) {}

		public SingleChoiceWithSubParams(string name, int value) : base(name){
			Value = value;
			Default = value;
			Values = new[]{""};
			SubParams = new[]{new Parameters()};
		}

		public override string StringValue { get { return Value.ToString(CultureInfo.InvariantCulture); } set { Value = int.Parse(value); } }
		public int Value2{
			get{
				SetValueFromControl();
				return Value;
			}
		}

		public override void ResetValue(){
			Value = Default;
			foreach (Parameters p in SubParams){
				p.ResetValues();
			}
		}

		public override void ResetDefault(){
			Default = Value;
			foreach (Parameters p in SubParams){
				p.ResetDefaults();
			}
		}

		public override bool IsModified{
			get{
				if (Value != Default){
					return true;
				}
				foreach (Parameters p in SubParams){
					if (p.IsModified){
						return true;
					}
				}
				return false;
			}
		}
		public string SelectedValue { get { return Value < 0 || Value >= Values.Count ? null : Values[Value]; } }

		public override Parameters GetSubParameters(){
			return SubParams[Value];
		}

		public override void Clear(){
			Value = 0;
			foreach (Parameters parameters in SubParams){
				parameters.Clear();
			}
		}

		public override void SetValueFromControl(){
			Grid tbl = (Grid) control;
			if (tbl == null){
				return;
			}
			ComboBox cb = (ComboBox) WpfUtils.GetGridChild(tbl, 0, 0);
			if (cb != null){
				Value = cb.SelectedIndex;
			}
			foreach (Parameters p in SubParams){
				p.SetValuesFromControl();
			}
		}

		public override void UpdateControlFromValue(){
			if (control == null){
				return;
			}
			Grid tlp = (Grid) control;
			ComboBox cb = (ComboBox) WpfUtils.GetGridChild(tlp, 0, 0);
			if (Value >= 0 && Value < Values.Count){
				cb.SelectedIndex = Value;
			}
			foreach (Parameters p in SubParams){
				p.UpdateControlsFromValue();
			}
		}

		protected override FrameworkElement Control{
			get{
				ParameterPanel[] panels = new ParameterPanel[SubParams.Count];
				for (int i = 0; i < panels.Length; i++){
					panels[i] = new ParameterPanel();
					panels[i].Init(SubParams[i], ParamNameWidth, (int) (TotalWidth));
				}
				ComboBox cb = new ComboBox();
				cb.SelectionChanged += (sender, e) =>{
					SetValueFromControl();
					ValueHasChanged();
				};
				if (Values != null){
					foreach (string value in Values){
						cb.Items.Add(value);
					}
					if (Value >= 0 && Value < Values.Count){
						cb.SelectedIndex = Value;
					}
				}
				Grid tlp = new Grid();
				tlp.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(100, GridUnitType.Star)});
				tlp.RowDefinitions.Add(new RowDefinition{Height = new GridLength(30, GridUnitType.Pixel)});
				tlp.RowDefinitions.Add(new RowDefinition{Height = new GridLength(100, GridUnitType.Star)});
				Grid.SetRow(cb, 0);
				tlp.Children.Add(cb);
				for (int i = 0; i < panels.Length; i++){
					panels[i].Visibility = (i == Value) ? Visibility.Visible : Visibility.Hidden;
					Grid.SetRow(panels[i], 1);
					tlp.Children.Add(panels[i]);
				}
				cb.SelectionChanged += (sender, e) =>{
					for (int i = 0; i < panels.Length; i++){
						panels[i].Visibility = (i == cb.SelectedIndex) ? Visibility.Visible : Visibility.Hidden;
					}
				};
				return tlp;
			}
		}
		public override float Height{
			get{
				float max = 0;
				foreach (Parameters param in SubParams){
					max = Math.Max(max, param.Height + 6);
				}
				return 44 + max;
			}
		}

		public override object Clone(){
			SingleChoiceWithSubParams s = new SingleChoiceWithSubParams(Name, Value)
			{Help = Help, Visible = Visible, Values = Values, Default = Default, SubParams = new Parameters[SubParams.Count]};
			for (int i = 0; i < SubParams.Count; i++){
				s.SubParams[i] = (Parameters) SubParams[i].Clone();
			}
			return s;
		}

		public void SetValueChangedHandlerForSubParams(ValueChangedHandler action){
			ValueChanged += action;
			foreach (Parameter p in GetSubParameters().GetAllParameters()){
				if (p is IntParam || p is DoubleParam){
					p.ValueChanged += action;
				} else if (p is SingleChoiceWithSubParams){
					((SingleChoiceWithSubParams) p).SetValueChangedHandlerForSubParams(action);
				}
			}
		}
	}
}