namespace R36S.A11y;

public sealed class Sound
{
	private readonly Audio audio;

	internal uint Buffer;

	public string Name { get; }

	public int Channels { get; internal set; } = 1;

	internal Sound(Audio audio, string name)
	{
		this.audio = audio;
		Name = name;
	}

	public Voice Play(bool loop = false, double volume = 1.0, double pitch = 1.0, (double X, double Y, double Z)? pos = null, double? pan = null, bool? reverb = null)
	{
		return audio.Play(this, loop, volume, pitch, pos, pan, reverb);
	}
}
