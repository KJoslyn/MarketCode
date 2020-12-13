using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public class DelayedActionThread
    {
        private readonly DateTime _performAtTime;
        private readonly int _sleepMs;

        public DelayedActionThread(Func<bool> checkStillValid, Action performAction, DateTime performAtTime, int sleepMs)
        {
            CheckStillValid = checkStillValid;
            PerformAction = performAction;
            _performAtTime = performAtTime;
            _sleepMs = sleepMs;
        }

        private Func<bool> CheckStillValid { get; init; }
        private Action PerformAction { get; init; }

        public void Run()
        {
            new Thread(() => PerformActionAtTime()).Start();
        }

        private void PerformActionAtTime()
        {
            bool valid = true;
            do
            {
                Thread.Sleep(_sleepMs);
                valid = CheckStillValid();
            }
            while (valid && DateTime.Now < _performAtTime);
            if (valid)
            {
                PerformAction();
            }
        }
    }
}
