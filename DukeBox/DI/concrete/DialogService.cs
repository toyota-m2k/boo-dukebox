using DukeBox.DI.contract;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace DukeBox.DI.concrete; 

class DialogBuilder : IDialogBuilder {
    enum DialogType {
        Confirm, // OK
        OkCancel, // OK Cancel
        YesNo, // Yes No
        Custom, // Custom buttons
    }

    DialogType _dialogType = DialogType.Custom;

    ContentDialog Dialog = new ContentDialog();
    public DialogBuilder(XamlRoot xamlRoot) {
        Dialog.XamlRoot = xamlRoot;
    }

    public IDialogBuilder Title(string title) {
        Dialog.Title = title;
        return this;
    }

    public IDialogBuilder Content(object content) {
        Dialog.Content = content;
        return this;
    }

    public IDialogBuilder PrimaryButton(string text, bool isDefault, Action? onClick) {
        Dialog.PrimaryButtonText = text;
        if (isDefault) {
            Dialog.DefaultButton = ContentDialogButton.Primary;
        }
        if (onClick != null) {
            Dialog.PrimaryButtonClick += (s, e) => onClick();
        }
        return this;
    }

    public IDialogBuilder SecondaryButton(string text, bool isDefault, Action? onClick) {
        Dialog.SecondaryButtonText = text;
        if (isDefault) {
            Dialog.DefaultButton = ContentDialogButton.Secondary;
        }
        if (onClick != null) {
            Dialog.SecondaryButtonClick += (s, e) => onClick();
        }
        return this;
    }

    public IDialogBuilder CloseButton(string text, bool isDefault, Action? onClick) {
        Dialog.CloseButtonText = text;
        if (isDefault) {
            Dialog.DefaultButton = ContentDialogButton.Close;
        }
        if (onClick != null) {
            Dialog.CloseButtonClick += (s, e) => onClick();
        }
        return this;
    }

    public IDialogBuilder Customize(Action<ContentDialog> customize) {
        customize(Dialog);
        return this;
    }

    public IDialogBuilder OnClose(Action? onClose) {
        if (onClose != null) {
            Dialog.Closed += (s, e) => onClose();
        }
        return this;
    }


    public async Task<ContentDialogResult> ShowAsync() {
        return await Dialog.ShowAsync();
    }

    public async Task ShowConfirm() {
        CloseButton("OK", true, null);
        await ShowAsync();
    }

    public async Task<bool> ShowOkCancel(bool okAsDefault = true) {
        PrimaryButton("OK", okAsDefault, null);
        CloseButton("Cancel", !okAsDefault, null);
        var result = await ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<bool> ShowYesNo(bool yesAsDefault = true) {
        PrimaryButton("Yes", yesAsDefault, null);
        CloseButton("No", !yesAsDefault, null);
        var result = await ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

public class DialogService : IDialogService {
    private IPageService _pageService;
    private XamlRoot _xamlRoot => _pageService.XamlRoot;

    public DialogService(IPageService pageService) {
        _pageService = pageService;
    }

    public IDialogBuilder Builder() {
        return new DialogBuilder(_xamlRoot);
    }

    public async Task ShowMessage(string title, string message) {
        var textBox = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
        await Builder()
            .Title(title)
            .Content(textBox)
            .ShowConfirm();
    }

    public async Task<bool> ShowOkCancel(string title, string message, bool okAsDefault = true) {
        var textBox = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
        return await Builder()
            .Title(title)
            .Content(textBox)
            .ShowOkCancel(okAsDefault);
    }

    public async Task<bool> ShowYesNo(string title, string message, bool yesAsDefault = true) {
        var textBox = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
        return await Builder()
            .Title(title)
            .Content(textBox)
            .ShowYesNo(yesAsDefault);
    }

    public async Task<string?> ShowInputText(string title, string message="", string defaultText = "", string placeholder = "", bool acceptEmptyString=false) {
        var textBox = new TextBox { Text = defaultText, AcceptsReturn = false, PlaceholderText = placeholder };
        object content = textBox;
        if (!string.IsNullOrEmpty(message)) {
            content = new StackPanel {
                Children = {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    textBox
                }
            };
        }
        var result = await Builder()
            .Title(title)
            .Content(content)
            .PrimaryButton("OK", true, null)
            .CloseButton("Cancel", false, null)
            .Customize(dialog => {
                if (!acceptEmptyString) {
                    // 空文字では OK を押せないようにする
                    dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
                    textBox.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
                }
            })
            .ShowAsync();
        if (result == ContentDialogResult.Primary) {
            return textBox.Text;
        }
        return null;
    }
}
