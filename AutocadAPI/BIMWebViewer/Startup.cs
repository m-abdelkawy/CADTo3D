using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BIMWebViewer.Startup))]
namespace BIMWebViewer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
