# ThemeManager

> **Please Note:** All code surrounded by `[ ]` needs to be replaced with the correct value by you.  

## What is the ThemeManager
The `ThemeManager` provides several options to provide themes for your application or control library. For example [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) and [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon) use it to provide different themes to the user.

In the following sections the usage will be described by the sample usage at [MahApps.Metro](https://github.com/MahApps/MahApps.Metro).

## Create built in themes

### Using XamlColorSchemeGenerator
You can provide built in themes in your application or library by running the [XamlColorSchemeGenerator](https://github.com/batzen/XamlColorSchemeGenerator) while building or via command line. For detailed information about the usage, please visit the [XamlColorSchemeGenerator site](https://github.com/batzen/XamlColorSchemeGenerator).

### Using ResourceDicitionaries
> **Please note:** It is not recommended to use this approach as you might miss the addition of new required resources when new versions of libraries are released.

You may also provide a custom `ResourceDictionary` with all the needed resources.
1. Get yourself a copy of the required `Template`
    * **MahApps.Metro**: https://github.com/MahApps/MahApps.Metro/blob/main/src/MahApps.Metro/Styles/Themes/Theme.Template.xaml
    * **Fluent.Ribbon**: https://github.com/fluentribbon/Fluent.Ribbon/blob/master/Fluent.Ribbon/Themes/Themes/Theme.Template.xaml
2. Replace all items surrounded by double curly braces `{{ Content to Replace }}`
3. Remember to create all needed variants. E.g.: If you want to support light and dark themes you will need to implement both.
  
    
## Changing the theme at runtime

### Change the theme using a built in theme
First of all you need to add the following namespace in your using section: 
```c#
using ControlzEx.Theming;
```

now you can apply the `Theme` of your choice to your `App`, to a specific `Window` or any `FrameworkElement`. Just call this line in your code: 
```c#
ThemeManager.Current.ChangeTheme([Affected item], "[Name of the theme]");
```

Where `[Affected item]` may be:
- `Application.Current`
- `this`
- any `Window`
- any `Control`
- any `FrameworkElement`

and `[Name of the theme]` is any valid theme name, e.g. `Dark.Blue` in MahApps.Metro.

The `ThemeManager` can be called from different locations, for example: 
- Inside the constructor of your `App.xaml.cs`
- Inside the constructor of your `Window`
- In a `Click-Event` of a `Button`
- In your `ViewModel`

**Example** 

If you want, for example, set your application theme to `Dark.Red`, when the user clicks a button called "ButtonChangeTheme" you can do this: 
```c#
private void ButtonChangeTheme_Click(object sender, RoutedEventArgs e)
{
    ThemeManager.Current.ChangeTheme(App.Current, "Dark.Red");
}
```

### Sync your theme with the Windows theme
It is also possible to sync your `Theme` with the users Windows theme. The first step is again to add the following namespace in your using section: 
```c#
using ControlzEx.Theming;
```

Now you need to define if you want to sync the `BaseColor`, the `Accent` or both: 
```c#
ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.[DoNotSync|SyncAll|SyncWithAccent|SyncWithAppMode|SyncWithHighContrast];
```

The table below explains the available `ThemeSyncModes`:

| ThemeSyncMode        | Description                              |
|----------------------|------------------------------------------|
| DoNotSync            | No sync at all                           |
| SyncAll              | Gets or sets whether changes to the "app mode", accent color and high contrast setting from windows should be detected at runtime and the current Theme be changed accordingly. |
| SyncWithAppMode      | Gets or sets whether changes to the "app mode" setting from windows should be detected at runtime and the current Theme be changed accordingly. |
| SyncWithAccent       | Gets or sets whether changes to the accent color settings from windows should be detected at runtime and the current Theme be changed accordingly. |
| SyncWithHighContrast | Gets or sets whether changes to the high contrast setting from windows should be detected at runtime and the current Theme be changed accordingly. |

> Explanation: "app mode" is the windows terminology for base color like "dark" or "light".

> ***Please note:*** Once you enable theme sync you should not change the theme manually as manual changes would be overwritten as soon as some event occurs that triggers a theme sync. Such events could be triggered by various things like changes to display settings, connecting/disconnecting via RDP etc.. 

To actually sync the `Theme` call this line: 
```c#
ThemeManager.Current.SyncTheme();
```

> Note: If you want to sync just a part of the `Theme` and use a custom `Theme` make sure that the `Theme` is correctly added to the `ThemeManager`. 

### Change the theme by creating a custom one
First of all you need to add the following namespace in your using section: 
```c#
using ControlzEx.Theming;
```

1. Optional: If you wish to use `HSL` to create the `AccentColors` instead of semi-transparent colors, you can set this in the options:
    ```c#
    RuntimeThemeGenerator.Current.Options.UseHSL = [true/false];
    ```

2. Now you can create a new `Theme` by providing the `Color` you wish:
    ```c#
    Theme newTheme = new Theme("[Name of the generated theme]",
                               "[DisplayName of the generated theme]",
                               [ThemeManager.BaseColorLight or ThemeManager.BaseColorDark],
                               "[AccentName]",
                               [AccentColor],
                               new SolidColorBrush(AccentColor),
                               true,
                               [IsHighContast: false/true]);
    ```
    Adjust all parameters surrounded by `[]` in the above snipped to your needs.
    
3. Optional: Add or override any `Resources` in the new `Theme` if needed, e.g.:
    ```c# 
    newTheme.Resources["MahApps.Colors.Highlight"] = HighlightColor;
    newTheme.Resources["MahApps.Brushes.Highlight"] = new SolidColorBrush(HighlightColor);
    ```
    
4. Optional: Add the `Theme` to the current `ThemeMangers` collection:
    ```c#
    ThemeManager.Current.AddTheme(newTheme);
    ```
    
5. Apply new `Theme`
    ```c#
    ThemeManager.Current.ChangeTheme([Affected item], newTheme);
    ```
    Where `[Affected item]` may be:
    - `Application.Current`
    - `this`
    - any `Window`
    - any `Control`
    - any `FrameworkElement`

    
### Override the LibraryThemeProvider
If you want to have even more control about the theme generation you can override the `LibraryThemeProvider` with your own provider. In the following sample we will show how to do this for MahApps.Metro.

1. Create a new `class` which derives from a `LibaryThemeProvider`, in our case `MahAppsLibraryThemeProvider`
    ```c#
    public class MyLibraryThemeProvider : MahAppsLibraryThemeProvider
    ```
2. Override the `static DefaultInstance`
    ```c#
    public static new readonly MyLibraryThemeProvider DefaultInstance = new MyLibraryThemeProvider();
    ```
3. Override the Methods you like (most likely you want to override this):
    ```c#
    public override void FillColorSchemeValues(Dictionary<string, string> values, RuntimeThemeColorValues colorValues)
    {
        // Check if all needed parameters are not null
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (colorValues is null) throw new ArgumentNullException(nameof(colorValues));

        // Add the values you like to override
        values.Add("MahApps.Colors.AccentBase", "[AccentBaseColor]");
        values.Add("MahApps.Colors.Accent",     "[AccentColor]");
        values.Add("MahApps.Colors.Accent2",    "[AccentColor2]");
        values.Add("MahApps.Colors.Accent3",    "[AccentColor3]");
        values.Add("MahApps.Colors.Accent4",    "[AccentColor4]");
    
        values.Add("MahApps.Colors.Highlight",  "[HighlightColor]");
        values.Add("MahApps.Colors.IdealForeground", colorValues.IdealForegroundColor.ToString(CultureInfo.InvariantCulture));
    }
    ```
4. Register your provider to your `App` or in `Generic.xaml` of your controls library
    a. add the following namespace:
    ```xaml
    xmlns:theming="clr-namespace:BIA_Controls.Theming"
    ```
    b. add this line in the `Resources` section: 
    ```xaml
    <theming:MyLibraryThemeProvider x:Key="{x:Static theming:MyLibraryThemeProvider.DefaultInstance}" />
    ```
    
Below is a complete sample with some customization:
- If the user selects the `Light` theme, the accent colors have the same appearance as the transparent ones, but they are solid.
- If the user selects the `Dark` theme, the accent colors are a bit brighter than in the original implementation.
- The `HighlightColor` is calculated differently
- The gray shades are calculated to be equally distributed from black to white

```c#
public class MyLibraryThemeProvider : MahAppsLibraryThemeProvider
{
    /// <inheritdoc/>
    public static new readonly MyLibraryThemeProvider DefaultInstance = new MyLibraryThemeProvider();

    public override void FillColorSchemeValues(Dictionary<string, string> values, RuntimeThemeColorValues colorValues)
    {
        // Check if all needed parameters are not null
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (colorValues is null) throw new ArgumentNullException(nameof(colorValues));

        bool isDarkMode = colorValues.Options.BaseColorScheme.Name == ThemeManager.BaseColorDark;
        Color baseColor = (Color)ColorConverter.ConvertFromString(colorValues.Options.BaseColorScheme.Values["MahApps.Colors.ThemeBackground"]);
        Color accent = colorValues.AccentBaseColor;
        double factor = isDarkMode ? 0.1 : 0.2;

        // Add the values you like to override
        values.Add("MahApps.Colors.AccentBase", accent.ToString(CultureInfo.InvariantCulture));
        values.Add("MahApps.Colors.Accent", AddColor(accent, baseColor, factor * 1).ToString(CultureInfo.InvariantCulture));
        values.Add("MahApps.Colors.Accent2", AddColor(accent, baseColor, factor * 2).ToString(CultureInfo.InvariantCulture));
        values.Add("MahApps.Colors.Accent3", AddColor(accent, baseColor, factor * 3).ToString(CultureInfo.InvariantCulture));
        values.Add("MahApps.Colors.Accent4", AddColor(accent, baseColor, factor * 4).ToString(CultureInfo.InvariantCulture));

        values.Add("MahApps.Colors.Highlight", AddColor(accent, isDarkMode ? Colors.White : Colors.Black, 0.8).ToString(CultureInfo.InvariantCulture));
        values.Add("MahApps.Colors.IdealForeground", colorValues.IdealForegroundColor.ToString(CultureInfo.InvariantCulture));

        // Gray Colors
        for (int i = 1; i <= 10; i++)
        {
            values.Add($"MahApps.Colors.Gray{i}", GetShadedGray(i / 11d, isDarkMode).ToString(CultureInfo.InvariantCulture));
        }
    }

    private static Color GetShadedGray(double percentage, bool inverse = false)
    {
        if (inverse)
        {
            percentage = 1 - percentage;
        }

        return Color.FromRgb((byte)(percentage * 255), (byte)(percentage * 255), (byte)(percentage * 255));
    }

    private static Color AddColor(Color baseColor, Color colorToAdd, double? factor)
    {
        byte firstColorAlpha = baseColor.A;
        byte secondColorAlpha = factor.HasValue ? (byte)(factor * 255) : colorToAdd.A;

        byte alpha = CompositeAlpha(firstColorAlpha, secondColorAlpha);

        byte r = CompositeColorComponent(baseColor.R, firstColorAlpha, colorToAdd.R, secondColorAlpha, alpha);
        byte g = CompositeColorComponent(baseColor.G, firstColorAlpha, colorToAdd.G, secondColorAlpha, alpha);
        byte b = CompositeColorComponent(baseColor.B, firstColorAlpha, colorToAdd.B, secondColorAlpha, alpha);

        return Color.FromArgb(255, r, g, b);
    }

    /// <summary>
    /// For a single R/G/B component. a = precomputed CompositeAlpha(a1, a2)
    /// </summary>
    private static byte CompositeColorComponent(byte c1, byte a1, byte c2, byte a2, byte a)
    {
        // Handle the singular case of both layers fully transparent.
        if (a == 0)
        {
            return 0;
        }

        return System.Convert.ToByte((((255 * c2 * a2) + (c1 * a1 * (255 - a2))) / a) / 255);
    }

    private static byte CompositeAlpha(byte a1, byte a2)
    {
        return System.Convert.ToByte(255 - ((255 - a2) * (255 - a1)) / 255);
    }
}
``` 

## Detect the current theme
You can get the current applied theme of the entire `App`, a single `Window` or any `FrameworkElement`. To do so import this namespace to your usings:
```c#
using ControlzEx.Theming;
```

Now you can call this statement to get the current theme:
```c#
Theme currentTheme = ThemeManager.Current.DetectTheme([ThemedItem]);
```
where `ThemedItem` may be:
- `Application.Current`
- `this`
- any `Window`
- any `Control`
- any `FrameworkElement`