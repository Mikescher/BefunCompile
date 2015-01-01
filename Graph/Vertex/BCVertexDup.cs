
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexDup : BCVertex
	{
		public BCVertexDup(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}
		public BCVertexDup(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "DUP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexDup(direction, positions);
		}
	}
}
