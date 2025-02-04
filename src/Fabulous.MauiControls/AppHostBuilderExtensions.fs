﻿namespace Fabulous.Maui

open Fabulous
open System.Runtime.CompilerServices
open Microsoft.Maui.Hosting
open Microsoft.Maui.Controls.Hosting
open System

[<Extension>]
type AppHostBuilderExtensions =
    [<Extension>]
    static member UseFabulousApp<'model, 'msg, 'marker when 'marker :> IFabApplication>
        (
            this: MauiAppBuilder,
            program: Program<unit, 'model, 'msg, 'marker>
        ) : MauiAppBuilder =
        this.UseMauiApp(fun (_serviceProvider: IServiceProvider) -> (Program.startApplication program) :> Microsoft.Maui.IApplication)

    [<Extension>]
    static member UseFabulousApp<'arg, 'model, 'msg, 'marker when 'marker :> IFabApplication>
        (
            this: MauiAppBuilder,
            program: Program<'arg, 'model, 'msg, 'marker>,
            arg: 'arg
        ) : MauiAppBuilder =
        this.UseMauiApp(fun (_serviceProvider: IServiceProvider) -> (Program.startApplicationWithArgs arg program) :> Microsoft.Maui.IApplication)
