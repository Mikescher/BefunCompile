using BefunCompile.Graph.Expression;

namespace BefunCompile.Graph
{
	public interface CalculateInterface
	{
		long GetVariableValue(ExpressionVariable v);
		long GetGridValue(long xx, long yy);

		void SetVariableValue(ExpressionVariable v, long value);
		void SetGridValue(long xx, long yy, long value);

		long PeekStack();
	}
}
