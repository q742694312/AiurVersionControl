﻿using AiurEventSyncer.Models;
using AiurEventSyncer.Remotes;
using AiurEventSyncer.Tests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace AiurEventSyncer.Tests
{
    [TestClass]
    public class AutoTest
    {
        private Repository<int> _localRepo;

        [TestInitialize]
        public void GetBasicRepo()
        {
            _localRepo = new Repository<int>();
            _localRepo.Commit(1);
            _localRepo.Commit(2);
            _localRepo.Commit(3);
            _localRepo.Assert(1, 2, 3);
        }

        [TestMethod]
        public async Task TestAutoPush()
        {
            var remoteRepo = new Repository<int>();
            await new ObjectRemote<int>(remoteRepo, autoPush: true).AttachAsync(_localRepo);

             _localRepo.Commit(50);
            remoteRepo.Assert(1, 2, 3, 50);

            _localRepo.Commit(200);
            _localRepo.Commit(300);

            _localRepo.Assert(1, 2, 3, 50, 200, 300);
            remoteRepo.Assert(1, 2, 3, 50, 200, 300);
        }

        [TestMethod]
        public async Task TestAutoPull()
        {
            var remoteRepo = _localRepo;
            var localRepo = new Repository<int>();
            await new ObjectRemote<int>(remoteRepo, autoPush: false, autoPull: true).AttachAsync(localRepo);
            localRepo.Assert(1, 2, 3);

            remoteRepo.Commit(50);
            localRepo.Assert(1, 2, 3, 50);

            remoteRepo.Commit(200);
            remoteRepo.Commit(300);

            localRepo.Assert(1, 2, 3, 50, 200, 300);
            remoteRepo.Assert(1, 2, 3, 50, 200, 300);
        }

        [TestMethod]
        public async Task DoubleWaySync()
        {
            var a = new Repository<int>() { Name = "Repo A" };
            var b = new Repository<int>() { Name = "Repo B" };
            await new ObjectRemote<int>(b, autoPush: true, autoPull: true) { Name = "A auto sync B." }.AttachAsync(a);

            a.Commit(5);
            a.Assert(5);
            b.Assert(5);

            b.Commit(10);
            a.Assert(5, 10);
            b.Assert(5, 10);

            a.Commit(100);
            b.Commit(200); // This is faster. Because while B is saving 200, A hasn't push 100.
            a.Assert(5, 10, 200, 100);
            b.Assert(5, 10, 200, 100);
        }

        [TestMethod]
        public async Task ComplicatedAutoTest()
        {
            //     B   D
            //    / \ / \
            //   A   C   E

            var a = new Repository<int>();
            var b = BookDbRepoFactory.BuildIntRepo();
            var c = new Repository<int>();
            var d = new Repository<int>();
            var e = new Repository<int>();

            await new ObjectRemote<int>(b, true) { Name = "a autopush b" }.AttachAsync(a);
            await new ObjectRemote<int>(b, false, true) { Name = "c autopull b" }.AttachAsync(c);
            await new ObjectRemote<int>(d, true) { Name = "c autopush d" }.AttachAsync(c);
            await new ObjectRemote<int>(d, false, true) { Name = "e autopull d" }.AttachAsync(e);

            a.Commit(5);

            b.Assert(5);
            c.Assert(5);
            d.Assert(5);
            e.Assert(5);
        }

        [TestMethod]
        public async Task DistributeTest()
        {
            //     server
            //    /       \
            //   sender    subscriber1,2,3

            var senderserver = new Repository<int>();
            var server = BookDbRepoFactory.BuildIntRepo();
            var subscriber1 = new Repository<int>() { Name = "subscriber1" };
            var subscriber2 = new Repository<int>() { Name = "subscriber2" };
            var subscriber3 = new Repository<int>() { Name = "subscriber3" };

            await new ObjectRemote<int>(server, true) { Name = "Sender Remote" }.AttachAsync(senderserver);
            await new ObjectRemote<int>(server, false, true) { Name = "Subscriber Remote 1" }.AttachAsync(subscriber1);
            await new ObjectRemote<int>(server, false, true) { Name = "Subscriber Remote 2" }.AttachAsync(subscriber2);
            senderserver.Commit(5);
            await new ObjectRemote<int>(server, false, true) { Name = "Subscriber Remote 3" }.AttachAsync(subscriber3);

            senderserver.Assert(5);
            server.Assert(5);
            subscriber1.Assert(5);
            subscriber2.Assert(5);
            subscriber3.Assert(5);
        }

        [TestMethod]
        public async Task DropTest()
        {
            var sender = new Repository<int>() { Name = "Sender" };
            var server = BookDbRepoFactory.BuildIntRepo();
            var subscriber = new Repository<int>() { Name = "Subscriber" };
            await new ObjectRemote<int>(server, true, false) { Name = "Sender to Server - Auto Push" }.AttachAsync(sender);

            var origin = await new ObjectRemote<int>(server, false, true) { Name = "Subscriber to Server - Auto Pull" }
                .AttachAsync(subscriber);

            sender.Commit(5);
            sender.Commit(10);
            server.Assert(5, 10);
            subscriber.Assert(5, 10);

            await origin.DropAsync();
            sender.Commit(100);
            sender.Commit(200);
            server.Assert(5, 10, 100, 200);
            subscriber.Assert(5, 10);
        }
    }
}
