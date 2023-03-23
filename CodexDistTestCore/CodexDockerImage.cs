﻿using k8s.Models;

namespace CodexDistTestCore
{
    public class CodexDockerImage
    {
        public string GetImageTag()
        {
            return "thatbenbierens/nim-codex:sha-b204837";
        }

        public string GetExpectedImageRevision()
        {
            return "b20483";
        }

        public List<V1EnvVar> CreateEnvironmentVariables(OfflineCodexNodes node, CodexNodeContainer environment)
        {
            var formatter = new EnvFormatter();
            formatter.Create(node, environment);
            return formatter.Result;
        }

        private class EnvFormatter
        {
            public List<V1EnvVar> Result { get; } = new List<V1EnvVar>();

            public void Create(OfflineCodexNodes node, CodexNodeContainer environment)
            {
                AddVar("API_PORT", environment.ApiPort.ToString());
                AddVar("DATA_DIR", environment.DataDir);
                AddVar("DISC_PORT", environment.DiscoveryPort.ToString());
                AddVar("LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{environment.ListenPort}");

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
                    AddVar("STORAGE_QUOTA", node.StorageQuota.SizeInBytes.ToString()!);
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
