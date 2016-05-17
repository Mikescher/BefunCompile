using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunGen.AST;
using BefunGen.AST.CodeGen;
using System;
using System.Text.RegularExpressions;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorBefunge93 : CodeGenerator
	{
		public CodeGeneratorBefunge93(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip) 
			: base(comp, fmtOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip)
		{
			// <EMPTY />
		}

		protected override string GenerateCode()
		{
			string codeFunge = CodeGenerator.GenerateCode(OutputLanguage.TextFunge, Graph, FormatOutput, ImplementSafeStackAccess, ImplementSafeGridAccess, UseGZip);

			var parser = new TextFungeParser();

			ASTObject.CGO = new CodeGenOptions
			{
				NumberLiteralRepresentation = NumberRep.Best,
				SetNOPCellsToCustom = false,

				DefaultNumeralValue = 0,
				DefaultCharacterValue = ' ',
				DefaultBooleanValue = false,

				StripDoubleStringmodeToogle = true,
				CompressHorizontalCombining = true,
				CompressVerticalCombining = true,
				CompileTimeEvaluateExpressions = true,
				RemUnreferencedMethods = true,

				ExtendedBooleanCast = false,
				DisplayModuloAccess = false,

				DefaultVarDeclarationWidth = 16,

				DefaultDisplayValue = ' ',
				DisplayBorder = '#',
				DisplayBorderThickness = 1,

				DefaultVarDeclarationSymbol = ' ',
				DefaultTempSymbol = ' ',
				DefaultResultTempSymbol = ' ',
				CustomNOPSymbol = '@',
			};

			var code = parser.generateCode(codeFunge, TextFungeParser.ExtractDisplayFromTFFormat(codeFunge), false);
			var codeLines = Regex.Split(code, @"\r?\n");
			codeLines[0] = codeLines[0].TrimEnd() + "  |  compiled with BefunCompile v" + BefunCompiler.VERSION + "(c) 2015";

			return string.Join(Environment.NewLine, codeLines);
		}

		public override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexBlock(BCVertexBlock comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexDecision(BCVertexDecision comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexDup(BCVertexDup comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExpression(BCVertexExpression comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexGet(BCVertexGet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexInput(BCVertexInput comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexNOP(BCVertexNOP comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexNot(BCVertexNot comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexOutput(BCVertexOutput comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexPop(BCVertexPop comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexRandom(BCVertexRandom comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexSet(BCVertexSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexSwap(BCVertexSwap comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionBCast(ExpressionBCast comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionNotDecision(ExpressionNot comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionNot(ExpressionNot comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionPeek(ExpressionPeek comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionVariable(ExpressionVariable comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionConstant(ExpressionConstant comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		public override string GenerateCodeExpressionGet(ExpressionGet comp, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}
	}
}
