using System.Windows.Input;

public class RelayCommand : ICommand {
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute; // 使 canExecute 可选

    public event EventHandler? CanExecuteChanged; // C# 6 null-conditional operator

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // 如果未提供 canExecute 函数，则始终认为可以执行
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

    public void Execute(object? parameter) => _execute();

    // 公开方法以允许 ViewModel 手动触发 CanExecuteChanged
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}