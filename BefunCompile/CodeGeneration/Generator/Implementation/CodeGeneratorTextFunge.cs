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
		private string Indent1 => FormatOutput ? new string(' ', 4) : string.Empty;
		private string Indent2 => FormatOutput ? new string(' ', 8) : string.Empty;

		private string _stackPush = "@@@";
		private string _stackPeek = "@@@";
		private string _stackPop  = "@@@";

		public CodeGeneratorTextFunge(BCGraph comp, CodeGeneratorOptions options) 
			: base(comp, options)
		{
		}

		protected override string GenerateCode()
		{
			bool useRealGrid = Graph.ListDynamicVariableAccess().Any() || Graph.ListConstantVariableAccess().Any();
			bool useStack    = Graph.Vertices.Any(p => !p.IsNotStackAccess());

			int stackSize = GetStackSize();
			if (stackSize == 0) useStack = false;

			int dispWidth = (int)Graph.Width;
			int dispHeight = (int)Graph.Height;

			if (useStack && ImplementSafeStackAccess)
			{
				_stackPush = "gis.Push";
				_stackPeek = "sr";
				_stackPop = "sp";
			}
			else if (useStack)
			{
				_stackPush = "gis.Push";
				_stackPeek = "gis.Peek";
				_stackPop = "gis.Pop";
			}

			Graph.OrderVerticesForFallThrough();

			Graph.TestGraph();

			List<int> activeJumps = Graph.GetAllJumps().Distinct().ToList();

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!FormatOutput);
			codebuilder.AppendLine(@"/* graphiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");
			if (FormatOutput) codebuilder.AppendLine();

			if (useRealGrid)
			{
				codebuilder.AppendLine(@"///<DISPLAY>");
				
				foreach (var line in Regex.Split(Graph.GenerateGridData("\n"), @"\n"))
				{
					codebuilder.AppendLine(@"///" + line);
				}

				codebuilder.AppendLine(@"///</DISPLAY>");

				codebuilder.AppendLine("program Befunge : display[{0}, {1}]", dispWidth, dispHeight);
			}
			else
			{
				codebuilder.AppendLine("program Befunge");
			}

			
			codebuilder.AppendLine(Indent1 + @"global");
			codebuilder.AppendLine(Indent2 + "int tmp;");
			codebuilder.AppendLine(Indent2 + "int tmp2;");
			codebuilder.AppendLine(Indent2 + "char tmpc;");

			if (useStack)
			{
				codebuilder.AppendLine(Indent2 + "stack<int>["+stackSize+"] gis;");
			}

			foreach (var variable in Graph.Variables)
			{
				codebuilder.AppendLine(Indent2 + "int " + variable.Identifier + ";");
			}

			codebuilder.AppendLine(Indent1 + "begin");
			
			foreach (var variable in Graph.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(Indent2 + variable.Identifier + "=" + variable.initial + ";");
			}

			if (Graph.Vertices.IndexOf(Graph.Root) != 0)
				codebuilder.AppendLine(Indent2 + "goto _" + Graph.Vertices.IndexOf(Graph.Root) + ";");

			for (int i = 0; i < Graph.Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine(Indent1 + "_" + i + ":");

				codebuilder.AppendLine(Indent(Graph.Vertices[i].GenerateCode(this), Indent2));

				if (Graph.Vertices[i].Children.Count == 1)
				{
					if (Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(Indent2 + "goto _" + Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) + ";");
				}
				else if (Graph.Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(Indent2 + "stop;");
				}
			}

			codebuilder.AppendLine(Indent1 + "end");

			codebuilder.AppendLine();

			if (useStack && ImplementSafeStackAccess)
				codebuilder.Append(GenerateStackAccess());

			if (useRealGrid && ImplementSafeGridAccess)
				codebuilder.Append(GenerateSafeGridAccess(Graph.Width, Graph.Height));

			codebuilder.AppendLine("end");

			return codebuilder.ToString();
		}

		private int GetStackSize()
		{
			if (Graph.Vertices.All(v => v.IsNotStackAccess())) return 0;

			return Graph.PredictStackSize() ?? 16384;
		}
		
		private string GenerateSafeGridAccess(long gridWidth, long gridHeight)
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"int gr(int x, int y)");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(Indent1 + @"if (x<0&&y<0&&x>={0}&&y>={1})then", gridWidth, gridHeight);
			codebuilder.AppendLine(Indent1 + @"return 0;");
			codebuilder.AppendLine(Indent1 + @"end");
			codebuilder.AppendLine(Indent1 + @"return (int)display[x, y];");
			codebuilder.AppendLine(@"end");

			codebuilder.AppendLine();

			codebuilder.AppendLine(@"void gw(int x, int y, int v)");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(Indent1 + @"if (x<0&&y<0&&x>={0}&&y>={1})then", gridWidth, gridHeight);
			codebuilder.AppendLine(Indent1 + @"return;");
			codebuilder.AppendLine(Indent1 + @"end");
			codebuilder.AppendLine(Indent1 + @"display[x, y] = (char)v;");
			codebuilder.AppendLine(@"end");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"int sp()");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(Indent1 + @"if (gis.Empty()) then return 0; end");
			codebuilder.AppendLine(Indent1 + @"return gis.Pop();");
			codebuilder.AppendLine(@"end");

			codebuilder.AppendLine();

			codebuilder.AppendLine(@"int sr()");
			codebuilder.AppendLine(@"begin");
			codebuilder.AppendLine(Indent1 + @"if (gis.Empty()) then return 0; end");
			codebuilder.AppendLine(Indent1 + @"return gis.Peek();");
			codebuilder.AppendLine(@"end");

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine(_stackPush+"(" + _stackPop + "()+" + _stackPop + "());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()-tmp);");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()*" + _stackPop + "());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()/tmp);");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()>tmp));");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()<tmp));");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()>=tmp));");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()<=tmp));");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("tmp=" + _stackPop + "();");
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()%tmp);");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexBlock(BCVertexBlock comp)
		{
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(this) + Environment.NewLine));
		}

		public override string GenerateCodeBCVertexDecision(BCVertexDecision comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("if(" + _stackPop + "()!=0)then");
			codebuilder.AppendLine(Indent1 + "goto _{0};", Graph.Vertices.IndexOf(comp.EdgeTrue));
			codebuilder.AppendLine("else");
			codebuilder.AppendLine(Indent1 + "goto _{0};", Graph.Vertices.IndexOf(comp.EdgeFalse));
			codebuilder.AppendLine("end");

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp)
		{
			return comp.Block.GenerateCode(this) + Environment.NewLine + comp.Decision.GenerateCode(this);
		}

		public override string GenerateCodeBCVertexDup(BCVertexDup comp)
		{
			return _stackPush + "(" + _stackPeek + "());";
		}

		public override string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp)
		{
			int vtrue = Graph.Vertices.IndexOf(comp.EdgeTrue);
			int vfalse = Graph.Vertices.IndexOf(comp.EdgeFalse);

			var exprBinMathValue = comp.Value as ExpressionBinMath;
			var exprNotValue = comp.Value as ExpressionNot;

			var builder = new SourceCodeBuilder();

			if (exprBinMathValue != null)
				builder.AppendLine("if({0})then", exprBinMathValue.GenerateCodeDecision(this, false));
			else if (exprNotValue != null)
				builder.AppendLine("if({0})then", exprNotValue.GenerateCodeDecision(this, false));
			else
				builder.AppendLine("if(({0})!=0)then", comp.Value.GenerateCode(this, false));

			builder.AppendLine(Indent1 + "goto _{0};", vtrue);
			builder.AppendLine("else");
			builder.AppendLine(Indent1 + "goto _{0};", vfalse);
			builder.AppendLine("end");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp)
		{
			return comp.Block.GenerateCode(this) + Environment.NewLine + comp.Decision.GenerateCode(this);
		}

		public override string GenerateCodeBCVertexExpression(BCVertexExpression comp)
		{
			return string.Format(_stackPush + "({0});", comp.Expression.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp)
		{
			if(ImplementSafeGridAccess)
				return string.Format(_stackPush + "(gr({0},{1}));", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
			else
				return string.Format(_stackPush + "(display[{0},{1}]);", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp)
		{
			if (!comp.ModeInteger && comp.Value is ExpressionConstant && IsASCIIChar(((ExpressionConstant)comp.Value).Value))
				return string.Format("out {0};", GetASCIICharRep(((ExpressionConstant)comp.Value).Value, "'"));

			if (comp.ModeInteger)
				return string.Format("out {0};", comp.Value.GenerateCode(this, true));

			return string.Format("out ({0})({1});", comp.ModeInteger ? "int" : "char", comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()+" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()-" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()*" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()/" + comp.SecondExpression.GenerateCode(this, false) + ");");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()>" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()<" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()>=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine(_stackPush + "((int)(" + _stackPop + "()<=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + "));");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine(_stackPush + "(" + _stackPop + "()%" + comp.SecondExpression.GenerateCode(this, false) + ");");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp)
		{
			if (ImplementSafeGridAccess)
				return string.Format("gw({0},{1}," + _stackPop + "());", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
			else
				return string.Format("display[{0},{1}] = " + _stackPop + "();", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));

		}

		public override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp)
		{
			if (ImplementSafeGridAccess)
				return string.Format("gw({0},{1},{2});", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false), comp.Value.GenerateCode(this, false));
			else
				return string.Format("display[{0},{1}] = (char)({2});", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false), comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp)
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
						return string.Format("{0}+={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(this, false));
					case BinaryMathType.SUB:
						return string.Format("{0}-={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(this, false));
					case BinaryMathType.MUL:
						return string.Format("{0}*={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(this, false));
					case BinaryMathType.DIV:
						return string.Format("{0}/={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(this, false));
					case BinaryMathType.MOD:
						return string.Format("{0}%={1};", comp.Variable.Identifier, exprMathValue.ValueB.GenerateCode(this, false));
				}
			}
			
			return string.Format("{0}={1};", comp.Variable.Identifier, comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexGet(BCVertexGet comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("tmp=" + _stackPop + "();");
			codebuilder.AppendLine(_stackPush + "(gr(" + _stackPop + "(),tmp));");

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("tmp=" + _stackPop + "();");
			codebuilder.AppendLine("{0}=gr(" + _stackPop + "(),v0);", comp.Variable.Identifier);

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexInput(BCVertexInput comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (comp.ModeInteger)
			{
				codebuilder.AppendLine("in tmpi;");
				codebuilder.AppendLine(_stackPush + "(tmpi)");
			}
			else
			{
				codebuilder.AppendLine("in tmpc;");
				codebuilder.AppendLine(_stackPush + "((int)tmpc)");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp)
		{
			return string.Format("in {0};", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexNOP(BCVertexNOP comp)
		{
			return string.Empty;
		}

		public override string GenerateCodeBCVertexNot(BCVertexNot comp)
		{
			return _stackPush + "((int)(" + _stackPop + "()!=0));";
		}

		public override string GenerateCodeBCVertexOutput(BCVertexOutput comp)
		{
			return string.Format("out ({0})" + _stackPop + "();", comp.ModeInteger ? "int" : "char");
		}

		public override string GenerateCodeBCVertexPop(BCVertexPop comp)
		{
			return _stackPop + "();";
		}

		public override string GenerateCodeBCVertexRandom(BCVertexRandom comp)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine("switch (rand[1])");
			builder.AppendLine("begin");
			builder.AppendLine(Indent1 + "case 1:");
			builder.AppendLine(Indent2 + "goto _{0};", Graph.Vertices.IndexOf(comp.Children[0]));
			builder.AppendLine(Indent1 + "end");
			builder.AppendLine(Indent1 + "case 2:");
			builder.AppendLine(Indent2 + "goto _{0};", Graph.Vertices.IndexOf(comp.Children[1]));
			builder.AppendLine(Indent1 + "end");
			builder.AppendLine(Indent1 + "case 3:");
			builder.AppendLine(Indent2 + "goto _{0};", Graph.Vertices.IndexOf(comp.Children[2]));
			builder.AppendLine(Indent1 + "end");
			builder.AppendLine(Indent1 + "case 4:");
			builder.AppendLine(Indent2 + "goto _{0};", Graph.Vertices.IndexOf(comp.Children[3]));
			builder.AppendLine(Indent1 + "end");
			builder.AppendLine("end");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexSet(BCVertexSet comp)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine("tmp="+ _stackPop + "();");
			builder.AppendLine("tmp2=" + _stackPop + "();");
			builder.AppendLine("gw(tmp2,tmp," + _stackPop + "());");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			return string.Format("out \"{0}\";", comp.Value);
		}

		public override string GenerateCodeBCVertexSwap(BCVertexSwap comp)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine("tmp=" + _stackPop + "();");
			builder.AppendLine("tmp2=" + _stackPop + "();");
			builder.AppendLine(_stackPush + "(tmp);");
			builder.AppendLine(_stackPush + "(tmp2);");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp)
		{
			return string.Format(_stackPush + "({0});", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp)
		{
			return string.Format("{0}=" + _stackPop + "();", comp.Variable.Identifier);
		}

		public override string GenerateCodeExpressionBCast(ExpressionBCast comp, bool forceLongReturn)
		{
			return string.Format("(bool)({0})", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(this, forceR), true);
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "/" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.GT:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.LT:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.GET:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.LET:
					return "(int)(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")";
				case BinaryMathType.MOD:
					return Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "%" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				default:
					throw new ArgumentException();
			}
		}

		public override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '/' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.MOD:
					return "(" + Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '%' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen()) + ")!=0";
				default:
					throw new ArgumentException();
			}
		}

		public override string GenerateCodeExpressionNotDecision(ExpressionNot comp, bool forceLongReturn)
		{
			return string.Format("{0}==0", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionNot(ExpressionNot comp, bool forceLongReturn)
		{
			return string.Format("(int)(({0})==0)", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionPeek(ExpressionPeek comp, bool forceLongReturn)
		{
			return _stackPeek + "()";
		}

		public override string GenerateCodeExpressionVariable(ExpressionVariable comp, bool forceLongReturn)
		{
			return comp.Identifier;
		}

		public override string GenerateCodeExpressionConstant(ExpressionConstant comp, bool forceLongReturn)
		{
			return comp.Value.ToString();
		}

		public override string GenerateCodeExpressionGet(ExpressionGet comp, bool forceLongReturn)
		{
			return string.Format("display[{0},{1}]", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}
	}
}
