using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerC : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var fn1 = Path.GetTempPath() + Guid.NewGuid() + ".b93.c";

			File.WriteAllText(fn1, code);

			Process gcc = new Process
			{
				StartInfo =
				{
					FileName = "gcc.exe",
					Arguments = string.Format(" -x c \"{0}\" -o \"{1}\"", fn1, path),
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			gcc.Start();
			dbgOutput.AppendLine();
			dbgOutput.AppendLine("> " + gcc.StartInfo.FileName + " " + gcc.StartInfo.Arguments);

			string gccoutput = gcc.StandardOutput.ReadToEnd();
			string gccerror = gcc.StandardError.ReadToEnd();
			gcc.WaitForExit();

			dbgOutput.AppendLine(gccerror);
			dbgOutput.AppendLine(gccoutput);

			if (gcc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(gccerror, gcc.ExitCode);
			}
		}

		protected override string Execute(string path)
		{
			Process prog = new Process
			{
				StartInfo =
				{
					FileName = path,
					Arguments = string.Empty,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			prog.Start();
			string output = prog.StandardOutput.ReadToEnd();
			prog.WaitForExit();

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(output, prog.ExitCode);
			}
			
			return output;
		}

		protected override string GetCodeExtension()
		{
			return "c";
		}

		protected override string GetBinaryExtension()
		{
			return "exe";
		}

		protected override string GetAcronym()
		{
			return "GCC";
		}
	}
}
