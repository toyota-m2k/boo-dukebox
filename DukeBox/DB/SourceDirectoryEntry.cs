using DukeBox.model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DukeBox.DB;

[Table("t_source_directory")]

public class SourceDirectoryEntry : ISourceDirectory {
    public static string[] DDL = {
        @"CREATE TABLE IF NOT EXISTS t_source_directory(
            Id INTEGER NOT NULL PRIMARY KEY,
            Path TEXT NOT NULL UNIQUE,
            Recursive INTEGER NOT NULL DEFAULT 1,
            Category INTEGER DEFAULT NULL
        )",
    };

    [Key]
    [Column("Id")]
    public long Id { get; set; }
    [Column("Path")]
    public string Path { get; set; } = string.Empty;
    [Column("Recursive")]
    public bool Recursive { get; set; } = true;
    [Column("Category")]
    public long? Category { get; set; }

    [ForeignKey(nameof(Category))]
    public CategoryEntry? CategoryRef { get; set; }

    // カテゴリー名（t_categoryテーブルのName列）を取得するプロパティ
    [NotMapped]
    public string CategoryName => CategoryRef?.Name ?? string.Empty;
}
