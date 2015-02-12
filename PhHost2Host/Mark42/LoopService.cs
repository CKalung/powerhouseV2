using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mark42
{
    /// <summary>
    /// Base class for all loop operations
    /// </summary>
    public abstract class LoopService : IService
    {
        #region Private members

        protected readonly ILogger _log;
        private readonly int _loopInterval;
        private readonly string _threadName;

        private Thread _loopThread;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes new instance.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="threadName">Name of the thread.</param>
        /// <param name="loopInterval">Interval between iterations.</param>
        /// <exception cref="ArgumentNullException">Either of parameters in <c>null</c>.</exception>
        protected LoopService(ILogger log, string threadName, int loopInterval)
        {
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }
            if (threadName == null)
            {
                throw new ArgumentNullException("threadName");
            }

            _log = log;
            _threadName = threadName;
            _loopInterval = loopInterval;
        }
        #endregion

        #region Properties

        public bool IsRunning { get; private set; }

        #endregion

        #region IService

        /// <summary>
        /// Starts loop thread
        /// </summary>
        /// <remarks>If call method several times consecutively - only first call will take affect; other calls will be ignored.</remarks>
        public virtual void Start()
        {
            if (IsRunning)
            {
                _log.Error("'{0}' is already started.", _threadName);
                return;
            }

            StartThread();

            //_log.Debug("'{0}' started.", _threadName);
        }

        /// <summary>
        /// Stops loop thread
        /// </summary>
        /// <remarks>If call method several times consecutively - only first call will take affect; other calls will be ignored.</remarks>
        public virtual void Stop()
        {
            if (!IsRunning)
            {
                _log.Error("{0} is not started.", _threadName);
                return;
            }

            //_log.Debug("Stopping '{0}'.", _threadName);
            IsRunning = false;
            _loopThread.Abort();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// This method will be called in the loop.
        /// </summary>
        protected abstract void Action();

        #endregion

        #region Private methods

        private void StartThread()
        {
            IsRunning = true;
            _loopThread = new Thread(Loop) { IsBackground = true, Name = _threadName };
            _loopThread.Start();
        }

        private void Loop()
        {
            while (true)
            {
                try
                {
                    Action();
                }
                catch (ThreadAbortException)
                {
                    //_log.Debug("Thread has been aborted.");
                }
                catch (Exception exception)
                {
                    string message = String.Format("Error caught in '{0}'.", _threadName);
                    _log.Error(message, exception);
                }
                Thread.Sleep(_loopInterval);
            }
        }

        #endregion
    }
}
