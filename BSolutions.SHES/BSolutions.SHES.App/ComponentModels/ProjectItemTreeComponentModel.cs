﻿using BSolutions.SHES.App.Messages;
using BSolutions.SHES.App.ViewModels;
using BSolutions.SHES.Models;
using BSolutions.SHES.Models.Entities;
using BSolutions.SHES.Models.Extensions;
using BSolutions.SHES.Models.Helpers;
using BSolutions.SHES.Models.Observables;
using BSolutions.SHES.Services.ProjectItems;
using BSolutions.SHES.Shared.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace BSolutions.SHES.App.ComponentModels
{
    public class ProjectItemTreeComponentModel : ObservableRecipient
    {
        private readonly ResourceLoader _resourceLoader;
        private readonly IProjectItemService _projectItemService;

        #region --- Properties ---

        public IAsyncRelayCommand AddProjectItemDialogCommand { get; }
        public IAsyncRelayCommand AddProjectItemCommand { get; }
        public IAsyncRelayCommand DeleteProjectItemCommand { get; }

        public ObservableCollection<ObservableProjectItem> ProjectItems { get; } = new ObservableCollection<ObservableProjectItem>();
        public ObservableCollection<ProjectItemTypeInfo> RestrictedProjectItemInfos { get; } = new ObservableCollection<ProjectItemTypeInfo>();

        private ObservableProject currentProject;
        public ObservableProject CurrentProject
        {
            get => currentProject;
            private set
            {
                SetProperty(ref currentProject, value);
            }
        }

        private ObservableProjectItem _selectedProjectItem;
        public ObservableProjectItem SelectedProjectItem
        {
            get => _selectedProjectItem;
            set
            {
                SetProperty(ref _selectedProjectItem, value);
                this.UpdateProjectItemTypes();

                AddProjectItemDialogCommand.NotifyCanExecuteChanged();
                DeleteProjectItemCommand.NotifyCanExecuteChanged();

                // Set current project item
                WeakReferenceMessenger.Default.Send(new CurrentTreeProjectItemChangedMessage(value));
                WeakReferenceMessenger.Default.Send(new CurrentDevicesProjectItemChangedMessage(value));
            }
        }

        private ProjectItemTypeInfo _newProjectItemType;
        public ProjectItemTypeInfo NewProjectItemType
        {
            get => _newProjectItemType;
            set
            {
                SetProperty(ref _newProjectItemType, value);
                OnPropertyChanged(nameof(this.NewProjectItemDialogHasErrors));
            }
        }

        
        private string _newProjectItemName;
        public string NewProjectItemName
        {
            get => _newProjectItemName;
            set
            {
                SetProperty(ref _newProjectItemName, value);
                OnPropertyChanged(nameof(this.NewProjectItemDialogHasErrors));
            }
        }

        public bool NewProjectItemDialogHasErrors
        {
            get => this.NewProjectItemType == null || string.IsNullOrEmpty(this.NewProjectItemName);
        }

        private bool _isTreeLoading = true;
        public bool IsTreeLoading
        {
            get => _isTreeLoading;
            set => SetProperty(ref _isTreeLoading, value);
        }

        #endregion

        #region --- Constructor ---

        /// <summary>Initializes a new instance of the <see cref="ProjectItemTreeComponentModel" /> class.</summary>
        /// <param name="projectItemService">The project item service.</param>
        public ProjectItemTreeComponentModel(IProjectItemService projectItemService)
        {
            this._projectItemService = projectItemService;

            // Resource Loader
            this._resourceLoader = ResourceLoader.GetForViewIndependentUse();

            // Commands
            AddProjectItemDialogCommand = new AsyncRelayCommand<ContentDialog>(async (dialog) => await AddProjectItemDialog(dialog), CanAddProjectItemDialog);
            AddProjectItemCommand = new AsyncRelayCommand(AddProjectItem);
            DeleteProjectItemCommand = new AsyncRelayCommand(async () => await DeleteProjectItem(), CanDeleteProjectItem);

            // Messages
            WeakReferenceMessenger.Default.Register<ProjectItemTreeComponentModel, CurrentProjectChangedMessage>(this, (r, m) => r.CurrentProject = m.Value);
        }

        #endregion

        #region --- Events ---

        public async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var projectItems = await this._projectItemService.GetProjectItemsAsync(this.CurrentProject, true);
            this.ProjectItems.AddRange(projectItems);
            this.SelectedProjectItem = projectItems.FirstOrDefault();
            this.IsTreeLoading = false;
        }

        #endregion

        #region --- Commands ---

        /// <summary>Opens the dialog to add a project item.</summary>
        /// <param name="dialog">The dialog.</param>
        private async Task AddProjectItemDialog(ContentDialog dialog)
        {
            await dialog.ShowAsync();
        }

        private bool CanAddProjectItemDialog(ContentDialog dialog)
        {
            return this.RestrictedProjectItemInfos.Count > 0;
        }

        /// <summary>
        /// Adds a new project item as child of the selected project item.
        /// </summary>
        private async Task AddProjectItem()
        {
            ObservableProjectItem item = new ObservableProjectItem(ReflectionHelper.GetInstance<ProjectItem>(this.NewProjectItemType.FullName));
            item.entity.ParentId = this.SelectedProjectItem.Id;
            item.Name = this.NewProjectItemName;

            // Insert new project item
            await this._projectItemService.AddAsync(item);

            // Add new project item to project tree
            this.SelectedProjectItem.Children.Add(item);

            // Causes the DataGrid to be reloaded with the new item
            WeakReferenceMessenger.Default.Send(new CurrentTreeProjectItemChangedMessage(this._selectedProjectItem));

            // Clear form
            this.NewProjectItemType = null;
            this.NewProjectItemName = string.Empty;
        }

        /// <summary>
        /// Deletes the selected project item.
        /// </summary>
        private async Task DeleteProjectItem()
        {
            ObservableProjectItem parent = this.ProjectItems.Traverse(pi => pi.Children)
                .FirstOrDefault(pi => pi.Id == this.SelectedProjectItem.Parent.Id);

            await this._projectItemService.DeleteAsync(this.SelectedProjectItem);
            parent.Children.Remove(this.SelectedProjectItem);
            this.SelectedProjectItem = parent;
        }

        private bool CanDeleteProjectItem()
        {
            return this.SelectedProjectItem != null && this.SelectedProjectItem.Id != this.ProjectItems.First().Id;
        }

        #endregion

        /// <summary>Updates the project item types for project item creation.</summary>
        private void UpdateProjectItemTypes()
        {
            this.RestrictedProjectItemInfos.Clear();

            if (this.SelectedProjectItem != null)
            {
                this.RestrictedProjectItemInfos.AddRange(this.SelectedProjectItem?.entity.GetRestrictChildrenInfos());
            }
        }
    }
}
