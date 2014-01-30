using System;
using System.Collections.Generic;

namespace BaseLib.ParamWf{
	[Serializable]
	public class ParametersWf : ICloneable{
		private readonly List<ParameterGroupWf> paramGroups = new List<ParameterGroupWf>();

		public ParametersWf(IList<ParameterWf> param, string name){
			AddParameterGroup(param, name, false);
		}

		public ParametersWf(ParameterWf param) : this(new[]{param}) {}
		public ParametersWf(IList<ParameterWf> param) : this(param, null) {}
		public ParametersWf() {}

		public ParameterWf[] GetAllParameters() {
			List<ParameterWf> result = new List<ParameterWf>();
			foreach (ParameterGroupWf pg in paramGroups) {
				result.AddRange(pg.ParameterList);
			}
			return result.ToArray();
		}

		public string[] GetAllGroupHeadings() {
			List<string> result = new List<string>();
			foreach (ParameterGroupWf pg in paramGroups) {
				result.Add(pg.Name);
			}
			return result.ToArray();
		}

		public string[] Markup {
			get{
				List<string> result = new List<string>();
				foreach (ParameterGroupWf paramGroup in paramGroups){
					result.AddRange(paramGroup.Markup);
				}
				return result.ToArray();
			}
		}
		public bool IsModified{
			get{
				foreach (ParameterGroupWf parameterGroup in paramGroups){
					if (parameterGroup.IsModified){
						return true;
					}
				}
				return false;
			}
		}

		public void AddParameterGroup(IList<ParameterWf> param, string name, bool collapsed){
			paramGroups.Add(new ParameterGroupWf(param, name, collapsed));
		}

		public object Clone(){
			ParametersWf newParam = new ParametersWf();
			foreach (ParameterGroupWf p in paramGroups){
				newParam.paramGroups.Add((ParameterGroupWf) p.Clone());
			}
			return newParam;
		}

		public int Count{
			get{
				int c = 0;
				foreach (ParameterGroupWf parameterGroup in paramGroups){
					c += parameterGroup.Count;
				}
				return c;
			}
		}
		public float Height{
			get{
				float h = 0;
				foreach (ParameterGroupWf parameter in paramGroups){
					h += parameter.Height;
				}
				return h;
			}
		}
		public int GroupCount { get { return paramGroups.Count; } }

		public IntParamWf GetIntParam(string name){
			return (IntParamWf) GetParam(name);
		}

		public ParameterWf GetParam(string name){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				ParameterWf p = parameterGroup.GetParam(name);
				if (p != null){
					return p;
				}
			}
			throw new Exception("Parameter does not exist: " + name);
		}

		public ParameterWf[] GetDropTargets() {
			List<ParameterWf> result = new List<ParameterWf>();
			foreach (ParameterGroupWf parameterGroup in paramGroups) {
				foreach (ParameterWf p in parameterGroup.ParameterList) {
					if (p.IsDropTarget) {
						result.Add(p);
					}
				}
			}
			return result.ToArray();
		}

		public void SetSizes(int paramNameWidth, int totalWidth) {
			foreach (ParameterGroupWf parameterGroup in paramGroups) {
				foreach (ParameterWf p in parameterGroup.ParameterList) {
				}
			}
		}

		public ParameterWf GetParamNoException(string name) {
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				ParameterWf p = parameterGroup.GetParam(name);
				if (p != null){
					return p;
				}
			}
			return null;
		}

		public void UpdateControlsFromValue(){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				parameterGroup.UpdateControlsFromValue();
			}
		}

		public ParameterGroupWf GetGroup(int i){
			return paramGroups[i];
		}

		public void SetValuesFromControl(){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				parameterGroup.SetParametersFromConrtol();
			}
		}

		public void Clear(){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				parameterGroup.Clear();
			}
		}

		public void ResetValues(){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				parameterGroup.ResetValues();
			}
		}

		public void ResetDefaults(){
			foreach (ParameterGroupWf parameterGroup in paramGroups){
				parameterGroup.ResetDefaults();
			}
		}

		public ParameterWf FindParameter(string paramName){
			return FindParameter(paramName, this);
		}

		private static ParameterWf FindParameter(string paramName, ParametersWf parameters) {
			ParameterWf p = parameters.GetParamNoException(paramName);
			if (p != null) {
				return p;
			}
			foreach (ParameterWf px in parameters.GetAllParameters()) {
			}
			return null;
		}
	}
}