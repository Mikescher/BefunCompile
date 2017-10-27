
using System.Collections.Generic;

namespace BefunCompile.Graph.Vertex
{
	interface IBlockVertex
	{
		IEnumerable<BCVertex> GetSubVertices();
	}
}
