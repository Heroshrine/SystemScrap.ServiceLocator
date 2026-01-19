namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Specifies the nature of a service registration.
    /// </summary>
    public enum ServiceKind
    {
        Instance,
        Transient,
        LazyProvider
    }
}