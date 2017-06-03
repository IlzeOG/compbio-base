using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BaseLibS.Util {
	public abstract class WorkDispatcher {
		protected HashSet<int> currentIndices = new HashSet<int>();
		public int nThreads;
		public int nTasks;
		protected Thread[] allWorkThreads;
		protected Process[] externalProcesses;
		protected Stack<int> toBeProcessed;
		protected readonly string infoFolder;

		protected WorkDispatcher(int nThreads, int nTasks, string infoFolder) {
			this.nThreads = Math.Min(nThreads, nTasks);
			this.nTasks = nTasks;
			this.infoFolder = infoFolder;
			if (!string.IsNullOrEmpty(infoFolder) && !Directory.Exists(infoFolder)) {
				Directory.CreateDirectory(infoFolder);
			}
		}

		public virtual void Abort() {
			if (allWorkThreads != null) {
				foreach (Thread t in allWorkThreads.Where(t => t != null)) {
					t.Abort();
				}
			}
		}

		public virtual void Start() {
			currentIndices = new HashSet<int>();
			toBeProcessed = new Stack<int>();
			for (int index = nTasks - 1; index >= 0; index--) {
				toBeProcessed.Push(index);
			}
			allWorkThreads = new Thread[nThreads];
			externalProcesses = new Process[nThreads];
			for (int i = 0; i < nThreads; i++) {
				allWorkThreads[i] = new Thread(Work) {Name = "Thread " + i + " of " + GetType().Name};
				allWorkThreads[i].Start(i);
			}
			while (true) {
				Thread.Sleep(1000);
				bool busy = false;
				for (int i = 0; i < nThreads; i++) {
					if (allWorkThreads[i].IsAlive) {
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
			//Please no catching here. Important for debugging.
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

		protected virtual void DoWork(int taskIndex, int threadIndex) {
			if (ExternalCalculation()) {
				ProcessSingleRunExternal(taskIndex, threadIndex);
			} else {
				InternalCalculation(taskIndex);
			}
		}

		public virtual void ProcessSingleRunExternal(int taskIndex, int threadIndex) {
			bool isUnix = FileUtils.IsUnix();
			string cmd = GetCommandFilename();
			string args = GetLogArgs(taskIndex, taskIndex) + GetCommandArguments(taskIndex);
			ProcessStartInfo psi = isUnix
				? new ProcessStartInfo("mono",
					Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), cmd + " " + args))
				: new ProcessStartInfo(cmd, args);
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			externalProcesses[threadIndex] = new Process {StartInfo = psi};
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			externalProcesses[threadIndex].Start();
			externalProcesses[threadIndex].WaitForExit();
			int exitcode = externalProcesses[threadIndex].ExitCode;
			int processid = externalProcesses[threadIndex].Id;
			externalProcesses[threadIndex].Close();
			if (exitcode != 0) {
				throw new Exception("Exception during execution of external process: " + processid);
			}
		}

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
				$"\"{infoFolder}\" \"{GetFilename()}\" \"{id}\" \"{GetName(taskIndex)}\" \"{GetComment(taskIndex)}\" \"{"Process"}\" ";
		}

		protected string GetFilename() {
			return GetMessagePrefix().Trim().Replace("/", "").Replace("(", "_").Replace(")", "_").Replace(" ", "_");
		}

		public virtual bool IsFallbackPosition => true;
		protected abstract string GetCommandArguments(int taskIndex);
		protected abstract string GetCommandFilename();
		protected abstract void InternalCalculation(int taskIndex);
		protected abstract bool ExternalCalculation();
		public abstract string GetMessagePrefix();

		protected virtual string GetComment(int taskIndex) {
			return "";
		}
	}
}