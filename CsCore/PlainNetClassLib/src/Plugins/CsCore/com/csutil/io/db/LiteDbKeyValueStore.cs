using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace com.csutil.keyvaluestore {

    public class LiteDbKeyValueStore : IKeyValueStore {

        private class PrimitiveWrapper { public object obj; }

        private BsonMapper bsonMapper;
        private LiteDatabase db;
        private LiteCollection<BsonDocument> collection;

        public IKeyValueStore fallbackStore { get; set; }

        private static bool IsPrimitiveType(System.Type t) { return t.IsPrimitive || t == typeof(string); }

        public LiteDbKeyValueStore(FileInfo dbFile) { Init(dbFile); }

        private void Init(System.IO.FileInfo dbFile, string collectionName = "Default") {
            bsonMapper = new BsonMapper();
            bsonMapper.IncludeFields = true;
            db = new LiteDatabase(dbFile.FullPath(), bsonMapper);
            collection = db.GetCollection(collectionName);
        }

        private BsonDocument GetBson(string key) { return collection.FindById(key); }

        public async Task<T> Get<T>(string key, T defaultValue) {
            var bson = GetBson(key);
            if (bson != null) { return (T)InternalGet(bson, typeof(T)); }
            if (fallbackStore != null) {
                var fallbackValue = await fallbackStore.Get<T>(key, defaultValue);
                if (!Equals(fallbackValue, defaultValue)) { InternalSet(key, fallbackValue); }
                return fallbackValue;
            }
            return defaultValue;
        }

        private object InternalGet(BsonDocument bson, Type targetType) {
            if (IsPrimitiveType(targetType)) { // unwrap the primitive:
                return bsonMapper.ToObject<PrimitiveWrapper>(bson).obj;
            }
            return bsonMapper.ToObject(targetType, bson);
        }

        public async Task<object> Set(string key, object obj) {
            var oldEntry = InternalSet(key, obj);
            if (fallbackStore != null) {
                var fallbackOldEntry = await fallbackStore.Set(key, obj);
                if (oldEntry == null && fallbackOldEntry != null) { oldEntry = fallbackOldEntry; }
            }
            return oldEntry;
        }

        private object InternalSet(string key, object obj) {
            var objType = obj.GetType();
            if (IsPrimitiveType(objType)) { obj = new PrimitiveWrapper() { obj = obj }; }
            var oldBson = GetBson(key);
            var newVal = bsonMapper.ToDocument(obj);
            if (oldBson == null) {
                collection.Insert(key, newVal);
                return null;
            } else {
                var oldVal = InternalGet(oldBson, objType);
                collection.Update(key, newVal);
                return oldVal;
            }
        }

        public async Task<bool> Remove(string key) {
            var res = collection.Delete(key);
            if (fallbackStore != null) { res &= await fallbackStore.Remove(key); }
            return res;
        }

        public async Task RemoveAll() {
            db.DropCollection(collection.Name);
            if (fallbackStore != null) { await fallbackStore.RemoveAll(); }
        }

        public async Task<bool> ContainsKey(string key) {
            if (null != GetBson(key)) { return true; }
            if (fallbackStore != null) { return await fallbackStore.ContainsKey(key); }
            return false;
        }

        public async Task<IEnumerable<string>> GetAllKeys() {
            IEnumerable<string> result = collection.FindAll().Map(x => GetKeyFromBsonDoc(x));
            if (fallbackStore != null) {
                var fallbackStoreKeys = await fallbackStore.GetAllKeys();
                if (fallbackStore != null) {
                    var filteredFallbackKeys = fallbackStoreKeys.Filter(e => !result.Contains(e));
                    result = result.Concat(filteredFallbackKeys);
                }
            }
            return result;
        }

        private static string GetKeyFromBsonDoc(BsonDocument x) {
            var key = x.Keys.First();
            AssertV2.AreEqual("_id", key);
            return key;
        }

    }

}