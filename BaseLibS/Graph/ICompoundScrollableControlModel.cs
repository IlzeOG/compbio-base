﻿namespace BaseLibS.Graph{
	public interface ICompoundScrollableControlModel : IScrollableControlModel{
		void Register(ICompoundScrollableControl control);
		float UserSf { get; set; }
	}
}