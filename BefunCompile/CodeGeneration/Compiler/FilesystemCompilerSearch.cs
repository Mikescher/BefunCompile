using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BefunCompile.CodeGeneration.Compiler
{
	public class FilesystemCompilerSearch
	{
		private static IEnumerable<string> SafeEnumerateDirectories(params string[] paths)
		{
			foreach (var spath in paths)
			{
				var path = Environment.ExpandEnvironmentVariables(spath);
				if (!Directory.Exists(path)) continue;

				foreach (var dir in Directory.EnumerateDirectories(path)) yield return dir;
			}
		}

		private static IEnumerable<string> SafeEnumerateFiles(params string[] paths)
		{
			foreach (var spath in paths)
			{
				var path = Environment.ExpandEnvironmentVariables(spath);
				if (!Directory.Exists(path)) continue;

				foreach (var dir in Directory.EnumerateFiles(path)) yield return dir;
			}
		}

		private static IEnumerable<string> SafeEnumeratePathDirectories()
		{
			var pathVars = Environment.GetEnvironmentVariable("PATH");
			if (pathVars == null) yield break;

			foreach (var path in pathVars.Split(';'))
			{
				if (Directory.Exists(path)) yield return path;
			}
		}

		public static IEnumerable<string> FindGCC()
		{
			if (File.Exists("gcc.exe")) yield return Path.GetFullPath("gcc.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "gcc.exe"))) yield return Path.Combine(path, "gcc.exe");
			}

			var files = SafeEnumerateFiles(@"C:\MinGW\bin\", @"%ProgramW6432%\MinGW\bin\", @"%programfiles%\MinGW\bin\", @"%programfiles(x86)%\MinGW\bin\")
							.Distinct()
							.OrderByDescending(Path.GetFileName).ToList();

			foreach (var file in files)
			{
				if ((Path.GetFileName(file) ?? "").ToLower() == "gcc.exe") yield return file;
			}
		}

		public static IEnumerable<string> FindCSC()
		{
			if (File.Exists("csc.exe")) yield return Path.GetFullPath("csc.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "csc.exe"))) yield return Path.Combine(path, "csc.exe");
			}

			var windirs = SafeEnumerateDirectories(@"%windir%\Microsoft.NET\Framework\", @"%windir%\Microsoft.NET\Framework64\")
								.Distinct()
								.OrderByDescending(Path.GetFileName)
								.ToList();

			foreach (var dir in windirs)
			{
				if (File.Exists(Path.Combine(dir, "csc.exe"))) yield return Path.Combine(dir, "csc.exe");
			}

			var msbuilddirs = SafeEnumerateDirectories(@"%ProgramW6432%\MSBuild\", @"%programfiles%\MSBuild\", @"%programfiles(x86)%\MSBuild\")
								.Distinct()
								.OrderByDescending(Path.GetFileName)
								.ToList();

			foreach (var dir in msbuilddirs)
			{
				if (File.Exists(Path.Combine(dir, "Bin", "csc.exe"))) yield return Path.Combine(dir, "Bin", "csc.exe");
			}
		}

		public static IEnumerable<string> FindJAVAC()
		{
			if (File.Exists("javac.exe")) yield return Path.GetFullPath("javac.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "javac.exe"))) yield return Path.Combine(path, "javac.exe");
			}

			var dirs = SafeEnumerateDirectories(@"C:\Java\", @"%ProgramW6432%\Java\", @"%programfiles%\Java\", @"%programfiles(x86)%\Java\")
								.Distinct()
								.OrderByDescending(Path.GetFileName).ToList();

			foreach (var dir in dirs)
			{
				if (File.Exists(Path.Combine(dir, "bin", "javac.exe"))) yield return Path.Combine(dir, "bin", "javac.exe");
			}
		}

		public static IEnumerable<string> FindJAR()
		{
			if (File.Exists("jar.exe")) yield return Path.GetFullPath("jar.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "jar.exe"))) yield return Path.Combine(path, "jar.exe");
			}

			var dirs = SafeEnumerateDirectories(@"C:\Java\", @"%ProgramW6432%\Java\", @"%programfiles%\Java\", @"%programfiles(x86)%\Java\")
								.Distinct()
								.OrderByDescending(Path.GetFileName).ToList();

			foreach (var dir in dirs)
			{
				if (File.Exists(Path.Combine(dir, "bin", "jar.exe"))) yield return Path.Combine(dir, "bin", "jar.exe");
			}
		}

		public static IEnumerable<string> FindJAVA()
		{
			if (File.Exists("java.exe")) yield return Path.GetFullPath("java.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "java.exe"))) yield return Path.Combine(path, "java.exe");
			}

			var dirs = SafeEnumerateDirectories(@"C:\Java\", @"%ProgramW6432%\Java\", @"%programfiles%\Java\", @"%programfiles(x86)%\Java\")
								.Distinct()
								.OrderByDescending(Path.GetFileName).ToList();

			foreach (var dir in dirs)
			{
				if (File.Exists(Path.Combine(dir, "bin", "java.exe"))) yield return Path.Combine(dir, "bin", "java.exe");
			}
		}

		public static IEnumerable<string> FindPYTH()
		{
			if (File.Exists("pypy.exe")) yield return Path.GetFullPath("pypy.exe");
			if (File.Exists("python.exe")) yield return Path.GetFullPath("python.exe");
			if (File.Exists("py.exe")) yield return Path.GetFullPath("py.exe");

			foreach (var path in SafeEnumeratePathDirectories())
			{
				if (File.Exists(Path.Combine(path, "pypy.exe"))) yield return Path.Combine(path, "pypy.exe");
				if (File.Exists(Path.Combine(path, "python.exe"))) yield return Path.Combine(path, "python.exe");
				if (File.Exists(Path.Combine(path, "py.exe"))) yield return Path.Combine(path, "py.exe");
			}

			var dirsPy = SafeEnumerateDirectories(@"C:\", @"%ProgramW6432%\Python\", @"%programfiles%\Python\", @"%programfiles(x86)%\Python\", @"%LOCALAPPDATA%\Programs\Python")
									.Distinct()
									.OrderByDescending(Path.GetFileName).ToList();

			foreach (var dir in dirsPy)
			{
				if (File.Exists(Path.Combine(dir, "python.exe"))) yield return Path.Combine(dir, "python.exe");
			}
		}

		public static IEnumerable<string> FindPYTH2()
		{
			foreach (var py in FindPYTH())
			{
				if (py.ToLower().EndsWith("py.exe")) yield return py; // PYTHON LAUNCHER

				bool found = false;
				try
				{
					var output = ProcessLauncher.ProcExecute(py, "--version");
					if (output.ExitCode == 0 && output.StdOut.ToLower().StartsWith("python 2")) found = true;
				}
				catch
				{
					found = false;
				}
				if (found) yield return py;
			}
		}

		public static IEnumerable<string> FindPYTH3()
		{
			foreach (var py in FindPYTH())
			{
				if (py.ToLower().EndsWith("py.exe")) yield return py; // PYTHON LAUNCHER

				bool found = false;
				try
				{
					var output = ProcessLauncher.ProcExecute(py, "--version");
					if (output.ExitCode == 0 && output.StdOut.ToLower().StartsWith("python 3")) found = true;
				}
				catch
				{
					found = false;
				}
				if (found) yield return py;
			}
		}
	}
}
