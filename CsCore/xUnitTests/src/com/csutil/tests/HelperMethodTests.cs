using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using com.csutil.datastructures;
using com.csutil.encryption;
using Xunit;

namespace com.csutil.tests {

    public class HelperMethodTests {

        [Fact]
        public void DelegateExtensionTests() {
            {
                Action a = null;
                Assert.False(a.InvokeIfNotNull());
                a = () => { };
                Assert.True(a.InvokeIfNotNull());
            }
            {
                Action<string> a = null;
                Assert.False(a.InvokeIfNotNull(""));
                a = (s) => { };
                Assert.True(a.InvokeIfNotNull(""));
            }
            {
                Action<string, int> a = null;
                Assert.False(a.InvokeIfNotNull("", 123));
                a = (s, i) => { };
                Assert.True(a.InvokeIfNotNull("", 123));
            }
            {
                Action<string, int, bool> a = null;
                Assert.False(a.InvokeIfNotNull("", 123, true));
                a = (s, i, b) => { };
                Assert.True(a.InvokeIfNotNull("", 123, true));
            }
        }

        [Fact]
        public void DictionaryExtensionTests() {
            var dic = new Dictionary<string, string>();
            Assert.Null(dic.AddOrReplace("s1", "a"));
            Assert.Equal("a", dic.AddOrReplace("s1", "b"));
            Assert.Equal("b", dic.AddOrReplace("s1", "a"));
        }

        [Fact]
        public void IEnumerableExtensionTests() {
            IEnumerable<string> list = null;
            Assert.True(list.IsNullOrEmpty());
            Assert.Equal("null", list.ToStringV2((s) => s));

            list = new List<string>();
            Assert.True(list.IsNullOrEmpty());
            Assert.Equal("()", list.ToStringV2((s) => s, bracket1: "(", bracket2: ")"));

            (list as List<string>).Add("s1");
            Assert.False(list.IsNullOrEmpty());
            Assert.Equal("{s1}", list.ToStringV2((s) => s, bracket1: "{", bracket2: "}"));

            (list as List<string>).Add("s2");
            Assert.False(list.IsNullOrEmpty());
            Assert.Equal("[s1, s2]", list.ToStringV2((s) => s, bracket1: "[", bracket2: "]"));
        }

        [Fact]
        public void ChangeTrackerTests() {
            var t = new ChangeTracker<string>("a");
            Assert.True(t.setNewValue("b"));
            Assert.False(t.setNewValue("b"));
            Assert.Equal("b", t.value);
        }

        [Fact]
        public void FixedSizedQueueTests() {
            var q = new FixedSizedQueue<string>(3);
            q.Enqueue("a").Enqueue("b").Enqueue("c");
            Assert.Equal(3, q.Count);
            q.Enqueue("d");
            Assert.Equal(3, q.Count); // "a" fell out of the queue
            Assert.Equal("b", q.Dequeue());
        }

        [Fact]
        public void DateTimeTests() {
            AssertV2.ThrowExeptionIfAssertionFails(false, () => {
                var d1 = DateTimeParser.NewDateTimeFromUnixTimestamp(-2);
                var d2 = DateTimeParser.NewDateTimeFromUnixTimestamp(2);
                Assert.True(d1.IsBefore(d2));
                Assert.False(d2.IsBefore(d1));
                Assert.True(d2.IsAfter(d1));
                Assert.False(d1.IsAfter(d2));
            });
        }

        [Fact]
        public void StringExtensionMethodTests() {
            string s = "abc";
            Assert.Equal("bc", s.Substring(1, "d", includeEnd: true));
            Assert.Equal("bc", s.Substring(1, "c", includeEnd: true));
            Assert.Equal("ab", s.Substring("c", includeEnd: false));

            s = "[{a}]-[{b}]";
            Assert.Equal("a}]-[{b}]", s.SubstringAfter("{"));
            Assert.Equal("{b}]", s.SubstringAfter("[", startFromBack: true));
            Assert.Throws<Exception>(() => { s.SubstringAfter("("); });

            s = "[(abc)]";
            Assert.Equal("abc", s.SubstringAfter("(").Substring(")", includeEnd: false));
        }

        [Fact]
        public void StringEncryptionTests() {
            var s = "some text..";
            var encrypted = s.Encrypt("123");
            Assert.NotEqual(s, encrypted);
            Assert.NotEqual(encrypted, s.Encrypt("124"));

            Assert.Equal(s, encrypted.Decrypt("123"));
            Assert.Throws<CryptographicException>(() => {
                Assert.NotEqual(s, encrypted.Decrypt("124"));
            });
        }

        [Fact]
        public void TaskExtensionTests() {
            Task t = null;
            Assert.Throws<AggregateException>(() => {
                t = Task.Run(() => { throw Log.e("Some error"); });
                t.Wait();
            });
            Assert.NotNull(t);
            Assert.NotNull(t.Exception);
            Assert.Throws<AggregateException>(() => {
                t.ThrowIfException();
            });
        }

        [Fact]
        public void TypeExtensionsTests() {
            var MySubClass1 = typeof(MySubClass1);
            Assert.True(MySubClass1.IsSubclassOf<MyClass1>());
            Assert.False(MySubClass1.IsSubclassOf<MySubClass2>());
            Assert.True(typeof(MySubClass2).IsSubclassOf<MyClass1>());
            Assert.False(typeof(MyClass1).IsSubclassOf<MySubClass1>());

            Assert.True(TypeCheck.AreEqual<MyClass1, MyClass1>());
            Assert.False(TypeCheck.AreEqual<MySubClass1, MyClass1>());

            Assert.True(typeof(MySubClass2).IsCastableTo<MyClass1>());
            Assert.False(typeof(MyClass1).IsCastableTo<MySubClass2>());

            Assert.True(typeof(MySubClass1).IsCastableTo(typeof(MyClass1)));
            Assert.True(typeof(MyClass1).IsCastableTo(typeof(MyClass1)));
            Assert.False(typeof(MyClass1).IsCastableTo(typeof(MySubClass1)));
        }

        private class MyClass1 { }
        private class MySubClass1 : MyClass1 { }
        private class MySubClass2 : MyClass1 { }

    }
}