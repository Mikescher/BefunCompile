
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPush : BCVertex
	{
		public readonly long value;

		public BCVertexPush(BCDirection d, long val)
			: base(d)
		{
			this.value = val;
		}
	}
}
