namespace Fabulous.Maui

open System.Runtime.CompilerServices
open Fabulous
open Microsoft.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Controls.PlatformConfiguration

type IFabEntry =
    inherit IFabInputView

module Entry =
    let WidgetKey = Widgets.register<Entry>()

    let ClearButtonVisibility =
        Attributes.defineBindableWithEquality<ClearButtonVisibility> Entry.ClearButtonVisibilityProperty

    let CursorPosition = Attributes.defineBindableInt Entry.CursorPositionProperty

    let FontAttributes =
        Attributes.defineBindableEnum<FontAttributes> Entry.FontAttributesProperty

    let FontFamily =
        Attributes.defineBindableWithEquality<string> Entry.FontFamilyProperty

    let FontSize = Attributes.defineBindableFloat Entry.FontSizeProperty

    let HorizontalTextAlignment =
        Attributes.defineBindableEnum<TextAlignment> Entry.HorizontalTextAlignmentProperty

    let IsPassword = Attributes.defineBindableBool Entry.IsPasswordProperty

    let IsTextPredictionEnabled =
        Attributes.defineBindableBool Entry.IsTextPredictionEnabledProperty

    let ReturnType = Attributes.defineBindableEnum<ReturnType> Entry.ReturnTypeProperty

    let SelectionLength = Attributes.defineBindableInt Entry.SelectionLengthProperty

    let VerticalTextAlignment =
        Attributes.defineBindableEnum<TextAlignment> Entry.VerticalTextAlignmentProperty

    let FontAutoScalingEnabled =
        Attributes.defineBindableBool Entry.FontAutoScalingEnabledProperty

    let Completed =
        Attributes.defineEventNoArg "Entry_Completed" (fun target -> (target :?> Entry).Completed)

    let CursorColor =
        Attributes.defineSmallScalar<FabColor> "Entry_CursorColor" SmallScalars.FabColor.decode (fun _ newValueOpt node ->
            let entry = node.Target :?> Entry

            let value =
                match newValueOpt with
                | ValueNone -> null
                | ValueSome x -> x.ToMauiColor()

            iOSSpecific.Entry.SetCursorColor(entry, value))

[<AutoOpen>]
module EntryBuilders =
    type Fabulous.Maui.View with

        static member inline Entry<'msg>(text: string, onTextChanged: string -> 'msg) =
            WidgetBuilder<'msg, IFabEntry>(
                Entry.WidgetKey,
                InputView.TextWithEvent.WithValue(ValueEventData.create text (fun args -> onTextChanged args.NewTextValue |> box))
            )

[<Extension>]
type EntryModifiers =
    [<Extension>]
    static member inline clearButtonVisibility(this: WidgetBuilder<'msg, #IFabEntry>, value: ClearButtonVisibility) =
        this.AddScalar(Entry.ClearButtonVisibility.WithValue(value))

    [<Extension>]
    static member inline cursorPosition(this: WidgetBuilder<'msg, #IFabEntry>, value: int) =
        this.AddScalar(Entry.CursorPosition.WithValue(value))

    [<Extension>]
    static member inline font
        (
            this: WidgetBuilder<'msg, #IFabEntry>,
            ?size: float,
            ?attributes: FontAttributes,
            ?fontFamily: string,
            ?fontAutoScalingEnabled: bool
        ) =

        let mutable res = this

        match size with
        | None -> ()
        | Some v -> res <- res.AddScalar(Entry.FontSize.WithValue(v))

        match attributes with
        | None -> ()
        | Some v -> res <- res.AddScalar(Entry.FontAttributes.WithValue(v))

        match fontFamily with
        | None -> ()
        | Some v -> res <- res.AddScalar(Entry.FontFamily.WithValue(v))

        match fontAutoScalingEnabled with
        | None -> ()
        | Some v -> res <- res.AddScalar(Entry.FontAutoScalingEnabled.WithValue(v))

        res

    [<Extension>]
    static member inline horizontalTextAlignment(this: WidgetBuilder<'msg, #IFabEntry>, value: TextAlignment) =
        this.AddScalar(Entry.HorizontalTextAlignment.WithValue(value))

    [<Extension>]
    static member inline isPassword(this: WidgetBuilder<'msg, #IFabEntry>, value: bool) =
        this.AddScalar(Entry.IsPassword.WithValue(value))

    [<Extension>]
    static member inline isTextPredictionEnabled(this: WidgetBuilder<'msg, #IFabEntry>, value: bool) =
        this.AddScalar(Entry.IsTextPredictionEnabled.WithValue(value))

    [<Extension>]
    static member inline returnType(this: WidgetBuilder<'msg, #IFabEntry>, value: ReturnType) =
        this.AddScalar(Entry.ReturnType.WithValue(value))

    [<Extension>]
    static member inline selectionLength(this: WidgetBuilder<'msg, #IFabEntry>, value: int) =
        this.AddScalar(Entry.SelectionLength.WithValue(value))

    [<Extension>]
    static member inline verticalTextAlignment(this: WidgetBuilder<'msg, #IFabEntry>, value: TextAlignment) =
        this.AddScalar(Entry.VerticalTextAlignment.WithValue(value))

    [<Extension>]
    static member inline onCompleted(this: WidgetBuilder<'msg, #IFabEntry>, onCompleted: 'msg) =
        this.AddScalar(Entry.Completed.WithValue(onCompleted))

    /// <summary>Link a ViewRef to access the direct Entry control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IFabEntry>, value: ViewRef<Entry>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))

[<Extension>]
type EntryPlatformModifiers =
    /// <summary>iOS platform specific. Sets the cursor color.</summary>
    /// <param name="value">The new cursor color.</param>
    [<Extension>]
    static member inline cursorColor(this: WidgetBuilder<'msg, #IFabEntry>, value: FabColor) =
        this.AddScalar(Entry.CursorColor.WithValue(value))
