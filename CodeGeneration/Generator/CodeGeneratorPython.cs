using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	public abstract class CodeGeneratorPython : CodeGenerator
	{
		protected abstract OutputLanguage LANG { get; }

		protected abstract string SHEBANG { get; }

		protected abstract IEnumerable<string> AdditionalImports { get; }

		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			comp.OrderVerticesForFallThrough();

			comp.TestGraph();

			SourceCodeBuilder codebuilder = new SourceCodeBuilder();
			codebuilder.AppendLine(SHEBANG);
			codebuilder.AppendLine();
			codebuilder.AppendLine(@"# compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015");

			if (comp.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"from random import randint");

			AdditionalImports.ToList().ForEach(codebuilder.AppendLine);

			if (comp.ListDynamicVariableAccess().Any() || comp.ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccess(comp, implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethods(comp));
			codebuilder.Append(GenerateStackAccess(implementSafeStackAccess));

			foreach (var variable in comp.Variables.Where(p => p.isUserDefinied))
				codebuilder.AppendLine(variable.Identifier + "=" + variable.initial);


			for (int i = 0; i < comp.Vertices.Count; i++)
			{
				codebuilder.AppendLine("def _" + i + "():");
				foreach (var variable in comp.Vertices[i].GetVariables().Distinct())
					codebuilder.AppendLine("    global " + variable.Identifier);

				codebuilder.AppendLine(Indent(comp.Vertices[i].GenerateCode(LANG, comp), "    "));

				if (comp.Vertices[i].Children.Count == 1)
					codebuilder.AppendLine("    return " + comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) + "");
				else if (comp.Vertices[i].Children.Count == 0)
					codebuilder.AppendLine("    return " + comp.Vertices.Count);
			}

			codebuilder.AppendLine("m=[" + string.Join(",", Enumerable.Range(0, comp.Vertices.Count).Select(p => "_" + p)) + "]");
			codebuilder.AppendLine("c=" + comp.Vertices.IndexOf(comp.Root));
			codebuilder.AppendLine("while c<" + comp.Vertices.Count + ":");
			codebuilder.AppendLine("    c=m[c]()");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));
		}

		private string GenerateStackAccess(bool implementSafeStackAccess)
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("s=[]");

			if (implementSafeStackAccess)
			{
				codebuilder.AppendLine(@"def sp():");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    if (len(s) == 0):");
				codebuilder.AppendLine(@"        return 0");
				codebuilder.AppendLine(@"    return s.pop()");
				codebuilder.AppendLine(@"def sa(v):");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    s.append(v)");
				codebuilder.AppendLine(@"def sr():");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    if (len(s) == 0):");
				codebuilder.AppendLine(@"        return 0");
				codebuilder.AppendLine(@"    return s[-1]");
			}
			else
			{
				codebuilder.AppendLine(@"def sp():");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    return s.pop()");
				codebuilder.AppendLine(@"def sa(v):");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    s.append(v)");
				codebuilder.AppendLine(@"def sr():");
				codebuilder.AppendLine(@"    global s");
				codebuilder.AppendLine(@"    return s[-1]");
			}

			return codebuilder.ToString();
		}

		private string GenerateHelperMethods(BCGraph comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (comp.Vertices.Any(p => p.IsRandom()))
			{
				codebuilder.AppendLine(@"def rd():");
				codebuilder.AppendLine(@"    return bool(random.getrandbits(1))");
			}

			codebuilder.AppendLine(@"def td(a,b):");
			codebuilder.AppendLine(@"    return ((0)if(b==0)else(a//b))");

			codebuilder.AppendLine(@"def tm(a,b):");
			codebuilder.AppendLine(@"    return ((0)if(b==0)else(a%b))");

			return codebuilder.ToString();
		}

		private string GenerateGridAccess(BCGraph comp, bool implementSafeGridAccess, bool useGZip)
		{
			if (useGZip)
				return GenerateGridAccess_GZip(comp, implementSafeGridAccess);
			return GenerateGridAccess_NoGZip(comp, implementSafeGridAccess);
		}

		private string GenerateGridAccess_NoGZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = comp.Width.ToString();
			string h = comp.Height.ToString();

			codebuilder.AppendLine(@"g=" + GenerateGridInitializer(comp) + ";");

			if (implementSafeGridAccess)
			{
				codebuilder.AppendLine(@"def gr(x,y):");
				codebuilder.AppendLine(@"    if(x>=0 and y>=0 and x<ggw and y<ggh):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"        return g[y][x];");
				codebuilder.AppendLine(@"    return 0;");
				codebuilder.AppendLine(@"def gw(x,y,v):");
				codebuilder.AppendLine(@"    if(x>=0 and y>=0 and x<ggw and y<ggh):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"        g[y][x]=v;");
			}
			else
			{
				codebuilder.AppendLine(@"def gr(x,y):");
				codebuilder.AppendLine(@"    return g[y][x];");
				codebuilder.AppendLine(@"def gw(x,y,v):");
				codebuilder.AppendLine(@"    g[y][x]=v;");
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccess_GZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = comp.Width.ToString();
			string h = comp.Height.ToString();

			codebuilder.AppendLine(GetBase64DecodeHeader());

			var b64 = GZip.GenerateBase64StringList(comp.GenerateGridData());
			for (int i = 0; i < b64.Count; i++)
			{
				if (i == 0 && (i + 1) == b64.Count)
					codebuilder.AppendLine(@"_g = " + '"' + b64[i] + '"' + ";");
				else if (i == 0)
					codebuilder.AppendLine(@"_g = (" + '"' + b64[i] + '"');
				else if ((i + 1) == b64.Count)
					codebuilder.AppendLine(@"  + " + '"' + b64[i] + '"' + ")");
				else
					codebuilder.AppendLine(@"  + " + '"' + b64[i] + '"');
			}

			codebuilder.AppendLine(GetGZipDecodeStatement());

			if (implementSafeGridAccess)
			{
				codebuilder.AppendLine(@"def gr(x,y):");
				codebuilder.AppendLine(@"    if(x>=0 and y>=0 and x<ggw and y<ggh):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"        return g[y*ggw + x];".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"    return 0;");
				codebuilder.AppendLine(@"def gw(x,y,v):");
				codebuilder.AppendLine(@"    if(x>=0 and y>=0 and x<ggw and y<ggh):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"        g[y*ggw + x]=v;".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"def gr(x,y):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"    return g[y*ggw + x];".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"def gw(x,y,v):".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"    g[y*ggw + x]=v;".Replace("ggw", w).Replace("ggh", h));
			}

			return codebuilder.ToString();
		}

		protected abstract string GetBase64DecodeHeader();
		protected abstract string GetGZipDecodeStatement();

		private string GenerateGridInitializer(BCGraph comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.Append('[');
			for (int y = 0; y < comp.Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('[');
				for (int x = 0; x < comp.Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append(comp.SourceGrid[x, y]);
				}
				codebuilder.Append(']');
			}
			codebuilder.Append(']');

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+sp());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(sp()-v0)");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(td(sp(),v0))");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa((1)if(sp()>v0)else(0))");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa((1)if(sp()<v0)else(0))");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa((1)if(sp()>=v0)else(0))");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa((1)if(sp()<=v0)else(0))");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(tm(sp(),v0))");
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
			return string.Format("return ({0})if(sp()!=0)else({1})", g.Vertices.IndexOf(comp.EdgeTrue), g.Vertices.IndexOf(comp.EdgeFalse));
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

			if (exprBinMathValue != null)
				return string.Format("return ({1})if({0})else({2})", exprBinMathValue.GenerateDecisionCode(LANG, g, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("return ({1})if({0})else({2})", exprNotValue.GenerateCodeDecision(LANG, g, false), vtrue, vfalse);
			else
				return string.Format("return ({1})if(({0})!=0)else({2})", comp.Value.GenerateCode(LANG, g, false), vtrue, vfalse);
		}

		protected override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp, BCGraph g)
		{
			return comp.Block.GenerateCode(LANG, g) + Environment.NewLine + comp.Decision.GenerateCode(LANG, g);
		}

		protected override string GenerateCodeBCVertexExpression(BCVertexExpression comp, BCGraph g)
		{
			return string.Format("sa({0})", comp.Expression.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp, BCGraph g)
		{
			return string.Format("sa(gr({0},{1}))", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp, BCGraph g)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("sa(td(sp()," + comp.SecondExpression.GenerateCode(LANG, g, false) + "))");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((1)if(sp()>" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("sa((1)if(sp()<" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("sa((1)if(sp()>=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("sa((1)if(sp()<=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(tm(sp()," + comp.SecondExpression.GenerateCode(LANG, g, false) + "))");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp, BCGraph g)
		{
			return string.Format("gw({0},{1},sp())", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp, BCGraph g)
		{
			return string.Format("gw({0},{1},{2})", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false), comp.Value.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp, BCGraph g)
		{
			return string.Format("{0}={1}", comp.Variable.Identifier, comp.Value.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexGet(BCVertexGet comp, BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "sa(gr(sp(),v0))";
		}

		protected override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + comp.Variable.Identifier + "=gr(sp(),v0)";
		}

		protected override string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return "sa(int(input(\"\")))";
			else
				return "sa(ord(input(\"\")[0]))";
		}

		protected override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return string.Format("{0}=int(input(\"\"))", comp.Variable.Identifier);
			else
				return string.Format("{0}=ord(input(\"\")[0])", comp.Variable.Identifier);
		}

		protected override string GenerateCodeBCVertexNOP(BCVertexNOP comp, BCGraph g)
		{
			return "";
		}

		protected override string GenerateCodeBCVertexNot(BCVertexNot comp, BCGraph g)
		{
			return "sa((0)if(sp()!=0)else(1))";
		}

		protected override string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g)
		{
			return "sp();";
		}

		protected override string GenerateCodeBCVertexRandom(BCVertexRandom comp, BCGraph g)
		{
			return "return (((g0)if(rd())else(g1))if(rd())else((g2)if(rd())else(g3)))"
				.Replace("g0", "" + g.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "" + g.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "" + g.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "" + g.Vertices.IndexOf(comp.Children[3]));
		}

		protected override string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "gw(v1,v0,sp())";
		}

		protected override string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "sa(v0)" + Environment.NewLine + "sa(v1)";
		}

		protected override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp, BCGraph g)
		{
			return string.Format("sa({0})", comp.Variable.Identifier);
		}

		protected override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp, BCGraph g)
		{
			return string.Format("{0}=sp()", comp.Variable.Identifier);
		}

		protected override string GenerateCodeExpressionBCast(ExpressionBCast comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("(1)if({0}!=0)else(0)", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")";
				case BinaryMathType.GT:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LT:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.GET:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LET:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")";
				default:
					throw new ArgumentException();
			}
		}

		protected override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return "(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")!=0";
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen());
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")!=0";
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
			return string.Format("(0)if({0}!=0)else(1)", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
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
			return string.Format("gr({0},{1})", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}
	}
}
