using Autofac;

namespace Surging.Cloud.CPlatform.Module
{
    public  class ContainerBuilderWrapper
    {
       
        public ContainerBuilder ContainerBuilder { get; private set; }
 
        public ContainerBuilderWrapper(ContainerBuilder builder)
        {
            ContainerBuilder = builder;
        }
        
    }
}
