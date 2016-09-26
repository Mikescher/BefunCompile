
using BefunCompile.Graph.Expression;
using BefunCompile.Math;
namespace BefunCompile.Graph
{
	public interface MemoryAccess
	{
		BCExpression getX();
		BCExpression getY();

		Vec2l getConstantPos();
	}
}
