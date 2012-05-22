using System;
using System.ComponentModel;
using System.Threading;

namespace SDB.ObjectRelationalMapping.Collections
{
    public class ThreadedBindingList<T> : BindingList<T>
    {
        private SynchronizationContext _context = SynchronizationContext.Current;

        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            ExecuteInContext(() => base.OnAddingNew(e));
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            ExecuteInContext(() => base.OnListChanged(e));
        }

        private void ExecuteInContext(Action action)
        {
            if (action == null)
                return;

            if (_context == null)
                _context = SynchronizationContext.Current;

            if (_context == null)
            {
                action();
            }
            else
            {
                _context.Send(delegate
                {
                    action();
                }, null);
            }
        }
    }
}
