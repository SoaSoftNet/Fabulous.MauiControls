namespace Fabulous.Maui

open System
open System.Runtime.CompilerServices
open Fabulous
open Microsoft.Maui
open Microsoft.Maui.Controls

type IFabEntryCell =
    inherit IFabCell

/// Microsoft.Maui doesn't provide an event for textChanged the EntryCell, so we implement it
type FabEntryCell() =
    inherit EntryCell()

    let mutable oldText = ""

    let textChanged = Event<EventHandler<TextChangedEventArgs>, _>()

    [<CLIEvent>]
    member _.TextChanged = textChanged.Publish

    override this.OnPropertyChanged(propertyName) =
        base.OnPropertyChanged(propertyName)

        if propertyName = EntryCell.TextProperty.PropertyName then
            textChanged.Trigger(this, TextChangedEventArgs(oldText, this.Text))

    override this.OnPropertyChanging(propertyName) =
        base.OnPropertyChanging(propertyName)

        if propertyName = EntryCell.TextProperty.PropertyName then
            oldText <- this.Text

module EntryCell =
    let WidgetKey = Widgets.register<FabEntryCell>()

    let Label = Attributes.defineBindableWithEquality<string> EntryCell.LabelProperty

    let LabelColor = Attributes.defineBindableAppThemeColor EntryCell.LabelColorProperty

    let Placeholder =
        Attributes.defineBindableWithEquality<string> EntryCell.PlaceholderProperty

    let HorizontalTextAlignment =
        Attributes.defineBindableEnum<TextAlignment> EntryCell.HorizontalTextAlignmentProperty

    let VerticalTextAlignment =
        Attributes.defineBindableEnum<TextAlignment> EntryCell.VerticalTextAlignmentProperty

    let Keyboard =
        Attributes.defineBindableWithEquality<Keyboard> EntryCell.KeyboardProperty

    let TextWithEvent =
        Attributes.defineBindableWithEvent "EntryCell_TextChanged" EntryCell.TextProperty (fun target -> (target :?> FabEntryCell).TextChanged)

    let OnCompleted =
        Attributes.defineEventNoArg "EntryCell_Completed" (fun target -> (target :?> EntryCell).Completed)

[<AutoOpen>]
module EntryCellBuilders =

    type Fabulous.Maui.View with

        static member inline EntryCell<'msg>(label: string, text: string, onTextChanged: string -> 'msg) =
            WidgetBuilder<'msg, IFabEntryCell>(
                EntryCell.WidgetKey,
                EntryCell.Label.WithValue(label),
                EntryCell.TextWithEvent.WithValue(ValueEventData.create text (fun args -> onTextChanged args.NewTextValue |> box))
            )

[<Extension>]
type EntryCellModifiers =
    /// <summary>Set the color of the label</summary>
    /// <param name="light">The color of the label in the light theme.</param>
    /// <param name="dark">The color of the label in the dark theme.</param>
    [<Extension>]
    static member inline labelColor(this: WidgetBuilder<'msg, #IFabEntryCell>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(EntryCell.LabelColor.WithValue(AppTheme.create light dark))

    /// <summary>Set the horizontal text alignment</summary>
    /// param name="alignment">The horizontal text alignment</summary>
    [<Extension>]
    static member inline horizontalTextAlignment(this: WidgetBuilder<'msg, #IFabEntryCell>, alignment: TextAlignment) =
        this.AddScalar(EntryCell.HorizontalTextAlignment.WithValue(alignment))

    /// <summary>Set the vertical text alignment</summary>
    /// param name="alignment">The vertical text alignment</summary>
    [<Extension>]
    static member inline verticalTextAlignment(this: WidgetBuilder<'msg, #IFabEntryCell>, alignment: TextAlignment) =
        this.AddScalar(EntryCell.VerticalTextAlignment.WithValue(alignment))

    /// <summary>Set the keyboard</summary>
    /// param name="keyboard">The keyboard type</summary>
    [<Extension>]
    static member inline keyboard(this: WidgetBuilder<'msg, #IFabEntryCell>, keyboard: Keyboard) =
        this.AddScalar(EntryCell.Keyboard.WithValue(keyboard))

    /// <summary>Set the placeholder text</summary>
    /// param name="placeholder">The placeholder</summary>
    [<Extension>]
    static member inline placeholder(this: WidgetBuilder<'msg, #IFabEntryCell>, placeholder: string) =
        this.AddScalar(EntryCell.Placeholder.WithValue(placeholder))

    [<Extension>]
    static member inline onCompleted(this: WidgetBuilder<'msg, #IFabEntryCell>, onCompleted: 'msg) =
        this.AddScalar(EntryCell.OnCompleted.WithValue(onCompleted))

    /// <summary>Link a ViewRef to access the direct EntryCell control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IFabEntryCell>, value: ViewRef<EntryCell>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))
