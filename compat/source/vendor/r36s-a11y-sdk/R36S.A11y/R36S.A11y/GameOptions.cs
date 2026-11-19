namespace R36S.A11y;

public sealed record GameOptions
{
	public int Fps { get; init; } = 60;

	public bool Repeat { get; init; }

	public bool Axes { get; init; }

	public double Deadzone { get; init; } = 0.2;

	public bool EnableAudio { get; init; } = true;

	public bool EnableInput { get; init; } = true;
}
