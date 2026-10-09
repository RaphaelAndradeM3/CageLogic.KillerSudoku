using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using CageLogic.Maui.Controls;

namespace CageLogic.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
	{
		if (BoardInputBehavior.TryHandleHardwareKey(keyCode.ToString(), e?.IsCtrlPressed == true))
			return true;

		return base.OnKeyDown(keyCode, e);
	}
}
