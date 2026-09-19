using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.UnityMapService.DataSources
{
    public abstract class PbfSource<T> : UnitySource<T> where T : MapboxTileData, new()
    {
        protected Dictionary<CanonicalTileId, Tile> _waitingList;
        private TypeMemoryCache<T> _memoryCache;
        private HashSet<CanonicalTileId> _activeRequestsToCancel;

        protected PbfSource(DataFetchingManager dataFetchingManager, MapboxCacheManager cacheManager, VectorSourceSettings settings) : base(dataFetchingManager, cacheManager, settings.TilesetId)
        {
            _waitingList = new Dictionary<CanonicalTileId, Tile>();
            _activeRequestsToCancel = new HashSet<CanonicalTileId>();
            
            _memoryCache = RegisterTypeToMemoryCache<T>(this.GetHashCode(), settings.CacheSize);
            _memoryCache.CacheItemDisposed += (t) =>
            {
                CacheItemDisposed(t);
            };
        }
        
        public override void LoadTile(CanonicalTileId requestedDataTileId) => LoadTileCore(requestedDataTileId);
        
        public override bool CheckInstantData(CanonicalTileId tileId)
        {
            return _memoryCache.Exists(tileId);
        }
        
        public override bool GetInstantData(CanonicalTileId tileId, out T data)
        {
            return _memoryCache.Get(tileId, out data);
        }

        public override void InvalidateData(CanonicalTileId tileId)
        {
            _memoryCache.Remove(tileId);
            RemoveData(_tilesetId, tileId.Z, tileId.X, tileId.Y);
        }

        public override bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
        {
            foreach (var id in retainedTiles)
            {
                if (CheckInstantData(id)) continue;
                
                LoadTileCore(id);
            }

            if (_waitingList.Count > 0)
            {
                _activeRequestsToCancel.Clear();
                foreach (var activeTile in _waitingList)
                {
                    if (!retainedTiles.Contains(activeTile.Key))
                    {
                        _activeRequestsToCancel.Add(activeTile.Key);
                    }
                }

                foreach (var id in _activeRequestsToCancel)
                {
                    CancelActiveRequests(id);
                }
            }

            _memoryCache.RetainTiles(retainedTiles);
            return true;
        }
        
        public override void CancelActiveRequests(CanonicalTileId unityTileId)
        {
            if (_waitingList.ContainsKey(unityTileId))
            {
                var tile = _waitingList[unityTileId];
                if (tile != null)
                    CancelFetching(tile, _tilesetId);
                _waitingList.Remove(unityTileId);
            }
        }
        
        public virtual void ClearMemoryCache()
        {
            _memoryCache.OnDestroy();
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var tile in _waitingList)
            {
                tile.Value.Cancel();
            }
            _memoryCache.OnDestroy();
        }

        
        
        
        //COROUTINE METHODS only used in initialization so far
        #region coroutines
        public override IEnumerator LoadTileCoroutine(CanonicalTileId requestedDataTileId, Action<T> callback = null)
        {
            T resultData = null;
            if (GetInstantData(requestedDataTileId, out resultData))
            {
                
            }
            else if (_waitingList.ContainsKey(requestedDataTileId))
            {
                while(_waitingList.ContainsKey(requestedDataTileId))
                {
                    yield return null;
                }
                GetInstantData(requestedDataTileId, out resultData);
            }
            else
            {
                _waitingList[requestedDataTileId] = null;
                yield return GetTileData<T>(requestedDataTileId, _tilesetId,
                    null,
                    1,
                    (data) =>
                    {
                        resultData = data;
                        _waitingList.Remove(requestedDataTileId);

                        if (data != null)
                        {
                            data.CacheType = CacheType.FileCache;
                            _memoryCache.Add(data);
                            CheckExpiration(data);
                        }

                    });
                
                if(resultData == null)
                {
                    var dataTile = CreateTile(requestedDataTileId, _tilesetId);
                    _waitingList[requestedDataTileId] = dataTile;
                    var working = true;
                    WebRequestData(dataTile, (fetchingResult) =>
                    {
                        _waitingList.Remove(dataTile.Id);
                        if (dataTile.CurrentTileState == TileState.Loaded)
                        {
                            resultData = VectorReceivedFromWeb(dataTile);
                        }
                        working = false;
                    });
                    while (working)
                    {
                        yield return null;
                    }
                }
            }

            callback?.Invoke(resultData);
        }

        public override IEnumerator LoadTilesCoroutine(IEnumerable<CanonicalTileId> retainedTiles, Action<List<T>> callback = null)
        {
            if (callback != null)
            {
                var results = new List<T>();
                var coroutines = retainedTiles.Select(x =>
                    LoadTileCoroutine(x,
                        (data) =>
                        {
                            if (data != null) results.Add(data);
                        }));
                yield return coroutines.WaitForAll();
                callback?.Invoke(results);
            }
            else
            {
                var coroutines = retainedTiles.Select(x => LoadTileCoroutine(x));
                yield return coroutines.WaitForAll();
            }
        }
        #endregion
        
        
        private void LoadTileCore(CanonicalTileId requestedDataTileId, Action<T> callback = null)
        {
            if (IsInProgress(requestedDataTileId))
            {
                callback?.Invoke(null);
                return;
            }
            
            var dataTile = CreateTile(requestedDataTileId, _tilesetId);
            _waitingList[requestedDataTileId] = dataTile;
            
            var dataTask = GetTileInfoAsync<T>(requestedDataTileId, _tilesetId, 0);
            if (dataTask != null) // can be null if sqlite cache isn't available
            {
                if (dataTask.IsCompleted) //supporting instant calls
                    HandleResponse(dataTask.DataResult);
                else 
                    dataTask.DataCompleted += (task, cacheItem) => { HandleResponse(cacheItem); };
            }
            else
            {
                CreateWebRequest(callback, dataTile);
            }

            void HandleResponse(T cacheItem)
            {
                _waitingList.Remove(requestedDataTileId);
                if (dataTile.CurrentTileState == TileState.Canceled)
                {
                    callback?.Invoke(null);
                    return;
                }
                else if (cacheItem != null)
                {
                    _memoryCache.Add(cacheItem);
                    CheckExpiration(cacheItem);
                    callback?.Invoke(cacheItem);
                }
                else
                {
                    CreateWebRequest(callback, dataTile);
                }
            }
        }

        private void CreateWebRequest(Action<T> callback, ByteArrayTile dataTile)
        {
            _waitingList[dataTile.Id] = dataTile;
            WebRequestData(dataTile, (fetchingResult) =>
            {
                var resultDataItem = VectorReceivedFromWeb(dataTile);
                if (_waitingList.ContainsKey(dataTile.Id))
                {
                    _waitingList.Remove(dataTile.Id);
                }
                callback?.Invoke(resultDataItem);
            });
        }

        private T VectorReceivedFromWeb(ByteArrayTile tile)
        {
            tile.AddLog(string.Format("{0} - {1}", Time.unscaledTime, " VectorReceivedHandler"));
            if (tile.CurrentTileState != TileState.Loaded)
            {
                //aborted web requests end up here
                return null;
            }

            var cacheItem = CreateVectorData(tile);
            _memoryCache.Add(cacheItem);
            SaveBlob(cacheItem, true);
            return cacheItem;
        }

        protected abstract ByteArrayTile CreateTile(CanonicalTileId canonicalTileId, string tilesetId);
        
        protected abstract T CreateVectorData(ByteArrayTile tile);
        
        private void CheckExpiration(T cacheItem)
        {
            if (cacheItem.ExpirationDate != null && 
                DateTime.Compare(cacheItem.ExpirationDate.Value, DateTime.Now) < 0)
            {
                TileExpired(cacheItem.TilesetId, cacheItem.TileId);
                var dataTile = CreateTile(cacheItem.TileId, cacheItem.TilesetId);
                dataTile.ETag = cacheItem.ETag;
                _waitingList.Add(cacheItem.TileId, dataTile);
                WebRequestUpdate(dataTile, (result) =>
                {
                    if (result.State == WebResponseResult.Failed)
                    {
                        Debug.LogError(result.ExceptionsAsString);
                        return;
                    }
                    
                    var tile = result.Tile as ByteArrayTile;
                    if (tile == null)
                        return;
                    
                    _waitingList.Remove(cacheItem.TileId);
                    if (tile.CurrentTileState != TileState.Canceled)
                    {
                        if (tile.StatusCode == 200)
                        {
                            //Debug.Log(string.Format("{0} - {1} : expired and returned 200, cached etag {0} new etag {1}", cacheItem.TileId, cacheItem.TilesetId, cacheItem.ETag, dataTile.ETag));
                            VectorReceivedFromWeb(tile);
                        }
                        else if (tile.StatusCode == 304)
                        {
                            //not changed, just update meta?
                            //Debug.Log(string.Format("{0} - {1} : expired but not changed, just update meta?", cacheItem.TileId, cacheItem.TilesetId));
                            UpdateExpiration(tile.Id, tile.TilesetId, tile.ExpirationDate);
                        }
                        TileUpdated(tile.TilesetId, tile.Id);
                    }
                });
                //Debug.Log(cacheItem.TileId + " tile needs an update");
            }
            else
            {
                //Debug.Log(cacheItem.TileId + " doesnt needs an update");
            }
        }
        
        protected bool IsInProgress(CanonicalTileId requestedDataTileId)
        {
            return _waitingList.ContainsKey(requestedDataTileId) || IsActiveRequest(requestedDataTileId);
        }
    }
}