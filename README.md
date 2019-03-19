![automove_tooltip2](./logo.jpg)  

[![Join the chat at https://gitter.im/ControlzEx/ControlzEx](https://img.shields.io/badge/GITTER-join%20chat-green.svg?style=flat-square)](https://gitter.im/ControlzEx/ControlzEx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build status](https://img.shields.io/appveyor/ci/punker76/controlzex.svg?style=flat-square)](https://ci.appveyor.com/project/punker76/controlzex)
[![Release](https://img.shields.io/github/release/ControlzEx/ControlzEx.svg?style=flat-square)](https://github.com/ControlzEx/ControlzEx/releases/latest)
[![Issues](https://img.shields.io/github/issues/ControlzEx/ControlzEx.svg?style=flat-square)](https://github.com/ControlzEx/ControlzEx/issues)
[![Downloads](https://img.shields.io/nuget/dt/ControlzEx.svg?style=flat-square)](http://www.nuget.org/packages/ControlzEx/)
[![Nuget](https://img.shields.io/nuget/vpre/ControlzEx.svg?style=flat-square)](http://nuget.org/packages/ControlzEx)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/ControlzEx/ControlzEx/blob/master/License.txt)

Shared Controlz for WPF and...

Made by [![Twitter Jan](https://img.shields.io/badge/twitter-%40punker76-55acee.svg?style=flat-square)](https://twitter.com/punker76) [![Twitter James](https://img.shields.io/badge/twitter-%40James_Willock-55acee.svg?style=flat-square)](https://twitter.com/James_Willock) [![Twitter Bastian](https://img.shields.io/badge/twitter-%40batzendev-55acee.svg?style=flat-square)](https://twitter.com/batzendev)

# Quick started for developers

* Download the latest (!!!) dotnet core 3.0.x SDK from here: [https://github.com/dotnet/core-sdk](https://github.com/dotnet/core-sdk) ([quick-link to download](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.exe))
  * Make sure you only have the latest dotnet core 3.0 installed!
* Make sure you have [Visual Studio 2019 Preview (Community Edition)](https://visualstudio.microsoft.com/de/vs/preview/), since dotnet core 3.0 sdk 009831 it no longer works with [Visual Studio 2017](https://visualstudio.microsoft.com)
* Clone the [ControlzEx repository](https://github.com/ControlzEx/ControlzEx.git)
* Open the solution from the src directory in Visual Studio
* Rebuild and start... (you might need to rebuild 2x, because we use the very fresh hot dotnet core stuff)

# What's in the box

* dotnet core 3.0 (preview) support (why, read here: https://blogs.msdn.microsoft.com/dotnet/2018/10/04/update-on-net-core-3-0-and-net-framework-4-8/ )
* targeting also .Net 4.5 and 4.6.2

## KeyboardNavigationEx

KeyboardNavigationEx is a helper class for a common focusing problem. The focus of an UI element itself isn't the problem. But if we use the common focusing methods, the control get the focus, but it doesn't get the focus visual style.

The KeyboardNavigation class handles the visual style only if the control get the focus from a keyboard device or if the SystemParameters.KeyboardCues is true.

With the KeyboardNavigationEx you can fix this in two simple ways.

First in code behind:

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += (s, e) => { KeyboardNavigationEx.Focus(this.TheElementWhichShouldGetTheFocus); };
    }
}

```

Or in XAML code:

```xaml
<Button controlzex:KeyboardNavigationEx.AlwaysShowFocusVisual="True">Hey, I get the focus visual style on mouse click! />
```

![keyboardfocusex](https://cloud.githubusercontent.com/assets/658431/15276251/143b9b3e-1ae3-11e6-9fe0-dc704675ad3b.gif)

## AutoMove ToolTip

An auto moving ToolTip. More [Info](http://jkarger.de/archive/2014/11/02/how-to-make-the-tooltip-follow-the-mouse/).

```xaml
<Button Margin="5"
        Padding="5"
        Content="Test Button 2"
        ToolTipService.ShowDuration="20000">
    <Button.ToolTip>
        <ToolTip local:ToolTipAssist.AutoMove="True">
            <ToolTip.Template>
                <ControlTemplate>
                    <Grid>
                        <Border Background="Gray"
                                BorderBrush="Black"
                                BorderThickness="1"
                                Opacity="0.9"
                                SnapsToDevicePixels="True" />
                        <TextBlock Margin="5"
                                    Foreground="WhiteSmoke"
                                    FontSize="22"
                                    Text="ToolTipHelper AutoMove sample"
                                    TextOptions.TextFormattingMode="Display"
                                    TextOptions.TextRenderingMode="ClearType" />
                    </Grid>
                </ControlTemplate>
            </ToolTip.Template>
        </ToolTip>
    </Button.ToolTip>
</Button>
```

![automove_tooltip2](https://cloud.githubusercontent.com/assets/658431/13525375/d708a9e8-e200-11e5-9bd9-035d50632b34.gif)  

![automove_tooltip](https://cloud.githubusercontent.com/assets/658431/13509772/7ff7406a-e18d-11e5-9238-e48c6e4fa2fa.gif)

## WindowChromeBehavior

ControlzEx provides a custom chrome for WPF windows and some other deeper fixes for it.

Most fixes comes from [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) and [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon).

Concrete implementation of techniques described here:

http://blogs.msdn.com/b/wpfsdk/archive/2008/09/08/custom-window-chrome-in-wpf.aspx

It's a fork of the original Microsoft WPF Shell Integration Library. Current Microsofts implementation can be found at:

http://referencesource.microsoft.com/

## PopupEx

Custom `Popup` that can be used in validation error templates or something else like in [MaterialDesignInXamlToolkit](https://github.com/ButchersBoy/MaterialDesignInXamlToolkit) or [MahApps.Metro](https://github.com/MahApps/MahApps.Metro).  

It provides some additional nice features:
  + repositioning if host-window size or location changed
  + repositioning if host-window gets maximized and vice versa
  + it's only topmost if the host-window is activated  

![2015-10-11_01h03_05](https://cloud.githubusercontent.com/assets/658431/10413784/ea365626-6fb6-11e5-9abc-c174159dcbf8.png)

## TabControlEx

Custom `TabControl` that keeps the `TabItem` content in the VisualTree after unselect them, so no re-create nightmare is done, after select the `TabItem` again. The visibility behavior can be set by `ChildContentVisibility` dependency property.  

Usage:

```xaml
<controlz:TabControlEx>
    <TabItem Header="Lorem">
        <TextBlock Text="Lorem ipsum dolor sit amet, consetetur sadipscing"
                   HorizontalAlignment="Center"
                   FontSize="30" />
    </TabItem>
    <TabItem Header="ipsum">
        <TextBox Text="Lorem ipsum dolor sit amet, consetetur sadipscing"
                 HorizontalAlignment="Center"
                 Margin="5" />
    </TabItem>
</controlz:TabControlEx>
```

## PackIconBase

A base class to help drive a common method for creating icon packs in WPF.

To create a new icon pack follow these steps:  

Define a key (typically an ```enum```):

```csharp
public enum PackIconKind
{
    Happy,
    Sad
}
```

Subclass ```PackIconBase```, adding
* Default style key
* A factory providing Path data for each key

```csharp
public class PackIcon : PackIconBase<PackIconKind>
{        
    static PackIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PackIcon), new FrameworkPropertyMetadata(typeof(PackIcon)));
    }     

    public PackIcon() : base(CreateIconData)
    { }

    private static IDictionary<PackIconKind, string> CreateIconData()
    {
        return new Dictionary<PackIconKind, string>
        {
            {PackIconKind.Happy, "M20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12M22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2A10,10 0 0,1 22,12M10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5C7,8.7 7.7,8 8.5,8C9.3,8 10,8.7 10,9.5M17,9.5C17,10.3 16.3,11 15.5,11C14.7,11 14,10.3 14,9.5C14,8.7 14.7,8 15.5,8C16.3,8 17,8.7 17,9.5M12,17.23C10.25,17.23 8.71,16.5 7.81,15.42L9.23,14C9.68,14.72 10.75,15.23 12,15.23C13.25,15.23 14.32,14.72 14.77,14L16.19,15.42C15.29,16.5 13.75,17.23 12,17.23Z"},
            {PackIconKind.Sad, "M20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12M22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2A10,10 0 0,1 22,12M15.5,8C16.3,8 17,8.7 17,9.5C17,10.3 16.3,11 15.5,11C14.7,11 14,10.3 14,9.5C14,8.7 14.7,8 15.5,8M10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5C7,8.7 7.7,8 8.5,8C9.3,8 10,8.7 10,9.5M12,14C13.75,14 15.29,14.72 16.19,15.81L14.77,17.23C14.32,16.5 13.25,16 12,16C10.75,16 9.68,16.5 9.23,17.23L7.81,15.81C8.71,14.72 10.25,14 12,14Z""}
        };
    }
}
```

Provide a default style (typically in your Generic.xaml, e.g:

```xaml
<Style TargetType="{x:Type local:PackIcon}">
    <Setter Property="Height" Value="16" />
    <Setter Property="Width" Value="16" />
    <Setter Property="HorizontalAlignment" Value="Left" />
    <Setter Property="VerticalAlignment" Value="Top" />
    <Setter Property="IsTabStop" Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type local:PackIcon}">
                <Viewbox>
                    <Canvas Width="24" Height="24">
                        <Path Data="{Binding Data, RelativeSource={RelativeSource TemplatedParent}}"                                  
                              Fill="{TemplateBinding Foreground}" />
                    </Canvas>
                </Viewbox>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```
### and finally...

Your users should now have a simple way to use your icon pack in their applications:

```xaml
<ns:PackIcon Kind="HappyIcon" />
```

## Licence

The MIT License (MIT)

Copyright (c) 2015-16 Jan Karger, Bastian Schmidt, James Willock

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
