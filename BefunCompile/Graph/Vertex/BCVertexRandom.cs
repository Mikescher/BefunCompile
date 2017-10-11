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
	public class BCVertexRandom : BCVertex
	{
		public BCVertexRandom(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public BCVertexRandom(BCDirection d, Vec2i pos)
			: base(d, new [] { pos })
		{

		}

		public override string ToString()
		{
			return "<~RAND~>";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexRandom(Direction, Positions);
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
			if (Children.Count != 4)
				throw new ArgumentException("#");
			return Children[new Random().Next(4)];
		}

		public override int? GetStacksizePredictorDelta()
		{
			return 0;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override BCModArea GetSideEffects()
		{
			return BCModArea.None;
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
			return true;
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Children.Select(child => g.Vertices.IndexOf(child));
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexRandom(this);
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
			var arg = other as BCVertexRandom;

			if (arg == null) return false;

			return true;
		}
	}
}
