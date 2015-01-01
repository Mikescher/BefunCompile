using BefunCompile.Math;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexTotalSet : BCVertex
	{
		public readonly long X;
		public readonly long Y;
		public readonly long Value;

		public BCVertexTotalSet(BCDirection d, Vec2i pos, long xx, long yy, long val)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public BCVertexTotalSet(BCDirection d, Vec2i[] pos, long xx, long yy, long val)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1}) = {2}", X, Y, Value);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexTotalSet(direction, positions, X, Y, Value);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
