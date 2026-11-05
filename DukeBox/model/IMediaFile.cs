namespace DukeBox.model; 
public interface IMediaFile {
    public long Id { get; }
    public string Name { get; }
    public long Size { get; }
    public string Type { get; }                         // ファイルの拡張子（.は含まない: eg. mp4, jpg, png, mp3, m4a）
    public string Path { get; }

    public bool Converted { get; }

    public long CreationDate { get; }                      // ファイルの作成日時（UnixTime）
    public long RegisteredDate { get; }                    // SAに登録された日時（UnixTime）
    public long LastModifiedDate { get; }                  // ファイルの最終更新日時（UnixTime）
    public bool Deleted { get; }                           // 削除フラグ（0:有効、1:削除済み）
    public int Rating { get; }
    public int Mark { get; }
    public string Label { get; }
    public long? Category { get; }
    public string CategoryName { get; }                  // カテゴリー名（t_categoryテーブルのName列）を取得するプロパティ
    public string Chapters { get; }
    public long Duration { get; }


    //public string FilePath { get; }                        // ファイルの絶対パスを返す。CopyPathが設定されている場合はCopyPathを返す。}
    public string BooMediaType { get; }                     // Boo ProtocolのMediaTypeを返す。動画なら"v"、画像なら"p"、音声なら"a"、それ以外は""を返す。
    public string ContentType { get; }                      // MIMEタイプ（Content-Type）を返す。mp4ならvideo/mp4、pngならimage/png、jpg/jpegならimage/jpeg、mp3/m4aならaudio/mpeg
    public int ComplementalRating { get; }                  // Rating == 0 （デフォルト）を 3 として扱うプロパティ
    public string ComplementalCategoryName { get; }
    /**
     * 変更可能なプロパティを変更した新しいIMediaFileインスタンスを返す。
     * 変更しない項目は null （デフォルト引数）を指定する。
     * 
     * category 引数の指定方法に注意：
     * categoryのデフォルトは null（カテゴリなし）なので、変更しない場合の null と区別するため、
     * 「カテゴリなし」に変更する場合は 0 を指定する。
     */
    public IMediaFile Modify(
        string? name = null, long? size = null, long? duration = null, bool? converted = null, long? lastModified = null,
        bool? isDeleted=null, int? rating = null, int? mark = null, string? label = null, long? category = null, string? chapters = null);

}
