using DukeBox.model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DukeBox.DB;

[Table("t_category")]
public class CategoryEntry : ICategory {
    public static string[] DDL = {
        @"CREATE TABLE IF NOT EXISTS t_category(
            Id INTEGER NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL UNIQUE,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            Color INTEGER NOT NULL DEFAULT 0,
            IconPath TEXT NOT NULL DEFAULT ''
        )",
    };

    [Key,Required]
    public long Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public int SortOrder { get; set; } = 0;
    [Required]
    public uint Color { get; set; } = 0;
    [Required]
    public string IconPath { get; set; } = string.Empty;

    [NotMapped]
    public string ColorText {
        get {
            if (Color == 0) {
                return string.Empty;
            }
            return $"#{A(Color):X2}{R(Color):X2}{G(Color):X2}{B(Color):X2}";
        }
    }

    private static byte A(uint v) {
        return (byte)((v & 0xFF000000) >> 24);
    }
    private static byte R(uint v) {
        return (byte)((v & 0x00FF0000) >> 16);
    }
    private static byte G(uint v) {
        return (byte)((v & 0x0000FF00) >> 8);
    }
    private static byte B(uint v) {
        return (byte)(v & 0x000000FF);
    }

    public static CategoryEntry FromCategory(ICategory category) {
        if (category is CategoryEntry entry) {
            return entry;
        } else {
            throw new ArgumentException("The provided category is not a CategoryEntry.", nameof(category));
        }
    }

    public static ICategory DEFAULT = new CategoryEntry() { Id = 0, Name = "Default" };
    public static ICategory ALL = new CategoryEntry() { Id = -1, Name = "ALL" };
}
