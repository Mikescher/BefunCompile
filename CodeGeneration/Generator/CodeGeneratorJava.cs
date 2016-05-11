using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	public class CodeGeneratorJava : CodeGenerator
	{
		private const OutputLanguage LANG = OutputLanguage.Java;

		protected override string GenerateCode(BCGraph comp, bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			comp.OrderVerticesForFallThrough();

			comp.TestGraph();

			string indent1 = "    ";

			if (!fmtOutput)
				indent1 = "";

			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");
			codebuilder.AppendLine("class Program{");

			if (fmtOutput) codebuilder.AppendLine("");

			if (comp.ListDynamicVariableAccess().Any() || comp.ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccess(comp, implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethods(comp));
			codebuilder.Append(GenerateStackAccess(implementSafeStackAccess));

			if (fmtOutput) codebuilder.AppendLine("");

			if (comp.Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine("long " + string.Join(",", comp.Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in comp.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine("long " + variable.Identifier + "=" + CodeGenerator.GenerateCodeExpressionConstant(LANG, variable.GetInitialConstant(), comp, false) + ";");
			}

			if (fmtOutput) codebuilder.AppendLine("");

			for (int i = 0; i < comp.Vertices.Count; i++)
			{
				if (comp.Vertices[i].IsInput())
					codebuilder.AppendLine("private int _" + i + "()throws java.io.IOException{");
				else
					codebuilder.AppendLine("private int _" + i + "(){");

				codebuilder.AppendLine(Indent(comp.Vertices[i].GenerateCode(LANG, comp), indent1));

				if (comp.Vertices[i].Children.Count == 1)
					codebuilder.AppendLine(indent1 + "return " + comp.Vertices.IndexOf(comp.Vertices[i].Children[0]) + ";");
				else if (comp.Vertices[i].Children.Count == 0)
					codebuilder.AppendLine(indent1 + "return " + comp.Vertices.Count + ";");

				codebuilder.AppendLine("}");

				if (fmtOutput) codebuilder.AppendLine("");
			}

			codebuilder.AppendLine();

			if (comp.IsInput())
				codebuilder.AppendLine("public void main()throws java.io.IOException{");
			else
				codebuilder.AppendLine("public void main(){");
			codebuilder.AppendLine(indent1 + "int c=" + comp.Vertices.IndexOf(comp.Root) + ";");
			codebuilder.AppendLine(indent1 + "while (c<" + comp.Vertices.Count + "){");
			codebuilder.AppendLine(indent1 + "switch(c){");
			for (int i = 0; i < comp.Vertices.Count; i++)
			{
				codebuilder.AppendLine(indent1 + "case " + i + ":c=_" + i + "();break;");
			}
			codebuilder.AppendLine("}");

			if (fmtOutput) codebuilder.AppendLine("");

			if (comp.IsInput())
				codebuilder.AppendLine("}}public static void main(String[]a){try{new Program().main();}catch(java.io.IOException e){}}}");
			else
				codebuilder.AppendLine("}}public static void main(String[]a){new Program().main();}}");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None));
		}

		private string GenerateHelperMethods(BCGraph comp)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();
			
			if (comp.IsInput())
			{
				codebuilder.AppendLine("private java.io.BufferedReader ib=new java.io.BufferedReader(new java.io.InputStreamReader(System.in));");
			}

			if (comp.Vertices.Any(p => p.IsRandom()))
			{
				codebuilder.AppendLine(@"private boolean rd(){return Math.random()<0.5;}");
			}

			codebuilder.AppendLine(@"private long td(long a,long b){return (b==0)?0:(a/b);}");
			codebuilder.AppendLine(@"private long tm(long a,long b){return (b==0)?0:(a%b);}");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess(bool implementSafeStackAccess)
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("private final static java.util.Stack<Long> s=new java.util.Stack<Long>();");

			if (implementSafeStackAccess)
			{
				codebuilder.AppendLine(@"private long sp(){return (s.size()==0)?0:s.pop();}");    //sp = pop
				codebuilder.AppendLine(@"private void sa(long v){ s.push(v); }");                   //sa = push
				codebuilder.AppendLine(@"private long sr(){return (s.size()==0)?0:s.peek();}");   //sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"private long sp(){return s.pop();}");    //sp = pop
				codebuilder.AppendLine(@"private void sa(long v){s.push(v);}");   //sa = push
				codebuilder.AppendLine(@"private long sr(){return s.peek();}");   //sr = peek
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccess(BCGraph comp, bool implementSafeGridAccess, bool useGzip)
		{
			if (useGzip)
				return GenerateGridAccess_GZip(comp, implementSafeGridAccess);
			return GenerateGridAccess_NoGZip(comp, implementSafeGridAccess);
		}

		private string GenerateGridAccess_NoGZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"private final static long[,]g=" + GenerateGridInitializer(comp) + ";");

			if (implementSafeGridAccess)
			{
				string w = comp.Width.ToString();
				string h = comp.Height.ToString();

				codebuilder.AppendLine(@"private long gr(long x,long y){return(x>=0&&y>=0&&x<ggw&&y<ggh)?g[y, x]:0;}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh)g[y, x]=v;}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"private long gr(long x,long y) {return g[y, x];}");
				codebuilder.AppendLine(@"private void gw(long x,long y,long v){g[y, x]=v;}");
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccess_GZip(BCGraph comp, bool implementSafeGridAccess)
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			long datasize = comp.Width * comp.Height;
			string w = comp.Width.ToString();
			string h = comp.Height.ToString();

			var b64 = GZip.GenerateBase64StringList(comp.GenerateGridData());
			for (int i = 0; i < b64.Count; i++)
			{
				if (i == 0 && (i + 1) == b64.Count)
					codebuilder.AppendLine(@"private final static String _g = " + '"' + b64[i] + '"' + ";");
				else if (i == 0)
					codebuilder.AppendLine(@"private final static String _g = " + '"' + b64[i] + '"' + "+");
				else if ((i + 1) == b64.Count)
					codebuilder.AppendLine(@"                                 " + '"' + b64[i] + '"' + ";");
				else
					codebuilder.AppendLine(@"                                 " + '"' + b64[i] + '"' + "+");
			}
			codebuilder.AppendLine(@"private final long[] g=zc(zd(java.util.Base64.getDecoder().decode(_g)));");

			codebuilder.AppendLine(@"private long[]zc(byte[]b){long[]r=new long[" + datasize + @"];for(int i=0;i<" + datasize + @";i++)r[i]=b[i];return r;}");
			codebuilder.AppendLine(@"private byte[]zd(byte[]o){byte[]d=java.util.Arrays.copyOfRange(o,1,o.length);for(int i=0;i<o[0];i++)d=zs(d);return d;}");
			codebuilder.AppendLine(@"private byte[]zs(byte[]o){try{");
			codebuilder.AppendLine(@"java.io.ByteArrayInputStream  y=new java.io.ByteArrayInputStream(o);");
			codebuilder.AppendLine(@"java.util.zip.GZIPInputStream s=new java.util.zip.GZIPInputStream(y);");
			codebuilder.AppendLine(@"java.io.ByteArrayOutputStream a=new java.io.ByteArrayOutputStream();");
			codebuilder.AppendLine(@"int res=0;byte buf[]=new byte[1024];while(res>=0){res=s.read(buf,0,1024);if(res>0)a.write(buf,0,res);}return a.toByteArray();");
			codebuilder.AppendLine(@"}catch(java.io.IOException e){return null;}}");

			if (implementSafeGridAccess)
			{

				codebuilder.AppendLine(@"private long gr(long x,long y){return(x>=0&&y>=0&&x<ggw&&y<ggh)?g[(int)(y*ggw+x)]:0;}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh)g[(int)(y*ggw+x)]=v;}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"private long gr(long x,long y) {return g[(int)(y*ggw+x)];}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private void gw(long x,long y,long v){g[(int)(y*ggw+x)]=v;}".Replace("ggw", w).Replace("ggh", h));
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
					codebuilder.AppendLine("{long v0=sp();sa(sp()-v0);}");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{long v0=sp();sa(td(sp(),v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{long v0=sp();sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.LT:
					codebuilder.AppendLine("{long v0=sp();sa((sp()<v0)?1:0);}");
					break;
				case BinaryMathType.GET:
					codebuilder.AppendLine("{long v0=sp();sa((sp()>=v0)?1:0);}");
					break;
				case BinaryMathType.LET:
					codebuilder.AppendLine("{long v0=sp();sa((sp()<=v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{long v0=sp();sa(tm(sp(),v0));}");
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
			return string.Format("if(sp()!=0)return {0};else return {1};", g.Vertices.IndexOf(comp.EdgeTrue), g.Vertices.IndexOf(comp.EdgeFalse));
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
				return string.Format("if({0})return {1};else return {2};", exprBinMathValue.GenerateDecisionCode(LANG, g, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("if({0})return {1};else return {2};", exprNotValue.GenerateCodeDecision(LANG, g, false), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)return {1};else return {2};", comp.Value.GenerateCode(LANG, g, false), vtrue, vfalse);
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
				return string.Format("System.out.print({0});", GetASCIICharRep(((ExpressionConstant) comp.Value).Value, "'"));

			if (comp.ModeInteger)
				return string.Format("System.out.print(String.valueOf({0}));", comp.Value.GenerateCode(LANG, g, true));

			return string.Format("System.out.print(String.valueOf(({0})({1})));", comp.ModeInteger ? "long" : "char", comp.Value.GenerateCode(LANG, g, false));
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
			return "{long v0=sp();sa(gr(sp(),v0));}";
		}

		protected override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp, BCGraph g)
		{
			return "{long v0=sp();" + comp.Variable.Identifier + "=gr(sp(),v0);}";
		}

		protected override string GenerateCodeBCVertexInput(BCVertexInput comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return "{long v0;while(long.TryParse(ib.readLine(),out v0));sa(v0);}";
			else
				return "sa(ib.readLine());";
		}

		protected override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return string.Format("{{long v0;while(long.TryParse(ib.readLine(),out v0));{0}=v0;}}", comp.Variable.Identifier);
			else
				return string.Format("{0}=ib.read();", comp.Variable.Identifier);
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
			return string.Format("System.out.print(String.valueOf(({0})(sp())));", comp.ModeInteger ? "long" : "char");
		}

		protected override string GenerateCodeBCVertexPop(BCVertexPop comp, BCGraph g)
		{
			return "sp();";
		}

		protected override string GenerateCodeBCVertexRandom(BCVertexRandom comp, BCGraph g)
		{
			return "if(rd()){if(rd()){return g0;}else{return g1;}}else{if(rd()){return g2;}else{return g3;}}"
				.Replace("g0", "" + g.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "" + g.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "" + g.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "" + g.Vertices.IndexOf(comp.Children[3]));
		}

		protected override string GenerateCodeBCVertexSet(BCVertexSet comp, BCGraph g)
		{
			return "{long v0=sp();long v1=sp();gw(v1,v0,sp());}";
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			return string.Format("System.out.print(\"{0}\");", comp.Value);
		}

		protected override string GenerateCodeBCVertexSwap(BCVertexSwap comp, BCGraph g)
		{
			return "{long v0=sp();long v1=sp();sa(v0);sa(v1);}";
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
				return string.Format("({0}!=0)?1L:0L", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?1:0", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
		}

		protected override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, BCGraph g, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			string conditionalSuffix = "?1:0";
			if (forceLongReturn)
				conditionalSuffix = "?1L:0L";

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
				return string.Format("({0}!=0)?0L:1L", Paren(comp.Value.GenerateCode(LANG, g, false), comp.NeedsParen()));
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

			return comp.Value + (forceLongReturn ? "L" : "");
		}

		protected override string GenerateCodeExpressionGet(ExpressionGet comp, BCGraph g, bool forceLongReturn)
		{
			return string.Format("gr({0},{1})", comp.X.GenerateCode(LANG, g, false), comp.Y.GenerateCode(LANG, g, false));
		}
	}
}
