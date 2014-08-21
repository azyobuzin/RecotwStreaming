using Microsoft.Owin.Cors;
using Owin;

namespace RecotwStreaming
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll)
                .MapSignalR()
                .UseNancy();
        }
    }
}
