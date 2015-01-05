using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullDecision : BCVertex
	{
		public BCVertex edgeTrue;
		public BCVertex edgeFalse;

		public BCExpression Value;

		public BCVertexFullDecision(BCDirection d, Vec2i pos, BCVertex childTrue, BCVertex childFalse, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.edgeTrue = childTrue;
			this.edgeFalse = childFalse;
			this.Value = val;
		}

		public BCVertexFullDecision(BCDirection d, Vec2i[] pos, BCVertex childTrue, BCVertex childFalse, BCExpression val)
			: base(d, pos)
		{
			this.edgeTrue = childTrue;
			this.edgeFalse = childFalse;
			this.Value = val;
		}

		public override string ToString()
		{
			return "IF (" + Value.ToString() + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullDecision(direction, positions, edgeTrue, edgeFalse, Value);
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
			var v = Value.Calculate(ci) != 0;

			return v ? edgeTrue : edgeFalse;
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
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeCSharp(g), g.vertices.IndexOf(edgeTrue), g.vertices.IndexOf(edgeFalse));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeC(g), g.vertices.IndexOf(edgeTrue), g.vertices.IndexOf(edgeFalse));
		}
	}
}
