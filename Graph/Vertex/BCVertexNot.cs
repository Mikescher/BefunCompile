
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexNot : BCVertex
	{
		public BCVertexNot(BCDirection d, Vec2i pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "NOT";
		}
	}
}
