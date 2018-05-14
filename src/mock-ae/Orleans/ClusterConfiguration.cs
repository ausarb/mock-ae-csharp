using System;
using System.Collections.Generic;
using System.Text;

namespace Mattersight.mock.ba.ae.Orleans
{
    public class ClusterConfiguration
    {
        public string OrleansClusterId { get; } = "dev";
        public string OrleansServiceId { get; } = "mock-ae-csharp";
    }
}
