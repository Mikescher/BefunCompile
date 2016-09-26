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
	public class BCVertexNot : BCVertex
	{
		public BCVertexNot(BCDirection d, Vec2i pos)
			: base(d, new[] { pos })
		{

		}

		private BCVertexNot(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "NOT";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexNot(Direction, Positions);
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
			stackbuilder.Push(!stackbuilder.PopBool());

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public override int? GetStacksizePredictorDelta()
		{
			return 0;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool IsOutput()
		{
			return false;
		}

		public override bool IsInput()
		{
			return false;
		}

		public override bool IsNotGridAccess()
		{
			return true;
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return true;
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
			return cg.GenerateCodeBCVertexNot(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Peek().AddAccess(this, UnstackifyValueAccessType.READWRITE);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexExprVarSet(Direction, Positions, access.Single().Value.Replacement, ExpressionNot.Create(access.Single().Value.Replacement));
		}

		public override bool IsIdentical(BCVertex other)
		{
			return other is BCVertexNot;
		}
	}
}
