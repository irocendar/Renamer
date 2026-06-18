using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Renamer;

public class FakeManifest : IManifest
{
    private readonly IManifest _manifest;
    private readonly ITranslationHelper _translationHelper;

    public string Name { get; set; }
    public string Description => _manifest.Description;
    public string Author => _manifest.Author;
    public ISemanticVersion Version => _manifest.Version;
    public ISemanticVersion? MinimumApiVersion => _manifest.MinimumApiVersion;
    public ISemanticVersion? MinimumGameVersion => _manifest.MinimumGameVersion;
    public string UniqueID => _manifest.UniqueID;
    public string? EntryDll => _manifest.EntryDll;
    public IManifestContentPackFor? ContentPackFor => _manifest.ContentPackFor;
    public IManifestDependency[] Dependencies => _manifest.Dependencies;
    public string[] UpdateKeys => _manifest.UpdateKeys;
    public IDictionary<string, object> ExtraFields => _manifest.ExtraFields;

    public FakeManifest(IManifest manifest, ITranslationHelper translationHelper)
    {
        _manifest = manifest;
        _translationHelper = translationHelper;
        Name = manifest.Name;
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        Name = _translationHelper.Get("ModName");
    }
}