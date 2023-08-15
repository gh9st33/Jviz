using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Speaker;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using System.Windows;
using Jviz.Helpers;

namespace Jviz
{
    public class Speech
    {
        public static Chat chatControl;
        public Speech(Chat chatcontrol)
        {
            chatControl = chatcontrol;
        }
        public static async Task VerificationEnroll(SpeechConfig config, Dictionary<string, string> profileMapping, string profileName)
        {
            try
            {
                using (var client = new VoiceProfileClient(config))
                using (var profile = await client.CreateProfileAsync(VoiceProfileType.TextDependentVerification, "en-us"))
                {
                    var phraseResult = await client.GetActivationPhrasesAsync(VoiceProfileType.TextDependentVerification, "en-us");
                    using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
                    {
                        chatControl.SendMessage($"Enrolling profile id {profile.Id}.");
                        // give the profile a human-readable display name
                        profileMapping.Add(profile.Id, profileName);

                        VoiceProfileEnrollmentResult result = null;
                        while (result is null || result.RemainingEnrollmentsCount > 0)
                        {
                            chatControl.SendMessage($"Speak the passphrase, \"${phraseResult.Phrases[0]}\"");
                            result = await client.EnrollProfileAsync(profile, audioInput);
                            chatControl.SendMessage($"Remaining enrollments needed: {result.RemainingEnrollmentsCount}");
                        }

                        if (result.Reason == ResultReason.EnrolledVoiceProfile)
                        {
                            await SpeakerVerify(config, profile, profileMapping);
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = VoiceProfileEnrollmentCancellationDetails.FromResult(result);
                            chatControl.SendMessage($"CANCELED {profile.Id}: ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                chatControl.SendMessage(ex.Message);
            }
        }

        public static async Task SpeakerVerify(SpeechConfig config, VoiceProfile profile, Dictionary<string, string> profileMapping)
        {
            try
            {
                var speakerRecognizer = new SpeakerRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());
                var model = SpeakerVerificationModel.FromProfile(profile);

                chatControl.SendMessage("Speak the passphrase to verify: \"My voice is my passport, please verify me.\"");
                var result = await speakerRecognizer.RecognizeOnceAsync(model);
                chatControl.SendMessage($"Verified voice profile for speaker {profileMapping[result.ProfileId]}, score is {result.Score}");
            }
            catch (Exception ex)
            {
                chatControl.SendMessage(ex.Message);
            }
        }
    }
}
