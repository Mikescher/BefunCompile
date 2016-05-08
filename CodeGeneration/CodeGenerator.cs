using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedParameter.Global
namespace BefunCompile.CodeGeneration
{
	public abstract class CodeGenerator
	{
		protected readonly MSZipImplementation MSZip = new MSZipImplementation();
		protected readonly GZipImplementation GZip = new GZipImplementation();

		private static Dictionary<OutputLanguage, CodeGenerator> _instances = null;

		private static CodeGenerator Instance(OutputLanguage lang)
		{
			if (_instances == null) _instances = new Dictionary<OutputLanguage, CodeGenerator>();

			if (_instances.ContainsKey(lang)) return _instances[lang];

			switch (lang)
			{
				case OutputLanguage.CSharp:
					return _instances[lang] = new CodeGeneratorCSharp();
				case OutputLanguage.C:
					return _instances[lang] = new CodeGeneratorC();
				case OutputLanguage.Python:
					return _instances[lang] = new CodeGeneratorPython();
				case OutputLanguage.Java:
					return _instances[lang] = new CodeGeneratorJava();
				default:
					throw new ArgumentOutOfRangeException();
			}
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

		#region Static generation

		public static string GenerateCode(OutputLanguage l, BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			return Instance(l).GenerateCode(comp, fmtOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip);
		}

		public static string GenerateCodeBCVertexBinaryMath(OutputLanguage l, BCVertexBinaryMath comp)
		{
			return Instance(l).GenerateCodeBCVertexBinaryMath(comp);
		}

		public static string GenerateCodeBCVertexBlock(OutputLanguage l, BCVertexBlock comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexBlock(comp, g);
		}

		public static string GenerateCodeBCVertexDecision(OutputLanguage l, BCVertexDecision comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexDecision(comp, g);
		}

		public static string GenerateCodeBCVertexDecisionBlock(OutputLanguage l, BCVertexDecisionBlock comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexDecisionBlock(comp, g);
		}

		public static string GenerateCodeBCVertexDup(OutputLanguage l, BCVertexDup comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexDup(comp, g);
		}

		public static string GenerateCodeBCVertexExprDecision(OutputLanguage l, BCVertexExprDecision comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprDecision(comp, g); 
		}

		public static string GenerateCodeBCVertexExprDecisionBlock(OutputLanguage l, BCVertexExprDecisionBlock comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprDecisionBlock(comp, g);
		}

		public static string GenerateCodeBCVertexExpression(OutputLanguage l, BCVertexExpression comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExpression(comp, g);
		}

		public static string GenerateCodeBCVertexExprGet(OutputLanguage l, BCVertexExprGet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprGet(comp, g);
		}

		public static string GenerateCodeBCVertexExprOutput(OutputLanguage l, BCVertexExprOutput comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprOutput(comp, g);
		}

		public static string GenerateCodeBCVertexExprPopBinaryMath(OutputLanguage l, BCVertexExprPopBinaryMath comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprPopBinaryMath(comp, g);
		}

		public static string GenerateCodeBCVertexExprPopSet(OutputLanguage l, BCVertexExprPopSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprPopSet(comp, g);
		}

		public static string GenerateCodeBCVertexExprSet(OutputLanguage l, BCVertexExprSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprSet(comp, g);
		}

		public static string GenerateCodeBCVertexExprVarSet(OutputLanguage l, BCVertexExprVarSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexExprVarSet(comp, g);
		}

		public static string GenerateCodeBCVertexGet(OutputLanguage l, BCVertexGet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexGet(comp, g);
		}

		public static string GenerateCodeBCVertexGetVarSet(OutputLanguage l, BCVertexGetVarSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexGetVarSet(comp, g);
		}

		public static string GenerateCodeBCVertexInput(OutputLanguage l, BCVertexInput comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexInput(comp, g);
		}

		public static string GenerateCodeBCVertexInputVarSet(OutputLanguage l, BCVertexInputVarSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexInputVarSet(comp, g);
		}

		public static string GenerateCodeBCVertexNOP(OutputLanguage l, BCVertexNOP comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexNOP(comp, g);
		}

		public static string GenerateCodeBCVertexNot(OutputLanguage l, BCVertexNot comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexNot(comp, g);
		}

		public static string GenerateCodeBCVertexOutput(OutputLanguage l, BCVertexOutput comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexOutput(comp, g);
		}

		public static string GenerateCodeBCVertexPop(OutputLanguage l, BCVertexPop comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexPop(comp, g);
		}

		public static string GenerateCodeBCVertexRandom(OutputLanguage l, BCVertexRandom comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexRandom(comp, g);
		}

		public static string GenerateCodeBCVertexSet(OutputLanguage l, BCVertexSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexSet(comp, g);
		}

		public static string GenerateCodeBCVertexStringOutput(OutputLanguage l, BCVertexStringOutput comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexStringOutput(comp, g);
		}

		public static string GenerateCodeBCVertexSwap(OutputLanguage l, BCVertexSwap comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexSwap(comp, g);
		}

		public static string GenerateCodeBCVertexVarGet(OutputLanguage l, BCVertexVarGet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexVarGet(comp, g);
		}

		public static string GenerateCodeBCVertexVarSet(OutputLanguage l, BCVertexVarSet comp, BCGraph g)
		{
			return Instance(l).GenerateCodeBCVertexVarSet(comp, g);
		}

		public static string GenerateCodeExpressionBCast(OutputLanguage l, ExpressionBCast comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionBCast(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionBinMath(OutputLanguage l, ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionBinMath(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionBinMathDecision(OutputLanguage l, ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionBinMathDecision(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionNotDecision(OutputLanguage l, ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionNotDecision(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionNot(OutputLanguage l, ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionNot(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionPeek(OutputLanguage l, ExpressionPeek comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionPeek(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionVariable(OutputLanguage l, ExpressionVariable comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionVariable(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionConstant(OutputLanguage l, ExpressionConstant comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionConstant(comp, g, forceLongReturn);
		}

		public static string GenerateCodeExpressionGet(OutputLanguage l, ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			return Instance(l).GenerateCodeExpressionGet(comp, g, forceLongReturn);
		}

		#endregion

		#region Abstract generation

		protected abstract string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip);

		protected abstract string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp);

		protected abstract string GenerateCodeBCVertexBlock(BCVertexBlock comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexDecision(BCVertexDecision comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexDup(BCVertexDup comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp, BCGraph g); 

		protected abstract string GenerateCodeBCVertexExpression(BCVertexExpression comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprGet(BCVertexExprGet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprSet(BCVertexExprSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexGet(BCVertexGet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexNOP(BCVertexNOP comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexNot(BCVertexNot comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexRandom(BCVertexRandom comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexVarGet(BCVertexVarGet comp, BCGraph g);

		protected abstract string GenerateCodeBCVertexVarSet(BCVertexVarSet comp, BCGraph g);

		protected abstract string GenerateCodeExpressionBCast(ExpressionBCast comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionNotDecision(ExpressionNot comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionNot(ExpressionNot comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionPeek(ExpressionPeek comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionVariable(ExpressionVariable comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionConstant(ExpressionConstant comp, BCGraph g, bool forceLongReturn);

		protected abstract string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn);

		#endregion
	}
}
