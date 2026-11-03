using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.view.viewmodel; 
internal class MainWindowViewModel {
    public ReactivePropertySlim<string> WindowTitle { get; } = new ReactivePropertySlim<string>("DukeBox");
}
