using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Recognizr.AzureMobileApp.Startup))]

namespace Recognizr.AzureMobileApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureMobileApp(app);
        }
    }
}