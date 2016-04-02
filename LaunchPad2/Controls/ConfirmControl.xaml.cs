using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Controls
{
    public partial class ConfirmControl : UserControl
    {
        public static readonly DependencyProperty CharactersProperty = DependencyProperty.Register(
        "Characters", typeof(ObservableCollection<ConfirmControlCharacterViewModel>), typeof(ConfirmControl),
        new PropertyMetadata(default(ObservableCollection<ConfirmControlCharacterViewModel>)));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(ConfirmControl),
            new PropertyMetadata(default(string), TextPropertyChangedCallback));

        public static readonly DependencyProperty IsConfirmedProperty = DependencyProperty.Register(
            "IsConfirmed", typeof (bool), typeof (ConfirmControl), new PropertyMetadata(default(bool), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (ConfirmControl) dependencyObject;
            var confirmed = (bool)dependencyPropertyChangedEventArgs.NewValue;

            if(!confirmed)
                control.Reset();
        }

        public bool IsConfirmed
        {
            get { return (bool) GetValue(IsConfirmedProperty); }
            set { SetValue(IsConfirmedProperty, value); }
        }

        public ConfirmControl()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                Reset();
                Focus();
            };
        }

        private void Reset()
        {
            IsConfirmed = false;
            foreach (var c in Characters)
                c.IsPressed = false;
        }

        public ObservableCollection<ConfirmControlCharacterViewModel> Characters
        {
            get { return (ObservableCollection<ConfirmControlCharacterViewModel>)GetValue(CharactersProperty); }
            set { SetValue(CharactersProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void TextPropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (ConfirmControl)dependencyObject;
            control.TextPropertyChangedCallback(dependencyPropertyChangedEventArgs);
        }

        private void TextPropertyChangedCallback(DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var text = (string)dependencyPropertyChangedEventArgs.NewValue;
            var viewModels = text.Select(c => new ConfirmControlCharacterViewModel(c));
            Characters = new ObservableCollection<ConfirmControlCharacterViewModel>(viewModels.ToList());
        }

        private void ConfirmControlOnKeyDown(object sender, KeyEventArgs e)
        {
            var nextChar = Characters.FirstOrDefault(c => !c.IsPressed);

            if (nextChar == null)
                return;

            var keyChar = e.Key.ToString().First();
            if (char.ToLower(nextChar.Value) == char.ToLower(keyChar))
                nextChar.IsPressed = true;
            else
            {
                // TODO beep or something
            }

            if (Characters.All(c => c.IsPressed))
            {
                IsConfirmed = true;
            }

            e.Handled = true;
        }
    }
}
