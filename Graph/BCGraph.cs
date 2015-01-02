using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

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

			if (vertices.Count(p => p.parents.Count == 0) > 1)
				return false;

			return true;
		}

		#region Minimize

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

		#region Substitute

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

		#region Flatten

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
			rule4.AddRep((l, p) => l[0].Duplicate());

			var rule5 = new BCModRule();
			rule5.AddPreq(v => !(v is BCVertexDecision || v is BCVertexFullDecision) && v.isOnlyStackManipulation());
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

		#region Variablize

		public IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return vertices.SelectMany(p => p.listConstantVariableAccess());
		}

		public IEnumerable<MemoryAccess> listDynamicVariableAccess() // auch gets intern beachten
		{
			return vertices.SelectMany(p => p.listDynamicVariableAccess());
		}

		public void SubstituteConstMemoryAccess(Func<long, long, long> gridGetter)
		{
			var ios = listConstantVariableAccess().ToList();

			variables = ios
				.Select(p => new Vec2l(p.getX().Calculate(null), p.getY().Calculate(null)))
				.Distinct()
				.Select((p, i) => ExpressionVariable.Create("var" + i, gridGetter(p.X, p.Y), p))
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
	}
}
