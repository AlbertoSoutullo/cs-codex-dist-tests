﻿using DistTestCore;
using Logging;

namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly K8sFactory k8SFactory = new K8sFactory();
        private readonly ConfigLoader configLoader = new ConfigLoader();
        private readonly TestFactory testFactory = new TestFactory();
        private readonly Configuration config;
        private readonly StartupChecker startupChecker;
        private readonly CancellationToken cancelToken;

        public ContinuousTestRunner(string[] args, CancellationToken cancelToken)
        {
            config = configLoader.Load(args);
            startupChecker = new StartupChecker(config, cancelToken);
            this.cancelToken = cancelToken;
        }

        public void Run()
        {
            startupChecker.Check();

            var taskFactory = new TaskFactory();
            var overviewLog = new FixtureLog(new LogConfig(config.LogPath, false), DateTime.UtcNow, "Overview");
            overviewLog.Log("Continuous tests starting...");
            var allTests = testFactory.CreateTests();

            ClearAllCustomNamespaces(allTests, overviewLog);

            StartLogDownloader(taskFactory);

            var testLoops = allTests.Select(t => new TestLoop(taskFactory, config, overviewLog, t.GetType(), t.RunTestEvery, cancelToken)).ToArray();

            foreach (var testLoop in testLoops)
            {
                if (cancelToken.IsCancellationRequested) break;

                overviewLog.Log("Launching test-loop for " + testLoop.Name);
                testLoop.Begin();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            overviewLog.Log("Finished launching test-loops.");
            cancelToken.WaitHandle.WaitOne();
            overviewLog.Log("Cancelling all test-loops...");
            taskFactory.WaitAll();
            overviewLog.Log("All tasks cancelled.");
        }

        private void ClearAllCustomNamespaces(ContinuousTest[] allTests, FixtureLog log)
        {
            foreach (var test in allTests) ClearAllCustomNamespaces(test, log);
        }

        private void ClearAllCustomNamespaces(ContinuousTest test, FixtureLog log)
        {
            if (string.IsNullOrEmpty(test.CustomK8sNamespace)) return;

            log.Log($"Clearing namespace '{test.CustomK8sNamespace}'...");
            var lifecycle = k8SFactory.CreateTestLifecycle(config.KubeConfigFile, config.LogPath, config.DataPath, test.CustomK8sNamespace, new DefaultTimeSet(), log);
            lifecycle.WorkflowCreator.CreateWorkflow().DeleteTestResources();
        }

        private void StartLogDownloader(TaskFactory taskFactory)
        {
            if (!config.DownloadContainerLogs) return;

            var path = Path.Combine(config.LogPath, "containers");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var lifecycle = k8SFactory.CreateTestLifecycle(config.KubeConfigFile, config.LogPath, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, new DefaultTimeSet(), new NullLog());
            var downloader = new ContinuousLogDownloader(lifecycle, config.CodexDeployment, path, cancelToken);

            taskFactory.Run(downloader.Run);
        }
    }
}
