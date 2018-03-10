using System.Web;

namespace Bugsnag.AspNet
{
  public static class Client
  {
    public const string HttpContextItemsKey = "Bugsnag.Client";

    private static readonly object _globalClientLock = new object();

    private static IClient _globalClient;

    public static IClient Current
    {
      get
      {
        // if we are in a request context then we should make sure we are
        // returning a request scoped client
        if (HttpContext.Current != null)
        {
          // we may need to do some locking here
          if (HttpContext.Current.Items[HttpContextItemsKey] is IClient client)
          {
            return client;
          }
          else
          {
            // this is the first time a client has been requested for this
            // request scope, so create one and attach it to the request
            var requestScopedClient = new Bugsnag.Client(ConfigurationSection.Configuration.Settings);
            HttpContext.Current.Items[HttpContextItemsKey] = requestScopedClient;
            return requestScopedClient;
          }
        }
        else
        {
          // we are not in a request scope so we fallback to a 'global' client
          lock (_globalClientLock)
          {
            if (_globalClient == null)
            {
              _globalClient = new Bugsnag.Client(ConfigurationSection.Configuration.Settings);
            }
          }

          return _globalClient;
        }
      }
    }
  }
}
