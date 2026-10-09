using CageLogic.Application.Progression;

namespace CageLogic.Application.Tests.Progression;

public sealed class ThemePreferenceUseCaseTests
{
	[Test]
	public void MissingValue_ReturnsNullToFollowSystemTheme()
	{
		var store = new FakeThemePreferenceStore();
		var useCase = new ThemePreferenceUseCase(store);

		var result = useCase.Get();

		Assert.That(result, Is.Null);
		Assert.That(store.Value, Is.Null);
	}

	[TestCase(AppThemePreference.Light)]
	[TestCase(AppThemePreference.Dark)]
	public void SaveAndLoad_PreservesTheSelectedTheme(AppThemePreference preference)
	{
		var store = new FakeThemePreferenceStore();
		var useCase = new ThemePreferenceUseCase(store);

		useCase.Set(preference);
		var result = useCase.Get();

		Assert.That(result, Is.EqualTo(preference));
		Assert.That(store.Value, Is.EqualTo(preference));
	}

	[Test]
	public void Save_RejectsUnknownThemeValues()
	{
		var useCase = new ThemePreferenceUseCase(new FakeThemePreferenceStore());

		Assert.Throws<ArgumentOutOfRangeException>(() => useCase.Set((AppThemePreference)999));
	}

	private sealed class FakeThemePreferenceStore : IThemePreferenceStore
	{
		public AppThemePreference? Value { get; private set; }

		public AppThemePreference? Get() => Value;

		public void Set(AppThemePreference? preference) => Value = preference;
	}
}
