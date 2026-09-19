namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// Opt-in extension for <see cref="Mapbox.BaseModule.Data.Interfaces.ILayerModule"/>
    /// implementations that want to observe tile lifecycle events (load / unload) to
    /// run their own bookkeeping. The observer side of the pattern; the subject side
    /// is <see cref="ITileLifecycleSource"/>.
    ///
    /// <see cref="MapboxMapVisualizer.Initialize"/> calls <see cref="AttachToMapVisualizer"/>
    /// on every layer module that implements this interface, after the modules are
    /// added but before their own <c>Initialize</c> runs. Modules that don't need to
    /// observe tile lifecycle simply don't implement this interface.
    /// </summary>
    public interface ITileLifecycleObserver
    {
        void AttachToMapVisualizer(ITileLifecycleSource source);
    }
}
