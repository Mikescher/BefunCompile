
namespace BefunCompile.Graph.Vertex
{
	interface IDecisionVertex
	{
		BCVertex EdgeTrue { get; set; }
		BCVertex EdgeFalse { get; set; }
	}
}
