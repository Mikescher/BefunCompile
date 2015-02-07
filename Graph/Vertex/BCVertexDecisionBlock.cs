using BefunCompile.Graph.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexDecisionBlock : BCVertex, IDecisionVertex
	{
		public readonly BCVertexBlock Block;
		public readonly BCVertexDecision Decision;

		public BCVertex EdgeTrue
		{
			get { return Decision.EdgeTrue; }
			set { Decision.EdgeTrue = value; }
		}

		public BCVertex EdgeFalse
		{
			get { return Decision.EdgeFalse; }
			set { Decision.EdgeFalse = value; }
		}

		public BCVertexDecisionBlock(BCDirection d, BCVertexBlock block, BCVertexDecision dec)
			: base(d, block.Positions.Concat(dec.Positions).ToArray())
		{
			Block = block;
			Decision = dec;
		}

		public override string ToString()
		{
			return Block + Environment.NewLine + Decision;
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullDecisionBlock(Direction, (BCVertexBlock)Block.Duplicate(), (BCVertexFullDecision)Decision.Duplicate());
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Block.ListConstantVariableAccess().Concat(Decision.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Block.ListDynamicVariableAccess().Concat(Decision.ListConstantVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			Block.Execute(outbuilder, stackbuilder, ci);

			return Decision.Execute(outbuilder, stackbuilder, ci);
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return Block.SubsituteExpression(prerequisite, replacement) | Decision.SubsituteExpression(prerequisite, replacement);
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
			return true;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return Block.GenerateCodeCSharp(g) + Environment.NewLine + Decision.GenerateCodeCSharp(g);
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return Block.GenerateCodeC(g) + Environment.NewLine + Decision.GenerateCodeC(g);
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return Block.GenerateCodePython(g) + Environment.NewLine + Decision.GenerateCodePython(g);
		}
	}
}
