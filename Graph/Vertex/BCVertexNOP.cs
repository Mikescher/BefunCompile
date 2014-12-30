﻿
using BefunCompile.Math;
namespace BefunCompile.Graph.Vertex
{
	public class BCVertexNOP : BCVertex
	{
		public BCVertexNOP(BCDirection d, Vec2i pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "NOP";
		}
	}
}
