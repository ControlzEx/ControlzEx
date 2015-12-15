# ControlzEx

[![Build status](https://ci.appveyor.com/api/projects/status/ij69o79y9rgdl450/branch/develop?svg=true)](https://ci.appveyor.com/project/punker76/controlzex/branch/develop)
[![Join the chat at https://gitter.im/ControlzEx/ControlzEx](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ControlzEx/ControlzEx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Shared Controlz for WPF and...

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

```csharp
<controlz:TabControlEx Style="{StaticResource {x:Type TabControl}}">
    <TabItem Header="Lorem">
        <TextBlock Text="Modern UI with MahApps.Metro"
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

## Licence

The MIT License (MIT)

Copyright (c) 2015 Jan Karger, Bastian Schmidt

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
