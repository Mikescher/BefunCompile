
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexNot : BCVertex
	{
		public BCVertexNot(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexNot(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "NOT";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexNot(direction, positions);
		}
	}
}
