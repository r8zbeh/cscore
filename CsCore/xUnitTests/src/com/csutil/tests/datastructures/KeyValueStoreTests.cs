using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.csutil.keyvaluestore;
using Xunit;

namespace com.csutil.tests.keyvaluestore {

    public class KeyValueStoreTests {

        public KeyValueStoreTests(Xunit.Abstractions.ITestOutputHelper logger) {
            logger.UseAsLoggingOutput();
            AssertV2.throwExeptionIfAssertionFails = true;
        }

        private class MyClass1 {
            public string myString1 { get; set; }
            public string myString2;
        }

        [Fact]
        public void ExampleUsage1() {
            IKeyValueStore store = new InMemoryKeyValueStore();
            string myKey1 = "myKey1";
            MyClass1 x1 = new MyClass1() { myString1 = "Abc", myString2 = "Abc2" };
            store.Set(myKey1, x1);
            MyClass1 x2 = store.Get<MyClass1>(myKey1, defaultValue: null).Result;
            Assert.Equal(x1.myString1, x2.myString1);
            Assert.Equal(x1.myString2, x2.myString2);
        }

        [Fact]
        public async void ExampleUsage2() {
            var storeFile = EnvironmentV2.instance.GetOrAddTempFolder("KeyValueStoreTests").GetChild("ExampleUsage2");
            storeFile.DeleteV2(); // Cleanup before tests if the test file exists
            string myKey1 = "test123";
            MyClass1 x1 = new MyClass1() { myString1 = "Abc", myString2 = "Abc2" };
            {   // Create a fast memory store and combine it with a LiteDB store that is persisted to disk:
                IKeyValueStore store = new InMemoryKeyValueStore().WithFallbackStore(new LiteDbKeyValueStore(storeFile));
                await store.Set(myKey1, x1);
                MyClass1 x2 = await store.Get<MyClass1>(myKey1, null);
                Assert.Equal(x1.myString1, x2.myString1);
                Assert.Equal(x1.myString2, x2.myString2);
            }
            { // Create a second store and check that the changes were persisted:
                IKeyValueStore store2 = new LiteDbKeyValueStore(storeFile);
                Assert.True(await store2.ContainsKey(myKey1));
                MyClass1 x2 = await store2.Get<MyClass1>(myKey1, null);
                Assert.Equal(x1.myString1, x2.myString1);
                Assert.Equal(x1.myString2, x2.myString2);
                await store2.Remove(myKey1);
                Assert.False(await store2.ContainsKey(myKey1));
            }
        }

        [Fact]
        public async void TestAllIKeyValueStoreImplementations() {
            var dbFile = EnvironmentV2.instance.GetOrAddTempFolder("KeyValueStoreTests").GetChild("TestAllIKeyValueStoreImplementations");
            dbFile.DeleteV2();
            await TestIKeyValueStoreImplementation(new InMemoryKeyValueStore());
            await TestIKeyValueStoreImplementation(new LiteDbKeyValueStore(dbFile));
            await TestIKeyValueStoreImplementation(new ExceptionWrapperKeyValueStore(new InMemoryKeyValueStore()));
            await TestIKeyValueStoreImplementation(new MockDekayKeyValueStore().WithFallbackStore(new InMemoryKeyValueStore()));
        }

        /// <summary> Runs typical requests on the passed store </summary>
        private static async Task TestIKeyValueStoreImplementation(IKeyValueStore store) {
            string myKey1 = "myKey1";
            var myValue1 = "myValue1";
            string myKey2 = "myKey2";
            var myValue2 = "myValue2";
            var myFallbackValue1 = "myFallbackValue1";

            // test Set and Get of values:
            Assert.False(await store.ContainsKey(myKey1));
            Assert.Equal(myFallbackValue1, await store.Get(myKey1, myFallbackValue1));
            await store.Set(myKey1, myValue1);
            Assert.Equal(myValue1, await store.Get<string>(myKey1, null));
            Assert.True(await store.ContainsKey(myKey1));

            // Test replacing values:
            var oldVal = await store.Set(myKey1, myValue2);
            Assert.Equal(myValue1, oldVal);
            Assert.Equal(myValue2, await store.Get<string>(myKey1, null));

            // Test add and remove of a second key:
            Assert.False(await store.ContainsKey(myKey2));
            await store.Set(myKey2, myValue2);
            Assert.True(await store.ContainsKey(myKey2));

            var keys = await store.GetAllKeys();
            Assert.Equal(2, keys.Count());

            await store.Remove(myKey2);
            Assert.False(await store.ContainsKey(myKey2));

            // Test RemoveAll:
            Assert.True(await store.ContainsKey(myKey1));
            await store.RemoveAll();
            Assert.False(await store.ContainsKey(myKey1));
        }

        [Fact]
        public async void TestExceptionCatching() {
            var myKey1 = "key1";
            int myValue1 = 1;
            string myDefaultString = "myDefaultValue";

            var innerStore = new InMemoryKeyValueStore();
            var exHandlerStore = new ExceptionWrapperKeyValueStore(innerStore, new HashSet<Type>());

            await innerStore.Set(myKey1, myValue1);
            // Cause an InvalidCastException:
            await Assert.ThrowsAsync<InvalidCastException>(() => innerStore.Get<string>(myKey1, myDefaultString));
            // Cause an InvalidCastException which is then catched and instead the default is returned:
            string x = await exHandlerStore.Get<string>(myKey1, myDefaultString);
            Assert.Equal(myDefaultString, x);

            // Add the InvalidCastException to the list of errors that should not be ignored:
            exHandlerStore.errorTypeBlackList.Add(typeof(InvalidCastException));
            // Now the same Get request passes the InvalidCastException on:
            await Assert.ThrowsAsync<InvalidCastException>(() => exHandlerStore.Get<string>(myKey1, myDefaultString));
        }

        [Fact]
        public async void TestStoreWithDelay() {
            // Simulates the DB on the server:
            var innerStore = new InMemoryKeyValueStore();
            // Simulates the connection to the server:
            var simulatedDelayStore = new MockDekayKeyValueStore().WithFallbackStore(innerStore);
            // Handles connection problems to the server:
            var exWrapperStore = new ExceptionWrapperKeyValueStore(simulatedDelayStore);
            // Represents the local cache in case the server cant be reached:
            var outerStore = new InMemoryKeyValueStore().WithFallbackStore(exWrapperStore);

            var key1 = "key1";
            var value1 = "value1";
            var key2 = "key2";
            var value2 = "value2";
            {
                var delayedSetTask = outerStore.Set(key1, value1);
                Assert.Equal(value1, await outerStore.Get(key1, "")); // The outer store already has the update
                Assert.NotEqual(value1, await innerStore.Get(key1, "")); // The inner store did not get the update yet
                // After waiting for set to fully finish the inner store has the update too:
                await delayedSetTask;
                Assert.Equal(value1, await innerStore.Get(key1, ""));
            }
            simulatedDelayStore.throwTimeoutError = true;
            var simulatedErrorCatched = false;
            exWrapperStore.onError = (Exception e) => { simulatedErrorCatched = true; };
            {
                await outerStore.Set(key2, value2); // This will cause a timeout error in the "delayed" store
                Assert.True(simulatedErrorCatched);
                Assert.Contains(key2, await outerStore.GetAllKeys()); // In the outer store the value was set
                Assert.False(await innerStore.ContainsKey(key2)); // The inner store never got the update
                Assert.Null(await exWrapperStore.GetAllKeys()); // Will throw another error and return null
            }
        }

    }

}