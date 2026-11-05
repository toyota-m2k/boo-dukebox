using DukeBox.DI.contract;
using DukeBox.model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox.view; 

public sealed partial class ItemPropertyDialog : ContentDialog {
    public class ItemPropertyDialogViewModel {
        public ReactiveProperty<string> Name { get; } = new("");
        public ReactiveProperty<int> Rating { get; } = new(3);
        public ReactiveProperty<int> Mark { get; } = new(0);

        public void InitByItem(IMediaFile file) {
            Name.Value = file.Name;
            Rating.Value = file.ComplementalRating;
            Mark.Value = file.Mark;
        }

        public void UpdateItem(IMediaFile file, IDBService dbService) {
            var name = !string.IsNullOrEmpty(Name.Value) && Name.Value != file.Name ? Name.Value : null;
            var rating = Rating.Value != file.ComplementalRating ? Rating.Value : (int?)null;
            var mark = Mark.Value != file.Mark ? Mark.Value : (int?)null;
            if (name == null && rating == null && mark == null) {
                return;
            }

            dbService.EditMediaFileStore(store => {
                store.Update(file, name: name, rating: rating, mark: mark);
                return true;
            });
        }
    }

    public ItemPropertyDialogViewModel ViewModel { get; } = new();

    // Rating(1..5) <--> RadioButtons.SelectedIndex(0..4)
    public static int RatingToIndex(int rating) => rating - 1;
    public void SetRatingIndex(int index) {
        if (index >= 0) {
            ViewModel.Rating.Value = index + 1;
        }
    }

    public ItemPropertyDialog(IMediaFile file) {
        ViewModel.InitByItem(file);
        InitializeComponent();
    }

    public static async Task EditItemProps(IDialogService dlgService, IDBService dbService, IMediaFile file) {
        var dlg = new ItemPropertyDialog(file);
        await dlgService.Builder(dlg)
            .Title("Property")
            .CloseButton("Cancel", isDefault: false, onClick: null)
            .PrimaryButton("OK", isDefault: true, onClick: () => {
                dlg.ViewModel.UpdateItem(file, dbService);
            })
            .ShowAsync();
    }
}
