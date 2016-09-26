using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Linq;

// ReSharper disable UnusedParameter.Global
namespace BefunCompile.CodeGeneration.Generator
{
	public abstract class CodeGenerator
	{
		protected readonly MSZipImplementation MSZip = new MSZipImplementation();
		protected readonly GZipImplementation GZip = new GZipImplementation();

		protected readonly BCGraph Graph;
		protected readonly CodeGeneratorOptions Options;

		protected bool FormatOutput => Options.FormatOutput;
		protected bool ImplementSafeStackAccess => Options.ImplementSafeStackAccess;
		protected bool ImplementSafeGridAccess => Options.ImplementSafeGridAccess;
		protected bool UseGZip => Options.UseGZip;
		protected bool AddCosmeticChoices => Options.AddCosmeticChoices;

		private static CodeGenerator Instance(OutputLanguage lang, BCGraph rg, CodeGeneratorOptions options)
		{
			return OutputLanguageHelper.CreateGenerator(lang, rg, options);
		}
		
		protected CodeGenerator(BCGraph graph, CodeGeneratorOptions options)
		{
			Graph = graph;
			Options = options;
		}

		#region Helper

		protected string Indent(string code, string indent)
		{
			return string.Join(Environment.NewLine, code.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(p => indent + p));
		}

		protected bool IsASCIIChar(long chr)
		{
			return
				(chr >= ' ' && chr <= '~' && chr != '\'' && chr != '\\') ||
				(chr == '\r') ||
				(chr == '\n') ||
				(chr == '\t');
		}

		protected string GetASCIICharRep(long chr, string marks)
		{
			if (chr >= ' ' && chr <= '~' && chr != '\'' && chr != '\\')
				return marks + (char)chr + marks;
			if (chr == '\r')
				return marks + @"\r" + marks;
			if (chr == '\n')
				return marks + @"\n" + marks;
			if (chr == '\t')
				return marks + @"\t" + marks;

			return null;
		}

		protected string Paren(string input, bool doParenthesis = true)
		{
			return doParenthesis ? ('(' + input + ')') : input;
		}

		#endregion
		
		public static string GenerateCode(OutputLanguage l, BCGraph comp, CodeGeneratorOptions options)
		{
			return Instance(l, comp, options).GenerateCode();
		}

		#region Abstract generation

		protected abstract string GenerateCode();

		public abstract string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp);

		public abstract string GenerateCodeBCVertexBlock(BCVertexBlock comp);

		public abstract string GenerateCodeBCVertexDecision(BCVertexDecision comp);

		public abstract string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp);

		public abstract string GenerateCodeBCVertexDup(BCVertexDup comp);

		public abstract string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp);

		public abstract string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp); 

		public abstract string GenerateCodeBCVertexExpression(BCVertexExpression comp);

		public abstract string GenerateCodeBCVertexExprGet(BCVertexExprGet comp);

		public abstract string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp);

		public abstract string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp);

		public abstract string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp);

		public abstract string GenerateCodeBCVertexExprSet(BCVertexExprSet comp);

		public abstract string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp);

		public abstract string GenerateCodeBCVertexGet(BCVertexGet comp);

		public abstract string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp);

		public abstract string GenerateCodeBCVertexInput(BCVertexInput comp);

		public abstract string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp);

		public abstract string GenerateCodeBCVertexNOP(BCVertexNOP comp);

		public abstract string GenerateCodeBCVertexNot(BCVertexNot comp);

		public abstract string GenerateCodeBCVertexOutput(BCVertexOutput comp);

		public abstract string GenerateCodeBCVertexPop(BCVertexPop comp);

		public abstract string GenerateCodeBCVertexRandom(BCVertexRandom comp);

		public abstract string GenerateCodeBCVertexSet(BCVertexSet comp);

		public abstract string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp);

		public abstract string GenerateCodeBCVertexSwap(BCVertexSwap comp);

		public abstract string GenerateCodeBCVertexVarGet(BCVertexVarGet comp);

		public abstract string GenerateCodeBCVertexVarSet(BCVertexVarSet comp);

		public abstract string GenerateCodeExpressionBCast(ExpressionBCast comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionNotDecision(ExpressionNot comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionNot(ExpressionNot comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionPeek(ExpressionPeek comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionVariable(ExpressionVariable comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionConstant(ExpressionConstant comp, bool forceLongReturn);

		public abstract string GenerateCodeExpressionGet(ExpressionGet comp, bool forceLongReturn);

		#endregion
	}
}
