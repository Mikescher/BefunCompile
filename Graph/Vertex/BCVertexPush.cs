using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPush : BCVertex
	{
		public BCExpression Value;

		public BCVertexPush(BCDirection d, Vec2i pos, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.Value = val;
		}

		public BCVertexPush(BCDirection d, Vec2i[] pos, BCExpression val)
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

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Value.listConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Value.listDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			stackbuilder.Push(Value.Calculate(ci));

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(Value))
			{
				Value = replacement(Value);
				found = true;
			}
			if (Value.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool isOnlyStackManipulation()
		{
			return Value.isOnlyStackManipulation();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("sa({0});", Value.GenerateCodeCSharp(g));
		}
	}
}
