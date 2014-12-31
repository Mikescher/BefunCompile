using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph
{
	public class BCGraph
	{
		public BCVertex root = null;

		public List<BCVertex> vertices = new List<BCVertex>();

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

		public bool TestUpdateParents()
		{
			foreach (var v in vertices)
			{
				if (!v.TestUpdateParents())
					return false;
			}

			return true;
		}

		public bool Optimize()
		{
			bool o1 = OptimizeNOP();
			bool o2 = OptimizeNOPSplit();
			bool o3 = OptimizeNOPTail();
			bool o4 = OptimizeNOPDecision();

			return o1 | o2 | o3 | o4;
		}

		public bool OptimizeNOP()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
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

		public bool OptimizeNOPSplit()
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.parents.Count > 1 && vertex.children.Count == 1 && vertex.parents.All(p => !(p is BCVertexDecision)))
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

		public bool OptimizeNOPTail()
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

		public bool OptimizeNOPDecision()
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
	}
}
