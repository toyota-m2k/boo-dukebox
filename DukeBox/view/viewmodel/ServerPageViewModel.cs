using DukeBox.DB;
using DukeBox.DI.contract;
using DukeBox.model;
using io.github.toyota32k.BooLib.Server.mdns;
using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DukeBox.view.viewmodel;

public class ServerPageViewModel: IMediaListSource {
    public UtLog Logger = new UtLog("ServerPage");
    private readonly IHttpService _httpService;
    private readonly IListingFileService _listingFileService;
    private readonly IPageService _pageService;
    private readonly IDBService _dBService;
    private readonly ISettingsService _settingsService;

    public ReactiveCommand StopServerCommand = new ReactiveCommand();
    public ReactiveCommand ShowQRCodeCommand = new ReactiveCommand();

    #region Filter

    // Category (Channel) 

    public List<ICategory> CategoryList { get; }
    public ReactiveProperty<ICategory> SelectedCategory { get; } = new(CategoryEntry.ALL);

    // Rating
    public ReactiveProperty<bool> RatingFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> Rating1Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating2Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating3Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating4Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating5Checked { get; } = new(true);
    public ReadOnlyReactiveProperty<IList<bool>?> RatingFilterParam{ get; }
    // Mark
    public ReactiveProperty<bool> MarkFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> Mark0Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark1Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark2Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark3Checked { get; } = new(true);
    public ReadOnlyReactiveProperty<IList<bool>?> MarkFilterParam { get; }

    // Type
    public ReactiveProperty<bool> TypeFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> TypeVideoChecked { get; } = new(true);
    public ReactiveProperty<bool> TypeAudioChecked { get; } = new(true);
    public ReactiveProperty<bool> TypePhotoChecked { get; } = new(true);
    public ReadOnlyReactiveProperty<IList<MediaFilter.MediaType>?> TypeFilterParam { get; }

    public ReadOnlyReactiveProperty<IMediaFilter> Filter { get; }


    #endregion

    #region Media File List

    public Func<bool, IList<IMediaFile>>? GetDisplayedItemsProc;

    public ObservableCollection<IMediaFile> MediaFiles => _listingFileService.MediaFiles;
    public IList<IMediaFile> GetMediaList(MediaListMode mode) {
        switch (mode) {
            default:
            case MediaListMode.ALL:
                return MediaFiles;
            case MediaListMode.LISTED:
                return GetDisplayedItemsProc?.Invoke(false) ?? MediaFiles;
            case MediaListMode.SELECTED:
                return GetDisplayedItemsProc?.Invoke(true) ?? MediaFiles;
        }
    }


    public ServerPageViewModel(
        IHttpService httpService, 
        IListingFileService listingFileService, 
        IPageService pageService, 
        IDBService dBService,
        ISettingsService settings
        ) {
        _httpService = httpService;
        _listingFileService = listingFileService;
        _pageService = pageService;
        _dBService = dBService;
        _settingsService = settings;

        StopServerCommand.Subscribe(StopServer);
        ShowQRCodeCommand.Subscribe(ShowQRCode);

        CategoryList = [CategoryEntry.ALL, CategoryEntry.DEFAULT, .. _dBService.CategoryStore.Enumerable()];

        List<ReactiveProperty<bool>> ratings = [Rating1Checked, Rating2Checked, Rating3Checked, Rating4Checked, Rating5Checked];
        RatingFilterParam = Observable.CombineLatest(ratings, s => {
            if (s.All(it => it) || s.All(it => !it)) {
                return null;
            }
            return s;
        }).DistinctUntilChanged().ToReadOnlyReactiveProperty();
        List<ReactiveProperty<bool>> marks = [Mark0Checked, Mark1Checked, Mark2Checked, Mark3Checked];
        MarkFilterParam = Observable.CombineLatest(marks, s => {
            if (s.All(it => it) || s.All(it => !it)) {
                return null;
            }
            return s;
        }).DistinctUntilChanged().ToReadOnlyReactiveProperty();

        List<ReactiveProperty<bool>> types = [TypeVideoChecked, TypeAudioChecked, TypePhotoChecked];
        TypeFilterParam = Observable.CombineLatest(types, s => {
            if (s.All(it => it) || s.All(it => !it)) {
                return null;
            }
            IList<MediaFilter.MediaType> result = new List<MediaFilter.MediaType>();
            if (s[0]) result.Add(MediaFilter.MediaType.VIDEO);
            if (s[1]) result.Add(MediaFilter.MediaType.AUDIO);
            if (s[2]) result.Add(MediaFilter.MediaType.PHOTO);
            return result;
        }).DistinctUntilChanged().ToReadOnlyReactiveProperty();

        Filter = Observable.CombineLatest(SelectedCategory, RatingFilterEnabled, RatingFilterParam, MarkFilterEnabled, MarkFilterParam, TypeFilterEnabled, TypeFilterParam,
             (category, ratingEnabled, ratingParam, markEnabled, markParam, typeEnabled, typeParam) => {
                 //if (category.Id == CategoryEntry.ALL.Id && !ratingEnabled && !markEnabled && !typeEnabled) {
                 //    return MediaFilter.Empty;
                 //}
                 var filter = new MediaFilter();
                 if (category.Id != CategoryEntry.ALL.Id) {
                     filter.Category = category.Id;
                 }
                 if (ratingEnabled && ratingParam != null) {
                     filter.Ratings = new List<bool>(ratingParam);
                 }
                 if (markEnabled && markParam != null) {
                     filter.Marks = new List<bool>(markParam);
                 }
                 if (typeEnabled && typeParam != null) {
                     filter.TargetTypes = typeParam;
                 }
                 return filter as IMediaFilter;
             }).DistinctUntilChanged().ToReadOnlyReactiveProperty<IMediaFilter>();

        Filter.Subscribe(filter => {
            _listingFileService.SetFilter(filter);
        });

        // DIで循環参照にならないよう、ここで_httpServerにIMediaListSourceを注入
        _httpService.SetMediaListSource(this);
    }
    #endregion

    private void StopServer() {
        _pageService.GoBack();
    }

    private async void ShowQRCode() {
        var dlg = new PairingQRCodeDialog {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            ServerName = _settingsService.EnsureServerName,
            Port = _settingsService.EnableSSL ? _settingsService.SSLServerPort : _settingsService.ServerPort,
            IsHttps = _settingsService.EnableSSL,
            Fingerprint = ComputeFingerprintIfPossible()
        };
        await dlg.ShowAsync();
    }

    public void UpdateMediaFile(IMediaFile file) {
        _dBService.EditMediaFileStore(store => {
            store.Update(file);
            return true;
        });
    }

    private string ComputeFingerprintIfPossible() {
        if (!_settingsService.EnableSSL) return "";
        if (string.IsNullOrEmpty(_settingsService.PFXFilePath) || !File.Exists(_settingsService.PFXFilePath)) return "";
        try {
            // EphemeralKeySet: MSIX サンドボックス下でも OS ストアアクセスを発生させない
            using var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _settingsService.PFXFilePath!, _settingsService.PFXPassword,
                X509KeyStorageFlags.EphemeralKeySet);
            //using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
            //    settings.PfxPath!, settings.PfxPassword,
            //    System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);
            return CertificateGenerator.ComputeSha256Fingerprint(cert);
        }
        catch (Exception e) {
            Debug.WriteLine(e);
            return "";
        }
    }

}
