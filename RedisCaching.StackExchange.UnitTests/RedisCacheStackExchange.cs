﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using PubComp.Caching.RedisCaching.StackExchange;
using PubComp.Testing.TestingUtils;
using PubComp.Caching.SystemRuntime;
using System.Configuration;
using System.Threading.Tasks;
using PubComp.Caching.RedisCaching.StackExchange.UnitTests.Mocks;

namespace PubComp.Caching.RedisCaching.StackExchange.UnitTests
{
    [TestClass]
    public class RedisCacheStackExchangeTests
    {
        //[TestMethod]
        //public void TestReadAppConfig()
        //{
        //    var config = ConfigurationManager.GetSection("PubComp/CacheNotificationsConfig") as IList<CacheNotificationsConfig>;
        //}

        [TestMethod]
        public void TestLoadRedisCacheFromConfigFile()
        {
            ICache redisCache = CacheManager.GetCache("redisCache");
            ICache localCache = CacheManager.GetCache("localCache");
            ICache layeredCache = CacheManager.GetCache("layeredCache");
            
            ICache testedCache = layeredCache;

            testedCache.ClearAll();
            redisCache.ClearAll();
            localCache.ClearAll();

            Thread.Sleep(5000);

            int keycount = 0;
            while (keycount < 10)
            {
                try
                {
                    string key = "key_" + keycount;
                    string val = "value_" + keycount;
                    testedCache.Set(key, val);

                    keycount++;
                    Thread.Sleep(100);

                    string valFromCache;

                    testedCache.TryGet(key, out valFromCache);
                    Assert.AreEqual(val, valFromCache);
                    System.Diagnostics.Debug.WriteLine("SetGet-{0} {1} {2}", testedCache.Name, key, valFromCache);

                    redisCache.TryGet(key, out valFromCache);
                    Assert.AreEqual(val, valFromCache);
                    System.Diagnostics.Debug.WriteLine("SetGet-{0} {1} {2}", redisCache.Name, key, valFromCache);

                    localCache.TryGet(key, out valFromCache);
                    Assert.AreEqual(val, valFromCache);
                    System.Diagnostics.Debug.WriteLine("SetGet-{0} {1} {2}", localCache.Name, key, valFromCache);

                }
                catch (Exception exp)
                {
                    System.Diagnostics.Debug.WriteLine("Error: {0}" + exp.Message);
                }
            }

        }

        [TestMethod]
        public void TestRedisCacheAllLoop()
        {
            var redisCache = new RedisCache(
                "C1",
                new RedisCachePolicy
                {
                });
            var localCache = new InMemoryCache("C1", new TimeSpan(0, 2, 0));

            redisCache.ClearAll();
            //localCache.ClearAll();
            Thread.Sleep(5000);
            int keycount = 0;
            while (keycount < 10)
            {
                try
                {
                    string key = "key_" + keycount;
                    string val = "value_" + keycount;
                    redisCache.Set(key, val);
                    localCache.Set(key, val);

                    keycount++;
                    Thread.Sleep(100);

                    string valFromCache;

                    redisCache.TryGet(key, out valFromCache);
                    Assert.AreEqual(val, valFromCache);
                    System.Diagnostics.Debug.WriteLine("SetGet-{0} {1} {2}", redisCache.Name, key, valFromCache);

                    localCache.TryGet(key, out valFromCache);
                    Assert.AreEqual(val, valFromCache);
                    System.Diagnostics.Debug.WriteLine("SetGet-{0} {1} {2}", localCache.Name, key, valFromCache);
                }
                catch (Exception exp)
                {
                    System.Diagnostics.Debug.WriteLine("Error: {0}" + exp.Message);
                }
            }

            localCache.Clear("key_6");

            Thread.Sleep(5000);


        }

        [TestMethod]
        public void TestRedisCacheBasic()
        {
            //var cache1 = new RedisCache(
            //    "cache1",
            //    new RedisCachePolicy
            //    {
            //    });

            var cache1 = CacheManager.GetCache("redisCache");

            cache1.ClearAll();
            
            int misses1 = 0;
            Func<string> getter1 = () => { misses1++; return misses1.ToString(); };

            int misses2 = 0;
            Func<string> getter2 = () => { misses2++; return misses2.ToString(); };

            string result;

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(1, misses1);
            Assert.AreEqual("1", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(2, misses1);
            Assert.AreEqual("2", result);
            
            cache1.ClearAll();

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(3, misses1);
            Assert.AreEqual("3", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(4, misses1);
            Assert.AreEqual("4", result);
        }


        [TestMethod]
        public void TestRedisCacheTwoCaches()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = @"172.20.0.219:6379"
                });
            cache1.ClearAll();


            var cache2 = new RedisCache(
                "cache2",
                new RedisCachePolicy
                {
                });
            cache2.ClearAll();

            int misses1 = 0;
            Func<string> getter1 = () => { misses1++; return misses1.ToString(); };

            int misses2 = 0;
            Func<string> getter2 = () => { misses2++; return misses2.ToString(); };

            string result;

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(1, misses1);
            Assert.AreEqual("1", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(2, misses1);
            Assert.AreEqual("2", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(1, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);

            cache1.ClearAll();

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(3, misses1);
            Assert.AreEqual("3", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(4, misses1);
            Assert.AreEqual("4", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public void TestRedisCacheStruct()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            int misses = 0;

            Func<int> getter = () => { misses++; return misses; };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestRedisCacheObject()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheNull()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return null; };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(null, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void TestRedisCacheObjectMutated()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);

            value.Add("2");

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_FromInsert()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new RedisCache(
                "insert-expire-cache",
                new RedisCachePolicy
                {
                    ExpirationFromAdd = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_Sliding()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new RedisCache(
                "sliding-expire-cache",
                new RedisCachePolicy
                {
                    SlidingExpiration = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1 + 60);

            // Should expire within TTL+60sec from last access
            CacheTestTools.AssertValueDoesChangeAfter(cache, "key", "1", getter, stopwatch, ttl + 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_Constant()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var expireAt = DateTime.Now.AddSeconds(ttl);
            stopwatch.Start();

            var cache = new RedisCache(
                "constant-expire",
                new RedisCachePolicy
                {
                    AbsoluteExpiration = expireAt,
                });
            cache.ClearAll();

            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheGetTwice()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheSetTwice()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;
            bool wasFound;

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("1", result);

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public void TestRedisCacheMassSetDataLoadTest()
        {
            var redisCache = CacheManager.GetCache("redisCache");
            redisCache.ClearAll();
                
            int keycount = 0;
            List<MockCacheItem> list = new List<MockCacheItem>();
            while (keycount++ < 100000)
            {
                list.Add(MockCacheItem.GetNewMockInstance(keycount.ToString()));
            }

            var stopWatch = Stopwatch.StartNew();
            Parallel.ForEach(list, (item =>
            {
                redisCache.Set(item.Key, item);
            }));

            TimeSpan elapsed = stopWatch.Elapsed;
            System.Diagnostics.Debug.WriteLine("TestRedisCacheLoadTest::Finished, Elapsed " + elapsed.Seconds.ToString());

            bool isFast = elapsed.Seconds < 20;//took less then 20 seconds
            Assert.IsTrue(isFast, "Redis SET load test was too slow, took: " + elapsed.Seconds);
        }

        [TestMethod]
        public void TestRedisCacheMassGetDataLoadTest()
        {
            var redisCache = CacheManager.GetCache("redisCache");

            int keycount = 0;
            List<string> list = new List<string>();
            while (keycount++ < 100000)
            {
                list.Add(MockCacheItem.GetKey(keycount.ToString()));
            }

            var stopWatch = Stopwatch.StartNew();
            Parallel.ForEach(list, (key =>
            {
                MockCacheItem item;
                redisCache.TryGet(key, out item);
                Assert.IsNotNull(item);
            }));

            TimeSpan elapsed = stopWatch.Elapsed;
            System.Diagnostics.Debug.WriteLine("TestRedisCacheLoadTest::Finished, Elapsed " + elapsed.Seconds.ToString());

            bool isFast = elapsed.Seconds < 20;//took less then 20 seconds
            Assert.IsTrue(isFast, "Redis GET load test was too slow, took: " + elapsed.Seconds);
        }
    }
}
