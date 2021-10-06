# Sliding Window Rate Limiter Middleware

This example demonstrates how to build custom rate limiting middleware for ASP.NET Core apps using Redis. This example uses [Basic Authentication](https://en.wikipedia.org/wiki/Basic_access_authentication) to limit api requests per-route or route-regex per-api key.

## Implementation Details

This app uses configurable custom middleware to limit api requests to it's various endpoints. Configurations can be added to the appsettings.json or appsettings.Development.json files in the form of:

```json
{
  "RedisRateLimits": [
    {
      "Path": "/api/RateLimited/limited",
      "Window": "30s",
      "MaxRequests": 5
    },
    {
      "PathRegex": "^/api/*",
      "Window": "1h",
      "MaxRequests": 50
    }
  ]
}
```

The middleware itself is contained in `Middleware/SlidingWindowRateLimiter.cs`

It extracts the api key, then pulls out the various rules needed:

```csharp
public IEnumerable<RateLimitRule> GetApplicableRules(HttpContext context)
{
    var limits = _config.GetSection("RedisRateLimits").Get<RateLimitRule[]>();
    var applicableRules = limits
        .Where(x => x.MatchPath(context.Request.Path))
        .OrderBy(x => x.MaxRequests)
        .GroupBy(x => new{x.PathKey, x.WindowSeconds})
        .Select(x=>x.First());
    return applicableRules;
}
```

Then run those rules through the following script:

```text
local current_time = redis.call('TIME')
local num_windows = ARGV[1]
for i=2, num_windows*2, 2 do
    local window = ARGV[i]
    local max_requests = ARGV[i+1]
    local curr_key = KEYS[i/2]
    local trim_time = tonumber(current_time[1]) - window
    redis.call('ZREMRANGEBYSCORE', curr_key, 0, trim_time)
    local request_count = redis.call('ZCARD',curr_key)
    if request_count >= tonumber(max_requests) then
        return 1
    end
end
for i=2, num_windows*2, 2 do
    local curr_key = KEYS[i/2]
    local window = ARGV[i]
    redis.call('ZADD', curr_key, current_time[1], current_time[1] .. current_time[2])
    redis.call('EXPIRE', curr_key, window)                
end
return 0
```

## Testing

Run the app with `dotnet run` and then run 

```bash
for n in {1..7}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X POST -H "Content-Length: 0" --user "foobar:password" http://localhost:5000/api/ratelimited/limited); sleep 0.5; done
```

To see the first rule throttle - then run

```bash
for n in {1..47}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X POST -H "Content-Length: 0" --user "foobar:password" http://localhost:5000/api/ratelimited/indirectly-limited); sleep 0.5; done
```

To test out the second rule and see it throttle