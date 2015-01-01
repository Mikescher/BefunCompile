using BefunCompile.Math;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexGet : BCVertex
	{
		public BCVertexGet(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexGet(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "GET";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexGet(direction, positions);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
