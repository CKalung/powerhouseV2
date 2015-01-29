using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mark42
{
    public interface IService
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
    }
}
