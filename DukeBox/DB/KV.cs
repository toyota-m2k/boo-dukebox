using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DukeBox.DB;

[Table("t_kv")]
public class KV {
    public static string[] DDL = {
        @"CREATE TABLE IF NOT EXISTS t_kv(
            Key TEXT NOT NULL PRIMARY KEY,
            Value TEXT
        )",
    };

    [Key, Required]
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}
