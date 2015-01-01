using BefunCompile.Math;
using System;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexNOP : BCVertex
	{
		public BCVertexNOP(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public BCVertexNOP(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public override string ToString()
		{
			return "NOP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexNOP(direction, positions);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}
	}
}
