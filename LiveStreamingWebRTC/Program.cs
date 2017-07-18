using LiveStreamingWebRTC.Logger;
using LiveStreamingWebRTC.Setup;
using System;

namespace LiveStreamingWebRTC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ILogger logger = null;
            try
            {
                using (var boostrap = new Boostrap())
                {
                    boostrap.Configure();
                    logger = boostrap.Resolve<ILogger>();

                    boostrap.Exec();
                    Console.Read();
                }
            }
            catch (Exception ex)
            {
                logger?.Error(ex.ToString());
                Console.Read();
            }
        }
    }
}
