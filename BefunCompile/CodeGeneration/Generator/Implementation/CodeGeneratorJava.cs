using BefunCompile.Graph;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	public class CodeGeneratorJava : CodeGenerator
	{
		public CodeGeneratorJava(BCGraph comp, CodeGeneratorOptions options) 
			: base(comp, options)
		{
			// <EMPTY />
		}

		protected override string GenerateCode()
		{
			Graph.OrderVerticesForFallThrough();

			Graph.TestGraph();

			string indent1 = "    ";

			if (!FormatOutput)
				indent1 = "";

			SourceCodeBuilder codebuilder = new SourceCodeBuilder(!FormatOutput);

			codebuilder.AppendLine($"/* transpiled with BefunCompile v{BefunCompiler.VERSION} (c) {BefunCompiler.YEAR} */");
			codebuilder.AppendLine("class Program{");

			if (FormatOutput) codebuilder.AppendLine("");

			bool isGrid = Graph.ListDynamicVariableAccess().Any() || Graph.ListConstantVariableAccess().Any();
			bool isStack = Graph.Vertices.Any(v => v.IsStackAccess());

			if (isGrid) codebuilder.Append(GenerateGridAccess());
			codebuilder.Append(GenerateHelperMethods());
			if (isStack) codebuilder.Append(GenerateStackAccess());

			if (FormatOutput) codebuilder.AppendLine("");

			if (Graph.Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine("long " + string.Join(",", Graph.Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in Graph.Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine("long " + variable.Identifier + "=" + GenerateCodeExpressionConstant(variable.GetInitialConstant(), false) + ";");
			}

			if (FormatOutput) codebuilder.AppendLine("");

			for (int i = 0; i < Graph.Vertices.Count; i++)
			{
				if (Graph.Vertices[i].IsInput())
					codebuilder.AppendLine("private int _" + i + "()throws java.io.IOException{");
				else
					codebuilder.AppendLine("private int _" + i + "(){");

				codebuilder.AppendLine(Indent(Graph.Vertices[i].GenerateCode(this), indent1));

				if (Graph.Vertices[i].Children.Count == 1)
					codebuilder.AppendLine(indent1 + "return " + Graph.Vertices.IndexOf(Graph.Vertices[i].Children[0]) + ";");
				else if (Graph.Vertices[i].Children.Count == 0)
					codebuilder.AppendLine(indent1 + "return " + Graph.Vertices.Count + ";");

				codebuilder.AppendLine("}");

				if (FormatOutput) codebuilder.AppendLine("");
			}

			codebuilder.AppendLine();

			if (Graph.IsInput())
				codebuilder.AppendLine("public void main()throws java.io.IOException{");
			else
				codebuilder.AppendLine("public void main(){");
			codebuilder.AppendLine(indent1 + "int c=" + Graph.Vertices.IndexOf(Graph.Root) + ";");
			codebuilder.AppendLine(indent1 + "while(c<" + Graph.Vertices.Count + "){");
			codebuilder.AppendLine(indent1 + "switch(c){");
			for (int i = 0; i < Graph.Vertices.Count; i++)
			{
				codebuilder.AppendLine(indent1 + "case " + i + ":c=_" + i + "();break;");
			}
			codebuilder.AppendLine("}");

			if (FormatOutput) codebuilder.AppendLine("");

			if (Graph.IsInput())
			{
				codebuilder.AppendLine("}");
				codebuilder.AppendLine("}");
				codebuilder.AppendLine("public static void main(String[]a){try{new Program().main();}catch(java.io.IOException e){}}");
				codebuilder.AppendLine("}");

			}
			else
			{
				codebuilder.AppendLine("}");
				codebuilder.AppendLine("}");
				codebuilder.AppendLine("public static void main(String[]a){new Program().main();}");
				codebuilder.AppendLine("}");

			}

			return codebuilder.ToString();
		}

		private string GenerateHelperMethods()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();
			
			if (Graph.IsInput())
			{
				codebuilder.AppendLine("private java.io.BufferedReader ib=new java.io.BufferedReader(new java.io.InputStreamReader(System.in));");
			}

			if (Graph.Vertices.Any(p => p.IsRandom()))
			{
				codebuilder.AppendLine(@"private boolean rd(){return Math.random()<0.5;}");
			}

			codebuilder.AppendLine(@"private long td(long a,long b){return(b==0)?0:(a/b);}");
			codebuilder.AppendLine(@"private long tm(long a,long b){return(b==0)?0:(a%b);}");

			return codebuilder.ToString();
		}

		private string GenerateStackAccess()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine("private final static java.util.Stack<Long> s=new java.util.Stack<Long>();");

			if (ImplementSafeStackAccess)
			{
				codebuilder.AppendLine(@"private long sp(){return(s.size()==0)?0:s.pop();}");    //sp = pop
				codebuilder.AppendLine(@"private void sa(long v){s.push(v);}");                  //sa = push
				codebuilder.AppendLine(@"private long sr(){return(s.size()==0)?0:s.peek();}");   //sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"private long sp(){return s.pop();}");    //sp = pop
				codebuilder.AppendLine(@"private void sa(long v){s.push(v);}");   //sa = push
				codebuilder.AppendLine(@"private long sr(){return s.peek();}");   //sr = peek
			}

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

			codebuilder.AppendLine(@"private final static long[,]g=" + GenerateGridInitializer() + ";");

			if (ImplementSafeGridAccess)
			{
				string w = Graph.Width.ToString();
				string h = Graph.Height.ToString();

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

		private string GenerateGridAccess_GZip()
		{
			SourceCodeBuilder codebuilder = new SourceCodeBuilder();

			long datasize = Graph.Width * Graph.Height;
			string w = Graph.Width.ToString();
			string h = Graph.Height.ToString();

			var b64 = GZip.GenerateBase64StringList(Graph.GenerateGridData());
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

			if (ImplementSafeGridAccess)
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

		public override string GenerateCodeBCVertexBlock(BCVertexBlock comp)
		{
			return string.Join("", comp.nodes.Select(p => p.GenerateCode(this) + Environment.NewLine));
		}

		public override string GenerateCodeBCVertexDecision(BCVertexDecision comp)
		{
			return string.Format("if(sp()!=0)return {0};else return {1};", Graph.Vertices.IndexOf(comp.EdgeTrue), Graph.Vertices.IndexOf(comp.EdgeFalse));
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
				return string.Format("if({0})return {1};else return {2};", exprBinMathValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else if (exprNotValue != null)
				return string.Format("if({0})return {1};else return {2};", exprNotValue.GenerateCodeDecision(this, false), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)return {1};else return {2};", comp.Value.GenerateCode(this, false), vtrue, vfalse);
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
				return string.Format("System.out.print({0});", GetASCIICharRep(((ExpressionConstant) comp.Value).Value, "'"));

			if (comp.ModeInteger)
				return string.Format("System.out.print(String.valueOf({0})+\" \");", comp.Value.GenerateCode(this, true));
			else
				return string.Format("System.out.print(String.valueOf((char)({0})));", comp.Value.GenerateCode(this, false));
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
			return "{long v0=sp();sa(gr(sp(),v0));}";
		}

		public override string GenerateCodeBCVertexGetVarSet(BCVertexGetVarSet comp)
		{
			return "{long v0=sp();" + comp.Variable.Identifier + "=gr(sp(),v0);}";
		}

		public override string GenerateCodeBCVertexInput(BCVertexInput comp)
		{
			if (comp.ModeInteger)
				return "{long v0;while(long.TryParse(ib.readLine(),out v0));sa(v0);}";
			else
				return "sa(ib.readLine());";
		}

		public override string GenerateCodeBCVertexInputVarSet(BCVertexInputVarSet comp)
		{
			if (comp.ModeInteger)
				return string.Format("{{long v0;while(long.TryParse(ib.readLine(),out v0));{0}=v0;}}", comp.Variable.Identifier);
			else
				return string.Format("{0}=ib.read();", comp.Variable.Identifier);
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
			if (comp.ModeInteger)
				return "System.out.print(String.valueOf((long)(sp()))+\" \");";
			else
				return "System.out.print(String.valueOf((char)(sp())));";
		}

		public override string GenerateCodeBCVertexPop(BCVertexPop comp)
		{
			return "sp();";
		}

		public override string GenerateCodeBCVertexRandom(BCVertexRandom comp)
		{
			return "if(rd()){if(rd()){return g0;}else{return g1;}}else{if(rd()){return g2;}else{return g3;}}"
				.Replace("g0", "" + Graph.Vertices.IndexOf(comp.Children[0]))
				.Replace("g1", "" + Graph.Vertices.IndexOf(comp.Children[1]))
				.Replace("g2", "" + Graph.Vertices.IndexOf(comp.Children[2]))
				.Replace("g3", "" + Graph.Vertices.IndexOf(comp.Children[3]));
		}

		public override string GenerateCodeBCVertexSet(BCVertexSet comp)
		{
			return "{long v0=sp();long v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			return string.Format("System.out.print(\"{0}\");", comp.Value);
		}

		public override string GenerateCodeBCVertexSwap(BCVertexSwap comp)
		{
			return "{long v0=sp();long v1=sp();sa(v0);sa(v1);}";
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
				return string.Format("({0}!=0)?1L:0L", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
			else
				return string.Format("({0}!=0)?1:0", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
		}

		public override string GenerateCodeExpressionBinMath(ExpressionBinMath comp, bool forceLongReturn)
		{
			bool forceL = comp.ForceLongReturnLeft();
			bool forceR = comp.ForceLongReturnRight();

			string conditionalSuffix = "?1:0";
			if (forceLongReturn)
				conditionalSuffix = "?1L:0L";

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
				return string.Format("({0}!=0)?0L:1L", Paren(comp.Value.GenerateCode(this, false), comp.NeedsParen()));
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

			return comp.Value + (forceLongReturn ? "L" : "");
		}

		public override string GenerateCodeExpressionGet(ExpressionGet comp, bool forceLongReturn)
		{
			return string.Format("gr({0},{1})", comp.X.GenerateCode(this, false), comp.Y.GenerateCode(this, false));
		}
	}
}
