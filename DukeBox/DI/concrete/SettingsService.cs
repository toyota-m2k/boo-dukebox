using DukeBox.DI.contract;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DukeBox.DI.concrete; 
public class SettingsService : ISettingsService {
    private readonly IAppConfigService AppConfigService;
    private readonly IDBService DBService;

    public SettingsService(IDBService dBService, IAppConfigService appConfigService) {
        DBService = dBService;  
        AppConfigService = appConfigService;
    }

    static readonly string KEY_SCAN_INTERVAL = "ScanInterval";
    static readonly TimeSpan DEFAULT_SCAN_INTERVAL = TimeSpan.FromMinutes(1);
    public TimeSpan ScanInterval { 
        get => TimeSpan.TryParse(DBService.KVStore[KEY_SCAN_INTERVAL], out var interval) ? interval : DEFAULT_SCAN_INTERVAL;
        set => DBService.KVStore[KEY_SCAN_INTERVAL] = value.ToString();
    }

    static readonly string KEY_USE_FFMPEG = "UseFFMpeg";

    public bool UseFFMpeg { 
        get => DBService.KVStore[KEY_USE_FFMPEG] == "true"; 
        set => DBService.KVStore[KEY_USE_FFMPEG] = value ? "true" : "false";
    }
    static readonly string KEY_FFMPEG_PATH = "FFMpegPath";
    public string FFMpegPath { 
        get => DBService.KVStore[KEY_FFMPEG_PATH] ?? string.Empty; 
        set => DBService.KVStore[KEY_FFMPEG_PATH] = value;
    }
    static readonly string KEY_WORKING_PATH = "WorkingPath";
    public string WorkingPath { 
        get => DBService.KVStore[KEY_WORKING_PATH] ?? string.Empty; 
        set => DBService.KVStore[KEY_WORKING_PATH] = value;
    }
    static readonly string KEY_CONVERTED_SOURCE_PATH = "ConvertedSourcePath";
    public string ConvertedSourcePath { 
        get => DBService.KVStore[KEY_CONVERTED_SOURCE_PATH] ?? string.Empty; 
        set => DBService.KVStore[KEY_CONVERTED_SOURCE_PATH] = value;
    }

    static readonly string KEY_ENABLE_HTTP = "EnableHttp";
    public bool EnableHttp { 
        get => DBService.KVStore[KEY_ENABLE_HTTP] == "true"; 
        set => DBService.KVStore[KEY_ENABLE_HTTP] = value ? "true" : "false";
    }

    static readonly string KEY_SERVER_NAME = "ServerName";
    static readonly string DEFAULT_SERVER_NAME = "DukeBox";
    public string ServerName {
        get => DBService.KVStore[KEY_SERVER_NAME] ?? DEFAULT_SERVER_NAME;
        set => DBService.KVStore[KEY_SERVER_NAME] = value;
    }

    public string EnsureServerName {
        get {
            var serverName = this.ServerName;
            if (string.IsNullOrWhiteSpace(serverName)) {
                serverName = AppConfigService.AppName;
            }
            return serverName;
        }
    }

    static readonly string KEY_SERVER_PORT = "ServerPort";
    static readonly int DEFAULT_SERVER_PORT = 3030;
    public int ServerPort { 
        get => int.TryParse(DBService.KVStore[KEY_SERVER_PORT], out var port) ? port : DEFAULT_SERVER_PORT; 
        set => DBService.KVStore[KEY_SERVER_PORT] = value.ToString();
    }

    static readonly string KEY_SSL_SERVER_PORT = "SSLServerPort";
    static readonly int DEFAULT_SSL_SERVER_PORT = 3031;
    public int SSLServerPort {
        get => int.TryParse(DBService.KVStore[KEY_SSL_SERVER_PORT], out var port) ? port : DEFAULT_SSL_SERVER_PORT;
        set => DBService.KVStore[KEY_SSL_SERVER_PORT] = value.ToString();
    }

    static readonly string KEY_ENABLE_SSL = "EnableSSL";
    public bool EnableSSL { 
        get => DBService.KVStore[KEY_ENABLE_SSL] == "true"; 
        set => DBService.KVStore[KEY_ENABLE_SSL] = value ? "true" : "false";
    }

    static readonly string KEY_ENABLE_MDNS_ADVERTISING = "EnableMDNSAdvertising";
    public bool EnableMDNSAdvertising {
        get => DBService.KVStore[KEY_ENABLE_MDNS_ADVERTISING] == "true";
        set => DBService.KVStore[KEY_ENABLE_MDNS_ADVERTISING] = value ? "true" : "false";
    }

    static readonly string KEY_PFX_FILE_PATH = "PFXFilePath";
    public string PFXFilePath {
        get => DBService.KVStore[KEY_PFX_FILE_PATH] ?? string.Empty;
        set => DBService.KVStore[KEY_PFX_FILE_PATH] = value;
    }

    static readonly string KEY_PFX_PASSWORD = "PFXPassword";
    public string PFXPassword {
        get => DBService.KVStore[KEY_PFX_PASSWORD] ?? string.Empty;
        set => DBService.KVStore[KEY_PFX_PASSWORD] = value;
    }

    public void Save() {
        DBService.KVStore.SaveChanges();
    }
}
