using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprDecision : BCVertex, IDecisionVertex
	{
		public BCVertex EdgeTrue { get; set; }
		public BCVertex EdgeFalse { get; set; }

		public BCExpression Value;

		public BCVertexExprDecision(BCDirection d, Vec2i[] pos, BCVertex childTrue, BCVertex childFalse, BCExpression val)
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
			return new BCVertexExprDecision(Direction, Positions, EdgeTrue, EdgeFalse, Value);
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

		public override bool IsNotGridAccess()
		{
			return Value.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return Value.IsNotStackAccess();
		}

		public override bool IsNotVariableAccess()
		{
			return Value.IsNotVariableAccess();
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

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			yield return g.Vertices.IndexOf(EdgeTrue);
			yield return g.Vertices.IndexOf(EdgeFalse);
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			int vtrue = g.Vertices.IndexOf(EdgeTrue);
			int vfalse = g.Vertices.IndexOf(EdgeFalse);

			var ExprBinMathValue = Value as ExpressionBinMath;
			var ExprNotValue = Value as ExpressionNot;

			if (ExprBinMathValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", ExprBinMathValue.GenerateDecisionCodeCSharp(g), vtrue, vfalse);
			else if (ExprNotValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", ExprNotValue.GenerateDecisionCodeCSharp(g), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeCSharp(g), vtrue, vfalse);
		}

		public override string GenerateCodeC(BCGraph g)
		{
			int vtrue = g.Vertices.IndexOf(EdgeTrue);
			int vfalse = g.Vertices.IndexOf(EdgeFalse);

			var ExprBinMathValue = Value as ExpressionBinMath;
			var ExprNotValue = Value as ExpressionNot;

			if (ExprBinMathValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", ExprBinMathValue.GenerateDecisionCodeC(g), vtrue, vfalse);
			else if (ExprNotValue != null)
				return string.Format("if({0})goto _{1};else goto _{2};", ExprNotValue.GenerateDecisionCodeC(g), vtrue, vfalse);
			else
				return string.Format("if(({0})!=0)goto _{1};else goto _{2};", Value.GenerateCodeC(g), vtrue, vfalse);
		}

		public override string GenerateCodePython(BCGraph g)
		{
			int vtrue = g.Vertices.IndexOf(EdgeTrue);
			int vfalse = g.Vertices.IndexOf(EdgeFalse);

			var ExprBinMathValue = Value as ExpressionBinMath;
			var ExprNotValue = Value as ExpressionNot;

			if (ExprBinMathValue != null)
				return string.Format("return ({1})if({0})else({2})", ExprBinMathValue.GenerateDecisionCodePython(g), vtrue, vfalse);
			else if (ExprNotValue != null)
				return string.Format("return ({1})if({0})else({2})", ExprNotValue.GenerateDecisionCodePython(g), vtrue, vfalse);
			else
				return string.Format("return ({1})if(({0})!=0)else({2})", Value.GenerateCodePython(g), vtrue, vfalse);
		}
	}
}
