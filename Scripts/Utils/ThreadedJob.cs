using System;
using System.Collections;

namespace Utils
{
    /// <summary>
    /// Took from http://answers.unity3d.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html
    /// Main restriction for multithreading: it must not rely on Unity objects and Unity engine.
    /// 
    /// USAGE:
    /// public class DeserializeJson: ThreadedJob
    /// {
    ///     private string json;
    ///     public JSONObject Result { get; private set; }
    ///     public DeserializeJson(string json)
    ///     {
    ///         this.json = json;
    ///     }
    ///     protected override void ThreadFunction()
    ///     {
    ///          Result = new JSONObject(json);
    ///     }
    /// }
    /// .....
    /// var job = new DeserializeJson(request.LastRequestWww.text);
    /// job.Start();
    /// 
    /// yield return job.WaitFor();
    /// 
    /// if (!string.IsNullOrEmpty(job.Error))
    /// {
    ///     yield break;
    /// }
    /// 
    /// var ret = job.Result;
    ///  .....
    /// </summary>
    public abstract class ThreadedJob
    {
        private bool isDone = false;

        private object isDoneLock = new object();

        private string error = null;

        private object errorLock = new object();

        private System.Threading.Thread thread = null;

        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (isDoneLock)
                {
                    tmp = isDone;
                }
                return tmp;
            }
            set
            {
                lock (isDoneLock)
                {
                    isDone = value;
                }
            }
        }

        public string Error
        {
            get
            {
                string tmp;
                lock (errorLock)
                {
                    tmp = error;
                }
                return tmp;
            }
            set
            {
                lock (errorLock)
                {
                    error = value;
                }
            }
        }

        /// <summary>
        /// Start thread
        /// </summary>
        public virtual void Start()
        {
            thread = new System.Threading.Thread(Run);
            thread.Start();
        }

        /// <summary>
        ///  Eric Lipert does not recommend use it: http://stackoverflow.com/a/1560567
        /// </summary>
        public virtual void Abort()
        {
            thread.Abort();
        }

        /// <summary>
        /// Main process job must be defined in this function
        /// </summary>
        protected abstract void ThreadFunction();

        /// <summary>
        /// Called when job is finished
        /// </summary>
        protected virtual void OnFinished() { }

        /// <summary>
        /// Update thread status. Aimed to use in MonoBehaviour.Update() function
        /// </summary>
        /// <returns></returns>
        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }

            if (!string.IsNullOrEmpty(Error))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Same as Update but for coroutines
        /// </summary>
        /// <returns></returns>
        public IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }

        private void Run()
        {
            try
            {
                ThreadFunction();
                IsDone = true;
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }
    }
}