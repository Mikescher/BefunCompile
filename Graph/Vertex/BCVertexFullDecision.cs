using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullDecision : BCVertex, IDecisionVertex
	{
		public BCVertex EdgeTrue { get; set; }
		public BCVertex EdgeFalse { get; set; }

		public BCExpression Value;

		public BCVertexFullDecision(BCDirection d, Vec2i[] pos, BCVertex childTrue, BCVertex childFalse, BCExpression val)
			: base(d, pos)
		{
			EdgeTrue = childTrue;
			EdgeFalse = childFalse;
			Value = val;
		}

		public override string ToString()
		{
			return "IF (" + Value + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullDecision(Direction, Positions, EdgeTrue, EdgeFalse, Value);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Value.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.ListDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			var v = Value.Calculate(ci) != 0;

			return v ? EdgeTrue : EdgeFalse;
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

		public override bool IsOnlyStackManipulation()
		{
			return false;
		}

		public override bool IsCodePathSplit()
		{
			return true;
		}

		public override bool IsBlock()
		{
			return false;
		}

		public override bool IsRandom()
		{
			return false;
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Value.GetVariables();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeCSharp(g), g.Vertices.IndexOf(EdgeTrue), g.Vertices.IndexOf(EdgeFalse));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeC(g), g.Vertices.IndexOf(EdgeTrue), g.Vertices.IndexOf(EdgeFalse));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("return ({1})if({0})else({2})", Value.GenerateCodePython(g), g.Vertices.IndexOf(EdgeTrue), g.Vertices.IndexOf(EdgeFalse));
		}
	}
}
