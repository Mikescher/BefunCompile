using BefunCompile.Math;
using System;
using System.Linq;
using System.Text;

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

		public override BCVertex Duplicate()
		{
			return new BCVertexPush(direction, positions, value);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			stackbuilder.Push(value);

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}
	}
}
