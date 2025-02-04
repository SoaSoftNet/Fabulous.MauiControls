namespace Fabulous.Maui

open System
open System.Runtime.CompilerServices
open Fabulous
open Microsoft.Maui.Controls
open Microsoft.Maui.Controls.PlatformConfiguration

type IFabTimePicker =
    inherit IFabView

type TimeSelectedEventArgs(newTime: TimeSpan) =
    inherit EventArgs()
    member _.NewTime = newTime

/// Microsoft.Maui doesn't provide an event for selecting the time on a TimePicker, so we implement it
type FabTimePicker() =
    inherit TimePicker()

    let timeSelected = Event<EventHandler<TimeSelectedEventArgs>, _>()

    [<CLIEvent>]
    member _.TimeSelected = timeSelected.Publish

    override this.OnPropertyChanged(propertyName) =
        base.OnPropertyChanged(propertyName)

        if propertyName = TimePicker.TimeProperty.PropertyName then
            timeSelected.Trigger(this, TimeSelectedEventArgs(this.Time))

module TimePicker =
    let WidgetKey = Widgets.register<FabTimePicker>()

    let CharacterSpacing =
        Attributes.defineBindableFloat TimePicker.CharacterSpacingProperty

    let TimeWithEvent =
        Attributes.defineBindableWithEvent "TimePicker_TimeSelected" TimePicker.TimeProperty (fun target -> (target :?> FabTimePicker).TimeSelected)

    let FontAttributes =
        Attributes.defineBindableEnum<FontAttributes> TimePicker.FontAttributesProperty

    let FontFamily =
        Attributes.defineBindableWithEquality<string> TimePicker.FontFamilyProperty

    let FontSize = Attributes.defineBindableFloat TimePicker.FontSizeProperty

    let Format = Attributes.defineBindableWithEquality<string> TimePicker.FormatProperty

    let TextColor = Attributes.defineBindableAppThemeColor TimePicker.TextColorProperty

    let FontAutoScalingEnabled =
        Attributes.defineBindableBool TimePicker.FontAutoScalingEnabledProperty

    let UpdateMode =
        Attributes.defineEnum<iOSSpecific.UpdateMode> "TimePicker_UpdateMode" (fun _ newValueOpt node ->
            let timePicker = node.Target :?> TimePicker

            let value =
                match newValueOpt with
                | ValueNone -> iOSSpecific.UpdateMode.Immediately
                | ValueSome v -> v

            iOSSpecific.TimePicker.SetUpdateMode(timePicker, value))

[<AutoOpen>]
module TimePickerBuilders =
    type Fabulous.Maui.View with

        static member inline TimePicker<'msg>(time: TimeSpan, onTimeSelected: TimeSpan -> 'msg) =
            WidgetBuilder<'msg, IFabTimePicker>(
                TimePicker.WidgetKey,
                TimePicker.TimeWithEvent.WithValue(ValueEventData.create time (fun args -> onTimeSelected args.NewTime |> box))
            )

[<Extension>]
type TimePickerModifiers =
    [<Extension>]
    static member inline characterSpacing(this: WidgetBuilder<'msg, #IFabTimePicker>, value: float) =
        this.AddScalar(TimePicker.CharacterSpacing.WithValue(value))

    [<Extension>]
    static member inline format(this: WidgetBuilder<'msg, #IFabTimePicker>, value: string) =
        this.AddScalar(TimePicker.Format.WithValue(value))

    [<Extension>]
    static member inline textColor(this: WidgetBuilder<'msg, #IFabTimePicker>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(TimePicker.TextColor.WithValue(AppTheme.create light dark))

    [<Extension>]
    static member inline font
        (
            this: WidgetBuilder<'msg, #IFabTimePicker>,
            ?size: float,
            ?attributes: FontAttributes,
            ?fontFamily: string,
            ?fontAutoScalingEnabled: bool
        ) =

        let mutable res = this

        match size with
        | None -> ()
        | Some v -> res <- res.AddScalar(TimePicker.FontSize.WithValue(v))

        match attributes with
        | None -> ()
        | Some v -> res <- res.AddScalar(TimePicker.FontAttributes.WithValue(v))

        match fontFamily with
        | None -> ()
        | Some v -> res <- res.AddScalar(TimePicker.FontFamily.WithValue(v))

        match fontAutoScalingEnabled with
        | None -> ()
        | Some v -> res <- res.AddScalar(TimePicker.FontAutoScalingEnabled.WithValue(v))

        res

    /// <summary>Link a ViewRef to access the direct TimePicker control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IFabTimePicker>, value: ViewRef<TimePicker>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))

[<Extension>]
type TimePickerPlatformModifiers =
    /// <summary>iOS platform specific. Sets a value that controls whether elements in the time picker are continuously updated while scrolling or updated once after scrolling has completed.</summary>
    /// <param name="mode">The new property value to assign.</param>
    [<Extension>]
    static member inline updateMode(this: WidgetBuilder<'msg, #IFabTimePicker>, mode: iOSSpecific.UpdateMode) =
        this.AddScalar(TimePicker.UpdateMode.WithValue(mode))
