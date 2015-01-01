using BefunCompile.Math;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullSet : BCVertex
	{
		public readonly long X;
		public readonly long Y;

		public BCVertexFullSet(BCDirection d, Vec2i pos, long xx, long yy)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexFullSet(BCDirection d, Vec2i[] pos, long xx, long yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullSet(direction, positions, X, Y);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
