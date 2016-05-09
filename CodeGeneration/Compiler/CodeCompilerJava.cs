using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerJava : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var guid = Guid.NewGuid();

			var fn0 = Path.Combine(Path.GetTempPath(), guid.ToString());
			var fn1 = Path.Combine(Path.GetTempPath(), guid.ToString(), "Program.java");
			var fn3 = Path.Combine(Path.GetTempPath(), guid.ToString(), "Program.class");

			Directory.CreateDirectory(fn0);

			File.WriteAllText(fn1, code);

			#region JVC

			Process jvc = new Process
			{
				StartInfo =
				{
					FileName = "javac",
					Arguments = string.Format("\"{0}\"", fn1),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			jvc.Start();
			dbgOutput.AppendLine();
			dbgOutput.AppendLine("> " + jvc.StartInfo.FileName + " " + jvc.StartInfo.Arguments);

			string jvcoutput = jvc.StandardOutput.ReadToEnd();
			string jvcerror = jvc.StandardError.ReadToEnd();
			jvc.WaitForExit();
			dbgOutput.AppendLine(jvcerror);
			dbgOutput.AppendLine(jvcoutput);

			File.Delete(fn1);
				
			if (jvc.ExitCode != 0)
			{
				Directory.Delete(fn0, true);

				throw new CodeCompilerError(jvcerror, jvc.ExitCode);
			}

			#endregion

			#region JAR

			Process jar = new Process
			{
				StartInfo =
				{
					FileName = "jar",
					Arguments = string.Format("-cfve \"{0}\" Program \"{1}\"", path, Path.GetFileName(fn3)),
					WorkingDirectory = fn0,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			jar.Start();
			dbgOutput.AppendLine();
			dbgOutput.AppendLine("> " + jar.StartInfo.FileName + " " + jar.StartInfo.Arguments);

			string jaroutput = jar.StandardOutput.ReadToEnd();
			string jarerror = jar.StandardError.ReadToEnd();
			jar.WaitForExit();
			dbgOutput.AppendLine(jarerror);
			dbgOutput.AppendLine(jaroutput);

			File.Delete(fn3);

			if (jar.ExitCode != 0)
			{
				Directory.Delete(fn0, true);

				throw new CodeCompilerError(jarerror, jvc.ExitCode);
			}

			#endregion

			Directory.Delete(fn0, true);
		}

		protected override string Execute(string path)
		{
			Process p_prog = new Process
			{
				StartInfo =
				{
					FileName = "java",
					Arguments = string.Format("-jar \"{0}\"", path),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					ErrorDialog = false
				}
			};

			p_prog.Start();
			string output = p_prog.StandardOutput.ReadToEnd();
			string error = p_prog.StandardError.ReadToEnd();
			p_prog.WaitForExit();

			if (p_prog.ExitCode != 0)
			{
				throw new CodeCompilerError(error, p_prog.ExitCode);
			}

			return output;
		}

		protected override string GetCodeExtension()
		{
			return "java";
		}

		protected override string GetBinaryExtension()
		{
			return "jar";
		}

		protected override string GetAcronym()
		{
			return "JVC";
		}
	}
}
