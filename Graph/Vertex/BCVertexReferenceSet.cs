using BefunCompile.Math;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexReferenceSet : BCVertex
	{
		public readonly long GetX;
		public readonly long GetY;
		public readonly long SetX;
		public readonly long SetY;

		public BCVertexReferenceSet(BCDirection d, Vec2i pos, long sx, long sy, long gx, long gy)
			: base(d, new Vec2i[] { pos })
		{
			this.GetX = gx;
			this.GetY = gy;

			this.SetX = sx;
			this.SetY = sy;
		}

		public BCVertexReferenceSet(BCDirection d, Vec2i[] pos, long sx, long sy, long gx, long gy)
			: base(d, pos)
		{
			this.GetX = gx;
			this.GetY = gy;

			this.SetX = sx;
			this.SetY = sy;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1}) = GET({2}, {3})", SetX, SetY, GetX, GetY);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexReferenceSet(direction, positions, SetX, SetY, GetX, GetY);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
