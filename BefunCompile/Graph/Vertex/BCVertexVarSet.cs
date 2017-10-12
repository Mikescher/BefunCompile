using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexVarSet : BCVertex, MemoryAccess
	{
		public ExpressionVariable Variable;

		public BCVertexVarSet(BCDirection d, Vec2i pos, ExpressionVariable var)
			: base(d, new [] { pos })
		{
			this.Variable = var;
		}

		public BCVertexVarSet(BCDirection d, Vec2i[] pos, ExpressionVariable var)
			: base(d, pos)
		{
			this.Variable = var;
		}

		public override string ToString()
		{
			return "SET(" + Variable.GetRepresentation() + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexVarSet(Direction, Positions, Variable);
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
			ci.SetVariableValue(Variable, stackbuilder.Pop());

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public override int? GetStacksizePredictorDelta()
		{
			return -1;
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

		public override BCModArea GetSideEffects()
		{
			return BCModArea.Stack_Read | BCModArea.Stack_Write | BCModArea.Variable_Write;
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

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexVarSet(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Pop().AddAccess(this, UnstackifyValueAccessType.READ);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexExprVarSet(Direction, Positions, Variable, access.Single().Value.Replacement);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexVarSet;

			if (arg == null) return false;

			return this.Variable.IsIdentical(arg.Variable);
		}
	}
}
