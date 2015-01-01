
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexSwap : BCVertex
	{
		public BCVertexSwap(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexSwap(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SWAP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexSwap(direction, positions);
		}
	}
}
