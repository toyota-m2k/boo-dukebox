using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DukeBox.model;

public interface IKVStore {
    int GetInt(string key, int def = 0);
    string? GetString(string key);
    string? this[string key] { get; }
}

public interface IMutableKVStore : IKVStore {
    void SetInt(string key, int value);
    void SetString(string key, string value);
    void Delete(string key);
    new string? this[string key] { get; set; }
}
