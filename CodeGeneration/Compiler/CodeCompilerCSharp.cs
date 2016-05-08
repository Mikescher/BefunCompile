using System.Diagnostics;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerCSharp : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var fn1 = Path.GetTempFileName() + ".b93.cs";
			File.WriteAllText(fn1, code);

			Process csc = new Process
			{
				StartInfo =
				{
					FileName = "csc.exe",
					Arguments = string.Format("/out:\"{1}\" /optimize /nologo \"{0}\"", fn1, path),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			csc.Start();
			dbgOutput.AppendLine();
			dbgOutput.AppendLine("> " + csc.StartInfo.FileName + " " + csc.StartInfo.Arguments);

			string cscoutput = csc.StandardOutput.ReadToEnd();
			string cscerror = csc.StandardError.ReadToEnd();
			csc.WaitForExit();

			dbgOutput.AppendLine(cscerror);
			dbgOutput.AppendLine(cscoutput);

			if (csc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(cscoutput, csc.ExitCode);
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
			return "cs";
		}

		protected override string GetBinaryExtension()
		{
			return "exe";
		}

		protected override string GetAcronym()
		{
			return "CSC";
		}
	}
}
