using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
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
			foreach (var line in Regex.Split(comp.GenerateGridData("\n"), @"\n"))
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
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(LANG, g) + Environment.NewLine));
		}

		protected override string GenerateCodeBCVertexDecision(BCVertexDecision comp, BCGraph g)
		{
			throw new NotImplementedException();
		}

		protected override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp, BCGraph g)
		{
			return comp.Block.GenerateCode(LANG, g) + Environment.NewLine + comp.Decision.GenerateCode(LANG, g);
		}

		protected override string GenerateCodeBCVertexDup(BCVertexDup comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp, BCGraph g)
		{
			int vtrue = g.Vertices.IndexOf(comp.EdgeTrue);
			int vfalse = g.Vertices.IndexOf(comp.EdgeFalse);

			var exprBinMathValue = comp.Value as ExpressionBinMath;
			var exprNotValue = comp.Value as ExpressionNot;

			var builder = new SourceCodeBuilder();

			if (exprBinMathValue != null)
				builder.AppendLine("if({0})then", exprBinMathValue.GenerateCodeDecision(LANG, g, false));
			else if (exprNotValue != null)
				builder.AppendLine("if({0})then", exprNotValue.GenerateCodeDecision(LANG, g, false));
			else
				builder.AppendLine("if({0})then", comp.Value.GenerateCode(LANG, g, false));

			builder.AppendLine("goto _{0};", vtrue);
			builder.AppendLine("else");
			builder.AppendLine("goto _{0};", vfalse);
			builder.AppendLine("end");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp, BCGraph g)
		{
			return comp.Block.GenerateCode(LANG, g) + Environment.NewLine + comp.Decision.GenerateCode(LANG, g);
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
			if (!comp.ModeInteger && comp.Value is ExpressionConstant && IsASCIIChar(((ExpressionConstant)comp.Value).Value))
				return string.Format("out {0};", GetASCIICharRep(((ExpressionConstant)comp.Value).Value, "'"));

			if (comp.ModeInteger)
				return string.Format("out {0};", comp.Value.GenerateCode(LANG, g, true));

			return string.Format("out ({0})({1});", comp.ModeInteger ? "int" : "char", comp.Value.GenerateCode(LANG, g, false));
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
			return string.Format("display[{0},{1}] = (char)({2});", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false), comp.Value.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp, BCGraph g)
		{
			var exprMathValue = comp.Value as ExpressionBinMath;
			if (exprMathValue != null && exprMathValue.ValueA == comp.Variable)
			{
				var exprSecondConst = exprMathValue.ValueB as ExpressionConstant;

				if (exprSecondConst != null && exprSecondConst.Value == 1 && exprMathValue.Type == BinaryMathType.ADD)
					return string.Format("{0}++;", comp.Variable.Identifier);

				if (exprSecondConst != null && exprSecondConst.Value == 1 && exprMathValue.Type == BinaryMathType.SUB)
					return string.Format("{0}--;", comp.Variable.Identifier);

				switch (exprMathValue.Type)
				{
					case BinaryMathType.ADD:
						return string.Format("{0}+={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(LANG, g, false));
					case BinaryMathType.SUB:
						return string.Format("{0}-={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(LANG, g, false));
					case BinaryMathType.MUL:
						return string.Format("{0}*={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(LANG, g, false));
					case BinaryMathType.DIV:
						return string.Format("{0}/={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(LANG, g, false));
					case BinaryMathType.MOD:
						return string.Format("{0}%={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(LANG, g, false));
				}
			}

			return string.Format("{0}={1};", comp.Variable.Identifier, comp.Value.GenerateCode(LANG, g, false));
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
			return string.Format("in {0};", comp.Variable.Identifier);
		}

		protected override string GenerateCodeBCVertexNOP(BCVertexNOP comp, BCGraph g)
		{
			return string.Empty;
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
			var builder = new SourceCodeBuilder();

			builder.AppendLine("switch (rand[1])");
			builder.AppendLine("begin");
			builder.AppendLine("case 1:");
			builder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.Children[0]));
			builder.AppendLine("end");
			builder.AppendLine("case 2:");
			builder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.Children[1]));
			builder.AppendLine("end");
			builder.AppendLine("case 3:");
			builder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.Children[2]));
			builder.AppendLine("end");
			builder.AppendLine("case 4:");
			builder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.Children[3]));
			builder.AppendLine("end");
			builder.AppendLine("end");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			return string.Format("out \"{0}\";", comp.Value);
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
			return string.Format("({0})?1:0", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			string conditionalSuffix = "?1:0";

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "/" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.MOD:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "%" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				default:
					throw new ArgumentException();
			}
		}

		protected override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '/' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.MOD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '%' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				default:
					throw new ArgumentException();
			}
		}

		protected override string GenerateCodeExpressionNotDecision(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("!({0})", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionNot(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("!({0})", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionPeek(ExpressionPeek comp, BCGraph g, bool forceLongReturn)
		{
			throw new System.NotImplementedException();
		}

		protected override string GenerateCodeExpressionVariable(ExpressionVariable comp, BCGraph g, bool forceLongReturn)
		{
			return comp.Identifier;
		}

		protected override string GenerateCodeExpressionConstant(ExpressionConstant comp, BCGraph g, bool forceLongReturn)
		{
			return "" + comp.Value;
		}

		protected override string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("display[{0},{1}]", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}
	}
}
