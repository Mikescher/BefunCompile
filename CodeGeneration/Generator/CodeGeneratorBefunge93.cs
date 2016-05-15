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
		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			string codeFunge = CodeGenerator.GenerateCode(OutputLanguage.TextFunge, comp, fmtOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip);

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

		protected override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexBlock(BCVertexBlock comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexDecision(BCVertexDecision comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexDup(BCVertexDup comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExpression(BCVertexExpression comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexGet(BCVertexGet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexNOP(BCVertexNOP comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexNot(BCVertexNot comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexRandom(BCVertexRandom comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionBCast(ExpressionBCast comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionNotDecision(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionNot(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionPeek(ExpressionPeek comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionVariable(ExpressionVariable comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionConstant(ExpressionConstant comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}
	}
}
