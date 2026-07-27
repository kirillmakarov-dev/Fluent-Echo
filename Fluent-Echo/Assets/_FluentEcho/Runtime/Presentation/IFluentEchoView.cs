using System;

namespace FluentEcho.Presentation
{
    public interface IFluentEchoView
    {
        event Action MicPressed;
        event Action DemoPressed;
        event Action RetryPressed;
        event Action ListenPressed;
        event Action<bool> MockModeChanged;

        void Build(string prompt, string[] targetWords);
        void SetProgress(string progress);
        void SetStatus(string status);
        void SetTranscript(string transcript);
        void SetWordMatches(bool[] matches);
        void SetListening(bool listening);
        void SetSuccess(bool success);
        void SetMode(bool mockMode);
    }
}
