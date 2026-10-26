using DukeBox.DB;
using DukeBox.DB.Accessor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model;

public interface IDataChangeEventSource {
    void Submit();
    void Reset();
}

public interface IMediaFileStore {
    IObservable<DataChangeInfo> Changes { get; }
    IEnumerable<IMediaFile> List();
    IEnumerable<IMediaFile> List(Func<IMediaFile, bool> predicate);
    IEnumerable<T> ListAs<T>(Func<IMediaFile, bool> predicate, Func<IMediaFile, T> select);
    IEnumerable<T> ListAs<T>(Func<IMediaFile, T?> select) where T : class;

    IMediaFile? GetById(long id);
    IMediaFile? GetByPath(string path);
}

public interface IMutableMediaFileStore : IMediaFileStore {
    IMediaFile AddOrUpdate(string path, string name, long size, string type, long duration = 0L, string copyPath = "",
        int rating = 0, int mark = 0, string label = "", long category = 0, string chapters = "");
    IMediaFile Add(string path, string name, long size, string type, long duration = 0L, string copyPath = "",
        int rating = 0, int mark = 0, string label = "", long category = 0, string chapters = "");

    IMediaFile Update(IMediaFile entry, string? name = null, long? size = null, long? duration = null, string? copyPath = null,
        int? rating = null, int? mark = null, string? label = null, long? category = null, string? chapters = null);
    void Remove(IMediaFile entry);
    void Remove(IEnumerable<IMediaFile> entries);
    IDataChangeEventSource ChangeEventSource { get; }
}
