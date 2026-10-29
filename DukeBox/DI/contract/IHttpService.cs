using Reactive.Bindings;
using System.Threading.Tasks;

namespace DukeBox.DI.contract; 
public interface IHttpService {
    IReadOnlyReactiveProperty<bool> Running { get; }
    bool Start();
    Task StopAsync();
}
