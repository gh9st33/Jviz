using NAudio.Wave;
using System;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace Jviz.Helpers
{
    public class AudioMonitor
    {
        private WaveInEvent waveIn;
        private RadCircularProgressBar progressBar;

        public AudioMonitor(RadCircularProgressBar progressBar)
        {
            this.progressBar = progressBar;
            Initialize();
        }

        private void Initialize()
        {
            waveIn = new WaveInEvent();
            waveIn.DataAvailable += WaveIn_DataAvailable;
        }

        public void StartMonitoring()
        {
            waveIn.StartRecording();
        }

        public void StopMonitoring()
        {
            waveIn.StopRecording();
            waveIn.Dispose();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // Calculate the RMS value of the audio data
            double sum = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                sum += sample * sample;
            }
            double rms = Math.Sqrt(sum / (e.BytesRecorded / 2));
            double rmsNormalized = rms / short.MaxValue;

            // Update the progress bar on the UI thread
            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.Value = rmsNormalized * 100; // Convert to percentage
            });
        }
    }
}