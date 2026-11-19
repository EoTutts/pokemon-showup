using System.Runtime.InteropServices;

namespace R36S.A11y;

internal static class Al
{
	internal const string Lib = "libopenal.so.1";

	internal const int AL_SOURCE_RELATIVE = 514;

	internal const int AL_PITCH = 4099;

	internal const int AL_POSITION = 4100;

	internal const int AL_LOOPING = 4103;

	internal const int AL_BUFFER = 4105;

	internal const int AL_GAIN = 4106;

	internal const int AL_ORIENTATION = 4111;

	internal const int AL_SOURCE_STATE = 4112;

	internal const int AL_PLAYING = 4114;

	internal const int AL_PAUSED = 4115;

	internal const int AL_REFERENCE_DISTANCE = 4128;

	internal const int AL_ROLLOFF_FACTOR = 4129;

	internal const int AL_MAX_DISTANCE = 4131;

	internal const int AL_FORMAT_MONO8 = 4352;

	internal const int AL_FORMAT_MONO16 = 4353;

	internal const int AL_FORMAT_STEREO8 = 4354;

	internal const int AL_FORMAT_STEREO16 = 4355;

	internal const int AL_INVERSE_DISTANCE_CLAMPED = 53250;

	internal const int ALC_HRTF_SOFT = 6546;

	internal const int AL_EFFECT_TYPE = 32769;

	internal const int AL_EFFECT_REVERB = 1;

	internal const int AL_EFFECTSLOT_EFFECT = 1;

	internal const int AL_AUXILIARY_SEND_FILTER = 131078;

	[DllImport("libopenal.so.1")]
	internal static extern nint alcOpenDevice(nint name);

	[DllImport("libopenal.so.1")]
	internal static extern nint alcCreateContext(nint device, int[]? attrs);

	[DllImport("libopenal.so.1")]
	internal static extern byte alcMakeContextCurrent(nint context);

	[DllImport("libopenal.so.1")]
	internal static extern byte alcIsExtensionPresent(nint device, string name);

	[DllImport("libopenal.so.1")]
	internal static extern void alcGetIntegerv(nint device, int param, int size, out int data);

	[DllImport("libopenal.so.1")]
	internal static extern void alcDestroyContext(nint context);

	[DllImport("libopenal.so.1")]
	internal static extern byte alcCloseDevice(nint device);

	[DllImport("libopenal.so.1")]
	internal static extern void alDistanceModel(int model);

	[DllImport("libopenal.so.1")]
	internal static extern void alGenBuffers(int n, out uint buffer);

	[DllImport("libopenal.so.1")]
	internal static extern void alBufferData(uint buffer, int format, byte[] data, int size, int freq);

	[DllImport("libopenal.so.1")]
	internal static extern void alGenSources(int n, out uint source);

	[DllImport("libopenal.so.1")]
	internal static extern void alSourcei(uint source, int param, int value);

	[DllImport("libopenal.so.1")]
	internal static extern void alSourcef(uint source, int param, float value);

	[DllImport("libopenal.so.1")]
	internal static extern void alSource3f(uint source, int param, float x, float y, float z);

	[DllImport("libopenal.so.1")]
	internal static extern void alSource3i(uint source, int param, int a, int b, int c);

	[DllImport("libopenal.so.1")]
	internal static extern void alGetSourcei(uint source, int param, out int value);

	[DllImport("libopenal.so.1")]
	internal static extern void alSourcePlay(uint source);

	[DllImport("libopenal.so.1")]
	internal static extern void alSourceStop(uint source);

	[DllImport("libopenal.so.1")]
	internal static extern void alSourcePause(uint source);

	[DllImport("libopenal.so.1")]
	internal static extern void alListener3f(int param, float x, float y, float z);

	[DllImport("libopenal.so.1")]
	internal static extern void alListenerfv(int param, float[] values);

	[DllImport("libopenal.so.1")]
	internal static extern void alGenAuxiliaryEffectSlots(int n, out uint slot);

	[DllImport("libopenal.so.1")]
	internal static extern void alGenEffects(int n, out uint effect);

	[DllImport("libopenal.so.1")]
	internal static extern void alEffecti(uint effect, int param, int value);

	[DllImport("libopenal.so.1")]
	internal static extern void alEffectf(uint effect, int param, float value);

	[DllImport("libopenal.so.1")]
	internal static extern void alAuxiliaryEffectSloti(uint slot, int param, int value);
}
