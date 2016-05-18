using BefunCompile.Graph;
using BefunCompile.Graph.Vertex;
using System.Collections.Generic;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorPython2 : CodeGeneratorPython
	{
		public CodeGeneratorPython2(BCGraph comp, CodeGeneratorOptions options) 
			: base(comp, options)
		{
			// <EMPTY />
		}

		protected override string SHEBANG => @"#!/usr/bin/env python2";
		
		protected override IEnumerable<string> AdditionalImports => new[] { @"import sys" };

		protected override string GetBase64DecodeHeader() => @"import zlib, base64";

		protected override string GetGZipDecodeStatement()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"g = base64.b64decode(_g)[1:]");
			codebuilder.AppendLine(@"for i in range(ord(base64.b64decode(_g)[0])):");
			codebuilder.AppendLine(@"    g = zlib.decompress(g, 16+zlib.MAX_WBITS)");
			codebuilder.AppendLine(@"g=list(map(ord, g))");

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp)
		{
			var builder = new SourceCodeBuilder();

			if (comp.ModeInteger)
				builder.AppendLine("sys.stdout.write(str({0}))", comp.Value.GenerateCode(this, false));
			else
				builder.AppendLine("sys.stdout.write(chr({0}))", comp.Value.GenerateCode(this, false));

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexOutput(BCVertexOutput comp)
		{
			var builder = new SourceCodeBuilder();

			if (comp.ModeInteger)
				builder.AppendLine("sys.stdout.write(str({0}))", "sp()");
			else
				builder.AppendLine("sys.stdout.write(chr({0}))", "sp()");

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine("sys.stdout.write(\"{0}\")", comp.Value);

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}
	}
}
