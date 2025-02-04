namespace Fabulous.Maui

#nowarn "44"

open System
open System.IO
open System.Runtime.CompilerServices
open Fabulous
open Fabulous.StackAllocatedCollections
open Microsoft.Maui.Controls
open Microsoft.Maui.Controls.PlatformConfiguration

type IFabNavigationPage =
    inherit IFabPage

// https://stackoverflow.com/a/2113902
type RequiresSubscriptionEvent() =
    let evt = Event<EventHandler, EventArgs>()
    let mutable counter = 0

    let published =
        { new IEvent<EventHandler, EventArgs> with
            member x.AddHandler(h) =
                evt.Publish.AddHandler(h)
                counter <- counter + 1

            member x.RemoveHandler(h) =
                evt.Publish.RemoveHandler(h)
                counter <- counter - 1

            member x.Subscribe(s) =
                let h = EventHandler(fun _ -> s.OnNext)
                x.AddHandler(h)

                { new IDisposable with
                    member y.Dispose() = x.RemoveHandler(h) } }

    member x.Trigger(v) = evt.Trigger(v)
    member x.Publish = published
    member x.HasListeners = counter > 0

/// Maui handles pages asynchronously, meaning a page will be added to the stack only after the animation is finished.
/// This is a problem for Fabulous, because the nav stack needs to be synchronized with the widget trees.
/// Otherwise rapid consecutive updates might end up with a wrong nav stack.
///
/// To work around that, we keep our own nav stack, and we update it synchronously.
type FabNavigationPage() as this =
    inherit NavigationPage()

    let _pagesSync =
        System.Collections.Generic.List((this :> INavigationPageController).Pages)

    let mutable popCount = 0

    let backNavigated = Event<EventHandler, EventArgs>()
    let backButtonPressed = RequiresSubscriptionEvent()

    do this.Popped.Add(this.OnPopped)

    [<CLIEvent>]
    member _.BackNavigated = backNavigated.Publish

    [<CLIEvent>]
    member _.BackButtonPressed = backButtonPressed.Publish

    member this.PagesSync = _pagesSync :> System.Collections.Generic.IReadOnlyList<Page>

    member this.PushSync(page: Page, ?animated: bool) =
        _pagesSync.Add(page)

        this.PushAsync(page, (animated <> Some false)) |> ignore

    member this.InsertPageBeforeSync(page: Page, index: int) =
        let next = _pagesSync[index]
        _pagesSync.Insert(index, page)
        this.Navigation.InsertPageBefore(page, next)

    member this.RemovePageSync(index: int) =
        if index < _pagesSync.Count then
            popCount <- popCount + 1
            let page = _pagesSync[index]
            _pagesSync.RemoveAt(index)
            this.Navigation.RemovePage(page)

    member this.PopSync(?animated: bool) =
        if _pagesSync.Count > 0 then
            popCount <- popCount + 1
            _pagesSync.RemoveAt(_pagesSync.Count - 1)
            this.PopAsync(animated <> Some false) |> ignore

    member this.OnPopped(_: NavigationEventArgs) =
        // Only trigger BackNavigated if Fabulous isn't the one popping the page (e.g. user tapped back button)
        if popCount > 0 then
            popCount <- popCount - 1
        else
            _pagesSync.RemoveAt(_pagesSync.Count - 1)
            backNavigated.Trigger(this, EventArgs())

    /// If we are listening to the BackButtonPressed event, cancel the automatic back navigation and trigger the event;
    /// otherwise just let the automatic back navigation happen
    override this.OnBackButtonPressed() =
        if backButtonPressed.HasListeners then
            backButtonPressed.Trigger(this, EventArgs())
            true
        else
            false

module NavigationPageUpdaters =
    let applyDiffNavigationPagePages _ (diffs: WidgetCollectionItemChanges) (node: IViewNode) =
        let navigationPage = node.Target :?> FabNavigationPage
        let pages = Array.ofSeq navigationPage.PagesSync

        let mutable popLastWithAnimation = false

        for diff in diffs do
            match diff with
            | WidgetCollectionItemChange.Insert(index, widget) ->
                let struct (itemNode, page) = Helpers.createViewForWidget node widget
                let page = page :?> Page

                node.TreeContext.Logger.Debug(
                    "[NavigationPage] applyDiffNavigationPagePages: Inserting page with index '{0}' and automationId = '{1}'",
                    index,
                    page.AutomationId
                )

                if index >= pages.Length then
                    navigationPage.PushSync(page)
                else
                    navigationPage.InsertPageBeforeSync(page, index)


                // Trigger the mounted event
                Dispatcher.dispatchEventForAllChildren itemNode widget Lifecycle.Mounted

            | WidgetCollectionItemChange.Update(index, diff) ->
                let page = pages[index]
                let childNode = node.TreeContext.GetViewNode(page)

                node.TreeContext.Logger.Debug(
                    "[NavigationPage] applyDiffNavigationPagePages: Updating page with index '{0}' and automationId = '{1}'",
                    index,
                    page.AutomationId
                )

                childNode.ApplyDiff(&diff)

            | WidgetCollectionItemChange.Replace(index, prevWidget, currWidget) ->
                let prevPage = pages[index]
                let prevItemNode = node.TreeContext.GetViewNode(prevPage)

                let struct (currItemNode, currPage) = Helpers.createViewForWidget node currWidget
                let currPage = currPage :?> Page

                node.TreeContext.Logger.Debug(
                    "[NavigationPage] applyDiffNavigationPagePages: Replacing page at index '{0}'. Old automationId = '{1}', new automationId = '{2}'",
                    index,
                    prevPage.AutomationId,
                    currPage.AutomationId
                )

                // Trigger the unmounted event for the old child
                Dispatcher.dispatchEventForAllChildren prevItemNode prevWidget Lifecycle.Unmounted
                prevItemNode.Disconnect()

                if index = 0 && pages.Length = 1 then
                    // We are trying to replace the root page
                    // First we push the new page, then we remove the old one
                    navigationPage.PushSync(currPage, false)
                    navigationPage.RemovePageSync(index)

                elif index = pages.Length - 1 then
                    // Last page, we pop it and push the new one
                    navigationPage.PushSync(currPage, animated = true)
                    navigationPage.RemovePageSync(index)

                else
                    // Page is not visible, we just replace it
                    navigationPage.RemovePageSync(index)
                    navigationPage.InsertPageBeforeSync(currPage, index + 1)

                // Trigger the mounted event for the new child
                Dispatcher.dispatchEventForAllChildren currItemNode currWidget Lifecycle.Mounted

            | WidgetCollectionItemChange.Remove(index, prevWidget) ->
                let prevPage = pages[index]
                let prevItemNode = node.TreeContext.GetViewNode(prevPage)

                node.TreeContext.Logger.Debug(
                    "[NavigationPage] applyDiffNavigationPagePages: Removing page at index '{0}' and automationId = '{1}'",
                    index,
                    prevPage.AutomationId
                )

                // Trigger the unmounted event for the old child
                Dispatcher.dispatchEventForAllChildren prevItemNode prevWidget Lifecycle.Unmounted
                prevItemNode.Disconnect()

                if index = pages.Length - 1 then
                    popLastWithAnimation <- true
                else
                    navigationPage.RemovePageSync(index)

        if popLastWithAnimation then
            navigationPage.PopSync()

    let updateNavigationPagePages (oldValueOpt: ArraySlice<Widget> voption) (newValueOpt: ArraySlice<Widget> voption) (node: IViewNode) =
        let navigationPage = node.Target :?> FabNavigationPage

        match newValueOpt with
        | ValueNone -> failwith "NavigationPage requires its Pages modifier to be set"

        | ValueSome widgets ->
            // Push all new pages but only animate the last one
            let mutable i = 0
            let span = ArraySlice.toSpan widgets

            for widget in span do
                let struct (itemNode, page) = Helpers.createViewForWidget node widget
                let page = page :?> Page

                node.TreeContext.Logger.Debug(
                    "[NavigationPage] updateNavigationPagePages: Inserting page with index '{0}' and automationId = '{1}'",
                    i,
                    page.AutomationId
                )

                let animateIfLastPage = i = span.Length - 1
                navigationPage.PushSync(page, animateIfLastPage)

                // Trigger the mounted event
                Dispatcher.dispatchEventForAllChildren itemNode widget Lifecycle.Mounted

                i <- i + 1

            // Silently remove all old pages
            match oldValueOpt with
            | ValueNone -> ()
            | ValueSome oldWidgets ->
                let pages = Array.ofSeq navigationPage.PagesSync
                let span = ArraySlice.toSpan oldWidgets

                for i = 0 to span.Length - 1 do
                    let prevPage = pages[i]
                    let prevItemNode = node.TreeContext.GetViewNode(prevPage)

                    node.TreeContext.Logger.Debug(
                        "[NavigationPage] updateNavigationPagePages: Removing page at index '{0}' and automationId = '{1}'",
                        i,
                        prevPage.AutomationId
                    )

                    navigationPage.Navigation.RemovePage(prevPage)

                    // Trigger the unmounted event for the old child
                    Dispatcher.dispatchEventForAllChildren prevItemNode span[i] Lifecycle.Unmounted
                    prevItemNode.Disconnect()

module NavigationPage =
    let WidgetKey = Widgets.register<FabNavigationPage>()

    let BackButtonTitle =
        Attributes.defineBindableWithEquality<string> NavigationPage.BackButtonTitleProperty

    let Pages =
        Attributes.defineWidgetCollection
            "NavigationPage_Pages"
            NavigationPageUpdaters.applyDiffNavigationPagePages
            NavigationPageUpdaters.updateNavigationPagePages

    let BarBackgroundColor =
        Attributes.defineBindableAppThemeColor NavigationPage.BarBackgroundColorProperty

    let BarBackground =
        Attributes.defineBindableAppTheme<Brush> NavigationPage.BarBackgroundProperty

    let BarTextColor =
        Attributes.defineBindableAppThemeColor NavigationPage.BarTextColorProperty

    let IconColor =
        Attributes.defineBindableAppThemeColor NavigationPage.IconColorProperty

    let HasNavigationBar =
        Attributes.defineBindableBool NavigationPage.HasNavigationBarProperty

    let HasBackButton =
        Attributes.defineBindableBool NavigationPage.HasBackButtonProperty

    let TitleIconImageSource =
        Attributes.defineBindableAppTheme<ImageSource> NavigationPage.TitleIconImageSourceProperty

    let BackNavigated =
        Attributes.defineEventNoArg "NavigationPage_BackNavigated" (fun target -> (target :?> FabNavigationPage).BackNavigated)

    let BackButtonPressed =
        Attributes.defineEventNoArg "NavigationPage_BackButtonPressed" (fun target -> (target :?> FabNavigationPage).BackButtonPressed)

    [<Obsolete("Use BackNavigated instead")>]
    let Popped =
        Attributes.defineEvent<NavigationEventArgs> "NavigationPage_Popped" (fun target -> (target :?> NavigationPage).Popped)

    [<Obsolete("Will be removed in next major version")>]
    let Pushed =
        Attributes.defineEvent<NavigationEventArgs> "NavigationPage_Pushed" (fun target -> (target :?> NavigationPage).Pushed)

    [<Obsolete("Use BackNavigated instead")>]
    let PoppedToRoot =
        Attributes.defineEvent<NavigationEventArgs> "NavigationPage_PoppedToRoot" (fun target -> (target :?> NavigationPage).PoppedToRoot)

    let TitleView = Attributes.defineBindableWidget NavigationPage.TitleViewProperty

    let HideNavigationBarSeparator =
        Attributes.defineBool "NavigationPage_HideNavigationBarSeparator" (fun _ newValueOpt node ->
            let page = node.Target :?> NavigationPage

            let value =
                match newValueOpt with
                | ValueNone -> false
                | ValueSome v -> v

            iOSSpecific.NavigationPage.SetHideNavigationBarSeparator(page, value))

    let IsNavigationBarTranslucent =
        Attributes.defineBool "NavigationPage_IsNavigationBarTranslucent" (fun _ newValueOpt node ->
            let page = node.Target :?> NavigationPage

            let value =
                match newValueOpt with
                | ValueNone -> false
                | ValueSome v -> v

            iOSSpecific.NavigationPage.SetIsNavigationBarTranslucent(page, value))

    let PrefersLargeTitles =
        Attributes.defineBool "NavigationPage_PrefersLargeTitles" (fun _ newValueOpt node ->
            let page = node.Target :?> NavigationPage

            let value =
                match newValueOpt with
                | ValueNone -> false
                | ValueSome v -> v

            iOSSpecific.NavigationPage.SetPrefersLargeTitles(page, value))

[<AutoOpen>]
module NavigationPageBuilders =
    type Fabulous.Maui.View with

        static member inline NavigationPage<'msg>() =
            CollectionBuilder<'msg, IFabNavigationPage, IFabPage>(NavigationPage.WidgetKey, NavigationPage.Pages)

[<Extension>]
type NavigationPageModifiers =
    /// <summary>Set the color of the BarBackgroundColor.</summary>
    /// <param name="light">The color of the barBackgroundColor in the light theme.</param>
    /// <param name="dark">The color of the barBackgroundColor in the dark theme.</param>
    [<Extension>]
    static member inline barBackgroundColor(this: WidgetBuilder<'msg, #IFabNavigationPage>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(NavigationPage.BarBackgroundColor.WithValue(AppTheme.create light dark))

    /// <summary>Set the color of the BarBackground.</summary>
    /// <param name="light">The color of the barBackground in the light theme.</param>
    /// <param name="dark">The color of the barBackground in the dark theme.</param>
    [<Extension>]
    static member inline barBackground(this: WidgetBuilder<'msg, #IFabNavigationPage>, light: Brush, ?dark: Brush) =
        this.AddScalar(NavigationPage.BarBackground.WithValue(AppTheme.create light dark))

    /// <summary>Set the color of the BarTextColor.</summary>
    /// <param name="light">The color of the barTextColor in the light theme.</param>
    /// <param name="dark">The color of the barTextColor in the dark theme.</param>
    [<Extension>]
    static member inline barTextColor(this: WidgetBuilder<'msg, #IFabNavigationPage>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(NavigationPage.BarTextColor.WithValue(AppTheme.create light dark))

    /// <summary>Event that is fired when the user back navigated.</summary>
    /// <param name="onBackNavigated">Msg to dispatch when the user back navigated.</param>
    [<Extension>]
    static member inline onBackNavigated(this: WidgetBuilder<'msg, #IFabNavigationPage>, onBackNavigated: 'msg) =
        this.AddScalar(NavigationPage.BackNavigated.WithValue(onBackNavigated))

    /// <summary>Event that is fired when the user presses the system back button. Doesn't support the iOS back button</summary>
    /// <param name="onBackButtonPressed">Msg to dispatch when the user presses the system back button.</param>
    /// <remarks>Setting this modifier will prevent the default behavior of the system back button. It's up to you to update the navigation stack.</remarks>
    [<Extension>]
    static member inline onBackButtonPressed(this: WidgetBuilder<'msg, #IFabNavigationPage>, onBackButtonPressed: 'msg) =
        this.AddScalar(NavigationPage.BackButtonPressed.WithValue(onBackButtonPressed))

    /// <summary>Event that is fired when the page is popped.</summary>
    /// <param name="onPopped">Msg to dispatch when then page is popped.</param>
    [<Extension; Obsolete("Use onBackNavigated instead")>]
    static member inline onPopped(this: WidgetBuilder<'msg, #IFabNavigationPage>, onPopped: 'msg) =
        this.AddScalar(NavigationPage.Popped.WithValue(fun _ -> box onPopped))

    /// <summary>Event that is fired when the page is pushed.</summary>
    /// <param name="onPushed">Msg to dispatch when then page is pushed.</param>
    [<Extension; Obsolete("Will be removed in next major version")>]
    static member inline onPushed(this: WidgetBuilder<'msg, #IFabNavigationPage>, onPushed: 'msg) =
        this.AddScalar(NavigationPage.Pushed.WithValue(fun _ -> box onPushed))

    /// <summary>Event that is fired when the page is popped to root.</summary>
    /// <param name="onPoppedToRoot">Msg to dispatch when then page is popped to root.</param>
    [<Extension; Obsolete("Use BackNavigated instead")>]
    static member inline onPoppedToRoot(this: WidgetBuilder<'msg, #IFabNavigationPage>, onPoppedToRoot: 'msg) =
        this.AddScalar(NavigationPage.PoppedToRoot.WithValue(fun _ -> box onPoppedToRoot))

[<Extension>]
type NavigationPageAttachedModifiers =
    /// <summary>Set the value for HasNavigationBar</summary>
    /// <param name= "value">true if the page has navigation bar ; otherwise, false.</param>
    [<Extension>]
    static member inline hasNavigationBar(this: WidgetBuilder<'msg, #IFabPage>, value: bool) =
        this.AddScalar(NavigationPage.HasNavigationBar.WithValue(value))

    /// <summary>Set the value for HasBackButton</summary>
    /// <param name= "value">true if the page has back button ; otherwise, false.</param>
    [<Extension>]
    static member inline hasBackButton(this: WidgetBuilder<'msg, #IFabPage>, value: bool) =
        this.AddScalar(NavigationPage.HasBackButton.WithValue(value))

    /// <summary>Set the value for BackButtonTitle</summary>
    /// <param name= "value">The title of the back button for the specified page.</param>
    [<Extension>]
    static member inline backButtonTitle(this: WidgetBuilder<'msg, #IFabPage>, value: string) =
        this.AddScalar(NavigationPage.BackButtonTitle.WithValue(value))

    /// <summary>Set the color of the IconColor.</summary>
    /// <param name="light">The color of the iconColor in the light theme.</param>
    /// <param name="dark">The color of the iconColor in the dark theme.</param>
    [<Extension>]
    static member inline iconColor(this: WidgetBuilder<'msg, #IFabPage>, light: FabColor, ?dark: FabColor) =
        this.AddScalar(NavigationPage.IconColor.WithValue(AppTheme.create light dark))

    /// <summary>Set the source of the TitleIconImageSource.</summary>
    /// <param name="light">The source of the titleIcon in the light theme.</param>
    /// <param name="dark">The source of the titleIcon in the dark theme.</param>
    [<Extension>]
    static member inline titleIcon(this: WidgetBuilder<'msg, #IFabPage>, light: ImageSource, ?dark: ImageSource) =
        this.AddScalar(NavigationPage.TitleIconImageSource.WithValue(AppTheme.create light dark))

    /// <summary>Set the source of the TitleIconImageSource.</summary>
    /// <param name="light">The source of the titleIcon in the light theme.</param>
    /// <param name="dark">The source of the titleIcon in the dark theme.</param>
    [<Extension>]
    static member inline titleIcon(this: WidgetBuilder<'msg, #IFabPage>, light: string, ?dark: string) =
        let light = ImageSource.FromFile(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromFile(v))

        NavigationPageAttachedModifiers.titleIcon(this, light, ?dark = dark)

    /// <summary>Set the source of the TitleIconImageSource.</summary>
    /// <param name="light">The source of the titleIcon in the light theme.</param>
    /// <param name="dark">The source of the titleIcon in the dark theme.</param>
    [<Extension>]
    static member inline titleIcon(this: WidgetBuilder<'msg, #IFabPage>, light: Uri, ?dark: Uri) =
        let light = ImageSource.FromUri(light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromUri(v))

        NavigationPageAttachedModifiers.titleIcon(this, light, ?dark = dark)

    /// <summary>Set the source of the TitleIconImageSource.</summary>
    /// <param name="light">The source of the titleIcon in the light theme.</param>
    /// <param name="dark">The source of the titleIcon in the dark theme.</param>
    [<Extension>]
    static member inline titleIcon(this: WidgetBuilder<'msg, #IFabPage>, light: Stream, ?dark: Stream) =
        let light = ImageSource.FromStream(fun () -> light)

        let dark =
            match dark with
            | None -> None
            | Some v -> Some(ImageSource.FromStream(fun () -> v))

        NavigationPageAttachedModifiers.titleIcon(this, light, ?dark = dark)

    /// <summary>Sets the value for TitleView</summary>
    /// <param name= "content">View to use as a title for the navigation page.</param>
    [<Extension>]
    static member inline titleView<'msg, 'marker, 'contentMarker when 'marker :> IFabPage and 'contentMarker :> IFabView>
        (
            this: WidgetBuilder<'msg, 'marker>,
            content: WidgetBuilder<'msg, 'contentMarker>
        ) =
        this.AddWidget(NavigationPage.TitleView.WithValue(content.Compile()))

    /// <summary>Link a ViewRef to access the direct NavigationPage control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IFabNavigationPage>, value: ViewRef<NavigationPage>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))

[<Extension>]
type NavigationPagePlatformModifiers =
    /// <summary>iOS platform specific. Sets a value that hides the navigation bar separator.</summary>
    /// <param name="value">true to hide the separator. Otherwise, false.</param>
    [<Extension>]
    static member inline hideNavigationBarSeparator(this: WidgetBuilder<'msg, #IFabNavigationPage>, value: bool) =
        this.AddScalar(NavigationPage.HideNavigationBarSeparator.WithValue(value))

    [<Extension>]
    static member inline isNavigationBarTranslucent(this: WidgetBuilder<'msg, #IFabNavigationPage>, value: bool) =
        this.AddScalar(NavigationPage.IsNavigationBarTranslucent.WithValue(value))

    [<Extension>]
    static member inline prefersLargeTitles(this: WidgetBuilder<'msg, #IFabNavigationPage>, value: bool) =
        this.AddScalar(NavigationPage.PrefersLargeTitles.WithValue(value))

[<Extension>]
type NavigationPageYieldExtensions =
    [<Extension>]
    static member inline Yield(_: CollectionBuilder<'msg, #IFabNavigationPage, IFabPage>, x: WidgetBuilder<'msg, #IFabPage>) : Content<'msg> =
        { Widgets = MutStackArray1.One(x.Compile()) }

    [<Extension>]
    static member inline Yield(_: CollectionBuilder<'msg, #IFabNavigationPage, IFabPage>, x: WidgetBuilder<'msg, Memo.Memoized<#IFabPage>>) : Content<'msg> =
        { Widgets = MutStackArray1.One(x.Compile()) }
