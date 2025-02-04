namespace Fabulous.Maui

open System.Runtime.CompilerServices
open Fabulous
open Microsoft.Maui.Controls

type IFabPanGestureRecognizer =
    inherit IFabGestureRecognizer

module PanGestureRecognizer =
    let WidgetKey = Widgets.register<PanGestureRecognizer>()

    let TouchPoints =
        Attributes.defineBindableInt PanGestureRecognizer.TouchPointsProperty

    let PanUpdated =
        Attributes.defineEvent<PanUpdatedEventArgs> "PanGestureRecognizer_PanUpdated" (fun target -> (target :?> PanGestureRecognizer).PanUpdated)

[<AutoOpen>]
module PanGestureRecognizerBuilders =
    type Fabulous.Maui.View with

        static member inline PanGestureRecognizer<'msg>(onPanUpdated: PanUpdatedEventArgs -> 'msg) =
            WidgetBuilder<'msg, IFabPanGestureRecognizer>(
                PanGestureRecognizer.WidgetKey,
                PanGestureRecognizer.PanUpdated.WithValue(fun args -> onPanUpdated args |> box)
            )

[<Extension>]
type PanGestureRecognizerModifiers =

    /// <summary>Sets the number of touch points in the gesture.</summary>
    /// <param name="value">The number of touch points that must be present on the screen to trigger the gesture.</param>
    [<Extension>]
    static member inline touchPoints(this: WidgetBuilder<'msg, #IFabPanGestureRecognizer>, value: int) =
        this.AddScalar(PanGestureRecognizer.TouchPoints.WithValue(value))
