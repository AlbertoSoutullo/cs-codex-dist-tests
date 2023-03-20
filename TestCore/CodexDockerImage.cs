﻿using k8s.Models;

namespace CodexDistTests.TestCore
{
    public class CodexDockerImage
    {
        public string GetImageTag()
        {
            return "thatbenbierens/nim-codex:sha-c9a62de";
        }

        public List<V1EnvVar> CreateEnvironmentVariables(OfflineCodexNode node)
        {
            var formatter = new EnvFormatter();
            formatter.Create(node);
            return formatter.Result;
        }

        private class EnvFormatter
        {
            public List<V1EnvVar> Result { get; } = new List<V1EnvVar>();

            public void Create(OfflineCodexNode node)
            {
                if (node.BootstrapNode != null)
                {
                    var debugInfo = node.BootstrapNode.GetDebugInfo();
                    AddVar("BOOTSTRAP_SPR", debugInfo.spr);
                }
                if (node.LogLevel != null)
                {
                    AddVar("LOG_LEVEL", node.LogLevel.ToString()!.ToUpperInvariant());
                }
                if (node.StorageQuota != null)
                {
                    AddVar("STORAGE_QUOTA", node.StorageQuota.ToString()!);
                }
            }

            private void AddVar(string key, string value)
            {
                Result.Add(new V1EnvVar
                {
                    Name = key,
                    Value = value
                });
            }
        }
    }
}