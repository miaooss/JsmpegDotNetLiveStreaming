using Microsoft.Practices.Unity;
using System;

namespace LiveStreamingWebRTC.Setup
{
    public class Boostrap : IDisposable
    {
        private UnityContainer uc;

        public Boostrap()
        {
            uc = new UnityContainer();
        }

        internal void Configure()
        {
            var factories = new[]
            {
                new Factory(uc)
            };

            foreach (var factory in factories)
            {
                factory.Configure();
            }
        }

        internal T Resolve<T>()
        {
            return uc.Resolve<T>();
        }

        internal WebRTCServer Exec()
        {
            var webRtc = uc.Resolve<WebRTCServer>();
            webRtc.Start(9000);
            return webRtc;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    uc.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Boostrap() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
