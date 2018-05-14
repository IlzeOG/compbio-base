﻿using System;
using BaseLibS.Calc.Func;
using BaseLibS.Calc.Util;
using BaseLibS.Util;

namespace BaseLibS.Calc.Const{
	[Serializable]
	internal class ConstPi : Constant{
		internal override double NumEvaluateDouble => Math.PI;
		internal override string ShortName => "pi";
		internal override ReturnType ReturnType => ReturnType.Real;
		internal override string Name => "";
		internal override string Description => "";
		internal override DocumentType DescriptionType => DocumentType.PlainText;
		internal override Topic Topic => Topic.Unknown;
	}
}