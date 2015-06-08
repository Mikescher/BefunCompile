
namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public enum UnstackifyValueAccessModifier
	{
		NONE,
		LEFT_EXPR,
		RIGHT_EXPR,

		EXPR_GRIDX,
		EXPR_GRIDY,
		EXPR_VALUE,
	}

	public enum UnstackifyValueAccessType
	{
		READ,
		WRITE,
		READWRITE,
		REMOVE,
	}

	public class UnstackifyValueAccess
	{
		public readonly BCVertex Vertex;
		public readonly UnstackifyValueAccessType Type;
		public readonly UnstackifyValueAccessModifier Modifier;

		public UnstackifyValue Value;

		public UnstackifyValueAccess(BCVertex v, UnstackifyValueAccessType t)
		{
			Vertex = v;
			Type = t;
			Modifier = UnstackifyValueAccessModifier.NONE;
		}

		public UnstackifyValueAccess(BCVertex v, UnstackifyValueAccessType t, UnstackifyValueAccessModifier m)
		{
			Vertex = v;
			Type = t;
			Modifier = m;
		}
	}
}
