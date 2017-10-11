using System.Diagnostics;
using System.Text;

// ReSharper disable MethodOverloadWithOptionalParameter
namespace BefunCompile.CodeGeneration.Compiler
{
	public static class ProcessLauncher
	{
		public static Process CurrentProcess;

		public static ProcessOutput ProcExecute(string command, string arguments, int? timeout) => ProcExecute(command, arguments, null, null, timeout);

		public static ProcessOutput ProcExecute(string command, string arguments) => ProcExecute(command, arguments, null, null, null);

		public static ProcessOutput ProcExecute(string command, string arguments, IOutputReciever dbgOutput = null, int? timeout = null) => ProcExecute(command, arguments, null, dbgOutput, timeout);

		public static ProcessOutput ProcExecute(string command, string arguments, string workingDirectory, IOutputReciever dbgOutput, int? timeout)
		{
			if (dbgOutput == null) dbgOutput = new DummyReciever();

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
			CurrentProcess = process;

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			if (timeout.HasValue)
			{
				var result = process.WaitForExit(timeout.Value);

				if (!result)
				{
					process.Kill();
					return new ProcessOutput(-1, builderOut.ToString(), builderErr + "\r\nProcess manually terminated by controller after timeout");
				}
			}
			else
			{
				process.WaitForExit();
				
			}

			return new ProcessOutput(process.ExitCode, builderOut.ToString(), builderErr.ToString());
		}

	}
}
