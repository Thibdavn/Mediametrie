using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Mediametrie_appEntities _context;
        public IEnumerable<Tasks> SelectedTasksAdd { get; private set; }
        public IEnumerable<Tasks> DisplayTasks { get; private set; }
        public IEnumerable<Containers> SelectedContainersAdd { get; private set; }
        public IEnumerable<ContainerRelations> SelectedContainerRelationsAdd { get; private set; }

        public int ContainerID;

        public Window1(Mediametrie_appEntities PassedContext, int PassedContainerID)
        {
            InitializeComponent();
            _context = PassedContext;
            ContainerID = PassedContainerID;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));
            // Load data by setting the CollectionViewSource.Source property:
            // tasksViewSource.Source = [generic data source]

            SelectedContainersAdd = _context.Containers.Local;
            SelectedContainerRelationsAdd = _context.ContainerRelations.Local;
            SelectedTasksAdd = _context.Tasks.Local;
            RefreshAddtasksView();
        }

        public void RefreshAddtasksView()
        {
            System.Windows.Data.CollectionViewSource tasksViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tasksViewSource")));

            DisplayTasks = _context.Tasks.Local;
            SelectedTasksAdd = Enumerable.Empty<Tasks>();
            foreach (Containers container in SelectedContainersAdd.Where(s => s.ContainerID == ContainerID))
            {
                foreach (ContainerRelations relation in container.ContainerRelations)
                {
                    SelectedTasksAdd = SelectedTasksAdd.Concat(_context.Tasks.Local.Where(t => t.TaskID == relation.Tasks.TaskID)).ToList();
                }
            }
            DisplayTasks = DisplayTasks.Except(SelectedTasksAdd);
            tasksViewSource.Source = DisplayTasks;
        }

        private void addContainerButton_Click(object sender, RoutedEventArgs e)
        {
            IList items = tasksDataGrid.SelectedItems;
            var collection = items.Cast<WpfApplication3.Tasks>();
            foreach (WpfApplication3.Tasks task in collection.ToList())
            {
                _context.Database.ExecuteSqlCommand("INSERT INTO dbo.ContainerRelations (TaskID, ContainerID) VALUES (" + task.TaskID + "," + ContainerID + ");");
            }
            _context.ContainerRelations.Load();
            RefreshAddtasksView();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
