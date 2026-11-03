using DukeBox.DB;
using DukeBox.DI.contract;
using DukeBox.model;
using io.github.toyota32k.BooLib.Server.http;
using io.github.toyota32k.BooLib.Server.http.model;
using io.github.toyota32k.BooLib.Server.http.response;
using io.github.toyota32k.BooLib.Server.mdns;
using io.github.toyota32k.BooLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reactive.Bindings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DukeBox.DI.concrete;

public class HttpService : IHttpService {
    private IDBService DBService { get; }
    private ISettingsService SettingsService { get; }
    private IAppConfigService AppConfigService { get; }
    private UtLog Logger = new UtLog("HTTP");
    private Regex RegRange = new Regex(@"bytes=(?<start>\d+)(?:-(?<end>\d+))?");

    #region Routing

    private string DataPathOf(IMediaFile entry) {
        if (entry.Converted) {
            var path = DBService.GetConvertedFilePath(entry);
            if (File.Exists(path)) {
                return path;
            }
        }
        return entry.Path;
    }

    // Shared Routed Handler
    private IHttpResponse RequestItem(HttpRequest request, string? type) {
        var p = QueryParser.Parse(request.Url);
        var id = Convert.ToInt64(p.GetValue("id", "0"));
        var oid = p.GetValue("o");
        var cid = p.GetValue("c");

        IMediaFile? entry = DBService.MediaFileStore.GetById(id);
        if (entry == null) {
            Logger.Warn($"[{request.Id}] no entry ({request.Url})");
            return HttpErrorResponse.NotFound(request);
        }
        if (type != null && type != entry.BooMediaType) {
            // type指定があるが、実際のメディアタイプと異なる場合は、Not Found
            Logger.Warn($"[{request.Id}] media type mismatch ({request.Url})");
            return HttpErrorResponse.NotFound(request);
        }

        if (entry.BooMediaType == "p") {
            // 画像ならNoRanged なレスポンスを返す
            Logger.Debug($"[{request.Id}] No-Ranged Request (photo).");
            var stream = new FileStream(DataPathOf(entry), FileMode.Open, FileAccess.Read, FileShare.Read);
            return StreamingHttpResponse.CreateForNoRanged(request, entry.ContentType, stream, entry.Size, () => stream.Dispose());
        }

        // 動画の場合は、Range指定に対応する。
        if (!request.Headers.TryGetValue("range", out var range)) {
            //Source?.StandardOutput($"BooServer: cmd=video({id})");
            Logger.Debug($"[{request.Id}] No-Ranged Request.");
            var stream = new FileStream(DataPathOf(entry), FileMode.Open, FileAccess.Read, FileShare.Read);
            return StreamingHttpResponse.CreateForRangedInitial(request, "video/mp4", stream, entry.Size, () => stream.Dispose());
        }
        else {
            Logger.Debug($"[{request.Id}] Ranged Request: {range}");
            var match = RegRange.Match(range);
            var ms = match.Groups["start"];
            var me = match.Groups["end"];
            var start = ms.Success ? Convert.ToInt64(ms.Value) : 0L;
            var end = me.Success ? Convert.ToInt64(me.Value) : 0L;
            if (start < 0 || end < 0 || (end > 0 && start > end)) {
                Logger.Error($"[{request.Id}] Hah? Start={start} End={end}");
            }
            //Logger.Debug($"Ranged Request. {start} - {end}");
            var stream = new FileStream(DataPathOf(entry), FileMode.Open, FileAccess.Read, FileShare.Read);
            return StreamingHttpResponse.CreateForRanged(request, "video/mp4", stream, start, end, entry.Size, () => stream.Dispose());
        }
    }

    private Dictionary<string, object> MediaFile2Dictionary(IMediaFile entry) {
        return new Dictionary<string, object>() {
            { "id", entry.Id },
            { "name", entry.Name },
            { "size", entry.Size },
            { "type", entry.Type },
            { "media", entry.BooMediaType },
            { "date", entry.CreationDate },
            { "rating", entry.Rating },
            { "mark", entry.Mark },
            { "label", entry.Label },
            { "categoryName", entry.CategoryName },
        };
    }

    private List<Route> Routes() {

        return new List<Route> {
            #region Common
            
            // nop 疎通確認用
            Route.get(
                name: "nop",
                regex: "/nop",
                process: (HttpRequest request) => {
                    return new TextHttpResponse(request, HttpStatusCode.Ok, $"DukeBox server is running.");
                }),
            // capability
            Route.get(
                name: "capability",
                regex:@"/capability",
                process: (HttpRequest request) => {
                    var cap = new Dictionary<string,object> {
                        {"cmd", "capability"},
                        {"serverName", "DukeBox"},
                        {"version", 2},
                        {"root", "/" },
                        {"category", true},
                        {"rating", false},
                        {"mark", false},
                        {"chapter", true},
                        {"reputation", 0 },             // reputation (category/mark/rating) コマンド対応 1:RO /2:RW
                        {"diff", false},                // date以降の更新チェック(check)、差分リスト取得に対応
                        {"sync", false },               // 端末間同期
                        {"acceptRequest", false },        // register command をサポートする
                        {"hasView", true },              // current get/set をサポートする
                        {"authentication", false },
                        {"types", "vap" },                // v: video, a: audio, p: photo
                    };
                    return TextHttpResponse.FromJson(request, cap);
                }),
            // categories：全カテゴリリストの要求
            Route.get(
                name: "categories",
                regex: @"/categories",
                process: (request) => {
                    var list = DBService.CategoryStore.List().Select(it=> new Dictionary<string, object>(){
                        {"label", it.Name},
                        {"color", $"{it.Color}"},
                        {"sort", $"{it.SortOrder}"},
                        {"svg", it.IconPath},
                    }).ToList();
                    var dic = new Dictionary<string, object>(){
                        { "cmd", "categories"},
                        {"categories", list},
                    };
                    return TextHttpResponse.FromJson(request, dic);
                }),
            
            #endregion

            #region Items List
            
            // list?auth=<auth-token>&sync=<true|false>&type=<all|video|photo>&f=<vp>&o=<ownerId>&s=<listMode>
            Route.get(
                name: "list",
                regex: @"/list(?:\?.*)?",
                process: (HttpRequest request) => {
                    var p = QueryParser.Parse(request.Url);
                    var category = p.GetValue("c");
                    var rating = Convert.ToInt32(p.GetValue("r")??"0");
                    var marks = (p.GetValue("m")??"0").Split('.').Select((v)=>Convert.ToInt32(v)).ToList();
                    var sourceType = (MediaListMode)Convert.ToInt32(p.GetValue("s")??"0");
                    var search = p.GetValue("t");
                    var types = p.GetValue("f")?.ToLower();
                    var video = types?.Contains("v") ?? true;
                    var photo = types?.Contains("p") ?? true;
                    var audio = types?.Contains("a") ?? false;
                    var date = Convert.ToInt64(p.GetValue("d")??"0");
                    var list = _mediaListSource.GetMediaList(sourceType).Where((it) => {
                        // filter
                        if(it.Deleted) return false;
                        if (!string.IsNullOrWhiteSpace(types)) {
                            switch(it.Type) {
                                case "mp4":
                                    if (!video) return false;
                                    break;
                                case "jpg":
                                case "jpeg":
                                case "gif":
                                case "png":
                                    if (!photo) return false;
                                    break;
                                case "mp3":
                                case "m4a":
                                    if (!audio) return false;
                                    break;
                                default:
                                    return false;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(category)) {
                            if (!it.Category.Equals(category)) {
                                return false;
                            }
                        }
                        if (rating!=0) {
                            if (it.Rating>=rating || (it.Rating==0 && rating<4)) {
                                return false;
                            }
                        }
                        if (marks.Count>0) {
                            if (!marks.Contains(it.Mark)) {
                                return false;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(search)) {
                            if (!it.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                            &&  !it.Label.Contains(search, StringComparison.OrdinalIgnoreCase)
                            ) {
                                return false;
                            }
                        }
                        return true;
                    }).Select((it) => {
                        return MediaFile2Dictionary(it);
                    }).ToList();

                    var dic = new Dictionary<string,object> {
                        {"cmd", "list" },
                        {"date", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                        {"list", list },
                    };
                    return TextHttpResponse.FromJson(request, dic);
                }),

            // CHECK: 前回のプレイリストから変更されたかどうかのチェック
            Route.get(
                name:  "check update",
                regex: @"/check(?:\?\w+)?",
                process: (HttpRequest request) => {
                    //Source?.StandardOutput($"BooServer: cmd=check");
                    var date = Convert.ToInt64(QueryParser.Parse(request.Url).GetValue("date"));
                    var list = DBService.MediaFileStore.Enumerable().Where((it) => {
                        if(it.Deleted) return false;
                        return it.RegisteredDate> date;
                    }).Select((it) => {
                        return MediaFile2Dictionary(it);
                    }).ToList();
                    var changed = list.Count > 0 ? "1" : "0";

                    var dic = new Dictionary<string, object>() {
                        {"cmd", "check"},
                        {"update", changed},
                        {"list", list},
                    };
                    return TextHttpResponse.FromJson(request, dic);
                }),

            #endregion

            #region Retrieve Item

            // video アイテムを取得
            Route.get(
                name: "video",
                regex: @"/video\?\w+",
                process: (HttpRequest request) => {
                    return RequestItem(request, "v");
                }),
            
            // photo アイテムを取得
            Route.get(
                name: "photo",
                regex: @"/photo\?\w+",
                process: (HttpRequest request) => {
                    return RequestItem(request, "p");
                }),
            
            // タイプを指定しないでアイテムを取得する。
            Route.get(
                name: "item",
                regex: @"/item\?\w+",
                process: (HttpRequest request) => {
                    return RequestItem(request, null);
                }),
            
            #endregion

            #region Item Attributes
            
            // chapter  アイテムのチャプターリストを取得する。
            Route.get(
                name: "chapters",
                regex: @"/chapter\?\w+",
                process: (request) => {
                    var id = QueryParser.Parse(request.Url)["id"];
                    var entry = DBService.MediaFileStore.GetById(Convert.ToInt64(id));
                    if(entry==null) {
                        return HttpErrorResponse.NotFound(request);
                    }
                    var dic = new Dictionary<string, object>(){
                        { "cmd", "chapter"},
                        { "id", id },
                    };
                    if (entry.Chapters!=null) {
                        var list = JsonConvert.DeserializeObject(entry.Chapters);
                        if(list != null) {
                           dic["chapters"] = list;
                        }
                    }
                    return TextHttpResponse.FromJson(request, dic);
                }),

            #endregion

            #region Reputation

            Route.get(
                name: "ytPlayer Get Reputation",
                regex: @"/reputation\?\w+",
                process: (request) => {
                    var p = QueryParser.Parse(request.Url);
                    var id = Convert.ToInt64(p.GetValue("id", "0"));
                    var entry = DBService.MediaFileStore.GetById(id);
                    if(entry==null) {
                        return HttpErrorResponse.NotFound(request);
                    }
                    var dic = new Dictionary<string, object>(){
                        { "cmd", "reputation"},
                        { "id", id },
                        { "category", entry.CategoryName },
                        { "rating", entry.Rating },
                        { "mark", entry.Mark },
                    };
                    return TextHttpResponse.FromJson(request, dic);
                }),

            Route.put(
                name: "ytPlayer Put Reputation",
                regex: @"/reputation",
                process: (request) => {
                    var content = request.Content?.TextContent;
                    if (string.IsNullOrWhiteSpace(content)) {
                        return HttpErrorResponse.BadRequest(request);
                    }
                    var json = JsonHelper.TryParse(content);
                    if (json == null) {
                        return HttpErrorResponse.BadRequest(request);
                    }
                    var id = json.GetString("id");
                    var entry = DBService.MediaFileStore.GetById(Convert.ToInt64(id));
                    if (entry==null) {
                        return HttpErrorResponse.NotFound(request);
                    }
                    var iRating = json.GetInt("rating",-1);
                    var iMark = json.GetInt("mark", -1);
                    int? rating = iRating>=0 ? iRating : null;
                    int? mark = iMark>=0 ? iMark : null;

                    DBService.EditMediaFileStore(store => {
                        store.Update(entry, rating:rating, mark:mark);
                        return true;
                    });
                    return TextHttpResponse.FromJson(request, new Dictionary<string, object>() {
                        { "cmd", "reputation" },
                        { "id", $"{id}" },
                    });
                }),
            #endregion

            #region Current Item on UI

            // current: カレントアイテムのget/set
            Route.get(
                        name: "Current Item",
                        regex: @"/current",
                        process: request => {
                            //Source?.StandardOutput("BooServer: cmd=current (get)");
                            Logger.Debug("Current (Get)");
                            // ToDo
                            var id = 0; // Source.CurrentId ?? "";
                            var dic = new Dictionary<string, object>() {
                                { "cmd", "current"},
                                { "id", $"{id}" },
                            };
                            return TextHttpResponse.FromJson(request, dic);
                        }
                    ),

                    Route.put(
                        name: "set Current Item",
                        regex: @"/current",
                        process: request => {
                            //Source?.StandardOutput("BooServer: cmd=current (put)");
                            Logger.Debug("Current (Put)");
                            var json = JsonHelper.TryParse(request.Content?.TextContent);
                            if (json == null) {
                                return HttpErrorResponse.BadRequest(request);
                            }
                            var id = json.GetString("id");
                            // ToDo
                            // Source.CurrentId = id;
                            return TextHttpResponse.FromJson(request, new Dictionary<string, object>() {
                                { "cmd", "current" },
                                { "id", $"{id}" },
                            });
                        }
                    ),
            #endregion
        };
    }
    #endregion

    #region IHttpService

    //private readonly ReactivePropertySlim<bool> _running = new (false);
    private readonly List<HttpServer> _servers = new();
    private readonly BehaviorSubject<IReadOnlyList<HttpServer>> _serverList = new([]);
    public IReadOnlyReactiveProperty<bool> Running { get; }
    private MdnsAdvertiser? _mdns = null;


    /// <summary>
    /// 旧シグネチャ。引数の <paramref name="port"/> は HTTP リスナーのポートとして扱う。
    /// HTTPS / mDNS 関連は <see cref="IUserSettingsService"/> から動的に読み出す。
    /// </summary>
    public bool Start() {
        if (Running.Value) return false;

        var routes = Routes();
        bool startHttp = SettingsService.EnableHttp;
        bool startHttps = SettingsService.EnableSSL;

        var anyRunning = false;
        if (startHttp) {
            anyRunning = StartOne(routes, SettingsService.ServerPort, certificate: null, label: "HTTP");
        }

        if (startHttps) {
            X509Certificate2? cert = null;
            try {
                if (!string.IsNullOrEmpty(SettingsService.PFXFilePath)) {
                    // UserKeySet | PersistKeySet:
                    //   - SslStream (= Windows SChannel SSP) は CAPI ベースの鍵コンテナを要求するため、
                    //     EphemeralKeySet (CNG メモリ鍵) は "the platform does not support ephemeral keys"
                    //     で蹴られる。
                    //   - 一方 MachineKeySet は MSIX サンドボックスで Access denied になる。
                    //   - その中間として CurrentUser スコープの CAPI 鍵コンテナ
                    //     (%APPDATA%\Microsoft\Crypto\RSA\<sid>\) に書き込む UserKeySet が
                    //     管理者権限なしでも動き、SChannel にも受け入れられる。
                    //   - PersistKeySet は SslStream のハンドシェイク中に GC で鍵が消える事故を避けるため。
                    cert = X509CertificateLoader.LoadPkcs12FromFile(
                        SettingsService.PFXFilePath!,
                        SettingsService.PFXPassword,
                        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
                }
            }
            catch (Exception e) {
                Logger.Error(e, $"HTTPS: cannot load PFX ({SettingsService.PFXFilePath})");
            }
            if (cert != null) {
                if (StartOne(routes, SettingsService.SSLServerPort, certificate: cert, label: "HTTPS")) {
                    anyRunning = true;
                }
                else {
                    cert.Dispose();
                }
            }
        }

        if (anyRunning) {
            StartMdns();
        }
        return anyRunning;
    }

    private bool StartOne(List<Route> routes, int port, X509Certificate2? certificate, string label) {
        var server = new HttpServer(routes, Logger, certificate);
        if (server.Start(port)) {
            _servers.Add(server);
            _serverList.OnNext(_servers.ToList());
            Logger.Info($"{label} listener started on port {port}");
            return true;
        }
        Logger.Error($"{label} listener failed to start on port {port}");
        return false;
    }

    private void StartMdns() {
        if (!SettingsService.EnableMDNSAdvertising) return;
        int port = SettingsService.EnableSSL ? SettingsService.SSLServerPort : SettingsService.ServerPort;
        bool isHttps = SettingsService.EnableSSL;
        string? fp = null;
        if (isHttps && !string.IsNullOrEmpty(SettingsService.PFXFilePath)) {
            try {
                // fingerprint 計算のみなので秘密鍵は不要だが、X509Certificate2 のコンストラクタは
                // PFX ロード時に常に鍵もパースする。EphemeralKeySet で OS ストアを汚染しない。
                using var cert = X509CertificateLoader.LoadPkcs12FromFile(
                    SettingsService.PFXFilePath!, SettingsService.PFXPassword,
                    X509KeyStorageFlags.EphemeralKeySet);
                fp = CertificateGenerator.ComputeSha256Fingerprint(cert);
            }
            catch (Exception e) {
                Logger.Error(e, "mDNS: failed to compute fingerprint");
            }
        }

        _mdns = new MdnsAdvertiser();
        try {
            var serverName = SettingsService.ServerName;
            if (string.IsNullOrWhiteSpace(serverName)) {
                serverName = AppConfigService.AppName;
            }
            _mdns.Start(serverName, port, isHttps, fp);
            Logger.Info($"mDNS advertised: _booapi._tcp / {serverName} (port={port}, https={isHttps})");
        }
        catch (Exception e) {
            Logger.Error(e, "mDNS advertise failed");
            _ = _mdns.StopAsync();
            _mdns = null;
        }
    }

    public async Task StopAsync() {
        if (_mdns != null) {
            try { 
                await _mdns.StopAsync(); 
            } catch (Exception e) { 
                Logger.Error(e, "mDNS stop"); 
            }
            _mdns = null;
        }
        foreach (var s in _servers) {
            try { 
                await s.StopAsync(); 
            } catch (Exception e) { 
                Logger.Error(e, "Server stop"); 
            }
        }

        try {
            // タイムアウト付きで、Running が false になるのを待つ。
            await Running.FirstAsync(it => !it)
                         .Timeout(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException) {
            Logger.Warn("Some servers did not stop within 5 seconds.");
        }

        // try/catch を使わない書き方も魅力的だけど、素直にtry/catch した方がわかりやすい気がする。
        //var stopped = await Running.FirstAsync(it => !it)
        //    .Select(_ => true)  // Running が false になったら true を返す
        //    .Timeout(TimeSpan.FromSeconds(5), Observable.Return(false)) // タイムアウトしたら false を返す
        //    .FirstAsync();
        //if (!stopped) {
        //    Logger.Warn("Some servers did not stop within 5 seconds.");
        //}

        _servers.Clear();
        _serverList.OnNext(_servers.ToList());
    }

    class MediaListSourceWrapper : IMediaListSource {
        private IDBService _dbService;
        public IMediaListSource? ExternalMediaListSource = null;
        public MediaListSourceWrapper(IDBService dbService) {
            _dbService = dbService;
        }
        public IList<IMediaFile> GetMediaList(MediaListMode mode) {
            return ExternalMediaListSource?.GetMediaList(mode) ?? _dbService.MediaFileStore.List();
        }
    }
    private MediaListSourceWrapper _mediaListSource;

    public void SetMediaListSource(IMediaListSource? source) {
        _mediaListSource.ExternalMediaListSource = source;
    }

    public HttpService(IDBService dbService, ISettingsService settingsService, IAppConfigService appConfigService) {
        DBService = dbService;
        SettingsService = settingsService;
        AppConfigService = appConfigService;
        _mediaListSource = new MediaListSourceWrapper(dbService);
        Running = _serverList
            .Select(list => list.Count == 0
                ? Observable.Return(false)  // 空なら false
                : Observable.CombineLatest(list.Select(s => s.Running))
                            .Select(values => values.Any(v => v)))
            .Switch()   // 新しい一覧が流れたら、古い購読を解除して切り替える
            .DistinctUntilChanged()
            .ToReadOnlyReactiveProperty()
            ;
    }


    #endregion


}
