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
	}
}
