using DukeBox.DI.contract;
using DukeBox.model;
using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox.view;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ServerPage : Page, INavigationAware {
    public ServerPageViewModel ViewModel { get; } = App.GetService<ServerPageViewModel>();

    public ServerPage() {
        InitializeComponent();
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e) {
        categoryFlyout.Hide();
    }

    private void OnCellEditing(object sender, WinUI.TableView.TableViewCellEditEndingEventArgs e) {
    }

    /**
     * 現在の TableView にはバグがある。
     * 1. セルをダブルクリックして、編集状態にする。
     * 2. 編集中のセルの外側（他のセルなど）をクリックして編集を解除しようとする。
     * この操作を行うと、
     *  App.UnhandledException --> ArgumentOutOfRangeException
     * で強制終了する。
     * 
     * バグが修正されない限り回避できそうにないので、
     * OnBeginningEdit() で編集開始をキャンセルして、編集用ダイアログを表示することとした。
     * 将来、バグが改修されたら、OnCellEditedの処理を復活したい。
     */
    private void OnCellEdited(object sender, WinUI.TableView.TableViewCellEditEndedEventArgs e) {
        ViewModel.Logger.Debug($"{e.Cell}/{e.Column.Header}");
        if (e.EditAction == WinUI.TableView.TableViewEditAction.Commit) {
            ViewModel.Logger.Debug("Edit committed");
            if (e.DataItem is model.IMediaFile file) {
                ViewModel.UpdateMediaFile(file);
            }
        }
    }

    private void OnCellDoubleTapped(object sender, WinUI.TableView.TableViewCellDoubleTappedEventArgs e) {
        ViewModel.Logger.Debug($"{e.Cell.Name} - {(e.Item as IMediaFile)?.Name??"null"}");

    }

    private void OnRowDoubleTapped(object sender, WinUI.TableView.TableViewRowDoubleTappedEventArgs e) {
        ViewModel.Logger.Debug($"ROW: {(e.Item as IMediaFile)?.Name ?? "null"}");
    }

    private void OnBeginningEdit(object sender, WinUI.TableView.TableViewBeginningEditEventArgs e) {
        ViewModel.Logger.Debug($"{e.Cell.Name} - {(e.DataItem as IMediaFile)?.Name ?? "null"}");

        e.Cancel = true;
    }

    private IEnumerable<IMediaFile> GetDisplayedList(bool onlySelected) {
        if (onlySelected) {
            return tableView.SelectedItems.OfType<IMediaFile>();
        } else {
            return tableView.Items.OfType<IMediaFile>();
        }
    }

    public void OnPageActivated() {
        ViewModel.GetDisplayedItemsProc = GetDisplayedList;
    }

    public void OnPageLeaving() {
        ViewModel.GetDisplayedItemsProc = null;
    }
}
