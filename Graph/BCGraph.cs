using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph
{
	public class BCGraph
	{
		private const int CODEGEN_C_INITIALSTACKSIZE = 16384;

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

		public List<BCVertex> walkGraphDual()
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				travelled.Add(curr);

				foreach (var child in curr.children.Where(p => !travelled.Contains(p)))
					untravelled.Push(child);

				foreach (var parent in curr.parents.Where(p => !travelled.Contains(p)))
					untravelled.Push(parent);
			}

			return travelled.ToList();
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

				if (v is BCVertexFullDecision && !v.children.Contains((v as BCVertexFullDecision).edgeTrue))
					return false;

				if (v is BCVertexFullDecision && !v.children.Contains((v as BCVertexFullDecision).edgeFalse))
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

				if (vertex.parents.Count > 1 && vertex.children.Count == 1 && vertex.parents.All(p => !(p is BCVertexDecision || p is BCVertexFullDecision)) && !vertex.parents.Any(p => p == vertex) && !vertex.children.Any(p => p == vertex))
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

		public IEnumerable<MemoryAccess> listDynamicVariableAccess()
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

			var ruleMinimize = new BCModRule(false);
			ruleMinimize.AddPreq(v => v is BCVertexNOP);

			bool b1 = ruleSubstitute.Execute(this);
			bool b2 = ruleCombine.Execute(this);
			bool b3 = ruleMinimize.Execute(this);
			bool b4 = RemoveNoDecisions();

			return b1 || b2 || b3 || b4;
		}

		private bool RemoveNoDecisions()
		{
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexFullDecision))
					continue;

				if (!((vertex as BCVertexFullDecision).Value is ExpressionConstant))
					continue;

				BCVertexFullDecision decision = vertex as BCVertexFullDecision;

				if (decision.Value.Calculate(null) != 0)
				{
					decision.edgeFalse.parents.Remove(decision);
					decision.children.Remove(decision.edgeFalse);
					decision.edgeFalse = null;
				}
				else
				{
					decision.edgeTrue.parents.Remove(decision);
					decision.children.Remove(decision.edgeTrue);
					decision.edgeTrue = null;
				}

				var remRule = new BCModRule(false);
				remRule.AddPreq(p => p == decision);

				bool exec = remRule.Execute(this);

				if (!exec)
					throw new Exception("errrrrrrr");

				var included = walkGraphDual();
				vertices = vertices.Where(p => included.Contains(p)).ToList();

				var headlessRule = new BCModRule(false);
				headlessRule.AddPreq(p => p.parents.Count == 0 && p != root);

				int hlc = 0;
				while (headlessRule.ArrayExecute(this))
					hlc++;

				return true;
			}

			return false;
		}

		#endregion

		#region CodeGeneration

		private string indent(string code, string indent)
		{
			return string.Join(Environment.NewLine, code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Select(p => indent + p));
		}

		private byte[] CompressData(byte[] data)
		{
			byte compressioncount = 0;
			while (compressioncount < 32)
			{
				byte[] compress = CompressSingleData(data);

				if (compress.Length >= data.Length)
					break;

				data = compress;
				compressioncount++;
			}

			return new byte[] { compressioncount }.Concat(data).ToArray();
		}

		private byte[] CompressSingleData(byte[] raw)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
				{
					gzip.Write(raw, 0, raw.Length);
				}
				return memory.ToArray();
			}
		}

		private string GenerateGridData()
		{
			StringBuilder codebuilder = new StringBuilder();

			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					codebuilder.Append((char)SourceGrid[x, y]);
				}
			}

			return codebuilder.ToString();
		}

		private string GenerateGridBase64DataString()
		{
			return Convert.ToBase64String(CompressData(Encoding.ASCII.GetBytes(GenerateGridData())));
		}

		private List<string> GenerateGridBase64DataStringList()
		{
			string data = GenerateGridBase64DataString();

			return Enumerable.Range(0, data.Length / 128 + 2).Select(i => (i * 128 > data.Length) ? "" : data.Substring(i * 128, System.Math.Min(i * 128 + 128, data.Length) - i * 128)).Where(p => p != "").ToList();
		}

		#region CodeGeneration (C#)

		public string GenerateCodeCSharp(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			TestGraph();

			string indent1 = "    ";
			string indent2 = "    " + "    ";

			if (!fmtOutput)
			{
				indent1 = "";
				indent2 = "";
			}

			StringBuilder codebuilder = new StringBuilder();
			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");
			codebuilder.AppendLine(@"public static class Program ");
			codebuilder.AppendLine("{");

			if (listDynamicVariableAccess().Count() > 0)
				codebuilder.Append(GenerateGridAccessCSharp(implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateStackAccessCSharp(implementSafeStackAccess));
			codebuilder.Append(GenerateHelperMethodsCSharp());

			codebuilder.AppendLine("static void Main(string[] args)");
			codebuilder.AppendLine("{");

			foreach (var variable in variables)
			{
				codebuilder.AppendLine(indent2 + "long " + variable.Identifier + "=" + variable.initial + ";");
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

			codebuilder.AppendLine("}}");

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

		private string GenerateGridAccessCSharp(bool implementSafeGridAccess, bool useGzip)
		{
			if (useGzip)
				return GenerateGridAccessCSharp_GZip(implementSafeGridAccess);
			else
				return GenerateGridAccessCSharp_NoGZip(implementSafeGridAccess);
		}

		private string GenerateGridAccessCSharp_NoGZip(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"private static readonly long[,] g = " + GenerateGridInitializerCSharp() + ";");

			if (implementSafeGridAccess)
			{
				string w = Width.ToString();
				string h = Height.ToString();

				codebuilder.AppendLine(@"private static long gr(long x,long y){return(x>=0&&y>=0&&x<ggw&&y<ggh)?g[y, x]:0;}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh)g[y, x]=v;}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"private static long gr(long x,long y) {return g[y, x];}");
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){g[y, x]=v;}");
			}

			return codebuilder.ToString();
		}

		private string GenerateGridAccessCSharp_GZip(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString();
			string h = Height.ToString();

			var b64 = GenerateGridBase64DataStringList();
			for (int i = 0; i < b64.Count; i++)
			{
				if (i == 0 && (i + 1) == b64.Count)
					codebuilder.AppendLine(@"private static readonly string _g = " + '"' + b64[i] + '"' + ";");
				else if (i == 0)
					codebuilder.AppendLine(@"private static readonly string _g = " + '"' + b64[i] + '"' + "+");
				else if ((i + 1) == b64.Count)
					codebuilder.AppendLine(@"                                    " + '"' + b64[i] + '"' + ";");
				else
					codebuilder.AppendLine(@"                                    " + '"' + b64[i] + '"' + "+");
			}
			codebuilder.AppendLine(@"private static readonly long[]  g = System.Array.ConvertAll(zd(System.Convert.FromBase64String(_g)),b=>(long)b);");

			codebuilder.AppendLine(@"private static byte[]zd(byte[]o){byte[]d=o.Skip(1).ToArray();for(int i=0;i<o[0];i++)d=zs(d);return d;}");
			codebuilder.AppendLine(@"private static byte[]zs(byte[]o){using(var c=new System.IO.MemoryStream(o))");
			codebuilder.AppendLine(@"                                 using(var z=new System.IO.Compression.GZipStream(c,System.IO.Compression.CompressionMode.Decompress))");
			codebuilder.AppendLine(@"                                 using(var r=new System.IO.MemoryStream()){z.CopyTo(r);return r.ToArray();}}");
			if (implementSafeGridAccess)
			{

				codebuilder.AppendLine(@"private static long gr(long x,long y){return(x>=0&&y>=0&&x<ggw&&y<ggh)?g[y*ggw+x]:0;}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){if(x>=0&&y>=0&&x<ggw&&y<ggh)g[y*ggw+x]=v;}".Replace("ggw", w).Replace("ggh", h));
			}
			else
			{
				codebuilder.AppendLine(@"private static long gr(long x,long y) {return g[y*ggw+x];}".Replace("ggw", w).Replace("ggh", h));
				codebuilder.AppendLine(@"private static void gw(long x,long y,long v){g[y*ggw+x]=v;}".Replace("ggw", w).Replace("ggh", h));
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

					codebuilder.Append(SourceGrid[x, y]);
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
			TestGraph();

			string indent1 = "    ";

			if (!fmtOutput)
				indent1 = "";

			StringBuilder codebuilder = new StringBuilder();
			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");

			codebuilder.AppendLine("#include <time.h>");
			codebuilder.AppendLine("#include <stdio.h>");
			codebuilder.AppendLine("#include <stdlib.h>");
			codebuilder.AppendLine("#define int64 long long");

			if (listDynamicVariableAccess().Count() > 0)
				codebuilder.Append(GenerateGridAccessC(implementSafeGridAccess));
			codebuilder.Append(GenerateHelperMethodsC());
			codebuilder.Append(GenerateStackAccessC(implementSafeStackAccess));

			codebuilder.AppendLine("int main(void)");
			codebuilder.AppendLine("{");

			foreach (var variable in variables)
			{
				codebuilder.AppendLine(indent1 + "int64 " + variable.Identifier + "=" + variable.initial + ";");
			}

			codebuilder.AppendLine(indent1 + "srand(time(NULL));");
			codebuilder.AppendLine(indent1 + "s=(int64*)calloc(q,sizeof(int64));");

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

			codebuilder.AppendLine(string.Format("int64*s;int q={0};int y=0;", CODEGEN_C_INITIALSTACKSIZE));

			if (implementSafeStackAccess)
			{
				codebuilder.AppendLine(@"int64 sp(){if(!y)return 0;return s[--y];}");										//sp = pop
				codebuilder.AppendLine(@"void sa(int64 v){if(q-y<8)s=(int64*)realloc(s,(q*=2)*sizeof(int64));s[y++]=v;}");	//sa = push
				codebuilder.AppendLine(@"int64 sr(){if(!y)return 0;return s[y-1];}");										//sr = peek
			}
			else
			{
				codebuilder.AppendLine(@"int64 sp(){return s[--y];}");														//sp = pop
				codebuilder.AppendLine(@"void sa(int64 v){if(q-y<8)s=(int64*)realloc(s,(q*=2)*sizeof(int64));s[y++]=v;}");	//sa = push
				codebuilder.AppendLine(@"int64 sr(){return s[y-1];}");														//sr = peek
			}

			return codebuilder.ToString();
		}

		private string GenerateHelperMethodsC()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"int rd(){return rand()%2==0;}");

			codebuilder.AppendLine(@"int64 td(int64 a,int64 b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"int64 tm(int64 a,int64 b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateGridAccessC(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString();
			string h = Height.ToString();

			codebuilder.AppendLine(@"int64 g[" + h + "][" + w + "]=" + GenerateGridInitializerC() + ";");

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

		private string GenerateGridInitializerC()
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

					codebuilder.Append(SourceGrid[x, y]);
				}
				codebuilder.Append('}');
			}
			codebuilder.Append('}');

			return codebuilder.ToString();
		}

		#endregion

		#region CodeGeneration (Python)

		public string GenerateCodePython(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			TestGraph();

			StringBuilder codebuilder = new StringBuilder();
			codebuilder.AppendLine(@"# compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015");
			codebuilder.AppendLine(@"# execute with at least Python3");
			codebuilder.AppendLine(@"from random import randint");

			if (listDynamicVariableAccess().Count() > 0)
				codebuilder.Append(GenerateGridAccessPython(implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethodsPython());
			codebuilder.Append(GenerateStackAccessPython(implementSafeStackAccess));

			foreach (var variable in variables)
				codebuilder.AppendLine(variable.Identifier + "=" + variable.initial);


			for (int i = 0; i < vertices.Count; i++)
			{
				codebuilder.AppendLine("def _" + i + "():");
				foreach (var variable in variables)
					codebuilder.AppendLine("    global " + variable.Identifier);

				codebuilder.AppendLine(indent(vertices[i].GenerateCodePython(this), "    "));

				if (vertices[i].children.Count == 1)
					codebuilder.AppendLine("    return " + vertices.IndexOf(vertices[i].children[0]) + "");
				else if (vertices[i].children.Count == 0)
					codebuilder.AppendLine("    return " + vertices.Count);
			}

			codebuilder.AppendLine("m=[" + string.Join(",", Enumerable.Range(0, vertices.Count).Select(p => "_" + p)) + "]");
			codebuilder.AppendLine("c=" + vertices.IndexOf(root));
			codebuilder.AppendLine("while c<" + vertices.Count + ":");
			codebuilder.AppendLine("    c=m[c]()");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));

		}

		private string GenerateStackAccessPython(bool implementSafeStackAccess)
		{
			var codebuilder = new StringBuilder();

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

		private string GenerateHelperMethodsPython()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.AppendLine(@"def rd():");
			codebuilder.AppendLine(@"    return bool(random.getrandbits(1))");

			codebuilder.AppendLine(@"def td(a,b):");
			codebuilder.AppendLine(@"    return bool(random.getrandbits(1))");

			codebuilder.AppendLine(@"def td(a,b):");
			codebuilder.AppendLine(@"    return ((0)if(b==0)else(a//b))");

			codebuilder.AppendLine(@"def tm(a,b):");
			codebuilder.AppendLine(@"    return ((0)if(b==0)else(a%b))");

			return codebuilder.ToString();
		}

		private string GenerateGridAccessPython(bool implementSafeGridAccess, bool useGZip)
		{
			if (useGZip)
				return GenerateGridAccessPython_GZip(implementSafeGridAccess);
			else
				return GenerateGridAccessPython_NoGZip(implementSafeGridAccess);
		}

		private string GenerateGridAccessPython_NoGZip(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString();
			string h = Height.ToString();

			codebuilder.AppendLine(@"g=" + GenerateGridInitializerPython() + ";");

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

		private string GenerateGridAccessPython_GZip(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString();
			string h = Height.ToString();

			codebuilder.AppendLine(@"import gzip, base64");
			codebuilder.AppendLine(@"_g=" + '"' + GenerateGridBase64DataString() + '"');
			codebuilder.AppendLine(@"g = base64.b64decode(_g)[1:]");
			codebuilder.AppendLine(@"for i in range(base64.b64decode(_g)[0]):");
			codebuilder.AppendLine(@"    g = gzip.decompress(g)");

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

		private string GenerateGridInitializerPython()
		{
			StringBuilder codebuilder = new StringBuilder();

			codebuilder.Append('[');
			for (int y = 0; y < Height; y++)
			{
				if (y != 0)
					codebuilder.Append(',');

				codebuilder.Append('[');
				for (int x = 0; x < Width; x++)
				{
					if (x != 0)
						codebuilder.Append(',');

					codebuilder.Append(SourceGrid[x, y]);
				}
				codebuilder.Append(']');
			}
			codebuilder.Append(']');

			return codebuilder.ToString();
		}

		#endregion

		#endregion
	}
}