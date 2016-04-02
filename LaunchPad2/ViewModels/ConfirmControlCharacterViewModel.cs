namespace LaunchPad2.ViewModels
{
    public class ConfirmControlCharacterViewModel : ViewModelBase
    {
        public ConfirmControlCharacterViewModel(char value)
        {
            Value = value;
        }

        public char Value { get; }

        private bool _isPressed;

        public bool IsPressed
        {
            get { return _isPressed; }
            set
            {
                if (value == _isPressed) return;
                _isPressed = value;
                OnPropertyChanged();
            }
        }
    }
}
