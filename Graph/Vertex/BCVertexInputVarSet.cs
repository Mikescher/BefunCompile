using BefunCompile.CodeGeneration;
using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexInputVarSet : BCVertex, MemoryAccess
	{
		public ExpressionVariable Variable;
		public readonly bool ModeInteger; // true = int | false = char

		public BCVertexInputVarSet(BCDirection d, Vec2i pos, ExpressionVariable var, bool modeInt)
			: base(d, new [] { pos })
		{
			this.Variable = var;
			this.ModeInteger = modeInt;
		}

		public BCVertexInputVarSet(BCDirection d, Vec2i[] pos, ExpressionVariable var, bool modeInt)
			: base(d, pos)
		{
			this.Variable = var;
			this.ModeInteger = modeInt;
		}

		public override string ToString()
		{
			return string.Format("SET({0}) = IN({1})", Variable, ModeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexInputVarSet(Direction, Positions, Variable, ModeInteger);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			throw new GraphExecuteException();
		}

		public BCExpression getX()
		{
			return null;
		}

		public BCExpression getY()
		{
			return null;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getY();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(null), getY().Calculate(null));
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(Variable))
			{
				Variable = (ExpressionVariable)replacement(Variable);
				found = true;
			}
			if (Variable.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsOutput()
		{
			return false;
		}

		public override bool IsInput()
		{
			return true;
		}

		public override bool IsNotGridAccess()
		{
			return true;
		}

		public override bool IsNotStackAccess()
		{
			return true;
		}

		public override bool IsNotVariableAccess()
		{
			return false;
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
			return Variable.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexInputVarSet(l, this, g);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			return state.Clone();
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return this;
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexInputVarSet;

			if (arg == null) return false;

			return this.Variable.IsIdentical(arg.Variable) && this.ModeInteger == arg.ModeInteger;
		}
	}
}
