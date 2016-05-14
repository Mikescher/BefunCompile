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
		private const OutputLanguage LANG = OutputLanguage.C;

		private const int CODEGEN_C_INITIALSTACKSIZE = 16384;

		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			comp.OrderVerticesForFallThrough();

			comp.TestGraph();

			List<int> activeJumps = comp.GetAllJumps().Distinct().ToList();

			string indent1 = "    ";

			if (!fmtOutput)
				indent1 = "";

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!fmtOutput);
			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");

			if (comp.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine("#include <time.h>");

			codebuilder.AppendLine("#include <stdio.h>");
			codebuilder.AppendLine("#include <stdlib.h>");
			codebuilder.AppendLine("#define int64 long long");

			if (comp.ListDynamicVariableAccess().Any() || comp.ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccess(comp, implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethods(comp));
			codebuilder.Append(GenerateStackAccess(implementSafeStackAccess));

			codebuilder.AppendLine("int main(void)");
			codebuilder.AppendLine("{");

			if (comp.Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + string.Join(",", comp.Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in comp.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + variable.Identifier + "=" + variable.initial + ";");
			}

			if (comp.ListDynamicVariableAccess().Any() && useGZip)
				codebuilder.AppendLine(indent1 + "d();");

			if (comp.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(indent1 + "srand(time(NULL));");

			codebuilder.AppendLine(indent1 + "s=(int64*)calloc(q,sizeof(int64));");

			if (comp.Vertices.IndexOf(comp.Root) != 0)
				codebuilder.AppendLine(indent1 + "goto _" + comp.Vertices.IndexOf(comp.Root) + ";");

			for (int i = 0; i < comp.Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine("_" + i + ":");

				codebuilder.AppendLine(Indent(comp.Vertices[i].GenerateCode(LANG, comp), indent1));

				if (comp.Vertices[i].Children.Count == 1)
				{
					if (comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(indent1 + "goto _" + comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) + ";");
				}
				else if (comp.Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(indent1 + "return 0;");
				}
			}

			codebuilder.AppendLine("}");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess(bool implementSafeStackAccess)
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(string.Format("int64*s;int q={0};int y=0;", CODEGEN_C_INITIALSTACKSIZE));

			if (implementSafeStackAccess)
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

		private string GenerateHelperMethods(BCGraph comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			if (comp.Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"int rd(){return rand()%2==0;}");

			codebuilder.AppendLine(@"int64 td(int64 a,int64 b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"int64 tm(int64 a,int64 b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateGridAccess(BCGraph comp, bool implementSafeGridAccess, bool useGZip)
		{
			if (useGZip)
				return GenerateGridAccess_GZip(comp, implementSafeGridAccess);
			return GenerateGridAccess_NoGZip(comp, implementSafeGridAccess);
		}

		private string GenerateGridAccess_GZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = comp.Width.ToString();
			string h = comp.Height.ToString();

			int mszLen;
			var msz = MSZip.GenerateAnsiCEscapedStringList(comp.GenerateGridData(), out mszLen);

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
			codebuilder.AppendLine(@"int64 g[" + (comp.Width * comp.Height) + "];");
			codebuilder.AppendLine(@"int d(){int s,w,i,j,h;h=z;for(;t<" + mszLen + ";t++)if(_g[t]==';')g[z++]=_g[++t];" +
									"else if(_g[t]=='}')return z-h;else if(_g[t]=='{'){t++;s=z;w=d();" +
									"for(i=1;i<_g[t+1]*9025+_g[t+2]*95+_g[t+3]-291872;i++)for(j=0;j<w;g[z++]=g[s+j++]);t+=3;}" +
									"else g[z++]=_g[t];return z-h;}");


			if (implementSafeGridAccess)
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

		private string GenerateGridAccess_NoGZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			string w = comp.Width.ToString();
			string h = comp.Height.ToString();

			codebuilder.AppendLine(@"int64 g[" + h + "][" + w + "]=" + GenerateGridInitializer(comp) + ";");

			if (implementSafeGridAccess)
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

		private string GenerateGridInitializer(BCGraph comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.Append('{');
			for (int y = 0; y < comp.Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('{');
				for (int x = 0; x < comp.Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append(comp.SourceGrid[x, y]);
				}
				codebuilder.Append('}');
			}
			codebuilder.Append('}');

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

		protected override string GenerateCodeBCVertexBlock(BCVertexBlock comp, BCGraph g)
		{
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(LANG, g) + Environment.NewLine));
		}

		protected override string GenerateCodeBCVertexDecision(BCVertexDecision comp, BCGraph g)
		{
			return string.Format("if(sp()!=0)goto _{0};else goto _{1};", g.Vertices.IndexOf(comp.EdgeTrue), g.Vertices.IndexOf(comp.EdgeFalse));
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
				return string.Format("if({0})goto _{1};else goto _{2};", exprBinMathValue.GenerateCodeDecision(LANG, g, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", exprNotValue.GenerateCodeDecision(LANG, g, false), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)goto _{1};else goto _{2};", comp.Value.GenerateCode(LANG, g, false), vtrue, vfalse);
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
			return string.Format("sa(gr({0},{1}));", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp, BCGraph g)
		{
			if (!comp.ModeInteger && comp.Value is ExpressionConstant && IsASCIIChar(((ExpressionConstant) comp.Value).Value))
				return string.Format("printf({0});", GetASCIICharRep(((ExpressionConstant) comp.Value).Value, "\""));

			if (comp.ModeInteger)
				return string.Format("printf(\"{0}\", {1});", "%lld", comp.Value.GenerateCode(LANG, g, true));

			return string.Format("printf(\"{0}\", ({1})({2}));", comp.ModeInteger ? "%lld" : "%c", comp.ModeInteger ? "int64" : "char", comp.Value.GenerateCode(LANG, g, false));
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
					codebuilder.AppendLine("sa(td(sp()," + comp.SecondExpression.GenerateCode(LANG, g, false) + "));");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((sp()>" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("sa((sp()<" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("sa((sp()>=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("sa((sp()<=" + Paren(comp.SecondExpression.GenerateCode(LANG, g, false), comp.NeedsParen()) + ")?1:0);");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(tm(sp()," + comp.SecondExpression.GenerateCode(LANG, g, false) + "));");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexExprPopSet(BCVertexExprPopSet comp, BCGraph g)
		{
			return string.Format("gw({0},{1},sp());", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexExprSet(BCVertexExprSet comp, BCGraph g)
		{
			return string.Format("gw({0},{1},{2});", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false), comp.Value.GenerateCode(LANG, g, false));
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
			return "{int64 v0=sp();sa(gr(sp(),v0));}";
		}

		protected override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g)
		{
			return "{int64 v0=sp();" + comp.Variable.Identifier + "=gr(sp(),v0);}";
		}

		protected override string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return "{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);sa(v1);}";
			else
				return "sa(getchar());";
		}

		protected override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return string.Format("{{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);{0}=v1;}}", comp.Variable.Identifier);
			else
				return string.Format("{0}=getchar();", comp.Variable.Identifier);
		}

		protected override string GenerateCodeBCVertexNOP(BCVertexNOP comp, BCGraph g)
		{
			return "";
		}

		protected override string GenerateCodeBCVertexNot(BCVertexNot comp, BCGraph g)
		{
			return "sa((sp()!=0)?0:1);";
		}

		protected override string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g)
		{
			return string.Format("printf(\"{0}\", ({1})(sp()));",
				comp.ModeInteger ? "%lld" : "%c",
				comp.ModeInteger ? "int64" : "char");
		}

		protected override string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g)
		{
			return "sp();";
		}

		protected override string GenerateCodeBCVertexRandom(BCVertexRandom comp, BCGraph g)
		{
			return "if(rd()){if(rd()){goto g0;}else{goto g1;}}else{if(rd()){goto g2;}else{goto g3;}}"
				.Replace("g0", "_" + g.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "_" + g.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "_" + g.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "_" + g.Vertices.IndexOf(comp.Children[3]));
		}

		protected override string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();gw(v1,v0,sp());}";
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			return string.Format("printf(\"{0}\");", comp.Value);
		}

		protected override string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();sa(v0);sa(v1);}";
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
			if (forceLongReturn)
				return string.Format("({0}!=0)?1LL:0LL", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?1:0", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			string conditionalSuffix = "?1:0";
			if (forceLongReturn)
				conditionalSuffix = "?1LL:0LL";

			switch (comp.Type)
			{
				case BinaryMathType.ADD:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '-' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")";
				case BinaryMathType.GT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LT:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.GET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + ">=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.LET:
					return "" + Paren(comp.ValueA.GenerateCode(LANG, g, false), comp.NeedsLSParen()) + "<=" + Paren(comp.ValueB.GenerateCode(LANG, g, false), comp.NeedsRSParen()) + conditionalSuffix;
				case BinaryMathType.MOD:
					return "tm(" + comp.ValueA.GenerateCode(LANG, g, false) + "," + comp.ValueB.GenerateCode(LANG, g, false) + ")";
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
					return "(" + Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '+' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + "!=" + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(comp.ValueA.GenerateCode(LANG, g, forceL), comp.NeedsLSParen()) + '*' + Paren(comp.ValueB.GenerateCode(LANG, g, forceR), comp.NeedsRSParen()) + ")!=0";
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
			if (forceLongReturn)
				return string.Format("({0}!=0)?0LL:1LL", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?0:1", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
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
			if (comp.Value >= Int32.MaxValue)
				forceLongReturn = true;

			return comp.Value + (forceLongReturn ? "LL" : "");
		}

		protected override string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("gr({0},{1})", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}
	}
}
