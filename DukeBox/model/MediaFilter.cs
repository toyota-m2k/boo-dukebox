using DukeBox.DI.contract;
using System;
using System.Collections.Generic;

namespace DukeBox.model;

public class MediaFilter : IMediaFilter {
    public long Category = -1; // -1: all / 0: default / 1...
    public IList<bool>? Ratings = null; // null: all / ratings to allow
    public IList<bool>? Marks = null;   // 

    public enum MediaType {
        VIDEO,
        AUDIO,
        PHOTO,
    }
    public IList<MediaType>? TargetTypes = null;

    public MediaType? MediaTypeOf(IMediaFile file) {
        switch (file.BooMediaType) {
            case "v": return MediaType.VIDEO;
            case "a": return MediaType.AUDIO;
            case "p": return MediaType.PHOTO;
            default: return null;
        }
    }

    public enum DateType {
        CreationDate,
        RegisteredDate,
        LastModifedDate,
    }

    DateTimeOffset DateOf(IMediaFile item, DateType dateType) {
        long date;
        switch (dateType) {
            default:
            case DateType.CreationDate:
                date = item.CreationDate;
                break;
            case DateType.RegisteredDate:
                date = item.RegisteredDate;
                break;
            case DateType.LastModifedDate:
                date = item.CreationDate;
                break;
        }
        return DateTimeOffset.FromUnixTimeMilliseconds(date);
    }

    public DateType DateTarget = DateType.CreationDate;
    public bool EnableStartDate = false;
    public bool EnableEndDate = false;
    public DateTimeOffset StartDate = DateTimeOffset.MinValue;
    public DateTimeOffset EndDate = DateTimeOffset.MaxValue;


    public bool Filter(IMediaFile item) {
        if (Category == 0) {
            if (item.Category != null && item.Category != 0) {
                return false;
            }
        }
        else if (Category > 0) {
            if (item.Category != Category) {
                return false;
            }
        }
        if (Ratings != null) {
            var r = Math.Clamp(item.ComplementalRating - 1, 0, Ratings.Count - 1);
            if (!Ratings[r]) {
                return false;
            }
        }
        if (Marks != null) {
            var m = Math.Clamp(item.Mark, 0, Marks.Count - 1);
            if (!Marks[m]) {
                return false;
            }
        }
        if (TargetTypes != null) {
            if (MediaTypeOf(item) is MediaType type) {
                if (!TargetTypes.Contains(type)) {
                    return false;
                }
            }
        }
        if (EnableStartDate) {
            var date = DateOf(item, DateTarget);
            if (date < StartDate) {
                return false;
            }
        }
        if (EnableEndDate) {
            var date = DateOf(item, DateTarget);
            if (date > EndDate) {
                return false;
            }
        }
        return true;
    }

    public bool IsEmpty =>
        Category < 0 && Ratings == null && Marks == null && TargetTypes == null && !EnableStartDate && !EnableEndDate;

    class EmptyFilter : IMediaFilter {
        public bool IsEmpty => true;
        public bool Filter(IMediaFile item) => true;
    }
    public static IMediaFilter Empty { get; } = new EmptyFilter();


    private static bool IsEqualList<T>(IList<T>? a, IList<T>? b) {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++) {
            if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) {
                return false;
            }
        }
        return true;
    }
    public static bool IsEqual(IMediaFilter a, IMediaFilter b) {
        //if (a == null && b == null) return true;
        //if (a == null || b == null) return false;
        if (a.IsEmpty && b.IsEmpty) return true;
        if (a is MediaFilter ma && b is MediaFilter mb) {
            if (ma.Category != mb.Category) return false;
            if (ma.EnableStartDate != mb.EnableStartDate) return false;
            if (ma.EnableEndDate != mb.EnableEndDate) return false;
            if (ma.EnableStartDate && ma.StartDate != mb.StartDate) return false;
            if (ma.EnableEndDate && ma.EndDate != mb.EndDate) return false;
            if (!IsEqualList(ma.Ratings, mb.Ratings)) return false;
            if (!IsEqualList(ma.Marks, mb.Marks)) return false;
            if (!IsEqualList(ma.TargetTypes, mb.TargetTypes)) return false;
            return true;
        }
        return false;
    }
}

