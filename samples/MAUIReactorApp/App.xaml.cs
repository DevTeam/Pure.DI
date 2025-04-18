﻿using MauiReactor;
using MAUIReactorApp.Components;
using MAUIReactorApp.Resources.Styles;

namespace MAUIReactorApp;

public partial class App
{
    public App(IServiceProvider serviceProvider)
        : base(serviceProvider) =>
        InitializeComponent();
}

public abstract class MauiReactorApplication : ReactorApplication<HomePage>
{
    protected MauiReactorApplication(IServiceProvider serviceProvider)
        : base(serviceProvider) =>
        this.UseTheme<ApplicationTheme>();
}