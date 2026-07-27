using System;
using FluentEcho.Data;

namespace FluentEcho.Services
{
    public interface ISpeechRecognitionService
    {
        event Action<string> TranscriptUpdated;
        event Action AnalysisStarted;
        event Action ListeningStopped;
        event Action<string> Failed;
        event Action<string> StatusChanged;

        bool IsListening { get; }
        bool IsReady { get; }

        void Configure(SpeechExerciseSO exercise);
        void Prepare();
        void StartListening();
        void StopListening();
        void Cancel();
    }
}
