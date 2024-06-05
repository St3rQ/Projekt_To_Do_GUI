using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projekt_To_Do
{
    public partial class MainWindow : Window
    {
        // Klasa reprezentująca zadanie
        public class TaskItem
        {
            public int Id { get; set; }
            public string Task { get; set; }
            public string Priority { get; set; }
            public string Category { get; set; }
            public DateTime Deadline { get; set; }
            public bool IsCompleted { get; set; }
        }

        // Kolekcja zadań
        ObservableCollection<TaskItem> tasks = new ObservableCollection<TaskItem>();
        ObservableCollection<TaskItem> filteredTasks = new ObservableCollection<TaskItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadTasksFromFile();
            Closing += Window_Closing;

            // Ustawienie źródła danych dla ListView
            listView.ItemsSource = filteredTasks;
            FilterTasks();
        }

        // Metoda obsługująca dodawanie nowego zadania
        private void AddTask(object sender, RoutedEventArgs e)
        {
            // Sprawdzenie, czy wszystkie wymagane pola są wypełnione
            if (string.IsNullOrWhiteSpace(Title.Text) || Priority.SelectedIndex == 0 || Category.SelectedIndex == 0 || Deadline.SelectedDate == null)
            {
                MessageBox.Show("Please fill in all required fields.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tworzenie nowego zadania na podstawie danych z pól TextBox, ComboBox i DatePicker
            TaskItem newTask = new TaskItem
            {
                Id = tasks.Count + 1,
                Task = Title.Text,
                Priority = ((ComboBoxItem)Priority.SelectedItem).Content.ToString(),
                Category = ((ComboBoxItem)Category.SelectedItem).Content.ToString(),
                Deadline = DateTime.ParseExact(Deadline.SelectedDate.Value.ToString("dd.MM.yyyy"), "dd.MM.yyyy", null), // Zmiana formatu daty
                IsCompleted = false // Domyślnie nowe zadanie nie jest zakończone
            };

            // Dodanie nowego zadania do kolekcji
            tasks.Add(newTask);

            // Wyczyszczenie pól po dodaniu zadania
            ClearFields();

            // Zapisz zmiany do pliku
            SaveTasksToFile();

            // Odświeżenie listy filtrowanych zadań
            FilterTasks();
        }

        private string currentlyEditingField = "";

        // Metoda obsługująca edycję zadania
        private void EditTask(object sender, RoutedEventArgs e)
        {
            // Sprawdzenie, czy wybrano zadanie do edycji
            if (listView.SelectedItem != null)
            {
                // Sprawdzenie, czy wybrano tylko jedno pole do edycji
                if ((Title.Text.Trim() != "" ? 1 : 0) + (Priority.SelectedIndex != 0 ? 1 : 0) + (Category.SelectedIndex != 0 ? 1 : 0) + (Deadline.SelectedDate != null ? 1 : 0) != 1)
                {
                    MessageBox.Show("Please select only one field to edit.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pobranie zaznaczonego zadania
                TaskItem selectedTask = (TaskItem)listView.SelectedItem;

                // Aktualizacja danych z wybranego pola
                if (!string.IsNullOrWhiteSpace(Title.Text))
                {
                    selectedTask.Task = Title.Text;
                    currentlyEditingField = "Task";
                }
                else if (Priority.SelectedIndex != 0)
                {
                    selectedTask.Priority = ((ComboBoxItem)Priority.SelectedItem).Content.ToString();
                    currentlyEditingField = "Priority";
                }
                else if (Category.SelectedIndex != 0)
                {
                    selectedTask.Category = ((ComboBoxItem)Category.SelectedItem).Content.ToString();
                    currentlyEditingField = "Category";
                }
                else if (Deadline.SelectedDate != null)
                {
                    selectedTask.Deadline = DateTime.ParseExact(Deadline.SelectedDate.Value.ToString("dd.MM.yyyy"), "dd.MM.yyyy", null); // Zmiana formatu daty
                    currentlyEditingField = "Deadline";
                }

                // Odświeżenie wyświetlanych danych
                listView.Items.Refresh();

                // Wyczyszczenie pól po edycji zadania
                ClearFields();

                // Zapisz zmiany do pliku
                SaveTasksToFile();
            }
            else
            {
                MessageBox.Show("Select a task to edit.");
            }
        }

        // Metoda obsługująca usuwanie zadania
        private void DeleteTask(object sender, RoutedEventArgs e)
        {
            // Sprawdzenie, czy wybrano zadanie do usunięcia
            if (listView.SelectedItem != null)
            {
                // Usunięcie zaznaczonego zadania z kolekcji
                tasks.Remove((TaskItem)listView.SelectedItem);

                // Aktualizacja numeracji zadań
                UpdateTaskIds();

                // Zapisz zmiany do pliku
                SaveTasksToFile();

                // Odświeżenie listy filtrowanych zadań
                FilterTasks();
            }
            else
            {
                MessageBox.Show("Select a task to delete.");
            }
        }

        private void Click_ClearFields(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        // Metoda czyszcząca pola TextBox, ComboBox i DatePicker
        private void ClearFields()
        {
            Title.Text = "";
            Priority.SelectedIndex = 0; // Ustawienie pierwszego elementu (--- Priority ---) jako domyślnego
            Category.SelectedIndex = 0; // Ustawienie pierwszego elementu (--- Category ---) jako domyślnego
            Deadline.SelectedDate = null;

            FilterTextBox.Text = "";
            FilterPriority.SelectedIndex = 0; // Ustawienie pierwszego elementu (--- Priority ---) jako domyślnego
            FilterCategory.SelectedIndex = 0; // Ustawienie pierwszego elementu (--- Category ---) jako domyślnego
            FilterDeadline.SelectedDate = null;
        }

        // Metoda aktualizująca numerację zadań po usunięciu
        private void UpdateTaskIds()
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Id = i + 1;
            }

            listView.Items.Refresh();
        }

        // Metoda filtrująca zadania
        private void FilterTasks(object sender = null, EventArgs e = null)
        {
            if (FilterTextBox == null || FilterPriority == null || FilterCategory == null || FilterDeadline == null) return;

            string filterText = FilterTextBox.Text?.ToLower();
            string filterPriority = FilterPriority.SelectedIndex > 0 ? ((ComboBoxItem)FilterPriority.SelectedItem).Content.ToString() : null;
            string filterCategory = FilterCategory.SelectedIndex > 0 ? ((ComboBoxItem)FilterCategory.SelectedItem).Content.ToString() : null;
            DateTime? filterDeadline = FilterDeadline.SelectedDate;

            var filtered = tasks.Where(task =>
                (string.IsNullOrWhiteSpace(filterText) || task.Task.ToLower().Contains(filterText)) &&
                (filterPriority == null || task.Priority == filterPriority) &&
                (filterCategory == null || task.Category == filterCategory) &&
                (!filterDeadline.HasValue || task.Deadline.Date == filterDeadline.Value.Date)
            ).ToList();

            filteredTasks.Clear();
            foreach (var task in filtered)
            {
                filteredTasks.Add(task);
            }
        }

        // Obsługa zmiany stanu zakończenia zadania
        private void TaskCompleted_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                TaskItem task = checkBox.DataContext as TaskItem;
                if (task != null)
                {
                    task.IsCompleted = checkBox.IsChecked ?? false;
                }
            }
        }

        // Metoda zapisująca stan wszystkich zadań do pliku tekstowego
        private void SaveTasksToFile()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var task in tasks)
                {
                    string isCompleted = task.IsCompleted ? "1" : "0"; // Zamień bool na "1" lub "0"
                    writer.WriteLine($"{task.Id};{task.Task};{task.Priority};{task.Category};{task.Deadline:dd.MM.yyyy};{isCompleted}");
                }
            }
        }

        // Metoda wczytująca stan wszystkich zadań z pliku tekstowego
        private void LoadTasksFromFile()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.txt");

            if (File.Exists(filePath))
            {
                tasks.Clear(); // Wyczyść istniejące zadania przed wczytaniem nowych

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(';');
                        if (parts.Length == 6)
                        {
                            TaskItem task = new TaskItem
                            {
                                Id = int.Parse(parts[0]),
                                Task = parts[1],
                                Priority = parts[2],
                                Category = parts[3],
                                Deadline = DateTime.ParseExact(parts[4], "dd.MM.yyyy", null), // Zmiana formatu daty
                                IsCompleted = parts[5] == "1" // Zamień "1" na true, "0" na false
                            };

                            tasks.Add(task);
                        }
                    }
                }
            }

            // Odświeżenie listy filtrowanych zadań
            FilterTasks();
        }

        // Metoda obsługująca zdarzenie zamknięcia okna
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Zapisz stan aplikacji do pliku
            SaveTasksToFile();
        }
    }
}
