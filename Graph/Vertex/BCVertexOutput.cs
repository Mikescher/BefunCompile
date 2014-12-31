
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexOutput : BCVertex
	{
		private readonly bool modeInteger; // true = int | false = char

		public BCVertexOutput(BCDirection d, Vec2i pos, char mode)
			: base(d, new Vec2i[] { pos })
		{
			modeInteger = mode == '.';
		}

		public BCVertexOutput(BCDirection d, Vec2i[] pos, char mode)
			: base(d, pos)
		{
			modeInteger = mode == '.';
		}

		public override string ToString()
		{
			return string.Format("OUT({0})", modeInteger ? "INT" : "CHAR");
		}
	}
}
