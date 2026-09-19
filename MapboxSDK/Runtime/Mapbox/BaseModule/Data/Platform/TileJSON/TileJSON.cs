using System;
using System.Text;
using Mapbox.BaseModule.Utilities;
using Newtonsoft.Json;

namespace Mapbox.BaseModule.Data.Platform.TileJSON
{
    public class TileJSON
    {

        private IFileSource _fileSource;
        private int _timeout;


        public IFileSource FileSource { get { return _fileSource; } }


        public TileJSON(IFileSource fileSource, int timeout)
        {
            _fileSource = fileSource;
            _timeout = timeout;
        }


        public IAsyncRequest Get(string tilesetName, Action<TileJSONResponse> callback)
        {
            string url = string.Format(
                "{0}v4/{1}.json?secure"
                , Constants.Map.BaseAPI
                , tilesetName
            );

            return _fileSource.Request(
                url
                , (Response response) =>
                {
                    if (response != null && response.Data != null && response.Data.Length > 0)
                    {
                        string json = Encoding.UTF8.GetString(response.Data);
                        TileJSONResponse tileJSONResponse = JsonConvert.DeserializeObject<TileJSONResponse>(json);
                        if (tileJSONResponse != null)
                        {
                            tileJSONResponse.Source = tilesetName;
                        }
                        callback(tileJSONResponse);
                    }
                    else
                    {
                        callback(null);    
                    }
                }
                , _timeout
            );
        }




    }
}