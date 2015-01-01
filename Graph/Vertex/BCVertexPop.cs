
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPop : BCVertex
	{
		public BCVertexPop(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexPop(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "POP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexPop(direction, positions);
		}
	}
}
