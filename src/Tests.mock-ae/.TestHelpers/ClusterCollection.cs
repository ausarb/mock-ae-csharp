using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Mattersight.mock.ba.ae.Tests.TestHelpers
{
    [CollectionDefinition(ClusterCollection.Name)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture>
    {
        public const string Name = "ClusterCollection";
    }
}
