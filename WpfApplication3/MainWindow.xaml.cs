using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //Defs pour l'affichage des tasks
        public Mediametrie_appEntities _context = new Mediametrie_appEntities();

        public IEnumerable<Tasks> SelectedTasks { get; private set; }

        public IEnumerable<Containers> SelectedContainers { get; private set; }

        public IEnumerable<ContainerRelations> SelectedContainerRelations { get; private set; }

        public class ComboboxItem
        {
            public string Text { get; set; }
            public int Id { get; set; }
            public override string ToString()
            {
                return Text;
            }
            public int GetId()
            {
                return Id;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));
            // Load data by setting the CollectionViewSource.Source property:
            // tasksViewSource.Source = [generic data source]

            _context.Tasks.Load();
            _context.Containers.Load();
            _context.ContainerRelations.Load();

            SelectedTasks = _context.Tasks.Local;
            SelectedContainers = _context.Containers.Local;
            SelectedContainerRelations = _context.ContainerRelations.Local;
            //SelectedTasks = _context.Tasks.Local.Where(t => t.DeadLineDate == DateTime.Today);
            //SelectedTasks = _context.Tasks.Local.Where(t => t.TaskPriority >= 3);

            tasksViewSource.Source = SelectedTasks;
            RefreshComboBox();
            InitializeComponent();
        }

        public void RefreshComboBox()
        {
            _context.Containers.Load();
            SelectedContainers = _context.Containers.Local;
            for (int i = 3; i <= ContainerDropdown.Items.Count - 1; i++)
            {
                ContainerDropdown.Items.RemoveAt(i);
            }
            foreach (Containers container in SelectedContainers)
            {
                ComboboxItem temp = new ComboboxItem();
                temp.Text = container.ContainerName;
                temp.Id = container.ContainerID;
                ContainerDropdown.Items.Add(temp);
            }
        }

        public void RefreshtasksView()
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));

            DeleteRelationButton.Visibility = System.Windows.Visibility.Hidden;
            addRelationButton.Visibility = System.Windows.Visibility.Hidden;
            if (ContainerDropdown.SelectedItem == cdi1)
                SelectedTasks = _context.Tasks.Local;
            else if (ContainerDropdown.SelectedItem == cdi2)
                SelectedTasks = _context.Tasks.Local.Where(t => t.TaskPriority >= 3).ToList();
            else if (ContainerDropdown.SelectedItem == cdi3)
                SelectedTasks = _context.Tasks.Local.Where(t => t.DeadLineDate == DateTime.Today).ToList();
            else
            {
                _context.ContainerRelations.Load();
                SelectedContainers = _context.Containers.Local;
                SelectedTasks = Enumerable.Empty<Tasks>();
                foreach (Containers container in SelectedContainers.Where(s => s.ContainerID == ((ComboboxItem)ContainerDropdown.SelectedItem).Id))
                {
                    foreach (ContainerRelations relation in container.ContainerRelations)
                    {
                        SelectedTasks = SelectedTasks.Concat(_context.Tasks.Local.Where(t => t.TaskID == relation.Tasks.TaskID)).ToList();
                    }
                }
                DeleteRelationButton.Visibility = System.Windows.Visibility.Visible;
                addRelationButton.Visibility = System.Windows.Visibility.Visible;
            }
            tasksViewSource.Source = SelectedTasks;
        }

        private void ContainerDropdown_SelectionChanged(object sender, System.EventArgs e)
        {
            RefreshtasksView();
        }

        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (e.EditAction == DataGridEditAction.Commit)
            {
                ListCollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource) as ListCollectionView;
                if (view.IsAddingNew || view.IsEditingItem)
                {
                    this.Dispatcher.BeginInvoke(new DispatcherOperationCallback(param =>
                    {
                        _context.SaveChanges();
                        RefreshtasksView();
                        return null;
                    }), DispatcherPriority.Background, new object[] { null });
                }
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void newTaskButton_Click(object sender, RoutedEventArgs e)
        {
            _context.Database.ExecuteSqlCommand("INSERT INTO dbo.Tasks (TaskName, TaskPriority) VALUES ('Nouvelle tâche', 1);");
            if (ContainerDropdown.SelectedIndex > 2)
                _context.Database.ExecuteSqlCommand("INSERT INTO dbo.ContainerRelations (TaskID, ContainerID) SELECT MAX(Tasks.TaskID), " + ((ComboboxItem)ContainerDropdown.SelectedItem).Id + " FROM Tasks, Containers;");
            _context.Tasks.Load();
            RefreshtasksView();
        }

        private void addRelationButton_Click(object sender, RoutedEventArgs e)
        {
            Window1 win1 = new Window1(_context, ((ComboboxItem)ContainerDropdown.SelectedItem).Id) { Owner = this};
            win1.ShowDialog();
            RefreshtasksView();
        }

        private void addContainerButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window2();

            if (dialog.ShowDialog() == true)
            {
                _context.Database.ExecuteSqlCommand("INSERT INTO dbo.Containers (ContainerName) VALUES ('" + dialog.ResponseText + "');");
                RefreshComboBox();
            }
            ContainerDropdown.SelectedIndex = ContainerDropdown.Items.Count - 1;
        }

        private void deleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = (System.Collections.IList)tasksDataGrid.SelectedItems;
            var collection = items.Cast<WpfApplication3.Tasks>();
            foreach (WpfApplication3.Tasks task in collection.ToList())
                _context.Tasks.Remove(task);
            _context.SaveChanges();
            RefreshtasksView();
        }

        private void deleteRelationButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = (System.Collections.IList)tasksDataGrid.SelectedItems;
            var collection = items.Cast<WpfApplication3.Tasks>();
            foreach (WpfApplication3.Tasks task in collection.ToList())
                _context.ContainerRelations.Remove(_context.ContainerRelations.Where(r => r.TaskID == task.TaskID && r.ContainerID == ((ComboboxItem)ContainerDropdown.SelectedItem).Id).First());
            _context.SaveChanges();
            RefreshtasksView();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            this._context.Dispose();
        }
    }
}
