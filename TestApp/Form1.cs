using System;
using System.Windows.Forms;
using SDB;
using SDB.DataServices.Auth;
using SDB.DataServices.Cache;
using SDB.DataServices.Tcp;
using SDB.ObjectRelationalMapping;
using SDB.ObjectRelationalMapping.Collections;
using TestApp.Entities;

namespace TestApp
{
    public partial class Form1 : Form
    {
        private readonly IProxyCollection<Person> _people;
        private readonly SdbConnector _connector;

        internal Form1(SdbConnector connector)
        {
            InitializeComponent();

            _connector = connector;

            Text = "TestApp: " + connector.Name;

            personBindingSource.DataSource = _people = connector.ObjectMapper.Get<Person>();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_connector != null)
                _connector.Dispose();

            base.OnClosed(e);
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            _people.Add(new Person());
        }
    }
}
