using DukeBox.DI.concrete;
using DukeBox.DI.contract;
using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DukeBox.view.viewmodel;

public class ServerPageViewModel {
    public UtLog Logger = new UtLog("ServerPage");
    private readonly IHttpService _httpService;
    private readonly IListingFileService _listingFileService;
    private readonly IPageService _pageService;
    private readonly IDBService _dBService;

    public ObservableCollection<IMediaFile> MediaFiles => _listingFileService.MediaFiles;
    public ReactiveCommand StopServerCommand = new ReactiveCommand();

    public ServerPageViewModel(IHttpService httpService, IListingFileService listingFileService, IPageService pageService, IDBService dBService) {
        _httpService = httpService;
        _listingFileService = listingFileService;
        _pageService = pageService;
        _dBService = dBService;
        StopServerCommand.Subscribe(StopServer);
    }

    private void StopServer() {
        _pageService.GoBack();
    }

    public void UpdateMediaFile(IMediaFile file) {
        _dBService.EditMediaFileStore(store => {
            store.Update(file);
            return true;
        });
    }
}
