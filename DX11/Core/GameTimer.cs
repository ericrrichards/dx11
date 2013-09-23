namespace Core {
    using System;
    using System.Diagnostics;
    using System.Threading;

    public class GameTimer {
        private readonly double _secondsPerCount;
        private double _deltaTime;

        private long _baseTime;
        private long _pausedTime;
        private long _stopTime;
        private long _prevTime;
        private long _currTime;

        private bool _stopped;

        public GameTimer() {
            _secondsPerCount = 0.0;
            _deltaTime = -1.0;
            _baseTime = 0;
            _pausedTime = 0;
            _prevTime = 0;
            _currTime = 0;
            _stopped = false;

            var countsPerSec = Stopwatch.Frequency;
            _secondsPerCount = 1.0 / countsPerSec;

        }

        public float TotalTime {
            get {
                if (_stopped) {
                    return (float)(((_stopTime - _pausedTime) - _baseTime) * _secondsPerCount);
                } else {
                    return (float)(((_currTime - _pausedTime) - _baseTime) * _secondsPerCount);
                }
            }
        }
        public float DeltaTime {
            get { return (float)_deltaTime; }
        }
        public float FrameTime { get; set; }

        public void Reset() {
            var curTime = Stopwatch.GetTimestamp();
            _baseTime = curTime;
            _prevTime = curTime;
            _stopTime = 0;
            _stopped = false;
        }

        public void Start() {
            var startTime = Stopwatch.GetTimestamp();
            if (_stopped) {
                _pausedTime += (startTime - _stopTime);
                _prevTime = startTime;
                _stopTime = 0;
                _stopped = false;
            }
        }

        public void Stop() {
            if (!_stopped) {
                var curTime = Stopwatch.GetTimestamp();
                _stopTime = curTime;
                _stopped = true;
            }
        }

        public void Tick() {
            if (_stopped) {
                _deltaTime = 0.0;
                return;
            }
            //while (_deltaTime < FrameTime) {
                var curTime = Stopwatch.GetTimestamp();
                _currTime = curTime;

                _deltaTime = (_currTime - _prevTime) * _secondsPerCount;
                //Thread.Sleep(0);
            //}
            _prevTime = _currTime;
            if (_deltaTime < 0.0) {
                _deltaTime = 0.0;
            }
        }
    }
}