namespace Fabulous.Maui

open System
open System.IO
open System.Runtime.CompilerServices
open Fabulous
open Fabulous.StackAllocatedCollections
open Microsoft.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Controls.PlatformConfiguration

type IFabPage =
    inherit IFabVisualElement

module Page =
    let BackgroundImageSource =
        Attributes.defineBindableAppTheme<ImageSource> Page.BackgroundImageSourceProperty

    let IconImageSource =
        Attributes.defineBindableAppTheme<ImageSource> Page.IconImageSourceProperty

    let IsBusy = Attributes.defineBindableBool Page.IsBusyProperty

    let Padding = Attributes.defineBindableWithEquality<Thickness> Page.PaddingProperty

    let Title = Attributes.defineBindableWithEquality<string> Page.TitleProperty

    let ToolbarItems =
        Attributes.defineListWidgetCollection<ToolbarItem> "Page_ToolbarItems" (fun target -> (target :?> Page).ToolbarItems)

    let Appearing =
        Attributes.defineEventNoArg "Page_Appearing" (fun target -> (target :?> Page).Appearing)

    let Disappearing =
        Attributes.defineEventNoArg "Page_Disappearing" (fun target -> (target :?> Page).Disappearing)

    let LayoutChanged =
        Attributes.defineEventNoArg "Page_LayoutChanged" (fun target -> (target :?> Page).LayoutChanged)

    let UseSafeArea =
        Attributes.defineSimpleScalarWithEquality<bool> "Page_UseSafeArea" (fun _ newValueOpt node ->
            let page = node.Target :?> Page

            let value =
                match newValueOpt with
                | ValueNone -> false
                | ValueSome v -> v

            iOSSpecific.Page.SetUseSafeArea(page, value))

[<Extension>]
type PageModifiers =
    /// <summary>The Page's title.</summary>
    [<Extension>]
    static member inline title(this: WidgetBuilder<'msg, #IFabPage>, value: string) =
        this.AddScalar(Page.Title.WithValue(value))

    /// <summary>Set the source of the IconImageSource.</summary>
    /// <param name="light">The source of the icon in the light theme.</param>
    /// <param name="dark">The source of the icon in the dark theme.</param>
    [<Extension>]
    static member inline icon(this: WidgetBuilder<'msg, #IFabPage>, light: ImageSource, ?dark: ImageSource) =
        this.AddScalar(Page.IconImageSource.WithValue(AppTheme.create light dark))

    /// <summary>Set the source of the IconImageSource.</summary>
    /// <param name="light">The source of the icon in the light theme.</param>
    /// <param name="dark">The source of the icon in the dark theme.</param>
    [<Extension>]
    static member inline icon(this: WidgetBuilder<'msg, #IFabPage>, light: string, ?dark: string) =
        let light = ImageSource.FromFile(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromFile(v))

        PageModifiers.icon(this, light, ?dark = dark)

    /// <summary>Set the source of the IconImageSource.</summary>
    /// <param name="light">The source of the icon in the light theme.</param>
    /// <param name="dark">The source of the icon in the dark theme.</param>
    [<Extension>]
    static member inline icon(this: WidgetBuilder<'msg, #IFabPage>, light: Uri, ?dark: Uri) =
        let light = ImageSource.FromUri(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromUri(v))

        PageModifiers.icon(this, light, ?dark = dark)

    /// <summary>Set the source of the IconImageSource.</summary>
    /// <param name="light">The source of the icon in the light theme.</param>
    /// <param name="dark">The source of the icon in the dark theme.</param>
    [<Extension>]
    static member inline icon(this: WidgetBuilder<'msg, #IFabPage>, light: Stream, ?dark: Stream) =
        let light = ImageSource.FromStream(fun () -> light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromStream(fun () -> v))

        PageModifiers.icon(this, light, ?dark = dark)

    /// <summary>Set the source of the BackgroundImageSource.</summary>
    /// <param name="light">The source of the background in the light theme.</param>
    /// <param name="dark">The source of the background in the dark theme.</param>
    [<Extension>]
    static member inline background(this: WidgetBuilder<'msg, #IFabPage>, light: ImageSource, ?dark: ImageSource) =
        this.AddScalar(Page.BackgroundImageSource.WithValue(AppTheme.create light dark))

    /// <summary>Set the source of the BackgroundImageSource.</summary>
    /// <param name="light">The source of the background in the light theme.</param>
    /// <param name="dark">The source of the background in the dark theme.</param>
    [<Extension>]
    static member inline background(this: WidgetBuilder<'msg, #IFabPage>, light: string, ?dark: string) =
        let light = ImageSource.FromFile(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromFile(v))

        PageModifiers.background(this, light, ?dark = dark)

    /// <summary>Set the source of the BackgroundImageSource.</summary>
    /// <param name="light">The source of the background in the light theme.</param>
    /// <param name="dark">The source of the background in the dark theme.</param>
    [<Extension>]
    static member inline background(this: WidgetBuilder<'msg, #IFabPage>, light: Uri, ?dark: Uri) =
        let light = ImageSource.FromUri(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromUri(v))

        PageModifiers.background(this, light, ?dark = dark)

    /// <summary>Set the source of the BackgroundImageSource.</summary>
    /// <param name="light">The source of the background in the light theme.</param>
    /// <param name="dark">The source of the background in the dark theme.</param>
    [<Extension>]
    static member inline background(this: WidgetBuilder<'msg, #IFabPage>, light: Stream, ?dark: Stream) =
        let light = ImageSource.FromStream(fun () -> light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromStream(fun () -> v))

        PageModifiers.background(this, light, ?dark = dark)

    /// <summary>Event that is fired when the page is appearing.</summary>
    /// <param name="onAppearing">Msg to dispatch when then page is appearing.</param>
    [<Extension>]
    static member inline onAppearing(this: WidgetBuilder<'msg, #IFabPage>, onAppearing: 'msg) =
        this.AddScalar(Page.Appearing.WithValue(onAppearing))

    /// <summary>Event that is fired when the page is disappearing.</summary>
    /// <param name="onDisappearing">Msg to dispatch when then page is disappearing.</param>
    [<Extension>]
    static member inline onDisappearing(this: WidgetBuilder<'msg, #IFabPage>, onDisappearing: 'msg) =
        this.AddScalar(Page.Disappearing.WithValue(onDisappearing))

    /// <summary>Event that is fired when the page layout has Changed.</summary>
    /// <param name="onLayoutChanged">Msg to dispatch when then page layout has Changed.</param>
    [<Extension>]
    static member inline onLayoutChanged(this: WidgetBuilder<'msg, #IFabPage>, onLayoutChanged: 'msg) =
        this.AddScalar(Page.LayoutChanged.WithValue(onLayoutChanged))

    [<Extension>]
    static member inline toolbarItems<'msg, 'marker when 'marker :> IFabPage>(this: WidgetBuilder<'msg, 'marker>) =
        WidgetHelpers.buildAttributeCollection<'msg, 'marker, IFabToolbarItem> Page.ToolbarItems this

    /// <summary>Marks the Page as busy. This will cause the platform specific global activity indicator to show a busy state.</summary>
    /// <param name="value">A bool indicating if the Page is busy or not.</param>
    [<Extension>]
    static member inline isBusy(this: WidgetBuilder<'msg, #IFabPage>, value: bool) =
        this.AddScalar(Page.IsBusy.WithValue(value))

    [<Extension>]
    static member inline padding(this: WidgetBuilder<'msg, #IFabPage>, value: Thickness) =
        this.AddScalar(Page.Padding.WithValue(value))

    [<Extension>]
    static member inline padding(this: WidgetBuilder<'msg, #IFabPage>, value: float) =
        PageModifiers.padding(this, Thickness(value))

    [<Extension>]
    static member inline padding(this: WidgetBuilder<'msg, #IFabPage>, left: float, top: float, right: float, bottom: float) =
        PageModifiers.padding(this, Thickness(left, top, right, bottom))

[<Extension>]
type PagePlatformModifiers =

    /// <summary>iOS platform specific. Sets a value that controls whether padding values are overridden with the safe area insets.</summary>
    [<Extension>]
    static member inline ignoreSafeArea(this: WidgetBuilder<'msg, #IFabPage>) =
        this.AddScalar(Page.UseSafeArea.WithValue(false))

[<Extension>]
type PageYieldExtensions =
    [<Extension>]
    static member inline Yield(_: AttributeCollectionBuilder<'msg, #IFabPage, IFabToolbarItem>, x: WidgetBuilder<'msg, #IFabToolbarItem>) : Content<'msg> =
        { Widgets = MutStackArray1.One(x.Compile()) }

    [<Extension>]
    static member inline Yield
        (
            _: AttributeCollectionBuilder<'msg, #IFabPage, IFabToolbarItem>,
            x: WidgetBuilder<'msg, Memo.Memoized<#IFabToolbarItem>>
        ) : Content<'msg> =
        { Widgets = MutStackArray1.One(x.Compile()) }
