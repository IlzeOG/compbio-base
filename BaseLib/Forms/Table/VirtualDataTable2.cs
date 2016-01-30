using System;
using System.Collections.Generic;
using BaseLibS.Table;

namespace BaseLib.Forms.Table{
	[Serializable]
	public class VirtualDataTable2 : TableModelImpl, ITable{
		public Func<int, object[]> GetRowData { private get; set; }
		private int rowInUse = -1;
		private object[] rowDataInUse;
		private readonly int rowCount;
		private List<int> persistentColInds;
		private DataTable2 persistentTable;

		public VirtualDataTable2(string name, string description, int rowCount){
			Name = name;
			Description = description;
			this.rowCount = rowCount;
		}

		public void AddColumn(string colName, int width, ColumnType columnType, string description,
			bool persistent){
			AddColumn(colName, width, columnType, description);
			if (persistent){
				AddPersistentColumn(colName, width, columnType, description, null);
			}
		}

		public void AddColumn(string colName, int width, ColumnType columnType, string description,
			RenderTableCell renderer, bool persistent){
			AddColumn(colName, width, columnType, description, renderer);
			if (persistent){
				AddPersistentColumn(colName, width, columnType, description, renderer);
			}
		}

		private void AddPersistentColumn(string colName, int width, ColumnType columnType, string description,
			RenderTableCell renderer){
			if (persistentTable == null){
				persistentTable = new DataTable2(Name, Description);
				persistentColInds = new List<int>();
			}
			persistentTable.AddColumn(colName, width, columnType, description, renderer);
			persistentColInds.Add(columnNames.Count - 1);
			persistentColInds.Sort();
		}

		public DataRow2 NewRow() { return new DataRow2(columnNames.Count, nameMapping); }

		public void FillPersistentData(){
			for (int i = 0; i < rowCount; i++){
				object[] rowData = GetRowData(i);
				DataRow2 row = persistentTable.NewRow();
				for (int j = 0; j < persistentColInds.Count; j++){
					row[j] = rowData[persistentColInds[j]];
				}
				persistentTable.AddRow(row);
			}
		}

		public override int RowCount => rowCount;

		public override object GetEntry(int row, int col){
			if (row >= RowCount || row < 0){
				return null;
			}
			if (rowInUse != row){
				rowDataInUse = GetRowDataImpl(row);
				rowInUse = row;
			}
			if (rowDataInUse == null){
				return null;
			}
			return col >= rowDataInUse.Length ? null : rowDataInUse[col];
		}

		public override void SetEntry(int row, int column, object value){
			if (persistentTable == null){
				throw new Exception("The table has no persistent columns.");
			}
			int ind = persistentColInds.BinarySearch(column);
			if (ind < 0){
				throw new Exception("The column is not persistent.");
			}
			persistentTable.SetEntry(row, ind, value);
		}

		private object[] GetRowDataImpl(int row){
			if (GetRowData == null){
				return null;
			}
			if (row < 0 || row >= rowCount){
				return null;
			}
			object[] result = GetRowData(row);
			if (persistentColInds != null){
				for (int i = 0; i < persistentColInds.Count; i++){
					result[persistentColInds[i]] = persistentTable.GetEntry(row, i);
				}
			}
			return result;
		}
	}
}