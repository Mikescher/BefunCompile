using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;

namespace BefunCompile.Graph.Expression
{
	public abstract class BCExpression
	{
		public abstract String GetRepresentation();

		public abstract long Calculate(ICalculateInterface ci);

		public abstract IEnumerable<MemoryAccess> ListConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> ListDynamicVariableAccess();

		public abstract BCModArea GetSideEffects();
		public bool IsOutput()         => (GetSideEffects() & BCModArea.IO_Write    ) != BCModArea.None;
		public bool IsInput()          => (GetSideEffects() & BCModArea.IO_Read     ) != BCModArea.None;
		public bool IsStackAccess()    => (GetSideEffects() & BCModArea.Any_Stack   ) != BCModArea.None;
		public bool IsGridAccess()     => (GetSideEffects() & BCModArea.Any_Grid    ) != BCModArea.None;
		public bool IsVariableAccess() => (GetSideEffects() & BCModArea.Any_Variable) != BCModArea.None;
		public bool IsStateModifying() => (GetSideEffects() & BCModArea.Any_Write   ) != BCModArea.None;

		public abstract bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public abstract IEnumerable<ExpressionVariable> GetVariables();

		public abstract bool IsAlwaysLongReturn();

		public abstract string GenerateCode(CodeGenerator cg, bool forceLongReturn);

		public abstract BCExpression ReplaceUnstackify(UnstackifyValueAccess access);

		public override string ToString()
		{
			return GetRepresentation();
		}

		protected string Paren(string input, bool doParenthesis = true)
		{
			return doParenthesis ? ('(' + input + ')') : input;
		}

		public abstract bool IsIdentical(BCExpression other);
		public abstract bool IsConstant(int value);
	}
}
