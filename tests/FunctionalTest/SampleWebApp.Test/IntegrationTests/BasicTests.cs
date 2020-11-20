﻿using AiurEventSyncer.Models;
using AiurEventSyncer.Remotes;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleWebApp.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWebApp.Tests.IntegrationTests
{
    [TestClass]
    public class BasicTests
    {
        private IHost _server;

        [TestInitialize]
        public async Task CreateServer()
        {
            _server = Program.BuildHost(null);
            await _server.StartAsync();
        }

        [TestCleanup]
        public async Task CleanServer()
        {
            await _server.StopAsync();
            _server.Dispose();
        }

        [TestMethod]
        public async Task RealCommunication()
        {
            var repo = new Repository<LogItem>();
            await repo.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPush: true));

            await repo.CommitAsync(new LogItem { Message = "1" });
            await repo.CommitAsync(new LogItem { Message = "2" });
            await repo.CommitAsync(new LogItem { Message = "3" });

            var repo2 = new Repository<LogItem>();
            await repo2.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPull: true));
            await repo2.PullAsync();
            await repo2.PullAsync();

            Assert.AreEqual(repo2.Commits.Count(), 3);
            Assert.AreEqual(repo2.Commits.ToArray()[0].Item.Message, "1");
            Assert.AreEqual(repo2.Commits.ToArray()[1].Item.Message, "2");
            Assert.AreEqual(repo2.Commits.ToArray()[2].Item.Message, "3");
        }

        [TestMethod]
        public async Task RealAutoPull()
        {
            var repo = new Repository<LogItem>();
            await repo.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPush: true));

            var repo2 = new Repository<LogItem>();
            await repo2.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPull: true));

            await Task.Delay(200); // Wait for connected.

            await repo.CommitAsync(new LogItem { Message = "1" });
            await repo.CommitAsync(new LogItem { Message = "2" });
            await repo.CommitAsync(new LogItem { Message = "3" });

            await Task.Delay(500); // Wait for pulled.

            Assert.AreEqual(repo2.Commits.Count(), 3);
            Assert.AreEqual(repo2.Commits.ToArray()[0].Item.Message, "1");
            Assert.AreEqual(repo2.Commits.ToArray()[1].Item.Message, "2");
            Assert.AreEqual(repo2.Commits.ToArray()[2].Item.Message, "3");
        }

        [TestMethod]
        public async Task DoubleWaySync()
        {
            var repoA = new Repository<LogItem>();
            await repoA.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPush: true, autoPull: true));

            await Task.WhenAll(
                repoA.CommitAsync(new LogItem { Message = "1" }),
                repoA.CommitAsync(new LogItem { Message = "2" }),
                repoA.CommitAsync(new LogItem { Message = "3" }),
                repoA.CommitAsync(new LogItem { Message = "4" }),
                repoA.CommitAsync(new LogItem { Message = "5" }),
                repoA.CommitAsync(new LogItem { Message = "6" })
            );

            var repoB = new Repository<LogItem>();
            await repoB.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares"));
            await repoB.PullAsync();
            Assert.AreEqual(repoB.Commits.Count(), 6);
        }

        [TestMethod]
        public async Task DoubleWayDataBinding()
        {
            var repoA = new Repository<LogItem>();
            await repoA.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPush: true, autoPull: true));

            var repoB = new Repository<LogItem>();
            await repoB.AddRemoteAsync(new WebSocketRemote<LogItem>("http://localhost:15000/repo.ares", autoPush: true, autoPull: true));

            await Task.Delay(200); // Wait for connected.

            await repoA.CommitAsync(new LogItem { Message = "1" });
            await repoA.CommitAsync(new LogItem { Message = "2" });

            //await Task.Delay(500); // Wait for connected.

            //Assert.AreEqual(repoA.Commits.Count(), 2);
            //Assert.AreEqual(repoB.Commits.Count(), 2);

            await repoB.CommitAsync(new LogItem { Message = "3" });
            await repoB.CommitAsync(new LogItem { Message = "4" });

            await Task.Delay(5700); // Wait for connected.


            Assert.AreEqual(repoA.Commits.Count(), 4);
            Assert.AreEqual(repoB.Commits.Count(), 4);
        }
    }
}
