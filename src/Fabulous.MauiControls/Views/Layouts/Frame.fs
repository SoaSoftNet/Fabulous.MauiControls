namespace Fabulous.Maui

open System.Runtime.CompilerServices
open Fabulous
open Microsoft.Maui.Controls

type IFabFrame =
    inherit IFabContentView

module Frame =
    let WidgetKey = Widgets.register<Frame>()

    let BorderColor = Attributes.defineBindableAppThemeColor Frame.BorderColorProperty

    let CornerRadius = Attributes.defineBindableFloat Frame.CornerRadiusProperty

    let HasShadow = Attributes.defineBindableBool Frame.HasShadowProperty

[<AutoOpen>]
module FrameBuilders =
    type Fabulous.Maui.View with

        static member inline Frame<'msg, 'marker when 'marker :> IFabView>(content: WidgetBuilder<'msg, 'marker>) =
            WidgetHelpers.buildWidgets<'msg, IFabFrame> Frame.WidgetKey [| ContentView.Content.WithValue(content.Compile()) |]

        static member inline Frame<'msg, 'marker when 'marker :> IFabView>() =
            WidgetHelpers.buildWidgets<'msg, IFabFrame> Frame.WidgetKey [||]


[<Extension>]
type FrameModifiers =
    /// <summary>Set the color of the frame border color</summary>
    /// <param name="light">The color of the frame border in the light theme.</param>
    /// <param name="dark">The color of the frame border in the dark theme.</param>
    [<Extension>]
    static member inline borderColor(this: WidgetBuilder<'msg, #IFabFrame>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(Frame.BorderColor.WithValue(AppTheme.create light dark))

    /// <summary>Set the corner radius of the frame</summary>
    /// <param name="value">The corner radius of the frame</param>
    [<Extension>]
    static member inline cornerRadius(this: WidgetBuilder<'msg, #IFabFrame>, value: float) =
        this.AddScalar(Frame.CornerRadius.WithValue(value))

    // <summary>Set the shadow of the frame</summary>
    // <param name="value">Whether the frame has a shadow</param>
    [<Extension>]
    static member inline hasShadow(this: WidgetBuilder<'msg, #IFabFrame>, value: bool) =
        this.AddScalar(Frame.HasShadow.WithValue(value))

    /// <summary>Link a ViewRef to access the direct Frame control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IFabFrame>, value: ViewRef<Frame>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))
