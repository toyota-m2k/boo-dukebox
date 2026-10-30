using DukeBox.DI.contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model;

public class MediaFilter : IMediaFilter {
    public long Category = -1; // -1: all / 0: default / 1...
    public int[]? Rating = null; // null: all / ratings to allow
    public int[]? Mark = null;   // 

    public enum MediaType {
        VIDEO,
        AUDIO,
        PHOTO,
    }
    public MediaType[]? TargetTypes = null;

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
        if (Rating != null) {
            if (!Rating.Contains(item.Rating)) {
                return false;
            }
        }
        if (Mark != null) {
            if (!Mark.Contains(item.Mark)) {
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

}

