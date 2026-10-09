namespace CageLogic.Application.Progression;

/// <summary>Loads or saves the user's optional Light/Dark preference; no value means follow the system.</summary>
public sealed class ThemePreferenceUseCase(IThemePreferenceStore store)
{
	public AppThemePreference? Get() => store.Get();

	public void Set(AppThemePreference? preference)
	{
		if (preference.HasValue && !Enum.IsDefined(preference.Value))
			throw new ArgumentOutOfRangeException(nameof(preference));
		store.Set(preference);
	}
}
