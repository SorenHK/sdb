using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Aga.Controls.Tree;
using SDB.DataServices;

namespace SDB.Viewer
{
    class SDBTreeModel : ITreeModel
    {
        public event EventHandler<TreeModelEventArgs> NodesChanged;
        public event EventHandler<TreeModelEventArgs> NodesInserted;
        public event EventHandler<TreeModelEventArgs> NodesRemoved;
        public event EventHandler<TreePathEventArgs> StructureChanged;

        private readonly DataServiceBase _dataService;

        public SDBTreeModel(DataServiceBase dataService)
        {
            _dataService = dataService;
            _dataService.RelationAdded += OnRelationAdded;
            _dataService.RelationRemoved += OnRelationRemoved;
            _dataService.ItemChanged += OnItemChanged;
        }

        private void OnRelationAdded(DbRelation relation)
        {
            
        }

        private void OnRelationRemoved(DbRelation relation)
        {
            
        }

        private void OnItemChanged(int id)
        {
            var item = _dataService.GetItem(id);

            if (NodesChanged != null)
                NodesChanged(this, new TreeModelEventArgs(new TreePath(item), new object[] { item }));
        }

        public IEnumerable GetChildren(TreePath treePath)
        {
            if (treePath.IsEmpty())
            {
                return _dataService.GetRelations(null);
            }

            if (treePath.LastNode is DbItem)
            {
                var item = treePath.LastNode as DbItem;
                return _dataService.GetRelations(item.Id);
            }
            else if (treePath.LastNode is DbRelation)
            {
                var relation = treePath.LastNode as DbRelation;
                if (relation.ToId != null)
                    return new[] { _dataService.GetItem(relation.ToId.Value) };
            }

            return null;
        }

        public bool IsLeaf(TreePath treePath)
        {
            if (treePath.IsEmpty())
            {
                var relations = _dataService.GetRelations(null);
                return relations == null || !relations.Any();
            }

            if (treePath.LastNode is DbItem)
            {
                var item = treePath.LastNode as DbItem;
                var relations = _dataService.GetRelations(item.Id);
                return relations == null || !relations.Any();
            }
            else if (treePath.LastNode is DbRelation)
            {
                var relation = treePath.LastNode as DbRelation;
                if (relation.ToId != null)
                {
                    var item = _dataService.GetItem(relation.ToId.Value);
                    return item == null;
                }
            }

            return true;
        }
    }
}
