using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexOutput : BCVertex
	{
		public readonly bool ModeInteger; // true = int | false = char

		public BCVertexOutput(BCDirection d, Vec2i pos, long mode)
			: base(d, new Vec2i[] { pos })
		{
			ModeInteger = (mode == '.');
		}

		public BCVertexOutput(BCDirection d, Vec2i[] pos, long mode)
			: base(d, pos)
		{
			ModeInteger = (mode == '.');
		}

		public BCVertexOutput(BCDirection d, Vec2i[] pos, bool mode)
			: base(d, pos)
		{
			ModeInteger = mode;
		}

		public override string ToString()
		{
			return string.Format("OUT_{0}", ModeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexOutput(direction, positions, ModeInteger);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			var c = stackbuilder.Pop();

			if (ModeInteger)
				outbuilder.Append(c);
			else
				outbuilder.Append((char)c);

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}
	}
}
