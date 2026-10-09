using CageLogic.Application.GameSessions;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionTimerTests
{
	[Test]
	public void BackgroundPause_ExcludesInactiveTime_AndRequiresExplicitResume()
	{
		var timeProvider = new ManualTimeProvider();
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle(), timeProvider: timeProvider);
		Assert.That(session.IsPaused, Is.True);
		timeProvider.Advance(TimeSpan.FromSeconds(30));
		Assert.That(session.ActiveElapsed, Is.EqualTo(TimeSpan.Zero));
		session.Start();
		timeProvider.Advance(TimeSpan.FromSeconds(12));

		session.PauseForBackground();
		timeProvider.Advance(TimeSpan.FromMinutes(2));

		Assert.That(session.IsPaused, Is.True);
		Assert.That(session.ActiveElapsed, Is.EqualTo(TimeSpan.FromSeconds(12)));

		session.Resume();
		timeProvider.Advance(TimeSpan.FromSeconds(8));
		Assert.That(session.IsPaused, Is.False);
		Assert.That(session.ActiveElapsed, Is.EqualTo(TimeSpan.FromSeconds(20)));
	}

	[Test]
	public void PauseIsIdempotent_BackgroundTimeIsExcluded_AndResumeIsExplicit()
	{
		var timeProvider = new ManualTimeProvider();
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle(), timeProvider: timeProvider);
		session.Start();
		timeProvider.Advance(TimeSpan.FromSeconds(35));
		session.Pause();
		session.Pause();
		timeProvider.Advance(TimeSpan.FromMinutes(4));
		session.Pause();
		Assert.That(session.ActiveElapsed, Is.EqualTo(TimeSpan.FromSeconds(35)));
		Assert.That(session.IsPaused, Is.True);

		session.Resume();
		session.Resume();
		timeProvider.Advance(TimeSpan.FromSeconds(25));
		Assert.That(session.ActiveElapsed, Is.EqualTo(TimeSpan.FromSeconds(60)));
		Assert.That(session.IsPaused, Is.False);
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private long _timestamp;

		public override long TimestampFrequency => TimeSpan.TicksPerSecond;

		public override long GetTimestamp() => _timestamp;

		public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
	}
}
