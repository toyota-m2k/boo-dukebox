using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.DI.contract; 
public interface ISettingsService {
    // Media Sources

    // FFMpeg
    bool UseFFMpeg { get; set; }
    string FFMpegPath { get; set; }
    string WorkingPath { get; set; }

    // Converted Source
    string ConvertedSourcePath { get; set; }

    // Server
    bool EnableHttp { get; set; }
    string ServerName { get; set; }
    int ServerPort { get; set; }
    int SSLServerPort { get; set; }
    bool EnableSSL { get; set; }
    bool EnableMDNSAdvertising { get; set; }
    string PFXFilePath { get; set; }
    string PFXPassword { get; set; }

}
