using System.Reflection;
using Nancy;
using Nancy.Responses;

namespace RecotwStreaming
{
    public class StaticPageModule : NancyModule
    {
        public StaticPageModule()
        {
            var asm = Assembly.GetExecutingAssembly();
            Get["/"] = _ => new EmbeddedFileResponse(asm, asm.GetName().Name, "index.html");
        }
    }
}
