
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPush : BCVertex
	{
		public readonly long value;

		public BCVertexPush(BCDirection d, Vec2i pos, long val)
			: base(d, new Vec2i[] { pos })
		{
			this.value = val;
		}

		public BCVertexPush(BCDirection d, Vec2i[] pos, long val)
			: base(d, pos)
		{
			this.value = val;
		}

		public override string ToString()
		{
			return "PUSH(" + value + ")";
		}
	}
}
