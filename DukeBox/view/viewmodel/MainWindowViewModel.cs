using Reactive.Bindings;

namespace DukeBox.view.viewmodel; 
internal class MainWindowViewModel {
    public ReactivePropertySlim<string> WindowTitle { get; } = new ReactivePropertySlim<string>("DukeBox");
}
