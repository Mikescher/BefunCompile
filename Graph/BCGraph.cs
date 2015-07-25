using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
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
		private const int CODEGEN_C_INITIALSTACKSIZE = 16384;

		public BCVertex Root;

		public List<BCVertex> Vertices = new List<BCVertex>();

		public List<ExpressionVariable> Variables = new List<ExpressionVariable>();

		public readonly long[,] SourceGrid;
		public readonly long Width;
		public readonly long Height;

		private readonly MSZipImplementation MSZip = new MSZipImplementation();
		private readonly GZipImplementation GZip = new GZipImplementation();

		private readonly UnstackifyWalker UnstackifyWalker;

		public BCGraph(long[,] sg, long w, long h)
		{
			SourceGrid = sg;
			Width = w;
			Height = h;

			UnstackifyWalker = new UnstackifyWalker(this);
		}

		public BCVertex GetVertex(Vec2i pos, BCDirection dir)
		{
			return Vertices.FirstOrDefault(p =>
				p.Positions.Length == 1 &&
				p.Positions[0].X == pos.X &&
				p.Positions[0].Y == pos.Y &&
				p.Direction == dir);
		}

		public void AfterGen()
		{
			foreach (var v in Vertices)
			{
				v.AfterGen();
			}
		}

		public void UpdateParents()
		{
			foreach (var v in Vertices)
			{
				v.Parents.Clear();
			}

			foreach (var v in Vertices)
			{
				v.UpdateParents();
			}
		}

		private List<BCVertex> WalkGraphByChildren()
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(Root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				travelled.Add(curr);

				foreach (var child in curr.Children.Where(p => !travelled.Contains(p)))
					untravelled.Push(child);
			}

			return travelled.ToList();
		}

		public bool TestGraph()
		{
			foreach (var v in Vertices)
			{
				if (!v.TestVertex())
					return false;

				if (v is BCVertexRandom && v.Children.Count != 4)
					return false;

				if (v is IDecisionVertex && !v.Children.Contains((v as IDecisionVertex).EdgeTrue))
					return false;

				if (v is IDecisionVertex && !v.Children.Contains((v as IDecisionVertex).EdgeFalse))
					return false;
			}

			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(Root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				travelled.Add(curr);

				foreach (var child in curr.Children.Where(p => !travelled.Contains(p)))
					untravelled.Push(child);
			}

			if (travelled.Count != Vertices.Count)
				return false;

			if (travelled.Any(p => !Vertices.Contains(p)))
				return false;

			if (Vertices.Count(p => p.Parents.Count == 0) > 1)
				return false;

			return true;
		}

		public List<Vec2l> GetAllCodePositions()
		{
			return Vertices.SelectMany(p => p.Positions).Select(p => new Vec2l(p.X, p.Y)).Distinct().ToList();
		}

		private IEnumerable<int> GetAllJumps()
		{
			if (Vertices.IndexOf(Root) != 0)
				yield return Vertices.IndexOf(Root);

			for (int i = 0; i < Vertices.Count; i++)
			{
				if (Vertices[i].Children.Count == 1)
				{
					if (Vertices.IndexOf(Vertices[i].Children[0]) != i + 1) // Fall through
						yield return Vertices.IndexOf(Vertices[i].Children[0]);
				}
			}

			foreach (var vx in Vertices)
			{
				foreach (var jump in vx.GetAllJumps(this))
				{
					yield return jump;
				}
			}
		}

		public void RemoveVertex(BCVertex oldVertex)
		{
			if (oldVertex.Children.Count != 1)
			{
				ReplaceVertex(oldVertex, new BCVertexNOP(BCDirection.UNKNOWN, oldVertex.Positions));
				return;
			}

			Vertices.Remove(oldVertex);

			var child = oldVertex.Children.Single();
			child.Parents.Remove(oldVertex);

			foreach (var parent in oldVertex.Parents)
			{
				parent.Children.Remove(oldVertex);

				parent.Children.Add(child);
				if (parent is IDecisionVertex)
				{
					if ((parent as IDecisionVertex).EdgeTrue == oldVertex)
						(parent as IDecisionVertex).EdgeTrue = child;

					if ((parent as IDecisionVertex).EdgeFalse == oldVertex)
						(parent as IDecisionVertex).EdgeFalse = child;
				}
				child.Parents.Add(parent);
			}


			if (!TestGraph())
				throw new Exception("Internal Parent Exception :( ");
		}

		public void ReplaceVertex(BCVertex oldVertex, BCVertex newVertex)
		{
			if (oldVertex == newVertex) return;

			Vertices.Remove(oldVertex);
			Vertices.Add(newVertex);

			if (Root == oldVertex)
				Root = newVertex;

			foreach (var parent in oldVertex.Parents)
			{
				parent.Children.Remove(oldVertex);
				parent.Children.Add(newVertex);
				if (parent is IDecisionVertex)
				{
					if ((parent as IDecisionVertex).EdgeTrue == oldVertex)
						(parent as IDecisionVertex).EdgeTrue = newVertex;

					if ((parent as IDecisionVertex).EdgeFalse == oldVertex)
						(parent as IDecisionVertex).EdgeFalse = newVertex;
				}

				newVertex.Parents.Add(parent);
			}

			foreach (var child in oldVertex.Children)
			{
				child.Parents.Remove(oldVertex);
				child.Parents.Add(newVertex);

				if (newVertex is IDecisionVertex && (oldVertex as IDecisionVertex).EdgeTrue == child)
					(newVertex as IDecisionVertex).EdgeTrue = child;

				if (newVertex is IDecisionVertex && (oldVertex as IDecisionVertex).EdgeFalse == child)
					(newVertex as IDecisionVertex).EdgeFalse = child;

				newVertex.Children.Add(child);
			}


			if (!TestGraph())
				throw new Exception("Internal Parent Exception :( ");
		}

		public void ReplaceVertex(BCVertex oldVertex, List<BCVertex> newVerticies)
		{
			if (newVerticies.Count == 1)
			{
				ReplaceVertex(oldVertex, newVerticies.Single());
				return;
			}

			var newFirst = newVerticies.First();
			var newLast = newVerticies.Last();

			Vertices.Remove(oldVertex);
			Vertices.AddRange(newVerticies);

			if (Root == oldVertex)
				Root = newFirst;

			for (int i = 1; i < newVerticies.Count; i++)
			{
				newVerticies[i - 1].Children.Add(newVerticies[i]);
				newVerticies[i].Parents.Add(newVerticies[i - 1]);
			}

			foreach (var parent in oldVertex.Parents)
			{
				parent.Children.Remove(oldVertex);
				parent.Children.Add(newFirst);
				if (parent is IDecisionVertex)
				{
					if ((parent as IDecisionVertex).EdgeTrue == oldVertex)
						(parent as IDecisionVertex).EdgeTrue = newFirst;

					if ((parent as IDecisionVertex).EdgeFalse == oldVertex)
						(parent as IDecisionVertex).EdgeFalse = newFirst;
				}

				newFirst.Parents.Add(parent);
			}

			foreach (var child in oldVertex.Children)
			{
				child.Parents.Remove(oldVertex);
				child.Parents.Add(newLast);

				if (newLast is IDecisionVertex && (oldVertex as IDecisionVertex).EdgeTrue == child)
					(newLast as IDecisionVertex).EdgeTrue = child;

				if (newLast is IDecisionVertex && (oldVertex as IDecisionVertex).EdgeFalse == child)
					(newLast as IDecisionVertex).EdgeFalse = child;

				newLast.Children.Add(child);
			}
		}

		#region O:1 Minimize

		public bool Minimize()
		{
			bool[] cb = new[]
			{
				MinimizeNOP(),
				MinimizeNOPSplit(),
				MinimizeNOPTail(),
				MinimizeNOPDecision(),
			};

			return cb.Any(p => p);
		}

		private bool MinimizeNOP()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Children.Contains(vertex))
					continue;

				if (vertex.Parents.Contains(vertex))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 1 && vertex.Parents[0].Children.Count == 1)
				{
					found = true;

					BCVertex prev = vertex.Parents[0];
					BCVertex next = vertex.Children[0];

					removed.Add(vertex);

					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Children.Remove(vertex);
					next.Parents.Remove(vertex);

					prev.Children.Add(next);
					next.Parents.Add(prev);

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (vertex == Root)
						Root = next;
				}
			}

			foreach (var rv in removed)
			{
				Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPSplit()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count > 1 && vertex.Children.Count == 1 && vertex.Parents.All(p => !p.IsCodePathSplit()) && vertex.Parents.All(p => p != vertex) && vertex.Children.All(p => p != vertex))
				{
					found = true;

					BCVertex[] prev = vertex.Parents.ToArray();
					BCVertex next = vertex.Children[0];

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					next.Parents.Remove(vertex);

					foreach (var pvertex in prev)
					{
						pvertex.Children.Remove(vertex);

						pvertex.Children.Add(next);
						next.Parents.Add(pvertex);
					}

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (vertex == Root)
						Root = next;
				}
			}

			foreach (var rv in removed)
			{
				Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPTail()
		{
			bool found = false;

			if (Root is BCVertexNOP && Root.Parents.Count == 0 && Root.Children.Count == 1)
			{
				found = true;

				BCVertex vertex = Root;

				Vertices.Remove(vertex);

				vertex.Children[0].Positions = vertex.Children[0].Positions.Concat(vertex.Positions).ToArray();

				vertex.Children[0].Parents.Remove(vertex);
				Root = vertex.Children[0];
			}

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 0 && vertex.Parents[0].Children.Count == 1 && vertex != Root)
				{
					found = true;

					BCVertex prev = vertex.Parents[0];

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Positions = prev.Positions.Concat(vertex.Positions).Distinct().ToArray();

					prev.Children.Remove(vertex);
				}
			}

			foreach (var rv in removed)
			{
				Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPDecision()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 1 && vertex.Parents[0] is BCVertexDecision)
				{
					found = true;

					BCVertexDecision prev = (BCVertexDecision)vertex.Parents[0];
					BCVertex next = vertex.Children[0];

					bool isDTrue = (prev.EdgeTrue == vertex);

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Children.Remove(vertex);
					next.Parents.Remove(vertex);

					prev.Children.Add(next);
					next.Parents.Add(prev);

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (isDTrue)
						prev.EdgeTrue = next;
					else
						prev.EdgeFalse = next;

					if (vertex == Root)
						Root = next;
				}
			}

			foreach (var rv in removed)
			{
				Vertices.Remove(rv);
			}

			return found;
		}

		#endregion

		#region O:2 Substitute

		public bool Substitute()
		{
			var rule1 = new BCModRule();
			rule1.AddPreq(v => v is BCVertexExpression);
			rule1.AddPreq(v => v is BCVertexExpression);
			rule1.AddPreq(v => v is BCVertexBinaryMath);
			rule1.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionBinMath.Create(((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression, ((BCVertexBinaryMath)l[2]).MathType)));

			var rule2 = new BCModRule();
			rule2.AddPreq(v => v is BCVertexExpression);
			rule2.AddPreq(v => v is BCVertexNot);
			rule2.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionNot.Create(((BCVertexExpression)l[0]).Expression)));

			var rule3 = new BCModRule();
			rule3.AddPreq(v => v is BCVertexExpression);
			rule3.AddPreq(v => v is BCVertexPop);

			var rule4 = new BCModRule();
			rule4.AddPreq(v => v is BCVertexSwap);
			rule4.AddPreq(v => v is BCVertexSwap);

			var rule5 = new BCModRule();
			rule5.AddPreq(v => v is BCVertexExpression && (v as BCVertexExpression).Expression.IsNotStackAccess());
			rule5.AddPreq(v => v is BCVertexDup);
			rule5.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));
			rule5.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));

			var rule6 = new BCModRule();
			rule6.AddPreq(v => v is BCVertexExpression && (v as BCVertexExpression).Expression.IsNotStackAccess());
			rule6.AddPreq(v => v is BCVertexExpression && (v as BCVertexExpression).Expression.IsNotStackAccess());
			rule6.AddPreq(v => v is BCVertexSwap);
			rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[1]).Expression));
			rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));

			var rule7_1 = new BCModRule();
			rule7_1.AddPreq(v => v is BCVertexSwap);
			rule7_1.AddPreq(v => v is BCVertexBinaryMath && (v as BCVertexBinaryMath).MathType == BinaryMathType.GT);
			rule7_1.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.LT));

			var rule7_2 = new BCModRule();
			rule7_2.AddPreq(v => v is BCVertexSwap);
			rule7_2.AddPreq(v => v is BCVertexBinaryMath && (v as BCVertexBinaryMath).MathType == BinaryMathType.LT);
			rule7_2.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.GT));

			var rule7_3 = new BCModRule();
			rule7_3.AddPreq(v => v is BCVertexSwap);
			rule7_3.AddPreq(v => v is BCVertexBinaryMath && (v as BCVertexBinaryMath).MathType == BinaryMathType.GET);
			rule7_3.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.LET));

			var rule7_4 = new BCModRule();
			rule7_4.AddPreq(v => v is BCVertexSwap);
			rule7_4.AddPreq(v => v is BCVertexBinaryMath && (v as BCVertexBinaryMath).MathType == BinaryMathType.LET);
			rule7_4.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.GET));

			bool[] cb = new[]
			{
				rule1.Execute(this),
				rule2.Execute(this),
				rule3.Execute(this),
				rule4.Execute(this),
				rule5.Execute(this),
				rule6.Execute(this),

				rule7_1.Execute(this),
				rule7_2.Execute(this),
				rule7_3.Execute(this),
				rule7_4.Execute(this),
			};

			return cb.Any(p => p);
		}

		#endregion

		#region O:3 Flatten

		public bool FlattenStack()
		{
			var rule1 = new BCModRule();
			rule1.AddPreq(v => v is BCVertexExpression);
			rule1.AddPreq(v => v is BCVertexExpression);
			rule1.AddPreq(v => v is BCVertexGet);
			rule1.AddRep((l, p) => new BCVertexExprGet(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression));

			var rule2 = new BCModRule();
			rule2.AddPreq(v => v is BCVertexExpression);
			rule2.AddPreq(v => v is BCVertexExpression);
			rule2.AddPreq(v => v is BCVertexSet);
			rule2.AddRep((l, p) => new BCVertexExprPopSet(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression));

			var rule3 = new BCModRule();
			rule3.AddPreq(v => v is BCVertexExpression);
			rule3.AddPreq(v => v is BCVertexExprPopSet);
			rule3.AddRep((l, p) => new BCVertexExprSet(BCDirection.UNKNOWN, p, ((BCVertexExprPopSet)l[1]).X, ((BCVertexExprPopSet)l[1]).Y, ((BCVertexExpression)l[0]).Expression));

			var rule4 = new BCModRule();
			rule4.AddPreq(v => v is BCVertexExprGet);
			rule4.AddPreq(v => v is BCVertexDup);
			rule4.AddRep((l, p) => l[0].Duplicate());
			rule4.AddRep((l, p) => { var v = l[0].Duplicate(); v.Positions = p; return v; });

			var rule5 = new BCModRule();
			rule5.AddPreq(v => !v.IsCodePathSplit() && v.IsNotGridAccess() && v.IsNotVariableAccess()); // <-- Stack Access
			rule5.AddPreq(v => ((v is BCVertexExprSet || v is BCVertexExprVarSet) && v.IsNotStackAccess())); // <-- No Stack Access
			rule5.AddRep((l, p) => l[1].Duplicate());
			rule5.AddRep((l, p) => l[0].Duplicate());

			var rule6 = new BCModRule();
			rule6.AddPreq(v => v is BCVertexExprGet);
			rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExprGet)l[0]).ToExpression()));

			var rule7 = new BCModRule();
			rule7.AddPreq(v => v is BCVertexExpression);
			rule7.AddPreq(v => v is BCVertexOutput);
			rule7.AddRep((l, p) => new BCVertexExprOutput(BCDirection.UNKNOWN, p, ((BCVertexOutput)l[1]).ModeInteger, ((BCVertexExpression)l[0]).Expression));

			bool[] cb = new[]
			{
				Substitute(),
				
				rule1.Execute(this),
				rule2.Execute(this),
				rule3.Execute(this),
				rule4.Execute(this),
				rule5.Execute(this),
				rule6.Execute(this),
				rule7.Execute(this),

				IntegrateDecisions(),
			};

			return cb.Any(p => p);
		}

		private bool IntegrateDecisions()
		{
			var rule = new BCModRule();
			rule.AddPreq(v => v is BCVertexExpression);
			rule.AddPreq(v => v is BCVertexDecision);

			var chain = rule.GetMatchingChain(this);

			if (chain == null)
				return false;

			var prev = chain[0].Parents.ToList();
			var nextTrue = ((BCVertexDecision)chain[1]).EdgeTrue;
			var nextFalse = ((BCVertexDecision)chain[1]).EdgeFalse;

			if (prev.Any(p => p is BCVertexExprDecision))
				return false;

			if (chain[1].Parents.Count > 1)
				return false;

			chain[0].Children.Clear();
			chain[0].Parents.Clear();
			Vertices.Remove(chain[0]);

			chain[1].Children.Clear();
			chain[1].Parents.Clear();
			Vertices.Remove(chain[1]);

			var newnode = new BCVertexExprDecision(BCDirection.UNKNOWN, chain.SelectMany(p => p.Positions).ToArray(), nextTrue, nextFalse, ((BCVertexExpression)chain[0]).Expression);

			Vertices.Add(newnode);

			nextTrue.Parents.Remove(chain[1]);
			newnode.Children.Add(nextTrue);
			nextTrue.Parents.Add(newnode);

			nextFalse.Parents.Remove(chain[1]);
			newnode.Children.Add(nextFalse);
			nextFalse.Parents.Add(newnode);

			foreach (var p in prev)
			{
				p.Children.Remove(chain[0]);
				p.Children.Add(newnode);
				newnode.Parents.Add(p);

				if (p is IDecisionVertex)
				{
					if ((p as IDecisionVertex).EdgeTrue == chain[0])
						(p as IDecisionVertex).EdgeTrue = newnode;
					if ((p as IDecisionVertex).EdgeFalse == chain[0])
						(p as IDecisionVertex).EdgeFalse = newnode;
				}
			}

			if (Root == chain[0])
				Root = newnode;

			return true;
		}

		#endregion

		#region O:4 Variablize

		public IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Vertices.SelectMany(p => p.ListConstantVariableAccess());
		}

		public IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Vertices.SelectMany(p => p.ListDynamicVariableAccess());
		}

		public bool VariablizeGraph(Func<long, long, long> gridGetter, List<MemoryAccess> dynamicAccess, List<MemoryAccess> constAccess)
		{
			if (dynamicAccess.Count == 0 && Variables.Count == 0)
				SubstituteConstMemoryAccess(gridGetter);

			return RecombineExpressions();
		}

		private void SubstituteConstMemoryAccess(Func<long, long, long> gridGetter)
		{
			var ios = ListConstantVariableAccess().ToList();

			Variables = ios
				.Select(p => new Vec2l(p.getX().Calculate(null), p.getY().Calculate(null)))
				.Distinct()
				.Select((p, i) => ExpressionVariable.CreateUserVariable(i, gridGetter(p.X, p.Y), p))
				.ToList();

			var vardic = new Dictionary<Vec2l, ExpressionVariable>();
			Variables.ForEach(p => vardic.Add(p.position, p));

			BCModRule vertexRule1 = new BCModRule();
			vertexRule1.AddPreq(p => p is BCVertexExprGet && ios.Contains((MemoryAccess)p));
			vertexRule1.AddRep((l, p) => new BCVertexVarGet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprGet)l[0]).getConstantPos()]));

			BCModRule vertexRule2 = new BCModRule();
			vertexRule2.AddPreq(p => p is BCVertexExprPopSet && ios.Contains((MemoryAccess)p));
			vertexRule2.AddRep((l, p) => new BCVertexVarSet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprPopSet)l[0]).getConstantPos()]));

			BCModRule vertexRule3 = new BCModRule();
			vertexRule3.AddPreq(p => p is BCVertexExprSet && ios.Contains((MemoryAccess)p));
			vertexRule3.AddRep((l, p) => new BCVertexExprVarSet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprSet)l[0]).getConstantPos()], ((BCVertexExprSet)l[0]).Value));

			BCExprModRule exprRule1 = new BCExprModRule();
			exprRule1.setPreq(p => p is ExpressionGet && ios.Contains((MemoryAccess)p));
			exprRule1.setRep(p => vardic[((ExpressionGet)p).getConstantPos()]);

			bool changed = true;

			while (changed)
			{
				bool[] cb = new[]
				{
					vertexRule1.Execute(this),
					vertexRule2.Execute(this),
					vertexRule3.Execute(this),
					exprRule1.Execute(this),
				};

				changed = cb.Any(p => p);
			}
		}

		private bool RecombineExpressions()
		{
			BCModRule combRule1 = new BCModRule();
			combRule1.AddPreq(p => p is BCVertexExpression && (p as BCVertexExpression).Expression.IsNotStackAccess());
			combRule1.AddPreq(p => p is BCVertexBinaryMath);
			combRule1.AddRep((l, p) => new BCVertexExprPopBinaryMath(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexBinaryMath)l[1]).MathType));

			BCModRule combRule2 = new BCModRule(false);
			combRule2.AddPreq(p => p is BCVertexDup);
			combRule2.AddPreq(p => p is BCVertexExprPopBinaryMath && (p as BCVertexExprPopBinaryMath).SecondExpression.IsNotStackAccess());
			combRule2.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionPeek.Create(), ((BCVertexExprPopBinaryMath)l[1]).MathType, ((BCVertexExprPopBinaryMath)l[1]).SecondExpression));

			BCModRule combRule3 = new BCModRule();
			combRule3.AddPreq(p => p is BCVertexExpression);
			combRule3.AddPreq(p => p is BCVertexNot);
			combRule3.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionNot.Create((l[0] as BCVertexExpression).Expression)));

			BCModRule combRule4 = new BCModRule();
			combRule4.AddPreq(p => p is BCVertexExpression);
			combRule4.AddPreq(p => p is BCVertexOutput);
			combRule4.AddRep((l, p) => new BCVertexExprOutput(BCDirection.UNKNOWN, p, (l[1] as BCVertexOutput).ModeInteger, ExpressionNot.Create((l[0] as BCVertexExpression).Expression)));

			BCModRule combRule5 = new BCModRule(true, true);
			combRule5.AddPreq(p => p is BCVertexExpression);
			combRule5.AddPreq(p => p is BCVertexDecision);
			combRule5.AddRep((l, p) => new BCVertexExprDecision(BCDirection.UNKNOWN, p, (l[1] as BCVertexDecision).EdgeTrue, (l[1] as BCVertexDecision).EdgeFalse, (l[0] as BCVertexExpression).Expression));

			BCModRule combRule6 = new BCModRule();
			combRule6.AddPreq(p => p is BCVertexExpression);
			combRule6.AddPreq(p => p is BCVertexExprPopBinaryMath && (p as BCVertexExprPopBinaryMath).SecondExpression.IsNotStackAccess());
			combRule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionBinMath.Create((l[0] as BCVertexExpression).Expression, (l[1] as BCVertexExprPopBinaryMath).SecondExpression, (l[1] as BCVertexExprPopBinaryMath).MathType)));

			BCModRule combRule7 = new BCModRule();
			combRule7.AddPreq(p => p is BCVertexDup);
			combRule7.AddPreq(p => p is BCVertexVarSet);
			combRule7.AddRep((l, p) => new BCVertexExprVarSet(BCDirection.UNKNOWN, p, (l[1] as BCVertexVarSet).Variable, ExpressionPeek.Create()));

			bool[] cb = new[]
			{
				combRule1.Execute(this),
				combRule2.Execute(this),
				combRule3.Execute(this),
				combRule4.Execute(this),
				combRule5.Execute(this),
				combRule6.Execute(this),
				combRule7.Execute(this),
				
				RemovePredeterminedDecisions(),
			};

			return cb.Any(p => p);
		}

		private bool RemovePredeterminedDecisions()
		{
			bool[] cb = new[]
			{
				RemovePredeterminedDecisions_0(),
				RemovePredeterminedDecisions_1(),
			};

			return cb.Any(p => p);
		}

		private bool RemovePredeterminedDecisions_0()
		{
			List<BCVertex> chain = null;

			foreach (var v in Vertices.Where(p => p is BCVertexDecision))
			{
				var prev = v.Parents.FirstOrDefault(p => p is BCVertexExpression && (p as BCVertexExpression).Expression is ExpressionConstant);
				if (prev == null)
					continue;
				if (prev.Parents.Count == 0)
					continue;

				chain = new List<BCVertex>() { prev, v };
			}

			if (chain == null)
				return false;


			var Expression = chain[0] as BCVertexExpression;
			var Decision = chain[1] as BCVertexDecision;

			var Prev = Expression.Parents;
			var Next = Expression.Expression.Calculate(null) != 0 ? Decision.EdgeTrue : Decision.EdgeFalse;

			//######## REPLACE ########

			Next.Parents.AddRange(Prev);
			foreach (var node in Prev)
			{
				node.Children.Add(Next);
				node.Children.Remove(Expression);

				if (node is IDecisionVertex)
				{
					if ((node as IDecisionVertex).EdgeTrue == Expression)
						(node as IDecisionVertex).EdgeTrue = Next;
					if ((node as IDecisionVertex).EdgeFalse == Expression)
						(node as IDecisionVertex).EdgeFalse = Next;
				}
			}
			Decision.Parents.Remove(Expression);

			Expression.Parents.Clear();
			Expression.Children.Clear();
			Vertices.Remove(Expression);

			if (Decision.Parents.Count == 0)
			{
				Next.Parents.Remove(Decision);

				Decision.Parents.Clear();
				Decision.Children.ForEach(p => p.Parents.Remove(Decision));
				Decision.Children.Clear();
				Vertices.Remove(Decision);

				RemoveHeadlessPaths();
			}

			return true;
		}

		private bool RemovePredeterminedDecisions_1()
		{
			foreach (var vertex in Vertices)
			{
				if (!(vertex is BCVertexExprDecision))
					continue;

				if (!((vertex as BCVertexExprDecision).Value is ExpressionConstant))
					continue;

				BCVertexExprDecision decision = vertex as BCVertexExprDecision;

				if (decision.Value.Calculate(null) != 0)
				{
					decision.EdgeFalse.Parents.Remove(decision);
					decision.Children.Remove(decision.EdgeFalse);
					decision.EdgeFalse = null;
				}
				else
				{
					decision.EdgeTrue.Parents.Remove(decision);
					decision.Children.Remove(decision.EdgeTrue);
					decision.EdgeTrue = null;
				}

				var remRule = new BCModRule(false);
				remRule.AddPreq(p => p == decision);

				bool exec = remRule.Execute(this);

				if (!exec)
					throw new Exception("errrrrrrr");

				var included = WalkGraphByChildren();
				Vertices = Vertices.Where(p => included.Contains(p)).ToList();

				foreach (var v in Vertices)
				{
					v.Parents.Where(p => !included.Contains(p))
						.ToList()
						.ForEach(p => v.Parents.Remove(p));
				}

				RemoveHeadlessPaths();

				return true;
			}

			return false;
		}

		private void RemoveHeadlessPaths()
		{
			for (; ; )
			{
				var headless = Vertices.FirstOrDefault(p => p.Parents.Count == 0 && p != Root);
				if (headless == null)
					break;

				Vertices.Remove(headless);
				headless.Children.ForEach(p => p.Parents.Remove(headless));
			}
		}

		#endregion

		#region O:5 Unstackify

		public bool Unstackify()
		{
			return UnstackifyWalker.Run();
		}

		#endregion

		#region O:6 Combine

		public bool CombineBlocks()
		{
			var ruleSubstitute = new BCModRule(false);
			ruleSubstitute.AddPreq(v => v.Children.Count <= 1 && !(v is BCVertexBlock));
			ruleSubstitute.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0]));

			var ruleCombine = new BCModRule(false);
			ruleCombine.AddPreq(v => v is BCVertexBlock);
			ruleCombine.AddPreq(v => v is BCVertexBlock);
			ruleCombine.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0] as BCVertexBlock, l[1] as BCVertexBlock));

			var ruleMinimize = new BCModRule(false);
			ruleMinimize.AddPreq(v => v is BCVertexNOP);

			bool[] cb = new[]
			{
				ruleSubstitute.Execute(this),
				ruleCombine.Execute(this),
				ruleMinimize.Execute(this),
				
				RemovePredeterminedDecisions(),
			};

			return cb.Any(p => p);
		}

		#endregion

		#region O:7 Reduce

		public bool ReduceBlocks()
		{
			var ruleRepl1 = new BCModRule(true, true);
			ruleRepl1.AddPreq(p => p is BCVertexBlock);
			ruleRepl1.AddPreq(p => p is BCVertexDecision);
			ruleRepl1.AddRep((l, p) => new BCVertexDecisionBlock(BCDirection.UNKNOWN, l[0] as BCVertexBlock, l[1] as BCVertexDecision));

			var ruleRepl2 = new BCModRule(true, true);
			ruleRepl2.AddPreq(p => p is BCVertexBlock);
			ruleRepl2.AddPreq(p => p is BCVertexExprDecision);
			ruleRepl2.AddRep((l, p) => new BCVertexExprDecisionBlock(BCDirection.UNKNOWN, l[0] as BCVertexBlock, l[1] as BCVertexExprDecision));

			bool[] cb = new[]
			{
				ruleRepl1.Execute(this),
				ruleRepl2.Execute(this),
				ReplaceVariableIntializer(),
			};

			return cb.Any(p => p);
		}

		private bool ReplaceVariableIntializer()
		{
			if (Root.Parents.Count != 0)
				return false;

			if (Variables.Count == 0)
				return false;

			if (!(Root is BCVertexBlock))
				return false;

			BCVertexBlock BRoot = Root as BCVertexBlock;
			foreach (var variable in Variables)
			{
				for (int i = 0; i < BRoot.nodes.Length; i++)
				{
					BCVertex node = BRoot.nodes[i];
					if (node is BCVertexExprVarSet && (node as BCVertexExprVarSet).Variable == variable && (node as BCVertexExprVarSet).Value is ExpressionConstant)
					{
						long ivalue = ((node as BCVertexExprVarSet).Value as ExpressionConstant).Value;
						variable.initial = ivalue;

						BCVertex newnode = BRoot.GetWithRemovedNode(node);

						var ruleRepl = new BCModRule(false, false);
						ruleRepl.AddPreq(p => p == BRoot);
						ruleRepl.AddRep((l, p) => (l[0] as BCVertexBlock).GetWithRemovedNode(node));

						if (ruleRepl.Execute(this))
							return true;
						else
							break;
					}

					if (node.GetVariables().Contains(variable))
						break;
				}
			}

			return false;
		}

		#endregion

		#region CodeGeneration

		private string indent(string code, string indent)
		{
			return string.Join(Environment.NewLine, code.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(p => indent + p));
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

		private void OrderVerticesForFallThrough()
		{
			List<BCVertex> newlist = new List<BCVertex>();
			Stack<BCVertex> nextStack = new Stack<BCVertex>();

			nextStack.Push(Root);

			while (nextStack.Any())
			{
				var node = nextStack.Pop();
				if (!newlist.Contains(node))
				{
					newlist.Add(node);

					node.Children.Where(p => !newlist.Contains(p)).ToList().ForEach(p => nextStack.Push(p));
				}
			}

			if (newlist.Count != Vertices.Count)
				throw new CodeGenException();

			Vertices = newlist;
		}

		#region CodeGeneration (C#)

		public string GenerateCodeCSharp(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			OrderVerticesForFallThrough();

			TestGraph();

			List<int> activeJumps = GetAllJumps().Distinct().ToList();

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

			if (ListDynamicVariableAccess().Any() || ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccessCSharp(implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateStackAccessCSharp(implementSafeStackAccess));
			codebuilder.Append(GenerateHelperMethodsCSharp());

			codebuilder.AppendLine("static void Main(string[] args)");
			codebuilder.AppendLine("{");

			if (Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine(indent2 + "long " + string.Join(",", Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(indent2 + "long " + variable.Identifier + "=" + variable.initial + ";");
			}

			if (Vertices.IndexOf(Root) != 0)
				codebuilder.AppendLine(indent2 + "goto _" + Vertices.IndexOf(Root) + ";");

			for (int i = 0; i < Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine(indent1 + "_" + i + ":");

				codebuilder.AppendLine(indent(Vertices[i].GenerateCodeCSharp(this), indent2));

				if (Vertices[i].Children.Count == 1)
				{
					if (Vertices.IndexOf(Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(indent2 + "goto _" + Vertices.IndexOf(Vertices[i].Children[0]) + ";");
				}
				else if (Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(indent2 + "return;");
				}
			}

			codebuilder.AppendLine("}}");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));
		}

		private string GenerateHelperMethodsCSharp()
		{
			StringBuilder codebuilder = new StringBuilder();

			if (Vertices.Any(p => p.IsRandom()))
			{
				codebuilder.AppendLine(@"private static readonly System.Random r = new System.Random();");
				codebuilder.AppendLine(@"private static bool rd(){ return r.Next(2)!=0; }");
			}

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

			var b64 = GZip.GenerateBase64StringList(GenerateGridData());
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

			codebuilder.AppendLine(@"private static byte[]zd(byte[]o){byte[]d=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Skip(o, 1));for(int i=0;i<o[0];i++)d=zs(d);return d;}");
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

		public string GenerateCodeC(bool fmtOutput, bool implementSafeStackAccess, bool implementSafeGridAccess, bool useGZip)
		{
			OrderVerticesForFallThrough();

			TestGraph();

			List<int> activeJumps = GetAllJumps().Distinct().ToList();

			string indent1 = "    ";

			if (!fmtOutput)
				indent1 = "";

			StringBuilder codebuilder = new StringBuilder();
			codebuilder.AppendLine(@"/* compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015 */");

			if (Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine("#include <time.h>");

			codebuilder.AppendLine("#include <stdio.h>");
			codebuilder.AppendLine("#include <stdlib.h>");
			codebuilder.AppendLine("#define int64 long long");

			if (ListDynamicVariableAccess().Any() || ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccessC(implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethodsC());
			codebuilder.Append(GenerateStackAccessC(implementSafeStackAccess));

			codebuilder.AppendLine("int main(void)");
			codebuilder.AppendLine("{");

			if (Variables.Any(p => !p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + string.Join(",", Variables.Where(p => !p.isUserDefinied)) + ";");
			}

			foreach (var variable in Variables.Where(p => p.isUserDefinied))
			{
				codebuilder.AppendLine(indent1 + "int64 " + variable.Identifier + "=" + variable.initial + ";");
			}

			if (ListDynamicVariableAccess().Any() && useGZip)
				codebuilder.AppendLine(indent1 + "d();");

			if (Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(indent1 + "srand(time(NULL));");

			codebuilder.AppendLine(indent1 + "s=(int64*)calloc(q,sizeof(int64));");

			if (Vertices.IndexOf(Root) != 0)
				codebuilder.AppendLine(indent1 + "goto _" + Vertices.IndexOf(Root) + ";");

			for (int i = 0; i < Vertices.Count; i++)
			{
				if (activeJumps.Contains(i))
					codebuilder.AppendLine("_" + i + ":");

				codebuilder.AppendLine(indent(Vertices[i].GenerateCodeC(this), indent1));

				if (Vertices[i].Children.Count == 1)
				{
					if (Vertices.IndexOf(Vertices[i].Children[0]) != i + 1) // Fall through
						codebuilder.AppendLine(indent1 + "goto _" + Vertices.IndexOf(Vertices[i].Children[0]) + ";");
				}
				else if (Vertices[i].Children.Count == 0)
				{
					codebuilder.AppendLine(indent1 + "return 0;");
				}
			}

			codebuilder.AppendLine("}");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));
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

			if (Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"int rd(){return rand()%2==0;}");

			codebuilder.AppendLine(@"int64 td(int64 a,int64 b){ return (b==0)?0:(a/b); }");
			codebuilder.AppendLine(@"int64 tm(int64 a,int64 b){ return (b==0)?0:(a%b); }");

			return codebuilder.ToString();
		}

		private string GenerateGridAccessC(bool implementSafeGridAccess, bool useGZip)
		{
			if (useGZip)
				return GenerateGridAccessC_GZip(implementSafeGridAccess);
			return GenerateGridAccessC_NoGZip(implementSafeGridAccess);
		}

		private string GenerateGridAccessC_GZip(bool implementSafeGridAccess)
		{
			StringBuilder codebuilder = new StringBuilder();

			string w = Width.ToString();
			string h = Height.ToString();

			int msz_len;
			var msz = MSZip.GenerateAnsiCEscapedStringList(GenerateGridData(), out msz_len);

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
			codebuilder.AppendLine(@"int64 g[" + (Width * Height) + "];");
			codebuilder.AppendLine(@"int d(){int s,w,i,j,h;h=z;for(;t<" + msz_len + ";t++)if(_g[t]==';')g[z++]=_g[++t];" +
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

		private string GenerateGridAccessC_NoGZip(bool implementSafeGridAccess)
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
			OrderVerticesForFallThrough();

			TestGraph();

			StringBuilder codebuilder = new StringBuilder();
			codebuilder.AppendLine(@"#!/usr/bin/env python3");
			codebuilder.AppendLine();
			codebuilder.AppendLine(@"# compiled with BefunCompile v" + BefunCompiler.VERSION + " (c) 2015");

			if (Vertices.Any(p => p.IsRandom()))
				codebuilder.AppendLine(@"from random import randint");

			if (ListDynamicVariableAccess().Any() || ListConstantVariableAccess().Any())
				codebuilder.Append(GenerateGridAccessPython(implementSafeGridAccess, useGZip));
			codebuilder.Append(GenerateHelperMethodsPython());
			codebuilder.Append(GenerateStackAccessPython(implementSafeStackAccess));

			foreach (var variable in Variables.Where(p => p.isUserDefinied))
				codebuilder.AppendLine(variable.Identifier + "=" + variable.initial);


			for (int i = 0; i < Vertices.Count; i++)
			{
				codebuilder.AppendLine("def _" + i + "():");
				foreach (var variable in Vertices[i].GetVariables())
					codebuilder.AppendLine("    global " + variable.Identifier);

				codebuilder.AppendLine(indent(Vertices[i].GenerateCodePython(this), "    "));

				if (Vertices[i].Children.Count == 1)
					codebuilder.AppendLine("    return " + Vertices.IndexOf(Vertices[i].Children[0]) + "");
				else if (Vertices[i].Children.Count == 0)
					codebuilder.AppendLine("    return " + Vertices.Count);
			}

			codebuilder.AppendLine("m=[" + string.Join(",", Enumerable.Range(0, Vertices.Count).Select(p => "_" + p)) + "]");
			codebuilder.AppendLine("c=" + Vertices.IndexOf(Root));
			codebuilder.AppendLine("while c<" + Vertices.Count + ":");
			codebuilder.AppendLine("    c=m[c]()");

			return string.Join(Environment.NewLine, codebuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != ""));

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

			if (Vertices.Any(p => p.IsRandom()))
			{
				codebuilder.AppendLine(@"def rd():");
				codebuilder.AppendLine(@"    return bool(random.getrandbits(1))");
			}

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

			var b64 = GZip.GenerateBase64StringList(GenerateGridData());
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

			codebuilder.AppendLine(@"g = base64.b64decode(_g)[1:]");
			codebuilder.AppendLine(@"for i in range(base64.b64decode(_g)[0]):");
			codebuilder.AppendLine(@"    g = gzip.decompress(g)");
			codebuilder.AppendLine(@"g=list(g)");

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