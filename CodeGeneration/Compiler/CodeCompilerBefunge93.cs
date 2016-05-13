using System;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerBefunge93 : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			throw new NotImplementedException();
		}

		protected override string Execute(string path)
		{
			throw new NotImplementedException();
		}

		protected override string GetCodeExtension()
		{
			return "b93";
		}

		protected override string GetBinaryExtension()
		{
			return "b93";
		}

		protected override string GetAcronym()
		{
			return "B93";
		}
	}
}
