using System;
using System.Collections.Generic;
using System.Windows;
using Telerik.Windows.Controls.ConversationalUI;
using Jviz.Helpers;
using OpenAI.Chat;

namespace Jviz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Author User { get; private set; } = new Author("User");
        public Author Assistant { get; private set; } = new Author("Assistant");

        private Speech Spch { get; set; }
        private Wake Wk { get; set; }
        private Chat ChatService { get; set; }
        private Dictionary<string, string> ProfileMapping = new Dictionary<string, string>();
        private AudioMonitor audioMonitor;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            ChatService = new Chat(MainChat);
            Spch = new Speech(ChatService);
            Wk = new Wake(ChatService);

            // Start audio monitoring
            audioMonitor = new AudioMonitor(MicInputMeter);
            audioMonitor.StartMonitoring();

            // Subscribe to events
            Wk.WakeWordDetected += OnWakeWordDetected;
            Wk.ProcessingDone += OnProcessingDone;
        }

        private async void btnIdentifySpeaker_Click(object sender, RoutedEventArgs e)
        {
            await Wk.StartWakeWordDetection();
        }

        private void btnRegisterProfile_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            audioMonitor.StopMonitoring();
            Wk.WakeWordDetected -= OnWakeWordDetected;
            Wk.ProcessingDone -= OnProcessingDone;
        }

        private void MainChat_SendMessage(object sender, SendMessageEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(e.Message.ToString()))
                {
                     ChatService.ReceiveMessage(e.Message.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnWakeWordDetected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                WakeWordIndicator.IsBusy = true;
            });
        }

        private void OnProcessingDone()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                WakeWordIndicator.IsBusy = false;
            });
        }
    }
}
