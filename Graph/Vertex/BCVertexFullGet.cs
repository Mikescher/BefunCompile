using BefunCompile.Math;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullGet : BCVertex
	{
		public readonly long X;
		public readonly long Y;

		public BCVertexFullGet(BCDirection d, Vec2i pos, long xx, long yy)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexFullGet(BCDirection d, Vec2i[] pos, long xx, long yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("GET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullGet(direction, positions, X, Y);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
