using io.github.toyota32k.BooLib.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SQLite;

namespace DukeBox.DB;

public class DBConnector : DbContext
{
    public DbSet<FileEntry> Entries { get; set; }
    public DbSet<KV> KVs { get; set; }
    public DbSet<CategoryEntry> Categories { get; set; }
    public DbSet<SourceDirectoryEntry> SourceDirectories { get; set; }


    //public int MajorVersion { get; } = 1;
    //public int MinorVersion { get; } = 1;

    public UtLog _logger = new UtLog("DBConnector");

    public static long DB_VERSION = 1L;

    private string _dbPath;
    //private SQLiteConnection mConnection;

    public DBConnector(string dbPath)
    {
        _dbPath = dbPath;
        //bool creation = false;
        //if (dbPath == ":memory:" || !Path.Exists(dbPath)) {
        //    creation = true;
        //}

        var builder = new SQLiteConnectionStringBuilder { DataSource = dbPath };

        using (var conn = new SQLiteConnection(builder.ConnectionString)) {
            conn.Open();
            InitTables(conn);
        }
        try {
            _ = Model;
        } catch(Exception e) {
            _logger.Error(e);
        }
    }

    private void InitTables(SQLiteConnection conn) {
        // WALモードに切り替え（設定はDBファイルに保存される）
        ExecuteRawSql(conn, "PRAGMA journal_mode=WAL");
        // DBのバージョンを取得
        var version = QueryLongRawSql(conn, "PRAGMA user_version");

        ExecuteRawSql(conn, DB.KV.DDL);
        ExecuteRawSql(conn, DB.CategoryEntry.DDL);
        ExecuteRawSql(conn, DB.FileEntry.DDL);
        ExecuteRawSql(conn, DB.SourceDirectoryEntry.DDL);


        if(version < DB_VERSION) {
            //if (version > 0) {  // version == 0なら初期化時なので、DDLは実行済み
            //    // バージョンアップ
            //    var sqls = DB.FileEntry.Migrate(version, DB_VERSION);
            //    if (sqls != null) {
            //        ExecuteRawSql(conn, sqls);
            //    }
            //    sqls = DB.DeviceMigrationInfo.Migrate(version, DB_VERSION);
            //    if (sqls != null) {
            //        ExecuteRawSql(conn, sqls);
            //    }
            //}
            ExecuteRawSql(conn, $"PRAGMA user_version = {DB_VERSION}");
        }
    }

    private void ExecuteRawSql(SQLiteConnection conn, params string[] sqls) {
        SQLiteCommandBuilder builder = new SQLiteCommandBuilder();
        if (conn == null) return;
        using (var cmd = conn.CreateCommand()) {
            foreach (var sql in sqls) {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
    private void QueryRawSql(SQLiteConnection conn, string sql, Action<SQLiteDataReader> action) {
        SQLiteCommandBuilder builder = new SQLiteCommandBuilder();
        if (conn== null) return;
        using (var cmd = conn.CreateCommand()) {
            cmd.CommandText = sql;
            using (var reader = cmd.ExecuteReader()) {
                action(reader);
            }
        }
    }
    private long QueryLongRawSql(SQLiteConnection conn, string sql, long defValue = 0L) {
        var result = defValue;
        QueryRawSql(conn, sql, (reader) => {
            if (reader.Read()) {
                result = reader.GetInt64(0);
            }
        });
        return result;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        //optionsBuilder.UseSqlite(mConnection);

        // SQL をデバッグ出力するなら、以下を有効にする
        //optionsBuilder.LogTo(msg => System.Diagnostics.Debug.WriteLine(msg));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
    }
}
