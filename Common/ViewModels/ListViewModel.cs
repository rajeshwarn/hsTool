﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Threading;
using Common.Command;
using Common.Models;

namespace Common.ViewModels
{
    public abstract class ListViewModel<TListModel, TItemModel> : NotificationBase
        where TListModel : ListModel<TItemModel>
        where TItemModel : ItemModel
    {
        protected TListModel _listModel;

        protected abstract bool FilterItem(TItemModel item);

        public ListViewModel(TListModel listModel)
        {
            _listModel = listModel;
            _listModel.CollectionChanged += _listModel_CollectionChanged;

            var rows = from x in _listModel where FilterItem(x) select x;
            foreach (var item in rows)
            {
                this.Items.Add(item);
            }
            ItemsView = CollectionViewSource.GetDefaultView(this.Items); 
        }

        private void _listModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var rows = from x in e.NewItems.Cast<TItemModel>() where FilterItem(x) select x;
                foreach (var item in rows)
                {
                    this.Items.Add(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach(var item in e.OldItems.OfType<TItemModel>().ToList())
                {
                    this.Items.Remove(item);
                }
            }
        }

        protected void ApplyFilter()
        {
            this.Items.Clear();

            var rows = from x in _listModel where FilterItem(x) select x;            
            foreach (var item in rows)
            {                
                this.Items.Add(item);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged("SearchText"); DelaySearch(_searchText); }
        }
        private string _searchText = string.Empty;
        private DispatcherTimer _timer = null;

        private void DelaySearch(string keyword)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Start();
            }
            else
            {
                _timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    this.ApplyFilter();
                };
                _timer.Start();
            }
        }

        #region fields

        public ObservableCollection<TItemModel> Items
        {
            get { return _items; }
            set { _items = value; OnPropertyChanged("Items"); }
        }
        private ObservableCollection<TItemModel> _items = new ObservableCollection<TItemModel>();

        public TItemModel SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; OnPropertyChanged("SelectedItem"); }
        }
        private TItemModel _selectedItem;

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set { _selectedItems = value; OnPropertyChanged("SelectedItems"); }
        }
        private IList _selectedItems;

        public ICollectionView ItemsView { get; set; }

        #endregion

        public ICommand SelectionChangedCommand
        {
            get
            {
                return selectionChangedCommand = selectionChangedCommand ?? new RelayCommand<IList>(p =>
                {
                    this.SelectedItems = p;
                });
            }
        }
        private ICommand selectionChangedCommand;

        public event EventHandler SelectionChanged;

        protected void onSelectionChanged(string propertyName)
        {
            var handler = this.SelectionChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public class SelectionDelegateCommand<TViewModel> : ActiveDelegateCommand<TViewModel>
            where TViewModel : ListViewModel<TListModel, TItemModel>
        {
            public SelectionDelegateCommand(TViewModel viewModel, Action<TViewModel> execute, Func<TViewModel, bool> canExecute = null)
                : base(viewModel, execute, canExecute)
            {
                _viewModel.SelectionChanged += (s, e) =>
                {
                    this.OnCanExecuteChanged();
                };
            }

            public override bool CanExecute(object parameter)
            {
                return _viewModel.SelectedItem != null && (_canExecute == null || _canExecute(_viewModel));
            }
        }
    }
}
