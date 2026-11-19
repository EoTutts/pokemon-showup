using System.Runtime.InteropServices;

namespace R36S.A11y;

internal static class Vorbis
{
	private const string Lib = "libvorbisfile.so.3";

	[DllImport("libvorbisfile.so.3")]
	internal static extern int ov_fopen(string path, nint vf);

	[DllImport("libvorbisfile.so.3")]
	internal static extern nint ov_info(nint vf, int link);

	[DllImport("libvorbisfile.so.3")]
	internal static extern long ov_read(nint vf, byte[] buffer, int length, int bigEndian, int word, int signed, out int bitstream);

	[DllImport("libvorbisfile.so.3")]
	internal static extern int ov_clear(nint vf);
}
