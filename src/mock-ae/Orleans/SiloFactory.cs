using System;
using System.Collections.Generic;
using System.Text;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    class SiloFactory
    {
        private readonly ClusterConfiguration _clusterConfiguration;

        public SiloFactory(ClusterConfiguration clusterConfiguration)
        {
            _clusterConfiguration = clusterConfiguration;
        }

        public ISiloHost Create()
        {
            throw new NotImplementedException();
        }
    }
}
