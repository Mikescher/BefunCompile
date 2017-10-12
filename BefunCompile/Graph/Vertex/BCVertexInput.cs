using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
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
	public class BCVertexInput : BCVertex
	{
		public readonly bool ModeInteger; // true = int | false = char

		public BCVertexInput(BCDirection d, Vec2i pos, long mode)
			: base(d, new Vec2i[] { pos })
		{
			ModeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, long mode)
			: base(d, pos)
		{
			ModeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, bool mode)
			: base(d, pos)
		{
			ModeInteger = mode;
		}

		public override string ToString()
		{
			return string.Format("IN({0})", ModeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexInput(Direction, Positions, ModeInteger);
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

		public override int? GetStacksizePredictorDelta()
		{
			return 1;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override BCModArea GetSideEffects()
		{
			return BCModArea.IO_Read | BCModArea.Stack_Write;
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
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexInput(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexInputVarSet(Direction, Positions, access.Single().Value.Replacement, ModeInteger);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexInput;

			if (arg == null) return false;

			return this.ModeInteger == arg.ModeInteger;
		}
	}
}
