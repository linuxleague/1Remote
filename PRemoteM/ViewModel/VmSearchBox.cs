﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PRM.Core;
using PRM.Core.Model;
using PRM.Core.Protocol;
using Shawn.Utils.PageHost;
using Shawn.Utils;

namespace PRM.ViewModel
{
    public class ActionItem : NotifyPropertyChangedBase
    {
        private string _actionName = "";

        public string ActionName
        {
            get => _actionName;
            set => SetAndNotifyIfChanged(nameof(ActionName), ref _actionName, value);
        }

        public Action<int> Run;
    }

    public class VmSearchBox : NotifyPropertyChangedBase
    {
        private readonly double _gridMainWidth;
        private readonly double _oneItemHeight;
        private readonly double _oneActionHeight;
        private readonly double _cornerRadius;
        private readonly FrameworkElement _listSelections;
        private readonly FrameworkElement _listActions;

        public PrmContext Context { get; }

        public VmSearchBox(PrmContext context, double gridMainWidth, double oneItemHeight, double oneActionHeight, double cornerRadius, FrameworkElement listSelections, FrameworkElement listActions)
        {
            Context = context;
            _gridMainWidth = gridMainWidth;
            _oneItemHeight = oneItemHeight;
            _oneActionHeight = oneActionHeight;
            this._listSelections = listSelections;
            this._listActions = listActions;
            _cornerRadius = cornerRadius;
            GridKeywordHeight = 46;
            ReCalcWindowHeight(false);
        }

        private VmProtocolServer _selectedItem;

        public VmProtocolServer SelectedItem
        {
            get
            {
                if (_selectedItem == null)
                    if (Context.AppData.VmItemList.Count > 0
                        && _selectedIndex >= 0
                        && _selectedIndex < Context.AppData.VmItemList.Count)
                    {
                        _selectedItem = Context.AppData.VmItemList[_selectedIndex];
                    }
                return _selectedItem;
            }
            private set => SetAndNotifyIfChanged(nameof(SelectedItem), ref _selectedItem, value);
        }

        private int _selectedIndex;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                SetAndNotifyIfChanged(nameof(SelectedIndex), ref _selectedIndex, value);
                if (Context.AppData.VmItemList.Count > 0
                    && _selectedIndex >= 0
                    && _selectedIndex < Context.AppData.VmItemList.Count)
                {
                    SelectedItem = Context.AppData.VmItemList[_selectedIndex];
                }
                else
                {
                    SelectedItem = null;
                }
            }
        }

        private ObservableCollection<ActionItem> _actions = new ObservableCollection<ActionItem>();

        public ObservableCollection<ActionItem> Actions
        {
            get => _actions;
            set => SetAndNotifyIfChanged(nameof(Actions), ref _actions, value);
        }

        private int _selectedActionIndex;

        public int SelectedActionIndex
        {
            get => _selectedActionIndex;
            set => SetAndNotifyIfChanged(nameof(SelectedActionIndex), ref _selectedActionIndex, value);
        }

        private string _filter = "";

        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    SetAndNotifyIfChanged(nameof(Filter), ref _filter, value);
                }


                Task.Factory.StartNew(() =>
                {
                    var filter = _filter;
                    Thread.Sleep(70);
                    if (filter == _filter)
                    {
                        UpdateItemsList(value);
                    }
                });
            }
        }

        private double _gridMainHeight;

        public double GridMainHeight
        {
            get => _gridMainHeight;
            set
            {
                SetAndNotifyIfChanged(nameof(GridMainHeight), ref _gridMainHeight, value);
                GridMainClip = new RectangleGeometry(new Rect(new Size(_gridMainWidth, GridMainHeight)), _cornerRadius, _cornerRadius);
            }
        }

        private RectangleGeometry _gridMainClip = null;

        public RectangleGeometry GridMainClip
        {
            get => _gridMainClip;
            set => SetAndNotifyIfChanged(nameof(GridMainClip), ref _gridMainClip, value);
        }

        public double GridKeywordHeight { get; }

        private double _gridSelectionsHeight;

        public double GridSelectionsHeight
        {
            get => _gridSelectionsHeight;
            set => SetAndNotifyIfChanged(nameof(GridSelectionsHeight), ref _gridSelectionsHeight, value);
        }

        private double _gridActionsHeight;

        public double GridActionsHeight
        {
            get => _gridActionsHeight;
            set => SetAndNotifyIfChanged(nameof(GridActionsHeight), ref _gridActionsHeight, value);
        }

        public void ReCalcWindowHeight(bool showGridAction)
        {
            // show action list
            if (showGridAction)
            {
                GridSelectionsHeight = (Actions?.Count ?? 0) * _oneActionHeight;
                GridActionsHeight = GridKeywordHeight + GridSelectionsHeight;
                GridMainHeight = GridActionsHeight;
            }
            // show server list
            else
            {
                const int nMaxCount = 8;
                int visibleCount = Context.AppData.VmItemList.Count(vm => vm.ObjectVisibility == Visibility.Visible);
                if (visibleCount >= nMaxCount)
                    GridSelectionsHeight = _oneItemHeight * nMaxCount;
                else
                    GridSelectionsHeight = _oneItemHeight * visibleCount;
                GridMainHeight = GridKeywordHeight + GridSelectionsHeight;
            }
        }

        public void ShowActionsList()
        {
            if (Context.AppData.VmItemList.All(x => x.ObjectVisibility != Visibility.Visible) 
                || SelectedIndex < 0 
                || SelectedIndex >= Context.AppData.VmItemList.Count)
            {
                return;
            }

            #region Build Actions

            var actions = new ObservableCollection<ActionItem>
            {
                new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("word_connect"),
                    Run = (id) => { GlobalEventHelper.OnRequestServerConnect?.Invoke(id); },
                },
                new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("word_edit"),
                    Run = (id) =>
                    {
                        Debug.Assert(SelectedItem?.Server != null);
                        GlobalEventHelper.OnRequestGoToServerEditPage?.Invoke(id, false, false);
                    },
                },
                new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("server_card_operate_duplicate"),
                    Run = (id) =>
                    {
                        Debug.Assert(SelectedItem?.Server != null);
                        GlobalEventHelper.OnRequestGoToServerEditPage?.Invoke(id, true, false);
                    },
                }
            };
            if (SelectedItem.Server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortBase)))
            {
                actions.Add(new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("server_card_operate_copy_address"),
                    Run = (id) =>
                    {
                        var pb = Context.AppData.VmItemList.First(x => x.Server.Id == id);
                        if (pb.Server is ProtocolServerWithAddrPortBase server)
                            try
                            {
                                Clipboard.SetText($"{server.Address}:{server.GetPort()}");
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                    },
                });
            }
            if (SelectedItem.Server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortUserPwdBase)))
            {
                actions.Add(new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("server_card_operate_copy_username"),
                    Run = (id) =>
                    {
                        var pb = Context.AppData.VmItemList.First(x => x.Server.Id == id);
                        if (pb.Server is ProtocolServerWithAddrPortUserPwdBase server)
                            try
                            {
                                Clipboard.SetText(server.UserName);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                    },
                });
            }
            if (SelectedItem.Server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortUserPwdBase)))
            {
                actions.Add(new ActionItem()
                {
                    ActionName = SystemConfig.Instance.Language.GetText("server_card_operate_copy_password"),
                    Run = (id) =>
                    {
                        var pb = Context.AppData.VmItemList.First(x => x.Server.Id == id);
                        if (pb.Server is ProtocolServerWithAddrPortUserPwdBase server)
                            try
                            {
                                Clipboard.SetText(Context.DbOperator.DecryptOrReturnOriginalString(server.Password));
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                    },
                });
            }

            #endregion Build Actions

            Actions = actions;
            SelectedActionIndex = 0;

            ReCalcWindowHeight(true);

            _listActions.Visibility = Visibility.Visible;

            var sb = new Storyboard();
            sb.AddSlideFromLeft(0.3, _gridMainWidth);
            sb.Begin(_listActions);
        }

        public void HideActionsList()
        {
            var sb = new Storyboard();
            sb.AddSlideToLeft(0.3, _gridMainWidth);
            sb.Completed += (o, args) =>
            {
                _listActions.Visibility = Visibility.Hidden;
                ReCalcWindowHeight(false);
            };
            sb.Begin(_listActions);
        }

        private void ShowAllItems()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                // show all
                foreach (var vm in Context.AppData.VmItemList)
                {
                    vm.ObjectVisibility = Visibility.Visible;
                    vm.DispNameControl = vm.OrgDispNameControl;
                    vm.SubTitleControl = vm.OrgSubTitleControl;
                }
            });
        }

        private string IsAnyGroupNameMatched(List<string> keywords)
        {
            if (!SystemConfig.Instance.Launcher.AllowGroupNameSearch) return string.Empty;

            bool anyGroupMatched = false;

            // TODO need a better tag matching
            // if tag name search enabled, show group name as prefix
            foreach (var tag in Context.AppData.Tags.Select(x => x.Name))
            {
                if (keywords.Any(keyword => tag.ToLower() == keyword.ToLower()))
                {
                    anyGroupMatched = true;
                    return tag;
                }
            }

            return string.Empty;
        }

        private readonly SolidColorBrush _highLightBrush = new SolidColorBrush(Color.FromArgb(80, 239, 242, 132));

        public void UpdateItemsList(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                ShowAllItems();
            }
            else
            {
                var keyWords = keyword.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var matchedTag = IsAnyGroupNameMatched(keyWords);

                App.Current.Dispatcher.Invoke(() =>
                {
                    // match keyword
                    foreach (var vm in Context.AppData.VmItemList)
                    {
                        Debug.Assert(vm != null);
                        Debug.Assert(!string.IsNullOrEmpty(vm.Server.ClassVersion));
                        Debug.Assert(!string.IsNullOrEmpty(vm.Server.Protocol));

                        if (string.IsNullOrEmpty(matchedTag) == false && vm.Server.Tags.Contains(matchedTag) == false)
                            continue;

                        var dispName = vm.Server.DispName;
                        // if tag name search enabled, and keyword match the tag name, show tag name as prefix
                        if (string.IsNullOrEmpty(matchedTag) == false)
                            dispName = $"{matchedTag} - {dispName}";
                        var subTitle = vm.Server.SubTitle;

                        var mrs = Context.KeywordMatchService.Matchs(new List<string>() { dispName, subTitle }, keyWords);
                        if (mrs.IsMatchAllKeywords)
                        {
                            vm.ObjectVisibility = Visibility.Visible;

                            var m1 = mrs.HitFlags[0];
                            if (m1.Any(x => x == true))
                            {
                                var sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                                for (int i = 0; i < m1.Count; i++)
                                {
                                    if (m1[i])
                                        sp.Children.Add(new TextBlock()
                                        {
                                            Text = dispName[i].ToString(),
                                            Background = _highLightBrush,
                                        });
                                    else
                                        sp.Children.Add(new TextBlock()
                                        {
                                            Text = dispName[i].ToString(),
                                        });
                                }

                                vm.DispNameControl = sp;
                            }
                            else
                            {
                                vm.DispNameControl = vm.OrgDispNameControl;
                            }

                            var m2 = mrs.HitFlags[1];
                            if (m2.Any(x => x == true))
                            {
                                var sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                                for (int i = 0; i < m2.Count; i++)
                                {
                                    if (m2[i])
                                        sp.Children.Add(new TextBlock()
                                        {
                                            Text = subTitle[i].ToString(),
                                            Background = _highLightBrush,
                                        });
                                    else
                                        sp.Children.Add(new TextBlock()
                                        {
                                            Text = subTitle[i].ToString(),
                                        });
                                }

                                vm.SubTitleControl = sp;
                            }
                            else
                            {
                                vm.SubTitleControl = vm.OrgSubTitleControl;
                            }
                        }
                        else
                        {
                            vm.ObjectVisibility = Visibility.Collapsed;
                        }
                    }
                });
            }


            App.Current.Dispatcher.Invoke(() =>
            {
                // reorder
                OrderItemByLastConnTime();

                ReCalcWindowHeight(false);
            });
        }

        private void OrderItemByLastConnTime()
        {
            for (var i = 1; i < Context.AppData.VmItemList.Count; i++)
            {
                var s0 = Context.AppData.VmItemList[i - 1];
                var s1 = Context.AppData.VmItemList[i];
                if (s0.Server.LastConnTime < s1.Server.LastConnTime)
                {
                    Context.AppData.VmItemList = new ObservableCollection<VmProtocolServer>(Context.AppData.VmItemList.OrderByDescending(x => x.Server.LastConnTime));
                    break;
                }
            }

            // index the list to first item
            for (var i = 0; i < Context.AppData.VmItemList.Count; i++)
            {
                var vm = Context.AppData.VmItemList[i];
                if (vm.ObjectVisibility == Visibility.Visible)
                {
                    SelectedIndex = i;
                    SelectedItem = vm;
                    break;
                }
            }
        }


        public void AddSelectedIndexOnVisibilityItems(int step)
        {
            var index = SelectedIndex;
            int count = 0;
            if (step > 0)
            {
                for (int i = SelectedIndex + 1; i < Context.AppData.VmItemList.Count; i++)
                {
                    if (Context.AppData.VmItemList[i].ObjectVisibility == Visibility.Visible)
                    {
                        ++count;
                        index = i;
                        if (count == step)
                            break;
                    }
                }
            }
            else if (step < 0)
            {
                step = Math.Abs(step);
                for (int i = SelectedIndex - 1; i >= 0; i--)
                {
                    if (Context.AppData.VmItemList[i].ObjectVisibility == Visibility.Visible)
                    {
                        ++count;
                        index = i;
                        if (count == step)
                            break;
                    }
                }
            }
            SelectedIndex = index;
        }
    }
}