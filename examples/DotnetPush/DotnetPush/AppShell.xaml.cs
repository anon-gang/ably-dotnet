﻿using DotnetPush.ViewModels;
using DotnetPush.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DotnetPush
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LogPage");
        }
    }
}
