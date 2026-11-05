using DukeBox.model;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DukeBox.DI.contract; 

public interface IMediaFilter {
    public bool IsEmpty { get; }
    public bool Filter(IMediaFile item);
}

public interface IListingFileService {
    ObservableCollection<IMediaFile> MediaFiles { get; }

    /**
     * StartScan()--StopScan() の間は true
     */
    IReadOnlyReactiveProperty<bool> IsRunning { get; }

    /**
     * FileScannerのリストを更新する。
     * Category, SourceDirectoryの変更があった場合に呼び出す。
     */
    Task UpdateScanners();

    /**
     * 定期的なScanner実行を開始する。
     */
    void StartScan();
    /**
     * 定期的なScanner実行を停止する。
     */
    Task StopScanAsync();

    void SetFilter(IMediaFilter? filter);
}
