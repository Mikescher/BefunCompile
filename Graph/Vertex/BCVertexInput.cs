
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexInput : BCVertex
	{
		private readonly bool modeInteger; // true = int | false = char

		public BCVertexInput(BCDirection d, Vec2i pos, char mode)
			: base(d, new Vec2i[] { pos })
		{
			modeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, char mode)
			: base(d, pos)
		{
			modeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, bool mode)
			: base(d, pos)
		{
			modeInteger = mode;
		}

		public override string ToString()
		{
			return string.Format("OUT({0})", modeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexInput(direction, positions, modeInteger);
		}
	}
}
