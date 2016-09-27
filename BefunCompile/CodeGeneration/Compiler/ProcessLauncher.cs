using System.Diagnostics;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	public static class ProcessLauncher
	{
		public static ProcessOutput ProcExecute(string command, string arguments) => ProcExecute(command, arguments, null, null);

		public static ProcessOutput ProcExecute(string command, string arguments, StringBuilder dbgOutput) => ProcExecute(command, arguments, null, dbgOutput);

		public static ProcessOutput ProcExecute(string command, string arguments, string workingDirectory) => ProcExecute(command, arguments, workingDirectory, null);

		public static ProcessOutput ProcExecute(string command, string arguments, string workingDirectory, StringBuilder dbgOutput)
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

				if (builderOut.Length == 0)
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

	}
}
