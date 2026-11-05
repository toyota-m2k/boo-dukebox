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
    IEnumerable<IMediaFile> Enumerable(bool tracking = false);
    IList<IMediaFile> List(bool tracking=false);

    IMediaFile? GetById(long id);
    IMediaFile? GetByPath(string path);
}

public interface IMutableMediaFileStore : IMediaFileStore {
    IMediaFile AddOrUpdate(string path, string name, long size, string type, DateTimeOffset mediaCreationDate, long duration = 0L, bool converted = false,
        int rating = 0, int mark = 0, string label = "", long category = 0, string chapters = "");
    IMediaFile Add(string path, string name, long size, string type, DateTimeOffset mediaCreationDate, long duration = 0L, bool converted = false,
        int rating = 0, int mark = 0, string label = "", long category = 0, string chapters = "");

    IMediaFile Update(IMediaFile entry, string? name = null, long? size = null, long? duration = null, bool? converted = null,
        int? rating = null, int? mark = null, string? label = null, long? category = null, string? chapters = null);
    IMediaFile Update(IMediaFile modifiedEntry);
    //void Remove(IMediaFile entry);
    //void Remove(IEnumerable<IMediaFile> entries);
    IDataChangeEventSource ChangeEventSource { get; }
}
