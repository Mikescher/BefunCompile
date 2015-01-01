using BefunCompile.Math;
using System;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPush : BCVertex
	{
		public readonly long Value;

		public BCVertexPush(BCDirection d, Vec2i pos, long val)
			: base(d, new Vec2i[] { pos })
		{
			this.Value = val;
		}

		public BCVertexPush(BCDirection d, Vec2i[] pos, long val)
			: base(d, pos)
		{
			this.Value = val;
		}

		public override string ToString()
		{
			return "PUSH(" + Value + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexPush(direction, positions, Value);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			stackbuilder.Push(Value);

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}
	}
}
