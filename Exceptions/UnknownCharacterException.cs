
namespace BefunCompile.Exceptions
{
	public class UnknownCharacterException : CodeParseException
	{
		public UnknownCharacterException(long c)
			: base("Unknown character: " + (long)c)
		{

		}
	}
}
