﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BaseLibS.Util {
	public abstract class WorkDispatcher {
		private const int initialDelay = 6;
		public readonly int nThreads;
		private readonly int nTasks;
		private Thread[] workThreads;
		private Process[] externalProcesses;
		private Stack<int> toBeProcessed;
		private readonly string infoFolder;
		private readonly bool externalCalculations;
		private readonly bool dotNetCore;

		protected WorkDispatcher(int nThreads, int nTasks, string infoFolder, bool externalCalculations, bool dotNetCore) {
			this.nThreads = Math.Min(nThreads, nTasks);
			this.nTasks = nTasks;
			this.infoFolder = infoFolder;
			this.dotNetCore = dotNetCore;
			if (!string.IsNullOrEmpty(infoFolder) && !Directory.Exists(infoFolder)) {
				Directory.CreateDirectory(infoFolder);
			}
			this.externalCalculations = externalCalculations;
		}

		public void Abort() {
			if (workThreads != null) {
				foreach (Thread t in workThreads.Where(t => t != null)) {
					t.Abort();
				}
			}
			if (ExternalCalculation() && externalProcesses != null) {
				foreach (Process process in externalProcesses) {
					if (process != null && IsRunning(process)) {
						try {
							process.Kill();
						} catch (Exception) { }
					}
				}
			}
		}

		public static bool IsRunning(Process process) {
			if (process == null) return false;
			try {
				Process.GetProcessById(process.Id);
			} catch (Exception) {
				return false;
			}
			return true;
		}

		public void Start() {
			toBeProcessed = new Stack<int>();
			for (int index = nTasks - 1; index >= 0; index--) {
				toBeProcessed.Push(index);
			}
			workThreads = new Thread[nThreads];
			externalProcesses = new Process[nThreads];
			for (int i = 0; i < nThreads; i++) {
				workThreads[i] = new Thread(Work) {Name = "Thread " + i + " of " + GetType().Name};
				workThreads[i].Start(i);
				Thread.Sleep(initialDelay);
			}
			while (true) {
				Thread.Sleep(1000);
				bool busy = false;
				for (int i = 0; i < nThreads; i++) {
					if (workThreads[i].IsAlive) {
						busy = true;
						break;
					}
				}
				if (!busy) {
					break;
				}
			}
		}

		public void Work(object threadIndex) {
			while (toBeProcessed.Count > 0) {
				int x;
				lock (this) {
					if (toBeProcessed.Count > 0) {
						x = toBeProcessed.Pop();
					} else {
						x = -1;
					}
				}
				if (x >= 0) {
					DoWork(x, (int) threadIndex);
				}
			}
		}

		protected void DoWork(int taskIndex, int threadIndex) {
			if (ExternalCalculation()) {
				ProcessSingleRunExternal(taskIndex, threadIndex);
			} else {
				InternalCalculation(taskIndex);
			}
		}

		private void ProcessSingleRunExternal(int taskIndex, int threadIndex) {
			bool isUnix = FileUtils.IsUnix();
			string cmd = GetCommandFilename();
			string args = GetLogArgs(taskIndex, taskIndex) + GetCommandArguments(taskIndex);
			ProcessStartInfo psi = IsRunningOnMono() && !dotNetCore
				? new ProcessStartInfo("mono", " --optimize=all,float32 --server " + cmd + " " + args)
				: new ProcessStartInfo(cmd, args);
			if (isUnix) {
				psi.WorkingDirectory = Directory.GetDirectoryRoot(cmd);
			}
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			externalProcesses[threadIndex] = new Process {StartInfo = psi};
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			externalProcesses[threadIndex].Start();
			int processid = externalProcesses[threadIndex].Id;
			externalProcesses[threadIndex].WaitForExit();
			int exitcode = externalProcesses[threadIndex].ExitCode;
			externalProcesses[threadIndex].Close();
			if (exitcode != 0) {
				throw new Exception("Exception during execution of external process: " + processid);
			}
		}

		/// <summary>
		/// http://www.mono-project.com/docs/gui/winforms/porting-winforms-applications/
		/// </summary>
		private static bool IsRunningOnMono() => Type.GetType("Mono.Runtime") != null;

		private string GetName(int taskIndex) {
			return GetFilename() + " (" + IntString(taskIndex + 1, nTasks) + "/" + nTasks + ")";
		}

		private static string IntString(int x, int n) {
			int npos = (int) Math.Ceiling(Math.Log10(n));
			string result = "" + x;
			if (result.Length >= npos) {
				return result;
			}
			return Repeat(npos - result.Length, "0") + result;
		}

		private static string Repeat(int n, string s) {
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < n; i++) {
				b.Append(s);
			}
			return b.ToString();
		}

		private string GetLogArgs(int taskIndex, int id) {
			return
				$"\"{infoFolder}\" \"{GetFilename()}\" \"{id}\" \"{GetName(taskIndex)}\" \"{GetComment(taskIndex)}\" \"Process\" ";
		}

		protected string GetFilename() {
			return GetMessagePrefix().Trim().Replace("/", "").Replace("(", "_").Replace(")", "_").Replace(" ", "_");
		}

		public virtual bool IsFallbackPosition => true;

		protected virtual string GetComment(int taskIndex) {
			return "";
		}

		protected string GetCommandFilename() {
			return "\"" + FileUtils.executablePath + Path.DirectorySeparatorChar + (dotNetCore ? ExecutableCore : Executable) + "\"";
		}

		protected abstract string Executable { get; }

		protected abstract string ExecutableCore { get; }

		protected bool ExternalCalculation() {
			return externalCalculations;
		}

		protected string GetCommandArguments(int taskIndex) {
			object[] o = GetArguments(taskIndex);
			string[] args = new string[o.Length + 1];
			args[0] = $"\"{Id}\"";
			for (int i = 0; i < o.Length; i++) {
				object o1 = o[i];
				string s = Parser.ToString(o1);
				args[i + 1] = "\"" + s + "\"";
			}
			return StringUtils.Concat(" ", args);
		}

		protected void InternalCalculation(int taskIndex) {
			Calculation(GetStringArgs(taskIndex));
		}

		public string GetMessagePrefix() {
			return MessagePrefix + " ";
		}

		private string[] GetStringArgs(int taskIndex) {
			object[] o = GetArguments(taskIndex);
			string[] args = new string[o.Length];
			for (int i = 0; i < o.Length; i++) {
				args[i] = $"{o[i]}";
			}
			return args;
		}

		public abstract void Calculation(string[] args);
		protected abstract object[] GetArguments(int taskIndex);
		protected abstract int Id { get; }
		protected abstract string MessagePrefix { get; }
	}
}