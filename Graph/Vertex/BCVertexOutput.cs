using BefunCompile.Math;
using System;
using System.Linq;
using System.Text;

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

		public BCVertexOutput(BCDirection d, Vec2i[] pos, bool mode)
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

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			var c = stackbuilder.Pop();

			if (modeInteger)
				outbuilder.Append(c);
			else
				outbuilder.Append((char)c);

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}
	}
}
