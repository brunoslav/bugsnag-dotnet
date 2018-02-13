﻿using Bugsnag.Payload;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Bugsnag.SessionTracking
{
  public class SessionsStore
  {
    private static readonly object _instanceLock = new object();
    private static SessionsStore _instance;

    private readonly Dictionary<IConfiguration, Dictionary<string, long>> _store;
    private readonly object _lock;
    private readonly Timer _timer;

    private SessionsStore()
    {
      _store = new Dictionary<IConfiguration, Dictionary<string, long>>();
      _lock = new object();
      _timer = new Timer(SendSessions, new AutoResetEvent(false), TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    private void SendSessions(object state)
    {
      Dictionary<IConfiguration, Dictionary<string, long>> sessionData = new Dictionary<IConfiguration, Dictionary<string, long>>();

      lock (_lock)
      {
        foreach (var item in _store)
        {
          sessionData[item.Key] = item.Value;
          _store[item.Key].Clear();
        }
      }

      foreach (var item in sessionData)
      {
        var payload = new BatchedSessions(item.Key, item.Value);
        ThreadQueueTransport.Instance.Send(payload);
      }
    }

    public static SessionsStore Instance
    {
      get
      {
        lock (_instanceLock)
        {
          if (_instance == null)
          {
            _instance = new SessionsStore();
          }
        }

        return _instance;
      }
    }

    public Session CreateSession(IConfiguration configuration)
    {
      var session = new Session();

      lock (_lock)
      {
        if (!_store.TryGetValue(configuration, out Dictionary<string, long> sessionCounts))
        {
          _store[configuration] = sessionCounts = new Dictionary<string, long>();
        }

        sessionCounts.TryGetValue(session.SessionKey, out long sessionCount);
        sessionCounts[session.SessionKey] = sessionCount + 1;
      }

      return session;
    }
  }
}