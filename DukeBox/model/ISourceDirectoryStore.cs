using DukeBox.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model;

public enum ConflictType {
    None,
    Registered,
    SubdirectoryOfRegistered,
    ParentDirectoryOfRegistered,
}

public interface ISourceDirectory {
    long Id { get; }
    string Path { get; }
    bool Recursive { get; }
    long? Category { get; }
    string CategoryName { get; }
}

public interface ISourceDirectoryStore {
    IEnumerable<ISourceDirectory> List();
    ISourceDirectory? Get(long id);
    ISourceDirectory? GetByPath(string path);
    ConflictType CheckConfliction(string path, bool includeSubdirectories = true);
}

public interface IMutableSourceDirectoryStore : ISourceDirectoryStore {
    ISourceDirectory? Add(string path, bool recursive = true, long category=0);
    //void Remove(ISourceDirectory entry);
    //void Remove(string path);
}
