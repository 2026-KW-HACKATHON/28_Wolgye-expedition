using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    public class DataFetchingResult
    {
        public Tile Tile;
        public WebResponseResult State;
		
        protected List<Exception> _exceptions;
        /// <summary> Exceptions that might have occured during creation of the tile. </summary>
        public ReadOnlyCollection<Exception> Exceptions
        {
            get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
        }
		
        /// <summary> Messages of exceptions otherwise empty string. </summary>
        public string ExceptionsAsString
        {
            get
            {
                if (null == _exceptions || _exceptions.Count == 0)
                {
                    return string.Empty;
                }

                return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
            }
        }

        public DataFetchingResult()
        {
			
        }
		
        public DataFetchingResult(Tile tile, WebRequestResponse webRequestResponse)
        {
            Tile = tile;
            State = webRequestResponse.Result;
            if (webRequestResponse.Exceptions != null)
            {
                _exceptions = new List<Exception>();
                foreach (var exception in webRequestResponse.Exceptions)
                {
                    _exceptions.Add(exception);
                }
            }
        }
    }
}