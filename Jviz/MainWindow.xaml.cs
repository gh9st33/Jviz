using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.VisualBasic;
using Telerik.Windows.Controls.ConversationalUI;
using Telerik.Windows.Controls;
using Jviz.Helpers;
using OpenAI.Chat;

namespace Jviz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Author User { get; set; } = new Author("User");
        public Author Assistant { get; set; } = new Author("Assistant");

        Speech Spch { get; set; }
        Wake Wk { get; set; }
        Chat ChatService { get; set; }
        Dictionary<string, string> ProfileMapping = new Dictionary<string, string>();
        private AudioMonitor audioMonitor;
        public MainWindow()
        {
            InitializeComponent();
            ChatService = new Chat(MainChat);
            Wk = ChatService.WakeService;
            Spch = new Speech(ChatService);
            audioMonitor = new AudioMonitor(MicInputMeter);
            audioMonitor.StartMonitoring();

            Wk.WakeWordDetected += OnWakeWordDetected;
            Wk.ProcessingDone += OnProcessingDone;
        }

        private void btnIdentifySpeaker_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRegisterProfile_Click(object sender, RoutedEventArgs e)
        {
            string ProfileName= txtProfileName.Text;
            if (ProfileName == "")
            {
                MessageBox.Show("Please enter a profile name");
                return;
            }

            else if (ProfileMapping.ContainsKey(ProfileName))
            {
                MessageBox.Show("Profile name already exists");
                if (listProfileNames.Items.Contains(ProfileName))
                {
                    listProfileNames.SelectedItem = ProfileName;
                }
                return;
            }
            object value = Task.Run(async () => await Speech.VerificationEnroll(Wk.Config, ProfileMapping, ProfileName));
            if (value != null)
            {
                ProfileMapping.Add(ProfileName, value.ToString());
                txtProfileName.Text = "";
                listProfileNames.Items.Add(ProfileName);
            }

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
            if (!string.IsNullOrWhiteSpace(e.Message.ToString()))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatService.ReceiveMessage(e.Message.ToString());
                });
            }
        }

        private void OnWakeWordDetected()
        {
            WakeWordIndicator.IsBusy = true;
        }

        private void OnProcessingDone()
        {
            WakeWordIndicator.IsBusy = false;
        }
    }
}
