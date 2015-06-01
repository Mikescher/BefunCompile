using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BefunCompile.Math
{
	public class GZipImplementation : CommonZipImplementation
	{
		public override List<byte> Compress(List<byte> ldata, ref int compressioncount)
		{
			byte[] data = ldata.ToArray();

			compressioncount = 0;
			while (compressioncount < 32)
			{
				byte[] compress = CompressSingle(data);

				if (compress.Length >= data.Count())
					break;

				data = compress;
				compressioncount++;
			}

			return new[] { (byte)compressioncount }.Concat(data).ToList();
		}

		public byte[] CompressSingle(byte[] raw)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
				{
					gzip.Write(raw, 0, raw.Length);
				}
				return memory.ToArray();
			}
		}

		public override List<byte> Decompress(List<byte> x, int resultsize)
		{
			byte[] d = x.Skip(1).ToArray();

			for (int i = 0; i < x[0]; i++)
				d = DecompressSingle(d);

			return d.ToList();
		}

		private static byte[] DecompressSingle(byte[] o)
		{
			using (var c = new MemoryStream(o))
			using (var z = new GZipStream(c, CompressionMode.Decompress))
			using (var r = new MemoryStream())
			{
				z.CopyTo(r);
				return r.ToArray();
			}
		}
	}
}
