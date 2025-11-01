# Changelog for ControlzEx

## vNext

### Maintenance

- [#218](../../issues/218) - Remove workaround fro already added member (thanks @DoctorKrolic)
- [#219](../../issues/219) - Remove unused box type (thanks @DoctorKrolic)

## 7.0.2

### Bug fixes

- [#213](../../issues/213) - Window border disappears when restored after starting maximized

## 7.0.1

### Bug fixes

- [#206](../../issues/206) - Maximized windows with WindowStyle=None do not fill screen
- [#209](../../issues/209) - Window Menu issue

## 7.0.0

### Breaking changes

- Default value of `GlowWindowBehavior.DWMSupportsBorderColor` now depends on the OS version
- `Constants.ResizeCornerGripThickness` was changed from 18 to 12

### Enhancements/Features

- [#194](../../issues/194) - Adding Backdrop support
- Added `WindowBackdropManager` to allow for backdrop effects in windows
- Added support for caption color, glass frame thickness and native caption buttons in `WindowChromeWindow` and `WindowChromeBehavior`
- Added `AppModeHelper` to allow for dark system menus inside applications. Use with caution as it relies on undocumented OS methods.
- Added `PopupBackdropManager` to allow for backdrop effects in popups. Use with caution as it relies on undocumented OS methods.
- Allowed "recursive" value replacement in theme generation
- Improved NC hit testing for maximized windows

### Bug fixes

- [#176](../../issues/176) - ControlzEx.WindowChromeWindow lacks documentation
- [#179](../../issues/179) - Initial Window CornerPreference
- [#182](../../issues/182) - NullReferenceException if Window closed in Window_Loaded
- [#197](../../issues/197) - Incorrect behavior when WindowChromeWindow ResizeMode="CanMinimize"
- Prevented race conditions during window closing
- Fixed NC-size on Windows 10
- Workaround NC-Size bug in Windows

### New Contributors

- @Lehonti made their first contribution in [#183](../../issues/183)
