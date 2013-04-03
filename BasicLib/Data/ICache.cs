﻿namespace BasicLib.Data{
	public interface ICache<Tk, Tv>{
		bool ContainsKey(Tk key);
		int Count { get; }
		void Add(Tk key, Tv value);
		Tv this[Tk key] { get; }
		void Clear();
	}
}