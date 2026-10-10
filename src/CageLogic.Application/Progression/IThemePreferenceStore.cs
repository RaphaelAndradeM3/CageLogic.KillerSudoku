namespace CageLogic.Application.Progression;

/// <summary>Local boundary for the optional player-selected appearance preference.</summary>
public interface IThemePreferenceStore
{
	AppThemePreference? Get();
	void Set(AppThemePreference? preference);
}

public enum AppThemePreference
{
	Light,
	Dark
}
