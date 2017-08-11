﻿using Xamarin.Forms;

namespace App2
{
    public class MainViewModel : ObservableObject
    {
        private readonly IAblyService _ably;
        private string _connectionStatus = "Initialised";
        private string _currentCar;
        private string _info;

        public MainViewModel(IAblyService ably)
        {
            _ably = ably;
            SendMessageCommand = new Command(() =>
            {
                _ably.SendMessage("test", "test", "Martin");
            });
            StartCommand = new Command(() =>
            {
                _ably.Connect();
            });
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string CurrentCar
        {
            get => _currentCar;
            set => SetProperty(ref _currentCar, value);
        }

        public string Info
        {
            get => _info;
            set => SetProperty(ref _info, value);
        }

        public Command SendMessageCommand { get; }

        public Command StartCommand { get; }
    }
}
