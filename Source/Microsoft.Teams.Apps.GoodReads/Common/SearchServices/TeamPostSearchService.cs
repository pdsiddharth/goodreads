// <copyright file="TeamPostSearchService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.SearchServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.Teams.Apps.GoodReads.Models.Configuration;

    /// <summary>
    /// Team post Search service which helps in creating index, indexer and data source if it doesn't exist
    /// for indexing table which will be used for search by Messaging Extension.
    /// </summary>
    public class TeamPostSearchService : ITeamPostSearchService, IDisposable
    {
        /// <summary>
        /// Azure Search service index name for team post.
        /// </summary>
        private const string TeamPostIndexName = "team-post-index";

        /// <summary>
        /// Azure Search service indexer name for team post.
        /// </summary>
        private const string TeamPostIndexerName = "team-post-indexer";

        /// <summary>
        /// Azure Search service data source name for team post.
        /// </summary>
        private const string TeamPostDataSourceName = "team-post-storage";

        /// <summary>
        /// Table name where team post data will get saved.
        /// </summary>
        private const string TeamPostTableName = "TeamPostEntity";

        /// <summary>
        /// Represents the sorting type as popularity means to sort the data based on number of votes.
        /// </summary>
        private const string SortByPopular = "Popularity";

        /// <summary>
        /// Azure Search service maximum search result count for team post entity.
        /// </summary>
        private const int ApiSearchResultCount = 1500;

        /// <summary>
        /// Used to initialize task.
        /// </summary>
        private readonly Lazy<Task> initializeTask;

        /// <summary>
        /// Instance of Azure Search service client.
        /// </summary>
        private readonly SearchServiceClient searchServiceClient;

        /// <summary>
        /// Instance of Azure Search index client.
        /// </summary>
        private readonly SearchIndexClient searchIndexClient;

        /// <summary>
        /// Instance of team post storage helper to update post and get information of posts.
        /// </summary>
        private readonly ITeamPostStorageProvider teamPostStorageProvider;

        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamPostSearchService> logger;

        /// <summary>
        /// Represents a set of key/value application configuration properties.
        /// </summary>
        private readonly SearchServiceSetting options;

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPostSearchService"/> class.
        /// </summary>
        /// <param name="optionsAccessor">A set of key/value application configuration properties.</param>
        /// <param name="teamPostStorageProvider">Team post storage provider dependency injection.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="searchServiceClient">Search service client dependency injection.</param>
        /// <param name="searchIndexClient">Search index client dependency injection.</param>
        public TeamPostSearchService(
            IOptions<SearchServiceSetting> optionsAccessor,
            ITeamPostStorageProvider teamPostStorageProvider,
            ILogger<TeamPostSearchService> logger,
            SearchServiceClient searchServiceClient,
            SearchIndexClient searchIndexClient)
        {
            optionsAccessor = optionsAccessor ?? throw new ArgumentNullException(nameof(optionsAccessor));

            this.options = optionsAccessor.Value;
            var searchServiceValue = this.options.SearchServiceName;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync());
            this.teamPostStorageProvider = teamPostStorageProvider;
            this.logger = logger;
            this.searchServiceClient = searchServiceClient;
            this.searchIndexClient = searchIndexClient;
        }

        /// <summary>
        /// Provide search result for table to be used by user's based on Azure Search service.
        /// </summary>
        /// <param name="searchScope">Scope of the search.</param>
        /// <param name="searchQuery">Query which the user had typed in Messaging Extension search field.</param>
        /// <param name="userObjectId">Azure Active Directory object id of the user.</param>
        /// <param name="count">Number of search results to return.</param>
        /// <param name="skip">Number of search results to skip.</param>
        /// <param name="sortBy">Represents sorting type like: Popularity or Newest.</param>
        /// <param name="filterQuery">Filter bar based query.</param>
        /// <returns>List of search results.</returns>
        public async Task<IEnumerable<TeamPostEntity>> GetTeamPostsAsync(
            TeamPostSearchScope searchScope,
            string searchQuery,
            string userObjectId,
            int? count = null,
            int? skip = null,
            string sortBy = null,
            string filterQuery = null)
        {
            await this.EnsureInitializedAsync();
            IEnumerable<TeamPostEntity> teamPosts = new List<TeamPostEntity>();
            var searchParameters = this.InitializeSearchParameters(searchScope, userObjectId, count, skip, sortBy, filterQuery);

            SearchContinuationToken continuationToken = null;
            var userPrivatePostCollection = new List<TeamPostEntity>();
            var teamPostResult = await this.searchIndexClient.Documents.SearchAsync<TeamPostEntity>(searchQuery, searchParameters);

            if (teamPostResult?.Results != null)
            {
                userPrivatePostCollection.AddRange(teamPostResult.Results.Select(p => p.Document));
                continuationToken = teamPostResult.ContinuationToken;
            }

            if (continuationToken == null)
            {
                return userPrivatePostCollection;
            }

            do
            {
                var teamPostResult1 = await this.searchIndexClient.Documents.ContinueSearchAsync<TeamPostEntity>(continuationToken);

                if (teamPostResult1?.Results != null)
                {
                    userPrivatePostCollection.AddRange(teamPostResult1.Results.Select(p => p.Document));
                    continuationToken = teamPostResult1.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return userPrivatePostCollection;
        }

        /// <summary>
        /// Creates Index, Data Source and Indexer for search service.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task RecreateSearchServiceIndexAsync()
        {
            try
            {
                await this.CreateSearchIndexAsync();
                await this.CreateDataSourceAsync();
                await this.CreateIndexerAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Run the indexer on demand.
        /// </summary>
        /// <returns>A task that represents the work queued to execute</returns>
        public async Task RunIndexerOnDemandAsync()
        {
            await this.searchServiceClient.Indexers.RunAsync(TeamPostIndexerName);
        }

        /// <summary>
        /// Dispose search service instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if already disposed else false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.searchServiceClient.Dispose();
                this.searchIndexClient.Dispose();
            }

            this.disposed = true;
        }

        /// <summary>
        /// Create index, indexer and data source if doesn't exist.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task InitializeAsync()
        {
            try
            {
                // When there is no team post created by user and Messaging Extension is open, table initialization is required here before creating search index or data source or indexer.
                await this.teamPostStorageProvider.GetTeamPostEntityAsync(string.Empty);
                await this.RecreateSearchServiceIndexAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Failed to initialize Azure Search Service: {ex.Message}", SeverityLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Create index in Azure Search service if it doesn't exist.
        /// </summary>
        /// <returns><see cref="Task"/> That represents index is created if it is not created.</returns>
        private async Task CreateSearchIndexAsync()
        {
            if (await this.searchServiceClient.Indexes.ExistsAsync(TeamPostIndexName))
            {
                await this.searchServiceClient.Indexes.DeleteAsync(TeamPostIndexName);
            }

            var tableIndex = new Index()
            {
                Name = TeamPostIndexName,
                Fields = FieldBuilder.BuildForType<TeamPostEntity>(),
            };
            await this.searchServiceClient.Indexes.CreateAsync(tableIndex);
        }

        /// <summary>
        /// Create data source if it doesn't exist in Azure Search service.
        /// </summary>
        /// <returns><see cref="Task"/> That represents data source is added to Azure Search service.</returns>
        private async Task CreateDataSourceAsync()
        {
            if (await this.searchServiceClient.DataSources.ExistsAsync(TeamPostDataSourceName))
            {
                return;
            }

            var dataSource = DataSource.AzureTableStorage(
                TeamPostDataSourceName,
                this.options.ConnectionString,
                TeamPostTableName);

            await this.searchServiceClient.DataSources.CreateAsync(dataSource);
        }

        /// <summary>
        /// Create indexer if it doesn't exist in Azure Search service.
        /// </summary>
        /// <returns><see cref="Task"/> That represents indexer is created if not available in Azure Search service.</returns>
        private async Task CreateIndexerAsync()
        {
            if (await this.searchServiceClient.Indexers.ExistsAsync(TeamPostIndexerName))
            {
                await this.searchServiceClient.Indexers.DeleteAsync(TeamPostIndexerName);
            }

            var indexer = new Indexer()
            {
                Name = TeamPostIndexerName,
                DataSourceName = TeamPostDataSourceName,
                TargetIndexName = TeamPostIndexName,
            };

            await this.searchServiceClient.Indexers.CreateAsync(indexer);
            await this.searchServiceClient.Indexers.RunAsync(TeamPostIndexerName);
        }

        /// <summary>
        /// Initialization of InitializeAsync method which will help in indexing.
        /// </summary>
        /// <returns>Represents an asynchronous operation.</returns>
        private Task EnsureInitializedAsync()
        {
            return this.initializeTask.Value;
        }

        /// <summary>
        /// Initialization of search service parameters which will help in searching the documents.
        /// </summary>
        /// <param name="searchScope">Scope of the search.</param>
        /// <param name="userObjectId">Azure Active Directory object id of the user.</param>
        /// <param name="count">Number of search results to return.</param>
        /// <param name="skip">Number of search results to skip.</param>
        /// <param name="sortBy">Represents sorting type like: Popularity or Newest.</param>
        /// <param name="filterQuery">Filter bar based query.</param>
        /// <returns>Represents an search parameter object.</returns>
        private SearchParameters InitializeSearchParameters(
            TeamPostSearchScope searchScope,
            string userObjectId,
            int? count = null,
            int? skip = null,
            string sortBy = null,
            string filterQuery = null)
        {
            SearchParameters searchParameters = new SearchParameters()
            {
                Top = count ?? ApiSearchResultCount,
                Skip = skip ?? 0,
                IncludeTotalResultCount = false,
                Select = new[]
                {
                    nameof(TeamPostEntity.PostId),
                    nameof(TeamPostEntity.Type),
                    nameof(TeamPostEntity.Title),
                    nameof(TeamPostEntity.Description),
                    nameof(TeamPostEntity.ContentUrl),
                    nameof(TeamPostEntity.Tags),
                    nameof(TeamPostEntity.CreatedDate),
                    nameof(TeamPostEntity.CreatedByName),
                    nameof(TeamPostEntity.UpdatedDate),
                    nameof(TeamPostEntity.UserId),
                    nameof(TeamPostEntity.TotalVotes),
                    nameof(TeamPostEntity.IsRemoved),
                },
                SearchFields = new[] { nameof(TeamPostEntity.Title) },
                Filter = string.IsNullOrEmpty(filterQuery) ? $"({nameof(TeamPostEntity.IsRemoved)} eq false)" : $"({nameof(TeamPostEntity.IsRemoved)} eq false) and ({filterQuery})",
            };

            switch (searchScope)
            {
                case TeamPostSearchScope.AllItems:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    break;

                case TeamPostSearchScope.PostedByMe:
                    searchParameters.Filter = $"{nameof(TeamPostEntity.UserId)} eq '{userObjectId}' " + $"and ({nameof(TeamPostEntity.IsRemoved)} eq false)";
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    break;

                case TeamPostSearchScope.Popular:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.TotalVotes)} desc" };
                    break;

                case TeamPostSearchScope.TeamPreferenceTags:
                    searchParameters.SearchFields = new[] { nameof(TeamPostEntity.Tags) };
                    searchParameters.Top = 5000;
                    searchParameters.Select = new[] { nameof(TeamPostEntity.Tags) };
                    break;

                case TeamPostSearchScope.FilterAsPerTeamTags:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    searchParameters.SearchFields = new[] { nameof(TeamPostEntity.Tags) };
                    break;

                case TeamPostSearchScope.FilterPostsAsPerDateRange:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    searchParameters.Top = 200;
                    break;

                case TeamPostSearchScope.UniqueUserNames:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    searchParameters.Select = new[] { nameof(TeamPostEntity.CreatedByName) };
                    break;

                case TeamPostSearchScope.SearchTeamPostsForTitleText:
                    searchParameters.OrderBy = new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    searchParameters.QueryType = QueryType.Full;
                    searchParameters.SearchFields = new[] { nameof(TeamPostEntity.Title) };
                    break;

                case TeamPostSearchScope.FilterTeamPosts:

                    if (!string.IsNullOrEmpty(sortBy))
                    {
                        searchParameters.OrderBy = sortBy == SortByPopular ? new[] { $"{nameof(TeamPostEntity.TotalVotes)} desc" } : new[] { $"{nameof(TeamPostEntity.UpdatedDate)} desc" };
                    }

                    searchParameters.SearchFields = new[] { nameof(TeamPostEntity.Tags) };
                    break;
            }

            return searchParameters;
        }
    }
}