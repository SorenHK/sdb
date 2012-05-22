using System.Windows.Forms;
using Aga.Controls.Tree;
using SDB.DataServices;
using SDB.DataServices.Cache;
using SDB.DataServices.Tcp;

namespace SDB.Viewer
{
    public partial class MainForm : Form
    {
        private readonly SDBTreeModel _model;
        private readonly DataServiceBase _dataService;

        public MainForm()
        {
            InitializeComponent();

            _dataService = new CacheDataService(new TcpDataService(new TcpClient("localhost")));

            tree.Model = _model = new SDBTreeModel(_dataService);

            tree.SelectionChanged += tree_SelectionChanged;
            tree.Expanding += tree_Expanding;
        }

        void tree_Expanding(object sender, TreeViewAdvEventArgs e)
        {
            
        }

        private void tree_SelectionChanged(object sender, System.EventArgs e)
        {
            
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_dataService != null)
                _dataService.Dispose();

            base.OnClosing(e);
        }

        private Node CreateNode(int itemId)
        {
            return new Node("Item #" + itemId);
        }
    }
}
