using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapbox.BaseModule.Data.Tasks
{
    public abstract class TaskResult
    {
        protected List<Exception> _exceptions;
		
        public void AddException(Exception e)
        {
            if (_exceptions == null)
                _exceptions = new List<Exception>();

            _exceptions.Add(e);
        }

        public IEnumerable<Exception> GetExceptions()
        {
            return _exceptions;
        }
        
        public string ExceptionsAsString
        {
            get
            {
                if (null == _exceptions || _exceptions.Count == 0)
                {
                    return string.Empty;
                }

                return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message + Environment.NewLine + e.StackTrace).ToArray());
            }
        }
    }
}