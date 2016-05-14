using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorTextFunge : CodeGenerator
	{
		private const OutputLanguage LANG = OutputLanguage.TextFunge;

		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			comp.OrderVerticesForFallThrough();

			comp.TestGraph();

			List<int> activeJumps = comp.GetAllJumps().Distinct().ToList();

			string indent1 = "    ";
			string indent2 = "    " + "    ";

			if (!fmtOutput)
			{
				indent1 = "";
				indent2 = "";
			}

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!fmtOutput);
			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");
			if (fmtOutput) codebuilder.AppendLine();

			codebuilder.AppendLine(@"///<DISPLAY>");
			foreach (var line in Regex.Split(comp.GenerateGridData(), @"\r?\n"))
			{
				codebuilder.AppendLine(@"///" + line);
			}
			codebuilder.AppendLine(@"///</DISPLAY>");

			codebuilder.AppendLine(string.Format("program Program : display[{0}, {1}]", comp.Width, comp.Height));

			if (comp.Variables.Any())
			{
				codebuilder.AppendLine(indent1 + @"global");
				foreach (var variable in comp.Variables)
				{
					codebuilder.AppendLine(indent2 + "int " + variable.Identifier + ";");
				}
			}
			codebuilder.AppendLine(indent1 + "begin");
			foreach (var variable in comp.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(indent2 + variable.Identifier + "=" + variable.initial + ";");
			}

			if (comp.Vertices.IndexOf(comp.Root) != 0)
				codebuilder.AppendLine(indent2 + "goto _" + comp.Vertices.IndexOf(comp.Root) + ";");

			for (int i = 0; i < comp.Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine(indent1 + "_" + i + ":");

				codebuilder.AppendLine(Indent(comp.Vertices[i].GenerateCode(LANG, comp), indent2));

				if (comp.Vertices[i].Children.Count == 1)
				{
					if (comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(indent2 + "goto _" + comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) + ";");
				}
				else if (comp.Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(indent2 + "stop;");
				}
			}

			codebuilder.AppendLine(indent1 + "end");
			codebuilder.AppendLine("end");

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp)
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
