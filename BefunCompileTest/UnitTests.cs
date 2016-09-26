using BefunCompile;
using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Compiler;
using BefunCompile.CodeGeneration.Generator;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace BefunCompileTest
{
	public class UnitTests
	{
		public static object testdata = TestData.Data;

		[TestCaseSource(nameof(testdata))]
		public void Test_GCC_Compile(TestData.BFDataSet set)
		{
			const OutputLanguage LANGUAGE = OutputLanguage.C;

			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			var gencode = compiler.GenerateCode(LANGUAGE);

			var file = Path.GetTempFileName() + "." + CodeCompiler.GetBinaryExtension(LANGUAGE);

			var consoleBuilder = new StringBuilder();
			CodeCompiler.Compile(LANGUAGE, gencode, file, consoleBuilder);
			Console.Out.WriteLine(consoleBuilder.ToString());
			
			string output = CodeCompiler.Execute(LANGUAGE, file).Replace("\r\n", "\n").Replace("\n", "\\n");

			Assert.AreEqual(output, set.Result);
		}

		[TestCaseSource(nameof(testdata))]
		public void Test_CSC_Compile(TestData.BFDataSet set)
		{
			const OutputLanguage LANGUAGE = OutputLanguage.CSharp;

			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			var gencode = compiler.GenerateCode(LANGUAGE);

			var file = Path.GetTempFileName() + "." + CodeCompiler.GetBinaryExtension(LANGUAGE);

			var consoleBuilder = new StringBuilder();
			CodeCompiler.Compile(LANGUAGE, gencode, file, consoleBuilder);
			Console.Out.WriteLine(consoleBuilder.ToString());

			string output = CodeCompiler.Execute(LANGUAGE, file).Replace("\r\n", "\n").Replace("\n", "\\n");

			Assert.AreEqual(output, set.Result);
		}

		[TestCaseSource(nameof(testdata))]
		public void Test_JVC_Compile(TestData.BFDataSet set)
		{
			const OutputLanguage LANGUAGE = OutputLanguage.Java;

			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			var gencode = compiler.GenerateCode(LANGUAGE);

			var file = Path.GetTempFileName() + "." + CodeCompiler.GetBinaryExtension(LANGUAGE);

			var consoleBuilder = new StringBuilder();
			CodeCompiler.Compile(LANGUAGE, gencode, file, consoleBuilder);
			Console.Out.WriteLine(consoleBuilder.ToString());

			string output = CodeCompiler.Execute(LANGUAGE, file).Replace("\r\n", "\n").Replace("\n", "\\n");

			Assert.AreEqual(output, set.Result);
		}

		[TestCaseSource(nameof(testdata))]
		public void Test_PY2_Generate(TestData.BFDataSet set)
		{
			const OutputLanguage LANGUAGE = OutputLanguage.Python2;

			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			compiler.GenerateCode(LANGUAGE);
		}

		[TestCaseSource(nameof(testdata))]
		public void Test_PY3_Generate(TestData.BFDataSet set)
		{
			const OutputLanguage LANGUAGE = OutputLanguage.Python3;

			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			compiler.GenerateCode(LANGUAGE);
		}

		//[TestCaseSource(nameof(testdata))]
		//public void Test_TF_Generate(TestData.BFDataSet set)
		//{
		//	const OutputLanguage LANGUAGE = OutputLanguage.TextFunge;
		//
		//	var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));
		//
		//	compiler.GenerateCode(LANGUAGE);
		//}
		//
		//[TestCaseSource(nameof(testdata))]
		//public void Test_BF_Generate(TestData.BFDataSet set)
		//{
		//	const OutputLanguage LANGUAGE = OutputLanguage.Befunge93;
		//
		//	var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));
		//
		//	compiler.GenerateCode(LANGUAGE);
		//}
	}
}
