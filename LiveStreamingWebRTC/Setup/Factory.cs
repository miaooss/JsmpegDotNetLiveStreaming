using LiveStreamingWebRTC.Logger;
using LiveStreamingWebRTC.Message;
using Microsoft.Practices.Unity;

namespace LiveStreamingWebRTC.Setup
{
    public class Factory
    {
        private readonly UnityContainer uc;
        public Factory(UnityContainer uc)
        {
            this.uc = uc;
        }

        public void Configure()
        {
            ConfigureLogger();
            ConfigureMessage();
            ConfigureRoot();
        }

        protected void ConfigureLogger()
        {
            RegisterSingletonType<ILogger, Logger.Logger>();
        }

        protected void ConfigureMessage()
        {
            RegisterSingletonType<IWebRTCMessageFactory, WebRTCMessageFactory>();
        }

        protected void ConfigureRoot()
        {
            RegisterSingletonType<IRTSPChannelFactory, RTSPChannelFactory>();
            RegisterSingletonType<WebRTCServer, WebRTCServer>();
        }


        protected void RegisterSingletonType<TInterface, TType>()
            where TType : TInterface
        {
            var lifeTimeManager = new ContainerControlledLifetimeManager();
            uc.RegisterType<TInterface, TType>(lifeTimeManager);
        }
    }
}
