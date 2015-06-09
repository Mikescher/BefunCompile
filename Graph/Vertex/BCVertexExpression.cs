using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExpression : BCVertex
	{
		public BCExpression Expression;

		public BCVertexExpression(BCDirection d, Vec2i pos, BCExpression expr)
			: base(d, pos)
		{
			Expression = expr;
		}

		public BCVertexExpression(BCDirection d, Vec2i[] pos, BCExpression expr)
			: base(d, pos)
		{
			Expression = expr;
		}

		public BCVertexExpression(BCDirection d, Vec2i pos, BCExpression expr_left, BinaryMathType type, BCExpression expr_right)
			: base(d, pos)
		{
			Expression = ExpressionBinMath.Create(expr_left, expr_right, type);
		}

		public BCVertexExpression(BCDirection d, Vec2i[] pos, BCExpression expr_left, BinaryMathType type, BCExpression expr_right)
			: base(d, pos)
		{
			Expression = ExpressionBinMath.Create(expr_left, expr_right, type);
		}

		public override string ToString()
		{
			return "PUSH(" + Expression + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExpression(Direction, Positions, Expression);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Expression.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Expression.ListDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			stackbuilder.Push(Expression.Calculate(ci));
			return Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(Expression))
			{
				Expression = replacement(Expression);
				found = true;
			}

			if (Expression.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsNotGridAccess()
		{
			return Expression.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return Expression.IsNotVariableAccess();
		}

		public override bool IsCodePathSplit()
		{
			return false;
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
			return Expression.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("sa({0});", Expression.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("sa({0});", Expression.GenerateCodeC(g));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("sa({0})", Expression.GenerateCodePython(g));
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (Expression.IsNotStackAccess())
			{
				state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));
			}
			else
			{
				state.Peek().AddAccess(this, UnstackifyValueAccessType.READ);
				state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));
			}

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_write = access.Single(p => p.Type == UnstackifyValueAccessType.WRITE);
			var var_read = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ);

			if (var_read != null)
			{
				return new BCVertexExprVarSet(Direction, Positions, var_write.Value.Replacement, Expression.ReplaceUnstackify(var_read));
			}
			else
			{
				return new BCVertexExprVarSet(Direction, Positions, var_write.Value.Replacement, Expression);
			}
		}
	}
}
