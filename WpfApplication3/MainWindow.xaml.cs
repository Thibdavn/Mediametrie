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
            foreach (Containers container in SelectedContainers)
            {
                ComboboxItem temp = new ComboboxItem();
                temp.Text = container.ContainerName;
                temp.Id = container.ContainerID;
                ContainerDropdown.Items.Add(temp);
            }
            InitializeComponent();
        }

        public void RefreshtasksView()
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));

            if (ContainerDropdown.SelectedItem == cdi1)
                SelectedTasks = _context.Tasks.Local;
            else if (ContainerDropdown.SelectedItem == cdi2)
                SelectedTasks = _context.Tasks.Local.Where(t => t.TaskPriority >= 3).ToList();
            else if (ContainerDropdown.SelectedItem == cdi3)
                SelectedTasks = _context.Tasks.Local.Where(t => t.DeadLineDate == DateTime.Today).ToList();
            else
            {
                SelectedTasks = Enumerable.Empty<Tasks>();
                foreach (Containers container in SelectedContainers.Where(s => s.ContainerID == ((ComboboxItem)ContainerDropdown.SelectedItem).Id))
                {
                    foreach (ContainerRelations relation in container.ContainerRelations)
                    {
                        SelectedTasks = SelectedTasks.Concat(_context.Tasks.Local.Where(t => t.TaskID == relation.Tasks.TaskID)).ToList();
                    }
                }
            }
            tasksViewSource.Source = SelectedTasks;
        }

       /* private void BindTotasksView()
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));

            if (ContainerDropdown.SelectedItem == cdi2)
                SelectedTasks = _context.Tasks.Local.Where(t => t.TaskPriority >= 3).ToList();
            else if (ContainerDropdown.SelectedItem == cdi3)
                SelectedTasks = _context.Tasks.Local.Where(t => t.DeadLineDate == DateTime.Today).ToList();
            else
            {
                SelectedTasks = Enumerable.Empty<Tasks>();
                foreach (Containers container in SelectedContainers.Where(s => s.ContainerID == ((ComboboxItem)ContainerDropdown.SelectedItem).Id))
                {
                    foreach (ContainerRelations relation in container.ContainerRelations)
                    {
                        SelectedTasks = SelectedTasks.Concat(_context.Tasks.Local.Where(t => t.TaskID == relation.Tasks.TaskID)).ToList();
                    }
                }
            }
        }*/

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
                        // This callback will be called after the CollectionView
                        // has pushed the changes back to the DataGrid.ItemSource.

                        // Write code here to save the data to the database.
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
            //BindTotasksView();
            _context.Tasks.Load();
            RefreshtasksView();
        }
        private void addContainerButton_Click(object sender, RoutedEventArgs e)
        {
            Window1 win1 = new Window1(_context, ((ComboboxItem)ContainerDropdown.SelectedItem).Id) { Owner = this};
            win1.ShowDialog();
            RefreshtasksView();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            this._context.Dispose();
        }
    }
}
