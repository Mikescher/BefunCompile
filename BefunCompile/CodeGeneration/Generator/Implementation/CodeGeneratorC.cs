using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	public class CodeGeneratorC : CodeGenerator
	{
		private const int CODEGEN_C_INITIALSTACKSIZE = 16384;

		public CodeGeneratorC(BCGraph comp, CodeGeneratorOptions options) 
			: base(comp, options)
		{
			// <EMPTY />
		}

		protected override string GenerateCode()
		{
			Graph.OrderVerticesForFallThrough();

			Graph.TestGraph();

			List<int> activeJumps = Graph.GetAllJumps().Distinct().ToList();

			string indent1 = "    ";

			if (!FormatOutput)
				indent1 = "";

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!FormatOutput);
			codebuilder.AppendLine(@"/* transpiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");

			if (Graph.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine("#include <time.h>");

			codebuilder.AppendLine("#include <stdio.h>");
			codebuilder.AppendLine("#include <stdlib.h>");
			codebuilder.AppendLine("#define int64 long long");

			if (Graph.ListDynamicVariableAccess().Any() || Graph.ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccess());
			codebuilder.Append(GenerateHelperMethods());
			codebuilder.Append(GenerateStackAccess());

			codebuilder.AppendLine("int main(void)");
			codebuilder.AppendLine("{");

			if (Graph.Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + string.Join(",", Graph.Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in Graph.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + variable.Identifier + "=" + variable.initial + ";");
			}

			if (Graph.ListDynamicVariableAccess().Any() && UseGZip)
				codebuilder.AppendLine(indent1 + "d();");

			if (Graph.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(indent1 + "srand(time(NULL));");

			codebuilder.AppendLine(indent1 + "s=(int64*)calloc(q,sizeof(int64));");

			if (Graph.Vertices.IndexOf(Graph.Root) != 0)
				codebuilder.AppendLine(indent1 + "goto _" + Graph.Vertices.IndexOf(Graph.Root) + ";");

			for (int i = 0; i < Graph.Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine("_" + i + ":");

				codebuilder.AppendLine(Indent(Graph.Vertices[i].GenerateCode(this), indent1));

				if (Graph.Vertices[i].Children.Count == 1)
				{
					if (Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(indent1 + "goto _" + Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) + ";");
				}
				else if (Graph.Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(indent1 + "return 0;");
				}
			}

			codebuilder.AppendLine("}");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("int64*s;int q={0};int y=0;", CODEGEN_C_INITIALSTACKSIZE);

			if (ImplementSafeStackAccess)
			{
				codebuilder.AppendLine(@"int64 sp(){if(!y)return 0;return s[--y];}");                                       //sp = pop
				codebuilder.AppendLine(@"void sa(int64 v){if(q-y<8)s=(int64*)realloc(s,(q*=2)*sizeof(int64));s[y++]=v;}");  //sa = push
				codebuilder.AppendLine(@"int64 sr(){if(!y)return 0;return s[y-1];}");                                       //sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"int64 sp(){return s[--y];}");                                                      //sp = pop
				codebuilder.AppendLine(@"void sa(int64 v){if(q-y<8)s=(int64*)realloc(s,(q*=2)*sizeof(int64));s[y++]=v;}");  //sa = push
				codebuilder.AppendLine(@"int64 sr(){return s[y-1];}");                                                      //sr = peek
			}

			return codebuilder.ToString();
		}

		private string GenerateHelperMethods()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (Graph.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"int rd(){return rand()%2==0;}");

			codebuilder.AppendLine(@"int64 td(int64 a,int64 b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"int64 tm(int64 a,int64 b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateGridAccess()
		{
			if (UseGZip)
				return GenerateGridAccess_GZip();
			return GenerateGridAccess_NoGZip();
		}

		private string GenerateGridAccess_GZip()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = Graph.Width.ToString();
			string h = Graph.Height.ToString();

			int mszLen;
			var msz = MSZip.GenerateAnsiCEscapedStringList(Graph.GenerateGridData(), out mszLen);

			for (int i = 0; i < msz.Count; i++)
			{
				if (i == 0 && (i + 1) == msz.Count)
					codebuilder.AppendLine(@"char* _g= = " + '"' + msz[i] + '"' + ";");
				else if (i == 0)
					codebuilder.AppendLine(@"char* _g = " + '"' + msz[i] + '"' + "");
				else if ((i + 1) == msz.Count)
					codebuilder.AppendLine(@"           " + '"' + msz[i] + '"' + ";");
				else
					codebuilder.AppendLine(@"           " + '"' + msz[i] + '"' + "");
			}
			codebuilder.AppendLine(@"int t=0;int z=0;");
			codebuilder.AppendLine(@"int64 g[" + (Graph.Width * Graph.Height) + "];");
			codebuilder.AppendLine(@"int d(){int s,w,i,j,h;h=z;for(;t<" + mszLen + ";t++)if(_g[t]==';')g[z++]=_g[++t];" +
									"else if(_g[t]=='}')return z-h;else if(_g[t]=='{'){t++;s=z;w=d();" +
									"for(i=1;i<_g[t+1]*9025+_g[t+2]*95+_g[t+3]-291872;i++)for(j=0;j<w;g[z++]=g[s+j++]);t+=3;}" +
									"else g[z++]=_g[t];return z-h;}");


			if (ImplementSafeGridAccess)
			{

				codebuilder.AppendLine(@"int64 gr(int64 x,int64 y){if(x>=0&&y>=0&&x<ggw&&y<ggh){return g[y*ggw+x];}else{return 0;}}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"void gw(int64 x,int64 y,int64 v){if(x>=0&&y>=0&&x<ggw&&y<ggh){g[y*ggw+x]=v;}}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"int64 gr(int64 x,int64 y){return g[y*ggw+x];}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"void gw(int64 x,int64 y,int64 v){g[y*ggw+x]=v;}".Replace("ggw", w).Replace("ggh", h));
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccess_NoGZip()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = Graph.Width.ToString();
			string h = Graph.Height.ToString();

			codebuilder.AppendLine(@"int64 g[" + h + "][" + w + "]=" + GenerateGridInitializer() + ";");

			if (ImplementSafeGridAccess)
			{

				codebuilder.AppendLine(@"int64 gr(int64 x,int64 y){if(x>=0&&y>=0&&x<ggw&&y<ggh){return g[y][x];}else{return 0;}}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"void gw(int64 x,int64 y,int64 v){if(x>=0&&y>=0&&x<ggw&&y<ggh){g[y][x]=v;}}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"int64 gr(int64 x,int64 y){return g[y][x];}");
				codebuilder.AppendLine(@"void gw(int64 x,int64 y,int64 v){g[y][x]=v;}");
			}

			return codebuilder.ToString();
		}

		private string GenerateGridInitializer()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.Append('{');
			for (int y = 0; y < Graph.Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('{');
				for (int x = 0; x < Graph.Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append(Graph.SourceGrid[x, y]);
				}
				codebuilder.Append('}');
			}
			codebuilder.Append('}');

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
					codebuilder.AppendLine("{int64 v0=sp();sa(sp()-v0);}");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{int64 v0=sp();sa(td(sp(),v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{int64 v0=sp();sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("{int64 v0=sp();sa((sp()<v0)?1:0);}");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("{int64 v0=sp();sa((sp()>=v0)?1:0);}");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("{int64 v0=sp();sa((sp()<=v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{int64 v0=sp();sa(tm(sp(),v0));}");
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
			return string.Format("if(sp()!=0)goto _{0};else goto _{1};", Graph.Vertices.IndexOf(comp.EdgeTrue), Graph.Vertices.IndexOf(comp.EdgeFalse));
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
				return string.Format("if({0})goto _{1};else goto _{2};", exprBinMathValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", exprNotValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)goto _{1};else goto _{2};", comp.Value.GenerateCode(this, false), vtrue, vfalse);
		}

		public override string GenerateCodeBCVertexExprDecisionBlock(BCVertexExprDecisionBlock comp)
		{
			return comp.Block.GenerateCode(this) + Environment.NewLine + comp.Decision.GenerateCode(this);
		}

		public override string GenerateCodeBCVertexExpression(BCVertexExpression comp)
		{
			return string.Format("sa({0});", comp.Expression.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprGet(BCVertexExprGet comp)
		{
			return string.Format("sa(gr({0},{1}));", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp)
		{
			if (!comp.ModeInteger && comp.Value is ExpressionConstant && IsASCIIChar(((ExpressionConstant) comp.Value).Value))
				return string.Format("printf({0});", GetASCIICharRep(((ExpressionConstant) comp.Value).Value, "\""));

			if (comp.ModeInteger)
				return string.Format("printf(\"{0}\", {1});", "%lld", comp.Value.GenerateCode(this, true));

			return string.Format("printf(\"{0}\", ({1})({2}));", comp.ModeInteger ? "%lld" : "%c", comp.ModeInteger ? "int64" : "char", comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprPopBinaryMath(BCVertexExprPopBinaryMath comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			switch (comp.MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(comp.SecondExpression.GenerateCode(this, true), comp.NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("sa(td(sp()," + comp.SecondExpression.GenerateCode(this, false) + "));");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((sp()>" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("sa((sp()<" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("sa((sp()>=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("sa((sp()<=" + Paren(comp.SecondExpression.GenerateCode(this, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(tm(sp()," + comp.SecondExpression.GenerateCode(this, false) + "));");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp)
		{
			return string.Format("gw({0},{1},sp());", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp)
		{
			return string.Format("gw({0},{1},{2});", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false), comp.Value.GenerateCode(this, false));
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
			return "{int64 v0=sp();sa(gr(sp(),v0));}";
		}

		public override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp)
		{
			return "{int64 v0=sp();" + comp.Variable.Identifier + "=gr(sp(),v0);}";
		}

		public override string GenerateCodeBCVertexInput(BCVertexInput comp)
		{
			if (comp.ModeInteger)
				return "{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);sa(v1);}";
			else
				return "sa(getchar());";
		}

		public override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp)
		{
			if (comp.ModeInteger)
				return string.Format("{{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);{0}=v1;}}", comp.Variable.Identifier);
			else
				return string.Format("{0}=getchar();", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexNOP(BCVertexNOP comp)
		{
			return "";
		}

		public override string GenerateCodeBCVertexNot(BCVertexNot comp)
		{
			return "sa((sp()!=0)?0:1);";
		}

		public override string GenerateCodeBCVertexOutput(BCVertexOutput comp)
		{
			return string.Format("printf(\"{0}\", ({1})(sp()));",
				comp.ModeInteger ? "%lld" : "%c",
				comp.ModeInteger ? "int64" : "char");
		}

		public override string GenerateCodeBCVertexPop(BCVertexPop comp)
		{
			return "sp();";
		}

		public override string GenerateCodeBCVertexRandom(BCVertexRandom comp)
		{
			return "if(rd()){if(rd()){goto g0;}else{goto g1;}}else{if(rd()){goto g2;}else{goto g3;}}"
				.Replace("g0", "_" + Graph.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "_" + Graph.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "_" + Graph.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "_" + Graph.Vertices.IndexOf(comp.Children[3]));
		}

		public override string GenerateCodeBCVertexSet(BCVertexSet comp)
		{
			return "{int64 v0=sp();int64 v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			return string.Format("printf(\"{0}\");", comp.Value);
		}

		public override string GenerateCodeBCVertexSwap(BCVertexSwap comp)
		{
			return "{int64 v0=sp();int64 v1=sp();sa(v0);sa(v1);}";
		}

		public override string GenerateCodeBCVertexVarGet(BCVertexVarGet comp)
		{
			return string.Format("sa({0});", comp.Variable.Identifier);
		}

		public override string GenerateCodeBCVertexVarSet(BCVertexVarSet comp)
		{
			return string.Format("{0}=sp();", comp.Variable.Identifier);
		}

		public override string GenerateCodeExpressionBCast(ExpressionBCast comp, bool forceLongReturn)
		{
			if (forceLongReturn)
				return string.Format("({0}!=0)?1LL:0LL", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?1:0", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			string conditionalSuffix = "?1:0";
			if (forceLongReturn)
				conditionalSuffix = "?1LL:0LL";

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")";
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(this, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(this, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(this, false) + "," + comp.ValueB.GenerateCode(this, false) + ")";
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
					return "(" + Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(comp.ValueA.GenerateCode(this, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(this, forceR), comp.NeedsRSParen()) + ")!=0";
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
			if (forceLongReturn)
				return string.Format("({0}!=0)?0LL:1LL", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?0:1", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
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
			if (comp.Value >= Int32.MaxValue)
				forceLongReturn = true;

			return comp.Value + (forceLongReturn ? "LL" : "");
		}

		public override string GenerateCodeExpressionGet(ExpressionGet comp, bool forceLongReturn)
		{
			return string.Format("gr({0},{1})", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}
	}
}
