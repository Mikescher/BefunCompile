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

		private bool implSafeGridAccess;
		private bool debugOutput;

		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			implSafeGridAccess = implementSafeGridAccess;
			debugOutput = fmtOutput;

			bool useRealGrid = comp.ListDynamicVariableAccess().Any() || comp.ListConstantVariableAccess().Any();
			bool useStack    = comp.Vertices.Any(p => !p.IsNotStackAccess());

			int stackSize = comp.PredictStackSize() ?? 16384; //TODO Better fallback - only predict when useStack
			if (stackSize == 0) useStack = false;

			int dispWidth = (int)comp.Width;
			int dispHeight = (int)comp.Height;
			int stackOffset = 0;

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

			if (useRealGrid || useStack)
			{
				codebuilder.AppendLine(@"///<DISPLAY>");

				if (useRealGrid)
				{
					stackOffset = dispWidth * dispHeight;

					foreach (var line in Regex.Split(comp.GenerateGridData("\n"), @"\n"))
					{
						codebuilder.AppendLine(@"///" + line);
					}
				}
				else
				{
					dispWidth = 0;
					dispHeight = 0;
				}

				if (useStack)
				{
					if (!useRealGrid && dispWidth < 80) dispWidth = 80;
					if (!useRealGrid && stackSize < dispWidth) dispWidth = stackSize;

					if (useRealGrid)
					{
						dispHeight++;
						stackOffset += dispWidth;
						codebuilder.AppendLine(@"///" + new string('#', dispWidth));
					}

					dispHeight += 1 + (stackSize / dispWidth);

					for (int i = 0; i <= stackSize / dispWidth; i++)
					{
						int w = System.Math.Min(dispWidth, stackSize - i * dispWidth);
						codebuilder.AppendLine(@"///" + new string('0', w));
					}
				}

				codebuilder.AppendLine(@"///</DISPLAY>");

				codebuilder.AppendLine("program Befunge : display[{0}, {1}]", dispWidth, dispHeight);
			}
			else
			{
				codebuilder.AppendLine("program Befunge");
			}

			
			codebuilder.AppendLine(indent1 + @"global");
			codebuilder.AppendLine(indent2 + "int tmp;");
			codebuilder.AppendLine(indent2 + "int tmp2;");
			codebuilder.AppendLine(indent2 + "char tmpc;");

			if (useStack)
			{
				codebuilder.AppendLine(indent2 + "int si;");
			}

			foreach (var variable in comp.Variables)
			{
				codebuilder.AppendLine(indent2 + "int " + variable.Identifier + ";");
			}

			codebuilder.AppendLine(indent1 + "begin");
			
			if (useStack)
			{
				codebuilder.AppendLine(indent2 + "si = -1;");
			}

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

			codebuilder.AppendLine();

			if (comp.Vertices.Any(p => !p.IsNotStackAccess()))
				codebuilder.Append(GenerateStackAccess(implementSafeStackAccess, stackOffset, dispWidth));

			codebuilder.AppendLine();

			if (useRealGrid && implementSafeGridAccess)
				codebuilder.Append(GenerateSafeGridAccess(comp.Width, comp.Height));

			codebuilder.AppendLine("end");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess(bool implementSafeStackAccess, int stackOffset, int displayWidth)
		{
			var codebuilder = new SourceCodeBuilder();

			if (implementSafeStackAccess)
			{
				if (debugOutput)
				{
					codebuilder.AppendLine(@"int sp()");
					codebuilder.AppendLine(@"begin");
					codebuilder.AppendLine(@"if(si<0)then");
					codebuilder.AppendLine(@"return 0;");
					codebuilder.AppendLine(@"end");
					codebuilder.AppendLine(@"si--;");
					codebuilder.AppendLine(@"return (int)display[(si+1+{0})%{1},(si+1+{0})/{1}];", stackOffset, displayWidth);
					codebuilder.AppendLine(@"end");
				}
				else
				{
					codebuilder.AppendLine(@"int sp()");
					codebuilder.AppendLine(@"begin");
					codebuilder.AppendLine(@"if(si<0)then");
					codebuilder.AppendLine(@"return 0;");
					codebuilder.AppendLine(@"end");
					codebuilder.AppendLine(@"si--;");
					codebuilder.AppendLine(@"tmp = (int)display[(si+1+{0})%{1},(si+1+{0})/{1}];", stackOffset, displayWidth);
					codebuilder.AppendLine(@"display[(si+1+{0})%{1},(si+1+{0})/{1}] = ' ';", stackOffset, displayWidth);
					codebuilder.AppendLine(@"return tmp;");
					codebuilder.AppendLine(@"end");
				}

				codebuilder.AppendLine();

				codebuilder.AppendLine(@"void sa(int v)");
				codebuilder.AppendLine(@"begin");
				codebuilder.AppendLine(@"si++;");
				codebuilder.AppendLine(@"display[(si+{0})%{1},(si+{0})/{1}] = (char)v;", stackOffset, displayWidth);
				codebuilder.AppendLine(@"end");
			
				codebuilder.AppendLine();

				codebuilder.AppendLine(@"int sr()");
				codebuilder.AppendLine(@"begin");
				codebuilder.AppendLine(@"if(si<0)then");
				codebuilder.AppendLine(@"return 0;");
				codebuilder.AppendLine(@"end");
				codebuilder.AppendLine(@"return (int)display[(si+{0})%{1},(si+{0})/{1}];", stackOffset, displayWidth);
				codebuilder.AppendLine(@"end");
			}
			else
			{
				if (debugOutput)
				{
					codebuilder.AppendLine(@"int sp()");
					codebuilder.AppendLine(@"begin");
					codebuilder.AppendLine(@"si--;");
					codebuilder.AppendLine(@"tmp = (int)display[(si+1+{0})%{1},(si+1+{0})/{1}];", stackOffset, displayWidth);
					codebuilder.AppendLine(@"display[(si+1+{0})%{1},(si+1+{0})/{1}] = ' ';", stackOffset, displayWidth);
					codebuilder.AppendLine(@"return tmp;");
					codebuilder.AppendLine(@"end");
				}
				else
				{
					codebuilder.AppendLine(@"int sp()");
					codebuilder.AppendLine(@"begin");
					codebuilder.AppendLine(@"si--;");
					codebuilder.AppendLine(@"return (int)display[(si+1+{0})%{1},(si+1+{0})/{1}];", stackOffset, displayWidth);
					codebuilder.AppendLine(@"end");
				}

				codebuilder.AppendLine();

				codebuilder.AppendLine(@"void sa(int v)");
				codebuilder.AppendLine(@"begin");
				codebuilder.AppendLine(@"si++;");
				codebuilder.AppendLine(@"display[(si+{0})%{1},(si+{0})/{1}] = (char)v;", stackOffset, displayWidth);
				codebuilder.AppendLine(@"end");

				codebuilder.AppendLine();

				codebuilder.AppendLine(@"int sr()");
				codebuilder.AppendLine(@"begin");
				codebuilder.AppendLine(@"return (int)display[(si+{0})%{1},(si+{0})/{1}];", stackOffset, displayWidth);
				codebuilder.AppendLine(@"end");
			}

			return codebuilder.ToString();
		}

		private string GenerateSafeGridAccess(long gridWidth, long gridHeight)
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"int gr(int x, int y)");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(@"if (x<0&&y<0&&x>={0}&&y>={1})then", gridWidth, gridHeight);
			codebuilder.AppendLine(@"return 0;");
			codebuilder.AppendLine(@"end");
			codebuilder.AppendLine(@"return (int)display[x, y];");
			codebuilder.AppendLine(@"end");

			codebuilder.AppendLine();

			codebuilder.AppendLine(@"void gw(int x, int y, int v)");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(@"if (x<0&&y<0&&x>={0}&&y>={1})then", gridWidth, gridHeight);
			codebuilder.AppendLine(@"return;");
			codebuilder.AppendLine(@"end");
			codebuilder.AppendLine(@"display[x, y] = (char)v;");
			codebuilder.AppendLine(@"end");

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp, BCGraph g)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+sp());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa(sp()-tmp);");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa(sp()/tmp);");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa((int)(sp()>tmp));");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa((int)(sp()<tmp));");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa((int)(sp()>=tmp));");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa((int)(sp()<=tmp));");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("tmp=sp();");
					codebuilder.AppendLine("sa(sp()%tmp);");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexBlock(BCVertexBlock comp, BCGraph g)
		{
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(LANG, g) + Environment.NewLine));
		}

		protected override string GenerateCodeBCVertexDecision(BCVertexDecision comp, BCGraph g)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("if(sp()!=0)then");
			codebuilder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.EdgeTrue));
			codebuilder.AppendLine("else");
			codebuilder.AppendLine("goto _{0};", g.Vertices.IndexOf(comp.EdgeFalse));
			codebuilder.AppendLine("end");

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp, BCGraph g)
		{
			return comp.Block.GenerateCode(LANG, g) + Environment.NewLine + comp.Decision.GenerateCode(LANG, g);
		}

		protected override string GenerateCodeBCVertexDup(BCVertexDup comp, BCGraph g)
		{
			return "sa(sr());";
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
				builder.AppendLine("if(({0})!=0)then", comp.Value.GenerateCode(LANG, g, false));

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
			return string.Format("sa({0});", comp.Expression.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp, BCGraph g)
		{
			if(implSafeGridAccess)
				return string.Format("sa(gr({0},{1}));", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
			else
				return string.Format("sa(display[{0},{1}]);", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
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
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+" + Paren(comp.SecondExpression.GenerateCode(LANG, g, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(comp.SecondExpression.GenerateCode(LANG, g, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(comp.SecondExpression.GenerateCode(LANG, g, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("sa(sp()/" + comp.SecondExpression.GenerateCode(LANG, g, false) + ");");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((int)(sp()>" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("sa((int)(sp()<" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("sa((int)(sp()>=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("sa((int)(sp()<=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(sp()%" + comp.SecondExpression.GenerateCode(LANG, g, false) + ");");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp, BCGraph g)
		{
			if (implSafeGridAccess)
				return string.Format("gw({0},{1},sp());", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
			else
				return string.Format("display[{0},{1}] = sp();", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));

		}

		protected override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp, BCGraph g)
		{
			if (implSafeGridAccess)
				return string.Format("gw({0},{1},{2});", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false), comp.Value.GenerateCode(LANG, g, false));
			else
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
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("tmp=sp();");
			codebuilder.AppendLine("sa(gr(sp(),tmp));");

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("tmp=sp();");
			codebuilder.AppendLine("{0}=gr(sp(),v0);", comp.Variable.Identifier);

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (comp.ModeInteger)
			{
				codebuilder.AppendLine("in tmpi;");
				codebuilder.AppendLine("sa(tmpi)");
			}
			else
			{
				codebuilder.AppendLine("in tmpc;");
				codebuilder.AppendLine("sa((int)tmpc)");
			}

			return codebuilder.ToString();
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
			return "sa((int)(sp()!=0));";
		}

		protected override string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g)
		{
			return string.Format("out ({0})sp();", comp.ModeInteger ? "int" : "char");
		}

		protected override string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g)
		{
			return "sp();";
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
			var builder = new SourceCodeBuilder();

			builder.AppendLine("tmp=sp();");
			builder.AppendLine("tmp2=sp();");
			builder.AppendLine("gw(tmp2,tmp,sp());");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			return string.Format("out \"{0}\";", comp.Value);
		}

		protected override string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine("tmp=sp();");
			builder.AppendLine("tmp2=sp();");
			builder.AppendLine("sa(tmp);");
			builder.AppendLine("sa(tmp2);");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp, BCGraph g)
		{
			return string.Format("sa({0});", comp.Variable.Identifier);
		}

		protected override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp, BCGraph g)
		{
			return string.Format("{0}=sp();", comp.Variable.Identifier);
		}

		protected override string GenerateCodeExpressionBCast(ExpressionBCast comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("(bool)({0})", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), true);
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "/" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.GT:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.LT:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.GET:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.LET:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.MOD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "%" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
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
					return "(" + Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '%' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen()) + ")!=0";
				default:
					throw new ArgumentException();
			}
		}

		protected override string GenerateCodeExpressionNotDecision(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("{0}==0", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionNot(ExpressionNot comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("(int)(({0})==0)", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionPeek(ExpressionPeek comp, BCGraph g, bool forceLongReturn)
		{
			return "sr()";
		}

		protected override string GenerateCodeExpressionVariable(ExpressionVariable comp, BCGraph g, bool forceLongReturn)
		{
			return comp.Identifier;
		}

		protected override string GenerateCodeExpressionConstant(ExpressionConstant comp, BCGraph g, bool forceLongReturn)
		{
			return comp.Value.ToString();
		}

		protected override string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("display[{0},{1}]", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}
	}
}
