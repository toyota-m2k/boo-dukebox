using DukeBox.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model; 


public class DataChangeInfo {
    public enum Change {
        Add,
        Remove,
        Update,
        ResetAll,
    }
    public Change Type { get; }
    public IMediaFile Item { get; }
    DataChangeInfo(Change type, IMediaFile item) {
        Type = type;
        Item = item;
    }

    public static DataChangeInfo Add(IMediaFile entry) {
        return new DataChangeInfo(Change.Add, entry);
    }
    public static DataChangeInfo Remove(IMediaFile entry) {
        return new DataChangeInfo(Change.Remove, entry);
    }
    public static DataChangeInfo Update(IMediaFile entry) {
        return new DataChangeInfo(Change.Update, entry);
    }
    public static DataChangeInfo ResetAll() {
        return new DataChangeInfo(Change.ResetAll, new FileEntry());
    }
}
