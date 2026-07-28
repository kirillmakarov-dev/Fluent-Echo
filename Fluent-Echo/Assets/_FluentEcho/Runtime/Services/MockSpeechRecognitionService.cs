using System;
using System.Collections;
using FluentEcho.Data;
using UnityEngine;

namespace FluentEcho.Services
{
    public sealed class MockSpeechRecognitionService : MonoBehaviour, ISpeechRecognitionService
    {
        [SerializeField] private string simulatedTranscript = "the dog is big";
        [SerializeField] private float responseDelaySeconds = 1.2f;

        private Coroutine responseRoutine;

        public event Action<string> TranscriptUpdated;
        public event Action AnalysisStarted;
        public event Action ListeningStopped;
        public event Action<string> StatusChanged;
        public event Action<string> Failed
        {
            add { }
            remove { }
        }

        public bool IsListening { get; private set; }
        public bool IsReady => true;

        public void Configure(SpeechExerciseSO exercise)
        {
            if (exercise != null)
                simulatedTranscript = exercise.GetRecognitionPrompt();
        }

        public void Prepare()
        {
            StatusChanged?.Invoke("Demo mode is ready. Press Start Speaking to preview a correct answer.");
        }

        public void StartListening()
        {
            if (IsListening)
                return;

            CancelRoutine();
            IsListening = true;
            responseRoutine = StartCoroutine(CompleteAfterDelay());
        }

        public void StopListening()
        {
            if (!IsListening)
                return;

            CancelRoutine();
            IsListening = false;
            AnalysisStarted?.Invoke();
            TranscriptUpdated?.Invoke(simulatedTranscript);
            ListeningStopped?.Invoke();
        }

        public void Cancel()
        {
            CancelRoutine();
            IsListening = false;
        }

        public void SetSimulatedTranscript(string transcript)
        {
            simulatedTranscript = transcript;
        }

        private IEnumerator CompleteAfterDelay()
        {
            yield return new WaitForSecondsRealtime(responseDelaySeconds);
            responseRoutine = null;
            IsListening = false;
            AnalysisStarted?.Invoke();
            TranscriptUpdated?.Invoke(simulatedTranscript);
            ListeningStopped?.Invoke();
        }

        private void CancelRoutine()
        {
            if (responseRoutine == null)
                return;

            StopCoroutine(responseRoutine);
            responseRoutine = null;
        }
    }
}
