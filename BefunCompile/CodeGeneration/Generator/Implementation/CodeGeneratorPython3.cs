using BefunCompile.Graph;
using BefunCompile.Graph.Vertex;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorPython3 : CodeGeneratorPython
	{
		public CodeGeneratorPython3(BCGraph comp, CodeGeneratorOptions options) 
			: base(comp, options)
		{
			// <EMPTY />
		}

		protected override string SHEBANG => @"#!/usr/bin/env python3";

		protected override IEnumerable<string> AdditionalImports => Enumerable.Empty<string>();

		protected override string GetBase64DecodeHeader() => @"import gzip, base64";

		protected override string GetGZipDecodeStatement()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"g = base64.b64decode(_g)[1:]");
			codebuilder.AppendLine(@"for i in range(base64.b64decode(_g)[0]):");
			codebuilder.AppendLine(@"    g = gzip.decompress(g)");
			codebuilder.AppendLine(@"g=list(g)");

			return codebuilder.ToString();
		}

		public override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp)
		{
			if (comp.ModeInteger)
				return string.Format("print({0},end=\" \",flush=True)", comp.Value.GenerateCode(this, false));
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", comp.Value.GenerateCode(this, false));
		}

		public override string GenerateCodeBCVertexOutput(BCVertexOutput comp)
		{
			if (comp.ModeInteger)
				return string.Format("print({0},end=\" \",flush=True)", "sp()");
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", "sp()");
		}

		public override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp)
		{
			return string.Format("print(\"{0}\",end=\"\",flush=True)", comp.Value);
		}
	}
}
