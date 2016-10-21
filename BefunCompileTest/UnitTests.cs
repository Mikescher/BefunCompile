using BefunCompile.CodeGeneration;
using NUnit.Framework;

namespace BefunCompileTest
{
	[TestFixture]
	public class UnitTests
	{
		private static object TD_FULL = TestData.Data;
		private static object TD_SMALL = TestData.Data_fast;

		[TestCaseSource(nameof(TD_FULL))]
		public void Test_GCC_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.C); }

		//[TestCaseSource(nameof(TD_FULL))]
		//public void Test_GCC_Compile(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.C); }

		[TestCaseSource(nameof(TD_FULL))]
		public void Test_GCC_RunWithTCC(TestData.BFDataSet set) { TestData.Test_RunTCC(set, OutputLanguage.C); }




		[TestCaseSource(nameof(TD_FULL))]
		public void Test_CSC_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.CSharp); }

		//[TestCaseSource(nameof(TD_FULL))]
		//public void Test_CSC_Compile(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.CSharp); }




		[TestCaseSource(nameof(TD_FULL))]
		public void Test_JVC_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.Java); }

		//[TestCaseSource(nameof(TD_FULL))]
		//public void Test_JVC_Compile(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.Java); }




		[TestCaseSource(nameof(TD_FULL))]
		public void Test_PY2_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.Python2); }

		//[TestCaseSource(nameof(TD_SMALL))]
		//public void Test_PY2_Compile(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.Python2); }




		[TestCaseSource(nameof(TD_FULL))]
		public void Test_PY3_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.Python3); }

		//[TestCaseSource(nameof(TD_SMALL))]
		//public void Test_PY3_Compile(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.Python3); }




		//[TestCaseSource(nameof(TD_FULL))]
		//public void Test_TF_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.TextFunge); }




		//[TestCaseSource(nameof(TD_FULL))]
		//public void Test_BF_Generate(TestData.BFDataSet set) { TestData.Test_Generate(set, OutputLanguage.Befunge93); }

		//[TestCaseSource(nameof(TD_SMALL))]
		//public void Test_BF_Execute(TestData.BFDataSet set) { TestData.Test_Execute(set, OutputLanguage.Befunge93); }
	}
}
