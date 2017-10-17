using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.StacksizePredictor;
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
		public BCVertex Root;

		public List<BCVertex> Vertices = new List<BCVertex>();

		public List<ExpressionVariable> Variables = new List<ExpressionVariable>();

		public readonly long[,] SourceGrid;
		public readonly long Width;
		public readonly long Height;

		public readonly List<string> UsedOptimizations = new List<string>();
		public readonly UnstackifyWalker Unstackifier;

		public BCGraph(long[,] sg, long w, long h)
		{
			SourceGrid = sg;
			Width = w;
			Height = h;
			Unstackifier = new UnstackifyWalker(this);
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

		public List<BCVertex> WalkGraphByChildren()
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

				if (v.Parents.Any(p => !Vertices.Contains(p)))
					return false;

				if (v.Children.Any(p => !Vertices.Contains(p)))
					return false;
			}

			if (Variables.Where(p => !p.isUserDefinied).Any(p => p.initial != 0))
				return false;

			if (Variables.Where(p => !p.isUserDefinied).Any(p => p.Scope == null))
				return false;

			if (Variables.Where(p => p.isUserDefinied).Any(p => p.Scope != null))
				return false;

			if (Variables.GroupBy(p => p.Identifier).Any(p => p.Count() > 1))
				return false;

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
			{
				var miss1 = travelled.Except(Vertices).ToList();
				var miss2 = Vertices.Except(travelled).ToList();

				return false;
			}

			if (travelled.Any(p => !Vertices.Contains(p)))
				return false;

			if (Vertices.Any(p => !travelled.Contains(p)))
				return false;

			if (Vertices.Count(p => p.Parents.Count == 0) > 1)
				return false;

			return true;
		}

		public List<Vec2l> GetAllCodePositions()
		{
			return Vertices.SelectMany(p => p.Positions).Select(p => new Vec2l(p.X, p.Y)).Distinct().ToList();
		}

		public IEnumerable<int> GetAllJumps()
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

		public bool IsInput() => Vertices.Any(p => p.IsInput());

		public bool IsOutput() => Vertices.Any(p => p.IsOutput());

		public void RemoveVertex(BCVertex oldVertex)
		{
			if (oldVertex == Root || oldVertex.Children.Count != 1)
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
		
		public int? PredictStackSize()
		{
			// null means possible unlimited stacksize
			// or simply undecidable
			return new StacksizePredictor(this).Predict();
		}

		public IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Vertices.SelectMany(p => p.ListConstantVariableAccess());
		}

		public IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Vertices.SelectMany(p => p.ListDynamicVariableAccess());
		}

		#region CodeGeneration

		public string GenerateGridData(string delim = "")
		{
			StringBuilder codebuilder = new StringBuilder();

			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					codebuilder.Append((char)SourceGrid[x, y]);
				}

				if (y+1 < Height) codebuilder.Append(delim);
			}

			return codebuilder.ToString();
		}

		public void OrderVerticesForFallThrough()
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

		#endregion
	}
}