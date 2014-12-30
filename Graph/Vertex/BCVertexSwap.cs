﻿
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexSwap : BCVertex
	{
		public BCVertexSwap(BCDirection d, Vec2i pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SWAP";
		}
	}
}
