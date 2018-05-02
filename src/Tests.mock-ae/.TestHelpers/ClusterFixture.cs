using System;
using Orleans.TestingHost;

namespace Mattersight.mock.ba.ae.Tests.TestHelpers
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            this.Cluster = new TestClusterBuilder().Build();
            this.Cluster.Deploy();
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }
}
