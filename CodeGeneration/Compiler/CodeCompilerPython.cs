using System.Diagnostics;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerPython : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path)
		{
			Process prog = new Process
			{
				StartInfo =
				{
					FileName = "python",
					Arguments = "\""+path+"\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			prog.Start();
			string output = prog.StandardOutput.ReadToEnd();
			string error = prog.StandardError.ReadToEnd();
			prog.WaitForExit();

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(error, prog.ExitCode);
			}

			return output;
		}

		protected override string GetCodeExtension()
		{
			return "py";
		}

		protected override string GetBinaryExtension()
		{
			return "py";
		}

		protected override string GetAcronym()
		{
			return "PYT";
		}
	}
}
