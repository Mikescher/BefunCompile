﻿
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPop : BCVertex
	{
		public BCVertexPop(BCDirection d, Vec2i pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "POP";
		}
	}
}
