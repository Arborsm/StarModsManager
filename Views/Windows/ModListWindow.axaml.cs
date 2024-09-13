using System;
using Avalonia.ReactiveUI;
using ReactiveUI;
using StarModsManager.ViewModels;

namespace StarModsManager.Views.Windows;

public partial class ModListWindow : ReactiveWindow<ModListViewModel>
{
    public ModListWindow()
    {
        InitializeComponent();
        this.WhenActivated(action => action(ViewModel!.BuyMusicCommand.Subscribe(Close)));
    }
}