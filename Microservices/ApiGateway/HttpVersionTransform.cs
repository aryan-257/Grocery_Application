using Yarp.ReverseProxy.Transforms;

public class HttpVersionTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        context.ProxyRequest.Version = new Version(1, 1);
        context.ProxyRequest.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        return ValueTask.CompletedTask;
    }
}
