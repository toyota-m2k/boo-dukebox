using DukeBox.model;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DukeBox.DI.contract;

public enum MediaListMode {
    ALL = 0,
    LISTED = 1,
    SELECTED = 2,
}

public interface IMediaListSource {
    IList<IMediaFile> GetMediaList(MediaListMode mode);
}

public interface IHttpService {
    IReadOnlyReactiveProperty<bool> Running { get; }
    bool Start();
    Task StopAsync();
    void SetMediaListSource(IMediaListSource? source);
}
