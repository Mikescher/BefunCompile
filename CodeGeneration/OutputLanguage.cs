
using BefunCompile.CodeGeneration.Compiler;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph;
using System;

namespace BefunCompile.CodeGeneration
{
	public enum OutputLanguage
	{
		CSharp,
		C,
		Python2,
		Python3,
		Java,
		TextFunge,
		Befunge93
	}

	public static class OutputLanguageHelper
	{
		public static OutputLanguage? ParseFromAbbrev(string abbrev)
		{
			switch (abbrev)
			{
				case "cs":
				case "csharp":
					return OutputLanguage.CSharp;
				case "c":
				case "ansic":
					return OutputLanguage.C;
				case "python":
				case "py":
				case "python3":
				case "py3":
					return OutputLanguage.Python3;
				case "python2":
				case "py2":
					return OutputLanguage.Python2;
				case "java":
					return OutputLanguage.Java;
				case "tf":
				case "textfunge":
					return OutputLanguage.TextFunge;
				case "befunge":
				case "befunge93":
				case "bef":
				case "bef93":
					return OutputLanguage.Befunge93;
				default:
					return null;
			}
		}

		public static CodeCompiler CreateCompiler(OutputLanguage ol)
		{
			switch (ol)
			{
				case OutputLanguage.CSharp:
					return new CodeCompilerCSharp();
				case OutputLanguage.C:
					return new CodeCompilerC();
				case OutputLanguage.Python2:
					return new CodeCompilerPython2();
				case OutputLanguage.Python3:
					return new CodeCompilerPython3();
				case OutputLanguage.Java:
					return new CodeCompilerJava();
				case OutputLanguage.TextFunge:
					return new CodeCompilerTextFunge();
				case OutputLanguage.Befunge93:
					return new CodeCompilerBefunge93();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static CodeGenerator CreateGenerator(OutputLanguage ol, BCGraph rg, bool fmt, bool ssa, bool sga, bool gz)
		{
			switch (ol)
			{
				case OutputLanguage.CSharp:
					return new CodeGeneratorCSharp(rg, fmt, ssa, sga, gz);
				case OutputLanguage.C:
					return new CodeGeneratorC(rg, fmt, ssa, sga, gz);
				case OutputLanguage.Python2:
					return new CodeGeneratorPython2(rg, fmt, ssa, sga, gz);
				case OutputLanguage.Python3:
					return new CodeGeneratorPython3(rg, fmt, ssa, sga, gz);
				case OutputLanguage.Java:
					return new CodeGeneratorJava(rg, fmt, ssa, sga, gz);
				case OutputLanguage.TextFunge:
					return new CodeGeneratorTextFunge(rg, fmt, ssa, sga, gz);
				case OutputLanguage.Befunge93:
					return new CodeGeneratorBefunge93(rg, fmt, ssa, sga, gz);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
