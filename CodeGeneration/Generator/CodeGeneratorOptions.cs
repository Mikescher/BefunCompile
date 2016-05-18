namespace BefunCompile.CodeGeneration.Generator
{
	public class CodeGeneratorOptions
	{
		public readonly bool FormatOutput;
		public readonly bool ImplementSafeStackAccess;
		public readonly bool ImplementSafeGridAccess;
		public readonly bool UseGZip;
		public readonly bool AddCosmeticChoices;

		public CodeGeneratorOptions(bool fmt, bool ssa, bool sga, bool gz, bool cc)
		{
			FormatOutput = fmt;
			ImplementSafeStackAccess = ssa;
			ImplementSafeGridAccess = sga;
			UseGZip = gz;
			AddCosmeticChoices = cc;
		}
	}
}
