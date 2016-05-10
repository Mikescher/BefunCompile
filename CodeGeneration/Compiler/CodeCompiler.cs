using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	public abstract class CodeCompiler
	{
		private static Dictionary<OutputLanguage, CodeCompiler> _instances = null;

		private static CodeCompiler Instance(OutputLanguage lang)
		{
			if (_instances == null) _instances = new Dictionary<OutputLanguage, CodeCompiler>();

			if (_instances.ContainsKey(lang)) return _instances[lang];

			switch (lang)
			{
				case OutputLanguage.CSharp:
					return _instances[lang] = new CodeCompilerCSharp();
				case OutputLanguage.C:
					return _instances[lang] = new CodeCompilerC();
				case OutputLanguage.Python:
					return _instances[lang] = new CodeCompilerPython();
				case OutputLanguage.Java:
					return _instances[lang] = new CodeCompilerJava();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void Compile(OutputLanguage l, string code, string path)
		{
			Compile(l, code, path, new StringBuilder());
		}

		public static void Compile(OutputLanguage l, string code, string path, StringBuilder dbgOutput)
		{
			Instance(l).Compile(code, path, dbgOutput);
		}

		public static string Execute(OutputLanguage l, string path)
		{
			return Instance(l).Execute(path);
		}

		public static string GetCodeExtension(OutputLanguage l)
		{
			return Instance(l).GetCodeExtension();
		}

		public static string GetBinaryExtension(OutputLanguage l)
		{
			return Instance(l).GetBinaryExtension();
		}

		public static string GetAcronym(OutputLanguage l)
		{
			return Instance(l).GetAcronym();
		}

		public static string ExecuteCode(OutputLanguage l, string code, StringBuilder dbgOutput)
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "." + GetBinaryExtension(l));

			try
			{
				Compile(l, code, path, dbgOutput ?? new StringBuilder());
				return Execute(l, path);
			}
			finally
			{
				File.Delete(path);
			}
		}

		protected ProcessOutput ProcExecute(string command, string arguments) => ProcExecute(command, arguments, null, null);

		protected ProcessOutput ProcExecute(string command, string arguments, StringBuilder dbgOutput) => ProcExecute(command, arguments, null, dbgOutput);

		protected ProcessOutput ProcExecute(string command, string arguments, string workingDirectory) => ProcExecute(command, arguments, workingDirectory, null);

		protected ProcessOutput ProcExecute(string command, string arguments, string workingDirectory, StringBuilder dbgOutput)
		{
			if (dbgOutput == null) dbgOutput = new StringBuilder();

			dbgOutput.AppendLine();
			dbgOutput.AppendLine(string.Format("> {0} {1}", command, arguments));

			Process process = new Process
			{
				StartInfo =
				{
					FileName = command,
					Arguments = arguments,
					WorkingDirectory = workingDirectory ?? string.Empty,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			StringBuilder builderOut = new StringBuilder();
			StringBuilder builderErr = new StringBuilder();

			process.OutputDataReceived += (sender, args) =>
			{
				if (args.Data == null) return;

				dbgOutput.AppendLine("1> " + args.Data);

				if (builderErr.Length == 0)
					builderOut.Append(args.Data);
				else
					builderOut.Append("\n" + args.Data);
			};

			process.ErrorDataReceived += (sender, args) =>
			{
				if (args.Data == null) return;

				dbgOutput.AppendLine("2> " + args.Data);

				if (builderErr.Length == 0)
					builderErr.Append(args.Data);
				else
					builderErr.Append("\n" + args.Data);
			};
			
			process.Start();

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			

			process.WaitForExit();

			return new ProcessOutput(process.ExitCode, builderOut.ToString(), builderErr.ToString());
		}

		protected abstract void Compile(string code, string path, StringBuilder dbgOutput);
		protected abstract string Execute(string path);
		protected abstract string GetCodeExtension();
		protected abstract string GetBinaryExtension();
		protected abstract string GetAcronym();
	}
}
