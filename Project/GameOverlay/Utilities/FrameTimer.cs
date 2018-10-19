﻿using System;
using System.Diagnostics;
using System.Threading;
using GameOverlay.Graphics;

namespace GameOverlay.Utilities
{
    /// <inheritdoc />
    /// <summary>
    ///     Creates a drawing loop and optionally limits fps
    /// </summary>
    /// <seealso cref="T:System.IDisposable" />
    public class FrameTimer : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="device">The device.</param>
        public delegate void FrameStageNotifyEventHandler(FrameTimer timer, D2DDevice device);

        private bool _exitTimerThread;
        private Stopwatch _stopwatch;
        private Thread _thread;
        private volatile bool _isPaused;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        public FrameTimer()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        /// <param name="framesPerSecond">The frames per second.</param>
        public FrameTimer(int framesPerSecond)
        {
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        /// <param name="framesPerSecond">The frames per second.</param>
        /// <param name="device">The device.</param>
        /// <exception cref="ArgumentNullException">device</exception>
        public FrameTimer(int framesPerSecond, D2DDevice device)
        {
            FramesPerSecond = framesPerSecond;
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <exception cref="ArgumentNullException">device</exception>
        public FrameTimer(D2DDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="framesPerSecond">The frames per second.</param>
        /// <exception cref="ArgumentNullException">device</exception>
        public FrameTimer(D2DDevice device, int framesPerSecond)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>
        ///     Gets or sets the device.
        /// </summary>
        /// <value>
        ///     The device.
        /// </value>
        public D2DDevice Device { get; set; }

        /// <summary>
        ///     Gets or sets the frames per second.
        /// </summary>
        /// <value>
        ///     The frames per second.
        /// </value>
        public int FramesPerSecond { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        public bool IsPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }

        /// <summary>
        ///     Occurs when [frame starting].
        /// </summary>
        public event FrameStageNotifyEventHandler OnFrameStarting;

        /// <summary>
        ///     Occurs when [on frame].
        /// </summary>
        public event FrameStageNotifyEventHandler OnFrame;

        /// <summary>
        ///     Occurs when [frame ending].
        /// </summary>
        public event FrameStageNotifyEventHandler OnFrameEnding;

        /// <summary>
        ///     Finalizes an instance of the <see cref="FrameTimer" /> class.
        /// </summary>
        ~FrameTimer()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Starts this instance.
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (_thread != null) return false;

            CreateThread();

            return true;
        }

        /// <summary>
        ///     Stops this instance.
        /// </summary>
        public void Stop()
        {
            IsPaused = false;
            ExitThread();
        }

        /// <summary>
        ///     Resumes this instance.
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        ///     Pauses this instance.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        private void FrameTimerMethod()
        {
            _stopwatch = new Stopwatch();
            IsPaused = false;

            while (!_exitTimerThread)
            {
                while (IsPaused)
                    Thread.Sleep(100);

                int currentFps = FramesPerSecond;

                if (currentFps < 1)
                {
                    InvokeEvents();
                    continue;
                }

                int sleeptimePerFrame = 1000 / currentFps;

                for (int i = 1; i < currentFps; i++)
                {
                    _stopwatch.Restart();

                    InvokeEvents();

                    _stopwatch.Stop();

                    int currentSleeptime = sleeptimePerFrame - (int) _stopwatch.ElapsedMilliseconds;

                    if (currentSleeptime > 0) Thread.Sleep(currentSleeptime);
                }
            }

            _stopwatch = null;
            IsPaused = false;
        }

        private void InvokeEvents()
        {
            if (Device == null) throw new InvalidOperationException("Renderer is null");

            OnFrameStarting?.Invoke(this, Device);

            if(!Device.IsDrawing) Device.BeginScene();

            OnFrame?.Invoke(this, Device);

            if(Device.IsDrawing) Device.EndScene();

            OnFrameEnding?.Invoke(this, Device);
        }

        private void CreateThread()
        {
            if (_thread != null) return;

            _exitTimerThread = false;

            _thread = new Thread(FrameTimerMethod)
            {
                IsBackground = true
            };

            _thread.Start();
        }

        private void ExitThread()
        {
            if (_thread == null || _exitTimerThread) return;

            _exitTimerThread = true;

            try
            {
                _thread.Join();
            }
            catch
            {
                // ignored
            }

            _exitTimerThread = false;
            _thread = null;
            IsPaused = true;
        }

        #region IDisposable Support

        private bool _disposedValue;

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (_thread != null && !_exitTimerThread) ExitThread();

            Device = null;

            OnFrameStarting = null;
            OnFrame = null;
            OnFrameEnding = null;

            _disposedValue = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}