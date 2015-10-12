## Controlz
Shared Controlz files for WPF and...

**PopupEx**: Custom `Popup` that can be used in validation error templates or something else like in [MaterialDesignInXamlToolkit](https://github.com/ButchersBoy/MaterialDesignInXamlToolkit) or [MahApps.Metro](https://github.com/MahApps/MahApps.Metro).  

It provides some additional nice features:
  + repositioning if host-window size or location changed
  + repositioning if host-window gets maximized and vice versa
  + it's only topmost if the host-window is activated  

![2015-10-11_01h03_05](https://cloud.githubusercontent.com/assets/658431/10413784/ea365626-6fb6-11e5-9abc-c174159dcbf8.png)

**TabControlEx**: Custom `TabControl` that keeps the `TabItem` content in the VisualTree after unselect them, so no re-create nightmare is done, after select the `TabItem` again. The visibility behavior can be set by `ChildContentVisibility` dependency property.  

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
