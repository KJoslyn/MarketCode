using Core.Model;
using Serilog;
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
        private readonly string _id;

        public DelayedActionThread(Func<bool> checkStillValid, Action performAction, DateTime performAtTime, int sleepMs, string id = "")
        {
            CheckStillValid = checkStillValid;
            PerformAction = performAction;
            _performAtTime = performAtTime;
            _sleepMs = sleepMs;
            _id = id;
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
                Log.Information("id {id}: Valid = {valid} at time {@Time}", _id, valid, DateTime.Now);
            }
            while (valid && DateTime.Now < _performAtTime);
            if (valid)
            {
                PerformAction();
            }
            else
            {
                Log.Information("id {id}: Found to be invalid at time {@Time}", _id, DateTime.Now);
            }
        }
    }
}
