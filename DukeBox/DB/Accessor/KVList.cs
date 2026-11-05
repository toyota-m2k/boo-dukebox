using DukeBox.model;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DukeBox.DB.Accessor;
public class KVList: IMutableKVStore {
    private DBConnector _connector;
    private DbSet<KV> _kvs;
    public KVList(DBConnector connector) {
        _connector = connector;
        _kvs = connector.KVs;
    }

    public string? this[string key] {
        get => GetString(key);
        set {
            if (value != null) {
                SetString(key, value);
            }
            else {
                Delete(key);
            }
        }
    }

    public void Delete(string key) {
        lock (_connector) {
            _kvs.RemoveRange(_kvs.Where(it => it.Key == key));
        }
    }

    public int GetInt(string key, int def=0) {
        lock (_connector) {
            return int.TryParse(_kvs.FirstOrDefault(it => it.Key == key)?.Value, out var result) ? result : def;
        }
    }

    public string? GetString(string key) {
        lock (_connector) {
            return _kvs.FirstOrDefault(it => it.Key == key)?.Value;
        }
    }

    public void SetInt(string key, int value) {
        SetString(key, value.ToString());
    }

    public void SetString(string key, string value) {
        lock (_connector) {
            var e = _kvs.FirstOrDefault(it => it.Key == key);
            if (e != null) {
                e.Value = value;
            }
            else {
                _kvs.Add(new KV() {
                    Key = key,
                    Value = value
                });
            }
        }
    }

    public void SaveChanges() {
        lock (_connector) {
            _connector.SaveChanges();
        }
    }
}
