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
		protected abstract string SHEBANG { get; }

		protected abstract IEnumerable<string> AdditionalImports { get; }

		public CodeGeneratorPython(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip) 
			: base(comp, fmtOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip)
		{
			// <EMPTY />
		}

		protected override string GenerateCode()
		{
			Graph.OrderVerticesForFallThrough();

			Graph.TestGraph();

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!FormatOutput);
			codebuilder.AppendLine(SHEBANG);
			codebuilder.AppendLine();
			codebuilder.AppendLine(@"# graphiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015");

			if (Graph.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"from random import randint");

			AdditionalImports.ToList().ForEach(codebuilder.AppendLine);

			if (Graph.ListDynamicVariableAccess().Any() || Graph.ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccess());
			codebuilder.Append(GenerateHelperMethods());
			codebuilder.Append(GenerateStackAccess());

			foreach (var variable in Graph.Variables.Where(p => p.isUserDefinied))
				codebuilder.AppendLine(variable.Identifier + "=" + variable.initial);


			for (int i = 0; i < Graph.Vertices.Count; i++)
			{
				codebuilder.AppendLine("def _" + i + "():");
				foreach (var variable in Graph.Vertices[i].GetVariables().Distinct())
					codebuilder.AppendLine("    global " + variable.Identifier);

				codebuilder.AppendLine(Indent(Graph.Vertices[i].GenerateCode(this), "    "));

				if (Graph.Vertices[i].Children.Count == 1)
					codebuilder.AppendLine("    return " + Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) + "");
				else if (Graph.Vertices[i].Children.Count == 0)
					codebuilder.AppendLine("    return " + Graph.Vertices.Count);
			}

			codebuilder.AppendLine("m=[" + string.Join(",", Enumerable.Range(0, Graph.Vertices.Count).Select(p => "_" + p)) + "]");
			codebuilder.AppendLine("c=" + Graph.Vertices.IndexOf(Graph.Root));
			codebuilder.AppendLine("while c<" + Graph.Vertices.Count + ":");
			codebuilder.AppendLine("    c=m[c]()");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("s=[]");

			if (ImplementSafeStackAccess)
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

		private string GenerateHelperMethods()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (Graph.Vertices.Any(p => p.IsRandom()))
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

		private string GenerateGridAccess()
		{
			if (UseGZip)
				return GenerateGridAccess_GZip();
			return GenerateGridAccess_NoGZip();
		}

		private string GenerateGridAccess_NoGZip()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = Graph.Width.ToString();
			string h = Graph.Height.ToString();

			codebuilder.AppendLine(@"g=" + GenerateGridInitializer() + ";");

			if (ImplementSafeGridAccess)
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

		private string GenerateGridAccess_GZip()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = Graph.Width.ToString();
			string h = Graph.Height.ToString();

			codebuilder.AppendLine(GetBase64DecodeHeader());

			var b64 = GZip.GenerateBase64StringList(Graph.GenerateGridData());
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

			if (ImplementSafeGridAccess)
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

		private string GenerateGridInitializer()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.Append('[');
			for (int y = 0; y < Graph.Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('[');
				for (int x = 0; x < Graph.Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append(Graph.SourceGrid[x, y]);
				}
				codebuilder.Append(']');
			}
			codebuilder.Append(']');

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexBinaryMath(BCVertexBinaryMath comp)
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

		public override string GenerateCodeBCVertexBlock(BCVertexBlock comp)
		{
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(this) + Environment.NewLine));
		}

		public override string GenerateCodeBCVertexDecision(BCVertexDecision comp)
		{
			return string.Format("return ({0})if(sp()!=0)else({1})", Graph.Vertices.IndexOf(comp.EdgeTrue), Graph.Vertices.IndexOf(comp.EdgeFalse));
		}

		public override string GenerateCodeBCVertexDecisionBlock(BCVertexDecisionBlock comp)
		{
			return comp.Block.GenerateCode(this) + Environment.NewLine + comp.Decision.GenerateCode(this);
		}

		public override string GenerateCodeBCVertexDup(BCVertexDup comp)
		{
			return "sa(sr());";
		}

		public override string GenerateCodeBCVertexExprDecision(BCVertexExprDecision comp)
		{
			int vtrue = Graph.Vertices.IndexOf(comp.EdgeTrue);
			int vfalse = Graph.Vertices.IndexOf(comp.EdgeFalse);

			var exprBinMathValue = comp.Value as ExpressionBinMath;
			var exprNotValue = comp.Value as ExpressionNot;

			if (exprBinMathValue != null)
				return string.Format("return ({1})if({0})else({2})", exprBinMathValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("return ({1})if({0})else({2})", exprNotValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else
				return string.Format("return ({1})if(({0})!=0)else({2})", comp.Value.GenerateCode(this, false), vtrue, vfalse);
		}

		public override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp)
		{
			return comp.Block.GenerateCode(this) + Environment.NewLine + comp.Decision.GenerateCode(this);
		}

		public override string GenerateCodeBCVertexExpression(BCVertexExpression comp)
		{
			return string.Format("sa({0})", comp.Expression.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp)
		{
			return string.Format("sa(gr({0},{1}))", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("sa(td(sp()," + comp.SecondExpression.GenerateCode(this, false) + "))");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((1)if(sp()>" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("sa((1)if(sp()<" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("sa((1)if(sp()>=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("sa((1)if(sp()<=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")else(0))");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(tm(sp()," + comp.SecondExpression.GenerateCode(this, false) + "))");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp)
		{
			return string.Format("gw({0},{1},sp())", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp)
		{
			return string.Format("gw({0},{1},{2})", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false), comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprVarSet(BCVertexExprVarSet comp)
		{
			return string.Format("{0}={1}", comp.Variable.Identifier, comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexGet(BCVertexGet comp)
		{
			return "v0=sp()" + Environment.NewLine + "sa(gr(sp(),v0))";
		}

		public override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp)
		{
			return "v0=sp()" + Environment.NewLine + comp.Variable.Identifier + "=gr(sp(),v0)";
		}

		public override string GenerateCodeBCVertexInput(BCVertexInput comp)
		{
			if (comp.ModeInteger)
				return "sa(int(input(\"\")))";
			else
				return "sa(ord(input(\"\")[0]))";
		}

		public override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp)
		{
			if (comp.ModeInteger)
				return string.Format("{0}=int(input(\"\"))", comp.Variable.Identifier);
			else
				return string.Format("{0}=ord(input(\"\")[0])", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexNOP(BCVertexNOP comp)
		{
			return "";
		}

		public override string GenerateCodeBCVertexNot(BCVertexNot comp)
		{
			return "sa((0)if(sp()!=0)else(1))";
		}

		public override string GenerateCodeBCVertexPop(BCVertexPop comp)
		{
			return "sp();";
		}

		public override string GenerateCodeBCVertexRandom(BCVertexRandom comp)
		{
			return "return (((g0)if(rd())else(g1))if(rd())else((g2)if(rd())else(g3)))"
				.Replace("g0", "" + Graph.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "" + Graph.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "" + Graph.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "" + Graph.Vertices.IndexOf(comp.Children[3]));
		}

		public override string GenerateCodeBCVertexSet(BCVertexSet comp)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "gw(v1,v0,sp())";
		}

		public override string GenerateCodeBCVertexSwap(BCVertexSwap comp)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "sa(v0)" + Environment.NewLine + "sa(v1)";
		}

		public override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp)
		{
			return string.Format("sa({0})", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp)
		{
			return string.Format("{0}=sp()", comp.Variable.Identifier);
		}

		public override string GenerateCodeExpressionBCast(ExpressionBCast comp, bool forceLongReturn)
		{
			return string.Format("(1)if({0}!=0)else(0)", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn)
		{
			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")";
				case BinaryMathType.GT:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LT:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.GET:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LET:
					return "(1)if(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")else(0)";
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")";
				default:
					throw new ArgumentException();
			}
		}

		public override string GenerateCodeExpressionBinMathDecision(ExpressionBinMath comp, bool forceLongReturn)
		{
			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return "(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")!=0";
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen());
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")!=0";
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
			return string.Format("(0)if({0}!=0)else(1)", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionPeek(ExpressionPeek comp, bool forceLongReturn)
		{
			return "sr()";
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
			return string.Format("gr({0},{1})", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}
	}
}
