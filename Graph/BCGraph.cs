using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph
{
	public class BCGraph
	{
		public BCVertex root = null;

		public List<BCVertex> vertices = new List<BCVertex>();

		public List<ExpressionVariable> variables = new List<ExpressionVariable>();

		public readonly long[,] SourceGrid;
		public readonly long Width;
		public readonly long Height;

		public BCGraph(long[,] sg, long w, long h)
		{
			this.SourceGrid = sg;
			this.Width = w;
			this.Height = h;
		}

		public BCVertex getVertex(Vec2i pos, BCDirection dir)
		{
			return vertices.FirstOrDefault(p =>
				p.positions.Length == 1 &&
				p.positions[0].X == pos.X &&
				p.positions[0].Y == pos.Y &&
				p.direction == dir);
		}

		public void AfterGen()
		{
			foreach (var v in vertices)
			{
				v.AfterGen();
			}
		}

		public void UpdateParents()
		{
			foreach (var v in vertices)
			{
				v.parents.Clear();
			}

			foreach (var v in vertices)
			{
				v.UpdateParents();
			}
		}

		public bool TestGraph()
		{
			foreach (var v in vertices)
			{
				if (!v.TestParents())
					return false;

				if (v is BCVertexRandom && v.children.Count != 4)
					return false;

				if (v is BCVertexDecision && !v.children.Contains((v as BCVertexDecision).edgeTrue))
					return false;

				if (v is BCVertexDecision && !v.children.Contains((v as BCVertexDecision).edgeFalse))
					return false;
			}

			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				travelled.Add(curr);

				foreach (var child in curr.children.Where(p => !travelled.Contains(p)))
					untravelled.Push(child);
			}

			if (travelled.Count != vertices.Count)
				return false;

			if (travelled.Any(p => !vertices.Contains(p)))
				return false;

			if (vertices.Count(p => p.parents.Count == 0) > 1)
				return false;

			return true;
		}

		public List<Vec2l> getAllCodePositions()
		{
			return vertices.SelectMany(p => p.positions).Select(p => new Vec2l(p.X, p.Y)).Distinct().ToList();
		}

		#region O:1 Minimize

		public bool Minimize()
		{
			bool o1 = MinimizeNOP();
			bool o2 = MinimizeNOPSplit();
			bool o3 = MinimizeNOPTail();
			bool o4 = MinimizeNOPDecision();

			return o1 || o2 || o3 || o4;
		}

		public bool MinimizeNOP()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.children.Contains(vertex))
					continue;

				if (vertex.parents.Contains(vertex))
					continue;

				if (vertex.parents.Count == 1 && vertex.children.Count == 1 && vertex.parents[0].children.Count == 1)
				{
					found = true;

					BCVertex prev = vertex.parents[0];
					BCVertex next = vertex.children[0];

					removed.Add(vertex);

					vertex.children.Clear();
					vertex.parents.Clear();

					prev.children.Remove(vertex);
					next.parents.Remove(vertex);

					prev.children.Add(next);
					next.parents.Add(prev);

					next.positions = next.positions.Concat(vertex.positions).Distinct().ToArray();

					if (vertex == root)
						root = next;
				}
			}

			foreach (var rv in removed)
			{
				vertices.Remove(rv);
			}

			return found;
		}

		public bool MinimizeNOPSplit()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.parents.Count > 1 && vertex.children.Count == 1 && vertex.parents.All(p => !(p is BCVertexDecision || p is BCVertexFullDecision)))
				{
					found = true;

					BCVertex[] prev = vertex.parents.ToArray();
					BCVertex next = vertex.children[0];

					removed.Add(vertex);
					vertex.children.Clear();
					vertex.parents.Clear();

					next.parents.Remove(vertex);

					foreach (var pvertex in prev)
					{
						pvertex.children.Remove(vertex);

						pvertex.children.Add(next);
						next.parents.Add(pvertex);
					}

					next.positions = next.positions.Concat(vertex.positions).Distinct().ToArray();

					if (vertex == root)
						root = next;
				}
			}

			foreach (var rv in removed)
			{
				vertices.Remove(rv);
			}

			return found;
		}

		public bool MinimizeNOPTail()
		{
			bool found = false;

			if (root is BCVertexNOP && root.parents.Count == 0 && root.children.Count == 1)
			{
				found = true;

				BCVertex vertex = root;

				vertices.Remove(vertex);

				vertex.children[0].positions = vertex.children[0].positions.Concat(vertex.positions).ToArray();

				vertex.children[0].parents.Clear();
				root = vertex.children[0];
			}

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.parents.Count == 1 && vertex.children.Count == 0 && vertex.parents[0].children.Count == 1 && vertex != root)
				{
					found = true;

					BCVertex prev = vertex.parents[0];

					removed.Add(vertex);
					vertex.children.Clear();
					vertex.parents.Clear();

					prev.positions = prev.positions.Concat(vertex.positions).Distinct().ToArray();

					prev.children.Remove(vertex);
				}
			}

			foreach (var rv in removed)
			{
				vertices.Remove(rv);
			}

			return found;
		}

		public bool MinimizeNOPDecision()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.parents.Count == 1 && vertex.children.Count == 1 && vertex.parents[0] is BCVertexDecision)
				{
					found = true;

					BCVertexDecision prev = vertex.parents[0] as BCVertexDecision;
					BCVertex next = vertex.children[0];

					bool isDTrue = (prev.edgeTrue == vertex);

					removed.Add(vertex);
					vertex.children.Clear();
					vertex.parents.Clear();

					prev.children.Remove(vertex);
					next.parents.Remove(vertex);

					prev.children.Add(next);
					next.parents.Add(prev);

					next.positions = next.positions.Concat(vertex.positions).Distinct().ToArray();

					if (isDTrue)
						prev.edgeTrue = next;
					else
						prev.edgeFalse = next;

					if (vertex == root)
						root = next;
				}
			}

			foreach (var rv in removed)
			{
				vertices.Remove(rv);
			}

			return found;
		}

		#endregion

		#region O:2 Substitute

		public bool Substitute()
		{
			var rule1 = new BCModRule();
			rule1.AddPreq(v => v is BCVertexPush);
			rule1.AddPreq(v => v is BCVertexPush);
			rule1.AddPreq(v => v is BCVertexBinaryMath);
			rule1.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, ExpressionBinMath.Create((l[0] as BCVertexPush).Value, (l[1] as BCVertexPush).Value, (l[2] as BCVertexBinaryMath).mtype)));

			var rule2 = new BCModRule();
			rule2.AddPreq(v => v is BCVertexPush);
			rule2.AddPreq(v => v is BCVertexNot);
			rule2.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, ExpressionNot.Create((l[0] as BCVertexPush).Value)));

			var rule3 = new BCModRule();
			rule3.AddPreq(v => v is BCVertexPush);
			rule3.AddPreq(v => v is BCVertexPop);

			var rule4 = new BCModRule();
			rule4.AddPreq(v => v is BCVertexSwap);
			rule4.AddPreq(v => v is BCVertexSwap);

			var rule5 = new BCModRule();
			rule5.AddPreq(v => v is BCVertexPush);
			rule5.AddPreq(v => v is BCVertexDup);
			rule5.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, (l[0] as BCVertexPush).Value));
			rule5.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, (l[0] as BCVertexPush).Value));

			var rule6 = new BCModRule();
			rule6.AddPreq(v => v is BCVertexPush);
			rule6.AddPreq(v => v is BCVertexPush);
			rule6.AddPreq(v => v is BCVertexSwap);
			rule6.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, (l[1] as BCVertexPush).Value));
			rule6.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, (l[0] as BCVertexPush).Value));

			bool b1 = rule1.Execute(this);
			bool b2 = rule2.Execute(this);
			bool b3 = rule3.Execute(this);
			bool b4 = rule4.Execute(this);
			bool b5 = rule5.Execute(this);
			bool b6 = rule6.Execute(this);

			return b1 || b2 || b3 || b4 || b5 || b6;
		}

		#endregion

		#region O:3 Flatten

		public bool FlattenStack()
		{
			var rule1 = new BCModRule();
			rule1.AddPreq(v => v is BCVertexPush);
			rule1.AddPreq(v => v is BCVertexPush);
			rule1.AddPreq(v => v is BCVertexGet);
			rule1.AddRep((l, p) => new BCVertexFullGet(BCDirection.UNKNOWN, p, (l[0] as BCVertexPush).Value, (l[1] as BCVertexPush).Value));

			var rule2 = new BCModRule();
			rule2.AddPreq(v => v is BCVertexPush);
			rule2.AddPreq(v => v is BCVertexPush);
			rule2.AddPreq(v => v is BCVertexSet);
			rule2.AddRep((l, p) => new BCVertexFullSet(BCDirection.UNKNOWN, p, (l[0] as BCVertexPush).Value, (l[1] as BCVertexPush).Value));

			var rule3 = new BCModRule();
			rule3.AddPreq(v => v is BCVertexPush);
			rule3.AddPreq(v => v is BCVertexFullSet);
			rule3.AddRep((l, p) => new BCVertexTotalSet(BCDirection.UNKNOWN, p, (l[1] as BCVertexFullSet).X, (l[1] as BCVertexFullSet).Y, (l[0] as BCVertexPush).Value));

			var rule4 = new BCModRule();
			rule4.AddPreq(v => v is BCVertexFullGet);
			rule4.AddPreq(v => v is BCVertexDup);
			rule4.AddRep((l, p) => l[0].Duplicate());
			rule4.AddRep((l, p) => { var v = l[0].Duplicate(); v.positions = p; return v; });

			var rule5 = new BCModRule();
			rule5.AddPreq(v => !(v is BCVertexDecision || v is BCVertexFullDecision || v is BCVertexRandom) && v.isOnlyStackManipulation());
			rule5.AddPreq(v => (v is BCVertexTotalSet || v is BCVertexTotalVarSet));
			rule5.AddRep((l, p) => l[1].Duplicate());
			rule5.AddRep((l, p) => l[0].Duplicate());

			var rule6 = new BCModRule();
			rule6.AddPreq(v => v is BCVertexFullGet);
			rule6.AddRep((l, p) => new BCVertexPush(BCDirection.UNKNOWN, p, (l[0] as BCVertexFullGet).ToExpression()));

			var rule7 = new BCModRule();
			rule7.AddPreq(v => v is BCVertexPush);
			rule7.AddPreq(v => v is BCVertexOutput);
			rule7.AddRep((l, p) => new BCVertexFullOutput(BCDirection.UNKNOWN, p, (l[1] as BCVertexOutput).ModeInteger, (l[0] as BCVertexPush).Value));

			bool b0 = Substitute();

			bool b1 = rule1.Execute(this);
			bool b2 = rule2.Execute(this);
			bool b3 = rule3.Execute(this);
			bool b4 = rule4.Execute(this);
			bool b5 = rule5.Execute(this);
			bool b6 = rule6.Execute(this);
			bool b7 = rule7.Execute(this);

			bool b8 = IntegrateDecisions();

			return b0 || b1 || b2 || b3 || b4 || b5 || b6 || b7 || b8;
		}

		private bool IntegrateDecisions()
		{
			var rule = new BCModRule();
			rule.AddPreq(v => v is BCVertexPush);
			rule.AddPreq(v => v is BCVertexDecision);

			var chain = rule.GetMatchingChain(this);

			if (chain == null)
				return false;

			var prev = chain[0].parents.ToList();
			var nextTrue = (chain[1] as BCVertexDecision).edgeTrue;
			var nextFalse = (chain[1] as BCVertexDecision).edgeFalse;

			if (prev.Any(p => p is BCVertexFullDecision))
				return false;

			if (chain[1].parents.Count > 1)
				return false;

			chain[0].children.Clear();
			chain[0].parents.Clear();
			vertices.Remove(chain[0]);

			chain[1].children.Clear();
			chain[1].parents.Clear();
			vertices.Remove(chain[1]);

			var newnode = new BCVertexFullDecision(BCDirection.UNKNOWN, chain.SelectMany(p => p.positions).ToArray(), nextTrue, nextFalse, (chain[0] as BCVertexPush).Value);

			vertices.Add(newnode);

			nextTrue.parents.Remove(chain[1]);
			newnode.children.Add(nextTrue);
			nextTrue.parents.Add(newnode);

			nextFalse.parents.Remove(chain[1]);
			newnode.children.Add(nextFalse);
			nextFalse.parents.Add(newnode);

			foreach (var p in prev)
			{
				p.children.Remove(chain[0]);
				p.children.Add(newnode);
				newnode.parents.Add(p);

				if (p is BCVertexDecision)
				{
					if ((p as BCVertexDecision).edgeTrue == chain[0])
						(p as BCVertexDecision).edgeTrue = newnode;
					if ((p as BCVertexDecision).edgeFalse == chain[0])
						(p as BCVertexDecision).edgeFalse = newnode;
				}
			}

			if (root == chain[0])
				root = newnode;

			return true;
		}

		#endregion

		#region O:4 Variablize

		public IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return vertices.SelectMany(p => p.listConstantVariableAccess());
		}

		public IEnumerable<MemoryAccess> listDynamicVariableAccessCSharp()
		{
			return vertices.SelectMany(p => p.listDynamicVariableAccess());
		}

		public void SubstituteConstMemoryAccess(Func<long, long, long> gridGetter)
		{
			var ios = listConstantVariableAccess().ToList();

			variables = ios
				.Select(p => new Vec2l(p.getX().Calculate(null), p.getY().Calculate(null)))
				.Distinct()
				.Select((p, i) => ExpressionVariable.Create("x" + i, gridGetter(p.X, p.Y), p))
				.ToList();

			Dictionary<Vec2l, ExpressionVariable> vardic = new Dictionary<Vec2l, ExpressionVariable>();
			variables.ForEach(p => vardic.Add(p.position, p));

			foreach (var variable in variables)
			{
				BCModRule vertexRule1 = new BCModRule();
				vertexRule1.AddPreq(p => p is BCVertexFullGet && ios.Contains(p as MemoryAccess));
				vertexRule1.AddRep((l, p) => new BCVertexFullVarGet(BCDirection.UNKNOWN, p, vardic[(l[0] as BCVertexFullGet).getConstantPos()]));

				BCModRule vertexRule2 = new BCModRule();
				vertexRule2.AddPreq(p => p is BCVertexFullSet && ios.Contains(p as MemoryAccess));
				vertexRule2.AddRep((l, p) => new BCVertexFullVarSet(BCDirection.UNKNOWN, p, vardic[(l[0] as BCVertexFullSet).getConstantPos()]));

				BCModRule vertexRule3 = new BCModRule();
				vertexRule3.AddPreq(p => p is BCVertexTotalSet && ios.Contains(p as MemoryAccess));
				vertexRule3.AddRep((l, p) => new BCVertexTotalVarSet(BCDirection.UNKNOWN, p, vardic[(l[0] as BCVertexTotalSet).getConstantPos()], (l[0] as BCVertexTotalSet).Value));

				BCExprModRule exprRule1 = new BCExprModRule();
				exprRule1.setPreq(p => p is ExpressionGet && ios.Contains(p as MemoryAccess));
				exprRule1.setRep(p => vardic[(p as ExpressionGet).getConstantPos()]);

				bool changed = true;

				while (changed)
				{
					bool b1 = vertexRule1.Execute(this);
					bool b2 = vertexRule2.Execute(this);
					bool b3 = vertexRule3.Execute(this);
					bool b4 = exprRule1.Execute(this);

					changed = b1 || b2 || b3 || b4;
				}
			}
		}

		#endregion

		#region O:5 Combination

		public bool CombineBlocks()
		{
			var ruleSubstitute = new BCModRule(false);
			ruleSubstitute.AddPreq(v => v.children.Count <= 1 && !(v is BCVertexBlock));
			ruleSubstitute.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0]));

			var ruleCombine = new BCModRule(false);
			ruleCombine.AddPreq(v => v is BCVertexBlock);
			ruleCombine.AddPreq(v => v is BCVertexBlock);
			ruleCombine.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0] as BCVertexBlock, l[1] as BCVertexBlock));

			bool b1 = ruleSubstitute.Execute(this);
			bool b2 = ruleCombine.Execute(this);

			return b1 || b2;
		}

		#endregion

		#region CodeGeneration

		private string indent(string code, string indent)
		{
			return string.Join(Environment.NewLine, code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Select(p => indent + p));
		}

		#region CodeGeneration (C#)

		public string GenerateCodeCSharp(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess)
		{
			string indent1 = "    ";
			string indent2 = "    " + "    ";

			if (!fmtOutput)
			{
				indent1 = "";
				indent2 = "";
			}

			StringBuilder codebuilder = new StringBuilder();

			if (listDynamicVariableAccessCSharp().Count() > 0)
				codebuilder.Append(GenerateGridAccessCSharp(implementSafeGridAccess));
			codebuilder.Append(GenerateStackAccessCSharp(implementSafeStackAccess));
			codebuilder.Append(GenerateHelperMethodsCSharp());

			codebuilder.AppendLine("static void Main(string[] args)");
			codebuilder.AppendLine("{");

			foreach (var variable in variables)
			{
				codebuilder.AppendLine(indent2 + "long " + variable.Identifier + "=0x" + variable.initial.ToString("X") + ";");
			}

			codebuilder.AppendLine(indent2 + "goto _" + vertices.IndexOf(root) + ";");

			for (int i = 0; i < vertices.Count; i++)
			{
				codebuilder.AppendLine(indent1 + "_" + i + ":");

				codebuilder.AppendLine(indent(vertices[i].GenerateCodeCSharp(this), indent2));

				if (vertices[i].children.Count == 1)
					codebuilder.AppendLine(indent2 + "goto _" + vertices.IndexOf(vertices[i].children[0]) + ";");
				else if (vertices[i].children.Count == 0)
					codebuilder.AppendLine(indent2 + "return;");
			}

			codebuilder.AppendLine("}");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));
		}

		private string GenerateHelperMethodsCSharp()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"private static readonly System.Random r = new System.Random();");
			codebuilder.AppendLine(@"private static bool rd(){ return r.Next(2)!=0; }");

			codebuilder.AppendLine(@"private static long td(long a,long b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"private static long tm(long a,long b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateStackAccessCSharp(bool implementSafeStackAccess)
		{
			var codebuilder = new StringBuilder();

			codebuilder.AppendLine("private static System.Collections.Generic.Stack<long> s=new System.Collections.Generic.Stack<long>();");

			if (implementSafeStackAccess)
			{
				codebuilder.AppendLine(@"private static long sp(){ return (s.Count==0)?0:s.Pop(); }");	  //sp = pop
				codebuilder.AppendLine(@"private static void sa(long v){ s.Push(v); }");				  //sa = push
				codebuilder.AppendLine(@"private static long sr(){ return (s.Count==0)?0:s.Peek(); }");	  //sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"private static long sp(){ return s.Pop(); }");	   //sp = pop
				codebuilder.AppendLine(@"private static void sa(long v){ s.Push(v); }");   //sa = push
				codebuilder.AppendLine(@"private static long sr(){ return s.Peek(); }");   //sr = peek
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccessCSharp(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"private static readonly long[,] g = " + GenerateGridInitializerCSharp() + ";");

			if (implementSafeGridAccess)
			{
				string w = Width.ToString("X");
				string h = Height.ToString("X");

				codebuilder.AppendLine(@"private static long gr(long x,long y){ (x>=0&&y>=0&&x<ggw&&y<ggh)?(return g[y, x]):0; }".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh)g[y, x]=v;}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"private static long gr(long x,long y) {return g[y, x];}");
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){g[y, x]=v;}");
			}

			return codebuilder.ToString();
		}

		private string GenerateGridInitializerCSharp()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.Append('{');
			for (int y = 0; y < Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('{');
				for (int x = 0; x < Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append("0x");
					codebuilder.Append(SourceGrid[x, y].ToString("X"));
				}
				codebuilder.Append('}');
			}
			codebuilder.Append('}');

			return codebuilder.ToString();
		}

		#endregion

		#region CodeGeneration (C)

		public string GenerateCodeC(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess)
		{
			string indent1 = "    ";

			if (!fmtOutput)
				indent1 = "";

			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine("#include <time.h>");
			codebuilder.AppendLine("#include <stdio.h>");
			codebuilder.AppendLine("#include <stdlib.h>");

			if (listDynamicVariableAccessCSharp().Count() > 0)
				codebuilder.Append(GenerateGridAccessC(implementSafeGridAccess));
			codebuilder.Append(GenerateHelperMethodsC());
			codebuilder.Append(GenerateStackAccessC(implementSafeStackAccess));

			codebuilder.AppendLine("int main(void)");
			codebuilder.AppendLine("{");

			foreach (var variable in variables)
			{
				codebuilder.AppendLine(indent1 + "long " + variable.Identifier + "=0x" + variable.initial.ToString("X") + ";");
			}

			codebuilder.AppendLine(indent1 + "srand(time(NULL));");

			codebuilder.AppendLine(indent1 + "goto _" + vertices.IndexOf(root) + ";");

			for (int i = 0; i < vertices.Count; i++)
			{
				codebuilder.AppendLine("_" + i + ":");

				codebuilder.AppendLine(indent(vertices[i].GenerateCodeC(this), indent1));

				if (vertices[i].children.Count == 1)
					codebuilder.AppendLine(indent1 + "goto _" + vertices.IndexOf(vertices[i].children[0]) + ";");
				else if (vertices[i].children.Count == 0)
					codebuilder.AppendLine(indent1 + "goto __;");
			}

			codebuilder.AppendLine("__:");
			codebuilder.AppendLine(indent1 + "return 0;");
			codebuilder.AppendLine("}");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));
		}

		private string GenerateStackAccessC(bool implementSafeStackAccess)
		{
			var codebuilder = new StringBuilder();

			codebuilder.AppendLine("struct k{struct k*h;long v;};");
			codebuilder.AppendLine("struct k*s=NULL;");

			if (implementSafeStackAccess)
			{
				codebuilder.AppendLine(@"long sp(){if(s==NULL)return 0;long r=s->v;s=s->h;return r;}");						   //sp = pop
				codebuilder.AppendLine(@"void sa(long v){struct k*n=(struct k*)malloc(sizeof(struct k));n->v=v;n->h=s;s=n;}"); //sa = push
				codebuilder.AppendLine(@"long sr(){if(s == NULL)return 0;return s->v;}");									   //sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"long sp(){long r=s->v;s=s->h;return r;}");											   //sp = pop
				codebuilder.AppendLine(@"void sa(long v){struct k*n=(struct k*)malloc(sizeof(struct k));n->v=v;n->h=s;s=n;}"); //sa = push
				codebuilder.AppendLine(@"long sr(){return s->v;}");															   //sr = peek
			}

			return codebuilder.ToString();
		}

		private string GenerateHelperMethodsC()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"int random(){return rand()%2==0;}");

			codebuilder.AppendLine(@"long td(long a,long b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"long tm(long a,long b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateGridAccessC(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString("X");
			string h = Height.ToString("X");

			codebuilder.AppendLine(@"long g[0x" + h + "][0x" + w + "]=" + GenerateGridInitializerCSharp() + ";");

			if (implementSafeGridAccess)
			{

				codebuilder.AppendLine(@"long gr(long x,long y){if(x>=0&&y>=0&&x<ggw&&y<ggh){return g[y][x];}else{return 0;}}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh){g[y][x]=v;}}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"long gr(long x,long y){return g[y][x];}");
				codebuilder.AppendLine(@"void gw(long x,long y,long v){g[y][x]=v;}");
			}

			return codebuilder.ToString();
		}

		#endregion

		#region CodeGeneration (Python)

		public string GenerateCodePython(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess)
		{
			throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
