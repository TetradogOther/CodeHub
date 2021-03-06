﻿using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using CodeHub.Helpers;
using CodeHub.Services;
using CodeHub.Views;
using Octokit;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System;

namespace CodeHub.ViewModels
{
    public class DeveloperProfileViewmodel : AppViewmodel
    {
        #region properties

        public int PaginationIndex { get; set; }
        public double MaxScrollViewerOffset { get; set; }

        public ObservableCollection<Activity> _events;
        public ObservableCollection<Activity> Events
        {
            get
            {
                return _events;
            }
            set
            {
                Set(() => Events, ref _events, value);
            }
        }

        public ObservableCollection<Repository> _repositories;
        public ObservableCollection<Repository> Repositories
        {
            get
            {
                return _repositories;
            }
            set
            {
                Set(() => Repositories, ref _repositories, value);
            }

        }

        public ObservableCollection<User> _followers;
        public ObservableCollection<User> Followers
        {
            get
            {
                return _followers;
            }
            set
            {
                Set(() => Followers, ref _followers, value);
            }
        }

        public ObservableCollection<User> _following;
        public ObservableCollection<User> Following
        {
            get
            {
                return _following;
            }
            set
            {
                Set(() => Following, ref _following, value);
            }
        }

        public bool _IsFollowersLoading;
        public bool IsFollowersLoading
        {
            get
            {
                return _IsFollowersLoading;
            }
            set
            {
                Set(() => IsFollowersLoading, ref _IsFollowersLoading, value);
            }
        }

        public bool _IsFollowingLoading;
        public bool IsFollowingLoading
        {
            get
            {
                return _IsFollowingLoading;
            }
            set
            {
                Set(() => IsFollowingLoading, ref _IsFollowingLoading, value);
            }
        }

        public User _developer;
        public User Developer
        {
            get
            {
                return _developer;
            }
            set
            {
                Set(() => Developer, ref _developer, value);
            }
        }

        public bool _isFollowing;
        public bool IsFollowing
        {
            get
            {
                return _isFollowing;
            }
            set
            {
                Set(() => IsFollowing, ref _isFollowing, value);
            }
        }

        public bool _IsEventsLoading;
        public bool IsEventsLoading
        {
            get
            {
                return _IsEventsLoading;
            }
            set
            {
                Set(() => IsEventsLoading, ref _IsEventsLoading, value);
            }
        }

        public bool _IsReposLoading;
        public bool IsReposLoading
        {
            get
            {
                return _IsReposLoading;
            }
            set
            {
                Set(() => IsReposLoading, ref _IsReposLoading, value);
            }
        }

        public bool _canFollow;
        public bool CanFollow
        {
            get
            {
                return _canFollow;
            }
            set
            {
                Set(() => CanFollow, ref _canFollow, value);
            }
        }

        public bool _followProgress;
        public bool FollowProgress
        {
            get
            {
                return _followProgress;
            }
            set
            {
                Set(() => FollowProgress, ref _followProgress, value);
            }
        }
        #endregion

        public async Task Load(object user)
        {
            if (!GlobalHelper.IsInternet())
            {
                //Sending NoInternet message to all viewModels
                Messenger.Default.Send(new GlobalHelper.LocalNotificationMessageType { Message = "No Internet", Glyph = "\uE704" });
            }
            else
            {
                isLoading = true;
                if (user is string login)
                {
                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        Developer = await UserUtility.GetUserInfo(login);
                    }
                }
                else
                {
                    Developer = user as User;
                    if(Developer.Name == null)
                    {
                        Developer = await UserUtility.GetUserInfo(Developer.Login);
                    }
                }
                if (Developer != null)
                {
                    if (Developer.Type == AccountType.Organization || Developer.Login == GlobalHelper.UserLogin)
                    {
                        CanFollow = false;
                    }
                    else
                    {
                        CanFollow = true;
                        FollowProgress = true;
                        if (await UserUtility.CheckFollow(Developer.Login))
                        {
                            IsFollowing = true;
                        }
                        FollowProgress = false;
                    }

                    IsEventsLoading = true;
                    Events = await ActivityService.GetUserPerformedActivity(Developer.Login);
                    IsEventsLoading = false;
                }
                isLoading = false;
            }
        }

        private RelayCommand _followCommand;
        public RelayCommand FollowCommand
        {
            get
            {
                return _followCommand
                    ?? (_followCommand = new RelayCommand(
                                          async () =>
                                          {
                                              FollowProgress = true;
                                              if (await UserUtility.FollowUser(Developer.Login))
                                              {
                                                  IsFollowing = true;
                                              }
                                              FollowProgress = false;
                                          }));
            }
        }

        private RelayCommand _unFollowCommand;
        public RelayCommand UnfollowCommand
        {
            get
            {
                return _unFollowCommand
                    ?? (_unFollowCommand = new RelayCommand(
                                          async () =>
                                          {
                                              FollowProgress = true;
                                              await UserUtility.UnFollowUser(Developer.Login);
                                              IsFollowing = false;
                                              FollowProgress = false;
                                          }));
            }
        }

        public async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot p = sender as Pivot;

            if (p.SelectedIndex == 0)
            {
                IsEventsLoading = true;
                if(Developer != null)
                    Events = await ActivityService.GetUserPerformedActivity(Developer.Login);
                IsEventsLoading = false;

            }
            else if (p.SelectedIndex == 1)
            {
                IsReposLoading = true;
                await LoadRepos();
                IsReposLoading = false;
            }
            else if (p.SelectedIndex == 2)
            {
                IsFollowersLoading = true;
                Followers = await UserUtility.GetAllFollowers(Developer.Login);
                IsFollowersLoading = false;
            }
            else if (p.SelectedIndex == 3)
            {
                IsFollowingLoading = true;
                Following = await UserUtility.GetAllFollowing(Developer.Login);
                IsFollowingLoading = false;
            }
        }

        public async Task LoadRepos()
        {
            PaginationIndex++;
            if (PaginationIndex > 1)
            {
                var repos = await RepositoryUtility.GetRepositoriesForUser(Developer.Login, PaginationIndex);
                if (repos != null)
                {
                    if (repos.Count > 0)
                    {
                        foreach (var i in repos)
                        {
                            Repositories.Add(i);
                        }
                    }
                    else
                    {
                        //no more repos to load
                        PaginationIndex = -1;
                    }
                }
            }
            else if (PaginationIndex == 1)
            {
                Repositories = await RepositoryUtility.GetRepositoriesForUser(Developer.Login, PaginationIndex);
            }
        }

        public void RepoDetailNavigateCommand(object sender, ItemClickEventArgs e)
        {
            SimpleIoc.Default.GetInstance<Services.IAsyncNavigationService>().NavigateAsync(typeof(RepoDetailView), e.ClickedItem as Repository);
        }

        public void UserTapped(object sender, ItemClickEventArgs e)
        {
            SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(DeveloperProfileView), e.ClickedItem as User);
        }

        public void FeedListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Activity activity = e.ClickedItem as Activity;

            switch (activity.Type)
            {
                case "IssueCommentEvent":
                    SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(IssueDetailView), new Tuple<Repository, Issue>(activity.Repo, ((IssueCommentPayload)activity.Payload).Issue));
                    break;

                case "IssuesEvent":
                    SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(IssueDetailView), new Tuple<Repository, Issue>(activity.Repo, ((IssueEventPayload)activity.Payload).Issue));
                    break;

                case "PullRequestReviewCommentEvent":
                    SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(PullRequestDetailView), new Tuple<Repository, PullRequest>(activity.Repo, ((PullRequestCommentPayload)activity.Payload).PullRequest));
                    break;

                case "PullRequestEvent":
                case "PullRequestReviewEvent":
                    SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(PullRequestDetailView), new Tuple<Repository, PullRequest>(activity.Repo, ((PullRequestEventPayload)activity.Payload).PullRequest));
                    break;

                default:
                    SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(RepoDetailView), activity.Repo);
                    break;
            }

        }
    }
}
