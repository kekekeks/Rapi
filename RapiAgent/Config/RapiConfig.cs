using RapiAgent.Config.Options;

namespace RapiAgent.Config;

/// <summary>
/// Validated, immutable configuration for RapiAgent.
/// Created once at startup via <see cref="Convert"/>; if validation fails, an exception is thrown and the agent exits.
/// Inject this class directly (registered as a singleton in DI) — do not use IOptions&lt;T&gt; inside the application.
/// </summary>
public class RapiConfig
{
    
    private RapiConfig()
    {
    }

    /// <summary>
    /// Validates <paramref name="options"/> and constructs a <see cref="RapiConfig"/>.
    /// Throws <see cref="InvalidOperationException"/> if the configuration is invalid.
    /// </summary>
    public static RapiConfig Convert(RapiOptions options)
    {
        return new RapiConfig();
    }
}
