using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.MapDebug.Scripts.Logging;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    [TestFixture]
    internal class SqliteTests
    {
        private ISqliteCache _sqliteCache;
        private uint _maxCacheSize = 100;
        private string testTilesetName = "test_tilesetId";
        private CanonicalTileId testTileId = new CanonicalTileId(16, 5, 5);
        private CanonicalTileId testTileIdDifferent = new CanonicalTileId(12, 1, 1);
        private string _beforeEtag = "etag1";
        private string _changeEtag = "etag2";
        
        [OneTimeSetUp]
        public void ReadySqliteDatabase()
        {
            var taskManager = new MockTaskManager();
            taskManager.Initialize();
            _sqliteCache = new MockSqliteCache(taskManager, _maxCacheSize);
            _sqliteCache.ReadySqliteDatabase();
        }

        [SetUp]
        public void Setup()
        {
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
        }

        [Test]
        public void IsUpToDateTest()
        {
            Assert.True(_sqliteCache.IsUpToDate());
        }

        [Test]
        public void MaxCacheSizeTest()
        {
            Assert.True(_sqliteCache.MaxCacheSize == _maxCacheSize);
        }

        [Test]
        public void TileCountEmptyTest()
        {
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
        }
        
        [Test]
        public void TileCountWrongTilesetTest()
        {
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName + "no"), 0);
        }

        [Test]
        public void SyncAddTest()
        {
            InsertSomeData(10, testTilesetName);
            var beforeCount = _sqliteCache.TileCount(testTilesetName);
            _sqliteCache.SyncAdd(testTilesetName, testTileIdDifferent, Array.Empty<byte>(), "", _beforeEtag, DateTime.Now, false);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), beforeCount + 1);
        }
        
        [Test]
        public void SyncAddTestNoOverwrite()
        {
            InsertSomeData(10, testTilesetName);
            var data = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            var beforeEtag = data.ETag.ToString();
            _sqliteCache.SyncAdd(testTilesetName, testTileId, Array.Empty<byte>(), "", _changeEtag, DateTime.Now, false);
            var data2 = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            Assert.AreEqual(beforeEtag, data2.ETag);
        }
        
        [Test]
        public void SyncAddTestForceOverwrite()
        {
            InsertSomeData(10, testTilesetName);
            var data = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            var beforeEtag = data.ETag.ToString();
            _sqliteCache.SyncAdd(testTilesetName, testTileId, Array.Empty<byte>(), "", _changeEtag, DateTime.Now, true);
            var data2 = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            Assert.AreNotEqual(beforeEtag, data2.ETag);
        }

        [Test]
        public void TileCountPostAddTest()
        {
            var insertCount = 10;
            InsertSomeData(insertCount, testTilesetName);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), insertCount);
        }
        
        [Test]
        public void GetTest()
        {
            InsertSomeData(10, testTilesetName);
            var data = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            Assert.IsNotNull(data);
            Assert.AreEqual(testTileId, data.TileId);
            Assert.IsNotEmpty(data.ETag.ToString());
            Assert.IsNotNull(data.ExpirationDate);
        }
        
        [Test]
        public void GetMissingTest()
        {
            var wrongName = testTilesetName + "ERROR";
            Assert.AreEqual(_sqliteCache.TileCount(wrongName), 0);
            var data = _sqliteCache.Get<VectorData>(wrongName, testTileId);
            Assert.IsNull(data);
        }
        
        [Test]
        public void ReadEtagAndExpirationTest()
        {
            InsertSomeData(10, testTilesetName);
            var data = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            Assert.IsNotEmpty(data.ETag);
            Assert.IsNotNull(data.ExpirationDate);
        }
        
        [Test]
        public void UpdateExpirationTest()
        {
            InsertSomeData(10, testTilesetName);
            var newExpiration = DateTime.Now.AddHours(1);
            _sqliteCache.UpdateExpiration(testTilesetName, testTileId, newExpiration);
            var getData = _sqliteCache.Get<VectorData>(testTilesetName, testTileId);
            Assert.LessOrEqual(newExpiration.Subtract(getData.ExpirationDate.Value).Seconds, 5);
        }
        
        [Test]
        public void UpdateExpirationMissingTileTest()
        {
            var wrongName = testTilesetName + "ERROR";
            Assert.AreEqual(_sqliteCache.TileCount(wrongName), 0);
            var newExpiration = DateTime.Now.AddHours(1);
            try
            {
                _sqliteCache.UpdateExpiration(wrongName, testTileId, newExpiration);
            }
            catch (Exception e)
            {
                Assert.IsNotNull(e);
            }
        }
        
        [Test]
        public void RemoveData()
        {
            InsertSomeData(5, testTilesetName);
            var beforeCount = _sqliteCache.TileCount(testTilesetName);
            Assert.True(beforeCount != 0);
            var effectedTileCount = _sqliteCache.RemoveData(testTilesetName, testTileId.Z, testTileId.X, testTileId.Y);
            Assert.AreEqual(1, effectedTileCount);
            Assert.True(_sqliteCache.TileCount(testTilesetName) == beforeCount - 1);
        }
        
        [Test]
        public void RemoveMissingData()
        {
            var wrongName = testTilesetName + "ERROR_RemoveMissingData";
            Assert.AreEqual(_sqliteCache.TileCount(wrongName), 0);
            var effectedTileCount = _sqliteCache.RemoveData(wrongName, testTileId.Z, testTileId.X, testTileId.Y);
            Assert.AreEqual(0, effectedTileCount);
        }
        
        [Test]
        public void AutoPrune()
        {
            var insertCount = _maxCacheSize + (SqliteCache.PruneCacheDelta * 2);
            InsertSomeData((int)insertCount, testTilesetName);

            var allTileCount = _sqliteCache.GetAllTiles().Count;
            Assert.AreEqual(allTileCount, _maxCacheSize, SqliteCache.PruneCacheDelta);
        }

        [Test]
        public void ClearTilesetTest()
        {
            var insertCount = 20;
            InsertSomeData(insertCount, testTilesetName);
            Assert.AreEqual(insertCount, _sqliteCache.TileCount(testTilesetName));
            Assert.GreaterOrEqual(_sqliteCache.GetAllTiles().Count, insertCount);
            var effectedRows = _sqliteCache.Clear(testTilesetName);
            Assert.Greater(effectedRows, 0);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
        }
        
        [Test]
        public void ClearTilesetWrongNameTest()
        {
            var insertCount = 20;
            InsertSomeData(insertCount, testTilesetName);
            var wrongTilesetName = testTilesetName + "ERROR_ClearMissingTest";
            
            var effectedRows = _sqliteCache.Clear(wrongTilesetName);
            
            Assert.AreEqual(0, effectedRows);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), insertCount);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), insertCount);
            Assert.AreEqual(_sqliteCache.TileCount(wrongTilesetName), 0);
            _sqliteCache.Clear(testTilesetName);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
        }
        
        [Test]
        public void ClearTilesetGroupTest()
        {
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
            Assert.AreEqual(_sqliteCache.GetAllTiles().Count, 0);
            
            var insertCount = 20;
            InsertSomeData(insertCount, testTilesetName);

            var allTileCount = _sqliteCache.GetAllTiles().Count;
            Assert.AreEqual(insertCount, allTileCount);
            Assert.AreEqual(insertCount, allTileCount);
           
            var effectedRows = _sqliteCache.Clear(testTilesetName);
            Assert.Greater(effectedRows, 0);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
        }

        private void InsertSomeData(int insertCount, string tilesetName)
        {
            for (int i = 0; i < insertCount; i++)
            {
                _sqliteCache.SyncAdd(tilesetName, new CanonicalTileId(testTileId.Z,testTileId.X,testTileId.Y + i), Array.Empty<byte>(), "", _beforeEtag, DateTime.Now, false);
            }
        }

        [Test]
        public void ClearDatabaseTest()
        {
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
            Assert.AreEqual(_sqliteCache.GetAllTiles().Count, 0);
            
            InsertSomeData(20, testTilesetName);
            InsertSomeData(20, testTilesetName + "Second");
            InsertSomeData(20, testTilesetName + "Third");
            
            var allTileCount = _sqliteCache.GetAllTiles().Count;
            Assert.AreEqual(allTileCount, 60);
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
            allTileCount = _sqliteCache.GetAllTiles().Count;
            Assert.AreEqual(0, allTileCount);
            Assert.AreEqual(_sqliteCache.TileCount(testTilesetName), 0);
        }

        [OneTimeTearDown]
        public void DeleteSqliteFileTest()
        {
            _sqliteCache.DeleteSqliteFile();
        }
    }
}