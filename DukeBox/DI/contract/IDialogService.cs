using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace DukeBox.DI.contract;

public interface IDialogBuilder {
    IDialogBuilder Content(object content);
    IDialogBuilder Title(string title);
    IDialogBuilder PrimaryButton(string text, bool isDefault, Action? onClick);
    IDialogBuilder SecondaryButton(string text, bool isDefault, Action? onClick);
    IDialogBuilder CloseButton(string text, bool isDefault, Action? onClick);
    IDialogBuilder OnClose(Action? onClose);
    IDialogBuilder Customize(Action<ContentDialog> customize);

    // OKボタンだけのダイアログを表示する
    // PrimaryButton/SecondaryButton/CloseButtonは無視
    Task ShowConfirm();

    // OK/Cancelボタンのダイアログを表示する
    // PrimaryButton/SecondaryButton/CloseButtonは無視
    Task<bool> ShowOkCancel(bool okAsDefault = true);

    // Yes/Noボタンのダイアログを表示する
    // PrimaryButton/SecondaryButton/CloseButtonは無視
    Task<bool> ShowYesNo(bool okAsDefault = true);
    
    // カスタムダイアログを表示する
    Task<ContentDialogResult> ShowAsync();
}

public interface IDialogService {
    IDialogBuilder Builder();

    // MessageBox
    Task ShowMessage(string title, string message);
    Task<bool> ShowOkCancel(string title, string message, bool okAsDefault = true);
    Task<bool> ShowYesNo(string title, string message, bool yesAsDefault = true);

    Task<string?> ShowInputText(string title, string message="", string defaultText = "", string placeholder = "", bool acceptEmptyString=false);

}
