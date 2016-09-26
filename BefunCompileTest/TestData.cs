using BefunCompile;
using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Compiler;
using BefunCompile.CodeGeneration.Generator;
using BefunCompileTest.Properties;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace BefunCompileTest
{
	public static class TestData
	{
		public class BFDataSet
		{
			public readonly string Name;
			public readonly string Code;
			public readonly string Result;

			public BFDataSet(string n, string c, string r)
			{
				Name = n;
				Code = c;
				Result = r;
			}

			public override string ToString() { return Name; }
		}

		public static readonly BFDataSet[] Data =
		{
			new BFDataSet("data_001", Resources.testdata_001, "233168"),
			new BFDataSet("data_002", Resources.testdata_002, "4613732"),
			new BFDataSet("data_003", Resources.testdata_003, "6857"),
			new BFDataSet("data_005", Resources.testdata_005, "232792560"),
			new BFDataSet("data_006", Resources.testdata_006, "25164150"),
			new BFDataSet("data_008", Resources.testdata_008, "5576689664895=23514624000"),
			new BFDataSet("data_011", Resources.testdata_011, "70600674"),
			new BFDataSet("data_013", Resources.testdata_013, "5537376230"),
			new BFDataSet("data_015", Resources.testdata_015, "137846528820"),
			new BFDataSet("data_016", Resources.testdata_016, "1366"),
			new BFDataSet("data_017", Resources.testdata_017, "21124"),
			new BFDataSet("data_018", Resources.testdata_018, "1074"),
			new BFDataSet("data_019", Resources.testdata_019, "171"),
			new BFDataSet("data_020", Resources.testdata_020, "648"),
			new BFDataSet("data_024", Resources.testdata_024, "2783915460"),
			new BFDataSet("data_026", Resources.testdata_026, "983"),
			new BFDataSet("data_028", Resources.testdata_028, "669171001"),
			new BFDataSet("data_031", Resources.testdata_031, "73682"),
			new BFDataSet("data_033", Resources.testdata_033, "100"),
			new BFDataSet("data_036", Resources.testdata_036, @"585585\n9009\n7447\n99\n33\n73737\n53835\n53235\n39993\n32223\n15351\n717\n585\n313\n9\n7\n5\n3\n1\n =872187"),
			new BFDataSet("data_038", Resources.testdata_038, "932718654"),
			new BFDataSet("data_039", Resources.testdata_039, "840"),
			new BFDataSet("data_040", Resources.testdata_040, "210"),
			new BFDataSet("data_041", Resources.testdata_041, "7652413"),
			new BFDataSet("data_042", Resources.testdata_042, "162"),
			new BFDataSet("data_043", Resources.testdata_043, @"4130952867\n1430952867\n4160357289\n1460357289\n4106357289\n1406357289\n\n= 16695334890"),
			new BFDataSet("data_045", Resources.testdata_045, "1533776805"),
			new BFDataSet("data_048", Resources.testdata_048, "9110846700"),
			new BFDataSet("data_049", Resources.testdata_049, "2969 6299 9629"),
			new BFDataSet("data_052", Resources.testdata_052, "142857"),
			new BFDataSet("data_053", Resources.testdata_053, "4075"),
			new BFDataSet("data_055", Resources.testdata_055, "249"),
			new BFDataSet("data_058", Resources.testdata_058, "26241"),
			new BFDataSet("data_061", Resources.testdata_061, @"1281\n8128\n2882\n8256\n5625\n2512\n  = 28684"),
			new BFDataSet("data_063", Resources.testdata_063, "49"),
			new BFDataSet("data_064", Resources.testdata_064, "1322"),
			new BFDataSet("data_065", Resources.testdata_065, "272"),
			new BFDataSet("data_067", Resources.testdata_067, "7273"),
			new BFDataSet("data_068", Resources.testdata_068, "6531031914842725"),
			new BFDataSet("data_069", Resources.testdata_069, "510510"),
			new BFDataSet("data_071", Resources.testdata_071, @"428570 /\n999997"),
			new BFDataSet("data_076", Resources.testdata_076, "190569291"),
			new BFDataSet("data_077", Resources.testdata_077, "71"),
			new BFDataSet("data_079", Resources.testdata_079, "73162890"),
		};

		public static readonly BFDataSet[] Data_fast =
		{
			new BFDataSet("data_005", Resources.testdata_005, "232792560"),
			new BFDataSet("data_018", Resources.testdata_018, "1074"),
			new BFDataSet("data_024", Resources.testdata_024, "2783915460"),
			new BFDataSet("data_040", Resources.testdata_040, "210"),
			new BFDataSet("data_069", Resources.testdata_069, "510510"),
			new BFDataSet("data_079", Resources.testdata_079, "73162890"),
		};

		public static void Test_Execute(TestData.BFDataSet set, OutputLanguage lang)
		{
			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			var gencode = compiler.GenerateCode(lang);

			var file = Path.GetTempFileName() + "." + CodeCompiler.GetBinaryExtension(lang);

			var consoleBuilder = new StringBuilder();
			CodeCompiler.Compile(lang, gencode, file, consoleBuilder);
			Console.Out.WriteLine(consoleBuilder.ToString());

			string output = CodeCompiler.Execute(lang, file).Replace("\r\n", "\n").Replace("\n", "\\n");

			Assert.AreEqual(output, set.Result);
		}

		public static void Test_Generate(TestData.BFDataSet set, OutputLanguage lang)
		{
			var compiler = new BefunCompiler(set.Code, true, new CodeGeneratorOptions(true, true, true, true, false));

			var result = compiler.GenerateCode(lang);

			Assert.IsNotEmpty(result);
		}
	}
}
