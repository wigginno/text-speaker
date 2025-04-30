// TextSpeaker/Views/SettingsWindow.axaml.cs
using Avalonia.Controls;
using TextSpeaker.ViewModels;
using System; // Added for EventArgs

namespace TextSpeaker.Views
{
    // Inherit directly from Window
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel? _previousVm;

        public SettingsWindow()
        {
            InitializeComponent();

            // Simplified: ViewModel interaction handled by bindings and commands
            // Hook up CloseRequested event if ViewModel signals it
             this.DataContextChanged += (sender, e) => // EventArgs 'e' has no OldValue/NewValue
             {
                 // Unsubscribe from the previous ViewModel if it exists
                 if (_previousVm != null)
                 {
                     _previousVm.CloseRequested -= ViewModel_CloseRequested;
                 }

                 // Get the new ViewModel from the DataContext property
                 if (this.DataContext is SettingsViewModel newVm)
                 {
                     newVm.CloseRequested += ViewModel_CloseRequested;
                     _previousVm = newVm; // Store reference to the new VM
                 }
                 else
                 {
                     _previousVm = null; // Clear reference if DataContext is not the expected VM
                 }
             };
        }

         private void ViewModel_CloseRequested(object? sender, EventArgs e)
         {
             this.Close();
         }

        // Optional: Override OnClosed to clean up event handler if needed,
        // though DataContextChanged might be sufficient if VM lifetime matches window.
        // protected override void OnClosed(EventArgs e)
        // {
        //     if (DataContext is SettingsViewModel vm)
        //     {
        //         vm.CloseRequested -= ViewModel_CloseRequested;
        //     }
        //     base.OnClosed(e);
        // }
    }
}
