using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mapbox.BaseModule.Utilities
{
    public class MeasureTime : IDisposable
    {
        private Stopwatch _sw;
        private string _format;
        private float _startingTime;
		
        public MeasureTime(string format = "")
        {
            _format = format;
            _sw = new Stopwatch();
            _startingTime = GetCurrentTime();
            _sw.Start();
        }
		
        public void Dispose()
        {
            _sw.Stop();
            Debug.Log(string.Format(_format, _startingTime, GetCurrentTime(), _sw.ElapsedMilliseconds));
        }

        private float GetCurrentTime()
        {
            return Time.timeSinceLevelLoad;
        }
    }
}